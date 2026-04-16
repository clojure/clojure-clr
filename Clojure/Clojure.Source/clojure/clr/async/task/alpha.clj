(ns clojure.clr.async.task.alpha
  "Task-based async/await interop for .NET 11+ runtime async.
   Provides idiomatic Clojure wrappers around System.Threading.Tasks.Task."
  (:refer-clojure :exclude [await])
  (:import [System.Threading.Tasks Task]))

(alias-type TaskObj |System.Threading.Tasks.Task`1[Object]|)


;;  ── Extracting a Task result ──────────────────────

; This would get a reflection warning, so we don't turn on *warn-on-reflection* until after.
; We WANT a reflection warning -- we need the DLR callsite mechanism on the call to .GetAwaiter.
; If we tag the task parameter as ^Task, we always pick up Task.GetAwaiter(), even for generic Task<TResult> tasks.
; That just does not work.

(defn result
  "Blocks the calling thread until the task completes and returns its result.
   For Task<T>, returns T. For void Task (non-generic), returns nil.
   Unwraps AggregateException to throw the inner exception directly.

   Usage:
     (t/result (t/->task 42))       ;=> 42
     (t/result (t/completed-task))   ;=> nil
     (t/result (t/async (t/await (t/delay-task 100)) \"done\"))  ;=> \"done\""
 [task]
  (let [t (.GetType ^Object task)]
    (when (.IsGenericType t)
      (let [^Type type-arg (aget (.GetGenericArguments t) 0)]
        (when-not (= "VoidTaskResult" (.Name type-arg))
		    (-> task .GetAwaiter .GetResult))))))	


(set! *warn-on-reflection* true)


;; ── Macros (only these two require macro status) ──────────────────────

(defmacro await
  "Suspends the current ^:async function until the given Task completes.
   Returns the task's result value. Only valid inside a ^:async defn
   or an (async ...) block.

   Usage:
     (require '[clojure.clr.async.task.alpha :as t])
     (t/await (.ReadAllTextAsync System.IO.File path))"
  [task-expr]
  `(await* ~task-expr))

(defmacro async
  "Executes body in an async context, returning Task<object>.
   The body may use (await ...) to suspend on Tasks.

   Usage:
     (t/async
       (let [data (t/await (fetch url))]
         (process data)))"
  [& body]
  `((^:async fn* [] ~@body)))

;; ── Waiting, but staying non-:async ─────────────────────────────────────────────────────────

(defn- convert-timeout
  "Converts a Clojure timeout value to an int for Task.WaitAll.
   Accepts nil (no timeout), a number (milliseconds), or a TimeSpan."
  [timeout]
  (cond
    (nil? timeout) -1
    (number? timeout) (int timeout)
    :else (int (.TotalMilliseconds ^TimeSpan timeout))))

(defn wait-all 
  "Waits for all of the provided Tasks to complete execution.  Returns nil.

   Usage: (t/wait-all tasks) 
          (t/wait-all tasks timeout)
          (t/wait-all task timeout cancellation-token)
  
   timeout values can be any numeric value (cast to int, milliseconds, -1 = no limit) or a TimeSpan"
  ([tasks] (Task/WaitAll ^Task/1 (into-array Task tasks)))
  ([tasks timeout] (Task/WaitAll ^Task/1  (into-array Task tasks) ^int (convert-timeout timeout)))
  ([tasks timeout cancellation-token] (Task/WaitAll ^Task/1  (into-array Task tasks) (convert-timeout timeout) cancellation-token)))


(defn wait-any
  "Waits for any of the provided Task to complete execution.  Returns the task that completed or nil if a timeout occurred.

   Usage: (t/wait-any tasks) 
          (t/wait-any tasks timeout)
          (t/wait-any task timeout cancellation-token)
  
   timeout values can be any numeric value (cast to int, milliseconds, -1 = no limit) or a TimeSpan"
  ([tasks]
    (let [^Task/1  task-array (into-array Task tasks)
          idx (Task/WaitAny task-array)]
        (nth tasks idx)))
  ([tasks timeout]
    (let [^Task/1 task-array (into-array Task tasks)
          idx (Task/WaitAny task-array ^int (convert-timeout timeout))]
        (when-not (= idx -1)
            (nth tasks idx))))
  ([tasks timeout cancellation-token]
    (let [^Task/1 task-array (into-array Task tasks)
          idx (Task/WaitAny task-array (convert-timeout timeout) cancellation-token)]
        (when-not (= idx -1)
            (nth tasks idx)))))

(defn wait-all-results
  "Waits for all of the provided Tasks to complete execution.  Returns a lazy sequence of the result for each task, with nil with for non-generic Tasks.

   Usage: (t/wait-all-results tasks) 
          (t/wait-all-results tasks timeout)
          (t/wait-all-results task timeout cancellation-token)
  
   timeout values can be any numeric value (cast to int, milliseconds, -1 = no limit) or a TimeSpan"

  ([tasks]
    (let [^Task/1 task-array (into-array Task tasks)]
      (Task/WaitAll task-array)
      (map result tasks)))
  ([tasks timeout]
    (let [^Task/1 task-array (into-array Task tasks)]
      (Task/WaitAll task-array ^int (convert-timeout timeout))
      (map result tasks)))
  ([tasks timeout cancellation-token]
    (let [^Task/1 task-array (into-array Task tasks)]
      (Task/WaitAll task-array (convert-timeout timeout) cancellation-token)
      (map result tasks))))

(defn wait-any-result 
 "Waits for any of the provided Task to complete execution.  Returns the result of the task that completed or nil if a timeout occurred.

   Usage: (t/wait-any-results tasks) 
          (t/wait-any-results tasks timeout)
          (t/wait-any-results task timeout cancellation-token)
  
   timeout values can be any numeric value (cast to int, milliseconds, -1 = no limit) or a TimeSpan"
  ([tasks]
    (let [^Task/1 task-array (into-array Task tasks)
          idx (Task/WaitAny task-array)]
        (when-not (= idx -1)
            (result (nth tasks idx)))))
  ([tasks timeout]
    (let [^Task/1 task-array (into-array Task tasks)
          idx (Task/WaitAny task-array ^int (convert-timeout timeout))]
        (when-not (= idx -1)
            (result (nth tasks idx)))))
  ([tasks timeout cancellation-token]
    (let [^Task/1 task-array (into-array Task tasks)
          idx (Task/WaitAny task-array (convert-timeout timeout) cancellation-token)]
        (when-not (= idx -1)
            (result (nth tasks idx))))))


;; ── Creating standard tasks

(defn delay-task
  "Returns a Task that completes after the specified duration in milliseconds.
   For TimeSpan, use (Task/Delay timespan) directly.

   Usage:
     (t/await (t/delay-task 1000))"
  [milliseconds]
  (Task/Delay (int milliseconds)))

(defn ->task
  "Wraps a value in a completed Task<object>.

   Usage:
     (t/->task 42)  ;=> completed Task whose result is 42"
  [value]
  (Task/FromResult (type-args Object) value))

(defn completed-task
  "Returns a cached, already-completed void Task."
  []
  Task/CompletedTask)

(defn task?
  "Returns true if x is a Task."
  [x]
  (instance? Task x))

(defn run
  "Runs f (zero-arg fn) on the thread pool. Returns Task<object>.

   Usage:
     (t/await (t/run (fn [] (+ 1 2 3))))"
  [f]
  (let [func (gen-delegate |System.Func`1[System.Object]| [] (f))]
    (Task/Run func)))
