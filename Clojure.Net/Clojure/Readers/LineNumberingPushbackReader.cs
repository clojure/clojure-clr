using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZO.SmartCore.IO;
using System.IO;

namespace Clojure.Readers
{
    public class LineNumberingPushbackReader : PushbackReader
    {
        public LineNumberingPushbackReader(TextReader r)
            : base(new LineNumberReader(r))
        {
        }

        public int LineNumber 
        { 
            get 
            { 
                return ((LineNumberReader)this.Reader).LineNumber + 1; 
            } 
        }
    }
}
