using clojure.lang;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Csharp.Tests;

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

    [Test]
    public void TestNamespaceWalkUpResolvesType()
    {
        // System.Text.Json.Serialization.JsonConverter lives in assembly System.Text.Json,
        // not System.Text.Json.Serialization. The namespace walk-up should find it by
        // stripping "Serialization" and trying "System.Text.Json".
        var type = RT.classForName("System.Text.Json.Serialization.JsonConverter");

        Assert.That(type, Is.Not.Null,
            "Should resolve type via namespace hierarchy walk-up when assembly name != namespace");
        Assert.That(type.Assembly.GetName().Name, Is.EqualTo("System.Text.Json"));
    }

    [Test]
    public void TestRuntimeAssemblyNamesHavePaths()
    {
        // Access _runtimeAssemblyNames via reflection to verify DependencyContext
        // entries have been resolved to actual file paths where possible.
        var field = typeof(RT).GetField("_runtimeAssemblyNames",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(field, Is.Not.Null, "_runtimeAssemblyNames field should exist");

        var lazy = field.GetValue(null);
        var dict = (Dictionary<AssemblyName, string>)lazy.GetType().GetProperty("Value").GetValue(lazy);

        // TPA entries should always have non-empty paths
        int withPaths = 0;
        foreach (var kvp in dict)
        {
            if (!string.IsNullOrWhiteSpace(kvp.Value))
                withPaths++;
        }

        Assert.That(withPaths, Is.GreaterThan(0),
            "At least some runtime assembly entries should have resolved file paths");
    }

    [Test]
    public void TestClassForNameCountersIncrement()
    {
        // Reset counters
        int failsBefore = RT.NumFails;

        // Load a type that requires runtime assembly resolution
        var type = RT.classForName("System.Text.Json.JsonSerializer");
        Assert.That(type, Is.Not.Null);

        // The type should have been found (via loaded assemblies or runtime resolution)
        // We just verify no new failures were recorded for this lookup
        Assert.That(RT.NumFails, Is.EqualTo(failsBefore),
            "Loading a valid type should not increment NumFails");
    }

    [Test]
    public void TestClassForNameReturnsNullForBogusType()
    {
        int failsBefore = RT.NumFails;

        var type = RT.classForName("This.Type.Does.Not.Exist.Anywhere");

        Assert.That(type, Is.Null, "Bogus type name should return null");
        Assert.That(RT.NumFails, Is.GreaterThan(failsBefore),
            "Failed lookup should increment NumFails");
    }
}