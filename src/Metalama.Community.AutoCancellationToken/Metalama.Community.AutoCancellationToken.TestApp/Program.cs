// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Community.AutoCancellationToken.TestApp;

internal static class Program
{
    private static async Task Main()
    {
        var cts = new CancellationTokenSource( TimeSpan.FromSeconds( 6 ) );

        try
        {
            await MyClass.MakeRequests( cts.Token );
        }
        catch ( Exception ex )
        {
            Console.WriteLine( ex.Message );
        }
    }
}