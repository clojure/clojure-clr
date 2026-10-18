;   Copyright (c) Rich Hickey. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.

(ns ^{:doc "More test classes that are AOT-compile for the tests in
           clojure.test-clojure.genclass."}
  clojure.test-clojure.genclass.examples6)

(assembly-load-from "Clojure.Tests.Support.dll")
(import '[Clojure.Tests.Support AllIn])

;; These gen-classes supporting CLJCLR-226 for by-ref paramter testing.
(gen-class :name clojure.test-clojure.genclass.examples6.CLJCLR226AllIn
           :extends Clojure.Tests.Support.AllIn)

(defn -InterfaceMethod [_ x] (int (+ x 2)))
(defn -VirtualMethod [_ x] (int (+ x 3)))
(defn -AbstractMethod [_ x] (int (+ x 4)))


(gen-class :name clojure.test-clojure.genclass.examples6.CLJCLR226Dictionary
           :implements [|System.Collections.Generic.IDictionary[Object,Object]|])

(def a-little-map {:a 1})

(defn -TryGetValue [_ k v]
  (if-let [x (get a-little-map  k)]
    (do (vreset! v x) true) 
    false))


(gen-class :name clojure.test-clojure.genclass.examples6.CLJCLR226EncoderConvert
           :extends System.Text.Encoder)

(defn -Convert [_ cs ci cn bs  bi bn f cu bu done] 
  (vreset! cu (int 7))
  (vreset! bu (int 8))
  (vreset! done true))


(gen-class :name clojure.test-clojure.genclass.examples6.CLJCLR226EncoderConvertBad
           :extends System.Text.Encoder
           :prefix "bad-")

(defn bad-Convert [_ cs ci cn bs  bi bn f cu bu done] 
  (vreset! cu (int 7))
  (vreset! bu 3000000000)
  (vreset! done true))

(gen-class :name clojure.test-clojure.genclass.examples6.CLJCLR226MethodsExample 
           :methods [[Bump [|System.Int32&|] Int32] ^:static [SBump [|System.Int32&|] Int32]])

(defn -Bump [this x] (vswap! x inc) (int 5))
(defn -SBump [x] (vswap! x + 10) (int 6))



;; These gen-classes supporting CLJCLR-227 for strict value testing (relaxing strictness).
(gen-class :name clojure.test-clojure.genclass.examples6.CLJCLR227IComparerExample
           :implements [System.Collections.IComparer]
           :state "state"
           :init "init"
           :constructors {[Object][]}
           :prefix "CLJCLR227-")

(defn CLJCLR227-init [retval]
  [[] retval])

(defn CLJCLR227-Compare [this x y] (.state this))


(gen-class :name clojure.test_clojure.genclass.examples6.CLJCLR227MemoryStreamExample
           :extends System.IO.MemoryStream
           :init "init"
           :constructors {[Object] [Int32]}
           :prefix "CLJCLR227M-")

(defn CLJCLR227M-init [x] [[x] nil])