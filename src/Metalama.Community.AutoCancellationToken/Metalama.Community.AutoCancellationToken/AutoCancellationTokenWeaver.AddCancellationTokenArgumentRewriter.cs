// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

namespace Metalama.Community.AutoCancellationToken
{
    internal sealed partial class AutoCancellationTokenWeaver
    {
        [CompileTime]
        private sealed class AddCancellationTokenArgumentRewriter : RewriterBase
        {
            private readonly Compilation _compilation;
            private readonly SyntaxAnnotation _generatedCodeAnnotation;
            private readonly Action<Diagnostic> _reportDiagnostic;
            private string? _cancellationTokenParameterName;

            public AddCancellationTokenArgumentRewriter(
                Compilation compilation,
                SyntaxAnnotation generatedCodeAnnotation,
                Action<Diagnostic> reportDiagnostic )
            {
                this._compilation = compilation;
                this._generatedCodeAnnotation = generatedCodeAnnotation;
                this._reportDiagnostic = reportDiagnostic;
            }

            protected override T VisitTypeDeclaration<T>( T node, Func<T, SyntaxNode?> baseVisit )
                => this.VisitTypeDeclarationCore( node, baseVisit );

            public override SyntaxNode? VisitMethodDeclaration( MethodDeclarationSyntax node )
            {
                if ( !this.IsInAnnotatedType )
                {
                    return node;
                }

                var semanticModel = this._compilation.GetSemanticModel( node.SyntaxTree );

                var methodSymbol = semanticModel.GetDeclaredSymbol( node );

                if ( methodSymbol is not { IsAsync: true } )
                {
                    return node;
                }

                var cancellationTokenParameter = methodSymbol.Parameters.Where( IsCancellationToken ).LastOrDefault();

                if ( cancellationTokenParameter == null ||
                     methodSymbol.Parameters.Where( IsCancellationToken ).Count() > 1 )
                {
                    if ( cancellationTokenParameter == null )
                    {
                        this.ReportIfCancellationIsSilentlyLost( node, methodSymbol, semanticModel );
                    }

                    return node;
                }

                this._cancellationTokenParameterName = cancellationTokenParameter.Name;

                try
                {
                    return base.VisitMethodDeclaration( node );
                }
                finally
                {
                    this._cancellationTokenParameterName = null;
                }
            }

            public override SyntaxNode VisitAnonymousMethodExpression( AnonymousMethodExpressionSyntax node )
                => VisitFunction(
                    node,
                    node.Modifiers.Any( SyntaxKind.StaticKeyword ),
                    base.VisitAnonymousMethodExpression );

            public override SyntaxNode VisitParenthesizedLambdaExpression( ParenthesizedLambdaExpressionSyntax node )
                => VisitFunction(
                    node,
                    node.Modifiers.Any( SyntaxKind.StaticKeyword ),
                    base.VisitParenthesizedLambdaExpression );

            public override SyntaxNode VisitSimpleLambdaExpression( SimpleLambdaExpressionSyntax node )
                => VisitFunction(
                    node,
                    node.Modifiers.Any( SyntaxKind.StaticKeyword ),
                    base.VisitSimpleLambdaExpression );

            public override SyntaxNode VisitLocalFunctionStatement( LocalFunctionStatementSyntax node )
                => VisitFunction(
                    node,
                    node.Modifiers.Any( SyntaxKind.StaticKeyword ),
                    base.VisitLocalFunctionStatement );

            private static T VisitFunction<T>( T node, bool isStatic, Func<T, SyntaxNode?> baseVisit )
                where T : SyntaxNode
            {
                if ( isStatic )
                {
                    return node;
                }

                return (T) baseVisit( node )!;
            }

            /// <summary>
            /// Determines whether appending a <see cref="System.Threading.CancellationToken"/> argument to <paramref name="invocation"/>
            /// would bind to a method whose corresponding parameter is a <see cref="System.Threading.CancellationToken"/>.
            /// </summary>
            private static bool WouldAcceptCancellationToken(
                SemanticModel semanticModel,
                InvocationExpressionSyntax invocation,
                out IMethodSymbol? candidate )
            {
                var invocationWithCt = invocation.AddArgumentListArguments(
                    SyntaxFactory.Argument( SyntaxFactory.DefaultExpression( CancellationTokenType ) ) );

                var newInvocationArgumentsCount = invocationWithCt.ArgumentList.Arguments.Count;

                if (

                    // the code compiles
                    semanticModel.GetSpeculativeSymbolInfo( invocation.SpanStart, invocationWithCt, default ).Symbol is
                        IMethodSymbol speculativeSymbol &&

                    // the added parameter corresponds to its own argument
                    speculativeSymbol.Parameters.Length >= newInvocationArgumentsCount &&

                    // that argument is CancellationToken
                    IsCancellationToken( speculativeSymbol.Parameters[newInvocationArgumentsCount - 1] ) )
                {
                    candidate = speculativeSymbol;

                    return true;
                }

                candidate = null;

                return false;
            }

            /// <summary>
            /// Reports <c>ACT001</c> when a member that takes part in virtual dispatch was left without a
            /// <see cref="System.Threading.CancellationToken"/> parameter although its body calls something that would have accepted
            /// one. Such a member is deliberately never transformed, because its signature is the dispatch contract,
            /// but the resulting loss of cancellation would otherwise be entirely silent.
            /// </summary>
            private void ReportIfCancellationIsSilentlyLost(
                MethodDeclarationSyntax node,
                IMethodSymbol methodSymbol,
                SemanticModel semanticModel )
            {
                if ( !methodSymbol.IsVirtual &&
                     !methodSymbol.IsAbstract &&
                     !methodSymbol.IsOverride &&
                     methodSymbol.ExplicitInterfaceImplementations.IsDefaultOrEmpty )
                {
                    return;
                }

                // Do not descend into static anonymous functions: they cannot capture a token, so a call inside one
                // would not have benefited even if the containing method had a parameter.
                foreach ( var invocation in node
                             .DescendantNodes( descendIntoChildren: n => !IsStaticAnonymousFunction( n ) )
                             .OfType<InvocationExpressionSyntax>() )
                {
                    if ( WouldAcceptCancellationToken( semanticModel, invocation, out var candidate ) )
                    {
                        this._reportDiagnostic(
                            Diagnostic.Create(
                                _cancellationNotPropagated,
                                node.Identifier.GetLocation(),
                                methodSymbol.Name,
                                candidate!.Name ) );

                        // One diagnostic per member is enough to make the point.
                        return;
                    }
                }
            }

            private static bool IsStaticAnonymousFunction( SyntaxNode node )
                => node switch
                {
                    AnonymousMethodExpressionSyntax anonymousMethod => anonymousMethod.Modifiers.Any( SyntaxKind.StaticKeyword ),
                    ParenthesizedLambdaExpressionSyntax lambda => lambda.Modifiers.Any( SyntaxKind.StaticKeyword ),
                    SimpleLambdaExpressionSyntax lambda => lambda.Modifiers.Any( SyntaxKind.StaticKeyword ),
                    LocalFunctionStatementSyntax localFunction => localFunction.Modifiers.Any( SyntaxKind.StaticKeyword ),
                    _ => false
                };

            public override SyntaxNode VisitInvocationExpression( InvocationExpressionSyntax node )
            {
                // There is no enclosing method to take the CancellationToken from. This happens for invocations in
                // members that VisitMethodDeclaration never sees. RewriterBase blocks the member kinds we know of,
                // but this guard makes an unanticipated one a no-op instead of a crash.
                if ( this._cancellationTokenParameterName == null )
                {
                    return node;
                }

                var semanticModel = this._compilation.GetSemanticModel( node.SyntaxTree );

                var mustAddArgument = WouldAcceptCancellationToken( semanticModel, node, out _ );

                node = (InvocationExpressionSyntax?) base.VisitInvocationExpression( node )!;

                if ( mustAddArgument )
                {
                    var arguments = node.ArgumentList.Arguments.GetWithSeparators().ToList();

                    if ( arguments.Count > 0 )
                    {
                        // Remove the trivia after the last argument.
                        arguments[arguments.Count - 1] =
                            arguments[arguments.Count - 1].AsNode()!.WithoutTrailingTrivia();

                        arguments.Add(
                            SyntaxFactory.Token( SyntaxKind.CommaToken )
                                .WithTrailingTrivia( SyntaxFactory.ElasticSpace )
                                .WithAdditionalAnnotations( this._generatedCodeAnnotation ) );
                    }

                    arguments.Add(
                        SyntaxFactory.Argument( SyntaxFactory.IdentifierName( this._cancellationTokenParameterName! ) )
                            .WithAdditionalAnnotations( this._generatedCodeAnnotation )
                            .WithTrailingTrivia( SyntaxFactory.ElasticSpace ) );

                    node = node.WithArgumentList(
                        SyntaxFactory.ArgumentList( SyntaxFactory.SeparatedList<ArgumentSyntax>( [..arguments] ) ) );
                }

                return node;
            }
        }
    }
}