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
    // RHC = Rich Hickey Context -- same enum as Compiler.C in the Java version

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "RHC")]
    public enum RHC
    {
        Statement, // value ignored
        Expression, // value required
        Return, // tail position relative to enclosing recur frame
        Eval
    }

    // value semantics
    public class ParserContext
    {
        #region Data

        readonly RHC _rhc;
        public RHC Rhc { get { return _rhc; } }

        public bool IsRecurContext { get { return _rhc == RHC.Return; } }

        readonly bool _isAssignContext;
        public bool IsAssignContext { get { return _isAssignContext; } }

        #endregion

        #region C-tors

        public ParserContext(RHC rhc)
        {
            _rhc = rhc;
            _isAssignContext = false;
        }

        public ParserContext(RHC rhc, bool isAssignContext)
        {
            _rhc = rhc;
            _isAssignContext = isAssignContext;
        }

        #endregion

        #region Modifiers

        public ParserContext SetRhc(RHC rhc)
        {
            if (_rhc == rhc)
                return this;

            return new ParserContext(rhc, _isAssignContext);
        }

        public ParserContext SetAssign(bool value)
        {
            if (_isAssignContext == value)
                return this;

            return new ParserContext(_rhc, value);
        }

        public ParserContext EvalOrExpr()
        {
            if (_rhc == RHC.Eval)
                return this;
            return SetRhc(RHC.Expression);
        }

        #endregion

        #region Object overrides

        public override bool Equals(object obj)
        {
            ParserContext pc = obj as ParserContext;
            if (obj == null)
                return false;

            return _rhc == pc._rhc && _isAssignContext == pc._isAssignContext;
        }

        public override int GetHashCode()
        {
            return Util.hashCombine(_isAssignContext.GetHashCode(), _rhc.GetHashCode());
        }

        #endregion
    }
}
