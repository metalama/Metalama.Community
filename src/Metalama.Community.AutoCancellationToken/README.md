## Metalama.Community.AutoCancellationToken 
Automatically propagates `CancellationToken` parameter to `async` methods and method calls within them.

*This is a [Metalama](https://github.com/metalama/Metalama) aspect. It transforms your source code during compilation.*

<!-- You can also [try this aspect on try.metalama.net](https://try.metalama.net/#autocancellationtoken). -->

<!-- [![CI badge](https://github.com/postsharp/Metalama.Community.AutoCancellationToken/workflows/Full%20Pipeline/badge.svg)](https://github.com/postsharp/Metalama.Community.AutoCancellationToken/actions?query=workflow%3A%22Full+Pipeline%22) -->

> ### ⚠️ Proof of concept
>
> **This aspect is a proof of concept, not a production-ready tool. Do not rely on it to make an application
> correctly cancellable.**
>
> Cancellation is a semantic decision, but this aspect works purely syntactically: it passes a token to a call
> because an overload happens to accept one. It has no way to know which calls *should* honour cancellation and
> which must always run to completion — cleanup, disposal, compensating writes, audit logging. Cancelling those
> causes resource leaks and partially applied state.
>
> Read the [limitations](#limitations) below before using it. Several of them fail silently.

#### Example

Your code:

```csharp
[AutoCancellationToken]
class C
{
    async Task MakeRequests(CancellationToken ct)
    {
        using var client = new HttpClient();
        await MakeRequest(client);
    }

    private static async Task MakeRequest(HttpClient client) => await client.GetAsync("https://example.org");
}
```

What gets compiled:

```csharp
class C
{
    async Task MakeRequests(CancellationToken ct)
    {
        using var client = new HttpClient();
        await MakeRequest(client, ct);
    }

    // The original signature is kept, so existing callers still compile.
    private static Task MakeRequest(HttpClient client) => MakeRequest(client, default);

    // The body moves to a new overload that takes the token.
    private static async Task MakeRequest(HttpClient client, CancellationToken cancellationToken)
        => await client.GetAsync("https://example.org", cancellationToken);
}
```

Notice that the call to `MakeRequest` now passes `ct`, that `MakeRequest` gained an overload taking a
`CancellationToken`, and that the call to `HttpClient.GetAsync` passes it on.

#### Installation
Install the NuGet package: `dotnet add package Metalama.Community.AutoCancellationToken`.

#### How to use

Add `[AutoCancellationToken]` to the types where you want it to apply.

By annotating a type with `[AutoCancellationToken]`, you add cancellation to its `async` methods. Specifically:

* For each `async` method that has no `CancellationToken` parameter, the original method is kept as a forwarder and
  an overload taking a `CancellationToken` is added. The original signature is never changed, so existing callers
  keep compiling.
* A `CancellationToken` argument is added to calls within `async` methods where:
    * `CancellationToken` can be added as a last argument and the added argument corresponds to a
      `CancellationToken` parameter (e.g. it's not a `params object[]` parameter or a generic parameter). The added
      argument can result in calling a different overload of the method, or specifying a value for an optional
      parameter.
    * The call is not in a `static` local function or a `static` lambda, which cannot capture the enclosing token.
    * The containing method doesn't have two or more `CancellationToken` parameters, since it wouldn't be clear
      which one to use.

#### Limitations

**The aspect decides what to cancel from syntax alone.** It cannot tell a cancellable operation from one that must
complete. If a method in an annotated type performs cleanup, disposal or logging through a call that offers a
`CancellationToken` overload, the aspect will pass the token and that work becomes cancellable. Review the
generated code before depending on it.

**Callers of the original signature never cancel.** The forwarder passes `default`, i.e. `CancellationToken.None`.
Code outside the aspect's reach keeps compiling, but gains no cancellation at all — silently. Only callers within
annotated types have the token propagated to them.

**Members involved in virtual dispatch are not transformed.** `virtual`, `abstract` and `override` methods, and
explicit interface implementations, are skipped entirely and receive no cancellation support. A method's signature
is its dispatch contract: adding an overload would let a derived class the aspect cannot see override only the
original signature, so calling the token-taking overload would run the base body and silently ignore the override.
Making only the overload virtual would instead break existing `override` declarations.

This one is *not* silent. When such a member calls something that would have accepted a token, the aspect reports
the warning `ACT001`, so you can see where cancellation was dropped and add a `CancellationToken` parameter by hand:

```
warning ACT001: 'RunAsync' is not given a CancellationToken parameter because it is virtual, abstract, an override
or an explicit interface implementation, and changing such a signature would break the dispatch contract. Its call
to 'GetAsync' therefore runs without cancellation. Declare a CancellationToken parameter on 'RunAsync' explicitly
to propagate cancellation.
```

Declaring the parameter yourself resolves it: the aspect then propagates that token through the body as usual.

**Propagation is not transitive, so a chain breaks at the first method the aspect does not own.** The aspect adds a
token to the `async` methods of annotated types and then passes it to calls that already accept one. It never adds a
parameter to a method merely because doing so is what would let the token travel further:

```csharp
[AutoCancellationToken]
class Caller
{
    // WorkAsync is in a type the aspect was not applied to, so it never gains a CancellationToken parameter and
    // there is no overload to pass the token to. The chain stops here, even though the cancellable call is one
    // hop away.
    async Task RunAsync() => await Helper.WorkAsync();
}

static class Helper
{
    public static async Task WorkAsync() => await Cancellable();
}
```

Closing that gap means deciding, for every method, whether it transitively reaches a call that can be cancelled —
and adding a parameter to one method changes the answer for all of its callers, so the analysis has to be iterated
to a fix point. That is possible, but generally out of scope for a build-time transformation because of its cost,
and impossible across an assembly boundary. Annotating every type involved in a call chain is the practical
workaround.

**Conversely, within an annotated type the aspect over-approximates.** Because it does not analyse which methods
need a token, *every* `async` method gets one, including methods that never use it:

```csharp
public Task DoNothingAsync() => DoNothingAsync(default);
public async Task DoNothingAsync(CancellationToken cancellationToken) => await Task.Yield(); // unused
```

**The aspect only sees the current compilation.** It cannot add tokens to methods in referenced assemblies, and it
cannot know whether a type deriving from yours will be transformed.

**Several constructs are still skipped without being reported.** A method whose last parameter is a `params` array,
a method that already declares two or more `CancellationToken` parameters, and a call whose candidate overload takes
a generic final parameter are all left untouched with no diagnostic. Only the virtual-dispatch case above reports
`ACT001`; for these, nothing tells you the aspect did nothing.

**Async iterators are not given `[EnumeratorCancellation]`.** The token added to an `async IAsyncEnumerable<T>`
method is not wired up to the enumerator, so a token passed to `WithCancellation` is unconsumed. This is the one
limitation the compiler does warn about, as `CS8425`.

If you need cancellation you can rely on, thread `CancellationToken` through explicitly.
