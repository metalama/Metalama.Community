// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Metalama.Community.Costura.Weaver;

[CompileTime]
internal sealed class AssemblyLoaderInfo
{
    public const string AssemblyNamesField = "assemblyNames";

    public const string SymbolNamesField = "symbolNames";

    public const string PreloadListField = "preloadList";

    public const string Preload32ListField = "preload32List";

    public const string Preload64ListField = "preload64List";

    public string? ChecksumsField { get; }

    public string? Md5HashField { get; }

    public CompilationUnitSyntax SourceTypeSyntax { get; }

    public string SourceTypeName { get; }

    private AssemblyLoaderInfo(
        string? checksumsField,
        string? md5HashField,
        CompilationUnitSyntax sourceTypeSyntax,
        string sourceTypeName )
    {
        this.ChecksumsField = checksumsField;
        this.Md5HashField = md5HashField;
        this.SourceTypeSyntax = sourceTypeSyntax;
        this.SourceTypeName = sourceTypeName;
    }

    public static AssemblyLoaderInfo LoadAssemblyLoader(
        bool createTemporaryAssemblies,
        bool hasUnmanaged )
    {
        const string sourceTypeName = "DependencyExtractor";

        string sourceTypeCode;

        if ( createTemporaryAssemblies )
        {
            sourceTypeCode = Resources.TemplateWithTempAssembly;
        }
        else if ( hasUnmanaged )
        {
            sourceTypeCode = Resources.TemplateWithUnmanagedHandler;
        }
        else
        {
            sourceTypeCode = Resources.Template;
        }

        var sourceTypeSyntax = SyntaxFactory.ParseCompilationUnit( sourceTypeCode );

        return new AssemblyLoaderInfo(
            Optional( "checksums" ),
            Optional( "md5Hash" ),
            sourceTypeSyntax,
            sourceTypeName );

        string? Optional( string field )
        {
            return sourceTypeSyntax.DescendantNodes()
                .OfType<FieldDeclarationSyntax>()
                .SelectMany( f => f.Declaration.Variables )
                .Any( f => f.Identifier.ValueText == field )
                ? field
                : null;
        }
    }
}