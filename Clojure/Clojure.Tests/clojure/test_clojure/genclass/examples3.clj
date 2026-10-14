;   Copyright (c) Rich Hickey. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.

(ns ^{:doc "More test classes that are AOT-compile for the tests in
           clojure.test-clojure.genclass."}
  clojure.test-clojure.genclass.examples3
  (:import [System.Threading.Tasks Task]))

(gen-class :name clojure.test_clojure.genclass.examples3.MemoryStreamFlushOverload
           :extends System.IO.MemoryStream)


(defn -FlushAsync [this ct]
  Task/CompletedTask)