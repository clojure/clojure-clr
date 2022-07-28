# Notes on loading/compiling

## The interface in clojure.core

We start in `core.clj` and then trace our way into the underlying code (C#).

There is a quite long and elaborate set of functions in `core.clj` that relate to loading in its various guises. 
That code currently starts at line 5862, and probably won't be far from there.  Look for

```
;;;;;;;;;;; require/use/load, contributed by Stephen C. Gilardi ;;;;;;;;;;;;;;;;;;
```

The two functions to start looking at are `compile` and `load`, at the bottom of this section of code.
The `load` function is actually used by functions preceding it in the file -- there is a `forward` declaration up at the beginning.

```
(defn compile
  "Compiles the namespace named by the symbol lib into a set of
  classfiles. The source for the lib must be in a proper
  classpath-relative directory. The output files will go into the
  directory specified by *compile-path*, and that directory too must
  be in the classpath."
  {:added "1.0"}
  [lib]
  (binding [*compile-files* true]
    (load-one lib true true))
  lib)
```

`load-one` essentially calls `load` and then does some checks and some bookkeeping.  
So, essentially, `compile` just calls `load` with the `*compile-files*` flag set to `true`.

Note in the comment the reference to `*compile-path*` and the suspicious statements 
"source for the lib must be in a proper classpath-relative directory" and "that directory too must be on the classpath". 
We'll need appropriate translation for the .Net world.

Note also that the argument to `compile` is a symbol, as opposed to the string used in `load`.  
`load-one` does that translation: the symbol `a-b.c.d` would convert to `"/a_b/c/d"`.

Now on to `load`.

```
(defn load
  "Loads Clojure code from resources in classpath. A path is interpreted as
  classpath-relative if it begins with a slash or relative to the root
  directory for the current namespace otherwise."
  {:redef true
   :added "1.0"}
  [& paths]
  (doseq [^String path paths]
    (let [^String path (if (.StartsWith path "/")
                          path
                         (str (root-directory (ns-name *ns*)) \/ path))]
      (when *loading-verbosely*
        (printf "(clojure.core/load \"%s\")\n" path)
        (flush))
      (check-cyclic-dependency path)
      (when-not (= path (first *pending-paths*))`
        (binding [*pending-paths* (conj *pending-paths* path)]
          (clojure.lang.RT/load (.Substring path 1)))))))
```

Basically some name hacking and some bookkeeping, then a call to `clojure.lang.RT.load`.  (Note that `load` take a seq of strings, doing the load for each one.)
We see here the distinction made between the input argument (a string) being "classpath-relative if it begins with a slash or relative to the root
directory for the current namespace otherwise."  What does that mean?

If a supplied path looks like `"/a_b/c/d"`, i.e., starting with a slash, it will be used as is.  
If there is no initial slash, say `"a_b/c/d"`, then the root of the current namespace is prepended. 
If the current namespace is `my-precious.sss`, you will end up with the path `"/my_precious/sss/a_b/c/d"`.

(Make sure you've already converted your hyphens to underscores before you get here. The conversion will be done on the namespace name for you, but not on the path you supplied.)

The binding of `*pending-path*` here is to record where we are in case we have load calls during this load -- we want to avoid trying to load recursively.

## In the C# code: what we look for

So we now transition to the C# code of `clojure.lang.RT.load`.  Note that is called without the leading slash.

This stuff is so arcane, I can barely understand it myself.  Needs to be cleaned up, which is to say, rethought completely, when I do the rewrite.

Given a string representing a path as shown above, say `"a_b/c/d"`  (remember no leading slash on the input here), 
what are we looking for and where are looking for it?

We are looking for either source files or assemblies, with preference given to whichever is newer.
The source files will be named one of

1. `a_b.c.d.clj`
2. `a_b.c.d.cljc`

The assemblies will be named one of

3. `a_b.c.d.clj.dll`
4. `a_b.c.d.cljc.dll`

>>> Convention #1:  When a .clj(c) source file is compiled, the compiled assembly with the same name.

We check  for all four of these.  

>>> Convention #2: It is assumed that there will not be both a `.clj` and `.cljc` version of either kind.

If a DLL exists and either the source file does not exist or the DLL is newer, then we call `Compiler.LoadAssembly` on it.  (See below.)
Else if a source file exists, we either call `RT.Compile` or `RT.LoadScript` on it, depending on the the value of `*compile-files*`.  (Also see below.)

If neither is found, we are not done.  There are two more possibilities.

5.  There might be a type called `__INIT__a_b$c$d` located in some loaded assembly. 
(Yes, we look at all loaded assemblies.  I eventually would like to make this look at a more restricted set.)
If it exists, we call the `Initialize` static method on that type.

>>> Convention #3: The work of initializing a compiled Clojure assembly is done by calling the `Initialize` method of type in the assembly named
as `__INIT__<name>`, where the `<name>` comes from the name of the source file/assembly with periods replaced by dollar signs.

6. We look for an embedded resource of the appropriate name in all loaded assemblies.  (as in (5), I'd eventually like to restrict which assemblies we look at.)
This is done because of how we chose to deliver Nuget packages for libraries:  library source files are included as embedded resources.
There are two types of resources we look for, distinguished by name.  One is an embedded assembly, one is an embedded text file.  They are distinguished by name.

I'm not sure if I even have any cases where an embedded assembly is found and I think there may be a bug there created when `.cljc` extensions were introduced.  
At any rate, it appears now to look only for an embedded resource named `a_b.c.d.cljc.dll`.
If it finds it, it uses an overload of `System.Reflection.Assembly.Load` that takes a byte array to load the assembly, 
then initializes it as described above.

Otherwise we look for an embedded resource named either `a_b.c.d.clj` or `a_b.c.d.cljc`, which it treats like a source file and calls either `RT.Compile` or `RT.LoadScript`, depending.

>>> Convention #4: an assembly or source file named appropriately as an embedded resource in a loaded assembly will be loaded.  
However, if something on the file system is found, that takes precedence.  
(Maybe all possibilities should be looked at and an error declared if more than one exists.)

## Where do we look

Where on the file system do we look.  The answer is in `RT.FindFile`.  
Well, actually that just interates throught the result of `RT.GetFindFilePaths()` to find the directories to search.
And what are those directories?

1. `System.AppDomain.CurrentDomain.BaseDirectory`  -- where the Clojure executable resides
2. `System.AppDomain.CurrentDomain.BaseDirectory` + `\bin` -- I don't know why
3. `Directory.GetCurrentDirectory()` -- the current working directory of the application
4. `Path.GetDirectoryName(typeof(RT).Assembly.Location)`  -- where the Clojure.dll assembly is located.  Not sure if that is every different from (1).
5.  The directory of the assembly ` Assembly.GetEntryAssembly()` -- this was added for some case I don't remember
6.  The set of paths that is the value of the environment variable `CLOJURE_LOAD_PATH`.  This is my workaround for not having the equivalent of the Java classpath.

There is one final option.  I no longer remember why this exists.  I do not know if it is ever used.
In `core-clr.clj` there is this:

```
(defn add-ns-load-mapping
  "Convenience function to assist with loading .clj files embedded in
  C# projects.  ns-root specifies part of a namespace such as MyNamespace.A and
  fs-root specifies the filesystem location in which to look for files within that
  namespace.  For example, if MyNamespace.A mapped to MyNsA would allow
  MyNamespace.A.B to be loaded from MyNsA\\B.clj.  When a .clj file is marked as an
  embedded resource in a C# project, it will be stored in the resulting .dll with
  the default project namespace prefixed to its path.  To allow these files to
  be loaded dynamically during development, the paths to these files can be mapped
  to allow them to be loaded from a different directory other than their root namespace
  (i.e. the common case where the project directory is different from its default
  namespace)."
  {:added "1.5"}
  [^String ns-root ^String fs-root]
  (swap! *ns-load-mappings* conj
	[(.Replace ns-root "." "/") fs-root]))
```

Essentially, the variable `*ns-load-mappings` is a sequence of two-element vectors, each vector having the form of `["MyNamespace.A", "MyNSA"]`.
if we are looking for a source file for `MyNamespace/A/B.clj`, say,  we will see if  `MyNSA/B.clj` exists.  Again, I have no idea if this is ever used.

>>> Convention #(I don't want to dignify this): we can map namespaces to directories to search.

## The missing pieces

As promised above:

1. `Compiler.LoadAssembly` -- pretty straightforward.  Just call `System.Reflection.LoadFrom` on the located assembly, then call its initialization method, as decribed above.
2. `RT.Compile` calls `Compiler.Compile` on appropriate arguments.  The var `*compile-path*` must be set.This creates a dynamic assembly, compiles all the forms from the source file into that assembly (as well as evaluating them), then saves the assembly.
3. `RT.LoadScript` calls `Compiler.load` which iterates through all the forms and evaluates them.  (They still have to be compiled in order to do this.  There is just less work to do and some special handling for `do` and `def` forms.




