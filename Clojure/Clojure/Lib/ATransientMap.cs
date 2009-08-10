using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace clojure.lang
{
    public abstract class ATransientMap : AFn, ITransientMap
    {
        abstract protected void EnsureEditable();
        abstract public ITransientMap assoc(object key, object val);
        abstract public object valAt(object key, object notFound);
        abstract public int count();
        abstract public IPersistentMap persistent();
        abstract public ITransientMap without(object key);

        IPersistentCollection ITransientCollection.persistent()
        {
            return persistent();
        }

        ITransientCollection ITransientCollection.conj(object val)
        {
            return conj(val);
        }

        ITransientAssociative ITransientAssociative.assoc(object key, object val)
        {
            return assoc(key, val);
        }

        public ITransientMap conj(object val)
        {
            EnsureEditable();
            if (val is IMapEntry)
            {
                IMapEntry e = (IMapEntry)val;

                return assoc(e.key(), e.val());
            }
            else if (val is DictionaryEntry)
            {
                DictionaryEntry de = (DictionaryEntry)val;
                return assoc(de.Key, de.Value);
            }

            else if (val is IPersistentVector)
            {
                IPersistentVector v = (IPersistentVector)val;
                if (v.count() != 2)
                    throw new ArgumentException("Vector arg to map conj must be a pair");
                return assoc(v.nth(0), v.nth(1));
            }

            // TODO: also handle DictionaryEntry?
            ITransientMap ret = this;
            for (ISeq es = RT.seq(val); es != null; es = es.next())
            {
                IMapEntry e = (IMapEntry)es.first();
                ret = ret.assoc(e.key(), e.val());
            }
            return ret;
        }


        public object valAt(object key)
        {
            return valAt(key, null);
        }
    }
}
