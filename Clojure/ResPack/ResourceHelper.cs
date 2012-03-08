using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Resources;

namespace ResPack
{
    public static class ResourceHelper
    {
        public static void WriteResourceFile(this IEnumerable<KeyValuePair<string, byte[]>> pairs, string fileName)
        {
            using (var writer = new ResourceWriter(fileName))
            {
                foreach (var p in pairs)
                {
                    writer.AddResourceData(p.Key, String.Empty, p.Value);
                }
            }
        }

        public static byte[] GetResourceData(string key, string fileName)
        {
            return ReadResourceFile(new[] {key}, fileName).First().Value;
        }

        public static IEnumerable<KeyValuePair<string, byte[]>> ReadResourceFile(this IEnumerable<string> keys,
                                                                                 string fileName)
        {
            using (var reader = new ResourceReader(fileName))
            {
                string _;
                byte[] data;
                var pairs = new Dictionary<string, byte[]>();

                foreach (var k in keys)
                {
                    reader.GetResourceData(k, out _, out data);
                    pairs.Add(k, data);
                }

                return pairs;
            }
        }
    }
}