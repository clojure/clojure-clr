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
using System.ComponentModel;
using System.Reflection;
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace clojure.lang.Runtime
{
    public static class Converter
    {
        #region Conversion entry points

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        internal static bool CanConvertFrom(Type fromType, Type toType, NarrowingLevel level)
        {
            ContractUtils.RequiresNotNull(fromType, "fromType");
            ContractUtils.RequiresNotNull(toType, "toType");

            // NarrowingLevel.Zero

            if (fromType == toType)
                return true;

            //  I don't want to consider boxing before numeric conversion, else I don't get the convert-int-to-long  behavior required to select Numbers.lt(long,long) over Numbers.lt(long,Object)
            //  We also need to get the narrow-long-to-int behavior required to avoid casting in host expression calls.
            //  IronRuby and IronPython both do this here: 
            //if (toType.IsAssignableFrom(fromType))
            //{
            //    return true;
            //}

            // Because long[] and ulong[] are inter-assignable, we run into problems.
            // Let's just not convert from an array of one primitive type to another.

            if (fromType.IsArray && toType.IsArray && (Util.IsPrimitiveNumeric(fromType.GetElementType()) || Util.IsPrimitiveNumeric(toType.GetElementType())))
            {
                return false;
            }

            if (!Util.IsPrimitiveNumeric(fromType) && toType.IsAssignableFrom(fromType))
            {
                return true;
            }

            if (fromType.IsCOMObject && toType.IsInterface)
                return true; // A COM object could be cast to any interface

            if (HasImplicitNumericConversion(fromType, toType))
                return true;

            // try available type conversions...
            object[] tcas = toType.GetCustomAttributes(typeof(TypeConverterAttribute), true);
            foreach (TypeConverterAttribute tca in tcas)
            {
                TypeConverter tc = GetTypeConverter(tca);

                if (tc == null) continue;

                if (tc.CanConvertFrom(fromType))
                {
                    return true;
                }
            }

            //!!!do user-defined implicit conversions here

            if (level == NarrowingLevel.None)
                return false;


            // NarrowingLevel.One

            if (WideningIntegerConversion(fromType, toType))
            {
                return true;
            }


            if (level == NarrowingLevel.One)
                return false;


            // NarrowingLevel.Two

            if (SpecialClojureConversion(fromType, toType))
            {
                return true;
            }

            if (DelegateType.IsAssignableFrom(toType) && typeof(IFn).IsAssignableFrom(fromType))
                return true;


            if (level == NarrowingLevel.Two)
                return false;


            // NarrowingLevel.Three

            if (toType == typeof(bool))
            {
                return true;
            }


            if (Util.IsPrimitiveNumeric(toType) && Util.IsPrimitiveNumeric(fromType))
            {
                return true;
            }

            // Handle conversions of IEnumerable<Object> or IEnumerable to IEnumerable<T> for any T
            // Similar to code in IPy's IronPython.Runtime.Converter.HasNarrowingConversion
            if (toType.IsGenericType)
            {
                Type genTo = toType.GetGenericTypeDefinition();
                if (genTo == typeof(IEnumerable<>))
                    return typeof(IEnumerable<Object>).IsAssignableFrom(fromType) || typeof(IEnumerable).IsAssignableFrom(fromType);
            }

            if (level == NarrowingLevel.Three)
                return false;


            // NarrowingLevel.All

            if (level < NarrowingLevel.All)
            {
                return false;
            }

            // pick up boxing numerics here
            if (toType.IsAssignableFrom(fromType))
            {
                return true;
            }

            // TODO: Rethink.  IPy has the following, but we get overload problems on Numbers ops
            //return HasNarrowingConversion(fromType, toType, level);

            // Handle conversions of IEnumerable<Object> or IEnumerable to IEnumerable<T> for any T
            // Similar to code in IPy's IronPython.Runtime.Converter.HasNarrowingConversion
            if (toType.IsGenericType)
            {
                Type genTo = toType.GetGenericTypeDefinition();
                if (genTo == typeof(IList<>) )
                {
                    return typeof(IList<object>).IsAssignableFrom(fromType);
                }
                else if (genTo == typeof(Nullable<>))
                {
                    if (fromType == typeof(DynamicNull) || CanConvertFrom(fromType, toType.GetGenericArguments()[0], level))
                    {
                        return true;
                    }
                }
                else if (genTo == typeof(IDictionary<,>) )
                {
                    return typeof(IDictionary<object,object>).IsAssignableFrom(fromType);
                }
            }

            return false;
        }

        // Ripped off from IPy
        private static TypeConverter GetTypeConverter(TypeConverterAttribute tca)
        {
            try
            {
                ConstructorInfo ci = Type.GetType(tca.ConverterTypeName).GetConstructor(Type.EmptyTypes);
                if (ci != null) return ci.Invoke(ArrayUtils.EmptyObjects) as TypeConverter;
            }
            catch (TargetInvocationException)
            {
            }
            return null;
        }

        #endregion

        #region Numeric conversion calculations

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        internal static bool WideningIntegerConversion(Type fromType, Type toType)
        {
            TypeCode fromTC = Type.GetTypeCode(fromType);
            TypeCode toTC = Type.GetTypeCode(toType);
            bool toBigInt = toType == typeof(BigInt) || toType == typeof(BigInteger);

            switch (fromTC)
            {
                case TypeCode.Char:
                    switch (toTC)
                    {
                        case TypeCode.Char:
                        case TypeCode.UInt16:
                        case TypeCode.UInt32:
                        case TypeCode.UInt64:
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                            return true;
                        case TypeCode.Object:
                            return toBigInt;
                        default:
                            return false;
                    }

                case TypeCode.Byte:
                    switch (toTC)
                    {
                        case TypeCode.Byte:
                        case TypeCode.UInt16:
                        case TypeCode.UInt32:
                        case TypeCode.UInt64:
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                            return true;
                        case TypeCode.Object:
                            return toBigInt;
                        default:
                            return false;
                    }

                case TypeCode.UInt16:
                    switch (toTC)
                    {
                        case TypeCode.UInt16:
                        case TypeCode.UInt32:
                        case TypeCode.UInt64:
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                            return true;
                        case TypeCode.Object:
                            return toBigInt;
                        default:
                            return false;
                    }

                case TypeCode.UInt32:
                    switch (toTC)
                    {
                        case TypeCode.UInt32:
                        case TypeCode.UInt64:
                        case TypeCode.Int64:
                            return true;
                        case TypeCode.Object:
                            return toBigInt;
                        default:
                            return false;
                    }

                case TypeCode.UInt64:
                    switch (toTC)
                    {
                        case TypeCode.UInt64:
                            return true;
                        case TypeCode.Object:
                            return toBigInt;
                        default:
                            return false;
                    }

                case TypeCode.SByte:
                    switch (toTC)
                    {
                        case TypeCode.SByte:
                        case TypeCode.Int16:
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                            return true;
                        case TypeCode.Object:
                            return toBigInt;
                        default:
                            return false;
                    }


                case TypeCode.Int16:
                    switch (toTC)
                    {
                        case TypeCode.Int16:
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                            return true;
                        case TypeCode.Object:
                            return toBigInt;
                        default:
                            return false;
                    }

                case TypeCode.Int32:
                    switch (toTC)
                    {
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                            return true;
                        case TypeCode.Object:
                            return toBigInt;
                        default:
                            return false;
                    }

                case TypeCode.Int64:
                    switch (toTC)
                    {
                        case TypeCode.Int64:
                            return true;
                        case TypeCode.Object:
                            return toBigInt;
                        default:
                            return false;
                    }

                case TypeCode.Object:
                    if (fromType == typeof(BigInt) || fromType == typeof(BigInteger))
                        return toBigInt;
                    else
                        return false;

                default:
                    return false;
            }

        }

        static bool SpecialClojureConversion(Type fromType, Type toType)
        {
            if (fromType == typeof(long))
            {
                if (toType.IsEnum)
                    return false;  // this can be handled later.

                switch (Type.GetTypeCode(toType))
                {
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.Char:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        return true;
                    case TypeCode.Object:
                        return toType == typeof(BigInt) || toType == typeof(BigInteger);
                    default:
                        return false;
                }
            }
            else if (fromType == typeof(double))
            {
                if (toType == typeof(float))
                    return true;
            }

            return false;
        }

        #region Cached Type instances
 
        private static readonly Type BigIntegerType = typeof(BigInteger);
        private static readonly Type BigDecimalType = typeof(BigDecimal);
        private static readonly Type BigIntType = typeof(BigInt);
        private static readonly Type DelegateType = typeof(Delegate);

        #endregion


        // Modified from IPy code
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private static bool HasImplicitNumericConversion(Type fromType, Type toType)
        {
            if (fromType.IsEnum)
                return false;

            if (fromType == typeof(bool))
            {
                if (toType == typeof(int)) return true;
                return HasImplicitNumericConversion(typeof(int), toType);
            }

            switch (Type.GetTypeCode(fromType))
            {
                case TypeCode.SByte:
                    switch (Type.GetTypeCode(toType))
                    {
                        case TypeCode.Int16:
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                        default:
                            if (toType == BigIntegerType) return true;
                            if (toType == BigIntType) return true;
                            if (toType == BigDecimalType) return true;
                            return false;
                    }
                case TypeCode.Byte:
                    switch (Type.GetTypeCode(toType))
                    {
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                        default:
                            if (toType == BigIntegerType) return true;
                            if (toType == BigIntType) return true;
                            if (toType == BigDecimalType) return true;
                            return false;
                    }
                case TypeCode.Int16:
                    switch (Type.GetTypeCode(toType))
                    {
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                        default:
                            if (toType == BigIntegerType) return true;
                            if (toType == BigIntType) return true;
                            if (toType == BigDecimalType) return true;
                            return false;
                    }
                case TypeCode.UInt16:
                    switch (Type.GetTypeCode(toType))
                    {
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                        default:
                            if (toType == BigIntegerType) return true;
                            if (toType == BigIntType) return true;
                            if (toType == BigDecimalType) return true;
                            return false;
                    }
                case TypeCode.Int32:
                    switch (Type.GetTypeCode(toType))
                    {
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                        default:
                            if (toType == BigIntegerType) return true;
                            if (toType == BigIntType) return true;
                            if (toType == BigDecimalType) return true;
                            return false;
                    }
                case TypeCode.UInt32:
                    switch (Type.GetTypeCode(toType))
                    {
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                        default:
                            if (toType == BigIntegerType) return true;
                            if (toType == BigIntType) return true;
                            if (toType == BigDecimalType) return true;
                            return false;
                    }
                case TypeCode.Int64:
                    switch (Type.GetTypeCode(toType))
                    {
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                        default:
                            if (toType == BigIntegerType) return true;
                            if (toType == BigIntType) return true;
                            if (toType == BigDecimalType) return true;
                            return false;
                    }
                case TypeCode.UInt64:
                    switch (Type.GetTypeCode(toType))
                    {
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                        default:
                            if (toType == BigIntegerType) return true;
                            if (toType == BigIntType) return true;
                            if (toType == BigDecimalType) return true;
                            return false;
                    }
                case TypeCode.Char:
                    switch (Type.GetTypeCode(toType))
                    {
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                        default:
                            if (toType == BigIntegerType) return true;
                            if (toType == BigIntType) return true;
                            if (toType == BigDecimalType) return true;
                            return false;
                    }
                case TypeCode.Single:
                    switch (Type.GetTypeCode(toType))
                    {
                        case TypeCode.Double:
                            return true;
                        default:
                            if (toType == BigDecimalType) return true;
                            return false;
                    }
                case TypeCode.Double:
                    switch (Type.GetTypeCode(toType))
                    {
                        default:
                            if (toType == BigDecimalType) return true;
                            return false;
                    }
                default:
                    return false;
            }
        }


//        //private static bool HasNarrowingConversion(Type fromType, Type toType, NarrowingLevel level)
//        //{
//        //    if (level == NarrowingLevel.Three)
//        //    {
//        //        if (toType == CharType && fromType == StringType) return true;
//        //        if (toType == StringType && fromType == CharType) return true;

//        //        //Check if there is an implicit convertor defined on fromType to toType
//        //        if (HasImplicitConversion(fromType, toType))
//        //        {
//        //            return true;
//        //        }
//        //    }

//        //    if (toType == DoubleType && fromType == DecimalType) return true;
//        //    if (toType == SingleType && fromType == DecimalType) return true;

//        //    //if (toType.IsArray)
//        //    //{
//        //    //    return typeof(PythonTuple).IsAssignableFrom(fromType);
//        //    //}

//        //    if (level == NarrowingLevel.Three)
//        //    {
//        //        if (IsNumeric(fromType) && IsNumeric(toType))
//        //        {
//        //            if (fromType != typeof(float) && fromType != typeof(double) && fromType != typeof(decimal))
//        //            {
//        //                return true;
//        //            }
//        //        }
//        //        if (fromType == typeof(bool) && IsNumeric(toType)) return true;

//        //        if (toType == CharType && fromType == StringType) return true;
//        //        if (toType == Int32Type && fromType == BooleanType) return true;

//        //        // Everything can convert to Boolean in Python
//        //        if (toType == BooleanType) return true;

//        //        // TODO: Figure out Clojure equivalent
//        //        //if (DelegateType.IsAssignableFrom(toType) && IsPythonType(fromType)) return true;
//        //        //if (IEnumerableType == toType && IsPythonType(fromType)) return true;

//        //        //if (toType == typeof(IEnumerator))
//        //        //{
//        //        //    if (IsPythonType(fromType)) return true;
//        //        //}
//        //        //else if (toType.IsGenericType)
//        //        //{
//        //        //    Type genTo = toType.GetGenericTypeDefinition();
//        //        //    if (genTo == IEnumerableOfTType)
//        //        //    {
//        //        //        return IEnumerableOfObjectType.IsAssignableFrom(fromType) ||
//        //        //            IEnumerableType.IsAssignableFrom(fromType) ||
//        //        //            fromType == typeof(OldInstance);
//        //        //    }
//        //        //    else if (genTo == typeof(System.Collections.Generic.IEnumerator<>))
//        //        //    {
//        //        //        if (IsPythonType(fromType)) return true;
//        //        //    }
//        //        //}
//        //    }

//        //    if (level == NarrowingLevel.All)
//        //    {
//        //        //__int__, __float__, __long__
//        //        if (IsNumeric(fromType) && IsNumeric(toType)) return true;
//        //   }

//        //    if (toType.IsGenericType)
//        //    {
//        //        Type genTo = toType.GetGenericTypeDefinition();
//        //        if (genTo == IListOfTType)
//        //        {
//        //            return IListOfObjectType.IsAssignableFrom(fromType);
//        //        }
//        //        else if (genTo == NullableOfTType)
//        //        {
//        //            if (fromType == typeof(DynamicNull) || CanConvertFrom(fromType, toType.GetGenericArguments()[0], level))
//        //            {
//        //                return true;
//        //            }
//        //        }
//        //        else if (genTo == IDictOfTType)
//        //        {
//        //            return IDictionaryOfObjectType.IsAssignableFrom(fromType);
//        //        }
//        //    }

//        //    if (fromType == BigIntegerType && toType == Int64Type) return true;
//        //    if (fromType == BigIntType && toType == Int64Type) return true;
//        //    if (toType.IsEnum && fromType == Enum.GetUnderlyingType(toType)) return true;

//        //    return false;
//        //}

//        // TODO: Merge with equivalent in clojure.lang.Util
//        internal static bool IsNumeric(Type t)
//        {
//            if (t.IsEnum) return false;

//            switch (Type.GetTypeCode(t))
//            {
//                case TypeCode.DateTime:
//                case TypeCode.DBNull:
//                case TypeCode.Char:
//                case TypeCode.Empty:
//                case TypeCode.String:
//                case TypeCode.Boolean:
//                    return false;
//                case TypeCode.Object:
//                    return t == BigIntType || t == BigIntegerType || t == BigDecimalType;
//                default:
//                    return true;
//            }
//        }

//        // ripped off from IPy
//        private static bool HasImplicitConversion(Type fromType, Type toType)
//        {
//            return
//                HasImplicitConversionWorker(fromType, fromType, toType) ||
//                HasImplicitConversionWorker(toType, fromType, toType);
//        }


//        // ripped off from IPy
//        private static bool HasImplicitConversionWorker(Type lookupType, Type fromType, Type toType)
//        {
//            while (lookupType != null)
//            {
//                foreach (MethodInfo method in lookupType.GetMethods())
//                {
//                    if (method.Name == "op_Implicit" &&
//                        method.GetParameters()[0].ParameterType.IsAssignableFrom(fromType) &&
//                        toType.IsAssignableFrom(method.ReturnType))
//                    {
//                        return true;
//                    }
//                }
//                lookupType = lookupType.BaseType;
//            }
//            return false;
        //        }


        #endregion

        #region Delegate creation

        // TODO:  Cache created delegates
        public static object ConvertToDelegate(object value, Type to)
        {
            IFn fn = value as IFn;
            if (fn == null) 
                return null;

            return GenDelegate.Create(to, fn);
        }

        #endregion
    }
}
