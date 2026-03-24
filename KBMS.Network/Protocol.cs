using System;
using System.Buffers.Binary;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace KBMS.Network;

public class Protocol
{
    public static async Task<Message?> ReceiveMessageAsync(Stream stream)
    {
        // Read length (4 bytes, big-endian)
        var lengthBytes = new byte[4];
        var readLength = await ReadExactAsync(stream, lengthBytes, 4);
        if (readLength < 4)
            return null;

        var length = BinaryPrimitives.ReadInt32BigEndian(lengthBytes);

        if (length == 0)
            throw new IOException("Connection closed");

        // Read type (1 byte)
        var typeByte = new byte[1];
        await ReadExactAsync(stream, typeByte, 1);
        var type = (MessageType)typeByte[0];

        // Read session ID length (2 bytes)
        var sessionIdLengthBytes = new byte[2];
        await ReadExactAsync(stream, sessionIdLengthBytes, 2);
        var sessionIdLength = BinaryPrimitives.ReadUInt16BigEndian(sessionIdLengthBytes);

        string? sessionId = null;
        if (sessionIdLength > 0)
        {
            var sessionIdBytes = new byte[sessionIdLength];
            await ReadExactAsync(stream, sessionIdBytes, sessionIdLength);
            sessionId = Encoding.UTF8.GetString(sessionIdBytes);
        }

        // Read request ID length (2 bytes)
        var requestIdLengthBytes = new byte[2];
        await ReadExactAsync(stream, requestIdLengthBytes, 2);
        var requestIdLength = BinaryPrimitives.ReadUInt16BigEndian(requestIdLengthBytes);

        string? requestId = null;
        if (requestIdLength > 0)
        {
            var requestIdBytes = new byte[requestIdLength];
            await ReadExactAsync(stream, requestIdBytes, requestIdLength);
            requestId = Encoding.UTF8.GetString(requestIdBytes);
        }

        // Read payload
        var payloadLength = length - 2 - sessionIdLength - 2 - requestIdLength;
        var payloadBytes = new byte[payloadLength];
        await ReadExactAsync(stream, payloadBytes, payloadLength);
        
        var content = Encoding.UTF8.GetString(payloadBytes);

        return new Message
        {
            Type = type,
            Content = content,
            SessionId = sessionId,
            RequestId = requestId
        };
    }

    public static async Task SendMessageAsync(Stream stream, Message message, SemaphoreSlim? messageLock = null)
    {
        var contentBytes = Encoding.UTF8.GetBytes(message.Content);
        
        var sessionIdBytes = string.IsNullOrEmpty(message.SessionId)
            ? Array.Empty<byte>()
            : Encoding.UTF8.GetBytes(message.SessionId);
        var sessionIdLength = (ushort)sessionIdBytes.Length;

        var requestIdBytes = string.IsNullOrEmpty(message.RequestId)
            ? Array.Empty<byte>()
            : Encoding.UTF8.GetBytes(message.RequestId);
        var requestIdLength = (ushort)requestIdBytes.Length;

        var totalLength = contentBytes.Length + 2 + sessionIdBytes.Length + 2 + requestIdBytes.Length;

        // Sequence of bytes to send:
        // [4:Length][1:Type][2:SessLen][Sess][2:ReqLen][Req][Payload]
        
        var fullMessage = new byte[4 + 1 + 2 + sessionIdLength + 2 + requestIdLength + contentBytes.Length];
        
        BinaryPrimitives.WriteInt32BigEndian(new Span<byte>(fullMessage, 0, 4), totalLength);
        fullMessage[4] = (byte)message.Type;
        BinaryPrimitives.WriteUInt16BigEndian(new Span<byte>(fullMessage, 5, 2), sessionIdLength);
        
        var offset = 7;
        if (sessionIdLength > 0)
        {
            Array.Copy(sessionIdBytes, 0, fullMessage, offset, sessionIdLength);
            offset += sessionIdLength;
        }

        BinaryPrimitives.WriteUInt16BigEndian(new Span<byte>(fullMessage, offset, 2), requestIdLength);
        offset += 2;

        if (requestIdLength > 0)
        {
            Array.Copy(requestIdBytes, 0, fullMessage, offset, requestIdLength);
            offset += requestIdLength;
        }

        Array.Copy(contentBytes, 0, fullMessage, offset, contentBytes.Length);

        if (messageLock != null) await messageLock.WaitAsync();
        try
        {
            await stream.WriteAsync(fullMessage, 0, fullMessage.Length);
            await stream.FlushAsync();
        }
        finally
        {
            if (messageLock != null) messageLock.Release();
        }
    }

    private static async Task<int> ReadExactAsync(Stream stream, byte[] buffer, int count)
    {
        int totalRead = 0;
        while (totalRead < count)
        {
            int read = await stream.ReadAsync(buffer, totalRead, count - totalRead);
            if (read == 0) return totalRead;
            totalRead += read;
        }
        return totalRead;
    }
}
