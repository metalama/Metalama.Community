// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Formatting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Threading;

namespace Metalama.Community.AutoCancellationToken
{
    internal partial class AutoCancellationTokenWeaver
    {
        [CompileTime]
        private abstract class RewriterBase : CSharpSyntaxRewriter
        {
            public override SyntaxNode VisitInterfaceDeclaration( InterfaceDeclarationSyntax node )
                => this.VisitTypeDeclaration( node, base.VisitInterfaceDeclaration );

            public override SyntaxNode VisitClassDeclaration( ClassDeclarationSyntax node ) => this.VisitTypeDeclaration( node, base.VisitClassDeclaration );

            public override SyntaxNode VisitStructDeclaration( StructDeclarationSyntax node ) => this.VisitTypeDeclaration( node, base.VisitStructDeclaration );

            public override SyntaxNode VisitRecordDeclaration( RecordDeclarationSyntax node ) => this.VisitTypeDeclaration( node, base.VisitRecordDeclaration );

            protected abstract T VisitTypeDeclaration<T>( T node, Func<T, SyntaxNode?> baseVisit )
                where T : TypeDeclarationSyntax;

            /// <summary>
            /// Gets a value indicating whether the type currently being visited carries the aspect.
            /// </summary>
            protected bool IsInAnnotatedType { get; private set; }

            /// <summary>
            /// Descends into <paramref name="node"/> while recording whether it carries the aspect, so that members
            /// are transformed only when their own containing type is annotated.
            /// </summary>
            /// <remarks>
            /// Descending unconditionally is what makes the aspect work on a nested type (#109). The previous
            /// implementation returned early for a type without the annotation, so a nested annotated type was never
            /// reached. Because <see cref="CSharpSyntaxRewriter.VisitMethodDeclaration"/> does not itself know which
            /// type it is in, the flag has to be saved and restored around the recursion.
            /// </remarks>
            protected T VisitTypeDeclarationCore<T>( T node, Func<T, SyntaxNode?> baseVisit )
                where T : TypeDeclarationSyntax
            {
                var previous = this.IsInAnnotatedType;
                this.IsInAnnotatedType = node.HasAnnotation( AnnotateNodesRewriter.Annotation );

                try
                {
                    return (T) baseVisit( node )!;
                }
                finally
                {
                    this.IsInAnnotatedType = previous;
                }
            }

            protected static readonly TypeSyntax CancellationTokenType = SyntaxFactory
                .ParseTypeName( typeof(CancellationToken).FullName! )
                .WithSimplifierAnnotation();

            protected static bool IsCancellationToken( IParameterSymbol parameter )
                => parameter.OriginalDefinition.Type.ToString() == typeof(CancellationToken).FullName;

            // Make sure VisitInvocationExpression is not called for expressions inside members that are not methods.
            // Note that this list is not the only line of defence: AddCancellationTokenArgumentRewriter also ignores
            // invocations reached without an enclosing method, so an unanticipated member kind cannot crash the weaver.
            public override SyntaxNode VisitPropertyDeclaration( PropertyDeclarationSyntax node ) => node;

            public override SyntaxNode VisitIndexerDeclaration( IndexerDeclarationSyntax node ) => node;

            public override SyntaxNode VisitEventDeclaration( EventDeclarationSyntax node ) => node;

            // Field-like events (event EventHandler E = Handler();) are a different node type than EventDeclarationSyntax.
            public override SyntaxNode VisitEventFieldDeclaration( EventFieldDeclarationSyntax node ) => node;

            public override SyntaxNode VisitFieldDeclaration( FieldDeclarationSyntax node ) => node;

            public override SyntaxNode VisitConstructorDeclaration( ConstructorDeclarationSyntax node ) => node;

            public override SyntaxNode VisitDestructorDeclaration( DestructorDeclarationSyntax node ) => node;

            public override SyntaxNode VisitOperatorDeclaration( OperatorDeclarationSyntax node ) => node;

            public override SyntaxNode VisitConversionOperatorDeclaration( ConversionOperatorDeclarationSyntax node ) => node;
        }
    }
}