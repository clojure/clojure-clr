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
    public sealed class TypeNotFoundException : Exception
    {
        #region Data

        public string TypeName { get; set; }

        #endregion

        #region C-tors

        private static string CreateMessage(string typeName)
        {
            return "Unable to find type: " + typeName;
        }

        public TypeNotFoundException()
        {
            TypeName = "?";
        }

        public TypeNotFoundException(string typename)
            : base(CreateMessage(typename))
        {
            TypeName = typename;
        }

        public TypeNotFoundException(string typename, Exception innerException)
            : base(CreateMessage(typename), innerException)
        {
            TypeName = typename;
        }

        private TypeNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            if (info == null)
                throw new ArgumentNullException("info");

            TypeName = info.GetString("TypeName");

        }

        #endregion

        #region Support

        public override string ToString()
        {
            return Message;
        }


        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new System.ArgumentNullException("info");
            base.GetObjectData(info, context);
            info.AddValue("TypeName", TypeName);
        }

        #endregion
    }
}