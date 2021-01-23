/**
 *   Copyright (c) Rich Hickey. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

/**
 *   Author: David Miller
 **/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using clojure.lang;

namespace Clojure
{
    public static class CljMain
    {
        private static readonly Symbol CLOJURE_MAIN = Symbol.intern("clojure.main");
        private static readonly Var REQUIRE = RT.var("clojure.core", "require");
        private static readonly Var LEGACY_REPL = RT.var("clojure.main", "legacy-repl");
        private static readonly Var LEGACY_SCRIPT = RT.var("clojure.main", "legacy-script");
        private static readonly Var MAIN = RT.var("clojure.main", "main");

        static void Main(string[] args)
        {
            RT.Init();
            REQUIRE.invoke(CLOJURE_MAIN);
            MAIN.applyTo(RT.seq(args));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static void legacy_repl(string[] args)
        {
            RT.Init();
            REQUIRE.invoke(CLOJURE_MAIN);
            LEGACY_REPL.invoke(RT.seq(args));

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static void legacy_script(string[] args)
        {
            RT.Init();
            REQUIRE.invoke(CLOJURE_MAIN);
            LEGACY_SCRIPT.invoke(RT.seq(args));
        }


    }
}
