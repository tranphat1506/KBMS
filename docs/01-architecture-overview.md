# Kiến trúc Hệ Quản Trị Tri Thức (KBMS V2)

Knowledge Base Management System (KBMS) được thiết kế xoay quanh mô hình client-server, cho phép người dùng nhập các lệnh truy vấn KBQL (Knowledge Base Query Language) và hệ thống dưới nền sẽ thực thi, lưu trữ, và suy diễn dữ liệu một cách an toàn, mạnh mẽ.

Với phiên bản V2, kiến trúc của KBMS được chia ra làm **3 Tầng Rõ Rệt (3-Tier Architecture)** nhằm đảm bảo tính toàn vẹn dữ liệu (ACID) hiệu năng tốc độ bứt phá.

## 1. Mạng lưới Môi trường và Front-end (Client / CLI)
- **KBMS.CLI**: Giao diện dòng lệnh tương tác trực tiếp với người dùng. Nhận text input từ bàn phím (Multiline support qua dấu chấm phẩy `;`). Lệnh sau đó chuyển thành gói tin TCP truyền thẳng tới Server.
- **KBMS.Network**: Xử lý giao tiếp giao thức TCP/IP giữa Application và Core DBMS Engine.

## 2. Tầng Cảm Biến Cú Pháp (Parser & AST Layer)
Tầng này nằm trên Server thụ lý các string text từ Network. Gồm quá trình biên dịch (Compile) chuỗi:
1. **Lexer**: Chặt một câu lệnh như `ALTER CONCEPT <TAM> ( ADD ( RULES(R1) ) )` thành một mảng hàng chục mảnh Token riêng biệt. Nó hiểu được đâu là Keywords, Symbol `(`, `)`, hay Identifier.
2. **Parser**: Chịu trách nhiệm chắp nối các Token rời rạc thành **Khối Cây Cú Pháp Trừu Tượng (Abstract Syntax Tree - AST)**.
   Quá trình Parser sẽ phân mảnh query thành 1 trong 5 nhánh ngôn ngữ gốc (KDL, KML, KQL, KCL, TCL) để điều tiết xuống tầng dưới rành mạch tránh chồng chéo luồng xử lý.

## 3. Storage Layer (Tầng Lưu Trữ Logical & Bộ Nhớ RAM)
Đây là Lõi Cảm Biến Khối Lượng và Năng Lực Giải Toán (Reasoning), hoạt động hoàn toàn trên RAM (Random Access Memory).
- Nhận Tree AST từ tầng Parser.
- **Buffer Pool (Cache Manager)**: Không bao giờ đọc dữ liệu trực tiếp từ đĩa Cốc Cốc cho mỗi câu query. Thay vào đó, tải các `Concept` và file danh sách các `ObjectInstance` lên trong một cái "Bể chứa" ở bộ nhớ trong (Singleton List). Tốc độ `SELECT` và toán học (Inference Engine `SOLVE`) sẽ vận hành ở tốc độ tối đa của máy tính CPU thay vì bị tắc nghẽn ở thẻ nhớ SSD/HDD.
- **Shadow Paging (Transaction RAM)**: Khi dính từ khóa TCL (`BEGIN TRANSACTION`), Tầng này lập tức che mờ cấu trúc RAM gốc, tạo một bảng Copy cho Frontend xào nấu `INSERT/UPDATE` thoải mái mà không lo hỏng dữ liệu hệ thống, cũng chưa thèm ghi xuống đĩa cứng.

## 4. Physical Storage Layer (Tầng Bộ Nhớ Vật Lý - Ổ Đĩa bền vững)
Tầng cuối cùng đảm bảo Durability (Toàn vẹn không mất dữ liệu).
- Khi TCL `COMMIT` khởi động từ Tầng Storage (RAM), Tầng Physical chạy chức năng Serialization Binary đóng gói toàn bộ dung lượng RAM xuống ổ đĩa, đè thành các tệp tin lưu trữ vật lý với định dạng riêng biệt chuẩn quốc tế KBMS:
  - **`.kmf`** (Knowledge Meta File)
  - **`.kdf`** (Knowledge Data File)
  - **`.klf`** (Knowledge Log File / Write-Ahead Log)

Quy trình tuần tự này giúp KBMS giữ vững tốc độ cực cao, nhưng cũng cực kỳ an toàn trước mọi lỗi mất điện, văng App hay chập cháy ổ đĩa!
