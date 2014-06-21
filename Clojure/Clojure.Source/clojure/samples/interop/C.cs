using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace dm.interop
{
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
        public int m3(ref int x) { x = x + 1; return x+20; }


        // Testing non-resolving of simple arg
        public int m4(ref int v) { return v + 1; }

        // Testing some ambiguity
        public string m5(string x, ref int y) { y = y + 10;  return x + y.ToString(); }
        public int m5(int x, ref int y) { y = y + 100; return x+y; }

        // Testing ambiguity in the ref
        public string m6(ref int x) { x += 111; return x.ToString(); }
        public string m6(ref string x) { x += "abc"; return x; }

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
        string _msg;
        object _data;

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
            x = x + 1;
        }

        // Trying for some ambiguity in calls with ref parameters
        public C5(string x, ref int y)
        {
            _msg = "String+int-by-ref c-tor";
            _data = x;
            y = y + 20;
        }

        // Trying for some ambiguity in calls with ref parameters
        public C5(int x, ref int y)
        {
            _msg = "int+int-by-ref c-tor";
            _data = x;
            y = y + 30;
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


}
