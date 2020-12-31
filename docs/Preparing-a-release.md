<!-- -*- mode: markdown ; mode: visual-line ; coding: utf-8 -*- -->

# Preparing for a new release

## Preparation

* Modify Clojure\Clojure\Bootstrap\version.properties to desired release version
* Set the version info in Clojure\CurrentVersion.props
* Build and test.  From the Clojure directory with X in {net461, netcoreapp3.1, net5.0} (for now, this will be Debug builds only):
** Clojure>  msbuild build.proj -t:Test -p:TestTargetFramework=X 
** Clojure>  msbuild build.proj -t:TestGet -p:TestTargetFramework=X
** Clojure>  msbuild build.proj -t:ZipAll 
* At this point, you will the artifacts for distribution in the Clojure\Stage directory.  Something along the lines of (with version/release adjusted suitably):
** Clojure.1.10.0-alpha1.nupkg
** Clojure.Main.1.10.0-alpha1.nupkg
** clojure-clr-1.10.0-alpha1-Debug-core3.1.zip
** clojure-clr-1.10.0-alpha1-Debug-net5.0.zip
** clojure-clr-1.10.0-alpha1-Debug-net4.6.1.zip
* Validate these by any manner of your choosing.  I moved the zips somewhere, unzipped them, and checked that the following start up:
** in core3.1 and net5.0:
*** > Clojure.Main.exe          
*** > dotnet Clojure.Main.dll
** in net461:
*** > Clojure.Main461.exe
*** > Clojure.Compile.exe     (Without command-line args, will just exit after the startup delay)
** I test the tool install via:
*** > dotnet tool list -g                        # to see if already installed
*** > dotnet tool uninstall clojure.main -g      # if so, do this
*** > dotnet tool install -g -add-source . --version 1.10.0-alpha1  Clojure.Main    # version needed only if pre-release.


## Repo

In git:

* Commit the changed version  (Prepare release ...)
* Tag this commit.
* Push the change and the tag to the github repo.

## Nuget distribution

** Clojure\Stage> dotnet nuget push Clojure.Main<whatever>.nupkg -Source nuget.org
** Clojure\Stage> dotnet nuget push Clojure<whatever>.nupkg  -Source nuget.org


## SourceForge distribution

* Upload to SourceForge

# Preparing for next release

* Modify Clojure\Clojure\Bootstrap\version.properties to desired development version  ( usually master-SNAPSHOT)
* Set the version info in Clojure\CurrentVersion.props to match.
* Commit the change (Prepare for next development iteration ...)