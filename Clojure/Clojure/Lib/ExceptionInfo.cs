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
using System.Runtime.Serialization;

namespace clojure.lang
{
    /// <summary>
    /// Exception that carries data (a map) as additional payload.
    /// </summary>
    /// <remarks> Clojure programs that need
    /// richer semantics for exceptions should use this in lieu of defining project-specific
    /// exception classes.</remarks>
    [Serializable]
    public class ExceptionInfo : Exception, IExceptionInfo
    {
        #region Data

        protected readonly IPersistentMap data;


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "get")]
        public IPersistentMap getData()
        {
            return data;
        }

        #endregion

        #region C-tors

        public ExceptionInfo(String s, IPersistentMap data)
            : this(s,data,null)
        {
        }

        public ExceptionInfo(String s, IPersistentMap data, Exception innerException)
            : base(s, innerException)
        {
            if (data != null)
                this.data = data;
            else
                throw new ArgumentException("Additional data must be non-nil.", "data");
        }

        public ExceptionInfo()
            : base()
        {
            this.data = PersistentHashMap.EMPTY;
        }

        public ExceptionInfo(string message)
            : base(message)
        {
            this.data = PersistentHashMap.EMPTY;
        }

        public ExceptionInfo(string message, Exception innerException)
            : base(message, innerException)
        {
            this.data = PersistentHashMap.EMPTY;
        }

        protected ExceptionInfo(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.data = (IPersistentMap)info.GetValue("data", typeof(IPersistentMap));
        }

        [System.Security.SecurityCritical]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            base.GetObjectData(info, context);
            info.AddValue("data", this.data, typeof(IPersistentMap));
        }


        #endregion

        #region Object methods

        public override string ToString()
        {
            return "clojure.lang.ExceptionInfo: " + Message + " " + data.ToString();
        }

        #endregion
    }
}