namespace Clojure.Tests.Support;

// Provide in by-ref parameters on a virtual, an abstract and an interface method.

public interface IAllIn
{
    public int InterfaceMethod(in int x);
}

public abstract class AllIn : IAllIn
{
    public virtual int VirtualMethod(in int x)
    {
        return x + 1;
    }

    public abstract int AbstractMethod(in int x);

    public virtual int InterfaceMethod(in int x)
    {
        return x + 1;
    }
}
