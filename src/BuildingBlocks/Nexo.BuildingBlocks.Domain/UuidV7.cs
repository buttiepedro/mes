using System.Security.Cryptography;

namespace Nexo.BuildingBlocks.Domain;

/// <summary>
/// Generates time-ordered UUIDv7 identifiers (RFC 9562): a 48-bit big-endian Unix
/// millisecond timestamp followed by the version/variant bits and random data.
/// The resulting <see cref="Guid"/> renders in canonical form and sorts by creation time.
/// </summary>
public static class UuidV7
{
    public static Guid NewGuid()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var random = new byte[10];
        RandomNumberGenerator.Fill(random);

        // Data1 (32 bits): the high 32 bits of the 48-bit millisecond timestamp.
        var timeHigh = (int)(timestamp >> 16);

        // Data2 (16 bits): the low 16 bits of the timestamp.
        var timeLow = (short)(timestamp & 0xFFFF);

        // Data3 (16 bits): version 7 (0b0111) in the high nibble + 12 random bits.
        var versionAndRandom = (short)(0x7000 | (((random[0] << 8) | random[1]) & 0x0FFF));

        var data4 = new byte[8];

        // Variant (0b10) in the two high bits of the first Data4 byte + 6 random bits.
        data4[0] = (byte)(0x80 | (random[2] & 0x3F));

        // Remaining 56 bits of randomness.
        Array.Copy(random, 3, data4, 1, 7);

        return new Guid(timeHigh, timeLow, versionAndRandom, data4);
    }
}
