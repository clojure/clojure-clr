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

namespace clojure.lang
{
    // This is essentially identical to DLR's System.Runtime.CompilerServices.Closure.
    // Because it does the exact same thing.

    [Serializable]
    public sealed class Closure
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
        public readonly object[] Constants;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
        public readonly object[] Locals;

        public Closure(object[] constants, object[] locals)
        {
            Constants = constants;
            Locals = locals;
        }
    }



    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Fn")]
    public interface IFnClosure
    {
        Closure GetClosure();
        void SetClosure(Closure closure);
    }
}
