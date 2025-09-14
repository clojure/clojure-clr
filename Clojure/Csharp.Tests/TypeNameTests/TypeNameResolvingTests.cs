using clojure.lang.TypeName;
using NUnit.Framework;
using System;

namespace Csharp.Tests.TypeNameTests;


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
public class TypeNameResolvingTests
{

    public static Type TR(string typename, bool throwOnError) => Type.GetType(typename, throwOnError);


    public static Type Resolve(string typeName)
    {
        var spec = ClrTypeSpec.Parse(typeName);
        return spec?.Resolve(null, (assemblyName, typename, throwOnError) => TR(typename, throwOnError), false, false);
    }

    [Test]
    public void SimpleClassName_ResolvesCorrectly()
    {
        Assert.That(Resolve("Csharp.Tests.TypeNameTests.Simple"), Is.EqualTo(typeof(Simple)));
    }

    [Test]
    public void NestedClassName_ResolvesCorrectly()
    {
        Assert.That(Resolve("Csharp.Tests.TypeNameTests.Outer+Inner"), Is.EqualTo(typeof(Outer.Inner)));
    }

    [Test]
    public void GenericClassName_ResolvesCorrectly()
    {
        Assert.That(Resolve("Csharp.Tests.TypeNameTests.OneG`1[System.String]"), Is.EqualTo(typeof(OneG<string>)));
        Assert.That(Resolve("Csharp.Tests.TypeNameTests.TwoG`2[System.String,System.Int32]"), Is.EqualTo(typeof(TwoG<string, int>)));
        Assert.That(
            Resolve("Csharp.Tests.TypeNameTests.GenParent`2+Child+GrandChild`1+GreatGrandChild`2[System.String, System.Int32, System.Double, System.String,System.Object]"),
            Is.EqualTo(typeof(GenParent<string, int>.Child.GrandChild<double>.GreatGrandChild<string, object>)));
    }

    [Test]
    public void NonExistentType_ResolvesToNull()
    {
        Assert.That(Resolve("Csharp.Tests.TypeNameTests.NonExistent"), Is.Null);
        Assert.That(Resolve("Csharp.Tests.TypeNameTests.Simple+NonExistent"), Is.Null);
        Assert.That(Resolve("Csharp.Tests.TypeNameTests.OneG`1[Non.Existent]"), Is.Null);
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
            Resolve("Csharp.Tests.TypeNameTests.GenParent`2+Child+GrandChild`1+GreatGrandChild`2[System.String, System.Int32, System.Double, System.String,System.Object][]**&"),
            Is.EqualTo(typeof(GenParent<string, int>.Child.GrandChild<double>.GreatGrandChild<string, object>).MakeArrayType().MakePointerType().MakePointerType().MakeByRefType()));
    }
}
