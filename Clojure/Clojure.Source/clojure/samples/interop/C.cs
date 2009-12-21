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
        public void m2() { Console.WriteLine(); }
        public void m2(int v) { Console.WriteLine(v); }
        public void m2(double v) { Console.WriteLine(v); }
        public void m2(object v) { Console.WriteLine(v); }
        public void m2(string format, object arg0) { Console.WriteLine(format, arg0); }
        public void m2(string format, object arg0, object arg1) { Console.WriteLine(format, arg0, arg1); }
        public void m2(string format, params object[] args) { Console.WriteLine(format, args); }

        // Testing ref/out resolving
        public int m3(int x) { return x; }
        public int m3(ref int x) { x = x + 1; return x; }


        // Testing non-resolving of simple arg
        public int m4(ref int v) { return v + 1; }



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

}
