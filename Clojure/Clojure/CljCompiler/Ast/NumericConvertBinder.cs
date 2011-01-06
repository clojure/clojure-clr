using System;
using System.Collections.Generic;
using System.Linq;
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Utils;
using Microsoft.Scripting.Generation;
using System.Dynamic;
using Microsoft.Scripting.Runtime;

namespace clojure.lang.CljCompiler.Ast
{
    public class NumericConvertBinder : DefaultBinder
    {
        // Inspired by the Converter class in IronRuby
        // Inspired == close to ripped-off
        public static NumericConvertBinder Instance = new NumericConvertBinder();


        public override bool CanConvertFrom(Type fromType, Type toType, bool toNotNullable, NarrowingLevel level)
        {
            //
            // NarrowLevel.Zero
            //

            if (fromType == toType)
                return true;

            //  I don't want to consider boxing before numeric conversion, else I don't get the convert-int-to-long  behavior required to select Numbers.lt(long,long) over Numbers.lt(long,Object)
            //  We also need to get the narrow-long-to-int behavior required to avoid casting in host expression calls.
            //  IronRuby does this here:
            //if (toType.IsAssignableFrom(fromType))
            //{
            //    return true;
            //}
            if ( !Util.IsPrimitiveNumeric(fromType) && toType.IsAssignableFrom(fromType))
            {
                return true;
            }


            //
            // NarrowingLevel.One
            //

            if (level < NarrowingLevel.One)
            {
                return false;
            }

            if (WideningIntegerConversion(fromType, toType))
            {
                return true;
            }
            

            //
            // NarrowingLevel.Two
            //

            if (level < NarrowingLevel.Two)
            {
                return false;
            }

            if ( SpecialClojureConversion(fromType, toType) )
            {
                return true;
            }
            
 
            //if (fromType == typeof(char) && toType == typeof(string))
            //{
            //    return true;
            //}

            if (toType == typeof(bool))
            {
                return true;
            }

            //
            // NarrowingLevel.Three
            //

            if (level < NarrowingLevel.Three)
            {
                return false;
            }

            if ( Util.IsNumeric(toType) && Util.IsNumeric(fromType) )
            {
                return true;
            }

            //
            // NarrowingLevel.All
            //

            if (level < NarrowingLevel.All)
            {
                return false;
            }

            // pick up boxing numerics here
            if (toType.IsAssignableFrom(fromType))
            {
                return true;
            }

            return false;

            //if (level == NarrowingLevel.All)
            //{
            //    if (fromType == typeof(long))
            //    {
            //        if (toType == typeof(int) || toType == typeof(uint) || toType == typeof(short) || toType == typeof(ushort) || toType == typeof(byte) || toType == typeof(sbyte))
            //            return true;
            //    }
            //    else if (fromType == typeof(double))
            //    {
            //        if (toType == typeof(float))
            //            return true;
            //    }
            //}

            //return base.CanConvertFrom(fromType, toType, toNotNullable, level);
        }

        // From IronRuby.  We'll see if it works for our purpose.
        public override Candidate PreferConvert(Type t1, Type t2)
        {
            switch (Type.GetTypeCode(t1)) {
                case TypeCode.SByte:
                    switch (Type.GetTypeCode(t2)) {
                        case TypeCode.Byte:
                        case TypeCode.UInt16:
                        case TypeCode.UInt32:
                        case TypeCode.UInt64:
                            return Candidate.Two;
                        default:
                            return Candidate.Equivalent;
                    }

                case TypeCode.Int16:
                    switch (Type.GetTypeCode(t2)) {
                        case TypeCode.UInt16:
                        case TypeCode.UInt32:
                        case TypeCode.UInt64:
                            return Candidate.Two;
                        default:
                            return Candidate.Equivalent;
                    }

                case TypeCode.Int32:
                    switch (Type.GetTypeCode(t2)) {
                        case TypeCode.UInt32:
                        case TypeCode.UInt64:
                            return Candidate.Two;
                        default:
                            return Candidate.Equivalent;
                    }

                case TypeCode.Int64:
                    switch (Type.GetTypeCode(t2)) {
                        case TypeCode.UInt64:
                            return Candidate.Two;
                        default:
                            return Candidate.Equivalent;
                    }

                case TypeCode.Boolean:
                    if (t2 == typeof(int)) {
                        return Candidate.Two;
                    }
                    return Candidate.Equivalent;

                case TypeCode.Decimal:
                case TypeCode.Double:
                    if (t2 == typeof(BigInteger)) {
                        return Candidate.Two;
                    }
                    return Candidate.Equivalent;

                case TypeCode.Char:
                    if (t2 == typeof(string)) {
                        return Candidate.Two;
                    }
                    return Candidate.Equivalent;
            }
            return Candidate.Equivalent;
        } 



        public override object Convert(object obj, Type toType)
        {
            if (obj is long)
            {
                long lobj = (long)obj;

                if (toType == typeof(long))
                    return obj;
                else if (toType == typeof(int))
                    return (int)lobj;
                else if (toType == typeof(uint))
                    return (uint)lobj;
                else if (toType == typeof(short))
                    return (uint)lobj;
                else if (toType == typeof(byte))
                    return (byte)lobj;
                else if (toType == typeof(sbyte))
                    return (sbyte)lobj;
            }
            else if (obj is double)
            {
                double d = (double)obj;
                if (toType == typeof(float))
                    return (float)d;
            }

            return base.Convert(obj, toType);
        }


        #region Numeric conversion calculations


        internal bool WideningIntegerConversion(Type fromType, Type toType)
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

                switch( Type.GetTypeCode(toType))
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


        #endregion

    }

    public class NumericConvertOverloadResolverFactory : OverloadResolverFactory
    {
        public static NumericConvertOverloadResolverFactory Instance = new NumericConvertOverloadResolverFactory(NumericConvertBinder.Instance);

        private readonly DefaultBinder _binder;

        public NumericConvertOverloadResolverFactory(DefaultBinder binder)
        {
            Assert.NotNull(binder);
            _binder = binder;
        }

        public override DefaultOverloadResolver CreateOverloadResolver(IList<DynamicMetaObject> args, CallSignature signature, CallTypes callType)
        {
            return new DefaultOverloadResolver(_binder, args, signature, callType);
        }
    }
}
