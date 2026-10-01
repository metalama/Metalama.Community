// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.AspectWeavers;
using Metalama.Framework.Engine.CodeModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System.Threading.Tasks;

namespace Metalama.Community.AutoCancellationToken
{
    [MetalamaPlugIn]
    internal partial class AutoCancellationTokenWeaver : IAspectWeaver
    {
        /// <summary>
        /// Reported when a member that takes part in virtual dispatch is left untransformed although its body calls
        /// something that would have accepted a <see cref="System.Threading.CancellationToken"/>. Without this, the
        /// loss of cancellation is entirely silent.
        /// </summary>
        private static readonly DiagnosticDescriptor _cancellationNotPropagated = new(
            "ACT001",
            "Cancellation is not propagated to a member that takes part in virtual dispatch",
            "'{0}' is not given a CancellationToken parameter because it is virtual, abstract, an override or an "
            + "explicit interface implementation, and changing such a signature would break the dispatch contract. "
            + "Its call to '{1}' therefore runs without cancellation. Declare a CancellationToken parameter on '{0}' "
            + "explicitly to propagate cancellation.",
            "Metalama.Community.AutoCancellationToken",
            DiagnosticSeverity.Warning,
            true );

        public async Task TransformAsync( AspectWeaverContext context )
        {
            var compilation = context.Compilation;

            var instancesNodes = context.AspectInstances.SelectMany( a => a.Key.DeclaringSyntaxReferences )
                .Select( x => x.GetSyntax() );

            await RunRewriterAsync( new AnnotateNodesRewriter( instancesNodes ) );

            await RunRewriterAsync(
                new AddCancellationTokenParameterRewriter(
                    compilation.Compilation,
                    context.GeneratedCodeAnnotation ) );

            // This pass runs after the parameter pass, so a method that has just gained a CancellationToken overload
            // is already visible here. That matters for the ACT001 diagnostic, which asks whether a call could have
            // accepted a token.
            await RunRewriterAsync(
                new AddCancellationTokenArgumentRewriter(
                    compilation.Compilation,
                    context.GeneratedCodeAnnotation,
                    context.ReportDiagnostic ) );

            context.Compilation = compilation;

            async Task RunRewriterAsync( CSharpSyntaxRewriter rewriter )
            {
                compilation = await compilation.RewriteSyntaxTreesAsync( rewriter, context.ServiceProvider );
            }
        }
    }
}