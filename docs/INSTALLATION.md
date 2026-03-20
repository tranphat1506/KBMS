# Installation & Environment Guide

This guide ensures your system is correctly configured to run KBMS.

## 1. Prerequisites
- **Operating System**: Windows, macOS, or Linux.
- **Runtime**: [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
- **IDE (Optional)**: Visual Studio 2022 or VS Code with C# Dev Kit.

## 2. Project Structure
- `KBMS.Server`: The core database engine and network listener.
- `KBMS.CLI`: The command-line interface for interacting with the server.
- `KBMS.Parser`: Lexer and Parser for KBQL.
- `KBMS.Storage`: Physical and storage layer implementations (WAL, Buffer Pool).
- `KBMS.Reasoning`: The Inference Engine and Numeric Solvers.

## 3. Running for the First Time

### Step 1: Clone and Build
```bash
git clone https://github.com/tranphat1506/KBMS.git
cd KBMS
dotnet build
```

### Step 2: Start the Server
The server listens on `localhost:34000` by default.
```bash
dotnet run --project KBMS.Server
```

### Step 3: Run the CLI
In a new terminal window:
```bash
dotnet run --project KBMS.CLI
```

## 4. Default Configurations
- **Data Directory**: By default, KBMS creates a `data/` folder in the server's execution directory to store `.kdf`, `.kmf`, and `.klf` files.
- **Security**: The default administrator is `root` (password is prompted or set on first run).
- **Encryption**: KBMS uses AES-256 for data at rest. Ensure you have a consistent encryption key configured in the server's `appsettings.json` if used in a production environment.
