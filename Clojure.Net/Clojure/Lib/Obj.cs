using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang
{
    /// <summary>
    /// Provides a basic implementation of <see cref="IObj">IObj</see> functionality.
    /// </summary>
    public abstract class Obj: IObj
    {
        #region Data

        /// <summary>
        /// The metatdata for the object.
        /// </summary>
        protected readonly IPersistentMap _meta;

        #endregion

        #region C-tors

        /// <summary>
        /// Initializes a new instance of <see cref="Obj">Obj</see> that has null metadata.
        /// </summary>
        public Obj() 
        {
            _meta = null;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Obj">Obj</see> that has 
        /// the given <see cref="IPersistentMap">IPersistentMap</see> as its metadata.
        /// </summary>
        /// <param name="meta">The map used to initialize the metadata.</param>
        public Obj(IPersistentMap meta)
        {
            _meta = meta;
        }

        #endregion
        
        #region IMeta Members

        /// <summary>
        /// Gets the metadata attached to the object.
        /// </summary>
        /// <returns>An immutable map representing the object's metadata.</returns>
         public IPersistentMap meta()
        {
            return _meta;
        }

        #endregion

        #region IObj methods

         /// <summary>
         /// Create a copy with new metadata.
         /// </summary>
         /// <param name="meta">The new metadata.</param>
         /// <returns>A copy of the object with new metadata attached.</returns>
         public abstract IObj withMeta(IPersistentMap meta);

        #endregion
    }
}
