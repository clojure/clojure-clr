/**
 *   Copyright (c) Rich Hickey. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

using clojure.lang;
using NExpect;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static NExpect.Expectations;

namespace Csharp.Tests.LibTests;

[TestFixture]
internal class PowersorterTests
{
    #region The driver

    // We use RNG to generate a seed for the RNG used _per test_.
    // Test results report the seed so we can replicate any failures.
    Random _random = new(0);

    [Test]
    [TestCaseSource(typeof(PowersorterTestData))]
    public void PowerSorter_SortsCorrectly_AndStably(OrderType type, int size)
    {
        int seed = _random.Next();

        object[] data = MakeTestExample(type, size, seed);
        string id = $"Type: {type}, Size: {size}, Seed: {seed}";

        object[] reference = [.. data];
        Array.Sort(reference, new KeyTagComparer());

        Powersorter.Powersort(data, new KeyComparer(), 0, data.Length - 1);
        Expect(AreIdentical(reference, data)).To.Be.True($"Data is not sorted correctly or not stable.. {id}");

        //Expect(IsSorted(data, new KeyComparer())).To.Be.True($"Data is not sorted correctly. {id}");
        //Expect(IsStable(data, new KeyComparer())).To.Be.True($"Data is not sorted stably. {id}");
    }


    static bool AreIdentical(object[] a, object[] b)
    {
        if (a.Length != b.Length)
            return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (!object.ReferenceEquals(a[i], b[i]))
                return false;
        }
        return true;
    }


    //private static bool IsSorted(object[] data, IComparer comparer)
    //{
    //    for (int i = 1; i < data.Length; i++)
    //    {
    //        if (comparer.Compare(data[i - 1], data[i]) > 0)
    //            return false;
    //    }
    //    return true;
    //}

    //private static bool IsStable(object[] data, IComparer comparer)
    //{
    //    for (int i = 1; i < data.Length; i++)
    //    {
    //        if (comparer.Compare(data[i - 1], data[i]) == 0)
    //        {
    //            if (((Data)data[i - 1]).Tag > ((Data)data[i]).Tag)
    //                return false;
    //        }
    //    }
    //    return true;
    //}

    #endregion

    #region Test data generation

    private class PowersorterTestData : IEnumerable
    {
        object[] MakePair(OrderType type, int size)
        {
            return new object[] { type, size };
        }

        List<object[]> MakeSmallRandomTests()
        {
            List<object[]> tests = [];
            for (int size = 0; size <= 7; size++)
            {
                tests.Add(MakePair(OrderType.Random, size));
            }

            return tests;
        }

        int[] sizes = new int[] {
            63, 64, 65, 127, 128, 129,  // around the minimum run boundaries
            255, 256, 257,  1023, 1024, 1025, // powers of two and their neighbors
            2112, // The awkward case mentioned in listsort.txt
            10000, 100000 // large sizes
        
        };

        List<object[]> MakeMeOneWithEverything()
        {
            List<object[]> tests = [];
            foreach (OrderType type in Enum.GetValues(typeof(OrderType)))
            {
                foreach (int size in sizes)
                {
                    tests.Add(MakePair(type, size));
                }
            }
            return tests;
        }


        public IEnumerator GetEnumerator()
        {
            foreach (var test in MakeSmallRandomTests())
            {
                yield return test;
            }

            foreach (var test in MakeMeOneWithEverything())
            {
                yield return test;
            }

        }
    }

    #endregion

    #region Data definition

    class Data
    {
        public int Key { get; set; }
        public int Tag { get; set; }
    }

    class KeyTagComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            if (x is Data dx && y is Data dy)
            {
                int c = dx.Key.CompareTo(dy.Key);
                if (c != 0)
                    return c;
                return dx.Tag.CompareTo(dy.Tag);
            }
            else
                throw new ArgumentException("Objects are not of type Data");
        }
    }

    class KeyComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            if (x is Data dx && y is Data dy)
            {
                return dx.Key.CompareTo(dy.Key);
            }
            else
                throw new ArgumentException("Objects are not of type Data");
        }
    }


    #endregion

    #region Data generation helpers

    public enum OrderType
    {
        Random,
        OneKey,
        FewKeys,
        Reversed,
        ReversedWithTies,
        Sorted,
        NearlySorted,
        SortedRandomTail,
        SawTooth,
        PipeOrgan,
        RandomRunLengths
    }

    private static object[] MakeTestExample(OrderType orderType, int size, int seed = 0)
    {
        switch (orderType)
        {
            case OrderType.Random:
                return MakeRandom(size, seed);
            case OrderType.OneKey:
                return MakeOneKey(size, seed);
            case OrderType.FewKeys:
                return MakeFewKeys(size, seed);
            case OrderType.Reversed:
                return MakeReversed(size, seed);
            case OrderType.ReversedWithTies:
                return MakeReversedWithTies(size, seed);
            case OrderType.Sorted:
                return MakeSorted(size, seed);
            case OrderType.NearlySorted:
                return MakeNearlySorted(size, seed);
            case OrderType.SortedRandomTail:
                return MakeSortedRandomTail(size, seed);
            case OrderType.SawTooth:
                return MakeSawTooth(size, seed);
            case OrderType.PipeOrgan:
                return MakePipeOrgan(size, seed);
            case OrderType.RandomRunLengths:
                return MakeRandomRunLengths(size, seed);
            default:
                throw new ArgumentException("Unknown order type");
        }
    }

    // Create a random distribution of keys.
    private static object[] MakeRandom(int size, int seed)
    {
        Random r = new(seed);

        object[] data = new object[size];

        for (int i = 0; i < size; i++)
        {
            data[i] = new Data { Key = r.Next(0, 10000), Tag = i };
        }

        return data;
    }


    // Create an array with all elements having the same key.
    private static object[] MakeOneKey(int size, int seed)
    {
        Random r = new(seed);

        int key = r.Next();

        object[] data = new object[size];

        for (int i = 0; i < size; i++)
        {
            data[i] = new Data { Key = key, Tag = i };
        }

        return data;
    }


    // Create an array with a few distinct keys.
    private static object[] MakeFewKeys(int size, int seed)
    {
        Random r = new(seed);

        int numKeys = Math.Min(10, Math.Max(1, size / 10));
        int[] keys = new int[numKeys];
        for (int i = 0; i < numKeys; i++)
        {
            keys[i] = r.Next(0, 10000);
        }

        object[] data = new object[size];
        for (int i = 0; i < size; i++)
        {
            data[i] = new Data { Key = keys[r.Next(0, numKeys)], Tag = i };
        }

        return data;
    }

    // Create an array with elements in reverse order, distinct keys.
    private static object[] MakeReversed(int size, int seed)
    {
        Random r = new(seed);

        object[] data = new object[size];

        for (int i = 0; i < size; i++)
        {
            data[i] = new Data { Key = size - i, Tag = i };
        }

        return data;

    }

    // Create an array with elements in reverse order, with duplicate keys.
    private static object[] MakeReversedWithTies(int size, int seed)
    {
        Random r = new(seed);

        object[] data = new object[size];

        for (int i = 0; i < size; i += 2)
        {
            data[i] = new Data { Key = size - i, Tag = i };
            if (i < size - 1)
                data[i + 1] = new Data { Key = size - i, Tag = i + 1 };
        }

        return data;
    }

    // Create an array with elements in sorted order, distinct keys.
    private static object[] MakeSorted(int size, int seed)
    {
        Random r = new(seed);

        object[] data = new object[size];

        for (int i = 0; i < size; i++)
        {
            data[i] = new Data { Key = i, Tag = i };
        }

        return data;

    }

    // Create an array with elements in nearly sorted order (10% swaps), distinct keys.
    private static object[] MakeNearlySorted(int size, int seed)
    {
        object[] data = MakeSorted(size, seed);

        int numSwaps = size switch
        {

            < 100 => 1,
            _ => size / 100
        };

        Random r = new(seed);
        for (int i = 0; i < numSwaps; i++)
        {
            int j = r.Next(0, size);
            int k = r.Next(0, size);
            (data[j], data[k]) = (data[k], data[j]);
            ((Data)data[j]).Tag = j;
            ((Data)data[k]).Tag = k;
        }

        return data;
    }

    // Create an array with elements in a sawtooth pattern = repeated increasing runs.  
    // The same keys used in each run.
    private static object[] MakeSawTooth(int size, int seed)
    {
        Random r = new(seed);

        object[] data = new object[size];

        int runLength = size switch
        {
            < 5 => size,
            < 10 => size / 2,
            < 50 => size / 5,
            _ => size / 10
        };

        object[] run = MakeSorted(runLength, seed);

        for (int i = 0; i < size; i += runLength)
        {
            object[] copy = run.Select(d => new Data { Key = ((Data)d).Key, Tag = 0 }).ToArray();
            Array.Copy(copy, 0, data, i, Math.Min(runLength, size - i));
        }

        SetTagsToPosition(data);

        return data;
    }

    // Increasing to middle, descreaing to end.  Same keys in each half.
    private static object[] MakePipeOrgan(int size, int seed)
    {
        Random r = new(seed);

        object[] data = new object[size];

        for (int i = 0; i < size / 2; i++)
        {
            data[i] = new Data { Key = i, Tag = i };
            data[size - i - 1] = new Data { Key = i, Tag = i };
        }

        if (size % 2 == 1)
        {
            data[size / 2] = new Data { Key = size / 2 + 1, Tag = size / 2 + 1 };
        }

        SetTagsToPosition(data);

        return data;

    }

    // Create an array with elements in sorted order, distinct keys, but with a random tail.  The tail is 10% of the size of the array.
    private static object[] MakeSortedRandomTail(int size, int seed)
    {
        if (size < 10) return MakeSorted(size, seed);

        int headSize = size - size / 10;
        int tailSize = size - headSize;

        object[] head = MakeSorted(size, seed);
        object[] tail = MakeRandom(size, seed);

        object[] data = new object[size];
        Array.Copy(head, 0, data, 0, headSize);
        Array.Copy(tail, 0, data, headSize, tailSize);
        SetTagsToPosition(data);

        return data;

    }

    // Create an array with elements in random order, but with runs of random lengths.  The average run length is 10% of the size of the array.
    private static object[] MakeRandomRunLengths(int size, int seed)
    {
        Random r = new(seed);

        int targetAverage = size switch
        {
            < 10 => 1,
            < 50 => 2,
            < 100 => 3,
            < 500 => 5,
            _ => size
        };

        int[] parts = PartitionWithTargetAverage(size, targetAverage, r);

        object[] data = new object[size];

        int count = 0;
        for (int i = 0; i < parts.Length; i++)
        {
            object[] run = r.NextDouble() < 0.5 ? MakeSorted(parts[i], r.Next()) : MakeReversed(parts[i], r.Next());
            Array.Copy(run, 0, data, count, parts[i]);
            count += parts[i];
        }

        SetTagsToPosition(data);

        return data;
    }

    private static int[] PartitionWithTargetAverage(int n, int targetAverage, Random r)
    {
        double p = 1.0 / targetAverage;
        List<int> parts = [];

        int current = 1;
        for (int i = 0; i < n; i++)
        {
            if (r.NextDouble() < p)
            {
                parts.Add(current);
                current = 1;
            }
            else
            {
                current++;
            }
        }

        if (current > 1)
            parts.Add(current - 1);
        int sum = parts.Sum();

        int[] result = [.. parts];
        Shuffle(result, r);
        return result;
    }

    private static void Shuffle(int[] xs, Random r)
    {
        // Fisher-Yates shuffle
        for (int i = xs.Length - 1; i > 0; i--)
        {
            int j = r.Next(i + 1);
            (xs[i], xs[j]) = (xs[j], xs[i]);
        }
    }


    // Set the tags to the position in the array.  This is used to check for stability.

    private static void SetTagsToPosition(object[] data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            if (data[i] is Data d)
            {
                d.Tag = i;
            }
        }
    }

    #endregion
}
