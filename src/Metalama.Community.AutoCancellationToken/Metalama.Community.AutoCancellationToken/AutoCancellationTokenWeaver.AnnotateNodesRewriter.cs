// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace Metalama.Community.AutoCancellationToken
{
    internal partial class AutoCancellationTokenWeaver
    {
        [CompileTime]
        private sealed class AnnotateNodesRewriter : RewriterBase
        {
            private readonly HashSet<SyntaxNode> _instancesNodes;

            public AnnotateNodesRewriter( IEnumerable<SyntaxNode> instancesNodes )
            {
                this._instancesNodes = [..instancesNodes];
            }

            public static SyntaxAnnotation Annotation { get; } = new();

            protected override T VisitTypeDeclaration<T>( T node, Func<T, SyntaxNode?> baseVisit )
            {
                // Always descend, even into a type that does not carry the aspect: a nested type may carry it itself,
                // and returning early here made the aspect a silent no-op on nested types (#109).
                var visited = (T) baseVisit( node )!;

                return this._instancesNodes.Contains( node )
                    ? visited.WithAdditionalAnnotations( Annotation )
                    : visited;
            }
        }
    }
}