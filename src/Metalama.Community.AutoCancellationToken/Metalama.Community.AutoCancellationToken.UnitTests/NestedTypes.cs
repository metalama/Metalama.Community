// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#pragma warning disable VSTHRD200, CA1822
#pragma warning disable IDE0051 // Remove unused private members - the token overload exists so that the
                                // transformed code has something to bind to.

namespace Metalama.Community.AutoCancellationToken.UnitTests.NestedTypes;

// The aspect used to be silently ignored on nested types (#109), because the rewriters returned early for a type
// without the annotation and so never reached a nested type that carried it.
[AutoCancellationToken]
internal class Outer
{
    // Transformed.
    public async Task OuterAsync() => await Helper();

    // Not transformed, as expected: the attribute applies to the members of the type it is applied to.
    internal class NestedNotAnnotated
    {
        public async Task NestedAsync() => await Helper();
    }

    // Transformed: a nested type that carries the attribute is now handled (#109).
    [AutoCancellationToken]
    internal class NestedAnnotated
    {
        public async Task NestedAsync() => await Helper();
    }

    private static async Task Helper() => await Task.Yield();

    private static async Task Helper( CancellationToken cancellationToken ) => await Task.Yield();
}
