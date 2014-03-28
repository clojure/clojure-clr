;   Copyright (c) Rich Hickey. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.

; Author: Stephen C. Gilardi

;;
;;  Tests for the Clojure functions documented at the URL:
;;
;;    http://clojure.org/Reader
;;
;;  scgilardi (gmail)
;;  Created 22 October 2008

(ns clojure.test-clojure.reader
  (:use clojure.test)
  (:use [clojure.instant :only [read-instant-datetime                        ;;; read-instant-date
                                read-instant-datetimeoffset                  ;;; read-instant-calendar
                                ]])                                          ;;; read-instant-timestamp
  (:require clojure.walk
            [clojure.test.generative :refer (defspec)]
            [clojure.test-clojure.generators :as cgen])
  (:import [clojure.lang BigInt Ratio]
           System.IO.Path  System.IO.FileInfo
           ))                                                                ;;; java.util.TimeZone

;; Symbols

(deftest Symbols
  (is (= 'abc (symbol "abc")))
  (is (= '*+!-_? (symbol "*+!-_?")))
  (is (= 'abc:def:ghi (symbol "abc:def:ghi")))
  (is (= 'abc/def (symbol "abc" "def")))
  (is (= 'abc.def/ghi (symbol "abc.def" "ghi")))
  (is (= 'abc/def.ghi (symbol "abc" "def.ghi")))
  (is (= 'abc:def/ghi:jkl.mno (symbol "abc:def" "ghi:jkl.mno")))
  (is (instance? clojure.lang.Symbol 'alphabet))
  )

;; Literals

(deftest Literals
  ; 'nil 'false 'true are reserved by Clojure and are not symbols
  (is (= 'nil nil))
  (is (= 'false false))
  (is (= 'true true)) )

;; Strings

(defn temp-file
  [& ignore]                                          ;;; [prefix suffix]
  (FileInfo.                                          ;;;  (doto (File/createTempFile prefix suffix)
       (Path/GetTempFileName)))                       ;;;    (.deleteOnExit)))

(defn read-from
  [source file form]
  (if (= :string source)
    (read-string form)
    (do
      (spit file form :file-mode System.IO.FileMode/Truncate)
      (let [v (load-file (str file))] (.Delete file) v))))          ;;;  (load-file (str file)))))

(defn code-units
  [s]
  (and (instance? String s) (map int s)))

(deftest Strings
  (is (= "abcde" (str \a \b \c \d \e)))
  (is (= "abc
  def" (str \a \b \c \newline \space \space \d \e \f)))
  (let [f (temp-file "clojure.core-reader" "test")]
    (doseq [source [:string :file]]
      (testing (str "Valid string literals read from " (name source))
        (are [x form] (= x (code-units
                            (read-from source (temp-file) (str "\"" form "\""))))                ;;;  f => (temp-file)
             [] ""
             [34] "\\\""
             [10] "\\n"

             [0] "\\0"
             [0] "\\000"
             [3] "\\3"
             [3] "\\03"
             [3] "\\003"
             [0 51] "\\0003"
             [3 48] "\\0030"
             [0377] "\\377"
             [0 56] "\\0008"

             [0] "\\u0000"
             [0xd7ff] "\\ud7ff"
             [0xd800] "\\ud800"
             [0xdfff] "\\udfff"
             [0xe000] "\\ue000"
             [0xffff] "\\uffff"
             [4 49] "\\u00041"))
      (testing (str "Errors reading string literals from " (name source))
        (are [err msg form] (thrown-with-msg? err msg
                              (read-from source f (str "\"" form "\"")))
             Exception #"EOF while reading string" "\\"
             Exception #"Unsupported escape character: \\o" "\\o"

             Exception #"Octal escape sequence must be in range \[0, 377\]" "\\400"
             Exception #"Invalid digit: 8" "\\8"
             Exception #"Invalid digit: 8" "\\8000"
             Exception #"Invalid digit: 8" "\\0800"
             Exception #"Invalid digit: 8" "\\0080"
             Exception #"Invalid digit: a" "\\2and"

             Exception #"Invalid unicode escape: \\u" "\\u"
             Exception #"Invalid unicode escape: \\ug" "\\ug"
             Exception #"Invalid unicode escape: \\ug" "\\ug000"
             Exception #"Invalid character length: 1, should be: 4" "\\u0"
             Exception #"Invalid character length: 3, should be: 4" "\\u004"
             Exception #"Invalid digit: g" "\\u004g")))))

;; Numbers

(deftest Numbers

  ; Read Integer
  (is (instance? Int64 2147483647))            ;;; Long
  (is (instance? Int64 +1))
  (is (instance? Int64 1))
  (is (instance? Int64 +0))
  (is (instance? Int64 0))
  (is (instance? Int64 -0))
  (is (instance? Int64 -1))
  (is (instance? Int64 -2147483648))

  ; Read Long
  (is (instance? Int64 2147483648))              ;;; Long
  (is (instance? Int64 -2147483649))
  (is (instance? Int64 9223372036854775807))
  (is (instance? Int64 -9223372036854775808))

  ;; Numeric constants of different types don't wash out. Regression fixed in
  ;; r1157. Previously the compiler saw 0 and 0.0 as the same constant and
  ;; caused the sequence to be built of Doubles.
  (let [x 0.0]
    (let [sequence (loop [i 0 l '()]
                     (if (< i 5)
                       (recur (inc i) (conj l i))
                       l))]
      (is (= [4 3 2 1 0] sequence))
      (is (every? #(instance? Int64 %)              ;;; Long 
                  sequence))))

  ; Read BigInteger
  (is (instance? BigInt 9223372036854775808))
  (is (instance? BigInt -9223372036854775809))
  (is (instance? BigInt 10000000000000000000000000000000000000000000000000))
  (is (instance? BigInt -10000000000000000000000000000000000000000000000000))

  ; Read Double
  (is (instance? Double +1.0e+1))
  (is (instance? Double +1.e+1))
  (is (instance? Double +1e+1))

  (is (instance? Double +1.0e1))
  (is (instance? Double +1.e1))
  (is (instance? Double +1e1))

  (is (instance? Double +1.0e-1))
  (is (instance? Double +1.e-1))
  (is (instance? Double +1e-1))

  (is (instance? Double 1.0e+1))
  (is (instance? Double 1.e+1))
  (is (instance? Double 1e+1))

  (is (instance? Double 1.0e1))
  (is (instance? Double 1.e1))
  (is (instance? Double 1e1))

  (is (instance? Double 1.0e-1))
  (is (instance? Double 1.e-1))
  (is (instance? Double 1e-1))

  (is (instance? Double -1.0e+1))
  (is (instance? Double -1.e+1))
  (is (instance? Double -1e+1))

  (is (instance? Double -1.0e1))
  (is (instance? Double -1.e1))
  (is (instance? Double -1e1))

  (is (instance? Double -1.0e-1))
  (is (instance? Double -1.e-1))
  (is (instance? Double -1e-1))

  (is (instance? Double +1.0))
  (is (instance? Double +1.))

  (is (instance? Double 1.0))
  (is (instance? Double 1.))

  (is (instance? Double +0.0))
  (is (instance? Double +0.))

  (is (instance? Double 0.0))
  (is (instance? Double 0.))

  (is (instance? Double -0.0))
  (is (instance? Double -0.))

  (is (instance? Double -1.0))
  (is (instance? Double -1.))

  ; Read BigDecimal
  (is (instance? BigDecimal 9223372036854775808M))
  (is (instance? BigDecimal -9223372036854775809M))
  (is (instance? BigDecimal 2147483647M))
  (is (instance? BigDecimal +1M))
  (is (instance? BigDecimal 1M))
  (is (instance? BigDecimal +0M))
  (is (instance? BigDecimal 0M))
  (is (instance? BigDecimal -0M))
  (is (instance? BigDecimal -1M))
  (is (instance? BigDecimal -2147483648M))

  (is (instance? BigDecimal +1.0e+1M))  
  (is (instance? BigDecimal +1.e+1M))
  (is (instance? BigDecimal +1e+1M))

  (is (instance? BigDecimal +1.0e1M))
  (is (instance? BigDecimal +1.e1M))
  (is (instance? BigDecimal +1e1M))

  (is (instance? BigDecimal +1.0e-1M))
  (is (instance? BigDecimal +1.e-1M))
  (is (instance? BigDecimal +1e-1M))

  (is (instance? BigDecimal 1.0e+1M))
  (is (instance? BigDecimal 1.e+1M))
  (is (instance? BigDecimal 1e+1M))

  (is (instance? BigDecimal 1.0e1M))
  (is (instance? BigDecimal 1.e1M))
  (is (instance? BigDecimal 1e1M))

  (is (instance? BigDecimal 1.0e-1M))
  (is (instance? BigDecimal 1.e-1M))
  (is (instance? BigDecimal 1e-1M))

  (is (instance? BigDecimal -1.0e+1M))
  (is (instance? BigDecimal -1.e+1M))
  (is (instance? BigDecimal -1e+1M))

  (is (instance? BigDecimal -1.0e1M))
  (is (instance? BigDecimal -1.e1M))
  (is (instance? BigDecimal -1e1M))

  (is (instance? BigDecimal -1.0e-1M))
  (is (instance? BigDecimal -1.e-1M))
  (is (instance? BigDecimal -1e-1M))

  (is (instance? BigDecimal +1.0M))
  (is (instance? BigDecimal +1.M))

  (is (instance? BigDecimal 1.0M))
  (is (instance? BigDecimal 1.M))

  (is (instance? BigDecimal +0.0M))
  (is (instance? BigDecimal +0.M))

  (is (instance? BigDecimal 0.0M))
  (is (instance? BigDecimal 0.M))

  (is (instance? BigDecimal -0.0M))
  (is (instance? BigDecimal -0.M))

  (is (instance? BigDecimal -1.0M))
  (is (instance? BigDecimal -1.M))

  (is (instance? Ratio 1/2))
  (is (instance? Ratio -1/2))
  (is (instance? Ratio +1/2))
)

;; Characters

(deftest t-Characters
  (let [f (temp-file "clojure.core-reader" "test")]
    (doseq [source [:string :file]]
      (testing (str "Valid char literals read from " (name source))
        (are [x form] (= x (read-from source (temp-file) form))                       ;;; f -> (temp-file)
             (first "o") "\\o"
             (char 0) "\\o0"
             (char 0) "\\o000"
             (char 047) "\\o47"
             (char 0377) "\\o377"

             (first "u") "\\u"
             (first "A") "\\u0041"
             (char 0) "\\u0000"
             (char 0xd7ff) "\\ud7ff"
             (char 0xe000) "\\ue000"
             (char 0xffff) "\\uffff"))
      (testing (str "Errors reading char literals from " (name source))
        (are [err msg form] (thrown-with-msg? err msg (read-from source f form))
             Exception #"EOF while reading character" "\\"
             Exception #"Unsupported character: \\00" "\\00"
             Exception #"Unsupported character: \\0009" "\\0009"

             Exception #"Invalid digit: 8" "\\o378"
             Exception #"Octal escape sequence must be in range \[0, 377\]" "\\o400"
             Exception #"Invalid digit: 8" "\\o800"
             Exception #"Invalid digit: a" "\\oand"
             Exception #"Invalid octal escape sequence length: 4" "\\o0470"

             Exception #"Invalid unicode character: \\u0" "\\u0"
             Exception #"Invalid unicode character: \\ug" "\\ug"
             Exception #"Invalid unicode character: \\u000" "\\u000"
             Exception #"Invalid character constant: \\ud800" "\\ud800"
             Exception #"Invalid character constant: \\udfff" "\\udfff"
             Exception #"Invalid unicode character: \\u004" "\\u004"
             Exception #"Invalid unicode character: \\u00041" "\\u00041"
             Exception #"Invalid digit: g" "\\u004g")))))

;; nil

(deftest t-nil)

;; Booleans

(deftest t-Booleans)

;; Keywords

(deftest t-Keywords
  (is (= :abc (keyword "abc")))
  (is (= :abc (keyword 'abc)))
  (is (= :*+!-_? (keyword "*+!-_?")))
  (is (= :abc:def:ghi (keyword "abc:def:ghi")))
  (is (= :abc/def (keyword "abc" "def")))
  (is (= :abc/def (keyword 'abc/def)))
  (is (= :abc.def/ghi (keyword "abc.def" "ghi")))
  (is (= :abc/def.ghi (keyword "abc" "def.ghi")))
  (is (= :abc:def/ghi:jkl.mno (keyword "abc:def" "ghi:jkl.mno")))
  (is (instance? clojure.lang.Keyword :alphabet))
  )

(deftest reading-keywords
  (are [x y] (= x (binding [*ns* (the-ns 'user)] (read-string y)))
       :foo ":foo"
       :foo/bar ":foo/bar"
       :user/foo "::foo")
  (are [err msg form] (thrown-with-msg? err msg (read-string form))
       Exception #"Invalid token: foo:" "foo:"
       Exception #"Invalid token: :bar/" ":bar/"
       Exception #"Invalid token: ::does.not/exist" "::does.not/exist"
	   Exception #"Invalid token: :5" ":5"))

;; Lists

(deftest t-Lists)

;; Vectors

(deftest t-Vectors)

;; Maps

(deftest t-Maps)

;; Sets

(deftest t-Sets)

;; Macro characters

;; Quote (')

(deftest t-Quote)

;; Character (\)

(deftest t-Character)

;; Comment (;)

(deftest t-Comment)

;; Deref (@)

(deftest t-Deref)

;; Dispatch (#)

;; #{} - see Sets above

;; Regex patterns (#"pattern")

(deftest t-Regex)

;; Metadata (^ or #^ (deprecated))

(deftest t-line-column-numbers
  (let [code "(ns reader-metadata-test
  (:require [clojure.java.io
             :refer (resource reader)]))

(let [a 5]
  ^:added-metadata
  (defn add-5
    [x]
    (reduce + x (range a))))"
        stream (clojure.lang.LineNumberingTextReader.                                       ;;; clojure.lang.LineNumberingPushbackReader.
                 (System.IO.StringReader. code))                                             ;;; java.io.StringReader.
        top-levels (take-while identity (repeatedly #(read stream false nil)))
        expected-metadata '{ns {:line 1, :column 1}
                            :require {:line 2, :column 3}
                            resource {:line 3, :column 21}
                            let {:line 5, :column 1}
                            defn {:line 6, :column 3 :added-metadata true}
                            reduce {:line 9, :column 5}
                            range {:line 9, :column 17}}
        verified-forms (atom 0)]
    (doseq [form top-levels]
      (clojure.walk/postwalk
        #(when (list? %)
           (is (= (expected-metadata (first %))
                  (meta %)))
           (is (->> (meta %)
                 vals
                 (filter number?)
                 (every? (partial instance? Int32))))                                      ;;; Integer
           (swap! verified-forms inc))
        form))
    ;; sanity check against e.g. reading returning ()
    (is (= (count expected-metadata) @verified-forms))))

(deftest t-Metadata
  (is (= (meta '^:static ^:awesome ^{:static false :bar :baz} sym) {:awesome true, :bar :baz, :static true})))

;; Var-quote (#')

(deftest t-Var-quote)

;; Anonymous function literal (#())

(deftest t-Anonymouns-function-literal)

;; Syntax-quote (`, note, the "backquote" character), Unquote (~) and
;; Unquote-splicing (~@)

(deftest t-Syntax-quote
  (are [x y] (= x y)
      `() ()    ; was NPE before SVN r1337
  ))

;; (read)
;; (read stream)
;; (read stream eof-is-error)
;; (read stream eof-is-error eof-value)
;; (read stream eof-is-error eof-value is-recursive)

(deftest t-read)

(deftest division
  (is (= clojure.core// /))
  (binding [*ns* *ns*]
    (eval '(do (ns foo
                 (:require [clojure.core :as bar])
                 (:use [clojure.test]))
               (is (= clojure.core// bar//))))))

(deftest Instants
  (testing "Instants are read as System.DateTime by default"                       ;;; java.util.Date
    (is (= System.DateTime (class #inst "2010-11-12T13:14:15.666"))))              ;;; java.util.Date
  (let [s "#inst \"2010-11-12T13:14:15.666-06:00\""]
    (binding [*data-readers* {'inst read-instant-datetime}]                        ;;; read-instant-date
      (testing "read-instant-datetime produces System.DateTime"                    ;;; "read-instant-date produces java.util.Date"
        (is (= System.DateTime (class (read-string s)))))                          ;;; java.util.Date
      (testing "System.DateTime instants round-trips"                              ;;; java.util.Date
        (is (= (-> s read-string)
               (-> s read-string pr-str read-string))))
      (testing "java.util.Date instants round-trip throughout the year"
        (doseq [month (range 1 13) day (range 1 29) hour (range 1 23)]
          (let [s (format "#inst \"2010-%02d-%02dT%02d:14:15.666-06:00\"" month day hour)]
            (is (= (-> s read-string)
                   (-> s read-string pr-str read-string))))))
      ;;;(testing "java.util.Date handling DST in time zones"                     ;;; not sure how to do this
      ;;;  (let [dtz (TimeZone/getDefault)]
      ;;;    (try
      ;;;      ;; A timezone with DST in effect during 2010-11-12
      ;;;      (TimeZone/setDefault (TimeZone/getTimeZone "Australia/Sydney"))
      ;;;      (is (= (-> s read-string)
      ;;;             (-> s read-string pr-str read-string)))
      ;;;      (finally (TimeZone/setDefault dtz)))))
      (testing "java.util.Date should always print in UTC"
        (let [d (read-string s)
              pstr (print-str d)
              len (.Length pstr)]                                                  ;;;.length
          (is (= (subs pstr (- len 7)) "-00:00\"")))))
    (binding [*data-readers* {'inst read-instant-datetimeoffset}]                  ;;; read-instant-calendar
      (testing "read-instant-calendar produces System.DateTimeOffset"              ;;; java.util.Calendar
        (is (instance? System.DateTimeOffset (read-string s))))                    ;;; java.util.Calendar
      (testing "System.DateTimeOffset round-trips"                                 ;;; java.util.Calendar
        (is (= (-> s read-string)
               (-> s read-string pr-str read-string))))
      (testing "System.DateTimeOffset remembers timezone in literal"               ;;; java.util.Calendar
        (is (= "#inst \"2010-11-12T13:14:15.666-06:00\""
               (-> s read-string pr-str)))
        (is (= (-> s read-string)
               (-> s read-string pr-str read-string))))
      (testing "System.DateTimeOffset preserves milliseconds"                       ;;; java.util.Calendar
        (is (= 666 (-> s read-string
                       (.Millisecond)))))))                                         ;;; (.get java.util.Calendar/MILLISECOND)))))))
  ;;;(let [s "#inst \"2010-11-12T13:14:15.123456789\""
  ;;;      s2 "#inst \"2010-11-12T13:14:15.123\""
  ;;;      s3 "#inst \"2010-11-12T13:14:15.123456789123\""]
  ;;;  (binding [*data-readers* {'inst read-instant-timestamp}]
  ;;;    (testing "read-instant-timestamp produces java.sql.Timestamp"
  ;;;      (is (= java.sql.Timestamp (class (read-string s)))))
  ;;;    (testing "java.sql.Timestamp preserves nanoseconds"
  ;;;      (is (= 123456789 (-> s read-string .getNanos)))
  ;;;      (is (= 123456789 (-> s read-string pr-str read-string .getNanos)))
  ;;;      ;; truncate at nanos for s3
  ;;;      (is (= 123456789 (-> s3 read-string pr-str read-string .getNanos))))
  ;;;    (testing "java.sql.Timestamp should compare nanos"
  ;;;      (is (= (read-string s) (read-string s3)))
  ;;;      (is (not= (read-string s) (read-string s2)))))
  ;;;  (binding [*data-readers* {'inst read-instant-date}]
  ;;;    (testing "read-instant-date should truncate at milliseconds"
  ;;;      (is (= (read-string s) (read-string s2)) (read-string s3)))))
  ;;;(let [s "#inst \"2010-11-12T03:14:15.123+05:00\""
  ;;;      s2 "#inst \"2010-11-11T22:14:15.123Z\""]
  ;;;  (binding [*data-readers* {'inst read-instant-date}]
  ;;;    (testing "read-instant-date should convert to UTC"
  ;;;      (is (= (read-string s) (read-string s2)))))
  ;;;  (binding [*data-readers* {'inst read-instant-timestamp}]
  ;;;    (testing "read-instant-timestamp should convert to UTC"
  ;;;      (is (= (read-string s) (read-string s2)))))
  ;;;  (binding [*data-readers* {'inst read-instant-calendar}]
  ;;;    (testing "read-instant-calendar should preserve timezone"
  ;;;      (is (not= (read-string s) (read-string s2)))))))
  )
  ;; UUID Literals
;; #uuid "550e8400-e29b-41d4-a716-446655440000"

(deftest UUID
  (is (= System.Guid (class #uuid "550e8400-e29b-41d4-a716-446655440000")))             ;;; java.util.UUID
  (is (.Equals #uuid "550e8400-e29b-41d4-a716-446655440000"                             ;;; .equals
               #uuid "550e8400-e29b-41d4-a716-446655440000"))
  #_(is (not (identical? #uuid "550e8400-e29b-41d4-a716-446655440000"                   ;;; this test doesn't work for us because System.GUid is a value type and value types are treated as values by idnentical?
                       #uuid "550e8400-e29b-41d4-a716-446655440000")))
  #_(is (= 4 (.version #uuid "550e8400-e29b-41d4-a716-446655440000")))                  ;;; No .version in CL
  (is (= (print-str #uuid "550e8400-e29b-41d4-a716-446655440000")
         "#uuid \"550e8400-e29b-41d4-a716-446655440000\"")))

(deftest unknown-tag
  (let [my-unknown (fn [tag val] {:unknown-tag tag :value val})
        throw-on-unknown (fn [tag val] (throw (Exception. (str "No data reader function for tag " tag))))         ;;; RuntimeException
        my-uuid (partial my-unknown 'uuid)
        u "#uuid \"550e8400-e29b-41d4-a716-446655440000\""
        s "#never.heard.of/some-tag [1 2]" ]
    (binding [*data-readers* {'uuid my-uuid}
              *default-data-reader-fn* my-unknown]
      (testing "Unknown tag"
        (is (= (read-string s)
               {:unknown-tag 'never.heard.of/some-tag
                :value [1 2]})))
      (testing "Override uuid tag"
        (is (= (read-string u)
               {:unknown-tag 'uuid
                :value "550e8400-e29b-41d4-a716-446655440000"}))))

    (binding [*default-data-reader-fn* throw-on-unknown]
      (testing "Unknown tag with custom throw-on-unknown"
        (are [err msg form] (thrown-with-msg? err msg (read-string form))
             Exception #"No data reader function for tag foo" "#foo [1 2]"
             Exception #"No data reader function for tag bar/foo" "#bar/foo [1 2]"
             Exception #"No data reader function for tag bar.baz/foo" "#bar.baz/foo [1 2]")))

    (testing "Unknown tag out-of-the-box behavior (like Clojure 1.4)"
      (are [err msg form] (thrown-with-msg? err msg (read-string form))
           Exception #"No reader function for tag foo" "#foo [1 2]"
           Exception #"No reader function for tag bar/foo" "#bar/foo [1 2]"
           Exception #"No reader function for tag bar.baz/foo" "#bar.baz/foo [1 2]"))))


(defn roundtrip
  "Print an object and read it back. Returns rather than throws
   any exceptions."
  [o]
  (binding [*print-length* nil
            *print-dup* nil
            *print-level* nil]
    (try
     (-> o pr-str read-string)
     (catch Exception t t))))                                    ;;; Throwable

(defn roundtrip-dup
  "Print an object with print-dup and read it back.
   Returns rather than throws any exceptions."
  [o]
  (binding [*print-length* nil
            *print-dup* true
            *print-level* nil]
    (try
     (-> o pr-str read-string)
     (catch Exception t t))))                                    ;;; Throwable

(defspec types-that-should-roundtrip
  roundtrip
  [^{:tag cgen/ednable} o]
  (when-not (= o %)
    (throw (ex-info "Value cannot roundtrip, see ex-data" {:printed o :read %}))))

(defspec types-that-need-dup-to-roundtrip
  roundtrip-dup
  [^{:tag cgen/dup-readable} o]
  (when-not (= o %)
    (throw (ex-info "Value cannot roundtrip, see ex-data" {:printed o :read %}))))