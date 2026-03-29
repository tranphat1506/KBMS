# 05.4. Thành phần Luật & Logic (Rules & Logic)

Luật (**Rule**) cho phép hệ thống tự động tính toán các thuộc tính mới từ những giá trị đã có.

### Kịch bản suy diễn đệ quy (Charlie Dataset):
```sql
-- Rule 1: Khởi tạo Danh hiệu
CREATE RULE HighHonor IF Student(grade >= 90) THEN Student(honor = 'High');

-- Rule 2: Chuyển đổi Danh hiệu sang Đặc quyền (Suy diễn đệ quy)
CREATE RULE HonorToGift IF Student(honor = 'High') THEN Student(gifted = true);
```

### Minh chứng Mã nguồn (Source Code):
![source_code_concept_rules.png](../assets/diagrams/source_code_concept_rules.png)
*Hình 5.2: Minh chứng mã nguồn xử lý tập luật đệ quy trong lớp Concept.*

### Minh chứng Kết quả (Terminal):
![terminal_test_rule_execution.png](../assets/diagrams/terminal_test_rule_execution.png)
*Hình 5.3: Kết quả thực thi suy diễn luật trên bộ dữ liệu kiểm thử Models.*

## 2. Kiểm thử Suy diễn (Forward Chaining Testing)

Tình trạng chính xác của thuật toán Suy diễn tiến được xác thực qua tệp `Phase5ForwardChainingTests.cs`.

*   **Scenario (Charlie Case)**: Chèn bản ghi `Charlie` với `grade = 95`.
*   **Engine Action**: 
    1. Rule 1 được kích hoạt -> `honor` trở thành 'High'.
    2. Rule 2 được kích hoạt (do honor thay đổi) -> `gifted` trở thành `true`.
*   **Hiệu năng thực tế**: Thời gian kích hoạt luật trung bình là **4ms**.
*   **Validation**: `Assert.Contains("High", selectRes.Content);` và `Assert.Contains("true", selectRes.Content);`

![Placeholder: Ảnh chụp Terminal thực tế chạy Phase5ForwardChainingTests với log suy diễn đệ quy của Charlie, hiển thị các bước nhảy logic và thời gian 4ms](../assets/diagrams/placeholder_fclosure_charlie_test_success.png)
