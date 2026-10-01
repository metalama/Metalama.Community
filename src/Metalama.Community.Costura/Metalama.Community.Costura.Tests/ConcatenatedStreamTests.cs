// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Community.Costura.Weaver;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Metalama.Community.Costura.Tests;

/// <summary>
/// Tests for <see cref="ConcatenatedStream" />, the custom <see cref="Stream" /> used to feed every embedded
/// resource into a single hash computation.
/// </summary>
/// <remarks>
/// Custom Stream implementations are a classic source of partial-read and boundary bugs, and this one had no
/// coverage at all. It is the only unit in the weaver that can be tested from a run-time test project: everything
/// else there is <c>[CompileTime]</c>, so its method bodies are replaced by a throwing stub at run time.
/// </remarks>
public sealed class ConcatenatedStreamTests
{
    private static ConcatenatedStream Create( params byte[][] chunks )
        => new( chunks.Select( c => (Stream) new MemoryStream( c ) ).ToArray() );

    private static byte[] ReadToEnd( Stream stream, int bufferSize = 1024 )
    {
        using var output = new MemoryStream();
        var buffer = new byte[bufferSize];
        int read;

        while ( ( read = stream.Read( buffer, 0, buffer.Length ) ) > 0 )
        {
            output.Write( buffer, 0, read );
        }

        return output.ToArray();
    }

    [Fact]
    public void Read_ConcatenatesAllStreamsInOrder()
    {
        using var stream = Create( [1, 2, 3], [4, 5], [6] );

        Assert.Equal( new byte[] { 1, 2, 3, 4, 5, 6 }, ReadToEnd( stream ) );
    }

    [Fact]
    public void Read_SpanningStreamBoundary_ReturnsBytesFromBothStreams()
    {
        using var stream = Create( [1, 2, 3], [4, 5, 6] );

        var buffer = new byte[6];
        var read = stream.Read( buffer, 0, 6 );

        // A single Read that spans the boundary must fill the whole buffer, not stop at the end of the first stream.
        Assert.Equal( 6, read );
        Assert.Equal( new byte[] { 1, 2, 3, 4, 5, 6 }, buffer );
    }

    [Fact]
    public void Read_WithBufferLargerThanContent_ReturnsOnlyAvailableBytes()
    {
        using var stream = Create( [1, 2], [3] );

        var buffer = new byte[10];
        var read = stream.Read( buffer, 0, buffer.Length );

        Assert.Equal( 3, read );
        Assert.Equal( new byte[] { 1, 2, 3 }, buffer.Take( 3 ).ToArray() );
        Assert.Equal( 0, stream.Read( buffer, 0, buffer.Length ) );
    }

    [Fact]
    public void Read_SkipsEmptyStreams()
    {
        using var stream = Create( [1], [], [], [2] );

        Assert.Equal( new byte[] { 1, 2 }, ReadToEnd( stream ) );
    }

    [Fact]
    public void Read_WithAllStreamsEmpty_ReturnsZero()
    {
        using var stream = Create( [], [] );

        Assert.Equal( 0, stream.Read( new byte[4], 0, 4 ) );
    }

    [Fact]
    public void Read_WithNoStreams_ReturnsZero()
    {
        using var stream = new ConcatenatedStream( [] );

        Assert.Equal( 0, stream.Read( new byte[4], 0, 4 ) );
    }

    [Fact]
    public void Read_WithZeroCount_ReturnsZeroAndConsumesNothing()
    {
        using var stream = Create( [1, 2] );

        Assert.Equal( 0, stream.Read( new byte[4], 0, 0 ) );
        Assert.Equal( new byte[] { 1, 2 }, ReadToEnd( stream ) );
    }

    [Fact]
    public void Read_HonoursOffset()
    {
        using var stream = Create( [1, 2, 3] );

        var buffer = new byte[5];
        var read = stream.Read( buffer, 2, 3 );

        Assert.Equal( 3, read );
        Assert.Equal( new byte[] { 0, 0, 1, 2, 3 }, buffer );
    }

    [Fact]
    public void Read_FromStreamsThatReturnPartialReads_StillReturnsEverything()
    {
        // Stream.Read is allowed to return fewer bytes than requested. The implementation must keep reading rather
        // than treating a short read as end-of-stream.
        var streams = new Stream[] { new ChunkedStream( [1, 2, 3, 4], 1 ), new ChunkedStream( [5, 6], 1 ) };
        using var stream = new ConcatenatedStream( streams );

        Assert.Equal( new byte[] { 1, 2, 3, 4, 5, 6 }, ReadToEnd( stream ) );
    }

    [Fact]
    public void CopyTo_ProducesTheWholeContent()
    {
        using var stream = Create( [1, 2], [3, 4], [5] );
        using var target = new MemoryStream();

        stream.CopyTo( target );

        Assert.Equal( new byte[] { 1, 2, 3, 4, 5 }, target.ToArray() );
    }

    [Fact]
    public void ResetAllToZero_RewindsTheUnderlyingStreams()
    {
        // The wrapper itself is not reusable - its queue is drained by reading. The point of ResetAllToZero is to
        // rewind the underlying streams so the caller can read them again, which is what ResourceHash relies on
        // after computing the hash.
        var inner = new[] { new MemoryStream( [1, 2] ), new MemoryStream( [3] ) };
        using var stream = new ConcatenatedStream( inner.Cast<Stream>().ToArray() );

        _ = ReadToEnd( stream );

        Assert.All( inner, s => Assert.Equal( s.Length, s.Position ) );

        stream.ResetAllToZero();

        Assert.All( inner, s => Assert.Equal( 0, s.Position ) );
    }

    [Fact]
    public void Capabilities_AreReadOnly()
    {
        using var stream = Create( [1] );

        Assert.True( stream.CanRead );
        Assert.False( stream.CanSeek );
        Assert.False( stream.CanWrite );
    }

    [Fact]
    public void UnsupportedMembers_ThrowNotSupportedException()
    {
        using var stream = Create( [1] );

        Assert.Throws<NotSupportedException>( () => stream.Length );
        Assert.Throws<NotSupportedException>( () => stream.Position );
        Assert.Throws<NotSupportedException>( () => stream.Position = 0 );
        Assert.Throws<NotSupportedException>( () => stream.Seek( 0, SeekOrigin.Begin ) );
        Assert.Throws<NotSupportedException>( () => stream.SetLength( 0 ) );
        Assert.Throws<NotSupportedException>( () => stream.Write( new byte[1], 0, 1 ) );
        Assert.Throws<NotSupportedException>( stream.Flush );
    }

    /// <summary>
    /// A stream that never returns more than <c>maxBytesPerRead</c> bytes from a single <see cref="Read" /> call,
    /// to exercise partial-read handling.
    /// </summary>
    private sealed class ChunkedStream : MemoryStream
    {
        private readonly int _maxBytesPerRead;

        public ChunkedStream( byte[] buffer, int maxBytesPerRead ) : base( buffer )
        {
            this._maxBytesPerRead = maxBytesPerRead;
        }

        public override int Read( byte[] buffer, int offset, int count )
            => base.Read( buffer, offset, Math.Min( count, this._maxBytesPerRead ) );
    }
}
