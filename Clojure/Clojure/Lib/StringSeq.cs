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
    /// <summary>
    /// A sequence of characters from a string.
    /// </summary>
    [Serializable]
    public class StringSeq : ASeq, IndexedSeq, IDrop, IReduceInit
    {
        #region Data

        /// <summary>
        /// The string providing the characters.
        /// </summary>
        private readonly string _s;

        public string S
        {
            get { return _s; }
        } 


        /// <summary>
        /// Current position in the string.
        /// </summary>
        private readonly int _i;

        public int I
        {
            get { return _i; }
        }




        #endregion

        #region C-tors and factory methods

        /// <summary>
        /// Create a <see cref="StringSeq">StringSeq</see> from a String.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        static public StringSeq create(string s)
        {
            return s.Length == 0
                ? null
                : new StringSeq(null, s, 0);
        }

        /// <summary>
        /// Construct a <see cref="StringSeq">StringSeq</see> from given metadata, string, position.
        /// </summary>
        /// <param name="meta">The metadata to attach.</param>
        /// <param name="s">The string.</param>
        /// <param name="i">The current position.</param>
        StringSeq(IPersistentMap meta, string s, int i)
            : base(meta)
        {
            this._s = s;
            this._i = i;
        }

        #endregion

        #region ISeq members

        /// <summary>
        /// Gets the first item.
        /// </summary>
        /// <returns>The first item.</returns>
        public override object first()
        {
            return _s[_i];
        }

        /// <summary>
        /// Return a seq of the items after the first.  Calls <c>seq</c> on its argument.  If there are no more items, returns nil."
        /// </summary>
        /// <returns>A seq of the items after the first, or <c>nil</c> if there are no more items.</returns>
        public override ISeq next()
        {
            return _i + 1 < _s.Length
                ? new StringSeq(_meta, _s, _i + 1)
                : null;
         }

        #endregion

        #region IPersistentCollection members

        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        /// <returns>The number of items in the collection.</returns>
         public override int count()
        {
            return _i < _s.Length ? _s.Length - _i : 0;
        }

        #endregion

        #region IObj methods

         /// <summary>
         /// Create a copy with new metadata.
         /// </summary>
         /// <param name="meta">The new metadata.</param>
         /// <returns>A copy of the object with new metadata attached.</returns>
         public override IObj withMeta(IPersistentMap meta)
        {
            return meta == _meta
                ? this
                : new StringSeq(meta, _s, _i);
        }

        #endregion

        #region IndexedSeq Members

         /// <summary>
         /// Gets the index associated with this sequence.
         /// </summary>
         /// <returns>The index associated with this sequence.</returns>
         public int index()
        {
            return _i;
        }

        #endregion

        #region IDrop members

        public Sequential drop(int n)
        {
            int ii = _i + n;
            if (ii < _s.Length)
                return new StringSeq(_meta, _s, ii);
            else
                return null;
        }

        #endregion

        #region IReduceInit members

        public object reduce(IFn f, object start)
        {
            object acc = start;
            for (int ii = _i; ii < _s.Length; ii++)
            {
                acc=f.invoke(acc, _s[ii]);
                if (RT.isReduced(acc))
                    return ((IDeref)acc).deref();
            }
            return acc;
        }

        #endregion
    }
}
