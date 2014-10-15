﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using NUnit.Framework;

using clojure.lang;

namespace Clojure.Tests.LibTests
{
    [TestFixture]
    public class FutureTests : AssertionHelper
    {
        [Test]
        public void ComputesTheValueOnAnotherThread()
        {
            int workerID = Thread.CurrentThread.ManagedThreadId;
            AFnImpl fn = new AFnImpl();
            fn._fn0 = () =>
                {
                    workerID = Thread.CurrentThread.ManagedThreadId;
                    return 42;
                };

            Future f = new Future(fn);
            Expect(f.deref(), EqualTo(42));
            Expect(workerID, Not.EqualTo(Thread.CurrentThread.ManagedThreadId));
        }

        [Test]
        public void CachesResultForSubsequentCalls()
        {
            int i = 0;
            AFnImpl fn = new AFnImpl();
            fn._fn0 = () => Interlocked.Increment(ref i);

            Future f = new Future(fn);
            Expect(f.deref(), EqualTo(1));
            Expect(f.deref(), EqualTo(1));
            Expect(i, EqualTo(1));
        }

        [Test]
        public void PropagatesExceptions()
        {
            AFnImpl fn = new AFnImpl();
            fn._fn0 = () => { throw new Exception("future exception"); };

            Future f = new Future(fn);
            try
            {
                f.deref();
                Assert.Fail("expected future exception");
            }
            catch (Exception ex)
            {
                Expect(ex.Message, EqualTo("Future has an error"));
                Expect(ex.InnerException, Is.Not.Null);
                Expect(ex.InnerException.Message, EqualTo("future exception"));
            }

            // Same result for subsequent derefs.
            try
            {
                f.deref();
                Assert.Fail("expected future exception");
            }
            catch (Exception ex)
            {
                Expect(ex.Message, EqualTo("Future has an error"));
                Expect(ex.InnerException, Is.Not.Null);
                Expect(ex.InnerException.Message, EqualTo("future exception"));
            }
        }

        [Test]
        public void CancelAbortsTheTask()
        {
            AFnImpl fn = new AFnImpl();
            fn._fn0 = () => { while (true); };

            Future f = new Future(fn);
            Expect(f.isCancelled(), EqualTo(false));
            Expect(f.cancel(true), EqualTo(true));
            Expect(f.isCancelled(), EqualTo(true));
        }

        [Test]
        public void SecondCancelReturnsFalse()
        {
            AFnImpl fn = new AFnImpl();
            fn._fn0 = () => { while (true); };

            Future f = new Future(fn);
            Expect(f.cancel(true), EqualTo(true));
            Expect(f.cancel(true), EqualTo(false));
        }

        [Test]
        public void CancelFailsAfterSuccessfulCompletion()
        {
            AFnImpl fn = new AFnImpl();
            fn._fn0 = () => { return 42; };

            Future f = new Future(fn);
            Expect(f.deref(), EqualTo(42));
            Expect(f.cancel(true), EqualTo(false));
            Expect(f.isCancelled(), EqualTo(false));
        }

        [Test]
        [ExpectedException(typeof(FutureAbortedException))]
        public void DerefThrowsAfterCancellation()
        {
            AFnImpl fn = new AFnImpl();
            fn._fn0 = () => { while (true); };

            Future f = new Future(fn);
            f.cancel(true);
            f.deref();
        }
    }
}