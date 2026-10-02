using System.Threading.Tasks;
#pragma warning disable CA1822 // Mark members as static
namespace Metalama.Community.Virtuosity.TestApp
{
  // The weaver must not crash on C# 14 extension blocks. Members declared inside an
  // extension block cannot be virtual, so they are not transformed (see issue #94).
  [Virtualize]
  internal static class ValueTaskExtensions
  {
    extension<T>(ValueTask<T> valueTask)
    {
      // Not transformed (extension members cannot be virtual).
      public ValueTask ToValueTask()
      {
        if (valueTask.IsCompletedSuccessfully)
        {
          valueTask.GetAwaiter().GetResult();
          return default;
        }
        return new ValueTask(valueTask.AsTask());
      }
      // Not transformed (extension members cannot be virtual).
      public bool IsDone => valueTask.IsCompleted;
    }
  }
  // A regular class is still virtualized normally even when the project contains extension blocks.
  [Virtualize]
  internal class RegularClass
  {
    // Transformed.
    public virtual void Public()
    {
    }
  }
}
