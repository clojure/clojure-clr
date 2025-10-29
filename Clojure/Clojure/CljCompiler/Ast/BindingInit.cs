/**
 *   Copyright (c) Rich Hickey. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

namespace clojure.lang.CljCompiler.Ast
{
    public struct BindingInit(LocalBinding binding, Expr init)
    {
        #region Data
        public readonly LocalBinding Binding => binding;

        public readonly Expr Init => init;

        #endregion

        #region Ctors

        #endregion

        #region Object overrides

        public override readonly bool Equals(object obj)
        {
            if (obj is not BindingInit)
                return false;

            BindingInit bi = (BindingInit)obj;

            return binding.Equals(bi.Binding) && init.Equals(bi.Init);
        }

        public static bool operator ==(BindingInit b1, BindingInit b2)
        {
            return b1.Equals(b2);
        }

        public static bool operator !=(BindingInit b1, BindingInit b2)
        {
            return !b1.Equals(b2);
        }

        public override readonly int GetHashCode()
        {
            return Util.hashCombine(binding.GetHashCode(), init.GetHashCode());
        }

        #endregion
    }
}
