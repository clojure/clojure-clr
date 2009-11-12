;   Copyright (c) David Miller. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.

;;; DM: This is one of the few bootstrap *.clj files where I did not even try to do a line-by-line
;;;     modification of the JVM version.  Too many differences.
;;; I put more of the support into C# rather than in Clojure, just so I could bang out the code quicker.
;;  This could be redone eventually. 



 (in-ns 'clojure.core)
 
 (import '(System.Reflection ConstructorInfo))

 ;;; The options-handling code here is taken from the JVM version.  
 
 ;;(defn- non-private-methods [#^Type c]
 ;; (loop [mm {}
 ;;        considered #{}
 ;;        c c]
 ;;   (if c
 ;;     (let [[mm considered]
 ;;           (loop [mm mm
 ;;                  considered considered
 ;;                  meths (seq (. c (GetMethods)))]
 ;;             (if meths
 ;;               (let [#^System.Reflection.MethodInfo meth (first meths)
 ;;                     mk (method-sig meth)]
 ;;                 (if (or (considered mk)
 ;;                         (not (or (.IsPublic meth) (.IsFamily meth)))
 ;;                         (.IsStatic meth)
 ;;                         (.IsFinal meth)
 ;;                         (= "Dispose" (.Name meth)))
 ;;                   (recur mm (conj considered mk) (next meths))
 ;;                   (recur (assoc mm mk meth) (conj considered mk) (next meths))))
 ;;               [mm considered]))]
 ;;       (recur mm considered (.BaseType c )))
 ;;     mm)))
 
 
 (defn- ctor-sigs [#^Type super]
  (for [#^ConstructorInfo ctor (.GetConstructors super)
        :when (not (.IsPrivate ctor))]
    (apply vector (map #(.ParameterType %) (.GetParameters ctor)))))
 
 
 (def #^{:private true} prim->class
     {'int Int32
      'long Int64
      'float Single
      'double Double
      ;'void  Void
      'short Int16 
      'boolean Boolean
      'byte Byte
      'char Char})

 
 (defn- #^Type the-class [x]					;;; #^Class
  (cond 
   (class? x) x
   (contains? prim->class x) (prim->class x)
   :else (let [strx (str x)]
           (clojure.lang.RT/classForName 
            (if (some #{\.} strx)
              strx
              (str "System." strx))))))         ;;;(str "java.lang." strx))))))
 
;;(defn- escape-class-name [#^Type c]
;;  (.. (.Name c) 
;;      (Replace "[]" "<>")))

;;(defn- overload-name [mname pclasses]
;;  (if (seq pclasses)
;;    (apply str mname (interleave (repeat \-) 
;;                                 (map escape-class-name pclasses)))
;;    (str mname "-void")))
  
 (defn- generate-class [options-map]
   (let [default-options {:prefix "-" :load-impl-ns true :impl-ns (ns-name *ns*)}
        {:keys [name extends implements constructors methods main factory state init exposes 
                exposes-methods prefix load-impl-ns impl-ns post-init]} 
          (merge default-options options-map)
        name (str name)
        super (if extends (the-class extends) Object)
        interfaces (map the-class implements)
        supers (cons super interfaces)
        ctor-sig-map (doall (or constructors (zipmap (ctor-sigs super) (ctor-sigs super))))
        class-mapper (fn [coll] (doall (map the-class coll)))
        ctor-sig-type-map (doall (zipmap (doall (map class-mapper (keys ctor-sig-map))) (doall (map class-mapper (vals ctor-sig-map)))))
        cname (. name (Replace "." "/"))
        pkg-name name
        impl-pkg-name (str impl-ns)
        impl-cname (.. impl-pkg-name (Replace "." "/") (Replace \- \_))

        ;;;ctype (. Type (getObjectType cname))
        ;;;iname (fn [#^Class c] (.. Type (getType c) (getInternalName)))
        ;;;totype (fn [#^Class c] (. Type (getType c)))
        ;;;to-types (fn [cs] (if (pos? (count cs))
        ;;;                    (into-array (map totype cs))
        ;;;                    (make-array Type 0)))
        ;;;obj-type #^Type (totype Object)
        ;;;arg-types (fn [n] (if (pos? n)
        ;;;                    (into-array (replicate n obj-type))
        ;;;                    (make-array Type 0)))
        ;;;super-type #^Type (totype super)
        init-name (str init)
        post-init-name (str post-init)
        factory-name (str factory)
        state-name (str state)
        main-name "main"
        methods (map (fn [x] [(nth x 0) 
                              (map the-class (nth x 1)) 
                              (the-class (nth x 2)) 
                              (:static ^x)]) 
                         methods)
 
        ;;var-name (fn [s] (str s "__var"))
        ;;class-type  (totype Class)
        ;;rt-type  (totype clojure.lang.RT)
        ;;var-type #^Type (totype clojure.lang.Var)
        ;;ifn-type (totype clojure.lang.IFn)
        ;;iseq-type (totype clojure.lang.ISeq)
        ;;ex-type  (totype java.lang.UnsupportedOperationException)
        ;;all-sigs (distinct (concat (map #(let[[m p] (key %)] {m [p]}) (mapcat non-private-methods supers))
        ;;                           (map (fn [[m p]] {(str m) [p]}) methods)))
        ;;sigs-by-name (apply merge-with concat {} all-sigs)
        ;;overloads (into {} (filter (fn [[m s]] (next s)) sigs-by-name)) 
        ;;var-fields (concat (when init [init-name]) 
        ;;                   (when post-init [post-init-name])
        ;;                   (when main [main-name])
        ;;                   ;(when exposes-methods (map str (vals exposes-methods)))
        ;;                   (distinct (concat (keys sigs-by-name)
        ;;                                     (mapcat (fn [[m s]] (map #(overload-name m (map the-class %)) s)) overloads)
        ;;                                     (mapcat (comp (partial map str) vals val) exposes))))
        ;;
      ]  
	(clojure.lang.GenClass/GenerateClass
		name super (seq interfaces)
		(seq ctor-sig-map) (seq ctor-sig-type-map) (seq methods)
		exposes exposes-methods  
		prefix  (. clojure.lang.RT booleanCast main) 
		factory-name state-name 
		init-name post-init-name 
		impl-cname impl-pkg-name 
		(. clojure.lang.RT booleanCast  load-impl-ns))))
		
	 
 (defmacro gen-class 

  [& options]
    (let [x *compile-files*]
      (print (str "*compile-files* = " x "\n"))
      ;(when *compile-files*
        (let [options-map (apply hash-map options)]
          `'~(generate-class options-map))))
      ;  )
        
        
;;;            [cname bytecode] (generate-class options-map)]
;;;        (clojure.lang.Compiler/writeClassFile cname bytecode))))