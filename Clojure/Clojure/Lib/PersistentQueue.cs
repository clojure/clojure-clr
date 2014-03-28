﻿/**
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
using System.Collections.Generic;

namespace clojure.lang
{
    /// <summary>
    /// A persistent queue. (Conses onto rear, peeks/pops from front.)
    /// </summary>
    /// <remarks>
    /// <para>See Okasaki's Batched Queues.</para>
    /// <para>This differs in that it uses an <see cref="IPersistentList">PersistentVector</see>
    /// as the rear, which is in-order,
    /// so no reversing or suspensions required for persistent use.</para>
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1708:IdentifiersShouldDifferByMoreThanCase")]
    [Serializable]
    public class PersistentQueue : Obj, IPersistentList, ICollection, ICollection<Object>, Counted, IHashEq
    {
        #region Data

        /// <summary>
        /// An empty <see cref="PersistentQueue">PersistentQueue</see>.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "EMPTY")]
        public static readonly PersistentQueue EMPTY = new PersistentQueue(null, 0, null, null);

        /// <summary>
        /// The number of elements in the queue.
        /// </summary>
        protected readonly int _cnt;

        /// <summary>
        /// The front elements of the queue.
        /// </summary>
        protected readonly ISeq _f;

        /// <summary>
        /// The rear elements of the queue.
        /// </summary>
        protected readonly IPersistentVector _r;

        /// <summary>
        /// Cached hash code.
        /// </summary>
        protected int _hash = -1;

        /// <summary>
        /// Caches the hashseq code, when computed.
        /// </summary>
        /// <remarks>The value <value>-1</value> indicates that the hasheq code has not been computed yet.</remarks>
        int _hasheq = -1;

        #endregion

        #region C-tors & factory methods

        /// <summary>
        /// Inititalizes a <see cref="PersistentQueue">PersistentQueue</see> from given metadata, front, rear.
        /// </summary>
        /// <param name="meta"></param>
        /// <param name="f"></param>
        /// <param name="r"></param>
        protected PersistentQueue(IPersistentMap meta, int cnt, ISeq f, IPersistentVector r)
            : base(meta)
        {
            _cnt = cnt;
            _f = f;
            _r = r;
        }

        #endregion

        #region Object overrides

        /// <summary>
        /// Determines if an object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare to.</param>
        /// <returns><value>true</value> if they are the same; <value>false</value> otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is Sequential))
                return false;

            ISeq ms = RT.seq(obj);
            for (ISeq s = seq(); s != null; s = s.next(), ms = ms.next())
                if (ms == null || !Util.equals(s.first(), ms.first()))
                    return false;
            return ms == null;
        }

        /// <summary>
        /// Get the hash code for the current object.
        /// </summary>
        /// <returns>The hash code.</returns>
        /// <remarks>Result is cached.</remarks>
        public override int GetHashCode()
        {
            if (_hash == -1)
            {
                int hash = 1;
                for (ISeq s = seq(); s != null; s = s.next())
                    hash = 31 * hash + (s.first() == null ? 0 : s.first().GetHashCode());
                _hash = hash;
            }
            return _hash;
        }

        #endregion

        #region IObj members

        /// <summary>
        /// Create a copy with new metadata.
        /// </summary>
        /// <param name="meta">The new metadata.</param>
        /// <returns>A copy of the object with new metadata attached.</returns>
        public override IObj withMeta(IPersistentMap meta)
        {
            return new PersistentQueue(meta, _cnt, _f, _r);
        }

        #endregion

        #region IPersistentStack Members

        /// <summary>
        /// Peek at the top (first) element in the stack.
        /// </summary>
        /// <returns>The top (first) element.</returns>
        public object peek()
        {
            return RT.first(_f);
        }

        /// <summary>
        /// Returns a new stack with the top element popped.
        /// </summary>
        /// <returns>The new stack.</returns>
        public IPersistentStack pop()
        {
            if (_f == null) //Java code: hmmmm... pop of empty queue => empty queue?
                return this;
            ISeq f1 = _f.next();
            IPersistentVector r1 = _r;
            if ( f1 == null )
            {
                f1 = RT.seq(_r);
                r1 = null;
            }
            return new PersistentQueue(meta(), _cnt-1, f1, r1);
        }

        #endregion

        #region IPersistentCollection Members

        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        /// <returns>The number of items in the collection.</returns>
        public int count()
        {
            return _cnt;
        }

        /// <summary>
        /// Gets an <see cref="ISeq">ISeq</see> to allow first/rest iteration through the collection.
        /// </summary>
        /// <returns>An <see cref="ISeq">ISeq</see> for iteration.</returns>
        public ISeq seq()
        {
            return _f == null
                ? null
                : new Seq(_f, RT.seq(_r));
        }

        /// <summary>
        /// Returns a new collection that has the given element cons'd on front of the existing collection.
        /// </summary>
        /// <param name="o">An item to put at the front of the collection.</param>
        /// <returns>A new immutable collection with the item added.</returns>
        public IPersistentCollection cons(object o)
        {
            // TODO: What if _f is null and _r is not?
            return _f == null // empty
                ? new PersistentQueue(meta(), _cnt+1, RT.list(o), null)
                : new PersistentQueue(meta(), _cnt+1, _f, (_r ?? PersistentVector.EMPTY).cons(o));
        }

        public IPersistentCollection empty()
        {
            return (IPersistentCollection)EMPTY.withMeta(meta());
        }

        public bool equiv(object o)
        {
            if (!(o is Sequential))
                return false;

            ISeq ms = RT.seq(o);
            for (ISeq s = seq(); s != null; s = s.next(), ms = ms.next())
                if ( ms == null || ! Util.equiv(s.first(),ms.first()))
                    return false;
            return ms == null;
        }


        #endregion

        #region ICollection Members

        public void Add(object item)
        {
            throw new InvalidOperationException("Cannot modify immutable queue");
        }

        public void Clear()
        {
            throw new InvalidOperationException("Cannot modify immutable queue");
        }

        public bool Contains(object item)
        {
            foreach (object element in this)
                if (Util.Equals(element, item))
                    return true;
            return false;
        }

        public void CopyTo(object[] array, int arrayIndex)
        {
            int i = arrayIndex;
            ISeq s;
            for (s = _f; s != null; s = s.next(), i++)
                array[i] = s.first();

            for (s = _r.seq(); s != null; s = s.next(), i++)
                array[i] = s.first();
        }

        public void CopyTo(Array array, int index)
        {
            int i = index;
            ISeq s;
            for (s = _f; s != null; s = s.next(), i++)
                array.SetValue(s.first(), i);

            for (s = _r.seq(); s != null; s = s.next(), i++)
                array.SetValue(s.first(), i);
        }

        public int Count
        {
            get { return count(); }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool IsSynchronized
        {
            get { return true; }
        }

        public object SyncRoot
        {
            get { return true; }
        }

        public bool Remove(object item)
        {
            throw new InvalidOperationException("Cannot modify immutable queue");
        }

        #endregion

        #region IEnumerable Members

        public IEnumerator<object> GetEnumerator()
        {
            ISeq s;
            for (s = _f; s != null; s = s.next())
                yield return s.first();

            if (_r != null)
            {
                for (s = _r.seq(); s != null; s = s.next())
                    yield return s.first();
            }
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            ISeq s;
            for (s = _f; s != null; s = s.next())
                yield return s.first();

            if (_r != null)
            {
                for (s = _r.seq(); s != null; s = s.next())
                    yield return s.first();
            }
        }

        #endregion

        #region IHashEq members

        public int hasheq()
        {
            if (_hasheq == -1)
            {
                //int hash = 1;
                //for (ISeq s = seq(); s != null; s = s.next())
                //{
                //    hash = 31 * hash + Util.hasheq(s.first());
                //}
                //_hasheq = hash;
                _hasheq = Murmur3.HashOrdered(this);
            }
            return _hasheq;
        }
        #endregion

        /// <summary>
        /// Represents an <see cref="ISeq">ISeq</see> over a <see cref="PersistentQueue">PersistentQueue</see>.
        /// </summary>
        [Serializable]
        sealed class Seq : ASeq
        {
            #region Data

            /// <summary>
            /// The front elements.
            /// </summary>
            private readonly ISeq _f;

            /// <summary>
            /// The rear elements.
            /// </summary>
            private readonly ISeq _rseq;

            #endregion

            #region C-tors & factory methods

            /// <summary>
            /// Initializes a <see cref="Seq">PersistentQueue.Seq</see> from given front and rear elements.
            /// </summary>
            /// <param name="f">The front elements.</param>
            /// <param name="rseq">The rear elements.</param>
            internal Seq(ISeq f, ISeq rseq)
            {
                _f = f;
                _rseq = rseq;
            }

            /// <summary>
            /// Initializes a <see cref="Seq">PersistentQueue.Seq</see> from given metadata and front and rear elements.
            /// </summary>
            /// <param name="meta">The metadata to attach.</param>
            /// <param name="f">The front elements.</param>
            /// <param name="rseq">The rear elements.</param>
            internal Seq(IPersistentMap meta, ISeq f, ISeq rseq)
                : base(meta)
            {
                _f = f;
                _rseq = rseq;
            }

            #endregion

            #region IObj members

            /// <summary>
            /// Create a copy with new metadata.
            /// </summary>
            /// <param name="meta">The new metadata.</param>
            /// <returns>A copy of the object with new metadata attached.</returns>
            public override IObj withMeta(IPersistentMap meta)
            {
                return new Seq(meta, _f, _rseq);
            }

            #endregion

            #region IPersistentCollection members

            /// <summary>
            /// Gets the number of items in the collection.
            /// </summary>
            /// <returns>The number of items in the collection.</returns>
            public override int count()
            {
                return RT.count(_f) + RT.count(_rseq);
            }

            #endregion

            #region ISeq members

            /// <summary>
            /// Gets the first item.
            /// </summary>
            /// <returns>The first item.</returns>
            public override object first()
            {
                return _f.first();
            }

            /// <summary>
            /// Return a seq of the items after the first.  Calls <c>seq</c> on its argument.  If there are no more items, returns nil."
            /// </summary>
            /// <returns>A seq of the items after the first, or <c>nil</c> if there are no more items.</returns>
            public override ISeq next()
            {
                ISeq f1 = _f.next();
                ISeq r1 = _rseq;
                if (f1 == null)
                {
                    if (_rseq == null)
                        return null;
                    f1 = _rseq;
                    r1 = null;
                }
                return new Seq(f1, r1);
            }



            #endregion
        }


    }
}
