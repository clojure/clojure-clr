;   Copyright (c) Rich Hickey. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.

(ns clojure.clr.process
  "A process invocation API wrapping the Java System.Diagnostic.Process API.

   The primary function is 'start' which starts a process and handles the
   streams as directed. It returns the Process object. Use 'exit-ref' to wait
   for completion and receive the exit value, and ‘stdout', 'stderr', 'stdin'
   to access the process streams. The 'exec' function handles the common case
   to 'start' a process, wait for process exit, and return stdout."
  (:require
   [clojure.clr.io :as cio]
   [clojure.string :as str])
  (:import
    [System.IO FileInfo StreamReader StreamWriter]
    #_[System.Diagnostics Process ProcessStartInfo]     ;;; defer until assembly loaded
    [clojure.lang IDeref IBlockingDeref]))

(try 
  (assembly-load-from (str clojure.lang.RT/SystemRuntimeDirectory "System.Diagnostics.Process.dll"))
  (catch Exception e))  ;; failing silently okay -- if we need it and didn't find it, a type reference will fail later

(import '[System.Diagnostics Process ProcessStartInfo])

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

  Returns the java.lang.Process."
  {:added "1.12"}
  ^Process [& opts+args]
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

    (Process/Start si)))

(defn stdin
  "Given a process, return the stdin of the external process (an OutputStream)"
  ^StreamWriter [^Process process]
  (.get_StandardInput process))

(defn stdout
  "Given a process, return the stdout of the external process (an InputStream)"
  ^StreamReader [^Process process]
  (.get_StandardOutput process))

(defn stderr
  "Given a process, return the stderr of the external process (an InputStream)"
  ^StreamReader [^Process process]
  (.get_StandardError process))

(defn exit-ref
  "Given a Process (the output of 'start'), return a reference that can be
  used to wait for process completion then returns the exit value."
  [^Process process]
  (reify
    IDeref
      (deref [_] 
        (.WaitForExit process)
        (.ExitCode process))
    IBlockingDeref
      (deref [_ timeout timeout-value] 
        (if (.WaitForExit process (int timeout))
          (.ExitCode process)
          timeout-value))))

#_(defn io-task
  {:skip-wiki true}
  [^Runnable f]
  (let [f (bound-fn* f)
        fut (.submit ^ExecutorService io-executor ^Callable f)]
    (reify
      clojure.lang.IDeref
      (deref [_] (#'clojure.core/deref-future fut))
      clojure.lang.IBlockingDeref
      (deref
        [_ timeout-ms timeout-val]
        (#'clojure.core/deref-future fut timeout-ms timeout-val))
      clojure.lang.IPending
      (isRealized [_] (.isDone fut))
      java.util.concurrent.Future
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
        opts (merge {:err :inherit} opts)]
    (let [proc (apply start opts command)
          captured (io-task #(slurp (stdout proc)))
          exit (deref (exit-ref proc))]
      (if (zero? exit)
        @captured
        (throw (Exception. (str "Process failed with exit=" exit)))))))

(comment
  ;; shell out and inherit the i/o
  (start {:out :inherit, :err :stdout} "ls" "-l")

  ;; write out and err to files, wait for process to exit, return exit code
  @(exit-ref (start {:out (to-file "out") :err (to-file "err")} "ls" "-l"))

  ;; capture output to string
  (-> (start "ls" "-l") stdout slurp)

  ;; with exec
  (exec "ls" "-l")

  ;; read input from file
  (-> (exec {:in (from-file "deps.edn")} "wc" "-l") clojure.string/trim parse-long)
  )