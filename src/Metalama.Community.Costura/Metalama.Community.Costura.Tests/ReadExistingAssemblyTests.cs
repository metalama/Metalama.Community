// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Metalama.Community.Costura.Tests;

/// <summary>
/// Verifies the behavior of <c>DependencyExtractor.ReadExistingAssembly</c>, the method the
/// <c>AppDomain.AssemblyResolve</c> handler calls first. The method lives in the <c>Common</c> runtime template, so
/// these tests compile that template into an in-memory assembly and invoke the method by reflection.
/// </summary>
/// <remarks>
/// The method used to identify the loaded assemblies with <c>Assembly.GetName()</c>, which recursed inside the resolve
/// handler on .NET Framework (issue #113). It now parses <c>Assembly.FullName</c> instead, and these tests verify that
/// the matching rules did not change: an assembly is matched on its simple name and its culture.
/// </remarks>
public sealed class ReadExistingAssemblyTests
{
    private static readonly Lazy<MethodInfo> _readExistingAssembly = new( CompileCommonTemplate );

    private static Assembly? ReadExistingAssembly( AssemblyName name )
        => (Assembly?) _readExistingAssembly.Value.Invoke( null, new object[] { name } );

    [Fact]
    public void LoadedAssemblyIsFoundByName()
    {
        var expected = typeof(ReadExistingAssemblyTests).Assembly;

        var actual = ReadExistingAssembly( new AssemblyName( expected.GetName().Name! ) );

        Assert.Same( expected, actual );
    }

    [Fact]
    public void LoadedAssemblyIsFoundByFullName()
    {
        var expected = typeof(ReadExistingAssemblyTests).Assembly;

        var actual = ReadExistingAssembly( new AssemblyName( expected.FullName! ) );

        Assert.Same( expected, actual );
    }

    [Fact]
    public void AssemblyThatIsNotLoadedIsNotFound()
        => Assert.Null( ReadExistingAssembly( new AssemblyName( "Metalama.Community.Costura.NotLoaded" ) ) );

    [Fact]
    public void SatelliteAssemblyOfALoadedAssemblyIsNotFound()
    {
        // The culture of the request must be honored: a request for the French resources of a loaded assembly must not
        // be satisfied by the culture-neutral assembly itself.
        var name = new AssemblyName( typeof(ReadExistingAssemblyTests).Assembly.GetName().Name! )
        {
            CultureInfo = CultureInfo.GetCultureInfo( "fr-FR" )
        };

        Assert.Null( ReadExistingAssembly( name ) );
    }

    private static MethodInfo CompileCommonTemplate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText( RuntimeTemplates.GetSource( "Common" ) );

        // The template targets the framework of the weaved application, so it is compiled against the reference
        // assemblies of the runtime that hosts the test.
        var references = ((string) AppContext.GetData( "TRUSTED_PLATFORM_ASSEMBLIES" )!)
            .Split( Path.PathSeparator )
            .Select( path => MetadataReference.CreateFromFile( path ) );

        var compilation = CSharpCompilation.Create(
            "Metalama.Community.Costura.Tests.CommonTemplate",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary ) );

        using var peStream = new MemoryStream();
        var result = compilation.Emit( peStream );

        var errors = result.Diagnostics.Where( d => d.Severity == DiagnosticSeverity.Error ).ToList();

        Assert.True(
            errors.Count == 0,
            "The 'Common' template does not compile:\n"
            + string.Join(
                "\n",
                errors.Select( e => $"  {e.Location.GetLineSpan()}: {e.Id} {e.GetMessage( CultureInfo.InvariantCulture )}" ) ) );

        var type = Assembly.Load( peStream.ToArray() )
            .GetType( "Metalama.Community.Costura.RunTime.DependencyExtractor" );

        Assert.NotNull( type );

        var method = type!.GetMethod( "ReadExistingAssembly", BindingFlags.Public | BindingFlags.Static );

        Assert.NotNull( method );

        return method!;
    }
}
