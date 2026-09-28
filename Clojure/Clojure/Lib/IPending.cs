using System;


namespace clojure.lang
{
    public interface IPending
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        bool isRealized();
    }
}
