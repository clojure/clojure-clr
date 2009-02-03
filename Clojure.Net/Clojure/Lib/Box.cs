using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang
{

    // TODO: Replace Box:  mostly this is used in the Java version in lieu of ref/out parameters.

    /// <summary>
    /// Boxes any value or reference.
    /// </summary>
    public class Box
    {
        /// <summary>
        /// The value being boxed.
        /// </summary>
        private object _val;

        /// <summary>
        /// Gets the boxed value.
        /// </summary>
        public object Val
        {
            get { return _val; }
            set { _val = value; }
        }

        /// <summary>
        /// Initializes a <see cref="Box">Box</see> to the given value.
        /// </summary>
        /// <param name="val"></param>
        public Box(object val)
        {
            _val = val;
        }
            

    }
}
