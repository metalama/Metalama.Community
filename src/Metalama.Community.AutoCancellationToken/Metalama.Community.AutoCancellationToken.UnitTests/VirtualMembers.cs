// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#pragma warning disable VSTHRD200, CA1822
#pragma warning disable IDE0051 // Remove unused private members - PrivateAsync shows that a non-virtual
                                // member is still transformed, and the token overload is a binding target.

namespace Metalama.Community.AutoCancellationToken.UnitTests.VirtualMembers;

// Members that take part in virtual dispatch are not transformed. The signature is the dispatch contract, so the
// forwarder-plus-overload approach of #81 cannot be applied to them:
//
//   - Emitting both members as virtual lets a derived class the aspect cannot see override only the original
//     signature; calling the token-taking overload would then run the base body and ignore the override, silently.
//   - Emitting only the token-taking overload as virtual would break an existing 'override M()' with CS0506.
[AutoCancellationToken]
internal class Base
{
    // Not transformed: virtual. ACT001 is reported because the call to Helper would have taken the token.
    public virtual async Task VirtualAsync() => await Helper();

    // Transformed: an ordinary method takes no part in dispatch.
    public async Task PlainAsync() => await Helper();

    // Transformed: a sealed class' method cannot be overridden either way, but this one is simply not virtual.
    private async Task PrivateAsync() => await Helper();

    private static async Task Helper() => await Task.Yield();

    private static async Task Helper( CancellationToken cancellationToken ) => await Task.Yield();
}

[AutoCancellationToken]
internal abstract class AbstractBase
{
    // Not transformed: abstract members have no body to move to an overload.
    public abstract Task AbstractAsync();
}

// Stands in for a derived class the aspect never sees: a different assembly, or simply no attribute.
internal class Derived : Base
{
    public override async Task VirtualAsync() => await Task.Yield();
}
