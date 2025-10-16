;   Copyright (c) Rich Hickey. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.

(in-ns 'clojure.core)

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;; printing ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

(import 'System.IO.TextWriter)   ;;; was (import '(java.io Writer))    (I have replaced ^Writer with ^System.IO.TextWriter throughout
;; Other global replaces:  .write => .Write, .append => .Write, ^Class => ^Type, ^Character => ^Char
(set! *warn-on-reflection* true)
(def ^:dynamic 
 ^{:doc "*print-length* controls how many items of each collection the
  printer will print. If it is bound to logical false, there is no
  limit. Otherwise, it must be bound to an integer indicating the maximum
  number of items of each collection to print. If a collection contains
  more items, the printer will print items up to the limit followed by
  '...' to represent the remaining items. The root binding is nil
  indicating no limit."
   :added "1.0"}
 *print-length* nil)

(def ^:dynamic 
 ^{:doc "*print-level* controls how many levels deep the printer will
  print nested objects. If it is bound to logical false, there is no
  limit. Otherwise, it must be bound to an integer indicating the maximum
  level to print. Each argument to print is at level 0; if an argument is a
  collection, its items are at level 1; and so on. If an object is a
  collection and is at a level greater than or equal to the value bound to
  *print-level*, the printer prints '#' to represent it. The root binding
  is nil indicating no limit."
   :added "1.0"}
 *print-level* nil)

(def ^:dynamic *verbose-defrecords* false)

(def ^:dynamic
 ^{:doc "*print-namespace-maps* controls whether the printer will print
  namespace map literal syntax. It defaults to false, but the REPL binds
  to true."
   :added "1.9"}
 *print-namespace-maps* false)

(defn- print-sequential [^String begin, print-one, ^String sep, ^String end, sequence, ^System.IO.TextWriter w]
  (binding [*print-level* (and (not *print-dup*) *print-level* (dec *print-level*))]
    (if (and *print-level* (neg? *print-level*))
      (.Write w "#")
      (do
        (.Write w begin)
        (when-let [xs (seq sequence)]
          (if (and (not *print-dup*) *print-length*)
            (loop [[x & xs] xs
                   print-length *print-length*]
              (if (zero? print-length)
                (.Write w "...")
                (do
                  (print-one x w)
                  (when xs
                    (.Write w sep)
                    (recur xs (dec print-length))))))
            (loop [[x & xs] xs]
              (print-one x w)
              (when xs
                (.Write w sep)
                (recur xs)))))
        (.Write w end)))))

(defn- print-meta [o, ^System.IO.TextWriter w]
  (when-let [m (meta o)]
    (when (and (pos? (count m))
               (or *print-dup*
                   (and *print-meta* *print-readably*)))
      (.Write w "^")
      (if (and (= (count m) 1) (:tag m))
          (pr-on (:tag m) w)
          (pr-on m w))
      (.Write w " "))))

(defn print-simple [o, ^System.IO.TextWriter w]
  (print-meta o w)
  (.Write w (str o)))

(defmethod print-method :default [o, ^System.IO.TextWriter w]
  (if (instance? clojure.lang.IObj o)
    (print-method (vary-meta o #(dissoc % :type)) w)
    (print-simple o w)))

(defmethod print-method nil [o, ^System.IO.TextWriter w]
  (.Write w "nil"))

(defmethod print-dup nil [o w] (print-method o w))

(defn print-ctor [o print-args ^System.IO.TextWriter w]
  (.Write w "#=(")
  (.Write w (.FullName ^Type (class o)))   ;;; .getName  => .FullName
  (.Write w ". ")
  (print-args o w)
  (.Write w ")"))

(defn- print-tagged-object [o rep ^System.IO.TextWriter w]
  (when (instance? clojure.lang.IMeta o)
    (print-meta o w))
  (.Write w "#object[")
  (let [c (class o)]
    (if (.IsArray c)                               ;;; .isArray
      (print-method (.Name c) w)                   ;;; .getName
      (.Write w (.Name c))))                       ;;; .getName
  (.Write w " ")
  (.Write w (format "0x%x " (System.Runtime.CompilerServices.RuntimeHelpers/GetHashCode o)))   ;;; (System/identityHashCode o)
  (print-method rep w)
  (.Write w "]"))

(defn- print-object [o, ^System.IO.TextWriter w]
  (print-tagged-object o (str o) w))

(defmethod print-method Object [o, ^System.IO.TextWriter w]
  (print-object o w))

(defmethod print-method clojure.lang.Keyword [o, ^System.IO.TextWriter w]
  (.Write w (str o)))

(defmethod print-dup clojure.lang.Keyword [o w] (print-method o w))
;;; MAJOR PROBLEM: no Number type in CLR.  We will just ask every ValueType to print itself.  TODO: Need to deal with BigDecimal and BigInteger later.
(defmethod print-method ValueType [o, ^System.IO.TextWriter w]   ;; Number => ValueType
  (.Write w (str o)))

;;; DM ADDED

(defn fp-str [x]
   (let [s (str x)]
     (if (or (.Contains s ".") (.Contains s "E"))
       s
       (str s ".0"))))
;;; Whelp, now they have added in print-method for Double and Single, in order to handle infinities and NaN

(defmethod print-method Double [o, ^System.IO.TextWriter w]+  
  (cond
    (= Double/PositiveInfinity o) (.Write w "##Inf")                            ;;; POSITIVE_INFINITY
    (= Double/NegativeInfinity o) (.Write w "##-Inf")                           ;;; NEGATIVE_INFINITY
    (Double/IsNaN ^Double o) (.Write w "##NaN")                                       ;;; (.IsNaN ^Double o)
    :else (.Write w (fp-str o))))

(defmethod print-method Single [o, ^System.IO.TextWriter w]
  (cond
    (= Single/PositiveInfinity o) (.Write w "##Inf")                             ;;; Float/POSITIVE_INFINITY
    (= Single/NegativeInfinity o) (.Write w "##-Inf")                            ;;; Float/NEGATIVE_INFINITY
    (Single/IsNaN ^Float o) (.Write w "##NaN")                                   ;;; (.IsNaN ^Float o)
    :else (.Write w (fp-str o))))       

;;;We need to cover all the numerics, or we are hosed on print-dup.
(defmethod print-method Int16 [o, ^System.IO.TextWriter w] (.Write w (str o)))
(defmethod print-method Int32 [o, ^System.IO.TextWriter w] (.Write w (str o)))
(defmethod print-method Int64 [o, ^System.IO.TextWriter w] (.Write w (str o)))
(defmethod print-method UInt16 [o, ^System.IO.TextWriter w] (.Write w (str o)))
(defmethod print-method UInt32 [o, ^System.IO.TextWriter w] (.Write w (str o)))
(defmethod print-method UInt64 [o, ^System.IO.TextWriter w] (.Write w (str o)))
(defmethod print-method Byte [o, ^System.IO.TextWriter w] (.Write w (str o)))
(defmethod print-method SByte [o, ^System.IO.TextWriter w] (.Write w (str o)))

(defmethod print-dup Int16 [o, ^System.IO.TextWriter w] (print-method o w))
(defmethod print-dup Int32 [o, ^System.IO.TextWriter w] (print-method o w))
(defmethod print-dup Int64 [o, ^System.IO.TextWriter w] (print-method o w))
(defmethod print-dup UInt16 [o, ^System.IO.TextWriter w] (print-method o w))
(defmethod print-dup UInt32 [o, ^System.IO.TextWriter w] (print-method o w))
(defmethod print-dup UInt64 [o, ^System.IO.TextWriter w] (print-method o w))
(defmethod print-dup Byte [o, ^System.IO.TextWriter w] (print-method o w))
(defmethod print-dup SByte [o, ^System.IO.TextWriter w] (print-method o w))
(defmethod print-dup Double [o, ^System.IO.TextWriter w] (print-method o w))
(defmethod print-dup Single [o, ^System.IO.TextWriter w] (print-method o w))

;;;

(defmethod print-dup ValueType [o, ^System.IO.TextWriter w]      ;;; Number => ValueType
  (print-ctor o
              (fn [o w]
                  (print-dup (str o) w))
              w))

(defmethod print-dup clojure.lang.Fn [o, ^System.IO.TextWriter w]
  (print-ctor o (fn [o w]) w))

(prefer-method print-dup clojure.lang.IPersistentCollection clojure.lang.Fn)
(prefer-method print-dup System.Collections.IDictionary clojure.lang.Fn)                        ;;; java.util.Map
(prefer-method print-dup System.Collections.ICollection clojure.lang.Fn)                        ;;; java.util.Collection

(defmethod print-method Boolean [o, ^System.IO.TextWriter w]
  (.Write w (if o "true" "false")))                                                ;;; (.Write w (str o)))  else we get True False

(defmethod print-dup Boolean [o w] (print-method o w))

(defmethod print-method clojure.lang.Symbol [o, ^System.IO.TextWriter w]
  (print-simple o w))

(defmethod print-dup clojure.lang.Symbol [^clojure.lang.Symbol o, ^System.IO.TextWriter w]                                       ;;; (print-method o w)), Added hints
  (if (or *print-dup* *print-readably*)
	(do 
		(print-meta o w)
		(.Write w (.ToStringEscaped o)))
	(print-method o w)))

(defmethod print-method clojure.lang.Var [o, ^System.IO.TextWriter w]
  (print-simple o w))

(defmethod print-dup clojure.lang.Var [^clojure.lang.Var o, ^System.IO.TextWriter w]
  (.Write w (str "#=(var " (.Name (.ns o)) "/" (.Symbol o) ")")))   ;;; .name => .Name, .sym => .Symbol

(defmethod print-method clojure.lang.ISeq [o, ^System.IO.TextWriter w]
  (print-meta o w)
  (print-sequential "(" pr-on " " ")" o w))

(defmethod print-dup clojure.lang.ISeq [o w] (print-method o w))
(defmethod print-dup clojure.lang.IPersistentList [o w] (print-method o w))
(prefer-method print-method clojure.lang.ISeq clojure.lang.IPersistentCollection)
(prefer-method print-dup clojure.lang.ISeq clojure.lang.IPersistentCollection)
(prefer-method print-method clojure.lang.ISeq System.Collections.ICollection)  ;;  java: java.util.Collection
(prefer-method print-dup clojure.lang.ISeq System.Collections.ICollection)  ;;  java: java.util.Collection



(defmethod print-dup System.Collections.ICollection [o, ^System.IO.TextWriter w]                     ;; java.util.Collection => System.Collections.ICollection
 (print-ctor o #(print-sequential "[" print-method " " "]" %1 %2) w))

(defmethod print-dup clojure.lang.IPersistentCollection [o, ^System.IO.TextWriter w]
  (print-meta o w)
  (.Write w "#=(")
  (.Write w (.FullName ^Type (class o)))   ;; .getName => .FullName
  (.Write w "/create ")
  (print-sequential "[" print-dup " " "]" o w)
  (.Write w ")"))

(prefer-method print-dup clojure.lang.IPersistentCollection System.Collections.ICollection)                ;; java.util.Collection => System.Collections.ICollection

(def ^{:tag String 
       :doc "Returns escape string for char or nil if none"
       :added "1.0"}
  char-escape-string
    {\newline "\\n"
     \tab  "\\t"
     \return "\\r"
     \" "\\\""
     \\  "\\\\"
     \formfeed "\\f"
     \backspace "\\b"})  

(defmethod print-method String [^String s, ^System.IO.TextWriter w]
  (if (or *print-dup* *print-readably*)
    (do (.Write w \")                               ;;; "   (Just to keep the display happy in the editor)
      (dotimes [n (count s)]
        (let [c (.get_Chars s n)                    ;; .charAt => .get_Chars
              e (char-escape-string c)]
          (if e (.Write w e) (.Write w c))))        
      (.Write w \"))                            ;;; "   (Just to keep the display happy in the editor)
    (.Write w s))                                 
  nil)

(defmethod print-dup String [s w] (print-method s w))

(defmethod print-method clojure.lang.IPersistentVector [v, ^System.IO.TextWriter w]
  (print-meta v w)
  (print-sequential "[" pr-on " " "]" v w))

(defn- print-prefix-map [prefix kvs print-one w]
  (print-sequential
    (str prefix "{")
    (fn [[k v] ^System.IO.TextWriter w]
      (do (print-one k w) (.Write w \space) (print-one v w)))             ;;; .append
    ", "
    "}"
    kvs w))
 
 (defn- print-map [m print-one w]
  (print-prefix-map nil m print-one w))

(defn- strip-ns
  [named]
  (if (symbol? named)
    (symbol nil (name named))
    (keyword nil (name named))))

(defn- lift-ns
  "Returns [lifted-ns lifted-kvs] or nil if m can't be lifted."
  [m]
  (when *print-namespace-maps*
    (loop [ns nil
           [[k v :as entry] & entries] (seq m)
           kvs []]
      (if entry
       (when (qualified-ident? k)
          (if ns
            (when (= ns (namespace k))
             (recur ns entries (conj kvs [(strip-ns k) v])))
            (when-let [new-ns (namespace k)]
              (recur new-ns entries (conj kvs [(strip-ns k) v])))))
        [ns kvs]))))

(defmethod print-method clojure.lang.IPersistentMap [m, ^System.IO.TextWriter w]
  (print-meta m w)
  (let [[ns lift-kvs] (lift-ns m)]
    (if ns
      (print-prefix-map (str "#:" ns) lift-kvs pr-on w)
      (print-map m pr-on w))))

(defmethod print-dup System.Collections.IDictionary [m, ^System.IO.TextWriter w]    ;;; java.util.Map
  (print-ctor m #(print-map (seq %1) print-method %2) w))

(defmethod print-dup clojure.lang.IPersistentMap [m, ^System.IO.TextWriter w]
  (print-meta m w)
  (.Write w "#=(")
  (.Write w (.FullName (class m)))   ;; .getName => .FullName
  (.Write w "/create ")
  (print-map m print-dup w)
  (.Write w ")"))
  
;; java.util
(prefer-method print-method clojure.lang.IPersistentCollection System.Collections.ICollection)         ;;; java.util.Collection
;;;(prefer-method print-method clojure.lang.IPersistentCollection java.util.RandomAccess)
;;;(prefer-method print-method java.util.RandomAccess java.util.List)
(prefer-method print-method clojure.lang.IPersistentCollection System.Collections.IDictionary)         ;;; java.util.Map

(defmethod print-method System.Collections.ICollection [c, ^System.IO.TextWriter w]                    ;;; java.util.List
  (if *print-readably*
    (do
      (print-meta c w)
      (print-sequential "(" pr-on " " ")" c w))
    (print-object c w)))

;;;(defmethod print-method java.util.RandomAccess [v, ^System.IO.TextWriter w]
;;;  (if *print-readably*
;;;    (do
;;;      (print-meta v w)
;;;      (print-sequential "[" pr-on " " "]" v w))
;;;    (print-object v w)))

(defmethod print-method System.Collections.IDictionary [m, ^System.IO.TextWriter w]                  ;;; java.util.Map
  (if *print-readably*
    (do
      (print-meta m w)
      (print-map m pr-on w))
    (print-object m w)))

;;;(defmethod print-method java.util.Set [s, ^System.IO.TextWriter w]                ;;; One example where we need true generic handling -- this should be ISet<T>
;;;  (if *print-readably*
;;;    (do
;;;      (print-meta s w)
;;;      (print-sequential "#{" pr-on " " "}" (seq s) w))
;;;   (print-object s w)))

;; Records

(defmethod print-method clojure.lang.IRecord [r, ^System.IO.TextWriter w]
  (print-meta r w)
  (.Write w "#")
  (.Write w (.FullName (class r)))   ;; .getName => .FullName
  (print-map r pr-on w))

(defmethod print-dup clojure.lang.IRecord [r, ^System.IO.TextWriter w]
  (print-meta r w)
  (.Write w "#")
  (.Write w (.FullName (class r)))   ;; .getName => .FullName
  (if *verbose-defrecords*
    (print-map r print-dup w)
    (print-sequential "[" pr-on ", " "]" (vals r) w)))

(prefer-method print-method clojure.lang.IRecord  System.Collections.IDictionary)    ;; java.util.Map  -> System.Collections.IDictionary
(prefer-method print-method clojure.lang.IRecord clojure.lang.IPersistentMap)
(prefer-method print-dup clojure.lang.IRecord clojure.lang.IPersistentMap)
(prefer-method print-dup clojure.lang.IPersistentMap System.Collections.IDictionary)    ;; java.util.Map  -> System.Collections.IDictionary
(prefer-method print-dup clojure.lang.IRecord clojure.lang.IPersistentCollection)
(prefer-method print-dup clojure.lang.IRecord System.Collections.IDictionary)    ;; java.util.Map  -> System.Collections.IDictionary
(prefer-method print-dup clojure.lang.IRecord System.Collections.ICollection)
(prefer-method print-method clojure.lang.IRecord System.Collections.ICollection)

(defmethod print-method clojure.lang.IPersistentSet [s, ^System.IO.TextWriter w]
  (print-meta s w)
  (print-sequential "#{" pr-on " " "}" (seq s) w))

(def ^{:tag String
       :doc "Returns name string for char or nil if none"
       :added "1.0"}
 char-name-string
   {\newline "newline"
    \tab "tab"
    \space "space"
    \backspace "backspace"
    \formfeed "formfeed"
    \return "return"})

(defmethod print-method Char [c, ^System.IO.TextWriter w]          ;;; ^Character c
  (if (or *print-dup* *print-readably*)
    (do (.Write w \\)
        (let [n (char-name-string c)]
          (if n (.Write w n) (.Write w ^Char c))))
    (.Write w ^Char c))
  nil)

(defmethod print-dup Char   [c w] (print-method c w))             ;;; java.lang.Character
;(defmethod print-dup Int32  [o w] (print-method o w))               ;;; java.lang.Integer
;(defmethod print-dup Double [o w] (print-method o w))                ;;; java.lang.Double
(defmethod print-dup clojure.lang.Ratio [o w] (print-method o w))
(defmethod print-dup clojure.lang.BigDecimal [o w] (print-method o w))    ;;; java.math.BigDecimal 
(defmethod print-dup clojure.lang.BigInt [o w] (print-method o w))
(defmethod print-dup clojure.lang.PersistentHashMap [o w] (print-method o w))
(defmethod print-dup clojure.lang.PersistentHashSet [o w] (print-method o w)) 
(defmethod print-dup clojure.lang.PersistentVector [o w] (print-method o w))
(defmethod print-dup clojure.lang.LazilyPersistentVector [o w] (print-method o w))

;;; ADDED LINES
(defmethod print-method clojure.lang.Ratio [o  ^System.IO.TextWriter w]   (.Write w (str o)))
(defmethod print-dup clojure.lang.BigInteger [o ^System.IO.TextWriter w] 
  (.Write w "#=(clojure.lang.BigInteger/Parse ")
  (print-dup (str o) w)
  (.Write w ")"))



(def primitives-classnames    ;; not clear what the equiv should be
  {Single  "System.Single"   ;;{Float/TYPE "Float/TYPE"
   Int32   "System.Int32"    ;; Integer/TYPE "Integer/TYPE"
   Int64   "System.Int64"    ;; Long/TYPE "Long/TYPE"
   Boolean "System.Boolean"  ;; Boolean/TYPE "Boolean/TYPE"
   Char    "System.Char"     ;; Character/TYPE "Character/TYPE"
   Double  "System.Double"   ;; Double/TYPE "Double/TYPE"
   Byte    "System.Byte"     ;; Byte/TYPE "Byte/TYPE"
   Int16   "System.Int16"    ;; Short/TYPE "Short/TYPE"})
   SByte   "System.SByte"    ;; ADDED
   UInt16  "System.UInt16"   ;; ADDED
   UInt32  "System.UInt32"   ;; ADDED
   UInt64  "System.UInt64"   ;; ADDED
   Decimal "System.Decimal" })  ;; ADDED
  
(defmethod print-method Type [^Type c, ^System.IO.TextWriter w]
  (if (.IsArray c)                                                   ;;; .isArray
    (print-method (clojure.lang.Util/arrayTypeToSymbol c) w)
    (.Write w (.FullName c))))                                       ;;; .getName => .FullName
  

(defmethod print-dup Type [^Type c, ^System.IO.TextWriter w]
  (cond
    (.IsPrimitive c) (do                                             ;;; .isPrimitive
                       (.Write w "#=(identity ")
                       (.Write w ^String (primitives-classnames c))
                       (.Write w ")"))
    (.IsArray c) (do                                                 ;;; .isArray ,  java.lang.Class/forName =>
                   (.Write w "#=(clojure.lang.RT/classForName \"")
                   (.Write w (.FullName c))                          ;;; .getName => .FullName
                   (.Write w "\")"))
    :else (do
            (.Write w "#=")
            (.Write w (.FullName c)))))                              ;;; .getName => .FullName

(defmethod print-method clojure.lang.BigDecimal [b, ^System.IO.TextWriter w]    ;;; java.math.BigDecimal
  (.Write w (str b))
  (.Write w "M"))

(defmethod print-method clojure.lang.BigInt [b, ^System.IO.TextWriter w]
  (.Write w (str b))
  (.Write w "N"))

(defmethod print-method System.Text.RegularExpressions.Regex [p ^System.IO.TextWriter w]         ;;; java.util.regex.Pattern =>
  (.Write w "#\"")
  (loop [[^Char c & r :as s] (seq (.ToString ^System.Text.RegularExpressions.Regex p))   ;;; .pattern => .ToString
         qmode false]
    (when s
      (cond
        (= c \\) (let [[^Char c2 & r2] r]
                   (.Write w \\)
                   (.Write w c2)
                   (if qmode
                      (recur r2 (not= c2 \E))
                      (recur r2 (= c2 \Q))))
        (= c \") (do;;; "   (Just to keep the display happy in the editor)
                   (if qmode
                     (.Write w "\\E\\\"\\Q")
                     (.Write w "\\\""))
                   (recur r qmode))
        :else    (do
                   (.Write w c)
                   (recur r qmode)))))
  (.Write w \"))                                ;;; "   (Just to keep the display happy in the editor)

(defmethod print-dup System.Text.RegularExpressions.Regex [p ^System.IO.TextWriter w] (print-method p w))  ;;; java.util.regex.Pattern =>
  
(defmethod print-dup clojure.lang.Namespace [^clojure.lang.Namespace n ^System.IO.TextWriter w]
  (.Write w "#=(find-ns ")
  (print-dup (.Name n) w)    ;; .name
  (.Write w ")"))

(defn- deref-as-map [^clojure.lang.IDeref o]
  (let [pending (and (instance? clojure.lang.IPending o)
                     (not (.isRealized ^clojure.lang.IPending o)))
        [ex val]
        (when-not pending
          (try [false (deref o)]
               (catch Exception e                                  ;;; Throwable
                 [true e])))]
    {:status
     (cond
      (or ex
          (and (instance? clojure.lang.Agent o)
               (agent-error o)))
      :failed

      pending
      :pending

      :else
      :ready)

     :val val}))

(defmethod print-method clojure.lang.IDeref [o ^System.IO.TextWriter w]
  (print-tagged-object o (deref-as-map o) w))

;;; DM:Added 
(defn- stack-frame-info [^System.Diagnostics.StackFrame sf]
  (if (nil? sf)
    nil
    (if-let [m (.GetMethod sf)]
      [(symbol (if-let [declaring-type (.DeclaringType m)]
                  (.FullName (.DeclaringType m))
                  "<Unknown type>"))
       (symbol (.Name m))
       (or (.GetFileName sf) "NO_FILE")
       (.GetFileLineNumber sf)]
      ["UNKNOWN" "NO_METHOD" "NO_FILE" -1])))

(defmethod print-method  System.Diagnostics.StackFrame [^System.Diagnostics.StackFrame o ^System.IO.TextWriter w]       ;;;  StackTraceElement  ^StackTraceElement
  (print-method (stack-frame-info o) w))                                                                            ;;;(print-method [(symbol (.getClassName o)) (symbol (.getMethodName o)) (.getFileName o) (.getLineNumber o)] w)) 

(defn StackTraceElement->vec
  "Constructs a data representation for a StackTraceElement: [class method file line]"
  {:added "1.9"}
  [^System.Diagnostics.StackFrame o]
  (if (nil? o)
    nil
    (stack-frame-info o)))

(defn Throwable->map
  "Constructs a data representation for a Throwable with keys:
    :cause - root cause message
    :phase - error phase
    :via - cause chain, with cause keys:
             :type - exception class symbol
             :message - exception message
             :data - ex-data
             :at - top stack element
    :trace - root cause stack elements"
  {:added "1.7"}
  [^Exception o]                                                                                                 ;;; ^Throwable
  (let [base (fn [^Exception t]                                                                                  ;;; ^Throwable
               (merge {:type (symbol (.FullName (class t)))}                                                     ;;; .getName
                 (when-let [msg (.Message t)]                                                                    ;;; .getLocalizedMessage
                   {:message msg})
                 (when-let [ed (ex-data t)]
                   {:data ed})
                 (let [st (.GetFrames (System.Diagnostics.StackTrace. t true))]                                  ;;; (.getStackTrace t)
                   (when (and st (pos? (alength st)))                                                            ;;; added the 'and st' because we may get a null back instread of an array.
                     {:at (StackTraceElement->vec (aget st 0))}))))                                              ;;; aget
        via (loop [via [], ^Exception t o]                                                                       ;;; ^Throwable
              (if t
                (recur (conj via t) (.InnerException t))                                                         ;;; .getCause
                via))
        ^Exception root (peek via)]                                                                              ;;; Throwable
    (merge {:via (vec (map base via))
            :trace (vec (map StackTraceElement->vec
		                     (.GetFrames (System.Diagnostics.StackTrace. (or root o) true))))}                   ;;;  .getStackTrace ^Throwable  
      (when-let [root-msg (.Message root)]                                                                       ;;; (.getLocalizedMessage root)
        {:cause root-msg})
      (when-let [data (ex-data root)]
        {:data data})
      (when-let [phase (-> o ex-data :clojure.error/phase)]
        {:phase phase}))))

(defn print-throwable [^Exception o ^System.IO.TextWriter w]                                                     ;;; ^Throwable
  (.Write w "#error {\n :cause ")
  (let [{:keys [cause data via trace]} (Throwable->map o)
        print-via #(do (.Write w "{:type ")
		               (print-method (:type %) w)
					   (.Write w "\n   :message ")
                       (print-method (:message %) w)
             (when-let [data (:data %)]
               (.Write w "\n   :data ")
               (print-method data w))
             (when-let [at (:at %)]
               (.Write w "\n   :at ")
               (print-method (:at %) w))
             (.Write w "}"))]
    (print-method cause w)
    (when data
      (.Write w "\n :data ")
      (print-method data w))
    (when via
      (.Write w "\n :via\n [")
      (when-let [fv (first via)]
	    (print-via fv)
        (doseq [v (rest via)]
          (.Write w "\n  ")
		  (print-via v)))
      (.Write w "]"))
    (when trace
      (.Write w "\n :trace\n [")
      (when-let [ft (first trace)]
        (print-method ft w)
        (doseq [t (rest trace)]
          (.Write w "\n  ")
          (print-method t w)))
      (.Write w "]")))
  (.Write w "}"))

(defmethod print-method Exception [^Exception o ^System.IO.TextWriter w]                                         ;;; Throwable ^Throwable
  (print-throwable o w))

(defmethod print-method clojure.lang.TaggedLiteral [o ^System.IO.TextWriter w]
  (.Write w "#")
  (print-method (:tag o) w)
  (.Write w " ")
  (print-method (:form o) w))

(defmethod print-method clojure.lang.ReaderConditional [o ^System.IO.TextWriter w]
  (.Write w "#?")
  (when (:splicing? o) (.Write w "@"))
  (print-method (:form o) w))

(def ^{:private true :dynamic true} print-initialized true)  

;;;(defn ^java.io.PrintWriter PrintWriter-on
;;;  "implements java.io.PrintWriter given flush-fn, which will be called
;;;  when .flush() is called, with a string built up since the last call to .flush().
;;;  if not nil, close-fn will be called with no arguments when .close is called.
;;;  autoflush? determines if the PrintWriter will autoflush, false by default."
;;;  {:added "1.10"}
;;;  ([flush-fn close-fn]
;;;   (PrintWriter-on flush-fn close-fn false))
;;;  ([flush-fn close-fn autoflush?]
;;;   (let [sb (StringBuilder.)]
;;;    (-> (proxy [Writer] []
;;;          (flush []
;;;                 (when (pos? (.length sb))
;;;                   (flush-fn (.toString sb)))
;;;                 (.setLength sb 0))
;;;          (close []
;;;                 (.flush ^Writer this)
;;;                 (when close-fn (close-fn))
;;;                 nil)
;;;          (write [str-cbuf off len]
;;;                 (when (pos? len)
;;;                   (if (instance? String str-cbuf)
;;;                     (.append sb ^String str-cbuf ^int off ^int len)
;;;                     (.append sb ^chars str-cbuf ^int off ^int len)))))
;;;        java.io.BufferedWriter.
;;;        java.io.PrintWriter.)))

(defn ^System.IO.TextWriter PrintWriter-on
  ([flush-fn close-fn]
   (PrintWriter-on flush-fn close-fn false))
  ([flush-fn close-fn autoflush?]                       ;;; there is no autflush property for StringWriter
   (proxy [System.IO.StringWriter] []  
     (Flush [] 
	      (let [^System.IO.StringWriter this this]
	       (proxy-super Flush))
	      (let [sb (.GetStringBuilder ^System.IO.StringWriter this)]
            (when (pos? (.Length sb))
              (flush-fn (.ToString sb)))
            (.set_Length sb 0)))
     (Close []
          (.Flush ^System.IO.StringWriter this)
          (when close-fn (close-fn))
		  (let [^System.IO.StringWriter this this]
		    (proxy-super Close))
		  nil))))


