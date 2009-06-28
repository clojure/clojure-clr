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
using System.Text.RegularExpressions;

namespace clojure.lang
{
    /// <summary>
    /// Shim class to provide java.util.regex.Matcher capabilities for the re-* functions in core.clj.
    /// </summary>
    public class JReMatcher
    {
        #region Data

        Regex _regex;
        Match _match;
        String _s;

        #endregion

        #region C-tors

        public JReMatcher(Regex regex, string s)
        {
            _regex = regex;
            _s = s;
            _match = null;
        }

        #endregion

        #region Matching

        // I'll even keep the names lowercase to match java.util.regex.Matcher

        // Careful analysis of the re-* methods in core.clj reveal that exactly these are needed.

        public bool find()
        {
            Match nextMatch;

            if (_match != null)
                nextMatch = _match.NextMatch();
            else if (_regex != null)
            {
                nextMatch = _regex.Match(_s);
                _regex = null;
                _s = null;
            }
            else
                return false;

            if (nextMatch.Success)
            {
                _match = nextMatch;
                return true;
            }
            else
            {
                _match = null;
                return false;
            }
        }

        // I don't implement the full functionality. 
        // This needs to be called on the first attempt to make a match
        //  because we have to rewrite the regex pattern to match the whole string
        public bool matches()
        {
            if (_regex == null)
                return false;

            string pattern = _regex.ToString();
            bool needFront = pattern.Length == 0 || pattern[0] != '^';
            bool needRear = pattern.Length == 0 || pattern[pattern.Length - 1] != '$';

            if (needFront || needRear)
            {
                pattern = (needFront ? "^" : String.Empty) + pattern + (needRear ? "$" : String.Empty);
                _regex = new Regex(pattern);
            }

            return find();
        }

        public int groupCount()
        {
            if (_match == null)
                throw new InvalidOperationException("Attempt to call groupCount on a non-realized or failed match.");

            return _match.Groups.Count - 1;
        }


        public string group()
        {
            if (_match == null)
                throw new InvalidOperationException("Attempt to call group on a non-realized or failed match.");

            return _match.Value;

        }

        public string group(int group)
        {
            if (_match == null)
                throw new InvalidOperationException("Attempt to call group on a non-realized or failed match.");

            if ( group < 0 || group >= _match.Groups.Count)
                throw new IndexOutOfRangeException("Attempt to call group with an index out of bounds.");

            return _match.Groups[group].Value;
        }


        #endregion

    }
}
