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
   (is (thrown? InvalidCastException (inc ^long (.IndexOf my-string "l"))))))    ;; NetFramework and Net >= 5 throw with different messages, so we just check the exception


(deftest CLJCLR-234-str-on-doubles-and-floats-with-integral-values
    (is (= "1.0" (str 1.0)))
    (is (= "10.0" (str 10.0)))
    (is (= "1.0" (str (float 1.0))))
    (is (= "10.0" (str (float 10.0))))
    (is (= "1000000.0" (str 1e6)))
    (is (= "Infinity" (str ##Inf))))


(defmacro with-culture [culture & form]
  `(let [current# (System.Globalization.CultureInfo/CurrentCulture)]
    (try
      (set! (.CurrentCulture System.Threading.Thread/CurrentThread) (System.Globalization.CultureInfo. ~culture))
      ~@form
      (finally
        (set! (.CurrentCulture System.Threading.Thread/CurrentThread) current#)))))

(deftest CLJCLR-236-printf-on-dot-d-for-cultures-with-dot-separators
  (with-culture "de-DE"
    (is (= "1.234.567" (format "%,d" 1234567)))
    (is (= "-9.223.372.036.854.775.808" (format "%,d" Int64/MinValue)))))

(deftest CLJCLR-235-printf-on-percent-s-should-use-invariant-culture
  (with-culture "de-DE"
    (is (= "1.5" (format "%s" 1.5)))
    (is (= "1.5" (format "%s" (float 1.5))))
    (is (= "1.0" (format "%s" 1.0)))
    (is (= "Infinity" (format "%s" ##Inf)))   
    (is (= "-Infinity" (format "%s" ##-Inf))))) 

(deftest CLJCLR-237-printf-on-dot-d-for-biginteger-for-cultures-with-dot-separators
  (testing "[3] grouping, '.' separator"
    (with-culture "de-DE"
      (is (= "12.345.678.901.234.567.890" (format "%,d" (biginteger 12345678901234567890N))))))
  (testing "[3] grouping, ',' separator"
    (with-culture "en-US"
      (is (= "12,345,678,901,234,567,890" (format "%,d" (biginteger 12345678901234567890N))))
      (is (= "-12,345,678,901,234,567,890" (format "%,d" (biginteger -12345678901234567890N))))))
(compile-when (>= (:major dotnet-version) 5)
  (testing "[0] grouping"
    (with-culture "en-US-POSIX"
      (is (= "12345678901234567890" (format "%,d" (biginteger 12345678901234567890N))))))
);; compile-when
  (testing "[3 2] grouping, ',' separator"
    (with-culture "en-IN"
      (is (= "1,23,45,67,89,01,23,45,67,890" (format "%,d" (biginteger 12345678901234567890N)))))))


(def dec-min Decimal/MinValue)
(def dec-max Decimal/MaxValue)

(deftest CLJCLR-238-quot-operations-need-some-love
  (testing "ClrDecimalOps.quotient must truncate"
    (is (= (decimal 617) (quot (decimal 1234.5) 2)))
    (is (= (decimal 3) (quot (decimal 7) (decimal 2)))))
  (testing "ClrDecimalOps.divide now throws on overflow"
    (is (thrown? OverflowException (/ Decimal/MaxValue (decimal 0.5)))))
  (testing "ClrDecimalOps.incP and decP now work (used to throw an InvalidCastException)"
    (is (= 79228162514264337593543950336M (inc' dec-max)))
    (is (= 79228162514264337593543950336M (clojure.lang.Numbers/incP dec-max)))
    (is (= -79228162514264337593543950336M (dec' dec-min)))
    (is (= -79228162514264337593543950336M (clojure.lang.Numbers/decP dec-min)))))

(deftest CLJCLR-239-restore-culture-invariant-printing-for-System-Decimal
  (with-culture "de-DE"
     (is (= "1234.5" (str (decimal 1234.5))))
     (is (= "1234.5" (format "%s" (decimal 1234.5))))))

(deftest CLJCLR-240-make-decimal-parse-string-culture-invariant
  (with-culture "de-DE"
    (is (= (decimal 1234.5) (decimal "1234.5")))))
  
