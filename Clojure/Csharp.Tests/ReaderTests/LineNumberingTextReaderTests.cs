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
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

using clojure.lang;
using System.IO;

namespace Clojure.Tests.ReaderTests
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable"), TestFixture]
    public class LineNumberingTextReaderTests : AssertionHelper
    {
        const string _sample = "abc\nde\nfghijk\r\nlmnopq\n\nrstuv";
        StringReader _sr;
        LineNumberingTextReader _rdr;

        [SetUp]
        public void Setup()
        {
            _sr = new StringReader(_sample);
            _rdr = new LineNumberingTextReader(_sr);

        }

        [TearDown]
        public void TearDown()
        {
            _rdr.Close();
        }

        [Test]
        public void Initializes_properly()
        {
            Expect(_rdr.Index, EqualTo(0));
            Expect(_rdr.ColumnNumber, EqualTo(1));
            Expect(_rdr.LineNumber, EqualTo(1));
            Expect(_rdr.Peek(), EqualTo((int)_sample[0]));
            Expect(_rdr.AtLineStart);
        }


        [Test]
        public void Reads_character_at_a_time()
        {
            int[] chars = new int[] { 
                'a', 'b', 'c', '\n', 
                'd', 'e', '\n', 
                'f', 'g', 'h', 'i', 'j', 'k', '\n', 
                'l', 'm', 'n', 'o', 'p', 'q', '\n', 
                '\n', 
                'r', 's', 't', 'u', 'v' };

            int[] positions = new int[] {
                2, 3, 4, 1,
                2, 3, 1,
                2, 3, 4, 5, 6, 7, 1, 
                2, 3, 4, 5, 6, 7, 1, 
                1,
                2, 3, 4, 5,6 };

            int[] lines = new int[] {
                1, 1, 1, 2,
                2, 2, 3,
                3, 3, 3, 3, 3, 3, 4,
                4, 4, 4, 4, 4, 4, 5,
                6,
                6, 6, 6, 6, 6 };

            int[] indexes = new int[] {
                1, 2, 3, 4,
                5, 6, 7,
                8, 9, 10, 11, 12, 13, 15, // \r\n
                16, 17, 18, 19, 20, 21, 22,
                23,
                24, 25, 26, 27, 28 };

            bool[] starts = new bool[] {
                false, false, false, true,
                false, false, true,
                false, false, false, false, false, false, true,
                false, false, false, false, false, false, true,
                true,
                false, false, false, false, false
            };

            int i = 0;
            int ch;
            while ((ch = _rdr.Read()) != -1)
            {
                Expect(_rdr.Index, EqualTo(indexes[i]));
                Expect(ch, EqualTo(chars[i]));
                Expect(_rdr.ColumnNumber, EqualTo(positions[i]));
                Expect(_rdr.LineNumber, EqualTo(lines[i]));
                Expect(_rdr.AtLineStart, EqualTo(starts[i]));
                ++i;
            }
        }

        [Test]
        public void Reads_lines_at_a_time()
        {
            string[] lines = new string[] {
                "abc", 
                "de",
                "fghijk",
                "lmnopq",
                "",
                "rstuv" };

            int[] positions = new int[] {
                1,1,1,1,1,6 };

            int[] lineNums = new int[] {
                2, 3, 4, 5, 6, 6 };

            bool[] starts = new bool[] {
                true, true, true, true, true, true
            };

            int[] indexes = new int[] {
                4,7,15,22,23,28
            };

            int index = 0;
            string line;
            while ((line = _rdr.ReadLine()) != null)
            {
                Expect(line, EqualTo(lines[index]));
                Expect(_rdr.ColumnNumber, EqualTo(positions[index]));
                Expect(_rdr.LineNumber, EqualTo(lineNums[index]));
                Expect(_rdr.AtLineStart, EqualTo(starts[index]));
                Expect(_rdr.Index, EqualTo(indexes[index]));
                ++index;
            }
        }

        [Test]
        public void Reads_blocks_just_fine()
        {
            char[][] buffers = new char[][] {
                new char[] { 'a',  'b',  'c', '\n', 'd' },
                new char[] { 'e',  '\n', 'f', 'g',  'h' },
                new char[] { 'i',  'j',  'k', '\n', 'l' },
                new char[] { 'm',  'n',  'o', 'p',  'q' },
                new char[] { '\n', '\n', 'r', 's',  't' },
                new char[] { 'u',  'v' } };
            int[] positions = new int[] { 2, 4, 2, 7, 4, 6 };
            int[] lineNums = new int[] { 2, 3, 4, 4, 6, 6 };
            bool[] starts = new bool[] { false, false, false, false, false, true, };
            int[] indexes = new int[] { 5, 10, 16, 21, 26, 28 };

            char[] buffer = new char[20];

            int index = 0;
            int count;
            while ((count = _rdr.Read(buffer, 0, 5)) != 0)
            {
                Expect(SameContents(buffer, buffers[index], count));
                Expect(_rdr.ColumnNumber, EqualTo(positions[index]));
                Expect(_rdr.LineNumber, EqualTo(lineNums[index]));
                Expect(_rdr.AtLineStart, EqualTo(starts[index]));
                Expect(_rdr.Index, EqualTo(indexes[index]));
                ++index;
            }

        }

        bool SameContents(char[] b1, char[] b2, int count)
        {
            for (int i = 0; i < count; i++)
                if (b1[i] != b2[i])
                    return false;

            return true;
        }


        [Test]
        [ExpectedException(typeof(IOException))]
        public void Double_unread_fails()
        {
            _rdr.Unread('a');
            _rdr.Unread('b');
        }

        [Test]
        public void Basic_unread_works()
        {
            int c1 = _rdr.Read();
            Expect(c1, EqualTo((int)'a'));
            Expect(_rdr.ColumnNumber, EqualTo(2));
            Expect(_rdr.LineNumber, EqualTo(1));
            Expect(_rdr.AtLineStart, False);
            Expect(_rdr.Index, EqualTo(1));

            int c2 = _rdr.Read();
            Expect(c2, EqualTo((int)'b'));
            Expect(_rdr.ColumnNumber, EqualTo(3));
            Expect(_rdr.LineNumber, EqualTo(1));
            Expect(_rdr.AtLineStart, False);
            Expect(_rdr.Index, EqualTo(2));

            _rdr.Unread('x');
            Expect(_rdr.ColumnNumber, EqualTo(2));
            Expect(_rdr.LineNumber, EqualTo(1));
            Expect(_rdr.AtLineStart, False);
            Expect(_rdr.Index, EqualTo(1));

            int c3 = _rdr.Read();
            Expect(c3, EqualTo((int)'x'));
            Expect(_rdr.ColumnNumber, EqualTo(3));
            Expect(_rdr.LineNumber, EqualTo(1));
            Expect(_rdr.AtLineStart, False);
            Expect(_rdr.Index, EqualTo(2));

            int c4 = _rdr.Read();
            Expect(c4, EqualTo((int)'c'));
            Expect(_rdr.ColumnNumber, EqualTo(4));
            Expect(_rdr.LineNumber, EqualTo(1));
            Expect(_rdr.AtLineStart, False);
            Expect(_rdr.Index, EqualTo(3));
        }

        [Test]
        public void UnreadingNewlineWorks()
        {
            int c1 = _rdr.Read();
            Expect(c1, EqualTo((int)'a'));
            Expect(_rdr.ColumnNumber, EqualTo(2));
            Expect(_rdr.LineNumber, EqualTo(1));
            Expect(_rdr.AtLineStart, False);
            Expect(_rdr.Index, EqualTo(1));

            int c2 = _rdr.Read();
            Expect(c2, EqualTo((int)'b'));
            Expect(_rdr.ColumnNumber, EqualTo(3));
            Expect(_rdr.LineNumber, EqualTo(1));
            Expect(_rdr.AtLineStart, False);
            Expect(_rdr.Index, EqualTo(2));

            int c3 = _rdr.Read();
            Expect(c3, EqualTo((int)'c'));
            Expect(_rdr.ColumnNumber, EqualTo(4));
            Expect(_rdr.LineNumber, EqualTo(1));
            Expect(_rdr.AtLineStart, False);
            Expect(_rdr.Index, EqualTo(3));

            int c4 = _rdr.Read();
            Expect(c4, EqualTo((int)'\n'));
            Expect(_rdr.ColumnNumber, EqualTo(1));
            Expect(_rdr.LineNumber, EqualTo(2));
            Expect(_rdr.AtLineStart);
            Expect(_rdr.Index, EqualTo(4));

            _rdr.Unread(c4);
            Expect(_rdr.ColumnNumber, EqualTo(4));
            Expect(_rdr.LineNumber, EqualTo(1));
            Expect(_rdr.AtLineStart, False);
            Expect(_rdr.Index, EqualTo(3));

            int c5 = _rdr.Read();
            Expect(c5, EqualTo((int)'\n'));
            Expect(_rdr.ColumnNumber, EqualTo(1));
            Expect(_rdr.LineNumber, EqualTo(2));
            Expect(_rdr.AtLineStart);
            Expect(_rdr.Index, EqualTo(4));

            int c6 = _rdr.Read();
            Expect(c6, EqualTo((int)'d'));
            Expect(_rdr.ColumnNumber, EqualTo(2));
            Expect(_rdr.LineNumber, EqualTo(2));
            Expect(_rdr.AtLineStart, False);
            Expect(_rdr.Index, EqualTo(5));
        }
    }
}
