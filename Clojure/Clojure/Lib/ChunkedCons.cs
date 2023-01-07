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
    [Serializable]
    public class ChunkedCons : ASeq, IChunkedSeq
    {
        #region Data

        readonly IChunk _chunk;
        readonly ISeq _more;

        #endregion

        #region C-tors

        ChunkedCons(IPersistentMap meta, IChunk chunk, ISeq more)
            : base(meta)
        {
            _chunk = chunk;
            _more = more;
        }

        public ChunkedCons(IChunk chunk, ISeq more)
            : this(null,chunk,more)
        {
        }

        #endregion

        #region IObj methods
        
        public override IObj withMeta(IPersistentMap meta)
        {
            return (meta == _meta)
                ? this
                :new ChunkedCons(meta, _chunk, _more);
        }

        #endregion

        #region ISeq methods

        public override object first()
        {
            return _chunk.nth(0);
        }

        public override ISeq next()
        {
            if (_chunk.count() > 1)
                return new ChunkedCons(_chunk.dropFirst(), _more);
            return chunkedNext();
        }

        public override ISeq more()
        {
            if (_chunk.count() > 1)
                return new ChunkedCons(_chunk.dropFirst(), _more);
            if (_more == null)
                return PersistentList.EMPTY;
            return _more;
        }

        #endregion

        #region IChunkedSeq Members

        public IChunk chunkedFirst()
        {
            return _chunk;
        }

        public ISeq chunkedNext()
        {
            return chunkedMore().seq();
        }

        public ISeq chunkedMore()
        {
            if (_more == null)
                return PersistentList.EMPTY;
            return _more;
        }

        #endregion

        #region IPersistentCollection Members


        //public new IPersistentCollection cons(object o)
        //{
        //    throw new NotImplementedException();
        //}

        #endregion
    }
}
