# Creating NuGet packaging for ClojureCLR libs

At present, we've been using lein-clr for managing our libraries.   We use the standard project layout.  Source code is in `<root>/src`.  If the project namespace is something like `clojure.tools.reader`, source code will be under `<root>/src/clojure/tools/reader`.

## Create the C# project and solution.

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

## Create the Nuget package.

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

# Pack and Publish

* Build the project
* From the command line, in `<root>/src/clojure`, execute:

    nuget pack clr.tools.reader.csproj -Prop Platform=AnyCPU
	
The -Prop is needed only because we built on the 3.5 runtime.  It's a nuget bug, perhaps fixed in a later release.

This will have created a `clr.tools.reader.1.0.0.0.nupkg` file (whatever the version is).  Given that it is a ZIP, you can inspect it with your favorite ZIP tool.

To publish:

    nuget push clr.tools.reader.1.0.0.0.nupkg`

Go to https://nuget.org to verify.

	
