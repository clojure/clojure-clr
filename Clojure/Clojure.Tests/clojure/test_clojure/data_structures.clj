;   Copyright (c) Rich Hickey. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.

; Author: Frantisek Sodomka


(ns clojure.test-clojure.data-structures
  (:use clojure.test
        [clojure.test.generative :exclude (is)])
  (:require [clojure.test-clojure.generators :as cgen]
            [clojure.data.generators :as gen]
			[clojure.string :as string]))


;; *** Helper functions ***

(defn diff [s1 s2]
  (seq (reduce disj (set s1) (set s2))))


;; *** Generative ***
(defspec subcollection-counts-are-consistent
  identity
  [^{:tag cgen/ednable-collection} coll]
  (let [n (count coll)]
    (dotimes [i n]
      (is (= n
             (+ i (count (nthnext coll i)))
             (+ i (count (drop i coll))))))))

(defn- transient? [x]
  (instance? clojure.lang.ITransientCollection x))

(defn gen-transient-action []
  (gen/rand-nth [[#(conj! %1 %2) #(conj %1 %2) (gen/uniform -100 100)]
                 [#(disj! %1 %2) #(disj %1 %2) (gen/uniform -100 100)]
                 [persistent! identity]
                 [identity transient]]))

(defn gen-transient-actions []
  (gen/reps #(gen/uniform 0 100) gen-transient-action))

(defn assert-same-collection [a b]
  (assert (= (count a) (count b) (.count a) (.count b)))               ;;; .size  .size
  (assert (= a b))
  (assert (= b a))
  (assert (.Equals ^Object a b))                                       ;;; .equals
  (assert (.Equals ^Object b a))                                       ;;; .equals
  (assert (= (hash a) (hash b)))
  (assert (= (.GetHashCode ^Object a) (.GetHashCode ^Object b)))       ;;; .hashCode .hashCode
  (assert (= a
             (into (empty a) a)
             (into (empty b) b)
             (into (empty a) b)
             (into (empty b) a))))

(defn apply-actions [coll actions]
  (reduce (fn [c [tfunc pfunc & args]]
            (apply (if (transient? c) tfunc pfunc) c args))
          coll
          actions))

(defn to-persistent [c]
  (if (transient? c) (persistent! c) c))

(defspec conj-persistent-transient
  identity
  [^{:tag clojure.test-clojure.data-structures/gen-transient-actions} actions]
  (assert-same-collection
   (to-persistent (apply-actions #{} actions))
   (to-persistent (apply-actions #{} actions))))

 ;; *** General ***

(defstruct equality-struct :a :b)

(deftest test-equality
  ; nil is not equal to any other value
  (are [x] (not (= nil x))
      true false
      0 0.0
      \space
      "" #""
      () [] #{} {}
      (lazy-seq nil)  ; SVN 1292: fixed (= (lazy-seq nil) nil)
      (lazy-seq ())
      (lazy-seq [])
      (lazy-seq {})
      (lazy-seq #{})
      (lazy-seq "")
      (lazy-seq (into-array []))
      (new Object) )

  ; numbers equality across types (see tests below - NOT IMPLEMENTED YET)

  ; ratios
  (is (== 1/2 0.5))
  (is (== 1/1000 0.001))
  (is (not= 2/3 0.6666666666666666))

  ; vectors equal other seqs by items equality
  (are [x y] (= x y)
      '() []        ; regression fixed in r1208; was not equal
      '(1) [1]
      '(1 2) [1 2]

      [] '()        ; same again, but vectors first
      [1] '(1)
      [1 2] '(1 2) )
  (is (not= [1 2] '(2 1)))  ; order of items matters

  ; list and vector vs. set and map
  (are [x y] (not= x y)
      ; only () equals []
      () #{}
      () {}
      [] #{}
      [] {}
      #{} {}
      ; only '(1) equals [1]
      '(1) #{1}
      [1] #{1} )

  ; sorted-map, hash-map and array-map - classes differ, but content is equal
  
;; TODO: reimplement all-are with new do-template?  
;;   (all-are (not= (class _1) (class _2))
;;       (sorted-map :a 1)
;;       (hash-map   :a 1)
;;       (array-map  :a 1))
;;   (all-are (= _1 _2)
;;       (sorted-map)
;;       (hash-map)
;;       (array-map))
;;   (all-are (= _1 _2)
;;       (sorted-map :a 1)
;;       (hash-map   :a 1)
;;       (array-map  :a 1))
;;   (all-are (= _1 _2)
;;       (sorted-map :a 1 :z 3 :c 2)
;;       (hash-map   :a 1 :z 3 :c 2)
;;       (array-map  :a 1 :z 3 :c 2))

  ; struct-map vs. sorted-map, hash-map and array-map
  (are [x] (and (not= (class (struct equality-struct 1 2)) (class x))
                (= (struct equality-struct 1 2) x))
      (sorted-map-by compare :a 1 :b 2)
      (sorted-map :a 1 :b 2)
      (hash-map   :a 1 :b 2)
      (array-map  :a 1 :b 2))

  ; sorted-set vs. hash-set
  (is (not= (class (sorted-set 1)) (class (hash-set 1))))
  (are [x y] (= x y)
      (sorted-set-by <) (hash-set)
      (sorted-set-by < 1) (hash-set 1)
      (sorted-set-by < 3 2 1) (hash-set 3 2 1)
      (sorted-set) (hash-set)
      (sorted-set 1) (hash-set 1)
      (sorted-set 3 2 1) (hash-set 3 2 1) ))


;; *** Collections ***

(deftest test-count
  (let [EMPTY clojure.lang.PersistentQueue/EMPTY]
    (are [x y] (= (count x) y)
         EMPTY 0 
         (into EMPTY [:a :b]) 2
         (-> (into EMPTY [:a :b]) pop pop) 0
         
         nil 0

         () 0
         '(1) 1
         '(1 2 3) 3

         [] 0
         [1] 1
         [1 2 3] 3

         #{} 0
         #{1} 1
         #{1 2 3} 3

         {} 0
         {:a 1} 1
         {:a 1 :b 2 :c 3} 3

         "" 0
         "a" 1
         "abc" 3

         (into-array []) 0
         (into-array [1]) 1
         (into-array [1 2 3]) 3

         (System.Collections.ArrayList. []) 0                   ;;; java.util.ArrayList.
         (System.Collections.ArrayList. [1]) 1                  ;;; java.util.ArrayList.
         (System.Collections.ArrayList. [1 2 3]) 3              ;;; java.util.ArrayList.

         (System.Collections.Hashtable. {}) 0                     ;;; java.util.HashMap.
         (System.Collections.Hashtable. {:a 1}) 1                 ;;; java.util.HashMap.
         (System.Collections.Hashtable. {:a 1 :b 2 :c 3}) 3 ))    ;;; java.util.HashMap.

  ; different types
  (are [x]  (= (count [x]) 1)
      nil true false
      0 0.0 "" \space
      () [] #{} {}  ))


(deftest test-conj
  ; doesn't work on strings or arrays
  (is (thrown? InvalidCastException (conj "" \a)))                   ;;; ClassCastException
  (is (thrown? InvalidCastException (conj (into-array []) 1)))       ;;; ClassCastException

  (are [x y] (= x y)
      (conj nil 1) '(1)
      (conj nil 3 2 1) '(1 2 3)

      (conj nil nil) '(nil)
      (conj nil nil nil) '(nil nil)
      (conj nil nil nil 1) '(1 nil nil)

      ; list -> conj puts the item at the front of the list
      (conj () 1) '(1)
      (conj () 1 2) '(2 1)

      (conj '(2 3) 1) '(1 2 3)
      (conj '(2 3) 1 4 3) '(3 4 1 2 3)

      (conj () nil) '(nil)
      (conj () ()) '(())

      ; vector -> conj puts the item at the end of the vector
      (conj [] 1) [1]
      (conj [] 1 2) [1 2]

      (conj [2 3] 1) [2 3 1]
      (conj [2 3] 1 4 3) [2 3 1 4 3]

      (conj [] nil) [nil]
      (conj [] []) [[]]

      ; map -> conj expects another (possibly single entry) map as the item,
      ;   and returns a new map which is the old map plus the entries
      ;   from the new, which may overwrite entries of the old.
      ;   conj also accepts a MapEntry or a vector of two items (key and value).
      (conj {} {}) {}
      (conj {} {:a 1}) {:a 1}
      (conj {} {:a 1 :b 2}) {:a 1 :b 2}
      (conj {} {:a 1 :b 2} {:c 3}) {:a 1 :b 2 :c 3}
      (conj {} {:a 1 :b 2} {:a 3 :c 4}) {:a 3 :b 2 :c 4}

      (conj {:a 1} {:a 7}) {:a 7}
      (conj {:a 1} {:b 2}) {:a 1 :b 2}
      (conj {:a 1} {:a 7 :b 2}) {:a 7 :b 2}
      (conj {:a 1} {:a 7 :b 2} {:c 3}) {:a 7 :b 2 :c 3}
      (conj {:a 1} {:a 7 :b 2} {:b 4 :c 5}) {:a 7 :b 4 :c 5}

      (conj {} (first {:a 1})) {:a 1}           ; MapEntry
      (conj {:a 1} (first {:b 2})) {:a 1 :b 2}
      (conj {:a 1} (first {:a 7})) {:a 7}
      (conj {:a 1} (first {:b 2}) (first {:a 5})) {:a 5 :b 2}

      (conj {} [:a 1]) {:a 1}                   ; vector
      (conj {:a 1} [:b 2]) {:a 1 :b 2}
      (conj {:a 1} [:a 7]) {:a 7}
      (conj {:a 1} [:b 2] [:a 5]) {:a 5 :b 2}

      (conj {} {nil {}}) {nil {}}
      (conj {} {{} nil}) {{} nil}
      (conj {} {{} {}}) {{} {}}

      ; set
      (conj #{} 1) #{1}
      (conj #{} 1 2 3) #{1 2 3}

      (conj #{2 3} 1) #{3 1 2}
      (conj #{3 2} 1) #{1 2 3}

      (conj #{2 3} 2) #{2 3}
      (conj #{2 3} 2 3) #{2 3}
      (conj #{2 3} 4 1 2 3) #{1 2 3 4}

      (conj #{} nil) #{nil}
      (conj #{} #{}) #{#{}} ))


;; *** Lists and Vectors ***

(deftest test-peek
  ; doesn't work for sets and maps
  (is (thrown? InvalidCastException (peek #{1})))            ;;; ClassCastException
  (is (thrown? InvalidCastException (peek {:a 1})))          ;;; ClassCastException

  (are [x y] (= x y)
      (peek nil) nil

      ; list = first
      (peek ()) nil
      (peek '(1)) 1
      (peek '(1 2 3)) 1

      (peek '(nil)) nil     ; special cases
      (peek '(1 nil)) 1
      (peek '(nil 2)) nil
      (peek '(())) ()
      (peek '(() nil)) ()
      (peek '(() 2 nil)) ()

      ; vector = last
      (peek []) nil
      (peek [1]) 1
      (peek [1 2 3]) 3

      (peek [nil]) nil      ; special cases
      (peek [1 nil]) nil
      (peek [nil 2]) 2
      (peek [[]]) []
      (peek [[] nil]) nil
      (peek [[] 2 nil]) nil ))


(deftest test-pop
  ; doesn't work for sets and maps
  (is (thrown? InvalidCastException (pop #{1})))            ;;; ClassCastException
  (is (thrown? InvalidCastException (pop #{:a 1})))         ;;; ClassCastException

  ; collection cannot be empty
  (is (thrown? InvalidOperationException (pop ())))             ;;; IllegalStateException
  (is (thrown? InvalidOperationException (pop [])))             ;;; IllegalStateException

  (are [x y] (= x y)
      (pop nil) nil

      ; list - pop first
      (pop '(1)) ()
      (pop '(1 2 3)) '(2 3)

      (pop '(nil)) ()
      (pop '(1 nil)) '(nil)
      (pop '(nil 2)) '(2)
      (pop '(())) ()
      (pop '(() nil)) '(nil)
      (pop '(() 2 nil)) '(2 nil)

      ; vector - pop last
      (pop [1]) []
      (pop [1 2 3]) [1 2]

      (pop [nil]) []
      (pop [1 nil]) [1]
      (pop [nil 2]) [nil]
      (pop [[]]) []
      (pop [[] nil]) [[]]
      (pop [[] 2 nil]) [[] 2] ))


;; *** Lists (IPersistentList) ***

(deftest test-list
  (are [x]  (list? x)
      ()
      '()
      (list)
      (list 1 2 3) )

  ; order is important
  (are [x y] (not (= x y))
      (list 1 2) (list 2 1)
      (list 3 1 2) (list 1 2 3) )

  (are [x y] (= x y)
      '() ()
      (list) '()
      (list 1) '(1)
      (list 1 2) '(1 2)

      ; nesting
      (list 1 (list 2 3) (list 3 (list 4 5 (list 6 (list 7)))))
        '(1 (2 3) (3 (4 5 (6 (7)))))

      ; different data structures
      (list true false nil)
        '(true false nil)
      (list 1 2.5 2/3 "ab" \x 'cd :kw)
        '(1 2.5 2/3 "ab" \x cd :kw)
      (list (list 1 2) [3 4] {:a 1 :b 2} #{:c :d})
        '((1 2) [3 4] {:a 1 :b 2} #{:c :d})

      ; evaluation
      (list (+ 1 2) [(+ 2 3) 'a] (list (* 2 3) 8))
        '(3 [5 a] (6 8))

      ; special cases
      (list nil) '(nil)
      (list 1 nil) '(1 nil)
      (list nil 2) '(nil 2)
      (list ()) '(())
      (list 1 ()) '(1 ())
      (list () 2) '(() 2) ))


;; *** Maps (IPersistentMap) ***

(deftest test-find
  (are [x y] (= x y)
      (find {} :a) nil

      (find {:a 1} :a) [:a 1]
      (find {:a 1} :b) nil

      (find {:a 1 :b 2} :a) [:a 1]
      (find {:a 1 :b 2} :b) [:b 2]
      (find {:a 1 :b 2} :c) nil

      (find {} nil) nil
      (find {:a 1} nil) nil
      (find {:a 1 :b 2} nil) nil ))


(deftest test-contains?
  ; contains? is designed to work preferably on maps and sets
  (are [x y] (= x y)
      (contains? {} :a) false
      (contains? {} nil) false

      (contains? {:a 1} :a) true
      (contains? {:a 1} :b) false
      (contains? {:a 1} nil) false

      (contains? {:a 1 :b 2} :a) true
      (contains? {:a 1 :b 2} :b) true
      (contains? {:a 1 :b 2} :c) false
      (contains? {:a 1 :b 2} nil) false

      ; sets
      (contains? #{} 1) false
      (contains? #{} nil) false

      (contains? #{1} 1) true
      (contains? #{1} 2) false
      (contains? #{1} nil) false

      (contains? #{1 2 3} 1) true
      (contains? #{1 2 3} 3) true
      (contains? #{1 2 3} 10) false
      (contains? #{1 2 3} nil) false)

  ; contains? also works on java.util.Map and java.util.Set.
  (are [x y] (= x y)
      (contains? (System.Collections.Hashtable. {}) :a) false                    ;;; java.util.HashMap.
      (contains? (System.Collections.Hashtable. {}) nil) false                   ;;; java.util.HashMap.

      (contains? (System.Collections.Hashtable. {:a 1}) :a) true                 ;;; java.util.HashMap.
      (contains? (System.Collections.Hashtable. {:a 1}) :b) false                ;;; java.util.HashMap.
      (contains? (System.Collections.Hashtable. {:a 1}) nil) false               ;;; java.util.HashMap.

      (contains? (System.Collections.Hashtable. {:a 1 :b 2}) :a) true            ;;; java.util.HashMap.
      (contains? (System.Collections.Hashtable. {:a 1 :b 2}) :b) true            ;;; java.util.HashMap.
      (contains? (System.Collections.Hashtable. {:a 1 :b 2}) :c) false           ;;; java.util.HashMap.
      (contains? (System.Collections.Hashtable. {:a 1 :b 2}) nil) false          ;;; java.util.HashMap.

      ; sets
      (contains? (|System.Collections.Generic.HashSet`1[System.Object]|. #{}) 1) false                        ;;; java.util.HashSet.
      (contains? (|System.Collections.Generic.HashSet`1[System.Object]|. #{}) nil) false                      ;;; java.util.HashSet.

      (contains? (|System.Collections.Generic.HashSet`1[System.Object]|. #{1}) 1) true                        ;;; java.util.HashSet.
      (contains? (|System.Collections.Generic.HashSet`1[System.Object]|. #{1}) 2) false                       ;;; java.util.HashSet.
      (contains? (|System.Collections.Generic.HashSet`1[System.Object]|. #{1}) nil) false                     ;;; java.util.HashSet.

      (contains? (|System.Collections.Generic.HashSet`1[System.Object]|. #{1 2 3}) 1) true                    ;;; java.util.HashSet.
      (contains? (|System.Collections.Generic.HashSet`1[System.Object]|. #{1 2 3}) 3) true                    ;;; java.util.HashSet.
      (contains? (|System.Collections.Generic.HashSet`1[System.Object]|. #{1 2 3}) 10) false                  ;;; java.util.HashSet.
      (contains? (|System.Collections.Generic.HashSet`1[System.Object]|. #{1 2 3}) nil) false)                ;;; java.util.HashSet.

  ; numerically indexed collections (e.g. vectors and Java arrays)
  ; => test if the numeric key is WITHIN THE RANGE OF INDEXES
  (are [x y] (= x y)
      (contains? [] 0) false
      (contains? [] -1) false
      (contains? [] 1) false

      (contains? [1] 0) true
      (contains? [1] -1) false
      (contains? [1] 1) false

      (contains? [1 2 3] 0) true
      (contains? [1 2 3] 2) true
      (contains? [1 2 3] 3) false
      (contains? [1 2 3] -1) false

      ; arrays
      (contains? (into-array []) 0) false
      (contains? (into-array []) -1) false
      (contains? (into-array []) 1) false

      (contains? (into-array [1]) 0) true
      (contains? (into-array [1]) -1) false
      (contains? (into-array [1]) 1) false

      (contains? (into-array [1 2 3]) 0) true
      (contains? (into-array [1 2 3]) 2) true
      (contains? (into-array [1 2 3]) 3) false
      (contains? (into-array [1 2 3]) -1) false)

  ; 'contains?' will not operate on non-associative things
  (are [x]  (is (thrown? Exception (contains? x 1)))
       '(1 2 3)
       3))


(deftest test-keys
  (are [x y] (= x y)      ; other than map data structures
      (keys ()) nil
      (keys []) nil
      (keys #{}) nil
      (keys "") nil )

  (are [x y] (= x y)
      ; (class {:a 1}) => clojure.lang.PersistentArrayMap
      (keys {}) nil
      (keys {:a 1}) '(:a)
      (diff (keys {:a 1 :b 2}) '(:a :b)) nil              ; (keys {:a 1 :b 2}) '(:a :b)

      ; (class (sorted-map :a 1)) => clojure.lang.PersistentTreeMap
      (keys (sorted-map)) nil
      (keys (sorted-map :a 1)) '(:a)
      (diff (keys (sorted-map :a 1 :b 2)) '(:a :b)) nil   ; (keys (sorted-map :a 1 :b 2)) '(:a :b)

      ; (class (hash-map :a 1)) => clojure.lang.PersistentHashMap
      (keys (hash-map)) nil
      (keys (hash-map :a 1)) '(:a)
      (diff (keys (hash-map :a 1 :b 2)) '(:a :b)) nil ))  ; (keys (hash-map :a 1 :b 2)) '(:a :b)


(deftest test-vals
  (are [x y] (= x y)      ; other than map data structures
      (vals ()) nil
      (vals []) nil
      (vals #{}) nil
      (vals "") nil )

  (are [x y] (= x y)
      ; (class {:a 1}) => clojure.lang.PersistentArrayMap
      (vals {}) nil
      (vals {:a 1}) '(1)
      (diff (vals {:a 1 :b 2}) '(1 2)) nil              ; (vals {:a 1 :b 2}) '(1 2)

      ; (class (sorted-map :a 1)) => clojure.lang.PersistentTreeMap
      (vals (sorted-map)) nil
      (vals (sorted-map :a 1)) '(1)
      (diff (vals (sorted-map :a 1 :b 2)) '(1 2)) nil   ; (vals (sorted-map :a 1 :b 2)) '(1 2)

      ; (class (hash-map :a 1)) => clojure.lang.PersistentHashMap
      (vals (hash-map)) nil
      (vals (hash-map :a 1)) '(1)
      (diff (vals (hash-map :a 1 :b 2)) '(1 2)) nil ))  ; (vals (hash-map :a 1 :b 2)) '(1 2)


(deftest test-key
  (are [x]  (= (key (first (hash-map x :value))) x)
      nil
      false true
      0 42
      0.0 3.14
      2/3
      0M 1M
      \c
      "" "abc"
      'sym
      :kw
      () '(1 2)
      [] [1 2]
      {} {:a 1 :b 2}
      #{} #{1 2} ))


(deftest test-val
  (are [x]  (= (val (first (hash-map :key x))) x)
      nil
      false true
      0 42
      0.0 3.14
      2/3
      0M 1M
      \c
      "" "abc"
      'sym
      :kw
      () '(1 2)
      [] [1 2]
      {} {:a 1 :b 2}
      #{} #{1 2} ))

(deftest test-get
  (let [m {:a 1, :b 2, :c {:d 3, :e 4}, :f nil, :g false, nil {:h 5}}]
    (is (thrown? Exception (get-in {:a 1} 5)))                        ;;; Throwable
    (are [x y] (= x y)
         (get m :a) 1
         (get m :e) nil
         (get m :e 0) 0
         (get m :b 0) 2
         (get m :f 0) nil

         (get-in m [:c :e]) 4
         (get-in m '(:c :e)) 4
         (get-in m [:c :x]) nil
         (get-in m [:f]) nil
         (get-in m [:g]) false
         (get-in m [:h]) nil
         (get-in m []) m
         (get-in m nil) m

         (get-in m [:c :e] 0) 4
         (get-in m '(:c :e) 0) 4
         (get-in m [:c :x] 0) 0
         (get-in m [:b] 0) 2
         (get-in m [:f] 0) nil
         (get-in m [:g] 0) false
         (get-in m [:h] 0) 0
         (get-in m [:x :y] {:y 1}) {:y 1}
         (get-in m [] 0) m
         (get-in m nil 0) m)))

(deftest test-nested-map-destructuring
  (let [sample-map {:a 1 :b {:a 2}}
        {ao1 :a {ai1 :a} :b} sample-map
        {ao2 :a {ai2 :a :as m1} :b :as m2} sample-map
        {ao3 :a {ai3 :a :as m} :b :as m} sample-map
        {{ai4 :a :as m} :b ao4 :a :as m} sample-map]
    (are [i o] (and (= i 2)
                    (= o 1))
         ai1 ao1
         ai2 ao2
         ai3 ao3
         ai4 ao4)))

;; *** Sets ***

(deftest test-hash-set
  (are [x] (set? x)
      #{}
      #{1 2}
      (hash-set)
      (hash-set 1 2) )

  ; order isn't important
  (are [x y] (= x y)
      #{1 2} #{2 1}
      #{3 1 2} #{1 2 3}
      (hash-set 1 2) (hash-set 2 1)
      (hash-set 3 1 2) (hash-set 1 2 3) )


  (are [x y] (= x y)
      ; equal classes
      (class #{}) (class (hash-set))
      (class #{1 2}) (class (hash-set 1 2))

      ; creating
      (hash-set) #{}
      (hash-set 1) #{1}
      (hash-set 1 2) #{1 2}

      ; nesting
      (hash-set 1 (hash-set 2 3) (hash-set 3 (hash-set 4 5 (hash-set 6 (hash-set 7)))))
        #{1 #{2 3} #{3 #{4 5 #{6 #{7}}}}}

      ; different data structures
      (hash-set true false nil)
        #{true false nil}
      (hash-set 1 2.5 2/3 "ab" \x 'cd :kw)
        #{1 2.5 2/3 "ab" \x 'cd :kw}
      (hash-set (list 1 2) [3 4] {:a 1 :b 2} #{:c :d})
        #{'(1 2) [3 4] {:a 1 :b 2} #{:c :d}}

      ; evaluation
      (hash-set (+ 1 2) [(+ 2 3) :a] (hash-set (* 2 3) 8))
        #{3 [5 :a] #{6 8}}

      ; special cases
      (hash-set nil) #{nil}
      (hash-set 1 nil) #{1 nil}
      (hash-set nil 2) #{nil 2}
      (hash-set #{}) #{#{}}
      (hash-set 1 #{}) #{1 #{}}
      (hash-set #{} 2) #{#{} 2}
	  #_(hash-set (Integer. -1)) #_(hash-set (Long. -1))))


(deftest test-sorted-set
  ; only compatible types can be used
  (is (thrown? ArgumentException (sorted-set 1 "a")))                 ;;; ClassCastException
  (is (thrown? InvalidCastException (sorted-set '(1 2) [3 4])))       ;;; ClassCastException

  ; creates set?
  (are [x] (set? x)
       (sorted-set)
       (sorted-set 1 2) )

  ; equal and unique
  (are [x] (and (= (sorted-set x) #{x})
                (= (sorted-set x x) (sorted-set x)))
      nil
      false true
      0 42
      0.0 3.14
      2/3
      0M 1M
      \c
      "" "abc"
      'sym
      :kw
      ()  ; '(1 2)
      [] [1 2]
      {}  ; {:a 1 :b 2}
      #{} ; #{1 2}
  )
  ; cannot be cast to java.lang.Comparable
  (is (thrown? InvalidCastException (sorted-set '(1 2) '(1 2))))                  ;;; ClassCastException
  (is (thrown? InvalidCastException (sorted-set {:a 1 :b 2} {:a 1 :b 2})))        ;;; ClassCastException
  (is (thrown? InvalidCastException (sorted-set #{1 2} #{1 2})))                  ;;; ClassCastException

  (are [x y] (= x y)
      ; generating
      (sorted-set) #{}
      (sorted-set 1) #{1}
      (sorted-set 1 2) #{1 2}

      ; sorting
      (seq (sorted-set 5 4 3 2 1)) '(1 2 3 4 5)

      ; special cases
      (sorted-set nil) #{nil}
      (sorted-set 1 nil) #{nil 1}
      (sorted-set nil 2) #{nil 2}
      (sorted-set #{}) #{#{}} ))


(deftest test-sorted-set-by
  ; only compatible types can be used
  ; NB: not a ClassCastException, but a RuntimeException is thrown,
  ; requires discussion on whether this should be symmetric with test-sorted-set
  (is (thrown? Exception (sorted-set-by < 1 "a")))
  (is (thrown? Exception (sorted-set-by < '(1 2) [3 4])))

  ; creates set?
  (are [x] (set? x)
       (sorted-set-by <)
       (sorted-set-by < 1 2) )

  ; equal and unique
  (are [x] (and (= (sorted-set-by compare x) #{x})
                (= (sorted-set-by compare x x) (sorted-set-by compare x)))
      nil
      false true
      0 42
      0.0 3.14
      2/3
      0M 1M
      \c
      "" "abc"
      'sym
      :kw
      ()  ; '(1 2)
      [] [1 2]
      {}  ; {:a 1 :b 2}
      #{} ; #{1 2}
  )
  ; cannot be cast to java.lang.Comparable
  ; NB: not a ClassCastException, but a RuntimeException is thrown,
  ; requires discussion on whether this should be symmetric with test-sorted-set
  (is (thrown? Exception (sorted-set-by compare '(1 2) '(1 2))))
  (is (thrown? Exception (sorted-set-by compare {:a 1 :b 2} {:a 1 :b 2})))
  (is (thrown? Exception (sorted-set-by compare #{1 2} #{1 2})))

  (are [x y] (= x y)
      ; generating
      (sorted-set-by >) #{}
      (sorted-set-by > 1) #{1}
      (sorted-set-by > 1 2) #{1 2}

      ; sorting
      (seq (sorted-set-by < 5 4 3 2 1)) '(1 2 3 4 5)

      ; special cases
      (sorted-set-by compare nil) #{nil}
      (sorted-set-by compare 1 nil) #{nil 1}
      (sorted-set-by compare nil 2) #{nil 2}
      (sorted-set-by compare #{}) #{#{}} ))


(deftest test-set
  ; set?
  (are [x] (set? (set x))
      () '(1 2)
      [] [1 2]
      #{} #{1 2}
      {} {:a 1 :b 2}
      (into-array []) (into-array [1 2])
      "" "abc" )

  ; unique
  (are [x] (= (set [x x]) #{x})
      nil
      false true
      0 42
      0.0 3.14
      2/3
      0M 1M
      \c
      "" "abc"
      'sym
      :kw
      () '(1 2)
      [] [1 2]
      {} {:a 1 :b 2}
      #{} #{1 2} )

  ; conversion
  (are [x y] (= (set x) y)
      () #{}
      '(1 2) #{1 2}

      [] #{}
      [1 2] #{1 2}

      #{} #{}         ; identity
      #{1 2} #{1 2}   ; identity

      {} #{}
      {:a 1 :b 2} #{[:a 1] [:b 2]}

      (into-array []) #{}
      (into-array [1 2]) #{1 2}

      "" #{}
      "abc" #{\a \b \c} ))


(deftest test-disj
  ; doesn't work on lists, vectors or maps
  (is (thrown? InvalidCastException (disj '(1 2) 1)))                ;;; ClassCastException
  (is (thrown? InvalidCastException (disj [1 2] 1)))                 ;;; ClassCastException
  (is (thrown? InvalidCastException (disj {:a 1} :a)))               ;;; ClassCastException

  ; identity
  (are [x] (= (disj x) x)
      nil
      #{}
      #{1 2 3}
      ; different data types
      #{nil
        false true
        0 42
        0.0 3.14
        2/3
        0M 1M
        \c
        "" "abc"
        'sym
        :kw
        [] [1 2]
        {} {:a 1 :b 2}
        #{} #{1 2}} )

  ; type identity
  (are [x] (= (class (disj x)) (class x))
      (hash-set)
      (hash-set 1 2)
      (sorted-set)
      (sorted-set 1 2) )

  (are [x y] (= x y)
      (disj nil :a) nil
      (disj nil :a :b) nil

      (disj #{} :a) #{}
      (disj #{} :a :b) #{}

      (disj #{:a} :a) #{}
      (disj #{:a} :a :b) #{}
      (disj #{:a} :c) #{:a}

      (disj #{:a :b :c :d} :a) #{:b :c :d}
      (disj #{:a :b :c :d} :a :d) #{:b :c}
      (disj #{:a :b :c :d} :a :b :c) #{:d}
      (disj #{:a :b :c :d} :d :a :c :b) #{}

      (disj #{nil} :a) #{nil}
      (disj #{nil} #{}) #{nil}
      (disj #{nil} nil) #{}

      (disj #{#{}} nil) #{#{}}
      (disj #{#{}} #{}) #{}
      (disj #{#{nil}} #{nil}) #{} ))


;; *** Queues ***

(deftest test-queues
  (let [EMPTY clojure.lang.PersistentQueue/EMPTY]
    (are [x y] (= x y)
      EMPTY EMPTY
      (into EMPTY (range 50)) (into EMPTY (range 50))
      ;;;(conj EMPTY (Long. -1)) (conj EMPTY (Integer. -1))
      ;;;(hash (conj EMPTY (Long. -1))) (hash (conj EMPTY (Integer. -1)))
      (hash [1 2 3]) (hash (conj EMPTY 1 2 3))
      (range 5) (into EMPTY (range 5))
      (range 1 6) (-> EMPTY
                    (into (range 6))
                    pop))
    (are [x y] (not= x y)
      (range 5) (into EMPTY (range 6))
      (range 6) (into EMPTY (range 5))
      (range 0 6) (-> EMPTY
                    (into (range 6))
                    pop)
      (range 1 6) (-> EMPTY
                    (into (range 7))
                    pop))))
       
	   
(deftest test-duplicates
  (let [equal-sets-incl-meta (fn [s1 s2]
                               (and (= s1 s2)
                                    (let [ss1 (sort s1)
                                          ss2 (sort s2)]
                                      (every? identity
                                              (map #(and (= %1 %2)
                                                         (= (meta %1) (meta %2)))
                                                   ss1 ss2)))))
        all-equal-sets-incl-meta (fn [& ss]
                                   (every? (fn [[s1 s2]]
                                             (equal-sets-incl-meta s1 s2))
                                           (partition 2 1 ss)))
        equal-maps-incl-meta (fn [m1 m2]
                               (and (= m1 m2)
                                    (equal-sets-incl-meta (set (keys m1))
                                                          (set (keys m2)))
                                    (every? #(= (meta (m1 %)) (meta (m2 %)))
                                            (keys m1))))
        all-equal-maps-incl-meta (fn [& ms]
                                   (every? (fn [[m1 m2]]
                                             (equal-maps-incl-meta m1 m2))
                                           (partition 2 1 ms)))
        cmp-first #(> (first %1) (first %2))
        x1 (with-meta [1] {:me "x"})
        y2 (with-meta [2] {:me "y"})
        z3a (with-meta [3] {:me "z3a"})
        z3b (with-meta [3] {:me "z3b"})
        v4a (with-meta [4] {:me "v4a"})
        v4b (with-meta [4] {:me "v4b"})
        v4c (with-meta [4] {:me "v4c"})
        w5a (with-meta [5] {:me "w5a"})
        w5b (with-meta [5] {:me "w5b"})
        w5c (with-meta [5] {:me "w5c"})]

    ;; Sets
    (is (thrown? ArgumentException                                          ;;; IllegalArgumentException
                 (read-string "#{1 2 3 4 1 5}")))
    ;; If there are duplicate items when doing (conj #{} x1 x2 ...),
    ;; the behavior is that the metadata of the first item is kept.
    (are [s x] (all-equal-sets-incl-meta s
                                         (apply conj #{} x)
                                         (set x)
                                         (apply hash-set x)
                                         (apply sorted-set x)
                                         (apply sorted-set-by cmp-first x))
      #{x1 y2} [x1 y2]
      #{x1 z3a} [x1 z3a z3b]
      #{w5b}    [w5b w5a w5c]
      #{z3a x1} [z3a z3b x1])

    ;; Maps
    (is (thrown? ArgumentException                                          ;;; IllegalArgumentException
                 (read-string "{:a 1, :b 2, :a -1, :c 3}")))
    ;; If there are duplicate keys when doing (assoc {} k1 v1 k2 v2
    ;; ...), the behavior is that the metadata of the first duplicate
    ;; key is kept, but mapped to the last value with an equal key
    ;; (where metadata of keys are not compared).
    (are [h x] (all-equal-maps-incl-meta h
                                         (apply assoc {} x)
                                         (apply hash-map x)
                                         (apply sorted-map x)
                                         (apply sorted-map-by cmp-first x)
                                         (apply array-map x))
      {x1 2, z3a 4} [x1 2, z3a 4]
      {x1 2, z3a 5} [x1 2, z3a 4, z3b 5]
      {z3a 5}       [z3a 2, z3a 4, z3b 5]
      {z3b 4, x1 5} [z3b 2, z3a 4, x1 5]
      {z3b v4b, x1 5} [z3b v4a, z3a v4b, x1 5]
      {x1 v4a, w5a v4c, v4a z3b, y2 2} [x1 v4a, w5a v4a, w5b v4b,
                                        v4a z3a, y2 2, v4b z3b, w5c v4c])))
										

(deftest test-assoc
  (are [x y] (= x y)
       [4] (assoc [] 0 4)
       [5 -7] (assoc [] 0 5 1 -7)
       {:a 1} (assoc {} :a 1)
       {:a 2 :b -2} (assoc {} :b -2 :a 2))
  (is (thrown? ArgumentException (assoc [] 0 5 1)))              ;;; IllegalArgumentException
  (is (thrown? ArgumentException (assoc {} :b -2 :a))))			;;; IllegalArgumentException	
  
  (defn is-same-collection [a b]
  (let [msg (format "(class a)=%s (class b)=%s a=%s b=%s"
                    (.Name (class a)) (.Name (class b)) a b)]               ;;; .getName .getName
    (is (= (count a) (count b) (.get_Count a) (.get_Count b)) msg)          ;;; .size .size
    (is (= a b) msg)
    (is (= b a) msg)
    (is (.Equals ^Object a b) msg)                                          ;;; .equals
    (is (.Equals ^Object b a) msg)                                          ;;; .equals
    (is (= (hash a) (hash b)) msg)
    (is (= (.GetHashCode ^Object a) (.GetHashCode ^Object b)) msg)))        ;;; .hashCode .hashCode

(deftest ordered-collection-equality-test
  (let [empty-colls [ []
                      '()
                      (lazy-seq)
                      clojure.lang.PersistentQueue/EMPTY
                      (vector-of :long) ]]
    (doseq [c1 empty-colls, c2 empty-colls]
      (is-same-collection c1 c2)))
  (let [colls1 [ [-3 :a "7th"]
                 '(-3 :a "7th")
                 (lazy-seq (cons -3
                   (lazy-seq (cons :a
                     (lazy-seq (cons "7th" nil))))))
                 (into clojure.lang.PersistentQueue/EMPTY
                       [-3 :a "7th"]) ]]
    (doseq [c1 colls1, c2 colls1]
      (is-same-collection c1 c2)))
  (is-same-collection [-3 1 7] (vector-of :long -3 1 7)))

(defn case-indendent-string-cmp [s1 s2]
  (compare (string/lower-case s1) (string/lower-case s2)))

(deftest set-equality-test
  (let [empty-sets [ #{}
                     (hash-set)
                     (sorted-set)
                     (sorted-set-by case-indendent-string-cmp) ]]
    (doseq [s1 empty-sets, s2 empty-sets]
      (is-same-collection s1 s2)))
  (let [sets1 [ #{"Banana" "apple" "7th"}
                (hash-set "Banana" "apple" "7th")
                (sorted-set "Banana" "apple" "7th")
                (sorted-set-by case-indendent-string-cmp "Banana" "apple" "7th") ]]
    (doseq [s1 sets1, s2 sets1]
      (is-same-collection s1 s2))))

(deftest map-equality-test
  (let [empty-maps [ {}
                     (hash-map)
                     (array-map)
                     (sorted-map)
                     (sorted-map-by case-indendent-string-cmp) ]]
    (doseq [m1 empty-maps, m2 empty-maps]
      (is-same-collection m1 m2)))
  (let [maps1 [ {"Banana" "like", "apple" "love", "7th" "indifferent"}
                (hash-map "Banana" "like", "apple" "love", "7th" "indifferent")
                (array-map "Banana" "like", "apple" "love", "7th" "indifferent")
                (sorted-map "Banana" "like", "apple" "love", "7th" "indifferent")
                (sorted-map-by case-indendent-string-cmp
                               "Banana" "like", "apple" "love", "7th" "indifferent") ]]
    (doseq [m1 maps1, m2 maps1]
      (is-same-collection m1 m2))))	  
	  
;; *** Collection hashes ***
;; See: http://clojure.org/data_structures#hash

(defn hash-ordered [collection]
  (-> (reduce (fn [acc e] (unchecked-add-int (unchecked-multiply-int 31 acc) (hash e)))
              1
              collection)
      (mix-collection-hash (count collection))))

(defn hash-unordered [collection]
  (-> (reduce unchecked-add-int 0 (map hash collection))
      (mix-collection-hash (count collection))))

(defn gen-elements
  []
  (gen/vec gen/anything))

(defspec ordered-collection-hashes-match
  identity
  [^{:tag clojure.test-clojure.data-structures/gen-elements} elem]
  (let [v (vec elem)
        l (apply list elem)]
    (is (= (hash v)
           (hash l)
           (hash (map identity elem))
           (hash-ordered elem)))))

(defspec unordered-set-hashes-match
  identity
  [^{:tag clojure.test-clojure.data-structures/gen-elements} elem]
  (let [unique-elem (distinct elem)
        s (into #{} unique-elem)]
    (is (= (hash s)
           (hash-unordered unique-elem)))))	              