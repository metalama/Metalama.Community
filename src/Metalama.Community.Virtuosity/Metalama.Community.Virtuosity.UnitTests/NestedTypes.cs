// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Community.Virtuosity.TestApp
{
    [Virtualize]
    internal class OuterClass
    {
        internal class NestedClass
        {
            // Transformed.
            public void M() { }
        }

        internal struct NestedStruct
        {
            // Not transformed.
            public void M() { }
        }

        internal interface INestedInterface
        {
            // Not transformed.
            void M() { }
        }
    }
}