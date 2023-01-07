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
using System.Threading;

namespace clojure.lang
{
    /// <summary>
    /// Implements countdown latch.
    /// </summary>
    /// <remarks>The interface that is implemented matches the one for java.util.concurrent.CountDownLatch.</remarks>

    public class CountDownLatch
    {
        #region Data

        readonly object _synch = new object();
        int _count;

        public int Count
        {
            get { return _count; }
        }

        #endregion

        #region C-tors

        public CountDownLatch(int count) 
        {
            if ( count  < 0 )
                throw new ArgumentException("Count must be non-negative.");

            lock (_synch)
            {
                _count = count;
            }
        }

        #endregion

        #region Implementation

        public void Await()
        {
            lock (_synch)
            {
                while ( _count > 0 )
                    Monitor.Wait(_synch);
            }
        }

        public bool Await(int timeoutMilliseconds)
        {
            lock (_synch)
            {
                if ( _count == 0 )
                    return true;

                 Monitor.Wait(_synch,timeoutMilliseconds);

                if ( _count == 0 )
                    return true;
                else
                    return false;
            }
           
        }


        public void CountDown()
        {
            lock (_synch)
            {
                if ( _count > 0 )
                {
                _count--;
                if ( _count == 0 )
                    Monitor.PulseAll(_synch);
                }
            }
        }

        #endregion

        #region Object overrides

        public override string ToString()
        {
            return String.Format("<{0}, Count = {1} >", base.ToString(),_count);
        }

        #endregion
    }
}
