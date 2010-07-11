;   Copyright (c) Chris Houser, Dec 2008. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
;   which can be found in the file CPL.TXT at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.

; Utilities meant to be used interactively at the REPL

(ns
  #^{:author "Chris Houser, Christophe Grand, Stephen Gilardi, Michel Salim, Christophe Grande"
     :doc "Utilities meant to be used interactively at the REPL"}
  clojure.repl
  )   ;;;(:import (java.io LineNumberReader InputStreamReader PushbackReader)
      ;;;         (clojure.lang RT Reflector)))

;; ----------------------------------------------------------------------
;; Examine Clojure functions (Vars, really)

(defn source-fn
  "Returns a string of the source code for the given symbol, if it can
find it. This requires that the symbol resolve to a Var defined in
a namespace for which the .clj is in the classpath. Returns nil if
it can't find the source. For most REPL usage, 'source' is more
convenient.

Example: (source-fn 'filter)"
  [x]
  (when-let [v (resolve x)]
    (when-let [filepath (:file (meta v))]
      (when-let [ ^System.IO.FileInfo info (clojure.lang.RT/FindFile filepath) ]    ;;; [strm (.getResourceAsStream (RT/baseLoader) filepath)]
        (with-open [ ^System.IO.TextReader rdr (.OpenText info)]                    ;;; [rdr (LineNumberReader. (InputStreamReader. strm))]
          (dotimes [_ (dec (:line (meta v)))] (.ReadLine rdr))                      ;;; .readLine
          (let [text (StringBuilder.)
                pbr (proxy [clojure.lang.PushbackTextReader] [rdr]                  ;;; [PushbackReader] [rdr]
                      (Read [] (let [i (proxy-super Read)]                          ;;; read read
                                 (.Append text (char i))                            ;;; .append
                                 i)))]
            (read (clojure.lang.PushbackTextReader. pbr))                           ;;; (read (PushbackReader. pbr))
            (str text)))))))

(defmacro source
  "Prints the source code for the given symbol, if it can find it.
  This requires that the symbol resolve to a Var defined in a
  namespace for which the .clj is in the classpath.

  Example: (source filter)"
  [n]
  `(println (or (source-fn '~n) (str "Source not found"))))

(defn apropos
  "Given a regular expression or stringable thing, return a seq of
all definitions in all currently-loaded namespaces that match the
str-or-pattern."
  [str-or-pattern]
  (let [matches? (if (instance? System.Text.RegularExpressions.Regex str-or-pattern)    ;;; java.util.regex.Pattern
                   #(re-find str-or-pattern (str %))
                   #(.Contains (str %) (str str-or-pattern)))]                          ;;; .contains
    (mapcat (fn [ns]
              (filter matches? (keys (ns-publics ns))))
            (all-ns))))

(defn dir-fn
  "Returns a sorted seq of symbols naming public vars in
  a namespace"
  [ns]
  (sort (map first (ns-publics (the-ns ns)))))

(defmacro dir
  "Prints a sorted directory of public vars in a namespace"
  [nsname]
  `(doseq [v# (dir-fn '~nsname)]
     (println v#)))
