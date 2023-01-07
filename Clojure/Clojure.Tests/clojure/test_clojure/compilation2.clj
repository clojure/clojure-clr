;   Copyright (c) Rich Hickey. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.

; Author: Frantisek Sodomka

(assembly-load-from "clojure.test_clojure.compilation.line_number_examples.clj.dll")           ;;; DM:Added
(assembly-load-from "clojure.test_clojure.compilation.load_ns.clj.dll")                        ;;; DM:Added
(ns clojure.test-clojure.compilation2
  (:import (clojure.lang Compiler Compiler+CompilerException))                                 ;;; Compiler$CompilerException
  (:require [clojure.test.generative :refer (defspec)]
		    [clojure.data.generators :as gen]
			[clojure.test-clojure.compilation.line-number-examples :as line]
			clojure.string)                                                                    ;;; DM:Added -- seem to have an order dependency that no longer works.
  (:use clojure.test
        [clojure.test-helper :only (should-not-reflect should-print-err-message)]))

; http://clojure.org/compilation


;;; this file splits off the tests that can only be done having AOT-compilation.
;;; Eventually, merge this back into compilation.clj.


(defrecord Y [a])
#clojure.test_clojure.compilation2.Y[1]
(defrecord Y [b])

(binding [*compile-path* "."]              ;;; "target/test-classes"
  (compile 'clojure.test-clojure.compilation.examples))


#_(deftest test-compiler-line-numbers                   ;;; DM: TODO :: Improve Compiler source information.  And then do https://github.com/clojure/clojure/commit/715754d3f69e85b07fa56047f0d43d400ab36fce
  (let [fails-on-line-number? (fn [expected function]
                                 (try
                                   (function)
                                   nil
                                   (catch Exception t                                                                    ;;; Throwable
                                     (let [frames (filter #(= "line_number_examples.clj" (.GetFileName %))               ;;; .getFileName
                                                          (.GetFrames (System.Diagnostics.StackTrace. t true)))          ;;; (.getStackTrace t))
                                           _ (if (zero? (count frames))
                                               (Console/WriteLine (.ToString t))                                         ;;; (.printStackTrace t)
                                               )
                                           actual (.GetFileLineNumber ^System.Diagnostics.StackFrame (first frames))]    ;;; .getLineNumber ^StackTraceElement
                                       (= expected actual)))))]
    (is (fails-on-line-number?  13 line/instance-field))
    (is (fails-on-line-number?  19 line/instance-field-reflected))
    (is (fails-on-line-number?  25 line/instance-field-unboxed))
    #_(is (fails-on-line-number?  32 line/instance-field-assign))
    (is (fails-on-line-number?  40 line/instance-field-assign-reflected))
    #_(is (fails-on-line-number?  47 line/static-field-assign))
    (is (fails-on-line-number?  54 line/instance-method))
    (is (fails-on-line-number?  61 line/instance-method-reflected))
    (is (fails-on-line-number?  68 line/instance-method-unboxed))
    (is (fails-on-line-number?  74 line/static-method))
    (is (fails-on-line-number?  80 line/static-method-reflected))
    (is (fails-on-line-number?  86 line/static-method-unboxed))
    (is (fails-on-line-number?  92 line/invoke))
    (is (fails-on-line-number? 101 line/threading))
    (is (fails-on-line-number? 112 line/keyword-invoke))
    (is (fails-on-line-number? 119 line/invoke-cast))))

(deftest clj-1208
  ;; clojure.test-clojure.compilation.load-ns has not been loaded
  ;; so this would fail if the deftype didn't load it in its static
  ;; initializer as the implementation of f requires a var from
  ;; that namespace
  (is (= 1 (.f (clojure.test_clojure.compilation.load_ns.x.)))))


(deftest CLJ-979
  (is (= clojure.test_clojure.compilation.examples.X
         (class (clojure.test-clojure.compilation.examples/->X))))
  (is (.b (clojure.test_clojure.compilation2.Y. 1)))
  (is (= clojure.test_clojure.compilation.examples.T
         (class (clojure.test_clojure.compilation.examples.T.))
         (class (clojure.test-clojure.compilation.examples/->T)))))


(deftest CLJ-1184-do-in-non-list-test
  (testing "do in a vector throws an exception"
    (is (thrown? Compiler+CompilerException                                          ;;; Compiler$CompilerException
                 (eval '[do 1 2 3]))))
  (testing "do in a set throws an exception"
    (is (thrown? Compiler+CompilerException                                          ;;; Compiler$CompilerException
                 (eval '#{do}))))

  ;; compile uses a separate code path so we have to call it directly
  ;; to test it
  (letfn [(compile [s]         (System.IO.Directory/CreateDirectory "test/clojure")               ;;; DM: Added the CreateDirectory
            (spit "test/clojure/bad_def_test.clj" (str "(ns test.clojure.bad-def-test)\n" s))     ;;; DM: Added test. to ns
            (try
             (binding [*compile-path* "test"]
               (clojure.core/compile 'test.clojure.bad-def-test))                                 ;;; DM: Added test. to name
             (finally
               (doseq [f (.GetFiles (System.IO.DirectoryInfo. "test/clojure"))                    ;;; .listFiles java.io.File.
                       :when (re-find #"bad_def_test" (str f))]
                 (.Delete f)))))]
    (testing "do in a vector throws an exception in compilation"
      (is (thrown? Compiler+CompilerException (compile "[do 1 2 3]"))))                           ;;; Compiler$CompilerException
    (testing "do in a set throws an exception in compilation"
      (is (thrown? Compiler+CompilerException (compile "#{do}"))))))                              ;;; Compiler$CompilerException