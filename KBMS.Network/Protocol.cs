using System;
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
        var readLength = await stream.ReadAsync(lengthBytes, 0, 4);
        if (readLength < 4)
            return null;

        var length = BitConverter.ToInt32(lengthBytes.Reverse().ToArray(), 0);

        if (length == 0)
            throw new IOException("Connection closed");

        // Read type (1 byte)
        var typeByte = new byte[1];
        await stream.ReadAsync(typeByte, 0, 1);
        var type = (MessageType)typeByte[0];

        // Read session ID length (2 bytes)
        var sessionIdLengthBytes = new byte[2];
        await stream.ReadAsync(sessionIdLengthBytes, 0, 2);
        var sessionIdLength = BitConverter.ToUInt16(sessionIdLengthBytes.Reverse().ToArray(), 0);

        string? sessionId = null;
        if (sessionIdLength > 0)
        {
            var sessionIdBytes = new byte[sessionIdLength];
            await stream.ReadAsync(sessionIdBytes, 0, sessionIdLength);
            sessionId = Encoding.UTF8.GetString(sessionIdBytes);
        }

        // Read request ID length (2 bytes) - NEW
        var requestIdLengthBytes = new byte[2];
        await stream.ReadAsync(requestIdLengthBytes, 0, 2);
        var requestIdLength = BitConverter.ToUInt16(requestIdLengthBytes.Reverse().ToArray(), 0);

        string? requestId = null;
        if (requestIdLength > 0)
        {
            var requestIdBytes = new byte[requestIdLength];
            await stream.ReadAsync(requestIdBytes, 0, requestIdLength);
            requestId = Encoding.UTF8.GetString(requestIdBytes);
        }

        // Read payload
        var payloadLength = length - 2 - sessionIdLength - 2 - requestIdLength;
        var payloadBytes = new byte[payloadLength];
        var totalRead = 0;
        while (totalRead < payloadLength)
        {
            var read = await stream.ReadAsync(payloadBytes, totalRead, payloadLength - totalRead);
            if (read == 0)
                throw new IOException("Connection closed while reading payload");
            totalRead += read;
        }
        var content = Encoding.UTF8.GetString(payloadBytes);

        return new Message
        {
            Type = type,
            Content = content,
            SessionId = sessionId,
            RequestId = requestId
        };
    }

    public static async Task SendMessageAsync(Stream stream, Message message)
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

        // Length (4 bytes, big-endian)
        var lengthBytes = BitConverter.GetBytes(totalLength).Reverse().ToArray();
        await stream.WriteAsync(lengthBytes, 0, 4);

        // Type (1 byte)
        var typeBytes = new[] { (byte)message.Type };
        await stream.WriteAsync(typeBytes, 0, 1);

        // Session ID length (2 bytes)
        var sessionIdLenBytes = BitConverter.GetBytes(sessionIdLength).Reverse().ToArray();
        await stream.WriteAsync(sessionIdLenBytes, 0, 2);

        // Session ID (if present)
        if (sessionIdLength > 0)
        {
            await stream.WriteAsync(sessionIdBytes, 0, sessionIdLength);
        }

        // Request ID length (2 bytes) - NEW
        var requestIdLenBytes = BitConverter.GetBytes(requestIdLength).Reverse().ToArray();
        await stream.WriteAsync(requestIdLenBytes, 0, 2);

        // Request ID (if present)
        if (requestIdLength > 0)
        {
            await stream.WriteAsync(requestIdBytes, 0, requestIdLength);
        }

        // Payload
        await stream.WriteAsync(contentBytes, 0, contentBytes.Length);
        await stream.FlushAsync();
    }
}
