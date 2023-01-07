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
using System.Runtime.Serialization;

namespace clojure.lang
{
    [Serializable]
    public class ClojureException : Exception
    {
        public ClojureException()
        {
        }

        public ClojureException(string msg)
            : base(msg)
        {
        }

        public ClojureException(string msg, Exception innerException)
            : base(msg, innerException)
        {
        }

        protected ClojureException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
