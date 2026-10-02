// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.AspectWeavers;
using Metalama.Framework.Engine.CodeModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Metalama.Community.Costura.Weaver;

[MetalamaPlugIn]
[UsedImplicitly]
internal class CosturaWeaver : IAspectWeaver
{
    public Task TransformAsync( AspectWeaverContext context )
    {
        var compilation = (CSharpCompilation) context.Compilation.Compilation;

        // Check the language version.
        if ( compilation.LanguageVersion < LanguageVersion.CSharp9 )
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "DE001",
                        "Language version too low",
                        "Metalama.Community.Costura requires language version at least 9.0, but it's set to {0}.",
                        "Metalama.Community.Costura",
                        DiagnosticSeverity.Error,
                        true ),
                    null,
                    compilation.LanguageVersion.ToDisplayString() ) );

            return Task.CompletedTask;
        }

        var options = context.GetOptions<CosturaOptions>( compilation.Assembly );

        const string excludedPath = @"Reference Assemblies\Microsoft\Framework\.NETFramework";

        var paths = compilation.References.Select(
                r => r switch
                {
                    PortableExecutableReference peReference => peReference.FilePath,
                    _ => throw new NotSupportedException()
                } )
            .Where( path => path != null && !path.Contains( excludedPath ) )
            .ToArray();

        var parseOptions = context.Compilation.GetParseOptions();

        // Embed resources.
        var checksums = new Checksums();
        var resourceEmbedder = new ResourceEmbedder();
        resourceEmbedder.EmbedResources( options, paths!, checksums );
        var unmanagedFromEmbedder = resourceEmbedder.HasUnmanaged;

        // Load references.
        var assemblyLoaderInfo =
            AssemblyLoaderInfo.LoadAssemblyLoader( options.CreateTemporaryAssemblies.GetValueOrDefault(), unmanagedFromEmbedder );

        // Generate code.
        var resourcesHash = ResourceHash.CalculateHash( resourceEmbedder.Resources );

        var moduleInitializerCode =
            Resources.ModuleInitializer.Replace( "TEMPLATE", assemblyLoaderInfo.SourceTypeName );

        var sourceTypeSyntax =
            new ResourceNameFinder( assemblyLoaderInfo, resourceEmbedder.Resources.Select( r => r.Name ) )
                .FillInStaticConstructor(
                    options.CreateTemporaryAssemblies.GetValueOrDefault(),
                    options.PreloadedLibraries.OrderBy( x => x.Priority ).ThenBy( x => x.Name ).Select( x => x.Name ),
                    resourcesHash,
                    checksums );

        // Add syntax trees.
        context.Compilation = context.Compilation.AddSyntaxTrees(
                SyntaxFactory.ParseSyntaxTree(
                    moduleInitializerCode,
                    parseOptions,
                    "__Costura.ModuleInitializer.cs",
                    Encoding.UTF8 ),
                SyntaxFactory.ParseSyntaxTree( Resources.Common, parseOptions, "__Costura.Common.cs", Encoding.UTF8 ),
                SyntaxFactory.SyntaxTree(
                    sourceTypeSyntax,
                    parseOptions,
                    $"__Costura.{assemblyLoaderInfo.SourceTypeName}.cs",
                    Encoding.UTF8 ) )
            .WithAdditionalResources( resourceEmbedder.Resources.ToArray() );

        return Task.CompletedTask;
    }
}