# Diary of a library port using lein-clr

Recording the steps of porting a ClojureJVM library toe ClojureCLR using lein-clr.

## Getting lein-clr

lein-clr is and add-on to leiningen to work with ClojureCLR.  
Developed by Shantanu Kumar (thank you) back in the early days of ClojureCLR, it just keeps working.  
The last update was in 2015.

Frankly, I don't use all its capabilities, just enough to get projects created, tested, and packaged (for clojars).

You will need Java.  I'll assume you can figure that out.

You will need Leiningen.  Go to the [Leiningen home page](https://leiningen.org/) for instructions.  
For Windows folks, there is a `lein.bat` file to do the heavy lifting.

Then follow the instructions on the [lein-clr home page](https://github.com/kumarshantanu/lein-clr) 
to install lein-clr.  Take note of the available commands.


## Creating a project

Make sure `lein.bat` is on your `PATH` or directly specify it by location in the following.

Go to the directory in which you wish to create the new project directory and execute

```
 lein new lein-clr <project-name>
 cd <project-name>
```

In my case, I'm working on porting core.async, so my command is

```
 lein new lein-clr clr.core.async
 cd clr.core.async
```

If necessary, do whatever magic is necessary to get this under your favorite version control system.  I will say no more.

## Editing the default project files

The new project command creates several files.

```
.gitignore
doc/intro.md
project.clj
README.md
src\clr\core\async\core.clj
test\clr\core\async\core_test.clj
```

At this point you have a working, if unexciting project.
You can run the one (failing) test with

```
lein clr test
```

to make sure things work.
well, you will most likely discover that the environment variable CLJCLR14_40 needs to be defined.  (Yeah, this version of lein-clr goes back to version 1.4 of ClojureClR.)
Set the environment variable to the path where your ClojureCLR binary is located.  (If you've installed it as a dotnet tool?  Tough luck.  lein-clr is about 7 years too early for that nonsense.  I supposed it could be updated.  Go for it.)

If all is okay, onward.

You can delete the `doc`, `src`, and `test` directories.  We will use a slightly different code layout, in keeping with recent practice elsewhere.

Edit the `README.md` to something reasonble for your project.

The `project.clj` file defines the details of the project.   Initially it looks like this;

```
(defproject clr.core.async "0.1.0-SNAPSHOT"
  :description "FIXME: write description"
  :url "http://example.com/FIXME"
  :license {:name "Eclipse Public License"
            :url "http://www.eclipse.org/legal/epl-v10.html"}
  :dependencies []
  :warn-on-reflection true
  :min-lein-version "2.0.0"
  :plugins [[lein-clr "0.2.1"]]
  :clr {:cmd-templates  {:clj-exe   [[?PATH "mono"] [CLJCLR14_40 %1]]
                         :clj-dep   [[?PATH "mono"] ["target/clr/clj/Debug 4.0" %1]]
                         :clj-url   "http://sourceforge.net/projects/clojureclr/files/clojure-clr-1.4.1-Debug-4.0.zip/download"
                         :clj-zip   "clojure-clr-1.4.1-Debug-4.0.zip"
                         :curl      ["curl" "--insecure" "-f" "-L" "-o" %1 %2]
                         :nuget-ver [[?PATH "mono"] [*PATH "nuget.exe"] "install" %1 "-Version" %2]
                         :nuget-any [[?PATH "mono"] [*PATH "nuget.exe"] "install" %1]
                         :unzip     ["unzip" "-d" %1 %2]
                         :wget      ["wget" "--no-check-certificate" "--no-clobber" "-O" %1 %2]}
        ;; for automatic download/unzip of ClojureCLR,
        ;; 1. make sure you have curl or wget installed and on PATH,
        ;; 2. uncomment deps in :deps-cmds, and
        ;; 3. use :clj-dep instead of :clj-exe in :main-cmd and :compile-cmd
        :deps-cmds      [; [:wget  :clj-zip :clj-url] ; edit to use :curl instead of :wget
                         ; [:unzip "../clj" :clj-zip]
                         ]
        :main-cmd      [:clj-exe "Clojure.Main.exe"]
        :compile-cmd   [:clj-exe "Clojure.Compile.exe"]})
```

- Edit the version number in the first line.
- Change the `:url` value to indicate the project's web location.
- Edit the `:description` text.

The standard code layout is for the `src/` directory to have the application code and the `test/` directory to have the tests.  
Projects in the ClojureJVM sphere initiated more recently seem to have a structure where all code is under `src/`, 
with app code under `src/main` and tests under `src/test`.  
In fact, because these projects may mix Clojure and non-Clojure code, 
subdirectories are used to segregate different categories.  
Hence, Clojure code will be under `src/main/clojure` and `src/test/clojure`.
Accordingly, add the following clauses to the `defproject`:

```
:source-paths ["src/main/clojure"]
:test-paths ["src/test/clojure"]
```

Finally, if you happen to be planning to deploy to `clojars.org`, Leiningen will help out if you include:

```
  :deploy-repositories [["clojars" {:url "https://clojars.org/repo/"
                                    :sign-releases  false}]]
```

## Contemplate the folly of attempting what you are about to do

Well, um, time to actually look at the code you are trying to port.  I'll go through this specifically using core.async.
The pain you are about to undergo is roughly proportional to how much interop there, and what fraction of that is of the trivial variety vs deeper levels of incompatibility.

The trivial variety is retyping method and sometimes class names: `.toString` to `.ToString`, `.HashCode` to `GetHashCode` and the like.  Call this level zero.

Level one would be where the computing model is different, but navigable without too much strain.  A common example is I/O.  

Level two would be where the computing model is significantly different and involves data structures that have no clear parallels on the other side.
Threading is one such thing.  There is a lot of this in `core.async`.  Now taking a deep breath.

## Assessing the folly

Make a quick scan of the code to assess the amount and kind of interop present.  
At the same time, you can sketch the DAG of dependencies amoung the `clj` files to help develop a plan of attack.
Step through all the source files.  Look at the namespace declaration to get the `clj` dependencies and see what Java namespaces and classes are imported.
Then scan the body of the code to look for interop calls.

Editing out (most of) the comments and the some other irrelevancies in the `ns` definitions we get this:


```
(ns clojure.core.async
  "Facilities for async programming and communication.
go blocks are dispatched over an internal thread pool, which
defaults to 8 threads. The size of this pool can be modified using
the Java system property `clojure.core.async.pool-size`.
Set Java system property `clojure.core.async.go-checking` to true
to validate go blocks do not invoke core.async blocking operations.
Property is read once, at namespace load time. Recommended for use
primarily during development. Invalid blocking calls will throw in
go block threads - use Thread.setDefaultUncaughtExceptionHandler()
to catch and handle."
  (:require [clojure.core.async.impl.protocols :as impl]
            [clojure.core.async.impl.channels :as channels]
            [clojure.core.async.impl.buffers :as buffers]
            [clojure.core.async.impl.timers :as timers]
            [clojure.core.async.impl.dispatch :as dispatch]
            [clojure.core.async.impl.ioc-macros :as ioc]
            [clojure.core.async.impl.mutex :as mutex]
            [clojure.core.async.impl.concurrent :as conc]
            )
  (:import [java.util.concurrent.atomic AtomicLong]
           [java.util.concurrent.locks Lock]
           [java.util.concurrent Executors Executor ThreadLocalRandom]
           [java.util Arrays ArrayList]
           [clojure.lang Var]))
```

```
(ns ^{:skip-wiki true}
  clojure.core.async.impl.buffers
  (:require [clojure.core.async.impl.protocols :as impl])
  (:import [java.util LinkedList]
           [clojure.lang Counted]))
```


```
(ns ^{:skip-wiki true}
  clojure.core.async.impl.channels
  (:require [clojure.core.async.impl.protocols :as impl]
            [clojure.core.async.impl.dispatch :as dispatch]
            [clojure.core.async.impl.mutex :as mutex])
  (:import [java.util LinkedList Queue]
           [java.util.concurrent.locks Lock]
           [clojure.lang IDeref]))

```

```
(ns ^{:skip-wiki true}
  clojure.core.async.impl.concurrent
  (:import [java.util.concurrent ThreadFactory]))
```

```
(ns ^{:skip-wiki true}
  clojure.core.async.impl.dispatch
  (:require [clojure.core.async.impl.protocols :as impl]
            [clojure.core.async.impl.exec.threadpool :as tp]))
```

```
(ns ^{:skip-wiki true}
  clojure.core.async.impl.ioc-macros
  (:refer-clojure :exclude [all])
  (:require [clojure.pprint :refer [pprint]]
            [clojure.tools.analyzer.ast :as ast]
            [clojure.tools.analyzer.env :as env]
            [clojure.tools.analyzer.passes :refer [schedule]]
            [clojure.tools.analyzer.passes.jvm.annotate-loops :refer [annotate-loops]]
            [clojure.tools.analyzer.passes.jvm.warn-on-reflection :refer [warn-on-reflection]]
            [clojure.tools.analyzer.jvm :as an-jvm]
            [clojure.core.async.impl.protocols :as impl]
            [clojure.set :as set])
  (:import [java.util.concurrent.locks Lock]
           [java.util.concurrent.atomic AtomicReferenceArray]))
```

```
(ns ^{:skip-wiki true}
  clojure.core.async.impl.mutex
  (:import [java.util.concurrent.locks Lock ReentrantLock]))
```

```
(ns ^{:skip-wiki true}
  clojure.core.async.impl.protocols)
```

```
(ns ^{:skip-wiki true}
  clojure.core.async.impl.timers
  (:require [clojure.core.async.impl.protocols :as impl]
            [clojure.core.async.impl.channels :as channels])
  (:import [java.util.concurrent DelayQueue Delayed TimeUnit ConcurrentSkipListMap]))
```

```
(ns clojure.core.async.impl.exec.threadpool
  (:require [clojure.core.async.impl.protocols :as impl]
            [clojure.core.async.impl.concurrent :as conc])
  (:import [java.util.concurrent Executors]))
```


Collecting the imports, we get:

```
java.util:  Arrays ArrayList LinkedList Queue
java.util.concurrent: Executors Executor ThreadFactory ThreadLocalRandom
java.util.concurrent.atomic:  AtomicLong AtomicReferenceArray ConcurrentSkipListMap DelayQueue Delayed TimeUnit 
java.util.concurrent.locks: Lock ReentrantLock
clojure.lang: Var Counted IDeref
```
 
The `clojure.lang` references are non-problematic.  ClojureCLR implements them.

The `java.util` references are also straightforward.  The CLR has pretty direct equivalents.

The `java.util.concurrent.*` classes are extremely problematic.  
A few are straightforward: `AtomicLong`, `AtomicReferenceArray`.  
Some are already implemented in ClojureCLR; others can be implemented easily.
For the rest, we are in trouble.  
CLR does not have thread factories, executors, and the like.  
Figuring out the correct model to use is going to require a deep dive into the ugly details of exactly how threads are being used here.  More below.

Doing a quick code scan to look for interop that is not caught by there references, we see:

- `clojure.core.async`: Most of the complexity is at the lower levels.  
A few direct calls to manipulate threads, the odd `ArrayList` -- not much to lose sleep over.

- `clojure.core.async.impl.buffers`: mostly protocol implementations, nothing serious.
-  `clojure.core.async.impl.channels`: mostly list manipulation
- `clojure.core.async.impl.concurrent`: only one function here, but it reifies `ThreadFactory`, so that's a problem.
- `clojure.core.async.impl.dispatch`: only a few functions, but all about thread manipulation.
- `clojure.core.async.impl.ioc-macros`: complicated code, but interop is minimal
- `clojure.core.async.impl.mutex`:  only one function, basically just reifies `Lock` around a `ReentrantLock`.  Have to check the docs, but CLR locks are reentrant, so probably a no-op.
- `clojure.core.async.impl.protocols`: protocols all the way, no interop at all
- `clojure.core.async.impl.timers`: Messes with time and queues, should be okay.
- `clojure.core.async.impl.exec.threadpool`: defines only one function, which plays with fixed thread pools.  Need a little thought.

While we are at it, we can map out the dependencies:

`clojure.core.async` depends on everything, so we know it will be last.

The following have no dependencies and can come first:

- `clojure.core.async.impl.concurrent`
- `clojure.core.async.impl.protocols`
- `clojure.core.async.impl.mutex`:

The following depend only on `protocols`:

- `clojure.core.async.impl.buffers`
- `clojure.core.async.impl.ioc-macros`

And we have one long chain:

- `clojure.core.async.impl.timers` depends on
- `clojure.core.async.impl.channels` depends on
- `clojure.core.async.impl.dispatch` depends on
- `clojure.core.async.impl.exec.threadpool` depends on
   - `clojure.core.async.impl.protocols` and
   - `clojure.core.async.impl.concurrent`

Any topological sort on the implied DAG gives an order of attack.

## Testing

You likely will want a testing strategy.  
If you are lucky, the library your porting has sufficient tests to provide confidence that your port is correct.
If you are luckier, there will be tests on a namespace-by-namespace basis so that you can test each piece as you work your way through the individual namespaces.

For `core.async`, we are mostly lucky.  There are two sets of tests for the public API in `core.async`.
And there are individual test suites for `buffers`, `concurrent`, `ioc_macros`, and `timers`.  (I'm ignoring one piece of experimental code and its tests)
There is an additional test suite `clojure.core.async.exceptions-test` with the stated pupose to  "[v]erify that exceptions thrown on a thread pool managed by
  core.async will propagate out to the JVM's default uncaught  exception handler."  It all looks pretty straightforward for porting and gives us waypoints for testing as we progress.
  
## Threading

For `core.async`, we have to think carefully about how to proceed.  
First, we need to understandthe model of computation that `core.async` is implementing.
Second, we need to consider our options under the CLR.
Third, we need to pick an option and go for it.

I won't go into details on `core.async`'s rationale, guiding principles.  Go here: 
 
- [Rationale](https://clojure.org/news/2013/06/28/clojure-clore-async-channels)
- [API docs](https://clojure.github.io/core.async/)
- [Github repo](https://clojure.github.io/core.async/index.html)
 
Let me hit the salient points for our work here.  Quoting from the aforementioned Rationale:

> The objectives of core.async are:
> - To provide facilities for independent threads of activity, communicating via queue-like _channels_
> - To support both real threads and shared use of thread pools (in any combination), as well as ClojureScript on JS engines
> - To build upon the work done on CSP and its derivatives

Channels are a communication and coordination device.  Think of them primarily as buffers than can lock a thread on reading/writing if empty/full.
As such, the implementation is mostly about locks (mutexes).  
No explicit thread manipulation.  
They can be accessed from 'regular' threads or from _go blocks_ (see next) -- "i.e. the channel is oblivious to the nature of the threads which use it."

However, the code to take/put from/to channels is not oblivious.  
The primary channel operators for code not in a go block are `>!!` ( _put blocking_ ) and `<!!` ( _take blocking_ ).
The equivalent operators inside go blocks (and only there) are `>! ( _put_ ) and `<!` ( _take_ ).

For go blocks, let us quote:

> `go` is a macro that takes its body and examines it for any channel operations. 
> It will turn the body into a state machine. 
> Upon reaching any blocking operation, the state machine will be 'parked' 
> and the actual thread of control will be released. 
> This approach is similar to that used in _C# async_. 
> When the blocking operation completes, the code will be resumed 
> (on a thread-pool thread, or the sole thread in a JS VM).

The `go` macro itself does code analysis and rewriting.  
It relies on the library `clojure.tools.analyzer`, fortunately a library I ported many years ago.
(Though I will need to check it for updates.)

The only thread manipulation involved is the use of a limited size thread pool.  
There are some little details about not being able to start another thread-pool thread from within a thread-pool thread, 
and some details about unhandled exceptions. 

How can we implement this on the CLR?  There is not a defined limited-size bespoke-use thread pool mechanism.
Three possible solutions come to mind.

1. Implement our own thread-pool having the appropriate characteristics.
2. Use the system thread pool.
3. Use the system thread pool, but implement everything in terms of the task library, the "modern" approach taken by the C# async model mentioned above.

Implementing one's own thread pool is generally discouraged.  
(I remember a series of articles, probably in MSDN Magazine, in the early days of .Net 
in which Stephen Toub stepped through an implementation of thread pooling.  
I think there were followups for correction of edge cases 
and a final conclusion that you shouldn't do this.  But I'm working from memory here.)
What is called for here is a very simple example of a thread pool, given that it has fixed size, 
but still one perhaps should heed the cautions.

Using the system thread pool should make most of the threading interaction very simple to implement.  But it lacks having a reserved pool of threads with a limited count.  How important is that?
Perhaps not important enough to overcome the warnings above and the inertia of rolling one's own.

Doing things with tasks would be interesting, but would require more thought than I care to put into it right now. 
Something to consider for the future perhaps.

So, #2 (use the system thread pool) is good enough to get us started.

And away we go.

## The process	

1. Pick a namespace with no unsatisfied dependencies.
2. Copy the file over and edit to fix interops.
3. use `lein clr repl` to load the file.  If it fails to load, go to 2.
4. Try some hand tests.
5. If there are unit tests available, port that code, and use `lein clr test`.
6. Repeat.

## Diary

- `clojure.core.async.impl.protocols`: Easy enough.  All protocols. 
The only hiccup is a conflict between the `Buffer` protocol and the imported `System.Buffer`.  Renamed `Buffer` as `ABuffer`.  (Is there a better solution?  How would we exclude the mapping `Buffer` -> `System.Buffer`?)

- ` clojure.core.async.impl.mutex`: Also easy.
In the original, one function, `mutex`, that just wraps a `ReentrantLock` and provides `Lock` semantics for it.
Not having the direct equivalent of `java.util.concurrent.locks.Lock`, 
I defined a protocol `ILock`  with `lock` and `unlock` methods, 
defined a type `Lock` that wraps a `System.Threading.Mutex` and implements `ILock`, and defined `mutex` to create a `Lock`.

- `clojure.core.async.impl.concurrent`: No real point in doing this -- it implements a function that reifies `java.util.concurrent.ThreadFactory` and we are not going to be creating threads like this.
But what the heck, might has well have some fun.  
I defined a protocol named `ThreadFactory` with the one method `newThread`, then did the equivalent `reify` -- just some minor changes to the thread creation code to accommodate the JVM/CLR API differences.
This came a test suite (okay, just one test), but it ported easily -- and passed.

- `clojure.core.async.impl.buffers`: Pretty easy.  For `java.util.LinkedList`, we substitute `System.Collections.Generic.LinkedList<Object>`, change the method names  (`.addFirst` to `.AddFirst`, e.g.), and off we go.
The test suite ran the first ... nope.  One nasty little API difference. `RemoveLast` has void return value; `removeLast` in Java returns the value removed.  So we have to get the last _node_, do the remove, then return the value from the node.  Sigh.
Then the tests work just fine. 

- `clojure.core.async.impl.exec.threadpool`: Here is one place where we hit threads directly.  The only function is `thread-pool-executor`; it takes an optional initialization function

- `clojure.core.async.impl.dispatch`: Another short one.  Mostly just uses what we just implemented in `threadpool.clj`.  Except for the use of ThreadLocal to set a flag indicating the thread is in our special thread pool.
Since we are not using a special thread pool, we have no good way to prevent threads spawning threads.  So it goes.  We're just going to ignore it.

- `clojure.core.async.impl.channels`: the largest of the `impl` files.  This one is a bit problematic, from an unusual direction.  There are multiple iterations through lists that delete via the iterator.
Deletions via an iterator are not possible in C#/.Net.   So all those loops had to be rewritten.  And there is one semantic difference.  the JVM can set up uncaught exception handlers per thread.  Doesn't happen in the CLR.  
I just bypassed that code completely.  (And the test suite dedicated to it.)








 