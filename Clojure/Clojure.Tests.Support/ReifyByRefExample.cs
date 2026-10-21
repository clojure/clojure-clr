namespace Clojure.Tests.Support;

public static class ReifyByRefExampleClass
{
    public static int AddOne(ref int x) { x += 1; return x; }

    public static int AddOneLong(ref long x) { x += 1; return (int)x; }
}

public interface ReifyByRefExampleInterface
{
    int Bump(ref int x);
}