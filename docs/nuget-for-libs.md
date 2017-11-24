# Creating NuGet packaging for ClojureCLR libs

At present, we've been using lein-clr for managing our libraries.   We use the standard project layout.  Source code is in `<root>/src`.  If the project namespace is something like `clojure.tools.reader`, source code will be under `<root>/src/clojure/tools/reader`.

(Though lately, I've been playing matching exactly the source code layout in the parent JVM libs, setting the :source-paths and :test-paths properties in project.clj to match.)

There are two ways to create the NuGet packaging.  One uses a C# project and Visual Studio to create the necessary .nuspec file.  The other just creates/edits the .nuspec file by hand.  Once the .nuspec file is in hand, packaging and publishing is the same for either method.

# Method 1: Creating the .nuspec file by hand (preferred/simpler)

## Method 1: Step 1:: Create the .nuspec file by hand 

Create a file called  X.nuspec, where X is named for the project, something like clojure.tools.reader.nuspec.
You can put the file anywhere; you'll have to adjust the relative paths to the files accordingly.  One possible directory is src/clojure or src/main/clojure, depending on how you have the 

The X.nuspec file should like this:

    <?xml version="1.0" encoding="utf-8"?>
    <package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd">
        <metadata>
            <id>clojure.tools.reader</id>
            <version>1.9.0-alpha15</version>
    	    <title>clojure.tools.reader</title>
            <authors>yourself</authors>
    		<owners>yourself</owners>
            <requireLicenseAcceptance>false</requireLicenseAcceptance>
            <licenseUrl>http://opensource.org/licenses/eclipse-1.0.php</licenseUrl>
            <projectUrl>https://github.com/clojure/clr.tools.reader</projectUrl>
            <iconUrl>http://clojure.org/file/view/clojure-icon.gif</iconUrl>
            <description>Something appropriate</description>
    		<releaseNotes>Something appropriate</releaseNotes>
    		<copyright>Copyright yourself 2017</copyright>
            <tags>Clojure ClojureCLR</tags>
    	<dependencies>
    	  <dependency id="clojure.tools.namespace" version="0.2.7"/>
    	  <dependency id="clojure.data.generators" version="0.1.0"/>
    	</dependencies>
      </metadata>
      <files>
        <file src="path\to\file1.clj" target="lib" />
        <file src="path\to\file2.clj" target="lib" />
        <file src="path\to\*.clj" target="lib" />
      </files>
    </package>

Notes on elements, from https://docs.microsoft.com/en-us/nuget/schema/nuspec :
	
	
* `<id>` -- package identifier, how it will be referenced on nuget.org
* `<authors>` -- comma separated, should match profile name on nuget.org.
* `<version>` -- `major.minor.patch` + optional pre-release suffix.

## Method 1: Step 2::  Pack and publish


* From the command line, in whatever directory you've placed the .nuspec file, execute:

    nuget pack clr.tools.reader.nuspec
	
This will have created a `clr.tools.reader.1.0.0.0.nupkg` file (whatever the version is).  Given that it is a ZIP, you can inspect it with your favorite ZIP tool.

To publish:

    nuget push clr.tools.reader.1.0.0.0.nupkg -source nuget.org

Go to https://nuget.org to verify.


# Method 2: Use a C# project and Visual Studio to create the .nuspec file

## Method 2: Step 1:: Create the C# project

Using Visual Studio (the easiest way), create a project, via `File > New project from existing code`.  

* Project type: Visual C# / Class Library
* Name = `clr.tools.reader`  
* Location of existing items: `<root>/src/clojure`

This will create C# project `<root>/src/clojure/clr.tools.reader.csproj` and a solution `<root>/src/clojure/clr.tools.reader.sln`.


Edit the solution properties:

* Assembly name: clr.tools.reader
* Default namespace = clojure
* Target framework  = .NET Framework 3.5

We'll need to edit the `AssemblyInfo.cs`.  One has not been created.  The easiest way to do so is to click the "Assembly Information" button on the solution properties (application tab) page.  Like the following, adjusted to the circumstances.

    [assembly: AssemblyTitle("clojure.tools.reader")]
    [assembly: AssemblyDescription("Does something for someone.")]
    [assembly: AssemblyConfiguration("")]
    [assembly: AssemblyCompany("Whatever")]
    [assembly: AssemblyProduct("clojure.tools.reader")]
    [assembly: AssemblyCopyright("Copyright © Someone 2049")]
	
    [assembly: AssemblyVersion("0.5.0.0")]
    [assembly: AssemblyFileVersion("0.5.0.0")]
	
When using this dialog, unclick the COM-visible checkbox.	
	
* Remove all references.
* You should see all the files in the <root>/src/clojure directory and below.  Remove any files you do not want in the solution.  For the rest, set properties:

    Build Action: Embedded Resource
	Copy to Output Directory: Do not copy

    
* Build the solution.
* If you want to verify, take some kind of .Net assembly viewer and check it out.  (I used ILSpy.  The files appear as resources.  You can inspect contents.)

### Method 2: step 2:: Create the nuspec file.

From the command line, `cd` into `<root>/src/clojure`.  Execute:

    nuget spec
	
This will find `clr.tools.reader.csproj` and create a tokenized `clr.tools.reader.nuspec` file.  That file will look something like this:

    <?xml version="1.0"?>
    <package >
      <metadata>
        <id>$id$</id>
            <version>$version$</version>
        <title>$title$</title>
        <authors>$author$</authors>
        <owners>$author$</owners>
        <licenseUrl>http://LICENSE_URL_HERE_OR_DELETE_THIS_LINE</licenseUrl>
        <projectUrl>http://PROJECT_URL_HERE_OR_DELETE_THIS_LINE</projectUrl>
        <iconUrl>http://ICON_URL_HERE_OR_DELETE_THIS_LINE</iconUrl>
        <requireLicenseAcceptance>false</requireLicenseAcceptance>
        <description>$description$</description>
        <releaseNotes>Summary of changes made in this release of the package.</releaseNotes>
        <copyright>Copyright 2016</copyright>
        <tags>Tag1 Tag2</tags>
      </metadata>
    </package>

Edit to look something like:


    <?xml version="1.0"?>
    <package >
      <metadata>
        <id>$id$</id>
        <version>$version$</version>
        <title>$title$</title>
        <authors>$author$</authors>
        <owners>$author$</owners>
        <licenseUrl>http://opensource.org/licenses/eclipse-1.0.php</licenseUrl>
        <projectUrl>https://github.com/clojure/clr.tools.reader</projectUrl>
        <requireLicenseAcceptance>false</requireLicenseAcceptance>
        <description>$description$</description>
        <releaseNotes>First version packaged for NuGet.</releaseNotes>
        <copyright>Copyright David Miller 2049</copyright>
        <tags>Clojure ClojureCLR</tags>
    	<dependencies>
    	  <dependency id="clojure.tools.namespace" version="0.2.7"/>
    	  <dependency id="clojure.data.generators" version="0.1.0"/>
    	</dependencies>
      </metadata>
    </package>

Put in dependencies as required.

# Method 2: Step 3:: Pack and Publish

* Build the project
* From the command line, in `<root>/src/clojure`, execute:

    nuget pack clr.tools.reader.csproj -Prop Platform=AnyCPU
	
The -Prop is needed only because we built on the 3.5 runtime.  It's a nuget bug, perhaps fixed in a later release.

This will have created a `clr.tools.reader.1.0.0.0.nupkg` file (whatever the version is).  Given that it is a ZIP, you can inspect it with your favorite ZIP tool.

To publish:

    nuget push clr.tools.reader.1.0.0.0.nupkg`

Go to https://nuget.org to verify.

	
