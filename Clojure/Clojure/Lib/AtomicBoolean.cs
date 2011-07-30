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
    /// Implements the Java <c>java.util.concurrent.atomic.AtomicBoolean</c> class.  
    /// </summary>
    /// <remarks>I hope.  Someone with more knowledge of these things should check this out.
    /// <para>So I could cheat and use Interlocked, I just used an int and make it 0 === false.</para></remarks>
    public sealed class AtomicBoolean
    {
        #region Data

        /// <summary>
        /// The current <see cref="Boolean">boolean</see> value.
        /// </summary>
        int _val;

        #endregion

        #region C-tors

        /// <summary>
        /// Initializes an <see cref="AtomicBoolean"/> with value false.
        /// </summary>
        public AtomicBoolean()
        {
            _val = 0;
        }

        /// <summary>
        /// Initializes an <see cref="AtomicBoolean"/> with a given value.
        /// </summary>
        /// <param name="initVal">The initial value.</param>
        public AtomicBoolean(bool initVal)
        {
            _val = BoolToInt(initVal);
        }

        #endregion

        #region  Bool<->int value hacks

        static int BoolToInt(bool val)
        {
            return val ? 1 : 0;
        }

        static bool IntToBool(int val)
        {
            return val != 0;
        }

        #endregion

        #region Value access

        /// <summary>
        /// Gets the current value.
        /// </summary>
        /// <returns>The current value.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "get")]
        public bool get()
        {
            return IntToBool(_val);
        }

        /// <summary>
        /// Sets the value if the expected value is current.
        /// </summary>
        /// <param name="oldVal">The expected value.</param>
        /// <param name="newVal">The new value.</param>
        /// <returns><value>true</value> if the value was set; <value>false</value> otherwise.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "compare")]
        public bool compareAndSet(bool oldVal, bool newVal)
        {
            int ioldVal = BoolToInt(oldVal);
            int inewVal = BoolToInt(newVal);

            int origVal = Interlocked.CompareExchange(ref _val, inewVal, ioldVal);
            return origVal == ioldVal;
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="newVal">The new value.</param>
        /// <returns>The new value.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "set")]
        public bool set(bool newVal)
        {
            int inewVal = BoolToInt(newVal); ;

            return IntToBool( Interlocked.Exchange(ref _val, inewVal) );
        }

        #endregion
    }
}
