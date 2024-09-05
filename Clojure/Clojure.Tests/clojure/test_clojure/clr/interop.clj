;   Copyright (c) Rich Hickey. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.

;; Tests added for ClojureCLR -- miscellaneous

(ns clojure.test-clojure.clr.interop
  (:use clojure.test
        [clojure.test-helper :only (should-not-reflect)]))

(assembly-load-from "Clojure.Tests.Support.dll")
(import '[Clojure.Tests.Support GenericsTest AlmostEverything AlmostEverything2 AlmostEverything3 AlmostNothing ParamsTest])


;; these will definitely cause reflection -- that's the point 

(defn access-zero-arity-member [x]
  (.ZeroArityMember x))

(defn access-non-existent-member [x]
 (.NonExistentMember x))

(defn access-overloaded-method [x y]
  (.Over x y))

(defn access-ambig [x s i]
  (let [m (int i)
    	v (.Ambig x s (by-ref m))]
    [v m]))

(defn access-ambig2 [x s i]
  (let [m (int i)
    	v (.Ambig ^AlmostEverything x s (by-ref m))]
    [v m]))



(set! *warn-on-reflection* true)

(deftest static-generic-method-with-type-args
  (is (instance? Int32 (GenericsTest/StaticMethod0 (type-args Int32))))
  (is (instance? Int32 (GenericsTest/StaticMethod0 (type-args Int32))))
  (is (instance? DateTime (GenericsTest/StaticMethod0 (type-args DateTime)))))

(deftest instance-generic-method-with-type-args
  (let [gt (GenericsTest.)]
	(is (instance? Int32 (GenericsTest/.InstanceMethod0 gt (type-args Int32))))
	(is (instance? Int32 (GenericsTest/.InstanceMethod0 gt (type-args Int32))))
	(is (instance? DateTime (GenericsTest/.InstanceMethod0 gt (type-args DateTime))))))

(deftest static-generic-method-with-type-args-with-param-tags
  (is (instance? Int32 ( ^[ (type-args Int32) ] GenericsTest/StaticMethod0)))
  (is (instance? Int32 (GenericsTest/StaticMethod0 (type-args Int32))))
  (is (instance? DateTime (GenericsTest/StaticMethod0 (type-args DateTime)))))


(deftest zero-arity-method-reflection
  (testing "reflection on zero-arity members")
    (is (= (access-zero-arity-member (AlmostEverything.)) "field"))
    (is (= (access-zero-arity-member (AlmostEverything2.)) "property"))
    (is (= (access-zero-arity-member (AlmostEverything3.)) "method"))
  (testing "failed reflection on zero-arity members")
    (is (thrown? MissingMethodException (access-non-existent-member (AlmostEverything.)))))

(deftest basic-method-reflection
   (let [ae (AlmostEverything.)]
     (is (= (access-overloaded-method ae (int 1)) "int"))
     (is (= (access-overloaded-method ae  1) "long"))
     (is (= (access-overloaded-method ae  1.0) "double"))
	 (is (= (access-overloaded-method ae "1") "object"))
	 (is (= (access-overloaded-method ae '(1)) "object"))))

(deftest hinted-method-selection
  (let [ae (AlmostEverything.)]
	(is (= ( ^[int] AlmostEverything/.Over ae (int 1)) "int"))
	(is (= ( ^[int] AlmostEverything/.Over ae  1) "int"))
	(is (= ( ^[long] AlmostEverything/.Over ae (int 1)) "long"))
	(is (= ( ^[Object] AlmostEverything/.Over ae (int 1)) "object"))))

;; there is an interaction between the (by-ref m) forms and the rewriting done by 'is' that I just don't understand.

(deftest by-ref-tests
  (let [ae (AlmostEverything.)]

    (let [m (int 12)
         v (.Out ^AlmostEverything ae (by-ref m))]
      (is (= v 33))
      (is (= m 13)))

    (let [m (int 12)]
      (is (= (.Out ^AlmostEverything ae m) 22))
      (is (= m 12)))
      
    (let [m (int 12) 
          v (^[ |System.Int32&| ] AlmostEverything/.Out ae (by-ref m))]
      (is (= v 33))
      (is (= m 13)))

    (let [m (int 12)
          v (^[ |System.Int32| ] AlmostEverything/.Out ae (by-ref m))]
      (is (= v 22))
      (is (= m 12)))))


;; we require hints to get to the correct overloaded method:
;; Ambig(string, ref int) vs Ambig(int, ref int
;;
;; However, we don't need hints to get to the correct overloaded method when the by-ref is the problem
;; AmbigRef(ref string) vs AmbigRef(ref int)


(defn access-ambig-string-hinted [x s i]
  (let [m (int i)
    	v (^ [String |System.Int32&| ] AlmostEverything/.Ambig  x s (by-ref m))]
    [v m]))

(defn access-ambig-int-hinted [x s i]
  (let [m (int i)
    	v (^ [int |System.Int32&| ] AlmostEverything/.Ambig  x s (by-ref m))]
    [v m]))


(deftest overloaded-by-ref-method-reflection 
  (let [ae (AlmostEverything.)]
	(is (= (access-ambig-string-hinted ae "help" 12) ["help22" 22]))
	(is (= (access-ambig-int-hinted ae 15 20) [135 120]))))


(deftest constructor-overloads
  (testing "Standard ctor calls"
    (is (= (.ToString (AlmostEverything.)) "void"))
    (is (= (.ToString (AlmostEverything. 7)) "int"))
    (is (= (.ToString (AlmostEverything. "thing")) "string"))
    (let [m (int 12)
          ae (AlmostEverything. (by-ref m))]
      (is (= (.ToString ae) "ref int"))
      (is (= m 13))))

  (testing "QME ctor calls"

    (is (= (.ToString (AlmostEverything/new)) "void"))
    (is (= (.ToString (AlmostEverything/new 7)) "int"))
    (is (= (.ToString (AlmostEverything/new "thing")) "string"))
    (let [m (int 12)
          ae (AlmostEverything/new (by-ref m))]
      (is (= (.ToString ae) "ref int"))
      (is (= m 13)))

  (testing " hinted QME ctor calls"

    (is (= (.ToString ( ^[] AlmostEverything/new)) "void"))
    (is (= (.ToString ( ^[int] AlmostEverything/new 7)) "int"))
    (is (= (.ToString ( ^[String] AlmostEverything/new "thing")) "string"))
    (let [m (int 12)
          ae ( ^[ |System.Int32&| ] AlmostEverything/new (by-ref m))]
      (is (= (.ToString ae) "ref int"))
      (is (= m 13))))))


(deftest params-args
  (let [pt (ParamsTest.)]
    (testing "params args via QME: static"
      ( is (= (ParamsTest/StaticParams 12 (into-array Object [1 2 3])) 15))
      ( is (= (ParamsTest/StaticParams 12 (into-array String ["abc" "de" "f"])) 15))
      ( is (= (ParamsTest/StaticParams 12 ^|System.String[]| (into-array String ["abc" "de" "f"])))))
    (testing "params via hinted QME: static"
      ( is (= ( ^[int objects] ParamsTest/StaticParams 12 (into-array Object [1 2 3])) 15))
      ( is (= ( ^[int objects ] ParamsTest/StaticParams 12 (into-array String ["abc" "de" "f"])) 15))
	  ( is (= ( ^[int |System.String[]| ] ParamsTest/StaticParams 12 (into-array String ["abc" "de" "f"])))))

    (testing "params args via QME: instance"
      ( is (= (ParamsTest/.InstanceParams pt 12 (into-array Object [1 2 3])) 15))
      ( is (= (ParamsTest/.InstanceParams pt 12 (into-array String ["abc" "de" "f"])) 15))
      ( is (= (ParamsTest/.InstanceParams pt 12 ^|System.String[]| (into-array String ["abc" "de" "f"])))))
    (testing "params via hinted QME: instance"
      ( is (= ( ^[int objects] ParamsTest/.InstanceParams pt 12 (into-array Object [1 2 3])) 15))
      ( is (= ( ^[int objects ] ParamsTest/.InstanceParams pt 12 (into-array String ["abc" "de" "f"])) 15))
	  ( is (= ( ^[int |System.String[]| ] ParamsTest/.InstanceParams pt 12 (into-array String ["abc" "de" "f"])))))))




