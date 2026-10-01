## Metalama.Community.Virtuosity 
Allows you to make all target methods virtual without adding the virtual keyword to every one of them.

*This is a [Metalama](https://github.com/metalama/Metalama) aspect. It transforms your source code during compilation.*
 
<!-- [![CI badge](https://github.com/postsharp/Metalama.Community.Virtuosity/workflows/Full%20Pipeline/badge.svg)](https://github.com/postsharp/Metalama.Community.Virtuosity/actions?query=workflow%3A%22Full+Pipeline%22) -->

#### Example
Your code:
```csharp
using Metalama.Community.Virtuosity;

[Virtualize]
class A 
{
  public void M1() { }
  protected void M2() { }
  private void M3() { }
}
```
What gets compiled:
```csharp
class A 
{
  public virtual void M1() { }
  protected virtual void M2() { }
  private void M3() { }
}
```

#### Installation
1. Install the NuGet package: `dotnet add package Metalama.Community.Virtuosity --prerelease`.
2. Apply the aspect to the relevant types by adding the `[Virtualize]` attribute to them.

#### How to use

By annotating a class or a record type with `[Virtualize]`, you make all methods `virtual` (except those that can't be `virtual`, like `static` and `private` methods).

For more details, see [Details](Details.md).