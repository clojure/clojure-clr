namespace Clojure.Tests.Support;

public class GenericsTest
{
    // Zero-arg generic instance method
    public T InstanceMethod0<T>() { return default; }

    // Zero-arg static method
    public static T StaticMethod0<T>() { return default; }

}
