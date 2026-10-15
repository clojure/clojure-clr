;   Copyright (c) Rich Hickey. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.
(assembly-load "clojure.test_clojure.genclass.examples.ExampleClass")  ;;; added because we do not have automatic class loading
(ns ^{:doc "Tests for clojure.core/gen-class"
      :author "Stuart Halloway, Daniel Solano Gómez"}
  clojure.test-clojure.genclass
  (:require clojure.test_clojure.genclass.examples          
            clojure.test_clojure.genclass.examples3    ;;; CLR-specific
            clojure.test_clojure.genclass.examples4)   ;;; CLR-specific
  (:use clojure.test clojure.test-helper)
  (:import [clojure.test_clojure.genclass.examples
            ExampleClass
            ;;;ExampleAnnotationClass
			;;;ProtectedFinalTester
            ArrayDefInterface
            ArrayGenInterface
            ImportedTypeHintInterface
            MemoryStreamExample                        ;;; CLR-specific
            MethodsExample                             ;;; CLR-specific
            CLJCLR221Disposable                        ;;; CLR-specific
            CLJCLR221Plain]                            ;;; CLR-specific
           [clojure.test_clojure.genclass.examples3    ;;; CLR-specific
            MemoryStreamFlushOverload]                 ;;; CLR-specific
           [clojure.test_clojure.genclass.examples4    ;;; CLR-specific
            CLJCLR222AbstractExtension]                ;;; CLR-specific
           [System.Reflection                          ;;; CLR-specific
            BindingFlags]                              ;;; CLR-specific
           
           ;;;[java.lang.annotation ElementType
           ;;;                      Retention
           ;;;                      RetentionPolicy
         ));;;                      Target]))

;; Framework 4.x cannot handle example2, so it is not compiled, meaning these must be guarded
(compile-when (>= (:major dotnet-version) 5)

(require 'clojure.test_clojure.genclass.examples2)
(import '[clojure.test_clojure.genclass.examples2  MemoryStreamReadOverload]         )
        
)  ;; compile-when

(deftest arg-support
  (let [example (ExampleClass.)
        o (Object.)]
    (is (= "foo with o, o" (.foo example o o)))
    (is (= "foo with o, i" (.foo example o (int 1))))
    (is (thrown? NotImplementedException (.foo example o)))))         ;;; java.lang.UnsupportedOperationException

(deftest name-munging
  (testing "mapping from Java fields to Clojure vars"
    (is (= #'clojure.test-clojure.genclass.examples/-foo-Object-Int32        ;;; -foo-Object-int
           (get-field ExampleClass 'foo_Object_Int32__var)))                 ;;; foo_Object_int__var
    ;;;(is (= #'clojure.test-clojure.genclass.examples/-ToString                ;;; -toString
  ));;;       (get-field ExampleClass 'ToString__var)))))                       ;;; toString__var ------ TODO: Figure out why JVM can find this var, we can't.

;todo - fix this, it depends on the order of things out of a hash-map
#_(deftest test-annotations
  (let [annot-class ExampleAnnotationClass
        foo-method          (.getDeclaredMethod annot-class "foo" (into-array [String]))]
    (testing "Class annotations:"
      (is (= 2 (count (.getDeclaredAnnotations annot-class))))
      (testing "@Deprecated"
        (let [deprecated (.getAnnotation annot-class Deprecated)]
          (is deprecated)))
      (testing "@Target([])"
        (let [resource (.getAnnotation annot-class Target)]
          (is (= 0 (count (.value resource)))))))
    (testing "Method annotations:"
      (testing "@Deprecated void foo(String):"
        (is (= 1 (count (.getDeclaredAnnotations foo-method))))
        (is (.getAnnotation foo-method Deprecated))))
    (testing "Parameter annotations:"
      (let [param-annots (.getParameterAnnotations foo-method)]
        (is (= 1 (alength param-annots)))
        (let [first-param-annots (aget param-annots 0)]
          (is (= 2 (alength first-param-annots)))
          (testing "void foo(@Retention(…) String)"
            (let [retention (aget first-param-annots 0)]
              (is (instance? Retention retention))
              (= RetentionPolicy/SOURCE (.value retention))))
          (testing "void foo(@Target(…) String)"
            (let [target (aget first-param-annots 1)]
              (is (instance? Target target))
              (is (= [ElementType/TYPE ElementType/PARAMETER] (seq (.value target)))))))))))

(deftest genclass-option-validation
  (is (fails-with-cause? ArgumentException #"Not a valid method name: has-hyphen"                            ;;; IllegalArgumentException
        (@#'clojure.core/validate-generate-class-options {:methods '[[fine [] void] [has-hyphen [] void]]}))))

;;;(deftest protected-final-access
;;;  (let [obj (ProtectedFinalTester.)]
;;;    (testing "Protected final method visibility"
;;;      (is (thrown? IllegalArgumentException (.findSystemClass obj "java.lang.String"))))
;;;    (testing "Allow exposition of protected final method."
;;;      (is (= String (.superFindSystemClass obj "java.lang.String"))))))

(deftest interface-array-type-hints
  (let [array-types       {:ints     (class (int-array 0))     :uints (class (uint-array 0))
                           :bytes    (class (byte-array 0))    :sbytes (class (sbyte-array 0))
                           :shorts   (class (short-array 0))   :ushorts (class (ushort-array 0))
                           :chars    (class (char-array 0))   
                           :longs    (class (long-array 0))    :ulongs (class (ulong-array 0))
                           :floats   (class (float-array 0))
                           :doubles  (class (double-array 0))
                           :booleans (class (boolean-array 0))
                           :maps     (class (into-array System.Collections.Hashtable []))}                         ;;; java.util.Map
        array-types       (assoc array-types
                                 :maps-2d (class (into-array (:maps array-types) [])))
        method-with-name  (fn [name methods] (first (filter #(= name (.Name %)) methods)))         ;;; .getName
        parameter-type    (fn [method] (.ParameterType (first (.GetParameters method))))           ;;; (first (.getParameterTypes method)))
        return-type       (fn [method] (.ReturnType method))]                                      ;;; .getReturnType
    (testing "definterface"
      (let [method-with-name #(method-with-name % (.GetMethods ArrayDefInterface))]                    ;;; .getMethods
        (testing "sugar primitive array hints"
          (are [name type] (= (type array-types)
                              (parameter-type (method-with-name name)))
               "takesByteArray"    :bytes              "takesSByteArray"    :sbytes
               "takesCharArray"    :chars
               "takesShortArray"   :shorts             "takesUShortArray"   :ushorts
               "takesIntArray"     :ints               "takesUIntArray"     :uints
               "takesLongArray"    :longs              "takesULongArray"    :ulongs
               "takesFloatArray"   :floats
               "takesDoubleArray"  :doubles
               "takesBooleanArray" :booleans))
        (testing "raw primitive array hints"
          (are [name type] (= (type array-types)
                              (return-type (method-with-name name)))
               "returnsByteArray"    :bytes             "returnsSByteArray"    :sbytes
               "returnsCharArray"    :chars
               "returnsShortArray"   :shorts            "returnsUShortArray"   :ushorts
               "returnsIntArray"     :ints              "returnsUIntArray"     :uints
               "returnsLongArray"    :longs             "returnsULongArray"    :ulongs
               "returnsFloatArray"   :floats
               "returnsDoubleArray"  :doubles
               "returnsBooleanArray" :booleans)))
      (testing "imported class type hints"
        (let [method-with-name #(method-with-name % (.GetMethods ImportedTypeHintInterface))]                   ;;; .getMethods
          (is (= System.Collections.Hashtable (return-type (method-with-name "returnsImportedType"))))          ;;; java.util.Map
          (is (= System.Collections.Hashtable (parameter-type (method-with-name "takesImportedType")))))))      ;;; java.util.Map
    (testing "gen-interface"
      (let [method-with-name #(method-with-name % (.GetMethods ArrayGenInterface))]                             ;;; .getMethods
        (testing "sugar primitive array hints"
          (are [name type] (= (type array-types)
                              (parameter-type (method-with-name name)))
               "takesByteArray"    :bytes            "takesSByteArray"    :sbytes
               "takesCharArray"    :chars
               "takesShortArray"   :shorts           "takesUShortArray"   :ushorts
               "takesIntArray"     :ints             "takesUIntArray"     :uints
               "takesLongArray"    :longs            "takesULongArray"    :ulongs
               "takesFloatArray"   :floats
               "takesDoubleArray"  :doubles
               "takesBooleanArray" :booleans))
        (testing "raw primitive array hints"
          (are [name type] (= (type array-types)
                              (return-type (method-with-name name)))
               "returnsByteArray"    :bytes          "returnsSByteArray"    :sbytes
               "returnsCharArray"    :chars
               "returnsShortArray"   :shorts         "returnsUShortArray"   :ushorts
               "returnsIntArray"     :ints           "returnsUIntArray"     :uints
               "returnsLongArray"    :longs          "returnsULongArray"    :ulongs
               "returnsFloatArray"   :floats
               "returnsDoubleArray"  :doubles
               "returnsBooleanArray" :booleans))))))

;;;(deftest gen-interface-source-file
;;;  (let [classReader (clojure.asm.ClassReader. "clojure.test_clojure.genclass.examples.ArrayGenInterface")
;;;        sourceFile (StringBuilder.)
;;;        sourceVisitor (proxy [clojure.asm.ClassVisitor] [clojure.asm.Opcodes/ASM4 nil]
;;;                        (visitSource [source debug] (.append sourceFile source)))]
;;;    (.accept classReader sourceVisitor 0)
;;;    (is (= "examples.clj" (str sourceFile)))))

(deftest array-descriptors->class
  (are [descr c] (= (#'clojure.core/the-class descr) c)
    "System.Guid[]" System.Guid/1                           ;;; "[Ljava.util.UUID;" java.util.UUID/1
    'String System.String                                   ;;; java.lang.String
    'String/1 System.String/1                               ;;; java.lang.String/1
    `System.Guid System.Guid                                ;;; 'java.util.UUID java.util.UUID
    'System.Guid/2 System.Guid/2                            ;;; 'java.util.UUID/2 java.util.UUID/2
    'int/1 int/1
    'boolean/9 bool/9))

#_(import '[java.lang.reflect Method Modifier])

#_(deftest interface-methods  
  (let [obj (InterfaceDefaultTest.)]    
    (is (= "bar impl" (JDK8InterfaceMethods/.bar obj)))    
    (is (= "default impl" (JDK8InterfaceMethods/.foo obj)))    
    ;; bind override var target
    (intern 'clojure.test-clojure.genclass.examples 'intf-foo (constantly "override"))
    (is (= "override" (JDK8InterfaceMethods/.foo obj))))  
  (testing "no forwarder methods for static methods on interfaces"
    (let [statics (set (->> (.getMethods JDK8InterfaceMethods)
                            (filter #(Modifier/isStatic (Method/.getModifiers %)))
                            (map Method/.getName)))
          forwarders (set (map Method/.getName (.getMethods InterfaceDefaultTest)))]
      (is (empty? (set/intersection statics forwarders))))))



;;;;;;;;;;;;;;;; CLR-specific ;;;;;;;;;;;;;;;;;;;;;

(compile-when (>= (:major dotnet-version) 5)
   
(deftest CLJCLR-219-genclass-byref-like-handled
  (testing "Can access type even with byref-like parameters on (uncalled) methods"
    (let [s (MemoryStreamExample.) 
          w (System.IO.StreamWriter. s)]
      (.Write w "hello")
      (.Flush w)
      (is (= 5 (.Length s)))))

  (testing "Can call method -- regression test"
    (let [m (MethodsExample.)]
      (is (= "hi" (.hello m))))))

(deftest CLJCLR-218-runtime-async-stubs
  (testing "Calls work (byte buffer)"
    (let [m (doto (MemoryStreamExample.)
               (.Write (byte-array [1 2 3 4]) 0 4)
               (.set_Position 0))
          buf (byte-array 8)]
       (is (= 4 (.Result (.ReadAsync m buf 0 8 System.Threading.CancellationToken/None))))
       (is (= [1 2 3 4 0 0 0 0] buf))))

  (testing "Calls work (ReadOnlyMemory buffer)"
    (let [m (doto (MemoryStreamExample.)
               (.Write (byte-array [1 2 3 4]) 0 4)
               (.set_Position 0))
          buf (|System.Memory[Byte]|. (byte-array 8))]
       (is (= 4 (.Result (.AsTask (.ReadAsync m buf System.Threading.CancellationToken/None)))))))

    (let [ms  (MemoryStreamExample.)
          tsk (.WriteAsync ms (|System.ReadOnlyMemory[Byte]|. (byte-array [1 2])) System.Threading.CancellationToken/None)]
      (.Wait (.AsTask tsk))
      (is (= 2 (.Length ms)))))

   (testing "arity overloads"
     (let [m (MemoryStreamReadOverload.)
           buf (byte-array 8)]
         (is (= 42 (.Result (.ReadAsync m buf 0 1000 System.Threading.CancellationToken/None))))
         (is (= 43 (.Result (.AsTask (.ReadAsync m (|System.Memory[Byte]|. buf) System.Threading.CancellationToken/None)))))))

    (testing "Wait called by framework instead of me"
      (let [p (MemoryStreamExample.)
            s (String. \a 100000)
            path (System.IO.Path/Combine (System.IO.Path/GetTempPath) (str "clj-test-" (.ToString (Guid/NewGuid) "N")))
            _ (spit path s)
            fs  (System.IO.FileStream. path System.IO.FileMode/Open)]
        (try
           (.Wait (.CopyToAsync fs p))
           (is (= 100000 (.Length p)))
           (finally 
             (.Close fs)
             (System.IO.File/Delete path)))))

   (testing "Flush call with no overload"
     (let [m (MemoryStreamExample.)]
       (is (nil? (.Wait (.FlushAsync m System.Threading.CancellationToken/None)))))

    (testing "Flush call with overload"
      (let [m (MemoryStreamFlushOverload.)]
        (is (nil? (.Wait (.FlushAsync m System.Threading.CancellationToken/None)))))))
)

(deftest CLJCLR-221-disposable-interface
  (testing "Can implement IDisposable and call Dispose"
    (let [d (CLJCLR221Disposable.)]
      (is (= (platform-newlines "disposed\n") (with-out-str (.Dispose ^IDisposable d))))))
  (testing "A plain gen-class does not declare Finalize" 
    (is (nil? (.GetMethod CLJCLR221Plain "Finalize" (enum-or BindingFlags/Instance BindingFlags/NonPublic BindingFlags/Public BindingFlags/DeclaredOnly))))))

(deftest CLJCLR-222-abstract-extension
  (testing "Can extend an abstract class and call the method"
    (let [e (CLJCLR222AbstractExtension.)]
      (is (.CanRead e))
      (is (thrown? NotImplementedException (.Flush e))))))