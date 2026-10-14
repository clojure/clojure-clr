;   Copyright (c) Rich Hickey. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.

;; Tests added for ClojureCLR -- miscellaneous
;; Someday, some of these could be moved to more appropriate locations

(ns clojure.test-clojure.clr.added
  (:use clojure.test
        [clojure.test.generative :exclude (is)]
        clojure.template)
  (:import (clojure.lang Compiler Compiler+CompilerException))   
  (:require [clojure.data.generators :as gen]
            [clojure.test-helper :as helper]))

(deftest test-bit-not
  (are [x y] (= x y)
       -1 (bit-not 0)
	   Int64/MinValue (bit-not 0x7FFFFFFFFFFFFFFF)))

;; CLJCLR-214 Misnamed exception keyword for macroexpansion

(defmacro boom [] (throw (InvalidCastException. "boom")))

(deftest macroexpansion-error-reported-properly
  (try     
    (macroexpand '(boom))
    (catch Exception e
      (is (= :macroexpansion (:clojure.error/phase (ex-data e))))
      (is (.StartsWith ^String (clojure.main/ex-str (clojure.main/ex-triage (Throwable->map e))) "Unexpected error (InvalidCastException) macroexpanding boom")))))
      
        
;; CLJCLR-215 Improve error message when trying to proxy a sealed class
(deftest proxy-sealed-class-error-message
  (is (thrown-with-cause-msg? Compiler+CompilerException #"Cannot proxy a sealed class System.String" (eval '(proxy [System.String] [] )))))

;; CLJCLR-216 Make Atom.compareAndSet work with ValueTypes

(deftest atom-compare-and-set-with-valuetypes
  (let [cas-fn (fn [oldv oldv2]
                 (let [a (atom oldv)
                       cas (compare-and-set! a oldv2 :new)
                       av @a]
                   [cas av]))]
    (are [oldv oldv2 cas av ] (= [cas av] (cas-fn oldv oldv2))
        false false true :new
        true  true true :new
        (= 1 1) true true :new
        (.Equals "a" "a") true true :new
        1  1 true :new
        1000 1000 true :new
        \a  \a true :new
        ;; make sure it works with nil
        nil nil true :new
        1 nil false 1
        ;; make sure not everything works
        false true false false
        1 2 false 1)))

;; CLJCLR-217 A struct type hint on a reflective call generates invalid IL

;; Use def here so we do not have any typing available in the calls below.
(def my-datetime (System.DateTime. 2020 1 1))
(def my-string "Hello")

(deftest struct-type-hint-on-reflective-call
  (testing "struct type hint on a reflective call"
    (is (= 2020 (.Year ^DateTime (.Date  my-datetime))))
    (is (instance? System.DateTime (identity ^DateTime (.AddDays my-datetime 1.0)))))
  (testing "reference hints still work"
    (is (= 5 (.Length ^String (.ToUpper my-string)))))
  (testing "primitive type hints still work"
    (is (= 3 (inc ^int (.IndexOf my-string "l")))))
  (testing "wrong type hint throws a catchable InvalidCastException and does not crash"
   (is (thrown-with-msg? InvalidCastException #"Unable to cast" (.ToLower ^String (.Date my-datetime))))
   (is (thrown-with-msg? InvalidCastException #"Unable to cast" (inc ^long (.IndexOf my-string "l"))))))


;; CLJCLR-132 Span overload causes boxing errors


;; CLJCLR-218 Runtime-async return conversion in proxy stubs throws access violations.





;; CLJCLR-220 gen-class misses methods of inherited interfaces



