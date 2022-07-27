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

using NUnit.Framework;
using static NExpect.Expectations;
using clojure.lang;
using System.Reflection;
using System.IO;
using NExpect;

namespace Clojure.Tests.LibTests
{
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE0060 // Remove unused parameter

    [TestFixture]
    public class GenProxyObjectOnlyTests
    {
        Type _proxyType;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            _proxyType = GenProxy.GenerateProxyClass(
                 typeof(object),
                 null,
                 "Object_");
        }

        [Test]
        public void GeneratedProxyHasCorrectName()
        {
            Expect(_proxyType.Name).To.Equal("Object_");
        }


        [Test]
        public void ImplementsIProxy()
        {
            Expect(_proxyType.GetInterface("IProxy")).Not.To.Be.Null();
        }
    }

    [TestFixture]
    public class GenProxyBasicSuperNoInterfaceTests
    {
        public class Impl1
        {

            public int im1(int s) { return 2 * s; }
            public int im1(string s) { return s.Length; }
            int im2(int s) { return 3 * s; }
            public void im3(int s) { }
        }

        Type _proxyType;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            _proxyType = GenProxy.GenerateProxyClass(
                 typeof(Impl1),
                 null,
                 "Impl1_");
        }

        [Test]
        public void HasCorrectBaseType()
        {
            Expect(_proxyType.BaseType).To.Equal(typeof(Impl1));
        }

        [Test]
        public void ImplementsPublicMethods()
        {
            Expect(_proxyType.GetMethod("im1",new Type[]{typeof(int)})).Not.To.Be.Null();
            Expect(_proxyType.GetMethod("im1", new Type[] { typeof(string) })).Not.To.Be.Null();
        }

        [Test]
        public void DoesNotImplementsPrivateMethods()
        {
            Expect(_proxyType.GetMethod("im2", new Type[] { typeof(int) })).To.Be.Null();
        }

        [Test]
        public void CanBeConstructedFromDefaultCtor()
        {
            object o = _proxyType.GetConstructor(Type.EmptyTypes).Invoke(Array.Empty<object>());
            Expect(o).Not.To.Be.Null();
        }

        [Test]
        public void CanCallBaseClassReflectedMethods()
        {
            object o = _proxyType.GetConstructor(Type.EmptyTypes).Invoke(Array.Empty<object>());
            Expect(_proxyType.GetMethod("im1",new Type[] {typeof(int)}).Invoke(o, new object[] { 21 })).To.Equal(42);
            Expect(_proxyType.GetMethod("im1", new Type[] { typeof(string) }).Invoke(o, new object[] { "test" })).To.Equal(4);
        }

        [Test]
        public void HandlesVoidReturnTypeMethods()
        {
            object o = _proxyType.GetConstructor(Type.EmptyTypes).Invoke(Array.Empty<object>());
            Expect(_proxyType.GetMethod("im3", new Type[] { typeof(int) }).Invoke(o, new object[] { 21 })).To.Be.Null();
        }
    }

    [TestFixture]
    public class GenProxyBasicSuperOneInterfaceTests
    {
        public class Impl1
        {
            public int im1(int s) { return 2 * s; }
            public int im1(string s) { return s.Length; }
            int im2(int s) { return 3 * s; }
            public void im3(int s) { }
        }

        public interface I1
        {
            int m1(int s);
            void m2(string s);
        }

        Type _proxyType;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            _proxyType = GenProxy.GenerateProxyClass(
                typeof(Impl1),
                RT.seq(LazilyPersistentVector.createOwning(typeof(I1))),
                "Impl1_I1");
        }

        [Test]
        public void ImplementsAppropriateInterfaces()
        {
            Expect(_proxyType.GetInterface("IProxy")).Not.To.Be.Null();
            Expect(_proxyType.GetInterface("I1")).Not.To.Be.Null();
        }

        [Test]
        public void ThrowsNotImplementedExceptionOnInterfaceMethod1()
        {
            object o = _proxyType.GetConstructor(Type.EmptyTypes).Invoke(Array.Empty<object>());
            try
            {
                _proxyType.GetMethod("m1").Invoke(o, new object[] { 21 });
            }
            catch (TargetInvocationException ex)
            {
                Expect(ex.InnerException is NotImplementedException);
            }
        }

        [Test]
        public void ThrowsNotImplementedExceptionOnInterfaceMethod2()
        {
            object o = _proxyType.GetConstructor(Type.EmptyTypes).Invoke(Array.Empty<object>());
           try
           {
               _proxyType.GetMethod("m2").Invoke(o, new object[] { "test" });
           }
           catch (TargetInvocationException ex)
           {
               Expect(ex.InnerException is NotImplementedException);
           }
        }


    }

    [TestFixture]
    public class GenProxyBasicSuperOneInterfaceMethodOverlapTests
    {
        public class Impl1
        {
            public int m1(int s) { return 2 * s; }
            public int m1(string s) { return s.Length; }
            int im2(int s) { return 3 * s; }
            public void im3(int s) { }
        }

        public interface I1
        {
            int m1(int s);
            void m2(string s);
        }

        Type _proxyType;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            _proxyType = GenProxy.GenerateProxyClass(
                typeof(Impl1),
                RT.seq(LazilyPersistentVector.createOwning(typeof(I1))),
                "Impl1_I1_2");
        }

        [Test]
        public void ImplementsAppropriateInterfaces()
        {
            Expect(_proxyType.GetInterface("IProxy")).Not.To.Be.Null();
            Expect(_proxyType.GetInterface("I1")).Not.To.Be.Null();
        }

        [Test]
        public void CanCallBaseClassReflectedMethods()
        {
            object o = _proxyType.GetConstructor(Type.EmptyTypes).Invoke(Array.Empty<object>());
            Expect(_proxyType.GetMethod("m1", new Type[] { typeof(int) }).Invoke(o, new object[] { 21 })).To.Equal(42);
            Expect(_proxyType.GetMethod("m1", new Type[] { typeof(string) }).Invoke(o, new object[] { "test" })).To.Equal(4);
        }

        [Test]
        public void ThrowsNotImplementedExceptionOnInterfaceMethod2()
        {
            object o = _proxyType.GetConstructor(Type.EmptyTypes).Invoke(Array.Empty<object>());
            try
            {
                _proxyType.GetMethod("m2").Invoke(o, new object[] { "test" });
            }
            catch (TargetInvocationException ex)
            {
                Expect(ex.InnerException is NotImplementedException);
            }
        }


    }

    [TestFixture]
    public class GenProxyBasicSuperTwoInterfacesTests
    {
        public class Impl1
        {
            public int im1(int s) { return 2 * s; }
            public int im1(string s) { return s.Length; }
            int im2(int s) { return 3 * s; }
            public void im3(int s) { }
        }

        public interface I1
        {
            int m1(int s);
            void m2(string s);
        }

         public interface I2
        {
            int m3(int s);
            int m3(string s);   //overload on method name
            int m4(string s);   //overload on paramtype+returntype
            int m1(int s);      // Collides with method on I1
        }

        Type _proxyType;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            _proxyType = GenProxy.GenerateProxyClass(
                typeof(Impl1),
                RT.seq(LazilyPersistentVector.createOwning(typeof(I1), typeof(I2))),
                "Impl1_I1I2");
        }

        [Test]
        public void ImplementsAppropriateInterfaces()
        {
            Expect(_proxyType.GetInterface("IProxy")).Not.To.Be.Null();
            Expect(_proxyType.GetInterface("I1")).Not.To.Be.Null();
            Expect(_proxyType.GetInterface("I2")).Not.To.Be.Null();
        }

    }

    [TestFixture]
    public class GenProxyIProxyTests
    {
        public class Impl1
        {
            public virtual int m1(int s)
            { 
                //Console.WriteLine("In Impl1.m1({0})", s);
                return 2 * s; 
            }

            public int m1(string s) 
            {
                //Console.WriteLine("In Impl1.m1({0})", s);
                return s.Length; 
            }

            int im2(int s) 
            {
                //Console.WriteLine("In Impl1.im2({0})", s);
                return 3 * s; 
            }
            public void im3(int s) 
            {
                //Console.WriteLine("In Impl1.im3({0})", s);
            }
        }

        public interface I1
        {
            int m1(int s);
            int m2(string s);
            void m2v(string s);
            string m2s(string s);
        }

        public interface I2
        {
            int m3(int s);
            int m3(string s);   //overload on method name
            int m4(string s);   //overload on paramtype+returntype
            int m1(int s);      // Collides with method on I1
        }

        public class AFunctionMeta : AFunction
        {
            protected IPersistentMap _meta;

            public AFunctionMeta()
            {
                _meta = null;
            }

            protected AFunctionMeta(IPersistentMap meta)
            {
                _meta = meta;
            }


            public override IObj withMeta(IPersistentMap meta)
            {
                throw new NotImplementedException();
            }

            public override IPersistentMap meta()
            {
                throw new NotImplementedException();
            }
        }

        public class Fn1 : AFunctionMeta
        {
            public override object invoke(object arg1, object arg2)
            {
                //Console.WriteLine("In Fn1");
                return 100;
            }
        }

        public class Fn2 : AFunctionMeta
        {
            public override object invoke(object arg1, object arg2)
            {
                //Console.WriteLine("In Fn2");
                return 200;
            }
        }

        public class Fn2V : AFunctionMeta
        {
            public static bool _called;

            public override object invoke(object arg1, object arg2)
            {
                //Console.WriteLine("In Fn2V");
                _called = true;
                return arg2;
            }
        }

        public class Fn2S : AFunctionMeta
        {
            public override object invoke(object arg1, object arg2)
            {
                //Console.WriteLine("In Fn2S");
                return "nice "+(string)arg2;
            }
        }



        public class Fn3 : AFunctionMeta
        {
            int _x;

            public Fn3(int x)
            {
                _x = x;
            }

            public override object invoke(object arg1, object arg2)
            {
                //Console.WriteLine("In Fn3");
                return _x;
            }
        }

        public class Fn4 : AFunctionMeta
        {
            int _x;

            public Fn4(int x)
            {
                _x = x;
            }

            public override object invoke(object arg1, object arg2)
            {
                //Console.WriteLine("In Fn4");
                return _x;
            }
        }

        Type _proxyType;
        Object _obj;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            _proxyType = GenProxy.GenerateProxyClass(
                typeof(Impl1),
                RT.seq(LazilyPersistentVector.createOwning(typeof(I1), typeof(I2))),
                "Impl1_I1I2_42");
        }

        [SetUp]
        public void Setup()
        {
            _obj = _proxyType.GetConstructor(Type.EmptyTypes).Invoke(Array.Empty<object>());

            IProxy ip = _obj as IProxy;
            ip.__initClojureFnMappings(
                PersistentHashMap.create(
                "m1", new Fn1(),
                "m2", new Fn2(),
                "m3", new Fn3(300),
                "m4", new Fn4(400),
                "m2v", new Fn2V(),
                "m2s", new Fn2S()
                ));
        }

        //[Test]
        //public void InitClojureFnMappingsDebug()
        //{
        //    GenProxy.SaveProxyContext();
        //}
            

        [Test]
        public void InitClojureFnMappingsWorks()
        {
            //SanityCheck.PrintMethods(_obj.GetType());
           
            Impl1 impl1 = _obj as Impl1;
            Expect(impl1.m1(21)).To.Equal(100);

            MethodInfo m1Method = _proxyType.GetMethod("m1", new Type[] { typeof(int) });
            Expect(m1Method).Not.To.Be.Null();
            Expect(m1Method.Invoke(_obj, new object[] { 21 })).To.Equal(100);

            I1 i1 = _obj as I1;
            Expect(i1).Not.To.Be.Null();
            Expect(i1.m1(42)).To.Equal(100);
            Expect(i1.m2("help")).To.Equal(200);
            // just hoping the next one doesn't blow up
            // We set a flag to test
            Fn2V._called = false;
            i1.m2v("abcd");
            Expect(Fn2V._called);

            Expect(i1.m2s("job")).To.Equal("nice job");

            I2 i2 = _obj as I2;
            Expect(i2).Not.To.Be.Null();
            Expect(i2.m1(25)).To.Equal(100);
            Expect(i2.m3(60)).To.Equal(300);
            Expect(i2.m3("test")).To.Equal(300);
            Expect(i2.m4("junk")).To.Equal(400);
        }

        [Test]
        public void GetClojureFnMappingsWorks()
        {
            IProxy ip = _obj as IProxy;
            IPersistentMap map = ip.__getClojureFnMappings();
            Expect(map.count()).To.Equal(6);
            Expect(map.containsKey("m1"));
            Expect(map.containsKey("m2"));
            Expect(map.containsKey("m2s"));
            Expect(map.containsKey("m2v"));
            Expect(map.containsKey("m3"));
            Expect(map.containsKey("m4"));
        }

        [Test]
        public void UpdateClojureFnMappingsWorks()
        {
            IProxy ip = _obj as IProxy;
            ip.__updateClojureFnMappings(
                PersistentHashMap.create("m3", new Fn3(500), "m4", new Fn4(600)));

            I2 i2 = _obj as I2;
            Expect(i2).Not.To.Be.Null();
            Expect(i2.m1(25)).To.Equal(100);
            Expect(i2.m3(60)).To.Equal(500);
            Expect(i2.m3("test")).To.Equal(500);
            Expect(i2.m4("junk")).To.Equal(600);


        }
    }

    [TestFixture]
    public class GenProxyMultipleBaseCtorsTests
    {
        public class Impl1
        {

            public int F1;
            public string F2 = String.Empty;

            public virtual int m1(int s) { return 2 * s; }
            public int m1(string s) { return s.Length; }
            int im2(int s) { return 3 * s; }

            public Impl1(int f1)
            {
                F1 = f1;
            }

            public Impl1(string f2)
            {
                F2 = f2;
            }

            public Impl1(int f1, string f2)
            {
                F1 = f1;
                F2 = f2;
            }
        }

        Type _proxyType;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            _proxyType = GenProxy.GenerateProxyClass(
                typeof(Impl1),
                null,
                "Impl1_2");
        }

        [Test]
        public void TestIntCtor()
        {
            object obj = _proxyType.GetConstructor(new Type[] { typeof(int)}).Invoke(new object[] { 42 });
            Expect(obj.GetType().GetField("F1").GetValue(obj)).To.Equal(42);
            Expect(obj.GetType().GetField("F2").GetValue(obj)).To.Equal(String.Empty);
        }


        [Test]
        public void TestStringCtor()
        {
            object obj = _proxyType.GetConstructor(new Type[] { typeof(string)}).Invoke(new object[] { "help" });
            Expect(obj.GetType().GetField("F1").GetValue(obj)).To.Equal(0);
            Expect(obj.GetType().GetField("F2").GetValue(obj)).To.Equal("help");
        }

        
        [Test]
        public void TestIntStringCtor()
        {
            object obj = _proxyType.GetConstructor(new Type[] { typeof(int), typeof(string)}).Invoke(new object[] { 42, "help" });
            Expect(obj.GetType().GetField("F1").GetValue(obj)).To.Equal(42);
            Expect(obj.GetType().GetField("F2").GetValue(obj)).To.Equal("help");
        }

       
    }

    [TestFixture]
    public class GenProxyInheritedInterfaceTests
    {

        public class Impl1
        {
            public int m1(int s) { return 2 * s; }
            public int m1(string s) { return s.Length; }
            int im2(int s) { return 3 * s; }
            public void im3(int s) { }
        }

        public interface I1
        {
            int m1(int s);
            void m2(string s);
        }

        public interface I2 : I1
        {
            int m3(int s);
            int m3(string s);   //overload on method name
            int m4(string s);   //overload on paramtype+returntype
        }

        Type _proxyType;
        Object _obj;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            _proxyType = GenProxy.GenerateProxyClass(
                typeof(Impl1),
                RT.seq(LazilyPersistentVector.createOwning(typeof(I2))),
                "Impl1_I2_sub_I1");
        }


        [SetUp]
        public void Setup()
        {
            _obj = _proxyType.GetConstructor(Type.EmptyTypes).Invoke(Array.Empty<object>());
        }

        [Test]
        public void JustSeeIfEverythingGetsCreated()
        {
            Impl1 impl1 = _obj as Impl1;
            Expect(impl1.m1(21)).To.Equal(42);
        }
    }

    [TestFixture]
    public class GenProxyRealisticTests
    {
        // This example sets up a proxy for System.IO.TextWriter that converts all characters to upper case.
        // The original code (in Clojure) from clojure-contrib is here:
        
        // (defn- upcase-writer 
        //  "Returns a proxy that wraps writer, converting all characters to upper case"
        //  [^java.io.Writer writer]
        //  (proxy [java.io.Writer] []
        //    (close [] (.close writer))
        //    (flush [] (.flush writer))
        //    (write ([^chars cbuf ^Integer off ^Integer len] 
        //              (.write writer cbuf off len))
        //           ([x]
        //              (condp = (class x)
        //                        String 
        //                          (let [s ^String x]
        //                             (.write writer (.toUpperCase s)))
        //                        Integer
        //                         (let [c ^Character x]
        //                             (.write writer (int (Character/toUpperCase (char c))))))))))
        // In a CLR version:
         //  (defn upcase-writer
         //    [^System.IO.TextWriter tw]
         //    (proxy [System.IO.TextWriter] []
         //      (Write ([^chars cbuf ^Int32 off #Int32 len] (.Write tw cbuf off len))
         //             ([x] (condp (class x)
         //                    System.String (let [s ^System.String x] (.Write tw (. s ToUpper)))
         //                    Int32 (let [c ^Int32 x] (.Write tw (int (Char/ToUpper (char c))))))))))

        // Not having the kind of closures I want, I'll fake it with a static variable.

        static StringWriter _tw;

        class CloseFn : AFn
        {
            public override object invoke(object ithis)
            {
                _tw.Close();
                return null;
            }
        }

        class FlushFn: AFn
        {
            public override object invoke(object ithis)
            {
                _tw.Flush();
                return null;
            }
        }

        class WriteFn : AFn
        {
            public override object invoke(object ithis, object arg1, object arg2, object arg3)
            {
                _tw.Write((char[])arg1, (int)arg2, (int)arg3);
                return null;
            }

            public override object invoke(object ithis, object arg1)
            {
                switch (Type.GetTypeCode(arg1.GetType()))
                {
                    case TypeCode.String:
                        string s = (string)arg1;
                        _tw.Write(s.ToUpper());
                        break;

                    case TypeCode.Int32:
                        int i = (int)arg1;
                        _tw.Write(Char.ToUpper(((Char)i)));
                        break;
                }

                return null;
            }        }

        Type _proxyType;
        Object _obj;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            _proxyType = GenProxy.GenerateProxyClass(
                typeof(TextWriter),
                null,
                "TextWriter1");
        }

        [SetUp]
        public void Setup()
        {
            _tw = new StringWriter();
            _obj = _proxyType.GetConstructor(Type.EmptyTypes).Invoke(Array.Empty<object>());

            IProxy ip = _obj as IProxy;
            ip.__initClojureFnMappings(
                PersistentHashMap.create(
                "Flush", new FlushFn(),
                "Close", new CloseFn(),
                "Write", new WriteFn()
                ));
        }



        [Test]
        public void ProxyObjectCanBeCreated()
        {
            Expect(_obj).Not.To.Be.Null();
        }

        [Test]
        public void ProxyObjectUppercasesString()
        {
            _proxyType.GetMethod("Write", new Type[] { typeof(String) }).Invoke(_obj, new object[] { "testing" });
            Expect(_tw.ToString()).To.Equal("TESTING");
        }


        [Test]
        public void ProxyObjectUppercasesInt()
        {
            MethodInfo mi = _proxyType.GetMethod("Write", new Type[] { typeof(int) });
            mi.Invoke(_obj, new object[] { 65 });
            Expect(_tw.ToString()).To.Equal("A");
        }

    }

    #region A little test for myself

    //[TestFixture]
    public class SanityCheck
    {
        public interface I1
        {
            int m1(int s);
            int m2(string s);
        }

        public class Impl1
        {
            public int m1(int s) { return 2 * s; }
            public int m1(string s) { return s.Length; }
            int m2(int s) { return 3 * s; }
        }

        public class Impl2 : I1
        {
            public int m1(int s) { return 2 * s; }
            public int m1(string s) { return s.Length; }
            int m2(int s) { return 3 * s; }
            public int m2(string s) { return s.Length; }
            public int m5(int x) { return 5*x; }
        }

        public class Impl3 : Impl2
        {
        }

#if NET462
        [Test]
        public void FindTypeInAssembly()
        {
            AssemblyName aname = new AssemblyName("MyAssy");
            AssemblyBuilder assyBldr = AppDomain.CurrentDomain.DefineDynamicAssembly(aname, AssemblyBuilderAccess.RunAndSave, ".");
            ModuleBuilder moduleBldr = assyBldr.DefineDynamicModule(aname.Name, aname.Name + ".dll", true);
            TypeBuilder tb = moduleBldr.DefineType("clojure.proxy.LongName.MyType", TypeAttributes.Public);
            Type myType = tb.CreateType();

            Type[] allTypes = assyBldr.GetTypes();
            Expect(allTypes.Contains<Type>(myType));
            Type findType = assyBldr.GetType("clojure.proxy.LongName.MyType", false);
            Expect(findType).Not.To.Be.Null();
            Type rtFound = RT.classForName("clojure.proxy.LongName.MyType");
            Expect(rtFound).Not.To.Be.Null();
           
        }
#endif

        //[Test]
        //public void TestThis()
        //{
        //    PrintMethods(typeof(Impl1));
        //    PrintMethods(typeof(Impl2));
        //    PrintMethods(typeof(Impl3));
        //    PrintMethods(typeof(I1));
        //    PrintInterfaceMaps(typeof(Impl1));
        //    PrintInterfaceMaps(typeof(Impl2));
        //    PrintInterfaceMaps(typeof(Impl3));

        //    Expect(true);
        //}

        private void PrintInterfaceMaps(Type type)
        {
            Console.WriteLine("Interface map for {0}",
                type.FullName);

            Type[] interfaces = type.GetInterfaces();
            if (interfaces.Length == 0)
                Console.WriteLine("No interfaces");
            else
            {
                foreach (Type iftype in interfaces)
                {
                    InterfaceMapping im = type.GetInterfaceMap(iftype);
                    MethodInfo[] ifMeths = im.InterfaceMethods;
                    MethodInfo[] tgtMeths = im.TargetMethods;

                    for (int i = 0; i < ifMeths.Length; i++)
                    {
                        PrintMethod(ifMeths[i]);
                        PrintMethod(tgtMeths[i]);
                        Console.WriteLine("-");
                    }
                }
            }

            Console.WriteLine("------");
        }

        internal static void PrintMethods(Type type)
        {
            Console.WriteLine("Methods for {0}", type.FullName);

            MethodInfo[] methods = type.GetMethods();
            foreach (MethodInfo method in methods)
                PrintMethod(method);

            Console.WriteLine("------------");

        }

        private static void PrintMethod(MethodInfo method)
        {
            Console.Write("{0} {1}(", method.ReturnType.FullName, method.Name);
            foreach (ParameterInfo p in method.GetParameters())
                Console.Write("{0}, ", p.ParameterType.FullName);
            Console.WriteLine(")");
            Console.WriteLine("Attribs: {0}", method.Attributes);
            Console.WriteLine("Calling: {0}", method.CallingConvention);
            Console.WriteLine("Impl:    {0}", method.GetMethodImplementationFlags());
        }

    }

    //[TestFixture]
    public class SanityCheck2
    {
        public abstract class C1
        {
            abstract public int Prop1 { get; }
            public abstract int m1(int x);
        }

        public interface I1
        {
            int m2(int x);
        }

#if NET462
        [Test]
        public void CanCreateConcreteImplementationOverAbstractProperty()
        {
            AssemblyName aname = new AssemblyName("MyAssy2");
            AssemblyBuilder assyBldr = AppDomain.CurrentDomain.DefineDynamicAssembly(aname, AssemblyBuilderAccess.RunAndSave, ".");
            ModuleBuilder moduleBldr = assyBldr.DefineDynamicModule(aname.Name, aname.Name + ".dll", true);
            TypeBuilder tb = moduleBldr.DefineType("C1Impl", TypeAttributes.Public, typeof(C1));
            tb.AddInterfaceImplementation(typeof(I1));

            MethodAttributes baseAttr = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual;
            MethodAttributes getSetAttr = baseAttr | MethodAttributes.SpecialName;
            MethodAttributes regAttribs = baseAttr & ~MethodAttributes.NewSlot;
            getSetAttr &= ~MethodAttributes.NewSlot;

            ILGenerator gen;

            MethodBuilder getProp1 = tb.DefineMethod("get_Prop1", getSetAttr, typeof(int), Type.EmptyTypes);
            gen = getProp1.GetILGenerator();
            gen.Emit(OpCodes.Ldc_I4_2);
            gen.Emit(OpCodes.Ret);

            MethodBuilder m1 = tb.DefineMethod("m1", regAttribs, typeof(int), new Type[] { typeof(int) });
            gen = m1.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Ldc_I4_3);
            gen.Emit(OpCodes.Add);
            gen.Emit(OpCodes.Ret);

            MethodBuilder m2 = tb.DefineMethod("m2", regAttribs, typeof(int), new Type[] { typeof(int) });
            gen = m2.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Ldc_I4_3);
            gen.Emit(OpCodes.Add);
            gen.Emit(OpCodes.Ret);

            Type myType = tb.CreateType();
            MethodInfo[] minfos = myType.GetMethods();

            Expect(minfos).Not.To.Be.Null();

            ConstructorInfo ctor = myType.GetConstructor(Type.EmptyTypes);
            object o = ctor.Invoke(new object[0]);

            MethodInfo getter = myType.GetMethod("get_Prop1");
            Expect(getter.Invoke(o, new object[0])).To.Equal(2));

            MethodInfo m1i = myType.GetMethod("m1");
            Expect(m1i.Invoke(o, new object[] { 12 })).To.Equal(15));

            I1 i1 = (I1)o;
            Expect(i1.m2(12)).To.Equal(15));
        }
#endif
        //[Test]
        //public void BoxingHurts()
        //{
        //    AssemblyName aname = new AssemblyName("MyAssy3");
        //    AssemblyBuilder assyBldr = AppDomain.CurrentDomain.DefineDynamicAssembly(aname, AssemblyBuilderAccess.RunAndSave, ".");
        //    ModuleBuilder moduleBldr = assyBldr.DefineDynamicModule(aname.Name, aname.Name + ".dll", true);
        //    TypeBuilder tb = moduleBldr.DefineType("TheBoxer", TypeAttributes.Public, typeof(object));

        //    MethodAttributes baseAttr = MethodAttributes.Public | MethodAttributes.HideBySig;

        //    MethodBuilder mb1 = tb.DefineMethod("Add1", baseAttr, typeof(int), new Type[] { typeof(int), typeof(int) });
        //    ILGenerator gen1 = mb1.GetILGenerator();
        //    LocalBuilder loc_i = gen1.DeclareLocal(typeof(Int32));
        //    LocalBuilder loc_sum = gen1.DeclareLocal(typeof(Int32));
        //    Label loopLabel = gen1.DefineLabel();
        //    Label testLabel = gen1.DefineLabel();

        //    // i=0;
        //    gen1.Emit(OpCodes.Ldc_I4_0);
        //    gen1.Emit(OpCodes.Stloc,loc_i);

        //    // sum = 0;
        //    gen1.Emit(OpCodes.Ldc_I4_0);
        //    gen1.Emit(OpCodes.Stloc,loc_sum);

        //    gen1.MarkLabel(loopLabel);


        //    // sum = sum + (c+c) + (c+c)
        //    gen1.Emit(OpCodes.Ldarg_2);
        //    gen1.Emit(OpCodes.Box,typeof(Int32));
        //    gen1.Emit(OpCodes.Unbox_Any, typeof(Int32));
        //    gen1.Emit(OpCodes.Ldarg_2);
        //    gen1.Emit(OpCodes.Box, typeof(Int32));
        //    gen1.Emit(OpCodes.Unbox_Any, typeof(Int32));
        //    gen1.Emit(OpCodes.Add);
        //    gen1.Emit(OpCodes.Ldarg_2);
        //    gen1.Emit(OpCodes.Box, typeof(Int32));
        //    gen1.Emit(OpCodes.Unbox_Any, typeof(Int32));
        //    gen1.Emit(OpCodes.Ldarg_2);
        //    gen1.Emit(OpCodes.Box, typeof(Int32));
        //    gen1.Emit(OpCodes.Unbox_Any, typeof(Int32));
        //    gen1.Emit(OpCodes.Add);
        //    gen1.Emit(OpCodes.Add);
        //    gen1.Emit(OpCodes.Ldloc, loc_sum);
        //    gen1.Emit(OpCodes.Add);
        //    gen1.Emit(OpCodes.Stloc, loc_sum);

        //    // i = i + 1
        //    gen1.Emit(OpCodes.Ldloc, loc_i);
        //    gen1.Emit(OpCodes.Ldc_I4_1);
        //    gen1.Emit(OpCodes.Add);
        //    gen1.Emit(OpCodes.Stloc, loc_i);

        //    // Test: i < n
        //    gen1.MarkLabel(testLabel);
        //    gen1.Emit(OpCodes.Ldloc, loc_i);
        //    gen1.Emit(OpCodes.Ldarg_1);
        //    gen1.Emit(OpCodes.Blt, loopLabel);


        //    gen1.Emit(OpCodes.Ret);

        //    MethodBuilder mb2 = tb.DefineMethod("Add2", baseAttr, typeof(int), new Type[] { typeof(int), typeof(int) });
        //    ILGenerator gen2 = mb2.GetILGenerator();
        //    loc_i = gen2.DeclareLocal(typeof(Int32));
        //    loc_sum = gen2.DeclareLocal(typeof(Int32));
        //    loopLabel = gen2.DefineLabel();
        //    testLabel = gen2.DefineLabel();

        //    // i=0;
        //    gen2.Emit(OpCodes.Ldc_I4_0);
        //    gen2.Emit(OpCodes.Stloc, loc_i);

        //    // sum = 0;
        //    gen2.Emit(OpCodes.Ldc_I4_0);
        //    gen2.Emit(OpCodes.Stloc, loc_sum);

        //    gen2.MarkLabel(loopLabel);


        //    // sum = sum + (c+c) + (c+c)
        //    gen2.Emit(OpCodes.Ldarg_2);
        //    gen2.Emit(OpCodes.Ldarg_2);
        //    gen2.Emit(OpCodes.Add);
        //    gen2.Emit(OpCodes.Ldarg_2);
        //    gen2.Emit(OpCodes.Ldarg_2);
        //    gen2.Emit(OpCodes.Add);
        //    gen2.Emit(OpCodes.Add);
        //    gen2.Emit(OpCodes.Ldloc, loc_sum);
        //    gen2.Emit(OpCodes.Add);
        //    gen2.Emit(OpCodes.Stloc, loc_sum);

        //    // i = i + 1
        //    gen2.Emit(OpCodes.Ldloc, loc_i);
        //    gen2.Emit(OpCodes.Ldc_I4_1);
        //    gen2.Emit(OpCodes.Add);
        //    gen2.Emit(OpCodes.Stloc, loc_i);

        //    // Test: i < n
        //    gen2.MarkLabel(testLabel);
        //    gen2.Emit(OpCodes.Ldloc, loc_i);
        //    gen2.Emit(OpCodes.Ldarg_1);
        //    gen2.Emit(OpCodes.Blt, loopLabel);

        //    gen2.Emit(OpCodes.Ret);

        //    Type myType = tb.CreateType();

        //    ConstructorInfo ctor = myType.GetConstructor(Type.EmptyTypes);
        //    object o = ctor.Invoke(new object[0]);

        //    MethodInfo add1 = myType.GetMethod("Add1");
        //    MethodInfo add2 = myType.GetMethod("Add2");

        //    Stopwatch sw = new Stopwatch();

        //    sw.Start();
        //    add1.Invoke(o, new object[] { 1000000, 2 });
        //    sw.Stop();

        //    Console.WriteLine("Add1: {0} ticks", sw.ElapsedTicks);

        //    sw.Reset();

        //    sw.Start();
        //    add1.Invoke(o, new object[] { 1000000, 2 });
        //    sw.Stop();

        //    Console.WriteLine("Add2: {0} ticks", sw.ElapsedTicks);

        //}


    }

    #endregion
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore CA1822 // Mark members as static
#pragma warning restore CA1822 // Mark members as static
#pragma warning restore IDE0060 // Remove unused parameter
}
