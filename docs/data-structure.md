# Data Structure - C# Model Classes

## Tổng quan

Tài liệu này mô tả cấu trúc dữ liệu (C# model classes) của KBMS, được thiết kế song song với SQL syntax để đảm bảo consistency và tương thích với COKB text structure.

---

## Mapping: SQL Statement ↔ Data Structure ↔ COKB Text File

| SQL Statement | Data Structure Class | COKB Logical File |
|---------------|----------------------|-------------------|
| `CREATE KNOWLEDGE BASE` | `KnowledgeBase` | N/A (directory) |
| `CREATE CONCEPT <name>` | `Concept` + `ConceptDetail` | `CONCEPTS.TXT`, `<CONCEPT>.TXT` |
| `ADD HIERARCHY <p> IS_A <c>` | `Hierarchy` | `HIERARCHY.TXT` |
| `CREATE RELATION <name>` | `Relation` | `RELATIONS.TXT` |
| `CREATE OPERATOR <sym>` | `Operator` | `OPERATORS.TXT` |
| `CREATE FUNCTION <name>` | `Function` | `FUNCTIONS.TXT` |
| `ADD COMPUTATION TO <c>` | `ComputationRelation` | `<CONCEPT>.TXT` |
| `CREATE RULE <name>` | `Rule` | `RULES.TXT`, `<CONCEPT>.TXT` |
| `INSERT INTO <concept>` | `Fact` | `FACTS.TXT` |
| `CREATE USER <name>` | `User` | N/A (separate storage) |

---

## DataType Enum (Giống MySQL)

```csharp
namespace KBMS.Models;

/// <summary>
/// Kiểu dữ liệu cho biến, tham số Function/Operator, giá trị Fact
/// Được thiết kế tương tự MySQL để tối ưu storage và index
/// </summary>
public enum DataType
{
    // ========== Numeric Types ==========
    /// <summary>
    /// Số nguyên 1 byte: -128 ~ 127
    /// </summary>
    TINYINT,

    /// <summary>
    /// Số nguyên 2 bytes: -32,768 ~ 32,767
    /// </summary>
    SMALLINT,

    /// <summary>
    /// Số nguyên 4 bytes: -2,147,483,648 ~ 2,147,483,647
    /// </summary>
    INT,

    /// <summary>
    /// Số nguyên 8 bytes: -2^63 ~ 2^63-1
    /// </summary>
    BIGINT,

    /// <summary>
    /// Số thực 4 bytes (độ chính xác đơn)
    /// </summary>
    FLOAT,

    /// <summary>
    /// Số thực 8 bytes (độ chính xác kép)
    /// </summary>
    DOUBLE,

    /// <summary>
    /// Số thập phân chính xác cao (cho số tiền, tọa độ GPS)
    /// Format: DECIMAL(precision, scale) - ví dụ DECIMAL(10,2)
    /// </summary>
    DECIMAL,

    // ========== String Types ==========
    /// <summary>
    /// Chuỗi có độ dài thay đổi (tối đa 65,535 ký tự)
    /// </summary>
    VARCHAR,

    /// <summary>
    /// Chuỗi có độ dài cố định
    /// </summary>
    CHAR,

    /// <summary>
    /// Chuỗi dài không giới hạn
    /// </summary>
    TEXT,

    // ========== Boolean Type ==========
    /// <summary>
    /// Boolean (true/false)
    /// </summary>
    BOOLEAN,

    // ========== Date/Time Types (nếu cần) ==========
    /// <summary>
    /// Ngày (YYYY-MM-DD)
    /// </summary>
    DATE,

    /// <summary>
    /// Ngày giờ (YYYY-MM-DD HH:MM:SS)
    /// </summary>
    DATETIME,

    /// <summary>
    /// Timestamp (số mili-giây từ epoch)
    /// </summary>
    TIMESTAMP,

    // ========== Reference Type ==========
    /// <summary>
    /// Tham chiếu đến một Concept khác
    /// </summary>
    OBJECT
}
```

---

## Core Models

### 1. KnowledgeBase

**SQL Mapping:** `CREATE KNOWLEDGE BASE <name> [DESCRIPTION '<desc>']`

**COKB Mapping:** Không có trong COKB (được lưu dưới dạng directory)

```csharp
namespace KBMS.Models;

/// <summary>
/// Đại diện cho một Knowledge Base trong hệ thống KBMS
/// </summary>
public class KnowledgeBase
{
    /// <summary>
    /// Unique identifier cho Knowledge Base
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tên của Knowledge Base (unique trong hệ thống)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả về Knowledge Base
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// User ID của người sở hữu Knowledge Base
    /// </summary>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Thời điểm tạo Knowledge Base
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Số lượng facts/objects trong KB
    /// </summary>
    public int FactCount { get; set; }

    /// <summary>
    /// Số lượng rules trong KB
    /// </summary>
    public int RuleCount { get; set; }
}
```

---

### 2. Concept

**SQL Mapping:** `CREATE CONCEPT <name> VARIABLES (...) [...]`

**COKB Mapping:** Entry trong `CONCEPTS.TXT` (tên concept)

```csharp
namespace KBMS.Models;

/// <summary>
/// Đại diện cho một Concept (khái niệm) trong COKB model
/// Concept chỉ lưu tên, chi tiết nằm trong ConceptDetail
/// </summary>
public class Concept
{
    /// <summary>
    /// Tên của Concept (unique trong KB)
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
```

---

### 3. ConceptDetail

**SQL Mapping:** `CREATE CONCEPT <name> VARIABLES (...) ALIASES ... BASE_OBJECTS ... CONSTRAINTS ...`

**COKB Mapping:** File `<CONCEPT_NAME>.TXT`

```csharp
namespace KBMS.Models;

/// <summary>
/// Chi tiết đầy đủ của một Concept (tương ứng file <CONCEPT>.TXT)
/// </summary>
public class ConceptDetail
{
    /// <summary>
    /// Tên của Concept
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Knowledge Base ID mà concept thuộc về
    /// </summary>
    public Guid KbId { get; set; }

    /// <summary>
    /// Mô tả về Concept
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Các tên gọi khác (aliases)
    /// begin_othername → end_othername
    /// </summary>
    public List<string> Aliases { get; set; } = new();

    /// <summary>
    /// Các đối tượng nền (base objects)
    /// </summary>
    public List<string> BaseObjects { get; set; } = new();

    /// <summary>
    /// Danh sách các biến thuộc Concept
    /// begin_variables → end_variables
    /// </summary>
    public List<Variable> Variables { get; set; } = new();

    /// <summary>
    /// Danh sách các biến tương đương nhau
    /// begin_same_variables → end_same_variables
    /// </summary>
    public List<SameVariableGroup> SameVariables { get; set; } = new();

    /// <summary>
    /// Danh sách các ràng buộc trên biến
    /// begin_constraints → end_constraints
    /// </summary>
    public List<string> Constraints { get; set; } = new();

    /// <summary>
    /// Danh sách các quan hệ được thiết lập
    /// begin_construct_relations → end_construct_relations
    /// </summary>
    public List<ConstructRelation> ConstructRelations { get; set; } = new();

    /// <summary>
    /// Danh sách các tính chất
    /// begin_properties → end_properties
    /// </summary>
    public List<Property> Properties { get; set; } = new();

    /// <summary>
    /// Danh sách các quan hệ tính toán
    /// begin_computation_relations → end_computation_relations
    /// </summary>
    public List<ComputationRelation> ComputationRelations { get; set; } = new();

    /// <summary>
    /// Danh sách các rules trong scope của concept
    /// begin_rules → end_rules
    /// </summary>
    public List<ConceptRule> ConceptRules { get; set; } = new();
}

/// <summary>
/// Biến thuộc Concept
/// begin_variables: var:type
/// </summary>
public class Variable
{
    /// <summary>
    /// Tên biến
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Kiểu dữ liệu (giống MySQL)
    /// </summary>
    public DataType Type { get; set; } = DataType.STRING;
}

/// <summary>
/// Nhóm các biến tương đương nhau
/// begin_same_variables: var1=var2
/// </summary>
public class SameVariableGroup
{
    /// <summary>
    /// Tên biến gốc
    /// </summary>
    public string FromVariable { get; set; } = string.Empty;

    /// <summary>
    /// Tên biến tương đương
    /// </summary>
    public string ToVariable { get; set; } = string.Empty;
}

/// <summary>
/// Quan hệ được thiết lập
/// begin_construct_relations: [relation, from, to]
/// </summary>
public class ConstructRelation
{
    /// <summary>
    /// Tên quan hệ
    /// </summary>
    public string RelationName { get; set; } = string.Empty;

    /// <summary>
    /// Concept nguồn
    /// </summary>
    public string FromConcept { get; set; } = string.Empty;

    /// <summary>
    /// Concept đích
    /// </summary>
    public string ToConcept { get; set; } = string.Empty;
}

/// <summary>
/// Tính chất của concept
/// begin_properties: key=value
/// </summary>
public class Property
{
    /// <summary>
    /// Tên tính chất
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Giá trị tính chất
    /// </summary>
    public object Value { get; set; } = null!;
}
```

---

### 4. Hierarchy

**SQL Mapping:** `ADD HIERARCHY <parent> IS_A <child>` hoặc `PART_OF`

**COKB Mapping:** `HIERARCHY.TXT` - `[parent, child]`

```csharp
namespace KBMS.Models;

/// <summary>
/// Đại diện cho quan hệ phân cấp giữa các Concepts
/// </summary>
public class Hierarchy
{
    /// <summary>
    /// Unique identifier cho Hierarchy
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Knowledge Base ID mà hierarchy thuộc về
    /// </summary>
    public Guid KbId { get; set; }

    /// <summary>
    /// Concept cha (parent - cấp cao hơn)
    /// </summary>
    public string ParentConcept { get; set; } = string.Empty;

    /// <summary>
    /// Concept con (child - cấp thấp hơn, đặc biệt hóa)
    /// </summary>
    public string ChildConcept { get; set; } = string.Empty;

    /// <summary>
    /// Loại quan hệ: IS_A hoặc PART_OF
    /// </summary>
    public HierarchyType Type { get; set; }
}

/// <summary>
/// Loại quan hệ phân cấp
/// </summary>
public enum HierarchyType
{
    /// <summary>
    /// Quan hệ "là một" (kế thừa/đặc biệt hóa)
    /// </summary>
    IS_A,

    /// <summary>
    /// Quan hệ "là một phần của" (thành phần)
    /// </summary>
    PART_OF
}
```

---

### 5. Relation

**SQL Mapping:** `CREATE RELATION <name> FROM <domain> TO <range> PROPERTIES ...`

**COKB Mapping:** `RELATIONS.TXT` - `[relation, domain, range, ...] {"props"}`

```csharp
namespace KBMS.Models;

/// <summary>
/// Đại diện cho quan hệ giữa hai Concepts
/// </summary>
public class Relation
{
    /// <summary>
    /// Unique identifier cho Relation
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Knowledge Base ID mà relation thuộc về
    /// </summary>
    public Guid KbId { get; set; }

    /// <summary>
    /// Tên của Relation (unique trong KB)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Concept nguồn (domain)
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Concept đích (range)
    /// </summary>
    public string Range { get; set; } = string.Empty;

    /// <summary>
    /// Các thuộc tính của relation (transitive, symmetric, etc.)
    /// </summary>
    public List<string> Properties { get; set; } = new();
}
```

---

### 6. Operator

**SQL Mapping:** `CREATE OPERATOR <symbol> PARAMS (...) RETURNS ... PROPERTIES ...`

**COKB Mapping:** `OPERATORS.TXT` - `[symbol, return_type, param_types...] {"props"}`

```csharp
namespace KBMS.Models;

/// <summary>
/// Đại diện cho một toán tử toán học
/// </summary>
public class Operator
{
    /// <summary>
    /// Ký hiệu toán tử (+, -, *, /, ^, %, etc.)
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Knowledge Base ID mà operator thuộc về
    /// </summary>
    public Guid KbId { get; set; }

    /// <summary>
    /// Số lượng tham số
    /// </summary>
    public int ParamCount { get; set; }

    /// <summary>
    /// Danh sách kiểu dữ liệu của các tham số
    /// </summary>
    public List<string> ParamTypes { get; set; } = new();

    /// <summary>
    /// Kiểu dữ liệu trả về
    /// </summary>
    public string ReturnType { get; set; } = string.Empty;

    /// <summary>
    /// Các thuộc tính toán tử (commutative, associative, etc.)
    /// </summary>
    public List<string> Properties { get; set; } = new();
}
```

---

### 7. Function

**SQL Mapping:** `CREATE FUNCTION <name> PARAMS (...) RETURNS ... BODY ...`

**COKB Mapping:** `FUNCTIONS.TXT` - `return_type name(param_types...) {"props"}`

```csharp
namespace KBMS.Models;

/// <summary>
/// Đại diện cho một hàm toán học
/// </summary>
public class Function
{
    /// <summary>
    /// Tên hàm
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Knowledge Base ID mà function thuộc về
    /// </summary>
    public Guid KbId { get; set; }

    /// <summary>
    /// Số lượng tham số
    /// </summary>
    public int ParamCount { get; set; }

    /// <summary>
    /// Danh sách kiểu dữ liệu của các tham số
    /// </summary>
    public List<string> ParamTypes { get; set; } = new();

    /// <summary>
    /// Kiểu dữ liệu trả về
    /// </summary>
    public string ReturnType { get; set; } = string.Empty;

    /// <summary>
    /// Các thuộc tính của hàm
    /// </summary>
    public List<string> Properties { get; set; } = new();

    /// <summary>
    /// Thân hàm (công thức tính toán)
    /// begin_function: name(params) → result type → begin_proc → end_proc
    /// </summary>
    public string? Body { get; set; }
}
```

---

### 8. ComputationRelation

**SQL Mapping:** `ADD COMPUTATION TO <concept> VARIABLES ... FORMULA ...`

**COKB Mapping:** Bên trong `<CONCEPT>.TXT` - begin_computation_relations

```csharp
namespace KBMS.Models;

/// <summary>
/// Quan hệ tính toán trong Concept
/// </summary>
public class ComputationRelation
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Concept name mà computation thuộc về
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// Flag: 0 = forward (biến đã biết → tính kết quả), 1 = backward (kết quả → tìm biến)
    /// </summary>
    public int Flag { get; set; }

    /// <summary>
    /// Danh sách các biến đầu vào
    /// </summary>
    public List<string> InputVariables { get; set; } = new();

    /// <summary>
    /// Rank của biểu thức (rf)
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// Biến kết quả (nếu flag = 0)
    /// </summary>
    public string? ResultVariable { get; set; }

    /// <summary>
    /// Biểu thức tính toán
    /// </summary>
    public string Expression { get; set; } = string.Empty;

    /// <summary>
    /// Trọng số/chi phí
    /// </summary>
    public int Cost { get; set; }
}
```

---

### 9. Rule

**SQL Mapping:** `CREATE RULE <name> TYPE <t> SCOPE <c> IF ... THEN ...`

**COKB Mapping:** `RULES.TXT` (global) hoặc trong `<CONCEPT>.TXT` (concept-specific)

```csharp
namespace KBMS.Models;

/// <summary>
/// Đại diện cho một luật suy luận trong COKB model
/// </summary>
public class Rule
{
    /// <summary>
    /// Unique identifier cho Rule
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Knowledge Base ID mà rule thuộc về
    /// </summary>
    public Guid KbId { get; set; }

    /// <summary>
    /// Tên của Rule (unique trong KB)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Loại luật (deduction, default, constraint, computation)
    /// </summary>
    public RuleType Type { get; set; }

    /// <summary>
    /// Mô tả/Nội dung của luật
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Concept mà luật áp dụng (scope)
    /// Nếu null thì luật áp dụng cho tất cả concepts
    /// </summary>
    public string? ScopeConcept { get; set; }

    /// <summary>
    /// Các biến được dùng trong luật
    /// begin_rule → variables declarations
    /// </summary>
    public List<Variable> Variables { get; set; } = new();

    /// <summary>
    /// Danh sách các điều kiện (IF part)
    /// hypothesis_part: [conditions]
    /// </summary>
    public List<string> Hypothesis { get; set; } = new();

    /// <summary>
    /// Danh sách các kết luận (THEN part)
    /// goal_part: [conclusions]
    /// </summary>
    public List<string> Conclusion { get; set; } = new();

    /// <summary>
    /// Trọng số/chi phí của luật
    /// </summary>
    public int Cost { get; set; }
}

/// <summary>
/// Luật cụ thể trong scope của một Concept
/// begin_rules trong <CONCEPT>.TXT
/// </summary>
public class ConceptRule
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tên loại luật (Kind_Rules)
    /// </summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    /// Các biến được dùng
    /// </summary>
    public List<Variable> Variables { get; set; } = new();

    /// <summary>
    /// Điều kiện (IF)
    /// </summary>
    public List<string> Hypothesis { get; set; } = new();

    /// <summary>
    /// Kết luận (THEN)
    /// </summary>
    public List<string> Conclusion { get; set; } = new();
}

/// <summary>
/// Loại luật suy luận
/// </summary>
public enum RuleType
{
    /// <summary>
    /// Luật suy luận thông thường
    /// </summary>
    Deduction,

    /// <summary>
    /// Luật mặc định
    /// </summary>
    Default,

    /// <summary>
    /// Luật ràng buộc
    /// </summary>
    Constraint,

    /// <summary>
    /// Luật tính toán
    /// </summary>
    Computation
}
```

---

### 10. Fact

**SQL Mapping:** `INSERT INTO <concept> VALUES (...)`

**COKB Mapping:** `FACTS.TXT` - `[concept, field=value, ...]`

```csharp
namespace KBMS.Models;

/// <summary>
/// Đại diện cho một instance của một Concept (fact/sự kiện)
/// </summary>
public class Fact
{
    /// <summary>
    /// Unique identifier cho Fact
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Knowledge Base ID mà fact thuộc về
    /// </summary>
    public Guid KbId { get; set; }

    /// <summary>
    /// Tên Concept mà fact là instance của
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// Dictionary chứa các giá trị của biến
    /// Format trong FACTS.TXT: field=value
    /// </summary>
    public Dictionary<string, object> Values { get; set; } = new();

    /// <summary>
    /// Thời điểm tạo fact
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Thời điểm cập nhật fact
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
```

---

### 13. AggregateResult (cho Aggregation Queries)

**SQL Mapping:** `SELECT COUNT(*), SUM(var), AVG(var), MAX(var), MIN(var) FROM ...`

**COKB Mapping:** N/A (internal calculation)

```csharp
namespace KBMS.Models;

/// <summary>
/// Kết quả truy vấn aggregation
/// </summary>
public class AggregateResult
{
    /// <summary>
    /// Tên của hàm aggregation
    /// </summary>
    public string AggregateFunction { get; set; } = string.Empty;

    /// <summary>
    /// Tên biến được aggregate (null cho COUNT(*))
    /// </summary>
    public string? Variable { get; set; }

    /// <summary>
    /// Giá trị kết quả (number type)
    /// </summary>
    public object Value { get; set; } = null!;

    /// <summary>
    /// Điều kiện WHERE đã áp dụng
    /// </summary>
    public string? WhereCondition { get; set; }
}

/// <summary>
/// Kết quả truy vấn GROUP BY
/// </summary>
public class GroupByResult
{
    /// <summary>
    /// Giá trị nhóm
    /// </summary>
    public object GroupValue { get; set; } = null!;

    /// <summary>
    /// Các kết quả aggregation trong nhóm này
    /// </summary>
    public List<AggregateResult> Aggregates { get; set; } = new();

    /// <summary>
    /// Danh sách các fact trong nhóm này (tùy chọn)
    /// </summary>
    public List<Fact> Facts { get; set; } = new();
}
```

---

### 14. JoinClause (cho JOIN Queries)

**SQL Mapping:** `SELECT ... JOIN ... ON ... WHERE ...`

**COKB Mapping:** N/A (internal calculation)

```csharp
namespace KBMS.Models;

/// <summary>
/// Mô tả một JOIN clause trong SELECT
/// </summary>
public class JoinClause
{
    /// <summary>
    /// Loại join: INNER, LEFT, RIGHT, FULL (default: INNER)
    /// </summary>
    public string JoinType { get; set; } = "INNER";

    /// <summary>
    /// Concept hoặc relation name được join
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Alias cho table được join
    /// </summary>
    public string? Alias { get; set; }

    /// <summary>
    /// Điều kiện join (ON clause)
    /// </summary>
    public Condition? OnCondition { get; set; }
}

/// <summary>
/// Kết quả truy vấn JOIN
/// </summary>
public class JoinResult
{
    /// <summary>
    /// Danh sách các fact đã được join
    /// </summary>
    public List<Dictionary<string, object>> Rows { get; set; } = new();

    /// <summary>
    /// Các JOIN đã áp dụng
    /// </summary>
    public List<JoinClause> Joins { get; set; } = new();
}
```

---

### 11. User

**SQL Mapping:** `CREATE USER <name> PASSWORD '<pass>' [ROLE <r>] [SYSTEM_ADMIN {true|false}]`

**COKB Mapping:** Không có (user management là bổ sung của KBMS)

```csharp
namespace KBMS.Models;

/// <summary>
/// Vai trò của user trong hệ thống
/// </summary>
public enum UserRole
{
    /// <summary>
    /// ROOT: Toàn quyền hệ thống, không cần kiểm tra permission
    /// </summary>
    ROOT,

    /// <summary>
    /// USER: Cần được cấp quyền cụ thể
    /// </summary>
    USER
}

/// <summary>
/// Mức quyền hạn trên một Knowledge Base
/// </summary>
public enum Privilege
{
    /// <summary>
    /// Đọc tri thức (SELECT, SOLVE, SHOW)
    /// </summary>
    READ,

    /// <summary>
    /// Đọc và ghi (READ + INSERT, UPDATE, DELETE)
    /// </summary>
    WRITE,

    /// <summary>
    /// Quản trị (WRITE + CREATE, DROP, GRANT trên KB)
    /// </summary>
    ADMIN
}

/// <summary>
/// Đại diện cho một người dùng trong hệ thống KBMS
/// </summary>
public class User
{
    /// <summary>
    /// Unique identifier cho User
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tên đăng nhập (unique trong hệ thống)
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password hash (BCrypt)
    /// </summary>
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Vai trò của user
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// Dictionary mapping KB Name → Privilege
    /// </summary>
    public Dictionary<string, Privilege> KbPrivileges { get; set; } = new();

    /// <summary>
    /// Quyền quản trị hệ thống (cho phép CREATE KB, GRANT, REVOKE)
    /// </summary>
    public bool SystemAdmin { get; set; }

    /// <summary>
    /// Thời điểm tạo user
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Thời điểm lần cuối hoạt động
    /// </summary>
    public DateTime LastActivityAt { get; set; }
}
```

---

## Support Models

### 13. Session

```csharp
namespace KBMS.Server;

/// <summary>
/// Session data cho mỗi kết nối client
/// </summary>
public class Session
{
    /// <summary>
    /// Unique session ID (hex string)
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Client ID (connection identifier)
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// User đã login (null nếu chưa login)
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Knowledge Base hiện tại được chọn (null nếu chưa chọn)
    /// </summary>
    public string? CurrentKb { get; set; }

    /// <summary>
    /// Thời điểm connect
    /// </summary>
    public DateTime ConnectedAt { get; set; }

    /// <summary>
    /// Thời điểm hoạt động cuối cùng
    /// </summary>
    public DateTime LastActivityAt { get; set; }
}
```

---

## Relationships Diagram

```
┌─────────────────┐
│  KnowledgeBase  │
│  - Id           │
│  - Name         │
│  - OwnerId      │──┐
│  - CreatedAt    │  │
│  - FactCount   │  │
│  - RuleCount    │  │
└─────────────────┘  │
                     │ 1:N
┌─────────────────┐  │
│     User        │  │
│  - Id           │──┘
│  - Username     │
│  - PasswordHash │
│  - Role         │
│  - KbPrivileges │◄───── N:M
│  - SystemAdmin  │       (User ←→ KB)
└─────────────────┘
         │
         │ owns
         ▼
┌─────────────────┐       ┌─────────────────┐
│     Concept     │◄──────│    Hierarchy    │
│  - Name         │       │  - Parent       │
│                  │       │  - Child        │
│                  │       │  - Type         │
│                  │       └─────────────────┘
│                  │
│                  ├──────┐ 1:1
│                  │       │
┌─────────────────┐  │  ┌─────────────────┐
│  ConceptDetail  │  │  │   Relation      │
│  - Name         │  │  │  - Id           │
│  - Variables    │  │  │  - Domain       │
│  - Constraints  │  │  │  - Range        │
│  - CompRels     │  │  │  - Properties   │
│  - ConceptRules │  │  └─────────────────┘
└─────────────────┘  │
                      │ 1:N
┌─────────────────┐  │
│     Fact        │  │
│  - Id           │  │
│  - ConceptName  │──┘
│  - Values       │
│  - CreatedAt    │
└─────────────────┘

┌─────────────────┐
│    Operator     │
│  - Symbol       │
│  - ParamTypes   │
│  - ReturnType   │
│  - Properties   │
└─────────────────┘

┌─────────────────┐
│    Function     │
│  - Name         │
│  - ParamTypes   │
│  - ReturnType   │
│  - Body         │
└─────────────────┘

┌─────────────────┐       ┌─────────────────┐
│     Rule        │◄──────│   Expression     │
│  - Id           │       │  - Type         │
│  - Type         │       │  - Content      │
│  - Hypothesis   │       │  - Children     │
│  - Conclusion   │       └─────────────────┘
└─────────────────┘
```

---

## Type Mappings

### DataType Enum Values (C# ↔ SQL ↔ Binary Storage)

| Enum Value | SQL Type | Storage Size | Mô tả |
|-----------|----------|--------------|---------|
| `DataType.TINYINT` | `TINYINT` | 1 byte | -128 ~ 127 |
| `DataType.SMALLINT` | `SMALLINT` | 2 bytes | -32,768 ~ 32,767 |
| `DataType.INT` | `INT` | 4 bytes | ±2.1 tỷ |
| `DataType.BIGINT` | `BIGINT` | 8 bytes | ±9.2e18 |
| `DataType.FLOAT` | `FLOAT` | 4 bytes | ~7 chữ số thập phân |
| `DataType.DOUBLE` | `DOUBLE` | 8 bytes | ~15 chữ số thập phân |
| `DataType.DECIMAL` | `DECIMAL(p,s)` | biến | Số thập phân chính xác (cho tiền) |
| `DataType.VARCHAR` | `VARCHAR(n)` | biến | Chuỗi dài thay đổi |
| `DataType.CHAR` | `CHAR(n)` | n bytes | Chuỗi dài cố định |
| `DataType.TEXT` | `TEXT` | biến | Chuỗi dài không giới hạn |
| `DataType.BOOLEAN` | `BOOLEAN` | 1 byte | true/false |
| `DataType.DATE` | `DATE` | 3 bytes | Ngày (YYYY-MM-DD) |
| `DataType.DATETIME` | `DATETIME` | 8 bytes | Ngày giờ |
| `DataType.TIMESTAMP` | `TIMESTAMP` | 4 bytes | Timestamp |
| `DataType.OBJECT` | `object` | Guid | Tham chiếu Concept |

### C# Type ↔ SQL Type ↔ Binary Storage Mapping

| C# Type | SQL Type | Binary Storage |
|---------|----------|----------------|
| `sbyte` | `TINYINT` | 1 byte (signed) |
| `byte` | `TINYINT UNSIGNED` | 1 byte (unsigned) |
| `short` | `SMALLINT` | 2 bytes (signed) |
| `ushort` | `SMALLINT UNSIGNED` | 2 bytes (unsigned) |
| `int` | `INT` | 4 bytes (signed) |
| `uint` | `INT UNSIGNED` | 4 bytes (unsigned) |
| `long` | `BIGINT` | 8 bytes (signed) |
| `ulong` | `BIGINT UNSIGNED` | 8 bytes (unsigned) |
| `float` | `FLOAT` | 4 bytes (IEEE 754) |
| `double` | `DOUBLE` | 8 bytes (IEEE 754) |
| `decimal` | `DECIMAL` | Binary decimal |
| `string` | `VARCHAR`/`TEXT` | UTF-8 bytes + length |
| `bool` | `BOOLEAN` | 1 byte |
| `DateTime` | `DATETIME` | 8 bytes (Unix timestamp ms) |
| `Guid` | `OBJECT` | 16 bytes |

### Role Values

| Enum Value | SQL Value | Description |
|------------|-----------|-------------|
| `UserRole.ROOT` | `ROOT` | System administrator |
| `UserRole.USER` | `USER` | Regular user |

### Privilege Values

| Enum Value | SQL Value | Description |
|------------|-----------|-------------|
| `Privilege.READ` | `READ` | Read-only access |
| `Privilege.WRITE` | `WRITE` | Read and write access |
| `Privilege.ADMIN` | `ADMIN` | Administrative access |

### Hierarchy Types

| Enum Value | SQL Value | COKB Text |
|------------|-----------|-----------|
| `HierarchyType.IS_A` | `IS_A` | Đặc biệt hóa |
| `HierarchyType.PART_OF` | `PART_OF` | Thành phần |

### Rule Types

| Enum Value | SQL Value | Description |
|------------|-----------|-------------|
| `RuleType.Deduction` | `deduction` | Luật suy luận thông thường |
| `RuleType.Default` | `default` | Luật mặc định |
| `RuleType.Constraint` | `constraint` | Luật ràng buộc |
| `RuleType.Computation` | `computation` | Luật tính toán |

---

## COKB Text ↔ C# Model Mapping Examples

### Example 1: CONCEPTS.TXT → C# Concept

```
COKB Text:
begin_concepts
TAMGIAC
PERSON
end_concepts

↓ Parsing ↓

C#:
List<Concept> concepts = new()
{
    new Concept { Name = "TAMGIAC" },
    new Concept { Name = "PERSON" }
};
```

---

### Example 2: HIERARCHY.TXT → C# Hierarchy

```
COKB Text:
begin_Hierarchy
[HINHHOC, TAMGIAC]
[CANH, TAMGIAC]
end_Hierarchy

↓ Parsing ↓

C#:
List<Hierarchy> hierarchies = new()
{
    new Hierarchy
    {
        ParentConcept = "HINHHOC",
        ChildConcept = "TAMGIAC",
        Type = HierarchyType.IS_A
    },
    new Hierarchy
    {
        ParentConcept = "CANH",
        ChildConcept = "TAMGIAC",
        Type = HierarchyType.PART_OF
    }
};
```

---

### Example 3: FACTS.TXT → C# Fact

```
COKB Text:
begin_facts
[TAMGIAC, a=3, b=4, c=5, S=6]
[PERSON, name="Alice", age=30]
end_facts

↓ Parsing ↓

C#:
List<Fact> facts = new()
{
    new Fact
    {
        ConceptName = "TAMGIAC",
        Values = new Dictionary<string, object>
        {
            { "a", 3.0 },
            { "b", 4.0 },
            { "c", 5.0 },
            { "S", 6.0 }
        }
    },
    new Fact
    {
        ConceptName = "PERSON",
        Values = new Dictionary<string, object>
        {
            { "name", "Alice" },
            { "age", 30 }
        }
    }
};
```
