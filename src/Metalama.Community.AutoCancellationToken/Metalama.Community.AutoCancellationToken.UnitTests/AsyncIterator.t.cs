// Warning CS8425 on `StreamAsync`: `Async-iterator 'Iterator.StreamAsync(CancellationToken)' has one or more parameters of type 'CancellationToken' but none of them is decorated with the 'EnumeratorCancellation' attribute, so the cancellation token parameter from the generated 'IAsyncEnumerable<>.GetAsyncEnumerator' will be unconsumed`
using System.Collections.Generic;
namespace Metalama.Community.AutoCancellationToken.UnitTests.AsyncIterator;
// An async iterator is transformed like any other async method, but the token is not wired up to the enumerator:
// the added parameter carries no [EnumeratorCancellation], so a token passed to WithCancellation is unconsumed.
// The compiler reports this as CS8425, which is captured in the baseline - so at least this limitation is visible.
[AutoCancellationToken]
internal class Iterator
{
  public IAsyncEnumerable<int> StreamAsync() => StreamAsync(default);
  public async IAsyncEnumerable<int> StreamAsync(System.Threading.CancellationToken cancellationToken)
  {
    await Task.Yield();
    yield return 1;
  }
}