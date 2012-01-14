/**
 * Copyright (c) Rich Hickey. All rights reserved.
 * The use and distribution terms for this software are covered by the
 * Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 * which can be found in the file epl-v10.html at the root of this distribution.
 * By using this software in any fashion, you are agreeing to be bound by
 * the terms of this license.
 * You must not remove this notice, or any other, from this software.
 */

/**
 *   Author: David Miller
 **/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang
{
    /// <summary>
    /// Exception that carries data (a map) as additional payload.
    /// </summary>
    /// <remarks> Clojure programs that need
    /// richer semantics for exceptions should use this in lieu of defining project-specific
    /// exception classes.</remarks>
    public class ExceptionInfo : Exception
    {
        protected readonly IPersistentMap data;

        public IPersistentMap getData()
        {
            return data;
        }

        public ExceptionInfo(String s, IPersistentMap data)
            : base(s)
        {
            this.data = data;
        }

        public ExceptionInfo(String s, IPersistentMap data, Exception innerException)
            : base(s, innerException)
        {
            this.data = data;
        }

        public override string ToString()
        {

            return "clojure.lang.ExceptionInfo: " + Message + " " + data.ToString();

        }
    }
}