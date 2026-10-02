// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Options;

namespace Metalama.Community.Costura;

[CompileTime]
[PublicAPI]
public class UnmanagedAssembly : IIncrementalKeyedCollectionItem<string>
{
    public UnmanagedAssembly( string name, UnmanagedAssemblyPlatform platform )
    {
        this.Name = name;
        this.Platform = platform;
    }

    public string Name { get; }

    public UnmanagedAssemblyPlatform Platform { get; }

    public object ApplyChanges( object changes, in ApplyChangesContext context ) => new UnmanagedAssembly( this.Name, ((UnmanagedAssembly) changes).Platform );

    string IIncrementalKeyedCollectionItem<string>.Key => this.Name;
}