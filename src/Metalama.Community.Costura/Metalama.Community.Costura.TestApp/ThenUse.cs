// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Newtonsoft.Json;
using Soothsilver.Random;
using System;
using Xunit;

namespace Metalama.Community.Costura.TestApp;

internal static class ThenUse
{
    public static void Stuff()
    {
        var serializeObject = JsonConvert.SerializeObject( new[] { "he", "ha" } );
        var r = serializeObject + R.Next( 0, 1 );
        Assert.Equal( @"[""he"",""ha""]0", r );
        Console.WriteLine( "This is still working: " + r );
    }
}