namespace Metalama.Community.Virtuosity.TestApp
{
  [Virtualize]
  internal class OuterClass
  {
    internal class NestedClass
    {
      // Transformed.
      public virtual void M()
      {
      }
    }
    internal struct NestedStruct
    {
      // Not transformed.
      public void M()
      {
      }
    }
    internal interface INestedInterface
    {
      // Not transformed.
      void M()
      {
      }
    }
  }
}