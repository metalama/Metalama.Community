// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using PostSharp.Engineering.BuildTools;
using PostSharp.Engineering.BuildTools.Build;
using PostSharp.Engineering.BuildTools.Build.Model;
using PostSharp.Engineering.BuildTools.Build.Solutions;
using PostSharp.Engineering.BuildTools.Dependencies.Definitions;
using PostSharp.Engineering.BuildTools.Docker;
using System;
using MetalamaDependencies = PostSharp.Engineering.BuildTools.Dependencies.Definitions.MetalamaDependencies.V2027_0;

// The .NET 11 SDK, which global.json names as the main SDK of the product and which the build agent installs. The
// version is a literal instead of a member of the product family, because the .NET 11 SDK is a prerelease and
// PostSharp.Engineering names only released feature bands. Keep it equal to the constant of the same name in the
// Metalama repository, and move both to MetalamaDependencies.Family.PreferredVersions.DotNetSdk once the .NET 11
// SDK is released.
const string dotNet11SdkVersion = "11.0.100-rc.1.26425.128";

// The .NET 10 SDK, which stays installed beside the .NET 11 one, because the build tool of this repository targets
// net10.0 and the .NET 11 SDK carries no .NET 10 runtime. The version comes from the product family, so that it
// matches the feature band that the Visual Studio version of the family installs.
var dotNet10SdkVersion = MetalamaDependencies.Family.PreferredVersions.DotNetSdk.V_10_0;

var product = new Product( MetalamaDependencies.MetalamaCommunity )
{
    OverriddenBuildAgentRequirements = new ContainerRequirements( ContainerHostKind.Windows )
    {
        Components =
        [
            new DotNetComponent( dotNet11SdkVersion, DotNetComponentKind.Sdk ),
            new DotNetComponent( dotNet10SdkVersion, DotNetComponentKind.Sdk ),
        ]
    },
    GenerateNuGetConfig = true,
    DotNetSdkVersion = new DotNetSdkVersion( dotNet11SdkVersion ) { AllowPrerelease = true },
    MSBuildVersion = new Version( 17, 14 ),
    
    Solutions =
    [
        new DotNetSolution( "Metalama.Community.sln" ) { CanFormatCode = true },

        // A standalone test: it consumes the produced NuGet package instead of a project reference, so it covers
        // the packaging itself - analyzer wiring, dependencies, .props/.targets - which the main solution cannot.
        new DotNetSolution(
            @"src\Metalama.Community.Virtuosity\Metalama.Community.Virtuosity.TestApp\Metalama.Community.Virtuosity.TestApp.sln" )
        {
            IsTestOnly = true
        },

        // A standalone test for issue #113: it runs a Costura-woven application whose assembly-resolve handler used
        // to recurse into itself. See the README.md next to the solution.
        new DotNetSolution(
            @"src\Metalama.Community.Costura\Metalama.Community.Costura.ResolveRecursionTestApp\Metalama.Community.Costura.ResolveRecursionTestApp.sln" )
        {
            IsTestOnly = true
        }
    ],
    PublicArtifacts = Pattern.Create(
        "Metalama.Community.AutoCancellationToken.$(PackageVersion).nupkg",
        "Metalama.Community.Costura.$(PackageVersion).nupkg",
        "Metalama.Community.Costura.Redist.$(PackageVersion).nupkg",
        "Metalama.Community.Virtuosity.$(PackageVersion).nupkg" ),
};

return new EngineeringApp( product ).Run( args );