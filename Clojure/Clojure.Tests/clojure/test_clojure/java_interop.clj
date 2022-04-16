;   Copyright (c) Rich Hickey. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.

; Author: Frantisek Sodomka

(assembly-load-with-partial-name "System.Drawing")                           ;;; DM: Added
(ns clojure.test-clojure.java-interop
  (:use clojure.test)
  (:require [clojure.data :as data]
                                                                             ;;; [clojure.inspector]
            [clojure.pprint :as pp]
            [clojure.set :as set]
            [clojure.test-clojure.proxy.examples :as proxy-examples])                           
  (:import                                                                   ;;; java.util.Base64
           (clojure.lang AtomicLong AtomicInteger)))                         ;;; java.util.concurrent.atomic

; http://clojure.org/java_interop
; http://clojure.org/compilation


(deftest test-dot
  ; (.instanceMember instance args*)
  (are [x] (= x "FRED")
      (.ToUpper "fred")                          ;;; toUpperCase
      (. "fred" ToUpper)                         ;;; toUpperCase
      (. "fred" (ToUpper)) )                     ;;; toUpperCase

  (are [x] (= x true)
      (.StartsWith "abcde" "ab")                     ;;; startsWith
      (. "abcde" StartsWith "ab")                    ;;; startsWith
      (. "abcde" (StartsWith "ab")) )                ;;; startsWith

  ; (.instanceMember Classname args*)
  (are [x] (= x "System.String")               ;;; java.lang.String
      (.FullName String)                           ;;; getName
      (. (identity String) FullName)               ;;; getName
      ;(. (identity String) (FullName)) )           ;;; getName
  )
  ; (Classname/staticMethod args*)
  (are [x] (= x 7)
      (Math/Abs -7)                                ;;; abs
      (. Math Abs -7)
      (. Math (Abs -7)) )

  ; (. target -prop)                                          ;;; Anyone know a class with public fields?
  (let [p (System.Drawing.Point. 1 2)]                              ;;; java.awt.Point.
    (are [x y] (= x y)
       1 (.-X p)                                              ;;; .-x
       2 (.-Y p)                                              ;;; .-y
       1 (. p -X)                                             ;;; -x
       2 (. p -Y)                                             ;;; -y
       1 (. (System.Drawing.Point. 1 2) -X)                             ;;; java.awt.Point.  -x
       2 (. (System.Drawing.Point. 1 2) -Y)))                           ;;; java.awt.Point.   -y

  ; Classname/staticField
  (are [x] (= x 2147483647)
      Int32/MaxValue                              ;;; Integer/MAX_VALUE
      (. Int32 MaxValue) ))                       ;;; Integer MAX_VALUE

;;;(definterface I (a []))
;;;(deftype T [a] I (a [_] "method"))

;;;(deftest test-reflective-field-name-ambiguous
;;;  (let [t (->T "field")]
;;;    (is (= "method" (. ^T t a)))
;;;    (is (= "field" (. ^T t -a)))
;;;    (is (= "method" (. t a)))
;;;    (is (= "field" (. t -a)))
;;;    (is (thrown? MissingMethodException (. t -BOGUS)))))                                ;;; IllegalArgumentException

(deftest test-double-dot
  (is (=  (.. Environment (GetEnvironmentVariables) (get_Item "Path"))             ;;;  (.. System (getProperties) (get "os.name"))
         (. (. Environment (GetEnvironmentVariables)) (get_Item "Path")))))        ;;;  (. (. System (getProperties) (get "os.name")))))


(deftest test-doto
  (let [m (doto (new System.Collections.Hashtable)           ;;; java.util.HashMap
            (.set_Item "a" 1)                                     ;;; .put
            (.set_Item "b" 2))]
    (are [x y] (= x y)
        (class m) System.Collections.Hashtable               ;;; java.util.HashMap
        {"a" 1 "b" 2} m )))                                  ;;; m {"a" 1 "b" 2}   (the other order does not work at this time)


(deftest test-new
  ;;;  ; Integer                                              ;;; no equivalent
  ;;;(are [expr cls value] (and (= (class expr) cls)
  ;;;                          (= expr value))
  ;;;    (new java.lang.Integer 42) java.lang.Integer 42
  ;;;    (java.lang.Integer. 123) java.lang.Integer 123 )

  ; Date
  (are [x] (= (class x) System.DateTime)          ;;; java.util.Date
      (new System.DateTime)                       ;;; java.util.Date
      (System.DateTime.) ))                       ;;; java.util.Date


(deftest test-instance?
  ; evaluation
  (are [x y] (= x y)
      (instance? Int32 (+ 1 2)) false                   ;;; java.lang.Integer
      (instance? Int64 (+ 1 2)) true )                   ;;; java.lang.Long

  ; different types
  (are [type literal] (instance? literal type)
      1   Int64                                 ;;; java.lang.Long
      1.0 Double                                ;;; java.lang.Double
      1M  BigDecimal                            ;;; java.math.BigDecimal
      \a  Char                                  ;;; java.lang.Character
      "a" String)                               ;;; java.lang.String )

  ; it is a Long, nothing else
  (are [x y] (= (instance? x 42) y)
      Int32 false                    ;;; java.lang.Integer
      Int64 true                     ;;; java.lang.Long
      Char false                     ;;; java.lang.Character
      String false )                 ;;; java.lang.String

  ; test compiler macro
  (is (let [Int64 String] (instance? Int64 "abc")))                     ;;; Long  Long
  (is (thrown? clojure.lang.ArityException (instance? Int64))))         ;;; Long

; set!

(defprotocol p (f [_]))
(deftype t [^:unsynchronized-mutable x] p (f [_] (set! (.x _) 1)))

(deftest test-set!
  (is (= 1 (f (t. 1)))))

; memfn


;;;(deftest test-bean
;;;  (let [b (bean java.awt.Color/black)]
;;;    (are [x y] (= x y)
;;;        (map? b) true
;;;
;;;        (:red b) 0
;;;        (:green b) 0
;;;        (:blue b) 0
;;;        (:RGB b) -16777216
;;;
;;;        (:alpha b) 255
;;;        (:transparency b) 1
;;;
;;;        (:missing b) nil
;;;        (:missing b :default) :default
;;;        (get b :missing) nil
;;;        (get b :missing :default) :default

;;;        (:class b) java.awt.Color )))

;;;(deftest test-iterable-bean
;;; (let [b (bean (java.util.Date.))]
;;;    (is (.iterator ^Iterable b))
;;;    (is (= (into [] b) (into [] (seq b))))
;;;    (is (hash b))))


; proxy, proxy-super

(deftest test-proxy-chain
  (testing "That the proxy functions can chain"
    (are [x y] (= x y)
        (-> (get-proxy-class Object) 
            construct-proxy
            (init-proxy {}) 
            (update-proxy {"ToString" (fn [_] "chain chain chain")})     ;;; toString
            str)
        "chain chain chain"

        (-> (proxy [Object] [] (ToString [] "superfuzz bigmuff"))           ;;; toString
            (update-proxy {"ToString" (fn [_] "chain chain chain")})     ;;; toString
            str)
        "chain chain chain")))

;;;;https://clojure.atlassian.net/browse/CLJ-1973
;;;(deftest test-proxy-method-order
;;;  (let [class-reader (clojure.asm.ClassReader. proxy-examples/proxy1-class-name)
;;;        method-order (atom [])
;;;        method-visitor (proxy [clojure.asm.ClassVisitor] [clojure.asm.Opcodes/ASM4 nil]
;;;                         (visitMethod [access name descriptor signature exceptions]
;;;                           (swap! method-order conj {:name name :descriptor descriptor})
;;;                           nil))
;;;        _ (.accept class-reader method-visitor 0)
;;;        expected [{:name "<init>", :descriptor "()V"}
;;;                  {:name "__initClojureFnMappings", :descriptor "(Lclojure/lang/IPersistentMap;)V"}
;;;                  {:name "__updateClojureFnMappings", :descriptor "(Lclojure/lang/IPersistentMap;)V"}
;;;                  {:name "__getClojureFnMappings", :descriptor "()Lclojure/lang/IPersistentMap;"}
;;;                  {:name "clone", :descriptor "()Ljava/lang/Object;"}
;;;                  {:name "hashCode", :descriptor "()I"}
;;;                  {:name "toString", :descriptor "()Ljava/lang/String;"}
;;;                  {:name "equals", :descriptor "(Ljava/lang/Object;)Z"}
;;;                  {:name "a", :descriptor "(Ljava/io/File;)Z"}
;;;                  {:name "a", :descriptor "(Ljava/lang/Boolean;)Ljava/lang/Object;"}
;;;                  {:name "a", :descriptor "(Ljava/lang/Runnable;)Z"}
;;;                  {:name "a", :descriptor "(Ljava/lang/String;)I"}
;;;                  {:name "b", :descriptor "(Ljava/lang/String;)Ljava/lang/Object;"}
;;;                  {:name "c", :descriptor "(Ljava/lang/String;)Ljava/lang/Object;"}
;;;                  {:name "d", :descriptor "(Ljava/lang/String;)Ljava/lang/Object;"}
;;;                  {:name "a", :descriptor "(Ljava/lang/Boolean;Ljava/lang/String;)I"}
;;;                  {:name "a", :descriptor "(Ljava/lang/String;Ljava/io/File;)Z"}
;;;                  {:name "a", :descriptor "(Ljava/lang/String;Ljava/lang/Runnable;)Z"}
;;;                  {:name "a", :descriptor "(Ljava/lang/String;Ljava/lang/String;)I"}]
;;;        actual @method-order]
;;;    (is (= expected actual)
;;;        (with-out-str (pp/pprint (data/diff expected actual))))))

;; serialized-proxy can be regenerated using a modified version of
;; Clojure with the proxy serialization prohibition disabled and the
;; following code:
;; revert 271674c9b484d798484d134a5ac40a6df15d3ac3 to allow serialization
(comment
  (require 'clojure.inspector)
  (let [baos (java.io.ByteArrayOutputStream.)]
    (with-open [baos baos]
      (.writeObject (java.io.ObjectOutputStream. baos) (clojure.inspector/list-model nil)))
    (prn (vector (System/getProperty "java.specification.version")
                 (.encodeToString (java.util.Base64/getEncoder) (.toByteArray baos))))))

(def serialized-proxies
  {"1.8" "rO0ABXNyAEVjbG9qdXJlLmluc3BlY3Rvci5wcm94eSRqYXZheC5zd2luZy50YWJsZS5BYnN0cmFjdFRhYmxlTW9kZWwkZmYxOTI3NGFydNi2XwhNRQIAAUwADl9fY2xvanVyZUZuTWFwdAAdTGNsb2p1cmUvbGFuZy9JUGVyc2lzdGVudE1hcDt4cgAkamF2YXguc3dpbmcudGFibGUuQWJzdHJhY3RUYWJsZU1vZGVscsvrOK4B/74CAAFMAAxsaXN0ZW5lckxpc3R0ACVMamF2YXgvc3dpbmcvZXZlbnQvRXZlbnRMaXN0ZW5lckxpc3Q7eHBzcgAjamF2YXguc3dpbmcuZXZlbnQuRXZlbnRMaXN0ZW5lckxpc3SxNsZ9hOrWRAMAAHhwcHhzcgAfY2xvanVyZS5sYW5nLlBlcnNpc3RlbnRBcnJheU1hcOM3cA+YxfTfAgACTAAFX21ldGFxAH4AAVsABWFycmF5dAATW0xqYXZhL2xhbmcvT2JqZWN0O3hyABtjbG9qdXJlLmxhbmcuQVBlcnNpc3RlbnRNYXBdfC8DdCByewIAAkkABV9oYXNoSQAHX2hhc2hlcXhwAAAAAAAAAABwdXIAE1tMamF2YS5sYW5nLk9iamVjdDuQzlifEHMpbAIAAHhwAAAABnQADmdldENvbHVtbkNvdW50c3IAJWNsb2p1cmUuaW5zcGVjdG9yJGxpc3RfbW9kZWwkZm5fXzkxNTbQ1M9FYoOj9wIAAHhyABZjbG9qdXJlLmxhbmcuQUZ1bmN0aW9uPgZwnJ5G/csCAAFMABFfX21ldGhvZEltcGxDYWNoZXQAHkxjbG9qdXJlL2xhbmcvTWV0aG9kSW1wbENhY2hlO3hwcHQAC2dldFJvd0NvdW50c3IAJWNsb2p1cmUuaW5zcGVjdG9yJGxpc3RfbW9kZWwkZm5fXzkxNTgf1DHD2//pRAIAAUwABW5yb3dzdAASTGphdmEvbGFuZy9PYmplY3Q7eHEAfgAPcHB0AApnZXRWYWx1ZUF0c3IAJWNsb2p1cmUuaW5zcGVjdG9yJGxpc3RfbW9kZWwkZm5fXzkxNjBYQ6uzEwbd+gIAAkwACWdldF9sYWJlbHEAfgAUTAAJZ2V0X3ZhbHVlcQB+ABR4cQB+AA9wcHA="
 "9" "rO0ABXNyAEVjbG9qdXJlLmluc3BlY3Rvci5wcm94eSRqYXZheC5zd2luZy50YWJsZS5BYnN0cmFjdFRhYmxlTW9kZWwkZmYxOTI3NGFydNi2XwhNRQIAAUwADl9fY2xvanVyZUZuTWFwdAAdTGNsb2p1cmUvbGFuZy9JUGVyc2lzdGVudE1hcDt4cgAkamF2YXguc3dpbmcudGFibGUuQWJzdHJhY3RUYWJsZU1vZGVscsvrOK4B/74CAAFMAAxsaXN0ZW5lckxpc3R0ACVMamF2YXgvc3dpbmcvZXZlbnQvRXZlbnRMaXN0ZW5lckxpc3Q7eHBzcgAjamF2YXguc3dpbmcuZXZlbnQuRXZlbnRMaXN0ZW5lckxpc3SxNsZ9hOrWRAMAAHhwcHhzcgAfY2xvanVyZS5sYW5nLlBlcnNpc3RlbnRBcnJheU1hcOM3cA+YxfTfAgACTAAFX21ldGFxAH4AAVsABWFycmF5dAATW0xqYXZhL2xhbmcvT2JqZWN0O3hyABtjbG9qdXJlLmxhbmcuQVBlcnNpc3RlbnRNYXBdfC8DdCByewIAAkkABV9oYXNoSQAHX2hhc2hlcXhwAAAAAAAAAABwdXIAE1tMamF2YS5sYW5nLk9iamVjdDuQzlifEHMpbAIAAHhwAAAABnQADmdldENvbHVtbkNvdW50c3IAJWNsb2p1cmUuaW5zcGVjdG9yJGxpc3RfbW9kZWwkZm5fXzkxNTbQ1M9FYoOj9wIAAHhyABZjbG9qdXJlLmxhbmcuQUZ1bmN0aW9uPgZwnJ5G/csCAAFMABFfX21ldGhvZEltcGxDYWNoZXQAHkxjbG9qdXJlL2xhbmcvTWV0aG9kSW1wbENhY2hlO3hwcHQAC2dldFJvd0NvdW50c3IAJWNsb2p1cmUuaW5zcGVjdG9yJGxpc3RfbW9kZWwkZm5fXzkxNTgf1DHD2//pRAIAAUwABW5yb3dzdAASTGphdmEvbGFuZy9PYmplY3Q7eHEAfgAPcHB0AApnZXRWYWx1ZUF0c3IAJWNsb2p1cmUuaW5zcGVjdG9yJGxpc3RfbW9kZWwkZm5fXzkxNjBYQ6uzEwbd+gIAAkwACWdldF9sYWJlbHEAfgAUTAAJZ2V0X3ZhbHVlcQB+ABR4cQB+AA9wcHA="
 "10" "rO0ABXNyAEVjbG9qdXJlLmluc3BlY3Rvci5wcm94eSRqYXZheC5zd2luZy50YWJsZS5BYnN0cmFjdFRhYmxlTW9kZWwkZmYxOTI3NGFydNi2XwhNRQIAAUwADl9fY2xvanVyZUZuTWFwdAAdTGNsb2p1cmUvbGFuZy9JUGVyc2lzdGVudE1hcDt4cgAkamF2YXguc3dpbmcudGFibGUuQWJzdHJhY3RUYWJsZU1vZGVscsvrOK4B/74CAAFMAAxsaXN0ZW5lckxpc3R0ACVMamF2YXgvc3dpbmcvZXZlbnQvRXZlbnRMaXN0ZW5lckxpc3Q7eHBzcgAjamF2YXguc3dpbmcuZXZlbnQuRXZlbnRMaXN0ZW5lckxpc3SRSMwtc98O3gMAAHhwcHhzcgAfY2xvanVyZS5sYW5nLlBlcnNpc3RlbnRBcnJheU1hcOM3cA+YxfTfAgACTAAFX21ldGFxAH4AAVsABWFycmF5dAATW0xqYXZhL2xhbmcvT2JqZWN0O3hyABtjbG9qdXJlLmxhbmcuQVBlcnNpc3RlbnRNYXBdfC8DdCByewIAAkkABV9oYXNoSQAHX2hhc2hlcXhwAAAAAAAAAABwdXIAE1tMamF2YS5sYW5nLk9iamVjdDuQzlifEHMpbAIAAHhwAAAABnQADmdldENvbHVtbkNvdW50c3IAJWNsb2p1cmUuaW5zcGVjdG9yJGxpc3RfbW9kZWwkZm5fXzkxNTbQ1M9FYoOj9wIAAHhyABZjbG9qdXJlLmxhbmcuQUZ1bmN0aW9uPgZwnJ5G/csCAAFMABFfX21ldGhvZEltcGxDYWNoZXQAHkxjbG9qdXJlL2xhbmcvTWV0aG9kSW1wbENhY2hlO3hwcHQAC2dldFJvd0NvdW50c3IAJWNsb2p1cmUuaW5zcGVjdG9yJGxpc3RfbW9kZWwkZm5fXzkxNTgf1DHD2//pRAIAAUwABW5yb3dzdAASTGphdmEvbGFuZy9PYmplY3Q7eHEAfgAPcHB0AApnZXRWYWx1ZUF0c3IAJWNsb2p1cmUuaW5zcGVjdG9yJGxpc3RfbW9kZWwkZm5fXzkxNjBYQ6uzEwbd+gIAAkwACWdldF9sYWJlbHEAfgAUTAAJZ2V0X3ZhbHVlcQB+ABR4cQB+AA9wcHA="
 "11" "rO0ABXNyAEVjbG9qdXJlLmluc3BlY3Rvci5wcm94eSRqYXZheC5zd2luZy50YWJsZS5BYnN0cmFjdFRhYmxlTW9kZWwkZmYxOTI3NGFydNi2XwhNRQIAAUwADl9fY2xvanVyZUZuTWFwdAAdTGNsb2p1cmUvbGFuZy9JUGVyc2lzdGVudE1hcDt4cgAkamF2YXguc3dpbmcudGFibGUuQWJzdHJhY3RUYWJsZU1vZGVscsvrOK4B/74CAAFMAAxsaXN0ZW5lckxpc3R0ACVMamF2YXgvc3dpbmcvZXZlbnQvRXZlbnRMaXN0ZW5lckxpc3Q7eHBzcgAjamF2YXguc3dpbmcuZXZlbnQuRXZlbnRMaXN0ZW5lckxpc3SRSMwtc98O3gMAAHhwcHhzcgAfY2xvanVyZS5sYW5nLlBlcnNpc3RlbnRBcnJheU1hcOM3cA+YxfTfAgACTAAFX21ldGFxAH4AAVsABWFycmF5dAATW0xqYXZhL2xhbmcvT2JqZWN0O3hyABtjbG9qdXJlLmxhbmcuQVBlcnNpc3RlbnRNYXBdfC8DdCByewIAAkkABV9oYXNoSQAHX2hhc2hlcXhwAAAAAAAAAABwdXIAE1tMamF2YS5sYW5nLk9iamVjdDuQzlifEHMpbAIAAHhwAAAABnQADmdldENvbHVtbkNvdW50c3IAJWNsb2p1cmUuaW5zcGVjdG9yJGxpc3RfbW9kZWwkZm5fXzkxNTbQ1M9FYoOj9wIAAHhyABZjbG9qdXJlLmxhbmcuQUZ1bmN0aW9uPgZwnJ5G/csCAAFMABFfX21ldGhvZEltcGxDYWNoZXQAHkxjbG9qdXJlL2xhbmcvTWV0aG9kSW1wbENhY2hlO3hwcHQAC2dldFJvd0NvdW50c3IAJWNsb2p1cmUuaW5zcGVjdG9yJGxpc3RfbW9kZWwkZm5fXzkxNTgf1DHD2//pRAIAAUwABW5yb3dzdAASTGphdmEvbGFuZy9PYmplY3Q7eHEAfgAPcHB0AApnZXRWYWx1ZUF0c3IAJWNsb2p1cmUuaW5zcGVjdG9yJGxpc3RfbW9kZWwkZm5fXzkxNjBYQ6uzEwbd+gIAAkwACWdldF9sYWJlbHEAfgAUTAAJZ2V0X3ZhbHVlcQB+ABR4cQB+AA9wcHA="
 "12" "rO0ABXNyAEVjbG9qdXJlLmluc3BlY3Rvci5wcm94eSRqYXZheC5zd2luZy50YWJsZS5BYnN0cmFjdFRhYmxlTW9kZWwkZmYxOTI3NGFydNi2XwhNRQIAAUwADl9fY2xvanVyZUZuTWFwdAAdTGNsb2p1cmUvbGFuZy9JUGVyc2lzdGVudE1hcDt4cgAkamF2YXguc3dpbmcudGFibGUuQWJzdHJhY3RUYWJsZU1vZGVscsvrOK4B/74CAAFMAAxsaXN0ZW5lckxpc3R0ACVMamF2YXgvc3dpbmcvZXZlbnQvRXZlbnRMaXN0ZW5lckxpc3Q7eHBzcgAjamF2YXguc3dpbmcuZXZlbnQuRXZlbnRMaXN0ZW5lckxpc3SRSMwtc98O3gMAAHhwcHhzcgAfY2xvanVyZS5sYW5nLlBlcnNpc3RlbnRBcnJheU1hcOM3cA+YxfTfAgACTAAFX21ldGFxAH4AAVsABWFycmF5dAATW0xqYXZhL2xhbmcvT2JqZWN0O3hyABtjbG9qdXJlLmxhbmcuQVBlcnNpc3RlbnRNYXBdfC8DdCByewIAAkkABV9oYXNoSQAHX2hhc2hlcXhwAAAAAAAAAABwdXIAE1tMamF2YS5sYW5nLk9iamVjdDuQzlifEHMpbAIAAHhwAAAABnQADmdldENvbHVtbkNvdW50c3IAJWNsb2p1cmUuaW5zcGVjdG9yJGxpc3RfbW9kZWwkZm5fXzk0ODSK6FCjrbDduAIAAHhyABZjbG9qdXJlLmxhbmcuQUZ1bmN0aW9uPgZwnJ5G/csCAAFMABFfX21ldGhvZEltcGxDYWNoZXQAHkxjbG9qdXJlL2xhbmcvTWV0aG9kSW1wbENhY2hlO3hwcHQAC2dldFJvd0NvdW50c3IAJWNsb2p1cmUuaW5zcGVjdG9yJGxpc3RfbW9kZWwkZm5fXzk0ODZ7gA7CIBYdJAIAAUwABW5yb3dzdAASTGphdmEvbGFuZy9PYmplY3Q7eHEAfgAPcHB0AApnZXRWYWx1ZUF0c3IAJWNsb2p1cmUuaW5zcGVjdG9yJGxpc3RfbW9kZWwkZm5fXzk0ODiLldew+D3/eAIAAkwACWdldF9sYWJlbHEAfgAUTAAJZ2V0X3ZhbHVlcQB+ABR4cQB+AA9wcHA="
  })

(defn- decode-base64
  [^String s]
  (.GetString System.Text.Encoding/UTF8 (System.Convert/FromBase64String s)))     ;;; (.decode (Base64/getDecoder) s)

(deftest test-proxy-non-serializable
  (testing "That proxy classes refuse serialization and deserialization"
    ;; Serializable listed directly in interface list:
    (is (thrown? System.Runtime.Serialization.SerializationException                                                     ;;;  java.io.NotSerializableException
                 (let [formatter (System.Runtime.Serialization.Formatters.Binary.BinaryFormatter.)]                      ;;; (-> (java.io.ByteArrayOutputStream.)
                    (.Serialize formatter (System.IO.MemoryStream.)                                                      ;;;     (java.io.ObjectOutputStream.)
                                          (proxy [Object System.Runtime.Serialization.ISerializable] [])))))             ;;;     (.writeObject (proxy [Object java.io.Serializable] [])))
    ;; Serializable included via inheritence:
    #_(is (thrown? java.io.NotSerializableException
                 (-> (java.io.ByteArrayOutputStream.)
                     (java.io.ObjectOutputStream.)
                     (.writeObject (clojure.inspector/list-model nil)))))
    ;; Deserialization also prohibited:          
    #_(let [java-version (System/getProperty "java.specification.version")      ;;; DM -- Added commenting out -- I don't feel like taking the time to reproduce this right now
          serialized-proxy (get serialized-proxies java-version)]
      (if serialized-proxy
        (is (thrown? java.io.NotSerializableException
                     (-> serialized-proxy
                         decode-base64
                         java.io.ByteArrayInputStream. java.io.ObjectInputStream.
                         .readObject)))
        (println "WARNING: Missing serialized proxy for Java" java-version "in test/clojure/test_clojure/java_interop.clj")))))

(deftest test-bases
  (are [x] (nil? (bases x))
    System.Object          ;; no super classes/interfaces                             ;;; java.lang.Object 
    System.IComparable)    ;; no super interfaces                                     ;;;java.lang.Comparable
  (are [x y] (set/subset? (set y) (set x))
    (bases System.Math) [System.Object]                                               ;;; java.lang.Math   java.lang.Object
    (bases System.Collections.ICollection) [System.Collections.IEnumerable]           ;;; java.util.Collection java.lang.Iterable
    (bases System.Int32) [System.ValueType System.IComparable System.IFormattable]))  ;;; java.lang.Integer java.lang.Number java.lang.Comparable

(deftest test-supers
  (are [x y] (set/subset? y (set x))
      (supers System.Math)                                      ;;; java.lang.Math)
        #{System.Object}                                        ;;; java.lang.Object}
      #_(supers System.Int32)                                     ;;; java.lang.Integer)
        #_#{System.IFormattable System.IConvertible System.IComparable |System.IEquatable`1[System.Int32]| |System.IComparable`1[System.Int32]|     ;;; java.lang.Number java.lang.Object
		System.Object System.ValueType}   ))                     ;;; java.lang.Comparable java.io.Serializable} ))

(deftest test-proxy-super
  (let [d (proxy [System.Collections.ArrayList] [[1 2 3]]                                ;;; java.util.BitSet  []
            (IndexOf [value startIndex]                                                      ;;; flip [bitIndex]
              (try
                (proxy-super IndexOf value startIndex)                                       ;;; (proxy-super flip bitIndex)
                (catch ArgumentOutOfRangeException e                             ;;; IndexOutOfBoundsException
                  (throw (ArgumentException. "replaced"))))))]                   ;;; IllegalArgumentException
    ;; normal call
    (is (zero? (.IndexOf d 1 0)))                                                     ;;; (nil? (.flip d 0))
    ;; exception should use proxied form and return IllegalArg
    (is (thrown? ArgumentException (.IndexOf d 1 -1)))                               ;;; (.flip d -1) IllegalArgumentException
    ;; same behavior on second call
    (is (thrown? ArgumentException (.IndexOf d 1 -1)))))                             ;;; (.flip d -1) IllegalArgumentException

; Arrays: [alength] aget aset [make-array to-array into-array to-array-2d aclone]
;   [float-array, int-array, etc]
;   amap, areduce

;; http://dev.clojure.org/jira/browse/CLJ-1657
(deftest test-proxy-abstract-super
  (let [p (proxy [System.IO.Stream] [])]                            ;;; java.io.Writer
    (is (thrown? NotImplementedException (.Write p nil 1 1)))))     ;;; UnsupportedOperationException  (.close p)

(defmacro deftest-type-array [type-array type]
  `(deftest ~(symbol (str "test-" type-array))
      ; correct type
      #_(is (= (class (first (~type-array [1 2]))) (class (~type 1))))

      ; given size (and empty)
      (are [x] (and (= (alength (~type-array x)) x)
                (= (vec (~type-array x)) (repeat x 0)))
          0 1 5 )

      ; copy of a sequence
      (are [x] (and (= (alength (~type-array x)) (count x))
                    (= (vec (~type-array x)) x))
          []    
          [1]
          [1 -2 3 0 5] )

      ; given size and init-value
      (are [x] (and (= (alength (~type-array x 42)) x)
                    (= (vec (~type-array x 42)) (repeat x 42)))
          0 1 5 )

      ; given size and init-seq
      (are [x y z] (and (= (alength (~type-array x y)) x)
                        (= (vec (~type-array x y)) z))
          0 [] []
          0 [1] []
          0 [1 2 3] []
          1 [] [0]
          1 [1] [1]
          1 [1 2 3] [1]
          5 [] [0 0 0 0 0]
          5 [1] [1 0 0 0 0]
          5 [1 2 3] [1 2 3 0 0]
          5 [1 2 3 4 5] [1 2 3 4 5]
          5 [1 2 3 4 5 6 7] [1 2 3 4 5] )))

(deftest-type-array int-array int)
(deftest-type-array long-array long)
; todo. fix, text broken for float/doube, should compare to 1.0 2.0 etc
#_(deftest-type-array float-array float)
#_(deftest-type-array double-array double)

; separate test for exceptions (doesn't work with above macro...)
(deftest test-type-array-exceptions
  (are [x] (thrown? OverflowException x)               ;;; NegativeArraySizeException
       (int-array -1)
       (long-array -1)
       (float-array -1)
       (double-array -1) ))


(deftest test-make-array
  ; negative size
  (is (thrown? ArgumentOutOfRangeException (make-array Int32 -1)))   ;;; NegativeArraySizeException Integer

  ; one-dimensional
  (are [x] (= (alength (make-array Int32 x)) x)      ;;; Integer
      0 1 5 )

  (let [a (make-array Int64 5)]      ;;; Long
    (aset a 3 42)
    (are [x y] (= x y)
        (aget a 3) 42
        (class (aget a 3)) Int64 ))      ;;; Long
      
  ; multi-dimensional
  (let [a (make-array Int64 3 2 4)]      ;;; Long
    (aset a 0 1 2 987)
    (are [x y] (= x y)
        (alength a) 3
        (alength (first a)) 2
        (alength (first (first a))) 4

        (aget a 0 1 2) 987
        (class (aget a 0 1 2)) Int64 )))      ;;; Long


(deftest test-to-array
  (let [v [1 "abc" :kw \c []]
        a (to-array v)]
    (are [x y] (= x y)
        ; length
        (alength a) (count v)

        ; content
        (vec a) v
        (class (aget a 0)) (class (nth v 0))
        (class (aget a 1)) (class (nth v 1))
        (class (aget a 2)) (class (nth v 2))
        (class (aget a 3)) (class (nth v 3))
        (class (aget a 4)) (class (nth v 4)) ))

  ; different kinds of collections
  (are [x] (and (= (alength (to-array x)) (count x))
                (= (vec (to-array x)) (vec x)))
      ()
      '(1 2)
      []
      [1 2]
      (sorted-set)
      (sorted-set 1 2)
      
      (int-array 0)
      (int-array [1 2 3])

      (to-array [])
      (to-array [1 2 3]) ))

(defn queue [& contents]
  (apply conj (clojure.lang.PersistentQueue/EMPTY) contents))

#_(defn array-typed-equals [expected actual]
  (and (= (class expected) (class actual))
       (java.util.Arrays/equals expected actual)))

#_(defmacro test-to-passed-array-for [collection-type]
  `(deftest ~(symbol (str "test-to-passed-array-for-" collection-type))
     (let [string-array# (make-array String 5)
           shorter# (~collection-type "1" "2" "3")
           same-length# (~collection-type "1" "2" "3" "4" "5")
           longer# (~collection-type "1" "2" "3" "4" "5" "6")]
       (are [expected actual] (array-typed-equals expected actual)
            (into-array String ["1" "2" "3" nil nil]) (.toArray shorter# string-array#)
            (into-array String ["1" "2" "3" "4" "5"]) (.toArray same-length# string-array#)
            (into-array String ["1" "2" "3" "4" "5" "6"]) (.toArray longer# string-array#)))))

;; Irrelevant for CLR -- CopyArray blows up on shorter destination, no creation of new destination
#_(test-to-passed-array-for vector)
#_(test-to-passed-array-for list)
;;(test-to-passed-array-for hash-set)
#_(test-to-passed-array-for queue)

(deftest test-into-array
  ; compatible types only
  (is (thrown? InvalidCastException (into-array [1 "abc" :kw])))          ;;; IllegalArgumentException
  ;;;(is (thrown? InvalidCastException (into-array [1.2 4])))                ;;; IllegalArgumentException -- works okay for me
  ;;;(is (thrown? ArgumentException (into-array [(byte 2) (short 3)])))   ;;; IllegalArgumentException -- works okay for me
  (is (thrown? ArgumentException (into-array Byte [100000000000000])))   ;;; IllegalArgumentException  Byte/Type

  ; simple case
  (let [v [1 2 3 4 5]
        a (into-array v)]
    (are [x y] (= x y)
        (alength a) (count v)
        (vec a) v
        (class (first a)) (class (first v)) ))
 
  (is (= \a (aget (into-array Char [\a \b \c]) 0)))                 ;;; Character/TYPE

  (is (= [nil 1 2] (seq (into-array [nil 1 2]))))
  
  (let [types [Int32              ;;; Integer/TYPE
               Byte               ;;; Byte/TYPE
               Single             ;;; Float/TYPE
               Int16              ;;; Short/TYPE
               Double             ;;; Double/TYPE
               Int64]             ;;; Long/TYPE]
        values [(byte 2) (short 3) (int 4) 5]]
    (for [t types]
      (let [a (into-array t values)]
        (is (== (aget a 0) 2))
        (is (== (aget a 1) 3))
        (is (== (aget a 2) 4))
        (is (== (aget a 3) 5)))))
   
  ; different kinds of collections
  (are [x] (and (= (alength (into-array x)) (count x))
                (= (vec (into-array x)) (vec x))
                (= (alength (into-array Int64 x)) (count x))          ;;; Long/TYPE
                (= (vec (into-array Int64 x)) (vec x)))               ;;; Long/TYPE
      ()
      '(1 2)
      []
      [1 2]
      (sorted-set)
      (sorted-set 1 2)

      (int-array 0)
      (int-array [1 2 3])

      (to-array [])
      (to-array [1 2 3]) ))


(deftest test-to-array-2d
  ; needs to be a collection of collection(s)
  (is (thrown? Exception (to-array-2d [1 2 3])))

  ; ragged array
  (let [v [[1] [2 3] [4 5 6]]
        a (to-array-2d v)]
    (are [x y] (= x y)
        (alength a) (count v)
        (alength (aget a 0)) (count (nth v 0))
        (alength (aget a 1)) (count (nth v 1))
        (alength (aget a 2)) (count (nth v 2))

        (vec (aget a 0)) (nth v 0)
        (vec (aget a 1)) (nth v 1)
        (vec (aget a 2)) (nth v 2) ))

  ; empty array
  (let [a (to-array-2d [])]
    (are [x y] (= x y)
        (alength a) 0
        (vec a) [] )))


(deftest test-alength
  (are [x] (= (alength x) 0)
      (int-array 0)
      (long-array 0)
      (float-array 0)
      (double-array 0)
      (boolean-array 0)
      (byte-array 0)
      (char-array 0)
      (short-array 0)
      (make-array Int32 0)  ;;;(make-array Integer/TYPE 0)
      (to-array [])
      (into-array [])
      (to-array-2d []) )

  (are [x] (= (alength x) 1)
      (int-array 1)
      (long-array 1)
      (float-array 1)
      (double-array 1)
      (boolean-array 1)
      (byte-array 1)
      (char-array 1)
      (short-array 1)
      (make-array Int32 1)  ;;;(make-array Integer/TYPE 1)
      (to-array [1])
      (into-array [1])
      (to-array-2d [[1]]) )

  (are [x] (= (alength x) 3)
      (int-array 3)
      (long-array 3)
      (float-array 3)
      (double-array 3)
      (boolean-array 3)
      (byte-array 3)
      (char-array 3)
      (short-array 3)
      (make-array Int32 3)  ;;;(make-array Integer/TYPE 3)
      (to-array [1 "a" :k])
      (into-array [1 2 3])
      (to-array-2d [[1] [2 3] [4 5 6]]) ))


(deftest test-aclone
  ; clone all arrays except 2D
  (are [x] (and (= (alength (aclone x)) (alength x))
                (= (vec (aclone x)) (vec x)))
      (int-array 0)
      (long-array 0)
      (float-array 0)
      (double-array 0)
      (boolean-array 0)
      (byte-array 0)
      (char-array 0)
      (short-array 0)
      (make-array Int32 0)  ;;;(make-array Integer/TYPE 0)
      (to-array [])
      (into-array [])

      (int-array [1 2 3])
      (long-array [1 2 3])
      (float-array [1 2 3])
      (double-array [1 2 3])
      (boolean-array [true false])
      (byte-array [(byte 1) (byte 2)])
      (byte-array [1 2])
      (byte-array 2 [1 2])
      (char-array [\a \b \c])
      (short-array [(short 1) (short 2)])
      (short-array [1 2])
      (short-array 2 [1 2])
      (make-array Int32 3)  ;;;(make-array Integer/TYPE 3)
      (to-array [1 "a" :k])
      (into-array [1 2 3]) )

  ; clone 2D
  (are [x] (and (= (alength (aclone x)) (alength x))
                (= (map alength (aclone x)) (map alength x))
                (= (map vec (aclone x)) (map vec x)))
      (to-array-2d [])
      (to-array-2d [[1] [2 3] [4 5 6]]) ))


; Type Hints, *warn-on-reflection*
;   ^ints, ^floats, ^longs, ^doubles

; Coercions: [int, long, float, double, char, boolean, short, byte]
;   num
;   ints/longs/floats/doubles

(deftest test-boolean
  (are [x y] (and (instance? System.Boolean (boolean x))            ;;; java.lang.Boolean
                  (= (boolean x) y))
      nil false
      false false
      true true

      0 true
      1 true
      () true
      [1] true

      "" true
      \space true
      :kw true ))


(deftest test-char
  ; int -> char
  (is (instance? System.Char (char 65)))               ;;; java.lang.Character

  ; char -> char
  (is (instance? System.Char (char \a)))               ;;; java.lang.Character
  (is (= (char \a) \a)))

;; Note: More coercions in numbers.clj

; Test that primitive boxing elision in statement context works
; correctly (CLJ-2621)

(defn inc-atomic-int [^AtomicInteger l]
  (.incrementAndGet l)
  nil)

(defn inc-atomic-long [^AtomicLong l]
  (.incrementAndGet l)
  nil)

(deftest test-boxing-prevention-when-compiling-statements
  (is (= 1 (.get (doto (AtomicInteger. 0) inc-atomic-int))))
  (is (= 1 (.get (doto (AtomicLong. 0) inc-atomic-long)))))