(defn f1 [l n] (if (> (count l) n) nil (recur (cons 'a l) n)))
(defn len [x]
  (. x Length))
(defn len2 [#^String x]
  (. x Length))

(defn test1 [] (time (f1 nil 100000)))
(defn test2 [] (time (reduce + (map len (replicate 100000 "asdf")))))
(defn test3 [] (time (reduce + (map len2 (replicate 100000 "asdf")))))
