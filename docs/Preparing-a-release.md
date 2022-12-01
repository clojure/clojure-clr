<!-- -*- mode: markdown ; mode: visual-line ; coding: utf-8 -*- -->

# Preparing for a new release

## Preparation

* Modify `Clojure\Clojure\Bootstrap\version.properties` to desired release version.
* Set the version info in `Clojure\CurrentVersion.props`.
    * Please note that you should use lowercase letters only in the pre-release designation in order to avoid problems on non-Windows systems, i.e., `beta1` instead of `Beta1`, `rc1` instead of `RC1`.
* Build and test.  From the `Clojure` directory with X in {net462, netcoreapp3.1, net6.0, net7.0} (Configuration will default to Debug. Substitute in names accordingly if that is case.)
    * `msbuild build.proj -t:Test -p:TestTargetFramework=X -p:Configuration=Release`
    * `msbuild build.proj -t:TestGen -p:TestTargetFramework=X -p:Configuration=Release`
* Package.  From the `Clojure` directory, run
    * `msbuild build.proj -t:ZipAll  -p:Configuration=Release`
* Artifacts.  At this point, you will the artifacts for distribution in the `Clojure\Stage` directory.  Something along the lines of (with version/release adjusted suitably):
    * Clojure.1.10.0-alpha1.nupkg
    * Clojure.Main.1.10.0-alpha1.nupkg
    * clojure-clr-1.10.0-alpha1-Release-core3.1.zip
    * clojure-clr-1.10.0-alpha1-Release-net5.0.zip
    * clojure-clr-1.10.0-alpha1-Release-net6.0.zip
    * clojure-clr-1.10.0-alpha1-Release-net4.6.1.zip
* Validate these by any manner of your choosing.  I moved the zips somewhere, unzipped them, and checked that the following start up:
    * in core3.1, net5.0, and net6.0:
        * > `Clojure.Main.exe`          (This might not be created on non-Windows builds.)
        * > `dotnet Clojure.Main.dll`
    * in net462:
        * > `Clojure.Main461.exe`
        * > `Clojure.Compile.exe`     (Without command-line args, will just exit after the startup delay)
    * I test the tool install via:
        * > `dotnet tool list -g`                        # to see if already installed
        * > `dotnet tool uninstall clojure.main -g`      # if so, do this
        * > `dotnet tool install -g --add-source . --version 1.10.0-alpha1  Clojure.Main`    # version needed only if pre-release.


## Repo

In git:

* Commit the changed version  (Prepare release ...)
* Tag this commit.
* Push the change and the tag to the github repo.

## Nuget distribution

* Clojure\Stage> `dotnet nuget push Clojure.Main<whatever>.nupkg -s nuget.org`
* Clojure\Stage> `dotnet nuget push Clojure<whatever>.nupkg  -s nuget.org`


## SourceForge distribution

* Upload to SourceForge

# Preparing for next release

* Modify `Clojure\Clojure\Bootstrap\version.properties` to desired development version  ( usually `master-SNAPSHOT`)
* Set the version info in `Clojure\CurrentVersion.props` to match.
* Commit the change (Prepare for next development iteration ...)