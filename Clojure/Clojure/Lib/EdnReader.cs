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
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace clojure.lang
{
    public static class EdnReader
    {
        #region Symbol definitions

        static readonly Keyword EOF = Keyword.intern(null,"eof");

        #endregion

        #region Macro characters & #-dispatch

        static IFn _taggedReader = new TaggedReader();

        static IFn[] _macros = new IFn[256];
        static IFn[] _dispatchMacros = new IFn[256];

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static EdnReader()
        {
            _macros['"'] = new StringReader();
	        _macros[';'] = new CommentReader();
	        _macros['^'] = new MetaReader();
	        _macros['('] = new ListReader();
	        _macros[')'] = new UnmatchedDelimiterReader();
	        _macros['['] = new VectorReader();
	        _macros[']'] = new UnmatchedDelimiterReader();
	        _macros['{'] = new MapReader();
	        _macros['}'] = new UnmatchedDelimiterReader();
	        _macros['\\'] = new CharacterReader();
	        _macros['#'] = new DispatchReader();

	        _dispatchMacros['^'] = new MetaReader();
	        //_dispatchMacros['"'] = new RegexReader();
	        _dispatchMacros['{'] = new SetReader();
	        _dispatchMacros['<'] = new UnreadableReader();
	        _dispatchMacros['_'] = new DiscardReader();
        }

        static bool isMacro(int ch)
        {
            return ch < _macros.Length && _macros[ch] != null;
        }

        static IFn getMacro(int ch)
        {
            return ch < _macros.Length ? _macros[ch] : null;
        }

        static bool isTerminatingMacro(int ch)
        {
            return (ch != '#' && ch != '\'' && ch < _macros.Length && _macros[ch] != null);
        }

        #endregion

        #region main entry points - readString, read

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "read")]
        static public Object readString(String s, IPersistentMap opts)
        {
            PushbackTextReader r = new PushbackTextReader(new System.IO.StringReader(s));
            return read(r, opts);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "read")]
        public static Object read(PushbackTextReader r, IPersistentMap opts)
        {
            return read(r, !opts.containsKey(EOF), opts.valAt(EOF), false, opts);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        public static object read(PushbackTextReader r,
            bool eofIsError,
            object eofValue,
            bool isRecursive,
            Object opts)
        {
            try
            {
                for (; ; )
                {
                    int ch = r.Read();

                    while (isWhitespace(ch))
                        ch = r.Read();

                    if (ch == -1)
                    {
                        if (eofIsError)
                            throw new EndOfStreamException("EOF while reading");
                        return eofValue;
                    }

                    if (Char.IsDigit((char)ch))
                    {
                        object n = readNumber(r, (char)ch);
                        return RT.suppressRead() ? null : n;
                    }

                    IFn macroFn = getMacro(ch);
                    if (macroFn != null)
                    {
                        object ret = macroFn.invoke(r, (char)ch, opts);
                        if (RT.suppressRead())
                            return null;
                        // no op macros return the reader
                        if (ret == r)
                            continue;
                        return ret;
                    }

                    if (ch == '+' || ch == '-')
                    {
                        int ch2 = r.Read();
                        if (Char.IsDigit((char)ch2))
                        {
                            Unread(r, ch2);
                            object n = readNumber(r, (char)ch);
                            return RT.suppressRead() ? null : n;
                        }
                        Unread(r, ch2);
                    }

                    //string token = readToken(r, (char)ch);
                    //return RT.suppressRead() ? null : interpretToken(token);
                    string rawToken;
                    string token;
                    string mask;
                    bool eofSeen;
                    readToken(r, (char)ch, true, out rawToken, out token, out mask, out eofSeen);
                    if (eofSeen)
                    {
                        if (eofIsError)
                            throw new EndOfStreamException("EOF while reading symbol");
                        return eofValue;
                    }
                    return RT.suppressRead() ? null : InterpretToken(rawToken, token, mask);
                }
            }
            catch (Exception e)
            {
                if (isRecursive)
                    throw;

                LineNumberingTextReader lntr = r as LineNumberingTextReader;
                if (lntr == null)
                    throw;

                throw new ReaderException(lntr.LineNumber, lntr.ColumnNumber, e);
            }
        }

        private static object ReadAux(PushbackTextReader r, object opts)
        {
            return read(r, true, null, true, opts);
        }

        #endregion

        #region Character hacking

        static void Unread(PushbackTextReader r, int ch)
        {
            if (ch != -1)
                r.Unread(ch);
        }

        static bool isWhitespace(int ch)
        {
            return Char.IsWhiteSpace((char)ch) || ch == ',';
        }

        static bool NonConstituent(int ch)
        {
            return ch == '@' || ch == '`' || ch == '~';
        }


        // Roughly a match to Java Character.digit(char,int),
        // though I don't handle all unicode digits.
        static int CharValueInRadix(int c, int radix)
        {
            if (char.IsDigit((char)c))
                return c - '0' < radix ? c - '0' : -1;

            if ('A' <= c && c <= 'Z')
                return c - 'A' < radix - 10 ? c - 'A' + 10 : -1;

            if ('a' <= c && c <= 'z')
                return c - 'a' < radix - 10 ? c - 'a' + 10 : -1;

            return -1;
        }

        static int readUnicodeChar(string token, int offset, int length, int radix)
        {
            if (token.Length != offset + length)
                throw new ArgumentException("Invalid unicode character: \\" + token);
            int uc = 0;
            for (int i = offset; i < offset + length; ++i)
            {
                int d = CharValueInRadix(token[i], radix);
                if (d == -1)
                    throw new ArgumentException("Invalid digit: " + token[i]);
                uc = uc * radix + d;
            }
            return (char)uc;
        }

        static int readUnicodeChar(PushbackTextReader r, int initch, int radix, int length, bool exact)
        {

            int uc = CharValueInRadix(initch, radix);
            if (uc == -1)
                throw new ArgumentException("Invalid digit: " + (char)initch);
            int i = 1;
            for (; i < length; ++i)
            {
                int ch = r.Read();
                if (ch == -1 || isWhitespace(ch) || isMacro(ch))
                {
                    Unread(r, ch);
                    break;
                }
                int d = CharValueInRadix(ch, radix);
                if (d == -1)
                    throw new ArgumentException("Invalid digit: " + (char)ch);
                uc = uc * radix + d;
            }
            if (i != length && exact)
                throw new ArgumentException("Invalid character length: " + i + ", should be: " + length);
            return uc;
        }

        #endregion

        #region  Other

        static List<Object> ReadDelimitedList(char delim, PushbackTextReader r, bool isRecursive, object opts)
        {
            LineNumberingTextReader lntr = r as LineNumberingTextReader;
            int firstLine = lntr != null ? lntr.LineNumber : -1;

            List<Object> a = new List<object>();

            for (; ; )
            {
                int ch = r.Read();

                while (isWhitespace(ch))
                    ch = r.Read();

                if (ch == -1)
                {
                    if (firstLine < 0)
                        throw new EndOfStreamException("EOF while reading");
                    else
                        throw new EndOfStreamException("EOF while reading, starting at line " + firstLine);
                }

                if (ch == delim)
                {
                    break;
                }

                IFn macroFn = getMacro(ch);
                if (macroFn != null)
                {
                    Object mret = macroFn.invoke(r, (char)ch, opts);
                    //no op macros return the reader
                    if (mret != r)
                        a.Add(mret);
                }
                else
                {
                    Unread(r, ch);
                    object o = read(r, true, null, isRecursive, opts);
                    if (o != r)
                        a.Add(o);
                }
            }

            return a;
        }

        #endregion

        #region Reading tokens

        static string readSimpleToken(PushbackTextReader r, char initch, bool leadConstituent)
        {
            if (leadConstituent && NonConstituent(initch))
                throw new InvalidOperationException("Invalid leading characters: " + (char)initch);

            StringBuilder sb = new StringBuilder();
            sb.Append(initch);

            for (; ; )
            {
                int ch = r.Read();
                if (ch == -1 || isWhitespace(ch) || isTerminatingMacro(ch))
                {
                    Unread(r, ch);
                    return sb.ToString();
                }
                sb.Append((char)ch);
            }
        }

        static void readToken(PushbackTextReader r, char initch, bool leadConstituent, out String rawToken, out String token, out String mask, out bool eofSeen)
        {
            if (leadConstituent && NonConstituent(initch))
                throw new InvalidOperationException("Invalid leading characters: " + (char)initch);

            bool allowSymEscape = RT.booleanCast(RT.AllowSymbolEscapeVar.deref());

            bool rawMode = false;

            StringBuilder sbRaw = new StringBuilder();
            StringBuilder sbToken = new StringBuilder();
            StringBuilder sbMask = new StringBuilder();

            if (allowSymEscape && initch == '|')
            {
                rawMode = true;
                sbRaw.Append(initch);
            }
            else
            {
                sbRaw.Append(initch);
                sbToken.Append(initch);
                sbMask.Append(initch);
            }

            for (; ; )
            {
                int ch = r.Read();
                if (rawMode)
                {
                    if (ch == -1)
                    {
                        rawToken = sbRaw.ToString();
                        token = sbToken.ToString();
                        mask = sbMask.ToString();
                        eofSeen = true;
                        return;
                    }
                    if (ch == '|')
                    {
                        int ch2 = r.Read();
                        if (ch2 == '|')
                        {
                            sbRaw.Append('|');
                            sbToken.Append('|');
                            sbMask.Append('a');
                        }
                        else
                        {
                            r.Unread(ch2);
                            rawMode = false;
                            sbRaw.Append(ch);
                        }
                    }
                    else
                    {
                        sbRaw.Append((char)ch);
                        sbToken.Append((char)ch);
                        sbMask.Append('a');
                    }
                }
                else
                {
                    if (ch == -1 || isWhitespace(ch) || isTerminatingMacro(ch))
                    {
                        Unread(r, ch);
                        rawToken = sbRaw.ToString();
                        token = sbToken.ToString();
                        mask = sbMask.ToString();
                        eofSeen = false;
                        return;
                    }
                    else if (NonConstituent(ch))
                        throw new InvalidOperationException("Invalid constituent character: " + (char)ch);
                    else if (ch == '|' && allowSymEscape)
                    {
                        rawMode = true;
                        sbRaw.Append((char)ch);
                    }
                    else
                    {
                        sbRaw.Append((char)ch);
                        sbToken.Append((char)ch);
                        sbMask.Append((char)ch);
                    }
                }
            }
        }

        public static object InterpretToken(string token)
        {
            return InterpretToken(token, token, token);
        }

        public static object InterpretToken(string rawToken, string token, string mask)
        {
            if (token.Equals("nil"))
            {
                return null;
            }
            else if (token.Equals("true"))
            {
                //return RT.T;
                return true;
            }
            else if (token.Equals("false"))
            {
                //return RT.F;
                return false;
            }

            object ret = matchSymbol(token, mask);
            if (ret != null)
                return ret;

            throw new ArgumentException("Invalid token: " + rawToken);
        }


        static Regex symbolPat = new Regex("^[:]?([^\\p{Nd}/].*/)?(/|[^\\p{Nd}/][^/]*)$");
        static Regex keywordPat = new Regex("^[:]?([^/].*/)?(/|[^/][^/]*)$");

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "maskName")]
        private static void ExtractNamesUsingMask(string token, string maskNS, string maskName, out string ns, out string name)
        {
            if (String.IsNullOrEmpty(maskNS))
            {
                ns = null;
                name = token;
            }
            else
            {
                ns = token.Substring(0, maskNS.Length - 1);
                name = token.Substring(maskNS.Length);
            }
        }

        static object matchSymbol(string token, string mask)
         {
            Match m = symbolPat.Match(mask);

            if (m.Success)
            {
                string maskNS = m.Groups[1].Value;
                string maskName = m.Groups[2].Value;
                if (maskNS != null && maskNS.EndsWith(":/")
                    || maskName.EndsWith(":")
                    || mask.IndexOf("::", 1) != -1)
                    return null;

                if (mask.StartsWith("::"))
                    return null;

                bool isKeyword = mask[0] == ':';

                if (isKeyword)
                {
                    Match m2 = keywordPat.Match(mask.Substring(1));
                    if (!m2.Success)
                        return null;
                    string ns;
                    string name;
                    ExtractNamesUsingMask(token.Substring(1), m2.Groups[1].Value, m2.Groups[2].Value, out ns, out name);
                    return Keyword.intern(ns, name); 
                }
                else
                {
                    string ns;
                    string name;
                    ExtractNamesUsingMask(token, maskNS, maskName, out ns, out name);
                    return Symbol.intern(ns, name);
                }
            }

            return null;
        }

        #endregion

        #region Reading numbers

        static Regex intRE = new Regex("^([-+]?)(?:(0)|([1-9][0-9]*)|0[xX]([0-9A-Fa-f]+)|0([0-7]+)|([1-9][0-9]?)[rR]([0-9A-Za-z]+)|0[0-9]+)(N)?$");
        static Regex ratioRE = new Regex("^([-+]?[0-9]+)/([0-9]+)$");
        static Regex floatRE = new Regex("^([-+]?[0-9]+(\\.[0-9]*)?([eE][-+]?[0-9]+)?)(M)?$");

        static object readNumber(PushbackTextReader r, char initch)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(initch);

            for (; ; )
            {
                int ch = r.Read();
                if (ch == -1 || isWhitespace(ch) || isMacro(ch))
                {
                    Unread(r, ch);
                    break;
                }
                sb.Append((char)ch);
            }

            string s = sb.ToString();
            object n = MatchNumber(s);
            if (n == null)
                throw new FormatException("Invalid number: " + s);
            return n;
        }

        public static object MatchNumber(string s)
        {
            Match m = intRE.Match(s);
            if (m.Success)
            {
                if (m.Groups[2].Success)
                {
                    // matched 0  or 0N only
                    if (m.Groups[8].Success)
                        return BigInt.ZERO;
                    return 0L;
                }
                bool isNeg = m.Groups[1].Value == "-";
                string n = null;
                int radix = 10;
                if (m.Groups[3].Success)
                {
                    n = m.Groups[3].Value;
                    radix = 10;
                }
                else if (m.Groups[4].Success)
                {
                    n = m.Groups[4].Value;
                    radix = 16;
                }
                else if (m.Groups[5].Success)
                {
                    n = m.Groups[5].Value;
                    radix = 8;
                }
                else if (m.Groups[7].Success)
                {
                    n = m.Groups[7].Value;
                    radix = Int32.Parse(m.Groups[6].Value, System.Globalization.CultureInfo.InvariantCulture);
                }
                if (n == null)
                    return null;

                BigInteger bn = BigInteger.Parse(n, radix);
                if (isNeg)
                    bn = bn.Negate();

                if (m.Groups[8].Success) // N suffix
                    return BigInt.fromBigInteger(bn);

                long ln;
                if (bn.AsInt64(out ln))
                    return Numbers.num(ln);

                return BigInt.fromBigInteger(bn);
            }

            m = floatRE.Match(s);

            if (m.Success)
            {
                if (m.Groups[4].Success)
                {
                    string val = m.Groups[1].Value;
                    // MS implementation of java.util.BigDecimal has a bug when the string has a leading+
                    //if ( val[0] == '+' )
                    //    val = val.Substring(1);
                    return BigDecimal.Parse(val);
                }
                return (object)Double.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
            }
            m = ratioRE.Match(s);
            if (m.Success)
            {
                // There is a bug in the BigInteger c-tor that causes it barf on a leading +.
                string numerString = m.Groups[1].Value;
                string denomString = m.Groups[2].Value;
                if (numerString[0] == '+')
                    numerString = numerString.Substring(1);
                //return Numbers.BIDivide(new BigInteger(numerString), new BigInteger(denomString));
                return Numbers.divide(
                    Numbers.ReduceBigInt(BigInt.fromBigInteger(BigInteger.Parse(numerString))),
                    Numbers.ReduceBigInt(BigInt.fromBigInteger(BigInteger.Parse(denomString))));
            }
            return null;
        }

        #endregion

        #region Readers

        public abstract class ReaderBase : AFn
        {
            public override object invoke(object arg1, object arg2, object arg3)
            {
                return Read((PushbackTextReader)arg1, (Char)arg2, arg3);
            }

            protected abstract object Read(PushbackTextReader r, char c, object opts);
        }

        #region CharacterReader

        public sealed class CharacterReader : ReaderBase
        {
            protected override object Read(PushbackTextReader r, char backslash, object opts)
            {
                int ch = r.Read();
                if (ch == -1)
                    throw new EndOfStreamException("EOF while reading character");
                String token = readSimpleToken(r, (char)ch, false);
                if (token.Length == 1)
                    return token[0];
                else if (token.Equals("newline"))
                    return '\n';
                else if (token.Equals("space"))
                    return ' ';
                else if (token.Equals("tab"))
                    return '\t';
                else if (token.Equals("backspace"))
                    return '\b';
                else if (token.Equals("formfeed"))
                    return '\f';
                else if (token.Equals("return"))
                    return '\r';
                else if (token.StartsWith("u"))
                {
                    char c = (char)readUnicodeChar(token, 1, 4, 16);
                    if (c >= '\uD800' && c <= '\uDFFF') // surrogate code unit?
                        throw new InvalidOperationException("Invalid character constant: \\u" + ((int)c).ToString("x"));
                    return c;
                }
                else if (token.StartsWith("o"))
                {
                    int len = token.Length - 1;
                    if (len > 3)
                        throw new InvalidOperationException("Invalid octal escape sequence length: " + len);
                    int uc = readUnicodeChar(token, 1, len, 8);
                    if (uc > 255) //octal377
                        throw new InvalidOperationException("Octal escape sequence must be in range [0, 377].");
                    return (char)uc;
                }
                throw new InvalidOperationException("Unsupported character: \\" + token);
            }
        }

        #endregion

        #region String Reader

        public sealed class StringReader : ReaderBase
        {
            protected override object Read(PushbackTextReader r, char doublequote, object opts)
            {
                StringBuilder sb = new StringBuilder();

                for (int ch = r.Read(); ch != '"'; ch = r.Read())
                {
                    if (ch == -1)
                        throw new EndOfStreamException("EOF while reading string");
                    if (ch == '\\')	//escape
                    {
                        ch = r.Read();
                        if (ch == -1)
                            throw new EndOfStreamException("EOF while reading string");
                        switch (ch)
                        {
                            case 't':
                                ch = '\t';
                                break;
                            case 'r':
                                ch = '\r';
                                break;
                            case 'n':
                                ch = '\n';
                                break;
                            case '\\':
                                break;
                            case '"':
                                break;
                            case 'b':
                                ch = '\b';
                                break;
                            case 'f':
                                ch = '\f';
                                break;
                            case 'u':
                                ch = r.Read();
                                if (CharValueInRadix(ch, 16) == -1)
                                    throw new InvalidOperationException("Invalid unicode escape: \\u" + (char)ch);
                                ch = readUnicodeChar((PushbackTextReader)r, ch, 16, 4, true);
                                break;
                            default:
                                {
                                    //if (CharValueInRadix(ch, 8) != -1)  -- this is correct, but we end up with different error message for 8,9 than JVM, so do the following to match:
                                    if (Char.IsDigit((char)ch))
                                    {
                                        ch = readUnicodeChar((PushbackTextReader)r, ch, 8, 3, false);
                                        if (ch > 255) //octal377
                                            throw new InvalidOperationException("Octal escape sequence must be in range [0, 377].");
                                    }
                                    else
                                        throw new InvalidOperationException("Unsupported escape character: \\" + (char)ch);
                                }
                                break;
                        }
                    }
                    sb.Append((char)ch);
                }
                return sb.ToString();
            }
        }

        #endregion

        #region Discard/Comment readers

        public sealed class CommentReader : ReaderBase
        {
            protected override object Read(PushbackTextReader r, char semicolon, object opts)
            {
                int ch;
                do
                {
                    ch = r.Read();
                } while (ch != -1 && ch != '\n' && ch != '\r');
                return r;
            }
        }

        public sealed class DiscardReader : ReaderBase
        {
            protected override object Read(PushbackTextReader r, char underscore, object opts)
            {
                ReadAux(r,opts);
                return r;
            }
        }

        #endregion

        #region DispatchReader

        public sealed class DispatchReader : ReaderBase
        {
            protected override object Read(PushbackTextReader r, char hash, object opts)
            {
                int ch = r.Read();
                if (ch == -1)
                    throw new EndOfStreamException("EOF while reading character");
                IFn fn = _dispatchMacros[ch];
                if (fn == null)
                {
                    // try tagged reader
                    if (Char.IsLetter((char)ch))
                    {
                        Unread(r, ch);
                        return _taggedReader.invoke(r,(char)ch, opts);
                    }
                    throw new InvalidOperationException(String.Format("No dispatch macro for: {0}", (char)ch));
                }
                return fn.invoke(r, (char)ch, opts);
            }
        }

        #endregion

        #region MetaReader

        public sealed class MetaReader : ReaderBase
        {

            protected override object Read(PushbackTextReader r, char caret, object opts)
            {
                int startLine = -1;
                int startCol = -1;
                LineNumberingTextReader lntr = r as LineNumberingTextReader;

                if (lntr != null)
                {
                    startLine = lntr.LineNumber;
                    startCol = lntr.ColumnNumber;
                }

                IPersistentMap metaAsMap;
                {
                    object meta = ReadAux(r,opts);

                    if (meta is Symbol || meta is String)
                        metaAsMap = RT.map(RT.TagKey, meta);
                    else if (meta is Keyword)
                        metaAsMap = RT.map(meta, true);
                    else if ((metaAsMap = meta as IPersistentMap) == null)
                        throw new ArgumentException("Metadata must be Symbol,Keyword,String or Map");
                }

                object o = ReadAux(r,opts);
                if (o is IMeta)
                {
                    if (startLine != -1 && o is ISeq)
                        metaAsMap = metaAsMap.assoc(RT.LineKey, startLine)
                            .assoc(RT.ColumnKey, startCol)
                            .assoc(RT.SourceSpanKey, RT.map(
                                RT.StartLineKey, startLine,
                                RT.StartColumnKey, startCol,
                                RT.EndLineKey, lntr.LineNumber,
                                RT.EndColumnKey, lntr.ColumnNumber));

                    IReference iref = o as IReference;
                    if (iref != null)
                    {
                        iref.resetMeta(metaAsMap);
                        return o;
                    }
                    object ometa = RT.meta(o);
                    for (ISeq s = RT.seq(metaAsMap); s != null; s = s.next())
                    {
                        IMapEntry kv = (IMapEntry)s.first();
                        ometa = RT.assoc(ometa, kv.key(), kv.val());
                    }
                    return ((IObj)o).withMeta((IPersistentMap)ometa);
                }
                else
                    throw new ArgumentException("Metadata can only be applied to IMetas");
            }
        }

        #endregion

        #region Collection readers

        public sealed class ListReader : ReaderBase
        {
            protected override object Read(PushbackTextReader r, char leftparen, object opts)
            {
                //int startLine = -1;
                //int startCol = -1;
                //LineNumberingTextReader lntr = r as LineNumberingTextReader;

                //if (lntr != null)
                //{
                //    startLine = lntr.LineNumber;
                //    startCol = lntr.ColumnNumber;
                //}
                IList<Object> list = ReadDelimitedList(')', r, true, opts);
                if (list.Count == 0)
                    return PersistentList.EMPTY;
                IObj s = (IObj)PersistentList.create((IList)list);
                return s;
            }
        }

        public sealed class VectorReader : ReaderBase
        {
            protected override object Read(PushbackTextReader r, char leftparen, object opts)
            {
                return LazilyPersistentVector.create(ReadDelimitedList(']', r, true, opts));
            }
        }

        public sealed class MapReader : ReaderBase
        {
            protected override object Read(PushbackTextReader r, char leftbrace, object opts)
            {
                Object[] a = ReadDelimitedList('}', r, true, opts).ToArray();
                if ((a.Length & 1) == 1)
                    throw new ArgumentException("Map literal must contain an even number of forms");
                return RT.map(a);
            }
        }

        public sealed class SetReader : ReaderBase
        {
            protected override object Read(PushbackTextReader r, char leftbracket, object opts)
            {
                return PersistentHashSet.createWithCheck(ReadDelimitedList('}', r, true, opts));
            }
        }

        public sealed class UnmatchedDelimiterReader : ReaderBase
        {
            protected override object Read(PushbackTextReader reader, char rightdelim, object opts)
            {
                throw new ArgumentException("Unmatched delimiter: " + rightdelim);
            }
        }

        #endregion

        #region UnreadableReader

        public sealed class UnreadableReader : ReaderBase
        {
            protected override object Read(PushbackTextReader reader, char leftangle, object opts)
            {
                throw new ArgumentException("Unreadable form");
            }
        }

        #endregion

        #region TaggedReader

        public sealed class TaggedReader : ReaderBase
        {
            protected override object Read(PushbackTextReader r, char leftparen, object opts)
            {
                object name = read(r, true, null, false, opts);
                Symbol sym = name as Symbol;
                if (sym == null)
                    throw new InvalidOperationException("Reader tag must be a symbol");
                return ReadTagged(r, sym, (IPersistentMap)opts);
            }

            static readonly Keyword READERS = Keyword.intern(null, "readers");
            static readonly Keyword DEFAULT = Keyword.intern(null, "default");

            private static object ReadTagged(PushbackTextReader r, Symbol tag, IPersistentMap opts)
            {
                object o = ReadAux(r, opts);

                ILookup readers = (ILookup)RT.get(opts, READERS);
                IFn dataReader = (IFn)RT.get(readers, tag);
                if (dataReader == null)
                    dataReader = (IFn)RT.get(RT.DefaultDataReadersVar.deref(), tag);
                if (dataReader == null)
                {
                    IFn defaultReader = (IFn)RT.get(opts, DEFAULT);
                    if (defaultReader != null)
                        return defaultReader.invoke(tag, o);
                    else
                        throw new InvalidOperationException("No reader function for tag " + tag.ToString());
                }
                else
                    return dataReader.invoke(o);
            }
        }

        #endregion

        #endregion

        #region ReaderException

        [Serializable]
        public sealed class ReaderException : Exception
        {
            readonly int _line;

            public int Line
            {
                get { return _line; }
            }

            readonly int _column;

            public int Column
            {
                get { return _column; }
            }

            public ReaderException(int line, int column, Exception e)
                : base(null, e)
            {
                _line = line;
                _column = column;
            }

            public ReaderException()
            {
                _line = -1;
                _column = -1;
            }

            public ReaderException(string msg)
                : base(msg)
            {
                _line = -1;
                _column = -1;
            }

            public ReaderException(string msg, Exception innerException)
                : base(msg, innerException)
            {
                _line = -1;
                _column = -1;
            }

            private ReaderException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
                _line = info.GetInt32("Line");
                _column = info.GetInt32("Column");
            }

            [System.Security.SecurityCritical]
            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                if (info == null)
                {
                    throw new ArgumentNullException("info");
                }
                base.GetObjectData(info, context);
                info.AddValue("Line", this._line, typeof(int));
                info.AddValue("Column", this._column, typeof(int));
            }
        }

        #endregion
    }
}

