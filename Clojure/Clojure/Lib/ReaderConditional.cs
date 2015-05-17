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
    public class ReaderConditional : ILookup
    {
        #region Data

        static readonly Keyword FORM_KW = Keyword.intern("form");
        static readonly Keyword SPLICING_KW = Keyword.intern("splicing?");

        public readonly object _form;
        public readonly bool? _splicing;

        #endregion

        #region Ctors and factories


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        public static ReaderConditional create(object form, bool? splicing)
        {
            return new ReaderConditional(form, splicing);
        }

        private ReaderConditional(object form, bool? splicing)
        {
            _form = form;
            _splicing = splicing;
        }

        #endregion

        #region ILookup methods

        public object valAt(object key)
        {
            return valAt(key, null);
        }

        public object valAt(object key, object notFound)
        {
            if (FORM_KW.Equals(key))
            {
                return _form;
            }
            else if (SPLICING_KW.Equals(key))
            {
                return _splicing;
            }
            else
            {
                return notFound;
            }
        }

        #endregion

        #region Object overrides

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            //if (o == null || GetType() != o.GetType()) return false;

            ReaderConditional that = obj as ReaderConditional;
            if (that == null)
                return false;

            if (_form != null ? !_form.Equals(that._form) : that._form != null) return false;
            if (_splicing != null ? (!_splicing.Equals(that._splicing)) : (that._splicing != null))
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            int result = Util.hash(_form);
            result = 31 * result + Util.hash(_splicing);
            return result;
        }
        #endregion
    }
}