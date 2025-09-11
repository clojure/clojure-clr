using clojure.lang;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Csharp.Tests;

public class TypeA { }
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
public class TypeNameParsingTests
{
    static Namespace _ns;

    [OneTimeSetUp]
    public void Setup()
    {
        RT.Init();

        _ns = Namespace.findOrCreate(Symbol.intern("Csharp.Tests"));

        _ns.importClass(Symbol.intern("TTypeA"), typeof(TypeA));
        _ns.importClass(Symbol.intern("TOneG"), typeof(OneG<>));
        _ns.importClass(Symbol.intern("TTwoG"), typeof(TwoG<,>));
        _ns.importClass(Symbol.intern("TGenParent"), typeof(GenParent<,>));

        RT.CurrentNSVar.bindRoot(_ns);
    }

    [TestCase("System.String", typeof(string))]
    [TestCase("Csharp.Tests.TypeA", typeof(TypeA))]
    public void NamespaceQualifiedClassName_ParsesCorrectly(string typename, Type expectedType)
    {
        var type = RT.classForName(typename);
        Assert.That(type, Is.EqualTo(expectedType));
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
    public void ClojureTypeAlias_ParsesCorrectly(string typename, Type expectedType)
    {
        var type = RT.classForName(typename);
        Assert.That(type, Is.EqualTo(expectedType));
    }

    [TestCase("TTypeA", typeof(TypeA))]
    [TestCase("TOneG", typeof(OneG<>))]
    [TestCase("TTwoG", typeof(TwoG<,>))]
    [TestCase("TGenParent", typeof(GenParent<,>))]
    [TestCase("String", typeof(string))]
    [TestCase("Int32", typeof(int))]
    public void AliasedTypename_ParsesCorrectly(string typename, Type expectedType)
    {
        var type = RT.classForName(typename);
        Assert.That(type, Is.EqualTo(expectedType));
    }

    [TestCase("int[]", typeof(int[]))]
    [TestCase("String[]", typeof(string[]))]
    [TestCase("TTypeA[]", typeof(TypeA[]))]
    [TestCase("Csharp.Tests.TypeA[]", typeof(TypeA[]))]
    public void ArrayType_ParsesCorrectly(string typename, Type expectedType)
    {
        var type = RT.classForName(typename);
        Assert.That(type, Is.EqualTo(expectedType));
    }


    [TestCase("int*", typeof(int))]
    [TestCase("String*", typeof(String))]
    [TestCase("TTypeA*", typeof(TypeA))]
    [TestCase("Csharp.Tests.TypeA*", typeof(TypeA))]
    public void PointerType_ParsesCorrectly(string typename, Type expectedType)
    {
        var type = RT.classForName(typename);
        Assert.That(type, Is.EqualTo(expectedType.MakePointerType()));
    }

    [TestCase("int&", typeof(int))]
    [TestCase("String&", typeof(String))]
    [TestCase("TTypeA&", typeof(TypeA))]
    [TestCase("Csharp.Tests.TypeA&", typeof(TypeA))]
    public void ByRefType_ParsesCorrectly(string typename, Type expectedType)
    {
        var type = RT.classForName(typename);
        Assert.That(type, Is.EqualTo(expectedType.MakeByRefType()));
    }

    [TestCase("TOneG[int]", typeof(OneG<int>))]
    [TestCase("TOneG[String]", typeof(OneG<string>))]
    [TestCase("TTwoG[int,String]", typeof(TwoG<int, string>))]
    public void GenericType_ParsesCorrectly(string typename, Type expectedType)
    {
        var type = RT.classForName(typename);
        Assert.That(type, Is.EqualTo(expectedType));
    }

    [TestCase("System.Collections.Generic.Dictionary`2[System.String, System.Collections.Generic.List`1[System.Int64]]",
              typeof(Dictionary<string, List<long>>))]
    //[TestCase("Csharp.Tests.TwoG`2[System.Int32, Csharp.Tests.OneG`1[System.String]]", typeof(TwoG<int, OneG<string>>))]

    //[TestCase("TTwoG[int, TOneG[String]]", typeof(TwoG<int, OneG<string>>))]
    public void GenericType_ParsesCorrectly1(string typename, Type expectedType)
    {
        var type = RT.classForName(typename);
        Assert.That(type, Is.EqualTo(expectedType));
    }

    [TestCase("System.Collections.Generic.Dictionary`2[System.String, System.Collections.Generic.List`1[System.Int64]]",
        typeof(Dictionary<string, List<long>>))]
    [TestCase("TTwoG[int, TOneG[String]]", typeof(TwoG<int, OneG<string>>))]
    public void GenericType_ParsesCorrectly2(string typename, Type expectedType)
    {
        var type = ClrTypeSpec.GetTypeFromName(typename);
        Assert.That(type, Is.EqualTo(expectedType));
    }


}

