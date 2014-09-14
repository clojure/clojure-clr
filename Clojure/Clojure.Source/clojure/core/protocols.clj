﻿;   Copyright (c) Rich Hickey. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.

(ns clojure.core.protocols)

(set! *warn-on-reflection* true)

(defprotocol CollReduce
  "Protocol for collection types that can implement reduce faster than
  first/next recursion. Called by clojure.core/reduce. Baseline
  implementation defined in terms of Iterable."
  (coll-reduce [coll f] [coll f val]))

(defprotocol InternalReduce
  "Protocol for concrete seq types that can reduce themselves
   faster than first/next recursion. Called by clojure.core/reduce."
  (internal-reduce [seq f start]))

(defn- seq-reduce
  ([coll f]
     (if-let [s (seq coll)]
       (internal-reduce (next s) f (first s))
       (f)))
  ([coll f val]
     (let [s (seq coll)]
       (internal-reduce s f val))))

(extend-protocol CollReduce
  nil
  (coll-reduce
   ([coll f] (f))
   ([coll f val] val))

  Object
  (coll-reduce
   ([coll f] (seq-reduce coll f))
   ([coll f val] (seq-reduce coll f val)))

  clojure.lang.IReduce
  (coll-reduce
   ([coll f] (.reduce coll f))
   ([coll f val] (.reduce coll f val)))
  
  ;;aseqs are iterable, masking internal-reducers
  clojure.lang.ASeq
  (coll-reduce
   ([coll f] (seq-reduce coll f))
   ([coll f val] (seq-reduce coll f val)))

  ;;for range
  clojure.lang.LazySeq
  (coll-reduce
   ([coll f] (seq-reduce coll f))
   ([coll f val] (seq-reduce coll f val)))

  ;;vector's chunked seq is faster than its iter
  clojure.lang.PersistentVector
  (coll-reduce
   ([coll f] (seq-reduce coll f))
   ([coll f val] (seq-reduce coll f val)))
  
  System.Collections.IEnumerable                     ;;;Iterable
  (coll-reduce
   ([coll f]
      (let [iter (.GetEnumerator coll)]              ;;; .iterator
        (if (.MoveNext iter)                         ;;; .hasNext
          (loop [ret (.Current iter)]                ;;; .next
            (if (.MoveNext iter)                     ;;; .hasNext
              (let [ret (f ret (.Current iter))]     ;;; .next
                (if (reduced? ret)
                  @ret
                  (recur ret)))
              ret))
          (f))))
   ([coll f val]
      (let [iter (.GetEnumerator coll)]             ;;; .iterator
        (loop [ret val]
          (if (.MoveNext iter)                      ;;; .hasNext
            (let [ret (f ret (.Current iter))]      ;;; .next
                (if (reduced? ret)
                  @ret
                  (recur ret)))
            ret)))))
  )

(extend-protocol InternalReduce
  nil
  (internal-reduce
   [s f val]
   val)
  
  ;; handles vectors and ranges
  clojure.lang.IChunkedSeq
  (internal-reduce
   [s f val]
   (if-let [s (seq s)]
    (if (chunked-seq? s)
       (let [ret (.reduce (chunk-first s) f val)]
         (if (reduced? ret)
           @ret
           (recur (chunk-next s)
                  f
                  ret)))
       (internal-reduce s f val))
	 val))

  clojure.lang.StringSeq
  (internal-reduce
   [str-seq f val]
   (let [s (.S str-seq)]                             ;;; .s
     (loop [i (.I str-seq)                           ;;; .i
            val val]
       (if (< i (.Length s))                         ;;; .length
         (let [ret (f val (.get_Chars s i))]       ;;; .charAt
                (if (reduced? ret)
                  @ret
                  (recur (inc i) ret)))
         val))))
  
  clojure.lang.ArraySeq_object                             ;;; ArraySeq
  (internal-reduce
       [a-seq f val]
       (let [^objects arr (.Array a-seq)]           ;;; .array
         (loop [i (.Index a-seq)                     ;;; .index
                val val]
           (if (< i (alength arr))
             (let [ret (f val (aget arr i))]
                (if (reduced? ret)
                  @ret
                  (recur (inc i) ret)))
             val))))

  Object                                       ;;;java.lang.Object
  (internal-reduce
   [s f val]
   (loop [cls (class s)
          s s
          f f
          val val]
     (if-let [s (seq s)]
       ;; roll over to faster implementation if underlying seq changes type
       (if (identical? (class s) cls)
         (let [ret (f val (first s))]
                (if (reduced? ret)
                  @ret
                  (recur cls (next s) f ret)))
         (internal-reduce s f val))
       val))))
       
(def arr-impl
  '(internal-reduce
       [a-seq f val]
       (let [^objects arr (.Array a-seq)]                   ;;; .array
         (loop [i (.Index a-seq)                   ;;; .index
                val val]
           (if (< i (alength arr))
             (let [ret (f val (aget arr i))]
                (if (reduced? ret)
                  @ret
                  (recur (inc i) ret)))
             val)))))

(defn- emit-array-impls*
  [syms]
  (apply
   concat
   (map
    (fn [s]
      [(symbol (str "clojure.lang.TypedArraySeq`1[" s "]"))    ;;;  (str "clojure.lang.ArraySeq$ArraySeq_" s)
       arr-impl])
    syms)))
		
(defmacro emit-array-impls
  [& syms]
  `(extend-protocol InternalReduce
     ~@(emit-array-impls* syms)))

;(emit-array-impls int long float double byte char boolean)
(emit-array-impls System.Int32 System.Int64 System.Single System.Double System.Byte System.SByte System.Char System.Boolean 
      System.Int16 System.UInt16 System.UInt32 System.UInt64)

(defprotocol IKVReduce
  "Protocol for concrete associative types that can reduce themselves
   via a function of key and val faster than first/next recursion over map
   entries. Called by clojure.core/reduce-kv, and has same
   semantics (just different arg order)."
  (kv-reduce [amap f init])) 