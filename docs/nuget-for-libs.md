# Creating NuGet packaging for ClojureCLR libs

At present, we've been using lein-clr for managing our libraries.   We use the standard project layout.  Source code is in `<root>/src`.  If the project namespace is something like `clojure.tools.namespace`, source code will be under `<root>/src/clojure/tools/namespace`.

(Though lately, I've been playing matching exactly the source code layout in the parent JVM libs, setting the :source-paths and :test-paths properties in project.clj to match.)


## Step 1: Create the C# project

From the command line, change directory to `<root>\src\clojure`, and then execute

	dotnet new classlib clojure.tools.namespace -o .
	
## Step 2: Edit the project
	
The following tasks can be done either with a text editor or by going into Visual Studio (or maybe VS Code) and taking the appropriate actions.
	
In the clojure.tools.namespace.csproj file:

* Change `<TargetFramework>net5.0</TargetFramework>` to `<TargetFrameworks>netstandard2.0;netstandard2.1</TargetFrameworks>`
* Remove the reference to Class1.cs.  Also you can delete this file.  (Accomplish both by deleting from the Server Explorer in VS.)
* Add attributes to identify the project:

	<PropertyGroup>
		<PackageId>clojure.tools.namespace</PackageId>
		<Title>clojure.tools.namespace</Title>
		<Product>clojure.tools.namespace</Product>
		<AssemblyTitle>clojure.tools.namespace</AssemblyTitle>
		<Authors>YOU!!!</Authors>
		<Description>Something appropriate.</Description>
		<Copyright>Copyright © Rich Hickey, You 202X</Copyright>
		<PackageLicenseExpression>EPL-1.0</PackageLicenseExpression>
		<RepositoryUrl>https://github.com/clojure/clojure.tools.namesapce</RepositoryUrl>
		<Company>ClojureCLR contributors</Company>
		<PackageTags>Clojure;ClojureCLR</PackageTags>
		<Version>1.1.0</Version> 
	</PropertyGroup>

	
Note: if you want to create a prerelease version (alphaN, betaN), include something like:

	    <Version>1.1.0-beta1</Version> 
	

* Add all the source .clj files to the project.  If you are working in VS, you should see all the files in the <root>/src/clojure directory and below.  Remove any files you do not want in the solution.  For the rest, set properties:

    Build Action: Embedded Resource
	Copy to Output Directory: Do not copy

* Or you can go in by hand and put into your .csproj file lines of the form

	<ItemGroup>
		<EmbeddedResource Include="tools\namespace\dependency.cljc" />
		<EmbeddedResource Include="tools\namespace\dir.clj" />
		...
	</ItemGroup>

If other libraries are required, use the nuget tool in VS to add those dependencies to the project, or hand-edit the .csproj file to add 

	<ItemGroup>
		<PackageReference Include="clr.tools.reader" Version="1.3.4" />
	</ItemGroup>
	
		
* Build the project.


## Step 3:: Pack and Publish

    dotnet pack 
	
This will create a file such `clojure.tools.namespace.1.0.0.nupkg`.  It is zip format, so you can inspect it with your favorite Zip tool.  Or use a tool such as Nuget Package Explorer that will also parse out the metadata.

To publish:

    dotnet nuget push clr.tools.namespace.1.0.0.nupkg -source nuget.org

Go to https://nuget.org to verify.

If you are using lein/clojars, don't forget to

* edit the project.clj file to update the version and any library dependencies.
* do `lein deploy clojars`
* You might need to updated version info on your readme.






	
