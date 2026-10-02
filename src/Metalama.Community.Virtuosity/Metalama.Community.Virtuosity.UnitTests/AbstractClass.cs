// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#pragma warning disable CA1822 // Mark members as static

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
        public void ConcreteMethod() { }

        // Transformed.
        public int ConcreteProperty { get; }
    }
}
