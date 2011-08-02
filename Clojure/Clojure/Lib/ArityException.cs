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
    public sealed class ArityException : ArgumentException
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
            : this(actual, name, null)
        {
        }

        public ArityException(int actual, string name, Exception cause)
            : base("Wrong number of args (" + actual + ") passed to: " + name, cause)
        {
            _actual = actual;
            _name = name;
        }

        public ArityException()
        {
            _actual = -1;
            _name = "<Unknown>";
        }

        public ArityException(string msg)
            : base(msg)
        {
            _actual = -1;
            _name = "<Unknown>";
        }

        public ArityException(string msg, Exception innerException)
            : base(msg, innerException)
        {
            _actual = -1;
            _name = "<Unknown>";
        }

        private ArityException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _actual = info.GetInt32("Actual");
            _name = info.GetString("Name");
        }

        [System.Security.SecurityCritical]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            base.GetObjectData(info, context);
            info.AddValue("Actual", this._actual, typeof(int));
            info.AddValue("Name", this._name, typeof(int));
        }

        #endregion
    }
}
