// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Community.AutoCancellationToken
{
    internal partial class AutoCancellationTokenWeaver
    {
        [CompileTime]
        private sealed class AddCancellationTokenParameterRewriter : RewriterBase
        {
            private readonly Compilation _compilation;
            private readonly SyntaxAnnotation _generatedCodeAnnotation;

            public AddCancellationTokenParameterRewriter(
                Compilation compilation,
                SyntaxAnnotation generatedCodeAnnotation )
            {
                this._compilation = compilation;
                this._generatedCodeAnnotation = generatedCodeAnnotation;
            }

            /// <summary>
            /// Kind of the annotation that marks a method to be split into a forwarder and an overload. Its data is
            /// the name chosen for the <see cref="System.Threading.CancellationToken"/> parameter.
            /// </summary>
            private const string _expandAnnotationKind = "Metalama.Community.AutoCancellationToken.Expand";

            protected override T VisitTypeDeclaration<T>( T node, Func<T, SyntaxNode?> baseVisit )
            {
                var visited = this.VisitTypeDeclarationCore( node, baseVisit );

                // A method cannot replace itself with two members, so VisitMethodDeclaration only marks the methods
                // to expand and the expansion happens here, where the member list is available.
                return (T) this.ExpandMarkedMethods( visited );
            }

            private TypeDeclarationSyntax ExpandMarkedMethods( TypeDeclarationSyntax type )
            {
                if ( !type.Members.Any( m => m.GetAnnotations( _expandAnnotationKind ).Any() ) )
                {
                    return type;
                }

                var members = new List<MemberDeclarationSyntax>( type.Members.Count + 1 );

                foreach ( var member in type.Members )
                {
                    if ( member is MethodDeclarationSyntax method &&
                         method.GetAnnotations( _expandAnnotationKind ).FirstOrDefault() is { } annotation )
                    {
                        var parameterName = annotation.Data!;
                        var original = method.WithoutAnnotations( _expandAnnotationKind );

                        members.Add( CreateForwarder( original, this._generatedCodeAnnotation ) );
                        members.Add( this.CreateOverload( original, parameterName ) );
                    }
                    else
                    {
                        members.Add( member );
                    }
                }

                return type.WithMembers( SyntaxFactory.List( members ) );
            }

            /// <summary>
            /// Builds the method that keeps the original signature and simply calls the new overload. Keeping it means
            /// existing callers, overrides and interface implementations are unaffected (#81).
            /// </summary>
            private static MethodDeclarationSyntax CreateForwarder(
                MethodDeclarationSyntax method,
                SyntaxAnnotation generatedCodeAnnotation )
            {
                SimpleNameSyntax name = method.TypeParameterList is { Parameters.Count: > 0 } typeParameters
                    ? SyntaxFactory.GenericName(
                        method.Identifier,
                        SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SeparatedList<TypeSyntax>(
                                typeParameters.Parameters.Select( p => SyntaxFactory.IdentifierName( p.Identifier ) ) ) ) )
                    : SyntaxFactory.IdentifierName( method.Identifier );

                var arguments = method.ParameterList.Parameters
                    .Select( p => SyntaxFactory.Argument( SyntaxFactory.IdentifierName( p.Identifier ) ) )
                    .Append( SyntaxFactory.Argument( SyntaxFactory.LiteralExpression( SyntaxKind.DefaultLiteralExpression ) ) );

                var invocation = SyntaxFactory.InvocationExpression(
                    name,
                    SyntaxFactory.ArgumentList( SyntaxFactory.SeparatedList( arguments ) ) );

                // The forwarder is not async: it returns the overload's task directly. That avoids CS1998, keeps
                // async iterators working, and is correct for an async void method too.
                return method
                    .WithModifiers(
                        SyntaxFactory.TokenList( method.Modifiers.Where( m => !m.IsKind( SyntaxKind.AsyncKeyword ) ) ) )
                    .WithBody( null )
                    .WithExpressionBody( SyntaxFactory.ArrowExpressionClause( invocation ) )
                    .WithSemicolonToken( SyntaxFactory.Token( SyntaxKind.SemicolonToken ) )
                    .WithAdditionalAnnotations( generatedCodeAnnotation );
            }

            /// <summary>
            /// Builds the overload that carries the original body and takes the token. The parameter deliberately has
            /// no default value: with one, a call using the original argument list would be ambiguous (CS0121).
            /// </summary>
            private MethodDeclarationSyntax CreateOverload( MethodDeclarationSyntax method, string parameterName )
            {
                var parameters = method.ParameterList.Parameters.GetWithSeparators().ToList();

                if ( parameters.Count > 0 )
                {
                    parameters[parameters.Count - 1] = parameters[parameters.Count - 1].AsNode()!.WithoutTrailingTrivia();

                    parameters.Add(
                        SyntaxFactory.Token( SyntaxKind.CommaToken )
                            .WithTrailingTrivia( SyntaxFactory.ElasticSpace )
                            .WithAdditionalAnnotations( this._generatedCodeAnnotation ) );
                }

                parameters.Add(
                    SyntaxFactory.Parameter(
                            default,
                            default,
                            CancellationTokenType.WithTrailingTrivia( SyntaxFactory.ElasticSpace ),
                            SyntaxFactory.Identifier( parameterName ),
                            null )
                        .WithAdditionalAnnotations( this._generatedCodeAnnotation ) );

                return method
                    .WithParameterList(
                        SyntaxFactory.ParameterList( SyntaxFactory.SeparatedList<ParameterSyntax>( [..parameters] ) ) )

                    // Both members are built from the same declaration, so without this the original's comments and
                    // documentation would appear twice: once above the forwarder and again above the overload.
                    .WithLeadingTrivia( SyntaxFactory.ElasticCarriageReturnLineFeed );
            }

            public override SyntaxNode VisitMethodDeclaration( MethodDeclarationSyntax node )
            {
                if ( !this.IsInAnnotatedType )
                {
                    return node;
                }

                var semanticModel = this._compilation.GetSemanticModel( node.SyntaxTree );

                var methodSymbol = semanticModel.GetDeclaredSymbol( node );

                if ( methodSymbol is not { IsAsync: true } ||
                     methodSymbol.Parameters.Any( IsCancellationToken ) ||
                     methodSymbol.Parameters.LastOrDefault() is { IsParams: true } )
                {
                    return node;
                }

                // A member that takes part in virtual dispatch cannot be split into a forwarder plus an overload,
                // because the signature is the dispatch contract:
                //
                //   - If both members are virtual, a derived class the aspect cannot see overrides only the original
                //     signature. Calling the token-taking overload then runs the base body and silently ignores the
                //     override. That code compiles, which makes the breakage worse.
                //   - If only the token-taking overload is virtual, an existing 'override M()' no longer compiles
                //     (CS0506) - the very breaking change #81 asks us to avoid.
                //
                // Transforming an override is only sound when the base type was transformed too, which we cannot rely
                // on across an assembly boundary. So every member involved in virtual dispatch is left alone.
                if ( methodSymbol.IsVirtual ||
                     methodSymbol.IsAbstract ||
                     methodSymbol.IsOverride ||
                     !methodSymbol.ExplicitInterfaceImplementations.IsDefaultOrEmpty )
                {
                    return node;
                }

                // The overload must not duplicate a method that already exists in the type.
                if ( WouldCollideWithExistingMember( methodSymbol ) )
                {
                    return node;
                }

                // TODO: Review: finding a unique name is a common pattern. Is there a common library implementation?
                const string defaultParameterName = "cancellationToken";
                var useParameterName = defaultParameterName;

                // The name must not collide with a parameter, a type parameter, or anything declared in the body:
                // a local declared in an enclosing scope of a nested one causes CS0136, and reusing the name of an
                // existing local would silently pass that local instead of the token. Collecting every identifier in
                // the declaration is a deliberate over-approximation - it can only make us pick a longer name.
                var usedNames = new HashSet<string>( StringComparer.Ordinal );

                foreach ( var token in node.DescendantTokens() )
                {
                    if ( token.IsKind( SyntaxKind.IdentifierToken ) )
                    {
                        usedNames.Add( token.ValueText );
                    }
                }

                for ( var i = 2; usedNames.Contains( useParameterName ); ++i )
                {
                    useParameterName = $"{defaultParameterName}{i}";
                }

                // Only mark the method here. The expansion into a forwarder plus an overload happens in
                // VisitTypeDeclaration, where the member list can be replaced.
                return node.WithAdditionalAnnotations( new SyntaxAnnotation( _expandAnnotationKind, useParameterName ) );
            }

            /// <summary>
            /// Determines whether adding a trailing <see cref="System.Threading.CancellationToken"/> parameter to
            /// <paramref name="method"/> would produce a signature that another member of the same type already has.
            /// </summary>
            private static bool WouldCollideWithExistingMember( IMethodSymbol method )
            {
                var containingType = method.ContainingType;

                if ( containingType == null )
                {
                    return false;
                }

                foreach ( var member in containingType.GetMembers( method.Name ) )
                {
                    if ( member is not IMethodSymbol otherMethod ||
                         SymbolEqualityComparer.Default.Equals( otherMethod, method ) ||
                         otherMethod.Parameters.Length != method.Parameters.Length + 1 ||
                         otherMethod.TypeParameters.Length != method.TypeParameters.Length )
                    {
                        continue;
                    }

                    if ( !IsCancellationToken( otherMethod.Parameters[otherMethod.Parameters.Length - 1] ) )
                    {
                        continue;
                    }

                    var parametersMatch = true;

                    for ( var i = 0; i < method.Parameters.Length; i++ )
                    {
                        if ( !SymbolEqualityComparer.Default.Equals(
                                otherMethod.Parameters[i].Type,
                                method.Parameters[i].Type ) )
                        {
                            parametersMatch = false;

                            break;
                        }
                    }

                    if ( parametersMatch )
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}