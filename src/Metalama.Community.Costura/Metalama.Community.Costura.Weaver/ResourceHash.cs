// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Compiler;
using Metalama.Framework.Aspects;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Metalama.Community.Costura.Weaver;

[CompileTime]
internal static class ResourceHash
{
    public static string CalculateHash( List<ManagedResource> resources )
    {
        var data = resources
            .OrderBy( r => r.Name )
            .Where( r => r.Name.StartsWith( "Costura", StringComparison.Ordinal ) )
            .Select( r => r.DataProvider!.Invoke() )
            .ToArray();

        var allStream = new ConcatenatedStream( data );

#pragma warning disable CA5351
        using var md5 = MD5.Create();
#pragma warning restore CA5351
        var hashBytes = md5.ComputeHash( allStream );

        var sb = new StringBuilder();

        foreach ( var t in hashBytes )
        {
            sb.Append( t.ToString( "X2", CultureInfo.InvariantCulture ) );
        }

        allStream.ResetAllToZero();

        return sb.ToString();
    }
}