;   Copyright (c) Rich Hickey. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.
;
;	Author: David Miller

; Test of new interop code
;
; Place this file in the clojure subdirectory of your main directory.
; Compile the file C.cs via:  csc /t:library C.cs
; Place C.dll in your root directory.
; Start Clojure and do:
;   (System.Reflection.Assembly/LoadFrom "C.dll")
;   (compile 'clojure.testinterop ')
;
; You should then be able to play games such as:
;



(ns clojure.testinterop)

; Create some instances
(def c1 (dm.interop.C1.))
(def c2 (dm.interop.C2.))
(def c3 (dm.interop.C3.))
(def c4 (dm.interop.C4.))


; Test the instance field/property/method()
(defn f1 [c]
  (.m1 c))
  
;  (f1 c1) => 11  ; accesses field
;  (f1 c2) => 21  ; accesses property
;  (f1 c3) => 31  ; acccesses zero-arity method
;  (f1 c4) => throws

; Test the static field/property/method()
(defn f1s []
  (+ (dm.interop.C1/m1s) (dm.interop.C2/m1s) (dm.interop.C3/m1s)))

 
; Test overload resolving
(defn f2none [c]
  (.m2 c))
  
; (f2none c1) =>  writes something appropriate.

; Really test overload resolving
(defn f2one [c x] 
  (.m2 c x))
  
; (f2one c1 7)
; (f2one c1 7.1)
; (f2one c1 "whatever")
; (f2one c1 '(a b c))

(defn f2two [c x y]
  (.m2 c x y))
  
; (f2two c1 "Here it is: {0}" 12)
; (f2two c23 "Here it is: {0}" 12)


; Test refparam, resolved at compile-time

(defn f3c [c n]
  (let [m (int n)]
     (.m3 #^dm.interop.C1 c (refparam m))
     m))
     
; Test refparam, resolved at runtime

; Test refparam, resolved at compile-time

(defn f3r [c n]
  (let [m (int n)]
     (.m3 c (refparam m))
     m))     
  
; Make sure we find the non-refparam overload
(defn f3n [c n]
  (let [m (int n)]
     (.m3 c m)))  
     
; (f3c c1 12) => 13
; (f3r c1 12) => 13
; (f3n c1 12) => 12

  