// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Options;

namespace Metalama.Community.Costura;

[CompileTime]
[PublicAPI]
public sealed class PreloadedLibrary : IIncrementalKeyedCollectionItem<string>
{
    public PreloadedLibrary( string name, int priority )
    {
        this.Name = name;
        this.Priority = priority;
    }

    public object ApplyChanges( object changes, in ApplyChangesContext context ) => new PreloadedLibrary( this.Name, ((PreloadedLibrary) changes).Priority );

    public string Name { get; }

    public int Priority { get; }

    string IIncrementalKeyedCollectionItem<string>.Key => this.Name;
}