;   Copyright (c) Rich Hickey. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.

#_(ns clojure.test-clojure.streams
  (:use clojure.test)
  (:import [java.util.stream Stream LongStream]
           [java.util.function Consumer Predicate Supplier]))

#_(deftest test-stream-reduce!
  (is (= :only-val (stream-reduce! + (.stream [:only-val]))))
  (is (= 0 (stream-reduce! + (.stream []))))
  (is (= 5 (stream-reduce! + (.stream [1 4]))))
  (is (= 6 (stream-reduce! + (.stream [1 2 3]))))
  (is (= [2 3 4]
         (stream-reduce! (fn [v i] (conj v (inc i)))
                         []
                         (.stream [1 2 3]))))

  (is (= 15 (stream-reduce! + (LongStream/rangeClosed 1 5))))

  (is (= 9 (stream-reduce! + (-> (Stream/of (to-array [1 2 3 4 5]))
                                 (.filter (reify Predicate (test [_ v] (odd? v))))))))
  (is (= {:a 1}
           (meta
            (stream-reduce! (fn [v i] (conj v (inc i)))
                            (with-meta [] {:a 1})
                            (.stream [1 2 3]))))))

#_(deftest test-stream-reduced!
  (is (= 5 (stream-reduce!
            (fn [x y] (reduced (+ x y)))
            (.stream [1 4]))))

    (is (= 45 (stream-reduce!
               (fn [acc v]
                 (if (= v 10)
                   (reduced acc)
                   (+ acc v)))
               (LongStream/rangeClosed 1 10)))))

#_(deftest stream-seq!-test
  (let [none (.stream [])
        one  (Stream/of "a")
        n    (.stream ["a" "b" "c"])
	inf  (Stream/generate (reify Supplier (get [_] 42)))
        st   (stream-seq! one)
        l100 (LongStream/range 0 100)]
    (is (empty? (map identity (stream-seq! none))))
    (is (seq? st))
    (is (= ["a"] (map identity st)))
    (is (= ["a" "b" "c"] (map identity (stream-seq! n))))
    (is (= [42 42 42 42 42] (take 5 (stream-seq! inf))))
    (is (= 4950 (reduce + (stream-seq! l100)))) 4950))

#_(deftest stream-transduce!-test
  (let [xf (comp (filter odd?) (take 5))]
    (let [st (Stream/of (to-array [1 2 3 4 5 6 7 8 9]))]
      (is (= [1 3 5 7 9] (stream-transduce! xf conj st))))

    (let [inf  (Stream/generate (reify Supplier (get [_] 0)))]
      (is (empty? (stream-transduce! xf conj (.limit inf 50)))))

    (let [inf  (Stream/generate (reify Supplier (get [_] 43)))]
      (is (= [43 43 43 43 43] (stream-transduce! xf conj (.limit inf 50)))))

    (let [inf  (Stream/generate (reify Supplier (get [_] 43)))]
      (is (= 215 (stream-transduce! xf + (.limit inf 50)))))

    (let [inf  (Stream/generate (reify Supplier (get [_] 43)))]
      (is (= 315 (stream-transduce! xf + 100 (.limit inf 50)))))

    (let [inf  (Stream/generate (reify Supplier (get [_] 43)))]
      (is (= "4343434343" (stream-transduce! xf str (.limit inf 50)))))))

#_(deftest stream-into!-test
  (let [none (.stream [])
        one  (Stream/of "a")
        n    (.stream ["a" "b" "c"])
        inf  (Stream/generate (reify Supplier (get [_] 42)))
        par  (-> (LongStream/rangeClosed 1 10) .boxed .parallel)
        xf   (comp (map #(+ 2 %)) (filter odd?))
        par2 (-> (LongStream/rangeClosed 1 10) .boxed .parallel)]
    (is (empty? (stream-into! [] none)))
    (is (= ["a"] (stream-into! [] one)))
    (is (= ["a" "b" "c"] (stream-into! [] n)))
    (is (= [42 42 42 42 42]
           (stream-into! [] (.limit inf 5))))
    (is (= [1 2 3 4 5 6 7 8 9 10]
           (stream-into! [] par)))
    (is (= {:a 1}
           (meta
            (stream-into! (with-meta [] {:a 1})
                          (.stream [1 2 3])))))
    (is (= {:a 1}
           (meta
            (stream-into! (with-meta clojure.lang.PersistentQueue/EMPTY {:a 1})
                          (.stream [1 2 3])))))
    (is (= [-1 -2 3 5 7 9 11] (stream-into! [-1 -2] xf par2)))))




    
(ns clojure.test-clojure.streams
  (:use clojure.test))

(deftest test-stream-reduce!
  (is (= :only-val (stream-reduce! + [:only-val])))
  (is (= 0 (stream-reduce! + [])))
  (is (= 5 (stream-reduce! + [1 4])))
  (is (= 6 (stream-reduce! + [1 2 3])))
  (is (= [2 3 4]
         (stream-reduce! (fn [v i] (conj v (inc i)))
                         []
                         [1 2 3])))

  (is (= 15 (stream-reduce! + (System.Linq.Enumerable/Range 1 5))))

  #_(is (= 9 (stream-reduce! + (-> (Stream/of (to-array [1 2 3 4 5]))
                                 (.filter (reify Predicate (test [_ v] (odd? v))))))))
  (is (= {:a 1}
           (meta
            (stream-reduce! (fn [v i] (conj v (inc i)))
                            (with-meta [] {:a 1})
                            [1 2 3])))))

(deftest test-stream-reduced!
  (is (= 5 (stream-reduce!
            (fn [x y] (reduced (+ x y)))
            [1 4])))

    (is (= 45 (stream-reduce!
               (fn [acc v]
                 (if (= v 10)
                   (reduced acc)
                   (+ acc v)))
               (System.Linq.Enumerable/Range 1 10)))))

(deftest stream-seq!-test
  (let [none (to-array [])
        one  (to-array ["a"])
        n    (to-array ["a" "b" "c"])
	    inf  (System.Linq.Enumerable/Repeat (type-args Int32) 42 Int32/MaxValue)  ;; we don't have a quick way to create an infinite sequence in .NET
        st   (stream-seq! one)
        l100 (System.Linq.Enumerable/Range 0 100)]
    (is (empty? (map identity (stream-seq! none))))
    (is (seq? st))
    (is (= ["a"] (map identity st)))
    (is (= ["a" "b" "c"] (map identity (stream-seq! n))))
    (is (= [42 42 42 42 42] (take 5 (stream-seq! inf))))
    (is (= 4950 (reduce + (stream-seq! l100))))))

(defn repeater [v cnt] 
 (System.Linq.Enumerable/Repeat (type-args Int32) v cnt))

(deftest stream-transduce!-test
  (let [xf (comp (filter odd?) (take 5))]
    (let [st (to-array [1 2 3 4 5 6 7 8 9])]
      (is (= [1 3 5 7 9] (stream-transduce! xf conj st))))

    (let [inf  (repeater 0 50)]
      (is (empty? (stream-transduce! xf conj inf))))

    (let [inf  (repeater 43 50)]
      (is (= [43 43 43 43 43] (stream-transduce! xf conj inf))))

    (let [inf   (repeater 43 50)]
      (is (= 215 (stream-transduce! xf + inf))))

    (let [inf   (repeater 43 50)]
      (is (= 315 (stream-transduce! xf + 100 inf))))

    (let [inf   (repeater 43 50)]
      (is (= "4343434343" (stream-transduce! xf str inf))))))

(deftest stream-into!-test
  (let [none (to-array [])
        one  (to-array ["a"])
        n    (to-array ["a" "b" "c"])
	    inf  (System.Linq.Enumerable/Repeat (type-args Int32) 42 Int32/MaxValue)  ;; we don't have a quick way to create an infinite sequence in .NET
        ;; par  (-> (LongStream/rangeClosed 1 10) .boxed .parallel)
        xf   (comp (map #(+ 2 %)) (filter odd?))
        ;;par2 (-> (LongStream/rangeClosed 1 10) .boxed .parallel)
        ]
    (is (empty? (stream-into! [] none)))
    (is (= ["a"] (stream-into! [] one)))
    (is (= ["a" "b" "c"] (stream-into! [] n)))
    (is (= [42 42 42 42 42]
           (stream-into! [] (repeater 42 5))))
    #_(is (= [1 2 3 4 5 6 7 8 9 10]
           (stream-into! [] par)))
    (is (= {:a 1}
           (meta
            (stream-into! (with-meta [] {:a 1})
                          (to-array [1 2 3])))))
    (is (= {:a 1}
           (meta
            (stream-into! (with-meta clojure.lang.PersistentQueue/EMPTY {:a 1})
                          [1 2 3]))))
    #_(is (= [-1 -2 3 5 7 9 11] (stream-into! [-1 -2] xf par2)))))