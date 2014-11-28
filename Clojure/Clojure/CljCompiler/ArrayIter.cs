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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang
{
    public static class ArrayIter
    {

        static IEnumerable<T> ArrayEnumerable<T>(T[] array, int start)
        {
            if (array == null || array.Length == 0 )
                yield break;
            else
            {
                for ( int i=start; i<array.Length; i++)
                    yield return array[i];
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "create")]
        public static IEnumerator<Object> create(params Object[] items )
        {
            return ArrayEnumerable<object>(items,0).GetEnumerator();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "create")]
        public static IEnumerator createFromObject(object array)
        {
            if (array == null)
                return ArrayEnumerable<Object>(null,0).GetEnumerator();

            Type eType = array.GetType().GetElementType();
            
            switch (Type.GetTypeCode(eType)) {

                case TypeCode.Int16:  return ArrayEnumerable<Int16>((Int16[])array,0).GetEnumerator();
                case TypeCode.Int32:  return ArrayEnumerable<Int32>((Int32[])array,0).GetEnumerator();
                case TypeCode.Int64:  return ArrayEnumerable<Int64>((Int64[])array,0).GetEnumerator();
               case TypeCode.UInt16:  return ArrayEnumerable<UInt16>((UInt16[])array,0).GetEnumerator();
               case TypeCode.UInt32:  return ArrayEnumerable<UInt32>((UInt32[])array,0).GetEnumerator();
               case TypeCode.UInt64:  return ArrayEnumerable<UInt64>((UInt64[])array,0).GetEnumerator();
               case TypeCode.Single:  return ArrayEnumerable<Single>((Single[])array,0).GetEnumerator();
               case TypeCode.Double:  return ArrayEnumerable<Double>((Double[])array,0).GetEnumerator();
               case TypeCode.Byte:  return ArrayEnumerable<Byte>((Byte[])array,0).GetEnumerator();
               case TypeCode.SByte:  return ArrayEnumerable<SByte>((SByte[])array,0).GetEnumerator();
               case TypeCode.Decimal:  return ArrayEnumerable<Decimal>((Decimal[])array,0).GetEnumerator();
               case TypeCode.Char:  return ArrayEnumerable<Char>((Char[])array,0).GetEnumerator();
               case TypeCode.Boolean:  return ArrayEnumerable<Boolean>((Boolean[])array,0).GetEnumerator();
                case TypeCode.Object: return ArrayEnumerable<Object>((Object[])array,0).GetEnumerator();
                default: 
                    return ((Array)array).GetEnumerator();
            }

        }
    }
}
