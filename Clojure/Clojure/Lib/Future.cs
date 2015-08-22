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
 *   Author: Shawn Hoover
 *   Edited: David Miller
 **/

using System;
using System.Threading;
using System.Runtime.Serialization;

namespace clojure.lang
{
    /// <summary>
    /// Implements IDeref and java.util.concurrent.Future, like the proxy in JVM clojure core.
    /// </summary>
    public class Future : IDeref, IBlockingDeref, IPending, IDisposable
    {
        #region Data

        readonly Thread _t;
        readonly ManualResetEvent _started = new ManualResetEvent(false);
        object _value;
        Exception _error;
        bool _cancelled;
        bool _disposed = false;

        #endregion

        #region C-tors

        public Future(IFn fn)
        {
            // TODO: Use a cached thread pool when agents have one.
            _t = new Thread(new ParameterizedThreadStart(ComputeFuture));
            _t.Name = "Future";
            _t.Start(fn);
        }

        #endregion

        #region Implementation

        // Worker method to execute the task.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void ComputeFuture(object state)
        {
            try
            {
                _started.Set();

                _value = ((IFn)state).invoke();
            }
            catch (ThreadAbortException)
            {
                _cancelled = true;
                Thread.ResetAbort();
            }
            catch (Exception ex)
            {
                _error = ex;
            }
        }

        #endregion

        #region Java-compatible interface

        /// <summary>
        ///
        /// </summary>
        /// <returns>True if the task completed due to normal completion, cancellation,
        /// or an exception.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "is")]
        public bool isDone()
        {
            return _t.Join(0);
        }

        /// <summary>
        /// Attempts to abort the future.
        /// </summary>
        /// <returns>True if the attempt succeeds. False if the task already completed
        /// or was cancelled previously.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "cancel")]
        public bool cancel(bool mayInterruptIfRunning)
        {
            // Already completed or cancelled.
            if (_t.Join(0))
                return false;

            if (mayInterruptIfRunning)
            {
                // Don't abort until the task thread has established its ThreadAbortException catch block.
                _started.WaitOne();

                _t.Abort();
            }
            _t.Join();
            return _cancelled;
        }

        /// <summary>
        /// Attempts to abort the future.
        /// </summary>
        /// <returns>True if the attempt succeeds. False if the task already completed
        /// or was cancelled previously.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "cancel")]
        public bool cancel()
        {
            return cancel(true);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Cancelled"), 
         System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "is")]
        public bool isCancelled()
        {
            return _cancelled;
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "get")]
        public object get()
        {
            return deref();
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "get")]        
        public object get(int millis)
        {
            if (!_t.Join(millis))
                throw new FutureTimeoutException();
            if (_cancelled)
            {
                throw new FutureAbortedException();
            }
            if (_error != null)
            {
                throw new InvalidOperationException("Future has an error", _error);
            }
            return _value;      
        }

        #endregion

        #region IDeref Members

        public object deref()
        {
            _t.Join();
            if (_cancelled)
            {
                throw new FutureAbortedException();
            }
            if (_error != null)
            {
                throw new InvalidOperationException("Future has an error", _error);
            }
            return _value;
        }

        #endregion

        #region IPending members

        public bool isRealized()
        {
            return isDone();
        }

        #endregion

        #region  IBlockingDeref

        public object deref(long ms, object timeoutValue)
        {
            if (_t.Join((int)ms))
                return _value;
            return timeoutValue;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    ((IDisposable)_started).Dispose(); 
                }

                _disposed = true;
            }
        }

        #endregion
    }

    [Serializable]
    public class FutureAbortedException : Exception
    {
        #region C-tors

        public FutureAbortedException() { }

        public FutureAbortedException(string msg) : base(msg) { }

        public FutureAbortedException(string msg, Exception inner) : base(msg, inner) { }

        protected FutureAbortedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion
    }

    [Serializable]
    public class FutureTimeoutException : Exception
    {
        #region C-tors

        public FutureTimeoutException() { }

        public FutureTimeoutException(string msg) : base(msg) { }

        public FutureTimeoutException(string msg, Exception inner) : base(msg, inner) { }

        protected FutureTimeoutException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion
    }
}

