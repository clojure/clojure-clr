using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang
{
    /// <summary>
    /// Represents a collection that supports function mapping/reduction.
    /// </summary>
    public interface IReduce
    {
        /// <summary>
        /// Reduce the collection using a function.
        /// </summary>
        /// <param name="f">The function to apply.</param>
        /// <returns>The reduced value</returns>
        /// <remarks>Computes f(...f(f(f(i0,i1),i2),i3),...).</remarks>
        object reduce(IFn f);


        /// <summary>
        /// Reduce the collection using a function.
        /// </summary>
        /// <param name="f">The function to apply.</param>
        /// <param name="start">An initial value to get started.</param>
        /// <returns>The reduced value</returns>
        /// <remarks>Computes f(...f(f(f(start,i0),i1),i2),...).</remarks>
        object reduce(IFn f, object start);
    }
}
