(ns clojure.test-clojure.clr.async.task
  (:require [clojure.test :refer [deftest is testing]]
            [clojure.clr.async.task.alpha :as t])
  (:import [System.Threading.Tasks Task]))

(set! *warn-on-reflection* true)

(def ^:private async-supported?
  (some? (Type/GetType "System.Runtime.CompilerServices.AsyncHelpers, System.Runtime")))

;; ── Utility function tests (work on any .NET version) ─────────────────

(deftest to-task-wraps-value
  (testing "->task returns an already-completed Task<object>"
    (let [^Task task (t/->task 42)]
      (is (t/task? task))
      (is (.IsCompleted task))
      (is (= 42 (t/result task))))))

(deftest completed-task-is-completed
  (testing "completed-task returns a completed Task"
    (let [^Task task (t/completed-task)]
      (is (t/task? task))
      (is (.IsCompleted task)))))

(deftest task-predicate
  (testing "task? returns true for tasks, false otherwise"
    (is (t/task? (t/->task 1)))
    (is (t/task? (t/completed-task)))
    (is (not (t/task? 42)))
    (is (not (t/task? "hello")))))

(deftest result-on-completed-task
  (testing "result returns value from completed Task<object>"
    (is (= 42 (t/result (t/->task 42)))))
  (testing "result returns nil from void completed-task"
    (is (nil? (t/result (t/completed-task))))))

(deftest delay-task-returns-task
  (testing "delay-task returns a Task"
    (is (t/task? (t/delay-task 1)))))

(deftest run-returns-task
  (testing "run returns a Task with the fn result"
    (let [task (t/run (fn [] (+ 1 2 3)))]
      (is (t/task? task))
      (is (= 6 (t/result task))))))

;; ── wait-XXX tests  ─────────────────────

(deftest wait-all-results-returns-results
  (when async-supported?
    (testing "wait-all-results returns array of results from multiple tasks"
      (let [result (vec (t/wait-all-results [(t/->task "a")
                                             (t/->task "b")
                                             (t/->task "c")]))]
        (is (= ["a" "b" "c"] result))))))

(deftest wait-all-runs-all-tasks-to-completion
  (when async-supported?     
    (testing "wait-all runs all tasks to completion"
      (let [tasks [(t/delay-task 100)
                   (t/delay-task 200)
                   (t/delay-task 300)]]
         (t/wait-all tasks)
         (is (every? #(.IsCompleted ^Task %) tasks))))))


(deftest wait-any-returns-first-task
  (when async-supported?
    (testing "wait-any returns the first completed task"
      (let [fast (t/->task "fast")
            slow (t/delay-task 5000)
            ^Task winner (t/wait-any [fast slow])]
        (is (= fast winner))))))

(deftest delay-task-waits
  (when async-supported?
    (testing "delay-task creates a delay"
      (let [result (t/result
                     (t/async
                       (let [start (System.DateTime/UtcNow)]
                         (t/await (t/delay-task 100))
                         (let [elapsed (.TotalMilliseconds
                                         (.Subtract (System.DateTime/UtcNow) start))]
                           (>= elapsed 90)))))]
        (is (true? result))))))

(deftest run-offloads-to-threadpool
  (when async-supported?
    (testing "run executes fn on thread pool and returns result"
      (let [result (t/result
                     (t/async
                       (t/await (t/run (fn [] (+ 1 2 3))))))]
        (is (= 6 result))))))

(deftest await-all-mixed-task-types
  (when async-supported?
    (testing "await-all works with Task<string> and other Task<T> types"
      (let [result (t/wait-all-results
                        [(Task/FromResult (type-args String) "a")
                         (t/->task "b")
                         (Task/FromResult (type-args String) "c")])]
        (is (= ["a" "b" "c"] result))))))

(deftest result-blocks-until-complete
  (when async-supported?
    (testing "result blocks until async work completes"
      (is (= "done"
             (t/result
               (t/async
                 (t/await (t/delay-task 50))
                 "done")))))))