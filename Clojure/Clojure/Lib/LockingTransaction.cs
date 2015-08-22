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
using System.Collections.Generic;
using System.Threading;
using System.Runtime.Serialization;

namespace clojure.lang
{
    /// <summary>
    /// Provides transaction semantics for <see cref="Agent">Agent</see>s, <see cref="Ref">Ref</see>s, etc.
    /// </summary>
    public class LockingTransaction
    {
        #region Constants & enums

        /// <summary>
        /// The number of times to retry a transaction in case of a conflict.
        /// </summary>
        public const int RetryLimit = 10000;

        /// <summary>
        /// How long to wait for a lock.
        /// </summary>
        public const int LockWaitMsecs = 100;

        /// <summary>
        /// How old another transaction must be before we 'barge' it.
        /// </summary>
        /// <remarks>
        /// Java version has BARGE_WAIT_NANOS, set at 10*1000000.
        /// If I'm thinking correctly tonight, that's 10 milliseconds.
        /// Ticks here are 100 nanos, so we should have  10 * 1000000/100 = 100000.
        /// </remarks>
        public const long BargeWaitTicks = 100000;


        // State constants
        // Should be an enum, but we want Interlocked capability

        /// <summary>
        /// Value: The transaction is running.
        /// </summary>
        const int RUNNING = 0;

        /// <summary>
        /// Value: The transaction is committing.
        /// </summary>
        const int COMMITTING = 1;

        /// <summary>
        /// Value: the transaction is getting ready to retry.
        /// </summary>
        const int RETRY = 2;

        /// <summary>
        /// The transaction has been killed.
        /// </summary>
        const int KILLED = 3;

        /// <summary>
        /// The transaction has been committed.
        /// </summary>
        const int COMMITTED = 4;
        
        #endregion

        #region supporting classes

        /// <summary>
        /// Exception thrown when a retry is necessary.
        /// </summary>
        [Serializable]
        public class RetryEx : Exception
        {
            #region C-tors

            public RetryEx()
            {
            }

            public RetryEx(String message)
                : base(message)
            {
            }

            public RetryEx(String message, Exception innerException)
                : base(message, innerException)
            {
            }

            protected RetryEx(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }

            #endregion

        }

        /// <summary>
        /// Exception thrown when a transaction has been aborted.
        /// </summary>
        [Serializable]
        public class AbortException : Exception
        {
            #region C-tors

            public AbortException()
            {
            }

            public AbortException(String message)
                : base(message)
            {
            }

            public AbortException(String message, Exception innerException)
                : base(message, innerException)
            {
            }

            protected AbortException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }

            #endregion
        }

        /// <summary>
        /// The current state of a transaction.
        /// </summary>
        public class Info
        {
            #region Data

            /// <summary>
            /// The status of the transaction.
            /// </summary>
            readonly AtomicInteger _status;

            /// <summary>
            /// The status of the transaction.
            /// </summary>
            internal AtomicInteger Status
            {
                get { return _status; }
            }

            /// <summary>
            /// The start point of the transaction.
            /// </summary>
            readonly long _startPoint;

            /// <summary>
            /// The start point of the transaction.
            /// </summary>
            public long StartPoint
            {
                get { return _startPoint; }
            }

            readonly CountDownLatch _latch;

            public CountDownLatch Latch
            {
                get { return _latch; }
            } 


            #endregion

            #region C-tors

            /// <summary>
            /// Construct an info.
            /// </summary>
            /// <param name="status">Current status.</param>
            /// <param name="startPoint">Start point.</param>
            public Info(int status, long startPoint)
            {
                _status = new AtomicInteger(status);
                _startPoint = startPoint;
                _latch = new CountDownLatch(1);
            }

            #endregion

            #region Other

            /// <summary>
            /// Is the transaction running?
            /// </summary>
            public bool IsRunning
            {
                get
                {
                    long s = _status.get();
                    return s == RUNNING || s == COMMITTING;
                }
            }

            #endregion
        }

        /// <summary>
        /// Pending call of a function on arguments.
        /// </summary>
        class CFn
        {
            #region Data

            /// <summary>
            ///  The function to be called.
            /// </summary>
            readonly IFn _fn;

            /// <summary>
            ///  The function to be called.
            /// </summary>
            public IFn Fn
            {
                get { return _fn; }
            }

            /// <summary>
            /// The arguments to the function.
            /// </summary>
            readonly ISeq _args;

            /// <summary>
            /// The arguments to the function.
            /// </summary>
            public ISeq Args
            {
                get { return _args; }
            }

            #endregion

            #region C-tors

            /// <summary>
            /// Construct one.
            /// </summary>
            /// <param name="fn">The function to invoke.</param>
            /// <param name="args">The arguments to invoke the function on.</param>
            public CFn(IFn fn, ISeq args)
            {
                _fn = fn;
                _args = args;
            }

            #endregion
        }

        #endregion

        #region Data

        /// <summary>
        /// The transaction running on the current thread.  (Thread-local.)
        /// </summary>
        [ThreadStatic]
        private static LockingTransaction _transaction;

        /// <summary>
        /// The current point.
        /// </summary>
        /// <remarks>
        /// <para>Used to provide a total ordering on transactions 
        /// for the purpose of determining preference on transactions 
        /// when there are conflicts.  
        /// Transactions consume a point for init, for each retry, 
        /// and on commit if writing.</para>
        /// </remarks>
        private static readonly AtomicLong _lastPoint = new AtomicLong();

        /// <summary>
        ///  The state of the transaction.
        /// </summary>
        /// <remarks>Encapsulated so things like Refs can look.</remarks>
        Info _info;

        /// <summary>
        /// The point at the start of the current retry (or first try).
        /// </summary>
        long _readPoint;

        /// <summary>
        /// The point at the start of the transaction.
        /// </summary>
        long _startPoint;

        /// <summary>
        /// The system ticks at the start of the transaction.
        /// </summary>
        long _startTime;

        /// <summary>
        /// Cached retry exception.
        /// </summary>
        readonly RetryEx _retryex = new RetryEx();

        /// <summary>
        /// Agent actions pending on this thread.
        /// </summary>
        readonly List<Agent.Action> _actions = new List<Agent.Action>();

        /// <summary>
        /// Ref assignments made in this transaction (both sets and commutes).
        /// </summary>
        readonly Dictionary<Ref, Object> _vals = new Dictionary<Ref, Object>();

        /// <summary>
        /// Refs that have been set in this transaction.
        /// </summary>
        readonly HashSet<Ref> _sets = new HashSet<Ref>();

        /// <summary>
        /// Ref commutes that have been made in this transaction.
        /// </summary>
        readonly SortedDictionary<Ref, List<CFn>> _commutes = new SortedDictionary<Ref, List<CFn>>();

        /// <summary>
        /// The set of Refs holding read locks.
        /// </summary>
        readonly HashSet<Ref> _ensures = new HashSet<Ref>(); 

        #endregion

        #region Debugging

        //string TId() 
        //{ 
        //    return String.Format("<{0}:{1}>", Thread.CurrentThread.ManagedThreadId, _readPoint);
        //}

        #endregion

        #region  Point manipulation

        /// <summary>
        /// Get a new read point value.
        /// </summary>
        void GetReadPoint()
        {
            _readPoint = _lastPoint.incrementAndGet();
        }

        /// <summary>
        /// Get a commit point value.
        /// </summary>
        /// <returns></returns>
        static long GetCommitPoint()
        {
            return _lastPoint.incrementAndGet();
        }

        #endregion

        #region Actions

        /// <summary>
        /// Stop this transaction.
        /// </summary>
        /// <param name="status">The new status.</param>
        void Stop(int status)
        {
            if (_info != null)
            {
                lock (_info)
                {
                    _info.Status.set(status);
                    _info.Latch.CountDown();
                }
                _info = null;
                _vals.Clear();
                _sets.Clear();
                _commutes.Clear();
                // Java commented out: _actions.Clear();
            }
        }

        void TryWriteLock(Ref r)
        {
            try
            {
                if (!r.TryEnterWriteLock(LockWaitMsecs))
                    throw _retryex;
            }
            catch (ThreadInterruptedException )
            {
                throw _retryex;
            }
        }

        void ReleaseIfEnsured(Ref r)
        {
            if (_ensures.Contains(r))
            {
                _ensures.Remove(r);
                r.ExitReadLock();
            }
        }


        object BlockAndBail(Info refinfo)
        {
            //stop prior to blocking
            Stop(RETRY);
            try
            {
                refinfo.Latch.Await(LockWaitMsecs);
            }
            catch (ThreadInterruptedException)
            {
                //ignore
            }
            throw _retryex;
        }


        /// <summary>
        /// Lock a ref.
        /// </summary>
        /// <param name="r">The ref to lock.</param>
        /// <returns>The most recent value of the ref.</returns>
        object Lock(Ref r)
        {
            // can't upgrade read lock, so release it.
            ReleaseIfEnsured(r);

            bool unlocked = true;
            try
            {
                TryWriteLock(r);
                unlocked = false;

                if (r.CurrentValPoint() > _readPoint)
                    throw _retryex;

                Info refinfo = r.TInfo;

                // write lock conflict
                if (refinfo != null && refinfo != _info && refinfo.IsRunning)
                {
                    if (!Barge(refinfo))
                    {
                        r.ExitWriteLock();
                        unlocked = true;
                        return BlockAndBail(refinfo);
                    }
                }

                r.TInfo = _info;
                return r.TryGetVal();
            }
            finally
            {
                if (!unlocked)
                {
                    r.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Kill this transaction.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        void Abort()
        {
            Stop(KILLED);
            throw new AbortException();
        }

        /// <summary>
        /// Determine if sufficient clock time has elapsed to barge another transaction.
        /// </summary>
        /// <returns><value>true</value> if enough time has elapsed; <value>false</value> otherwise.</returns>
        private bool BargeTimeElapsed()
        {
            return Environment.TickCount - _startTime > BargeWaitTicks;
        }

        /// <summary>
        /// Try to barge a conflicting transaction.
        /// </summary>7
        /// <param name="refinfo">The info on the other transaction.</param>
        /// <returns><value>true</value> if we killed the other transaction; <value>false</value> otherwise.</returns>
        private bool Barge(Info refinfo)
        {
            bool barged = false;
            // if this transaction is older
            //   try to abort the other
            if (BargeTimeElapsed() && _startPoint < refinfo.StartPoint)
            {
                barged = refinfo.Status.compareAndSet(RUNNING, KILLED);
                if (barged)
                    refinfo.Latch.CountDown();
            }
            return barged;
        }

        /// <summary>
        /// Get the transaction running on this thread (throw exception if no transaction). 
        /// </summary>
        /// <returns>The running transaction.</returns>
        public static LockingTransaction GetEx()
        {
            LockingTransaction t = _transaction;
            if (t == null || t._info == null)
                throw new InvalidOperationException("No transaction running");
            return t;
        }

        /// <summary>
        /// Get the transaction running on this thread (or null if no transaction).
        /// </summary>
        /// <returns>The running transaction if there is one, else <value>null</value>.</returns>
        static internal LockingTransaction GetRunning()
        {
            LockingTransaction t = _transaction;
            if (t == null || t._info == null)
                return null;
            return t;
        }

        /// <summary>
        /// Is there a transaction running on this thread?
        /// </summary>
        /// <returns><value>true</value> if there is a transaction running on this thread; <value>false</value> otherwise.</returns>
        /// <remarks>Initial lowercase in name for core.clj compatibility.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        public static bool isRunning()
        {
            return GetRunning() != null;
        }

        /// <summary>
        /// Invoke a function in a transaction
        /// </summary>
        /// <param name="fn">The function to invoke.</param>
        /// <returns>The value computed by the function.</returns>
        /// <remarks>Initial lowercase in name for core.clj compatibility.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        public static object runInTransaction(IFn fn)
        {
            // TODO: This can be called on something more general than  an IFn.
            // We can could define a delegate for this, probably use ThreadStartDelegate.
            // Should still have a version that takes IFn.
            LockingTransaction t = _transaction;
            Object ret;

            if (t == null)
            {
                _transaction = t = new LockingTransaction();
                try
                {
                    ret = t.Run(fn);
                }
                finally
                {
                    _transaction = null;
                }
            }
            else
            {
                if (t._info != null)
                    ret = fn.invoke();
                else
                    ret = t.Run(fn);
            }

            return ret;
        }

        class Notify
        {
            public readonly Ref _ref;
            public readonly object _oldval;
            public readonly object _newval;

            public Notify(Ref r, object oldval, object newval)
            {
                _ref = r;
                _oldval = oldval;
                _newval = newval;
            }
        }


        /// <summary>
        /// Start a transaction and invoke a function.
        /// </summary>
        /// <param name="fn">The function to invoke.</param>
        /// <returns>The value computed by the function.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        object Run(IFn fn)
        {
            // TODO: Define an overload called on ThreadStartDelegate or something equivalent.

            bool done = false;
            object ret = null;
            List<Ref> locked = new List<Ref>();
            List<Notify> notify = new List<Notify>();

            for (int i = 0; !done && i < RetryLimit; i++)
            {
                try
                {
                    GetReadPoint();
                    if (i == 0)
                    {
                        _startPoint = _readPoint;
                        _startTime = Environment.TickCount;
                    }

                    _info = new Info(RUNNING, _startPoint);
                    ret = fn.invoke();

                    // make sure no one has killed us before this point,
                    // and can't from now on
                    if (_info.Status.compareAndSet(RUNNING, COMMITTING))
                    {
                        foreach (KeyValuePair<Ref, List<CFn>> pair in _commutes)
                        {
                            Ref r = pair.Key;
                            if (_sets.Contains(r))
                                continue;

                            bool wasEnsured = _ensures.Contains(r);
                            // can't upgrade read lock, so release
                            ReleaseIfEnsured(r);
                            TryWriteLock(r);
                            locked.Add(r);

                            if (wasEnsured && r.CurrentValPoint() > _readPoint )
                                throw _retryex;

                            Info refinfo = r.TInfo;
                            if ( refinfo != null && refinfo != _info && refinfo.IsRunning)
                            {
                                if (!Barge(refinfo))
                                {
                                    throw _retryex;
                                }
                            }
                            object val = r.TryGetVal();
                            _vals[r] = val;
                            foreach (CFn f in pair.Value)
                                _vals[r] = f.Fn.applyTo(RT.cons(_vals[r], f.Args));
                        }
                        foreach (Ref r in _sets)
                        {
                            TryWriteLock(r);
                            locked.Add(r);
                        }
                        // validate and enqueue notifications
                        foreach (KeyValuePair<Ref, object> pair in _vals)
                        {
                            Ref r = pair.Key;
                            r.Validate(pair.Value);
                        }

                        // at this point, all values calced, all refs to be written locked
                        // no more client code to be called
                        long commitPoint = GetCommitPoint();
                        foreach (KeyValuePair<Ref, object> pair in _vals)
                        {
                            Ref r = pair.Key;
                            object oldval = r.TryGetVal();
                            object newval = pair.Value;
                          
                            r.SetValue(newval, commitPoint);
                            if (r.getWatches().count() > 0)
                                notify.Add(new Notify(r, oldval, newval));
                        }

                        done = true;
                        _info.Status.set(COMMITTED);
                    }
                }
                catch (RetryEx)
                {
                    // eat this so we retry rather than fall out
                }
                catch (Exception ex)
                {
                    if (ContainsNestedRetryEx(ex))
                    {
                        // Wrapped exception, eat it.
                    }
                    else
                    {
                        throw;
                    }
                }
                finally
                {
                    for (int k = locked.Count - 1; k >= 0; --k)
                    {
                        locked[k].ExitWriteLock();
                    }
                    locked.Clear();
                    foreach (Ref r in _ensures)
                        r.ExitReadLock();
                    _ensures.Clear();
                    Stop(done ? COMMITTED : RETRY);
                    try
                    {
                        if (done) // re-dispatch out of transaction
                        {
                            foreach (Notify n in notify)
                            {
                                n._ref.NotifyWatches(n._oldval, n._newval);
                            }
                            foreach (Agent.Action action in _actions)
                            {
                                Agent.DispatchAction(action);
                            }
                        }
                    }
                    finally
                    {
                        notify.Clear();
                        _actions.Clear();
                    }
                }
            }
            if (!done)
                throw new InvalidOperationException("Transaction failed after reaching retry limit");
            return ret;
        }

        /// <summary>
        /// Determine if the exception wraps a <see cref="RetryEx">RetryEx</see> at some level.
        /// </summary>
        /// <param name="ex">The exception to test.</param>
        /// <returns><value>true</value> if there is a nested  <see cref="RetryEx">RetryEx</see>; <value>false</value> otherwise.</returns>
        /// <remarks>Needed because sometimes our retry exceptions get wrapped.  You do not want to know how long it took to track down this problem.</remarks>
        private static bool ContainsNestedRetryEx(Exception ex)
        {
            for (Exception e = ex; e != null; e = e.InnerException)
                if (e is RetryEx)
                    return true;
            return false;
        }

        /// <summary>
        /// Add an agent action sent during the transaction to a queue.
        /// </summary>
        /// <param name="action">The action that was sent.</param>
        internal void Enqueue(Agent.Action action)
        {
            _actions.Add(action);
        }

        /// <summary>
        /// Get the value of a ref most recently set in this transaction (or prior to entering).
        /// </summary>
        /// <param name="r"></param>
        /// <param name="tvals"></param>
        /// <returns>The value.</returns>
        internal object DoGet(Ref r)
        {
            if (!_info.IsRunning)
                throw _retryex;
            if (_vals.ContainsKey(r))
            {
                return _vals[r];
            }
            try
            {
                r.EnterReadLock();
                if (r.TVals == null)
                    throw new InvalidOperationException(r.ToString() + " is not bound.");
                Ref.TVal ver = r.TVals;
                do
                {
                    if (ver.Point <= _readPoint)
                    {
                        return ver.Val;
                    }
                } while ((ver = ver.Prior) != r.TVals);
            }
            finally
            {
                r.ExitReadLock();
            }
            // no version of val precedes the read point
            r.AddFault();
            throw _retryex;
        }

        /// <summary>
        /// Set the value of a ref inside the transaction.
        /// </summary>
        /// <param name="r">The ref to set.</param>
        /// <param name="val">The value.</param>
        /// <returns>The value.</returns>
        internal object DoSet(Ref r, object val)
        {
            if (!_info.IsRunning)
                throw _retryex;
            if (_commutes.ContainsKey(r))
                throw new InvalidOperationException("Can't set after commute");
            if (!_sets.Contains(r))
            {
                _sets.Add(r);
                Lock(r);
            }
            _vals[r] = val;
            return val;
        }

        /// <summary>
        /// Touch a ref.  (Lock it.)
        /// </summary>
        /// <param name="r">The ref to touch.</param>
        internal void DoEnsure(Ref r)
        {
            if (!_info.IsRunning)
                throw _retryex;
            if (_ensures.Contains(r))
                return;

            r.EnterReadLock();

            // someone completed a write after our shapshot
            if (r.CurrentValPoint() > _readPoint)
            {
                r.ExitReadLock();
                throw _retryex;
            }

            Info refinfo = r.TInfo;

            // writer exists
            if (refinfo != null && refinfo.IsRunning)
            {
                r.ExitReadLock();
                if (refinfo != _info)  // not us, ensure is doomed
                    BlockAndBail(refinfo);
            }
            else
                _ensures.Add(r);
        }


        /// <summary>
        /// Post a commute on a ref in this transaction.
        /// </summary>
        /// <param name="r">The ref.</param>
        /// <param name="fn">The commuting function.</param>
        /// <param name="args">Additional arguments to the function.</param>
        /// <returns>The computed value.</returns>
        internal object DoCommute(Ref r, IFn fn, ISeq args)
        {
            if (!_info.IsRunning)
                throw _retryex;
            if (!_vals.ContainsKey(r))
            {
                object val = null;
                try
                {
                    r.EnterReadLock();
                    val = r.TryGetVal();
                }
                finally
                {
                    r.ExitReadLock();
                }
                _vals[r] = val;
            }
            List<CFn> fns;
            if (! _commutes.TryGetValue(r, out fns))
                _commutes[r] = fns = new List<CFn>();
            fns.Add(new CFn(fn, args));
            object ret = fn.applyTo(RT.cons(_vals[r], args));
            _vals[r] = ret;

            return ret;
        }

        #endregion
    }
}
