using System;
using System.Collections;
using System.Diagnostics;

namespace clojure.lang;

// This code is adapted from the Powersort algorithm by J. Ian Munro and Sebastian Wild.
// Original paper: Nearly-Optimal Mergesorts: Fast, Practical Sorting Methods that Optimally Adapt to Exixsting Runs, https://arxiv.org/pdf/1805.04154
// The orginal code is available at https://github.com/sebawild/nearly-optimal-mergesort-code and is under the MIT license.
// I highly suggest visiting the web page https://power-sort.github.io/ which has extensive notes and a wonderful visualizer.

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

/**
 * Powersort implementation as described in the paper.
 *
 * @author Sebastian Wild (wild@uwaterloo.ca)
 */

// The original code is in Java, which I have ported fairly directly to C#.
// Many of the comments have been copied verbatim.
// Significant changes:
//   (1) the Java code is for sorting arrays of ints.  Our code is for sorting arrays of objects, using a provided IComparer.
//   (2) our requirements are for a stable sort.  Within the algorithm, to meet this demand,
//       we need the method for merging runs to ensure stability.
//         (a) This means that we must use MergesAndRuns.mergeRunsCopyHalf instead of MergesAndRuns.mergeRuns in the sorting method.
//         (b) MergesAndRuns.extendAndReverseRunRight is also stable as it reverses only strictly descending runs.
//         (c) Insertsort.insertionSort is stable as implemented (straight insertion with strict < comparison). Ditto the replacement binary sort.
//   (3) several variations of the sorting algorithm are given: powersort, powersortBitwise, powersortIncreasingOnlyMSB.
//       we need only the first of these.  Thus, we really don't need the flags that are used to pick the algorithm to use.
//       Nor do we need the constructor to check constraints on flag combinations.
//       In addition, we can dispense with the weirdness of the instance field myMinRunLen being used to initialize the static field minRunLen
//       when the algorthm starts.  In fact, we can just pass that value to the sorting method directly and avoid the static minRunLen
//       (which would have been a problem when multithreading.
//       With these changes, we can make the class and all methods be static.
//   (4) No need to have the instrumentation to count merge costs.


// Modifications to the Wild code were made to incorporate elements added in CPython's implementation.
// These modifications include:
//   (1) Adaptive selection of the minimum run length.
//   (2) Binary insertion sort instead of selection sort.
//   (3) Pre-merge trimming of runs to reduce the amount of data that needs to be moved during merging.
//   (4) Galloping mode during merging.
//
// The CPython code for this is at https://github.com/python/cpython/blob/v3.13.3/Objects/listobject.c
// There is an excellent writeup at https://github.com/python/cpython/blob/v3.13.3/Objects/listsort.txt of some of the algorithmic decisions,
// such as galloping, bineary insertion sort, // and, of particular relevance to ClojureCLR, why reducing comparisions is more important than reducing data moves.
//  
// CPython is licensed under Python Software Foundation License Version 2 (https://github.com/python/cpython/blob/main/LICENSE). That contains a lot of extra licensing
// agreements and a historical background.  The relevant part for our purposes:

/*
    PYTHON SOFTWARE FOUNDATION LICENSE VERSION 2
    --------------------------------------------

    1. This LICENSE AGREEMENT is between the Python Software Foundation
    ("PSF"), and the Individual or Organization("Licensee") accessing and
    otherwise using this software ("Python") in source or binary form and
    its associated documentation.

    2. Subject to the terms and conditions of this License Agreement, PSF hereby
    grants Licensee a nonexclusive, royalty-free, world-wide license to reproduce,
    analyze, test, perform and/or display publicly, prepare derivative works,
    distribute, and otherwise use Python alone or in any derivative version,
    provided, however, that PSF's License Agreement and PSF's notice of copyright,
    i.e., "Copyright (c) 2001 Python Software Foundation; All Rights Reserved"
    are retained in Python alone or in any derivative version prepared by Licensee.

    3. In the event Licensee prepares a derivative work that is based on
    or incorporates Python or any part thereof, and wants to make
    the derivative work available to others as provided herein, then
    Licensee hereby agrees to include in any such work a brief summary of
    the changes made to Python.

    4. PSF is making Python available to Licensee on an "AS IS"
    basis.  PSF MAKES NO REPRESENTATIONS OR WARRANTIES, EXPRESS OR
    IMPLIED.  BY WAY OF EXAMPLE, BUT NOT LIMITATION, PSF MAKES NO AND
    DISCLAIMS ANY REPRESENTATION OR WARRANTY OF MERCHANTABILITY OR FITNESS
    FOR ANY PARTICULAR PURPOSE OR THAT THE USE OF PYTHON WILL NOT
    INFRINGE ANY THIRD PARTY RIGHTS.

    5. PSF SHALL NOT BE LIABLE TO LICENSEE OR ANY OTHER USERS OF PYTHON
    FOR ANY INCIDENTAL, SPECIAL, OR CONSEQUENTIAL DAMAGES OR LOSS AS
    A RESULT OF MODIFYING, DISTRIBUTING, OR OTHERWISE USING PYTHON,
    OR ANY DERIVATIVE THEREOF, EVEN IF ADVISED OF THE POSSIBILITY THEREOF.

    6. This License Agreement will automatically terminate upon a material
    breach of its terms and conditions.

    7. Nothing in this License Agreement shall be deemed to create any
    relationship of agency, partnership, or joint venture between PSF and
    Licensee.  This License Agreement does not grant permission to use PSF
    trademarks or trade name in a trademark sense to endorse or promote
    products or services of Licensee, or any third party.

    8. By copying, installing or otherwise using Python, Licensee
    agrees to be bound by the terms and conditions of this License
    Agreement.

*/


public static class Powersorter
{
    /*

    Please refer to Munro+Wild paper and the listsort.txt document cited above for a much more complete discussion.
    My goal here is just to describe the basic algorithm above so that we can understand the optimizations we will apply to it.

    I assume basic familiarity with mergesort, insertion sort, etc.  The basic mergesort you probably learned starts of treating an array of size n
    as n segments of length 1.  One then takes pairs of those segments and merges them into segments of size 2 that are in order.  
    Then takes pairs of the size 2 segments and merge them to get ordered segments of size 4, etc., until you end up with one ordered segment.
    It works, but can be improved (fewer moves, fewer comparisons) by taking advantage of the structure of the data at runime.

    I also assume familiarity with the notion of a sorting algorithm being 'stable'.  Namely, if the input array contains two entries with equal keys,
    the one that is to the left of the other will still be to the left of the other in the sorted array.  We want a stable sorting algorithm in the end.
    I will mention throughout where one has to be careful to guarantee stability.  

    For the basic mergesort listed above, stability is achieved if, when merging two adjacent segments, when  'left' entry has the same key as a 'right' entry,
    the left entry is moved into the result before the right entry is.

    Powersort (implemented here), Timsort and some other related sorts are varieties of what Knuth called "natural mergesorts" (according to Munro+Wild).

    Optimization #1: The most basic natural mergesort identifies existing 'runs', maximal segements that are already ordered and uses those segments as the initial state
    from which to begin merging adjacent pairs of segments.  A run is a segment of the array which a[i] <= a[i+1] <= ... <= a[j].  Munro+Wil use the term "weakly increasing"
    for this.  

    Optimization #2: If we are starting at position i and looking for the longest run, we can write the obvious loop to keep iterating until we find an entry 
    that is strictly less than its predecessor.  This would be a maximal weakly increasing segment.  However, if we look at the first pair of entries and notice
    that a[i] > a[i+1] we can instead find the maximal _strictly decreasing_ segment, then reverse it in place.  This would be better than just treating a[i] by itself
    as a segment of length 1.  This is quite effective as reversing in place is linear and requires no additional comparisons.  Though we can work with identified 
    weakly increasing segments, we must us _strictly decreasing_ here; if we had adjacent items with identical keys, reversing the segment would reverse their order,
    violating stability.

    Optimization #3: Minimum run length.  It could be that (some of) the runs identified are still quite short.  Mergesort is not efficient merging short runs. 
    One can enforce a minimum run length as follows.  Take the run (the rightmost so far) that we have identifed.  If it is shorter than a value we'll call minRunLength,
    we will form a run of length minRunLength by just extending the right end of our run and sorting that segment using another, in-place, stable sorting algorithm.
    There we use a form of insertion sort that is optimized using the fact that the beginning of the run  is already sorted.  
    Later, we will use another algorithm that is even more efficient than this insertion sort.   It is important here that the minRunLength be large
    enough to improve the performance of the mergesort, but small enough that we do not pay a penalty for using something like insertionsort.  In CPython, the value
    of minRunLength is determined dynamically at the start of the sorting run.

    Optimization #4: Merge order.  Let's consider a simplified mergesort where we identify the runs, then do merges.  The order in which you do merges affects the efficiency. 
    Let us take the you have identified the set of runs, one can do merges of adjacent runs over and over until the array is sorted.  The order in which you do the merges
    has a significant effect on the total cost.  We can assume the cost of merging is proportional to the sum of the lengths of the two runs.  

    A merge sequence is defined by a binary treebuilt over the runs as leaves.  Different ways of building the tree, i.e. different merge orders, result in varying merge costs.
    Minimizing the cost is related to the notion of creating     an optimal binary search tree, as the Munro/Wild paper points out (with citations of other relevant literature).

    Forming an optimal binary search tree takes too much time and space, so we will be satisfied with near-optimality as a tradeoff.
    The approach in Powersort keeps a stack of runs that remain to be merged; the stack is in left-to-right order over some intial segment of runs.
    On each iteration:  find the next run, look at the runs already set up to its left, perform some merges now, push the run onto the stack, continue.
    At the end, there will be some runs still on the stack. These will be merged left-to-right.  

    Priority of merges is calculated by calculating the 'power' of a node.  The power is calculated between adjacent runs. Of note is that the calculation is local:
    given the left and right indexes of the array as a whole, the start index of run A, the start and end indexes of B (adjectent to the right of A).   
    Some arithmetic and bit-twiddling and we get a number.  See method NodePower.  (I won't go into details.  The calculation is based on entropy calculations
    related to binary search trees.  Read the paper.)  

    At any given point in time, we have a stack of runs waiting to be merged.  They are in order from left to right in the stack (bottom to top), 
    which also corresponds to the order by node power.  We maintain this invariant by the following trick: when we are ready to add a run to the stack,
    we merge the run with runs to the left that have a higher power rating.  Thus, when we push this (perhaps now larger run), its power is greater than the one's below it.

    Another trick:  Rather than the obvious stack implementation, we use the facts that (a) the runs have a unique index (its power), (b) stack order corresponds to power order, 
    and (c) the highest value for the power is log(n)+1 (n = size of array), to preallocate an array of that size and store each run in the array in the index given by its power.

    Now the heart of the algorithm's main loop is:  
       Let A be the rightmost identifed run (from the previous iteration).
       Let B be the next run (calculated as above)
       Let k = Power of the A/B merge 
       Working top down on the stack, merge A successively with the top item of the stack if its power is > k.
       Add the (merged) A to the stack at index k.
       B becomes the new A for the next iteration.

    When this loop exits,  we likely will have some runs still in the stack.  Merge them, starting at the top.

    And there you have it.

    The remaining optimizations come from CPython and similar implementations.  They are not in the basic algorithm, but will be implemented in the real code.

    Optimization #4: Adaptive minRunLength.  Details of this are in the listsort.txt document cited above.  
    We set a constant MaxMinRun.  (CPython uses 64).
    If the array size is less than this, we just use our direct sort (which will be binary insertion sort) and forgo doing mergesort at all.
    Otherwise, we compute a minRunLen between MaxMinRun/2 and MaxMinRun+1 that, when n is divided by it, is a power of 2 or close to one.
    See ComputeMinRun.

    Optimzation #5: Pre-merge trim.  For adjacent runs A and B, search for two locations:  
      (1) The place where the first element of B would be inserted into A.
      (2) The place where the first element of A would be inserted into B.
    We can then reduce the size of A and B to be merged by excluding the left side of A and the right side of B to those positions --
    those elements are already in their final positions after the merge.  This reduces the amount of data that needs to be moved during merging.
   
    Optimization #6: Galloping in the pre-merge trim. When doing the searching, search elements at progressive offsets: 1, 3, 7,....  
    The offset value increases exponentially. Once you have identified a bracketing range where the item lies, you can do binary search within that range.
    This has some provable advantages over linear searches on longer ranges.

    Optimization #7: Galloping during merging.  One begins merging linearly.  If one run is winning over the other for a number of consecutive comparisons, 
    switch to galloping mode to do lookups.  Add a drop back to linear mode if the galloping becomes less successful.  

    All of these optimizations occur in the implementation.

    */

    // Indicates an unused index in the leftRunStart array.
    private const int NullIndex = int.MinValue;

    // Sort array A[left..right] using the provided IComparer.  The sort is stable.
    public static void Powersort(object[] A, IComparer comp, int left, int right)
    {
        int n = right - left + 1;

        if (n <= 1)
            // Well, that was easy.
            return;

        // Original Wild code used a constant minRunLen, but CPython uses an adaptive value based on the size of the array.
        int minRunLen = ComputeMinRun(n);

        // The stack of identified runs waiting to be merged.
        // We use the fact that the maximum power of a node is log(n)+1 to preallocate an array of that size.
        // The stack is implemented as two parallel arrays, one for the start index of the run and one for the end index of the run.
        // This is a sparse stack implementation.  An item in the stack is stored at the index corresponding to its power.
        // The stack is in left-to-right order, which also corresponds to increasing power.
        // This invariant is maintained by merging runs with higher power than the current run before pushing the current run onto the stack.

        int lgnPlus2 = Log2(n) + 2;
        int[] leftRunStart = new int[lgnPlus2];
        int[] leftRunEnd = new int[lgnPlus2];

#if NETFRAMEWORK 
            // Array.Fill not availble in .NET Framework, so we have to do this the old fashioned way.
            for (int i = 0; i < leftRunStart.Length; i++)
            {
                leftRunStart[i] = NullIndex;
            }
#else
        Array.Fill(leftRunStart, NullIndex);
#endif

        // the index of the highest occupied slot in the leftRunStart and leftRunEnd arrays.  This is the top of the stack.
        int top = 0;

        // Buffer for merging.  Note we allocate to the size of the input array.
        // Possible space optimization: allocate to the size of the largest run, increasing the size of the buffer as needed.

        object[] buffer = new object[n];

        // CPython addition -- allows sharing of galloping state between merges.  This is a performance optimization.
        MergeState ms = new();

        // Identify leftmost run.

        int startA = left;
        int endA = ExtendAndReverseRunRight(A, comp, startA, right);

        // extend to minRunLen
        int lenA = endA - startA + 1;
        if (lenA < minRunLen)
        {
            endA = Math.Min(right, startA + minRunLen - 1);
            BinarySort(A, comp, startA, endA, lenA);       // original Wild code used InsertionSort, but CPython uses BinarySort
        }

        // Iterate through remaining runs.
        // We have run A, not yet stacked, identified on previous iteration (or, initially, above)
        //  We will identify the next run B, then merge A with runs on the stack that have higher power than the A/B merge.
        // Then we stack (the possibly merged) A and continue with B as the new A.

        while (endA < right)
        {
            // identify next run B

            int startB = endA + 1;
            int endB = ExtendAndReverseRunRight(A, comp, startB, right);

            // extend to minRunLen
            int lenB = endB - startB + 1;
            if (lenB < minRunLen)
            {
                endB = Math.Min(right, startB + minRunLen - 1);
                BinarySort(A, comp, startB, endB, lenB);  // original Wild code used InsertionSort, but CPython uses BinarySort
            }

            // Compute the power of the A/B merge node.

            int k = NodePower(left, right, startA, startB, endB);
            Debug.Assert(k != top);

            // Merge A with runs on the stack that have higher power than the A/B merge node.
            for (int l = top; l > k; --l)
            {
                if (leftRunStart[l] == NullIndex) continue;
                MergeRunsCopyHalf(ms, A, comp, leftRunStart[l], leftRunEnd[l] + 1, endA, buffer);
                startA = leftRunStart[l];
                leftRunStart[l] = NullIndex;
            }

            // store left half of merge between A and B
            leftRunStart[k] = startA;
            leftRunEnd[k] = endA;
            top = k;

            // Now B becomes the new A for the next iteration.
            startA = startB;
            endA = endB;
        }

        Debug.Assert(endA == right);

        // All runs identified. There are still merges on the stack to be done.  Merge them in order from top to bottom.

        for (int l = top; l > 0; --l)
        {
            if (leftRunStart[l] == NullIndex) continue;
            MergeRunsCopyHalf(ms, A, comp, leftRunStart[l], leftRunEnd[l] + 1, right, buffer);
        }
    }


    // The merge routine.
    // This modifies the original Wild code with CPython modifications to implement
    //   (1) Pre-merge trimming of runs, with galloping.
    //   (2) Galloping during merging.

    static void MergeRunsCopyHalf(MergeState ms, object[] A, IComparer comp, int l, int m, int r, object[] B)
    {
        int n1 = m - l;
        int n2 = r - m + 1;

        Debug.Assert(B.Length >= n1 || B.Length >= n2, "Insufficient space in temporary array B");


        // We are merging  P = A[l..(m-1)] and Q = A[m..r]
        // Pre-merge trimming involves identifying:
        //   (a) where in P the first element of Q will go
        //   (b) where in Q the last element of P will go
        // This identifies offsets (the left side of P or the right side of Q) that do not need to be moved.
        // (And in fact, we could have a situation where A[m-1] <= A[m] and no merge is required.)
        // From this we get reduced runs:  P' and Q'.
        // Which side gets copied to the buffer depends on the sizes of reduced runs.       

        // Determine where the first element of Q would be inserted in P
        int lprime = GallopRight(A, comp, l, m - 1, l, A[m]);

        if (lprime == m)
            // all of P is <= first element of Q, nothing to do.
            return;

        n1 = m - lprime;  // reduced size

        // Determine where the last element of P would be inserted in Q
        int rprime = GallopLeft(A, comp, m, r, r, A[m - 1]);

        if (rprime == m)
            // this will only happen with an inconsistent comparator
            // But if we don't do this, we will get an execption in merging code.
            return;

        n2 = rprime - m;

        // Merge.  The smaller will be copied to the buffer, then we will merge from the buffer into the original array.

        if (n1 <= n2)
        {
            MergeLow(ms, A, comp, lprime, m, rprime - 1, B);
        }
        else
        {
            MergeHigh(ms, A, comp, lprime, m, rprime - 1, B);
        }
    }


    // From CPython: galloping

    /*
        Adapted from CPython's code.
        A _lot_ of adaption since we have to adjust from C's pointer arithmetic 
        to C#'s array indexing with specified beginning/ending indexes.

        Locate the proper position of key in a sorted vector. 
        For stability, we must differentiate which direction we are searching
        and where our probe is inserted relative to any elements in the target array
        that have equal keys:
    
        GallopLeft: if the vector contains an element equal to the probe,
        return the position immediately to the left of the leftmost equal element. 
    
        GallopRight: if the vector contains an element equal to the probe,
        return the position immediately to the right of the rightmost equal element.
   
        The target array is A[left..right] (inclusive), sorted.
        Condition: left < right.
        The hint is an index into the array at which to begin the search.  
        Condition: left <= hint <= right.

        The return value is the int k in left..(right+1), such that 
            
            a[k-1] < key <= a[k]   (GallopLeft)
            a[k-1] <= key < a[k]   (GallopRight)

        pretending that A[left-1] is minus infinity and A[right+1] is plus infinity.  
        if k = left, key belongs at the beginning of the array; 
        if k = right+1, key belongs at the end (i.e., it does not get inserted).

     */

    static int GallopLeft(object[] A, IComparer comp, int left, int right, int hint, object probe)
    {
        int n = right - left + 1;

        Debug.Assert(probe is not null && n > 0 && left <= hint && hint <= right);

        int ofs = 1;
        int lastofs = 0;

        if (comp.Compare(A[hint], probe) < 0)
        {
            // A[hint] < probe
            // gallop right, until a[hint + lastofs] < key <= a[hint + ofs]

            int maxofs = right - hint + 1;
            while (ofs < maxofs)
            {
                if (comp.Compare(A[hint + ofs], probe) < 0)
                {
                    lastofs = ofs;
                    Debug.Assert(ofs <= (Int32.MaxValue - 1) / 2);
                    ofs = (ofs << 1) + 1;
                }
                else                // key <= a[hint + ofs]
                    break;
            }
            if (ofs > maxofs)
                ofs = maxofs;
            // Translate back to offsets relative to left.
            lastofs += hint;
            ofs += hint;
        }
        else
        {
            // key <= a[hint] -- gallop left, until
            // a[hint - ofs] < key <= a[hint - lastofs]
            int maxofs = hint - left + 1;
            while (ofs < maxofs)
            {
                if (comp.Compare(A[hint - ofs], probe) < 0)
                    break;
                // key <= a[hint - ofs]
                lastofs = ofs;
                Debug.Assert(ofs <= (Int32.MaxValue - 1) / 2);
                ofs = (ofs << 1) + 1;
            }
            if (ofs > maxofs)
                ofs = maxofs;
            // Translate back to positive offsets relative to right.
            int k = lastofs;
            lastofs = hint - ofs;
            ofs = hint - k;
        }

        Debug.Assert(left - 1 <= lastofs && lastofs < ofs && ofs <= right + 1);

        // Now a[lastofs] < key <= a[ofs], so key belongs somewhere to the
        // right of lastofs but no farther right than ofs.  Do a binary
        // search, with invariant a[lastofs-1] < key <= a[ofs].

        ++lastofs;
        while (lastofs < ofs)
        {
            int m = lastofs + ((ofs - lastofs) >> 1);

            if (comp.Compare(A[m], probe) < 0)
                lastofs = m + 1;                  /* a[m] < key */
            else
                ofs = m;                          /* key <= a[m] */
        }
        Debug.Assert(lastofs == ofs);             /* so a[ofs-1] < key <= a[ofs] */
        return ofs;
    }


    static int GallopRight(object[] A, IComparer comp, int left, int right, int hint, object probe)
    {
        int n = right - left + 1;

        Debug.Assert(probe is not null && n > 0 && left <= hint && hint <= right);

        int ofs = 1;
        int lastofs = 0;


        if (comp.Compare(probe, A[hint]) < 0)
        {
            // key <  a[hint] -- gallop left, until
            // a[hint - ofs] <= key < a[hint - lastofs]

            int maxofs = hint - left + 1;
            while (ofs < maxofs)
            {
                if (comp.Compare(probe, A[hint - ofs]) < 0)
                {
                    lastofs = ofs;
                    Debug.Assert(ofs <= (Int32.MaxValue - 1) / 2);
                    ofs = (ofs << 1) + 1;
                }
                else
                    break;
            }
            if (ofs > maxofs)
                ofs = maxofs;

            // Translate back to positive offsets relative to right.
            int k = lastofs;
            lastofs = hint - ofs;
            ofs = hint - k;

        }
        else
        {
            // a[hint] <= probe -- gallop right, until
            // a[hint + lastofs] <= key < a[hint + ofs]
            int maxofs = right - hint + 1;
            while (ofs < maxofs)
            {
                if (comp.Compare(probe, A[hint + ofs]) < 0)
                    break;
                else
                {
                    lastofs = ofs;
                    Debug.Assert(ofs <= (Int32.MaxValue - 1) / 2);
                    ofs = (ofs << 1) + 1;
                }
            }
            if (ofs > maxofs)
                ofs = maxofs;
            /*   Translate back to offsets relative to left. */
            lastofs += hint;
            ofs += hint;

        }

        Debug.Assert(left - 1 <= lastofs && lastofs < ofs && ofs <= right + 1);

        // Now a[lastofs] < key <= a[ofs], so key belongs somewhere to the
        // right of lastofs but no farther right than ofs.  Do a binary
        // search, with invariant a[lastofs-1] < key <= a[ofs].

        ++lastofs;
        while (lastofs < ofs)
        {
            int m = lastofs + ((ofs - lastofs) >> 1);

            if (comp.Compare(probe, A[m]) < 0)
                ofs = m;                           // key < a[m]
            else
                lastofs = m + 1;                   // a[m] <= key

        }

        Debug.Assert(lastofs == ofs);            // so a[ofs-1] <= key < a[ofs]
        return ofs;
    }

    // More from CPython


    // When we get into galloping mode, we stay there until both runs win less
    // often than MIN_GALLOP consecutive times.  See listsort.txt for more info.

    const int MinGallop = 7;

    // One MergeState exists on the stack per invocation of mergesort.  It's just
    // a convenient way to pass state around among the helper functions.

    class MergeState
    {
        // This controls when we get *into* galloping mode.  It's initialized
        // to MIN_GALLOP.  merge_lo and merge_hi tend to nudge it higher for
        // random data, and lower for highly structured data.
        public int MinGallop { get; set; } = Powersorter.MinGallop;
    }

    // Merge the two runs A[l..m-1] and A[m..r] in place.
    // The left run is smaller than the right run.
    // We copy the left run into a temporary array, then merge back into the original array.
    static void MergeLow(MergeState ms, object[] A, IComparer comp, int l, int m, int r, object[] B)
    {

        Debug.Assert(ms is not null);

        // We are merging P = A[l..(m-1)] and Q = A[m,r].
        // B is a buffer to hold the shorter array during copying.

        int np = m - l;         // size of P
        int nq = r - m + 1;     // size of Q

        Debug.Assert(np > 0 && nq > 0, "MergeLow requires that both runs be non-empty");
        Debug.Assert(np <= nq, "MergeLow requires that the first run be no longer than the second run");
        Debug.Assert(B.Length >= np, "Insufficient space in temporary array B");

        Array.Copy(A, l, B, 0, np);  // Copy P into buffer B

        int dIdx = l;  // Index in A where we are writing the merged output
        int pIdx = 0;     // Index in buffer B (P)
        int qIdx = m;     // Index in A for Q

        object[] D = A;  // For clarity, D is the destination array (A), always index by dIdx
        object[] P = B;  // For clarity, P is the buffer (B), always index by pIdx
        object[] Q = A;  // For clarity, Q is the second run in A, always index by qIdx

        // pre-merge trim guarantess that Q[0] < P[0], so we can copy the first element of Q to D and advance the indexes accordingly.

        D[dIdx++] = Q[qIdx++];  // Copy first element of Q to D
        --nq;


        // This try block is to catch any exceptions that might occur, the only real possibiltiy being an exception from the comparator.
        // If that happens, we want to copy the remaining elements of P into D before rethrowing the exception.
        try
        {
            if (nq == 0) goto CopyP;
            if (np == 1) goto CopyQ;

            int minGallop = ms.MinGallop;


            // This loop keeps going until we run out of elements in either P or Q.
            // It starts in simple copying mode, but if one of P or Q wins minGallop times in a row, we switch to galloping mode.
            // If we are in galloping mode but fall out of it, then we iterate back to the simple copying mode.
            // Both the simple mode and gallop mode loops have exits for when we are done.

            for (; ; )
            {
                int pcount = 0;          /* # of times P won in a row */
                int qcount = 0;          /* # of times Q won in a row */

                // We move one element at time from P/Q to D.
                // We exit the loop in three ways:
                //  - We run out of elements in Q (then we goto CopyP to finish copying P).
                //  - There is only one element left in P (then we goto CopyQ to finish copying Q and the last element of P)
                //  - one of P or Q has won minGallop times in a row (then we goto galloping mode).  That's the breaks.
                for (; ; )
                {
                    Debug.Assert(np > 1 && nq > 0);

                    if (comp.Compare(Q[qIdx], P[pIdx]) < 0)
                    {
                        // Q < P.  Copy Q to D.

                        D[dIdx++] = Q[qIdx++];
                        --nq;
                        ++qcount;
                        pcount = 0;
                        if (nq == 0)
                            goto CopyP;
                        if (qcount >= minGallop)
                            break;
                    }
                    else
                    {
                        // P <= Q.  Copy P to D.

                        D[dIdx++] = P[pIdx++];
                        --np;
                        ++pcount;
                        qcount = 0;
                        if (np == 1)
                            goto CopyQ;
                        if (pcount >= minGallop)
                            break;
                    }
                }


                // One run is winning so consistently that galloping may
                // be a huge win.  So try that, and continue galloping until
                // (if ever) neither run appears to be winning consistently
                // anymore.

                ++minGallop;
                do
                {
                    Debug.Assert(np > 1 && nq > 0);

                    if (minGallop > 1)
                        --minGallop;
                    ms.MinGallop = minGallop;

                    int k = GallopRight(P, comp, pIdx, pIdx + np - 1, pIdx, Q[qIdx]);
                    pcount = k - pIdx;

                    if (k != pIdx)
                    {
                        Array.Copy(P, pIdx, D, dIdx, pcount);
                        pIdx += pcount;
                        dIdx += pcount;
                        np -= pcount;
                        if (np == 1)
                            goto CopyQ;
                        // np==0 is impossible now if the comparison
                        // function is consistent, but we can't assume
                        // that it is.
                        if (np == 0)
                            goto CopyP;
                    }
                    //  We can copy one element from Q to D, at minimum.
                    D[dIdx++] = Q[qIdx++];
                    --nq;
                    if (nq == 0)
                        goto CopyP;

                    k = GallopLeft(Q, comp, qIdx, qIdx + nq - 1, qIdx, P[pIdx]);
                    qcount = k - qIdx;
                    if (k != qIdx)
                    {
                        Array.Copy(Q, qIdx, D, dIdx, qcount);
                        qIdx += qcount;
                        dIdx += qcount;
                        nq -= qcount;
                        if (nq == 0)
                            goto CopyP;
                    }
                    // We can copy one element from P to D, at minimum.
                    D[dIdx++] = P[pIdx++];
                    --np;
                    if (np == 1)
                        goto CopyQ;
                } while (pcount >= MinGallop || qcount >= MinGallop);
                ++minGallop;           // penalize it for leaving galloping mode
                ms.MinGallop = minGallop;
            }


        CopyP:

            {
                // We have no more elements in Q, so copy the remaining elements of P into D and return.
                // Thus, when B is empty, we may copy the remaining elements of P into D and return.
                if (np > 0)
                    Array.Copy(P, pIdx, D, dIdx, np);
                return;
            }

        CopyQ:

            {
                Debug.Assert(np == 1 && nq > 0);
                // only one element in P, and the last element of P belongs at the end of the merge.
                // Copy Q and then the last element of P.
                Array.Copy(Q, qIdx, D, dIdx, nq);
                D[dIdx + nq] = P[pIdx];
                return;
            }
        }
        catch (Exception)
        {
            if (np > 0)
                Array.Copy(P, pIdx, D, dIdx, np);
            throw;
        }
    }


    // Same as MergeLow, but the right run is smaller than the left run.

    static void MergeHigh(MergeState ms, object[] A, IComparer comp, int l, int m, int r, object[] B)
    {

        Debug.Assert(ms is not null);

        // We are merging P = A[l..(m-1)] and Q = A[m,r].
        // B is a buffer to hold the shorter array during copying.

        int np = m - l;         // size of P
        int nq = r - m + 1;     // size of Q

        Debug.Assert(np > 0 && nq > 0, "MergeHigh requires that both runs be non-empty");
        Debug.Assert(nq <= np, "MergeHigh requires that the second run be no longer than the first run");
        Debug.Assert(B.Length >= nq, "Insufficient space in temporary array B");

        Array.Copy(A, m, B, 0, nq);  // Copy Q into buffer B

        // We are filling down from the right.
        int dIdx = r;     // Index in A where we are writing the merged output
        int pIdx = m - 1;   // Index in A for P
        int qIdx = nq - 1;  // Index in B (for Q)

        object[] D = A;   // For clarity, D is the destination array (A), always index by dIdx
        object[] P = A;   // For clarity, P is the first run in A, always index by pIdx
        object[] Q = B;   // For clarity, Q is the buffer B, holding the copy of Q

        // pre-merge trim guarantess that Q[right-end] < P[right-end], so we can copy the last element of P to D and advance the indexes accordingly.

        D[dIdx--] = P[pIdx--];  // Copy last element of P to D
        --np;

        // This try block is to catch any exceptions that might occur, the only real possibiltiy being an exception from the comparator.
        // If that happens, we want to copy the remaining elements of P into D before rethrowing the exception.
        try
        {
            if (np == 0) goto CopyQ;
            if (nq == 1) goto CopyP;

            int minGallop = ms.MinGallop;

            // This loop keeps going until we run out of elements in either P or Q.
            // It starts in simple copying mode, but if one of P or Q wins minGallop times in a row, we switch to galloping mode.
            // If we are in galloping mode but fall out of it, then we iterate back to the simple copying mode.
            // Both the simple mode and gallop mode loops have exits for when we are done.

            for (; ; )
            {
                int pcount = 0;          /* # of times P won in a row */
                int qcount = 0;          /* # of times Q won in a row */

                // We move one element at time from P/Q to D.
                // We exit the loop in three ways:
                //  - We run out of elements in Q (then we goto CopyP to finish copying P).
                //  - There is only one element left in P (then we goto CopyQ to finish copying Q and the last element of P)
                //  - one of P or Q has won minGallop times in a row (then we goto galloping mode).  That's the breaks.
                for (; ; )
                {
                    Debug.Assert(np > 0 && nq > 1);

                    if (comp.Compare(Q[qIdx], P[pIdx]) < 0)
                    {
                        // Q < P.  Copy P to D.

                        D[dIdx--] = P[pIdx--];
                        --np;
                        ++pcount;
                        qcount = 0;
                        if (np == 0)
                            goto CopyQ;
                        if (pcount >= minGallop)
                            break;
                    }
                    else
                    {
                        // P <= Q.  Copy Q to D.

                        D[dIdx--] = Q[qIdx--];
                        --nq;
                        ++qcount;
                        pcount = 0;
                        if (nq == 1)
                            goto CopyP;
                        if (qcount >= minGallop)
                            break;
                    }
                }


                // One run is winning so consistently that galloping may
                // be a huge win.  So try that, and continue galloping until
                // (if ever) neither run appears to be winning consistently
                // anymore.
                ++minGallop;
                do
                {
                    Debug.Assert(np > 0 && nq > 1);

                    if (minGallop > 1)
                        --minGallop;
                    ms.MinGallop = minGallop;

                    int k = GallopRight(P, comp, l, pIdx, pIdx, Q[qIdx]);
                    pcount = pIdx + 1 - k;

                    if (k != pIdx + 1)
                    {
                        Array.Copy(P, pIdx - pcount + 1, D, dIdx - pcount + 1, pcount);
                        pIdx -= pcount;
                        dIdx -= pcount;
                        np -= pcount;
                        if (np == 0)
                            goto CopyQ;
                    }
                    //  We can copy one element from Q to D, at minimum.
                    D[dIdx--] = Q[qIdx--];
                    --nq;
                    if (nq == 1)
                        goto CopyP;

                    k = GallopLeft(Q, comp, 0, qIdx, qIdx, P[pIdx]);
                    qcount = qIdx + 1 - k;
                    if (k != qIdx + 1)
                    {
                        Array.Copy(Q, qIdx - qcount + 1, D, dIdx - qcount + 1, qcount);
                        qIdx -= qcount;
                        dIdx -= qcount;
                        nq -= qcount;
                        if (nq == 1)
                            goto CopyP;
                        if (nq == 0)
                            goto CopyQ;
                    }
                    // We can copy one element from P to D, at minimum.
                    D[dIdx--] = P[pIdx--];
                    --np;
                    if (np == 0)
                        goto CopyQ;
                } while (pcount >= MinGallop || qcount >= MinGallop);
                ++minGallop;           /* penalize it for leaving galloping mode */
                ms.MinGallop = minGallop;
            }


        CopyP:

            {
                Debug.Assert(nq == 1 && np > 0);
                // only one element in Q, and the first element of Q belongs at the beginning of the merge.
                // Copy Q and then the last element of P.
                Array.Copy(P, l, D, l + 1, np);
                D[l] = Q[0];
                return;

            }

        CopyQ:

            {
                // We have no more elements in P, so copy the remaining elements of Q into D and return.
                // Thus, when P is empty, we may copy the remaining elements of Q into D and return.
                if (nq > 0)
                    Array.Copy(Q, 0, D, dIdx - nq + 1, nq);
                return;
            }
        }
        catch (Exception)
        {
            if (nq > 0)
                Array.Copy(Q, 0, D, dIdx - nq + 1, nq);
            throw;
        }
    }



    // More Munro/Wild code


    // Reverse the order of the elements in a[lo..hi], in place.
    public static void ReverseRange(object[] a, int lo, int hi)
    {
        while (lo < hi)
        {
            object t = a[lo]; a[lo++] = a[hi]; a[hi--] = t;
        }
    }


    // Extend the run right from index i, and reverse it if it is descending.
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


    // Ported from CPython (including the comments).

    // binarysort is the best method for sorting small arrays: it does few
    // compares, but can do data movement quadratic in the number of elements.
    // ss->keys is viewed as an array of n kays, a[:n]. a[:ok] is already sorted.
    //
    // Pass ok = 0(or 1) if you don't know.
    //
    // It's sorted in-place, by a stable binary insertion sort. If ss->values
    // isn't NULL, it's permuted in lockstap with ss->keys.
    // On entry, must have n >= 1, and 0 <= ok <= n <= MAX_MINRUN.
    // Return -1 if comparison raises an exception, else 0.
    //
    // Even in case of error, the output slice will be some permutation of
    // the input (nothing is lost or duplicated).

    // That code had switchable sections (via #if sections) with both insertion sort and binary sort.
    // I deleted the insertion sort code.
    // Other changes: ss->keys vs ss-> values is not a thing -- we have our comparator.

    static void BinarySort(object[] A, IComparer comp, int left, int right, int nPresorted)
    {
        int n = right - left + 1;

        Debug.Assert(right >= left);
        Debug.Assert(nPresorted <= n);

        if (nPresorted == 0)
            nPresorted = 1;


        /* Regular insertion sort has average- and worst-case O(n**2) cost
           for both # of comparisons and number of bytes moved. But its branches
           are highly predictable, and it loves sorted input (n-1 compares and no
           data movement). This is significant in cases like sortperf.py's %sort,
           where an out-of-order element near the start of a run is moved into
           place slowly but then the remaining elements up to length minrun are
           generally at worst one slot away from their correct position (so only
           need 1 or 2 commpares to resolve). If comparisons are very fast (such
           as for a list of Python floats), the simple inner loop leaves it
           very competitive with binary insertion, despite that it does
           significantly more compares overall on random data.

           Binary insertion sort has worst, average, and best case O(n log n)
           cost for # of comparisons, but worst and average case O(n**2) cost
           for data movement. The more expensive comparisons, the more important
           the comparison advantage. But its branches are less predictable the
           more "randomish" the data, and that's so significant its worst case
           in real life is random input rather than reverse-ordered (which does
           about twice the data movement than random input does).

           Note that the number of bytes moved doesn't seem to matter. MAX_MINRUN
           of 64 is so small that the key and value pointers all fit in a corner
           of L1 cache, and moving things around in that is very fast. */

        int L, R, M;
        int ok = left + nPresorted;
        object pivot;

        for (; ok <= right; ++ok)
        {
            // set L to where a[ok] belongs 
            L = left;
            R = ok;
            pivot = A[ok];
            /* Slice invariants. vacuously true at the start:
             * all a[left:L]  <= pivot
             * all a[L:R]     unknown
             * all a[R:ok]  > pivot
             */
            Debug.Assert(L < R);
            do
            {
                /* don't do silly ;-) things to prevent overflow when finding
                   the midpoint; L and R are very far from filling a Py_ssize_t */
                M = (L + R) >> 1;

                if (comp.Compare(pivot, A[M]) < 0)
                    R = M;
                else
                    L = M + 1;

            } while (L < R);

            Debug.Assert(L == R);

            /* a[:L] holds all elements from a[:ok] <= pivot now, so pivot belongs
               at index L. Slide a[L:ok] to the right a slot to make room for it.
               Caution: using memmove is much slower under MSVC 5; we're not
               usually moving many slots. Years later: under Visual Studio 2022,
               memmove seems just slightly slower than doing it "by hand". */

            for (M = ok; M > L; --M)
                A[M] = A[M - 1];

            A[L] = pivot;
        }
    }


    // Code from Wild.


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


    // CPython's minRunLength calculations.

    const int MaxMinRunPower = 6;
    const int MaxMinRun = 1 << 6;

    // Ported from CPython
    static int ComputeMinRun(int n)
    {
        int r = 0;

        Debug.Assert(n >= 0);
        while (n >= MaxMinRun)
        {
            r |= n & 1;
            n >>= 1;
        }
        return n + r;
    }

}
