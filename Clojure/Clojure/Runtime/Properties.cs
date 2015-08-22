/**
 *   Copyright (c) Rich Hickey. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

/**
 *   Author: David Miller
 **/

using System.Collections.Generic;
using System.IO;
using System;
using System.Runtime.Serialization;

namespace clojure.runtime
{
    /// <summary>
    /// Implements part of the functionaligy of java.util.Properties.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces"), Serializable]
    public sealed class Properties : Dictionary<string,string>
    {

        public Properties()
        {
        }

        private Properties(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "get")]
        public string getProperty(string key)
        {
            string value = null;
            TryGetValue(key, out value);
            return value;
        }

        public void LoadFromString(string content)
        {
            using (TextReader rdr = new StringReader(content))
            {
                Load(rdr);
            }
        }

        public void Load(string fileName)
        {
            using ( TextReader rdr = File.OpenText(fileName) )
            {
                Load(rdr);
            }
        }

        public void Load(TextReader rdr)
        {
            Clear();

            string line;
            while ((line = rdr.ReadLine()) != null)
            {
                line = line.Trim();
                if (string.IsNullOrEmpty(line) ||
                    line.StartsWith(";") ||
                    line.StartsWith("#") ||
                    line.StartsWith("'") ||
                    !line.Contains("="))
                    continue;

                int index = line.IndexOf('=');
                string key = line.Substring(0, index).Trim();
                string value = line.Substring(index + 1).Trim();

                if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                    (value.StartsWith("'") && value.EndsWith("'")))
                {
                    value = value.Substring(1, value.Length - 2);
                }

                this[key] = value;
            }
        }
    }
}
