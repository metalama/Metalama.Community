// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using System.Threading;

namespace Metalama.Community.Costura.ResolveRecursionTestApp;

/// <summary>
/// The reproduction itself. It runs in a child process because the failure it provokes kills the process.
/// </summary>
internal static class Repro
{
    public static int Run()
    {
        // The security engine reports a refused demand with the localized text of the Security_Generic resource key.
        // A UI culture other than the neutral one of mscorlib makes that lookup load a satellite assembly, which is
        // what raises AssemblyResolve a second time.
        Thread.CurrentThread.CurrentUICulture = new CultureInfo( "de-DE" );
        Console.WriteLine( $"UI culture: {CultureInfo.CurrentUICulture.Name}" );

        // A permission set that grants everything the Costura resolve handler needs, but no FileIOPermission.
        // Assembly.GetName() demands FileIOPermissionAccess.PathDiscovery on the assembly code base, so the demand is
        // refused here. Any PermitOnly or Deny frame on the stack has this effect; the process is fully trusted.
        var permitted = new PermissionSet( PermissionState.None );
        permitted.AddPermission( new SecurityPermission( PermissionState.Unrestricted ) );
        permitted.AddPermission( new ReflectionPermission( PermissionState.Unrestricted ) );

        Console.WriteLine( "Restricting the stack to a permission set without FileIOPermission." );

        string json;

        permitted.PermitOnly();

        try
        {
            // Newtonsoft.Json is not next to the executable, so the first call into it raises AssemblyResolve and
            // Costura answers it from the embedded resources.
            json = UseEmbeddedAssembly();
        }
        finally
        {
            CodeAccessPermission.RevertPermitOnly();
        }

        Console.WriteLine( $"The embedded assembly was resolved and used: {json}" );

        return 0;
    }

    [MethodImpl( MethodImplOptions.NoInlining )]
    private static string UseEmbeddedAssembly() => JsonConvert.SerializeObject( new[] { "he", "ha" } );
}
