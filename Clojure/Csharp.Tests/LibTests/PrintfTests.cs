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

using NUnit.Framework;
using static NExpect.Expectations;
using clojure.lang;
using NExpect;

namespace Clojure.Tests.LibTests
{
    [TestFixture]
    public class PrintfTests
    {
        void Test(string result, string fmt, params object[] args)
        {
            Expect(Printf.Format(fmt,args)).To.Equal(result);
        }

        [SetUp]
        public void SetUpCultureAware()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        }

        #region Basic text

        [Test]
        public void WorksOnEmptyString()
        {
            Test("","");
        }

        [Test]
        public void WorksOnStringWithNoFormats()
        {
            Test("abc","abc");
        }

        [Test]
        public void WorksOnStringWithPercentSpec()
        {
           Test("abc%def%","abc%%def%%");
        }

        [Test]
        public void WorksOnStringWithOnlyPercents()
        {
            Test("%%%", "%%%%%%");
        }

        [Test]
        [ExpectedException(typeof(UnknownFormatConversionException))]
        public void FailsOnStringWithOddNumberOfPercents()
        {
            Printf.Format("%%%%%");
        }

        #endregion

        #region  Argument specificiers

        [Test]
        public void WorksWithBasicArgument()
        {
            Test("abc12def","abc%ddef",12);
        }


        [Test]
        public void WorksWithMoreThanOneSpecifier()
        {
            Test("abc12def14ghi", "abc%ddef%dghi", 12, 14);
        }

        [Test]
        [ExpectedException(typeof(MissingFormatArgumentException))]
        public void DetectsInsufficientNumberOfArgs()
        {
            Printf.Format("abc%ddef%dghi%d", 12, 14);
        }

        [Test]
        public void RepeatIndexWorks()
        {
            Test("abc12def12ghi13", "abc%ddef%<dghi%d", 12, 13, 14);
        }

        [Test]
        public void SimpleArgIndexingWorks()
        {
            Test("abc14def", "abc%3$ddef", 12, 13, 14);
        }

        [Test]
        public void ArgIndexingWorks()
        {
            Test("abc14def12ghi13", "abc%3$ddef%1$dghi%2$d", 12, 13, 14);
        }

        [Test]
        public void SucceedsOnZeroArgIndexThoughIDontKnowWhyJavaDoes()
        {
            Test("abc12def","abc%0$ddef", 12);
        }

        [Test]
        [ExpectedException(typeof(UnknownFormatConversionException))]
        public void FailsNegativeArgIndex()
        {
            Printf.Format("abc%-1$ddef", 12);
        }

        [Test]
        [ExpectedException(typeof(MissingFormatArgumentException))]
        public void FailsOnArgIndexTooLarge()
        {
            Printf.Format("abc%2$ddef", 12);
        }

        #endregion

        #region General conversions

        // Conversions: b B s S h H

        [Test]
        [ExpectedException(typeof(FormatFlagsConversionMismatchException))]
        public void BoolSpecWithAltFlagFails()
        {
            Printf.Format("%#b", "abc");
        }

        [Test]
        [ExpectedException(typeof(FormatFlagsConversionMismatchException))]
        public void HashSpecWithAltFlagFails()
        {
            Printf.Format("%#h", "abc");
        }

        [Test]
        [ExpectedException(typeof(FormatFlagsConversionMismatchException))]
        public void GeneralSpecWithPlusFlagFails()
        {
            Printf.Format("%+s", "abc");
        }

        [Test]
        [ExpectedException(typeof(FormatFlagsConversionMismatchException))]
        public void GeneralSpecWithLeadingSpaceFlagFails()
        {
            Printf.Format("% s", "abc");

        }

        [Test]
        [ExpectedException(typeof(FormatFlagsConversionMismatchException))]
        public void GeneralSpecWithZeroPadFlagFails()
        {
            Printf.Format("%0s", "abc");
        }

        [Test]
        [ExpectedException(typeof(MissingFormatWidthException))]
        public void GeneralSpecWithBadLeftJustifyWidthFails()
        {
            Printf.Format("%-s", "abc");
        }

        [Test]
        public void BooleanSpecPrintsNullOnNull()
        {
            Test("False", "%b", null);  // Spec error: false.ToString() == "False", spec says do this but expects "false"
            Test("FALSE", "%B", null);
        }

        [Test]
        public void BooleanSpecPrintsFalseOnFalse()
        {
            Test("False", "%b", false);  // Spec error: false.ToString() == "False", spec says do this but expects "false"
            Test("FALSE", "%B", false);
        }

        [Test]
        public void BooleanSpecPrintsTrueOnTrue()
        {
            Test("True", "%b", true);  // Spec error: true.ToString() == "True", spec says do this but expects "true"
            Test("TRUE", "%B", true);
        }

        [Test]
        public void BooleanSpecPrintsFalseOnOtherArg()
        {
            Test("True", "%b", 12);  // Spec error: true.ToString() == "True", spec says do this but expects "true"
            Test("TRUE", "%B", 12);
            Test("True", "%b", "abc");
        }

        [Test]
        public void BooleanSpecPrintUsingWidth()
        {
            Test("     True", "%9b", 12);
            Test("    False", "%9b", false);
        }

        [Test]
        public void BooleanSpecPrintUsingLeftJustifyWidth()
        {
            Test("True     ", "%-9b", 12);
            Test("False    ", "%-9b", false);
        }

        [Test]
        public void HashSpecPrintHashCode()
        {
            Test(Convert.ToString("abcde".GetHashCode(), 16), "%h", "abcde");
            Test(Convert.ToString("abcde".GetHashCode(), 16).ToUpper(), "%H", "abcde");
        }

        [Test]
        public void HashSpecPrintsNullOnNull()
        {
            Test("null", "%h", null);  
            Test("NULL", "%H", null);
        }

        [Test]
        public void HashSpecPrintsUsingWidth()
        {
            Test(Convert.ToString("abcde".GetHashCode(), 16).PadLeft(20), "%20h", "abcde");
        }

        [Test]
        public void HashSpecPrintsUsingLeftJustify()
        {
            Test(Convert.ToString("abcde".GetHashCode(), 16).PadRight(20), "%-20h", "abcde");
        }


        [Test]
        public void StringSpecPrintsString()
        {
            Test("abcde", "%s", "abcde");
            Test("ABCDE", "%S", "abcde");
        }

        [Test]
        public void StringSpecPrintsNullOnNull()
        {
            Test("null", "%s", null);
            Test("NULL", "%S", null);
        }

        [Test]
        public void StringSpecPrintsUsingWidth()
        {
            Test("       abcde", "%12s", "abcde");
        }

        [Test]
        public void StringSpecPrintsUsingLeftJustify()
        {
            Test("abcde       ", "%-12s", "abcde");
        }

         //TODO: For String spec, test other arg types
        // TODO: For String spec, test IFormattable

        #endregion

        #region Integer tests

        [Test]
        [ExpectedException(typeof(MissingFormatWidthException))]
        public void IntSpecLeftJustifyWithNoWidthFails()
        {
            Printf.Format("%-d", 12);
        }

        [Test]
        [ExpectedException(typeof(MissingFormatWidthException))]
        public void IntSpecZeroPadWithNoWidthFails()
        {
            Printf.Format("%0d", 12);
        }

        [Test]
        [ExpectedException(typeof(IllegalFormatFlagsException))]
        public void IntSpecZeroPadWithLeftJustfyFails()
        {
            Printf.Format("%-012d", 12);
        }

        [Test]
        [ExpectedException(typeof(IllegalFormatFlagsException))]
        public void IntSpecPlusWithLeadingSpaceFails()
        {
            Printf.Format("%+ d", 12);
        }

        [Test]
        [ExpectedException(typeof(IllegalFormatPrecisionException))]
        public void IntSpecWithPrecionFails()
        {
            Printf.Format("%12.2d", 12);
        }


        [Test]
        [ExpectedException(typeof(FormatFlagsConversionMismatchException))]
        public void IntSpecWithAltFlagFails()
        {
            Printf.Format("%#d", 12);
        }

        [Test]
        [ExpectedException(typeof(FormatFlagsConversionMismatchException))]
        public void OctIntSpecWithParenFlagFails()
        {
            Printf.Format("%(o", 12);
        }

        [Test]
        [ExpectedException(typeof(FormatFlagsConversionMismatchException))]
        public void HexIntSpecWithParenFlagFails()
        {
            Printf.Format("%(x", 12);
        }

        [Test]
        public void IntSpecPrintsNullOnNull()
        {
            Test("null", "%d", null);
            Test("null", "%o", null);
            Test("null", "%x", null);
            Test("NULL", "%X", null);
        }

        [Test]
        public void IntSpecPrintsAllTypes()
        {
            Test("12", "%d", (byte)12);
            Test("12", "%d", (ushort)12);
            Test("12", "%d", (uint)12);
            Test("12", "%d", (ulong)12);
            Test("12", "%d", (sbyte)12);
            Test("12", "%d", (short)12);
            Test("12", "%d", (int)12);
            Test("12", "%d", (long)12);
            Test("-12", "%d", (sbyte)-12);
            Test("-12", "%d", (short)-12);
            Test("-12", "%d", (int)-12);
            Test("-12", "%d", (long)-12);
            Test("999999999999999999999999", "%d", BigInteger.Parse("999999999999999999999999"));
        }

        [Test]
        public void OctIntSpecPrintsAllTypes()
        {
            Test("14", "%o", (byte)12);
            Test("14", "%o", (ushort)12);
            Test("14", "%o", (uint)12);
            Test("14", "%o", (ulong)12);
            Test("14", "%o", (sbyte)12);
            Test("14", "%o", (short)12);
            Test("14", "%o", (int)12);
            Test("14", "%o", (long)12);
            Test("364", "%o", (sbyte)-12);
            Test("177764", "%o", (short)-12);
            Test("37777777764", "%o", (int)-12);
            Test("1777777777777777777764", "%o", (long)-12);
            Test("77777777777777777777777777", "%o", BigInteger.Parse("77777777777777777777777777", 8));
        }


        [Test]
        public void HexIntSpecPrintsAllTypes()
        {
            Test("c", "%x", (byte)12);
            Test("c", "%x", (ushort)12);
            Test("c", "%x", (uint)12);
            Test("c", "%x", (ulong)12);
            Test("c", "%x", (sbyte)12);
            Test("c", "%x", (short)12);
            Test("c", "%x", (int)12);
            Test("c", "%x", (long)12);
            Test("f4", "%x", (sbyte)-12);
            Test("fff4", "%x", (short)-12);
            Test("fffffff4", "%x", (int)-12);
            Test("fffffffffffffff4", "%x", (long)-12);
            Test("FFEEDDCCBBAA998877665544332211", "%x", BigInteger.Parse("FFEEDDCCBBAA998877665544332211", 16));

        }

        [Test]
        public void IntSpecPrintsNegVals()
        {
            Test("-12", "%d", (sbyte)-12);
            Test("-12", "%d", (short)-12);
            Test("-12", "%d", (int)-12);
            Test("-12", "%d", (long)-12);
        }

        [Test]
        public void IntSpecGroups()
        {
            Test("12,345,678", "%,d", 12345678);
            Test("-12,345,678", "%,d", -12345678);
            Test("12", "%,d", 12);
            Test("-12", "%,d", -12);
            Test("123,456", "%,d", 123456);
            Test("-123,456", "%,d", -123456);
        }

        [Test]
        public void IntSpecIncludesSign()
        {
            Test("+12345678", "%+d", 12345678);
            Test("-12345678", "%+d", -12345678);
        }

        [Test]
        public void IntSpecIncludesLeadingSpace()
        {
            Test(" 12345678", "% d", 12345678);
            Test("-12345678", "% d", -12345678);
        }

        [Test]
        public void IntSpecZeroPads()
        {
            Test("000012345678", "%012d", 12345678);
            Test("-00012345678", "%012d", -12345678);
        }

        [Test]
        public void IntSpecDoesNegParens()
        {
            Test("12345678", "%(d", 12345678);
            Test("(12345678)", "%(d", -12345678);
        }

        [Test]
        public void IntSpecDoesWidthAndJustify()
        {
            Test("      123456", "%12d", 123456);
            Test("     -123456", "%12d", -123456);
            Test("123456      ", "%-12d", 123456);
            Test("-123456     ", "%-12d", -123456);
            Test("      123456", "%(12d", 123456);
            Test("    (123456)", "%(12d", -123456);
            Test("123456      ", "%(-12d", 123456);
            Test("(123456)    ", "%(-12d", -123456);
            Test("000000123456", "%012d", 123456);
            Test("-00000123456", "%012d", -123456);
            Test("000000123456", "%(012d", 123456);
            Test("(0000123456)", "%(012d", -123456);
            Test("     +123456", "%+12d", 123456);
            Test("+123456     ", "%-+12d", 123456);
            Test("     123,456", "%,12d", 123456);
            Test("    -123,456", "%,12d", -123456);
            Test("123,456     ", "%-,12d", 123456);
            Test("-123,456    ", "%-,12d", -123456);
        }

        [Test]
        [ExpectedException(typeof(FormatFlagsConversionMismatchException))]
        public void OctIntSpecWIthParenFlagFails()
        {
            Printf.Format("%(o", 12);
        }

        [Test]
        [ExpectedException(typeof(FormatFlagsConversionMismatchException))]
        public void OctIntSpecWIthLeadingSpaceFlagFails()
        {
            Printf.Format("% o", 12);
        }

        [Test]
        [ExpectedException(typeof(FormatFlagsConversionMismatchException))]
        public void OctIntSpecWIthPlusFlagFails()
        {
            Printf.Format("%+o", 12);
        }



        [Test]
        [ExpectedException(typeof(FormatFlagsConversionMismatchException))]
        public void HexIntSpecWIthParenFlagFails()
        {
            Printf.Format("%(x", 12);
        }

        [Test]
        [ExpectedException(typeof(FormatFlagsConversionMismatchException))]
        public void HexIntSpecWIthLeadingSpaceFlagFails()
        {
            Printf.Format("% x", 12);
        }

        [Test]
        [ExpectedException(typeof(FormatFlagsConversionMismatchException))]
        public void HexIntSpecWIthPlusFlagFails()
        {
            Printf.Format("%+x", 12);
        }

        [Test]
        public void OctIntSpecBasicallyWorks()
        {
            Test("30071","%o",12345);
            Test("37777747707", "%o", -12345);
            Test("          30071", "%15o", 12345);
            Test("    37777747707", "%15o", -12345);
        }

        [Test]
        public void OctIntSpecWithAltPrependsZero()
        {
            Test("030071", "%#o", 12345);
            Test("037777747707", "%#o", -12345);
            Test("         030071", "%#15o", 12345);
            Test("   037777747707", "%#15o", -12345);
        }

        [Test]
        public void OctIntSpecZeroPadding()
        {
            Test("000000000030071", "%015o", 12345);
            Test("000037777747707", "%015o", -12345);
            Test("000000000030071", "%0#15o", 12345);
            Test("000037777747707", "%0#15o", -12345);
        }

        [Test]
        public void HexIntSpecBasicallyWorks()
        {
            Test("303b", "%x", 12347);
            Test("303B", "%X", 12347);
            Test("ffffcfc5", "%x", -12347);
            Test("           303b", "%15x", 12347);
            Test("       ffffcfc5", "%15x", -12347);
        }

        [Test]
        public void HexIntSpecWithAltPrependsZeroX()
        {
            Test("0x303b", "%#x", 12347);
            Test("0X303B", "%#X", 12347);
            Test("0xffffcfc5", "%#x", -12347);
            Test("         0x303b", "%#15x", 12347);
            Test("     0xffffcfc5", "%#15x", -12347);
        }


        [Test]
        public void HexIntSpecZeroPadding()
        {
            Test("00000000000303b", "%015x", 12347);
            Test("0000000ffffcfc5", "%015x", -12347);
            Test("0x000000000303b", "%0#15x", 12347);
            Test("0x00000ffffcfc5", "%0#15x", -12347);
        }


        [Test]
        public void DecIntSpecWithBigIntegerArgumentsWorks()
        {
            Test("123456789","%d",BigInteger.Parse("123456789"));
            Test("-123456789","%d",BigInteger.Parse("-123456789"));
            Test("      123456789","%15d",BigInteger.Parse("123456789"));
            Test("     -123456789","%15d",BigInteger.Parse("-123456789"));
            Test("123456789      ","%-15d",BigInteger.Parse("123456789"));
            Test("-123456789     ","%-15d",BigInteger.Parse("-123456789"));

            Test("     +123456789", "%+15d", BigInteger.Parse("123456789"));
            Test("     -123456789", "%+15d", BigInteger.Parse("-123456789"));

            Test("000000123456789", "%015d", BigInteger.Parse("123456789"));
            Test("-00000123456789", "%015d", BigInteger.Parse("-123456789"));

            // not implemented yet
            //Test("    123,456,789", "%,15d", BigInteger.Parse("123456789"));
            //Test("   -123,456,789", "%,15d", BigInteger.Parse("-123456789"));

            Test("      123456789", "%(15d", BigInteger.Parse("123456789"));
            Test("    (123456789)", "%(15d", BigInteger.Parse("-123456789"));
        }

        [Test]
        public void OctIntSpecWithBigIntegerArgumentsWorks()
        {
            Test("123456777", "%o", BigInteger.Parse("123456777", 8));
            Test("-123456777", "%o", BigInteger.Parse("-123456777", 8));
            Test("      123456777", "%15o", BigInteger.Parse("123456777", 8));
            Test("     -123456777", "%15o", BigInteger.Parse("-123456777", 8));
            Test("123456777      ", "%-15o", BigInteger.Parse("123456777", 8));
            Test("-123456777     ", "%-15o", BigInteger.Parse("-123456777", 8));

            Test("     0123456777", "%#15o", BigInteger.Parse("123456777", 8));
            Test("    -0123456777", "%#15o", BigInteger.Parse("-123456777", 8));

            Test("     +123456777", "%+15o", BigInteger.Parse("123456777", 8));
            Test("     -123456777", "%+15o", BigInteger.Parse("-123456777", 8));

            Test("000000123456777", "%015o", BigInteger.Parse("123456777", 8));
            Test("-00000123456777", "%015o", BigInteger.Parse("-123456777", 8));

            Test("      123456777", "%(15o", BigInteger.Parse("123456777", 8));
            Test("    (123456777)", "%(15o", BigInteger.Parse("-123456777", 8));
        }

        [Test]
        public void HexIntSpecWithBigIntegerArgumentsWorks()
        {
            Test("123456789ABCDEF", "%x", BigInteger.Parse("123456789ABCDEF", 16));
            Test("-123456789ABCDEF", "%x", BigInteger.Parse("-123456789ABCDEF", 16));
            Test("     123456789ABCDEF", "%20x", BigInteger.Parse("123456789ABCDEF", 16));
            Test("    -123456789ABCDEF", "%20x", BigInteger.Parse("-123456789ABCDEF", 16));
            Test("123456789ABCDEF     ", "%-20x", BigInteger.Parse("123456789ABCDEF", 16));
            Test("-123456789ABCDEF    ", "%-20x", BigInteger.Parse("-123456789ABCDEF", 16));

            Test("   0x123456789ABCDEF", "%#20x", BigInteger.Parse("123456789ABCDEF", 16));
            Test("  -0x123456789ABCDEF", "%#20x", BigInteger.Parse("-123456789ABCDEF", 16));

            Test("    +123456789ABCDEF", "%+20x", BigInteger.Parse("123456789ABCDEF", 16));
            Test("    -123456789ABCDEF", "%+20x", BigInteger.Parse("-123456789ABCDEF", 16));

            Test("00000123456789ABCDEF", "%020x", BigInteger.Parse("123456789ABCDEF", 16));
            Test("-0000123456789ABCDEF", "%020x", BigInteger.Parse("-123456789ABCDEF", 16));

            Test("     123456789ABCDEF", "%(20x", BigInteger.Parse("123456789ABCDEF", 16));
            Test("   (123456789ABCDEF)", "%(20x", BigInteger.Parse("-123456789ABCDEF", 16));
        }


        #endregion

        #region Float tests

        [Test]
        [ExpectedException(typeof(MissingFormatWidthException))]
        public void FloatSpecLeftJustifyWithNoWidthFails()
        {
            Printf.Format("%-f", 12.0);
        }

        [Test]
        [ExpectedException(typeof(MissingFormatWidthException))]
        public void FloatSpecZeroPadWithNoWidthFails()
        {
            Printf.Format("%0f", 12.0);
        }

        [Test]
        [ExpectedException(typeof(IllegalFormatFlagsException))]
        public void FloatSpecZeroPadWithLeftJustfyFails()
        {
            Printf.Format("%-012f", 12.0);
        }

        [Test]
        [ExpectedException(typeof(IllegalFormatFlagsException))]
        public void FloatSpecPlusWithLeadingSpaceFails()
        {
            Printf.Format("%+ f", 12.0);
        }


        [Test]
        [ExpectedException(typeof(FormatFlagsConversionMismatchException))]
        public void HexFloatSpecWithParensFlagFails()
        {
            Printf.Format("%(a", 12.0);
        }

        [Test]
        [ExpectedException(typeof(FormatFlagsConversionMismatchException))]
        public void HexFloatSpecWithGroupFlagFails()
        {
            Printf.Format("%,a", 12.0);
        }

        [Test]
        [ExpectedException(typeof(FormatFlagsConversionMismatchException))]
        public void ScientificSpecWithGroupFlagFails()
        {
            Printf.Format("%,e", 12.0);

        }

        [Test]
        [ExpectedException(typeof(FormatFlagsConversionMismatchException))]
        public void GeneralFloatSpecWithAlternateFlagFails()
        {
            Printf.Format("%#g", 12.0);
        }

        [Test]
        public void FloatSpecPrintsNullOnNull()
        {
            Test("null", "%e", null);
            Test("NULL", "%E", null);
            Test("null", "%g", null);
            Test("NULL", "%G", null);
            Test("null", "%a", null);
            Test("NULL", "%A", null);
            Test("null", "%f", null);
        }

        [Test]
        public void FloatSpecPrintsOnNaNandInfinity()
        {
            Test("NaN", "%e", Double.NaN);
            Test("NaN", "%E", Double.NaN);
            Test("NaN", "%g", Double.NaN);
            Test("NaN", "%G", Double.NaN);
            Test("NaN", "%f", Double.NaN);
            Test("Infinity", "%e", Double.PositiveInfinity);
            Test("Infinity", "%f", Double.PositiveInfinity);
            Test("Infinity", "%g", Double.PositiveInfinity);
            Test("-Infinity", "%e", Double.NegativeInfinity);
            Test("-Infinity", "%f", Double.NegativeInfinity);
            Test("-Infinity", "%g", Double.NegativeInfinity);
        }

        [Test]
        public void DecFloatPrintsBasics()
        {
            Test("1234.57", "%.2f", 1234.56789);
            Test("-1234.57", "%.2f", -1234.56789);
            Test("     1234.57", "%12.2f", 1234.56789);
            Test("    -1234.57", "%12.2f", -1234.56789);
            Test("1234.57     ", "%-12.2f", 1234.56789);
            Test("-1234.57    ", "%-12.2f", -1234.56789);
        }

        [Test]
        public void DecFloatPrintsWithFlags()
        {
            Test("    1,234.57", "%,12.2f", 1234.56789);
            Test("     1234.57", "%#12.2f", 1234.56789);
            Test("    +1234.57", "%+12.2f", 1234.56789);
            Test("     1234.57", "% 12.2f", 1234.56789);
            Test("000001234.57", "%012.2f", 1234.56789);
            Test("   (1234.57)", "%(12.2f", -1234.56789);
        }

        [Test]
        public void ScientificFloatPrintsBasics()
        {
            // Do we want to match the e+xx of the Java version?
            Test(" 1.2346e+000","%12.4e",1.23456789e0);
            Test(" 1.2346e+001","%12.4e",1.23456789e1);
            Test(" 1.2346e+002","%12.4e",1.23456789e2);
            Test(" 1.2346e+003","%12.4e",1.23456789e3);
            Test(" 1.2346e+004","%12.4e",1.23456789e4);
            Test(" 1.2346e+005","%12.4e",1.23456789e5);
            Test(" 1.2346e+006","%12.4e",1.23456789e6);
            Test(" 1.2346e+007","%12.4e",1.23456789e7);
            Test(" 1.2346e+008","%12.4e",1.23456789e8);
            Test(" 1.2346e+009","%12.4e",1.23456789e9);
            Test(" 1.2346e+010","%12.4e",1.23456789e10);
            Test(" 1.2346e-001","%12.4e",1.23456789e-1);
            Test(" 1.2346e-002","%12.4e",1.23456789e-2);
            Test(" 1.2346e-003","%12.4e",1.23456789e-3);
            Test(" 1.2346e-004","%12.4e",1.23456789e-4);
            Test(" 1.2346e-005","%12.4e",1.23456789e-5);
            Test(" 1.2346e-006","%12.4e",1.23456789e-6);
            Test(" 1.2346e-007","%12.4e",1.23456789e-7);
            Test(" 1.2346e-008","%12.4e",1.23456789e-8);
            Test(" 1.2346e-009","%12.4e",1.23456789e-9);
            Test(" 1.2346e-010","%12.4e",1.23456789e-10);

            Test("1.2346e+000 ", "%-12.4e", 1.23456789e0);
            Test("1.2346e+001 ", "%-12.4e", 1.23456789e1);
        }

        [Test]
        public void ScientificFloatPrintsWithFlags()
        {
            Test("   1.23e+003", "%#12.2e", 1234.56789);
            Test("  +1.23e+003", "%+12.2e", 1234.56789);
            Test("   1.23e+003", "% 12.2e", 1234.56789);
            Test("0001.23e+003", "%012.2e", 1234.56789);
            Test(" (1.23e+003)", "%(12.2e", -1234.56789);
        }

        [Test]
        public void GeneralFloatPrintsBasics()
        {
            Test("       1.235", "%12.4g", 1.23456789e0);
            Test("       12.35", "%12.4g", 1.23456789e1);
            Test("       123.5", "%12.4g", 1.23456789e2);
            Test("        1235", "%12.4g", 1.23456789e3);
            Test("   1.235e+04", "%12.4g", 1.23456789e4);
            Test("   1.235e+05", "%12.4g", 1.23456789e5);
            Test("   1.235e+06", "%12.4g", 1.23456789e6);
            Test("   1.235e+07", "%12.4g", 1.23456789e7);
            Test("   1.235e+08", "%12.4g", 1.23456789e8);
            Test("   1.235e+09", "%12.4g", 1.23456789e9);
            Test("   1.235e+10", "%12.4g", 1.23456789e10);
            Test("      0.1235", "%12.4g", 1.23456789e-1);
            Test("     0.01235", "%12.4g", 1.23456789e-2);
            Test("    0.001235", "%12.4g", 1.23456789e-3);
            Test("   0.0001235", "%12.4g", 1.23456789e-4);
            Test("   1.235e-05", "%12.4g", 1.23456789e-5);
            Test("   1.235e-06", "%12.4g", 1.23456789e-6);
            Test("   1.235e-07", "%12.4g", 1.23456789e-7);
            Test("   1.235e-08", "%12.4g", 1.23456789e-8);
            Test("   1.235e-09", "%12.4g", 1.23456789e-9);
            Test("   1.235e-10", "%12.4g", 1.23456789e-10);

            Test("1.235       ", "%-12.4g", 1.23456789e0);
            Test("0.1235      ", "%-12.4g", 1.23456789e-1);
            Test("1.235e+07   ", "%-12.4g", 1.23456789e7);
        }

        [Test]
        public void GeneralFloatPrintsWithFlags()
        {
            //Test("       1.235","%,12.4g", 1.23456789e0);
            Test("      +1.235", "%+12.4g", 1.23456789e0);
            Test("       1.235", "% 12.4g", 1.23456789e0);
            Test("00000001.235", "%012.4g", 1.23456789e0);
            Test("     (1.235)", "%(12.4g", -1.23456789e0);

            //Test("       1,235", "%,12.4g", 1.23456789e3);
            Test("       +1235", "%+12.4g", 1.23456789e3);
            Test("        1235", "% 12.4g", 1.23456789e3);
            Test("000000001235", "%012.4g", 1.23456789e3);
            Test("      (1235)", "%(12.4g", -1.23456789e3);

            //Test("    0.001235", "%,12.4g", 1.23456789e-3);
            Test("   +0.001235", "%+12.4g", 1.23456789e-3);
            Test("    0.001235", "% 12.4g", 1.23456789e-3);
            Test("00000.001235", "%012.4g", 1.23456789e-3);
            Test("  (0.001235)", "%(12.4g", -1.23456789e-3);

            //Test("   1.235e+07", "%,12.4g", 1.23456789e7);
            Test("  +1.235e+07", "%+12.4g", 1.23456789e7);
            Test("   1.235e+07", "% 12.4g", 1.23456789e7);
            Test("0001.235e+07", "%012.4g", 1.23456789e7);
            Test(" (1.235e+07)", "%(12.4g", -1.23456789e7);

            //Test("   1.235e-07", "%,12.4g", 1.23456789e-7);
            Test("  +1.235e-07", "%+12.4g", 1.23456789e-7);
            Test("   1.235e-07", "% 12.4g", 1.23456789e-7);
            Test("0001.235e-07", "%012.4g", 1.23456789e-7);
            Test(" (1.235e-07)", "%(12.4g", -1.23456789e-7);


        }

        #endregion

        #region Character tests

        [Test]
        [ExpectedException(typeof(IllegalFormatPrecisionException))]
        public void CharSpecWithPrecionFails()
        {
            Printf.Format("%12.2c", 'a');
        }


        [Test]
        [ExpectedException(typeof(FormatFlagsConversionMismatchException))]
        public void CharSpecWithAltFlagFails()
        {
            Printf.Format("%#c", 'a');
        }

        [Test]
        [ExpectedException(typeof(FormatFlagsConversionMismatchException))]
        public void CharSpecWithPlusFlagFails()
        {
            Printf.Format("%+c", 'a');
        }

        [Test]
        [ExpectedException(typeof(FormatFlagsConversionMismatchException))]
        public void CharSpecWithLeadingSpaceFlagFails()
        {
            Printf.Format("% c", 'a');
        }

        [Test]
        [ExpectedException(typeof(FormatFlagsConversionMismatchException))]
        public void CharSpecWithZeroPadFlagFails()
        {
            Printf.Format("%0c", 'a');
        }

        [Test]
        [ExpectedException(typeof(FormatFlagsConversionMismatchException))]
        public void CharSpecWithGroupFlagFails()
        {
            Printf.Format("%,c", 'a');
        }

        [Test]
        [ExpectedException(typeof(FormatFlagsConversionMismatchException))]
        public void CharSpecWithParenFlagFails()
        {
            Printf.Format("%(c", 'a');
        }


        [Test]
        [ExpectedException(typeof(MissingFormatWidthException))]
        public void CharSpecLeftJustifyWithNoWidthFails()
        {
            Printf.Format("%-c", 'a');
        }

        [Test]
        public void CharSpecPrintsNullOnNull()
        {
            Test("null", "%c", null);
            Test("NULL", "%C", null);
        }

        [Test]
        public void CharSpecBasics()
        {
            Test("a", "%c", 'a');
            Test("    a", "%5c", 'a');
            Test("a    ", "%-5c", 'a');
            Test("\n", "%c", '\n');
            Test("\u005c", "%c", (byte)0x5c);
            Test("\u1bcd", "%c", (short)0x1BCD);
            Test("\uabcd", "%c", (ushort)0xABCD);
            Test("\uabcd", "%c", (int)0xABCD);
            Test("\uabcd", "%c", (uint)0xABCD);
        }

        #endregion

        #region Text tests

        [Test]
        [ExpectedException(typeof(IllegalFormatPrecisionException))]
        public void PercentSpecWithPrecionFails()
        {
            Printf.Format("%5.2%");
        }

        [Test]
        [ExpectedException(typeof(IllegalFormatPrecisionException))]
        public void LineSepSpecLeftJustifyWithNoWidthFails()
        {
            Printf.Format("%5.2n");
        }


        [Test]
        [ExpectedException(typeof(IllegalFormatFlagsException))]
        public void PercentSpecWithAlternateFlagFails()
        {
            Printf.Format("%#%");
        }

        [Test]
        [ExpectedException(typeof(IllegalFormatFlagsException))]
        public void PercentSpecWithLeadingPlusFlagFails()
        {
            Printf.Format("%+%");
        }

        [Test]
        [ExpectedException(typeof(IllegalFormatFlagsException))]
        public void PercentSpecWithLeadingSpaceFlagFails()
        {
            Printf.Format("% %");
        }

        [Test]
        [ExpectedException(typeof(IllegalFormatFlagsException))]
        public void PercentSpecWithZeroPadFlagFails()
        {
            Printf.Format("%0%");
        }

        [Test]
        [ExpectedException(typeof(IllegalFormatFlagsException))]
        public void PercentSpecWithgGroupFlagFails()
        {
            Printf.Format("%,%");
        }

        [Test]
        [ExpectedException(typeof(IllegalFormatFlagsException))]
        public void PercentSpecWithParenFlagFails()
        {
            Printf.Format("%(%");
        }

        [Test]
        [ExpectedException(typeof(IllegalFormatFlagsException))]
        public void LineSepSpecWithAlternateFlagFails()
        {
            Printf.Format("%#n");
        }

        [Test]
        [ExpectedException(typeof(IllegalFormatFlagsException))]
        public void LineSepSpecWithLeadingPlusFlagFails()
        {
            Printf.Format("%+n");
        }

        [Test]
        [ExpectedException(typeof(IllegalFormatFlagsException))]
        public void LineSepSpecWithLeadingSpaceFlagFails()
        {
            Printf.Format("% n");
        }

        [Test]
        [ExpectedException(typeof(IllegalFormatFlagsException))]
        public void LineSepSpecWithZeroPadFlagFails()
        {
            Printf.Format("%0n");
        }

        [Test]
        [ExpectedException(typeof(IllegalFormatFlagsException))]
        public void LineSepSpecWithgGroupFlagFails()
        {
            Printf.Format("%,n");
        }

        [Test]
        [ExpectedException(typeof(IllegalFormatFlagsException))]
        public void LineSepSpecWithParenFlagFails()
        {
            Printf.Format("%(n");
        }

        [Test]
        [ExpectedException(typeof(IllegalFormatWidthException))]
        public void LineSepSpecWithLeftJustifyFlagFails()
        {
            Printf.Format("%-5n");
        }

        [Test]
        public void TextSpecBasics()
        {
            Test("%", "%%");
            Test("  %", "%3%");
            Test("%  ", "%-3%");
            Test("\n", "%n");
        }

        #endregion

        #region DateTime

        [Test]
        public void DateTimeBasics()
        {
            Test("01","%tH", new DateTime(2009, 7, 1, 1, 10, 20));
            Test("14", "%tH", new DateTime(2009, 7, 1, 14, 10, 20));
            Test("01", "%tI", new DateTime(2009, 7, 1, 1, 10, 20));
            Test("02", "%tI", new DateTime(2009, 7, 1, 14, 10, 20));
            Test("1", "%tk", new DateTime(2009, 7, 1, 1, 10, 20));
            Test("14", "%tk", new DateTime(2009, 7, 1, 14, 10, 20));
            Test("1", "%tl", new DateTime(2009, 7, 1, 1, 10, 20));
            Test("2", "%tl", new DateTime(2009, 7, 1, 14, 10, 20));
            Test("02", "%tM", new DateTime(2009, 7, 1, 14, 02, 03));
            Test("44", "%tM", new DateTime(2009, 7, 1, 14, 44, 50));
            Test("03", "%tS", new DateTime(2009, 7, 1, 14, 02, 03));
            Test("50", "%tS", new DateTime(2009, 7, 1, 14, 44, 50));
            Test("050", "%tL", new DateTime(2009, 7, 1, 14, 02, 03).AddMilliseconds(50));
            Test("000027300", "%tN", new DateTime(2009, 7, 1, 14, 20, 03).AddTicks(273));
            Test("AM", "%tp", new DateTime(2009, 7, 1, 1, 10, 20));  // java has lowercase here.
            Test("PM", "%tp", new DateTime(2009, 7, 1, 14, 10, 20));  // java has lowercase here.
            Test("93784010", "%tQ", new DateTime(1970, 1, 1) + new TimeSpan(1, 2, 3, 4, 10));
            Test("93784", "%ts", new DateTime(1970, 1, 1) + new TimeSpan(1, 2, 3, 4, 10));
            Test("02:03:04", "%tT", new DateTime(2009, 7, 1, 2, 3, 4));
            Test("14:52:43", "%tT", new DateTime(2009, 7, 1, 14, 52, 43));

            Test("", "%tz", new DateTime(2009, 7, 1, 1, 10, 20));
            Test("", "%tZ", new DateTime(2009, 7, 1, 1, 10, 20));

            Test("July", "%tB", new DateTime(2009, 7, 1, 1, 10, 20));
            Test("Jul", "%tb", new DateTime(2009, 7, 1, 1, 10, 20));
            Test("Jul", "%th", new DateTime(2009, 7, 1, 1, 10, 20));
            Test("Wednesday", "%tA", new DateTime(2009, 7, 1, 1, 10, 20));
            Test("Wed", "%ta", new DateTime(2009, 7, 1, 1, 10, 20));

            Test("19", "%tC", new DateTime(1998, 7, 1, 1, 10, 20));
            Test("20", "%tC", new DateTime(2009, 7, 1, 1, 10, 20));
            Test("09", "%tC", new DateTime(998, 7, 1, 1, 10, 20));

            Test("1998", "%tY", new DateTime(1998, 7, 1, 1, 10, 20));
            Test("2009", "%tY", new DateTime(2009, 7, 1, 1, 10, 20));
            Test("0998", "%tY", new DateTime(998, 7, 1, 1, 10, 20));
            Test("98", "%ty", new DateTime(1998, 7, 1, 1, 10, 20));
            Test("09", "%ty", new DateTime(2009, 7, 1, 1, 10, 20));

            Test("001", "%tj", new DateTime(2009, 1, 1));
            Test("004", "%tj", new DateTime(2009, 1, 1).AddDays(3));
            Test("201", "%tj", new DateTime(2009, 1, 1).AddDays(200));

            Test("01", "%tm", new DateTime(2009, 1, 1));
            Test("11", "%tm", new DateTime(2009, 11, 25));
            Test("01", "%td", new DateTime(2009, 1, 1));
            Test("25", "%td", new DateTime(2009, 11, 25));
            Test("1", "%te", new DateTime(2009, 1, 1));
            Test("25", "%te", new DateTime(2009, 11, 25));

            Test("01:02:03 AM", "%tr", new DateTime(2009, 7, 1, 1, 2, 3));
            Test("02:10:20 PM", "%tr", new DateTime(2009, 7, 1, 14, 10, 20));

            Test("01:02", "%tR", new DateTime(2009, 7, 1, 1, 2, 3));
            Test("14:10", "%tR", new DateTime(2009, 7, 1, 14, 10, 20));

            Test("Wed, 01 Jul 2009 01:02:03 GMT", "%tc", new DateTime(2009, 7, 1, 1, 2, 3));
            Test("Wed, 01 Jul 2009 14:10:20 GMT", "%tc", new DateTime(2009, 7, 1, 14, 10, 20));

            Test("07/01/09", "%tD", new DateTime(2009, 7, 1, 1, 2, 3));
            Test("11/25/98", "%tD", new DateTime(1998, 11, 25, 1, 2, 3));
            Test("2009-07-01", "%tF", new DateTime(2009, 7, 1, 1, 2, 3));
            Test("1998-11-25", "%tF", new DateTime(1998, 11, 25, 1, 2, 3));

        }


        [Test]
        public void DateTimeOffsetBasics()
        {
            Test("-05:12", "%tz", new DateTimeOffset(2009, 7, 1, 1, 10, 20,new TimeSpan(-5,-12,0)));
            Test("+05:12", "%tz", new DateTimeOffset(2009, 7, 1, 1, 10, 20, new TimeSpan(5, 12, 0)));
            Test("", "%tZ", new DateTimeOffset(2009, 7, 1, 1, 10, 20, new TimeSpan(-5, 12, 0)));
        }


        #endregion

    }
}
