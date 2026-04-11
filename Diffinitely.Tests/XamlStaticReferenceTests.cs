using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Diffinitely.Tests;

public class XamlStaticReferenceTests
{
    [Fact]
    public void PRReviewRemoteUserControl_XamlStaticReferences_AllResolveAtRuntime()
    {
        // 1. Locate the XAML file
        var repoRoot = FindRepoRoot();
        var xamlPath = Path.Combine(repoRoot, "Diffinitely", "ToolWindows", "PRReviewRemoteUserControl.xaml");
        Assert.True(File.Exists(xamlPath), $"XAML file not found at {xamlPath}");

        // 2. Load XAML content
        var xamlContent = File.ReadAllText(xamlPath);
        var xamlDoc = XDocument.Parse(xamlContent);

        // 3. Extract x:Static references to VsBrushes and VsResourceKeys
        // Pattern: {x:Static styles:VsBrushes.SomeKey} or {x:Static styles:VsResourceKeys.SomeKey}
        var staticRefPattern = new Regex(@"\{x:Static\s+styles:(VsBrushes|VsResourceKeys)\.(\w+)\}");
        var matches = staticRefPattern.Matches(xamlContent);

        var references = matches
            .Cast<Match>()
            .Select(m => new { TypeName = m.Groups[1].Value, FieldName = m.Groups[2].Value })
            .Distinct()
            .ToList();

        Assert.NotEmpty(references); // Ensure we found some references to validate

        // 4. Load the VS Shell assembly and reflect on the types
        var shellAssembly = LoadVsShellAssembly();
        var failures = new List<string>();

        foreach (var reference in references)
        {
            var fullTypeName = $"Microsoft.VisualStudio.Shell.{reference.TypeName}";
            var type = shellAssembly.GetType(fullTypeName);
            
            if (type == null)
            {
                failures.Add($"Type '{fullTypeName}' not found in assembly");
                continue;
            }

            var field = type.GetField(reference.FieldName, BindingFlags.Public | BindingFlags.Static);
            var property = type.GetProperty(reference.FieldName, BindingFlags.Public | BindingFlags.Static);

            if (field == null && property == null)
            {
                failures.Add($"'{reference.TypeName}.{reference.FieldName}' does not exist as a static field or property at runtime");
            }
        }

        // 5. Assert all references are valid
        Assert.True(failures.Count == 0, 
            $"Found {failures.Count} invalid XAML static reference(s):\n" + string.Join("\n", failures));
    }

    private static string FindRepoRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory, "Diffinitely.slnx")))
            {
                return directory;
            }
            directory = Directory.GetParent(directory)?.FullName;
        }
        throw new InvalidOperationException("Could not find repository root (no Diffinitely.slnx found)");
    }

    private static Assembly LoadVsShellAssembly()
    {
        // The Microsoft.VisualStudio.Shell.15.0 assembly is referenced by the main project
        // Look for it in well-known NuGet package locations
        var nugetPackagesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nuget", "packages", "microsoft.visualstudio.extensibility.framework");

        if (!Directory.Exists(nugetPackagesPath))
        {
            throw new InvalidOperationException($"Could not find VS Extensibility Framework package at {nugetPackagesPath}");
        }

        // Find the latest version
        var versionDirs = Directory.GetDirectories(nugetPackagesPath)
            .OrderByDescending(d => d)
            .ToList();

        foreach (var versionDir in versionDirs)
        {
            var dllPath = Path.Combine(versionDir, "lib", "net472", "design", "Microsoft.VisualStudio.Shell.15.0.dll");
            if (File.Exists(dllPath))
            {
                return Assembly.LoadFrom(dllPath);
            }
        }

        throw new FileNotFoundException("Could not find Microsoft.VisualStudio.Shell.15.0.dll in NuGet packages");
    }
}
