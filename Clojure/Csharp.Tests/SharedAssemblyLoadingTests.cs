using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using clojure.lang;

namespace Csharp.Tests
{
    [TestFixture]
    public class SharedAssemblyLoadingTests
    {
        [OneTimeSetUp]
        public void Setup()
        {
            RT.Init();
        }

        [Test]
        public void TestTrustedPlatformAssembliesAreAvailable()
        {
            // This test verifies that TRUSTED_PLATFORM_ASSEMBLIES are accessible
            var trustedAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
            
            Assert.That(trustedAssemblies, Is.Not.Null.And.Not.Empty,
                "TRUSTED_PLATFORM_ASSEMBLIES should be available in .NET Core/5+");
            
            // Verify we can see shared runtime assemblies
            var paths = trustedAssemblies.Split(System.IO.Path.PathSeparator);
            Assert.That(paths.Length, Is.GreaterThan(0), "Should have multiple trusted assemblies");
            
        }

        [Test]
        public void TestCanLoadSharedRuntimeTypes()
        {
            // Test loading types from shared runtime libraries that are in separate assemblies
            // These types are commonly used but live in different assemblies in .NET Core/5+
            var testCases = new[]
            {
                new { TypeName = "System.Collections.Concurrent.ConcurrentDictionary`2", ExpectedAssembly = "System.Collections.Concurrent" },
                new { TypeName = "System.Text.Json.JsonSerializer", ExpectedAssembly = "System.Text.Json" },
                new { TypeName = "System.Net.Http.HttpClient", ExpectedAssembly = "System.Net.Http" }
            };

            foreach (var testCase in testCases)
            {
                var type = RT.classForName(testCase.TypeName);
                
                Assert.That(type, Is.Not.Null, 
                    $"Failed to load type: {testCase.TypeName}");
                
                // Verify it's from the expected assembly
                Assert.That(type.Assembly.GetName().Name, 
                    Is.EqualTo(testCase.ExpectedAssembly),
                    $"Type {testCase.TypeName} should be from assembly {testCase.ExpectedAssembly}");
                
            }
        }

        [Test]
        public void TestCanLoadSystemCollectionsConcurrentTypes()
        {
            // Specifically test System.Collections.Concurrent which is a common issue
            // These types moved to a separate assembly in .NET Core
            var concurrentTypes = new[]
            {
                "System.Collections.Concurrent.ConcurrentDictionary`2",
                "System.Collections.Concurrent.ConcurrentQueue`1",
                "System.Collections.Concurrent.BlockingCollection`1"
            };

            foreach (var typeName in concurrentTypes)
            {
                var type = RT.classForName(typeName);
                
                Assert.That(type, Is.Not.Null, 
                    $"Failed to load concurrent collection type: {typeName}");
                
            }
        }

        [Test]
        public void TestDuplicateTypeNamesHandledCorrectly()
        {
            // Test that duplicate type names (like "Casing" which appears in multiple assemblies)
            // are handled without throwing exceptions
            
            // This should not throw even though "Casing" exists in both 
            // System.Text.Json and System.Private.CoreLib
            Assert.DoesNotThrow(() => 
            {
                RT.Init(); // Re-init to ensure the duplicate handling works
            }, "RT.Init should handle duplicate type names without throwing");
        }

        [Test]
        public void TestCanLoadNuGetPackageTypes()
        {
            // Test that we can load types from NuGet packages (via DependencyContext)
            // Using types from Microsoft.Extensions.DependencyModel which we reference
            var type = RT.classForName("Microsoft.Extensions.DependencyModel.DependencyContext");
            
            Assert.That(type, Is.Not.Null, 
                "Should be able to load types from NuGet packages");
            
        }
    }
}