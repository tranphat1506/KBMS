# KẾ HOẠCH TRIỂN KHAI KBMS (Knowledge Base Management System)

## Tổng quan
Dự án KBMS là hệ thống quản trị cơ sở tri thức dựa trên mô hình COKB (Computational Object Knowledge Base) với kiến trúc 4 tầng tương tự MySQL.

---

## CÔNG VIỆC 1: XÁC ĐỊNH YÊU CẦU

### 1.1 Nghiên cứu mô hình COKB (6 thành phần)
| Thành phần | Mô tả | Cấu trúc dữ liệu |
|-----------|-------|------------------|
| **Concept** | Các khái niệm cơ bản | `Concept {Id, Name, Description, KbId}` |
| **Hierarchy** | Quan hệ phân cấp | `Hierarchy {ParentId, ChildId}` |
| **Relation** | Quan hệ giữa các khái niệm | `Relation {Id, Name, Domain, Range, KbId}` |
| **Operator** | Các toán tử toán học | `Operator {Symbol, ParamTypes, ReturnType}` |
| **Function** | Các hàm toán học | `Function {Name, ParamTypes, ReturnType, Body}` |
| **Rule** | Luật suy luận | `Rule {Id, KbId, Name, Hypothesis, Conclusion}` |

### 1.2 Yêu cầu quản trị
- **Tổ chức tri thức**: Tạo/Xóa/Sửa các cơ sở tri thức (KB)
- **Quản trị tri thức**: Thêm/Xóa/Sửa các thành phần COKB
- **Truy vấn**: KBDDL và KBDML
- **Khai thác tri thức**: Suy luận, tính toán trên tri thức

### 1.3 Yêu cầu phân quyền
- **ROOT**: Full quyền truy cập, KHÔNG cần kiểm tra permission
- **USER**: Có danh sách quyền hạn cụ thể cho từng KB hoặc hệ thống

---

## CÔNG VIỆC 2: THIẾT KẾ KIẾN TRÚC

### 2.1 Kiến trúc tổng thể 4 tầng

```
┌─────────────────────────────────────┐
│   Application Layer                 │
│   - CLI Interface (Console App)     │
│   - Client Applications             │
│   - Network Protocol (TCP Socket)   │
└─────────────────────────────────────┘
                ↕ TCP/Socket
┌─────────────────────────────────────┐
│   KBMS Server Layer                 │
│   - Connection Manager               │
│   - Authentication Manager          │
│   - Query Parser (KBDDL/KBDML)      │
│   - Query Optimizer                 │
│   - Knowledge Manager               │
│   - Reasoning Engine                │
└─────────────────────────────────────┘
                ↕ File I/O
┌─────────────────────────────────────┐
│   Knowledge Storage Engine           │
│   - Binary Storage (.bin files)     │
│   - Index Manager (B+ Tree/Hash)     │
│   - Encryption/Decryption           │
│   - WAL (Write Ahead Log)           │
└─────────────────────────────────────┘
                ↕ File System
┌─────────────────────────────────────┐
│   Physical Storage                  │
│   data/                             │
│     ├── geometry/                   │  (Mỗi KB là 1 folder riêng)
│     │   ├── metadata.bin            │
│     │   ├── concepts.bin            │
│     │   ├── relations.bin           │
│     │   ├── operators.bin           │
│     │   ├── functions.bin          │
│     │   ├── rules.bin               │
│     │   ├── objects.bin             │
│     │   ├── index.bin               │
│     │   └── wal.log                 │
│     └── users/                     │
│         └── users.bin               │
└─────────────────────────────────────┘
```

### 2.2 Ngôn ngữ truy vấn

#### KBDDL (Knowledge Base Definition Language)
| Lệnh | Cú pháp | Chức năng |
|------|--------|-----------|
| CREATE KB | `CREATE KNOWLEDGE BASE <name>` | Tạo cơ sở tri thức mới |
| USE KB | `USE <name>` | Chọn KB hiện tại |
| DROP KB | `DROP KNOWLEDGE BASE <name>` | Xóa KB |
| CREATE CONCEPT | `CREATE CONCEPT <name> (...)` | Tạo khái niệm |
| CREATE RULE | `CREATE RULE <name> IF <hypothesis> THEN <conclusion>` | Tạo luật suy luận |
| CREATE USER | `CREATE USER <username> PASSWORD <password>` | Tạo user mới |
| GRANT | `GRANT <privilege> ON <kb_name> TO <username>` | Cấp quyền cho user |

#### KBDML (Knowledge Base Manipulation Language)
| Lệnh | Cú pháp | Chức năng |
|------|--------|-----------|
| SELECT | `SELECT <concept> WHERE <conditions>` | Truy vấn khái niệm |
| INSERT | `INSERT INTO <concept> VALUES (field1=value1, ...)` | Thêm đối tượng |
| UPDATE | `UPDATE <concept> SET field=value WHERE conditions` | Cập nhật đối tượng |
| DELETE | `DELETE FROM <concept> WHERE conditions` | Xóa đối tượng |
| SOLVE | `SOLVE <concept> KNOWN <knowns> FIND <unknowns>` | Suy luận/tính toán |

### 2.3 Hệ thống phân quyền

#### User Roles & Privileges
```csharp
public enum UserRole
{
    ROOT,   // Full quyền, không cần check permission
    USER    // Cần check permission theo danh sách
}

public enum Privilege
{
    READ,   // Đọc tri thức
    WRITE,  // Đọc và ghi
    ADMIN   // Quản trị KB (CREATE, DROP, GRANT)
}

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public byte[] PasswordHash { get; set; }
    public UserRole Role { get; set; }
    public Dictionary<string, Privilege> KbPrivileges { get; set; }  // KB Name -> Privilege
    public bool SystemAdmin { get; set; }  // Quyền quản trị hệ thống (cho USER)
}
```

#### Quy tắc phân quyền
```
ROOT: Không cần check - toàn quyền trên hệ thống

USER: Check permission theo action
  - CREATE KNOWLEDGE BASE: Cần SystemAdmin = true
  - DROP KNOWLEDGE BASE: Cần ADMIN privilege trên KB đó
  - SELECT: Cần ít nhất READ privilege trên KB
  - INSERT/UPDATE/DELETE: Cần WRITE privilege trên KB
  - SOLVE: Cần READ privilege trên KB
  - CREATE CONCEPT/RULE: Cần ADMIN privilege trên KB
  - GRANT: Chỉ ROOT hoặc SystemAdmin = true
```

---

## CÔNG VIỆC 3: CÀI ĐẶT VÀ TRIỂN KHAI

### 3.1 Cấu trúc thư mục project (C# / .NET)

```
KBMS/
├── KBMS.sln                    # Solution file
├── KBMS.Server/                # Server project (Console App)
│   ├── Program.cs
│   ├── Server/
│   │   ├── KbmsServer.cs       # KBMS Server
│   │   ├── ConnectionManager.cs
│   │   └── AuthenticationManager.cs
│   ├── Parser/
│   │   ├── QueryParser.cs
│   │   ├── Lexer.cs
│   │   └── Ast/
│   │       └── *.cs            # AST nodes
│   ├── Optimizer/
│   │   └── QueryOptimizer.cs
│   ├── Knowledge/
│   │   └── KnowledgeManager.cs
│   └── Reasoning/
│       ├── ReasoningEngine.cs
│       ├── ForwardChaining.cs
│       └── BackwardChaining.cs
├── KBMS.Storage/               # Storage Engine (Class Library)
│   ├── Engine.cs              # Storage Engine chính
│   ├── BinaryFormat.cs        # Binary format handler
│   ├── Encryption.cs          # Encryption/Decryption
│   ├── IndexManager.cs        # Index Manager (B+ Tree)
│   └── WalManager.cs          # WAL Manager
├── KBMS.Models/               # Data Models (Class Library)
│   ├── KnowledgeBase.cs
│   ├── Concept.cs
│   ├── Relation.cs
│   ├── Operator.cs
│   ├── Function.cs
│   ├── Rule.cs
│   ├── ObjectInstance.cs
│   └── User.cs
├── KBMS.CLI/                  # CLI Client (Console App)
│   ├── Program.cs
│   └── Cli.cs
├── KBMS.Network/              # Network (Class Library)
│   ├── Protocol.cs            # Network protocol
│   ├── Message.cs             # Message types
│   └── TcpClient.cs
├── KBMS.Tests/                # Unit Tests (xUnit)
│   └── *.cs
├── data/                      # Physical Storage
│   ├── geometry/
│   │   ├── metadata.bin
│   │   ├── concepts.bin
│   │   ├── relations.bin
│   │   ├── operators.bin
│   │   ├── functions.bin
│   │   ├── rules.bin
│   │   ├── objects.bin
│   │   ├── index.bin
│   │   └── wal.log
│   └── users/
│       └── users.bin
├── logs/                      # Log files
│   └── kbms.log
├── config/
│   └── appsettings.json
├── README.md
└── .gitignore
```

### 3.2 Cài đặt Data Models (6 thành phần COKB)

#### File: `KBMS.Models/KnowledgeBase.cs`
```csharp
namespace KBMS.Models;

public class KnowledgeBase
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid OwnerId { get; set; }
    public string Description { get; set; }
    public int ObjectCount { get; set; }
    public int RuleCount { get; set; }
}
```

#### File: `KBMS.Models/Concept.cs`
```csharp
namespace KBMS.Models;

public class Concept
{
    public Guid Id { get; set; }
    public Guid KbId { get; set; }
    public string Name { get; set; }
    public List<Variable> Variables { get; set; }
    public List<Constraint> Constraints { get; set; }
    public List<ComputationRelation> CompRels { get; set; }
}

public class Variable
{
    public string Name { get; set; }
    public string Type { get; set; }
}

public class Constraint
{
    public string Expression { get; set; }
}

public class ComputationRelation
{
    public string FromVariable { get; set; }
    public string ToVariable { get; set; }
    public string Formula { get; set; }
}
```

#### File: `KBMS.Models/Relation.cs`
```csharp
namespace KBMS.Models;

public class Relation
{
    public Guid Id { get; set; }
    public Guid KbId { get; set; }
    public string Name { get; set; }
    public string Domain { get; set; }
    public string Range { get; set; }
}
```

#### File: `KBMS.Models/Operator.cs`
```csharp
namespace KBMS.Models;

public class Operator
{
    public string Symbol { get; set; }
    public int ParamCount { get; set; }
    public List<string> ParamTypes { get; set; }
    public string ReturnType { get; set; }
    public List<string> Properties { get; set; }
}
```

#### File: `KBMS.Models/Function.cs`
```csharp
namespace KBMS.Models;

public class Function
{
    public string Name { get; set; }
    public int ParamCount { get; set; }
    public List<string> ParamTypes { get; set; }
    public string ReturnType { get; set; }
    public string Body { get; set; }
}
```

#### File: `KBMS.Models/Rule.cs`
```csharp
namespace KBMS.Models;

public class Rule
{
    public Guid Id { get; set; }
    public Guid KbId { get; set; }
    public string Name { get; set; }
    public List<Expression> Hypothesis { get; set; }
    public List<Expression> Conclusion { get; set; }
}

public class Expression
{
    public string Type { get; set; }
    public string Content { get; set; }
    public List<Expression> Children { get; set; }
}
```

#### File: `KBMS.Models/ObjectInstance.cs`
```csharp
namespace KBMS.Models;

public class ObjectInstance
{
    public Guid Id { get; set; }
    public Guid KbId { get; set; }
    public string ConceptName { get; set; }
    public Dictionary<string, object> Values { get; set; }
}
```

#### File: `KBMS.Models/User.cs`
```csharp
namespace KBMS.Models;

public enum UserRole
{
    ROOT,
    USER
}

public enum Privilege
{
    READ,
    WRITE,
    ADMIN
}

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public byte[] PasswordHash { get; set; }
    public UserRole Role { get; set; }
    public Dictionary<string, Privilege> KbPrivileges { get; set; }
    public bool SystemAdmin { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 3.3 Cài đặt Storage Engine

#### File: `KBMS.Storage/Encryption.cs`
```csharp
using System.Security.Cryptography;
using System.Text;

namespace KBMS.Storage;

public class Encryption
{
    private readonly byte[] _key;

    public Encryption(string key)
    {
        // SHA256 để tạo key 256-bit
        using var sha = SHA256.Create();
        _key = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
    }

    public byte[] Encrypt(byte[] data)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(data, 0, data.Length);
        }

        var result = new byte[aes.IV.Length + ms.ToArray().Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(ms.ToArray(), 0, result, aes.IV.Length, ms.ToArray().Length);

        return result;
    }

    public byte[] Decrypt(byte[] encryptedData)
    {
        using var aes = Aes.Create();
        aes.Key = _key;

        byte[] iv = new byte[aes.IV.Length];
        byte[] cipher = new byte[encryptedData.Length - aes.IV.Length];
        Buffer.BlockCopy(encryptedData, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(encryptedData, iv.Length, cipher, 0, cipher.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(cipher);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var result = new MemoryStream();
        cs.CopyTo(result);

        return result.ToArray();
    }
}
```

#### File: `KBMS.Storage/BinaryFormat.cs`
```csharp
using System.IO;
using System.Text;

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

        // Data (serialize với JSON hoặc binary)
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
        return JsonSerializer.Deserialize<T>(json);
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
        // CRC32 implementation
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
```

#### File: `KBMS.Storage/Engine.cs`
```csharp
using System.IO;
using KBMS.Models;

namespace KBMS.Storage;

public class StorageEngine
{
    private readonly string _dataDir;
    private readonly Encryption _encryption;
    private readonly IndexManager _indexManager;
    private readonly WalManager _wal;

    public StorageEngine(string dataDir, string encryptionKey)
    {
        _dataDir = dataDir;
        _encryption = new Encryption(encryptionKey);
        _indexManager = new IndexManager();
        _wal = new WalManager();

        Directory.CreateDirectory(_dataDir);
    }

    public KnowledgeBase CreateKb(string kbName, Guid ownerId, string description = "")
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        Directory.CreateDirectory(kbPath);

        var kb = new KnowledgeBase
        {
            Id = Guid.NewGuid(),
            Name = kbName,
            CreatedAt = DateTime.Now,
            OwnerId = ownerId,
            Description = description,
            ObjectCount = 0,
            RuleCount = 0
        };

        SaveKbMetadata(kb);
        _indexManager.CreateIndex(kbPath);

        return kb;
    }

    public KnowledgeBase? LoadKb(string kbName)
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        var metadataPath = Path.Combine(kbPath, "metadata.bin");

        if (!File.Exists(metadataPath))
            return null;

        var data = File.ReadAllBytes(metadataPath);
        return BinaryFormat.Deserialize<KnowledgeBase>(data, _encryption);
    }

    private void SaveKbMetadata(KnowledgeBase kb)
    {
        var kbPath = Path.Combine(_dataDir, kb.Name);
        var metadataPath = Path.Combine(kbPath, "metadata.bin");

        var data = BinaryFormat.Serialize(kb, _encryption);
        File.WriteAllBytes(metadataPath, data);
    }

    public bool InsertObject(string kbName, ObjectInstance obj)
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        var objectsPath = Path.Combine(kbPath, "objects.bin");

        // Ghi WAL trước
        _wal.WriteLog(kbPath, $"INSERT_OBJECT:{obj.Id}");

        var objects = LoadAllObjects(kbName);
        objects.Add(obj);

        var data = BinaryFormat.Serialize(objects, _encryption);
        File.WriteAllBytes(objectsPath, data);

        // Update index
        _indexManager.AddIndex(kbPath, obj.Id, obj.ConceptName, obj.Values);

        // Update KB metadata
        var kb = LoadKb(kbName);
        if (kb != null)
        {
            kb.ObjectCount = objects.Count;
            SaveKbMetadata(kb);
        }

        _wal.Commit(kbPath);
        return true;
    }

    public List<ObjectInstance> SelectObjects(string kbName, Dictionary<string, object>? conditions = null)
    {
        var objects = LoadAllObjects(kbName);

        if (conditions == null || conditions.Count == 0)
            return objects;

        return objects.Where(obj =>
            conditions.All(kv =>
                obj.Values.ContainsKey(kv.Key) &&
                obj.Values[kv.Key]?.Equals(kv.Value) == true
            )
        ).ToList();
    }

    public bool UpdateObject(string kbName, Guid objId, Dictionary<string, object> values)
    {
        var kbPath = Path.Combine(_dataDir, kbName);

        _wal.WriteLog(kbPath, $"UPDATE_OBJECT:{objId}");

        var objects = LoadAllObjects(kbName);
        var obj = objects.FirstOrDefault(o => o.Id == objId);

        if (obj == null)
            return false;

        foreach (var kv in values)
        {
            obj.Values[kv.Key] = kv.Value;
        }

        var objectsPath = Path.Combine(kbPath, "objects.bin");
        var data = BinaryFormat.Serialize(objects, _encryption);
        File.WriteAllBytes(objectsPath, data);

        _indexManager.UpdateIndex(kbPath, objId, obj.ConceptName, obj.Values);
        _wal.Commit(kbPath);
        return true;
    }

    public bool DeleteObject(string kbName, Guid objId)
    {
        var kbPath = Path.Combine(_dataDir, kbName);

        _wal.WriteLog(kbPath, $"DELETE_OBJECT:{objId}");

        var objects = LoadAllObjects(kbName);
        var obj = objects.FirstOrDefault(o => o.Id == objId);

        if (obj == null)
            return false;

        objects.Remove(obj);

        var objectsPath = Path.Combine(kbPath, "objects.bin");
        var data = BinaryFormat.Serialize(objects, _encryption);
        File.WriteAllBytes(objectsPath, data);

        _indexManager.RemoveIndex(kbPath, objId);
        _wal.Commit(kbPath);

        // Update KB metadata
        var kb = LoadKb(kbName);
        if (kb != null)
        {
            kb.ObjectCount = objects.Count;
            SaveKbMetadata(kb);
        }

        return true;
    }

    private List<ObjectInstance> LoadAllObjects(string kbName)
    {
        var kbPath = Path.Combine(_dataDir, kbName);
        var objectsPath = Path.Combine(kbPath, "objects.bin");

        if (!File.Exists(objectsPath))
            return new List<ObjectInstance>();

        var data = File.ReadAllBytes(objectsPath);
        return BinaryFormat.Deserialize<List<ObjectInstance>>(data, _encryption);
    }
}
```

#### File: `KBMS.Storage/IndexManager.cs`
```csharp
using System.Collections.Generic;
using System.IO;
using KBMS.Models;

namespace KBMS.Storage;

public class IndexManager
{
    private class BPlusTreeNode
    {
        public bool IsLeaf { get; set; }
        public List<Guid> Keys { get; set; }
        public List<BPlusTreeNode> Children { get; set; }
        public List<ObjectIndexEntry> Entries { get; set; }
    }

    private class ObjectIndexEntry
    {
        public Guid ObjectId { get; set; }
        public string ConceptName { get; set; }
        public Dictionary<string, object> Values { get; set; }
    }

    public void CreateIndex(string kbPath)
    {
        var indexPath = Path.Combine(kbPath, "index.bin");
        // Tạo empty index structure
        File.WriteAllBytes(indexPath, new byte[0]);
    }

    public void AddIndex(string kbPath, Guid objId, string conceptName, Dictionary<string, object> values)
    {
        // Implementation để thêm vào B+ Tree
        // ...
    }

    public void UpdateIndex(string kbPath, Guid objId, string conceptName, Dictionary<string, object> values)
    {
        // Implementation để update B+ Tree
        // ...
    }

    public void RemoveIndex(string kbPath, Guid objId)
    {
        // Implementation để xóa từ B+ Tree
        // ...
    }

    public List<Guid> FindByCondition(string kbPath, Dictionary<string, object> conditions)
    {
        // Sử dụng B+ Tree để tìm kiếm nhanh
        return new List<Guid>();
    }
}
```

#### File: `KBMS.Storage/WalManager.cs`
```csharp
using System.IO;

namespace KBMS.Storage;

public class WalManager
{
    public void WriteLog(string kbPath, string logEntry)
    {
        var walPath = Path.Combine(kbPath, "wal.log");
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var line = $"[{timestamp}] {logEntry}\n";
        File.AppendAllText(walPath, line);
    }

    public void Commit(string kbPath)
    {
        var walPath = Path.Combine(kbPath, "wal.log");
        File.AppendAllText(walPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] COMMIT\n");
    }

    public List<string> Recover(string kbPath)
    {
        var walPath = Path.Combine(kbPath, "wal.log");
        if (!File.Exists(walPath))
            return new List<string>();

        return File.ReadAllLines(walPath).ToList();
    }
}
```

### 3.4 Cài đặt Server Components

#### File: `KBMS.Server/ConnectionManager.cs`
```csharp
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using KBMS.Models;

namespace KBMS.Server;

/// <summary>
/// Quản lý các kết nối active và session per-connection
/// Đảm bảo mỗi client có session riêng biệt, không bị conflict
/// </summary>
public class ConnectionManager
{
    /// <summary>
    /// Session mapping: Client ID → Session
    /// Client ID có thể là TcpClient, connection ID, hoặc session ID từ client
    /// </summary>
    private readonly ConcurrentDictionary<string, Session> _sessions;

    /// <summary>
    /// Session ID generator
    /// </summary>
    private readonly System.Random _random = new();

    public ConnectionManager()
    {
        _sessions = new ConcurrentDictionary<string, Session>();
    }

    /// <summary>
    /// Tạo session mới khi client connect
    /// </summary>
    public Session CreateSession(string clientId)
    {
        var sessionId = GenerateSessionId();
        var session = new Session
        {
            SessionId = sessionId,
            ClientId = clientId,
            User = null,
            CurrentKb = null,
            ConnectedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        _sessions[clientId] = session;
        return session;
    }

    /// <summary>
    /// Lấy session theo client ID
    /// </summary>
    public Session? GetSession(string clientId)
    {
        return _sessions.TryGetValue(clientId, out var session) ? session : null;
    }

    /// <summary>
    /// Lấy session theo session ID
    /// </summary>
    public Session? GetSessionBySessionId(string sessionId)
    {
        foreach (var session in _sessions.Values)
        {
            if (session.SessionId == sessionId)
                return session;
        }
        return null;
    }

    /// <summary>
    /// Set user cho session (sau khi login thành công)
    /// </summary>
    public void SetSessionUser(string clientId, User user)
    {
        if (_sessions.TryGetValue(clientId, out var session))
        {
            session.User = user;
            session.LastActivityAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Set current KB cho session (sau khi USE command)
    /// </summary>
    public void SetSessionKb(string clientId, string? kbName)
    {
        if (_sessions.TryGetValue(clientId, out var session))
        {
            session.CurrentKb = kbName;
            session.LastActivityAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Update last activity time
    /// </summary>
    public void UpdateActivity(string clientId)
    {
        if (_sessions.TryGetValue(clientId, out var session))
        {
            session.LastActivityAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Remove session khi client disconnect
    /// </summary>
    public void RemoveSession(string clientId)
    {
        _sessions.TryRemove(clientId, out _);
    }

    /// <summary>
    /// Validate session: kiểm tra user đã login chưa
    /// </summary>
    public bool IsAuthenticated(string clientId)
    {
        var session = GetSession(clientId);
        return session?.User != null;
    }

    /// <summary>
    /// Get current user từ session
    /// </summary>
    public User? GetCurrentUser(string clientId)
    {
        return GetSession(clientId)?.User;
    }

    /// <summary>
    /// Get current KB từ session
    /// </summary>
    public string? GetCurrentKb(string clientId)
    {
        return GetSession(clientId)?.CurrentKb;
    }

    /// <summary>
    /// Cleanup expired sessions (session timeout)
    /// </summary>
    public void CleanupExpiredSessions(TimeSpan timeout)
    {
        var now = DateTime.UtcNow;
        var expiredKeys = new List<string>();

        foreach (var kvp in _sessions)
        {
            if (now - kvp.Value.LastActivityAt > timeout)
            {
                expiredKeys.Add(kvp.Key);
            }
        }

        foreach (var key in expiredKeys)
        {
            _sessions.TryRemove(key, out _);
        }
    }

    private string GenerateSessionId()
    {
        var bytes = new byte[16];
        _random.NextBytes(bytes);
        return Convert.ToHexString(bytes).ToLower();
    }
}

/// <summary>
/// Session data structure
/// </summary>
public class Session
{
    public string SessionId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;  // Có thể là connection ID hoặc endpoint
    public User? User { get; set; }
    public string? CurrentKb { get; set; }
    public DateTime ConnectedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
}
```

#### File: `KBMS.Server/AuthenticationManager.cs`
```csharp
using System;
using System.Linq;
using KBMS.Models;

namespace KBMS.Server;

public class AuthenticationManager
{
    private readonly StorageEngine _storage;

    public AuthenticationManager(StorageEngine storage)
    {
        _storage = storage;
    }

    public User? Login(string username, string password)
    {
        // Load users từ storage
        var users = LoadUsers();

        var user = users.FirstOrDefault(u => u.Username == username);

        if (user == null)
            return null;

        // Verify password (dùng BCrypt)
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        return user;
    }

    /// <summary>
    /// Kiểm tra quyền truy cập
    /// ROOT: Luôn trả về true (không cần check)
    /// USER: Kiểm tra theo danh sách privilege
    /// </summary>
    public bool CheckPrivilege(User user, string action, string? kbName = null)
    {
        // ROOT - không cần check permission
        if (user.Role == UserRole.ROOT)
            return true;

        // USER - cần check
        return CheckUserPrivilege(user, action, kbName);
    }

    private bool CheckUserPrivilege(User user, string action, string? kbName)
    {
        // Kiểm tra action
        switch (action.ToUpper())
        {
            case "CREATE_KB":
                // Cần SystemAdmin = true
                return user.SystemAdmin;

            case "DROP_KB":
                // Cần ADMIN privilege trên KB đó
                if (kbName == null) return false;
                return user.KbPrivileges.TryGetValue(kbName, out var priv1) && priv1 == Privilege.ADMIN;

            case "SELECT":
            case "SOLVE":
                // Cần ít nhất READ privilege trên KB
                if (kbName == null) return false;
                return user.KbPrivileges.ContainsKey(kbName);

            case "INSERT":
            case "UPDATE":
            case "DELETE":
                // Cần WRITE privilege trên KB
                if (kbName == null) return false;
                return user.KbPrivileges.TryGetValue(kbName, out var priv2) && (priv2 == Privilege.WRITE || priv2 == Privilege.ADMIN);

            case "CREATE_CONCEPT":
            case "CREATE_RULE":
            case "CREATE_OPERATOR":
            case "CREATE_FUNCTION":
                // Cần ADMIN privilege trên KB
                if (kbName == null) return false;
                return user.KbPrivileges.TryGetValue(kbName, out var priv3) && priv3 == Privilege.ADMIN;

            case "GRANT":
                // Chỉ ROOT hoặc SystemAdmin = true
                return user.SystemAdmin;

            default:
                return false;
        }
    }

    private List<User> LoadUsers()
    {
        // Load từ file users.bin
        // ...
        return new List<User>();
    }

    private void SaveUsers(List<User> users)
    {
        // Save vào file users.bin
        // ...
    }

    public User CreateUser(string username, string password, UserRole role, bool systemAdmin = false)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role,
            SystemAdmin = systemAdmin,
            KbPrivileges = new Dictionary<string, Privilege>(),
            CreatedAt = DateTime.Now
        };

        var users = LoadUsers();
        users.Add(user);
        SaveUsers(users);

        return user;
    }

    public bool GrantPrivilege(string username, string kbName, Privilege privilege)
    {
        var users = LoadUsers();
        var user = users.FirstOrDefault(u => u.Username == username);

        if (user == null || user.Role == UserRole.ROOT)
            return false;

        user.KbPrivileges[kbName] = privilege;
        SaveUsers(users);

        return true;
    }
}
```

#### File: `KBMS.Server/KbmsServer.cs`
```csharp
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using KBMS.Network;
using KBMS.Parser;
using KBMS.Knowledge;
using KBMS.Storage;
using KBMS.Models;

namespace KBMS.Server;

public class KbmsServer
{
    private readonly string _host;
    private readonly int _port;
    private readonly AuthenticationManager _authManager;
    private readonly ConnectionManager _connectionManager;
    private readonly QueryParser _parser;
    private readonly KnowledgeManager _knowledgeManager;
    private readonly StorageEngine _storage;

    private TcpListener? _listener;
    private bool _isRunning;

    public KbmsServer(
        string host = "0.0.0.0",
        int port = 3307,
        StorageEngine? storage = null)
    {
        _host = host;
        _port = port;
        _storage = storage ?? new StorageEngine("data", "kbms_encryption_key");
        _authManager = new AuthenticationManager(_storage);
        _connectionManager = new ConnectionManager();
        _parser = new QueryParser();
        _knowledgeManager = new KnowledgeManager(_storage, _authManager);
    }

    public async Task StartAsync()
    {
        _listener = new TcpListener(IPAddress.Parse(_host), _port);
        _listener.Start();
        _isRunning = true;

        Console.WriteLine($"KBMS Server started on {_host}:{_port}");

        while (_isRunning)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting connection: {ex.Message}");
            }
        }
    }

    public void Stop()
    {
        _isRunning = false;
        _listener?.Stop();
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        // Generate unique client ID for this connection
        var clientId = GenerateClientId();

        try
        {
            // Create session for this connection
            var session = _connectionManager.CreateSession(clientId);
            Console.WriteLine($"[{session.SessionId}] Client connected from {client.Client?.RemoteEndPoint}");

            using var stream = client.GetStream();

            while (client.Connected)
            {
                var message = await Protocol.ReceiveMessageAsync(stream);
                if (message == null) break;

                // Update last activity
                _connectionManager.UpdateActivity(clientId);

                var response = ProcessMessage(message, clientId);
                await Protocol.SendMessageAsync(stream, response);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{clientId}] Client error: {ex.Message}");
        }
        finally
        {
            // Remove session on disconnect
            _connectionManager.RemoveSession(clientId);
            Console.WriteLine($"[{clientId}] Client disconnected");
            client.Close();
        }
    }

    private Message ProcessMessage(Message message, string clientId)
    {
        try
        {
            switch (message.Type)
            {
                case MessageType.LOGIN:
                    return HandleLogin(message, clientId);

                case MessageType.QUERY:
                    return HandleQuery(message, clientId);

                case MessageType.LOGOUT:
                    return HandleLogout(message, clientId);

                default:
                    return new Message
                    {
                        Type = MessageType.ERROR,
                        Content = $"Unknown message type: {message.Type}"
                    };
            }
        }
        catch (Exception ex)
        {
            return new Message
            {
                Type = MessageType.ERROR,
                Content = $"Error: {ex.Message}"
            };
        }
    }

    private Message HandleLogin(Message message, string clientId)
    {
        var parts = message.Content.Split(' ');
        if (parts.Length < 2)
        {
            return new Message { Type = MessageType.ERROR, Content = "Invalid login format" };
        }

        var username = parts[0];
        var password = parts[1];

        var user = _authManager.Login(username, password);

        if (user == null)
        {
            return new Message { Type = MessageType.ERROR, Content = "Invalid credentials" };
        }

        // Set user to session
        _connectionManager.SetSessionUser(clientId, user);

        var session = _connectionManager.GetSession(clientId);
        return new Message
        {
            Type = MessageType.RESULT,
            Content = $"LOGIN_SUCCESS:{user.Username}:{user.Role}:{session?.SessionId}"
        };
    }

    private Message HandleQuery(Message message, string clientId)
    {
        // Get current user from session (per-connection)
        var user = _connectionManager.GetCurrentUser(clientId);
        if (user == null)
        {
            return new Message { Type = MessageType.ERROR, Content = "Not authenticated. Please login first." };
        }

        // Parse query
        var ast = _parser.Parse(message.Content);
        if (ast == null)
        {
            return new Message { Type = MessageType.ERROR, Content = "Invalid query" };
        }

        // Handle USE command - update session's current KB
        if (ast.Type.Equals("USE", StringComparison.OrdinalIgnoreCase))
        {
            var kbName = DetermineKbName(ast);
            if (kbName != null)
            {
                // Check READ privilege before setting current KB
                if (!_authManager.CheckPrivilege(user, "SELECT", kbName))
                {
                    return new Message { Type = MessageType.ERROR, Content = "Permission denied" };
                }
                _connectionManager.SetSessionKb(clientId, kbName);
            }
        }

        // Get current KB from session if not specified in query
        var queryKbName = DetermineKbName(ast) ?? _connectionManager.GetCurrentKb(clientId);

        // Check privilege
        var action = DetermineAction(ast);

        if (!_authManager.CheckPrivilege(user, action, queryKbName))
        {
            return new Message { Type = MessageType.ERROR, Content = $"Permission denied: {action} on {queryKbName ?? "system"}" };
        }

        // Execute query
        var result = _knowledgeManager.Execute(ast, user);

        return new Message
        {
            Type = MessageType.RESULT,
            Content = JsonSerializer.Serialize(result)
        };
    }

    private Message HandleLogout(Message message, string clientId)
    {
        // Clear user from session
        _connectionManager.SetSessionUser(clientId, null);
        return new Message { Type = MessageType.RESULT, Content = "LOGOUT_SUCCESS" };
    }

    private string DetermineAction(AstNode ast)
    {
        return ast.Type.ToUpper();
    }

    private string? DetermineKbName(AstNode ast)
    {
        return ast.KbName;
    }

    /// <summary>
    /// Generate unique client ID for each connection
    /// Format: conn_<timestamp>_<random>
    /// </summary>
    private string GenerateClientId()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = Guid.NewGuid().ToString("N")[..8];
        return $"conn_{timestamp}_{random}";
    }
}
```

### 3.5 Cài đặt Parser

#### File: `KBMS.Parser/QueryParser.cs`
```csharp
using System;
using System.Collections.Generic;
using KBMS.Parser.Ast;

namespace KBMS.Parser;

public class QueryParser
{
    private readonly Lexer _lexer = new();

    public AstNode? Parse(string query)
    {
        var tokens = _lexer.Tokenize(query);
        if (tokens.Count == 0)
            return null;

        var parser = new ParserCore(tokens);
        return parser.Parse();
    }
}

public class ParserCore
{
    private readonly List<Token> _tokens;
    private int _position = 0;

    public ParserCore(List<Token> tokens)
    {
        _tokens = tokens;
    }

    public AstNode? Parse()
    {
        var token = Peek();

        if (token == null)
            return null;

        return token.Type.ToUpper() switch
        {
            "CREATE" => ParseCreate(),
            "DROP" => ParseDrop(),
            "USE" => ParseUse(),
            "SELECT" => ParseSelect(),
            "INSERT" => ParseInsert(),
            "UPDATE" => ParseUpdate(),
            "DELETE" => ParseDelete(),
            "SOLVE" => ParseSolve(),
            "SHOW" => ParseShow(),
            "GRANT" => ParseGrant(),
            _ => null
        };
    }

    private AstNode ParseCreate()
    {
        Consume("CREATE");

        var next = Peek();
        if (next?.Value == "KNOWLEDGE")
        {
            // CREATE KNOWLEDGE BASE <name>
            Consume("KNOWLEDGE");
            Consume("BASE");
            var name = Consume(TokenType.IDENTIFIER)?.Value;
            return new CreateKbNode { KbName = name! };
        }
        else if (next?.Value == "USER")
        {
            // CREATE USER <username> PASSWORD <password>
            Consume("USER");
            var username = Consume(TokenType.IDENTIFIER)?.Value;
            Consume("PASSWORD");
            var password = Consume(TokenType.STRING)?.Value;
            return new CreateUserNode { Username = username!, Password = password! };
        }

        throw new ParseException("Unknown CREATE statement");
    }

    private AstNode ParseDrop()
    {
        Consume("DROP");
        Consume("KNOWLEDGE");
        Consume("BASE");
        var name = Consume(TokenType.IDENTIFIER)?.Value;
        return new DropKbNode { KbName = name! };
    }

    private AstNode ParseUse()
    {
        Consume("USE");
        var name = Consume(TokenType.IDENTIFIER)?.Value;
        return new UseKbNode { KbName = name! };
    }

    private AstNode ParseSelect()
    {
        Consume("SELECT");
        var conceptName = Consume(TokenType.IDENTIFIER)?.Value;
        var conditions = new Dictionary<string, object>();

        if (Peek()?.Value == "WHERE")
        {
            Consume("WHERE");
            conditions = ParseConditions();
        }

        return new SelectNode
        {
            ConceptName = conceptName!,
            Conditions = conditions
        };
    }

    private AstNode ParseInsert()
    {
        Consume("INSERT");
        Consume("INTO");
        var conceptName = Consume(TokenType.IDENTIFIER)?.Value;
        Consume("VALUES");
        Consume("(");
        var values = ParseValueList();
        Consume(")");

        return new InsertNode
        {
            ConceptName = conceptName!,
            Values = values
        };
    }

    private Dictionary<string, object> ParseConditions()
    {
        var conditions = new Dictionary<string, object>();

        while (true)
        {
            var key = Consume(TokenType.IDENTIFIER)?.Value;
            Consume("=");
            var value = ParseValue();

            if (key != null && value != null)
            {
                conditions[key] = value;
            }

            if (Peek()?.Value != ",")
                break;

            Consume(",");
        }

        return conditions;
    }

    private Dictionary<string, object> ParseValueList()
    {
        var values = new Dictionary<string, object>();

        while (true)
        {
            var key = Consume(TokenType.IDENTIFIER)?.Value;
            Consume("=");
            var value = ParseValue();

            if (key != null && value != null)
            {
                values[key] = value;
            }

            if (Peek()?.Value != ",")
                break;

            Consume(",");
        }

        return values;
    }

    private object? ParseValue()
    {
        var token = Consume();
        return token?.Type switch
        {
            TokenType.NUMBER => double.Parse(token.Value),
            TokenType.STRING => token.Value.Trim('\''),
            TokenType.IDENTIFIER => token.Value,
            _ => null
        };
    }

    private AstNode ParseSolve()
    {
        Consume("SOLVE");
        var conceptName = Consume(TokenType.IDENTIFIER)?.Value;
        Consume("KNOWN");
        var known = ParseValueList();
        Consume("FIND");
        var find = Consume(TokenType.IDENTIFIER)?.Value;

        return new SolveNode
        {
            ConceptName = conceptName!,
            Known = known,
            Find = find!
        };
    }

    private AstNode ParseGrant()
    {
        Consume("GRANT");
        var privilege = Consume(TokenType.IDENTIFIER)?.Value;
        Consume("ON");
        var kbName = Consume(TokenType.IDENTIFIER)?.Value;
        Consume("TO");
        var username = Consume(TokenType.IDENTIFIER)?.Value;

        return new GrantNode
        {
            Privilege = privilege!,
            KbName = kbName!,
            Username = username!
        };
    }

    private Token? Peek()
    {
        return _position < _tokens.Count ? _tokens[_position] : null;
    }

    private Token? Consume()
    {
        return _position < _tokens.Count ? _tokens[_position++] : null;
    }

    private Token? Consume(string expectedValue)
    {
        var token = Peek();
        if (token?.Value.Equals(expectedValue, StringComparison.OrdinalIgnoreCase) == true)
        {
            return Consume();
        }
        throw new ParseException($"Expected '{expectedValue}', got '{token?.Value}'");
    }

    private Token? Consume(TokenType expectedType)
    {
        var token = Peek();
        if (token?.Type == expectedType)
        {
            return Consume();
        }
        throw new ParseException($"Expected {expectedType}, got {token?.Type}");
    }
}
```

#### File: `KBMS.Parser/Lexer.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace KBMS.Parser;

public class Lexer
{
    private static readonly TokenRule[] Rules =
    {
        new(TokenType.WHITESPACE, @"\s+"),
        new(TokenType.COMMENT, @"--.*"),
        new(TokenType.NUMBER, @"\d+(\.\d+)?"),
        new(TokenType.STRING, @"'[^']*'"),
        new(TokenType.IDENTIFIER, @"[a-zA-Z_][a-zA-Z0-9_]*"),
        new(TokenType.OPERATOR, @"[+\-*/^=<>]"),
        new(TokenType.PUNCTUATION, @"[(),.;]"),
    };

    public List<Token> Tokenize(string input)
    {
        var tokens = new List<Token>();
        int position = 0;

        while (position < input.Length)
        {
            var matched = false;

            foreach (var rule in Rules)
            {
                var regex = new Regex($"^{rule.Pattern}");
                var match = regex.Match(input[position..]);

                if (match.Success)
                {
                    var value = match.Value;
                    position += value.Length;

                    if (rule.Type != TokenType.WHITESPACE && rule.Type != TokenType.COMMENT)
                    {
                        tokens.Add(new Token(rule.Type, value));
                    }

                    matched = true;
                    break;
                }
            }

            if (!matched)
            {
                throw new LexerException($"Unknown character at position {position}: {input[position]}");
            }
        }

        return tokens;
    }
}

public record TokenRule(TokenType Type, string Pattern);

public class Token
{
    public TokenType Type { get; }
    public string Value { get; }

    public Token(TokenType type, string value)
    {
        Type = type;
        Value = value;
    }
}

public enum TokenType
{
    IDENTIFIER,
    NUMBER,
    STRING,
    OPERATOR,
    PUNCTUATION,
    WHITESPACE,
    COMMENT
}
```

### 3.6 Cài đặt CLI

#### File: `KBMS.CLI/Cli.cs`
```csharp
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using KBMS.Network;
using KBMS.Models;

namespace KBMS.CLI;

public class Cli
{
    private readonly string _host;
    private readonly int _port;
    private TcpClient? _client;
    private NetworkStream? _stream;

    public Cli(string host = "localhost", int port = 3307)
    {
        _host = host;
        _port = port;
    }

    public async Task ConnectAsync()
    {
        _client = new TcpClient();
        await _client.ConnectAsync(_host, _port);
        _stream = _client.GetStream();
        Console.WriteLine($"Connected to KBMS Server at {_host}:{_port}");
    }

    public async Task DisconnectAsync()
    {
        if (_stream != null)
        {
            await Protocol.SendMessageAsync(_stream, new Message
            {
                Type = MessageType.LOGOUT,
                Content = ""
            });
        }
        _stream?.Close();
        _client?.Close();
    }

    public async Task<Message> ExecuteCommandAsync(string command)
    {
        if (_stream == null)
            throw new InvalidOperationException("Not connected");

        var message = new Message
        {
            Type = MessageType.QUERY,
            Content = command
        };

        await Protocol.SendMessageAsync(_stream, message);
        return await Protocol.ReceiveMessageAsync(_stream);
    }

    public async Task StartInteractiveAsync()
    {
        await ConnectAsync();

        Console.WriteLine("KBMS CLI v1.0");
        Console.WriteLine("Type 'HELP' for available commands, 'EXIT' to quit.\n");

        string? currentKb = null;
        string? currentUser = null;

        while (true)
        {
            string prompt = currentUser != null
                ? $"kbms{(currentKb != null ? $"/{currentKb}" : "")}> "
                : "login> ";

            Console.Write(prompt);
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input))
                continue;

            if (input.ToUpper() == "EXIT")
                break;

            if (input.ToUpper() == "HELP")
            {
                ShowHelp();
                continue;
            }

            // Handle LOGIN separately
            if (input.StartsWith("LOGIN", StringComparison.OrdinalIgnoreCase))
            {
                var msg = await ExecuteCommandAsync(input);
                if (msg.Type == MessageType.RESULT && msg.Content.StartsWith("LOGIN_SUCCESS"))
                {
                    var parts = msg.Content.Split(':');
                    currentUser = parts[1];
                    Console.WriteLine($"Logged in as {currentUser} ({parts[2]})");
                }
                else
                {
                    Console.WriteLine($"Error: {msg.Content}");
                }
                continue;
            }

            // Check if logged in
            if (currentUser == null)
            {
                Console.WriteLine("Please login first. Usage: LOGIN <username> <password>");
                continue;
            }

            var response = await ExecuteCommandAsync(input);

            if (response.Type == MessageType.RESULT)
            {
                // Handle USE command
                if (input.StartsWith("USE", StringComparison.OrdinalIgnoreCase))
                {
                    currentKb = input.Split()[1];
                    Console.WriteLine($"Using knowledge base: {currentKb}");
                }
                else
                {
                    Console.WriteLine(response.Content);
                }
            }
            else if (response.Type == MessageType.ERROR)
            {
                Console.WriteLine($"Error: {response.Content}");
            }
        }

        await DisconnectAsync();
    }

    private void ShowHelp()
    {
        Console.WriteLine("Available Commands:");
        Console.WriteLine("  LOGIN <username> <password>     - Login to server");
        Console.WriteLine("  CREATE KNOWLEDGE BASE <name>   - Create new knowledge base");
        Console.WriteLine("  DROP KNOWLEDGE BASE <name>     - Drop knowledge base");
        Console.WriteLine("  USE <name>                     - Select knowledge base");
        Console.WriteLine("  SELECT <concept> WHERE <cond>   - Query objects");
        Console.WriteLine("  INSERT INTO <concept> VALUES (...) - Insert object");
        Console.WriteLine("  UPDATE <concept> SET ...      - Update object");
        Console.WriteLine("  DELETE FROM <concept> WHERE ... - Delete object");
        Console.WriteLine("  SOLVE <concept> KNOWN ... FIND - Solve reasoning");
        Console.WriteLine("  CREATE USER <name> PASSWORD <p> - Create user");
        Console.WriteLine("  GRANT <privilege> ON <kb> TO <user> - Grant privilege");
        Console.WriteLine("  SHOW CONCEPTS / SHOW RULES     - Show knowledge");
        Console.WriteLine("  EXIT                           - Exit CLI");
    }
}
```

#### File: `KBMS.CLI/Program.cs`
```csharp
using System;
using System.Threading.Tasks;
using KBMS.CLI;

namespace KBMS.CLI;

class Program
{
    static async Task Main(string[] args)
    {
        var cli = new Cli();
        await cli.StartInteractiveAsync();
    }
}
```

### 3.7 Network Protocol

#### File: `KBMS.Network/Protocol.cs`
```csharp
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace KBMS.Network;

public enum MessageType : byte
{
    LOGIN = 1,
    QUERY = 2,
    RESULT = 3,
    ERROR = 4,
    LOGOUT = 5
}

public class Message
{
    public MessageType Type { get; set; }
    public string Content { get; set; }
    public string? SessionId { get; set; }  // Session ID for authentication
}

public class Message
{
    public MessageType Type { get; set; }
    public string Content { get; set; }
}

public class Protocol
{
    public static async Task<Message> ReceiveMessageAsync(Stream stream)
    {
        // Read length (4 bytes, big-endian)
        var lengthBytes = new byte[4];
        await stream.ReadAsync(lengthBytes, 0, 4);
        var length = BitConverter.ToInt32(lengthBytes.Reverse().ToArray(), 0);

        if (length == 0)
            throw new IOException("Connection closed");

        // Read type (1 byte)
        var typeByte = new byte[1];
        await stream.ReadAsync(typeByte, 0, 1);
        var type = (MessageType)typeByte[0];

        // Read session ID length (2 bytes) - optional
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

        // Read payload
        var payloadLength = length - 2 - sessionIdLength;  // Subtract sessionIdLength field and sessionId bytes
        var payloadBytes = new byte[payloadLength];
        await stream.ReadAsync(payloadBytes, 0, payloadLength);
        var content = Encoding.UTF8.GetString(payloadBytes);

        return new Message
        {
            Type = type,
            Content = content,
            SessionId = sessionId
        };
    }

    public static async Task SendMessageAsync(Stream stream, Message message)
    {
        var contentBytes = Encoding.UTF8.GetBytes(message.Content);
        var sessionIdBytes = string.IsNullOrEmpty(message.SessionId)
            ? Array.Empty<byte>()
            : Encoding.UTF8.GetBytes(message.SessionId);

        var sessionIdLength = (ushort)sessionIdBytes.Length;
        var totalLength = contentBytes.Length + 2 + sessionIdBytes.Length;  // +2 for sessionIdLength field

        // Length (4 bytes, big-endian)
        var lengthBytes = BitConverter.GetBytes(totalLength).Reverse().ToArray();
        await stream.WriteAsync(lengthBytes, 0, 4);

        // Type (1 byte)
        var typeBytes = new[] { (byte)message.Type };
        await stream.WriteAsync(typeBytes, 0, 1);

        // Session ID length (2 bytes)
        var sessionIdLengthBytes = BitConverter.GetBytes(sessionIdLength).Reverse().ToArray();
        await stream.WriteAsync(sessionIdLengthBytes, 0, 2);

        // Session ID (if present)
        if (sessionIdLength > 0)
        {
            await stream.WriteAsync(sessionIdBytes, 0, sessionIdLength);
        }

        // Payload
        await stream.WriteAsync(contentBytes, 0, contentBytes.Length);
        await stream.FlushAsync();
    }
}
```

---

## SESSION MANAGEMENT ARCHITECTURE

### Vấn đề đã giải quyết

| Trước | Sau khi bổ sung |
|-------|-----------------|
| `_currentUser` global state cho tất cả connections | Per-connection session trong ConnectionManager |
| Multi-user conflict | Mỗi client có session riêng biệt |
| Không có session ID | Session ID được tạo và gửi kèm mỗi message |
| Không thể validate session | Session validation trước khi xử lý query |
| Current KB không được track per-user | Current KB được lưu trong session |

### Flow chi tiết

```
┌─────────────┐                    ┌──────────────────────┐
│    CLI      │                    │   KBMS Server         │
└──────┬──────┘                    └──────────┬───────────┘
       │                                      │
       │ 1. TCP Connect                       │
       │─────────────────────────────────────>│
       │                                      │ Generate Client ID
       │                                      │ Create Session
       │                                      │
       │ 2. LOGIN user password               │
       │─────────────────────────────────────>│
       │                                      │ Verify password
       │                                      │ Set User to Session
       │                                      │
       │ 3. LOGIN_SUCCESS:USER:ROLE:SID      │
       │<─────────────────────────────────────│
       │    (save SessionId)                 │
       │                                      │
       │ 4. QUERY (with SessionId)            │
       │─────────────────────────────────────>│
       │                                      │ Validate SessionId
       │                                      │ Get User from Session
       │                                      │ Check Permission
       │                                      │ Execute Query
       │                                      │
       │ 5. RESULT                            │
       │<─────────────────────────────────────│
       │                                      │
       │ ... (more queries with SessionId) ...│
       │                                      │
       │ 6. LOGOUT                            │
       │─────────────────────────────────────>│
       │                                      │ Clear User from Session
       │                                      │
       │ 7. LOGOUT_SUCCESS                    │
       │<─────────────────────────────────────│
       │                                      │
       │ 8. TCP Disconnect                    │
       │─────────────────────────────────────>│
       │                                      │ Remove Session
```

### Message Format (cập nhật)

```
┌─────────────────────────────────────────────────────────┐
│ Length (4 bytes)      - Total message length            │
├─────────────────────────────────────────────────────────┤
│ Type (1 byte)          - LOGIN/QUERY/RESULT/ERROR/LOGOUT│
├─────────────────────────────────────────────────────────┤
│ SessionIdLen (2 bytes) - Session ID length (0-65535)    │
├─────────────────────────────────────────────────────────┤
│ SessionId (variable)    - Hex string session ID        │
├─────────────────────────────────────────────────────────┤
│ Content (variable)      - Query/Result/Error message    │
└─────────────────────────────────────────────────────────┘
```

### Session Lifecycle

```
Client Connect
       ↓
Generate Client ID
       ↓
Create Session (no user)
       ↓
LOGIN command
       ↓
Verify credentials
       ↓
Set User to Session
       ↓
[Authenticated State]
       ↓
Queries (with validation)
       ↓
LOGOUT or Timeout
       ↓
Clear User from Session
       ↓
Client Disconnect
       ↓
Remove Session completely
```

---

## THỨ TỰ TRIỂN KHAI

### Phase 1: Project Setup & Models (Tuần 1)
1. Tạo C# Solution với các projects:
   - `KBMS.Models` (Class Library) - Data Models
   - `KBMS.Storage` (Class Library) - Storage Engine
   - `KBMS.Network` (Class Library) - Network Protocol
   - `KBMS.Server` (Console App) - Server
   - `KBMS.CLI` (Console App) - CLI Client
   - `KBMS.Tests` (xUnit) - Unit Tests

2. Cài đặt Data Models (C, H, R, Ops, Funcs, Object, User)
3. Cài đặt User model với:
   - UserRole (ROOT, USER)
   - Privilege (READ, WRITE, ADMIN)
   - KbPrivileges dictionary

### Phase 2: Storage Engine (Tuần 2)
1. Cài đặt Encryption/Decryption (AES-256)
2. Cài đặt Binary Format (.bin với header, encrypted data, CRC32)
3. Cài đặt Index Manager (B+ Tree)
4. Cài đặt WAL Manager
5. Cài đặt Storage Engine với CRUD cơ bản

### Phase 3: Server Components (Tuần 3)
1. Cài đặt Network Protocol (TCP Socket, message format):
   - Message format: Length (4B) + Type (1B) + SessionIdLength (2B) + SessionId + Payload
   - Session tracking trong message header
2. Cài đặt ConnectionManager với session management:
   - Create session khi client connect
   - Session-User mapping per-connection
   - Session timeout và cleanup
   - Validate session trước khi xử lý query
3. Cài đặt Lexer (tokenizer)
4. Cài đặt Query Parser (AST builder)
5. Cài đặt Authentication Manager với permission checking:
   - ROOT: Luôn trả về true
   - USER: Kiểm tra theo KbPrivileges
   - Loại bỏ _currentUser global state
6. Cài đặt KbmsServer với ConnectionManager:
   - Generate unique client ID per connection
   - Use ConnectionManager thay vì AuthenticationManager._currentUser
   - Session validation trong HandleQuery

### Phase 4: Knowledge Manager & Reasoning (Tuần 4)
1. Cài đặt Knowledge Manager
2. Cài đặt Forward Chaining
3. Cài đặt Backward Chaining
4. Cài đặt SOLVE command

### Phase 5: CLI & Testing (Tuần 5)
1. Cài đặt CLI Interface
2. Test end-to-end với các lệnh
3. Test phân quyền (ROOT vs USER)
4. Đánh giá hiệu năng

---

## FILE CHÍNH CẦN TẠO

| File | Mô tả |
|------|-------|
| `KBMS.Models/*.cs` | Các data models (KnowledgeBase, Concept, Relation, Operator, Function, Rule, ObjectInstance, User) |
| `KBMS.Storage/Encryption.cs` | Mã hóa/giải mã AES-256 |
| `KBMS.Storage/BinaryFormat.cs` | Format file .bin |
| `KBMS.Storage/IndexManager.cs` | Indexing (B+ Tree) |
| `KBMS.Storage/WalManager.cs` | Write Ahead Log |
| `KBMS.Storage/Engine.cs` | Storage Engine chính |
| `KBMS.Network/Protocol.cs` | Network Protocol (TCP) với Session ID |
| `KBMS.Parser/Lexer.cs` | Lexer/Tokenizer |
| `KBMS.Parser/QueryParser.cs` | Query Parser |
| `KBMS.Parser/Ast/*.cs` | AST nodes |
| `KBMS.Server/ConnectionManager.cs` | Connection & Session management per-connection |
| `KBMS.Server/AuthenticationManager.cs` | Authentication & Permission checking |
| `KBMS.Server/KbmsServer.cs` | KBMS Server với ConnectionManager |
| `KBMS.Server/KnowledgeManager.cs` | Knowledge Manager |
| `KBMS.Reasoning/ReasoningEngine.cs` | Reasoning Engine |
| `KBMS.CLI/Cli.cs` | CLI Interface |

---

## CÔNG NGHỆ

- **.NET:** .NET 6.0 hoặc .NET 8.0
- **Language:** C# 10.0+
- **Network:** System.Net.Sockets (TCP)
- **Encryption:** System.Security.Cryptography (AES-256)
- **Password Hashing:** BCrypt.Net-Next
- **Serialization:** System.Text.Json
- **Testing:** xUnit

---

## KIỂM THỬ (VERIFICATION)

### Test Cases:

#### 1. Storage Engine Test
```
1. Tạo KB mới với name="geometry"
2. Verify file data/geometry/metadata.bin được tạo
3. Verify file được mã hóa (không đọc được bằng text editor)
4. Insert object TAMGIAC với a=3, b=4, c=5
5. Verify data/geometry/objects.bin được tạo và mã hóa
6. Select TAMGIAC WHERE a=3
7. Verify kết quả trả về object đã insert
8. Update TAMGIAC SET a=6
9. Verify object được update
10. Delete object
11. Verify object bị xóa
```

#### 2. Permission Test (ROOT vs USER)
```
1. Tạo user ROOT
2. Login với ROOT
3. CREATE KNOWLEDGE BASE geometry -> SUCCESS (không cần check)
4. CREATE USER testuser PASSWORD pass123 -> SUCCESS
5. CREATE USER testadmin PASSWORD pass123 -> SUCCESS

6. Logout
7. Login với testuser (USER, không có privilege)
8. CREATE KNOWLEDGE BASE testkb -> FAILED (không có SystemAdmin)
9. USE geometry -> FAILED (không có READ privilege)
10. SELECT TAMGIAC -> FAILED

11. Login với ROOT
12. GRANT READ ON geometry TO testuser
13. Login với testuser
14. USE geometry -> SUCCESS
15. SELECT TAMGIAC -> SUCCESS
16. INSERT INTO TAMGIAC VALUES (...) -> FAILED (không có WRITE)

17. Login với ROOT
18. GRANT WRITE ON geometry TO testuser
19. Login với testuser
20. INSERT INTO TAMGIAC VALUES (...) -> SUCCESS

21. Login với ROOT
22. GRANT ADMIN ON geometry TO testuser
23. Login với testuser
24. DROP KNOWLEDGE BASE geometry -> FAILED (ADMIN không thể DROP, chỉ ROOT)
```

#### 3. Query Test
```
1. LOGIN admin password
2. CREATE KNOWLEDGE BASE geometry -> SUCCESS
3. USE geometry -> SUCCESS
4. CREATE CONCEPT TAMGIAC (a, b, c, S) -> SUCCESS
5. INSERT INTO TAMGIAC VALUES (a=3, b=4, c=5) -> SUCCESS
6. SELECT TAMGIAC WHERE a=3 -> SUCCESS (return 1 object)
7. UPDATE TAMGIAC SET a=6 WHERE a=3 -> SUCCESS
8. SELECT TAMGIAC WHERE a=6 -> SUCCESS
9. DELETE FROM TAMGIAC WHERE a=6 -> SUCCESS
10. SELECT TAMGIAC -> SUCCESS (return empty)
```

#### 4. Reasoning Test
```
1. Tạo concept TAMGIAC với variables a, b, c, S
2. Tạo luật: S = sqrt(a^2 + b^2 + c^2) ??? (example)
3. INSERT INTO TAMGIAC VALUES (a=3, b=4, c=5)
4. SOLVE TAMGIAC KNOWN a=3 b=4 c=5 FIND S -> S = 7.07...
```

#### 5. Connection & Session Test
```
1. Start KBMS Server (dotnet run --project KBMS.Server)
2. Start 2 CLI instances simultaneously (2 separate terminals)
3. CLI #1: LOGIN user1 password123
4. CLI #2: LOGIN user2 password123
5. Verify both logins successful (không conflict)
6. CLI #1: CREATE KNOWLEDGE BASE kb1 -> SUCCESS (nếu user1 có quyền)
7. CLI #2: USE kb1 -> FAILED (user2 chưa được cấp quyền)
8. Verify mỗi CLI có session riêng biệt
9. Disconnect CLI #1
10. Verify CLI #2 vẫn hoạt động bình thường
11. Test session timeout: chờ > timeout period
12. Verify session bị cleanup
```

#### 6. Network Test
```
1. Start KBMS Server (dotnet run --project KBMS.Server)
2. Start CLI (dotnet run --project KBMS.CLI)
3. Connect from CLI to Server
4. Execute LOGIN command
5. Execute CREATE KNOWLEDGE BASE
6. Execute INSERT, SELECT
7. Verify responses
8. Disconnect
```

### Chạy kiểm thử:
```bash
# Build solution
dotnet build KBMS.sln

# Run Server
dotnet run --project KBMS.Server

# Run CLI
dotnet run --project KBMS.CLI

# Run tests
dotnet test KBMS.Tests
```

---

## CÔNG VIỆC 6: KIỂM TRA KIẾM BỐ SUNG KBQL VÀ TEST SUITES

### 6.1 Phân tích KBQL hiện tại và cập nhật Documentation

Đã cập nhật các tài liệu kỹ thuật sau:

| File | Thay đổi | Trạng thái |
|------|---------|---------|
| `docs/sql-syntax.md` | Thêm bảng tóm tắt với trạng thái (✓, ⚠️, ✗) | ✓ |
| `docs/sql-syntax.md` | Thêm JOIN Syntax chi tiết với ví dụ | ✓ |
| `docs/sql-syntax.md` | Thêm Aggregation Functions (COUNT, SUM, AVG, MAX, MIN) | ✓ |
| `docs/sql-syntax.md` | Thêm GROUP BY và HAVING syntax | ✓ |
| `docs/sql-syntax.md` | Thêm ORDER BY và LIMIT/OFFSET syntax | ✓ |
| `docs/sql-syntax.md` | Thêm Spatial/Geometric Queries (NEAREST, INSIDE, INTERSECT, v.v.) | ✓ |
| `docs/ast-design.md` | Cập nhật mapping table với status column | ✓ |
| `docs/ast-design.md` | Mở rộng SelectNode với Aggregation, GroupBy, OrderBy, Limit | ✓ |
| `docs/ast-design.md` | Thêm JoinClause, AggregateClause, OrderByClause, LimitClause classes | ✓ |
| `docs/integration-mapping.md` | Cập nhật mapping với status column | ✓ |
| `docs/integration-mapping.md` | Thêm 8 entries cho features mới | ✓ |
| `docs/data-structure.md` | Thêm AggregateResult, GroupByResult, JoinClause, JoinResult | ✓ |

---

### 6.2 Tạo Test Suite Multi-Domain

Đã tạo tài liệu test case toàn diện cho 4 domain:

| Domain | Test Cases | Status |
|--------|-----------|--------|
| **Hình học (Geometry)** | 4 levels (Easy → Expert) với 25 test cases | ✓ |
| **Đại số (Algebra)** | POLYNOMIAL, EQUATION, MATRIX concepts | ✓ |
| **Vật lý (Physics)** | OBJECT, FORCE, ENERGY concepts | ✓ |
| **Tài chính (Finance)** | INVOICE, PRODUCT, ORDER concepts | ✓ |

**File:** `docs/test-suites.md`

---

### 6.3 Tổng kết KBQL Coverage

#### Trạng thái tổng thể

| Category | Features | Total | Implemented | Planned |
|---------|----------|-------|------------|---------|
| **DDL** | 9 | 9 (100%) | 0 |
| **User Management** | 4 | 4 (100%) | 0 |
| **Basic DML** | 4 | 4 (100%) | 0 |
| **JOIN** | 4 | 1 (25%) | 3 (75%) |
| **Aggregation** | 5 | 0 (0%) | 5 (100%) |
| **GROUP BY/HAVING** | 2 | 0 (0%) | 2 (100%) |
| **ORDER BY** | 2 | 0 (0%) | 2 (100%) |
| **LIMIT/OFFSET** | 2 | 0 (0%) | 2 (100%) |
| **Reasoning** | 1 | 1 (100%) | 0 |
| **SHOW** | 7 | 7 (100%) | 0 |

**Tổng cộng:** **42 features** → **30 implemented (71.4%)**, **12 planned (28.6%)**

---

### 6.4 Ưu tiên Implementation

| Priority | Feature | Impact | Dependencies |
|----------|---------|--------|-------------|
| **HIGH** | COUNT, SUM, AVG, MAX, MIN | Critical cho Expert queries | None |
| **HIGH** | GROUP BY, HAVING | Kết hợp với aggregation | Aggregation |
| **HIGH** | ORDER BY, LIMIT/OFFSET | Sorting, pagination | None |
| **MEDIUM** | JOIN syntax clarification | Better documentation | None |
| **MEDIUM** | Function returns (object, list) | Geometric functions | None |
| **LOW** | Spatial queries (NEAREST, INSIDE) | Advanced features | JOIN |

---

### 6.5 Files Documentation Updates

| File | Section | Update |
|------|---------|--------|
| `docs/sql-syntax.md` | DML summary table with status | ✓ |
| `docs/sql-syntax.md` | JOIN syntax section with examples | ✓ |
| `docs/sql-syntax.md` | Aggregation functions section | ✓ |
| `docs/sql-syntax.md` | GROUP BY/HAVING section | ✓ |
| `docs/sql-syntax.md` | ORDER BY/LIMIT section | ✓ |
| `docs/sql-syntax.md` | Spatial/Geometric queries section | ✓ |
| `docs/ast-design.md` | Mapping table with status | ✓ |
| `docs/ast-design.md` | Enhanced SelectNode | ✓ |
| `docs/ast-design.md` | JoinClause, AggregateClause classes | ✓ |
| `docs/integration-mapping.md` | Mapping table with status | ✓ |
| `docs/integration-mapping.md` | Overall progress summary | ✓ |
| `docs/data-structure.md` | AggregateResult, GroupByResult | ✓ |
| `docs/data-structure.md` | JoinClause, JoinResult | ✓ |
| `docs/test-suites.md` | Complete test suite (NEW) | ✓ |

---

### 6.6 Verification Queries

```sql
-- === LEVEL 1: Easy ===
SELECT TAMGIAC WHERE a=3;
SELECT DIEM WHERE x=0 AND y=0;
INSERT INTO TAMGIAC VALUES (a=3, b=4, c=5, S=0);
SOLVE TAMGIAC FOR CV GIVEN a=3, b=4, c=5;
SHOW CONCEPTS;

-- === LEVEL 2: Medium ===
SELECT DOAN JOIN THUOC WHERE A=D1;
SELECT DOAN JOIN SONG WHERE SONG_DOAN=D1D2;
SOLVE TAMGIAC FOR GOC_A.a GIVEN a=3, b=4, c=5;

-- === LEVEL 3: Hard ===
SOLVE TAMGIAC FOR is_right GIVEN a=3, b=4, c=5;
SOLVE TAMGIAC FOR ha GIVEN a=3, b=4, c=5;
SOLVE TAMGIAC FOR O GIVEN a=3, b=4, c=5;

-- === LEVEL 4: Expert (sau khi bổ sung) ===
SELECT COUNT(*) FROM TAMGIAC WHERE is_right=true;
SELECT AVG(S) FROM TAMGIAC WHERE a>3;
SELECT TAMGIAC ORDER BY S DESC LIMIT 1;
SELECT TAMGIAC GROUP BY is_isosceles HAVING COUNT(*) > 5;

-- === Domain: Đại số ===
SELECT POLYNOMIAL WHERE degree=2;
SOLVE EQUATION FOR x GIVEN left=(x²-4), right=0;

-- === Domain: Vật lý ===
SELECT OBJECT WHERE mass>10;
SOLVE FORCE FOR magnitude GIVEN mass=5, acceleration=9.8;

-- === Domain: Tài chính ===
SELECT INVOICE WHERE amount>1000;
SELECT PRODUCT ORDER BY price DESC LIMIT 10;
SELECT COUNT(*) FROM ORDER GROUP BY customer;
```

---

## KẾ HOẠCH TRÁI KHAI TÀI LIỆU

**Hoàn thành:** Tất cả documentation đã được cập nhật để phản ánh KBQL coverage đầy đủ.

**Công việc tiếp theo:** Implement các features đã được định nghĩa trong tài liệu:
1. Implement Aggregation Functions trong Storage Engine
2. Implement JOIN support trong Query Parser và Knowledge Manager
3. Implement GROUP BY và HAVING
4. Implement ORDER BY và LIMIT/OFFSET
5. Implement Spatial/Geometric query support

**Test suite:** Sử dụng file `docs/test-suites.md` để verify tất cả test cases sau khi implement.