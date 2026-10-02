// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#pragma warning disable VSTHRD200, VSTHRD100

using System;
using System.Threading.Tasks;

#pragma warning disable CA1822  // Mark members as static
#pragma warning disable CS0649  // Field is never assigned to
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable SA1306  // Field should begin with lower-case letter
#pragma warning disable SA1401  // Field should be private

namespace Metalama.Community.Virtuosity.TestApp
{
    [Virtualize]
    internal class C : IDisposable
    {
        // Not transformed.
        private void ImplicitPrivate() { }

        // Not transformed.
        private void ExplicitPrivate() { }

        // Transformed.
        public void Public() { }

        // Not transformed (already virtual).
        public virtual void PublicVirtual() { }

        // Transformed.
        protected async void ProtectedAsync()
        {
            await Task.Yield();
        }

        // Transformed.
        private protected void PrivateProtected() { }

        // Transformed (sealed removed).
        public sealed override string ToString()
        {
            return "";
        }

        // Not transformed.
        public override int GetHashCode()
        {
            return 0;
        }

        // Not transformed.
        public static void PublicStatic() { }

        // Not transformed.
        void IDisposable.Dispose() { }

        // Transformed.
        public int Property { get; }

        // Not transformed.
        protected int Field;
    }
}