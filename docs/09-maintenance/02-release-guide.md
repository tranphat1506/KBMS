# Hướng dẫn Tạo GitHub Release Chuẩn

Tài liệu này hướng dẫn cách đóng gói và xuất bản một phiên bản (Release) chuyên nghiệp cho hệ quản trị KBMS trên GitHub.

## 1. Quy tắc Đặt tên (Semantic Versioning)

Sử dụng định dạng: `vX.Y.Z`
* **X (Major):** Thay đổi lớn về kiến trúc hoặc Breaking Changes (vd: đổi định dạng file .dat).
* **Y (Minor):** Thêm tính năng mới (vd: thêm thuật toán suy diễn mới).
* **Z (Patch):** Sửa lỗi nhỏ, tối ưu hiệu năng.

## 2. Cấu trúc Release Notes mẫu

Khi tạo Release trên GitHub, hãy sử dụng Markdown để trình bày:

```markdown
# Release v3.4.0 - "The Reasoning Update"

## Tính năng mới
* **Inference Engine:** Hỗ trợ giải phương trình đa biến bằng phương pháp Newton-Raphson.
* **Visuals:** Tích hợp bộ sơ đồ trực quan hóa quy trình bằng Mermaid (đã convert sang PNG).
* **Samples:** Bổ sung gói tri thức mẫu cho Tài chính và Y tế.

## Các lỗi đã sửa
* Sửa lỗi Logic trong thuật toán Forward Chaining khi xử lý Rule lồng nhau.
* Khắc phục lỗi hiển thị tiếng Việt trong giao diện CLI.

## Đóng gói ứng dụng (Assets)
Mọi phiên bản đều được đóng gói sẵn cho các nền tảng phổ biến:
* `KBMS_Server_v3.4.0_win_x64.zip`
* `KBMS_CLI_v3.4.0_macos_arm64.tar.gz`
```

---

## 3. Quy trình đóng gói riêng biệt

Để tạo các bản Release riêng lẻ cho từng thành phần, hãy thực hiện các lệnh sau tùy theo mục tiêu:

### 3.1. Đóng gói KBMS Server (.NET)
Server cần được đóng gói kèm theo các tệp cấu hình cơ bản.
```bash
# Build cho Windows
dotnet publish KBMS.Server -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Build cho macOS (Chip Apple)
dotnet publish KBMS.Server -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true
```
*Lưu ý:* Sau khi build, hãy nén thư mục `publish` và đổi tên thành `KBMS_Server_v3.4.0_[nền_tảng].zip`.

### 3.2. Đóng gói KBMS CLI (.NET)
CLI thường được đóng gói thành một file thực thi duy nhất để dễ dàng di chuyển.
```bash
# Build cho Windows
dotnet publish KBMS.CLI -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Build cho macOS
dotnet publish KBMS.CLI -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true
```
*Lưu ý:* Tên file nén nên là `KBMS_CLI_v3.4.0_[nền_tảng].zip`.

### 3.3. Đóng gói KBMS Studio (Electron)
Studio yêu cầu NodeJS và sử dụng `electron-builder` để tạo bộ cài đặt hoặc bản portable.
```bash
cd kbms-studio
npm install
npm run build
npx electron-builder build --win --mac --publish never
```
*Lưu ý:* Các tệp tin cài đặt (.exe, .dmg) sẽ nằm trong thư mục `kbms-studio/release/`. Hãy đặt tên là `KBMS_Studio_v3.4.0_[nền_tảng].zip`.

---

## 4. Checklist trước khi Publish

- [ ] Da cap nhat version trong file kbms.ini va package.json.
- [ ] Da chay toan bo unit test va dam bao pass 100%.
- [ ] Da cap nhat readme.md hoac changelog.
- [ ] Da gan Tag cho commit cuoi cung tren nhanh main.
- [ ] Da build rieng biet 3 thanh phan va kiem tra thu tren moi truong sach.
