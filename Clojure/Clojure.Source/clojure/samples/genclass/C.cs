using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace dm
{
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable CA1822 // mark as static
    public class C1
    {
        protected int x;
        protected string y = String.Empty;


        public void m1(int x) { Message("m1", x.ToString()); }


        public int m2(int x, string y) { Message("m2", x.ToString(), y); return x + y.Length; }
        public int m2(int x) { Message("m2", x.ToString()); return 2 * x; }


        protected int m3(object y) { Message("m3", y.ToString()); return y.GetHashCode(); }
        protected int m3(int x) { Message("m3", x.ToString()); return x + 1; }

        protected int m4(int x) { Message("m4", x.ToString()); return x + 1; }

        private int m5(int x) { Message("m5", x.ToString()); return x + 1; }

        public C1(int x, string y) { Message("ctor1", x.ToString(), y); this.x = x; this.y = y; }
        protected C1(string y, int x) { Message("ctor2", y, x.ToString()); this.y = y; this.x = x; }
        public C1() { Message("defaultctor"); }

        private static void Message(string name, params string[] strs)
        {
            Console.WriteLine("In {0}: {1}", name, string.Join(", ", strs));
        }
    }

    public interface I1
    {
        object m5(object x);
        int m2(string x);
        int m2(int x);
    }

#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore CA1822 // mark as static
}
