;   Copyright (c) Rich Hickey. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.

; Author: Frantisek Sodomka

(assembly-load-with-partial-name "System.Drawing")         ;;; DM: Added
(ns clojure.test-clojure.java-interop
  (:use clojure.test))

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
      (. Int32 MaxValue) ))                    ;;; Integer MAX_VALUE


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
      Int64 true                   ;;; java.lang.Long
      Char false                    ;;; java.lang.Character
      String false ))               ;;; java.lang.String

; set!

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


(deftest test-bases
  (are [x y] (= x y)
      (bases  System.Math)                                      ;;; java.lang.Math)
        (list System.Object)                                    ;;; java.lang.Object)
      (bases System.Collections.ICollection)                    ;;; java.util.Collection)
        (list System.Collections.IEnumerable)                   ;;; java.lang.Iterable)
      (bases System.Object)                                     ;;; java.lang.Object)
        nil
      (bases System.IComparable)                                ;;; java.lang.Comparable)
        nil
      (bases System.Int32)                                      ;;; java.lang.Integer)
        (list System.ValueType System.IComparable System.IFormattable System.IConvertible |System.IComparable`1[System.Int32]| |System.IEquatable`1[System.Int32]|) ))  ;;; (java.lang.Number java.lang.Comparable)

(deftest test-supers
  (are [x y] (= x y)
      (supers System.Math)                                      ;;; java.lang.Math)
        #{System.Object}                                        ;;; java.lang.Object}
      (supers System.Int32)                                     ;;; java.lang.Integer)
        #{System.IFormattable System.IConvertible System.IComparable |System.IEquatable`1[System.Int32]| |System.IComparable`1[System.Int32]|     ;;; java.lang.Number java.lang.Object
		System.Object System.ValueType}   ))                     ;;; java.lang.Comparable java.io.Serializable} ))


; Arrays: [alength] aget aset [make-array to-array into-array to-array-2d aclone]
;   [float-array, int-array, etc]
;   amap, areduce

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
      (char-array [\a \b \c])
      (short-array [(short 1) (short 2)])
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
