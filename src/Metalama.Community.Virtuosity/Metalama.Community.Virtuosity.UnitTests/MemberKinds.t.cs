using System;
namespace Metalama.Community.Virtuosity.Tests.MemberKinds
{
  // Pins which member kinds the weaver virtualizes. Only methods and properties are visited, so indexers and
  // events are left alone. See Details.md.
  [Virtualize]
  internal class MemberKinds
  {
    // Transformed.
    public virtual void PublicMethod()
    {
    }
    // Transformed.
    internal virtual void InternalMethod()
    {
    }
    // Transformed.
    protected internal virtual void ProtectedInternalMethod()
    {
    }
    // Transformed: private protected contains the protected keyword, which is one of the required modifiers.
    private protected virtual void PrivateProtectedMethod()
    {
    }
    // Transformed.
    public virtual int PublicProperty { get; set; }
    // Not transformed: indexers are not visited by the weaver.
    public int this[int index] => index;
    // Not transformed: field-like events are not visited by the weaver.
    public event EventHandler? FieldLikeEvent;
    // Not transformed: events with accessors are not visited either.
    public event EventHandler? AccessorEvent
    {
      add
      {
      }
      remove
      {
      }
    }
    // Not transformed: an operator cannot be virtual.
    public static MemberKinds operator +(MemberKinds a, MemberKinds b) => a;
    // Not transformed: a constructor cannot be virtual.
    public MemberKinds()
    {
    }
    // The sealed modifier is removed even from a member that is not virtualized, so a member the author
    // deliberately sealed becomes overridable again. See Details.md.
    public override string ToString() => string.Empty;
  }
  [Virtualize]
  internal class SealedClass
  {
    // The sealed modifier is removed from the class, so its members can be virtualized.
    public virtual void PublicMethod()
    {
    }
  }
}