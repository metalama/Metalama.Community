// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.IO;
using System.Reflection;

namespace Metalama.Community.Costura.ResolveRecursionTestApp;

/// <summary>
/// Standalone reproduction of https://github.com/metalama/Metalama.Community/issues/113: the Costura
/// <c>AppDomain.AssemblyResolve</c> handler called <c>Assembly.GetName()</c>, which issues a path-discovery security
/// demand on the assembly code base. When the demand is refused, the security engine loads the localized text of the
/// <c>Security_Generic</c> resource key. On a non-English UI culture that loads a satellite assembly, which raises
/// <c>AssemblyResolve</c> again, so the handler recurses. mscorlib detects the recursive lookup of the same resource
/// key and kills the process with <c>COR_E_FAILFAST</c>.
/// </summary>
internal static class Program
{
    private static int Main( string[] args )
    {
        if ( args.Length > 0 && args[0] == ChildArgument )
        {
            return Repro.Run();
        }

        return RunParent();
    }

    internal const string ChildArgument = "--child";

    private static int RunParent()
    {
        Console.WriteLine( "Costura assembly-resolve recursion test (issue #113)." );

        var directory = CreateIsolatedCopy( out var childPath );

        try
        {
            Console.WriteLine( $"Running the reproduction in a child process: {childPath}" );
            Console.WriteLine( "----------------------------------------------------------------" );

            var result = IsolatedProcess.Run( childPath, ChildArgument, TimeSpan.FromSeconds( 30 ) );

            Console.WriteLine( "----------------------------------------------------------------" );

            if ( result.TimedOut )
            {
                // mscorlib calls System.Diagnostics.Assert.Fail before Environment.FailFast, which opens a modal dialog
                // on a visible window station. The child process runs on a desktop of its own, so nobody sees the dialog
                // and the child waits on it until this test kills it.
                Console.WriteLine(
                    "FAILED: the child process did not exit. It is waiting on the dialog below, which is how mscorlib "
                    + "reports '[mscorlib recursive resource lookup bug]' before it calls Environment.FailFast. The "
                    + "Costura resolve handler re-entered itself through Assembly.GetName(). This is issue #113." );

                Console.WriteLine();
                Console.WriteLine( result.DialogText ?? "(no window found on the desktop of the child process)" );

                return 1;
            }

            switch ( result.ExitCode )
            {
                case 0:
                    Console.WriteLine( "PASSED: the embedded assembly was resolved without re-entering the resolve handler." );

                    return 0;

                case _corFailFast:
                    Console.WriteLine(
                        "FAILED: the child process died with COR_E_FAILFAST (0x80131623), which is how mscorlib reports "
                        + "'Infinite recursion during resource lookup within mscorlib'. The Costura resolve handler "
                        + "re-entered itself through Assembly.GetName(). This is issue #113." );

                    return 1;

                default:
                    Console.WriteLine( $"FAILED: the child process exited with the unexpected code 0x{result.ExitCode:X8}." );

                    return 1;
            }
        }
        finally
        {
            DeleteIsolatedCopy( directory );
        }
    }

    private const uint _corFailFast = 0x80131623;

    /// <summary>
    /// Copies this executable alone into a directory of its own, so that its dependencies can only be resolved from
    /// the resources that Costura embedded into it. The directory is under the temporary directory rather than under
    /// the output directory, because the latter gives paths longer than <c>MAX_PATH</c>. Its name is unique, so that
    /// concurrent runs of this test cannot overwrite or lock each other's copy. Returns the directory and sets
    /// <paramref name="childPath"/> to the copy of this executable in it.
    /// </summary>
    private static string CreateIsolatedCopy( out string childPath )
    {
        var thisPath = new Uri( Assembly.GetExecutingAssembly().CodeBase! ).LocalPath;

        // Path.GetRandomFileName gives a short unique name, which keeps the child path below MAX_PATH.
        var directory = Path.Combine( Path.GetTempPath(), "CosturaResolveRecursionTest-" + Path.GetRandomFileName() );

        Directory.CreateDirectory( directory );

        childPath = Path.Combine( directory, Path.GetFileName( thisPath ) );
        File.Copy( thisPath, childPath, true );

        var configPath = thisPath + ".config";

        if ( File.Exists( configPath ) )
        {
            File.Copy( configPath, childPath + ".config", true );
        }

        return directory;
    }

    /// <summary>
    /// Removes the directory created by <see cref="CreateIsolatedCopy"/>. A failure to remove it does not fail the
    /// test, because the test result is already known at this point.
    /// </summary>
    private static void DeleteIsolatedCopy( string directory )
    {
        try
        {
            Directory.Delete( directory, true );
        }
        catch ( IOException e )
        {
            Console.WriteLine( $"Warning: could not delete '{directory}': {e.Message}" );
        }
        catch ( UnauthorizedAccessException e )
        {
            Console.WriteLine( $"Warning: could not delete '{directory}': {e.Message}" );
        }
    }
}
