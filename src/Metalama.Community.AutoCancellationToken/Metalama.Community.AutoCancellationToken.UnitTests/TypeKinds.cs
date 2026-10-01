// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#pragma warning disable VSTHRD200, CA1822

namespace Metalama.Community.AutoCancellationToken.UnitTests.TypeKinds;

// RewriterBase handles classes, structs, records and interfaces, but only classes were ever tested.
[AutoCancellationToken]
internal struct Struct
{
    public async Task RunAsync() => await Helpers.Helper();
}

[AutoCancellationToken]
internal record Record
{
    public async Task RunAsync() => await Helpers.Helper();
}

[AutoCancellationToken]
internal record struct RecordStruct
{
    public async Task RunAsync() => await Helpers.Helper();
}

[AutoCancellationToken]
internal interface IInterface
{
    // A default interface member is implicitly virtual, so it is not transformed and ACT001 is reported.
    async Task RunAsync() => await Helpers.Helper();
}

internal static class Helpers
{
    public static async Task Helper() => await Task.Yield();

    public static async Task Helper( CancellationToken cancellationToken ) => await Task.Yield();
}
