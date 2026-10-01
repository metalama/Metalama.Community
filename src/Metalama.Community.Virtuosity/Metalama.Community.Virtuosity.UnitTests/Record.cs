// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#pragma warning disable CA1822 // Mark members as static
#pragma warning disable IDE0051 // Remove unused private members - the private members are here to show that they are not virtualized.

namespace Metalama.Community.Virtuosity.Tests.Record
{
    // Records are named in the README and VisitRecordDeclaration handles them specially, but they had no coverage.
    [Virtualize]
    internal record Record
    {
        // Transformed.
        public void PublicMethod() { }

        // Transformed.
        protected void ProtectedMethod() { }

        // Transformed.
        internal void InternalMethod() { }

        // Not transformed: private members cannot be virtual.
        private void PrivateMethod() { }

        // Not transformed: static members are not virtualized.
        public static void StaticMethod() { }

        // Transformed.
        public int PublicProperty { get; init; }

        // Not transformed.
        private int PrivateProperty { get; init; }
    }

    // The sealed modifier is removed from the record, and its members are virtualized.
    [Virtualize]
    internal sealed record SealedRecord
    {
        // Transformed.
        public void PublicMethod() { }
    }

    [Virtualize]
    internal abstract record AbstractRecord
    {
        // Not transformed: an abstract member is implicitly virtual (#98).
        public abstract void AbstractMethod();

        // Transformed.
        public void ConcreteMethod() { }
    }

    // A positional record: the compiler-generated members must not be disturbed.
    [Virtualize]
    internal record PositionalRecord( int Value )
    {
        // Transformed.
        public void PublicMethod() { }
    }
}
