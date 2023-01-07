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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang
{
    /// <summary>
    /// Provides Murmur3 hashing algorithm
    /// </summary>
    /// <remarks>
    /// <para>The ClojureJVM version imported the Guava Murmur3 implementation and made some changes.</para>
    /// <para>I copied the API stubs, then implemented the API based on the algorithm description at 
    ///     http://en.wikipedia.org/wiki/MurmurHash.
    ///     See also: https://code.google.com/p/smhasher/wiki/MurmurHash3. </para>
    /// <para>Because the original algorithm was based on unsigned arithmetic, 
    /// I built methods that implemented those directly, then built versions 
    /// returning signed integers, as required by most users.</para>
    /// <para>Implementation of HashUnordered and HashOrdered taken from ClojureJVM.</para>
    /// </remarks>
    public static class Murmur3
    {
        #region Data

        const int Seed = 0;
        const uint C1 = 0xcc9e2d51;
        const uint C2 = 0x1b873593;
        const int R1 = 15;
        const int R2 = 13;
        const int M = 5;
        const uint N = 0xe6546b64;

        #endregion

        #region int-returning API

        public static int HashInt(int input)
        {
            return unchecked((int)HashIntU((uint)input));
        }

        public static int HashLong(long input)
        {
            return unchecked((int)HashLongU((ulong)input));
        }

        public static int HashString(string input)
        {
            return unchecked((int)HashStringU(input));
        }

        public static int HashOrdered(IEnumerable input)
        {
            return unchecked((int)HashOrderedU(input));
        }

        public static int HashUnordered(IEnumerable input)
        {
            return unchecked((int)HashUnorderedU(input));
        }

        public static int MixCollHash(int hash, int count)
        {
            return unchecked((int)MixCollHashU((uint)hash, count));
        }

        #endregion

        #region uint-returning API

        public static uint HashIntU(int input)
        {
            return unchecked(HashIntU((uint)input));
        }

        public static uint HashLongU(long input)
        {
            return unchecked(HashLongU((ulong)input));
        }

        public static uint HashIntU(uint input)
        {
            if (input == 0)
                return 0;

            uint key = MixKey(input);
            uint hash = MixHash(Seed, key);

            return Finalize(hash, 4);
        }

        public static uint HashLongU(ulong input)
        {
            if (input == 0)
                return 0;

            uint low = (uint)input;
            uint high = (uint)(input >> 32);

            uint key = MixKey(low);
            uint hash = MixHash(Seed, key);

            key = MixKey(high);
            hash = MixHash(hash, key);

            return Finalize(hash, 8);
        }

        public static uint HashStringU(string input)
        {
            uint hash = Seed;

            // step through the string 2 chars at a time
            for (int i = 1; i < input.Length; i += 2)
            {
                uint key = unchecked((uint)input[i - 1] | (((uint)input[i]) << 16));
                key = MixKey(key);
                hash = MixHash(hash, key);
            }

            // deal with remaining character if odd
            if ((input.Length & 1) == 1)
            {
                uint key = input[input.Length - 1];
                key = MixKey(key);
                hash ^= key;
            }

            return Finalize(hash, 2 * input.Length);
        }
        
        public static uint HashOrderedU(IEnumerable xs)
        {
            int n = 0;
            uint hash = 1;

            foreach (Object x in xs)
            {
                hash = 31 * hash + unchecked((uint)Util.hasheq(x));
                ++n;
            }
            return FinalizeCollHash(hash, n);
        }

        public static uint HashUnorderedU(IEnumerable xs)
        {
            uint hash = 0;
            int n = 0;

            foreach (Object x in xs )
            {
                hash +=  unchecked((uint)Util.hasheq(x));
                ++n;
            }

            return FinalizeCollHash(hash, n);
        }

        public static uint MixCollHashU(uint hash, int count)
        {
            return FinalizeCollHash(hash, count);
        }

        #endregion

        #region Implementation details

        private static uint MixKey(uint key)
        {
            key *= C1;
            key = RotateLeft(key, R1);
            key *= C2;
            return key;
        }

        private static uint MixHash(uint hash, uint key)
        {
            hash ^= key;
            hash = RotateLeft(hash, R2);
            hash = hash * M + N;
            return hash;
        }

        // Finalization mix - force all bits of a hash block to avalanche
        private static uint Finalize(uint hash, int length)
        {
            hash ^= (uint)length;
            hash ^= hash >> 16;
            hash *= 0x85ebca6b;
            hash ^= hash >> 13;
            hash *= 0xc2b2ae35;
            hash ^= hash >> 16;
            return hash;
        }

        private static uint FinalizeCollHash(uint hash, int count)
        {
            uint h1 = Seed;
            uint k1 = MixKey(hash);
            h1 = MixHash(h1, k1);
            return Finalize(h1, count);
        }

        private static uint RotateLeft(uint x, int n)
        {
            return (x << n) | (x >> (32 - n));
        }
        #endregion
    }
}
