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
using System.Collections;

namespace clojure.lang
{

    /// <summary>
    /// A persistent vector based on an array.  Holds a lazily-allocated <see cref="PersistentVector">PersistentVector</see>
    /// if operations such as <see cref="LazilyPersistentVector.assoc()">assoc()</see> 
    /// are called that require a true persistent collection.
    /// </summary>
    public static class LazilyPersistentVector 
    {
        #region C-tors and factory methods

        /// <summary>
        /// Create a <see cref="LazilyPersistentVector">LazilyPersistentVector</see> for an array of items.
        /// </summary>
        /// <param name="items">An array of items</param>
        /// <returns>A <see cref="LazilyPersistentVector">LazilyPersistentVector</see>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        static public IPersistentVector createOwning(params object[] items)
        {
            //if (items.Length <= Tuple.MAX_SIZE)
            //    return Tuple.createFromArray(items);
            //else 
            if (items.Length <= 32)
                return new PersistentVector(items.Length, 5, PersistentVector.EmptyNode, items);
            return PersistentVector.create(items);

                //: new LazilyPersistentVector(null, items, null);
        }

        //static int fcount(Object c)
        //{
        //    if (c == null)  // not in Java version.  How did this pass tests?
        //        return 0;

        //    Counted ctd = c as Counted;
        //    if (ctd != null)
        //        return ctd.count();

        //    String s = c as String;   // not in Java version.  How did this pass tests?
        //    if (s != null)
        //        return s.Length;

        //    return ((ICollection)c).Count;
        //}

        /// <summary>
        /// Create a <see cref="LazilyPersistentVector">LazilyPersistentVector</see> from an ICollection of items.
        /// </summary>
        /// <param name="coll">The collection of items.</param>
        /// <returns>A <see cref="LazilyPersistentVector">LazilyPersistentVector</see>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        static public IPersistentVector create(object obj)
        {
            //if ((obj is Counted || RT.SupportsRandomAccess(obj))
            //    && fcount(obj) <= Tuple.MAX_SIZE)
            //    return Tuple.createFromColl(obj);

            if (obj is IReduceInit ri)
                return PersistentVector.create(ri);
            if (obj is ISeq)
                return PersistentVector.create(RT.seq(obj));

            if (obj is IEnumerable ie)
                return PersistentVector.create1(ie);

            return createOwning(RT.toArray(obj));
        }

        #endregion
    }
}
