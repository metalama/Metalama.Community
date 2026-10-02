using System;
using System.Threading.Tasks;
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable CS0649 // Field is never assigned to
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable SA1306 // Field should begin with lower-case letter
#pragma warning disable SA1401 // Field should be private
namespace Metalama.Community.Virtuosity.TestApp
{
  [Virtualize]
  internal class C : IDisposable
  {
    // Not transformed.
    private void ImplicitPrivate()
    {
    }
    // Not transformed.
    private void ExplicitPrivate()
    {
    }
    // Transformed.
    public virtual void Public()
    {
    }
    // Not transformed (already virtual).
    public virtual void PublicVirtual()
    {
    }
    // Transformed.
    protected async virtual void ProtectedAsync()
    {
      await Task.Yield();
    }
    // Transformed.
    private protected virtual void PrivateProtected()
    {
    }
    // Transformed (sealed removed).
    public override string ToString()
    {
      return "";
    }
    // Not transformed.
    public override int GetHashCode()
    {
      return 0;
    }
    // Not transformed.
    public static void PublicStatic()
    {
    }
    // Not transformed.
    void IDisposable.Dispose()
    {
    }
    // Transformed.
    public virtual int Property { get; }
    // Not transformed.
    protected int Field;
  }
}