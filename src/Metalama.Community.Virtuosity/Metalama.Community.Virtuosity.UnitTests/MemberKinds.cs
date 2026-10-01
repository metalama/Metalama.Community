// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#pragma warning disable CA1822 // Mark members as static
#pragma warning disable CS0067 // Event is never used
#pragma warning disable CA1003 // Use generic event handler instances
#pragma warning disable CA1070 // Do not declare event fields as virtual

using System;

namespace Metalama.Community.Virtuosity.Tests.MemberKinds
{
    // Pins which member kinds the weaver virtualizes. Only methods and properties are visited, so indexers and
    // events are left alone. See Details.md.
    [Virtualize]
    internal class MemberKinds
    {
        // Transformed.
        public void PublicMethod() { }

        // Transformed.
        internal void InternalMethod() { }

        // Transformed.
        protected internal void ProtectedInternalMethod() { }

        // Transformed: private protected contains the protected keyword, which is one of the required modifiers.
        private protected void PrivateProtectedMethod() { }

        // Transformed.
        public int PublicProperty { get; set; }

        // Not transformed: indexers are not visited by the weaver.
        public int this[int index] => index;

        // Not transformed: field-like events are not visited by the weaver.
        public event EventHandler? FieldLikeEvent;

        // Not transformed: events with accessors are not visited either.
        public event EventHandler? AccessorEvent
        {
            add { }
            remove { }
        }

        // Not transformed: an operator cannot be virtual.
        public static MemberKinds operator +( MemberKinds a, MemberKinds b ) => a;

        // Not transformed: a constructor cannot be virtual.
        public MemberKinds() { }

        // The sealed modifier is removed even from a member that is not virtualized, so a member the author
        // deliberately sealed becomes overridable again. See Details.md.
        public sealed override string ToString() => string.Empty;
    }

    [Virtualize]
    internal sealed class SealedClass
    {
        // The sealed modifier is removed from the class, so its members can be virtualized.
        public void PublicMethod() { }
    }
}
