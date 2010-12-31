(ns clojure.samples.counter)

(def s (apply str (repeat 20 "This is a really long string"))) 

(defn count-num-chars [^String s] 
  (let [len (.Length s) 
        space (int 32)] 
    (loop [i (int 0), c (int 0)] 
      (if (< i len) 
        (recur 
         (unchecked-inc i) 
         (if (== (int (.get_Chars s i)) space) 
           c 
           (unchecked-inc c))) 
        c)))) 

(defn cnc [n]
  (dotimes [_ n] (count-num-chars s)))

(defn f []
  (let [sw (System.Diagnostics.Stopwatch.)
        nanosec-per-tick (/ 1000000000 System.Diagnostics.Stopwatch/Frequency)]
	(.Start sw)
	(dotimes [_ 1000]
	   (count-num-chars s))
    (.Stop sw)
	(println "Time (nsec): " (* (.ElapsedTicks sw) nanosec-per-tick))))

(defn g [n]
  (time (cnc n)))

