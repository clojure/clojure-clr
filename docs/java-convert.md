# Moving from ClojureJVM to ClojureCLR

ClojureCLR attempts to stick as closely to the implementation of ClojureJVM as possible, but there are some differences in the underlying platforms that require some adjustments to your code. This page details some of the differences.

##  Getting started

Clojure
Clojure.Main
Clojure.Cljr

## CLI & deps.edn


## JVM vs CLR: packaging and classpath

java environment:  .class files discoverable on the classpath, and the java command to run them.  

CLR environment:  compiled code is packaged as assemblies, which are specialized (managed) DLL or EXE files.
CLOJURE_LOAD_PATH

AOT-compilation


## JVM vs CLR -- interop



## 

