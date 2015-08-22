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
    public struct BindingInit
    {
        #region Data

        private readonly LocalBinding _binding;
        public LocalBinding Binding { get { return _binding; } }

        private readonly Expr _init;
        public Expr Init { get { return _init; } }

        #endregion

        #region Ctors

        public BindingInit(LocalBinding binding, Expr init)
        {
            _binding = binding;
            _init = init;
        }

        #endregion

        #region Object overrides

        public override bool Equals(object obj)
        {
            if ( ! (obj is BindingInit) )
                return false;

            BindingInit bi = (BindingInit) obj;

            return _binding.Equals(bi._binding) && bi._init.Equals(bi._init);
        }

        public static bool operator ==(BindingInit b1, BindingInit b2)
        {
            return b1.Equals(b2);
        }

        public static bool operator !=(BindingInit b1, BindingInit b2)
        {
            return !b1.Equals(b2);
        }

        public override int GetHashCode()
        {
            return Util.hashCombine(_binding.GetHashCode(), _init.GetHashCode()); 
        }
       
        #endregion
    }
}
