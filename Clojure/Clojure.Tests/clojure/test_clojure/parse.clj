(ns clojure.test-clojure.parse
  (:require
    [clojure.test :refer :all]
    [clojure.test.check :as chk]
    [clojure.test.check.generators :as gen]
    [clojure.test.check.properties :as prop])
  (:import
    [System Guid]))                                    ;;; [java.util UUID]

(deftest test-parse-long
  (are [s expected]
    (= expected (parse-long s))
    "100" 100
    "+100" 100
    "0" 0
    "+0" 0
    "-0" 0
    "-42" -42
    "9223372036854775807" Int64/MaxValue               ;;; Long/MAX_VALUE
    "+9223372036854775807" Int64/MaxValue              ;;; Long/MAX_VALUE
    "-9223372036854775808" Int64/MinValue              ;;; Long/MIN_VALUE
    "077" 77) ;; leading 0s are ignored! (not octal)

  (are [s] ;; do not parse
    (nil? (parse-long s))
    "0.3" ;; no float
    "9223372036854775808" ;; past max long
    "-9223372036854775809" ;; past min long
    "0xA0" ;; no hex
    "2r010")) ;; no radix support

;; generative test - gen long -> str -> parse, compare
(deftest test-gen-parse-long
  (let [res (chk/quick-check
              100000
              (prop/for-all* [gen/large-integer]
                #(= % (-> % str parse-long))))]
    (if (:result res)
      (is true) ;; pass
      (is (:result res) (pr-str res)))))
(compile-when (contains? #{:dotnet :core} dotnet-platform)   ;;; 4.6.1 is variant on a number of these
(deftest test-parse-double
  (are [s expected]
    (= expected (parse-double s))
    "1.234" 1.234
    "+1.234" 1.234
    "-1.234" -1.234
    "+0" +0.0
    "-0.0" -0.0
    "0.0" 0.0
    "5" 5.0
    ;;;"Infinity" Double/PositiveInfinity                                         ;;; POSITIVE_INFINITY  -- this is culture dependent
    ;;;"-Infinity" Double/NegativeInfinity                                        ;;; NEGATIVE_INFINITY  -- this is culture dependent
    "1.7976931348623157E308" Double/MaxValue                                      ;;; MAX_VALUE
    "4.9E-324" Double/Epsilon                                                     ;;; MIN_VALUE
    "1.7976931348623157E309" Double/PositiveInfinity  ;; past max double          ;;;/POSITIVE_INFINITY
    "2.5e-324" Double/Epsilon  ;; past min double, above half minimum             ;;; MIN_VALUE
    "2.4e-324" 0.0)  ;; below minimum double
  (is (Double/IsNaN (parse-double "NaN")))                                        ;;; isNaN
  (are [s] ;; nil on invalid string
    (nil? (parse-double s))
    "double" ;; invalid string
    "1.7976931348623157G309")) ;; invalid, but similar to valid
) ;; compile-when
;; generative test - gen double -> str -> parse, compare
(deftest test-gen-parse-double
  (let [res (chk/quick-check
              100000
              (prop/for-all* [gen/double]
                #(let [parsed (-> % str parse-double)]
                   (if (Double/isNaN %)
                     (Double/isNaN parsed)
                     (= % parsed)))))]
    (if (:result res)
      (is true) ;; pass
      (is (:result res) (pr-str res)))))

(deftest test-parse-uuid
  (is (parse-uuid (.ToString (Guid/NewGuid))))                      ;;; .toString UUID/randomUUID
  (is (nil? (parse-uuid "BOGUS"))) ;; nil on invalid uuid string
  (are [s] ;; throw on invalid type (not string)
    (try (parse-uuid s) (is false) (catch Exception _ (is true)))   ;;; Throwable
    123
    ))                 ;;;  nil   -- when passed to Guid/Parse, this will throw a an exception that parse-uuid actually handles -- different exception standard from Java.

(deftest test-parse-boolean
  (is (identical? true (parse-boolean "true")))
  (is (identical? false (parse-boolean "false")))

  (are [s] ;; nil on invalid string
    (nil? (parse-boolean s))
    "abc"
    "TRUE"
    "FALSE"
    " true ")

  (are [s] ;; throw on invalid type (not string)
    (try (parse-boolean s) (is false) (catch Exception _ (is true)))   ;;; Throwable
    nil
    false
    true
    100))