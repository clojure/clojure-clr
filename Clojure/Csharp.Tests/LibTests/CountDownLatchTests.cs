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
using NUnit.Framework;
using static NExpect.Expectations;
using clojure.lang;
using System.Threading;
using NExpect;

namespace Clojure.Tests.LibTests
{
    [TestFixture]
    public class CountDownLatchTests
    {

        static int _count = 0;

        [Test]
        public void BasicTest_StartWaiters()
        {
            _count = 0;
            const int NumThreads = 10;
            CountDownLatch latch = new CountDownLatch(1);
            EventWaitHandle[] handles = new EventWaitHandle[NumThreads];

            for (int i=0; i<NumThreads; i++)
            {
                handles[i] = new EventWaitHandle(false, EventResetMode.ManualReset);

                Thread thr = new Thread(delegate(object x) { 
                    latch.Await();
                    Interlocked.Increment(ref _count);
                    ((EventWaitHandle)x).Set();
                });
                thr.Name = String.Format("Thread {0}",i);
                thr.Start(handles[i]);
            }
            EventWaitHandle handle = new EventWaitHandle(false, EventResetMode.ManualReset);
            Thread thr1 = new Thread(delegate(object x)
            {
                Thread.Sleep(100);
                EventWaitHandle.WaitAll(handles);
                ((EventWaitHandle)x).Set();
            });
            thr1.SetApartmentState(ApartmentState.MTA);
            thr1.Start(handle);
            
            Thread.Sleep(100);
            Expect(_count).To.Equal(0);
            latch.CountDown();
            handle.WaitOne();
            Expect(_count).To.Equal(NumThreads);
        }

        [Test]
        public void BasicTest_Countdown()
        {
            const int NumThreads = 10;
            CountDownLatch latch = new CountDownLatch(NumThreads);

            for (int i = 0; i < NumThreads; i++)
            {
                Thread thr = new Thread(delegate()
                {
                    Thread.Sleep(100);
                    latch.CountDown();
                });
                thr.Name = String.Format("Thread {0}", i);
                thr.Start();
            }

            latch.Await();
            Expect(latch.Count).To.Equal(0);
        }


        [Test]
        public void AwaitThatTimesOutReturnsFalse()
        {
            const int NumThreads = 10;
            CountDownLatch latch = new CountDownLatch(NumThreads);

            bool result = latch.Await(50);
            Expect(result).To.Be.False();
        }


        [Test]
        public void AwaitThatDoesNotTimeOutReturnsTrue()
        {
            const int LatchCount = 10;
            CountDownLatch latch = new CountDownLatch(LatchCount);

            Thread thr = new Thread(delegate()
            {
                Thread.Sleep(100);
                for ( int i=0; i<LatchCount; i++ ) 
                    latch.CountDown();
            });
            thr.Start();

            bool result = latch.Await(50000);
            Expect(result);
        }
    }
}
