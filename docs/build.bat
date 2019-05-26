
dotnet build Clojure
dotnet build Clojure.Main
dotnet build Clojure.Tests
dotnet publish Clojure.Main
dotnet publish Clojure.Tests
xcopy Clojure.Main\bin\Debug\netcoreapp2.1\publish testing /E /Y
xcopy Clojure.Tests\bin\Debug\netstandard2.0\publish testing /E /Y
cd testing
dotnet Clojure.Main.dll

