// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#pragma warning disable VSTHRD200

namespace Metalama.Community.AutoCancellationToken.UnitTests;

[AutoCancellationToken]
internal class MyClass
{
    public static async Task MakeRequests( CancellationToken ct )
    {
        using var client = new HttpClient();

        // After transformation, the call to MakeRequest should include a CancellationToken.

        await MakeRequest( client );
        Console.WriteLine( "request 1 succeeded" );
        await MakeRequest( client );
        Console.WriteLine( "request 2 succeeded" );
    }

    public static async Task MakeRequests_MultipleCtParams( CancellationToken ct1, CancellationToken ct2 )
    {
        using var client = new HttpClient();

        // After transformation, the call to MakeRequest should *not* include a CancellationToken, because
        // the containing method has two or more CancellationToken parameters.

        await MakeRequest( client );
        Console.WriteLine( "request 1 succeeded" );
        await MakeRequest( client );
        Console.WriteLine( "request 2 succeeded" );
    }

    public static async Task MakeRequests_StaticLocal( CancellationToken ct1 )
    {
        await StaticLocalMakeRequest();

        static async Task StaticLocalMakeRequest()
        {
            using var client = new HttpClient();

            // After transformation, the call to MakeRequest should *not* include a CancellationToken, because
            // the containing method is a static local function (this is the documented behaviour).

            await MakeRequest( client );
            Console.WriteLine( "request 1 succeeded" );
            await MakeRequest( client );
            Console.WriteLine( "request 2 succeeded" );
        }
    }

    // After transformation, MakeRequest should contain a CancellationToken parameter, and the call to client.GetAsync
    // should include this argument.
    private static async Task MakeRequest( HttpClient client )
    {
        await client.GetAsync( "https://httpbin.org/delay/5" );
    }

    public static async Task MakeRequests_FinalParamterIsParamsArray( CancellationToken ct )
    {
        using var client = new HttpClient();

        // After transformation, the call to MakeRequest_FinalParamterIsParamsArray should *not* include a CancellationToken
        // because the final parameter is params object[].

        await MakeRequest_FinalParamterIsParamsArray( client );
        Console.WriteLine( "request 1 succeeded" );
        await MakeRequest_FinalParamterIsParamsArray( client );
        Console.WriteLine( "request 2 succeeded" );
    }

    // This method should not be transformed because adding a parameter after the existing
    // final params object[] parameter is not valid C#.
    private static async Task MakeRequest_FinalParamterIsParamsArray( HttpClient client, params object[] args )
    {
        await client.GetAsync( "https://httpbin.org/delay/5" );
    }

    public static async Task MakeRequests_FinalParameterOfAvailableOverloadIsGeneric( CancellationToken ct )
    {
        using var client = new HttpClient();

        // After transformation, the call to Helper1.MakeRequest_Generic should *not* include a CancellationToken
        // because the final parameter of the available overload in class Helper1 is generic.

        await Helper1.MakeRequest_Generic( client );
        Console.WriteLine( "request 1 succeeded" );
        await Helper1.MakeRequest_Generic( client );
        Console.WriteLine( "request 2 succeeded" );
    }

    public static async Task MakeRequests_ExistingOverload( CancellationToken ct )
    {
        using var client = new HttpClient();

        // After transformation, the call to Helper2.MakeRequest should include a CancellationToken
        // because a compatible overload already exists in class Helper2.

        await Helper2.MakeRequest( client );
        Console.WriteLine( "request 1 succeeded" );
        await Helper2.MakeRequest( client );
        Console.WriteLine( "request 2 succeeded" );
    }

    // After transformation, MakeRequest should contain a CancellationToken parameter, and the call to client.GetAsync
    // should include this argument. The weaver must choose a name other than "cancellationToken" for the added parameter.
    public static async Task MakeRequest_ParameterNameCollision( HttpClient client, int cancellationToken )
    {
        await client.GetAsync( "https://httpbin.org/delay/5" );
    }
}

#pragma warning disable SA1402 // FileMayOnlyContainASingleType

// Not transformed
internal static class Helper1
{
    public static async Task MakeRequest_Generic( HttpClient client )
    {
        await client.GetAsync( "https://httpbin.org/delay/5" );
    }

    public static async Task MakeRequest_Generic<T>( HttpClient client, T arg )
    {
        await client.GetAsync( "https://httpbin.org/delay/5" );
    }
}

// Not transformed
internal static class Helper2
{
    public static async Task MakeRequest( HttpClient client )
    {
        await client.GetAsync( "https://httpbin.org/delay/5" );
    }

    public static async Task MakeRequest( HttpClient client, CancellationToken ct )
    {
        await client.GetAsync( "https://httpbin.org/delay/5", ct );
    }
}