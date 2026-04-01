# 04.5. Tầng Lưu trữ

Tầng Lưu trữ chịu trách nhiệm đảm bảo tính bền vững (Durability), an toàn (Security) và điều phối truy cập đồng thời cho tri thức.

## 1. Ánh xạ Thành phần Hệ thống

Hệ thống quản lý việc lưu trữ qua một phân cấp các Manager chuyên biệt:

![Sơ đồ thành phần tầng lưu trữ. | width=0.75](../assets/diagrams/storage_layer_map.png)
*Hình 4.4: Bản đồ giải phẫu thành phần tầng lưu trữ vật lý.*

*   **[StoragePool](../00-glossary/01-glossary.md#storagepool) (Singleton)**: Quản lý danh sách các [KBs](../00-glossary/01-glossary.md#kbs) đang nạp và đảm bảo Thread-safety khi truy cập đa luồng.
*   **[BufferPoolManager](../00-glossary/01-glossary.md#bufferpoolmanager) ([LRU](../00-glossary/01-glossary.md#lru) Cache)**: Điều phối bộ nhớ đệm RAM theo từng trang (**[Page](../00-glossary/01-glossary.md#page)**).
*   **[DiskManager](../00-glossary/01-glossary.md#diskmanager) (Physical Handler)**: Giao tiếp trực tiếp với hệ điều hành để đọc/ghi tệp nạp theo khối (16KB).
*   **WalManager (Transaction Logger)**: Quản lý tệp nhật ký `.wal` để đảm bảo [ACID](../00-glossary/01-glossary.md#acid).

## 1. Quản lý Trang

*   **[Slotted Page](../00-glossary/01-glossary.md#slotted-page) Layout**: Sử dụng cấu trúc trang 16KB với **24 Bytes [Header](../00-glossary/01-glossary.md#header)** (PageId, [LSN](../00-glossary/01-glossary.md#lsn), Prev, Next, FreePtr, Count).
*   **Hệ thống Slot**: Cho phép quản lý các bản ghi có độ dài thay đổi cực kỳ hiệu quả mà không làm phân mảnh đĩa.

## 2. Cấu trúc Chỉ mục (B+ Tree Indexing)

*   **Hiệu năng**: Tìm kiếm và truy xuất bản ghi với độ phức tạp $O(\log n)$.
*   **[Clustered Index](../00-glossary/01-glossary.md#clustered-index)**: Tích hợp dữ liệu trực tiếp vào các nút lá (Leaf Nodes) của cây [B+ Tree](../00-glossary/01-glossary.md#b-tree) để tối thiểu hóa số lần đọc đĩa.

## 4. Điều phối Truy cập Đồng thời (Latching & Pinning)

Hệ thống hỗ trợ đa người dùng thao tác cùng lúc thông qua cơ chế [Page](../00-glossary/01-glossary.md#page) [Latching](../00-glossary/01-glossary.md#latching):
*   **[Page](../00-glossary/01-glossary.md#page) [Pinning](../00-glossary/01-glossary.md#pinning)**: Khi một Query cần dữ liệu, Bpm sẽ cung cấp một [Frame](../00-glossary/01-glossary.md#frame) trong RAM và tăng `PinCount`. Trang có `PinCount > 0` sẽ bị khóa vĩnh viễn trong RAM (Pinned), thuật toán [LRU](../00-glossary/01-glossary.md#lru) không thể loại bỏ nó cho tới khi được `Unpin`.
*   **[LRU](../00-glossary/01-glossary.md#lru) [Eviction](../00-glossary/01-glossary.md#eviction)**: Khi Cache đầy, các trang có `PinCount = 0` và ít được sử dụng nhất sẽ bị trục xuất (Evicted) sau khi đã được đẩy (**[Flush](../00-glossary/01-glossary.md#flush)**) xuống đĩa nếu là trang bẩn (**Dirty**).
*   **Write-Ahead Logging (WAL)**: Mọi thay đổi dữ liệu đều được ghi nhật ký trước khi thực hiện trên RAM, đảm bảo khả năng phục hồi (Recovery) bất chấp mọi sự cố nguồn điện.

## 4. Bảo mật Dữ liệu

*   **[AES-256](../00-glossary/01-glossary.md#aes-256)**: Mã hóa toàn diện dữ liệu tĩnh ([At-rest](../00-glossary/01-glossary.md#at-rest)). Tệp dữ liệu thô trên đĩa hoàn toàn không thể đọc được nếu không có [Master Key](../00-glossary/01-glossary.md#master-key) của hệ thống.

**Hai tầng Lưu trữ**: Hệ thống phân tách vật lý thành hai loại tệp: tệp dữ liệu thực thể (`.kdb`) lưu các `ObjectInstance` và tệp nhật ký giao dịch (`.wal`) lưu Write-Ahead Log. Cả hai đều được mã hóa [AES-256](../00-glossary/01-glossary.md#aes-256) để bảo vệ bí mật dữ liệu khi lưu trữ tĩnh ([At-rest](../00-glossary/01-glossary.md#at-rest)).