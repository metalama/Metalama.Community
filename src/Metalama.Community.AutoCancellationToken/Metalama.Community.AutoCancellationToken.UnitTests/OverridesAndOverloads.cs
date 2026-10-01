// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#pragma warning disable VSTHRD200, CA1822

namespace Metalama.Community.AutoCancellationToken.UnitTests.OverridesAndOverloads;

internal interface IRunner
{
    Task RunAsync();
}

internal abstract class RunnerBase
{
    public abstract Task ExecuteAsync();

    public virtual Task VirtualAsync() => Task.CompletedTask;
}

// Since #81 the original signature is kept and an overload is added, so implicitly implementing an interface
// member is no longer a problem. Members involved in virtual dispatch are still skipped entirely - see
// VirtualMembers for why the forwarder-plus-overload approach cannot work for them.
[AutoCancellationToken]
internal class Runner : RunnerBase, IRunner
{
    // Not transformed: takes part in virtual dispatch.
    public override async Task ExecuteAsync() => await Task.Yield();

    // Not transformed: takes part in virtual dispatch.
    public override async Task VirtualAsync() => await Task.Yield();

    // Transformed: the original signature is kept, so IRunner.RunAsync is still implemented.
    public async Task RunAsync() => await Task.Yield();

    // Not transformed: the type already declares ExistingOverloadAsync(CancellationToken).
    public async Task ExistingOverloadAsync() => await Task.Yield();

    public async Task ExistingOverloadAsync( CancellationToken cancellationToken ) => await Task.Yield();

    // Transformed: kept as a forwarder, with the body moved to the new overload.
    public async Task PlainAsync() => await Task.Yield();
}

// Not transformed: explicit interface implementation.
[AutoCancellationToken]
internal class ExplicitRunner : IRunner
{
    async Task IRunner.RunAsync() => await Task.Yield();
}
