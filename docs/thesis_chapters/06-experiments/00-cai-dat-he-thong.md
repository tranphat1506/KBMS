# 6. Chương 6: Cài đặt và Thực nghiệm

Chương này trình bày chi tiết quy trình cài đặt hệ thống KBMS, các kịch bản kiểm thử đa tầng để chứng minh tính ổn định và các kết quả đánh giá hiệu năng trên các bộ dữ liệu thực tế.

## 6.1. Hướng dẫn cài đặt hệ thống

Hệ thống KBMS được thiết kế theo kiến trúc module hóa, cho phép cài đặt và chạy các thành phần độc lập trên các môi trường khác nhau.

### 6.1.1. Cài đặt KBMS.Server (Cốt lõi)

KBMS.Server là thành phần quản lý lưu trữ (LSM-Tree) và công cụ suy diễn (Rete Engine).

**Yêu cầu hệ thống:**
*   .NET 8.0 SDK hoặc Runtime.
*   Bộ nhớ trống tối thiểu: 512MB RAM.
*   Ổ đĩa: 1GB cho file dữ liệu và WAL (Write-Ahead Log).

**Các bước cài đặt:**
1.  **Clone mã nguồn:**
    ```bash
    git clone https://github.com/tranphat1506/KBMS.git
    cd KBMS/KBMS.Server
    ```
2.  **Xây dựng ứng dụng:**
    ```bash
    dotnet build -c Release
    ```
3.  **Chạy máy chủ:**
    ```bash
    dotnet run -c Release -- --port 35000 --data-dir ./data
    ```

### 6.1.2. Cài đặt KBMS.CLI (Giao diện dòng lệnh)

Công cụ dành cho quản trị viên và nhà phát triển để tương tác trực tiếp với tri thức qua ngôn ngữ KBQL.

**Các bước cài đặt:**
1.  Truy cập thư mục CLI: `cd KBMS.CLI`
2.  Xây dựng: `dotnet build -c Release`
3.  Sử dụng:
    ```bash
    ./KBMS.CLI --server localhost --port 35000
    ```

### 6.1.3. Cài đặt KBMS-Studio (Giao diện đồ họa)

Giao diện web trực quan giúp thiết kế Concept, Rule và theo dõi trạng thái hệ thống.

**Yêu cầu:** Node.js v18+.

**Các bước cài đặt:**
1.  Cài đặt dependencies:
    ```bash
    cd KBMS-Studio
    npm install
    ```
2.  Khởi chạy chế độ phát triển:
    ```bash
    npm run dev
    ```
3.  Truy cập qua trình duyệt: `http://localhost:5173`

---
> [!TIP]
> Người dùng nên khởi chạy **KBMS.Server** trước khi sử dụng CLI hoặc Studio để đảm bảo việc kết nối và truy xuất dữ liệu diễn ra thông suốt.
