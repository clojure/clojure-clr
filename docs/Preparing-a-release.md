<!-- -*- mode: markdown ; mode: visual-line ; coding: utf-8 -*- -->

# Preparing for a new release

## Preparation

* Modify Clojure\Clojure\Bootstrap\version.properties to desired release version
* Modify Clojure.nuspec to desired release version
* Make sure CLOJURE_SNK is set to the .snk file to use
** > set CLOJURE_SNK=d:\work\ClojureClr.snk
* Build and test all of {Release, Debug} X {3.5, 4.0}
* Commit the changed version  (Prepare release ...)
* Tag this commit.
* Push the change and the tag to github.

## For NuGet

* Make sure ILMERGE is on the PATH
* Build the ILMERGEd dlls
** > msbuild build.proj /target:ilmerge /p:Configuration="Release 4.0" /p:Platform="Any CPU"
** > msbuild build.proj /target:ilmerge /p:Configuration="Release 3.5" /p:Platform="Any CPU"
* Build the nuget package
** > nuget pack Clojure.nuspec
* Upload the nuget package
** > nuget push whatever.nupkg

## For SourceForge

* Build the distributions:
** > msbuild build.proj /target:Dist /p:Configuration="Release 4.0" /p:Platform="Any CPU"
** > msbuild build.proj /target:Dist /p:Configuration="Release 3.5" /p:Platform="Any CPU"
** > msbuild build.proj /target:Dist /p:Configuration="Debug 4.0" /p:Platform="Any CPU"
** > msbuild build.proj /target:Dist /p:Configuration="Debug 3.5" /p:Platform="Any CPU"
* Zip each of the four directories into separate zips. Naming is like "clojure-clr-1.6.0-Release-4.0.zip"
* Upload to SourceForge

# Preparing for next release

* Modify Clojure\Clojure\Bootstrap\version.properties to desired development version  (master-SNAPSHOT)
* If next iteration is a new point release, modify the version in the AssemblyInfo of all projects
* Commit the change (Prepare for next development iteration ...)