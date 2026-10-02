namespace Metalama.Community.AutoCancellationToken.UnitTests.TransitivePropagation;
[AutoCancellationToken]
internal class Caller
{
  // The chain stops here. Helper.WorkAsync is in a type the aspect was not applied to, so it never gains a
  // CancellationToken parameter and there is no overload for the token to be passed to.
  public Task RunAsync() => RunAsync(default);
  public async Task RunAsync(System.Threading.CancellationToken cancellationToken) => await Helper.WorkAsync();
}
// Not annotated. The aspect does not add a parameter to WorkAsync even though doing so is what would let a token
// reach the cancellable call inside it: deciding that requires a fix-point analysis over the call graph.
internal static class Helper
{
  public static async Task WorkAsync() => await Cancellable();
  private static async Task Cancellable() => await Task.Yield();
  private static async Task Cancellable(CancellationToken cancellationToken) => await Task.Yield();
}