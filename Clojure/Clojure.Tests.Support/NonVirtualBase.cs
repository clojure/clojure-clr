namespace Clojure.Tests.Support;

public class NonVirtualBase
{
    public int Plain(int x)
    {
        return x * 10;
    }
}

public interface IPlain
{
    int Plain(int x);  // Same signature as PlainNonVirtual.Plain
}

