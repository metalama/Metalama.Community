namespace Metalama.Community.Virtuosity.Tests.Record
{
  // Records are named in the README and VisitRecordDeclaration handles them specially, but they had no coverage.
  [Virtualize]
  internal record Record
  {
    // Transformed.
    public virtual void PublicMethod()
    {
    }
    // Transformed.
    protected virtual void ProtectedMethod()
    {
    }
    // Transformed.
    internal virtual void InternalMethod()
    {
    }
    // Not transformed: private members cannot be virtual.
    private void PrivateMethod()
    {
    }
    // Not transformed: static members are not virtualized.
    public static void StaticMethod()
    {
    }
    // Transformed.
    public virtual int PublicProperty { get; init; }
    // Not transformed.
    private int PrivateProperty { get; init; }
  }
  // The sealed modifier is removed from the record, and its members are virtualized.
  [Virtualize]
  internal record SealedRecord
  {
    // Transformed.
    public virtual void PublicMethod()
    {
    }
  }
  [Virtualize]
  internal abstract record AbstractRecord
  {
    // Not transformed: an abstract member is implicitly virtual (#98).
    public abstract void AbstractMethod();
    // Transformed.
    public virtual void ConcreteMethod()
    {
    }
  }
  // A positional record: the compiler-generated members must not be disturbed.
  [Virtualize]
  internal record PositionalRecord(int Value)
  {
    // Transformed.
    public virtual void PublicMethod()
    {
    }
  }
}