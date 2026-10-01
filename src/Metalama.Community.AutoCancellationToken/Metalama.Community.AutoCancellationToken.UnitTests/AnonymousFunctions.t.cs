namespace Metalama.Community.AutoCancellationToken.UnitTests.AnonymousFunctions;
// Only the static-suppression branch of the anonymous-function visitors was covered before. These exercise the
// propagating branch, which is what the visitors exist for.
[AutoCancellationToken]
internal class AnonymousFunctions
{
  // The token is propagated into a non-static local function.
  public static async Task LocalFunction(CancellationToken ct)
  {
    await Inner();
    async Task Inner() => await Helper(ct);
  }
  // The token is propagated into a non-static lambda.
  public static async Task ParenthesizedLambda(CancellationToken ct)
  {
    Func<Task> f = async () => await Helper(ct);
    await f();
  }
  // The token is propagated into a non-static simple lambda.
  public static async Task SimpleLambda(CancellationToken ct)
  {
    Func<int, Task> f = async _ => await Helper(ct);
    await f(0);
  }
  // The token is propagated into a non-static anonymous method.
  public static async Task AnonymousMethod(CancellationToken ct)
  {
    Func<Task> f = async delegate
    {
      await Helper(ct);
    };
    await f();
  }
  // Not propagated into a static lambda: it cannot capture the enclosing parameter.
  public static async Task StaticLambda(CancellationToken ct)
  {
    Func<Task> f = static async () => await Helper();
    await f();
  }
  // Not propagated into a static anonymous method, for the same reason.
  public static async Task StaticAnonymousMethod(CancellationToken ct)
  {
    Func<Task> f = static async delegate
    {
      await Helper();
    };
    await f();
  }
  private static async Task Helper() => await Task.Yield();
  private static async Task Helper(CancellationToken cancellationToken) => await Task.Yield();
}