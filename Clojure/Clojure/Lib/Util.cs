/**
 *   Copyright (c) David Miller. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BigDecimal = java.math.BigDecimal;

namespace clojure.lang
{
    public class Util
    {

        static public int Hash(object o)
        {
            return o == null ? 0 : o.GetHashCode();
        }

        static public int HashCombine(int seed, int hash)
        {
            //a la boost
            return (int)(seed ^ (hash + 0x9e3779b9 + (seed << 6) + (seed >> 2)));

        }


        static public bool equiv(object k1, object k2)
        {
            if (k1 == k2)
                return true;
            if (k1 != null)
            {
                if (IsNumeric(k1))
                    return Numbers.equiv(k1, k2);
                else if (k1 is IPersistentCollection && k2 is IPersistentCollection)
                    return ((IPersistentCollection)k1).equiv(k2);
                return k1.Equals(k2);
            }
            return false;
        }

        public static bool equals(object k1, object k2)
        {
            // Changed in Rev 1215
            //if(k1 == k2)
            //    return true;
	
            //if(k1 != null)
            //{
            //    if (IsNumeric(k1))
            //        return Numbers.equiv(k1, k2);

            //    return k1.Equals(k2);
            //}
	    
            //return false;
            if (k1 == k2)
                return true;
            return k1 != null && k1.Equals(k2);
        }

        public static int compare(object k1, object k2)
        {
            if (k1 == k2)
                return 0;
            if (k1 != null)
            {
                if (k2 == null)
                    return 1;
                if (IsNumeric(k1))
                    return Numbers.compare(k1, k2);
                return ((IComparable)k1).CompareTo(k2);
            }
            return -1;
        }


        public static int ConvertToInt(object o)
        {
            // ToInt32 rounds.  We need truncation.
            return (int)Convert.ToDouble(o);
        }

        public static long ConvertToLong(object o)
        {
            // ToInt64 rounds.  We need truncation.
            return (long)Convert.ToDouble(o);
        }

        public static float ConvertToFloat(object o)
        {
            return (float)Convert.ToDouble(o);
        }

        public static double ConvertToDouble(object o)
        {
            return ConvertToDouble(o);
        }

        public static bool IsNumeric(object o)
        {
            return o != null && IsNumeric(o.GetType());
        }


        public static int BitCount(int x)
        { 
            x -= ((x >> 1) & 0x55555555);
            x = (((x >> 2) & 0x33333333) + (x & 0x33333333));
            x = (((x >> 4) + x) & 0x0f0f0f0f);
            return ((x * 0x01010101) >> 24);
        }

        // A variant of the above that avoids multiplying
        // This algo is in a lot of places.
        // See, for example, http://aggregate.org/MAGIC/#Population%20Count%20(Ones%20Count)
        public static uint BitCount(uint x)
        {
            x -= ((x >> 1) & 0x55555555);
            x = (((x >> 2) & 0x33333333) + (x & 0x33333333));
            x = (((x >> 4) + x) & 0x0f0f0f0f);
            x += (x >> 8);
            x += (x >> 16);
            return (x & 0x0000003f);
        }

        // This algo is in a lot of places.
        // See, for example, http://aggregate.org/MAGIC/#Leading%20Zero%20Count
        public static uint LeadingZeroCount(uint x)
        {
            x |= (x >> 1);
            x |= (x >> 2);
            x |= (x >> 4);
            x |= (x >> 8);
            x |= (x >> 16);

            return  32u - BitCount(x);

            // THE DLR BigInteger code uses the following.
            // It's probably faster.

            //int shift = 0;

            //if ((value & 0xFFFF0000) == 0) { value <<= 16; shift += 16; }
            //if ((value & 0xFF000000) == 0) { value <<= 8; shift += 8; }
            //if ((value & 0xF0000000) == 0) { value <<= 4; shift += 4; }
            //if ((value & 0xC0000000) == 0) { value <<= 2; shift += 2; }
            //if ((value & 0x80000000) == 0) { value <<= 1; shift += 1; }

            //return shift;
        }

        public static int Mask(int hash, int shift)
        {
        	return (hash >> shift) & 0x01f;
        }


        public static bool IsPrimitive(Type t)
        {
            return t != null && t.IsPrimitive && t != typeof(void);
        }

        #region core.clj compatibility

        public static int hash(object o)
        {
            return Hash(o);
        }

        #endregion

        #region Stolen code
        // The following code is from Microsoft's DLR..
        // It had the following notice:

        /* ****************************************************************************
         *
         * Copyright (c) Microsoft Corporation. 
         *
         * This source code is subject to terms and conditions of the Microsoft Public License. A 
         * copy of the license can be found in the License.html file at the root of this distribution. If 
         * you cannot locate the  Microsoft Public License, please send an email to 
         * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
         * by the terms of the Microsoft Public License.
         *
         * You must not remove this notice, or any other, from this software.
         *
         *
         * ***************************************************************************/



        internal static Type GetNonNullableType(Type type)
        {
            if (IsNullableType(type))
            {
                return type.GetGenericArguments()[0];
            }
            return type;
        }

        internal static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }


        internal static bool IsNumeric(Type type)
        {
            type = GetNonNullableType(type);
            if (!type.IsEnum)
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Char:
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.Double:
                    case TypeCode.Single:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        return true;
                }
                if (type == typeof(BigInteger) || type == typeof(BigDecimal) || type == typeof(Ratio))
                    return true;
            }
            return false;
        }

        #endregion


        internal static Exception UnreachableCode()
        {
            return new InvalidOperationException("Invalid value in switch: default should not be reached.");
        }
    }
}
