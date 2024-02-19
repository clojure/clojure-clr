;   Copyright (c) Rich Hickey. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.

(ns #?(:clj clojure.java.basis.impl :cljr clojure.clr.basis.impl)
  (:require
    [clojure.edn :as edn]
    #?( :clj [clojure.java.io :as jio] :cljr [clojure.clr.io :as cio] ))
  (:import
    #?(:clj [java.io PushbackReader] :cljr [clojure.lang PushbackTextReader] )))

(set! *warn-on-reflection* true)

(defn- read-edn
  "Coerce f to a reader via clojure.java.io/reader and read one edn value.
  The reader should contain a single value. Empty input returns nil.
  The reader will be read to EOF and closed."
  [f]
  (let [reader (#?(:clj jio/reader :cljr cio/text-reader) f)
        EOF (Object.)]
    (with-open [rdr (#?(:clj PushbackReader. :cljr PushbackTextReader.) reader)]
      (let [val (edn/read {:default tagged-literal :eof EOF} rdr)]
        (if (identical? EOF val)
          nil
          (if (not (identical? EOF (edn/read {:eof EOF} rdr)))
            (throw (ex-info "Invalid file, expected edn to contain a single value." {}))
            val))))))

(defn- read-basis
  "Read basis edn from basis file or throw"
  [basis-file]
  (when-let [f (#?(:clj jio/file :cljr cio/file-info) basis-file)]
    (when (#?(:clj .exists :cljr .Exists) f)
      (read-edn f))))

;; delay construction until needed, access via initial-basis
(def init-basis
  (delay (read-basis ( #?(:clj System/getProperty :cljr Environment/GetEnvironmentVariable) "clojure.basis"))))

;; delay construction until needed, access via current-basis
(def the-basis
  (delay (atom @init-basis)))

(defn update-basis!
  "Update the runtime basis by applying f with args"
  [f & args]
  (apply swap! @the-basis f args))