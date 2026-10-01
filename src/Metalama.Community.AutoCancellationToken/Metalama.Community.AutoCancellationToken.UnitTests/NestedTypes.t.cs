namespace Metalama.Community.AutoCancellationToken.UnitTests.NestedTypes;
// The aspect used to be silently ignored on nested types (#109), because the rewriters returned early for a type
// without the annotation and so never reached a nested type that carried it.
[AutoCancellationToken]
internal class Outer
{
  // Transformed.
  public Task OuterAsync() => OuterAsync(default);
  public async Task OuterAsync(System.Threading.CancellationToken cancellationToken) => await Helper(cancellationToken);
  // Not transformed, as expected: the attribute applies to the members of the type it is applied to.
  internal class NestedNotAnnotated
  {
    public async Task NestedAsync() => await Helper();
  }
  // Transformed: a nested type that carries the attribute is now handled (#109).
  [AutoCancellationToken]
  internal class NestedAnnotated
  {
    public Task NestedAsync() => NestedAsync(default);
    public async Task NestedAsync(System.Threading.CancellationToken cancellationToken) => await Helper(cancellationToken);
  }
  private static async Task Helper() => await Task.Yield();
  private static async Task Helper(CancellationToken cancellationToken) => await Task.Yield();
}