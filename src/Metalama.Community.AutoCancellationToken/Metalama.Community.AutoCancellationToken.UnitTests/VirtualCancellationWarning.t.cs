// Warning ACT001 on `VirtualLosesCancellation`: `'VirtualLosesCancellation' is not given a CancellationToken parameter because it is virtual, abstract, an override or an explicit interface implementation, and changing such a signature would break the dispatch contract. Its call to 'HasTokenOverload' therefore runs without cancellation. Declare a CancellationToken parameter on 'VirtualLosesCancellation' explicitly to propagate cancellation.`
// Warning ACT001 on `VirtualLosesCancellation`: `'VirtualLosesCancellation' is not given a CancellationToken parameter because it is virtual, abstract, an override or an explicit interface implementation, and changing such a signature would break the dispatch contract. Its call to 'HasTokenOverload' therefore runs without cancellation. Declare a CancellationToken parameter on 'VirtualLosesCancellation' explicitly to propagate cancellation.`
namespace Metalama.Community.AutoCancellationToken.UnitTests.VirtualCancellationWarning;
// A type the aspect does not touch, so its methods gain no CancellationToken overload.
internal static class External
{
  public static async Task NoTokenOverload() => await Task.Yield();
  public static async Task HasTokenOverload() => await Task.Yield();
  public static async Task HasTokenOverload(CancellationToken cancellationToken) => await Task.Yield();
}
// Members that take part in virtual dispatch are never given a CancellationToken parameter, because the signature is
// the dispatch contract. ACT001 makes the resulting loss of cancellation visible instead of silent, but only when the
// member would actually have benefited - that is, when its body calls something that would have accepted a token.
[AutoCancellationToken]
internal class Base
{
  // ACT001: virtual, and the call would have taken the token.
  public virtual async Task VirtualLosesCancellation() => await External.HasTokenOverload();
  // No ACT001: nothing in the body would have accepted a token.
  public virtual async Task VirtualGainsNothing() => await External.NoTokenOverload();
  // No ACT001: the author already declared a token, so it is propagated normally.
  public virtual async Task VirtualWithToken(CancellationToken cancellationToken) => await External.HasTokenOverload(cancellationToken);
  // No ACT001: a static local function cannot capture the enclosing token, so nothing was lost by not having one.
  public virtual async Task VirtualWithStaticLocalFunction()
  {
    await Inner();
    static async Task Inner() => await External.HasTokenOverload();
  }
  // No ACT001: not part of virtual dispatch, so it is transformed normally.
  public Task PlainIsTransformed() => PlainIsTransformed(default);
  public async Task PlainIsTransformed(System.Threading.CancellationToken cancellationToken) => await External.HasTokenOverload(cancellationToken);
}
[AutoCancellationToken]
internal class Derived : Base
{
  // ACT001: an override is equally unable to gain a parameter.
  public override async Task VirtualLosesCancellation() => await External.HasTokenOverload();
}
[AutoCancellationToken]
internal abstract class AbstractBase
{
  // No ACT001: an abstract member has no body, so there is nothing that could have used a token.
  public abstract Task AbstractAsync();
}