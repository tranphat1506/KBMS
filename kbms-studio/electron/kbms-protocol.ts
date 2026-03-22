import { Buffer } from 'node:buffer';

export enum MessageType {
    LOGIN = 1,
    QUERY = 2,
    RESULT = 3,
    ERROR = 4,
    LOGOUT = 5,
    METADATA = 6,
    ROW = 7,
    FETCH_DONE = 8
}

export interface KbmsMessage {
    type: MessageType;
    content: string;
    sessionId?: string;
    requestId?: string;
}

export class Protocol {
    static pack(message: KbmsMessage): Buffer {
        const contentBuffer = Buffer.from(message.content, 'utf8');
        const sessionIdBuffer = message.sessionId ? Buffer.from(message.sessionId, 'utf8') : Buffer.alloc(0);
        const requestIdBuffer = message.requestId ? Buffer.from(message.requestId, 'utf8') : Buffer.alloc(0);
        
        const sessionIdLength = sessionIdBuffer.length;
        const requestIdLength = requestIdBuffer.length;
        
        // totalLength = content + 2 (sessionIdLen) + sessionId + 2 (requestIdLen) + requestId
        const totalLength = contentBuffer.length + 2 + sessionIdLength + 2 + requestIdLength;
        
        // 4 bytes length
        const lengthBuffer = Buffer.alloc(4);
        lengthBuffer.writeInt32BE(totalLength, 0);
        
        // 1 byte type
        const typeBuffer = Buffer.from([message.type]);
        
        // 2 bytes sessionIdLength
        const sessionIdLenBuffer = Buffer.alloc(2);
        sessionIdLenBuffer.writeUInt16BE(sessionIdLength, 0);
        
        // 2 bytes requestIdLength
        const requestIdLenBuffer = Buffer.alloc(2);
        requestIdLenBuffer.writeUInt16BE(requestIdLength, 0);
        
        return Buffer.concat([
            lengthBuffer, 
            typeBuffer, 
            sessionIdLenBuffer, 
            sessionIdBuffer, 
            requestIdLenBuffer, 
            requestIdBuffer, 
            contentBuffer
        ]);
    }

    /**
     * Parses a buffer stream and returns messages.
     * Returns the parsed messages and the remaining buffer.
     */
    static unpack(buffer: Buffer): { messages: KbmsMessage[], remaining: Buffer } {
        const messages: KbmsMessage[] = [];
        let offset = 0;

        while (offset + 4 <= buffer.length) {
            const length = buffer.readInt32BE(offset);
            
            // Expected length of the entire packet segment
            const packetTotalBytes = 4 + 1 + length;
            
            if (offset + packetTotalBytes > buffer.length) {
                break;
            }

            const type = buffer.readUInt8(offset + 4) as MessageType;
            const sessionIdLength = buffer.readUInt16BE(offset + 5);
            
            let sessionId: string | undefined = undefined;
            if (sessionIdLength > 0) {
                sessionId = buffer.toString('utf8', offset + 7, offset + 7 + sessionIdLength);
            }
            
            const requestIdOffset = offset + 7 + sessionIdLength;
            const requestIdLength = buffer.readUInt16BE(requestIdOffset);
            
            let requestId: string | undefined = undefined;
            if (requestIdLength > 0) {
                requestId = buffer.toString('utf8', requestIdOffset + 2, requestIdOffset + 2 + requestIdLength);
            }
            
            const payloadLength = length - 2 - sessionIdLength - 2 - requestIdLength;
            const contentStart = requestIdOffset + 2 + requestIdLength;
            const content = buffer.toString('utf8', contentStart, contentStart + payloadLength);

            messages.push({ type, content, sessionId, requestId });
            
            offset += packetTotalBytes;
        }

        return { 
            messages, 
            remaining: buffer.subarray(offset) 
        };
    }
}
