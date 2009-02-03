using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Runtime.CompilerServices;

namespace clojure.lang
{

    /// <summary>
    /// Implements a stream running over an IEnumerator.
    /// </summary>
    public class IteratorStream : IStream
    {
        #region Data

        /// <summary>
        /// The IEnumerator being streamed.
        /// </summary>
        private readonly IEnumerator _iter;

        #endregion

        #region Ctors and factory methods

        /// <summary>
        /// Constructs an <see cref="IteratorStream">IteratorStream</see> over an IEnumerator.
        /// </summary>
        /// <param name="iter">The IEnumerator to stream over.</param>
        public IteratorStream(IEnumerator iter)
        {
            _iter = iter;
        }

        #endregion

        #region IStream Members

        /// <summary>
        /// Get the next value in the stream.
        /// </summary>
        /// <returns>The next value.</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public object next()
        {
            if (_iter.MoveNext())
                return _iter.Current;
            return RT.eos();
        }

       

        #endregion
    }
}
