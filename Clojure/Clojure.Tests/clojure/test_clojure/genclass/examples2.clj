;   Copyright (c) Rich Hickey. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.

(ns ^{:doc "More test classes that are AOT-compile for the tests in
           clojure.test-clojure.genclass."}
  clojure.test-clojure.genclass.examples2
  (:import [System.Threading.Tasks Task]))


;; Framework 4.x does not have ValueTask`1
(compile-when (>= (:major dotnet-version) 5)


(gen-class :name clojure.test_clojure.genclass.examples2.MemoryStreamReadOverload
           :extends System.IO.MemoryStream)


(defn -ReadAsync
  ([this b o c t] (Task/FromResult (type-args System.Int32) (int 42)))
  ([this m t] (|System.Threading.Tasks.ValueTask`1[int]|. (int 43))))

) ;; compile-when