using System;
using System.Collections.Generic;
using System.Text;

namespace KBMS.Storage.V3;

/// <summary>
/// Represents a parsed physical record inside a Page.
/// Converts typed data (int, string, bool) to/from byte arrays for the SlottedPage.
/// Format matches standard DB slotted layouts with offset dictionaries.
/// </summary>
public class Tuple
{
    // Extracted raw field bytes
    public List<byte[]> Fields { get; set; } = new();

    public Tuple() { }

    public Tuple(List<byte[]> fields)
    {
        Fields = fields;
    }

    /// <summary>
    /// Serializes this tuple into a single byte array for insertion into a SlottedPage.
    /// Format:
    /// [FieldCount (2 bytes)]
    /// [Offsets Array (2 bytes per field) - points to end of field relative to tuple start]
    /// [Field Data 1] [Field Data 2] ...
    /// </summary>
    public byte[] Serialize()
    {
        int fieldCount = Fields.Count;
        int headerSize = 2 + (fieldCount * 2);

        int totalDataSize = 0;
        foreach (var field in Fields)
        {
            totalDataSize += field?.Length ?? 0;
        }

        byte[] result = new byte[headerSize + totalDataSize];

        // 1. Write FieldCount
        BitConverter.GetBytes((short)fieldCount).CopyTo(result, 0);

        int currentOffset = headerSize;
        int headerPos = 2;

        for (int i = 0; i < fieldCount; i++)
        {
            var field = Fields[i];
            
            if (field != null && field.Length > 0)
            {
                Buffer.BlockCopy(field, 0, result, currentOffset, field.Length);
                currentOffset += field.Length;
            }
            
            // 2. Write End Offset for this field
            BitConverter.GetBytes((short)currentOffset).CopyTo(result, headerPos);
            headerPos += 2;
        }

        return result;
    }

    /// <summary>
    /// Deserializes a raw byte array from a SlottedPage back into a Tuple.
    /// </summary>
    public static Tuple Deserialize(byte[] data)
    {
        if (data == null || data.Length < 2) return new Tuple();

        short fieldCount = BitConverter.ToInt16(data, 0);
        var tuple = new Tuple();

        int currentOffset = 2 + (fieldCount * 2); // Start of data section
        int headerPos = 2;

        for (int i = 0; i < fieldCount; i++)
        {
            short endOffset = BitConverter.ToInt16(data, headerPos);
            headerPos += 2;

            int fieldLength = endOffset - currentOffset;
            
            if (fieldLength == 0)
            {
                tuple.Fields.Add(Array.Empty<byte>()); // Treat as null / empty
            }
            else
            {
                byte[] fieldData = new byte[fieldLength];
                Buffer.BlockCopy(data, currentOffset, fieldData, 0, fieldLength);
                tuple.Fields.Add(fieldData);
            }

            currentOffset = endOffset;
        }

        return tuple;
    }

    // ================= TYPE HELPERS FOR INSERTION =================

    public void AddInt(int value) => Fields.Add(BitConverter.GetBytes(value));
    public void AddLong(long value) => Fields.Add(BitConverter.GetBytes(value));
    public void AddBool(bool value) => Fields.Add(BitConverter.GetBytes(value));
    public void AddFloat(float value) => Fields.Add(BitConverter.GetBytes(value));
    public void AddGuid(Guid value) => Fields.Add(value.ToByteArray());
    
    public void AddString(string? value) 
    {
        if (value == null) Fields.Add(Array.Empty<byte>());
        else Fields.Add(Encoding.UTF8.GetBytes(value));
    }

    // ================= TYPE HELPERS FOR RETRIEVAL =================

    public int GetInt(int index) => BitConverter.ToInt32(Fields[index], 0);
    public long GetLong(int index) => BitConverter.ToInt64(Fields[index], 0);
    public bool GetBool(int index) => BitConverter.ToBoolean(Fields[index], 0);
    public float GetFloat(int index) => BitConverter.ToSingle(Fields[index], 0);
    public Guid GetGuid(int index) => new Guid(Fields[index]);
    public string GetString(int index) => Encoding.UTF8.GetString(Fields[index]);
}
