// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#pragma warning disable VSTHRD200, CA1822
#pragma warning disable IDE0051 // Remove unused private members - the token overload exists so that the
                                // transformed code has something to bind to.

namespace Metalama.Community.AutoCancellationToken.UnitTests.LocalNameCollision;

[AutoCancellationToken]
internal class LocalNameCollision
{
    // The added parameter must not collide with a local declared in the body (CS0136).
    public async Task LocalVariable()
    {
        var cancellationToken = 0;
        await Helper();
        _ = cancellationToken;
    }

    // ... nor with a local function's name.
    public async Task LocalFunctionName()
    {
        await Helper();
        void cancellationToken() { }
        cancellationToken();
    }

    private static async Task Helper() => await Task.Yield();

    private static async Task Helper( CancellationToken cancellationToken ) => await Task.Yield();
}
