/**
 *   Copyright (c) David Miller. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang
{
    public class ChunkedCons : ASeq, IChunkedSeq
    {
        #region Data

        readonly Indexed _chunk;
        readonly ISeq _more;
        readonly int _offset;

        #endregion

        #region C-tors

        ChunkedCons(IPersistentMap meta, Indexed chunk, int offset, ISeq more)
            : base(meta)
        {
            _chunk = chunk;
            _offset = offset;
            _more = more;
        }

        public ChunkedCons(Indexed chunk, ISeq more)
            : this(chunk, 0, more)
        {
        }

        public ChunkedCons(Indexed chunk, int offset, ISeq more)
        {
            _chunk = chunk;
            _offset = offset;
            _more = more;
        }

        #endregion

        #region IObj methods
        
        public override IObj withMeta(IPersistentMap meta)
        {
            return (meta == _meta)
                ? this
                :new ChunkedCons(meta, _chunk, _offset, _more);
        }

        #endregion

        #region ISeq methods

        public override object first()
        {
            return _chunk.nth(_offset);
        }

        public override ISeq next()
        {
            if (_offset + 1 < _chunk.count())
                return new ChunkedCons(_chunk, _offset + 1, _more);
            return chunkedNext();
        }

        #endregion

        #region IChunkedSeq Members

        public Indexed chunkedFirst()
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
