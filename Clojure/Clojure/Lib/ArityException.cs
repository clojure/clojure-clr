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
    public class ArityException : ArgumentException
    {
        #region Data

        readonly int _actual;

        public int Actual
        {
            get { return _actual; }
        }

        readonly string _name;

        public string Name
        {
            get { return _name; }
        } 

        #endregion

        #region C-tors

        public ArityException(int actual, string name)
            : this(actual,name,null)
        {
        }

        public ArityException(int actual, string name, Exception cause)
            : base("Wrong number of args (" + actual + ") passed to: " + name, cause)
        {
            _actual = actual;
            _name = name;
        }

        #endregion
    }
}
