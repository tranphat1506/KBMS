# Storage Schema - Binary Storage

## Tong quan

Tài liệu này mô tả cấu trúc lưu trữ của KBMS:
- **Binary Storage**: Serialize data structures sang binary file với encryption
- **No logical text format**: Các file .txt không còn được sử dụng

---

## Mapping: SQL Statement ↔ Storage Operations

| SQL Statement | Storage Method | Binary File |
|---------------|----------------|-------------|
| `CREATE KNOWLEDGE BASE <name>` | `CreateKb(name)` | `data/<name>/` (directory) |
| `DROP KNOWLEDGE BASE <name>` | `DropKb(name)` | `data/<name>/` (entire dir) |
| `CREATE CONCEPT <name>` | `CreateConcept(concept)` | `data/<kb>/concepts.bin` |
| `DROP CONCEPT <name>` | `DropConcept(name)` | `data/<kb>/concepts.bin` |
| `ADD HIERARCHY <p> IS_A <c>` | `AddHierarchy(parent, child, type)` | `data/<kb>/hierarchies.bin` |
| `CREATE RELATION <name> ...` | `CreateRelation(relation)` | `data/<kb>/relations.bin` |
| `CREATE OPERATOR <sym> ...` | `CreateOperator(operator)` | `data/<kb>/operators.bin` |
| `CREATE FUNCTION <name> ...` | `CreateFunction(function)` | `data/<kb>/functions.bin` |
| `ADD COMPUTATION TO <c> ...` | `AddComputation(conceptName, comp)` | `data/<kb>/<CONCEPT>.bin` |
| `CREATE RULE <name> ...` | `CreateRule(rule)` | `data/<kb>/rules.bin` |
| `INSERT INTO <concept>` | `InsertObject(obj)` | `data/<kb>/facts.bin` |
| `SELECT <concept>` | `SelectObjects(conditions)` | `data/<kb>/facts.bin` |
| `UPDATE <concept>` | `UpdateObject(id, values)` | `data/<kb>/facts.bin` |
| `DELETE FROM <concept>` | `DeleteObject(id)` | `data/<kb>/facts.bin` |
| `SOLVE <concept>` | `SelectObjects(...) + Reasoning` | Multiple |
| `SHOW CONCEPTS` | `LoadConcepts()` | `data/<kb>/concepts.bin` |
| `SHOW RULES` | `LoadRules()` | `data/<kb>/rules.bin` |
| `CREATE USER <name>` | `CreateUser(user)` | `data/users/users.bin` |
| `DROP USER <name>` | `DropUser(name)` | `data/users/users.bin` |
| `GRANT <privilege>` | `GrantPrivilege(...)` | `data/users/users.bin` |
| `REVOKE <privilege>` | `RevokePrivilege(...)` | `data/users/users.bin` |

---

## Directory Structure

```
KBMS/
├── data/                                 # Physical storage root
│   ├── <kb_name>/                        # Each KB is a separate directory
│   │   ├── concepts.bin                  # Danh sách concepts
│   │   ├── hierarchies.bin               # Quan hệ phân cấp
│   │   ├── relations.bin                 # Quan hệ giữa concepts
│   │   ├── operators.bin                 # Các toán tử
│   │   ├── functions.bin                 # Các hàm
│   │   ├── rules.bin                      # Các luật suy luận
│   │   │
│   │   ├── <CONCEPT_NAME>.bin           # Chi tiết mỗi concept
│   │   │   # Variables, Constraints, Computation Relations, Rules
│   │   │
│   │   ├── facts.bin                     # Các instances/objects
│   │   ├── index.bin                      # B+ Tree index (tìm kiếm nhanh)
│   │   └── wal.log                       # Write Ahead Log
│   │
│   └── users/                            # User management
│       └── users.bin                     # Danh sách users
│
└── logs/                                 # Application logs
    └── kbms.log                          # Server log file
```

---

## Binary File Format

### Physical Storage Format

Tất cả file `.bin` trong KBMS sử dụng định dạng binary:

```
┌─────────────────────────────────────────────────────────────────┐
│                     KBMS Binary File Format                     │
├─────────────────────────────────────────────────────────────────┤
│  [4 bytes]  Magic Bytes: 0x4B 0x42 0x4D 0x53 ("KBMS")        │
│  [2 bytes]  Version: 0x0001 (version 1)                        │
│  [4 bytes]  Data Length (N) - length of encrypted data          │
│  [N bytes]  Encrypted Data:                                     │
│      [16 bytes] IV (Initialization Vector)                      │
│      [M bytes] AES-256 Encrypted Content (Binary Format)       │
│  [4 bytes]  CRC32 Checksum (of all data except checksum)      │
└─────────────────────────────────────────────────────────────────┘
```

---

## Binary Format (per data type)

### concepts.bin / hierarchies.bin / relations.bin / operators.bin / functions.bin / rules.bin

```
┌─────────────────────────────────────────────────────────────────┐
│  [4 bytes]  Magic Bytes: "KBMS"                               │
│  [2 bytes]  Version: 0x0001                                   │
│  [4 bytes]  Count (number of records)                        │
│  [4 bytes]  Data Length (N)                                   │
│  [16 bytes] IV                                                │
│  [N bytes]  AES-256 Encrypted List<T>                        │
│  [4 bytes]  CRC32                                            │
└─────────────────────────────────────────────────────────────────┘
```

### <CONCEPT_NAME>.bin

```
┌─────────────────────────────────────────────────────────────────┐
│  [4 bytes]  Magic Bytes: "KBMS"                               │
│  [2 bytes]  Version: 0x0001                                   │
│  [4 bytes]  KbId (Guid)                                       │
│  [N bytes]  AES-256 Encrypted ConceptDetail                   │
│  [4 bytes]  CRC32                                            │
└─────────────────────────────────────────────────────────────────┘
```

### facts.bin

```
┌─────────────────────────────────────────────────────────────────┐
│  [4 bytes]  Magic Bytes: "KBMS"                               │
│  [2 bytes]  Version: 0x0001                                   │
│  [4 bytes]  Count (number of facts)                           │
│  [4 bytes]  Data Length (N)                                   │
│  [16 bytes] IV                                                │
│  [N bytes]  AES-256 Encrypted List<Fact>                       │
│  [4 bytes]  CRC32                                            │
└─────────────────────────────────────────────────────────────────┘
```

### users.bin

```
┌─────────────────────────────────────────────────────────────────┐
│  [4 bytes]  Magic Bytes: "KBMS"                               │
│  [2 bytes]  Version: 0x0001                                   │
│  [4 bytes]  Count (number of users)                           │
│  [4 bytes]  Data Length (N)                                   │
│  [16 bytes] IV                                                │
│  [N bytes]  AES-256 Encrypted List<User>                        │
│  [4 bytes]  CRC32                                            │
└─────────────────────────────────────────────────────────────────┘
```

---

## File Summary

| File | Chứa gì |
|------|---------|
| `concepts.bin` | List tên tất cả concepts |
| `hierarchies.bin` | List các quan hệ phân cấp (IS_A, PART_OF) |
| `relations.bin` | List các relations giữa 2 concepts |
| `operators.bin` | List các toán tử (+, -, *, /, ^, =, >, <, ...) |
| `functions.bin` | List các hàm (sqrt, heron, distance, ...) |
| `rules.bin` | List các luật suy luận toàn cục |
| `<CONCEPT_NAME>.bin` | Chi tiết concept: Variables, Constraints, Computation Relations, Rules |
| `facts.bin` | Các instances/objects của tất cả concepts |
| `index.bin` | Index B+ Tree để tìm kiếm nhanh |
| `wal.log` | Log để recovery khi crash |
| `users.bin` | Danh sách users |

---

## Encryption Algorithm

### Algorithm Details
- **Algorithm:** AES-256 (Advanced Encryption Standard)
- **Mode:** CBC (Cipher Block Chaining)
- **Padding:** PKCS7
- **Key Derivation:** SHA-256 of encryption key string

### Key Derivation
```csharp
byte[] DeriveKey(string keyString)
{
    using var sha = SHA256.Create();
    return sha.ComputeHash(Encoding.UTF8.GetBytes(keyString));
}
```

---

## CRC32 Checksum

### Purpose
Đảm bảo tính toàn vẹn của file (detect corruption)

### Algorithm
```csharp
uint ComputeCrc32(byte[] data)
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
```

---

## Storage Engine Interface

```csharp
public interface IStorageEngine
{
    // Knowledge Base Operations
    void CreateKb(string name, Guid ownerId, string description);
    void DropKb(string name);
    bool KbExists(string name);

    // Concept Operations
    void CreateConcept(string conceptName);
    List<string> LoadConcepts(string kbName);
    void RemoveConcept(string kbName, string conceptName);

    // Concept Detail Operations
    void CreateConceptFile(string conceptName, ConceptDetail detail);
    ConceptDetail? LoadConceptDetail(string kbName, string conceptName);
    void UpdateConceptDetail(string kbName, string conceptName, ConceptDetail detail);

    // Hierarchy Operations
    void AddHierarchy(string parent, string child, HierarchyType type);
    List<Hierarchy> LoadHierarchies(string kbName);
    void RemoveHierarchy(string parent, string child);

    // Relation Operations
    void CreateRelation(Relation relation);
    List<Relation> LoadRelations(string kbName);
    void DropRelation(string kbName, string relationName);

    // Operator Operations
    void CreateOperator(Operator op);
    List<Operator> LoadOperators(string kbName);
    void DropOperator(string kbName, string symbol);

    // Function Operations
    void CreateFunction(Function func);
    List<Function> LoadFunctions(string kbName);
    void DropFunction(string kbName, string name);

    // Rule Operations
    void CreateRule(Rule rule);
    List<Rule> LoadRules(string kbName);
    void DropRule(string kbName, string ruleName);

    // Computation Operations
    void AddComputation(string conceptName, ComputationRelation comp);
    void RemoveComputation(string conceptName, string resultVar);

    // Fact/Instance Operations
    void InsertFact(string conceptName, Dictionary<string, object> values);
    List<Fact> LoadFacts(string kbName, string? conceptName = null);
    void UpdateFact(string kbName, Guid factId, Dictionary<string, object> values);
    void DeleteFact(string kbName, Guid factId);

    // User Operations
    User CreateUser(string username, string password, UserRole role);
    User? LoadUser(string username);
    List<User> LoadAllUsers();
    bool DeleteUser(string username);
}
```

---

## Error Handling

### Common Errors

| Error Type | Description | Recovery |
|------------|-------------|-----------|
| Invalid Magic Bytes | File không trong KBMS format | Cannot recover, report error |
| CRC32 Mismatch | File bị corrupted | Attempt recovery from WAL |
| Decryption Failed | Wrong encryption key | Prompt for correct key |
| Parse Error | Invalid binary format | Report line number và issue |
| Unknown Concept | Referenced concept không tồn tại | Report error, abort operation |
