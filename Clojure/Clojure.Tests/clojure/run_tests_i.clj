(assembly-load-from "clojure.tools.namespace.dll")
(assembly-load-from "clojure.data.generators.dll")
(assembly-load-from "clojure.test.generative.dll")
(require '[clojure.test.generative.runner :as runner])
(runner/-main-no-exit "clojure/test_clojure")


;;; clojure.test-clojure.reflect   -- TODO: need to rewrite reflect tests
