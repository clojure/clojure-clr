using clojure.lang;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Csharp.Tests.TypeNameTests;

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

public class TypeSpecComparer
{
    IClrTypeIdentifier _name;
    string _assemblyName = null;
    List<IClrTypeIdentifier> _nested = [];
    List<TypeSpecComparer> _genericParams = [];
    List<IClrModifierSpec> _modifiers = [];
    bool _isByRef = false;

    class InternalName : IClrTypeIdentifier
    {
        public string DisplayName { get; init; }

        public InternalName(string name)
        {
            DisplayName = name;
        }

        public bool Equals(IClrTypeName other)
        {
            return other is not null && other.DisplayName == DisplayName;
        }

        string IClrTypeIdentifier.InternalName => throw new System.NotImplementedException();

        public IClrTypeName NestedName(IClrTypeIdentifier innerName)
        {
            throw new System.NotImplementedException();
        }
    }


    public bool SameAs(ClrTypeSpec spec)
    {
        if (spec == null)
            return false;
        if (!_name.Equals(spec.Name))
            return false;

        if (!string.Equals(_assemblyName, spec.AssemblyName))
            return false;

        if (_isByRef != spec.IsByRef)
            return false;

        var nested = spec.Nested.ToList();

        if (_nested.Count != nested.Count)
            return false;

        for (int i = 0; i < _nested.Count; i++)
            if (!_nested[i].Equals(nested[i]))
                return false;

        var genericParams = spec.GenericParams.ToList();

        if (_genericParams.Count != genericParams.Count)
            return false;

        for (int i = 0; i < _genericParams.Count; i++)
        {
            if (!_genericParams[i].SameAs(genericParams[i]))
                return false;
        }

        var modifiers = spec.Modifiers.ToList();

        if (_modifiers.Count != modifiers.Count)
            return false;

        for (int i = 0; i < _modifiers.Count; i++)
            if (!_modifiers[i].Equals(modifiers[i]))
                return false;

        return true;
    }

    public static TypeSpecComparer Create(string name)
    {
        var cmp = new TypeSpecComparer();
        cmp._name = new InternalName(name);

        return cmp;
    }

    public TypeSpecComparer WithAssembly(string assemblyName)
    {
        _assemblyName = assemblyName;
        return this;
    }

    public TypeSpecComparer WithNested(params string[] names)
    {
        _nested = names.Select(n => (IClrTypeIdentifier)new InternalName(n)).ToList();
        return this;
    }

    public TypeSpecComparer WithGenericParams(params TypeSpecComparer[] specs)
    {
        _genericParams = specs.ToList();
        return this;
    }

    public TypeSpecComparer WithModifiers(params IClrModifierSpec[] mods)
    {
        _modifiers = mods.ToList();
        return this;
    }

    public TypeSpecComparer SetIsByRef()
    {
        _isByRef = true;
        return this;
    }
}


[TestFixture]
public class TypeNameParsingTests
{
    //static Namespace _ns;

    [OneTimeSetUp]
    public void Setup()
    {
        //RT.Init();

        //_ns = Namespace.findOrCreate(Symbol.intern("Csharp.Tests"));

        //_ns.importClass(Symbol.intern("TTypeA"), typeof(TypeA));
        //_ns.importClass(Symbol.intern("TOneG"), typeof(OneG<>));
        //_ns.importClass(Symbol.intern("TTwoG"), typeof(TwoG<,>));
        //_ns.importClass(Symbol.intern("TGenParent"), typeof(GenParent<,>));

        //RT.CurrentNSVar.bindRoot(_ns);
    }


    [TestCase("A", "A", "#1")]
    [TestCase("A.B", "A.B", "#2")]
    [TestCase("A\\+B", "A\\+B", "#3")]
    public void BasicName_ParsesCorrectly(string typeName, string expect, string idString)
    {
        var spec = ClrTypeSpec.Parse(typeName);
        var cmp = TypeSpecComparer.Create(expect);
        Assert.That(cmp.SameAs(spec), Is.True, idString);
    }

    [Test]
    public void TypeNameStartsWithSpace_ParsesCorrectly()
    {
        var spec = ClrTypeSpec.Parse(" A.B");
        var cmp = TypeSpecComparer.Create("A.B");
        Assert.That(cmp.SameAs(spec), Is.True);
    }

    [Test]
    public void TypeNameWithSpaceAfterComma_ParsesCorrectly()
    {
        var cmp = TypeSpecComparer.Create("A")
            .WithGenericParams(
                TypeSpecComparer.Create("B"),
                TypeSpecComparer.Create("C"));

        var spec = ClrTypeSpec.Parse("A[B, C]");
        Assert.That(cmp.SameAs(spec), Is.True);

        spec = ClrTypeSpec.Parse("A[B,C]");
        Assert.That(cmp.SameAs(spec), Is.True);
    }

    [Test]
    public void NestedName_ParsesCorrectly()
    {
        var spec = ClrTypeSpec.Parse("A+B");
        var cmp = TypeSpecComparer.Create("A").WithNested("B");
        Assert.That(cmp.SameAs(spec), Is.True);

        spec = ClrTypeSpec.Parse("A+B+C");
        cmp = TypeSpecComparer.Create("A").WithNested("B", "C");
        Assert.That(cmp.SameAs(spec), Is.True);
    }

    [Test]
    public void AssemblyName_ParsesCorrectly()
    {
        var spec = ClrTypeSpec.Parse("A, MyAssembly");
        var cmp = TypeSpecComparer.Create("A").WithAssembly("MyAssembly");
        Assert.That(cmp.SameAs(spec), Is.True);

        spec = ClrTypeSpec.Parse("A+B, MyAssembly");
        cmp = TypeSpecComparer.Create("A").WithNested("B").WithAssembly("MyAssembly");
        Assert.That(cmp.SameAs(spec), Is.True);
    }

    [Test]
    public void ArraySpec_ParsesCorrectly()
    {
        var spec = ClrTypeSpec.Parse("A[]");
        var cmp = TypeSpecComparer.Create("A").WithModifiers(new ClrArraySpec(1, false));
        Assert.That(cmp.SameAs(spec), Is.True);

        spec = ClrTypeSpec.Parse("A[,,]");
        cmp = TypeSpecComparer.Create("A").WithModifiers(new ClrArraySpec(3, false));
        Assert.That(cmp.SameAs(spec), Is.True);

        spec = ClrTypeSpec.Parse("A[,][]");
        cmp = TypeSpecComparer.Create("A").WithModifiers(new ClrArraySpec(2, false), new ClrArraySpec(1, false));
        Assert.That(cmp.SameAs(spec), Is.True);

        spec = ClrTypeSpec.Parse("A[*]");
        cmp = TypeSpecComparer.Create("A").WithModifiers(new ClrArraySpec(1, true));
        Assert.That(cmp.SameAs(spec), Is.True);
    }

    [Test]
    public void PointerSpec_ParsesCorrectly()
    {
        var spec = ClrTypeSpec.Parse("A*");
        var cmp = TypeSpecComparer.Create("A").WithModifiers(new ClrPointerSpec(1));
        Assert.That(cmp.SameAs(spec), Is.True);

        spec = ClrTypeSpec.Parse("A**");
        cmp = TypeSpecComparer.Create("A").WithModifiers(new ClrPointerSpec(2));
        Assert.That(cmp.SameAs(spec), Is.True);

        spec = ClrTypeSpec.Parse("A*[]");
        cmp = TypeSpecComparer.Create("A").WithModifiers(new ClrPointerSpec(1), new ClrArraySpec(1, false));
        Assert.That(cmp.SameAs(spec), Is.True);

        spec = ClrTypeSpec.Parse("A*&");
        cmp = TypeSpecComparer.Create("A").WithModifiers(new ClrPointerSpec(1)).SetIsByRef();
        Assert.That(cmp.SameAs(spec), Is.True);
    }

    [Test]
    public void ByRef_ParsesCorrectly()
    {
        var spec = ClrTypeSpec.Parse("A&");
        var cmp = TypeSpecComparer.Create("A").SetIsByRef();
        Assert.That(cmp.SameAs(spec), Is.True);

        spec = ClrTypeSpec.Parse("A+B&");
        cmp = TypeSpecComparer.Create("A").WithNested("B").SetIsByRef();
        Assert.That(cmp.SameAs(spec), Is.True);

        spec = ClrTypeSpec.Parse("A*&");
        cmp = TypeSpecComparer.Create("A").WithModifiers(new ClrPointerSpec(1)).SetIsByRef();
        Assert.That(cmp.SameAs(spec), Is.True);

        spec = ClrTypeSpec.Parse("A[]&");
        cmp = TypeSpecComparer.Create("A").WithModifiers(new ClrArraySpec(1, false)).SetIsByRef();
        Assert.That(cmp.SameAs(spec), Is.True);
    }

    [Test]
    public void GenericParams_ParsesCorrectly()
    {
        var spec = ClrTypeSpec.Parse("A[B]");
        var cmp = TypeSpecComparer.Create("A")
            .WithGenericParams(
                TypeSpecComparer.Create("B"));
        Assert.That(cmp.SameAs(spec), Is.True);

        spec = ClrTypeSpec.Parse("A[B,C]");
        cmp = TypeSpecComparer.Create("A")
            .WithGenericParams(
                TypeSpecComparer.Create("B"),
                TypeSpecComparer.Create("C"));

        Assert.That(cmp.SameAs(spec), Is.True);
        spec = ClrTypeSpec.Parse("A[B+C,D]");
        cmp = TypeSpecComparer.Create("A")
            .WithGenericParams(
                TypeSpecComparer.Create("B").WithNested("C"),
                TypeSpecComparer.Create("D"));
        Assert.That(cmp.SameAs(spec), Is.True);

        spec = ClrTypeSpec.Parse("A[B,C[D]]");
        cmp = TypeSpecComparer.Create("A")
            .WithGenericParams(
                TypeSpecComparer.Create("B"),
                TypeSpecComparer.Create("C")
                    .WithGenericParams(
                        TypeSpecComparer.Create("D")));
        Assert.That(cmp.SameAs(spec), Is.True);

        spec = ClrTypeSpec.Parse("A[B[C],D[E,F]]");
        cmp = TypeSpecComparer.Create("A")
            .WithGenericParams(
                TypeSpecComparer.Create("B")
                    .WithGenericParams(
                        TypeSpecComparer.Create("C")),
                TypeSpecComparer.Create("D")
                    .WithGenericParams(
                        TypeSpecComparer.Create("E"),
                        TypeSpecComparer.Create("F")));
        Assert.That(cmp.SameAs(spec), Is.True);

        spec = ClrTypeSpec.Parse("A[B[C,D[E,F]],G]");
        cmp = TypeSpecComparer.Create("A")
            .WithGenericParams(
                TypeSpecComparer.Create("B")
                    .WithGenericParams(
                        TypeSpecComparer.Create("C"),
                        TypeSpecComparer.Create("D")
                            .WithGenericParams(
                                TypeSpecComparer.Create("E"),
                                TypeSpecComparer.Create("F"))),
                TypeSpecComparer.Create("G"));
        Assert.That(cmp.SameAs(spec), Is.True);

        spec = ClrTypeSpec.Parse("A+B[C,D[E,F]]");
        cmp = TypeSpecComparer.Create("A")
            .WithNested("B")
            .WithGenericParams(
                TypeSpecComparer.Create("C"),
                TypeSpecComparer.Create("D")
                    .WithGenericParams(
                        TypeSpecComparer.Create("E"),
                        TypeSpecComparer.Create("F")));
        Assert.That(cmp.SameAs(spec), Is.True);


        spec = ClrTypeSpec.Parse("A[ [B, AssemblyB], C, [D, AssemblyD]], AssemblyA");
        cmp = TypeSpecComparer.Create("A")
            .WithAssembly("AssemblyA")
            .WithGenericParams(
                TypeSpecComparer.Create("B").WithAssembly("AssemblyB"),
                TypeSpecComparer.Create("C"),
                TypeSpecComparer.Create("D").WithAssembly("AssemblyD"));
        Assert.That(cmp.SameAs(spec), Is.True);

    }

    [Test]
    public void GenericArg_CannotBeByRef()
    {
        var exn = Assert.Throws<System.ArgumentException>(() => ClrTypeSpec.Parse("A[B&]"));
        Assert.That(exn.Message, Does.Contain("Generic argument can't be byref or pointer type"));
    }

    [Test]
    public void GenericArg_CannotBePointer()
    {
        var exn = Assert.Throws<System.ArgumentException>(() => ClrTypeSpec.Parse("A[B*]"));
        Assert.That(exn.Message, Does.Contain("Generic argument can't be byref or pointer type"));

    }

    [Test]
    public void CannotTakeByRefOfByRef()
    {
        var exn = Assert.Throws<System.ArgumentException>(() => ClrTypeSpec.Parse("A&&"));
        Assert.That(exn.Message, Does.Contain("Can't have a byref of a byref"));



    }

    [Test]
    public void CannotHavePointerAfterByRef()
    {
        var exn = Assert.Throws<System.ArgumentException>(() => ClrTypeSpec.Parse("A&*"));
        Assert.That(exn.Message, Does.Contain("Can't have a pointer to a byref type"));
    }

    [Test]
    public void CannotHaveMissingCloseBracketInGenericArgumentAssemblyName()
    {
        var exn = Assert.Throws<System.ArgumentException>(() => ClrTypeSpec.Parse("A[[B, AssemblyB"));
        Assert.That(exn.Message, Does.Contain("Unmatched ']' while parsing generic argument assembly name"));
    }

    [Test]
    public void ByRefQualifierMustBeLast()
    {
        var exn = Assert.Throws<System.ArgumentException>(() => ClrTypeSpec.Parse("A&[]"));
        Assert.That(exn.Message, Does.Contain("Byref qualifier must be the last one of a type"));
    }

    [Test]
    public void MissingCharactersAfterLeftBracketIsBad()
    {
        var exn = Assert.Throws<System.ArgumentException>(() => ClrTypeSpec.Parse("A["));
        Assert.That(exn.Message, Does.Contain("Invalid array/generic spec"));
    }

    [Test]
    public void CannotHaveGenericArgsAfterArrayOrPointer()
    {
        var exn = Assert.Throws<System.ArgumentException>(() => ClrTypeSpec.Parse("A[][B]"));
        Assert.That(exn.Message, Does.Contain("generic args after array spec or pointer type"));

        exn = Assert.Throws<System.ArgumentException>(() => ClrTypeSpec.Parse("A*[B]"));
        Assert.That(exn.Message, Does.Contain("generic args after array spec or pointer type"));
    }

    [Test]
    public void InvalidGenericArgsSeparator_IsBad()
    {
        var exn = Assert.Throws<System.ArgumentException>(() => ClrTypeSpec.Parse("A[ [B, AssemblyB ] + C  ]  "));
        Assert.That(exn.Message, Does.Contain("Invalid generic arguments separator"));
    }

    [Test]
    public void ErrorParsingGenericParamsSpec_IsBad()
    {
        var exn = Assert.Throws<System.ArgumentException>(() => ClrTypeSpec.Parse("A[B,"));
        Assert.That(exn.Message, Does.Contain("Error parsing generic params spec"));
    }

    [Test]
    public void TwoBoundDesignatorsInArraySpec_IsBad()
    {
        var exn = Assert.Throws<System.ArgumentException>(() => ClrTypeSpec.Parse("A[**]"));
        Assert.That(exn.Message, Does.Contain("Array spec cannot have 2 bound dimensions"));
    }

    [Test]
    public void InvalidCharacterInArraySpec_IsBad()
    {
        var exn = Assert.Throws<System.ArgumentException>(() => ClrTypeSpec.Parse("A[*!]"));
        Assert.That(exn.Message, Does.Contain("Invalid character in array spec"));
    }

    [Test]
    public void ErrorParsingArraySpec_IsBad()
    {
        var exn = Assert.Throws<System.ArgumentException>(() => ClrTypeSpec.Parse("A[,"));
        Assert.That(exn.Message, Does.Contain("Error parsing array spec"));
    }

    [Test]
    public void CannotHaveBoundAndDimensionTogetherInArraySpec()
    {
        var exn = Assert.Throws<System.ArgumentException>(() => ClrTypeSpec.Parse("A[*,]"));
        Assert.That(exn.Message, Does.Contain("Invalid array spec, multi-dimensional array cannot be bound"));
    }


    [Test]
    public void UnmatchedRightBracket_IsBad()
    {
        var exn = Assert.Throws<System.ArgumentException>(() => ClrTypeSpec.Parse("A[]]"));
        Assert.That(exn.Message, Does.Contain("Unmatched ']'"));
    }


    [Test]
    public void UnknownCharacterInModifiers_IsBad()
    {
        var exn = Assert.Throws<System.ArgumentException>(() => ClrTypeSpec.Parse("A*!"));
        Assert.That(exn.Message, Does.Contain("Bad type def"));
    }

    // I don't know how to trigger this error
    //[Test]
    //public void UnclosedAssemblyQualifiedNameInGenericArg_IsBad()
    //{
    //    var exn = Assert.Throws<System.ArgumentException>(() => ClrTypeSpec.Parse("A[ [B, AssemblyB ]  ]  "));
    //    Assert.That(exn.Message, Does.Contain("Unclosed assembly-qualified type name"));
    //}
}

