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
using System.IO;


using NUnit.Framework;
using static NExpect.Expectations;
using clojure.lang;
using NExpect;
//using BigDecimal = java.math.BigDecimal;


namespace Clojure.Tests.LibTests
{
    [TestFixture]
    public class LispReaderTests
    {
        #region matchNumber tests

        [Test]
        public void MatchNumberMatchesZero()
        {
            object o1 = LispReader.MatchNumber("0");
            object o2 = LispReader.MatchNumber("-0");
            object o3 = LispReader.MatchNumber("+0");

            Expect(o1).To.Be.An.Instance.Of<long>(); 
            Expect((long)o1).To.Equal(0);
            Expect(o2).To.Be.An.Instance.Of<long>(); 
            Expect((long)o2).To.Equal(0);
            Expect(o3).To.Be.An.Instance.Of<long>(); 
            Expect((long)o3).To.Equal(0);
        }

        [Test]
        public void MatchNumberMatchesDecimal()
        {
            object o1 = LispReader.MatchNumber("123");
            object o2 = LispReader.MatchNumber("+123");
            object o3 = LispReader.MatchNumber("-123");
            object o4 = LispReader.MatchNumber("123456789123456789123456789");

            Expect(o1).To.Be.An.Instance.Of<long>();
            Expect((long)o1).To.Equal(123);
            Expect(o2).To.Be.An.Instance.Of<long>();
            Expect((long)o2).To.Equal(123);
            Expect(o3).To.Be.An.Instance.Of<long>(); 
            Expect((long)o3).To.Equal(-123);
            Expect(o4).To.Equal(BigInt.fromBigInteger(BigInteger.Parse("123456789123456789123456789")));
        }

        [Test]
        public void MatchNumberMatchesHexadecimal()
        {
            object o1 = LispReader.MatchNumber("0X12A");
            object o2 = LispReader.MatchNumber("0xFFF");
            object o3 = LispReader.MatchNumber("0xFFFFFFFFFFFFFFFFFFFFFFFF");

            Expect(o1).To.Be.An.Instance.Of<long>();
            Expect((long)o1).To.Equal(0x12A);
            Expect(o2).To.Be.An.Instance.Of<long>();
            Expect((long)o2).To.Equal(0xFFF);
            Expect(o3).To.Equal(BigInt.fromBigInteger(BigInteger.Parse("FFFFFFFFFFFFFFFFFFFFFFFF", 16)));
        }


        [Test]
        public void MatchNumberMatchesOctal()
        {
            object o1 = LispReader.MatchNumber("0123");
            object o2 = LispReader.MatchNumber("+0123");
            object o3 = LispReader.MatchNumber("-0123");
            object o4 = LispReader.MatchNumber("01234567012345670123456777");

            Expect(o1).To.Be.An.Instance.Of<long>(); 
            Expect((long)o1).To.Equal(83);
            Expect(o2).To.Be.An.Instance.Of<long>(); 
            Expect((long)o2).To.Equal(83);
            Expect(o3).To.Be.An.Instance.Of<long>(); 
            Expect((long)o3).To.Equal(-83);
            Expect(o4).To.Equal(BigInt.fromBigInteger(BigInteger.Parse("1234567012345670123456777", 8)));
        }

        [Test]
        public void MatchNumberMatchesSpecifiedRadix()
        {
            object o1 = LispReader.MatchNumber("2R1100");
            object o2 = LispReader.MatchNumber("4R123");
            object o3 = LispReader.MatchNumber("-4R123");
            object o4 = LispReader.MatchNumber("30R1234AQ");

            Expect(o1).To.Be.An.Instance.Of<long>();
            Expect((long)o1).To.Equal(12);
            Expect(o2).To.Be.An.Instance.Of<long>();
            Expect((long)o2).To.Equal(27);
            Expect(o3).To.Be.An.Instance.Of<long>(); 
            Expect((long)o3).To.Equal(-27);
            Expect(o4).To.Equal(BigInteger.Parse("1234AQ", 30).ToInt64());
        }

        [Test]
        public void MatchNumberMatchesFloats()
        {
            object o1 = LispReader.MatchNumber("123.7");
            object o2 = LispReader.MatchNumber("-123.7E4");
            object o3 = LispReader.MatchNumber("+1.237e4");
            object o4 = LispReader.MatchNumber("+1.237e-4");
            object o5 = LispReader.MatchNumber("1.237e+4");
            object o6 = LispReader.MatchNumber("1.");
            object o7 = LispReader.MatchNumber("1.e3");

            Expect(o1).To.Equal(123.7);
            Expect(o2).To.Equal(-1237000.0);
            Expect(o3).To.Equal(1.237e4);
            Expect(o4).To.Equal(1.237e-4);
            Expect(o5).To.Equal(1.237e4);
            Expect(o6).To.Equal(1.0);
            Expect(o7).To.Equal(1.0e3);
        }

        [Test]
        public void MatchNumberMatchesDecimals()
        {
            TestDecimalMatch("123.7M","123.7");
            TestDecimalMatch("-123.7E4M","-123.7E+4");
            TestDecimalMatch("+123.7E4M","123.7E4");
            TestDecimalMatch("0.0001234500M", "0.0001234500");
            TestDecimalMatch("123456789.987654321E-6M", "123.456789987654321");  
        }

        void TestDecimalMatch(string inStr,string bdStr)
        {
            object o = LispReader.MatchNumber(inStr);
            Expect(o).To.Equal(BigDecimal.Parse(bdStr));
        }

        [Test]
        public void MatchNumberMatchesRatios()
        {
            object o1 = LispReader.MatchNumber("12/1");
            object o2 = LispReader.MatchNumber("12/4");
            object o3 = LispReader.MatchNumber("12/5");
            object o4 = LispReader.MatchNumber("12345678900000/123456789");

            Expect(o1).To.Be.An.Instance.Of<long>(); 
            Expect((long)o1).To.Equal(12);
            Expect(o2).To.Be.An.Instance.Of<long>(); 
            Expect((long)o2).To.Equal(3);
            Expect(o3).To.Equal(new Ratio(BigInteger.Create(12), BigInteger.Create(5)));
            Expect(o4).To.Be.An.Instance.Of<long>(); 
            Expect((long)o4).To.Equal(100000);
        }

        [Test]
        public void MatchNumberReadsWholeString()
        {
            object o1 = LispReader.MatchNumber(" 123");
            object o2 = LispReader.MatchNumber("123 ");
            object o3 = LispReader.MatchNumber(" 12.3");
            object o4 = LispReader.MatchNumber("12.3 ");
            object o5 = LispReader.MatchNumber(" 1/23");
            object o6 = LispReader.MatchNumber("1/23 ");

            Expect(o1).To.Be.Null();
            Expect(o2).To.Be.Null();
            Expect(o3).To.Be.Null();
            Expect(o4).To.Be.Null();
            Expect(o5).To.Be.Null();
            Expect(o6).To.Be.Null();
        }

        [Test]
        public void MatchNumberFailsToMatchWeirdThings()
        {
            object o1 = LispReader.MatchNumber("123a");
            object o2 = LispReader.MatchNumber("0x123Z");
            object o4 = LispReader.MatchNumber("12.4/24.2");
            object o5 = LispReader.MatchNumber("1.7M3");

            Expect(o1).To.Be.Null();
            Expect(o2).To.Be.Null();
            Expect(o4).To.Be.Null();
            Expect(o5).To.Be.Null();
        }


        [Test]
        [ExpectedException(typeof(FormatException))]
        public void MatchNumberFailsOnRadixSnafu()
        {
            LispReader.MatchNumber("10RAA");
        }
        #endregion

        #region Helpers

        static PushbackTextReader CreatePushbackReaderFromString(string s)
        {
            return new PushbackTextReader(new StringReader(s));
        }

        static object ReadFromString(string s)
        {
            return LispReader.read(CreatePushbackReaderFromString(s),true,null,false);
        }

        static LineNumberingTextReader CreateLNPBRFromString(string s)
        {
            return new LineNumberingTextReader(new StringReader(s));
        }

        static object ReadFromStringNumbering(string s)
        {
            return LispReader.read(CreateLNPBRFromString(s),true,null,false);
        }


        #endregion
        
        #region Testing EOF

        [Test]
        public void EofValueReturnedOnEof()
        {
            object o = LispReader.read(CreatePushbackReaderFromString("   "), false, 7, false);
            Expect(o).To.Equal(7);
        }

        [Test]
        [ExpectedException(typeof(System.IO.EndOfStreamException))]
        public void EofValueFailsOnEof()
        {
            LispReader.read(CreatePushbackReaderFromString("   "), true, 7, false);
        }

        #endregion

        #region Testing a few numbers

        [Test]
        public void ReadReadsIntegers()
        {
            object o1 = ReadFromString("123");
            object o2 = ReadFromString("-123");
            object o3 = ReadFromString("+123");
            object o4 = ReadFromString("123456789123456789123456789");

            Expect(o1).To.Be.An.Instance.Of<long>(); 
            Expect((long)o1).To.Equal(123);
            Expect(o2).To.Be.An.Instance.Of<long>(); 
            Expect((long)o2).To.Equal(-123);
            Expect(o3).To.Be.An.Instance.Of<long>(); 
            Expect((long)o3).To.Equal(123);
            Expect(o4).To.Equal(BigInt.fromBigInteger(BigInteger.Parse("123456789123456789123456789")));
        }

        [Test]
        public void ReadReadsFloats()
        {
            object o1 = ReadFromString("123.4");
            object o2 = ReadFromString("-123.4E4");
            object o3 = ReadFromString("+123.4E-2");

            Expect(o1).To.Equal(123.4);
            Expect(o2).To.Equal(-123.4E4);
            Expect(o3).To.Equal(123.4E-2);
        }

        [Test]
        public void ReadReadsRatios()
        {
            object o1 = ReadFromString("123/456");
            object o2 = ReadFromString("-123/456");
            object o3 = ReadFromString("+123/456");

            Expect(o1).To.Be.An.Instance.Of<Ratio>();
            Expect(o2).To.Be.An.Instance.Of<Ratio>();
            Expect(o3).To.Be.An.Instance.Of<Ratio>();
        }



        #endregion

        #region Special tokens

        [Test]
        public void SlashAloneIsSlash()
        {
            object o = ReadFromString("/");
            Expect(o).To.Be.An.Instance.Of<Symbol>();
            Expect(((Symbol)o).Name).To.Equal("/");
            Expect(((Symbol)o).Namespace).To.Be.Null();
        }

        [Test]
        public void ClojureSlashIsSpecial()
        {
            object o = ReadFromString("clojure.core//");
            Expect(o).To.Be.An.Instance.Of<Symbol>();
            Expect(((Symbol)o).Name).To.Equal("/");
            Expect(((Symbol)o).Namespace).To.Equal("clojure.core");
        }

        [Test]
        public void TrueReturnsT()
        {
            object o = ReadFromString("true");
            Expect(o).To.Be.An.Instance.Of<bool>();
            Expect(o).To.Equal(true);
        }

        [Test]
        public void FalseReturnsF()
        {
            object o = ReadFromString("false");
            Expect(o).To.Be.An.Instance.Of<bool>();
            Expect(o).To.Equal(false);
        }

        [Test]
        public void NilIsNull()
        {
            object o = ReadFromString("nil");
            Expect(o).To.Be.Null();
        }

        #endregion

        #region Symbolic tests

        [Test]
        public void ReadReadsSymbolWithNoNS()
        {
            object o1 = ReadFromString("abc");

            Expect(o1).To.Be.An.Instance.Of<Symbol>();
            Expect(((Symbol)o1).Name).To.Equal("abc");
            Expect(((Symbol)o1).Namespace).To.Be.Null();
        }

        [Test]
        public void ReadReadsSymbolWithNS()
        {
            object o1 = ReadFromString("ab/cd");

            Expect(o1).To.Be.An.Instance.Of<Symbol>();
            Expect(((Symbol)o1).Name).To.Equal("cd");
            Expect(((Symbol)o1).Namespace).To.Equal("ab");
        }

        [Test]
        public void TwoSlashesIsOkayApparently()
        {
            object o1 = ReadFromString("ab/cd/e");

            Expect(o1).To.Be.An.Instance.Of<Symbol>();
            Expect(((Symbol)o1).Name).To.Equal("e");
            Expect(((Symbol)o1).Namespace).To.Equal("ab/cd");
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void NamespaceEndingWithColonSlashIsBad()
        {
            ReadFromString("ab:/cd");
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void NameEndingWithColonIsBad()
        {
            ReadFromString("ab/cd:");
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void NameEndingWithColonIsBad2()
        {
            ReadFromString("cd:");
        }

        [Test]
        public void NameMayContainMultipleColons()
        {
            object o1 = ReadFromString("a:b:c/d:e:f");
            Expect(o1).To.Be.An.Instance.Of<Symbol>();
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void NameContainingDoubleColonNotAtBeginningIsBad()
        {
            ReadFromString("ab::cd");
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void NamespaceContainingDoubleColonNotAtBeginningIsBad()
        {
            ReadFromString("ab::cd/ef");
        }

        [Test]
        public void PipeEscapingTurnsOffSpecialCharacters()
        {
            object o1 = ReadFromString("|ab(1 2)[1 2]{1 2}#{1 2}cd|");
            Expect(o1).To.Be.An.Instance.Of<Symbol>();
            Expect(((Symbol)o1).Name).To.Equal("ab(1 2)[1 2]{1 2}#{1 2}cd");
            Expect(((Symbol)o1).Namespace).To.Be.Null();
        }

        [Test]
        public void PipeEscapingWorksInside()
        {
            object o1 = ReadFromString("ab|(1 2)[1 2]{1 2}#{1 2}|cd");
            Expect(o1).To.Be.An.Instance.Of<Symbol>();
            Expect(((Symbol)o1).Name).To.Equal("ab(1 2)[1 2]{1 2}#{1 2}cd");
            Expect(((Symbol)o1).Namespace).To.Be.Null();
        }

        [Test]
        public void PipeEscapingMultipleTimesWorks()
        {
            object o1 = ReadFromString("ab|(1 2)[1 2]|cd|{1 2}#{1 2}|ef");
            Expect(o1).To.Be.An.Instance.Of<Symbol>();
            Expect(((Symbol)o1).Name).To.Equal("ab(1 2)[1 2]cd{1 2}#{1 2}ef");
            Expect(((Symbol)o1).Namespace).To.Be.Null();
        }

        [Test]
        public void PipeEscapingEscapesItself()
        {
            object o1 = ReadFromString("ab|cd||ef|gh||||");
            Expect(o1).To.Be.An.Instance.Of<Symbol>();
            Expect(((Symbol)o1).Name).To.Equal("abcd|efgh|");
            Expect(((Symbol)o1).Namespace).To.Be.Null();
        }

        [Test]
        public void PipeEscapingEatsSlash()
        {
            object o1 = ReadFromString("ab|cd/ef|gh");
            Expect(o1).To.Be.An.Instance.Of<Symbol>();
            Expect(((Symbol)o1).Name).To.Equal("abcd/efgh");
            Expect(((Symbol)o1).Namespace).To.Be.Null();
        }

        [Test]
        public void PipeEscapingEatsSlash2()
        {
            object o1 = ReadFromString("ab/cd|ef/gh|ij");
            Expect(o1).To.Be.An.Instance.Of<Symbol>();
            Expect(((Symbol)o1).Name).To.Equal("cdef/ghij");
            Expect(((Symbol)o1).Namespace).To.Equal("ab");
        }

        [Test]
        [ExpectedException(typeof(System.IO.EndOfStreamException))]
        public void PipeEscapingWithOddPipesIsBad()
        {
            ReadFromString("ab|cd|ef|gh");
        }

        #endregion

        #region Keyword tests

        [Test]
        public void LeadingColonIsKeyword()
        {
            object o1 = ReadFromString(":abc");
            Expect(o1).To.Be.An.Instance.Of<Keyword>();
            Expect(((Keyword)o1).Namespace).To.Be.Null();
            Expect(((Keyword)o1).Name).To.Equal("abc");
        }

        [Test]
        public void LeadingColonWithNSIsKeyword()
        {
            object o1 = ReadFromString(":ab/cd");
            Expect(o1).To.Be.An.Instance.Of<Keyword>();
            Expect(((Keyword)o1).Namespace).To.Equal("ab");
            Expect(((Keyword)o1).Name).To.Equal("cd");
        }

        // TODO: Add more tests dealing with :: resolution.

        [Test]
        public void LeadingDoubleColonMakesKeywordInCurrentNamespace()
        {
            object o1 = ReadFromString("::abc");
            Expect(o1).To.Be.An.Instance.Of<Keyword>();
            Expect(((Keyword)o1).Namespace).To.Equal(((Namespace)RT.CurrentNSVar.deref()).Name.Name);
            Expect(((Keyword)o1).Name).To.Equal("abc");
        }

        [Test]
        public void ColonDigitIsKeyword()
        {
            object o1 = ReadFromString(":1");
            Expect(o1).To.Be.An.Instance.Of<Keyword>();
            Expect(((Keyword)o1).Namespace).To.Be.Null();
            Expect(((Keyword)o1).Name).To.Equal("1");
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void ColonWithNSAndMissingNameIsBad()
        {
            ReadFromString(":bar/");
        }


        // At one time, this test worked.  Now, according to the documentation, it should not work.  Did something change?  Never mind.
        //[Test]
        //public void LeadingDoubleColonDoesNotSetNamespaceIfPeriodsInName()
        //{
        //    object o1 = ReadFromString("::ab.cd");
        //    Expect(o1).To.Be.An.Instance.Of<Keyword)));
        //    Expect(((Keyword)o1).Namespace).To.Be.Null();
        //    Expect(((Keyword)o1).Name).To.Equal("ab.cd"));
        //}

        #endregion

        #region String tests

        [Test]
        public void DoubleQuotesSurroundAString()
        {
            object o1 = ReadFromString("\"abc\"");
            Expect(o1).To.Be.An.Instance.Of<string>();
            Expect(o1).To.Equal("abc");
        }

        [Test]
        [ExpectedException(typeof(System.IO.EndOfStreamException))]
        public void NoEndingDoubleQuoteFails()
        {
            ReadFromString("\"abc");
        }

        [Test]
        public void EmptyStringWorks()
        {
            object o1 = ReadFromString("\"\"");
            Expect(o1).To.Be.An.Instance.Of<string>();
            Expect(o1).To.Equal(String.Empty);

        }

        [Test]
        public void EscapesWorkInStrings()
        {
            char[] chars = new char[] {
                '"', 'a', 
                '\\', 't', 'b',
                '\\', 'r', 'c',
                '\\', 'n', 'd',
                '\\', '\\', 'e',
                '\\', '"', 'f',
                '\\', 'b', 'g',
                '\\', 'f', 'h', '"' 
            };

            string s = new String(chars);
            Expect(s.Length).To.Equal(24);


            object o1 = ReadFromString(s);
            Expect(o1).To.Equal("a\tb\rc\nd\\e\"f\bg\fh");
        }

        [Test]
        [ExpectedException(typeof(System.IO.EndOfStreamException))]
        public void EOFinEscapeIsError()
        {
            char[] chars = new char[] {
                '"', 'a', 
                '\\', 't', 'b',
                '\\', 'r', 'c',
                '\\', 'n', 'd',
                '\\' 
            };
            string s = new String(chars);

            ReadFromString(s);
        }

        [Test]
        public void UnicodeEscapeInsertsUnicodeCharacter()
        {
                char[] chars = new char[] {
                '"', 'a', 
                '\\', 'u', '1', '2', 'C', '4',
                'b', '"'
            };

            string s = new String(chars);

            object o1 = ReadFromString(s);
            Expect(o1).To.Equal("a\u12C4b");
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void UnicodeEscapeWithBadCharacterFails()
        {
            char[] chars = new char[] {
                '"', 'a', 
                '\\', 'u', '1', '2', 'X', '4',
                'b', '"'
            };
            string s = new String(chars);

            ReadFromString(s);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void UnicodeEscapeWithEOFFails()
        {
            char[] chars = new char[] {
                '"', 'a', 
                '\\', 'u', '1', '2', 'A', '"'
            };
            string s = new String(chars);

            ReadFromString(s);
        }


        [Test]
        public void OctalEscapeInsertsCharacter()
        {
            char[] chars = new char[] {
                '"', 'a', 
                '\\', '1', '2',  '4',
                'b', '"'
            };
            string s = new String(chars);

            object o1 = ReadFromString(s);
            Expect(o1).To.Equal("a\x0054b");  // hex/octal conversion
        }


        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void OctalEscapeWithBadDigitFails()
        {
            char[] chars = new char[] {
                '"', 'a', 
                '\\', '1', '8',  '4',
                'b', '"'
            };
            string s = new String(chars);

            ReadFromString(s);
        }


        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void OctalEscapeWithEOFFails()
        {
            char[] chars = new char[] {
                '"', 'a', 
                '\\', '1', '8',  '"'
            };
            string s = new String(chars);

            ReadFromString(s);
        }

        [ExpectedException(typeof(ArgumentException))]
        public void OctalEscapeOutOfRangeFails()
        {
            char[] chars = new char[] {
                '"', 'a', 
                '\\', '4', '7',  '7',
                'b', '"'
            };
            string s = new String(chars);

            ReadFromString(s);
        }


        #endregion

        #region Character tests

        [Test]
        public void BackslashYieldsNextCharacter()
        {
            object o1 = ReadFromString("\\a");
            Expect(o1).To.Be.An.Instance.Of<Char>();
            Expect(o1).To.Equal('a');
        }

        [Test]
        public void BackslashYieldsNextCharacterStoppingAtTerminator()
        {
            object o1 = ReadFromString("\\a b");
            Expect(o1).To.Be.An.Instance.Of<Char>();
            Expect(o1).To.Equal('a');
        }

        [Test]
        [ExpectedException(typeof(System.IO.EndOfStreamException))]
        public void BackslashFollowedByEOFFails()
        {
            ReadFromString("\\");
        }

        [Test]
        public void BackslashRecognizesSpecialNames()
        {
            object o1 = ReadFromString("\\newline");
            object o2 = ReadFromString("\\space");
            object o3 = ReadFromString("\\tab");
            object o4 = ReadFromString("\\backspace");
            object o5 = ReadFromString("\\formfeed");
            object o6 = ReadFromString("\\return");

            Expect(o1).To.Equal('\n');
            Expect(o2).To.Equal(' ');
            Expect(o3).To.Equal('\t');
            Expect(o4).To.Equal('\b');
            Expect(o5).To.Equal('\f');
            Expect(o6).To.Equal('\r');
        }

        [Test]
        public void BackslashRecognizesUnicode()
        {
            object o1 = ReadFromString("\\u12C4");
            Expect(o1).To.Equal('\u12C4');
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void BackslashUnicodeWithEOFFails()
        {
            ReadFromString("\\u12C 4");
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void BackslashUnicodeInBadRangeFails()
        {
           ReadFromString("\\uDAAA");
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void BackslashUnicodeWithBadDigitFails()
        {
            ReadFromString("\\u12X4");
        }

        [Test]
        public void BackslashRecognizesOctal()
        {
            object o1 = ReadFromString("\\o124");
            Expect(o1).To.Equal('\x54');
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void BackslashOctalWithEOFFails()
        {
            ReadFromString("\\u12 4");
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void BackslashOctalInBadRangeFails()
        {
            ReadFromString("\\o444");
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void BackslashOctalWithBadDigitFails()
        {
            ReadFromString("\\o128");
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void BackslashOctalWithTooManyDigitsFails()
        {
            ReadFromString("\\o0012 aa");
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void BackslashWithOtherFails()
        {
            ReadFromString("\\aa");
        }

        #endregion

        #region comment tests

        [Test]
        public void SemicolonIgnoresToEndOfLine()
        {
            object o1 = ReadFromString("  ; ignore \n 123");
            Expect(o1).To.Be.An.Instance.Of<long>();
            Expect((long)o1).To.Equal(123);
        }

        [Test]
        public void SharpBangIgnoresToEndOfLine()
        {
            object o1 = ReadFromString("  #! ignore \n 123");
            Expect(o1).To.Be.An.Instance.Of<long>(); 
            Expect((long)o1).To.Equal(123);
        }

        #endregion

        #region Discard tests

        [Test]
        public void SharpUnderscoreIgnoresNextForm()
        {
            object o1 = ReadFromString("#_ (1 2 3) 4");
            Expect(o1).To.Be.An.Instance.Of<long>();
            Expect((long)o1).To.Equal(4);
        }

        [Test]
        public void SharpUnderscoreIgnoresNextFormInList()
        {
            object o1 = ReadFromString("( abc #_ (1 2 3) 12)");
            Expect(o1).To.Be.An.Instance.Of<PersistentList>();
            PersistentList pl = o1 as PersistentList;
            Expect(pl.count()).To.Equal(2);
            Expect(pl.first()).To.Be.An.Instance.Of<Symbol>();
            Expect(((Symbol)pl.first()).Name).To.Equal("abc");
            Expect(((Symbol)pl.first()).Namespace).To.Be.Null();
            Expect(pl.next().first()).To.Be.An.Instance.Of<long>();
            Expect((long)(pl.next().first())).To.Equal(12);
            Expect(pl.next().next()).To.Be.Null();
        }

        #endregion

        #region List tests

        [Test]
        public void CanReadBasicList()
        {
            Object o1 = ReadFromString("(abc 12)");
            Expect(o1).To.Be.An.Instance.Of<PersistentList>();
            PersistentList pl = o1 as PersistentList;
            Expect(pl.count()).To.Equal(2);
            Expect(pl.first()).To.Be.An.Instance.Of<Symbol>();
            Expect(((Symbol)pl.first()).Name).To.Equal("abc");
            Expect(((Symbol)pl.first()).Namespace).To.Be.Null();
            Expect(pl.next().first()).To.Be.An.Instance.Of<long>();
            Expect((long)(pl.next().first())).To.Equal(12);
            Expect(pl.next().next()).To.Be.Null();
        }

        [Test]
        public void CanReadEmptyList()
        {
            Object o1 = ReadFromString("(   )");
            Expect(o1).To.Be.An.Instance.Of<IPersistentList>();
            IPersistentList pl = o1 as IPersistentList;
            Expect(pl.count()).To.Equal(0);
        }

        [Test]
        public void CanReadNestedList()
        {
            Object o1 = ReadFromString("(a (b c) d)");
            Expect(o1).To.Be.An.Instance.Of<IPersistentList>();
            IPersistentList pl = o1 as IPersistentList;
            ISeq seq = pl.seq();
            Expect(pl.count()).To.Equal(3);
            Expect(seq.next().first()).To.Be.An.Instance.Of<IPersistentList>();
            IPersistentList sub = seq.next().first() as IPersistentList;
            Expect(sub.count()).To.Equal(2);
        }

        [Test]
        [ExpectedException(typeof(System.IO.EndOfStreamException))]
        public void MissingListTerminatorFails()
        {
            ReadFromString("(a b 1 2");
        }

        [Test]
        public void ListGetsLineNumber()
        {
            Object o1 = ReadFromStringNumbering("\n\n (a b \n1 2)");
            Expect(o1).To.Be.An.Instance.Of<IObj>();
            IObj io = o1 as IObj;
            Expect(io.meta().valAt(RT.LineKey)).To.Equal(3);
            IPersistentMap sourceSpanMap = (IPersistentMap)io.meta().valAt(RT.SourceSpanKey);
            Expect(sourceSpanMap.valAt(RT.StartLineKey)).To.Equal(3);
            Expect(sourceSpanMap.valAt(RT.StartColumnKey)).To.Equal(3);
            Expect(sourceSpanMap.valAt(RT.EndLineKey)).To.Equal(4);
            Expect(sourceSpanMap.valAt(RT.EndColumnKey)).To.Equal(5);            
        }

        #endregion

        #region VectorTests

        [Test]
        public void CanReadBasicVector()
        {
            Object o1 = ReadFromString("[abc 12]");
            Expect(o1).To.Be.An.Instance.Of<PersistentVector>();
            IPersistentVector pl = o1 as IPersistentVector;
            Expect(pl.count()).To.Equal(2);
            Expect(pl.nth(0)).To.Be.An.Instance.Of<Symbol>();
            Expect(((Symbol)pl.nth(0)).Name).To.Equal("abc");
            Expect(((Symbol)pl.nth(0)).Namespace).To.Be.Null();
            Expect(pl.nth(1)).To.Be.An.Instance.Of<long>();
            Expect((long)(pl.nth(1))).To.Equal(12);
        }

        [Test]
        public void CanReadEmptyVector()
        {
            Object o1 = ReadFromString("[   ]");
            Expect(o1).To.Be.An.Instance.Of<IPersistentVector>();
            IPersistentVector v = o1 as IPersistentVector;
            Expect(v.count()).To.Equal(0);
        }

        [Test]
        public void VectorCanContainNestedList()
        {
            Object o1 = ReadFromString("[a (b c) d]");
            Expect(o1).To.Be.An.Instance.Of<IPersistentVector>();
            IPersistentVector v = o1 as IPersistentVector;
            Expect(v.count()).To.Equal(3);
            Expect(v.nth(1)).To.Be.An.Instance.Of<IPersistentList>();
            IPersistentList sub = v.nth(1) as IPersistentList;
            Expect(sub.count()).To.Equal(2);
        }

        [Test]
        public void VectorCanContainNestedVector()
        {
            Object o1 = ReadFromString("[a [b c] d]");
            Expect(o1).To.Be.An.Instance.Of<IPersistentVector>();
            IPersistentVector v = o1 as IPersistentVector;
            Expect(v.count()).To.Equal(3);
            Expect(v.nth(1)).To.Be.An.Instance.Of<IPersistentVector>();
            IPersistentVector sub = v.nth(1) as IPersistentVector;
            Expect(sub.count()).To.Equal(2);
        }

        [Test]
        [ExpectedException(typeof(System.IO.EndOfStreamException))]
        public void MissingVectorTerminatorFails()
        {
            ReadFromString("[a b 1 2");
        }

        #endregion

        #region Map tests

        [Test]
        public void CanReadBasicMap()
        {
            Object o1 = ReadFromString("{:abc 12 14 a}");
            Expect(o1).To.Be.An.Instance.Of<IPersistentMap>();
            IPersistentMap m = o1 as IPersistentMap;
            Expect(m.count()).To.Equal(2);
            object v = m.valAt(Keyword.intern(null, "abc"));
            Expect(v).To.Be.An.Instance.Of<long>();
            Expect((long)v).To.Equal(12);
            Expect(m.valAt(14)).To.Equal(Symbol.intern("a"));
        }

        [Test]
        public void CanReadEmptyMap()
        {
            Object o1 = ReadFromString("{   }");
            Expect(o1).To.Be.An.Instance.Of<IPersistentMap>();
            IPersistentMap m = o1 as IPersistentMap;
            Expect(m.count()).To.Equal(0);
        }


        [Test]
        [ExpectedException(typeof(System.IO.EndOfStreamException))]
        public void MissingRightBraceFails()
        {
            ReadFromString("{a b 1 2");
        }

        //[Test]
        //[ExpectedException(typeof(ArgumentException))]
        //public void MapWithOddNumberOfEntriesFails()
        //{
        //    Object o1 = ReadFromString("{a b 1}");
        //}



        #endregion

        #region Set tests

        [Test]
        public void CanReadBasicSet()
        {
            Object o1 = ReadFromString("#{abc 12}");
            Expect(o1).To.Be.An.Instance.Of<IPersistentSet>();
            IPersistentSet s = o1 as IPersistentSet;
            Expect(s.count()).To.Equal(2);
            Expect(s.contains(Symbol.intern("abc")));
            Expect(s.contains(12));
        }

        [Test]
        public void CanReadEmptySet()
        {
            Object o1 = ReadFromString("#{   }");
            Expect(o1).To.Be.An.Instance.Of<IPersistentSet>();
            IPersistentSet s = o1 as IPersistentSet;
            Expect(s.count()).To.Equal(0);
        }

        [Test]
        [ExpectedException(typeof(System.IO.EndOfStreamException))]
        public void MissingSetTerminatorFails()
        {
            ReadFromString("#{a b 1 2");
        }

        #endregion

        #region Unmatched delimiter tests

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void NakedRightParenIsBad()
        {
            ReadFromString("}");
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void NakedRightBracketIsBad()
        {
            ReadFromString("]");
        }


        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void NakedRightBraceIsBad()
        {
            ReadFromString("}");
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void MismatchedDelimiterIsBad()
        {
            ReadFromString("( a b c }");
        }

        #endregion

        #region  Wrapping forms

        [Test]
        public void QuoteWraps()
        {
            object o1 = ReadFromString("'a");
            Expect(o1).To.Be.An.Instance.Of<ISeq>();
            ISeq s = o1 as ISeq;
            Expect(s.count()).To.Equal(2);
            Expect(s.first()).To.Equal(Symbol.intern("quote"));
            Expect(s.next().first()).To.Be.An.Instance.Of<Symbol>();
        }

        [Test]
        public void QuoteWraps2()
        {
            object o1 = ReadFromString("'(a b c)");
            Expect(o1).To.Be.An.Instance.Of<ISeq>();
            ISeq s = o1 as ISeq;
            Expect(s.count()).To.Equal(2);
            Expect(s.first()).To.Equal(Symbol.intern("quote"));
            Expect(s.next().first()).To.Be.An.Instance.Of<IPersistentList>();
            Expect(((IPersistentList)s.next().first()).count()).To.Equal(3);
        }

        [Test]
        public void DerefWraps()
        {
            object o1 = ReadFromString("@a");
            Expect(o1).To.Be.An.Instance.Of<ISeq>();
            ISeq s = o1 as ISeq;
            Expect(s.count()).To.Equal(2);
            Expect(s.first()).To.Equal(Symbol.intern("clojure.core", "deref"));
            Expect(s.next().first()).To.Be.An.Instance.Of<Symbol>();
        }

        [Test]
        public void DerefWraps2()
        {
            object o1 = ReadFromString("@(a b c)");
            Expect(o1).To.Be.An.Instance.Of<ISeq>();
            ISeq s = o1 as ISeq;
            Expect(s.count()).To.Equal(2);
            Expect(s.first()).To.Equal(Symbol.intern("clojure.core", "deref"));
            Expect(s.next().first()).To.Be.An.Instance.Of<IPersistentList>();
            Expect(((IPersistentList)s.next().first()).count()).To.Equal(3);
        }

        #endregion

        #region Syntax-quote tests

        [Test]
        public void SQOnSelfEvaluatingReturnsQuotedThing()
        {
            object o1 = ReadFromString("`:abc");
            object o2 = ReadFromString("`222");
            object o3 = ReadFromString("`\\a)");
            object o4 = ReadFromString("`\"abc\"");

            Expect(o1).To.Be.An.Instance.Of<Keyword>();
            Expect(o1).To.Equal(Keyword.intern(null, "abc"));
            Expect(o2).To.Be.An.Instance.Of<long>();
            Expect((long)o2).To.Equal(222);
            Expect(o3).To.Be.An.Instance.Of<char>();
            Expect((char)o3).To.Equal('a');
            Expect(o4).To.Be.An.Instance.Of<string>();
            Expect(o4).To.Equal("abc");
        }

        [Test]
        public void SQOnSpecialFormQuotes()
        {
            object o1 = ReadFromString("`def");
            Expect(o1).To.Be.An.Instance.Of<ISeq>();
            ISeq s = o1 as ISeq;
            Expect(s.count()).To.Equal(2);
            Expect(s.first()).To.Equal(Symbol.intern("quote"));
            Expect(s.next().first()).To.Be.An.Instance.Of<Symbol>();
            Symbol sym = s.next().first() as Symbol;
            Expect(sym.Namespace).To.Be.Null();
            Expect(sym.Name).To.Equal("def");
        }

        [Test]
        public void SQOnRegularSymbolResolves()
        {
            object o1 = ReadFromString("`abc");
            Expect(o1).To.Be.An.Instance.Of<ISeq>();
            ISeq s = o1 as ISeq;
            Expect(s.count()).To.Equal(2);
            Expect(s.first()).To.Equal(Symbol.intern("quote"));
            Expect(s.next().first()).To.Be.An.Instance.Of<Symbol>();
            Symbol sym = s.next().first() as Symbol;
            Expect(sym.Namespace).To.Equal(((Namespace)RT.CurrentNSVar.deref()).Name.Name);
            Expect(sym.Name).To.Equal("abc");
        }

        [Test]
        public void SQOnGensymGenerates()
        {
            object o1 = ReadFromString("`abc#");
            Expect(o1).To.Be.An.Instance.Of<ISeq>();
            ISeq s = o1 as ISeq;
            Expect(s.count()).To.Equal(2);
            Expect(s.first()).To.Equal(Symbol.intern("quote"));
            Expect(s.next().first()).To.Be.An.Instance.Of<Symbol>();
            Symbol sym = s.next().first() as Symbol;
            Expect(sym.Namespace).To.Be.Null();
            Expect(sym.Name.StartsWith("abc_")); ;
        }


        [Test]
        public void SQOnGensymSeesSameTwice()
        {
            object o1 = ReadFromString("`(abc# abc#)");
            // Return should be 
            //    (clojure/seq (clojure/concat (clojure/list (quote abc__N)) 
            //                                 (clojure/list (quote abc__N)))))

            Expect(o1).To.Be.An.Instance.Of<ISeq>();
            ISeq s = o1 as ISeq;
            Expect(s.count()).To.Equal(2);
            Expect(s.first()).To.Equal(Symbol.intern("clojure.core","seq"));
            Expect(s.next().first()).To.Be.An.Instance.Of<ISeq>();
            ISeq s1 = s.next().first() as ISeq;

            Expect(s1.count()).To.Equal(3);
            Expect(s1.first()).To.Equal(Symbol.intern("clojure.core","concat"));

            Expect(s1.next().first()).To.Be.An.Instance.Of<ISeq>();
            ISeq s2 = s1.next().first() as ISeq;
            Expect(s2.count()).To.Equal(2);

            Expect(s2.next().first()).To.Be.An.Instance.Of<ISeq>();
            ISeq s2a = s2.next().first() as ISeq;
            Expect(s2a.next().first()).To.Be.An.Instance.Of<Symbol>();
            Symbol sym1 = s2a.next().first() as Symbol;

            Expect(s1.next().next().first()).To.Be.An.Instance.Of<ISeq>();
            ISeq s3 = s1.next().next().first() as ISeq;
            Expect(s3.count()).To.Equal(2);

            Expect(s3.next().first()).To.Be.An.Instance.Of<ISeq>();
            ISeq s3a = s3.next().first() as ISeq;
            Expect(s3a.next().first()).To.Be.An.Instance.Of<Symbol>();
            Symbol sym2 = s3a.next().first() as Symbol;

            Expect(sym1.Namespace).To.Be.Null();
            Expect(sym1.Name.StartsWith("abc__"));
            Expect(sym1).To.Equal(sym2);
        }

        [Test]
        public void SQOnMapMakesMap()
        {
            Object o1 = ReadFromString("`{:a 1 :b 2}");
            //  (clojure/apply 
            //      clojure/hash-map 
            //         (clojure/seq 
            //             (clojure/concat (clojure/list :a) 
            //                             (clojure/list 1) 
            //                             (clojure/list :b) 
            //                             (clojure/list 2))))

            Expect(o1).To.Be.An.Instance.Of<ISeq>();
            ISeq s = o1 as ISeq;
            Expect(s.count()).To.Equal(3);
            Expect(s.first()).To.Equal(Symbol.intern("clojure.core/apply"));
            Expect(s.next().first()).To.Equal(Symbol.intern("clojure.core/hash-map"));
            Expect(s.next().next().first()).To.Be.An.Instance.Of<ISeq>();

            ISeq s0 = s.next().next().first() as ISeq;
            ISeq s2;

            Expect(s0.count()).To.Equal(2);
            Expect(s0.first()).To.Equal(Symbol.intern("clojure.core/seq"));
            Expect(s0.next().first()).To.Be.An.Instance.Of<ISeq> ();
            ISeq s1 = s0.next().first() as ISeq;

            Expect(s1.count()).To.Equal(5);
            Expect(s1.first()).To.Equal(Symbol.intern("clojure.core/concat"));

            s1 = s1.next();
            Expect(s1.first()).To.Be.An.Instance.Of<ISeq> ();

            s2 = s1.first() as ISeq;
            Expect(s2.first()).To.Equal(Symbol.intern("clojure.core/list"));
            Expect(s2.next().first()).To.Equal(Keyword.intern(null,"a"));


            s1 = s1.next();
            Expect(s1.first()).To.Be.An.Instance.Of<ISeq>();

            s2 = s1.first() as ISeq;
            Expect(s2.first()).To.Equal(Symbol.intern("clojure.core/list"));
            object v2 = s2.next().first();
            Expect(v2).To.Be.An.Instance.Of<long>();
            Expect((long)v2).To.Equal(1);

            s1 = s1.next();
            Expect(s1.first()).To.Be.An.Instance.Of<ISeq>();

            s2 = s1.first() as ISeq;
            Expect(s2.first()).To.Equal(Symbol.intern("clojure.core/list"));
            Expect(s2.next().first()).To.Equal(Keyword.intern(null, "b"));


            s1 = s1.next();
            Expect(s1.first()).To.Be.An.Instance.Of<ISeq>();

            s2 = s1.first() as ISeq;
            Expect(s2.first()).To.Equal(Symbol.intern("clojure.core/list"));
            v2 = s2.next().first();
            Expect(v2).To.Be.An.Instance.Of<long>();
            Expect((long)v2).To.Equal(2);
        }

        public void SQOnVectorMakesVector()
        {
            Object o1 = ReadFromString("`[:b 2]");
            //  (clojure/apply 
            //      clojure/vector 
            //         (clojure/concat (clojure/list :b) 
            //                         (clojure/list 2)))

            Expect(o1).To.Be.An.Instance.Of<ISeq>();
            ISeq s = o1 as ISeq;
            Expect(s.count()).To.Equal(3);
            Expect(s.first()).To.Equal(Symbol.intern("clojure.core/apply"));
            Expect(s.next().first()).To.Equal(Symbol.intern("clojure.core/vector"));
            Expect(s.next().next().first()).To.Be.An.Instance.Of<ISeq>();

            ISeq s1 = s.next().next().first() as ISeq;
            ISeq s2;

            Expect(s1.count()).To.Equal(3);
            Expect(s1.first()).To.Equal(Symbol.intern("clojure.core/concat"));

            s1 = s1.next();
            Expect(s1.first()).To.Be.An.Instance.Of<ISeq>();

            s2 = s1.first() as ISeq;
            Expect(s2.first()).To.Equal(Symbol.intern("clojure.core/list"));
            Expect(s2.next().first()).To.Equal(Keyword.intern(null, "b"));


            s1 = s1.next();
            Expect(s1.first()).To.Be.An.Instance.Of<ISeq>();

            s2 = s1.first() as ISeq;
            Expect(s2.first()).To.Equal(Symbol.intern("clojure.core/list"));
            Expect(s2.next().first()).To.Equal(2);
        }

        [Test]
        public void SQOnSetMakesSet()
        {
            Object o1 = ReadFromString("`#{:b 2}");
            //  (clojure/apply 
            //      clojure/hash-set 
            //         (clojure/seq
            //             (clojure/concat (clojure/list :b) 
            //                             (clojure/list 2))))

            Expect(o1).To.Be.An.Instance.Of<ISeq>();
            ISeq s = o1 as ISeq;
            Expect(s.count()).To.Equal(3);
            Expect(s.first()).To.Equal(Symbol.intern("clojure.core/apply"));
            Expect(s.next().first()).To.Equal(Symbol.intern("clojure.core/hash-set"));
            Expect(s.next().next().first()).To.Be.An.Instance.Of<ISeq>();

            ISeq s0 = s.next().next().first() as ISeq;
            Expect(s0.count()).To.Equal(2);
            Expect(s0.first()).To.Equal(Symbol.intern("clojure.core/seq"));
            Expect(s0.next().first()).To.Be.An.Instance.Of<ISeq>();

            ISeq s1 = s0.next().first() as ISeq;
            
            Expect(s1.count()).To.Equal(3);
            Expect(s1.first()).To.Equal(Symbol.intern("clojure.core/concat"));

            s1 = s1.next();
            Expect(s1.first()).To.Be.An.Instance.Of<ISeq>();
            ISeq s2 = s1.first() as ISeq;
            s1 = s1.next();
            Expect(s1.first()).To.Be.An.Instance.Of<ISeq>();
            ISeq s3 = s1.first() as ISeq;

            Expect(s2.first()).To.Equal(Symbol.intern("clojure.core/list"));
            Expect(s3.first()).To.Equal(Symbol.intern("clojure.core/list"));


            object e1 = s2.next().first();
            object e2 = s3.next().first();

            // Set elements can occur in any order

            //Expect(e1).To.Equal(Keyword.intern(null, "b"))| EqualTo(2);
            //Expect(e2).To.Equal(Keyword.intern(null, "b")) | EqualTo(2);
            Expect(e1).To.Not.Equal(e2);
        }

        [Test]
        public void SQOnListMakesList()
        {
            Object o1 = ReadFromString("`(:b 2)");
            //   (clojure/seq (clojure/concat (clojure/list :b) 
            //                                (clojure/list 2))))

            Expect(o1).To.Be.An.Instance.Of<ISeq>();

            ISeq s0 = o1 as ISeq;
            Expect(s0.count()).To.Equal(2);
            Expect(s0.first()).To.Equal(Symbol.intern("clojure.core/seq"));
            Expect(s0.next().first()).To.Be.An.Instance.Of<ISeq>();
            ISeq s1 = s0.next().first() as ISeq;
            ISeq s2;

            Expect(s1.count()).To.Equal(3);
            Expect(s1.first()).To.Equal(Symbol.intern("clojure.core/concat"));

            s1 = s1.next();
            Expect(s1.first()).To.Be.An.Instance.Of<ISeq>();

            s2 = s1.first() as ISeq;
            Expect(s2.first()).To.Equal(Symbol.intern("clojure.core/list"));
            Expect(s2.next().first()).To.Equal(Keyword.intern(null, "b"));


            s1 = s1.next();
            Expect(s1.first()).To.Be.An.Instance.Of<ISeq>();

            s2 = s1.first() as ISeq;
            Expect(s2.first()).To.Equal(Symbol.intern("clojure.core/list"));
            object v = s2.next().first();
            Expect(v).To.Be.An.Instance.Of<long>();
            Expect((long)v).To.Equal(2);
        }


        [Test]
        public void UnquoteStandaloneReturnsUnquoteObject()
        {
            object o1 = ReadFromString("~x");

            //Expect(o1).To.Be.An.Instance.Of<LispReader.Unquote)));
            //LispReader.Unquote u = o1 as LispReader.Unquote;
            //Expect(u.Obj).To.Equal(Symbol.intern("x")));
            Expect(o1).To.Be.An.Instance.Of<ISeq>();
            ISeq s = o1 as ISeq;
            Expect(s.first()).To.Equal(Symbol.intern("clojure.core/unquote"));
            Expect(s.next().first()).To.Equal(Symbol.intern("x"));
            Expect(s.count()).To.Equal(2);

        }

        [Test]
        public void UnquoteSpliceStandaloneReturnsUnquoteSpliceObject()
        {
            object o1 = ReadFromString("~@x");

            Expect(o1).To.Be.An.Instance.Of<ISeq>();
            ISeq s = o1 as ISeq;
            Expect(s.first()).To.Equal(Symbol.intern("clojure.core/unquote-splicing"));
            Expect(s.next().first()).To.Equal(Symbol.intern("x"));
            Expect(s.count()).To.Equal(2);
        }

        [Test]
        public void SQonUnquoteDequotes()
        {
            object o1 = ReadFromString("`(a ~b)");
            // (clojure/seq (clojure/concat (clojure/list (quote NS/a)) 
            //                              (clojure/list b)))


            Expect(o1).To.Be.An.Instance.Of<ISeq>();

            ISeq s0 = o1 as ISeq;
            Expect(s0.count()).To.Equal(2);
            Expect(s0.first()).To.Equal(Symbol.intern("clojure.core/seq"));
            Expect(s0.next().first()).To.Be.An.Instance.Of<ISeq>();

            ISeq s1 = s0.next().first() as ISeq;
            ISeq s2;

            Expect(s1.count()).To.Equal(3);
            Expect(s1.first()).To.Equal(Symbol.intern("clojure.core/concat"));

            s1 = s1.next();
            Expect(s1.first()).To.Be.An.Instance.Of<ISeq>();

            s2 = s1.first() as ISeq;
            Expect(s2.first()).To.Equal(Symbol.intern("clojure.core/list"));
            Expect(s2.next().first()).To.Be.An.Instance.Of<ISeq>();
            ISeq s3 = s2.next().first() as ISeq;

            Expect(s3.count()).To.Equal(2);
            Expect(s3.first()).To.Equal(Symbol.intern("quote"));
            Expect(s3.next().first()).To.Equal(Symbol.intern(((Namespace)RT.CurrentNSVar.deref()).Name.Name,"a"));


            s1 = s1.next();
            Expect(s1.first()).To.Be.An.Instance.Of<ISeq>();

            s2 = s1.first() as ISeq;
            Expect(s2.first()).To.Equal(Symbol.intern("clojure.core/list"));
            Expect(s2.next().first()).To.Equal(Symbol.intern("b"));
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void SQonUnquoteSpliceNotInListFails()
        {
            ReadFromString("`~@x");
        }

        [Test]
        public void SqOnUnquoteSpliceSplices()
        {
            object o1 = ReadFromString("`(a ~@b)");
            // (clojure/seq (clojure/concat (clojure/list (quote user/a)) b))

            Expect(o1).To.Be.An.Instance.Of<ISeq>();

            ISeq s0 = o1 as ISeq;
            Expect(s0.count()).To.Equal(2);
            Expect(s0.first()).To.Equal(Symbol.intern("clojure.core/seq"));
            Expect(s0.next().first()).To.Be.An.Instance.Of<ISeq>();

            ISeq s1 = s0.next().first() as ISeq;
            ISeq s2;

            Expect(s1.count()).To.Equal(3);
            Expect(s1.first()).To.Equal(Symbol.intern("clojure.core/concat"));

            s1 = s1.next();
            Expect(s1.first()).To.Be.An.Instance.Of<ISeq>();

            s2 = s1.first() as ISeq;
            Expect(s2.first()).To.Equal(Symbol.intern("clojure.core/list"));
            Expect(s2.next().first()).To.Be.An.Instance.Of<ISeq>();
            ISeq s3 = s2.next().first() as ISeq;

            Expect(s3.count()).To.Equal(2);
            Expect(s3.first()).To.Equal(Symbol.intern("quote"));
            Expect(s3.next().first()).To.Equal(Symbol.intern(((Namespace)RT.CurrentNSVar.deref()).Name.Name, "a"));


            s1 = s1.next();
            Expect(s1.first()).To.Equal(Symbol.intern("b"));
         }

        // We should test to see that 'line' meta info is not preserved.

        [Test]
        public void SQOnLparenRParenReturnsEmptyList()
        {
            object o1 = ReadFromString("`()");
            //  (clojure/list)
            Expect(o1).To.Be.An.Instance.Of<ISeq>();
            ISeq s = o1 as ISeq;
            Expect(s.count()).To.Equal(1);
            Expect(s.first()).To.Equal(Symbol.intern("clojure.core/list"));
        }

        #endregion

        #region #-dispatch tests

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void SharpDispatchOnInvalidCharFails()
        {
            ReadFromString("#1(1 2)");
        }

        #endregion

        #region Meta reader tests

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void MetaOnImproperMetadataFails()
        {
            ReadFromString("^1 (a b c");
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void MetaOnAppliedToNonIObjFails()
        {
            ReadFromString("^{:a 1} 7");
        }

        [Test]
        public void MetaAppliesHashMetaDataToObject()
        {
            object o1 = ReadFromString("^{a 1} (a b)");

            Expect(o1).To.Be.An.Instance.Of<ISeq>();
            ISeq s = o1 as ISeq;

            Expect(s.count()).To.Equal(2);
            Expect(s.first()).To.Equal(Symbol.intern("a"));
            Expect(s.next().first()).To.Equal(Symbol.intern("b"));

            Expect(o1).To.Be.An.Instance.Of<IObj>();
            IObj o = o1 as IObj;
            Expect(o.meta()).To.Be.An.Instance.Of<IPersistentMap>();
            IPersistentMap m = o.meta() as IPersistentMap;

            Expect(m.count()).To.Equal(1);
            object v = m.valAt(Symbol.intern("a"));
            Expect(v).To.Be.An.Instance.Of<long>();
            Expect((long)v).To.Equal(1);
        }


        [Test]
        public void MetaAppliesSymbolAsTagMetaDataToObject()
        {
            object o1 = ReadFromString("^c (a b)");

            Expect(o1).To.Be.An.Instance.Of<ISeq>();
            ISeq s = o1 as ISeq;

            Expect(s.count()).To.Equal(2);
            Expect(s.first()).To.Equal(Symbol.intern("a"));
            Expect(s.next().first()).To.Equal(Symbol.intern("b"));

            Expect(o1).To.Be.An.Instance.Of<IObj>();
            IObj o = o1 as IObj;
            Expect(o.meta()).To.Be.An.Instance.Of<IPersistentMap>();
            IPersistentMap m = o.meta() as IPersistentMap;

            Expect(m.count()).To.Equal(1);
            Expect(m.valAt(Keyword.intern(null,"tag"))).To.Equal(Symbol.intern("c"));
        }

        [Test]
        public void MetaAppliesKeywordWithTrueToObject()
        {
            object o1 = ReadFromString("^:c (a b)");

            Expect(o1).To.Be.An.Instance.Of<ISeq>();
            ISeq s = o1 as ISeq;

            Expect(s.count()).To.Equal(2);
            Expect(s.first()).To.Equal(Symbol.intern("a"));
            Expect(s.next().first()).To.Equal(Symbol.intern("b"));

            Expect(o1).To.Be.An.Instance.Of<IObj>();
            IObj o = o1 as IObj;
            Expect(o.meta()).To.Be.An.Instance.Of<IPersistentMap>();
            IPersistentMap m = o.meta() as IPersistentMap;

            Expect(m.count()).To.Equal(1);
            Expect(m.valAt(Keyword.intern(null, "c"))).To.Equal(true);
        }

        [Test]
        public void MetaAppliesStringAsTagMetaDataToObject()
        {
            object o1 = ReadFromString("^\"help\" (a b)");

            Expect(o1).To.Be.An.Instance.Of<ISeq>();
            ISeq s = o1 as ISeq;

            Expect(s.count()).To.Equal(2);
            Expect(s.first()).To.Equal(Symbol.intern("a"));
            Expect(s.next().first()).To.Equal(Symbol.intern("b"));

            Expect(o1).To.Be.An.Instance.Of<IObj>();
            IObj o = o1 as IObj;
            Expect(o.meta()).To.Be.An.Instance.Of<IPersistentMap>();
            IPersistentMap m = o.meta() as IPersistentMap;

            Expect(m.count()).To.Equal(1);
            Expect(m.valAt(Keyword.intern(null, "tag"))).To.Equal("help");
        }

        [Test]
        public void MetaAddsLineupNumberAsMetaDataIfAvailable()
        {
            object o1 = ReadFromStringNumbering("\n\n^c (a b)");

            Expect(o1).To.Be.An.Instance.Of<ISeq>();
            ISeq s = o1 as ISeq;

            Expect(s.count()).To.Equal(2);
            Expect(s.first()).To.Equal(Symbol.intern("a"));
            Expect(s.next().first()).To.Equal(Symbol.intern("b"));

            Expect(o1).To.Be.An.Instance.Of<IObj>();
            IObj o = o1 as IObj;
            Expect(o.meta()).To.Be.An.Instance.Of<IPersistentMap>();
            IPersistentMap m = o.meta() as IPersistentMap;

            Expect(m.count()).To.Equal(4);
            Expect(m.valAt(Keyword.intern(null, "tag"))).To.Equal(Symbol.intern(null,"c"));
            Expect(m.valAt(Keyword.intern(null, "line"))).To.Equal(3);
        }

        #endregion

        #region Var reader tests

        [Test]
        public void VarWrapsVar()
        {
            Object o1 = ReadFromString("#'abc");

            Expect(o1).To.Be.An.Instance.Of<ISeq>();
            ISeq s = o1 as ISeq;
            Expect(s.count()).To.Equal(2);
            Expect(s.first()).To.Equal(Symbol.intern("var"));
            Expect(s.next().first()).To.Equal(Symbol.intern("abc"));
        }

        #endregion

        #region Regex reader tests

        [Test]
        public void SharpDoubleQuoteGeneratesRegex()
        {
            object o1 = ReadFromString("#\"abc\"");

            Expect(o1).To.Be.An.Instance.Of<System.Text.RegularExpressions.Regex>();
            System.Text.RegularExpressions.Regex r = o1 as System.Text.RegularExpressions.Regex;
            Expect(r.ToString()).To.Equal("abc");
        }

        [Test]
        [ExpectedException(typeof(System.IO.EndOfStreamException))]
        public void SharpDQHitsEOFFails()
        {
            ReadFromString("#\"abc");
        }

        [Test]
        public void SharpDQEscapesOnBackslash()
        {
            char[] chars = new char[] {
                '#', '"', 'a', '\\', '"', 'b', 'c', '"'
            };

            //  input =  #"a\"bc" -- should go over the "

            string str = new String(chars);

            object o1 = ReadFromString(str);

            Expect(o1).To.Be.An.Instance.Of<System.Text.RegularExpressions.Regex>();
            System.Text.RegularExpressions.Regex r = o1 as System.Text.RegularExpressions.Regex;
            Expect(r.ToString()).To.Equal("a\\\"bc");
        }

        #endregion

        #region Fn reader & Arg reader tests

        [Test]
        public void SharpFnWithNoArgsGeneratesNoArgFn()
        {
            object o1 = ReadFromString("#(+ 1 2)");
            // (fn* [] (+ 1 2))

            Expect(o1).To.Be.An.Instance.Of<ISeq>();
            ISeq s = o1 as ISeq;

            Expect(s.first()).To.Equal(Symbol.intern("fn*"));
            s = s.next();
            Expect(s.first()).To.Be.An.Instance.Of<IPersistentVector>();
            IPersistentVector arglist = s.first() as IPersistentVector;

            Expect(arglist.count()).To.Equal(0);

            s = s.next();
            Expect(s.first()).To.Be.An.Instance.Of<ISeq>();
            Expect(s.next()).To.Be.Null();

            ISeq form = s.first() as ISeq;

            Expect(form.count()).To.Equal(3);
            Expect(form.first()).To.Equal(Symbol.intern("+"));
            object o2 = form.next().first();
            Expect(o2).To.Be.An.Instance.Of<long>();
            Expect((long)o2).To.Equal(1);
            object o3 = form.next().next().first();
            Expect(o3).To.Be.An.Instance.Of<long>();
            Expect((long)o3).To.Equal(2);
        }

        [Test]
        public void SharpFnWithArgsGeneratesFnWithArgs()
        {
            object o1 = ReadFromString("#(+ %2 2)");
            // (fn* [p1__N p2__M] (+ p2__M 2))

            Expect(o1).To.Be.An.Instance.Of<ISeq>();
            ISeq s = o1 as ISeq;

            Expect(s.first()).To.Equal(Symbol.intern("fn*"));
            s = s.next();
            Expect(s.first()).To.Be.An.Instance.Of<IPersistentVector>();
            IPersistentVector arglist = s.first() as IPersistentVector;

            Expect(arglist.count()).To.Equal(2);
            Expect(arglist.nth(0)).To.Be.An.Instance.Of<Symbol>();
            Expect(arglist.nth(1)).To.Be.An.Instance.Of<Symbol>();
            Symbol arg1 = arglist.nth(0) as Symbol;
            Symbol arg2 = arglist.nth(1) as Symbol;
            Expect(arg1.Name).To.Start.With("p1__");
            Expect(arg2.Name).To.Start.With("p2__");

            s = s.next();
            Expect(s.first()).To.Be.An.Instance.Of<ISeq>();
            Expect(s.next()).To.Be.Null();

            ISeq form = s.first() as ISeq;

            Expect(form.count()).To.Equal(3);
            Expect(form.first()).To.Equal(Symbol.intern("+"));
            Expect(form.next().first()).To.Equal(arg2);
            o1 = form.next().next().first();
            Expect(o1).To.Be.An.Instance.Of<long>();
            Expect((long)o1).To.Equal(2);
        }

        [Test]
        public void SharpFnWithRestArgGeneratesFnWithRestArg()
        {
            object o1 = ReadFromString("#(+ %2 %&)");
            // (fn* [p1__N p2__M & rest__X] (+ p2__M rest__X))

            Expect(o1).To.Be.An.Instance.Of<ISeq>();
            ISeq s = o1 as ISeq;

            Expect(s.first()).To.Equal(Symbol.intern("fn*"));
            s = s.next();
            Expect(s.first()).To.Be.An.Instance.Of<IPersistentVector>();
            IPersistentVector arglist = s.first() as IPersistentVector;

            Expect(arglist.count()).To.Equal(4);
            Expect(arglist.nth(0)).To.Be.An.Instance.Of<Symbol>();
            Expect(arglist.nth(1)).To.Be.An.Instance.Of<Symbol>();
            Expect(arglist.nth(2)).To.Be.An.Instance.Of<Symbol>();
            Expect(arglist.nth(3)).To.Be.An.Instance.Of<Symbol>();
            Symbol arg1 = arglist.nth(0) as Symbol;
            Symbol arg2 = arglist.nth(1) as Symbol;
            Symbol arg3 = arglist.nth(2) as Symbol;
            Symbol arg4 = arglist.nth(3) as Symbol;
            Expect(arg1.Name).To.Start.With("p1__");
            Expect(arg2.Name).To.Start.With("p2__");
            Expect(arg3.Name).To.Equal("&");
            Expect(arg4.Name).To.Start.With("rest__");

            s = s.next();
            Expect(s.first()).To.Be.An.Instance.Of<ISeq>();
            Expect(s.next()).To.Be.Null();

            ISeq form = s.first() as ISeq;

            Expect(form.count()).To.Equal(3);
            Expect(form.first()).To.Equal(Symbol.intern("+"));
            Expect(form.next().first()).To.Equal(arg2);
            Expect(form.next().next().first()).To.Equal(arg4);
        }

        [Test]
        public void SharpFnWithAnonArgGeneratesFnWithArgs()
        {
            object o1 = ReadFromString("#(+ % 2)");
            // (fn* [p1__N] (+ p1__N 2))

            Expect(o1).To.Be.An.Instance.Of<ISeq>();
            ISeq s = o1 as ISeq;

            Expect(s.first()).To.Equal(Symbol.intern("fn*"));
            s = s.next();
            Expect(s.first()).To.Be.An.Instance.Of<IPersistentVector>();
            IPersistentVector arglist = s.first() as IPersistentVector;

            Expect(arglist.count()).To.Equal(1);
            Expect(arglist.nth(0)).To.Be.An.Instance.Of<Symbol>();
            Symbol arg1 = arglist.nth(0) as Symbol;
            Expect(arg1.Name).To.Start.With("p1__");

            s = s.next();
            Expect(s.first()).To.Be.An.Instance.Of<ISeq>();
            Expect(s.next()).To.Be.Null();

            ISeq form = s.first() as ISeq;

            Expect(form.count()).To.Equal(3);
            Expect(form.first()).To.Equal(Symbol.intern("+"));
            Expect(form.next().first()).To.Equal(arg1);
            o1 = form.next().next().first();
            Expect(o1).To.Be.An.Instance.Of<long>();
            Expect((long)o1).To.Equal(2);
        }

        [Test]
        public void ArgReaderOutsideSharpFnReturnsSymbolAsIs()
        {
            object o1 = ReadFromString("%2");

            Expect(o1).To.Be.An.Instance.Of<Symbol>();
            Expect(((Symbol)o1).Name).To.Equal("%2");
            Expect(((Symbol)o1).Namespace).To.Be.Null();

        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void ArgReaderFollowedByNonNumericFails()
        {
            ReadFromString("#(+ %a 2)");
        }



        #endregion

        #region Eval reader tests

        // TODO: EvalReader tests


        #endregion

    }
}
