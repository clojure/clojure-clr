;   Copyright (c) Rich Hickey. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.

;;; stacktrace.clj: print Clojure-centric stack traces

;; by Stuart Sierra
;; January 6, 2009

(ns ^{:doc "Print stack traces oriented towards Clojure, not Java."
       :author "Stuart Sierra"}
  clojure.stacktrace)

(set! *warn-on-reflection* true)

(defn root-cause
  "Returns the last 'cause' Throwable in a chain of Throwables."
  {:added "1.1"} 
  [^Exception tr]                                                   ;;; Throwable
  (if-let [cause (.InnerException tr)]                              ;;; .getCause
    (recur cause)
    tr))

(defn print-trace-element
  "Prints a Clojure-oriented view of one element in a stack trace."
  {:added "1.1"} 
  [^System.Diagnostics.StackFrame e]                                ;;; StackTraceElement
  (let [class (or (when-let [m (.. e  (GetMethod))]
                    (when-let [t (.ReflectedType m)]
                      (.FullName t)))
                  "")
	method (or (when-let [m (.GetMethod e)]
                     (.Name m))
                   "")]
    (let [match (re-matches #"^([A-Za-z0-9_.-]+)\$(\w+)__\d+$" (str class))]
      (if (and match (= "invoke" method))
	(apply printf "%s/%s" (rest match))
	(printf "%s.%s" class method))))
  (printf " (%s:%d)" (or (.GetFileName e) "") (.GetFileLineNumber e)))

(defn print-throwable1
  "Prints the class and message of a Throwable. Prints the ex-data map
  if present."
  {:added "1.1"} 
  [^Exception tr]                                             ;;; Throwable
  (printf "%s: %s" (.FullName (class tr)) (.Message tr))      ;;; .getName .getMessage
  (when-let [info (ex-data tr)]
    (newline)
    (pr info)))

(defn print-stack-trace
  "Prints a Clojure-oriented stack trace of tr, a Throwable.
  Prints a maximum of n stack frames (default: unlimited).
  Does not print chained exceptions (causes)."
  {:added "1.1"} 
  ([tr] (print-stack-trace tr nil))
  ([^Exception tr n]                                                     ;;; Throwable
     (let [st (.GetFrames (System.Diagnostics.StackTrace. tr true))]     ;;;  (.getStackTrace tr)]
       (print-throwable1 tr)
       (newline)
       (print " at ") 
       (if-let [e (first st)]
         (print-trace-element e)
         (print "[empty stack trace]"))
       (newline)
       (doseq [e (if (nil? n)
		   (rest st)
		   (take (dec n) (rest st)))]
	 (print "    ")
	 (print-trace-element e)
	 (newline)))))

(defn print-cause-trace
  "Like print-stack-trace but prints chained exceptions (causes)."
  {:added "1.1"} 
  ([^Exception tr] (print-cause-trace tr nil))
  ([^Exception tr n]                                                     ;;; Throwable
     (print-stack-trace tr n)
     (when-let [cause (.InnerException tr)]                              ;;; (.getTrace tr)]
       (print "Caused by: " )
       (recur cause n))))

(defn e
  "REPL utility.  Prints a brief stack trace for the root cause of the
  most recent exception."  
  {:added "1.1"} 
  []
  (print-stack-trace (root-cause *e) 8))
