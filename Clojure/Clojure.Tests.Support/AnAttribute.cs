using System;

namespace Clojure.Tests.Support;

[AttributeUsage(AttributeTargets.All)]
public class AnAttribute : Attribute
{
    public string SecondaryValue { get; set; }
    public string PrimaryValue { get; private set; }


    public AnAttribute(string primaryValue)
    {
        PrimaryValue = primaryValue;
    }
}
