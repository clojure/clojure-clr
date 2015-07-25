/**
 * Copyright (c) Rich Hickey. All rights reserved.
 * The use and distribution terms for this software are covered by the
 * Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 * which can be found in the file epl-v10.html at the root of this distribution.
 * By using this software in any fashion, you are agreeing to be bound by
 * the terms of this license.
 * You must not remove this notice, or any other, from this software.
 */

/* ghadi shayban Sep 24, 2014 */ 

/**
 *   Author: David Miller
 **/

using System;
using System.Collections;
using System.Collections.Generic;

namespace clojure.lang
{
    public sealed class RecordEnumerable: IEnumerable<Object>, IEnumerable
    {

        #region Data

        readonly int _basecnt;
        readonly ILookup _rec;
        readonly IPersistentVector _baseFields;
        readonly IEnumerator _extmap;
        
        #endregion

        #region Ctors and factories

        public RecordEnumerable(ILookup rec, IPersistentVector baseFields, IEnumerator extmap)
        {
            _rec = rec;
            _baseFields = baseFields;
            _basecnt = baseFields.count();
            _extmap = extmap;
        }

        #endregion

        public IEnumerator<object> GetEnumerator()
        {
            for (int i = 0; i < _basecnt; i++)
            {
                object k = _baseFields.nth(i);
                yield return Tuple.create(k,_rec.valAt(k));
            }

            while (_extmap.MoveNext())
                yield return _extmap.Current;

        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
