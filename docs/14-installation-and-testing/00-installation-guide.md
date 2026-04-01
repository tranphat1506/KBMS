# 14.1. Hướng dẫn Cài đặt & Triển khai

Hệ thống [KBMS](../00-glossary/01-glossary.md#kbms) được thiết kế để chạy đa nền tảng nhờ sức mạnh của .NET 8 và [Electron](../00-glossary/01-glossary.md#electron). Dưới đây là hướng dẫn chi tiết để thiết lập môi trường trên 3 hệ điều hành phổ biến.

---

## 1. Yêu cầu Tiên quyết

Dù ở hệ điều hành nào, bạn cũng cần cài đặt các thành phần cốt lõi sau:
1.  **.NET 8 SDK**: Để chạy Server và [CLI](../00-glossary/01-glossary.md#cli).
2.  **Node.js (v18+)**: Để chạy và phát triển Studio [IDE](../00-glossary/01-glossary.md#ide).
3.  **Git**: Để quản lý mã nguồn.

---

## 2. Cài đặt trên Windows 10/11

### Bước 1: Cài đặt .NET 8 & Node.js
*   **Cách 1 (Thủ công)**: Tải Installer từ [dotnet.microsoft.com](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) và [nodejs.org](https://nodejs.org/).
*   **Cách 2 (Dùng lệnh - PowerShell/[Chocolatey](../00-glossary/01-glossary.md#chocolatey))**:
    ```powershell
    choco install dotnet-sdk-8.0 nodejs git -y
    ```

### Bước 2: Chạy hệ thống
1.  Mở PowerShell, di chuyển đến thư mục Server:
    ```powershell
    dotnet run --project KBMS.Server
    ```
2.  Mở thư mục Studio:
    ```powershell
    npm install
    npm run dev
    ```

![Giao diện cài đặt thành công KBMS trên Windows](../assets/diagrams/placeholder_windows_install_success.png)

---

## 3. Cài đặt trên macOS

### Bước 1: Cài đặt qua [Homebrew]
Mở Terminal và thực hiện:
```zsh
# Cài đặt Homebrew nếu chưa có
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

# Cài đặt dependencies
brew install --cask dotnet-sdk
brew install node git
```

### Bước 2: Cấp quyền thực thi
Đối với [CLI](../00-glossary/01-glossary.md#cli), bạn cần cấp quyền cho tệp thực thi:
```zsh
chmod +x ./kbms-cli
```

![Giao diện cài đặt thành công KBMS trên macOS](../assets/diagrams/placeholder_macos_install_success.png)

---

## 4. Cài đặt trên Linux

### Bước 1: Cấu hình Repository
Thực hiện các lệnh sau để cài đặt .NET 8:
```bash
declare repo_version=$(if [ $(command -v lsb_release) ]; then lsb_release -r -s; else echo 22.04; fi)
wget https://packages.microsoft.com/config/ubuntu/$repo_version/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

sudo apt-get update && sudo apt-get install -y dotnet-sdk-8.0 nodejs npm git
```

### Bước 2: Cài đặt Electron
```bash
cd kbms-studio
npm install
# Lưu ý: Cần có môi trường X11 hoặc Wayland để chạy giao diện Studio
npm run electron:dev
```

![Giao diện cài đặt thành công KBMS trên Linux](../assets/diagrams/placeholder_linux_install_success.png)

---

## 5. Xác thực Cài đặt

Sau khi cài đặt, hãy chạy lệnh sau để đảm bảo mọi thứ đã sẵn sàng:

*Bảng 14.1: Yêu cầu cấu hình hệ thống tối thiểu*
| Thành phần | Lệnh kiểm tra | Kết quả mong đợi |
| :--- | :--- | :--- |
| **Server Engine** | `dotnet --list-sdks` | Có phiên bản `8.0.x` |
| **Studio Base** | `node -v` | Có phiên bản `v18.x` hoặc cao hơn |
| **Network Port** | `netstat -ano | findstr 3307` | Cổng 3307 đang `LISTENING` |

---

