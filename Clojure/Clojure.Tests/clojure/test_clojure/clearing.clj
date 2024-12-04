;   Copyright (c) Rich Hickey. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.

(ns clojure.test-clojure.clearing
  (:import
    [java.lang.reflect Field])
  (:require
    [clojure.string :as str]
    [clojure.test :refer :all]))

(set! *warn-on-reflection* true)

;;; ClojureCLR does not do locals clearing.
;;; A very long time ago, in a conversation with the DLR/IronXXX people
;;; (I think Rich Hickey raised the issue), we were told that locals clearing
;;; would not be necessary on the CLR.
;;; 
;;; Indeed, if one runs CLJ-2145-repro with 1E9 as suggested, exhaustion does not occur.

#_(defn fields
  [o]
  (.getDeclaredFields (class o)))

#_(defn primitive?
  [^Field field]
  (.isPrimitive (.getType field)))

#_(defn special-fn-field?
  [^String field-name]
  (or (= field-name "__meta")
    (str/starts-with? field-name "__cached_class__")
    (str/starts-with? field-name "const__")
    (str/ends-with? field-name "__")))

#_(defn clearable-closed-overs
  [fobj]
  (->> (fields fobj)
    (remove primitive?) ;; can't clear primitives
    (remove #(special-fn-field? (.getName ^Field %)))))

#_(defn private-field-value [^Object obj ^Field field]
  (. field (setAccessible true))
  (. field (get obj)))

;; Check whether all non-primitive closed-overs in a function are nil
#_(defn cleared?
  [fobj]
  (every? #(nil? (private-field-value fobj %)) (clearable-closed-overs fobj)))

;; ---

;; After invocation, check all closed-over non-primitive fields in a :once fn

#_(defn check-clear
  [f]
  (is (not (cleared? f)))
  (f)
  (cleared? f))

#_(deftest test-clearing
  (let [x :a]
    ;; base case
    (is (check-clear (^{:once true} fn* [] x)))

    ;; conditional above fn
    (when true
      (is (check-clear (^{:once true} fn* [] x))))
    (case x
      :a (is (check-clear (^{:once true} fn* [] x))))

    ;; loop above fn
    (loop []
      (is (check-clear (^{:once true} fn* [] x))))

    ;; conditional below fn
    (is (check-clear (^{:once true} fn* [] (when true x))))

    ;; loop below fn
    (is (not (check-clear (^{:once true} fn* [] (loop [] x)))))
    (is (not (check-clear (^{:once true} fn* [] (loop [] x) nil))))

    ;; recur in :once below fn
    (is (not (check-clear (^{:once true} fn* [] (if false (recur) x)))))
    ))

#_(deftest test-nested
  (let [x :a]
    ;; nested fns
    (let [inner (^{:once true} fn* [] x)
          outer (fn* [] inner)]
      (is (not (check-clear outer))) ;; outer not :once
      (is (check-clear inner)))

    (let [inner (^{:once true} fn* [] x)
          outer (^{:once true} fn* [] inner)]
      (is (check-clear outer))
      (is (check-clear inner)))

    (let [inner (^{:once true} fn* [] x)
          middle (^{:once true} fn* [] inner)
          outer (^{:once true} fn* [] middle)]
      (is (check-clear outer))
      (is (check-clear middle))
      (is (check-clear inner)))))

;; Repro from CLJ-2145
#_(defn consume [x] (doseq [_ x] _))
#_(defn call-and-keep [f] (f) f)
#_(defn repro [x]
  (if true (call-and-keep (^:once fn* [] (consume x)))))
#_(deftest CLJ-2145-repro
  (let [f (repro (range 100))] ;; 1e9 to exhaust
    (is (cleared? f))))