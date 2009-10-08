;   Copyright (c) David Miller. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.

(in-ns 'clojure.core)

;;;;;; Extensions to core for the CLR platform  ;;;;;;;


 
 (defmacro gen-delegate 
    [type argVec & body] `(clojure.lang.GenDelegate/Create ~type (fn ~argVec ~@body)))
    
;;; Additional numeric casts
;;; Somewhat useless until our arithmetic package is extended to support all these types.

(defn uint
  "Coerce to uint"
  {:tag UInt32   
   :inline (fn  [x] `(. clojure.lang.RT (uintCast ~x)))}
  [x] (. clojure.lang.RT (uintCast x)))
  
(defn ushort
  "Coerce to ushort"
  {:tag UInt16   
   :inline (fn  [x] `(. clojure.lang.RT (ushortCast ~x)))}
  [x] (. clojure.lang.RT (ushortCast x)))
  
(defn ulong
  "Coerce to ulong"
  {:tag UInt64   
   :inline (fn  [x] `(. clojure.lang.RT (ulongCast ~x)))}
  [x] (. clojure.lang.RT (ulongCast x)))
  
(defn decimal
  "Coerce to decimal"
  {:tag UInt16   
   :inline (fn  [x] `(. clojure.lang.RT (ushortCast ~x)))}
  [x] (. clojure.lang.RT (ushortCast x)))
  
(defn sbyte
  "Coerce to sbyte"
  {:tag SByte   
   :inline (fn  [x] `(. clojure.lang.RT (sbyteCast ~x)))}
  [x] (. clojure.lang.RT (sbyteCast x)))

