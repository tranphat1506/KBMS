using System.IO;
using System.Text;
using System.Text.Json;

namespace KBMS.Storage;

public class BinaryFormat
{
    private static readonly byte[] MagicBytes = { 0x4B, 0x42, 0x4D, 0x53 }; // "KBMS"
    private const ushort Version = 1;

    public static byte[] Serialize<T>(T data, Encryption encryption)
    {
        using var ms = new MemoryStream();

        // Header
        ms.Write(MagicBytes, 0, MagicBytes.Length);
        WriteUInt16(ms, Version);

        // Data (serialize with JSON)
        var json = JsonSerializer.Serialize(data);
        var dataBytes = Encoding.UTF8.GetBytes(json);
        var encryptedBytes = encryption.Encrypt(dataBytes);

        // Data length
        WriteUInt32(ms, (uint)encryptedBytes.Length);

        // Data
        ms.Write(encryptedBytes, 0, encryptedBytes.Length);

        // CRC32 checksum
        var checksum = ComputeCrc32(ms.ToArray());
        WriteUInt32(ms, checksum);

        return ms.ToArray();
    }

    public static T Deserialize<T>(byte[] data, Encryption encryption)
    {
        using var ms = new MemoryStream(data);

        // Verify Magic Bytes
        byte[] magic = new byte[4];
        ms.Read(magic, 0, 4);
        if (!magic.SequenceEqual(MagicBytes))
            throw new InvalidDataException("Invalid KBMS file format");

        // Version
        ushort version = ReadUInt16(ms);

        // Read encrypted data
        uint dataLength = ReadUInt32(ms);
        byte[] encryptedData = new byte[dataLength];
        ms.Read(encryptedData, 0, (int)dataLength);

        // Decrypt
        byte[] decryptedData = encryption.Decrypt(encryptedData);

        // Verify checksum
        uint expectedChecksum = ReadUInt32(ms);
        uint actualChecksum = ComputeCrc32(data.Take((int)(ms.Length - 4)).ToArray());
        if (expectedChecksum != actualChecksum)
            throw new InvalidDataException("Checksum mismatch");

        // Deserialize
        var json = Encoding.UTF8.GetString(decryptedData);
        return JsonSerializer.Deserialize<T>(json) ?? throw new InvalidDataException("Failed to deserialize data");
    }

    private static void WriteUInt16(Stream s, ushort value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        s.Write(bytes, 0, 2);
    }

    private static ushort ReadUInt16(Stream s)
    {
        byte[] bytes = new byte[2];
        s.Read(bytes, 0, 2);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return BitConverter.ToUInt16(bytes, 0);
    }

    private static void WriteUInt32(Stream s, uint value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        s.Write(bytes, 0, 4);
    }

    private static uint ReadUInt32(Stream s)
    {
        byte[] bytes = new byte[4];
        s.Read(bytes, 0, 4);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }

    private static uint ComputeCrc32(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in data)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
            {
                crc = (crc >> 1) ^ (0xEDB88320 & ((~crc) & 1));
            }
        }
        return ~crc;
    }
}
