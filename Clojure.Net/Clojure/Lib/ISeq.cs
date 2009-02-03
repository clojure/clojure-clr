using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang
{
    /// <summary>
    /// Represents an immutable first/rest sequence.
    /// </summary>
    /// <remarks><para>Being a non-null ISeq implies that there is at least one element.  
    /// A null value for <c>rest()</c> implies the end of the sequence.</para>
    /// A standard iteration is of the form:
    /// <code>
    /// for ( ISeq s = init;  s != null; s = s.rest() )
    /// {
    ///   ... s.first() ...
    /// }
    /// </code>
    /// </remarks>
    public interface ISeq: IPersistentCollection, Sequential
    {
        /// <summary>
        /// Gets the first item.
        /// </summary>
        /// <returns>The first item.</returns>
        object first();

        /// <summary>
        /// Gets the rest of the sequence.
        /// </summary>
        /// <returns>The rest of the sequence, or <c>null</c> if no more elements.</returns>
        ISeq rest();

        /// <summary>
        /// Adds an item to the beginning of the sequence.
        /// </summary>
        /// <param name="o">The item to add.</param>
        /// <returns>A new sequence containing the new item in front of the items already in the sequence.</returns>
        /// <remarks>This overrides the <c>cons</c> method in <see cref="IPersistentCollection">IPersistentCollection</see>
        /// by giving an <see cref="ISeq">ISeq</see> in return.</remarks>
        new ISeq cons(object o);
    }
}
