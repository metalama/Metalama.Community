// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Options;
using System;

namespace Metalama.Community.Costura;

#pragma warning disable SA1623

[PublicAPI]
public sealed class CosturaOptions : IHierarchicalOptions<ICompilation>
{
    // Intentionally not initialized to empty because a default value means that all assemblies are to be included unless they are excluded.

    /// <summary>
    ///     Gets or sets a value indicating whether the <c>.pdb</c> files should also be embedded. The default value is
    ///     <c>true</c>.
    /// </summary>
    public bool? IncludeDebugSymbols { get; init; }

    /// <summary>
    ///     Gets or sets a value indicating whether resources embedded into the main assembly should be compressed. The default
    ///     value is <c>true</c>.
    /// </summary>
    public bool? CompressResources { get; init; }

    /// <summary>
    ///     Gets or sets a value indicating whether
    ///     embedded assemblies are placed in the output folder anyway, even
    ///     though they aren't necessary anymore.
    /// </summary>
    public bool? IsCleanupDisabled
    {
        get;

        [Obsolete( "This option does not work." )]
        init;
    }

    /// <summary>
    ///     Gets or sets a value indicating whether embedded assemblies should be copied to disk before loading them into
    ///     memory. This is helpful for some scenarios that expected an assembly to be loaded from a physical file.
    ///     For example, if some code checks the assembly's assembly location. The default value is <c>false</c>.
    /// </summary>
    public bool? CreateTemporaryAssemblies { get; init; }

    /// <summary>
    ///     Gets or sets a value indicating whether satellite assemblies (typically containing localized resources,
    ///     i.e. with a name like 'resources.dll') should be included, The default value is <c>true</c>.
    /// </summary>
    public bool? IncludeSatelliteAssemblies { get; init; }

    /// <summary>
    ///     Gets or sets the lists of assembly names to embed. The assembly name should not include the <c>.exe</c> or
    ///     <c>.dll</c> extension.
    ///     The names can include wildcards at the end of the name for partial matching. For example <c>System.*</c> will
    ///     exclude all assemblies that start with System..
    ///     By default, all references with Copy Local set to <c>true</c> are embedded.
    /// </summary>
    public IncrementalHashSet<string> IncludedAssemblies { get; init; } = IncrementalHashSet.Empty<string>();

    /// <summary>
    ///     Gets or sets the list of assembly names to exclude from embedding. The assembly name should not include the
    ///     <c>.exe</c> or  <c>.dll</c> extension.
    ///     The names can include wildcards at the end of the name for partial assembly name matching. For example
    ///     <c>System.*</c> will exclude all assemblies that start with System..
    /// </summary>
    public IncrementalHashSet<string> ExcludedAssemblies { get; init; } = IncrementalHashSet.Empty<string>();

    /// <summary>
    ///     Gets or sets the list of mixed-mode assemblies. These assemblies must be loaded differently than the other.
    /// </summary>
    public IncrementalKeyedCollection<string, UnmanagedAssembly> UnmanagedAssemblies { get; init; } =
        IncrementalKeyedCollection.Empty<string, UnmanagedAssembly>();

    /// <summary>
    ///     Gets or sets the list of native libraries can be loaded by this add-in automatically.
    ///     To include a native library include it in your project as an
    ///     Embedded Resource in a folder called Costura32 or Costura64
    ///     depending on the bitness of the library.
    ///     Optionally you can also specify the order that preloaded
    ///     libraries are loaded. When using temporary assemblies
    ///     from disk mixed mode assemblies are also preloaded.
    /// </summary>
    public IncrementalKeyedCollection<string, PreloadedLibrary> PreloadedLibraries { get; init; } =
        IncrementalKeyedCollection.Empty<string, PreloadedLibrary>();

    public object ApplyChanges( object changes, in ApplyChangesContext context )
    {
        var otherOptions = (CosturaOptions) changes;

        return new CosturaOptions()
        {
            CompressResources = otherOptions.CompressResources ?? this.CompressResources,
            IncludedAssemblies = this.IncludedAssemblies.ApplyChangesSafe( otherOptions.IncludedAssemblies, context )!,
            IncludeSatelliteAssemblies = otherOptions.IncludeSatelliteAssemblies ?? this.IncludeSatelliteAssemblies,
            UnmanagedAssemblies = this.UnmanagedAssemblies.ApplyChangesSafe( otherOptions.UnmanagedAssemblies, context )!,
            ExcludedAssemblies = this.ExcludedAssemblies.ApplyChangesSafe( otherOptions.ExcludedAssemblies, context )!,
            PreloadedLibraries = this.PreloadedLibraries.ApplyChangesSafe( otherOptions.PreloadedLibraries, context )!,
            CreateTemporaryAssemblies = otherOptions.CreateTemporaryAssemblies ?? this.CreateTemporaryAssemblies,
            IncludeDebugSymbols = otherOptions.IncludeDebugSymbols ?? this.IncludeDebugSymbols,
#pragma warning disable CS0618 // Type or member is obsolete
            IsCleanupDisabled = otherOptions.IsCleanupDisabled ?? this.IsCleanupDisabled
#pragma warning restore CS0618 // Type or member is obsolete
        };
    }

    public IHierarchicalOptions GetDefaultOptions( OptionsInitializationContext context )
        => new CosturaOptions() { CompressResources = true, IncludeSatelliteAssemblies = true, IncludeDebugSymbols = true };
}