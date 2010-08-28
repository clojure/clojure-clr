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


namespace clojure.lang.CljCompiler.Ast
{
    // value semantics
    public class ParserContext
    {
        #region Data

        readonly bool _isRecurContext;

        public bool IsRecurContext
        {
            get { return _isRecurContext; }
        }

        readonly bool _isAssignContext;

        public bool IsAssignContext
        {
            get { return _isAssignContext; }
        } 


        #endregion

        #region C-tors

        public ParserContext()
        {
            _isAssignContext = false;
            _isRecurContext = false;
        }

        public ParserContext(bool isRecurContext, bool isAssignContext)
        {
            _isRecurContext = isRecurContext;
            _isAssignContext = isAssignContext;
        }

        #endregion

        #region Modifiers

        public ParserContext SetRecur(bool value)
        {
            if (_isRecurContext == value)
                return this;

            return new ParserContext(value, _isAssignContext);
        }

        public ParserContext SetAssign(bool value)
        {
            if (_isAssignContext == value)
                return this;

            return new ParserContext(_isRecurContext, value);
        }

        #endregion

        #region Object overrides

        public override bool Equals(object obj)
        {
            ParserContext pc = obj as ParserContext;
            if (obj == null)
                return false;

            return _isRecurContext == pc._isRecurContext && _isAssignContext == pc._isAssignContext;
        }

        public override int GetHashCode()
        {
            return Util.HashCombine(_isAssignContext.GetHashCode(), _isRecurContext.GetHashCode());
        }

        #endregion
    }
}
