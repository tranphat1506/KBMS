# 6.1. Quy trình Cài đặt & Xác thực Hệ thống (macOS)

Tài liệu này trình bày quy trình triển khai chuẩn cho hệ thống [KBMS](../00-glossary/01-glossary.md#kbms) trên môi trường **macOS**. Việc sử dụng các công cụ quản lý gói hiện đại giúp đảm bảo tính nhất quán của các phụ thuộc (dependencies) trong dự án.

## 1. Thành phần Yêu cầu
Hệ thống yêu cầu các thành phần phần mềm sau được cài đặt qua trình quản lý gói `Homebrew`:
- **.NET 8.0 SDK**: Nền tảng thực thi cho KBMS Server và CLI.
- **Node.js (v18+)**: Nền tảng cho KBMS Studio (IDE).
- **Git**: Quản lý mã nguồn tri thức.

## 2. Các bước Triển khai
Mở ứng dụng `Terminal` và thực thi chuỗi lệnh sau:

```zsh
# Cài đặt .NET SDK
brew install --cask dotnet-sdk

# Cài đặt Node.js và các công cụ bổ trợ
brew install node git
```

Sau khi cài đặt, thực hiện cấp quyền thực thi cho tệp nhị phân CLI:
```zsh
chmod +x ./kbms-cli
```

## 3. Nhật ký Xác thực Cài đặt (Verification Log)
Dưới đây là nhật ký thực tế khi khởi chạy phiên bản KBMS đầu tiên từ dòng lệnh để đảm bảo môi trường đã sẵn sàng:

```zsh
$ ./kbms-cli VERSION
[KBMS CLI V3.4] Knowledge Base Management System
Compatible Engine: V3.x (Binary Storage Optimized)
Status: READY

$ cd KBMS.Server && dotnet run
[2026-04-01 23:41:02] [INFO] [System] [Kernel] KBMS Server started on 127.0.0.1:8400
[SystemBootstrapper] 'system' Knowledge Base (V3) found. Loading...
[SystemBootstrapper] Successfully loaded 3 concepts and 12 internal rules.
[Kernel] Listening for binary protocol connections...
```

Việc xác thực thông qua nhật ký trên cho thấy Server đã nhận diện được cơ sở tri thức hệ thống (`system KB`) và sẵn sàng tiếp nhận các yêu cầu từ Client.
