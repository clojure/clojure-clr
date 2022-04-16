/**
 *   Copyright (c) Rich Hickey. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

/**
 *   Author: David Miller
 **/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using clojure.lang.Runtime;
using Microsoft.Scripting.Hosting;
using RTProperties = clojure.runtime.Properties;


namespace clojure.lang
{
#pragma warning disable IDE1006 // Naming Styles 
    public static class RT
    {
        #region Default symbol-to-class map

        //simple-symbol->class
        public static readonly IPersistentMap DefaultImports = PersistentHashMap.create((IDictionary)CreateDefaultImportDictionary());

        static Dictionary<Symbol,Type> CreateDefaultImportDictionary()
        {
            var q = GetAllTypesInNamespace("System");
            var d = q.ToDictionary(keySelector: t => Symbol.intern(t.Name));

            // ADDED THESE TO SUPPORT THE BOOTSTRAPPING IN THE JAVA CORE.CLJ
            d.Add(Symbol.intern("StringBuilder"), typeof(StringBuilder));
            d.Add(Symbol.intern("BigInteger"), typeof(clojure.lang.BigInteger));
            d.Add(Symbol.intern("BigDecimal"), typeof(clojure.lang.BigDecimal));

            return d;
        }

        static IEnumerable<Type> GetAllTypesInNamespace(string nspace)
        {
            var q = AppDomain.CurrentDomain.GetAssemblies()
                       .SelectMany(t => t.GetTypes())
                       .Where(t => (t.IsClass || t.IsInterface || t.IsValueType) &&
                                    t.Namespace == nspace &&
                                    t.IsPublic &&
                                    !t.IsGenericTypeDefinition &&
                                    !t.Name.StartsWith("_") &&
                                    !t.Name.StartsWith("<"));
           
            return q; 
        }

     
        #endregion

        #region Some misc. goodies

        public static readonly object[] EmptyObjectArray = new Object[] { };

        static readonly RTProperties _versionProperties = new RTProperties();

        public static RTProperties GetVersionProperties() { return _versionProperties; }

        // Note that we name the environment variable such that it can be
        // directly manipulated as a POSIX shell identifier, which is useful
        // for folks using Cygwin and its ilk.
        public const string ClojureLoadPathString = "CLOJURE_LOAD_PATH";

        static public readonly Object REQUIRE_LOCK = new Object();

        #endregion

        #region  It's true (or not)

        // The JVM version has these to provide standard boxed values for true/false.
        // I've tried it in the CLR version, two ways: with the types of the as bool and the types as Object.
        // Very little difference between those.
        // However, getting rid of them entirely speeds up compilation by about 25%.
        // Thus, I'm thinking boxing bools is less expensive than a static field lookup.

        //public static readonly Object T = true;//Keyword.intern(Symbol.intern(null, "t"));
        //public static readonly Object F = false;//Keyword.intern(Symbol.intern(null, "t"));

        public static bool IsTrue(object o)
        {
            if (o == null)
                return false;
            if (o is Boolean boolean)
                return boolean;
            else
                return true;
        }

        #endregion

        #region Predefined namespaces

        // We need this initialization to happen earlier than most of the Var inits.
        public static readonly Namespace ClojureNamespace 
            = Namespace.findOrCreate(Symbol.intern("clojure.core"));

        #endregion

        #region Useful Keywords

        public static readonly Keyword TagKey 
            = Keyword.intern(null, "tag");

        public static readonly Keyword ConstKey
            = Keyword.intern(null, "const");

        public static readonly Keyword EvalFileKey
            = Keyword.intern("clojure.core", "eval-file");

        public static readonly Keyword LineKey 
            = Keyword.intern(null, "line");

        public static readonly Keyword ColumnKey
            = Keyword.intern(null, "column");

        public static readonly Keyword FileKey
            = Keyword.intern(null, "file");

        public static readonly Keyword SourceSpanKey
            = Keyword.intern(null, "source-span");

        public static readonly Keyword StartLineKey
            = Keyword.intern(null, "start-line");

        public static readonly Keyword StartColumnKey
            = Keyword.intern(null, "start-column");

        public static readonly Keyword EndLineKey
            = Keyword.intern(null, "end-line");
        
        public static readonly Keyword EndColumnKey
            = Keyword.intern(null, "end-column");

        public static readonly Keyword DeclaredKey
            = Keyword.intern(null, "declared");

        public static readonly Keyword DocKey
            = Keyword.intern(null, "doc");

        #endregion

        #region Vars (namespace-related)

        public static readonly Var CurrentNSVar
            = Var.intern(ClojureNamespace, Symbol.intern("*ns*"), ClojureNamespace).setDynamic();


        public static readonly Var InNSVar
            //= Var.intern(CLOJURE_NS, Symbol.intern("in-ns"), RT.F);
            = Var.intern(ClojureNamespace, Symbol.intern("in-ns"), false);

        public static readonly Var NSVar 
            //= Var.intern(CLOJURE_NS, Symbol.intern("ns"), RT.F);
            = Var.intern(ClojureNamespace, Symbol.intern("ns"), false);

        #endregion

        #region Vars (I/O-related)

        public static readonly Var OutVar 
            = Var.intern(ClojureNamespace, Symbol.intern("*out*"), new StreamWriter(System.Console.OpenStandardOutput())).setDynamic();
        
        public static readonly Var ErrVar
            = Var.intern(ClojureNamespace, Symbol.intern("*err*"), new StreamWriter(System.Console.OpenStandardError())).setDynamic();

        public static readonly Var InVar =
            Var.intern(ClojureNamespace, Symbol.intern("*in*"),
            new clojure.lang.LineNumberingTextReader(new StreamReader(System.Console.OpenStandardInput()))).setDynamic();

        static readonly Var PrintReadablyVar
            //= Var.intern(CLOJURE_NS, Symbol.intern("*print-readably*"), RT.T);
            = Var.intern(ClojureNamespace, Symbol.intern("*print-readably*"), true).setDynamic();
        
        public static readonly Var PrintMetaVar 
            //= Var.intern(CLOJURE_NS, Symbol.intern("*print-meta*"), RT.F);
            = Var.intern(ClojureNamespace, Symbol.intern("*print-meta*"), false).setDynamic();
        
        public static readonly Var PrintDupVar 
            //= Var.intern(CLOJURE_NS, Symbol.intern("*print-dup*"), RT.F);
            = Var.intern(ClojureNamespace, Symbol.intern("*print-dup*"), false).setDynamic();

        // We need this Var initializaed early on, I'm willing to waste the local init overhead to keep the code here with the others.
        static readonly Var FlushOnNewlineVar
            //= Var.intern(CLOJURE_NS, Symbol.intern("*flush-on-newline*"), RT.T);
            = Var.intern(ClojureNamespace, Symbol.intern("*flush-on-newline*"), true).setDynamic();

        static readonly Var PrintInitializedVar 
            = Var.intern(ClojureNamespace, Symbol.intern("print-initialized")).setDynamic();
        
        static readonly Var PrOnVar 
            = Var.intern(ClojureNamespace, Symbol.intern("pr-on"));

        public static readonly Var AllowSymbolEscapeVar
            = Var.intern(ClojureNamespace, Symbol.intern("*allow-symbol-escape*"), true).setDynamic();

        #endregion

        #region Vars (miscellaneous)

        public static readonly Var AllowUnresolvedVarsVar
            //= Var.intern(CLOJURE_NS, Symbol.intern("*allow-unresolved-vars*"), RT.F);
            = Var.intern(ClojureNamespace, Symbol.intern("*allow-unresolved-vars*"), false).setDynamic();

        public static readonly Var ReaderResolverVar
            = Var.intern(ClojureNamespace, Symbol.intern("*reader-resolver*"), null).setDynamic();

        public static readonly Var WarnOnReflectionVar
            //= Var.intern(CLOJURE_NS, Symbol.intern("*warn-on-reflection*"), RT.F);
            = Var.intern(ClojureNamespace, Symbol.intern("*warn-on-reflection*"), false).setDynamic();
 
        //public static readonly Var MACRO_META 
        //    = Var.intern(CLOJURE_NS, Symbol.intern("*macro-meta*"), null);

        public static readonly Var MathContextVar
            = Var.intern(ClojureNamespace, Symbol.intern("*math-context*"), null).setDynamic();
        
        public static readonly Var AgentVar
            = Var.intern(ClojureNamespace, Symbol.intern("*agent*"), null).setDynamic();

        static readonly Object _readeval = ReadTrueFalseUnknown(Environment.GetEnvironmentVariable("CLOJURE_READ_EVAL") ?? Environment.GetEnvironmentVariable("clojure.read.eval") ?? "true");
            
        public static readonly Var ReadEvalVar
            = Var.intern(ClojureNamespace, Symbol.intern("*read-eval*"),_readeval).setDynamic();

        public static readonly Var DataReadersVar
            = Var.intern(ClojureNamespace, Symbol.intern("*data-readers*"), RT.map()).setDynamic();

        public static readonly Var DefaultDataReaderFnVar
            = Var.intern(ClojureNamespace, Symbol.intern("*default-data-reader-fn*"), RT.map());

        public static readonly Var DefaultDataReadersVar
           = Var.intern(ClojureNamespace, Symbol.intern("default-data-readers"), RT.map());

        public static readonly Var SuppressReadVar 
            = Var.intern(ClojureNamespace, Symbol.intern("*suppress-read*"), null).setDynamic();

        public static readonly Var AssertVar
            //= Var.intern(CLOJURE_NS, Symbol.intern("*assert*"), RT.T);
            = Var.intern(ClojureNamespace, Symbol.intern("*assert*"), true).setDynamic();

        public static readonly Var CmdLineArgsVar
            = Var.intern(ClojureNamespace, Symbol.intern("*command-line-args*"), null).setDynamic();

        public static readonly Var UseContextClassloaderVar
            //= Var.intern(CLOJURE_NS, Symbol.intern("*use-context-classloader*"), RT.T);
            = Var.intern(ClojureNamespace, Symbol.intern("*use-context-classloader*"), true).setDynamic();

        // boolean
        public static readonly Var UncheckedMathVar = Var.intern(Namespace.findOrCreate(Symbol.intern("clojure.core")),
            Symbol.intern("*unchecked-math*"), false).setDynamic();

        #endregion

        #region  Clojure-environment IFns needing support

        static readonly Symbol InNsSymbol = Symbol.intern("in-ns");

        sealed class InNamespaceFn : AFn
        {
            public override object invoke(object arg1)
            {
                Symbol nsname = (Symbol)arg1;
                Namespace ns = Namespace.findOrCreate(nsname);
                CurrentNSVar.set(ns);
                return ns;
            }
        }
        static readonly Symbol NsSymbol = Symbol.intern("ns");

        sealed class BootNamespaceFN : AFn
        {
            public override object invoke(object __form, object __env, object arg1)
            {
                Symbol nsname = (Symbol)arg1;
                Namespace ns = Namespace.findOrCreate(nsname);
                CurrentNSVar.set(ns);
                return ns;
            }
        }


        //static readonly Symbol IDENTICAL = Symbol.intern("identical?");

        //sealed class IdenticalFn : AFn
        //{
        //    public override object invoke(object arg1, object arg2)
        //    {
        //        //return Object.ReferenceEquals(arg1, arg2) ? RT.T : RT.F;
        //        if ( arg1 is ValueType )
        //            //return arg1.Equals(arg2) ? RT.T : RT.F;
        //            return arg1.Equals(arg2);
        //        else
        //            //return arg1 == arg2 ? RT.T : RT.F;
        //            return arg1 == arg2;
        //    }
        //}

        static readonly Symbol LoadFileSymbol = Symbol.intern("load-file");

        sealed class LoadFileFn : AFn
        {
            public override object invoke(object arg1)
            {
                return Compiler.loadFile(arg1.ToString());
            }
        }

        #endregion

        #region Initialization

        static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            AssemblyName asmName = new AssemblyName(args.Name);
            var name = asmName.Name;
            var stream = GetEmbeddedResourceStream(name, out _);
            if (stream == null)
            {
                name += ".dll";
                stream = GetEmbeddedResourceStream(name, out _);
                if (stream == null)
                    return null;
            }
            return Assembly.Load(ReadStreamBytes(stream));
        }

#if MONO
        public static bool checkSpecAsserts = ReadTrueFalseDefault(Environment.GetEnvironmentVariable("CLOJURE_SPEC_CHECK_ASSERTS"), false);
#else
        public static bool checkSpecAsserts = ReadTrueFalseDefault(Environment.GetEnvironmentVariable("clojure.spec.check-asserts"), false);
#endif

#if MONO
        public static bool instrumentMacros = !ReadTrueFalseDefault(Environment.GetEnvironmentVariable("CLOJURE_SPEC_SKIP_MACROS"), false);
#else
        public static bool instrumentMacros = !ReadTrueFalseDefault(Environment.GetEnvironmentVariable("clojure.spec.skip-macros"), false);
#endif
        
        internal static volatile bool CHECK_SPECS = false;

        static RT()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

            // TODO: Check for existence of ClojureContext.Default before doing this?

            ScriptRuntimeSetup setup = new ScriptRuntimeSetup();
            LanguageSetup lsetup = new LanguageSetup(
                typeof(ClojureContext).AssemblyQualifiedName,
                ClojureContext.ClojureDisplayName,
                ClojureContext.ClojureNames.Split(new Char[] { ';' }),
                ClojureContext.ClojureFileExtensions.Split(new Char[] { ';' }));


            setup.LanguageSetups.Add(lsetup);
            ScriptRuntime env = new ScriptRuntime(setup);
            env.GetEngine("clj");


            _versionProperties.LoadFromString(clojure.lang.Properties.Resources.version); 

            Keyword arglistskw = Keyword.intern(null, "arglists");
            Symbol namesym = Symbol.intern("name");

            OutVar.Tag = Symbol.intern("System.IO.TextWriter");

            CurrentNSVar.Tag = Symbol.intern("clojure.lang.Namespace");

            AgentVar.setMeta(map(DocKey, "The agent currently running an action on this thread, else nil."));
            AgentVar.Tag = Symbol.intern("clojure.lang.Agent");

            // We don't have MathContext (yet)
            //MATH_CONTEXT.Tag = Symbol.intern("java.math.MathContext");

            //// during bootstrap, ns same as in-ns
            //Var nv = Var.intern(CLOJURE_NS, NAMESPACE, new InNamespaceFn());
            Var nv = Var.intern(ClojureNamespace, NsSymbol, new BootNamespaceFN());
            nv.setMacro();

            Var v;
            v = Var.intern(ClojureNamespace, InNsSymbol, new InNamespaceFn());
            v.setMeta(map(DocKey, "Sets *ns* to the namespace named by the symbol, creating it if needed.",
                arglistskw, list(vector(namesym))));

            v = Var.intern(ClojureNamespace, LoadFileSymbol, new LoadFileFn());
            v.setMeta(map(DocKey, "Sequentially read and evaluate the set of forms contained in the file.",
                arglistskw, list(vector(namesym))));

            //v = Var.intern(CLOJURE_NS, IDENTICAL, new IdenticalFn());
            //v.setMeta(map(dockw, "tests if 2 arguments are the same object",
            //    arglistskw, list(vector(Symbol.intern("x"), Symbol.intern("y")))));

            // load Clojure.Source.dll to pick up all the clojure source files as embedded resources.
            // If not found, we hope that the source files core.clj, etc. are available on the standard file search path


            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            try
            {
                Assembly.LoadFile(Path.Combine(baseDir, "Clojure.Source.dll"));
            }
            catch (FileLoadException)
            {
                // this is okay.  It just means that the assets clojure/core.clj and company are going to be somewhere else
            }
            catch (FileNotFoundException)
            {
                // this is okay.  It just means that the assets clojure/core.clj and company are going to be somewhere else
            }

            if ( RuntimeBootstrapFlag._doRTBootstrap )
                load("clojure/core");
        }

        public static void LoadSpecCode()
        {
            try
            {
                Var.pushThreadBindings(RT.map(Compiler.CompileFilesVar, false));
                // We need to prevent loading more than once.
                IFn require = clojure.clr.api.Clojure.var("clojure.core", "require");
                require.invoke(clojure.clr.api.Clojure.read("clojure.spec.alpha"));
                require.invoke(clojure.clr.api.Clojure.read("clojure.core.specs.alpha"));
                //load("clojure/spec/alpha");
                //load("clojure/core/specs/alpha");
            }
            finally
            {
                Var.popThreadBindings();
            }
        }

        public static void Init()
        {
            DoInit();
        }

        private static bool INIT = false; // init guard

        [MethodImpl(MethodImplOptions.Synchronized)]
        static void DoInit()
        {
            if (INIT) { return; } else { INIT = true; }


            // load spec
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                Assembly.LoadFile(Path.Combine(baseDir, "clojure.spec.alpha.dll"));
                Assembly.LoadFile(Path.Combine(baseDir, "clojure.core.specs.alpha.dll"));
            }

            PostBootstrapInit();

            CHECK_SPECS = RT.instrumentMacros;
        }

        public static void PostBootstrapInit()
        {
            Var.pushThreadBindings(
                RT.mapUniqueKeys(CurrentNSVar, CurrentNSVar.deref(),
                WarnOnReflectionVar, WarnOnReflectionVar.deref(),
                RT.UncheckedMathVar, RT.UncheckedMathVar.deref()));
            try
            {
                Symbol USER = Symbol.intern("user");
                Symbol CLOJURE = Symbol.intern("clojure.core");

                Var in_ns = var("clojure.core", "in-ns");
                Var refer = var("clojure.core", "refer");
                in_ns.invoke(USER);
                refer.invoke(CLOJURE);
                MaybeLoadCljScript("user.clj");

                // start socket servers
                Var require = var("clojure.core", "require");
                Symbol SERVER = Symbol.intern("clojure.core.server");
                require.invoke(SERVER);
                Var start_servers = var("clojure.core.server", "start-servers");
                start_servers.invoke(Environment.GetEnvironmentVariables());
            }
            finally
            {
                Var.popThreadBindings();
            }
        }

        #endregion

        #region Id generation

        // This is AtomicInteger in the JVM version.
        // The only place accessed is in nextID, so seems unnecessary.
        private static int _id;

        // initial-lowercase name, used in core.clj

        static public int nextID()
        {
            return Interlocked.Increment(ref _id);
        }

        #endregion

        #region Keyword support


        public static Keyword keyword(string ns, string name)
        {
            return Keyword.intern((Symbol.intern(ns, name)));
        }

        #endregion

        #region Var support


        static public Var var(String ns, String name)
        {
            return Var.intern(Namespace.findOrCreate(Symbol.intern(null, ns)), Symbol.intern(null, name));
        }


        static public Var var(String ns, String name, Object init)
        {
            return Var.intern(Namespace.findOrCreate(Symbol.intern(null, ns)), Symbol.intern(null, name), init);
        }

        #endregion

        #region Collections support

        private const int CHUNK_SIZE = 32;
        
        // Because of the need to look before you leap (make sure one element exists)
        // this is more complicated than the JVM version:  In JVM-land, you can hasNext before you move.


        public static ISeq chunkEnumeratorSeq(IEnumerator iter)
        {
            if (!iter.MoveNext())
                return null;

            return PrimedChunkEnumeratorSeq(iter);
        }

        private static ISeq PrimedChunkEnumeratorSeq(IEnumerator iter)
        {
            return new LazySeq(new ChunkEnumeratorSeqHelper(iter));
        }

        private class ChunkEnumeratorSeqHelper : AFn
        {
            readonly IEnumerator _iter;

            public ChunkEnumeratorSeqHelper(IEnumerator iter)
            {
                _iter = iter;
            }

            // Assumes MoveNext has already been called on _iter.
            public override object invoke()
            {
                object[] arr = new object[CHUNK_SIZE];
                bool more = true;
                int n = 0;
                for (; n < CHUNK_SIZE && more; ++n)
                {
                    arr[n] = _iter.Current;
                    more = _iter.MoveNext();
                }

                return new ChunkedCons(new ArrayChunk(arr, 0, n), more ?  PrimedChunkEnumeratorSeq(_iter) : null);
            }
        }



        public static ISeq seq(object coll)
        {
            if (coll is ASeq aseq)
                return aseq;

            return coll is LazySeq lseq ? lseq.seq() : seqFrom(coll);
        }

        // N.B. canSeq must be kept in sync with this!

        private static ISeq seqFrom(object coll)
        {
            if (coll == null)
                return null;

            if (coll is Seqable seq)
                return seq.seq();

            if (coll.GetType().IsArray)
                return ArraySeq.createFromObject(coll);

            if (coll is string str)
                return StringSeq.create(str);

            if (coll is IEnumerable ie)  // java: Iterable  -- reordered clauses so others take precedence.
                return chunkEnumeratorSeq(ie.GetEnumerator());            // chunkIteratorSeq

            // The equivalent for Java:Map is IDictionary.  IDictionary is IEnumerable, so is handled above.
            //else if(coll isntanceof Map)  
            //     return seq(((Map) coll).entrySet());
            // Used to be in the java version:
            //else if (coll is IEnumerator)  // java: Iterator
            //    return EnumeratorSeq.create((IEnumerator)coll);

            throw new ArgumentException("Don't know how to create ISeq from: " + coll.GetType().FullName);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        static public bool canSeq(object coll)
        {
            return coll == null
                || coll is ISeq
                || coll is Seqable
                || coll.GetType().IsArray
                || coll is String
                || coll is IEnumerable;
        }
        static IEnumerable NullIterator()
        {
            yield break;
        }

        static IEnumerable StringIterator(string str)
        {
            for (int i = 0; i < str.Length; i++)
                yield return str[i];
        }


        public static IEnumerator iter(Object coll)
        {
            if (coll == null)
                return NullIterator().GetEnumerator();

            // handled by IEnumerable case above
            if (coll is IDictionary dict)
                return dict.GetEnumerator();

            // reordered
            if (coll is IEnumerable ie)
                return ie.GetEnumerator();

            if (coll is string str)
                return StringIterator(str).GetEnumerator();

            if (coll.GetType().IsArray)
                return ArrayIter.createFromObject(coll);

            return iter(seq(coll));
        }


        public static object seqOrElse(object o)
        {
            return seq(o) == null ? null : o;
        }


        public static ISeq keys(object coll)
        {
            if (coll is IPersistentMap ipm)
                return APersistentMap.KeySeq.createFromMap(ipm);
            return APersistentMap.KeySeq.create(seq(coll));
        }


        public static ISeq vals(object coll)
        {
            if (coll is IPersistentMap ipm)
                return APersistentMap.ValSeq.createFromMap(ipm);
            return APersistentMap.ValSeq.create(seq(coll));
        }


        public static IPersistentMap meta(object x)
        {
            return x is IMeta m ? m.meta() : null;
        }


        public static int count(object o)
        {
            return o is Counted c ? c.count() : CountFrom(Util.Ret1(o, o = null));
        }

        static int CountFrom(object o)
        {
            if (o == null)
                return 0;

            if (o is IPersistentCollection)
            {
                ISeq s = seq(o);
                o = null;
                int i = 0;
                for (; s != null; s = s.next())
                {
                    if (s is Counted)
                        return i + s.count();
                    i++;
                }
                return i;
            }

            if (o is string str)
                return str.Length;

            if (o is ICollection c)
                return c.Count;

            if (o is IDictionary d)
                return d.Count;

            if (o is DictionaryEntry)
                return 2;

            if (o.GetType().IsGenericType && o.GetType().Name == "KeyValuePair`2")
                return 2;

            if (o is Array a)
                return a.GetLength(0);

            throw new InvalidOperationException("count not supported on this type: " + Util.NameForType(o.GetType()));
        }


        public static IPersistentCollection conj(IPersistentCollection coll, Object x)
        {
            if (coll == null)
                return new PersistentList(x);
            return coll.cons(x);
        }


        public static ISeq cons(object x, object coll)
        {
            if (coll == null)
                return new PersistentList(x);


            if (coll is ISeq s)
                return new Cons(x, s);

            return new Cons(x, seq(coll));
        }


        public static object first(object x)
        {
            ISeq seq = x as ISeq ?? RT.seq(x);

            if (seq == null)
                return null;

            return seq.first();
        }


        public static object second(object x)
        {
            return first(next(x));
        }


        public static object third(object x)
        {
            return first(next(next(x)));
        }


        public static object fourth(object x)
        {
            return first(next(next(next(x))));
        }


        public static ISeq next(object x)
        {
            ISeq seq = (x as ISeq) ?? RT.seq(x);

            if (seq == null)
                return null;

            return seq.next();
        }


        public static ISeq more(object x)
        {
            ISeq seq = x as ISeq ?? RT.seq(x);

            if (seq == null)
                return PersistentList.EMPTY;

            return seq.more();
        }




        public static object peek(object x)
        {
            return x == null
                ? null
                : ((IPersistentStack)x).peek();
        }


        public static object pop(object x)
        {
            return x == null
                ? null
                : ((IPersistentStack)x).pop();
        }


        static public Object get(Object coll, Object key)
        {

            if (coll is ILookup ilu)
                return ilu.valAt(key);

            return GetFrom(coll, key);
        }

        static object GetFrom(object coll, object key)
        {
            if (coll == null)
                return null;

            if (coll is IDictionary m)
                return m[key];


            if (coll is IPersistentSet set)
                return set.get(key);


            if (Util.IsNumeric(key) && (coll is string || coll.GetType().IsArray))
            {
                int n = Util.ConvertToInt(key);
                return n >= 0 && n < count(coll) ? nth(coll, n) : null;
            }

            if (coll is ITransientSet tset)
                return tset.get(key);

            return null;
        }


        static public Object get(Object coll, Object key, Object notFound)
        {
            if (coll is ILookup ilu)
                return ilu.valAt(key, notFound);

            return GetFrom(coll, key, notFound);
        }

        static object GetFrom(object coll, object key, object notFound)
        {
           if (coll == null)
                return notFound;


            if (coll is IDictionary m)
            {
                if (m.Contains(key))
                    return m[key];
                return notFound;
            }

            if (coll is IPersistentSet set)
            {
                if (set.contains(key))
                    return set.get(key);
                return notFound;
            }

            if (Util.IsNumeric(key) && (coll is string || coll.GetType().IsArray))
            {
                int n = Util.ConvertToInt(key);
                return n >= 0 && n < count(coll) ? nth(coll, n) : notFound;
            }

            if (coll is ITransientSet tset)
            {
                if (tset.contains(key))
                    return tset.get(key);
                return notFound;
            }

            return notFound;
        }


        public static Associative assoc(object coll, object key, Object val)
        {
            if (coll == null)
                return new PersistentArrayMap(new object[] { key, val });
            return ((Associative)coll).assoc(key, val);
        }


        public static object contains(object coll, object key)
        {
            if (coll == null)
                //return RT.F;
                return false;

            if (coll is Associative assoc)
                //return ((Associative)coll).containsKey(key) ? RT.T : RT.F;
                return assoc.containsKey(key);

            if (coll is IPersistentSet set)
                //return ((IPersistentSet)coll).contains(key) ? RT.T : RT.F;
                return set.contains(key);


            if (coll is IDictionary m)
            {
                //return m.Contains(key) ? RT.T : RT.F;
                return key != null && m.Contains(key);
            }

            // TODO: Make this work for ISet<T> no matter the T
            if (coll is ISet<object> iso)
            {
                // return  iso.Contains(key) ? RT.T : RT.F;
                return iso.Contains(key);
            }

            if (Util.IsNumeric(key) && (coll is String || coll.GetType().IsArray))
            {
                int n = Util.ConvertToInt(key);
                return n >= 0 && n < count(coll);
            }

            if (coll is ITransientSet tset)
                return tset.contains(key);  // RT.T, RT.F

            if (coll is ITransientAssociative2 ta2)
                return ta2.containsKey(key);    // RT.T, RT.F

            throw new ArgumentException("contains? not supported on type: " + coll.GetType().Name);
        }


        public static object find(object coll, object key)
        {
            if (coll == null)
                return null;

            if (coll is Associative assoc)
                return assoc.entryAt(key);

            if (coll is IDictionary m)
            {
                if (m.Contains(key))
                    return MapEntry.create(key, m[key]);
                return null;
            }

            if (coll is ITransientAssociative2 ta2)
                return ta2.entryAt(key);

            return null;
        }


        public static object dissoc(object coll, object key)
        {
            return coll == null
                ? null
                : ((IPersistentMap)coll).without(key);
        }

        //static public Object nth(object coll, long n)
        //{
        //    return nth(coll, (int)n);
        //}

        static public bool SupportsRandomAccess(object coll)
        {
            return coll is Indexed ||
                coll == null ||
                coll is String ||
                coll.GetType().IsArray ||
                coll is IList ||
                coll is DictionaryEntry ||
                coll is IMapEntry ||
                coll is JReMatcher ||
                coll is Match ||
                (coll.GetType().IsGenericType && coll.GetType().Name == "KeyValuePair`2");
        }


        static public Object nth(object coll, int n)
        {

            if (coll is Indexed indexed)
                return indexed.nth((int)n);

            return NthFrom(Util.Ret1(coll, coll = null), (int)n);
        }

        static object NthFrom(object coll, int n)
        {
            if (coll == null)
                return null;

            if (coll is string str)
                return str[n];

            if (coll.GetType().IsArray)
                return Reflector.prepRet(coll.GetType().GetElementType(),((Array)coll).GetValue(n));

            // Java has RandomAccess here.  CLR has no equiv.
            // Trying to replace it with IList caused some real problems,  See the fix in ASeq.

            if (coll is IList ilist)
                return ilist[n];

            if (coll is JReMatcher jrem)
                return jrem.group(n);

            if (coll is Match match)
                return match.Groups[n];

            if (coll is DictionaryEntry e)
            {
                if (n == 0)
                    return e.Key;
                else if (n == 1)
                    return e.Value;
                throw new ArgumentOutOfRangeException("n");
            }

            if (coll.GetType().IsGenericType && coll.GetType().Name == "KeyValuePair`2")
            {
                if (n == 0)
                    return coll.GetType().InvokeMember("Key", BindingFlags.GetProperty, null, coll, null);
                else if (n == 1)
                    return coll.GetType().InvokeMember("Value", BindingFlags.GetProperty, null, coll, null);
                throw new ArgumentOutOfRangeException("n");
            }

            if (coll is IMapEntry me)
            {
                if (n == 0)
                    return me.key();
                else if (n == 1)
                    return me.val();
                throw new ArgumentOutOfRangeException("n");
            }

            if (coll is Sequential)
            {
                ISeq seq = RT.seq(coll);
                coll = null;
                for (int i = 0; i <= n && seq != null; ++i, seq = seq.next())
                {
                    if (i == n)
                        return seq.first();
                }
                throw new ArgumentOutOfRangeException("n");
            }
            
            throw new InvalidOperationException("nth not supported on this type: " + Util.NameForType(coll.GetType()));
        }


        //static public Object nth(Object coll, long n, Object notFound)
        //{
        //    return nth(coll, (int)n, notFound);
        //}


        static public Object nth(Object coll, int n, Object notFound)
        {

            if (coll is Indexed v)
                return v.nth(n, notFound);

            return NthFrom(coll, n, notFound);
        }

        static object NthFrom(object coll, int n, object notFound)
        {
            if (coll == null)
                return notFound;
 
            if (n < 0)
                return notFound;

            if (coll is string s)
            {
                if (n < s.Length)
                    return s[n];
                return notFound;
            }

            if (coll.GetType().IsArray)
            {
                Array a = (Array)coll;
                if (n < a.Length)
                    return Reflector.prepRet(a.GetType().GetElementType(),a.GetValue(n)); 
                return notFound;
            }

            // Causes a problem with infinite LazySequences
            // Four years after the fact, I now know why the change was made in Java Rev 1218.
            // There is no RandomAccess equivalent in CLR.
            // So we don't blow off IList's completely, I put this after the code that catches LazySeqs.
            //IList list = coll as IList;
            //if (list != null)   // Changed to RandomAccess in Java Rev 1218.  
            //{
            //    if (n < list.Count)
            //        return list[n];
            //    return notFound;
            //}

            if (coll is JReMatcher jrem)
            {
                if (jrem.IsUnrealizedOrFailed)
                    return notFound;
                int groups = jrem.groupCount();
                if (groups > 0 && n <= jrem.groupCount())
                    return jrem.group(n);
                return notFound;
            }

            if (coll is Match m)
            {
                if (n < m.Groups.Count)
                    return m.Groups[n];
                return notFound;
            }


            if (coll is DictionaryEntry e)
            {
                if (n == 0)
                    return e.Key;
                else if (n == 1)
                    return e.Value;
                return notFound;
            }

            if (coll.GetType().IsGenericType && coll.GetType().Name == "KeyValuePair`2")
            {
                if (n == 0)
                    return coll.GetType().InvokeMember("Key", BindingFlags.GetProperty, null, coll, null);
                else if (n == 1)
                    return coll.GetType().InvokeMember("Value", BindingFlags.GetProperty, null, coll, null);
                return notFound;
            }

            if (coll is IMapEntry me)
            {
                if (n == 0)
                    return me.key();
                else if (n == 1)
                    return me.val();
                return notFound;
            }

            if (coll is Sequential)
            {
                ISeq seq = RT.seq(coll);
                coll = null;  // release in case GC
                for (int i = 0; i <= n && seq != null; ++i, seq = seq.next())
                {
                    if (i == n)
                        return seq.first();
                }
                return notFound;
            }

            if (coll is IList list)
            {
                if (n < list.Count)
                    return list[n];
                return notFound;
            }

            throw new InvalidOperationException("nth not supported on this type: " + Util.NameForType(coll.GetType()));
        }

        #endregion

        #region boxing/casts

        #region box

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        static public Object box(Object x)
        {
            return x;
        }

        #endregion

        #region char casting


        public static char charCast(object x)
        {
            if (x is char ch)
                return ch;

            long n = Util.ConvertToLong(x);
            if (n < Char.MinValue || n > char.MaxValue)
                throw new ArgumentException("Value out of range for char: " + x);

            return (char)n;
        }

        // JVM version has the following, but they really seem to mess things up for me.

        //public static char charCast(byte x)
        //{
        //    char i = (char)x;
        //    if (i != x)
        //        throw new ArgumentException("Value out of range for char: " + x);
        //    return i;
        //}

        //public static char charCast(sbyte x)
        //{
        //    char i = (char)x;
        //    if (i != x)
        //        throw new ArgumentException("Value out of range for char: " + x);
        //    return i;
        //}

        //public static char charCast(short x)
        //{
        //    char i = (char)x;
        //    if (i != x)
        //        throw new ArgumentException("Value out of range for char: " + x);
        //    return i;
        //}

        //public static char charCast(ushort x)
        //{
        //    char i = (char)x;
        //    if (i != x)
        //        throw new ArgumentException("Value out of range for char: " + x);
        //    return i;
        //}

        //public static char charCast(int x)
        //{
        //    char i = (char)x;
        //    if (i != x)
        //        throw new ArgumentException("Value out of range for char: " + x);
        //    return i;
        //}

        //public static char charCast(uint x)
        //{
        //    char i = (char)x;
        //    if (i != x)
        //        throw new ArgumentException("Value out of range for char: " + x);
        //    return i;
        //}


        public static char charCast(long x)
        {
            char i = (char)x;
            if (i != x)
                throw new ArgumentException("Value out of range for char: " + x);
            return i;
        }

        //public static char charCast(ulong x)
        //{
        //    char i = (char)x;
        //    if (i != x)
        //        throw new ArgumentException("Value out of range for char: " + x);
        //    return i;
        //}

        //public static char charCast(float x)
        //{
        //    if (x >= Char.MinValue && x <= char.MaxValue)
        //        return (char)x;
        //    throw new ArgumentException("Value out of range for char: " + x);
        //}


        public static char charCast(double x)
        {
            return x >= Char.MinValue && x <= char.MaxValue ? (char)x : throw new ArgumentException("Value out of range for char: " + x);
        }

        #endregion

        #region bool casting


        static public bool booleanCast(object x)
        {
            return x is Boolean b ? b : x != null;
        }


        static public bool booleanCast(bool x)
        {
            return x;
        }

        #endregion

        #region casting misc numeric types from object


        public static byte byteCast(object x)
        {
            if (x is byte b)
                return b;

            long n = Util.ConvertToLong(x);
            if (n < Byte.MinValue || n > Byte.MaxValue)
                throw new ArgumentException("Value out of range for byte: " + x);

            return (byte)n; 
        }


        public static sbyte sbyteCast(object x)
        {
            if (x is sbyte b)
                return b;

            long n = Util.ConvertToLong(x);
            if (n < SByte.MinValue || n > SByte.MaxValue)
                throw new ArgumentException("Value out of range for byte: " + x);

            return (sbyte)n;
        }



        public static short shortCast(object x)
        {
            if (x is short s)
                return s;
            long n = Util.ConvertToLong(x);
            if (n < short.MinValue || n > short.MaxValue)
                throw new ArgumentException("Value out of range for short: " + x);

            return (short)n; 
        }


        public static ushort ushortCast(object x)
        {
            if (x is ushort u)
                return u;
            long n = Util.ConvertToLong(x);
            if (n < ushort.MinValue || n > ushort.MaxValue)
                throw new ArgumentException("Value out of range for ushort: " + x);

            return (ushort)n;
        }


        public static uint uintCast(object x)
        {
            if (x is uint u)
                return u;
            long n = Util.ConvertToLong(x);
            if (n < uint.MinValue || n > uint.MaxValue)
                throw new ArgumentException("Value out of range for uint: " + x);

            return (uint)n;
        }


        public static ulong ulongCast(object x)
        {
            return Convert.ToUInt64(x);
        }


        public static decimal decimalCast(object x)
        {
            return Convert.ToDecimal(x);
        }

        #endregion

        #region int casting

        public static int intCast(object x)
        {
            if (x is int i)
                return i;
            return intCast(longCast(x));
        }


        static public int intCast(char x)
        {
            return x;
        }


        static public int intCast(byte x)
        {
            return x;
        }


        static public int intCast(sbyte x)
        {
            return x;
        }


        static public int intCast(short x)
        {
            return x;
        }


        static public int intCast(ushort x)
        {
            return x;
        }


        static public int intCast(int x)
        {
            return x;
        }


        static public int intCast(uint x)
        {
            if (x > int.MaxValue)
                throw new ArgumentException("Value out of range for int: " + x);

            return (int)x;
        }


        static public int intCast(float x)
        {
            if (x < int.MinValue || x > int.MaxValue)
                throw new ArgumentException("Value out of range for int: " + x);

            return (int)x;
        }


        static public int intCast(long x)
        {
            int i = (int)x;

            if ( i != x )
                throw new ArgumentException("Value out of range for int: " + x);

            return i;
        }


        static public int intCast(ulong x)
        {
            if ( x > int.MaxValue)
                throw new ArgumentException("Value out of range for int: " + x);

            return (int)x;
        }


        static public int intCast(double x)
        {
            if (x < int.MinValue || x > int.MaxValue)
                throw new ArgumentException("Value out of range for int: " + x);

            return (int)x;
        }

        #endregion

        #region long casting


        public static long longCast(object x)
        {
            if (x is long lng)
                return lng;

            if (x is int i)
                return (long)i;

            if (x is BigInt bi)
            {
                if (bi.Bipart == null)
                    return bi.Lpart;
                else
                    throw new ArgumentException("Value out of range for long: " + x);
            }

            BigInteger big = x as BigInteger;
            if (big != null)
            {
                if (big.AsInt64(out long n))
                    return n;
                else
                    throw new ArgumentException("Value out of range for long: " + x);
            }

            if (x is ulong ux)
            {
                if (ux > long.MaxValue)
                    throw new ArgumentException("Value out of range for long: " + x);
                return (long)x;
            }

            if (x is byte || x is short || x is uint || x is sbyte || x is ushort)
                return Util.ConvertToLong(x);


            if (x is Ratio r)
                return longCast(r.BigIntegerValue());

            if (x is Char)
                return longCast((char)x);

            return longCast(Util.ConvertToDouble(x));
        }



        public static long longCast(char x) { return x; }

        public static long longCast(byte x) { return x; }

        public static long longCast(sbyte x) { return x; }

        public static long longCast(short x) { return x; }

        public static long longCast(ushort x) { return x; }

        public static long longCast(uint x) { return x; }


        public static long longCast(int x) { return x; }


        static public long longCast(float x)
        {
            if ( x < long.MinValue || x > long.MaxValue )
                throw new ArgumentException("Value out of range for long: " + x);
            
            return (long)x;
        }


        static public long longCast(long x)
        {
            return x;
        }


        static public long longCast(ulong x)
        {
            if (x > long.MaxValue)
                throw new ArgumentException("Value out of range for long: " + x);

            return (long)x;
        }


        static public long longCast(double x)
        {
            if (x < long.MinValue || x > long.MaxValue)
                throw new ArgumentException("Value out of range for long: " + x);

            return (long)x;
        }

        #endregion

        #region float casting


        public static float floatCast(object x)
        {
            if (x is float f)
                return f;

            double n = Util.ConvertToDouble(x);
            if (n < float.MinValue || n > float.MaxValue)
                throw new ArgumentException("Value out of range for float: " + x);

            return (float)n;
        }


        public static float floatCast(char x) { return x; }

        public static float floatCast(byte x) { return x; }

        public static float floatCast(sbyte x) { return x; }

        public static float floatCast(short x) { return x; }

        public static float floatCast(ushort x) { return x; }

        public static float floatCast(int x) { return x; }

        public static float floatCast(uint x) { return x; }

        public static float floatCast(long x) { return x; }

        public static float floatCast(ulong x) { return x; }

        public static float floatCast(float x) { return x; }



        public static float floatCast(decimal x)
        {
            return floatCast((double)x);
        }


        public static float floatCast(double x)
        {
            if (x < float.MinValue || x > float.MaxValue)
                throw new ArgumentException("Valueout of range for float: " + x);
            return (float)x;
        }

        #endregion

        #region double casting


        public static double doubleCast(object x)
        {
            return Util.ConvertToDouble(x);
        }


        public static double doubleCast(char x) { return x; }

        public static double doubleCast(byte x) { return x; }

        public static double doubleCast(sbyte x) { return x; }

        public static double doubleCast(short x) { return x; }

        public static double doubleCast(ushort x) { return x; }

        public static double doubleCast(int x) { return x; }

        public static double doubleCast(uint x) { return x; }

        public static double doubleCast(long x) { return x; }

        public static double doubleCast(ulong x) { return x; }

        public static double doubleCast(float x) { return x; }

        public static double doubleCast(double x) { return x; }

        public static double doubleCast(decimal x) { return (double)x; }

        #endregion

        #region unchecked byte casting


        static public byte uncheckedByteCast(Object x)
        {
            return Util.ConvertToByte(x);
        }


        static public byte uncheckedByteCast(char x) { return (byte)x; }

        static public byte uncheckedByteCast(byte x) { return (byte)x; }

        static public byte uncheckedByteCast(sbyte x) { return (byte)x; }

        static public byte uncheckedByteCast(short x) { return (byte)x; }

        static public byte uncheckedByteCast(ushort x) { return (byte)x; }

        static public byte uncheckedByteCast(int x) { return (byte)x; }

        static public byte uncheckedByteCast(uint x) { return (byte)x; }

        static public byte uncheckedByteCast(long x) { return (byte)x; }

        static public byte uncheckedByteCast(ulong x) { return (byte)x; }

        static public byte uncheckedByteCast(float x) { return (byte)x; }

        static public byte uncheckedByteCast(double x) { return (byte)x; }

        static public byte uncheckedByteCast(decimal x) { return (byte)x; }

        #endregion

        #region unchecked sbyte casting


        static public sbyte uncheckedSByteCast(Object x)
        {
            return Util.ConvertToSByte(x);
        }


        static public sbyte uncheckedSByteCast(char x) { return (sbyte)x; }

        static public sbyte uncheckedSByteCast(byte x) { return (sbyte)x; }

        static public sbyte uncheckedSByteCast(sbyte x) { return (sbyte)x; }

        static public sbyte uncheckedSByteCast(short x) { return (sbyte)x; }

        static public sbyte uncheckedSByteCast(ushort x) { return (sbyte)x; }

        static public sbyte uncheckedSByteCast(int x) { return (sbyte)x; }

        static public sbyte uncheckedSByteCast(uint x) { return (sbyte)x; }

        static public sbyte uncheckedSByteCast(long x) { return (sbyte)x; }

        static public sbyte uncheckedSByteCast(ulong x) { return (sbyte)x; }

        static public sbyte uncheckedSByteCast(float x) { return (sbyte)x; }

        static public sbyte uncheckedSByteCast(double x) { return (sbyte)x; }

        static public sbyte uncheckedSByteCast(decimal x) { return (sbyte)x; }

        #endregion

        #region unchecked short casting


        static public short uncheckedShortCast(Object x)
        {
            return Util.ConvertToShort(x);
        }


        static public short uncheckedShortCast(char x) { return (short)x; }

        static public short uncheckedShortCast(byte x) { return (short)x; }

        static public short uncheckedShortCast(sbyte x) { return (short)x; }

        static public short uncheckedShortCast(short x) { return (short)x; }


        static public short uncheckedShortCast(ushort x) { return (short)x; }

        static public short uncheckedShortCast(int x) { return (short)x; }

        static public short uncheckedShortCast(uint x) { return (short)x; }

        static public short uncheckedShortCast(long x) { return (short)x; }

        static public short uncheckedShortCast(ulong x) { return (short)x; }

        static public short uncheckedShortCast(float x) { return (short)x; }

        static public short uncheckedShortCast(double x) { return (short)x; }

        static public short uncheckedShortCast(decimal x) { return (short)x; }

        #endregion

        #region unchecked ushort casting


        static public ushort uncheckedUShortCast(Object x)
        {
            return Util.ConvertToUShort(x);
        }


        static public ushort uncheckedUShortCast(char x) { return (ushort)x; }
        static public ushort uncheckedUShortCast(byte x) { return (ushort)x; }
        static public ushort uncheckedUShortCast(sbyte x) { return (ushort)x; }
        static public ushort uncheckedUShortCast(short x) { return (ushort)x; }
        static public ushort uncheckedUShortCast(ushort x) { return (ushort)x; }
        static public ushort uncheckedUShortCast(int x) { return (ushort)x; }
        static public ushort uncheckedUShortCast(uint x) { return (ushort)x; }
        static public ushort uncheckedUShortCast(long x) { return (ushort)x; }
        static public ushort uncheckedUShortCast(ulong x) { return (ushort)x; }
        static public ushort uncheckedUShortCast(float x) { return (ushort)x; }
        static public ushort uncheckedUShortCast(double x) { return (ushort)x; }
        static public ushort uncheckedUShortCast(decimal x) { return (ushort)x; }

        #endregion

        #region unchecked int casting


        static public int uncheckedIntCast(Object x)
        {
            return Util.ConvertToInt(x);
        }


        static public int uncheckedIntCast(char x) { return (int)x; }

        static public int uncheckedIntCast(byte x) { return (int)x; }

        static public int uncheckedIntCast(sbyte x) { return (int)x; }

        static public int uncheckedIntCast(short x) { return (int)x; }

        static public int uncheckedIntCast(ushort x) { return (int)x; }

        static public int uncheckedIntCast(int x) { return (int)x; }

        static public int uncheckedIntCast(uint x) { return (int)x; }

        static public int uncheckedIntCast(long x) { return (int)x; }

        static public int uncheckedIntCast(ulong x) { return (int)x; }

        static public int uncheckedIntCast(float x) { return (int)x; }

        static public int uncheckedIntCast(double x) { return (int)x; }

        static public int uncheckedIntCast(decimal x) { return (int)x; }

        #endregion

        #region unchecked uint casting


        static public uint uncheckedUIntCast(Object x)
        {
            return Util.ConvertToUInt(x);
        }


        static public uint uncheckedUIntCast(char x) { return (uint)x; }

        static public uint uncheckedUIntCast(byte x) { return (uint)x; }

        static public uint uncheckedUIntCast(sbyte x) { return (uint)x; }

        static public uint uncheckedUIntCast(short x) { return (uint)x; }

        static public uint uncheckedUIntCast(ushort x) { return (uint)x; }

        static public uint uncheckedUIntCast(int x) { return (uint)x; }

        static public uint uncheckedUIntCast(uint x) { return (uint)x; }

        static public uint uncheckedUIntCast(long x) { return (uint)x; }

        static public uint uncheckedUIntCast(ulong x) { return (uint)x; }

        static public uint uncheckedUIntCast(float x) { return (uint)x; }

        static public uint uncheckedUIntCast(double x) { return (uint)x; }

        static public uint uncheckedUIntCast(decimal x) { return (uint)x; }

        #endregion
        
        #region unchecked long casting


        static public long uncheckedLongCast(Object x)
        {
            return Util.ConvertToLong(x);
        }


        static public long uncheckedLongCast(char x) { return (long)x; }

        static public long uncheckedLongCast(byte x) { return (long)x; }

        static public long uncheckedLongCast(sbyte x) { return (long)x; }

        static public long uncheckedLongCast(short x) { return (long)x; }

        static public long uncheckedLongCast(ushort x) { return (long)x; }

        static public long uncheckedLongCast(int x) { return (long)x; }

        static public long uncheckedLongCast(uint x) { return (long)x; }

        static public long uncheckedLongCast(long x) { return (long)x; }

        static public long uncheckedLongCast(ulong x) { return (long)x; }

        static public long uncheckedLongCast(float x) { return (long)x; }

        static public long uncheckedLongCast(double x) { return (long)x; }

        static public long uncheckedLongCast(decimal x) { return (long)x; }

        #endregion

        #region unchecked ulong casting


        static public ulong uncheckedULongCast(Object x)
        {
            return Util.ConvertToULong(x);
        }


        static public ulong uncheckedULongCast(char x) { return (ulong)x; }

        static public ulong uncheckedULongCast(byte x) { return (ulong)x; }

        static public ulong uncheckedULongCast(sbyte x) { return (ulong)x; }

        static public ulong uncheckedULongCast(short x) { return (ulong)x; }

        static public ulong uncheckedULongCast(ushort x) { return (ulong)x; }

        static public ulong uncheckedULongCast(int x) { return (ulong)x; }

        static public ulong uncheckedULongCast(uint x) { return (ulong)x; }

        static public ulong uncheckedULongCast(long x) { return (ulong)x; }

        static public ulong uncheckedULongCast(ulong x) { return (ulong)x; }

        static public ulong uncheckedULongCast(float x) { return (ulong)x; }

        static public ulong uncheckedULongCast(double x) { return (ulong)x; }

        static public ulong uncheckedULongCast(decimal x) { return (ulong)x; }

        #endregion

        #region unchecked float casting


        static public float uncheckedFloatCast(Object x)
        {
            return Util.ConvertToFloat(x);
        }


        static public float uncheckedFloatCast(char x) { return (float)x; }

        static public float uncheckedFloatCast(byte x) { return (float)x; }

        static public float uncheckedFloatCast(sbyte x) { return (float)x; }

        static public float uncheckedFloatCast(short x) { return (float)x; }

        static public float uncheckedFloatCast(ushort x) { return (float)x; }

        static public float uncheckedFloatCast(int x) { return (float)x; }

        static public float uncheckedFloatCast(uint x) { return (float)x; }

        static public float uncheckedFloatCast(long x) { return (float)x; }

        static public float uncheckedFloatCast(ulong x) { return (float)x; }

        static public float uncheckedFloatCast(float x) { return (float)x; }

        static public float uncheckedFloatCast(double x) { return (float)x; }

        static public float uncheckedFloatCast(decimal x) { return (float)x; }

        #endregion

        #region unchecked double casting


        static public double uncheckedDoubleCast(Object x)
        {
            return Util.ConvertToDouble(x);
        }


        static public double uncheckedDoubleCast(char x) { return (double)x; }

        static public double uncheckedDoubleCast(byte x) { return (double)x; }

        static public double uncheckedDoubleCast(sbyte x) { return (double)x; }

        static public double uncheckedDoubleCast(short x) { return (double)x; }

        static public double uncheckedDoubleCast(ushort x) { return (double)x; }

        static public double uncheckedDoubleCast(int x) { return (double)x; }

        static public double uncheckedDoubleCast(uint x) { return (double)x; }

        static public double uncheckedDoubleCast(long x) { return (double)x; }

        static public double uncheckedDoubleCast(ulong x) { return (double)x; }

        static public double uncheckedDoubleCast(float x) { return (double)x; }

        static public double uncheckedDoubleCast(double x) { return (double)x; }

        static public double uncheckedDoubleCast(decimal x) { return (double)x; }

        #endregion

        #region unchecked decimal casting


        static public decimal uncheckedDecimalCast(Object x)
        {
            return Util.ConvertToDecimal(x);
        }


        static public decimal uncheckedDecimalCast(char x) { return (decimal)x; }

        static public decimal uncheckedDecimalCast(byte x) { return (decimal)x; }

        static public decimal uncheckedDecimalCast(sbyte x) { return (decimal)x; }

        static public decimal uncheckedDecimalCast(short x) { return (decimal)x; }

        static public decimal uncheckedDecimalCast(ushort x) { return (decimal)x; }

        static public decimal uncheckedDecimalCast(int x) { return (decimal)x; }

        static public decimal uncheckedDecimalCast(uint x) { return (decimal)x; }

        static public decimal uncheckedDecimalCast(long x) { return (decimal)x; }

        static public decimal uncheckedDecimalCast(ulong x) { return (decimal)x; }

        static public decimal uncheckedDecimalCast(float x) { return (decimal)x; }

        static public decimal uncheckedDecimalCast(double x) { return (decimal)x; }

        static public decimal uncheckedDecimalCast(decimal x) { return (decimal)x; }

        #endregion

        #region unchecked char casting


        static public char uncheckedCharCast(Object x)
        {
            return x is char c ? c : (char)Util.ConvertToLong(x);
        }


        static public char uncheckedCharCast(char x) { return (char)x; }

        static public char uncheckedCharCast(byte x) { return (char)x; }

        static public char uncheckedCharCast(sbyte x) { return (char)x; }

        static public char uncheckedCharCast(short x) { return (char)x; }

        static public char uncheckedCharCast(ushort x) { return (char)x; }

        static public char uncheckedCharCast(int x) { return (char)x; }

        static public char uncheckedCharCast(uint x) { return (char)x; }

        static public char uncheckedCharCast(long x) { return (char)x; }

        static public char uncheckedCharCast(ulong x) { return (char)x; }

        static public char uncheckedCharCast(float x) { return (char)x; }

        static public char uncheckedCharCast(double x) { return (char)x; }

        static public char uncheckedCharCast(decimal x) { return (char)x; }

        #endregion

        #region IntPtr casting


        static public IntPtr intPtrCast(object x)
        {
            if(x is IntPtr ptr)
                return ptr;
            return IntPtr.Zero;
        }
        #endregion

        #region UIntPtr casting


        static public UIntPtr uintPtrCast(object x)
        {
            if(x is UIntPtr ptr)
                return ptr;
            return UIntPtr.Zero;
        }
        #endregion

        #endregion

        #region  More collection support


        public static IPersistentMap map(params object[] init)
        {
            if (init == null || init.Length == 0)
                return PersistentArrayMap.EMPTY;
            else if (init.Length <= PersistentArrayMap.HashtableThreshold)
                return PersistentArrayMap.createWithCheck(init);
            else 
                return PersistentHashMap.createWithCheck(init);
        }


        public static IPersistentMap mapUniqueKeys(params object[] init)
        {
            if (init == null)
                return PersistentArrayMap.EMPTY;
            else if (init.Length <= PersistentArrayMap.HashtableThreshold)
                return new PersistentArrayMap(init);
            return PersistentHashMap.create(init);
        }


        public static IPersistentSet set(params object[] init)
        {
            return PersistentHashSet.createWithCheck(init);
        }


        public static IPersistentVector vector(params object[] init)
        {
            return LazilyPersistentVector.createOwning(init);
        }


        public static IPersistentVector subvec(IPersistentVector v, int start, int end)
        {
            if ( start < 0 )
                throw new ArgumentOutOfRangeException("start","cannot be less than zero");

            if (end < start )
                throw new ArgumentOutOfRangeException("end","cannot be less than start");

            if ( end > v.count())
                throw new ArgumentOutOfRangeException("end","cannot be past the end of the vector");

            if (start == end)
                return PersistentVector.EMPTY;
            return new APersistentVector.SubVector(null, v, start, end);
        }


        #endregion

        #region List support


        public static ISeq list()
        {
            return null;
        }


        public static ISeq list(object arg1)
        {
            return new PersistentList(arg1);
        }


        public static ISeq list(object arg1, object arg2)
        {
            return listStar(arg1, arg2, null);
        }


        public static ISeq list(object arg1, object arg2, object arg3)
        {
            return listStar(arg1, arg2, arg3, null);
        }


        public static ISeq list(object arg1, object arg2, object arg3, object arg4)
        {
            return listStar(arg1, arg2, arg3, arg4, null);
        }


        public static ISeq list(object arg1, object arg2, object arg3, object arg4, object arg5)
        {
            return listStar(arg1, arg2, arg3, arg4, arg5, null);
        }




        public static ISeq listStar(object arg1, ISeq rest)
        {
            return cons(arg1, rest);
        }


        public static ISeq listStar(object arg1, object arg2, ISeq rest)
        {
            return cons(arg1, cons(arg2, rest));
        }


        public static ISeq listStar(object arg1, object arg2, object arg3, ISeq rest)
        {
            return cons(arg1, cons(arg2, cons(arg3, rest)));
        }


        public static ISeq listStar(object arg1, object arg2, object arg3, object arg4, ISeq rest)
        {
            return cons(arg1, cons(arg2, cons(arg3, cons(arg4, rest))));
        }


        public static ISeq listStar(object arg1, object arg2, object arg3, object arg4, object arg5, ISeq rest)
        {
            return cons(arg1, cons(arg2, cons(arg3, cons(arg4, cons(arg5, rest)))));
        }


        public static ISeq arrayToList(object[] items)
        {
            ISeq ret = null;
            for (int i = items.Length - 1; i >= 0; --i)
                ret = (ISeq)cons(items[i], ret);
            return ret;
        }

        public static Object[] object_array(Object sizeOrSeq)
        {
            if (Util.IsNumeric(sizeOrSeq))
                return new Object[Util.ConvertToInt(sizeOrSeq)];
            else
            {
                ISeq s = RT.seq(sizeOrSeq);
                int size = RT.count(s);
                Object[] ret = new Object[size];
                for (int i = 0; i < size && s != null; i++, s = s.next())
                    ret[i] = s.first();
                return ret;
            }
        }


        public static object[] toArray(object coll)
        {
            if (coll == null)
                return EmptyObjectArray;

            if (coll is object[] objArray)
                return objArray;

            // In CLR, ICollection does not have a toArray.  
            // ICollection derives from IEnumerable, so the IEnumerable clause will take care of it.
            //if (coll instanceof Collection)
            //  return ((Collection)coll).toArray();
            //  TODO: List has a toArray -- generic -- need type. 

            if (coll is IEnumerable ie)
                return IEnumToArray(ie);

            // Java has Map here, but IDictionary is IEnumerable, so it will be handled by previous clause.

            if (coll is string s)
            {
                char[] chars = s.ToCharArray();
                // TODO: Determine if we need to make a copy (Java version does, not sure if CLR requires it)
                object[] ret = new object[chars.Length];
                for (int i = 0; i < chars.Length; i++)
                    ret[i] = chars[i];
                return ret;
            }

            // This used to be in the java version.  No longer.  Do we need?
            //else if (coll is ISeq)
            //    return toArray((ISeq)coll);
            //else if (coll is IPersistentCollection)
            //    return toArray(((IPersistentCollection)coll).seq());

            if (coll.GetType().IsArray)
            {
                ISeq iseq = (seq(coll));
                object[] ret = new object[count(iseq)];
                for (int i = 0; i < ret.Length; i++, iseq = iseq.next())
                    ret[i] = iseq.first();
                return ret;
            }
            
            throw new InvalidOperationException("Unable to convert: " + coll.GetType() + " to Object[]");
        }

        private static object[] IEnumToArray(IEnumerable e)
        {
            List<object> list = new List<object>();
            foreach (object o in e)
                list.Add(o);

            return list.ToArray();
        }

        //private static object[] toArray(ISeq seq)
        //{
        //    object[] array = new object[seq.count()];
        //    int i = 0;
        //    for (ISeq s = seq; s != null; s = s.rest(), i++)
        //        array[i] = s.first();
        //    return array;
        //}

        public static T[] SeqToArray<T>(ISeq x)
        {
            if (x == null)
                return new T[0];

            T[] array = new T[RT.Length(x)];
            int i = 0;
            for (ISeq s = x; s != null; s = s.next(), i++)
                array[i] = (T)s.first();
            return array;
        }



        static public object seqToTypedArray(ISeq seq)
        {
            Type type = (seq != null && seq.first() != null) ? seq.first().GetType() : typeof(Object);
            return seqToTypedArray(type, seq);
        }


        static public object seqToTypedArray(Type type, ISeq seq)
        {
            Array ret = Array.CreateInstance(type, Length(seq));
            if (type == typeof(int))
                for (int i = 0; seq != null; ++i, seq = seq.next())
                    ret.SetValue(intCast(seq.first()), i);
            else if (type == typeof(byte))
                for (int i = 0; seq != null; ++i, seq = seq.next())
                    ret.SetValue(byteCast(seq.first()), i);
            else if (type == typeof(float))
                for (int i = 0; seq != null; ++i, seq = seq.next())
                    ret.SetValue(floatCast(seq.first()), i);
            else if (type == typeof(short))
                for (int i = 0; seq != null; ++i, seq = seq.next())
                    ret.SetValue(shortCast(seq.first()), i);
            else if (type == typeof(char))
                for (int i = 0; seq != null; ++i, seq = seq.next())
                    ret.SetValue(charCast(seq.first()), i);
            else if (type == typeof(sbyte))
                for (int i = 0; seq != null; ++i, seq = seq.next())
                    ret.SetValue(sbyteCast(seq.first()), i);
            else if (type == typeof(uint))
                for (int i = 0; seq != null; ++i, seq = seq.next())
                    ret.SetValue(uintCast(seq.first()), i);
            else if (type == typeof(ushort))
                for (int i = 0; seq != null; ++i, seq = seq.next())
                    ret.SetValue(ushortCast(seq.first()), i);
            else if (type == typeof(ulong))
                for (int i = 0; seq != null; ++i, seq = seq.next())
                    ret.SetValue(ulongCast(seq.first()), i);
            else
                for (int i = 0; seq != null; ++i, seq = seq.next())
                    ret.SetValue(seq.first(), i);
            return ret;
        }

        static public int Length(ISeq list)
        {
            int i = 0;
            for (ISeq c = list; c != null; c = c.next())
                i++;
            return i;
        }

        public static int BoundedLength(ISeq list, int limit)
        {
            int i = 0;
            for (ISeq c = list; c != null && i <= limit; c = c.next())
            {
                i++;
            }
            return i;
        }

        #endregion

        #region Reader support

        static bool ReadTrueFalseDefault(string s, bool def)
        {
            if ("true".Equals(s))
                return true;
            else if ("false".Equals(s))
                return false;
            return def;
        }

        static Object ReadTrueFalseUnknown(String s)
        {
            if (s.Equals("true"))
                return true;
            else if (s.Equals("false"))
                return false;
            return Keyword.intern(null, "unknown");
        }


        public static bool isReduced(Object r)
        {
            return r is Reduced;
        }
        

        public static bool suppressRead()
        {
            return booleanCast(SuppressReadVar.deref());
        }


        static public string printString(object x)
        {
            using (StringWriter sw = new StringWriter())
            {
                print(x, sw);
                return sw.ToString();
            }
        }


        static public Object readString(String s)
        {
            return readString(s,null);
        }


        static public Object readString(String s, Object opts)
        {
            using (PushbackTextReader r = new PushbackTextReader(new StringReader(s)))
                return LispReader.read(r, opts);
        }


        static public void print(Object x, TextWriter w)
        {
            //call multimethod
            if (PrintInitializedVar.isBound && RT.booleanCast(PrintInitializedVar.deref()))
            {
                PrOnVar.invoke(x, w);
                return;
            }

            bool readably = booleanCast(PrintReadablyVar.deref());

            // Print meta, if exists & should be printed
            if (x is Obj)
            {
                Obj o = x as Obj;
                if (RT.count(o.meta()) > 0 && 
                     ((readably && booleanCast(PrintMetaVar.deref()))
                    || booleanCast(PrintDupVar.deref())))
                {
                    IPersistentMap meta = o.meta();
                    w.Write("#^");
                    if (meta.count() == 1 && meta.containsKey(TagKey))
                        print(meta.valAt(TagKey), w);
                    else
                        print(meta, w);
                    w.Write(' ');
                }
            }

            if (x == null)
                w.Write("nil");
            else if (x is ISeq || x is IPersistentList)
            {
                w.Write('(');
                printInnerSeq(seq(x), w);
                w.Write(')');
            }
            else if (x is string)
            {
                string s = x as string;
                if (!readably)
                    w.Write(s);
                else
                {
                    w.Write('"');
                    foreach (char c in s)
                    {
                        switch (c)
                        {
                            case '\n':
                                w.Write("\\n");
                                break;
                            case '\t':
                                w.Write("\\t");
                                break;
                            case '\r':
                                w.Write("\\r");
                                break;
                            case '"':
                                w.Write("\\\"");
                                break;
                            case '\\':
                                w.Write("\\\\");
                                break;
                            case '\f':
                                w.Write("\\f");
                                break;
                            case '\b':
                                w.Write("\\b");
                                break;
                            default:
                                w.Write(c);
                                break;
                        }
                    }
                    w.Write('"');
                }
            }
            else if (x is IPersistentMap)
            {
                w.Write('{');
                for (ISeq s = seq(x); s != null; s = s.next())
                {
                    IMapEntry e = (IMapEntry)s.first();
                    print(e.key(), w);
                    w.Write(' ');
                    print(e.val(), w);
                    if (s.next() != null)
                        w.Write(", ");
                }
                w.Write('}');
            }
            else if (x is IPersistentVector)
            {
                IPersistentVector v = x as IPersistentVector;
                int n = v.count();
                w.Write('[');
                for (int i = 0; i < n; i++)
                {
                    print(v.nth(i), w);
                    if (i < n - 1)
                        w.Write(" ");
                }
                w.Write(']');
            }
            else if (x is IPersistentSet)
            {
                w.Write("#{");
                for (ISeq s = seq(x); s != null; s = s.next())
                {
                    print(s.first(), w);
                    if (s.next() != null)
                        w.Write(" ");
                }
                w.Write('}');
            }
            else if (x is Char)
            {
                char c = (char)x;
                if (!readably)
                    w.Write(c);
                else
                {
                    w.Write('\\');
                    switch (c)
                    {
                        case '\n':
                            w.Write("newline");
                            break;
                        case '\t':
                            w.Write("tab");
                            break;
                        case ' ':
                            w.Write("space");
                            break;
                        case '\b':
                            w.Write("backspace");
                            break;
                        case '\f':
                            w.Write("formfeed");
                            break;
                        case '\r':
                            w.Write("return");
                            break;
                        default:
                            w.Write(c);
                            break;
                    }
                }
            }
            else if (x is Type type)
            {
                string tName = type.AssemblyQualifiedName;
                if (LispReader.NameRequiresEscaping(tName))
                    tName = LispReader.VbarEscape(tName);
                w.Write("#=");
                w.Write(tName);
            }
            else if (x is BigDecimal && readably)
            {
                w.Write(x.ToString());
                w.Write("M");
            }
            else if (x is BigInt && readably)
            {
                w.Write(x.ToString());
                w.Write("N");
            }
            else if (x is BigInteger && readably)
            {
                w.Write(x.ToString());
                w.Write("BIGINT");
            }
            else if (x is Var)
            {
                Var v = x as Var;
                w.Write("#=(var {0}/{1})", v.Namespace.Name, v.Symbol);
            }
            else if (x is Regex r)
            {
                w.Write("#\"{0}\"", r.ToString());
            }
            //else
            //    w.Write(x.ToString());
            // The clause above is what Java has, and would have been nice.
            // Doesn't work for me, for one reason:  
            // When generating initializations for static variables in the classes representing IFns,
            //    let's say the value is the double 7.0.
            //    we generate code that says   (double)RT.readFromString("7")
            //    so we get a boxed int, which CLR won't cast to double.  Sigh.
            //    So I need double/float to print a trailing .0 even when integer-valued.
            else if (x is double || x is float)
            {
                string s = x.ToString();
                if (!s.Contains('.') && !s.Contains('E'))
                    s += ".0";
                w.Write(s);
            }
            else
                w.Write(x.ToString());
        }


        private static void printInnerSeq(ISeq x, TextWriter w)
        {
            for (ISeq s = x; s != null; s = s.next())
            {
                print(s.first(), w);
                if (s.next() != null)
                    w.Write(' ');
            }
        }

        public static string PrintToConsole(object x)
        {
            string ret = printString(x);
            Console.WriteLine(ret);
            return ret;
        }



        #endregion

        #region Locating types

        static readonly char[] _triggerTypeChars = new char[] { '`', ','};
        
        public static Type classForName(string p)
        {

            // fastest path, will succeed for assembly qualified names (returned by Type.AssemblyQualifiedName)
            // or namespace qualified names (returned by Type.FullName) in the executing assembly or mscorlib
            // e.g. "UnityEngine.Transform, UnityEngine, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"
            Type t = Type.GetType(p, false);

            // Added the IsPublic check to deal with shadowed types in .Net Core,
            // e.g. System.Environment in assemblies System.Private.CoreLib and System.Runtime.Exceptions.
            // It is private in the former and public in the latter.
            // Unfortunately, Type.GetType was finding the former.
            if (t != null && t.IsPublic)
                return t;

            t = Compiler.FindDuplicateType(p);
            if (t != null)
                return t;

            AppDomain domain = AppDomain.CurrentDomain;
            Assembly[] assys = domain.GetAssemblies();
            List<Type> candidateTypes = new List<Type>();

            // fast path, will succeed for namespace qualified names (returned by Type.FullName)
            // e.g. "UnityEngine.Transform"
            foreach (Assembly assy in assys)
            {
                  Type t1 = assy.GetType(p, false);
                  if(t1 != null && t1.IsPublic)
                        return t1;
            }

            // slow path, will succeed for display names (returned by Type.Name)
            // e.g. "Transform"
            foreach (Assembly assy1 in assys)
            {
                Type t1 = assy1.GetType(p, false);

                if (IsRunningOnMono)
                {
                    // I do not know why Assembly.GetType fails to find types in our assemblies in Mono
                    if (t1 == null)
                    {
                        if (!assy1.IsDynamic)
                        {
                            try
                            {

                                foreach (Type tt in assy1.GetTypes())
                                {
                                    if (tt.Name.Equals(p))
                                    {
                                        t1 = tt;
                                        break;
                                    }
                                }
                            }
                            catch (System.Reflection.ReflectionTypeLoadException)
                            {
                            }
                        }
                    }
                }

                if (t1 != null && !candidateTypes.Contains(t1))
                    candidateTypes.Add(t1);
            }

            if (candidateTypes.Count == 0)
                t = null;
            else if (candidateTypes.Count == 1)
                t = candidateTypes[0];
            else // multiple, ambiguous
                t = null;

            if (t == null && p.IndexOfAny(_triggerTypeChars) != -1)
                t = ClrTypeSpec.GetTypeFromName(p);

            return t;
        }
        

        public static Type classForNameE(string p)
        {
            Type t = classForName(p);
            if (t == null)
            {
                throw new TypeNotFoundException(p);
            }
            return t;
        }

        #endregion

        #region Array interface


        public static int alength(Array a)
        {
            return a.Length;
        }


        public static Array aclone(Array a)
        {
            return (Array)a.Clone();
        }


        public static object aget(Array a, int idx)
        {
            return a.GetValue(idx);
        }


        public static object aset(Array a, int idx, object val)
        {
            a.SetValue(val, idx);
            return val;
        }


      
        // overloads for array getters/setters


        public static object aget(Object[] xs, int i)
        {
            return xs[i];
        }


        public static object aset(Object[] xs, int i, object v)
        {
            xs[i] = v;
            return v;
        }


        public static bool aget(bool[] xs, int i)
        {
            return xs[i];
        }


        public static bool aset(bool[] xs, int i, bool v)
        {
            xs[i] = v;
            return v;
        }


        public static char aget(char[] xs, int i)
        {
            return xs[i];
        }


        public static char aset(char[] xs, int i, char v)
        {
            xs[i] = v;
            return v;
        }


        public static byte aget(byte[] xs, int i)
        {
            return xs[i];
        }


        public static byte aset(byte[] xs, int i, byte v)
        {
            xs[i] = v;
            return v;
        }


        public static sbyte aget(sbyte[] xs, int i)
        {
            return xs[i];
        }


        public static sbyte aset(sbyte[] xs, int i, sbyte v)
        {
            xs[i] = v;
            return v;
        }


        public static short aget(short[] xs, int i)
        {
            return xs[i];
        }


        public static short aset(short[] xs, int i, short v)
        {
            xs[i] = v;
            return v;
        }


        public static ushort aget(ushort[] xs, int i)
        {
            return xs[i];
        }


        public static ushort aset(ushort[] xs, int i, ushort v)
        {
            xs[i] = v;
            return v;
        }


        public static int aget(int[] xs, int i)
        {
            return xs[i];
        }


        public static int aset(int[] xs, int i, int v)
        {
            xs[i] = v;
            return v;
        }


        public static uint aget(uint[] xs, int i)
        {
            return xs[i];
        }


        public static uint aset(uint[] xs, int i, uint v)
        {
            xs[i] = v;
            return v;
        }


        public static long aget(long[] xs, int i)
        {
            return xs[i];
        }


        public static long aset(long[] xs, int i, long v)
        {
            xs[i] = v;
            return v;
        }


        public static ulong aget(ulong[] xs, int i)
        {
            return xs[i];
        }


        public static ulong aset(ulong[] xs, int i, ulong v)
        {
            xs[i] = v;
            return v;
        }


        public static float aget(float[] xs, int i)
        {
            return xs[i];
        }


        public static float aset(float[] xs, int i, float v)
        {
            xs[i] = v;
            return v;
        }


        public static double aget(double[] xs, int i)
        {
            return xs[i];
        }


        public static double aset(double[] xs, int i, double v)
        {
            xs[i] = v;
            return v;
        }


        public static decimal aget(decimal[] xs, int i)
        {
            return xs[i];
        }


        public static decimal aset(decimal[] xs, int i, decimal v)
        {
            xs[i] = v;
            return v;
        }

        #endregion

        #region Things not in the Java version

        [Serializable]
        class DefaultComparer : IComparer, ISerializable
        {
            #region IComparer Members

            public int Compare(object x, object y)
            {
                return Util.compare(x, y);  // was ((IComparable)x).CompareTo(y); -- changed in Java rev 1145
            }

            #endregion

            #region core.clj compatibility

            //  Somewhere, there is an explicit call to compare
            public int compare(object x, object y)
            {
                return Util.compare(x, y);  // was ((IComparable)x).CompareTo(y);-- changed in Java rev 1145
            }

            #endregion

            #region ISerializable Members

            [System.Security.SecurityCritical]
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.SetType(typeof(DefaultComparerSerializationHelper));
            }

            [Serializable]
            class DefaultComparerSerializationHelper : IObjectReference
            {
                #region IObjectReference Members

                public object GetRealObject(StreamingContext context)
                {
                    return DefaultComparerInstance;
                }

                #endregion
            }


            #endregion
        }

        static public readonly IComparer DefaultComparerInstance = new DefaultComparer();


        // do we need this?
        //static Boolean HasTag(object o, object tag)
        //{
        //    return Util.equals(tag,,RT.get(RT.meta(o),TAG_KEY);
        //}


        public static long nanoTime()
        {
            return DateTime.Now.Ticks * 100;
        }

        private static readonly Stopwatch _stopwatch = new Stopwatch();

        public static object StartStopwatch()
        {
            _stopwatch.Reset();
            _stopwatch.Start();
            return null;
        }

        public static double StopStopwatch()
        {
            _stopwatch.Stop();
            return _stopwatch.Elapsed.TotalMilliseconds;
        }


        
        // In core.clj, we see (cast Number x)  in a number of numeric methods.
        // There is no Number wrapper here.
        // The intent is:  if x is not a numeric value, throw a ClassCastException (Java),
        //                 else return x
        // And here it is:
        public static object NumberCast(object x)
        {
            if (!Util.IsNumeric(x))
                throw new InvalidCastException("Expected a number");
            return x;
        }

        // The Java guys use Class.cast to do casting.
        // We don't have that.
        // Perhaps this will work.
        // NOPE!
        //public static object Cast(Type t, object o)
        //{
        //    return Type.DefaultBinder.ChangeType(o, t, null);
        //}


        // The Java version goes through Array.sort to do this,
        // but I don't have a way to pass a comparator.

        class ComparerConverter : IComparer
        {
            readonly IFn _fn;

            public ComparerConverter(IFn fn)
            {
                _fn = fn;
            }

            #region IComparer Members

            public int  Compare(object x, object y)
            {
 	            return Util.ConvertToInt(_fn.invoke( x,y ));
            }

            #endregion
        }

        public static void SortArray(Array a, IFn fn)
        {
                Array.Sort(a, new ComparerConverter(fn));
        }



        static readonly Random _random = new Random();


        public static double random()
        {
            lock (_random)
            {
                return _random.NextDouble();
            }
        }


        public static string CultureToString(object x)
        {
            return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", x);
        }


        // Surprisingly hard, due to non-BMP (multiple character) codes.
        // This solution from http://stackoverflow.com/questions/228038/best-way-to-reverse-a-string-in-c-2-0
        public static string StringReverse(string input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            // allocate a buffer to hold the output
            char[] output = new char[input.Length];
            for (int outputIndex = 0, inputIndex = input.Length - 1; outputIndex < input.Length; outputIndex++, inputIndex--)
            {
                // check for surrogate pair
                if (input[inputIndex] >= 0xDC00 && input[inputIndex] <= 0xDFFF &&
                        inputIndex > 0 && input[inputIndex - 1] >= 0xD800 && input[inputIndex - 1] <= 0xDBFF)
                {
                    // preserve the order of the surrogate pair code units
                    output[outputIndex + 1] = input[inputIndex];
                    output[outputIndex] = input[inputIndex - 1];
                    outputIndex++;
                    inputIndex--;
                }
                else
                {
                    output[outputIndex] = input[inputIndex];
                }
            }

            return new string(output);
        }

        private static readonly bool _isRunningOnMono = Type.GetType("Mono.Runtime") != null;

        public static bool IsRunningOnMono { get { return _isRunningOnMono; } }

        #endregion

        # region Loading/compiling


        public static void load(String relativePath)
        {
            load(relativePath, true);
        }


        public static void load(String relativePath, Boolean failIfNotFound)
        {
            string cljsourcename = relativePath + ".clj";
            string cljcsourcename = relativePath + ".cljc";
            string cljassemblyname = relativePath.Replace('/', '.') + ".clj.dll";
            string cljcassemblyname = relativePath.Replace('/', '.') + ".cljc.dll";
            string sourcename = cljsourcename;
            string assemblyname = cljassemblyname;

            if (!RuntimeBootstrapFlag.DisableFileLoad)
            {
                FileInfo cljInfo = FindFile(sourcename);
                if (cljInfo == null )
                {
                    sourcename = cljcsourcename;
                    cljInfo = FindFile(sourcename);
                }
                FileInfo assyInfo = FindFile(assemblyname);
                if ( assyInfo == null )
                {
                    assemblyname = cljcassemblyname;
                    assyInfo = FindFile(assemblyname);
                }


                if ((assyInfo != null &&
                     (cljInfo == null || assyInfo.LastWriteTime >= cljInfo.LastWriteTime)))
                {
                    try
                    {
                        Var.pushThreadBindings(RT.mapUniqueKeys(CurrentNSVar, CurrentNSVar.deref(),
                                                          WarnOnReflectionVar, WarnOnReflectionVar.deref(),
                                                          RT.UncheckedMathVar, RT.UncheckedMathVar.deref()));
                        Compiler.LoadAssembly(assyInfo, relativePath);
                        return;
                    }
                    finally
                    {
                        Var.popThreadBindings();
                    }
                }

                if (cljInfo != null)
                {
                    if (booleanCast(Compiler.CompileFilesVar.deref()))
                        Compile(cljInfo, sourcename);
                    else
                        LoadScript(cljInfo, sourcename);
                    return;
                }
            }

            try
            {
                Var.pushThreadBindings(RT.map(CurrentNSVar, CurrentNSVar.deref(),
                    WarnOnReflectionVar, WarnOnReflectionVar.deref(),
                    RT.UncheckedMathVar, RT.UncheckedMathVar.deref()));
                if (Compiler.TryLoadInitType(relativePath))
                    return;
            }
            finally
            {
                Var.popThreadBindings();
            }


            bool loaded = TryLoadFromEmbeddedResource(relativePath, assemblyname);


            if (!loaded && failIfNotFound)
                throw new FileNotFoundException(String.Format("Could not locate {0}, {1}, {2} or {3} on load path.{4}", 
                    cljassemblyname, 
                    cljcassemblyname,
                    cljsourcename,
                    cljcsourcename,
                    relativePath.Contains("_") ? " Please check that namespaces with dashes use underscores in the Clojure file name." : ""));
        }

        private static bool TryLoadFromEmbeddedResource(string relativePath, string assemblyname)
        {
            var asmStream = GetEmbeddedResourceStream(assemblyname, out _);
            if (asmStream != null)
            {
                try
                {
                    Var.pushThreadBindings(RT.map(CurrentNSVar, CurrentNSVar.deref(),
                                                  WarnOnReflectionVar, WarnOnReflectionVar.deref(),
                                                  RT.UncheckedMathVar, RT.UncheckedMathVar.deref()));
                    Compiler.LoadAssembly(ReadStreamBytes(asmStream), relativePath);
                    return true;
                }
                finally
                {
                    Var.popThreadBindings();
                }
            }

            var embeddedCljName = relativePath.Replace("/", ".") + ".clj";
            var stream = GetEmbeddedResourceStream(embeddedCljName, out Assembly containingAssembly);
            if ( stream == null )
            {
                embeddedCljName = relativePath.Replace("/", ".") + ".cljc";
                stream = GetEmbeddedResourceStream(embeddedCljName, out containingAssembly);
            }
            if (stream != null)
            {
                using (var rdr = new StreamReader(stream))
                {
                    if (booleanCast(Compiler.CompileFilesVar.deref()))
                        Compile(containingAssembly.FullName, embeddedCljName, rdr, relativePath);
                    else
                        LoadScript(containingAssembly.FullName, embeddedCljName, rdr, relativePath);
                }
                return true;
            }
            return false;
        }

        private static void MaybeLoadCljScript(string cljname)
        {
            LoadCljScript(cljname, false);
        }

        static void LoadCljScript(string cljname)
        {
            LoadCljScript(cljname, true);
        }

        static void LoadCljScript(string cljname, bool failIfNotFound)
        {
            FileInfo cljInfo = FindFile(cljname);
            if (cljInfo != null)
                LoadScript(cljInfo,cljname);
            else if (failIfNotFound)
                throw new FileNotFoundException(String.Format("Could not locate Clojure resource on {0}", ClojureLoadPathString));
        }

        public  static void LoadScript(FileInfo cljInfo, string relativePath)
        {
            using (TextReader rdr = cljInfo.OpenText())
                LoadScript(cljInfo.FullName, cljInfo.Name, rdr, relativePath);
        }

        private static void LoadScript(string fullName, string name, TextReader rdr, string relativePath)
        {
            Compiler.load(rdr, fullName, name, relativePath);
        }

        private static void Compile(FileInfo cljInfo, string relativePath)
        {
            using ( TextReader rdr = cljInfo.OpenText() )
                Compile(cljInfo.Directory.FullName, cljInfo.Name, rdr, relativePath);
        }

        private static void Compile(string dirName, string name, TextReader rdr, string relativePath)
        {
            Compiler.Compile(rdr, dirName, name, relativePath);
        }

        static FileInfo FindFile(string path, string filename)
        {
            string probePath = ConvertPath(Path.Combine(path, filename));
            if (File.Exists(probePath))
                return new FileInfo(probePath);

            return null;
        }

        public static IEnumerable<string> GetFindFilePaths()
        {
            return GetFindFilePathsRaw().Distinct();
        }

        static IEnumerable<string> GetFindFilePathsRaw()
        {
            yield return System.AppDomain.CurrentDomain.BaseDirectory;
            yield return Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "bin");
            yield return Directory.GetCurrentDirectory();
            yield return Path.GetDirectoryName(typeof(RT).Assembly.Location);

            Assembly assy = Assembly.GetEntryAssembly();
            if ( assy != null )
                yield return Path.GetDirectoryName(assy.Location);

            string rawpaths = (string)System.Environment.GetEnvironmentVariable(ClojureLoadPathString);
            if (rawpaths == null)
                yield break;

            string[] paths = rawpaths.Split(Path.PathSeparator);
            foreach (string path in paths)
                yield return path;
        }


        public static FileInfo FindFile(string fileName)
        {
            FileInfo fi;

            foreach (string path in GetFindFilePaths())
                if ((fi = FindFile(path, fileName)) != null)
                    return fi;

            return FindRemappedFile(fileName);
        }

        public static readonly Var NSLoadMappings
            = Var.intern(Namespace.findOrCreate(Symbol.intern("clojure.core")),
                                                 Symbol.intern("*ns-load-mappings*"), new Atom(PersistentVector.EMPTY)).setDynamic();

        public static FileInfo FindRemappedFile(string fileName)
        {
            if (!(NSLoadMappings.deref() is Atom nsLoadMappings)) return null;

            var nsLoadMappingsVal = nsLoadMappings.deref() as PersistentVector;
            foreach (var x in nsLoadMappingsVal)
            {
                if (!(x is PersistentVector mapping) || mapping.length() < 2) continue;
                if (!(mapping[0] is string nsRoot)) continue;
                nsRoot = nsRoot.Replace('.', '/');
                if(fileName.StartsWith(nsRoot))
                {
                    var fsRoot = mapping[1] as string;
                    var probePath = ConvertPath(fsRoot) + ConvertPath(fileName.Substring(nsRoot.Length));
                    if(File.Exists(probePath))
                        return new FileInfo(probePath);
                }
            }
            return null;
        }

        public static IEnumerable<FileInfo> FindFiles(string fileName)
        {
            FileInfo fi;

            foreach (string path in GetFindFilePaths())
                if ((fi = FindFile(path, fileName)) != null)
                    yield return fi;
        }

        static string ConvertPath(string path)
        {
            return path.Replace('/', Path.DirectorySeparatorChar);
        }

        static Stream GetEmbeddedResourceStream(string resourceName, out Assembly containingAssembly)
        {
            containingAssembly = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!asm.IsDynamic)
                {
                    try
                    {
                        var stream = asm.GetManifestResourceStream(resourceName);
                        if (stream != null)
                        {
                            containingAssembly = asm;
                            return stream;
                        }
                    }
                    catch (ArgumentException)
                    {
                    }
                    catch (IOException)
                    {
                    }
                    catch (NotImplementedException)
                    {
                    }
                    catch (BadImageFormatException)
                    {
                    }
                    catch (NotSupportedException)
                    {
                        // Hopefully only dynamic assemblies
                        // We catch them above because we know dynamic assemblies do not support this
                    }
                }
            }
            return null;
        }

        static byte[] ReadStreamBytes(Stream stream)
        {
            try
            {
                var len = stream.Length;
                var data = new byte[len];
                stream.Read(data, 0, (int)len);
                return data;
            }
            finally
            {
                stream.Dispose();
            }
        }

        // duck typing stderr plays nice with e.g. swank 

        public static TextWriter errPrintWriter()
        {
            object w = ErrVar.deref();

            if (w is TextWriter tw)
                return tw;

            if (w is Stream s)
                return new StreamWriter(s);

            throw new ArgumentException("Unknown type for *err*");
        }

#pragma warning restore IDE1006 // Naming Styles
        #endregion

        #region Completely misc

        // I needed a ULP (unit of least precision) to implement in some tests.
        // JVM has this in java.lang.Math.  CLR's System.Math does not have this method.
        // So I'm defining it here.  Variations of this code all over StackOverflow.

        public static double Ulp(double x)
        {
            if (Double.IsNaN(x))
                return double.NaN;
            else if (Double.IsInfinity(x))
                return double.PositiveInfinity;
            else if (x == 0.0 || x == -0.0)
                return double.Epsilon;
            else
            {
                double absX = Math.Abs(x);
                var bits = BitConverter.DoubleToInt64Bits(absX);
                if (absX == Double.MaxValue)
                    return BitConverter.Int64BitsToDouble(bits) - BitConverter.Int64BitsToDouble(bits - 1);
                double nextValue = BitConverter.Int64BitsToDouble(bits + 1);
                return nextValue - absX;
            }
        }

        #endregion
    }

    public static class RuntimeBootstrapFlag
    {
        public static bool _doRTBootstrap = true;

        /// <summary>
        /// Disable file loading
        /// </summary>
        /// <remarks>Prevent the load method from searching the file system for .clj and .clj.dll files.  
        /// Used in production systems when all namespaces are to be found in loaded assemblies.</remarks>
        public static bool DisableFileLoad = false;
    }

}
