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
    public class HostArg
    {
        #region Enum

        public enum ParameterType
        {
            Standard,
            ByRef
        }

        #endregion

        #region Data

        readonly ParameterType _paramType;

        public ParameterType ParamType
        {
            get { return _paramType; }
        }

        readonly Expr _argExpr;

        public Expr ArgExpr
        {
            get { return _argExpr; }
        }

        readonly LocalBinding _localBinding;

        public LocalBinding LocalBinding
        {
            get { return _localBinding; }
        }

        #endregion

        #region C-tors

        public HostArg(ParameterType paramType, Expr argExpr, LocalBinding lb)
        {
            _paramType = paramType;
            _argExpr = argExpr;
            _localBinding = lb;
        }

        #endregion
    }
}
