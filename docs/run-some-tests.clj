(assembly-load-from "clojure.tools.namespace.dll")
(assembly-load-from "clojure.data.generators.dll")
(assembly-load-from "clojure.test.generative.dll")
(assembly-load-from "clojure.test.check.dll")

;;;(System/setProperty "java.awt.headless" "true")
(require
 '[clojure.test :as test]
 '[clojure.tools.namespace.find :as ns])
 
(def excludes #{
	'clojure.test-clojure.genclass.examples 
	'clojure.test-clojure.protocols.examples 
	'clojure.test-clojure.attributes 
	'clojure.test-clojure.compilation.load-ns 
	'clojure.test-clojure.compilation.line-number-examples
	
	'clojure.test-clojure.compilation
	'clojure.test-clojure.genclass
	}) 
	
(def namespaces (remove excludes	
                        (ns/find-namespaces-in-dir (System.IO.DirectoryInfo. "clojure/test_clojure")))) 

(doseq [ns namespaces] (print "Loading " (str ns) " ... ") (require ns) (println "done."))

(doseq [ns namespaces] (test/run-tests ns))
