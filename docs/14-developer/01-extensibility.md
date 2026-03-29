# Hướng dẫn Mở rộng Hệ thống (Developer Guide)

KBMS được thiết kế với kiến trúc mở, cho phép lập trình viên dễ dàng mở rộng khả năng tính toán và suy diễn bằng cách can thiệp vào mã nguồn C#.

## 1. Mở rộng Hàm (Custom Functions)

Bạn có thể thêm các hàm toán học hoặc xử lý chuỗi mới để sử dụng trong lệnh `CALC()` hoặc trong các `Rule/Equation`.

### Bước 1: Khai báo hàm trong C#
Trong project `KBMS.Reasoning`, bạn có thể đăng ký hàm mới cho `InferenceEngine` thông qua `FunctionResolver`.

```csharp
// Ví dụ: Thêm hàm Sigmoid cho bài toán mạng nơ-ron
inferenceEngine.FunctionResolver = (name) => {
    if (name.Equals("Sigmoid", StringComparison.OrdinalIgnoreCase)) {
        return new Function {
            Name = "Sigmoid",
            Parameters = new List<Parameter> { new Parameter { Name = "x" } },
            Body = "1 / (1 + Exp(-x))"
        };
    }
    return null;
};
```

### Bước 2: Sử dụng trong KBQL
```kbql
SELECT CALC(Sigmoid(weight * input)) FROM Neuron;
```

---

## 2. Tùy biến Toán tử (Custom Operators)

KBMS cho phép định nghĩa các toán tử mới (Syntactic Sugar) để rút gọn biểu thức.

### Bước 1: Đăng ký Toán tử
Bạn có thể sử dụng `OperatorResolver` để định nghĩa cách xử lý cho các ký hiệu toán tử mới.

```csharp
// Định nghĩa toán tử tích vô hướng
inferenceEngine.OperatorResolver = (symbol) => {
    if (symbol == ".*.") {
        return new Operator {
            Symbol = ".*.",
            Body = "a1*b1 + a2*b2 + a3*b3" -- Giả sử cho vector 3D
        };
    }
    return null;
};
```

---

## 3. Mở rộng Model tri thức (Knowledge Models)

Nếu bạn muốn thêm các thuộc tính mới cho `Concept`, hãy chỉnh sửa tập tin `Concept.cs` trong project `KBMS.Models`.

*   **Variables:** Danh sách các biến và kiểu dữ liệu.
*   **Constraints:** Các ràng buộc logic.
*   **Equations:** Các phương trình liên quan.

---

## 4. Tích hợp giao diện (Studio Integration)

Để mở rộng KBMS Studio, bạn cần làm việc với project **Electron + React**:
1.  **Monaco Editor:** Thêm các từ khóa mới vào file `kbql-language.ts` để hỗ trợ highlight.
2.  **Telemetry:** Mở rộng luồng `Diagnostic` để hiển thị thêm các thông số hiệu năng mới lên Dashboard.

## 5. Lưu ý cho Nhà phát triển

*   **Tính ACID:** Khi thay đổi `StorageEngine`, luôn đảm bảo việc ghi log (WAL) được thực hiện trước khi flush page lên đĩa.
*   **Hiệu năng:** Các hàm tùy biến trong `ReasoningEngine` nên được tối ưu hóa để tránh làm chậm vòng lặp suy diễn (Forward Chaining).
