using System;
using System.Collections;
using System.Diagnostics;

namespace clojure.lang;

// This code is adapted from the Powersort algorithm by J. Ian Munro and Sebastian Wild.
// Original paper: Nearly-Optimal Mergesorts: Fast, Practical Sorting Methods that Optimally Adapt to Exixsting Runs, https://arxiv.org/pdf/1805.04154
// The orginal code is available at https://github.com/sebawild/nearly-optimal-mergesort-code and is under the MIT license.
/*
  MIT License

Copyright (c) 2018 Sebastian Wild

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/

// The original code is in Java, which I have ported fairly directly to C#.
// Many of the comments have been copied verbatim.
// Significant changes:
//   (1) the Java code is for sorting arrays of ints.  Our code is for sorting arrays of objects, using a provided IComparer.
//   (2) our requirements are for a stable sort.  Within the algorithm, to meet this demand,
//       we need the method for merging runs to ensure stability.
//         (a) This means that we must use MergesAndRuns.mergeRunsCopyHalf instead of MergesAndRuns.mergeRuns in the sorting method.
//         (b) MergesAndRuns.extendAndReverseRunRight is also stable as it reverses only strictly descending runs.
//         (c) Insertsort.insertionSort is stable as implemented (straight insertion with strict < comparison).
//   (3) several variations of the sorting algorithm are given: powersort, powersortBitwise, powersortIncreasingOnlyMSB.
//       we need only the first of these.  Thus, we really don't need the flags that are used to pick the algorithm to use.
//       Nor do we need the constructor to check constraints on flag combinations.
//       In addition, we can dispense with the weirdness of the instance field myMinRunLen being used to initialize the static field minRunLen
//       when the algorthm starts.  In fact, we can just pass that value to the sorting method directly and avoid the static minRunLen
//       (which would have been a problem when multithreading.
//       With these changes, we can make the class and all methods be static.
//   (4) No need to have the instrumentation to count merge costs.

/**
 * Powersort implementation as described in the paper.
 *
 * Natural runs are extended to minRunLen if needed before we continue
 * merging.
 * Unless useMsbMergeType is false, node powers are computed using
 * a most-significant-bit trick;
 * otherwise a loop is used.
 * If onlyIncreasingRuns is true, only weakly increasing runs are picked up.
 *
 * @author Sebastian Wild (wild@uwaterloo.ca)
 */

public static class Powersorter
{
    public const int DefaultMinRunLen = 16;


    // Indicates an unused index in the leftRunStart array.
    private const int NullIndex = int.MinValue;


    public static void Powersort(object[] A, IComparer comp, int left, int right, int minRunLen = DefaultMinRunLen)
    {
        int n = right - left + 1;
        int lgnPlus2 = Log2(n) + 2;
        int[] leftRunStart = new int[lgnPlus2];
        int[] leftRunEnd = new int[lgnPlus2];
#if NETFRAMEWORK
        for (int i = 0; i < leftRunStart.Length; i++)
        {
            leftRunStart[i] = NullIndex;
        }
#else
        Array.Fill(leftRunStart, NullIndex);
#endif
        int top = 0;
        object[] buffer = new object[n];

        int startA = left;
        int endA = ExtendAndReverseRunRight(A, comp, startA, right);

        // extend to minRunLen
        int lenA = endA - startA + 1;
        if (lenA < minRunLen)
        {
            endA = Math.Min(right, startA + minRunLen - 1);
            InsertionSort(A, comp, startA, endA, lenA);
        }

        while (endA < right)
        {
            int startB = endA + 1;
            int endB = ExtendAndReverseRunRight(A, comp, startB, right);

            // extend to minRunLen
            int lenB = endB - startB + 1;
            if (lenB < minRunLen)
            {
                endB = Math.Min(right, startB + minRunLen - 1);
                InsertionSort(A, comp, startB, endB, lenB);
            }

            int k = NodePower(left, right, startA, startB, endB);
            Debug.Assert(k != top);

            for (int l = top; l > k; --l)
            {
                if (leftRunStart[l] == NullIndex) continue;
                MergeRunsCopyHalf(A, comp, leftRunStart[l], leftRunEnd[l] + 1, endA, buffer);
                startA = leftRunStart[l];
                leftRunStart[l] = NullIndex;
            }

            // store left half of merge between A and B
            leftRunStart[k] = startA;
            leftRunEnd[k] = endA;
            top = k;
            startA = startB;
            endA = endB;
        }

        Debug.Assert(endA == right);

        for (int l = top; l > 0; --l)
        {
            if (leftRunStart[l] == NullIndex) continue;
            MergeRunsCopyHalf(A, comp, leftRunStart[l], leftRunEnd[l] + 1, right, buffer);
        }
    }




    /**
    * Merges runs A[l..m-1] and A[m..r] in-place into A[l..r]
    * by copying the shorter run into temporary storage B and
    * merging back into A.
    * B.length must be at least min(m-l,r-m+1)
    */

    public static void MergeRunsCopyHalf(object[] A, IComparer comp, int l, int m, int r, object[] B)
    {
        int n1 = m - l;
        int n2 = r - m + 1;

        Debug.Assert(B.Length >= n1 || B.Length >= n2, "Insufficient space in temporary array B");

        if (n1 <= n2)
        {
            Array.Copy(A, l, B, 0, n1);
            int i1 = 0, i2 = m, o = l;
            while (i1 < n1 && i2 <= r)
                A[o++] = comp.Compare(B[i1], A[i2]) <= 0 ? B[i1++] : A[i2++];
            while (i1 < n1)
                A[o++] = B[i1++];
        }
        else
        {
            Array.Copy(A, m, B, 0, n2);
            int i1 = m - 1, i2 = n2 - 1, o = r;
            while (i1 >= l && i2 >= 0)
                A[o--] = comp.Compare(A[i1], B[i2]) <= 0 ? B[i2--] : A[i1--];
            while (i2 >= 0)
                A[o--] = B[i2--];
        }
    }



    /**
	 * Reverse the specified range of the specified array.
	 *
	 * @param a  the array in which a range is to be reversed
	 * @param lo the index of the first element in the range to be
	 *           reversed
	 * @param hi the index of the last element in the range to be
	 *           reversed
	 */
    public static void ReverseRange(object[] a, int lo, int hi)
    {
        while (lo < hi)
        {
            object t = a[lo]; a[lo++] = a[hi]; a[hi--] = t;
        }
    }


    public static int ExtendAndReverseRunRight(object[] A, IComparer comp, int i, int right)
    {
        Debug.Assert(i <= right);

        int j = i;
        if (j == right) return j;
        // Find end of run, and reverse range if descending
        if (comp.Compare(A[j], A[++j]) > 0)
        { // Strictly Descending
            while (j < right && comp.Compare(A[j + 1], A[j]) < 0)
                ++j;
            ReverseRange(A, i, j);
        }
        else
        { // Weakly Ascending
            while (j < right && comp.Compare(A[j + 1], A[j]) >= 0)
                ++j;
        }
        return j;
    }


    /**
     * Sort A[left..right] by straight-insertion sort (both endpoints
     * inclusive), assuming the leftmost nPresorted elements form a weakly
     * increasing run
     */
    public static void InsertionSort(object[] A, IComparer comp, int left, int right, int nPresorted)
    {
        Debug.Assert(right >= left);
        Debug.Assert(right - left + 1 >= nPresorted);

        for (int i = left + nPresorted; i <= right; ++i)
        {
            int j = i - 1;
            object v = A[i];
            while (comp.Compare(v, A[j]) < 0)
            {
                A[j + 1] = A[j];
                --j;
                if (j < left) break;
            }
            A[j + 1] = v;
        }
    }



    private static int NodePower(int left, int right, int startA, int startB, int endB)
    {
        int n = (right - left + 1);
        long l = (long)startA + (long)startB - ((long)left << 1); // 2*middleA
        long r = (long)startB + (long)endB + 1 - ((long)left << 1); // 2*middleB
        int a = (int)((l << 30) / n); // middleA / 2n
        int b = (int)((r << 30) / n); // middleB / 2n
        return (int)Util.LeadingZeroCount((uint)(a ^ b));
    }


    static int Log2(int n)
    {
        if (n <= 0) throw new ArgumentException("lg undefined on non-positive numbers");
        return 31 - (int)Util.LeadingZeroCount((uint)n);
    }
}
