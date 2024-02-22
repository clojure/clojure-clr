;   Copyright (c) Rich Hickey. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.

(ns clojure.clr.process
  "A process invocation API wrapping the Java System.Diagnostic.Process API.

   The primary function here is 'start' which starts a process and handles the
   streams as directed. It returns a map that contains keys to access the streams
   (if available) and the Java Process object. It is also deref-able to wait for
   process exit.

   Use ‘slurp' to capture the output of a process stream, and 'ok?’ to wait for a
   non-error exit. The 'exec' function handles the common case of `start'ing a
   process, waiting for process exit, slurp, and return stdout."
  (:require
   [clojure.clr.io :as cio]
   [clojure.string :as str])
  (:import
    [System.IO StringWriter FileInfo]
    [System.Diagnostics Process ProcessStartInfo]
    [clojure.lang IDeref IBlockingDeref]))

(set! *warn-on-reflection* true)


(def ^:private ^FileInfo null-file
  (delay
    (cio/file-info
     (if (= (.Platform Environment/OSVersion) PlatformID/Win32NT)
       "NUL"
       "/dev/null"))))

(defn start
  "Start an external command, defined in args.
  The process environment vars are inherited from the parent by
  default (use :clear-env to clear them).

  If needed, provide options in map as first arg:
    :redirct-in  - if value is true-ish, redirect standard input 
    :redirect-out - if value is true-ish, redirect standard output
    :redirect-err - if vlaue is true-ish, redirect standard error
    :dir - current directory when the process runs (default=\".\")
    :clear-env - if true, remove all inherited parent env vars
    :env - {env-var value} of environment variables to set (all strings)

  Returns an ILookup containing the System.Diagnostics.Process in :process and the
  streams :in :out :err. The map is also an IDeref that waits for process exit
  and returns the exit code."
  {:added "1.12"}
  [& opts+args]
  (let [[opts command] (if (map? (first opts+args))
                         [(first opts+args) (rest opts+args)]
                         [{} opts+args])
        {:keys [redirect-in redirect-out redirect-err dir env clear-env]
         :or {redirect-in false, redirect-out false, redirect-err false, dir "."}} opts
        si (ProcessStartInfo. ^String (first command) ^String (str/join " " (rest command)))
        si-env (.EnvironmentVariables si)
        process (Process.)]

    (.set_UseShellExecute si false)
    (.set_RedirectStandardInput si (boolean redirect-in))
    (.set_RedirectStandardOutput si (boolean redirect-out))
    (.set_RedirectStandardError si (boolean redirect-err))
    (when clear-env
       (.Clear si-env))
    (when env
      (run! (fn [[k v]] (.Add si-env k v)) env))

    (let [process (Process/Start si)
          m {:process process
             :in (when redirect-in (.get_StandardInput process))
             :out (when redirect-out (.get_StandardOutput process))
             :err (when redirect-err (.get_StandardError process))}]
      (reify
        clojure.lang.ILookup
        (valAt [_ key] (get m key))
        (valAt [_ key not-found] (get m key not-found))

        IDeref
        (deref [_] 
          (.WaitForExit process)
          (.ExitCode process))

        IBlockingDeref
        (deref [_ timeout timeout-value] 
          (if (.WaitForExit process (int timeout))
            (.ExitCode process)
            timeout-value))))))

(defn ok?
  "Given the map returned from 'start', wait for the process to exit
  and then return true on success"
  {:added "1.12"}
  [process-map]
  (let [p ^Process (:process process-map)]
	(.WaitForExit p)
	(zero? (.ExitCode p))))



#_(defn io-task
  {:skip-wiki true}
  [f]
  (let [f (bound-fn* f)
        fut (clojure.lang.Future f)]
    (reify
      clojure.lang.IDeref
      (deref [_] (#'clojure.core/deref-future fut))
      clojure.lang.IBlockingDeref
      (deref
        [_ timeout-ms timeout-val]
        (#'clojure.core/deref-future fut timeout-ms timeout-val))
      clojure.lang.IPending
      (isRealized [_] (.isDone fut))
      clojure.lang.Future
      (get [_] (.get fut))
      (get [_ timeout unit] (.get fut timeout unit))
      (isCancelled [_] (.isCancelled fut))
      (isDone [_] (.isDone fut))
      (cancel [_ interrupt?] (.cancel fut interrupt?)))))

(defn io-task
  {:skip-wiki true}
  [f]
  (let [f (bound-fn* f)
        fut (clojure.lang.Future. f)]
    (reify
      clojure.lang.IDeref
      (deref [_] (.get fut))
      clojure.lang.IBlockingDeref
      (deref
        [_ timeout-ms timeout-val]
        (.get fut timeout-ms timeout-val))
      clojure.lang.IPending
      (isRealized [_] (.isDone fut)))))
  


(defn exec
  "Execute a command and on successful exit, return the captured output,
  else throw RuntimeException. Args are the same as 'start' and options
  if supplied override the default 'exec' settings."
  {:added "1.12"}
  [& opts+args]
  (let [[opts command] (if (map? (first opts+args))
                         [(first opts+args) (rest opts+args)]
                         [{} opts+args])
        opts (merge {:redirect-err true :redirect-out true} opts)]
    (let [state (apply start opts command)
          captured (io-task #(slurp (:out state)))]
      (if (ok? state)
        @captured
        (throw (Exception. (str "Process failed with exit=" (.ExitCode ^Process (:process state)))))))))

(comment
  ;; shell out and inherit the i/o
  (start {:out :inherit, :err :stdout} "ls" "-l")

  ;; write out and err to files, wait for process to exit, return exit code
  @(start {:out (to-file "out") :err (to-file "err")} "ls" "-l")

  ;; capture output to string
  (-> (start "ls" "-l") :out slurp)

  ;; with exec
  (exec "ls" "-l")

  ;; read input from file
  (exec {:in (from-file "deps.edn")} "wc" "-l")
  )