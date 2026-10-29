// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Metalama.Community.Costura.Tests;

/// <summary>
/// Verifies that the runtime templates injected into the user's compilation are valid C# and that they call no API
/// that is unsafe inside an assembly-resolve handler.
/// </summary>
/// <remarks>
/// These templates ship as string constants, so a syntax error in one of them does not break our own build -
/// it breaks the build of whoever consumes the package, and only on the code path that selects that template.
/// Issue #101 was exactly that: four call sites in <c>TemplateWithUnmanagedHandler</c> had been corrupted into
/// <c>private init;.ReadExistingAssembly(...)</c>, and no test ever caused that template to be parsed.
/// </remarks>
public sealed class RuntimeTemplateTests
{
    /// <summary>
    /// Gets the name of every runtime template.
    /// </summary>
    public static TheoryData<string> TemplateNames
    {
        get
        {
            var data = new TheoryData<string>();

            foreach ( var templateName in RuntimeTemplates.Names )
            {
                data.Add( templateName );
            }

            return data;
        }
    }

    [Theory]
    [MemberData( nameof(TemplateNames) )]
    public void TemplateIsValidCSharp( string templateName )
    {
        var source = RuntimeTemplates.GetSource( templateName );

        Assert.False( string.IsNullOrWhiteSpace( source ), $"Template '{templateName}' is empty." );

        var syntaxTree = CSharpSyntaxTree.ParseText( source );

        var errors = syntaxTree.GetDiagnostics()
            .Where( d => d.Severity == DiagnosticSeverity.Error )
            .ToList();

        Assert.True(
            errors.Count == 0,
            $"Template '{templateName}' is not valid C#:\n" +
            string.Join(
                "\n",
                errors.Select( e => $"  {e.Location.GetLineSpan()}: {e.Id} {e.GetMessage( CultureInfo.InvariantCulture )}" ) ) );
    }

    /// <summary>
    /// Verifies that no runtime template calls <see cref="Assembly.GetName()" />.
    /// </summary>
    /// <remarks>
    /// On .NET Framework, <c>Assembly.GetName()</c> issues a <c>FileIOPermission</c> path-discovery demand on the
    /// assembly code base. The templates run inside the <c>AppDomain.AssemblyResolve</c> handler. When the security
    /// policy makes that demand non-trivial, the security engine needs the localized text of the
    /// <c>Security_Generic</c> resource key, and loading the satellite resource assembly that holds it raises
    /// <c>AssemblyResolve</c> again. The handler then recurses until the stack overflows, which is issue #113.
    /// <c>Assembly.FullName</c> exposes the same simple name, version and culture from a cached string and issues
    /// no demand, so it is the safe way to identify an assembly that is already loaded.
    /// </remarks>
    [Theory]
    [MemberData( nameof(TemplateNames) )]
    public void TemplateDoesNotCallAssemblyGetName( string templateName )
    {
        var root = CSharpSyntaxTree.ParseText( RuntimeTemplates.GetSource( templateName ) ).GetRoot();

        var callSites = root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where( i => i.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "GetName" } )
            .ToList();

        Assert.True(
            callSites.Count == 0,
            $"Template '{templateName}' calls GetName() on an assembly. This issues a path-discovery security demand, "
            + "which can re-enter the AssemblyResolve handler and recurse until the stack overflows (issue #113). "
            + "Use Assembly.FullName instead. Call sites:\n"
            + string.Join( "\n", callSites.Select( c => $"  {c.GetLocation().GetLineSpan()}: {c}" ) ) );
    }

    [Fact]
    public void AllTemplatesAreCovered()
    {
        // Guards against the template set silently shrinking, which would make the theories above vacuously green.
        var names = RuntimeTemplates.Names.ToList();

        Assert.Contains( "Common", names );
        Assert.Contains( "Template", names );
        Assert.Contains( "TemplateWithTempAssembly", names );
        Assert.Contains( "TemplateWithUnmanagedHandler", names );
        Assert.Contains( "ModuleInitializer", names );
    }
}
