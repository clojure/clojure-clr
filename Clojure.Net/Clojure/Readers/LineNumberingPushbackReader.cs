/**
 *   Copyright (c) Rich Hickey. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

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
