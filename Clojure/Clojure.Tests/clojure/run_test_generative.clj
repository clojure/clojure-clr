
(assembly-load-from "clojure.tools.reader.dll")
(assembly-load-from "clojure.tools.namespace.dll")
(assembly-load-from "clojure.data.generators.dll")
(assembly-load-from "clojure.test.generative.dll")
(assembly-load-from "clojure.test.check.dll")

(when-not (System.Environment/GetEnvironmentVariable "clojure.test.generative.msec")  ;;; System/getProperty
  (System.Environment/SetEnvironmentVariable "clojure.test.generative.msec" "60000")) ;;; System/setProperty 
(require '[clojure.test.generative.runner :as runner]
         '[clojure.test.generative.clr :as clr]
         '[clojure.tools.namespace.find :as ns])


; clojure.test.generative does not have way to filter namespaces.
; runner/-main runs all specs in all namespaces in the given directories.
; runner/run-suite takes a seq of tests.
; the only function for producing such a collection does all in a directory, and loads the namespaces in the process.
; we need to _not_ run specs because we cannot load some namespaces that rely on AOT-compilation.
; So I'm duplicating runner/dir-tests to allow filtering namespaces before they are loaded.
; unfortunately, I need a private function to make even that happen.


(defn ns-tests
  "Returns all tests in namespaces"
   [nses]
  (let [load (fn [s] (require s) s)]
    (->> nses
         (map load)
         (apply #'runner/find-vars-in-namespaces)
         (mapcat runner/get-tests))))

(defn my-runner
  "modifed form runner/-main"
  [& nses]
  (if (seq nses)
    (try
     (let [result (runner/run-suite (runner/config) (ns-tests nses))]
       (println "\n" result)
       (Environment/Exit (:failures result)))	                  ;;; System/exit
     (catch Exception t                                           ;;; Throwable
	   (prn (str "Exception: " (.Message t)))
       (clr/print-stack-trace t)                                  ;;; (.printStackTrace t)
       (Environment/Exit -1))                                     ;;; System/exit
     (finally
      (shutdown-agents)))
    (do
      (println "Specify at least one namespace with tests")
      (Environment/Exit -1))))   

(def namespaces (remove (read-string (or (System.Environment/GetEnvironmentVariable "clojure.test-clojure.exclude-namespaces") "#{}"))
                        (ns/find-namespaces-in-dir (System.IO.DirectoryInfo. "clojure/test_clojure"))))


(println (read-string (or (System.Environment/GetEnvironmentVariable "clojure.test-clojure.exclude-namespaces") "#{}")))
(println namespaces)
(apply my-runner namespaces)