;   Copyright (c) Rich Hickey. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.
; Authors: Fogus

(ns clojure.test-clojure.method-thunks
  (:use clojure.test)
  #_(:require [clojure.java.io :as jio])
  #_(:import (clojure.lang Compiler )                                       ;;; Tuple -- can't import -- conflicts with System.Tuple.  We will qualify it below.
           (java.util Arrays UUID Locale)
           (java.io File FileFilter)
           clojure.lang.IFn$LL))

(set! *warn-on-reflection* true)

(deftest method-arity-selection
  (is (= '([] [] [])
         (take 3 (repeatedly ^[] clojure.lang.Tuple/create))))              ;;; added clojure.lang.
  (is (= '([1] [2] [3])
         (map ^[_] clojure.lang.Tuple/create [1 2 3])))                     ;;; added clojure.lang.
  (is (= '([1 4] [2 5] [3 6])
         (map ^[_ _] clojure.lang.Tuple/create [1 2 3] [4 5 6]))))          ;;; added clojure.lang.

(deftest method-signature-selection
  (is (= [1.23 3.14]
         (map ^[double] Math/Abs [1.23 -3.14])))                            ;;; abs
  (is (= [(float 1.23)  (float 3.14)]
         (map ^[float] Math/Abs [1.23 -3.14])))                             ;;; abs
  (is (= [1 2 3]
         (map ^[long] Math/Abs [1 2 -3])))                                  ;;; abs
  #_(is (= [#uuid "00000000-0000-0001-0000-000000000002"]                   ;;; System.GUID doesn't have conventient constructors for testing
         (map ^[long long] UUID/new [1] [2])))
  (is (= ["abc" "abc"] (let [s (.ToCharArray "abc")] (map ^[chars] String/new [s s]))))  ;; ADDED
  #_(is (= '("a" "12")                                                       ;;; I'm not sure what is being tested here
         (map ^[Object] String/valueOf ["a" 12])))
  #_(is (= ["A" "B" "C"]                                                     ;;; Im not sure what is being tested here -- .toUpperCase has only one overload with a single argument
         (map ^[java.util.Locale] String/.toUpperCase ["a" "b" "c"] (repeat java.util.Locale/ENGLISH))))
  (is (= ["A" "B" "C"]  
         (map ^[System.Globalization.CultureInfo] String/.ToUpper ["a" "b" "c"] (repeat System.Globalization.CultureInfo/CurrentCulture))))  ;;; ADDED -- but we'll do the same here anyway
  #_(is (thrown? ClassCastException
               (doall (map ^[long] String/valueOf [12 "a"])))
  (is (thrown? Exception (doall (map ^[double] Math/Abs [1.23 DateTime/Now]))))   ;;  "abc" we'd try to convert the string and get a format exception
  )
  (testing "bad method names"
    (is (thrown-with-msg? Exception #"static method" (eval 'String/foo)))          ;;; java.lang.String
    (is (thrown-with-msg? Exception #"instance method" (eval 'String/.foo)))       ;;;java.lang.String
    (is (thrown-with-msg? Exception #"constructor" (eval 'Math/new)))))

(def mt ^[_] clojure.lang.Tuple/create)                                           ;;; added clojure.lang.
(def mts {:fromString ^[String] Guid/Parse})                                      ;;; [_] UUID/fromString
(def gbs ^[] String/.ToCharArray)                                                   ;;; /.getBytes

(deftest method-thunks-in-structs
  (is (= #uuid "00000000-0000-0001-0000-000000000002"
         ((:fromString mts) "00000000-0000-0001-0000-000000000002")))
  (is (= [1] (mt 1)))
  (is (= \a (first (seq (gbs "a"))))))                                            ;;; 97

#_(deftest hinted-method-values                                                             ;;; Not sure what to translate this to
  (is (thrown? Exception (eval '(.listFiles (jio/file ".") #(File/.isDirectory %)))))
  (is (seq (.listFiles (jio/file ".") ^FileFilter File/.isDirectory)))
  (is (seq (File/.listFiles (jio/file ".") ^FileFilter File/.isDirectory)))
  (is (seq (^[FileFilter] File/.listFiles (jio/file ".") ^FileFilter File/.isDirectory))))

;;; ADDED

(deftest type-args-work
  (is (= [2 2 2] (take 3 (System.Linq.Enumerable/Repeat (type-args Int32) 2 5)))))

