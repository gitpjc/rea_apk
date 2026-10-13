using System.Buffers.Binary;

namespace Iamp.Protocol;

public sealed record ProtocolFrame(byte[] Payload);

/// <summary>Observed F15i V1.1.2 framing. Command semantics are deliberately outside this codec.</summary>
public static class FrameCodec
{
    // A defensive local budget, not a claim about the device's maximum frame size.
    public const int MaximumPayload = 8192;

    public static byte[] Encode(ReadOnlySpan<byte> payload)
    {
        if (payload.Length is < 1 or > MaximumPayload)
            throw new ArgumentOutOfRangeException(nameof(payload));
        var bytes = new byte[payload.Length + 6];
        bytes[0] = 0xAA;
        bytes[1] = 0x55;
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(2), (ushort)payload.Length);
        payload.CopyTo(bytes.AsSpan(4));
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(bytes.Length - 2), ComputeChecksum(bytes.AsSpan(2, bytes.Length - 4)));
        return bytes;
    }

    public static ProtocolFrame Decode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 7 || bytes[0] != 0xAA || bytes[1] != 0x55)
            throw new FormatException("Invalid frame marker or minimum size.");
        var length = BinaryPrimitives.ReadUInt16LittleEndian(bytes[2..]);
        if (length is < 1 or > MaximumPayload || bytes.Length != length + 6)
            throw new FormatException("Invalid or incomplete frame length.");
        if (ComputeChecksum(bytes.Slice(2, length + 2)) != BinaryPrimitives.ReadUInt16BigEndian(bytes[^2..]))
            throw new FormatException("Frame checksum mismatch.");
        return new ProtocolFrame(bytes.Slice(4, length).ToArray());
    }

    public static ushort ComputeChecksum(ReadOnlySpan<byte> lengthAndPayload)
    {
        ushort crc = 0;
        foreach (var value in lengthAndPayload)
        {
            crc ^= (ushort)(value << 8);
            for (var bit = 0; bit < 8; bit++)
                crc = (ushort)((crc & 0x8000) != 0 ? (crc << 1) ^ 0x1021 : crc << 1);
        }
        return (ushort)(crc ^ 0xFFFF);
    }
}

/// <summary>Reassembles fragmented/coalesced notifications within a fixed local memory budget.</summary>
public sealed class FrameStreamDecoder
{
    private readonly List<byte> buffer = [];
    public int BufferedBytes => buffer.Count;
    public int RejectedCandidates { get; private set; }

    public IReadOnlyList<ProtocolFrame> Push(ReadOnlySpan<byte> data)
    {
        var frames = new List<ProtocolFrame>();
        foreach (var value in data)
        {
            buffer.Add(value);
            while (buffer.Count > 0)
            {
                if (buffer[0] != 0xAA || (buffer.Count > 1 && buffer[1] != 0x55))
                {
                    buffer.RemoveAt(0);
                    continue;
                }
                if (buffer.Count < 4) break;
                var length = buffer[2] | buffer[3] << 8;
                if (length is < 1 or > FrameCodec.MaximumPayload)
                {
                    RejectedCandidates++;
                    buffer.RemoveAt(0);
                    continue;
                }
                var total = length + 6;
                if (buffer.Count < total) break;
                try
                {
                    frames.Add(FrameCodec.Decode(buffer.GetRange(0, total).ToArray()));
                    buffer.RemoveRange(0, total);
                }
                catch (FormatException)
                {
                    RejectedCandidates++;
                    buffer.RemoveAt(0);
                }
            }
        }
        return frames;
    }

    public void Reset() { buffer.Clear(); RejectedCandidates = 0; }
}
