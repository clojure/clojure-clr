;   Copyright (c) Rich Hickey. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.

; Author: Frantisek Sodomka, Robert Lachlan

(ns clojure.test-clojure.multimethods
  (:use clojure.test))

; http://clojure.org/multimethods

; defmulti
; defmethod
; remove-method
; prefer-method
; methods
; prefers


;hierarchies for tests below, generated and literal
(def h1 (reduce #(apply derive (cons %1 %2)) (make-hierarchy)
                     [[:p1 :a1] [:p1 :a2] [:p2 :a2] [:c :p2] [:c :p1]]))
(def h2 (reduce #(apply derive (cons %1 %2)) (make-hierarchy)
                     [[:p1 :a1] [:p1 :a2] [:p2 :a2] [:c :p2]]))
(def h3 (reduce #(apply derive (cons %1 %2)) (make-hierarchy)
                     [[:p1 :a1] [:p2 :a2] [:c :p2] [:c :p1]]))
(def h4 {:parents {:x8 #{:x6 :x7}, :x7 #{:x5}, :x6 #{:x5}, :x5 #{:x4},
		   :x4 #{:x3 :x2}, :x3 #{:x1}, :x2 #{:x1}},
	 :ancestors {:x8 #{:x4 :x5 :x6 :x7 :x3 :x2 :x1},
		     :x7 #{:x4 :x5 :x3 :x2 :x1}, :x6 #{:x4 :x5 :x3 :x2 :x1},
		     :x5 #{:x4 :x3 :x2 :x1}, :x4 #{:x3 :x2 :x1}, :x3 #{:x1},
		     :x2 #{:x1}},
	 :descendants {:x7 #{:x8}, :x6 #{:x8}, :x5 #{:x8 :x6 :x7},
		       :x4 #{:x8 :x5 :x6 :x7}, :x3 #{:x8 :x4 :x5 :x6 :x7},
		       :x2 #{:x8 :x4 :x5 :x6 :x7},
		       :x1 #{:x8 :x4 :x5 :x6 :x7 :x3 :x2}}})
(def h5 {:parents {:x2 #{:x1}, :x3 #{:x1}, :x4 #{:x3 :x2}, :x6 #{:x5},
		   :x7 #{:x5}, :x8 #{:x6 :x7}},
	 :ancestors {:x2 #{:x1}, :x3 #{:x1}, :x4 #{:x3 :x2 :x1}, :x6 #{:x5},
		     :x7 #{:x5}, :x8 #{:x5 :x6 :x7}},
	 :descendants {:x1 #{:x4 :x3 :x2}, :x2 #{:x4}, :x3 #{:x4},
		       :x5 #{:x8 :x6 :x7}, :x7 #{:x8}, :x6 #{:x8}}})
(def h6 {:parents {:a #{:b}}, :ancestors {:a #{:b}}, :descendants {:b #{:a}}})
(def h7 {:parents {System.Collections.IDictionary #{::maps}},                                       ;;; java.util.Map
	 :ancestors {System.Collections.IDictionary #{::maps}},                                         ;;; java.util.Map
	 :descendants {::maps #{System.Collections.IDictionary}}})                                      ;;; java.util.Map


; derive, [underive]
(deftest derive-test
  (is (= (derive h5 :x5 :x4) h4))
  (is (= (derive (make-hierarchy) :a :b) h6))
  (is (= (derive (make-hierarchy) System.Collections.IDictionary ::maps) h7)))                      ;;; java.util.Map



(deftest underive-test
  (is (= (underive (make-hierarchy) :x :y) (make-hierarchy)))
  (is (= (underive (derive (make-hierarchy) ::a ::b) ::a ::b)
	 (make-hierarchy)))
  (is (= (underive h1 :c :p1) h2))
  (is (= (underive h1 :p1 :a2) h3))
  (is (= (underive h4 :x5 :x4) h5))
  (is (= (underive h5 :x5 :x4) h5))
  (is (= (underive h4 :x8 :x1) h4))
  (is (= (underive h4 :x9 :x4) h4))
  (is (= (underive h4 :x5 :x10) h4))
  (is (= (underive h7 System.Collections.IDictionary ::maps) (make-hierarchy)))                     ;;; java.util.Map
  (is (= (underive h7 System.Collections.Hashtable ::maps) h7)))                              ;;; java.util.HashMap



; isa?
(deftest isa-test
  (is (isa? h4 :x5 :x4))
  (is (not (isa? h5 :x5 :x4)))
  (is (isa? h4 :x8 :x1))
  (is (not (isa? h5 :x8 :x1)))
  (is (isa? System.Collections.Hashtable System.Collections.IDictionary))                                      ;;; java.util.HashMap java.util.Map
  (is (isa? h7 System.Collections.IDictionary ::maps))
  (is (not (isa? (make-hierarchy) System.Collections.IDictionary ::a))))                            ;;; java.util.Map



; parents, ancestors, descendants
(deftest family-relation
  (is (= (parents h4 :x1) nil))
  (is (= (parents h4 :x4) #{:x2 :x3}))
  (is (= (ancestors h5 :x1) nil))
  (is (= (ancestors h4 :x4) #{:x1 :x2 :x3}))
  (is (= (descendants h4 :y) nil))
  (is (= (descendants h5 :x5) #{:x6 :x7 :x8})))

; some simple global hierarchy tests

(derive ::y1 ::y2)
(derive ::y3 ::y4)

(deftest global-isa1
  (derive ::y4 ::y1)
  (is (isa? ::y1 ::y2))
  (isa? ::y3 ::y2))            ;;;(not (isa? ::y3 ::y2)))  TODO: The original code has not, which is clearly wrong.
  

(derive System.Collections.Hashtable ::y4)                                         ;;; java.util.HashMap 
;;;the following test seems to have a timing dependency because of the global underive.  Adding an extra derive
(deftest global-isa2  (derive ::y4 ::y1)
  (is (isa? ::y3 ::y2))
  (is (isa? System.Collections.Hashtable ::y2)))                                   ;;; java.util.HashMap 


(deftest global-underive
  (derive ::y4 ::y1)
  (underive ::y4 ::y1)
  (is (not (isa? ::y3 ::y1)))
  (is (not (isa? System.Collections.Hashtable ::y2))))                              ;;; java.util.HashMap 


; make-hierarchy
(deftest make-hierarchy-test
  (is (= {:parents {} :descendants {} :ancestors {}} (make-hierarchy))))
