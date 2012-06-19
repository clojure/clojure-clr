using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using clojure.lang;

namespace Clojure.Tests.LibTests
{
    /// <summary>
    /// This class is used to provide a dummy value for meta calls.
    /// </summary>
    /// <remarks>Most of the use of mocking within these tests was to provide a meta value for objects.  
    /// Only identity was tested.  An instance of this class will suffice for most testing uses.  
    /// Anything that needs more can just tie its test to a real implementation of IPersistentMap.</remarks>
    public class DummyMeta : IPersistentMap
    {

        public IPersistentMap assoc(object key, object val)
        {
            throw new NotImplementedException();
        }

        public IPersistentMap assocEx(object key, object val)
        {
            throw new NotImplementedException();
        }

        public IPersistentMap without(object key)
        {
            throw new NotImplementedException();
        }

        public IPersistentMap cons(object o)
        {
            throw new NotImplementedException();
        }

        public int count()
        {
            throw new NotImplementedException();
        }

        public bool containsKey(object key)
        {
            throw new NotImplementedException();
        }

        public IMapEntry entryAt(object key)
        {
            throw new NotImplementedException();
        }

        Associative Associative.assoc(object key, object val)
        {
            throw new NotImplementedException();
        }


        IPersistentCollection IPersistentCollection.cons(object o)
        {
            throw new NotImplementedException();
        }

        public IPersistentCollection empty()
        {
            throw new NotImplementedException();
        }

        public bool equiv(object o)
        {
            throw new NotImplementedException();
        }

        public ISeq seq()
        {
            throw new NotImplementedException();
        }

        public object valAt(object key)
        {
            throw new NotImplementedException();
        }

        public object valAt(object key, object notFound)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<IMapEntry> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
