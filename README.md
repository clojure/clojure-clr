# ClojureCLR

This project is a native implementation of Clojure on the Common Language Runtime (CLR),
the execution engine of Microsoft's .Net Framework.

ClojureCLR is programmed in C# (and Clojure itself).

## Goals

* Implement a feature-complete Clojure on top of CLR
* Stay as close as possible to the JVM implementation
* Have some fun

## Getting Started

ClojureCLR can either run as a a standalone application, or embedded as a library in .NET applications. See the
[getting started](https://github.com/clojure/clojure-clr/wiki/Getting-started) guide for instructions on how to
install, run or integrate ClojureCLR.

## Documentation

Most of the documentation for [Clojure](https://clojure.org/) should be equally applicable to ClojureCLR. See the
ClojureCLR [wiki]((https://github.com/clojure/clojure-clr/wiki)) and the articles under the [docs](/docs) directory
in this repository for documentation specific to the ClojureCLR project.

## Community and Support

* The [#clr](https://clojurians.slack.com/archives/C060SFCPR) channel in the [Clojurians Slack](https://clojurians.slack.com)
* The [ClojureCLR](https://ask.clojure.org/index.php/clojureclr) category in [Ask Clojure](https://ask.clojure.org/)
* [ClojureCLR JIRA](https://clojure.atlassian.net/jira/software/c/projects/CLJCLR/issues/) is used for issue tracking.
Note that creating issues in the Clojure JIRA requires signing a
[contributor agreement](https://clojure.org/dev/contributor_agreement). Minor issues might be easier to raise on Slack.

## Libraries

Many of the standard libraries from JVM Clojure have [CLR ports](https://github.com/orgs/clojure/repositories?q=clr).

## Other Resources

* [David Miller and Clojure on the CLR](https://soundcloud.com/defn-771544745/48-david-miller-and-clojure-on-the-clr) with [@dmiller](https://github.com/dmiller/)
* [Transform Microsoft Office Solutions into Cloud-savvy Linked Data Microservices With Clojure on .NET](https://www.youtube.com/watch?v=pImaXoTPWWA) with [@bcalco](https://github.com/bcalco/)

## License ##

    Copyright (c) Rich Hickey. All rights reserved. The use and
    distribution terms for this software are covered by the Eclipse
    Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
    which can be found in the file epl-v10.html at the root of this
    distribution. By using this software in any fashion, you are
    agreeing to be bound by the terms of this license. You must
    not remove this notice, or any other, from this software.