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
using System.Globalization;
using System.Collections;
//using BigDecimal = java.math.BigDecimal;

namespace clojure.lang
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces")]
    public static class Util
    {

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "hash")]
        static public int hash(object o)
        {
            return o == null ? 0 : o.GetHashCode();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "hasheq")]
        public static int hasheq(object o)
        {
            if (o == null)
                return 0;

            IHashEq ihe = o as IHashEq;
            if (ihe != null)
                return dohasheq(ihe);

            if (Util.IsNumeric(o))
                return Numbers.hasheq(o);

            String s = o as string;
            if (s != null)
                return Murmur3.HashInt(s.GetHashCode());

            return o.GetHashCode();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "dohasheq")]
        private static int dohasheq(IHashEq ihe)
        {
            return ihe.hasheq();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "hash")]
        static public int hashCombine(int seed, int hash)
        {
            //a la boost
            return (int)(seed ^ (hash + 0x9e3779b9 + (seed << 6) + (seed >> 2)));

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "equiv")]
        static public bool equiv(object k1, object k2)
        {
            if (k1 == k2)
                return true;
            if (k1 != null)
            {
                if (IsNumeric(k1) && IsNumeric(k2))
                    return Numbers.equal(k1, k2);

                else if (k1 is IPersistentCollection || k2 is IPersistentCollection)
                    return pcequiv(k1, k2);
                return k1.Equals(k2);
            }
            return false;
        }

        public delegate bool EquivPred(object k1, object k2);

        static EquivPred _equivNull = (k1, k2) => { return k2 == null; };
        static EquivPred _equivEquals = (k1, k2) => { return k1.Equals(k2); };
        static EquivPred _equivNumber = (k1, k2) =>
        {
            if (IsNumeric(k2))
                return Numbers.equal(k1, k2);
            return false;
        };
        static EquivPred _equivColl = (k1, k2) =>
            {
                if (k1 is IPersistentCollection || k2 is IPersistentCollection)
                    return pcequiv(k1, k2);
                return k1.Equals(k2);
            };

        public static EquivPred GetEquivPred(object k1)
        {
            if (k1 == null)
                return _equivNull;
            else if (IsNumeric(k1))
                return _equivNumber;
            else if (k1 is string || k1 is Symbol)
                return _equivEquals;
            else if (k1 is ICollection || k1 is IDictionary)
                return _equivColl;
            return _equivEquals;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "equiv")]
        public static bool equiv(long x, long y)
        {
            return x == y;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "equiv")]
        public static bool equiv(double x, double y)
        {
            return x == y;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "equiv")]
        public static bool equiv(long x, object y)
        {
            return equiv((object)x, y);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "equiv")]
        public static bool equiv(object x, long y)
        {
            return equiv(x, (object)y);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "equiv")]
        public static bool equiv(double x, Object y)
        {
            return equiv((object)x, y);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "equiv")]
        public static bool equiv(object x, double y)
        {
            return equiv(x, (object)y);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "equiv")]
        public static bool equiv(bool x, bool y)
        {
            return x == y;
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "equiv")]
        public static bool equiv(object x, bool y)
        {
            return equiv(x, (object)y);
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "equiv")]
        public static bool equiv(bool x, object y)
        {
            return equiv((object)x, y);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "equiv")]
        public static bool equiv(char x, char y)
        {
            return x == y;
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "pcequiv")]
        public static bool pcequiv(object k1, object k2)
        {
            IPersistentCollection ipc1 = k1 as IPersistentCollection;

            if (ipc1 != null)
                return ipc1.equiv(k2);
            return ((IPersistentCollection)k2).equiv(k1);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "equals")]
        public static bool equals(object k1, object k2)
        {
            // Had to change this back when doing the new == vs = 
            // Changed in Rev 1215
            //if (k1 == k2)
            //    return true;

            //if (k1 != null)
            //{
            //    if (IsNumeric(k1) && IsNumeric(k2))
            //        return Numbers.equiv(k1, k2);

            //    return k1.Equals(k2);
            //}

            //return false;
            if (k1 == k2)
                return true;
            return k1 != null && k1.Equals(k2);
        }

        //public static bool equals(long x, long y)
        //{
        //    return x == y;
        //}

        //public static bool equals(double x, double y)
        //{
        //    return x == y;
        //}

        //public static bool equals(long x, object y)
        //{
        //    return equals(Numbers.num(x), y);
        //}

        //public static bool equals(object x, long y)
        //{
        //    return equals(x, Numbers.num(y));
        //}

        //public static bool equals(double x, object y)
        //{
        //    return equals(Numbers.num(x), y);
        //}

        //public static bool equals(object x, double y)
        //{
        //    return equals(x, Numbers.num(y));
        //}

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "identical")]
        public static bool identical(object k1, object k2)
        {
            // I would prefer simpler version below, but it can't handle simple true/false (boxed booleans)

            if (k1 is ValueType)
                return k1.Equals(k2);
            else
                return k1 == k2;

            //return k1 == k2;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "class")]
        public static Type classOf(object x)
        {
            if (x != null)
                return x.GetType();
            return null;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "compare")]
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "nil")]
        public static object Ret1(object ret, object nil)
        {
            return ret;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "nil")]
        public static ISeq Ret1(ISeq ret, object nil)
        {
            return ret;
        }


        public static int ConvertToInt(object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                    return (int)(Byte)o;
                case TypeCode.Char:
                    return (int)(Char)o;
                case TypeCode.Decimal:
                    return (int)(decimal)o;
                case TypeCode.Double:
                    return (int)(double)o;
                case TypeCode.Int16:
                    return (int)(short)o;
                case TypeCode.Int32:
                    return (int)o;
                case TypeCode.Int64:
                    return (int)(long)o;
                case TypeCode.SByte:
                    return (int)(sbyte)o;
                case TypeCode.Single:
                    return (int)(float)o;
                case TypeCode.UInt16:
                    return (int)(ushort)o;
                case TypeCode.UInt32:
                    return (int)(uint)o;
                case TypeCode.UInt64:
                    return (int)(ulong)o;
                default:
                    return Convert.ToInt32(o, CultureInfo.InvariantCulture);
            }
        }

        public static uint ConvertToUInt(object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                    return (uint)(Byte)o;
                case TypeCode.Char:
                    return (uint)(Char)o;
                case TypeCode.Decimal:
                    return (uint)(decimal)o;
                case TypeCode.Double:
                    return (uint)(double)o;
                case TypeCode.Int16:
                    return (uint)(short)o;
                case TypeCode.Int32:
                    return (uint)(int)o;
                case TypeCode.Int64:
                    return (uint)(long)o;
                case TypeCode.SByte:
                    return (uint)(sbyte)o;
                case TypeCode.Single:
                    return (uint)(float)o;
                case TypeCode.UInt16:
                    return (uint)(ushort)o;
                case TypeCode.UInt32:
                    return (uint)o;
                case TypeCode.UInt64:
                    return (uint)(ulong)o;
                default:
                    return Convert.ToUInt32(o, CultureInfo.InvariantCulture);
            } 
        }

        public static long ConvertToLong(object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                    return (long)(Byte)o;
                case TypeCode.Char:
                    return (long)(Char)o;
                case TypeCode.Decimal:
                    return (long)(decimal)o;
                case TypeCode.Double:
                    return (long)(double)o;
                case TypeCode.Int16:
                    return (long)(short)o;
                case TypeCode.Int32:
                    return (long)(int)o;
                case TypeCode.Int64:
                    return (long)o;
                case TypeCode.SByte:
                    return (long)(sbyte)o;
                case TypeCode.Single:
                    return (long)(float)o;
                case TypeCode.UInt16:
                    return (long)(ushort)o;
                case TypeCode.UInt32:
                    return (long)(uint)o;
                case TypeCode.UInt64:
                    return (long)(ulong)o;
                default:
                    return Convert.ToInt64(o, CultureInfo.InvariantCulture);
            }
        }

        public static ulong ConvertToULong(object o)
        {

            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                    return (ulong)(Byte)o;
                case TypeCode.Char:
                    return (ulong)(Char)o;
                case TypeCode.Decimal:
                    return (ulong)(decimal)o;
                case TypeCode.Double:
                    return (ulong)(double)o;
                case TypeCode.Int16:
                    return (ulong)(short)o;
                case TypeCode.Int32:
                    return (ulong)(int)o;
                case TypeCode.Int64:
                    return (ulong)(long)o;
                case TypeCode.SByte:
                    return (ulong)(sbyte)o;
                case TypeCode.Single:
                    return (ulong)(float)o;
                case TypeCode.UInt16:
                    return (ulong)(ushort)o;
                case TypeCode.UInt32:
                    return (ulong)(uint)o;
                case TypeCode.UInt64:
                    return (ulong)o;
                default:
                    return Convert.ToUInt64(o, CultureInfo.InvariantCulture);
            } 
        }

        //

        public static short ConvertToShort(object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                    return (short)(Byte)o;
                case TypeCode.Char:
                    return (short)(Char)o;
                case TypeCode.Decimal:
                    return (short)(decimal)o;
                case TypeCode.Double:
                    return (short)(double)o;
                case TypeCode.Int16:
                    return (short)o;
                case TypeCode.Int32:
                    return (short)(int)o;
                case TypeCode.Int64:
                    return (short)(long)o;
                case TypeCode.SByte:
                    return (short)(sbyte)o;
                case TypeCode.Single:
                    return (short)(float)o;
                case TypeCode.UInt16:
                    return (short)(ushort)o;
                case TypeCode.UInt32:
                    return (short)(uint)o;
                case TypeCode.UInt64:
                    return (short)(ulong)o;
                default:
                    return Convert.ToInt16(o, CultureInfo.InvariantCulture);
            } 
        }

        public static ushort ConvertToUShort(object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                    return (ushort)(Byte)o;
                case TypeCode.Char:
                    return (ushort)(Char)o;
                case TypeCode.Decimal:
                    return (ushort)(decimal)o;
                case TypeCode.Double:
                    return (ushort)(double)o;
                case TypeCode.Int16:
                    return (ushort)(short)o;
                case TypeCode.Int32:
                    return (ushort)(int)o;
                case TypeCode.Int64:
                    return (ushort)(long)o;
                case TypeCode.SByte:
                    return (ushort)(sbyte)o;
                case TypeCode.Single:
                    return (ushort)(float)o;
                case TypeCode.UInt16:
                    return (ushort)o;
                case TypeCode.UInt32:
                    return (ushort)(uint)o;
                case TypeCode.UInt64:
                    return (ushort)(ulong)o;
                default:
                    return Convert.ToUInt16(o, CultureInfo.InvariantCulture);
            }
        }

        //

        public static sbyte ConvertToSByte(object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                    return (sbyte)(Byte)o;
                case TypeCode.Char:
                    return (sbyte)(Char)o;
                case TypeCode.Decimal:
                    return (sbyte)(decimal)o;
                case TypeCode.Double:
                    return (sbyte)(double)o;
                case TypeCode.Int16:
                    return (sbyte)(short)o;
                case TypeCode.Int32:
                    return (sbyte)(int)o;
                case TypeCode.Int64:
                    return (sbyte)(long)o;
                case TypeCode.SByte:
                    return (sbyte)o;
                case TypeCode.Single:
                    return (sbyte)(float)o;
                case TypeCode.UInt16:
                    return (sbyte)(ushort)o;
                case TypeCode.UInt32:
                    return (sbyte)(uint)o;
                case TypeCode.UInt64:
                    return (sbyte)(ulong)o;
                default:
                    return Convert.ToSByte(o, CultureInfo.InvariantCulture);
            }
        }

        public static byte ConvertToByte(object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                    return (byte)o;
                case TypeCode.Char:
                    return (byte)(Char)o;
                case TypeCode.Decimal:
                    return (byte)(decimal)o;
                case TypeCode.Double:
                    return (byte)(double)o;
                case TypeCode.Int16:
                    return (byte)(short)o;
                case TypeCode.Int32:
                    return (byte)(int)o;
                case TypeCode.Int64:
                    return (byte)(long)o;
                case TypeCode.SByte:
                    return (byte)(sbyte)o;
                case TypeCode.Single:
                    return (byte)(float)o;
                case TypeCode.UInt16:
                    return (byte)(ushort)o;
                case TypeCode.UInt32:
                    return (byte)(uint)o;
                case TypeCode.UInt64:
                    return (byte)(ulong)o;
                default:
                    return Convert.ToByte(o, CultureInfo.InvariantCulture);
            }
        }

        //
        
        public static float ConvertToFloat(object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                    return (float)(byte)o;
                case TypeCode.Char:
                    return (float)(Char)o;
                case TypeCode.Decimal:
                    return (float)(decimal)o;
                case TypeCode.Double:
                    return (float)(double)o;
                case TypeCode.Int16:
                    return (float)(short)o;
                case TypeCode.Int32:
                    return (float)(int)o;
                case TypeCode.Int64:
                    return (float)(long)o;
                case TypeCode.SByte:
                    return (float)(sbyte)o;
                case TypeCode.Single:
                    return (float)o;
                case TypeCode.UInt16:
                    return (float)(ushort)o;
                case TypeCode.UInt32:
                    return (float)(uint)o;
                case TypeCode.UInt64:
                    return (float)(ulong)o;
                default:
                    return Convert.ToSingle(o, CultureInfo.InvariantCulture);
            }
        }

        public static double ConvertToDouble(object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                    return (double)(byte)o;
                case TypeCode.Char:
                    return (double)(Char)o;
                case TypeCode.Decimal:
                    return (double)(decimal)o;
                case TypeCode.Double:
                    return (double)o;
                case TypeCode.Int16:
                    return (double)(short)o;
                case TypeCode.Int32:
                    return (double)(int)o;
                case TypeCode.Int64:
                    return (double)(long)o;
                case TypeCode.SByte:
                    return (double)(sbyte)o;
                case TypeCode.Single:
                    return (double)(float)o;
                case TypeCode.UInt16:
                    return (double)(ushort)o;
                case TypeCode.UInt32:
                    return (double)(uint)o;
                case TypeCode.UInt64:
                    return (double)(ulong)o;
                default:
                    return Convert.ToDouble(o, CultureInfo.InvariantCulture);
            }
        }


        public static decimal ConvertToDecimal(object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                    return (decimal)(byte)o;
                case TypeCode.Char:
                    return (decimal)(Char)o;
                case TypeCode.Decimal:
                    return (decimal)o;
                case TypeCode.Double:
                    return (decimal)(double)o;
                case TypeCode.Int16:
                    return (decimal)(short)o;
                case TypeCode.Int32:
                    return (decimal)(int)o;
                case TypeCode.Int64:
                    return (decimal)(long)o;
                case TypeCode.SByte:
                    return (decimal)(sbyte)o;
                case TypeCode.Single:
                    return (decimal)(float)o;
                case TypeCode.UInt16:
                    return (decimal)(ushort)o;
                case TypeCode.UInt32:
                    return (decimal)(uint)o;
                case TypeCode.UInt64:
                    return (decimal)(ulong)o;
                default:
                    return Convert.ToDecimal(o, CultureInfo.InvariantCulture);
            }
           
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        public static char ConvertToChar(object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                    return (char)(byte)o;
                case TypeCode.Char:
                    return (Char)o;
                case TypeCode.Decimal:
                    return (char)(decimal)o;
                case TypeCode.Double:
                    return (char)(double)o;
                case TypeCode.Int16:
                    return (char)(short)o;
                case TypeCode.Int32:
                    return (char)(int)o;
                case TypeCode.Int64:
                    return (char)(long)o;
                case TypeCode.SByte:
                    return (char)(sbyte)o;
                case TypeCode.Single:
                    return (char)(float)o;
                case TypeCode.UInt16:
                    return (char)(ushort)o;
                case TypeCode.UInt32:
                    return (char)(uint)o;
                case TypeCode.UInt64:
                    return (char)(ulong)o;
                default:
                    return Convert.ToChar(o, CultureInfo.InvariantCulture);
            }
        }


        public static bool IsNumeric(object o)
        {
            return o != null && IsNumeric(o.GetType());
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2233:OperationsShouldNotOverflow", MessageId = "x*16843009")]
        public static int BitCount(int x)
        { 
            x -= ((x >> 1) & 0x55555555);
            x = (((x >> 2) & 0x33333333) + (x & 0x33333333));
            x = (((x >> 4) + x) & 0x0f0f0f0f);
            unchecked
            {
                return ((x * 0x01010101) >> 24);
            }
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
            //return t != null && t.IsPrimitive && t != typeof(void);
            // STRUCT TEST
            return t != null && t.IsValueType && t != typeof(void);
        }


        public static string NameForType(Type t)
        {
            if (t == null)
                Console.WriteLine("Bad type");

            if (!t.IsNested)
                return t.Name;

            // for nested types, we have to work harder
            string fullName = t.FullName;
            int index = fullName.LastIndexOf('.');
            string nameToUse = fullName.Substring(index + 1);
            return nameToUse;
        }


        public static bool IsInteger(object o)
        {
            return o != null && IsIntegerType(o.GetType());

        }

        public static bool IsNonCharNumeric(object o)
        {
            return o != null && !(o is Char) && IsIntegerType(o.GetType());
        }

        internal static bool IsIntegerType(Type type)
        {
            //type = GetNonNullableType(type);
            //if (!type.IsEnum)
            //{
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        return true;
                }
                if (type == typeof(BigInt) || type == typeof(BigInteger))
                    return true;
            //}
            return false;
        }


        // I can hardly claim this is original.
        public static void Shuffle(IList list)
        {
            Random rnd = new Random();
            for (int i = list.Count - 1; i >= 1; i--)
            {
                int j = rnd.Next(i + 1);
                object temp = list[j];
                list[j] = list[i];
                list[i] = temp;
            }
        }


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
            //type = GetNonNullableType(type);
            //if (!type.IsEnum)
            //{
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
                if (type == typeof(BigInt) || type == typeof(BigInteger) || type == typeof(BigDecimal) || type == typeof(Ratio))
                    return true;
            //}
            return false;
        }

        internal static bool IsPrimitiveNumeric(Type type)
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
            return false;
        }

        #endregion


        internal static Exception UnreachableCode()
        {
            return new InvalidOperationException("Invalid value in switch: default should not be reached.");
        }
    }
}
