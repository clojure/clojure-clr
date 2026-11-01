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

(assembly-load-from "Clojure.Tests.Support.dll")
(import '[Clojure.Tests.Support  AlmostEverything] )

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

;; Return [w r] = [printed-string read-value] for a value x, using *print-dup* true and *read-eval* true
(defn- print-dup-roundtrip [x]
   (let [w (binding [*print-dup* true] (pr-str x))
         r (binding [*read-eval* true] (read-string w))]
     [w r]))

(deftest CLJCLR-241-make-print-dup-on-decimal-print-readably
  (with-culture "de-DE"
     (let [[w r] (print-dup-roundtrip (decimal 1234.5))]
       (is (= "#=(clojure.core/decimal \"1234.5\")" w))
       (is (= (decimal 1234.5) r)))))
  
(deftest CLJCLR-242-make-compare-antisymmetric-on-decimal-char-ratio
  (testing "basic numeric comparisons involving decimal, char, and ratio"
    (let [d (decimal 1234.5)
          c \a
          r 1/2
          one 1
          dhalf 0.5]
      (are [result x y] (= result (compare x y))
          1 d     one
          1 d     c
          1 d     dhalf
          1 d     r

         -1 one   d
         -1 one   c
          1 one   r
          1 one   dhalf

         -1 c     d
          1 c     r
          1 c     one
          1 c     dhalf

         -1 r     d
         -1 r     c
         -1 r     one
          0 r     dhalf

         -1 dhalf d
         -1 dhalf c
          0 dhalf r
         -1 dhalf one   )))

  (testing "ratio fails against non-numeric"
    (is (thrown? ArgumentException (compare 3/5 "5")))
    (is (thrown? ArgumentException (compare "5" 3/5)))
    (is (thrown? ArgumentException (compare 3/5 false)))
    (is (thrown? ArgumentException (compare false 3/5)))))
  

(deftest CLJCLR-243-persistent-tree-map-and-set-handle-argumentexception-in-equality-teseting
  (is (not= (sorted-set :a) (sorted-set 1)))
  (is (not= (sorted-set 1) (sorted-set :a)))
  (is (not= (sorted-map 1 1) (sorted-map :a 1)))
  (is (not= (sorted-map :a 1) (sorted-map 1 1)))
  (is (not= (sorted-set "a") (sorted-set 1))))


(deftest CLJCLR-244-print-dup-for-array-maps-etc
   (are [x] (let [[w r] (print-dup-roundtrip x)]
              (= x r))
     (clojure.lang.PersistentArrayMap/create {:a 1})
     (clojure.lang.PersistentHashMap/create {:a 1 })
     (clojure.lang.PersistentTreeMap/create {:a 1}))
        
   (are [x] (let [[w r] (print-dup-roundtrip x)]
              (= (class x) (class r)))
     (clojure.lang.PersistentArrayMap/create {:a 1})
     (clojure.lang.PersistentTreeMap/create {:a 1})))


(deftest CLJCLR-245-fix-array-sorting-handling-of-boolean-functions
   (is (= [1 2 3] (sort < [3 1 2])))
   (is (= [3 2 1] (sort > [3 1 2])))
   (is (= [1 2 3] (sort #(< %1 %2) [3 1 2])))
   (is (= [1 2 3] (sort (comparator <) [3 1 2])))
   (is (= [3 2 1] (sort-by identity > [3 1 2]))))

(defrecord Data [key tag])

(defn make-small-keyset-data [count num-keys]
  (let [data (map #(->Data %1 %2) (repeatedly count #(rand-int num-keys)) (range count))]
    (object-array data)))

(defn make-one-key-data [count]
  (let [data (map #(->Data 1 %2) (repeat count 1) (range count))]
    (object-array data)))

(deftest CLJCLR-246-make-sort-use-a-stable-sort-algorithm
   (let [data (make-one-key-data 1000)
         sorted (sort-by #(:key %) data)]
     (is (every? (fn [[x y]]
                    (<= (:key x) (:key y)))
                (map vector sorted (rest sorted))) "not sorted")
     (is (every? (fn [[x y]]
                    (or (< (:key x) (:key y))
                        (< (:tag x) (:tag y))))
                (map vector sorted (rest sorted))) "not stable"))
   (let [data (make-small-keyset-data 1000 3)
         sorted (sort-by #(:key %) data)]
     (is (every? (fn [[x y]]
                    (<= (:key x) (:key y)))
                (map vector sorted (rest sorted))) "not sorted")
     (is (every? (fn [[x y]]
                    (or (< (:key x) (:key y))
                        (< (:tag x) (:tag y))))
                (map vector sorted (rest sorted))) "not stable")))

(deftest CLJCLR-246-sort-with-comparator-exception-leaves-data-permuted
  ;; Analysis suggests that total comparisons will be about 
  ;;     2*n*(log_2 n + 1).
  ;; We need to run enough tests to make sure we get exceptions.
  ;; From testing the parameters, we will get plenty from the runs below.
  (let [tester (fn [n]
                 (let [counter (atom 0)
                       comparator (fn [x y]
                                    (swap! counter inc)
                                    (when (> @counter n)  (throw (ArgumentException. "Count exceeded")))
                                    (< x y))
                       data (make-small-keyset-data 1024 2)
                       copy (object-array (seq data))]
                   (try (sort-by #(:key %) comparator data)
                        (catch ArgumentException e))   ;; many will fail, which is the point.  We just want to check for data permuted, not corrupted.
                   (is (= (frequencies (seq copy)) (frequencies (seq data))))))]
    (doseq [n (range 100 7501 100)]
        (tester n))))

(deftest CLJCLR-247-sort-by-throws-InvalidCastException-on-invalid-comparator
  (is (thrown? InvalidCastException (sort-by identity #'< [1 2 3]))))

(deftest CLJCLR-248-sort-throws-InvalidCastException-on-invalid-comparator
  (is (thrown? InvalidCastException (sort #'< [1 2 3]))))

(deftest CLJCLR-248-sort-uses-default-comparer-on-nil-comp-argument
  (is (= [1 2 3] (sort nil [3 1 2])))
  (is (= ["B" "a" "b"] (sort nil ["b" "B" "a"]))))


(deftest CLJCLR-249-handling-by-ref-in-constructors  ;; requires also the CLJCLR-250 fix
  (let [n (identity (int 12))
        result (AlmostEverything. (by-ref n))]
     (is (= "ref int" (.ToString result)))
     (is (= 13 n)))
  (let [n 12
       ae (AlmostEverything. "x" (by-ref n))
       s  (.ToString ae)]
    (is (= "string+ref int" s))
    (is (= n 32))))

(deftest CLJCLR-250-handling-by-ref-in-methods
  (is (thrown-with-cause-msg? 
         clojure.lang.Compiler+CompilerException
         #"Unresolvable by-ref argument"
         (eval '(let [x (Object.)
                      s (identity "abc") ;; to avoid the string typing so we get ambiguity -- well, below, actually.
                      m (int 1)]
               	  (.Ambig x s (by-ref m))))))
  (is (thrown-with-cause-msg? 
         clojure.lang.Compiler+CompilerException
         #"Unresolvable by-ref argument"
         (eval '(let [x (Object.)
                      s (identity "abc") ;; to avoid the string typing so we get ambiguity.
                      m (int 1)]
               	  (.Ambig ^Clojure.Tests.Support.AlmostEverything x s (by-ref m))))))  ;; need full namespace, because the type is not imported in the eval context

  (let [n 12
        ae (AlmostEverything.)
        r (.Out ae (by-ref n))]
     (is (= 33 r))
     (is (= 13 n)))

  (let [n 12
        r (Int32/TryParse "42" (by-ref n))]
    (is (= true r))
    (is (= 42 n)))

  (is (thrown-with-cause-msg? 
         clojure.lang.Compiler+CompilerException
         #"Unresolvable by-ref argument"
         (eval '(let [n 12
                      ae (Clojure.Tests.Support.AlmostEverything.)]  ;; need full namespace
                  (.Over ae (by-ref n)))))))

(deftest CLJCLR-251-handling-by-ref-in-param-tags
  (is (thrown-with-cause-msg? 
         clojure.lang.Compiler+CompilerException
         #"Error - param-tags .* insufficient to resolve"
         (eval '(let [n 12
                      ae (Clojure.Tests.Support.AlmostEverything.)]  ;; need full namespace
                  (^[int] Clojure.Tests.Support.AlmostEverything/.Out ae (by-ref n))))))

  (is (thrown-with-cause-msg? 
         clojure.lang.Compiler+CompilerException
         #"Error - param-tags .* insufficient to resolve"
         (eval '(let [n (int 12)
                      ae (Clojure.Tests.Support.AlmostEverything.)]  ;; need full namespace
                  (^[int] Clojure.Tests.Support.AlmostEverything/.Out ae (by-ref n))))))

  (is (thrown-with-cause-msg? 
         clojure.lang.Compiler+CompilerException
         #"Error - param-tags .* insufficient to resolve"
         (eval '(let [n (int 12)]
                  (^[int] Clojure.Tests.Support.AlmostEverything/new (by-ref n))))))  ;; need full namespace


  ;; int case
  (let [n (int 12)
        ae (AlmostEverything.)
        r (^[|System.Int32&|] AlmostEverything/.Out ae (by-ref n))]
     (is (= 33 r))
     (is (= 13 n)))

  ;; long case
  (let [n 12
        ae (AlmostEverything.)
        r (^[|System.Int32&|] AlmostEverything/.Out ae (by-ref n))]
     (is (= 33 r))
     (is (= 13 n)))
 
 ;; object case
  (let [n (identity 12)
        ae (AlmostEverything.)
        r (^[|System.Int32&|] AlmostEverything/.Out ae (by-ref n))]
     (is (= 33 r))
     (is (= 13 n)))

  ;; plain arg passed to by-ref param works
  (let [n (identity 12)
        ae (AlmostEverything.)
        r (^[|System.Int32&|] AlmostEverything/.Out ae n)]
     (is (= 33 r))
     (is (= 12 n)))   ;; not changed

  ;; plain arg to constructor passed to by-ref param works, differing type
  (let [n 12
        ae (^[|System.Int32&|] AlmostEverything/new n)
        r (.ToString ae)]
     (is (= "ref int" r))
     (is (= 12 n))) 


  ;; by-ref-ness picks Out(ref int) -- int case
  (let [n (int 12)
        ae (AlmostEverything.)
        r (^[_] AlmostEverything/.Out ae (by-ref n))]
     (is (= 33 r))
     (is (= 13 n)))

  ;; by-ref-ness picks Out(ref int) -- long case
  (let [n 12
        ae (AlmostEverything.)
        r (^[_] AlmostEverything/.Out ae (by-ref n))]
     (is (= 33 r))
     (is (= 13 n)))

  (let [n 12
        ae (^[_] AlmostEverything/new (by-ref n))
        s (.ToString ae)]
     (is (= "ref int" s))
     (is (= 13 n)))

  (is (thrown-with-cause-msg? 
         clojure.lang.Compiler+CompilerException
         #"Error - param-tags .* insufficient to resolve"
         (eval '(let [n (int 12)
                      ae (Clojure.Tests.Support.AlmostEverything.)]  ;; need full namespace
                  (^[_] Clojure.Tests.Support.AlmostEverything/.Out ae n)))))

  (is (thrown-with-cause-msg? 
         clojure.lang.Compiler+CompilerException
         #"No instance field, property, or method taking 0 args named Out found for AlmostEverything"
         (eval '(let [n (int 12)
                      ae (Clojure.Tests.Support.AlmostEverything.)]  ;; need full namespace
                  (^[_] Clojure.Tests.Support.AlmostEverything/.Out n)))))

  ;; parse-time error because can't convert long <-> DataTime
  (is (thrown-with-cause-msg? 
         clojure.lang.Compiler+CompilerException
         #"Error - param-tags .* insufficient to resolve"
         (eval '(let [d 12  ;; long
                      s "1996/12/25"]
                  (^[String |System.DateTime&|] DateTime/TryParse s (by-ref d))))))

  ;; works when untyped
  (let [d nil  ;; untyped
        s "1996/12/25"
        r (^[String |System.DateTime&|] DateTime/TryParse s (by-ref d))]
       (is (= true r)))

(compile-when (>= (:major dotnet-version) 9)  ;; .NET 9 dropped the class constraint -- can't be handled by earlier versions
  ;; generic 'ref T' method with a primitive local
  (let [a 5 
        old (^[(type-args Int64) _ _] System.Threading.Interlocked/Exchange (by-ref a) 6)]
     (is (= 5 old))
     (is (= 6 a)))

  ;; the non-param-tagged version works too, but not tested anywhere else

  (let [a 5 
        old (System.Threading.Interlocked/Exchange (type-args Int64) (by-ref a) 6)]
     (is (= 5 old))
     (is (= 6 a)))
) ;; compile-when

  (let [a (identity "x") 
        old (^[(type-args String) _ _] System.Threading.Interlocked/Exchange (by-ref a) "y")]
     (is (= "x" old))
     (is (= "y" a)))

  (testing "Invocation with param-tags having incorrect number of args"  ;; parallel to the test in param_tags.clj, but with too many entries in the hint
    (let [e (try
              (eval '(^[long long] Math/Max -1))                                                              ;;; Math/abs
              (catch Compiler+CompilerException e (str "-> " (.Message (.InnerException e)))))]                ;;; Compiler$CompilerException .getMessage  .getCause
      (is (not (nil? (re-find #"expected 2.*received 1" e))) "Error message was expected to indicate 2 arguments were expected but 1 was provided"))))
