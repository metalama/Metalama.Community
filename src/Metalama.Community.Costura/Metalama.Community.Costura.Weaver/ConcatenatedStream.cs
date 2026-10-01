// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using System;
using System.Collections.Generic;
using System.IO;

namespace Metalama.Community.Costura.Weaver;

/// <summary>
///     Multiple streams rolled into one. Read-only. Comes from https://stackoverflow.com/a/3879231/1580088.
/// </summary>
[RunTimeOrCompileTime]
internal sealed class ConcatenatedStream : Stream
{
    private readonly Stream[] _allStreams;
    private readonly Queue<Stream> _streams;

    public ConcatenatedStream( Stream[] streams )
    {
        this._allStreams = streams;
        this._streams = new Queue<Stream>( streams );
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public void ResetAllToZero()
    {
        foreach ( var stream in this._allStreams )
        {
            stream.Position = 0;
        }
    }

    public override int Read( byte[] buffer, int offset, int count )
    {
        var totalBytesRead = 0;

        while ( count > 0 && this._streams.Count > 0 )
        {
            var bytesRead = this._streams.Peek().Read( buffer, offset, count );

            if ( bytesRead == 0 )
            {
                this._streams.Dequeue();

                continue;
            }

            totalBytesRead += bytesRead;
            offset += bytesRead;
            count -= bytesRead;
        }

        return totalBytesRead;
    }

    public override void Flush()
    {
        throw new NotSupportedException();
    }

    public override long Seek( long offset, SeekOrigin origin )
    {
        throw new NotSupportedException();
    }

    public override void SetLength( long value )
    {
        throw new NotSupportedException();
    }

    public override void Write( byte[] buffer, int offset, int count )
    {
        throw new NotSupportedException();
    }
}