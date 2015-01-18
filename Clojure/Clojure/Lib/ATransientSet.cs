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


namespace clojure.lang
{
    public abstract class ATransientSet : AFn, ITransientSet
    {
        #region Data

        protected volatile ITransientMap _impl;

        #endregion

        #region C-tors

        protected ATransientSet(ITransientMap impl)
        {
            _impl = impl;
        }

        #endregion

        #region Counted Members

        public int count()
        {
            return _impl.count();
        }

        #endregion

        #region ITransientCollection Members

        public ITransientCollection conj(object val)
        {
            ITransientMap m = _impl.assoc(val, val);
            if (m != _impl)
                _impl = m;
            return this;
        }

        public abstract IPersistentCollection persistent();

        #endregion

        #region ITransientSet Members

        public ITransientSet disjoin(object key)
        {
            ITransientMap m = _impl.without(key);
            if (m != _impl)
                _impl = m;
            return this;
        }

        public bool contains(object key)
        {
            return this != _impl.valAt(key, this);
        }

        public object get(object key)
        {
            return _impl.valAt(key);
        }

        #endregion

        #region IFn overrides

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "0#")]
        public override object invoke(object key)
        {
            return _impl.valAt(key);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration")]
        public override object invoke(object key, object notFound)
        {
            return _impl.valAt(key, notFound);
        }

        #endregion
    }
}
