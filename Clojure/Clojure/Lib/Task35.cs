using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

#if CLR2

namespace clojure.lang
{
    /// <summary>
    /// Minimal implementation of task functionality to support reducers library under .Net 3.5
    /// </summary>
    class Task35 : IDisposable
    {

        #region Data

        IFn _fn;
        object _result;
        Exception _error;
        ManualResetEvent _waitHandle = new ManualResetEvent(false);
        bool _done = false;
        bool _disposed = false;

        #endregion

        #region C-tors

        public Task35(IFn fn)
        {
            _fn = fn;
        }

        #endregion

        #region Implementation

        public void Start() {
            ThreadPool.QueueUserWorkItem(o =>
                {
                    try 
                    {
                        _result = _fn.invoke();
                    }
                    catch (Exception e) 
                    {
                        _error = e;
                    }
                    finally 
                    {
                        _waitHandle.Set();
                    }
                });
        }

        public void Wait() 
        {
            _waitHandle.WaitOne();
        }

        public object Result()
        {
            Wait();
            if ( _error != null )
                throw _error;
            return _result;
        }

        public void RunSynchronously()
        {
            Start();
            Wait();
        }

        public bool IsDone() 
        {
            return _waitHandle.WaitOne(0);
        }

        public bool ThrewException() 
        {
            Wait();
            return _error != null;
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
                    ((IDisposable)_waitHandle).Dispose();
                }

                _disposed = true;
            }
        }

        #endregion
    }

}

#endif
