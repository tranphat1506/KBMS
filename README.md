# KBMS - Knowledge-Based Management System

KBMS is a high-performance, transactional knowledge-based management system designed for storing, managing, and reasoning over complex symbolic and numeric knowledge. Distinct from traditional relational or NoSQL databases, KBMS combines a powerful 4-tier architecture with a sophisticated inference engine capable of both logical deduction (Forward/Backward Chaining) and advanced numeric constraint solving.

## Key Features

### Advanced Reasoning Engine
- **Inference Types**: Built-in support for Forward and Backward Chaining.
- **Numeric Solver**: Integrated numeric constraint solver (using Brent's method) for resolving algebraic equations within knowledge concepts.
- **Inheritance & Hierarchy**: First-class support for `IS_A` relationships with full property and rule inheritance.

### Transactional Integrity & Storage
- **Buffer Pool**: High-speed memory-mapped RAM cache for lazy-loading and rapid data access.
- **Shadow Paging**: Ensures atomic transactions. Changes are buffered in a shadow pool and only promoted to main memory and disk upon a successful `COMMIT`.
- **Write-Ahead Logging (WAL)**: Robust `.klf` logging for crash recovery and transactional safety.
- **Unified Format**: Custom binary serialization (`.kdf`, `.kmf`) with built-in encryption.

### 4-Tier Architecture
1. **Physical Layer**: Manages file I/O, paging, and the binary storage format.
2. **Storage Layer**: Implements the Buffer Pool, WAL, and Shadow Paging.
3. **Knowledge Layer**: Handles Concepts, Variables, Attributes, and Hierarchy.
4. **Reasoning Layer**: Executes KBQL queries, solves constraints, and performs inference.

## Quick Start

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Installation
1. Clone the repository: `git clone https://github.com/tranphat1506/KBMS.git`
2. Build the project: `dotnet build`
3. Run the Server: `dotnet run --project KBMS.Server`
4. Connect via CLI: `dotnet run --project KBMS.CLI`

### Basic Usage
```kbql
CREATE KNOWLEDGE BASE world_kb;
USE world_kb;

CREATE CONCEPT Rectangle (
    VARIABLES (
        width: double,
        height: double,
        area: double
    )
    CONSTRAINTS (
        area = width * height
    )
);

INSERT INTO Rectangle ATTRIBUTE ( width:5.0, height:10.0 );

SOLVE ON CONCEPT Rectangle GIVEN width:2.0, height:4.0 FIND area SAVE;
```

## 📚 Documentation
- [Architecture Details](docs/ARCHITECTURE.md)
- [KBQL Reference](docs/KBQL_REFERENCE.md)
- [Installation Guide](docs/INSTALLATION.md)
- [Usage Guide & Examples](docs/USAGE_GUIDE.md)

## ⚖️ License
Distributed under the MIT License. See `LICENSE` for more information.
