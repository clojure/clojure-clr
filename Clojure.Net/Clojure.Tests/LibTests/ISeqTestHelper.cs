using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using clojure.lang;

namespace Clojure.Tests.LibTests
{
    public class ISeqTestHelper : AssertionHelper
    {
        public void VerifyISeqContents(ISeq s, IList<object> values)
        {
            int i=0;

            for (; s != null; s = s.rest(), i++)
                Expect(s.first(), EqualTo(values[i]));

            Expect(i, EqualTo(values.Count));
        }

        public void VerifyISeqCons(ISeq s, object newVal, IList<object> values)
        {
            ISeq newSeq = s.cons(newVal);

            Expect(newSeq.first(), EqualTo(newVal));
            VerifyISeqContents(newSeq.rest(), values);
        }
    }
}
