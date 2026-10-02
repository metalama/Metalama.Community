namespace Metalama.Community.Virtuosity.Tests.AbstractClass
{
  [Virtualize]
  internal abstract class AbstractClass
  {
    // Not transformed (an abstract member is implicitly virtual and cannot be marked virtual).
    public abstract void AbstractMethod();
    // Not transformed.
    protected abstract int AbstractProperty { get; }
    // Transformed.
    public virtual void ConcreteMethod()
    {
    }
    // Transformed.
    public virtual int ConcreteProperty { get; }
  }
}
