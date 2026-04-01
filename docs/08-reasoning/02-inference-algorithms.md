# 08.2. Thuật toán Suy diễn và Bao đóng (Inference Algorithms)

Hạt nhân thực thi của hệ thống suy diễn trong [KBMS](../00-glossary/01-glossary.md#kbms) V3 là thuật toán **[F-Closure](../00-glossary/01-glossary.md#f-closure)**. Đây là một tập hợp các quy tắc lan truyền tri thức đệ quy nhằm tối ưu hóa việc tìm kiếm các chân lý tri thức từ các sự thật ban đầu (**[Ground Truth](../00-glossary/01-glossary.md#ground-truth) - GT**).

---

## 1. Thuật toán F-Closure (Knowledge Closure)

Thuật toán `F-Closure` duy trì một tập hợp các sự thật hội tụ, cập nhật liên tục cho đến khi đạt được trạng thái điểm dừng (**[Fixed-point](../00-glossary/01-glossary.md#fixed-point)**).

### Các bước thực thi chính:
1.  **Initialization**: Khởi tạo `BaoDong = GT`.
2.  **Cyclic Evaluation**: Vòng lặp quét hằng số hệ thống cho đến khi không có sự thực mới nào phát sinh:
    -   **SameVariable Propagation**: Đồng bộ giá trị giữa các biến có thuộc tính tương đương. 
    -   **[Rule](../00-glossary/01-glossary.md#rule) Evaluation**: Khớp các luật `IF-THEN` từ metadata. Nếu tất cả các giả thuyết trong phần `IF` thuộc `BaoDong`, hệ thống sẽ kích hoạt (Fire) và đưa kết luận vào `BaoDong`.
    -   **NCalc Computation**: Tính toán các biểu thức số học ngay khi các toán tử đầu vào đã có giá trị.
    -   **[Equation](../00-glossary/01-glossary.md#equation) Solving**: Tích hợp các bộ giải phương trình số học khi hệ thống chỉ còn thiếu một biến chưa biết trong ràng buộc.
3.  **Recursive Sub-closure**: Đối với các thực thể là một phần của đối tượng khác (`PART-OF`), thuật toán thực hiện bao đóng đệ quy cho các thành phần con trước khi tổng hợp kết quả lên cha.

---

## 2. Đặc tả Cài đặt (InferenceEngine.cs)

Đoạn mã giả dưới đây mô tả cách thức bao đóng được triển khai trong mã máy:

```csharp
public ReasoningResult FindClosure(Concept concept, List<Fact> initialFacts) {
    var closure = new KnowledgeSet(initialFacts);
    bool changed = true;

    while (changed && iterationCount < SAFETY_LIMIT) {
        changed = false;
        
        // 1. Lan truyền phân cấp
        changed |= PropagateHierarchy(concept, closure);
        
        // 2. Đánh giá tính toán và quy tắc
        changed |= EvaluateRules(concept.Rules, closure);
        changed |= SolveEquations(concept.Equations, closure);
        
        // 3. Lan truyền quan hệ liên đới
        changed |= PropagateSameVariables(concept, closure);
        
        iterationCount++;
    }
    return new ReasoningResult(closure);
}
```

---

## 3. Các Cơ chế Thúc đẩy Hiệu năng (Optimization Mechanisms)

Để tối ưu hóa quá trình tính toán bao đóng, [KBMS](../00-glossary/01-glossary.md#kbms) áp dụng các kỹ thuật sau:

- **Giới hạn An toàn (Safety Iteration Limit)**: Mặc định $N=50$ vòng lặp để ngăn chặn các vòng lặp đệ quy vô hạn trong trường hợp tri thức bị mâu thuẫn.
- **Tiền xử lý Làm phẳng ([Concept](../00-glossary/01-glossary.md#concept) [Flattening](../00-glossary/01-glossary.md#flattening))**: Trước khi bắt đầu suy diễn, toàn bộ cấu trúc kế thừa và thành phần con được "làm phẳng" thành 1 danh sách biến phẳng, giúp tăng tốc độ khớp luật.
- **[Early Exit](../00-glossary/01-glossary.md#early-exit) Criterion**: Hệ thống tự động kiểm tra danh sách kết luận mục tiêu (**Targets**). Nếu tất cả các mục tiêu đã có giá trị, thuật toán sẽ ngắt vòng lặp ngay lập tức mà không cần đợi nốt các chu kỳ thừa.
