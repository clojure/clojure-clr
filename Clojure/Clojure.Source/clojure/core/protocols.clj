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

;; mutates the iterator, respects reduced
(defn iterator-reduce!
  ([^System.Collections.IEnumerator iter f]               ;;; ^java.lang.Iterator
   (if (.MoveNext iter)                                   ;;; .hasNext
     (iterator-reduce! iter f (.Current iter))            ;;; .next
     (f)))
  ([^System.Collections.IEnumerator iter f val]           ;;; ^java.lang.Iterator
   (loop [ret val]
     (if (.MoveNext iter)                                 ;;; .hasNext
       (let [ret (f ret (.Current iter))]                 ;;; .next
         (if (reduced? ret)
           @ret
           (recur ret)))
       ret))))

(defn- iter-reduce
  ([^System.Collections.IEnumerable coll f]               ;;;  ^Iterable
   (iterator-reduce! (.GetEnumerator coll) f))            ;;; .iterator
  ([^System.Collections.IEnumerable coll f val]           ;;;  ^Iterable
   (iterator-reduce! (.GetEnumerator coll) f val)))       ;;; .iterator

(defn- naive-seq-reduce
  "Reduces a seq, ignoring any opportunities to switch to a more
  specialized implementation."
  [s f val]
  (loop [s (seq s)
         val val]
    (if s
      (let [ret (f val (first s))]
        (if (reduced? ret)
          @ret
          (recur (next s) ret)))
      val)))

(defn- interface-or-naive-reduce
  "Reduces via IReduceInit if possible, else naively."
  [coll f val]
  (if (instance? clojure.lang.IReduceInit coll)
    (.reduce ^clojure.lang.IReduceInit coll f val)
    (naive-seq-reduce coll f val)))

(extend-protocol CollReduce
  nil
  (coll-reduce
   ([coll f] (f))
   ([coll f val] val))

  Object
  (coll-reduce
   ([coll f] (seq-reduce coll f))
   ([coll f val] (seq-reduce coll f val)))

  clojure.lang.IReduceInit
  (coll-reduce
    ([coll f] (.reduce ^clojure.lang.IReduce coll f))
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
   ([coll f] (iter-reduce coll f))
   ([coll f val] (iter-reduce coll f val)))

  clojure.lang.APersistentMap+KeySeq                ;;; $KeySeq
  (coll-reduce
    ([coll f] (iter-reduce coll f))
    ([coll f val] (iter-reduce coll f val)))

  clojure.lang.APersistentMap+ValSeq                ;;; $ValSeq
  (coll-reduce
    ([coll f] (iter-reduce coll f))
    ([coll f val] (iter-reduce coll f val))))

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
       (interface-or-naive-reduce s f val))
	 val))

  clojure.lang.StringSeq
  (internal-reduce
   [str-seq f val]
   (let [s (.S str-seq)                              ;;; .s
         len (.Length s)]                            ;;; .length
     (loop [i (.I str-seq)                           ;;; .i
            val val]
       (if (< i len)                        
         (let [ret (f val (.get_Chars s i))]        ;;; .charAt
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
       (if (identical? (class s) cls)
         (let [ret (f val (first s))]
                (if (reduced? ret)
                  @ret
                  (recur cls (next s) f ret)))
         (interface-or-naive-reduce s f val))
       val))))
       
(defprotocol IKVReduce
  "Protocol for concrete associative types that can reduce themselves
   via a function of key and val faster than first/next recursion over map
   entries. Called by clojure.core/reduce-kv, and has same
   semantics (just different arg order)."
  (kv-reduce [amap f init])) 

(defprotocol Datafiable
  :extend-via-metadata true

  (datafy [o] "return a representation of o as data (default identity)"))

(extend-protocol Datafiable
  nil
  (datafy [_] nil)

  Object
  (datafy [x] x))

(defprotocol Navigable
  :extend-via-metadata true

  (nav [coll k v] "return (possibly transformed) v in the context of coll and k (a key/index or nil),
defaults to returning v."))

(extend-protocol Navigable
  Object
  (nav [_ _ x] x))