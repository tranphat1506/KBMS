Hệ thống KBMS V3 được xác thực thông qua 4 kịch bản thực tế với độ phức tạp tăng dần, từ suy diễn logic đơn lẻ đến bao đóng tri thức trên mạng lưới thực thể khổng lồ. Các kịch bản này được thiết kế để kiểm tra toàn diện khả năng tích hợp giữa ngôn ngữ truy vấn KBQL và công cụ suy diễn Inference Engine.

## 1. Kịch bản A: Hệ tri thức Giáo dục (Education)
- **Mục tiêu**: Xác thực tính đúng đắn của Forward Chaining khi xử lý các luật logic kết hợp nhiều thuộc tính (GPA, Behavior, Credits).
- **Thực thi**: 
```sql
-- Chèn dữ liệu cơ sở
INSERT INTO Student ATTRIBUTE(name: 'Le Phat', gpa: 3.9, behavior: 95, credits: 18);
-- Suy diễn trạng thái học thuật
SELECT SOLVE(status) FROM Student WHERE name = 'Le Phat';
```
- **Kết quả**: Hệ thống suy diễn thành công trạng thái `DeanList` dựa trên quy tắc `R_DeanList`.

## 2. Kịch bản B: Chẩn đoán Y tế (Medical Diagnostic)
- **Mục tiêu**: Đánh giá hiệu năng suy diễn trên miền giá trị số thực và các ngưỡng chẩn đoán lâm sàng.
- **Thực thi**:
```sql
INSERT INTO Patient ATTRIBUTE(p_id: 'P1', p_age: 65, p_bmi: 32.5);
SELECT SOLVE(p_risk) FROM Patient WHERE p_id = 'P1';
```
- **Kết quả**: Xác định mức độ rủi ro tim mạch (`p_risk = 'High'`) tức thời với độ trễ cực thấp.

## 3. Kịch bản C: Đồ thị Đô thị Thông minh (SmartCity Transit)
- **Mục tiêu**: Kiểm tra khả năng xử lý xung đột và bao đóng tri thức lan truyền trên mạng lưới cảm biến.
- **Thực thi**:
```sql
INSERT INTO Sensor ATTRIBUTE(id: 'S1', speed: 5, zid: 'Z1');
SELECT SOLVE(id) FROM Sensor WHERE zid = 'Z1';
```
- **Kết quả**: Hệ thống nhận diện trạng thái tắc nghẽn (`JAMMED`) thông qua việc lan truyền giá trị trên thuộc tính định danh.

## 4. Kịch bản D: Phân loại Hình học (Geometry)
- **Mục tiêu**: Xác thực khả năng tính toán biểu thức phức tạp (định lý Pythagoras) trong điều kiện của luật.
- **Thực thi**:
```sql
INSERT INTO Triangle ATTRIBUTE(ta: 3.0, tb: 4.0, tc: 5.0);
SELECT SOLVE(t_type) FROM Triangle WHERE ta = 3.0;
```
- **Kết quả**: Phân loại chính xác `RightTriangle` nhờ cơ chế tính toán biểu thức toán học trực tiếp trong Rete Network.

## Nhật ký Thực thi Kiểm thử (xUnit Test Logs)

Dưới đây là nhật ký thực tế từ bộ kiểm thử `ComplexScenarioTests`, xác nhận sự ổn định của hệ thống:

```zsh
[Passed] KBMS.Tests.ComplexScenarioTests.Test_Scenario_A_Education_Inference
[Passed] KBMS.Tests.ComplexScenarioTests.Test_Scenario_B_Medical_Diagnostic
[Passed] KBMS.Tests.ComplexScenarioTests.Test_Scenario_C_SmartCity_Transit
[Passed] KBMS.Tests.ComplexScenarioTests.Test_Scenario_D_Geometry_Pythagoras
[Passed] KBMS.Tests.Phase5ForwardChainingTests.Charlie_Recursive_Inference

Passed! - Failed: 0, Passed: 6, Skipped: 0, Total: 6, Duration: 536 ms
```
