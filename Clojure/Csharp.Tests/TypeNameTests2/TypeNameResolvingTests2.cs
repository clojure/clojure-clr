using clojure.lang;
using clojure.lang.TypeName2;
using NUnit.Framework;
using System;

namespace Csharp.Tests.TypeNameTests2;

public class Simple { }

public class Outer
{
    public class Inner { }
}

public class OneG<T> { }
public class TwoG<T1, T2> { }

public class GenParent<T1, T2>
{
    public class Child
    {
        public class GrandChild<T3>
        {
            public class GreatGrandChild<T4, T5>
            {

            }
        }
    }
}


[TestFixture]
public class TypeNameResolvingTests2
{
    static Namespace _ns;

    [OneTimeSetUp]
    public void Setup()
    {
        RT.Init();

        _ns = Namespace.findOrCreate(Symbol.intern("Csharp.Tests"));

        _ns.importClass(Symbol.intern("TSimple"), typeof(Simple));
        _ns.importClass(Symbol.intern("TOneG"), typeof(OneG<>));
        _ns.importClass(Symbol.intern("TTwoG"), typeof(TwoG<,>));
        _ns.importClass(Symbol.intern("TGenParent"), typeof(GenParent<,>));

        RT.CurrentNSVar.bindRoot(_ns);
    }

    public static Type Resolve(string typeName) => ClrTypeSpec.GetTypeFromName(typeName, _ns);

    [Test]
    public void SimpleClassName_ResolvesCorrectly()
    {
        Assert.That(Resolve("Csharp.Tests.TypeNameTests2.Simple"), Is.EqualTo(typeof(Simple)));
    }

    [Test]
    public void NestedClassName_ResolvesCorrectly()
    {
        Assert.That(Resolve("Csharp.Tests.TypeNameTests2.Outer+Inner"), Is.EqualTo(typeof(Outer.Inner)));
    }

    [Test]
    public void GenericClassName_ResolvesCorrectly()
    {
        Assert.That(Resolve("Csharp.Tests.TypeNameTests2.OneG`1[System.String]"), Is.EqualTo(typeof(OneG<string>)));
        Assert.That(Resolve("Csharp.Tests.TypeNameTests2.TwoG`2[System.String,System.Int32]"), Is.EqualTo(typeof(TwoG<string, int>)));
        Assert.That(
            Resolve("Csharp.Tests.TypeNameTests2.GenParent`2+Child+GrandChild`1+GreatGrandChild`2[System.String, System.Int32, System.Double, System.String,System.Object]"),
            Is.EqualTo(typeof(GenParent<string, int>.Child.GrandChild<double>.GreatGrandChild<string, object>)));
    }

    [Test]
    public void NonExistentType_ResolvesToNull()
    {
        Assert.That(Resolve("Csharp.Tests.TypeNameTests2.NonExistent"), Is.Null);
        Assert.That(Resolve("Csharp.Tests.TypeNameTests2.Simple+NonExistent"), Is.Null);
        Assert.That(Resolve("Csharp.Tests.TypeNameTests2.OneG`1[Non.Existent]"), Is.Null);
    }

    [Test]
    public void PointerType_ResolvesCorrectly()
    {
        Assert.That(Resolve("System.String*"), Is.EqualTo(typeof(string).MakePointerType()));
        Assert.That(Resolve("System.String**"), Is.EqualTo(typeof(string).MakePointerType().MakePointerType()));
    }

    [Test]
    public void ByRefType_ResolvesCorrectly()
    {
        Assert.That(Resolve("System.String&"), Is.EqualTo(typeof(string).MakeByRefType()));
        Assert.That(Resolve("System.String*&"), Is.EqualTo(typeof(string).MakePointerType().MakeByRefType()));
    }

    [Test]
    public void ArrayType_ResolvesCorrectly()
    {
        Assert.That(Resolve("System.String[]"), Is.EqualTo(typeof(string).MakeArrayType()));
        Assert.That(Resolve("System.String[*]"), Is.EqualTo(typeof(string).MakeArrayType(1)));
        Assert.That(Resolve("System.String[,]"), Is.EqualTo(typeof(string).MakeArrayType(2)));
        Assert.That(Resolve("System.String[,,]"), Is.EqualTo(typeof(string).MakeArrayType(3)));
    }


    [Test]
    public void Everything_ResolvesCorrectly()
    {
        Assert.That(
            Resolve("Csharp.Tests.TypeNameTests2.GenParent`2+Child+GrandChild`1+GreatGrandChild`2[System.String, System.Int32, System.Double, System.String,System.Object][]**&"),
            Is.EqualTo(typeof(GenParent<string, int>.Child.GrandChild<double>.GreatGrandChild<string, object>).MakeArrayType().MakePointerType().MakePointerType().MakeByRefType()));
    }

    [TestCase("System.String", typeof(string))]
    [TestCase("Csharp.Tests.TypeNameTests2.Simple", typeof(Simple))]
    public void NamespaceQualifiedClassName_ParsesCorrectly(string typename, Type expectedType)
    {
        Assert.That(Resolve(typename), Is.EqualTo(expectedType));
    }


    [TestCase("int", typeof(int))]
    [TestCase("long", typeof(long))]
    [TestCase("float", typeof(float))]
    [TestCase("double", typeof(double))]
    [TestCase("bool", typeof(bool))]
    [TestCase("char", typeof(char))]
    [TestCase("byte", typeof(byte))]
    [TestCase("uint", typeof(uint))]
    [TestCase("ulong", typeof(ulong))]
    [TestCase("ushort", typeof(ushort))]
    [TestCase("sbyte", typeof(sbyte))]
    [TestCase("ints", typeof(int[]))]
    [TestCase("longs", typeof(long[]))]
    [TestCase("floats", typeof(float[]))]
    [TestCase("doubles", typeof(double[]))]
    [TestCase("bools", typeof(bool[]))]
    [TestCase("chars", typeof(char[]))]
    [TestCase("bytes", typeof(byte[]))]
    [TestCase("uints", typeof(uint[]))]
    [TestCase("ulongs", typeof(ulong[]))]
    [TestCase("ushorts", typeof(ushort[]))]
    [TestCase("sbytes", typeof(sbyte[]))]
    public void ClojureSimplelias_ParsesCorrectly(string typename, Type expectedType)
    {
        Assert.That(Resolve(typename), Is.EqualTo(expectedType));

    }

    [TestCase("TSimple", typeof(Simple))]
    [TestCase("TOneG", typeof(OneG<>))]
    [TestCase("TTwoG", typeof(TwoG<,>))]
    [TestCase("TGenParent", typeof(GenParent<,>))]
    [TestCase("String", typeof(string))]
    [TestCase("Int32", typeof(int))]
    public void AliasedTypename_ParsesCorrectly(string typename, Type expectedType)
    {
        Assert.That(Resolve(typename), Is.EqualTo(expectedType));
    }

    [TestCase("int[]", typeof(int[]))]
    [TestCase("String[]", typeof(string[]))]
    [TestCase("TSimple[]", typeof(Simple[]))]
    [TestCase("Csharp.Tests.TypeNameTests2.Simple[]", typeof(Simple[]))]
    public void ArrayType_ParsesCorrectly(string typename, Type expectedType)
    {
        Assert.That(Resolve(typename), Is.EqualTo(expectedType));
    }

    [TestCase("int*", typeof(int))]
    [TestCase("String*", typeof(String))]
    [TestCase("TSimple*", typeof(Simple))]
    [TestCase("Csharp.Tests.TypeNameTests2.Simple*", typeof(Simple))]
    public void PointerType_ParsesCorrectly(string typename, Type expectedType)
    {
        Assert.That(Resolve(typename), Is.EqualTo(expectedType.MakePointerType()));
    }


    [TestCase("int**", typeof(int))]
    [TestCase("String**", typeof(String))]
    [TestCase("TSimple**", typeof(Simple))]
    [TestCase("Csharp.Tests.TypeNameTests2.Simple**", typeof(Simple))]
    public void DoublePointerType_ParsesCorrectly(string typename, Type expectedType)
    {
        Assert.That(Resolve(typename), Is.EqualTo(expectedType.MakePointerType().MakePointerType()));
    }


    [TestCase("int&", typeof(int))]
    [TestCase("String&", typeof(String))]
    [TestCase("TSimple&", typeof(Simple))]
    [TestCase("Csharp.Tests.TypeNameTests2.Simple&", typeof(Simple))]
    public void ByRefType_ParsesCorrectly(string typename, Type expectedType)
    {
        Assert.That(Resolve(typename), Is.EqualTo(expectedType.MakeByRefType()));
    }



    [TestCase("TOneG[int]", typeof(OneG<int>))]
    [TestCase("TOneG[String]", typeof(OneG<string>))]
    [TestCase("TTwoG[int,String]", typeof(TwoG<int, string>))]
    public void AliasedGenericType_ParsesCorrectly(string typename, Type expectedType)
    {
        Assert.That(Resolve(typename), Is.EqualTo(expectedType));
    }



    [TestCase("Csharp.Tests.TypeNameTests2.OneG[int]", typeof(OneG<int>))]
    [TestCase("Csharp.Tests.TypeNameTests2.TwoG[int,String]", typeof(TwoG<int, string>))]
    [TestCase("Csharp.Tests.TypeNameTests2.GenParent[int,String]", typeof(GenParent<int, string>))]
    [TestCase("Csharp.Tests.TypeNameTests2.GenParent[int,String]+Child", typeof(GenParent<int, string>.Child))]
    [TestCase("Csharp.Tests.TypeNameTests2.GenParent[int,String]+Child+GrandChild[double]", typeof(GenParent<int, string>.Child.GrandChild<double>))]
    [TestCase("Csharp.Tests.TypeNameTests2.GenParent[int,String]+Child+GrandChild[double]+GreatGrandChild[int,long]", typeof(GenParent<int, string>.Child.GrandChild<double>.GreatGrandChild<int, long>))]
    [TestCase("TOneG[int]", typeof(OneG<int>))]
    [TestCase("TTwoG[int,String]", typeof(TwoG<int, string>))]
    [TestCase("TGenParent[int,String]", typeof(GenParent<int, string>))]
    [TestCase("TGenParent[int,String]+Child", typeof(GenParent<int, string>.Child))]
    [TestCase("TGenParent[int,String]+Child+GrandChild[double]", typeof(GenParent<int, string>.Child.GrandChild<double>))]
    [TestCase("TGenParent[int,String]+Child+GrandChild[double]+GreatGrandChild[int,long]", typeof(GenParent<int, string>.Child.GrandChild<double>.GreatGrandChild<int, long>))]
    [TestCase("TTwoG[TOneG[int],TGenParent[long,String]+Child]", typeof(TwoG<OneG<int>, GenParent<long, string>.Child>))]
    public void InferredArityGenericType_ParsesCorrectly(string typename, Type expectedType)
    {
        Assert.That(Resolve(typename), Is.EqualTo(expectedType));
    }

}


