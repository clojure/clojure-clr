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
   :inline (fn  [x] `(. clojure.lang.RT (decimalCast ~x)))}
  [x] (. clojure.lang.RT (decimalCast x)))
  
(defn sbyte
  "Coerce to sbyte"
  {:tag SByte   
   :inline (fn  [x] `(. clojure.lang.RT (sbyteCast ~x)))}
  [x] (. clojure.lang.RT (sbyteCast x)))

;;; Additional aset-XXX variants

(def-aset
  #^{:doc "Sets the value at the index/indices. Works on arrays of uint. Returns val."}
  aset-uint setUInt uint)
  
(def-aset
  #^{:doc "Sets the value at the index/indices. Works on arrays of ushort. Returns val."}
  aset-ushort setUShort ushort)
  
(def-aset
  #^{:doc "Sets the value at the index/indices. Works on arrays of ulong. Returns val."}
  aset-ulong setULong ulong)
  
(def-aset
  #^{:doc "Sets the value at the index/indices. Works on arrays of decimal. Returns val."}
  aset-decimal setDecimal decimal)
  
(def-aset
  #^{:doc "Sets the value at the index/indices. Works on arrays of sbyte. Returns val."}
  aset-sbyte setSByte sbyte)
  
(defn enum-val [t n]
  "Gets a value from an enum from the name"
  (let [s (if (string? n) n (name n))]
   (Enum/Parse t s)))
  
; Support for interop

(defn by-ref [v]
  "Signals that a by-ref parameter is desired at this position in an interop call or method signature.
  
  Should only be used in CLR interop code.  Throws an exception otherwise."
  (throw (ArgumentException. "by-ref not used at top-level in an interop call or method signature")))
  

  
