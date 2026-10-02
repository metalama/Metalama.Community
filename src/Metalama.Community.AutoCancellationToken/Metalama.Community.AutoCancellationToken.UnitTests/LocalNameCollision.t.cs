namespace Metalama.Community.AutoCancellationToken.UnitTests.LocalNameCollision;
[AutoCancellationToken]
internal class LocalNameCollision
{
  // The added parameter must not collide with a local declared in the body (CS0136).
  public Task LocalVariable() => LocalVariable(default);
  public async Task LocalVariable(System.Threading.CancellationToken cancellationToken2)
  {
    var cancellationToken = 0;
    await Helper(cancellationToken2);
    _ = cancellationToken;
  }
  // ... nor with a local function's name.
  public Task LocalFunctionName() => LocalFunctionName(default);
  public async Task LocalFunctionName(System.Threading.CancellationToken cancellationToken2)
  {
    await Helper(cancellationToken2);
    void cancellationToken()
    {
    }
    cancellationToken();
  }
  private static async Task Helper() => await Task.Yield();
  private static async Task Helper(CancellationToken cancellationToken) => await Task.Yield();
}