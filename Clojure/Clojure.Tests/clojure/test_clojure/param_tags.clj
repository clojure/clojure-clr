;   Copyright (c) Rich Hickey. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.
; Authors: Fogus

(ns clojure.test-clojure.param-tags
  (:use clojure.test)
  (:require
    [clojure.string :as str]
    [clojure.reflect :as r]
    [clojure.test-helper :refer [should-not-reflect]])
  (:import
    #_(clojure.test SwissArmy ConcreteClass)
    (clojure.lang #_Tuple Compiler Compiler+CompilerException)            ;;; Compiler$CompilerException  +  commented out Tuple (overlap with System.Tuple)
    (System.Globalization CultureInfo)
    (System.Text Encoding)))                                              ;;; (java.util Arrays UUID Locale)

(assembly-load-from "Clojure.Tests.Support.dll")
(import '[clojure.test SwissArmy])

(set! *warn-on-reflection* true)

(deftest no-hints-with-param-tags
  (should-not-reflect
   (defn touc-no-reflect [s]
     (^[] String/.ToUpper s)))                                                                                        ;;; .toUpperCase
  (should-not-reflect
   (defn touc-no-reflectq [s]
     (^[] System.String/.ToUpper s)))                                                                                 ;;; .toUpperCase
  (should-not-reflect
   (defn touc-no-reflect-arg-tags [s]
     (^[System.Globalization.CultureInfo] String/.ToUpper s System.Globalization.CultureInfo/CurrentCulture)))        ;;; java.util.Locale]  .toUpperCase  java.util.Locale/ENGLISH
  (should-not-reflect
   (defn no-overloads-no-reflect [v]
     (DateTimeOffset/.Year v))))                                                                                      ;;; java.time.OffsetDateTime/.getYear

#_(deftest no-param-tags-use-qualifier
  ;; both Date and OffsetDateTime have .getYear - want to show here the qualifier is used
  (let [f (fn [^java.util.Date d] (java.time.OffsetDateTime/.getYear d))                                         
        date (java.util.Date. 1714495523100)]
    ;; works when passed OffsetDateTime
    (is (= 2024 (f (-> date .toInstant (.atOffset java.time.ZoneOffset/UTC)))))

    ;; fails when passed Date, expects OffsetDateTime
    (is (thrown? ClassCastException
          (f date)))))

(deftest param-tags-in-invocation-positions
  (testing "qualified static method invocation"
    (is (= 3 (^[long] Math/Abs -3)))                                                                        ;;; Math/abs
    (is (= [1 2] (^[_ _] clojure.lang.Tuple/create 1 2)))                                                   ;;; Tuple/creat
    #_(is (= "42" (Long/toString 42))))                                                                     ;;; THis doesn't have param-tags, so not sure what the point is.
  #_(testing "qualified ctor invocation"                                                                    ;;; Can't match against CLR types, so we'll roll our own below.
    (is (= (^[long long] UUID/new 1 2) #uuid "00000000-0000-0001-0000-000000000002"))
    (is (= (^[long long] java.util.UUID/new 1 2) #uuid "00000000-0000-0001-0000-000000000002"))
    (is (= "a" (^[String] String/new "a"))))
  (testing "qualified ctor invocation"
    (is (= (^[char int] String/new \a 2) "aa"))
    (is (= (^[char int] System.String/new \a 2) "aa"))
    (is (= (^[chars] System.String/new (String/.ToCharArray "abc")) "abc")))
  (testing "qualified instance method invocation"
    (is (= \A (String/.get_Chars "A" 0)))                                                                    ;;; String/.charAt
    (is (= "A" (^[System.Globalization.CultureInfo] String/.ToUpper "a" CultureInfo/InvariantCulture)))      ;;; (^[java.util.Locale] String/.toUpperCase "a" java.util.Locale/ENGLISH)
    (is (= "A" (^[CultureInfo] String/.ToUpper "a" CultureInfo/InvariantCulture)))                           ;;; (^[Locale] String/.toUpperCase "a" java.util.Locale/ENGLISH)
    (is (= 65 (aget (^[String] Encoding/.GetBytes Encoding/UTF8 "A") 0)))                                    ;;; (^[String] String/.getBytes "A" "US-ASCII")
    (is (= "42" (^[] Int64/.ToString 42))))                                                                  ;;; (^[] Long/.toString 42)
  #_(testing "string repr array type resolutions"                                                            ;;; no equivalent in CLR
     (let [lary (long-array [1 2 3 4 99 100])
           oary (into-array [1 2 3 4 99 100])
           sary (into-array String ["a" "b" "c"])]
       (is (= 4 (^[longs long] Arrays/binarySearch lary (long 99))))
       (is (= 4 (^[objects _] Arrays/binarySearch oary 99)))
       (is (= 4 (^["[Ljava.lang.Object;" _] Arrays/binarySearch oary 99)))
       (is (= 1 (^["[Ljava.lang.Object;" _] Arrays/binarySearch sary "b")))))
  (testing "bad method names"
    (is (thrown? Exception (eval '(^[] System.String/foo "a"))))                                             ;;; java.lang.String
    (is (thrown? Exception (eval '(^[] System.String/.foo "a"))))                                            ;;; java.lang.String
    (is (thrown? Exception (eval '(^[] Math/new "a"))))))


;; Mapping of symbols returned from reflect call to :parameter-type used as arguments to .getDeclaredMethod,
;; :arg-type used as arguments to the methods and constructors being tested, :arg-tag used as arg-tags
;; to the methods and constructors being tested.
(def reflected-parameter-types {'System.Int32 {:parameter-type Int32                                              ;;;    'int Integer/TYPE
                                      :arg-type "(int 42)"
                                      :arg-tag "int"}
                                 'System.Boolean {:parameter-type Boolean                                         ;;; 'boolean Boolean/TYPE
                                          :arg-type "true"
                                          :arg-tag "boolean"}
                                'System.Int64 {:parameter-type Int64                                              ;;; 'long  Long/TYPE
                                       :arg-type "42"
                                       :arg-tag "long"}
                                '|System.Int64[]|  {:parameter-type |System.Int64[]|                              ;;; 'long<> (Class/forName "[J")
                                         :arg-type "(long-array [1 2])"
                                         :arg-tag "longs"}                                                        ;;; "long*"
                                ;;;'int<><> {:parameter-type (Class/forName "[[I")
                                ;;;          :arg-type "(make-array Integer/TYPE 1 2)"
                                ;;;          :arg-tag "int**"}
                                 '|System.Object[]| {:parameter-type |System.Object[]|                            ;;; 'System.Object<> java.lang.Object<>  (Class/forName "[Ljava.lang.Object;")
                                                     :arg-type "(into-array Object [1 2])"                        ;;;  "(into-array [1 2])"
                                                     :arg-tag "|System.Object[]|"}                                ;;; "\"[Ljava.lang.Object;\""
                                '|System.String[]| {:parameter-type |System.String[]|                             ;;;  'System.String<> java.lang.String<> (Class/forName "[Ljava.lang.String;")
                                                     :arg-type "(into-array [\"a\" \"b\"])"
                                                     :arg-tag "|System.String[]|"}})                              ;;;  "\"[Ljava.lang.String;\"

(defn is-static-method? [class method-name params]  
  (let [method (.GetMethod ^Type class ^String (name method-name)  ^|System.Type[]| params)]                      ;;;  getDeclaredMethod ^Class ^"[Ljava.lang.Object;"
    (.IsStatic method)))                                                                                          ;;; (java.lang.reflect.Modifier/isStatic (.getModifiers method))

(defn get-methods
  "Reflect the class located at `path`, filter out the public members, add a :type
   of :constructor, :static, or :instance to each."
  [path]
  (let [reflected-class (r/reflect (resolve path))
        public (filter #(contains? (:flags %) :public) (:members reflected-class))]
    (reduce (fn [res m]
              (let [class (-> m :declaring-class resolve)
                    params (into-array Type (map #(-> % reflected-parameter-types :parameter-type) (:parameter-types m)))]      ;;;  Class
                (cond
                  (not (contains? m :return-type)) (conj res (assoc m :type :constructor))
                  (is-static-method? class (:name m) params) (conj res (assoc m :type :static))
                  :else (conj res (assoc m :type :instance)))))
            [] public)))

(defn exercise-constructor
  "Provided a map of data returned from a call to reflect representing a constructor.
   Construct a new instance of the class providing the appropriate arg-tags and return
   a map containing the new instance and expected target class"
  [{:keys [declaring-class parameter-types] :as m}]
  (let [target-class (-> declaring-class str clojure.lang.RT/classForName)                                     ;;; Class/forName
        args (str/join " " (map #(-> % reflected-parameter-types :arg-type) parameter-types))
        arg-tags (str/join " " (map #(-> % reflected-parameter-types :arg-tag) parameter-types))
        fun-call-str (read-string (str "(^[" arg-tags "] " declaring-class ". " args ")"))
        _ (should-not-reflect #(eval 'fun-call-str))
        new-instance (eval fun-call-str)]
    {:expected target-class :actual new-instance}))

(defn exercise-static-method
  "Provided a map of data returned from a call to reflect representing a static class method.
   Call the static method providing the appropriate arg-tags and return a map containing
   the actual and expected response."
  [{:keys [name declaring-class parameter-types]}]
  (let [class (str declaring-class)
        method (str name)
        args (str/join " " (map #(-> % reflected-parameter-types :arg-type) parameter-types))
        arg-tags (str/join " " (map #(-> % reflected-parameter-types :arg-tag) parameter-types))
        expected-response (str/join "-" parameter-types)
        fun-call-str (read-string (str "(^[" arg-tags "] " class "/" method " " args ")"))
        _ (should-not-reflect #(eval 'fun-call-str))
        response (eval fun-call-str)]
    {:expected expected-response :actual response}))

(defn exercise-instance-method
  "Provided a map of data returned from a call to reflect representing a class instance method.
   Call the method providing the appropriate arg-tags and return a map containing
   the actual and expected response."
  [{:keys [name declaring-class parameter-types]}]
  (let [method (str "." name)
        args (str/join " " (map #(-> % reflected-parameter-types :arg-type) parameter-types))
        arg-tags (str/join " " (map #(-> % reflected-parameter-types :arg-tag) parameter-types))
        expected-response (str/join "-" parameter-types)
        fun-call-str (read-string (str "(^[" arg-tags "] " declaring-class "/" method " " "(" declaring-class ".)" " " args ")"))
        _ (should-not-reflect #(eval 'fun-call-str))
        response (eval fun-call-str)]
    {:expected expected-response :actual response}))

(deftest arg-tags-in-constructors-and-static-and-instance-methods
  (doseq [m (get-methods 'clojure.test.SwissArmy)]
    (case (:type m)
      :constructor (let [{:keys [expected actual]} (exercise-constructor m)]
                     (is (instance? expected actual)))
      :static (let [{:keys [expected actual]} (exercise-static-method m)]
                (is (= expected actual)))
      :instance (let [{:keys [expected actual]} (exercise-instance-method m)]
                  (is (= expected actual))))))

(defmacro arg-tags-called-in-macro
  [a-type b-type a b]
  `(^[~a-type ~b-type] SwissArmy/staticArityOverloadMethod ~a ~b))

(deftest arg-tags-in-macro
  (is (= "System.Int32-System.Int32" (arg-tags-called-in-macro int int 1 2))))                      ;;; "int-int"

#_(deftest bridge-methods                                                                           ;;; no concept of bridge methods
  (testing "Allows correct intended usage."
    (let [concrete (ConcreteClass.)]
     (is (= 42 (^[Integer] ConcreteClass/.stampWidgets concrete (int 99))))))
  (testing "Will not call bridge method."
    (is (thrown? Compiler$CompilerException
                 (eval '(let [concrete (clojure.test.ConcreteClass.)]
                          (^[Object] ConcreteClass/.stampWidgets concrete (int 99))))))))


(deftest incorrect-arity-invocation-error-messages

  (testing "Invocation with param-tags having incorrect number of args"
    (let [e (try
              (eval '(^[long] Math/Abs -1 -2 -3))                                                              ;;; Math/abs
              (catch Compiler+CompilerException e (str "-> " (.Message (.InnerException e)))))]                ;;; Compiler$CompilerException .getMessage  .getCause
      (is (not (nil? (re-find #"expected 1.*received 3" e))) "Error message was expected to indicate 1 argument was expected but 2 were provided"))))