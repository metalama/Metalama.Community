// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Community.Virtuosity;

[RequireAspectWeaver( typeof(VirtuosityWeaver) )]
public sealed class VirtualizeAttribute : TypeAspect
{
    public override void BuildEligibility( IEligibilityBuilder<INamedType> builder )
    {
        base.BuildEligibility( builder );

        builder.MustSatisfy(
            t => t.TypeKind is TypeKind.Class,
            t => $"{t} must be class" );
    }
}