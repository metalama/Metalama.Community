// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Community.Costura.Weaver;

[CompileTime]
internal sealed class ResourceNameFinder
{
    private readonly AssemblyLoaderInfo _info;
    private readonly IEnumerable<string> _resourceNames;

    public ResourceNameFinder( AssemblyLoaderInfo info, IEnumerable<string> resourceNames )
    {
        this._info = info;
        this._resourceNames = resourceNames;
    }

    public CompilationUnitSyntax FillInStaticConstructor(
        bool createTemporaryAssemblies,
        IEnumerable<string> preloadedLibraries,
        string resourcesHash,
        Checksums checksums )
    {
        var statements = new List<StatementSyntax>();

        var orderedResources = preloadedLibraries
            .Join(
                this._resourceNames,
                p => p.ToLowerInvariant(),
                r =>
                {
                    var parts = r.Split( '.' );
                    GetNameAndExt( parts, out var name, out _ );

                    return name;
                },
                ( _, r ) => r )
            .Union( this._resourceNames.OrderBy( r => r ) );

        foreach ( var resource in orderedResources )
        {
            var parts = resource.Split( '.' );

            GetNameAndExt( parts, out var name, out var ext );

            if ( string.Equals( parts[0], "Costura", StringComparison.OrdinalIgnoreCase ) )
            {
                if ( createTemporaryAssemblies )
                {
                    AddToList( statements, AssemblyLoaderInfo.PreloadListField, resource );
                }
                else
                {
                    var isPdb = string.Equals( ext, "pdb", StringComparison.OrdinalIgnoreCase );

                    AddToDictionary( statements, isPdb ? AssemblyLoaderInfo.SymbolNamesField : AssemblyLoaderInfo.AssemblyNamesField, name, resource );
                }
            }
            else if ( string.Equals( parts[0], "Costura32", StringComparison.OrdinalIgnoreCase ) )
            {
                AddToList( statements, AssemblyLoaderInfo.Preload32ListField, resource );
            }
            else if ( string.Equals( parts[0], "Costura64", StringComparison.OrdinalIgnoreCase ) )
            {
                AddToList( statements, AssemblyLoaderInfo.Preload64ListField, resource );
            }
        }

        if ( this._info.ChecksumsField != null )
        {
            foreach ( var checksum in checksums.AllChecksums )
            {
                AddToDictionary( statements, this._info.ChecksumsField, checksum.Key, checksum.Value );
            }
        }

        if ( this._info.Md5HashField != null )
        {
            statements.Add(
                ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName( this._info.Md5HashField ),
                        LiteralExpression( SyntaxKind.StringLiteralExpression, Literal( resourcesHash ) ) ) ) );
        }

        var staticConstructor = ConstructorDeclaration( this._info.SourceTypeName )
            .AddModifiers( Token( SyntaxKind.StaticKeyword ) )
            .WithBody( Block( statements ) );

        return this._info.SourceTypeSyntax.InsertNodesAfter(
            this._info.SourceTypeSyntax.DescendantNodes().OfType<ClassDeclarationSyntax>().Single().Members.Last(),
            [staticConstructor] );
    }

    private static void GetNameAndExt( string[] parts, out string name, out string ext )
    {
        var isCompressed = string.Equals( parts[^1], "compressed", StringComparison.OrdinalIgnoreCase );

        ext = parts[^(isCompressed ? 2 : 1)];

        name = string.Join( ".", parts.Skip( 1 ).Take( parts.Length - (isCompressed ? 3 : 2) ) );
    }

    private static void AddToDictionary( List<StatementSyntax> statements, string field, string key, string name )
    {
        statements.Add(
            ExpressionStatement(
                InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName( field ),
                            IdentifierName( "Add" ) ) )
                    .AddArgumentListArguments(
                        Argument( LiteralExpression( SyntaxKind.StringLiteralExpression, Literal( key ) ) ),
                        Argument( LiteralExpression( SyntaxKind.StringLiteralExpression, Literal( name ) ) ) ) ) );
    }

    private static void AddToList( List<StatementSyntax> statements, string field, string name )
    {
        statements.Add(
            ExpressionStatement(
                InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName( field ),
                            IdentifierName( "Add" ) ) )
                    .AddArgumentListArguments( Argument( LiteralExpression( SyntaxKind.StringLiteralExpression, Literal( name ) ) ) ) ) );
    }
}