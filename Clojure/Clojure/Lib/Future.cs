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
 **/

using System;
using System.Threading;

namespace clojure.lang
{
    /// <summary>
    /// Implements IDeref and java.util.concurrent.Future, like the proxy in JVM clojure core.
    /// </summary>
    public class Future : IDeref
    {
        #region Data

        readonly Thread _t;
        readonly ManualResetEvent _started = new ManualResetEvent(false);
        object _value;
        Exception _error;
        bool _cancelled;

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
        public bool isDone()
        {
            return _t.Join(0);
        }

        /// <summary>
        /// Attempts to abort the future.
        /// </summary>
        /// <returns>True if the attempt succeeds. False if the task already completed
        /// or was cancelled previously.</returns>
        public bool cancel()
        {
            // Already completed or cancelled.
            if (_t.Join(0))
                return false;

            // Don't abort until the task thread has established its ThreadAbortException catch block.
            _started.WaitOne();

            _t.Abort();
            _t.Join();
            return _cancelled;
        }

        public bool isCancelled()
        {
            return _cancelled;
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
                throw new Exception("Future has an error", _error);
            }
            return _value;
        }

        #endregion
    }

    public class FutureAbortedException : Exception
    {
        public FutureAbortedException() { }

        public FutureAbortedException(string msg) : base(msg) { }

        public FutureAbortedException(string msg, Exception inner) : base(msg, inner) { }
    }
}

