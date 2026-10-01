// Warning ACT001 on `RunAsync`: `'RunAsync' is not given a CancellationToken parameter because it is virtual, abstract, an override or an explicit interface implementation, and changing such a signature would break the dispatch contract. Its call to 'Helper' therefore runs without cancellation. Declare a CancellationToken parameter on 'RunAsync' explicitly to propagate cancellation.`
namespace Metalama.Community.AutoCancellationToken.UnitTests.TypeKinds;
// RewriterBase handles classes, structs, records and interfaces, but only classes were ever tested.
[AutoCancellationToken]
internal struct Struct
{
  public Task RunAsync() => RunAsync(default);
  public async Task RunAsync(System.Threading.CancellationToken cancellationToken) => await Helpers.Helper(cancellationToken);
}
[AutoCancellationToken]
internal record Record
{
  public Task RunAsync() => RunAsync(default);
  public async Task RunAsync(System.Threading.CancellationToken cancellationToken) => await Helpers.Helper(cancellationToken);
}
[AutoCancellationToken]
internal record struct RecordStruct
{
  public Task RunAsync() => RunAsync(default);
  public async Task RunAsync(System.Threading.CancellationToken cancellationToken) => await Helpers.Helper(cancellationToken);
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
  public static async Task Helper(CancellationToken cancellationToken) => await Task.Yield();
}