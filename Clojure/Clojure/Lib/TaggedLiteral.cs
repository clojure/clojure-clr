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

namespace clojure.lang
{
    public class TaggedLiteral : ILookup
    {
        #region Data

        public static readonly Keyword TagKeyword = Keyword.intern("tag");
        public static readonly Keyword FormKeyword = Keyword.intern("form");

        public readonly Symbol _tag;
        public readonly Object _form;

        #endregion

        #region Ctors and factories

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "create")]
        public static TaggedLiteral create(Symbol tag, Object form)
        {
            return new TaggedLiteral(tag, form);
        }

        private TaggedLiteral(Symbol tag, Object form)
        {
            _tag = tag;
            _form = form;
        }

        #endregion

        #region ILookup methods

        public object valAt(object key)
        {
            return valAt(key, null);
        }

        public object valAt(object key, object notFound)
        {
            if (FormKeyword.Equals(key))
            {
                return _form;
            }
            else if (TagKeyword.Equals(key))
            {
                return _tag;
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

            TaggedLiteral that = obj as TaggedLiteral;
            if (that == null)
                return false;

            if (_form != null ? !_form.Equals(that._form) : that._form != null) 
                return false;
            if (_tag != null ? !_tag.Equals(that._tag) : that._tag != null) 
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            int result = Util.hash(_tag);
            result = 31 * result + Util.hash(_form);
            return result;
        }

        #endregion
    }
}