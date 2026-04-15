(ns clojure.clr.async.task
  "Task-based async/await interop for .NET 11+ runtime async.
   Provides idiomatic Clojure wrappers around System.Threading.Tasks.Task."
  (:refer-clojure :exclude [await])
  (:import [System.Threading.Tasks Task]))


;; ── Internal helpers ──────────────────────────────────────────────────

; This would get a reflection warning, so we don't turn on *warn-on-reflection* until after.
; We WANT a reflection warning -- we need the DLR callsite mechanism on the call to .GetAwaiter.
; If we tag the task parameter as ^Task, we always pick up Task.GetAwaiter(), even for generic Task<TResult> tasks.
; That just does not work.

(defn- task-result
  "Extracts the result from a completed Task<T> via its typed awaiter.
   Uses GetAwaiter().GetResult() which unwraps exceptions cleanly
   (no AggregateException wrapping unlike .Result).
   Returns nil for non-generic Task (void)."
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
     (require '[clojure.clr.async.task :as t])
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

;; ── Functions ─────────────────────────────────────────────────────────

(def ^:private task-object-type |System.Threading.Tasks.Task`1[System.Object]|)

(defn ^:async await-all
  "Awaits all tasks in parallel. Takes a collection of any Task types
   (Task<string>, Task<int>, Task<Object>, etc.).
   Returns an Object[] of results. Must be called in an async context.

   Usage:
     (let [results (t/await (t/await-all [task-a task-b task-c]))]
       (vec results))"
  [tasks]
  (let [^|System.Threading.Tasks.Task[]| task-array (into-array Task tasks)]
    (if (every? #(instance? task-object-type %) task-array)
      ;; Fast path: all Task<Object> — use generic WhenAll, no per-element reflection
      (let [^|System.Threading.Tasks.Task`1[System.Object][]| obj-array
            (into-array task-object-type tasks)]
        (await* (Task/WhenAll (type-args Object) obj-array)))
      ;; Slow path: mixed types — await non-generic, extract results via reflection
      (do
        (await* (Task/WhenAll task-array))
        (into-array Object (map task-result task-array))))))

(defn ^:async await-any
  "Awaits the first task to complete from a collection of any Task types.
   Returns the first completed Task (not its result).
   Must be called in an async context.

   Usage:
     (let [winner (t/await (t/await-any [fast-task slow-task]))]
       (t/result winner))"
  [tasks]
  (let [^|System.Threading.Tasks.Task[]| task-array (into-array Task tasks)]
    (await* (Task/WhenAny task-array))))

(defn delay-task
  "Returns a Task that completes after the specified duration in milliseconds.
   For TimeSpan, use (Task/Delay timespan) directly.

   Usage:
     (t/await (t/delay-task 1000))"
  [milliseconds]
  (Task/Delay (int milliseconds)))

(defn run
  "Runs f (zero-arg fn) on the thread pool. Returns Task<object>.

   Usage:
     (t/await (t/run (fn [] (+ 1 2 3))))"
  [f]
  (let [func (gen-delegate |System.Func`1[System.Object]| [] (f))]
    (Task/Run func)))

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

(defn result
  "Blocks the calling thread until the task completes and returns its result.
   For Task<T>, returns T. For void Task, returns nil.
   Unwraps AggregateException to throw the inner exception directly.

   Usage:
     (t/result (t/->task 42))       ;=> 42
     (t/result (t/completed-task))   ;=> nil
     (t/result (t/async (t/await (t/delay-task 100)) \"done\"))  ;=> \"done\""
  [task]
  (task-result task))
