// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Metalama.Community.Costura.Tests;

/// <summary>
/// Gives the tests access to the runtime templates that the weaver injects into the user's compilation. The templates
/// are <c>const string</c> fields of the internal <c>Resources</c> class of the weaver assembly, read here by
/// reflection so that the tests cover exactly what the weaver injects.
/// </summary>
internal static class RuntimeTemplates
{
    public static IEnumerable<string> Names => GetFields().Select( f => f.Name );

    public static string GetSource( string templateName )
        => (string) GetFields().Single( f => f.Name == templateName ).GetValue( null )!;

    private static IEnumerable<FieldInfo> GetFields()
    {
        var resourcesType = Assembly.Load( "Metalama.Community.Costura.Weaver" )
            .GetType( "Metalama.Community.Costura.Weaver.Resources" );

        Assert.NotNull( resourcesType );

        return resourcesType!
            .GetFields( BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static )
            .Where( f => f.IsLiteral && f.FieldType == typeof(string) )
            .OrderBy( f => f.Name );
    }
}
