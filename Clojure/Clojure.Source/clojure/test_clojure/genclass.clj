;   Copyright (c) Rich Hickey. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.

; Author: Stuart Halloway

(ns clojure.test-clojure.genclass
  (:use clojure.test clojure.test-helper)
  (:import clojure.test-clojure.genclass.examples.ExampleClass))

(deftest arg-support
  (let [example (ExampleClass.)
        o (Object.)]
    (is (= "foo with o, o" (.foo example o o)))
    (is (= "foo with o, i" (.foo example o (int 1))))
    (is (thrown? ArgumentException (.foo example o)))))         ;;; java.lang.UnsupportedOperationException

(deftest name-munging
  (testing "mapping from Java fields to Clojure vars"
    (is (= #'clojure.test-clojure.genclass.examples/-foo-Object-int
           (get-field ExampleClass 'foo_Object_int__var)))
    ;;;(is (= #'clojure.test-clojure.genclass.examples/-toString   ------ TODO: Figure out why JVM can find this var, we can't.
    ;;;       (get-field ExampleClass 'toString__var))))
           )

(deftest genclass-option-validation
  (is (fails-with-cause? ArgumentException #"Not a valid method name: has-hyphen"                            ;;; IllegalArgumentException
        (@#'clojure.core/validate-generate-class-options {:methods '[[fine [] void] [has-hyphen [] void]]}))))