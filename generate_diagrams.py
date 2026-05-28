import os
import base64
import json
import urllib.request

def generate_mermaid_image(mermaid_code, output_path):
    # Encode the mermaid code to base64
    json_data = json.dumps({"code": mermaid_code, "mermaid": {"theme": "default"}})
    encoded_code = base64.urlsafe_b64encode(json_data.encode('utf-8')).decode('utf-8')
    
    # URL for mermaid.ink
    url = f"https://mermaid.ink/img/{encoded_code}"
    
    # Download the image
    req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
    try:
        with urllib.request.urlopen(req) as response, open(output_path, 'wb') as out_file:
            data = response.read()
            out_file.write(data)
        print(f"Generated {output_path}")
    except Exception as e:
        print(f"Failed to generate {output_path}: {e}")

# Ensure diagram directory exists
os.makedirs("docs/assets/diagrams", exist_ok=True)

# 1. Use Case Diagram
uc_code = """
flowchart LR
    %% Actors
    Admin([Quản trị viên])
    KE([Kỹ sư tri thức])
    App([Ứng dụng Client])

    %% Use Cases
    subgraph KBMS[Hệ Quản trị Tri thức KBMS]
        UC1(Quản lý người dùng & RBAC)
        UC2(Định nghĩa Concept & Rule)
        UC3(Giám sát hiệu năng & WAL)
        UC4(Nạp dữ kiện - Insert Fact)
        UC5(Yêu cầu suy diễn - Infer)
        UC6(Truy vấn kết quả - DQL)
    end

    %% Relationships
    Admin --> UC1
    Admin --> UC3
    KE --> UC2
    KE --> UC4
    App --> UC4
    App --> UC5
    App --> UC6
"""
generate_mermaid_image(uc_code, "docs/assets/diagrams/kbms_usecase.png")

# 2. Activity Diagram
activity_code = """
stateDiagram-v2
    [*] --> Client: Gửi lệnh KBQL (TCP Binary)
    Client --> Parser: Phân tích cú pháp (Lexer/Parser)
    
    state Parser {
        direction LR
        Tokenize --> ValidateSyntax
    }
    
    Parser --> AST: Khởi tạo Cây cú pháp (AST)
    
    state AST_Processing {
        direction TB
        AST --> DDL: Lệnh định nghĩa
        AST --> DML: Lệnh thao tác
        AST --> DQL: Lệnh truy vấn/suy diễn
    }
    
    DDL --> Storage: Cập nhật Schema (B+ Tree)
    DML --> Storage: Lưu dữ kiện mới (WAL)
    DQL --> Inference: Kích hoạt Forward Chaining
    
    Storage --> [*]: Trả kết quả
    Inference --> [*]: Trả tập kết quả
"""
generate_mermaid_image(activity_code, "docs/assets/diagrams/kbms_kbql_flow.png")

# 3. Class Diagram
class_code = """
classDiagram
    class Concept {
        +String Name
        +List~Attribute~ Attributes
        +List~Rule~ Rules
        +List~Function~ Funcs
        +List~Relation~ Relations
    }
    
    class Attribute {
        +String Name
        +Type DataType
        +Any Value
    }
    
    class Rule {
        +String RuleID
        +Condition LHS
        +Action RHS
        +evaluate() Boolean
    }
    
    class Function {
        +String FuncName
        +Expression Expr
        +calculate() Number
    }
    
    class Relation {
        +String RelType
        +Concept Target
    }
    
    Concept "1" *-- "many" Attribute : has
    Concept "1" *-- "many" Rule : contains
    Concept "1" *-- "many" Function : computes
    Concept "1" *-- "many" Relation : linked to
"""
generate_mermaid_image(class_code, "docs/assets/diagrams/kbms_class_model.png")

# 4. Data Flow Forward Chaining
df_code = """
flowchart TD
    A[Dữ kiện mới] --> B(Working Memory)
    B --> C{Rete Network}
    
    C -->|Pattern Matching| D[Alpha Network]
    D --> E[Beta Network]
    
    E --> F{Có Rule thỏa mãn?}
    F -- Có --> G[Agenda]
    G --> H[Thực thi tính toán - Fire Rule]
    H --> I[Sinh dữ kiện mới]
    I -->|Đẩy ngược vào| B
    
    F -- Không --> J[Đạt F-Closure]
    J --> K([Trả kết quả])
"""
generate_mermaid_image(df_code, "docs/assets/diagrams/kbms_forward_chaining.png")
