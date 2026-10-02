// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#pragma warning disable VSTHRD200, CA1822, CA2211, CS0067, CA1003
#pragma warning disable IDE0021 // Use block body for constructor - the expression body is part of the test.
#pragma warning disable IDE0051 // Remove unused private members - the token overloads exist so that the
                                // transformed code has something to bind to.

namespace Metalama.Community.AutoCancellationToken.UnitTests.NonMethodMembers;

// Regression test for #104: invocations inside members that are not methods used to reach the argument rewriter
// without an enclosing method, dereferencing a null parameter name and crashing the weaver.
[AutoCancellationToken]
internal class NonMethodMembers
{
    // Not transformed: an operator has no CancellationToken parameter to propagate.
    public static NonMethodMembers operator +( NonMethodMembers a, NonMethodMembers b )
    {
        Helper( a );

        return a;
    }

    // Not transformed.
    public static explicit operator string( NonMethodMembers a )
    {
        Helper( a );

        return string.Empty;
    }

    // Not transformed.
    public event EventHandler? Changed = CreateHandler();

    // Not transformed.
    public int Value => Compute();

    // Not transformed.
    public int this[int index] => Compute();

    // Not transformed.
    public NonMethodMembers() => Helper( this );

    private static void Helper( NonMethodMembers value ) { }

    private static void Helper( NonMethodMembers value, CancellationToken cancellationToken ) { }

    private static int Compute() => 0;

    private static int Compute( CancellationToken cancellationToken ) => 0;

    private static EventHandler? CreateHandler() => null;

    private static EventHandler? CreateHandler( CancellationToken cancellationToken ) => null;
}
