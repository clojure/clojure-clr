using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;

namespace clojure.lang
{
    /// <summary>
    /// Implements a delay of a function call.
    /// </summary>
    public class Delay
    {
        #region Data

        /// <summary>
        /// The value, after it has been computed.
        /// </summary>
        object _val;

        /// <summary>
        /// The function being delayed.
        /// </summary>
        IFn _fn;

        #endregion

        #region C-tors

        /// <summary>
        /// Construct a delay for a function.
        /// </summary>
        /// <param name="fn">The function to delay.</param>
        public Delay(IFn fn)
        {
            _fn = fn;
            _val = null;
        }

        #endregion

        #region Delay operations

        /// <summary>
        /// Force a delay (or identity if not a delay).
        /// </summary>
        /// <param name="x">The object to force.</param>
        /// <returns>The computed valued (if a delay); the object itself (if not a delay).</returns>
        public static object force(object x)
        {
            return (x is Delay)
                ? ((Delay)x).get()
                : x;
        }


        /// <summary>
        /// Get the value.
        /// </summary>
        /// <returns>The value</returns>
        /// <remarks>Forces the computation if it has not happened yet.</remarks>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private object get()
        {
            if (_fn != null)
            {
                _val = _fn.invoke();
                _fn = null;
            }
            return _val;
        }


        #endregion
    }
}
