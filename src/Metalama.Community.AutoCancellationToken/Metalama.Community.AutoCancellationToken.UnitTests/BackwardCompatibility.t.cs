namespace Metalama.Community.AutoCancellationToken.UnitTests.BackwardCompatibility;
// #81: the aspect must not change the signature of an existing method, because that breaks callers. The original
// signature is kept as a forwarder and the token-taking method is added as a new overload.
[AutoCancellationToken]
internal class Service
{
  public Task DoWorkAsync(int value) => DoWorkAsync(value, default);
  public async Task DoWorkAsync(int value, System.Threading.CancellationToken cancellationToken) => await Task.Yield();
  // A generic method: the forwarder must pass the type arguments through.
  public Task DoGenericAsync<T>(T value) => DoGenericAsync<T>(value, default);
  public async Task DoGenericAsync<T>(T value, System.Threading.CancellationToken cancellationToken) => await Task.Yield();
  // A method with no parameters at all.
  public Task DoNothingAsync() => DoNothingAsync(default);
  public async Task DoNothingAsync(System.Threading.CancellationToken cancellationToken) => await Task.Yield();
}
// This type is NOT annotated, so it stands in for code outside the aspect's reach - an existing caller. It must
// still compile against the original signatures, which is the whole point of #81.
internal class ExistingCaller
{
  public async Task CallAsync()
  {
    var service = new Service();
    await service.DoWorkAsync(1);
    await service.DoGenericAsync("x");
    await service.DoNothingAsync();
  }
}