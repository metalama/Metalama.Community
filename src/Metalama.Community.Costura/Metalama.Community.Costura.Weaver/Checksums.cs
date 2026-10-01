// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Metalama.Community.Costura.Weaver;

[CompileTime]
internal sealed class Checksums
{
    private readonly Dictionary<string, string> _checksums = [];

    public IReadOnlyDictionary<string, string> AllChecksums => this._checksums;

    public static string CalculateChecksum( string filename )
    {
        using var fs = new FileStream( filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite );

        return CalculateChecksum( fs );
    }

    private static string CalculateChecksum( Stream stream )
    {
        using var bs = new BufferedStream( stream );
#pragma warning disable CA5350
        using var sha1 = new SHA1CryptoServiceProvider();
#pragma warning restore CA5350

        var hash = sha1.ComputeHash( bs );
        var formatted = new StringBuilder( 2 * hash.Length );

        foreach ( var b in hash )
        {
            formatted.AppendFormat( CultureInfo.InvariantCulture, "{0:X2}", b );
        }

        return formatted.ToString();
    }

    public void Add( string resourceName, string checksum ) => this._checksums.Add( resourceName, checksum );
}