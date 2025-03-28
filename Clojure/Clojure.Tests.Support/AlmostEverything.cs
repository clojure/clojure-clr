using System;

namespace Clojure.Tests.Support;

public class AlmostEverything
{
    // Testing property/field/0-arity-method
    public string InstanceField = "InstanceField";
    public static string StaticField = "StaticField";
    public string InstanceProperty { get; set; } = "InstanceProperty";
    public static string StaticProperty { get; set; } = "StaticProperturn";
    public string InstanceMethod0() => "InstanceMethod0";
    public static string StaticMethod0() => "StaticMethod0";

    // for testing reflection on zero-arity member access
    // field/property/0-arity-method
    // Other classes provide other definitions

    // here we have a field
    public string ZeroArityMember = "field";

    // Testing constructors

    string _msg;
    object _data;

    public override string ToString()
    {
        return _msg;
    }

    public AlmostEverything()
    {
        _msg = "void";
        _data = null;
    }

    public AlmostEverything(int x)
    {
        _msg = "int";
        _data = x;
    }

    public AlmostEverything(string x)
    {
        _msg = "string";
        _data = x;
    }

    public AlmostEverything(ref int x)
    {
        _msg = "ref int";
        _data = x;
        x += 1;
    }

    // Trying for some ambiguity in calls with ref parameters
    public AlmostEverything(string x, ref int y)
    {
        _msg = "string+ref int";
        _data = x;
        y += 20;
    }

    // Trying for some ambiguity in calls with ref parameters
    public AlmostEverything(int x, ref int y)
    {
        _msg = "int+ref int";
        _data = x;
        y += 30;
    }



    // Testing overloads
    public string Over() => "no-arg";
    public string Over(int v) => "int";
    public string Over(long v) => "long";
    public string Over(double v) => "double";
    public string Over(object v) => "object";
    public string Over(string format, object arg0) => String.Format(format, arg0);
    public string Over(string format, object arg0, object arg1) => String.Format(format, arg0, arg1);
    public string Over(string format, params object[] args) => String.Format(format, args);

    // Testing ref/out resolving
    public int Out(int x) { return x + 10; }
    public int Out(ref int x) { x += 1; return x + 20; }

    // Testing non-resolving of simple arg
    public int Out2(ref int v) { return v + 1; }

    // Testing some ambiguity
    public string Ambig(string x, ref int y) { y += 10; return x + y.ToString(); }
    public int Ambig(int x, ref int y) { y += 100; return x + y; }


    // Testing ambiguity in the ref
    public string AmbigRef(ref int x) { x += 111; return x.ToString(); }
    public string AmbigRef(ref string x) { x += "abc"; return x; }

    public int Params(string format, params object[] args) => args.Length;
}

// For testing reflection
public class AlmostEverything2
{
    public string Over(string format, object arg0) => String.Format(format+"!!!", arg0);
    public string InstanceField => "InstanceField2";
    public static string StaticField => "StaticField2";
    public string InstanceProperty { get; set; } = "InstanceProperty2";
    public static string StaticProperty { get; set; } = "StaticProperturn2";
    public string InstanceMethod0() => "InstanceMethod02";
    public static string StaticMethod0() => "StaticMethod02";


    // for testing reflection on zero-arity member access
    // field/property/0-arity-method
    // Other classes provide other definitions

    // here we have a property
    public string ZeroArityMember { get; set; } =  "property";
}

// For testing reflection
public class AlmostEverything3
{
    // for testing reflection on zero-arity member access
    // field/property/0-arity-method
    // Other classes provide other definitions

    // here we have a zero-arity method
    public string ZeroArityMember() => "method";
}

    // All attempts at resolving members should fail here.
    public class AlmostNothing
{
}


// For testing params, with and without ref/out, with and without ambiguity

public class ParamsTest
{
    public static int StaticParams(int x, params object[] ys)
    {
        return x + ys.Length;
    }

    public static int StaticParams(int x, params string[] ys)
    {
        int count = x;
        foreach (String y in ys)
            count += y.Length;

        return count;
    }

    public int InstanceParams(int x, params object[] ys)
    {
        return x + ys.Length;
    }

    public int InstanceParams(int x, params string[] ys)
    {
        int count = x;
        foreach (String y in ys)
            count += y.Length;

        return count;
    }

    public static int StaticRefWithParams(ref int x, params object[] ys)
    {
        x += ys.Length;
        return ys.Length;
    }


}


/*
* 
* 
* using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace dm.interop
{
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable CA1822 // Mark members as static
public class C1
{
    // Testing property/field/0-arity-method
    public int m1 = 11;

    // Testing static poperty/field/0-arity-method
    static public int m1s = 110;

    // Testing overloads
    public void m2() { Console.WriteLine("m2()"); }
    public void m2(int v) { Console.WriteLine("m2(int) => {0}",v); }
    public void m2(double v) { Console.WriteLine("m2(double) => {0}", v); }
    public void m2(object v) { Console.WriteLine("m2(object) => {0}", v); }
    public void m2(string format, object arg0) { Console.WriteLine(format, arg0); }
    public void m2(string format, object arg0, object arg1) { Console.WriteLine(format, arg0, arg1); }
    public void m2(string format, params object[] args) { Console.WriteLine(format, args); }

    // Testing ref/out resolving
    public int m3(int x) { return x+10; }
    public int m3(ref int x) { x += 1; return x+20; }


    // Testing non-resolving of simple arg
    public int m4(ref int v) { return v + 1; }

    // Testing some ambiguity
    public string m5(string x, ref int y) { y += 10;  return x + y.ToString(); }
    public int m5(int x, ref int y) { y += 100; return x+y; }

    // Testing ambiguity in the ref
    public string m6(ref int x) { x += 111; return x.ToString(); }
    public string m6(ref string x) { x += "abc"; return x; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
    public void m7(string format, params object[] args) { Console.WriteLine("Count is {0}", args.Length); }
}

public class C2
{
    public void m2(string format, object arg0) { Console.WriteLine(format + "!!!!!", arg0); }

    public int m1 { get { return 21; } }
    static public int m1s { get { return 210; } }
}

public class C3
{
    public int m1() { return 31; }
    static public int m1s() { return 310; }
}

// All attempts at resolving members should fail here.
public class C4
{
}

// For playing with c-tors
public class C5
{
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0052 // Remove unread private members
    string _msg;
    object _data;
#pragma warning restore IDE0052 // Remove unread private members
#pragma warning restore IDE0044 // Add readonly modifier

    public override string ToString()
    {
        return String.Format("Constructed with {0}", _msg);
    }

    public C5()
    {
        _msg = "Default c-tor";
        _data = null;
    }

    public C5(int x)
    {
        _msg = "Int32 c-ctor";
        _data = x;
    }

    public C5(string x)
    {
        _msg = "String c-ctor";
        _data = x;
    }

    public C5(ref int x)
    {
        _msg = "Int32-by-ref c-ctor";
        _data = x;
        x += 1;
    }

    // Trying for some ambiguity in calls with ref parameters
    public C5(string x, ref int y)
    {
        _msg = "String+int-by-ref c-tor";
        _data = x;
        y += 20;
    }

    // Trying for some ambiguity in calls with ref parameters
    public C5(int x, ref int y)
    {
        _msg = "int+int-by-ref c-tor";
        _data = x;
        y += 30;
    }

}

// For testing params, with and without ref/out, with and without ambiguity

public class C6
{
    public static int sm1(int x, params object[] ys)
    {
        return x + ys.Length;
    }

    public static int sm1(int x, params string[] ys)
    {
        int count = x;
        foreach (String y in ys)
            count += y.Length;

        return count;
    }

    public int m1(int x, params object[] ys)
    {
        return x + ys.Length;
    }

    public int m1(int x, params string[] ys)
    {
        int count = x;
        foreach (String y in ys)
            count += y.Length;

        return count;
    }

    public static int m2(ref int x, params object[] ys)
    {
        x += ys.Length;
        return ys.Length;
    }


}

public class ParentClass
{
    public static string Create()
    {
        return "Parent create, no args";
    }

    public static string Create(string input)
    {
        return String.Format("Parent create, arg = {0}", input);
    }
}

public class DerivedClass : ParentClass
{
    public new static string Create()
    {
        return "Derived create, no args";
    }

    public new static string Create(string input)
    {
        return String.Format("Derived create, arg = {0}", input);
    }

    public static string Create2()
    {
        return "Derived create2, no args";
    }

    public static string Create2(string input)
    {
        return String.Format("Derived create2, arg = {0}", input);
    }

}

#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore CA1822 // Mark members as static
}

* 
*/