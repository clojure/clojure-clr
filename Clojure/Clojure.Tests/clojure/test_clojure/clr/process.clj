;   Copyright (c) Rich Hickey. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.

(ns clojure.test-clojure.clr.process
  (:require
    [clojure.test :refer :all]
    [clojure.clr.process :as p]                                                                             ;;; clojure.java.process
    [clojure.string :as str]))

(deftest test-stderr-redirect
  ;; capture to stdout and return string
  (is (not (str/blank? (p/exec {:redirect-out true} "bash" "-c" "ls"))))                                    ;;; added options map

  ;; print to stderr, capture nil
  (is (str/blank? (p/exec {:redirect-out true} "bash" "-c" "ls >&2")))

  ;; redirect, then capture to string
  #_(is (not (str/blank? (p/exec {:err :stdout} "bash" "-c" "ls >&2")))))                                   ;;; as far as I know, there is no way to redirect StandardError to StandardOutput directly.

(deftest test-process-deref
  (is (zero? @(p/exit-ref (p/start "powershell.exe" "Start-Sleep -Seconds 1"))))                            ;;; (p/start "sleep" "1")
  (is (zero? (deref (p/exit-ref (p/start "powershell.exe" "Start-Sleep -Seconds 1")) 2500 :timeout)))       ;;; (p/start "sleep" "1")
  (is (= :timeout (deref (p/exit-ref (p/start "powershell.exe" "Start-Sleep -Seconds 1")) 1 :timeout))))    ;;; (p/start "sleep" "1")