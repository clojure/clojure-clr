/**
 *   Copyright (c) Rich Hickey. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace clojure.lang.CljCompiler.Ast
{
    // In invocation position
    //   param-tags - direct invocation of resolved constructor, static, or instance method
    //   else method invocation with inference + reflection
    // In value position, will emit as a multi-arity method thunk with
    //	 params matching the arity set of the named method

    public class QualifiedMethodExpr : Expr
    {
        #region nested classes

        public enum EMethodKind
        {
            CTOR, INSTANCE, STATIC
        }

        public class SignatureHint
        {
            public GenericTypeArgList GenericTypeArgs {  get; private set; }
            public List<Type> Args { get; private set; }

            public int ArgCount => Args?.Count ?? 0;

            private SignatureHint(IPersistentVector tagV)
            {
                // tagV is not null, but might be empty.
                // tagV == []  -> no type-args, zero-argument method or property or field.
                if ( tagV == null || tagV.count() == 0)
                {
                    GenericTypeArgs = GenericTypeArgList.Empty;
                    Args = null;
                    return;
                }

                var firstItem = tagV.nth(0);
                ISeq remainingTags = null;

                if (firstItem is ISeq && RT.first(firstItem) is Symbol symbol && symbol.Equals(HostExpr.TypeArgsSym))
                {
                    GenericTypeArgs = GenericTypeArgList.Create(RT.next(firstItem));
                    remainingTags = RT.next(tagV);
                }
                else
                {
                    GenericTypeArgs = GenericTypeArgList.Empty;
                    remainingTags = RT.seq(tagV);
                }

                Args = Compiler.TagsToClasses(remainingTags);
            }


            public static SignatureHint MaybeCreate(IPersistentVector tagV)
            {
                if (tagV == null)
                    return null;

                return new SignatureHint(tagV);
            }

        }


        #endregion

        #region Data

        public Type MethodType { get; private set; }
        public SignatureHint HintedSig { get; private set; }
        readonly Symbol _methodSymbol;
        public string MethodName { get; private set; }
        public EMethodKind Kind { get; private set; }
        readonly Type _tagClass;

        #endregion

        #region Ctors

        public QualifiedMethodExpr(Type methodType, Symbol sym)
        {
            MethodType = methodType;
            _methodSymbol = sym;
            _tagClass = Compiler.TagOf(sym) != null ? HostExpr.TagToType(Compiler.TagOf(sym)) : typeof(AFn);
            HintedSig = SignatureHint.MaybeCreate(Compiler.ParamTagsOf(sym));

            if (sym.Name.StartsWith("."))
            {
                Kind = EMethodKind.INSTANCE;
                MethodName = sym.Name.Substring(1);
            }
            else if (sym.Name.Equals("new"))
            {
                Kind = EMethodKind.CTOR;
                MethodName = sym.Name;
            }
            else
            {
                Kind = EMethodKind.STATIC;
                MethodName = sym.Name;
            }
        }

        #endregion


        #region Type mangling

        public bool HasClrType => true;

        public Type ClrType => _tagClass;

        #endregion

        #region eval

        public object Eval()
        {
            return BuildThunk(new ParserContext(RHC.Eval), this).Eval();
        }

        #endregion

        #region Code generation

        public void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            BuildThunk(new ParserContext(rhc), this).Emit(rhc, objx, ilg);
        }

        public bool HasNormalExit()
        {
            return true;
        }

        // (Java) TBD: caching/reuse of thunks
        private static FnExpr BuildThunk(ParserContext  pcon, QualifiedMethodExpr qmexpr)
        {
            // When qualified symbol has param-tags:
            //   (fn invoke__Class_meth ([this? args*] (methodSymbol this? args*)))
            // When no param-tags:
            //   (fn invoke__Class_meth ([this?] (methodSymbol this?))
            //                          ([this? arg1] (methodSymbol this? arg1)) ...)

            IPersistentCollection form = PersistentVector.EMPTY;
            Symbol instanceParam = qmexpr.Kind == EMethodKind.INSTANCE ? Compiler.ThisSym : null;
            string thunkName = $"invoke__{qmexpr.MethodType.Name}_{qmexpr._methodSymbol.Name}";

            HashSet<int> arities;

            if (qmexpr.HintedSig != null )
            {
                arities = new HashSet<int>();
                arities.Add(qmexpr.HintedSig.ArgCount);
            }
            else
                arities = AritySet(qmexpr.MethodType, qmexpr.MethodName, qmexpr.Kind);

            foreach (int arity in arities)
            {
                IPersistentVector parameters = BuildParams(instanceParam, arity);
                ISeq body = RT.listStar(qmexpr._methodSymbol, parameters.seq());
                form = RT.conj(form, RT.list(parameters, body));
            }

            ISeq thunkForm = RT.listStar(Symbol.intern("fn"),Symbol.intern(thunkName), RT.seq(form));
            return (FnExpr) Compiler.AnalyzeSeq(pcon,thunkForm,thunkName);
        }

        private static IPersistentVector BuildParams(Symbol instanceParam, int arity)
        {
            IPersistentVector parameters = PersistentVector.EMPTY;
            if (instanceParam != null)
                parameters = parameters.cons(instanceParam);
            for (int i = 0; i < arity; i++)
                parameters = parameters.cons(Symbol.intern(null, $"arg{i + 1}"));

            return parameters;
        }

        // Given a class, method name, and method kind, returns a sorted set of
        // arity counts pertaining to the method's overloads

        private static HashSet<int> AritySet(Type t, string methodName, EMethodKind kind)
        {
            var res = new HashSet<int>();
            List<MethodBase> methods = MethodsWithName(t, methodName, kind);

            foreach ( var method in methods)
                res.Add(method.GetParameters().Length);

            return res;
        }

        // Returns a list of methods or ctors matching the name and kind given.
        // Otherwise, will throw if the information provided results in no matches
        private static List<MethodBase> MethodsWithName(Type t, string methodName, EMethodKind kind)
        {
            if (kind == EMethodKind.CTOR)
            {
                var ctors = t.GetConstructors().ToList<MethodBase>();
                if (ctors.Count == 0)
                    throw NoMethodWithNameException(t, methodName, kind);
                return ctors;
            }

            var methods = t.GetMethods().ToList<MethodBase>()
                .Where(m => m.Name.Equals(methodName) && (kind == EMethodKind.INSTANCE ? !m.IsStatic : m.IsStatic))
                .ToList();

            if (methods.Count == 0)
                throw NoMethodWithNameException(t, methodName, kind);

            return methods;
        }

        internal static MethodBase ResolveHintedMethod(Type t, string methodName, EMethodKind kind, SignatureHint hint)
        {
            // hint is non-null and hint.IsEmpty is false;
            // It might have generic type args, and/or args

            List<MethodBase> methods = MethodsWithName(t, methodName, kind);

            // If we have generic type args and the list is non-empty, we need to choose only methods that have the same number of generic type args, fully instantiated.

            int gtaCount = hint.GenericTypeArgs?.Count ?? 0;
            if ( gtaCount > 0)
            {
                methods = methods
                    .Where(m => m.IsGenericMethod && m.GetGenericArguments().Length == gtaCount)
                    .Select(m => ((MethodInfo)m).MakeGenericMethod(hint.GenericTypeArgs.ToArray()))
                    .Cast<MethodBase>()
                    .ToList();
            }

            int arity = hint.Args?.Count ?? 0;

            var filteredMethods = methods
                .Where(m => m.GetParameters().Length == arity)
                .Where(m => Compiler.SignatureMatches(hint.Args, m))
                .ToList();

            if (filteredMethods.Count == 1)
                return filteredMethods[0];
            else
                throw ParamTagsDontResolveException(t, methodName, hint);
         
        }

        private static ArgumentException NoMethodWithNameException(Type t, string methodName, EMethodKind kind)
        {
            string kindStr = kind == EMethodKind.CTOR ? "" : kind.ToString().ToLower();
            return new ArgumentException($"Error - no matches found for {kindStr} {Compiler.MethodDescription(t, methodName)}");
        }

        private static ArgumentException ParamTagsDontResolveException(Type t, string methodName, SignatureHint hint)
        {
            List<Type> hintedSig = hint.Args;

            IEnumerable<Object> tagNames = hintedSig.Cast<Object>().Select(tag => tag == null ? Compiler.ParamTagAny : tag);
            IPersistentVector paramTags = PersistentVector.create(tagNames);

            string genericTypeArgs = hint.GenericTypeArgs == null ? "" : $"<{hint.GenericTypeArgs}>";

            return new ArgumentException($"Error - param-tags {genericTypeArgs}{paramTags} insufficient to resolve {Compiler.MethodDescription(t, methodName)}");
        }


        #endregion
    }
}
