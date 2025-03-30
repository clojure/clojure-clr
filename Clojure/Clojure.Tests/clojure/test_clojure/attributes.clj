;   Copyright (c) Rich Hickey. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.

;;  Original authors: Stuart Halloway, Rich Hickey

(ns clojure.test-clojure.attributes
  (:use clojure.test))

(assembly-load-from "Clojure.Tests.Support.dll")
(import '[Clojure.Tests.Support AnAttribute AnotherAttribute])

(definterface Foo (foo []))

(deftype ^{ObsoleteAttribute "abc"
           AnotherAttribute 7
		   AnAttribute #{ "def" 
                          { :__args ["ghi"] :SecondaryValue "jkl" }}}
   Bar [^int a
        ^{ :tag int
		    NonSerializedAttribute {}
            ObsoleteAttribute "abc"}
		b]

Foo (^{ObsoleteAttribute "abc"
       AnotherAttribute 7
	   AnAttribute #{ "def" 
                      { :__args ["ghi"] :SecondaryValue "jkl" }}}
     foo [this] 42))

(defn get-custom-attributes [x]
  (.GetCustomAttributes x false))

(defn attribute->map [attr]
  (cond 
	(instance? NonSerializedAttribute attr) {:type NonSerializedAttribute}
	(instance? ObsoleteAttribute attr) {:type ObsoleteAttribute :message (.Message attr)}
	(instance? AnotherAttribute attr) {:type AnotherAttribute :primary (.PrimaryValue attr)}
	(instance? AnAttribute attr) {:type AnAttribute :primary (.PrimaryValue attr) :secondary (.SecondaryValue attr)}
	:else {:type (class attr)}))


(def expected-attributes
 #{ {:type ObsoleteAttribute :message "abc"}
	{:type AnotherAttribute :primary 7}
	{:type AnAttribute :primary "def" :secondary nil}
	{:type AnAttribute :primary "ghi" :secondary "jkl"}})

(def expected-attributes+ser
 #{ {:type SerializableAttribute}
    {:type ObsoleteAttribute :message "abc"}
	{:type AnotherAttribute :primary 7}
	{:type AnAttribute :primary "def" :secondary nil}
	{:type AnAttribute :primary "ghi" :secondary "jkl"}})

(def expected-attributes-field
 #{ {:type NonSerializedAttribute}
    {:type ObsoleteAttribute :message "abc"}})

(deftest test-attributes-on-type
  (is (=
       expected-attributes+ser
       (into #{} (map attribute->map (get-custom-attributes Bar))))))

(deftest test-attributes-on-field
  (is (=
       expected-attributes-field
       (into #{} (map attribute->map (get-custom-attributes (.GetField Bar "b")))))))

(deftest test-attributes-on-method
  (is (=
       expected-attributes
       (into #{} (map attribute->map (get-custom-attributes (.GetMethod Bar "foo")))))))

(gen-class :name foo.Bar
           :extends clojure.lang.Box
           :constructors {^{ObsoleteAttribute "help"} [Object] [Object]}
           :init init
		   :class-attributes {
             ObsoleteAttribute "abc"
             AnotherAttribute 7
		     AnAttribute #{ "def" 
                            { :__args ["ghi"] :SecondaryValue "jkl" }}}
           :prefix "foo")

(defn foo-init [obj]
  [[obj] nil])

(assembly-load "foo.Bar")

(deftest test-attributes-on-constructor
  (is (some #(instance? ObsoleteAttribute %)
            (for [ctor (.GetConstructors (clojure.lang.RT/classForName "foo.Bar"))
                  attribute (get-custom-attributes ctor)]
              attribute))))

(deftest test-attributes-on-genclass-class
  (is (=
       expected-attributes
       (into #{} (map attribute->map (get-custom-attributes (clojure.lang.RT/classForName "foo.Bar")))))))

  