using System;

namespace Clojure.Tests.Support;

[AttributeUsage(AttributeTargets.All)]
public class AnotherAttribute : Attribute
{
    public long PrimaryValue { get; private set; }


    public AnotherAttribute(long primaryValue)
    {
        PrimaryValue = primaryValue;
    }
}