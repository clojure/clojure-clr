# Creating NuGet packaging for ClojureCLR libs

We suggest what seems to be the standard project layout in Clojure libraries.  Source code is in `<root>\src\main`.  Clojure source is in `<root>\src\main\clojure`. 
 If the project namespace is something like `clojure.tools.namespace`, source code will be under `<root>\src\main\clojure\clojure\tools\namespace`. 

 We will put our .Net-related source in `<root>\src\main\dotnet`.

(You might run into older libraries that were developed before we standardized on this approach.  They might have the clojure source directly under `<root>/src/clojure` and the .Net-related code under there also.}

The sample code below uses `clojure.tools.namespace` as the name of the library.  You should substitute appropriately.

## Step 0: Understanding the goal

We want to distribute our Clojure source code via Nuget.  We need a DLL containing the `.clj` source files as embedded resources.  Their names should match the clojure namespace designation with `.clj`, `.cljc`, or other variant appended.  However, in keeping with `load` searching, hyphens shoudl be replaced with underscores.

Thus, for our example `clojure.tools.namespace`, we need embedded resources with names

```
clojure.tools.namespace.clj
clojure.tools.namespace.dependency.clj
...
```

The rest of this document outlines the steps to take to create the desired DLL and upload it to Nuget.

## Step 1: Create the C# project

From the command line, create and change directory to `<root>\src\main\dotnet\packager`. Execute

```
	dotnet new classlib -n clojure.tools.namespace -f netstandard2.1 -o . 
```
The actual name of the library is irrelevant.  We will override the only aspect of relevance, the default namespace, below.

If you prefer, Visual Studio or VS Code could be used to create this project.  In this case you will also get a `.sln` file and your `.csproj` might be a directory lower (unless you put solution and project in the same directory), so you will have to adjust the file paths below.

## Step 2: Edit the project
	
The following tasks can be done either with a text editor or by going into Visual Studio or maybe VS Code and taking the appropriate actions.

* Delete `Class1.cs`. (In modern project files, likely there is no reference to it in the '.csproj' file, so deletion suffices.)
	
In the `.csproj` file:

* Change 

```
<TargetFramework>netstandard2.1</TargetFramework>
```
 
 to 
 
 ```
 <TargetFrameworks>netstandard2.0;netstandard2.1</TargetFrameworks>
 ```

 * You might have to remove `<Nullable>enable</Nullable>`.

* Add attributes to identify the project:

```
	<PropertyGroup>
		<PackageId>clojure.tools.namespace</PackageId>
		<RootNamespace>clojure.tools</RootNamespace>		
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
```
	
Note: if you want to create a prerelease version (alphaN, betaN), you can set the version accordingly:

```
<Version>1.1.0-beta1</Version> 
```

Note: Pay attention to the `RootNamespace` element.  In this example, we give it the value `clojure.tools` and not `clojure.tools.namespace`.  The reason is that the root namespace is prepended to the resource names, followed by the directory path with periods instead of directory separators.  The default would be fine for source files in subdirectories, but Clojure source often has a layout like this?

```
...\clojure\tools\namespace.clj
...\clojure\tools\namespace\dependency.clj
...
```

and that first file's name as an embedded resource might be

```
clojure.tools.namespace.namespace.clj
```

and the ClojureCLR loader will not find it. Setting the root namespace to `clojure.tools` will yield approriately named resources:


```
clojure.tools.namespace.clj
clojure.tools.namespace.dependency.clj
...
```


* Add all the source .clj files to the project as embedded resources.   Do not try to do this using `Add>Existing Item...` in Visual Studio; VS will copy the files into this project directory.  You do not want that. You should manually edit the `.csproj` file and add the following:  

```
	<ItemGroup>
		<EmbeddedResource CopyToOutputDirectory="Never" Include ="..\..\clojure\clojure\tools\**\*"/>
	</ItemGroup>
```

The number of `..\`s required will depend on the relative placement of your project to the Clojure source code.  If you are working in Visual Studio, it is easy to tell when you have it right:  a list of the files will appear in the Solution Explorer.

* If other libraries are required, use the NuGet package manager tool in VS to add those dependencies to the project, or hand-edit the .csproj file to add something like

	<ItemGroup>
		<PackageReference Include="clr.tools.reader" Version="1.3.4" />
	</ItemGroup>
	
		
* Build the project.


## Step 3:: Pack and Publish

Create the package by executing

```
    dotnet pack -p Configuration=Release
```	

This will create a file such as `clojure.tools.namespace.1.1.0.nupkg`.  It is zip format, so you can inspect it with your favorite Zip tool.  Or use a tool such as Nuget Package Explorer that will also parse out the metadata.

To publish, change directory to the `bin\Release` subdirectory and execute

```
dotnet nuget push clojure.tools.namespace.1.1.0.nupkg -s nuget.org
```

Go to https://nuget.org to verify.

If you are using lein/clojars, don't forget to

* Edit the `project.clj` file to update the version and any library dependencies.
* Execute  `lein deploy clojars` 







	
