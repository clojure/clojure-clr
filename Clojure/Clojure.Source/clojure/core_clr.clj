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
  {:inline (fn  [x] `(. clojure.lang.RT (~(if *unchecked-math* 'uncheckedUIntCast 'uintCast) ~x)))
   :added "1.0"}
  [x] (. clojure.lang.RT (uintCast x)))
  
(defn ushort
  "Coerce to ushort"
  {:inline (fn  [x] `(. clojure.lang.RT (~(if *unchecked-math* 'uncheckedUShortCast 'ushortCast) ~x)))
   :added "1.0"}
  [x] (. clojure.lang.RT (ushortCast x)))
  
(defn ulong
  "Coerce to ulong"
  {:inline (fn  [x] `(. clojure.lang.RT (~(if *unchecked-math* 'uncheckedULongCast 'ulongCast) ~x)))
   :added "1.0"}
  [x] (. clojure.lang.RT (ulongCast x)))
  
(defn decimal
  "Coerce to decimal"
  {:inline (fn  [x] `(. clojure.lang.RT (~(if *unchecked-math* 'uncheckedDecimalCast 'decimalCast) ~x)))
   :added "1.0"}
  [x] (. clojure.lang.RT (decimalCast x)))
  
(defn sbyte
  "Coerce to sbyte"
  {:inline (fn  [x] `(. clojure.lang.RT (~(if *unchecked-math* 'uncheckedSByteCast 'sbyteCast) ~x)))
   :added "1.0"}
  [x] (. clojure.lang.RT (sbyteCast x)))

;;; Additional aset-XXX variants

(def-aset
  ^{:doc "Sets the value at the index/indices. Works on arrays of uint. Returns val."
    :added "1.0"}
  aset-uint setUInt uint)
  
(def-aset
  ^{:doc "Sets the value at the index/indices. Works on arrays of ushort. Returns val."
    :added "1.0"}
  aset-ushort setUShort ushort)
  
(def-aset
  ^{:doc "Sets the value at the index/indices. Works on arrays of ulong. Returns val."
    :added "1.0"}
  aset-ulong setULong ulong)
  
(def-aset
  ^{:doc "Sets the value at the index/indices. Works on arrays of decimal. Returns val."
    :added "1.0"}
  aset-decimal setDecimal decimal)
  
(def-aset
  ^{:doc "Sets the value at the index/indices. Works on arrays of sbyte. Returns val."
    :added "1.0"}
  aset-sbyte setSByte sbyte)
  
(defn enum-val [t n]
  "Gets a value from an enum from the name"
  {:added "1.0"}
  (let [s (if (string? n) n (name n))]
   (Enum/Parse t s)))
  
; Support for interop

(defn by-ref
  "Signals that a by-ref parameter is desired at this position in an interop call or method signature.
  
  Should only be used in CLR interop code.  Throws an exception otherwise."
  {:added "1.2"}
   [v] (throw (ArgumentException. "by-ref not used at top-level in an interop call or method signature")))
  
(defn generic
  "Signals that a generic method reference is desired for this interop call

  Should only be used in CLR interop code.  Throws an exception otherwise."
  {:added "1.3"}
  [v] (throw (ArgumentException. "generic not used in interop call")))

(defmacro sys-func
   "Translates to a gen-delegate for a System.Func<,...> call"
   [typesyms & body ]
   (let [types (map (fn [tsym] (clojure.lang.CljCompiler.Ast.HostExpr/MaybeType tsym false)) typesyms)
         join  ; clojure.string not yet loaded
		       (fn [coll] 
			      (loop [sb (StringBuilder. (str (first coll)))
				         more (next coll)]
				    (if more
					    (recur (-> sb (.Append ",") (.Append (str (first more))))
						       (next more))
					    (str sb))))
		ftype (symbol (str "System.Func`" (count types) "[" (join types) "]"))]
	  `(gen-delegate ~ftype ~@body)))

; Attribute handling

(defn enum? [v]
  (instance? Enum v))
  
(defn array? [v]
  (instance? Array v))

(defn- is-attribute? [c]
  (and (class? c)
       (.IsAssignableFrom System.Attribute c)))

(defn- attribute-filter [[k v]]
  (when (symbol? k)
    (when-let [c (resolve k)]
      (is-attribute? c))))
      
      
; Note: we are not handling the non-CLS-compliant case of a one-dimensional array of arg values -- yet.

(defn- normalize-attribute-arg-value [v]
  (cond
	(symbol? v) (let [ev (eval v)]
	              (enum? ev) ev
	              (class? ev) ev
	              :else   ev ) ;(throw (ArgumentException. (str "Unsupported attribute argument value: " v " of class " (class ev)))))
	:else v))
     
      
(defn- normalize-attribute-arg [arg]
  (cond
     (vector? arg) { :__args (map normalize-attribute-arg-value arg) }
     (map? arg)    (into1 {} (map (fn [k v] [k (normalize-attribute-arg-value v)]) arg))
     :else         { :__args [ (normalize-attribute-arg-value arg) ] }))
    
(defn- resolve-attribute [v]
  (cond
    (is-attribute? v) v
    (symbol? v) (when-let [c (resolve v)]
                   (when (is-attribute? c)
                     c))
    :else nil))
         

(defn- extract-attributes [m]
   (into1 {} 
     (remove nil? 
       (for [[k v] (seq m)]
         (when-let [c (resolve-attribute k)]
           [ c (normalize-attribute-arg v) ])))))
                
