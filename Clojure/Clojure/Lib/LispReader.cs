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
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
//using BigDecimal = java.math.BigDecimal;

namespace clojure.lang
{
    /// <summary>
    /// Implements the Lisp reader, a marvel to behold.
    /// </summary>
    public static class LispReader
    {
        #region Symbol definitions

        static readonly Symbol QUOTE = Symbol.intern("quote");
        static readonly Symbol THE_VAR = Symbol.intern("var");
        static readonly Symbol UNQUOTE = Symbol.intern("clojure.core", "unquote");
        static readonly Symbol UNQUOTE_SPLICING = Symbol.intern("clojure.core", "unquote-splicing");
        static readonly Symbol DEREF = Symbol.intern("clojure.core", "deref");
        //static readonly Symbol READ_COND = Symbol.intern("clojure.core", "read-cond");
        //static readonly Symbol READ_COND_SPLICING = Symbol.intern("clojure.core", "read-cond-splicing");
        //static readonly Symbol META = Symbol.intern("clojure.core", "meta");
        static readonly Symbol APPLY = Symbol.intern("clojure.core", "apply");
        static readonly Symbol CONCAT = Symbol.intern("clojure.core", "concat");
        static readonly Symbol HASHMAP = Symbol.intern("clojure.core", "hash-map");
        static readonly Symbol HASHSET = Symbol.intern("clojure.core", "hash-set");
        static readonly Symbol VECTOR = Symbol.intern("clojure.core", "vector");
        static readonly Symbol WITH_META = Symbol.intern("clojure.core", "with-meta");
        static readonly Symbol LIST = Symbol.intern("clojure.core", "list");
        static readonly Symbol SEQ = Symbol.intern("clojure.core", "seq");

        static readonly Keyword UNKNOWN = Keyword.intern(null, "unknown");

        #endregion

        #region Var environments

        //symbol->gensymbol
        /// <summary>
        /// Dynamically bound var to a map from <see cref="Symbol">Symbol</see>s to ...
        /// </summary>
        static readonly Var GENSYM_ENV = Var.create(null).setDynamic();

        //sorted-map num->gensymbol
        static readonly Var ARG_ENV = Var.create(null).setDynamic();

        // Dynamic var set to true in a read-cond context
        static readonly Var READ_COND_ENV = Var.create(null).setDynamic();

        static IFn _ctorReader = new CtorReader();

        #endregion

        #region Macro characters & #-dispatch

        static IFn[] _macros = new IFn[256];
        static IFn[] _dispatchMacros = new IFn[256];

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static LispReader()
        {
            _macros['"'] = new StringReader();
            _macros[';'] = new CommentReader();
            _macros['\''] = new WrappingReader(QUOTE);
            _macros['@'] = new WrappingReader(DEREF);//new DerefReader();
            _macros['^'] = new MetaReader();
            _macros['`'] = new SyntaxQuoteReader();
            _macros['~'] = new UnquoteReader();
            _macros['('] = new ListReader();
            _macros[')'] = new UnmatchedDelimiterReader();
            _macros['['] = new VectorReader();
            _macros[']'] = new UnmatchedDelimiterReader();
            _macros['{'] = new MapReader();
            _macros['}'] = new UnmatchedDelimiterReader();
            ////	macros['|'] = new ArgVectorReader();            
            _macros['\\'] = new CharacterReader();
            _macros['%'] = new ArgReader();
            _macros['#'] = new DispatchReader();


            _dispatchMacros['^'] = new MetaReader();
            _dispatchMacros['\''] = new VarReader();
            _dispatchMacros['"'] = new RegexReader();
            _dispatchMacros['('] = new FnReader();
            _dispatchMacros['{'] = new SetReader();
            _dispatchMacros['='] = new EvalReader();
            _dispatchMacros['!'] = new CommentReader();
            _dispatchMacros['<'] = new UnreadableReader();
            _dispatchMacros['_'] = new DiscardReader();
            _dispatchMacros['?'] = new ConditionalReader();
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
            return (ch != '#' && ch != '\'' && ch != '%' && isMacro(ch));
        }

        #endregion

        #region main entry point -- read


        // Reader opts

        internal static readonly Keyword OPT_EOF = Keyword.intern(null, "eof");
        internal static readonly Keyword OPT_FEATURES = Keyword.intern(null, "features");
        internal static readonly Keyword OPT_READ_COND = Keyword.intern(null, "read-cond");

        // EOF special value to throw on eof
        static readonly Keyword EOFTHROW = Keyword.intern(null, "eofthrow");

        // Platform features - always installed
        static readonly Keyword PLATFORM_KEY = Keyword.intern(null, "cljr");
        static readonly Object PLATFORM_FEATURES = PersistentHashSet.create(PLATFORM_KEY);

        // Reader conditional options - use with :read-cond
        internal static readonly Keyword COND_ALLOW = Keyword.intern(null, "allow");
        internal static readonly Keyword COND_PRESERVE = Keyword.intern(null, "preserve");

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        static public Object read(PushbackTextReader r, Object opts)
        {
            bool eofIsError = true;
            object eofValue = null;
            IPersistentMap optsMap = opts as IPersistentMap;
            if (optsMap != null)
            {
                Object eof = optsMap.valAt(OPT_EOF, EOFTHROW);
                if (!EOFTHROW.Equals(eof))
                {
                    eofIsError = false;
                    eofValue = eof;
                }
            }
            return read(r, eofIsError, eofValue, false, opts);
        }
        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        public static object read(PushbackTextReader r,
            bool eofIsError,
            object eofValue,
            bool isRecursive)
        {
            return read(r, eofIsError, eofValue, isRecursive, PersistentHashMap.EMPTY);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        static public Object read(PushbackTextReader r, bool eofIsError, object eofValue, bool isRecursive, object opts)
        {
            return read(r, eofIsError, eofValue, null, null, isRecursive, opts, null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        static private Object read(PushbackTextReader r, bool eofIsError, Object eofValue, bool isRecursive, Object opts, Object pendingForms)
        {
            return read(r, eofIsError, eofValue, null, null, isRecursive, opts, EnsurePending(pendingForms));
        }

        static Object EnsurePending(object pendingForms)
        {
            if (pendingForms == null)
                return new LinkedList<Object>();
            else
                return pendingForms;
        }

        static private Object InstallPlatformFeature(Object opts)
        {
            if (opts == null)
                return RT.mapUniqueKeys(LispReader.OPT_FEATURES, PLATFORM_FEATURES);
            else
            {
                IPersistentMap mopts = (IPersistentMap)opts;
                Object features = mopts.valAt(OPT_FEATURES);
                if (features == null)
                    return mopts.assoc(LispReader.OPT_FEATURES, PLATFORM_FEATURES);
                else
                    return mopts.assoc(LispReader.OPT_FEATURES, RT.conj((IPersistentSet)features, PLATFORM_KEY));
            }
        }

        static private Object read(PushbackTextReader r, bool eofIsError, object eofValue, char? returnOn, object returnOnValue, bool isRecursive, object opts, object pendingForms)
        {
            if (UNKNOWN.Equals(RT.ReadEvalVar.deref()))
                throw new InvalidOperationException("Reading disallowed - *read-eval* bound to :unknown");

            opts = InstallPlatformFeature(opts);

            try
            {
                for (; ; )
                {
                    var pfl = pendingForms as LinkedList<Object>;
                    if (pfl != null && pfl.Count != 0)
                    {
                        object val = pfl.First.Value;
                        pfl.RemoveFirst();
                        return val;
                    }

                    int ch = r.Read();

                    while (isWhitespace(ch))
                        ch = r.Read();

                    if (ch == -1)
                    {
                        if (eofIsError)
                            throw new EndOfStreamException("EOF while reading");
                        return eofValue;
                    }

                    if (returnOn.HasValue && (returnOn.Value == ch))
                    {
                        return returnOnValue;
                    }

                    if (Char.IsDigit((char)ch))
                    {
                        object n = readNumber(r, (char)ch);
                        return n;
                    }

                    IFn macroFn = getMacro(ch);
                    if (macroFn != null)
                    {
                        object ret = macroFn.invoke(r, (char)ch, opts, pendingForms);
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
                            return n;
                        }
                        Unread(r, ch2);
                    }

                    //string token = readToken(r, (char)ch);
                    //return RT.suppressRead() ? null : interpretToken(token);
                    string rawToken;
                    string token;
                    string mask;
                    bool eofSeen;
                    readToken(r, (char)ch, out rawToken, out token, out mask, out eofSeen);
                    if (eofSeen)
                    {
                        if (eofIsError)
                            throw new EndOfStreamException("EOF while reading symbol");
                        return eofValue;
                    }
                    return InterpretToken(rawToken, token, mask);
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

        private static object ReadAux(PushbackTextReader r, object opts, object pendingForms)
        {
            return read(r, true, null, true, opts, pendingForms);
        }

        #endregion

        #region Character hacking

        static internal void Unread(PushbackTextReader r, int ch)
        {
            if (ch != -1)
                r.Unread(ch);
        }

        static internal bool isWhitespace(int ch)
        {
            return Char.IsWhiteSpace((char)ch) || ch == ',';
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

        // Sentinel values for reading lists
        static readonly object READ_EOF = new object();
        static readonly object READ_FINISHED = new object();

        static List<Object> ReadDelimitedList(char delim, PushbackTextReader r, bool isRecursive, object opts, object pendingForms)
        {
            LineNumberingTextReader lntr = r as LineNumberingTextReader;
            int firstLine = lntr != null ? lntr.LineNumber : -1;

            List<Object> a = new List<object>();

            for (; ; )
            {

                Object form = read(r, false, READ_EOF, delim, READ_FINISHED, isRecursive, opts, pendingForms);

                if (form == READ_EOF)
                {
                    if (firstLine < 0)
                        throw new EndOfStreamException("EOF while reading");
                    else
                        throw new EndOfStreamException("EOF while reading, starting at line " + firstLine);
                }
                else if (form == READ_FINISHED)
                {
                    return a;
                }

                a.Add(form);
            }
        }

        static Symbol garg(int n)
        {
            return Symbol.intern(null, (n == -1 ? "rest" : ("p" + n)) + "__" + RT.nextID() + "#");
        }


        #endregion

        #region Reading tokens

        static string readSimpleToken(PushbackTextReader r, char initch)
        {
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


        static void readToken(PushbackTextReader r, char initch, out String rawToken, out String token, out String mask, out bool eofSeen)
        {
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
                return null;
            else if (token.Equals("true"))
                return true;  // RT.T
            else if (token.Equals("false"))
                return false; // RT.F;

            object ret = matchSymbol(token, mask);

            if (ret != null)
                return ret;

            throw new ArgumentException("Invalid token: " + rawToken);
        }


        //static Regex symbolPat = new Regex("[:]?([\\D&&[^/]].*/)?(/|[\\D&&[^/]][^/]*)");
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
                {
                    Match m2 = keywordPat.Match(mask.Substring(2));
                    if (!m2.Success)
                        return null;


                    string ns;
                    string name;
                    ExtractNamesUsingMask(token.Substring(2), m2.Groups[1].Value, m2.Groups[2].Value, out ns, out name);
                    Symbol ks = Symbol.intern(ns, name);
                    Namespace kns;
                    if (ks.Namespace != null)
                        kns = Compiler.namespaceFor(ks);
                    else
                        kns = Compiler.CurrentNamespace;
                    // auto-resolving keyword
                    if (kns != null)
                        return Keyword.intern(kns.Name.Name, ks.Name);
                    else
                        return null;
                }

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



        //    // either namespaceToken is null or containts
        //    // no :: except at beginning
        //    if (token.IndexOf("::", 1) != -1)
        //        return null;

        //    string nsStr;
        //    string nameStr;
        //    bool hasNS;

        //    if (lastSlashIndex == -1)
        //    {
        //        hasNS = false;
        //        nsStr = String.Empty;
        //        nameStr = token;
        //    }
        //    else
        //    {
        //        hasNS = true;
        //        nsStr = token.Substring(0, lastSlashIndex);
        //        nameStr = token.Substring(lastSlashIndex + 1);
        //    }

        //    // Must begin with non-digit, or ':' + non-digit if there is a namespace
        //    Match nameMatch = hasNS ? nameSymbolPat.Match(nameStr) : nsSymbolPat.Match(nameStr);
        //    if (!nameMatch.Success)
        //        return null;

        //    // no trailing :
        //    if (nameStr.EndsWith(":"))
        //        return null;

        //    if (hasNS)
        //    {
        //        // Must begin with non-digit or ':' + non-digit
        //        Match nsMatch = nsSymbolPat.Match(nsStr);
        //        if (!nsMatch.Success)
        //            return null;

        //        // no trailing :
        //        if (nsStr.EndsWith(":"))
        //            return null;

        //    }

        //    // Do keyword detection

        //    if (hasNS)
        //    {
        //        if (nsStr.StartsWith("::"))
        //        {
        //            Symbol nsSym = Symbol.intern(nsStr.Substring(2));
        //            Namespace ns = Compiler.CurrentNamespace.LookupAlias(nsSym);
        //            if (ns == null)
        //                ns = Namespace.find(nsSym);
        //            if (ns == null)
        //                return null;
        //            else
        //                return Keyword.intern(ns.Name.getName(), nameStr);
        //        }
        //        else if (nsStr.StartsWith(":"))
        //            return Keyword.intern(nsStr.Substring(1), nameStr);
        //        else
        //            return Symbol.intern(nsStr, nameStr);
        //    }
        //    else
        //    {
        //        if ( nameStr.StartsWith("::"))
        //            return Keyword.intern(Compiler.CurrentNamespace.Name.getName(),nameStr.Substring(2));
        //        else if ( nameStr.StartsWith(":") )
        //            return Keyword.intern(nameStr.Substring(1));
        //        else
        //            return Symbol.intern(null,nameStr);  // Avoid / interpretation in intern(string)
        //    }
        //}

        #endregion

        #region Symbol printing helpers

        public static bool NameRequiresEscaping(string s)
        {
            if (String.IsNullOrEmpty(s))
                return true;

            // Contains bad character
            foreach (char c in s)
                if (c == '|' ||
                    c == '/' ||
                    isWhitespace(c) ||
                    isTerminatingMacro(c))
                    return true;

            char firstChar = s[0];
            if (firstChar == ':' || isMacro(firstChar) || Char.IsDigit(firstChar))
                return true;

            // contains a :: anywhere
            if (s.Contains("::"))
                return true;

            // Begins with +/- and a digit

            if ((firstChar == '+' || firstChar == '-') && s.Length >= 2 && Char.IsDigit(s[1]))
                return true;

            return false;
        }

        public static string VbarEscape(string s)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("|");

            foreach (char c in s)
            {
                sb.Append(c);
                if (c == '|')
                    sb.Append('|');
            }
            sb.Append("|");
            return sb.ToString();
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
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "2#")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "3#")]
            public override object invoke(object arg1, object arg2, object opts, object pendingForms)
            {
                return Read((PushbackTextReader)arg1, (Char)arg2, opts, pendingForms);
            }

            protected abstract object Read(PushbackTextReader r, char c, object opts, object pendingForms);
        }

        #region CharacterReader

        public sealed class CharacterReader : ReaderBase
        {
            protected override object Read(PushbackTextReader r, char backslash, object opts, object pendingForms)
            {
                int ch = r.Read();
                if (ch == -1)
                    throw new EndOfStreamException("EOF while reading character");
                String token = readSimpleToken(r, (char)ch);
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
            protected override object Read(PushbackTextReader r, char doublequote, object opts, object pendingForms)
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
            protected override object Read(PushbackTextReader r, char semicolon, object opts, object pendingForms)
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
            protected override object Read(PushbackTextReader r, char underscore, object opts, object pendingForms)
            {
                ReadAux(r, opts, EnsurePending(pendingForms));
                return r;
            }
        }

        #endregion

        #region Collection readers

        public sealed class ListReader : ReaderBase
        {
            protected override object Read(PushbackTextReader r, char leftparen, object opts, object pendingForms)
            {
                int startLine = -1;
                int startCol = -1;
                LineNumberingTextReader lntr = r as LineNumberingTextReader;

                if (lntr != null)
                {
                    startLine = lntr.LineNumber;
                    startCol = lntr.ColumnNumber;
                }
                IList<Object> list = ReadDelimitedList(')', r, true, opts, EnsurePending(pendingForms));
                if (list.Count == 0)
                    return PersistentList.EMPTY;
                IObj s = (IObj)PersistentList.create((IList)list);
                if (startLine != -1)
                {
                    return s.withMeta(RT.map(
                        RT.LineKey, startLine, // This is what is supported by the JVM version
                        RT.ColumnKey, startCol,
                        // We add a :source-span key, value is map with the other values.
                        // A map is used here so that we are print-dup--serializable.
                        RT.SourceSpanKey, RT.map(
                            RT.StartLineKey, startLine,
                            RT.StartColumnKey, startCol,
                            RT.EndLineKey, lntr.LineNumber,
                            RT.EndColumnKey, lntr.ColumnNumber)));
                }
                else
                    return s;
            }
        }

        public sealed class VectorReader : ReaderBase
        {
            protected override object Read(PushbackTextReader r, char leftparen, object opts, object pendingForms)
            {
                return LazilyPersistentVector.create(ReadDelimitedList(']', r, true, opts, EnsurePending(pendingForms)));
            }
        }

        public sealed class MapReader : ReaderBase
        {
            protected override object Read(PushbackTextReader r, char leftbrace, object opts, object pendingForms)
            {
                Object[] a = ReadDelimitedList('}', r, true, opts, EnsurePending(pendingForms)).ToArray();
                if ((a.Length & 1) == 1)
                    throw new ArgumentException("Map literal must contain an even number of forms");
                return RT.map(a);
            }
        }

        public sealed class SetReader : ReaderBase
        {
            protected override object Read(PushbackTextReader r, char leftbracket, object opts, object pendingForms)
            {
                return PersistentHashSet.createWithCheck(ReadDelimitedList('}', r, true, opts, EnsurePending(pendingForms)));
            }
        }

        public sealed class UnmatchedDelimiterReader : ReaderBase
        {
            protected override object Read(PushbackTextReader reader, char rightdelim, object opts, object pendingForms)
            {
                throw new ArgumentException("Unmatched delimiter: " + rightdelim);
            }
        }

        #endregion

        #region Wrapping readers

        public sealed class WrappingReader : ReaderBase
        {
            readonly Symbol _sym;

            public WrappingReader(Symbol sym)
            {
                _sym = sym;
            }

            protected override object Read(PushbackTextReader r, char quote, object opts, object pendingForms)
            {
                //object o = read(r, true, null, true, opts, pendingForms);
                object o = ReadAux(r, opts, EnsurePending(pendingForms));
                return RT.list(_sym, o);
            }
        }

        public sealed class DeprecatedWrappingReader : ReaderBase
        {
            readonly Symbol _sym;
            readonly string _macro;

            public DeprecatedWrappingReader(Symbol sym, string macro)
            {
                _sym = sym;
                _macro = macro;
            }

            protected override object Read(PushbackTextReader r, char quote, object opts, object pendingForms)
            {
                Console.WriteLine("WARNING: read macro {0} is deprecated; use {1} instead", _macro, _sym.getName());
                //object o = read(r, true, null, true, opts, pendingForms);
                object o = ReadAux(r, opts, EnsurePending(pendingForms));
                return RT.list(_sym, o);
            }
        }

        #endregion

        #region Syntax quote

        public sealed class SyntaxQuoteReader : ReaderBase
        {
            protected override object Read(PushbackTextReader r, char backquote, object opts, object pendingForms)
            {
                try
                {
                    Var.pushThreadBindings(RT.map(GENSYM_ENV, PersistentHashMap.EMPTY));
                    //object form = read(r, true, null, true, opts, pendingForms);
                    object form = ReadAux(r, opts, EnsurePending(pendingForms));
                    return syntaxQuote(form);
                }
                finally
                {
                    Var.popThreadBindings();
                }
            }

            static object syntaxQuote(object form)
            {
                bool checkMeta;
                object ret = AnalyzeSyntaxQuote(form, out checkMeta);

                if (checkMeta)
                {
                    IObj formAsIobj = form as IObj;

                    if (formAsIobj != null && formAsIobj.meta() != null)
                    {
                        //filter line numbers & source span info
                        IPersistentMap newMeta = formAsIobj.meta().without(RT.LineKey).without(RT.ColumnKey).without(RT.SourceSpanKey);
                        if (newMeta.count() > 0)
                            return RT.list(WITH_META, ret, syntaxQuote(formAsIobj.meta()));
                    }
                }

                return ret;
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
            private static object AnalyzeSyntaxQuote(object form, out bool checkMeta)
            {
                checkMeta = true;

                if (Compiler.IsSpecial(form))
                    return RT.list(Compiler.QuoteSym, form);

                Symbol sym = form as Symbol;

                if (sym != null)
                {
                    if (sym.Namespace == null && sym.Name.EndsWith("#"))
                    {
                        IPersistentMap gmap = (IPersistentMap)GENSYM_ENV.deref();
                        if (gmap == null)
                            throw new InvalidDataException("Gensym literal not in syntax-quote");
                        Symbol gs = (Symbol)gmap.valAt(sym);
                        if (gs == null)
                            GENSYM_ENV.set(gmap.assoc(sym, gs = Symbol.intern(null,
                                                                              sym.Name.Substring(0, sym.Name.Length - 1)
                                                                              + "__" + RT.nextID() + "__auto__")));
                        sym = gs;
                    }
                    else if (sym.Namespace == null && sym.Name.EndsWith("."))
                    {
                        Symbol csym = Symbol.intern(null, sym.Name.Substring(0, sym.Name.Length - 1));
                        csym = Compiler.resolveSymbol(csym);
                        sym = Symbol.intern(null, csym.Name + ".");
                    }
                    else if (sym.Namespace == null && sym.Name.StartsWith("."))
                    {
                        // simply quote method names
                    }
                    else
                    {
                        object maybeClass = null;
                        if (sym.Namespace != null)
                            maybeClass = Compiler.CurrentNamespace.GetMapping(
                                Symbol.intern(null, sym.Namespace));
                        Type t = maybeClass as Type;

                        if (t != null)
                        {
                            // Classname/foo -> package.qualified.Classname/foo
                            sym = Symbol.intern(t.Name, sym.Name);
                        }
                        else
                            sym = Compiler.resolveSymbol(sym);
                    }
                    return RT.list(Compiler.QuoteSym, sym);
                }

                if (isUnquote(form))
                {
                    checkMeta = false;
                    return RT.second(form);
                }

                if (isUnquoteSplicing(form))
                    throw new ArgumentException("splice not in list");

                if (form is IPersistentCollection)
                {
                    if (form is IRecord)
                        return form;

                    if (form is IPersistentMap)
                    {
                        IPersistentVector keyvals = flattenMap(form);
                        return RT.list(APPLY, HASHMAP, RT.list(SEQ, RT.cons(CONCAT, sqExpandList(keyvals.seq()))));
                    }

                    IPersistentVector v = form as IPersistentVector;
                    if (v != null)
                    {
                        return RT.list(APPLY, VECTOR, RT.list(SEQ, RT.cons(CONCAT, sqExpandList(v.seq()))));
                    }

                    IPersistentSet s = form as IPersistentSet;
                    if (s != null)
                    {
                        return RT.list(APPLY, HASHSET, RT.list(SEQ, RT.cons(CONCAT, sqExpandList(s.seq()))));
                    }


                    if (form is ISeq || form is IPersistentList)
                    {
                        ISeq seq = RT.seq(form);
                        if (seq == null)
                            return RT.cons(LIST, null);
                        else
                            return RT.list(SEQ, RT.cons(CONCAT, sqExpandList(seq)));
                    }
                    else
                        throw new InvalidOperationException("Unknown Collection type");
                }

                if (form is Keyword
                        || Util.IsNumeric(form)
                        || form is Char
                        || form is String)
                    return form;
                else
                    return RT.list(Compiler.QuoteSym, form);
            }


            private static ISeq sqExpandList(ISeq seq)
            {
                IPersistentVector ret = PersistentVector.EMPTY;
                for (; seq != null; seq = seq.next())
                {
                    Object item = seq.first();
                    //if (item is Unquote)
                    //    ret = ret.cons(RT.list(LIST, ((Unquote)item).Obj));
                    // REV 1184
                    if (isUnquote(item))
                        ret = ret.cons(RT.list(LIST, RT.second(item)));
                    else if (isUnquoteSplicing(item))
                        ret = ret.cons(RT.second(item));
                    else
                        ret = ret.cons(RT.list(LIST, syntaxQuote(item)));
                }
                return ret.seq();
            }

            private static IPersistentVector flattenMap(object form)
            {
                IPersistentVector keyvals = PersistentVector.EMPTY;
                for (ISeq s = RT.seq(form); s != null; s = s.next())
                {
                    IMapEntry e = (IMapEntry)s.first();
                    keyvals = (IPersistentVector)keyvals.cons(e.key());
                    keyvals = (IPersistentVector)keyvals.cons(e.val());
                }
                return keyvals;
            }
        }

        sealed class UnquoteReader : ReaderBase
        {
            protected override object Read(PushbackTextReader r, char comma, object opts, object pendingForms)
            {
                int ch = r.Read();
                if (ch == -1)
                    throw new EndOfStreamException("EOF while reading character");
                pendingForms = EnsurePending(pendingForms);
                if (ch == '@')
                {
                    //object o = read(r, true, null, true, opts, pendingForms);
                    object o = ReadAux(r, opts, pendingForms);
                    return RT.list(UNQUOTE_SPLICING, o);
                }
                else
                {
                    Unread(r, ch);
                    //object o = read(r, true, null, true, opts, pendingForms);
                    object o = ReadAux(r, opts, pendingForms);
                    //return new Unquote(o);
                    // per Rev 1184
                    return RT.list(UNQUOTE, o);
                }
            }
        }

        #region Unquote helpers

        // Per rev 1184
        static bool isUnquote(object form)
        {
            return form is ISeq && Util.equals(RT.first(form), UNQUOTE);
        }

        static bool isUnquoteSplicing(object form)
        {
            return form is ISeq && Util.equals(RT.first(form), UNQUOTE_SPLICING);
        }

        #endregion

        #endregion

        #region DispatchReader

        public sealed class DispatchReader : ReaderBase
        {
            protected override object Read(PushbackTextReader r, char hash, object opts, object pendingForms)
            {
                int ch = r.Read();
                if (ch == -1)
                    throw new EndOfStreamException("EOF while reading character");
                IFn fn = _dispatchMacros[ch];
                // Try the ctor reader first
                if (fn == null)
                {
                    Unread(r, ch);
                    pendingForms = EnsurePending(pendingForms);
                    object result = _ctorReader.invoke(r, (char)ch, opts, pendingForms);

                    if (result != null)
                        return result;
                    else
                        throw new InvalidOperationException(String.Format("No dispatch macro for: {0}", (char)ch));
                }
                return fn.invoke(r, (char)ch, opts, pendingForms);
            }
        }

        #endregion

        #region MetaReader

        public sealed class MetaReader : ReaderBase
        {
            protected override object Read(PushbackTextReader r, char caret, object opts, object pendingForms)
            {
                int startLine = -1;
                int startCol = -1;
                LineNumberingTextReader lntr = r as LineNumberingTextReader;

                if (lntr != null)
                {
                    startLine = lntr.LineNumber;
                    startCol = lntr.ColumnNumber;
                }

                pendingForms = EnsurePending(pendingForms);

                IPersistentMap metaAsMap;
                {
                    object meta = ReadAux(r, opts, pendingForms);

                    if (meta is Symbol || meta is String)
                        metaAsMap = RT.map(RT.TagKey, meta);
                    else if (meta is Keyword)
                        metaAsMap = RT.map(meta, true);
                    else if ((metaAsMap = meta as IPersistentMap) == null)
                        throw new ArgumentException("Metadata must be Symbol,Keyword,String or Map");
                }

                object o = ReadAux(r, opts, pendingForms);
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

        #region VarReader

        public sealed class VarReader : ReaderBase
        {
            protected override object Read(PushbackTextReader r, char quote, object opts, object pendingForms)
            {
                //object o = read(r, true, null, true, opts, pendingForms);
                object o = ReadAux(r, opts, EnsurePending(pendingForms));
                //		if(o instanceof Symbol)
                //			{
                //			Object v = Compiler.maybeResolveIn(Compiler.currentNS(), (Symbol) o);
                //			if(v instanceof Var)
                //				return v;
                //			}
                return RT.list(THE_VAR, o);
            }
        }

        #endregion

        #region RegexReader

        public sealed class RegexReader : ReaderBase
        {
            protected override object Read(PushbackTextReader r, char doublequote, object opts, object pendingForms)
            {
                StringBuilder sb = new StringBuilder();
                for (int ch = r.Read(); ch != '"'; ch = r.Read())
                {
                    if (ch == -1)
                        throw new EndOfStreamException("EOF while reading regex");
                    sb.Append((char)ch);
                    if (ch == '\\')	//escape
                    {
                        ch = r.Read();
                        if (ch == -1)
                            throw new EndOfStreamException("EOF while reading regex");
                        sb.Append((char)ch);
                    }
                }
                return new Regex(sb.ToString());
            }
        }

        #endregion

        #region Fn and Arg readers

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Fn")]
        public sealed class FnReader : ReaderBase
        {
            //static ListReader _listReader = new ListReader();

            protected override object Read(PushbackTextReader r, char lparen, object opts, object pendingForms)
            {
                if (ARG_ENV.deref() != null)
                    throw new InvalidOperationException("Nested #()s are not allowed");
                try
                {
                    Var.pushThreadBindings(RT.map(ARG_ENV, PersistentTreeMap.EMPTY));
                    r.Unread('(');
                    ////object form = ReadAux(r, true, null, true, opts, pendingForms);
                    object form = ReadAux(r, opts, EnsurePending(pendingForms));
                    //object form = _listReader.invoke(r, '(');

                    IPersistentVector args = PersistentVector.EMPTY;
                    PersistentTreeMap argsyms = (PersistentTreeMap)ARG_ENV.deref();
                    ISeq rargs = argsyms.rseq();
                    if (rargs != null)
                    {
                        int higharg = (int)((IMapEntry)rargs.first()).key();
                        if (higharg > 0)
                        {
                            for (int i = 1; i <= higharg; ++i)
                            {
                                object sym = argsyms.valAt(i);
                                if (sym == null)
                                    sym = garg(i);
                                args = args.cons(sym);
                            }
                        }
                        object restsym = argsyms.valAt(-1);
                        if (restsym != null)
                        {
                            args = args.cons(Compiler.AmpersandSym);
                            args = args.cons(restsym);
                        }
                    }
                    return RT.list(Compiler.FnSym, args, form);
                }
                finally
                {
                    Var.popThreadBindings();
                }
            }
        }

        sealed class ArgReader : ReaderBase
        {
            protected override object Read(PushbackTextReader r, char pct, object opts, object pendingForms)
            {
                //if (ARG_ENV.deref() == null)
                //    return interpretToken(readToken(r, '%'));
                if (ARG_ENV.deref() == null)
                {
                    return InterpretToken(readSimpleToken(r, '%'));
                }

                int ch = r.Read();
                Unread(r, ch);
                //% alone is first arg
                if (ch == -1 || isWhitespace(ch) || isTerminatingMacro(ch))
                {
                    return registerArg(1);
                }
                //object n = ReadAux(r, true, null, true, opts, pendingForms);
                object n = ReadAux(r, opts, EnsurePending(pendingForms));
                if (n.Equals(Compiler.AmpersandSym))
                    return registerArg(-1);
                if (!Util.IsNumeric(n))
                    throw new ArgumentException("arg literal must be %, %& or %integer");
                return registerArg(Util.ConvertToInt(n));
            }

            static Symbol registerArg(int n)
            {
                PersistentTreeMap argsyms = (PersistentTreeMap)ARG_ENV.deref();
                if (argsyms == null)
                {
                    throw new InvalidOperationException("arg literal not in #()");
                }
                Symbol ret = (Symbol)argsyms.valAt(n);
                if (ret == null)
                {
                    ret = garg(n);
                    ARG_ENV.set(argsyms.assoc(n, ret));
                }
                return ret;
            }
        }

        #endregion

        #region EvalREader

        //TODO: Need to figure out who to deal with typenames in the context of multiple loaded assemblies.
        public sealed class EvalReader : ReaderBase
        {
            protected override object Read(PushbackTextReader r, char eq, object opts, object pendingForms)
            {
                if (!RT.booleanCast(RT.ReadEvalVar.deref()))
                {
                    throw new InvalidOperationException("EvalReader not allowed when *read-eval* is false");
                }

                Object o = read(r, true, null, true, opts, EnsurePending(pendingForms));
                if (o is Symbol)
                {
                    return RT.classForNameE(o.ToString());
                }
                else if (o is IPersistentList)
                {
                    Symbol fs = (Symbol)RT.first(o);
                    if (fs.Equals(THE_VAR))
                    {
                        Symbol vs = (Symbol)RT.second(o);
                        return RT.var(vs.Namespace, vs.Name);  //Compiler.resolve((Symbol) RT.second(o),true);
                    }

                    if (fs.Name.EndsWith("."))
                    {
                        Object[] args = RT.toArray(RT.next(o));
                        //return Reflector.InvokeConstructor(RT.classForName(fs.Name.Substring(0, fs.Name.Length - 1)), args);
                        // I think the JVM code is wrong here
                        string s = fs.ToString();
                        return Reflector.InvokeConstructor(RT.classForNameE(s.Substring(0, s.Length - 1)), args);
                    }
                    if (Compiler.NamesStaticMember(fs))
                    {

                        Object[] args = RT.toArray(RT.next(o));
                        return Reflector.InvokeStaticMethod(fs.Namespace, fs.Name, args);
                    }

                    Object v = Compiler.maybeResolveIn(Compiler.CurrentNamespace, fs);
                    if (v is Var)
                    {
                        return ((IFn)v).applyTo(RT.next(o));
                    }
                    throw new InvalidOperationException("Can't resolve " + fs);
                }
                else
                    throw new InvalidOperationException("Unsupported #= form");
            }
        }

        #endregion

        #region CtorReader

        public sealed class CtorReader : ReaderBase
        {
            protected override object Read(PushbackTextReader r, char c, object opts, object pendingForms)
            {
                pendingForms = EnsurePending(pendingForms);

                Object name = read(r, true, null, false, opts, pendingForms);
                Symbol sym = name as Symbol;
                if (sym == null)
                    throw new ArgumentException("Reader tag must be a symbol");

                Object form = read(r, true, null, true, opts, pendingForms);

                if (IsPreserveReadCond(opts) || RT.suppressRead())
                {
                    return TaggedLiteral.create(sym, form);
                }
                else
                {
                    return sym.Name.Contains(".") ? ReadRecord(form, sym, opts, pendingForms) : ReadTagged(form, sym, opts, pendingForms);
                }

            }


            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "opts"), 
             System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "pendingForms")]
            static object ReadTagged(object o, Symbol tag, object opts, object pendingForms)
            {
                ILookup dataReaders = (ILookup)RT.DataReadersVar.deref();
                IFn dataReader = (IFn)RT.get(dataReaders, tag);
                if (dataReader == null)
                {
                    dataReaders = (ILookup)RT.DefaultDataReadersVar.deref();
                    dataReader = (IFn)RT.get(dataReaders, tag);
                    if (dataReader == null)
                    {
                        IFn default_reader = (IFn)RT.DefaultDataReaderFnVar.deref();
                        if (default_reader != null)
                            return default_reader.invoke(tag, o);
                        else
                            throw new ArgumentException("No reader function for tag " + tag.ToString());
                    }
                }
                return dataReader.invoke(o);
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "opts"), 
             System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "pendingForms")]
            static object ReadRecord(object form, Symbol recordName, object opts, object pendingForms)
            {
                bool readeval = RT.booleanCast(RT.ReadEvalVar.deref());
                if (!readeval)
                    throw new InvalidOperationException("Record construction syntax can only be used when *read-eval* == true ");

                Type recordType = RT.classForNameE(recordName.ToString());

                IPersistentVector recordEntries;
                IPersistentMap vals;

                object ret = null;
                ConstructorInfo[] allCtors = recordType.GetConstructors();

                if ((recordEntries = form as IPersistentVector) != null)
                {
                    // shortForm
                    bool ctorFound = false;
                    foreach (ConstructorInfo cinfo in allCtors)
                        if (cinfo.GetParameters().Length == recordEntries.count())
                            ctorFound = true;

                    if (!ctorFound)
                        throw new ArgumentException(String.Format("Unexpected number of constructor arguments to {0}: got {1}", recordType.ToString(), recordEntries.count()));

                    ret = Reflector.InvokeConstructor(recordType, RT.toArray(recordEntries));
                }
                else if ((vals = form as IPersistentMap) != null)
                {
                    for (ISeq s = RT.keys(vals); s != null; s = s.next())
                    {
                        if (!(s.first() is Keyword))
                            throw new ArgumentException(String.Format("Unreadable defrecord form: key must be of type clojure.lang.Keyword, got {0}", s.first().ToString()));
                    }

                    ret = Reflector.InvokeStaticMethod(recordType, "create", new Object[] { vals });
                }
                else
                {
                    throw new ArgumentException("Unreadable constructor form starting with \"#" + recordName + "\"");
                }

                return ret;
            }
        }

        #endregion

        #region UnreadableReader

        public sealed class UnreadableReader : ReaderBase
        {
            protected override object Read(PushbackTextReader reader, char leftangle, object opts, object pendingForms)
            {
                throw new ArgumentException("Unreadable form");
            }
        }

        #endregion

        #endregion

        #region Conditional reading

        static bool IsPreserveReadCond(Object opts)
        {
            if (RT.booleanCast(READ_COND_ENV.deref()))
            {
                IPersistentMap optsMap = opts as IPersistentMap;
                if (optsMap != null)
                {
                    Object readCond = optsMap.valAt(OPT_READ_COND);
                    return COND_PRESERVE.Equals(readCond);
                }
            }
            return false;
        }

        public sealed class ConditionalReader : ReaderBase
        {

            protected override object Read(PushbackTextReader r, char c, object opts, object pendingForms)
            {
                checkConditionalAllowed(opts);

                int ch = r.Read();
                if (ch == -1)
                    throw new EndOfStreamException("EOF while reading character");

                bool splicing = false;

                if (ch == '@')
                {
                    splicing = true;
                    ch = r.Read();
                }

                while (isWhitespace(ch))
                    ch = r.Read();

                if (ch == -1)
                    throw new EndOfStreamException("EOF while reading character");

                if (ch != '(')
                    throw new InvalidOperationException("read-cond body must be a list");

                try
                {
                    Var.pushThreadBindings(RT.map(READ_COND_ENV, true /* RT.T */));

                    if (IsPreserveReadCond(opts))
                    {
                        IFn listReader = getMacro(ch); // should always be a list
                        Object form = listReader.invoke(r, (char)ch, opts, EnsurePending(pendingForms));

                        return ReaderConditional.create(form, splicing);
                    }
                    else
                    {
                        return readCondDelimited(r, splicing, opts, pendingForms);
                    }
                }
                finally
                {
                    Var.popThreadBindings();
                }
            }
        }

        internal static readonly Keyword DEFAULT_FEATURE = Keyword.intern(null, "default");
        internal static readonly IPersistentSet RESERVED_FEATURES =
             RT.set(Keyword.intern(null, "else"), Keyword.intern(null, "none"));


        public static bool HasFeature(Object feature, Object opts)
        {
            if (!(feature is Keyword))
                throw new InvalidOperationException("Feature should be a keyword: " + feature);

            if (DEFAULT_FEATURE.Equals(feature))
                return true;

            IPersistentSet custom = (IPersistentSet)((IPersistentMap)opts).valAt(OPT_FEATURES);
            return custom != null && custom.contains(feature);
        }

        static readonly object READ_STARTED = new object();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "read")]
        public static Object readCondDelimited(PushbackTextReader r, bool splicing, object opts, object pendingForms)
        {
            object result = READ_STARTED;
            object form; // The most recently ready form
            Boolean toplevel = (pendingForms == null);
            pendingForms = EnsurePending(pendingForms);

            LineNumberingTextReader lntr = r as LineNumberingTextReader;
            int firstLine = lntr != null ? lntr.LineNumber : -1;

            for (; ; )
            {

                if (result == READ_STARTED)
                {
                    // Read the next feature
                    form = read(r, false, READ_EOF, ')', READ_FINISHED, true, opts, pendingForms);

                    if (form == READ_EOF)
                    {
                        if (firstLine < 0)
                            throw new EndOfStreamException("EOF while reading");
                        else
                            throw new EndOfStreamException("EOF while reading, starting at line " + firstLine);
                    }
                    else if (form == READ_FINISHED)
                    {
                        break; // read-cond form is done
                    }

                    if (RESERVED_FEATURES.contains(form))
                        throw new ArgumentException("Feature name " + form + " is reserved.");

                    if (HasFeature(form, opts))
                    {

                        //Read the form corresponding to the feature, and assign it to result if everything is kosher

                        form = read(r, false, READ_EOF, ')', READ_FINISHED, true, opts, pendingForms);

                        if (form == READ_EOF)
                        {
                            if (firstLine < 0)
                                throw new EndOfStreamException("EOF while reading");
                            else
                                throw new EndOfStreamException("EOF while reading, starting at line " + firstLine);
                        }
                        else if (form == READ_FINISHED)
                        {
                            if (firstLine < 0)
                                throw new ArgumentException("read-cond requires an even number of forms.");
                            else
                                throw new ArgumentException("read-cond starting on line " + firstLine + " requires an even number of forms");
                        }
                        else
                        {
                            result = form;
                        }
                    }
                }

                // When we already have a result, or when the feature didn't match, discard the next form in the reader
                try
                {
                    Var.pushThreadBindings(RT.map(RT.SuppressReadVar, true /* RT.T */));
                    form = read(r, false, READ_EOF, ')', READ_FINISHED, true, opts, pendingForms);

                    if (form == READ_EOF)
                    {
                        if (firstLine < 0)
                            throw new EndOfStreamException("EOF while reading");
                        else
                            throw new EndOfStreamException("EOF while reading, starting at line " + firstLine);
                    }
                    else if (form == READ_FINISHED)
                    {
                        break;
                    }
                }
                finally
                {
                    Var.popThreadBindings();
                }

            }

            if (result == READ_STARTED)  // no features matched
                return r;

            if (splicing)
            {
                IList<Object> resultAsList = result as IList<object>;

                if (resultAsList == null)
                    throw new ArgumentException("Spliced form list in read-cond-splicing must implement IList<Object>");

                if (toplevel)
                    throw new InvalidOperationException("Reader conditional splicing not allowed at the top level.");

                LinkedList<Object> pendingLinked = pendingForms as LinkedList<Object>;
                LinkedListNode<Object> node = pendingLinked.First;
                foreach (object item in resultAsList)
                {
                    if (node == null)
                        node = pendingLinked.AddFirst(item);
                    else
                        node = pendingLinked.AddAfter(node, item);
                }
                return r;
            }
            else
            {
                return result;
            }
        }

        private static void checkConditionalAllowed(Object opts)
        {
            IPersistentMap mopts = (IPersistentMap)opts;
            if (!(opts != null && (COND_ALLOW.Equals(mopts.valAt(OPT_READ_COND)) ||
                COND_PRESERVE.Equals(mopts.valAt(OPT_READ_COND)))))
                throw new InvalidOperationException("Conditional read not allowed");
        }
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
