namespace Clojure.Tests.Support;

// Provide in by-ref parameters on a virtual, an abstract and an interface method.

public interface IAllIn
{
    public int InterfaceMethod(ref int x);
}

public abstract class AllIn : IAllIn
{
    public virtual int VirtualMethod(ref int x)
    {
        x++;
        return x;
    }

    public abstract int AbstractMethod(ref int x);

    public int InterfaceMethod(ref int x)
    {
        x++;
        return x;
    }
}
