# 1.1. Lý do chọn đề tài

Trong lĩnh vực Trí tuệ nhân tạo, việc biểu diễn tri thức và xây dựng cơ chế suy luận tự động là hai bài toán nền tảng cho mọi hệ chuyên gia (Expert System). Các hệ quản trị cơ sở dữ liệu quan hệ (RDBMS) như MySQL hay PostgreSQL đã chứng minh hiệu quả vượt trội trong việc lưu trữ và truy xuất dữ liệu có cấu trúc. Tuy nhiên, bản chất của RDBMS là quản lý dữ liệu — không phải quản lý tri thức. Khi đối mặt với các bài toán đòi hỏi khả năng suy diễn, chẳng hạn như từ giả thiết "tam giác ABC có a = 3, b = 4, c = 5" để tự động tính ra diện tích, bán kính đường tròn ngoại tiếp hay phân loại đó là tam giác vuông, thì RDBMS không có cơ chế nào để thực hiện điều này. Khoảng cách giữa "dữ liệu thô" và "tri thức có khả năng suy diễn" chính là động lực thúc đẩy sự phát triển của các hệ thống dựa trên tri thức (Knowledge-based Systems).

Mô hình COKB (Computational Object Knowledge Base), được đề xuất bởi PGS.TS Đỗ Văn Nhơn từ năm 2001 [1], là một phương pháp biểu diễn tri thức theo hướng tiếp cận Ontology, cho phép mô hình hóa các miền tri thức phức tạp bao gồm cả khái niệm, quan hệ, phương trình tính toán và luật suy diễn. Mô hình này đã được ứng dụng thành công trong nhiều nghiên cứu về hệ giải bài toán thông minh, đặc biệt trong lĩnh vực hình học phẳng và hình học giải tích [2], [3]. Tuy nhiên, cho đến nay vẫn chưa có một hệ thống phần mềm hoàn chỉnh nào đóng vai trò như một hệ quản trị cơ sở tri thức (Knowledge Base Management System — KBMS) cho mô hình COKB — tức là một hệ thống tích hợp được cả khả năng lưu trữ bền vững, suy diễn tự động và cung cấp ngôn ngữ truy vấn tri thức cho người dùng.

Xuất phát từ thực tế đó, đề tài **"THIẾT KẾ HỆ HỖ TRỢ QUẢN TRỊ CƠ SỞ TRI THỨC DẠNG COKB"** được thực hiện nhằm nghiên cứu và phát triển một hệ thống KBMS hoàn chỉnh, lấy mô hình COKB làm nền tảng biểu diễn tri thức, đồng thời kế thừa các kỹ thuật quản trị dữ liệu từ lĩnh vực cơ sở dữ liệu để đảm bảo tính bền vững và hiệu năng trong thực tế.

# 1.2. Cơ sở lý thuyết về mô hình COKB

## 1.2.1. Cấu trúc tổng quát của mô hình COKB

Mô hình COKB là sự mở rộng của các phương pháp biểu diễn tri thức truyền thống — như logic vị từ, hệ luật dẫn, mạng ngữ nghĩa và Frame — theo hướng tích hợp khả năng tính toán trực tiếp vào cấu trúc đối tượng. Điểm khác biệt cốt lõi của COKB so với các phương pháp khác nằm ở chỗ: mỗi đối tượng trong COKB không chỉ mang thông tin mô tả (thuộc tính, quan hệ) mà còn chứa các phương trình toán học và luật suy diễn, cho phép hệ thống tự động tính toán và sinh ra tri thức mới từ dữ liệu đầu vào.

Theo [1] và [3], một cơ sở tri thức COKB được xác định bởi bộ 6 thành phần:

$$COKB = (C, H, R, Ops, Funcs, Rules)$$

Trong đó:

- **C (Concepts)** là tập hợp các khái niệm hay lớp đối tượng tính toán trong miền tri thức. Ví dụ, trong hình học phẳng, C bao gồm các khái niệm như Điểm, Đoạn thẳng, Góc, Tam giác, Tứ giác. Mỗi khái niệm được phân cấp (cấp 0, cấp 1, ..., cấp n) dựa trên độ phức tạp cấu trúc thuộc tính bên trong.

- **H (Hierarchy)** là tập các quan hệ phân cấp đặc biệt hóa giữa các khái niệm, tương tự quan hệ IS-A trong lập trình hướng đối tượng. Ví dụ: "Tam giác vuông" IS-A "Tam giác", nghĩa là mọi tri thức của Tam giác đều được kế thừa sang Tam giác vuông.

- **R (Relations)** là tập các quan hệ ngữ nghĩa giữa các đối tượng, chẳng hạn như quan hệ "song song", "vuông góc", "bằng nhau" trong hình học, hay "tương tác thuốc" trong y tế.

- **Ops (Operators)** là các toán tử tính toán trên các miền giá trị (số thực, vector, ma trận...), cho phép thực hiện các phép biến đổi đại số trên thuộc tính của đối tượng.

- **Funcs (Functions)** là các hàm xác định ánh xạ giữa các thuộc tính, ví dụ hàm tính diện tích tam giác theo ba cạnh.

- **Rules** là tập hợp các luật dẫn dùng để suy diễn ra tri thức mới. Mỗi luật có dạng "NẾU tập sự kiện U đúng THÌ suy ra tập sự kiện V", viết gọn là U → V.

## 1.2.2. Mô hình đối tượng tính toán

Ở cấp độ thực thể, mỗi đối tượng tính toán (Computational Object) O trong hệ thống được biểu diễn bởi bộ ba [2]:

$$O = (Attrs, Facts, Rules)$$

Trong đó **Attrs** là tập các thuộc tính của đối tượng — bản thân mỗi thuộc tính cũng có thể là một đối tượng tính toán thuộc lớp khái niệm khác, tạo nên cấu trúc đệ quy. **Facts** là tập các sự kiện, giá trị hay tính chất đã được xác định của đối tượng. **Rules** là các phương trình và luật dẫn nội tại ràng buộc mối quan hệ giữa các thuộc tính bên trong đối tượng đó.

Lấy ví dụ cụ thể: đối tượng Tam giác trong hình học phẳng có Attrs gồm ba đỉnh (A, B, C), ba cạnh (a, b, c), ba góc, các đường cao, đường trung tuyến, diện tích S, nửa chu vi p, bán kính đường tròn nội tiếp r và ngoại tiếp R. Phần Rules chứa hàng chục phương trình toán học — từ định lý cosin, định lý sin, công thức Heron cho đến các hệ thức giữa diện tích với đường cao. Khi người dùng cung cấp giá trị cho một vài thuộc tính (ví dụ: a = 3, b = 4, c = 5), hệ thống sẽ dựa vào các phương trình này để tự động suy diễn ra toàn bộ giá trị còn lại.

## 1.2.3. Cơ chế suy luận trên mô hình COKB

Quá trình suy luận trên mô hình COKB thực chất là quá trình mở rộng tập sự kiện đã biết (giả thiết) bằng cách áp dụng lặp đi lặp lại các luật trong Rules cho đến khi không còn luật nào có thể kích hoạt thêm. Trong lý thuyết, quá trình này tương ứng với việc tìm **bao đóng** (F-Closure) của tập sự kiện ban đầu [2], [4].

Luận văn của Mai Trung Thành [2] đã hệ thống hóa 6 quy tắc suy luận cơ bản trên mô hình COKB (ký hiệu RC1–RC6):

- **RC1 (Vốn có)**: Suy diễn sự kiện từ chính định nghĩa thuộc tính của đối tượng. Ví dụ, khi tạo một Tam giác thì hệ thống tự động xác nhận sự kiện "đối tượng này có 3 cạnh, 3 góc".

- **RC2 (Mặc nhiên)**: Các phép biến đổi đồng nhất và bắc cầu. Ví dụ, nếu AB = CD và CD = EF thì suy ra AB = EF.

- **RC3 (Thay thế quan hệ)**: Sử dụng các quan hệ tính toán (phương trình) để tính giá trị biến. Đây là quy tắc được kích hoạt nhiều nhất, vì phần lớn tri thức COKB được mã hóa dưới dạng phương trình toán học.

- **RC4 (Luật dẫn)**: Thực thi các luật logic dạng mệnh đề IF-THEN.

- **RC5 (Giải hệ phương trình)**: Khi nhiều phương trình cùng chia sẻ biến chung, hệ thống phối hợp chúng thành hệ phương trình và sử dụng các phương pháp số (Newton-Raphson, Brent) để giải.

- **RC6 (Hành vi nội bộ)**: Suy diễn dựa trên cấu trúc PART-OF — khi một thuộc tính bên trong đối tượng thay đổi, tri thức được lan truyền ngược lên đối tượng cha.

Việc kết hợp 6 quy tắc này trong một vòng lặp suy diễn tiến (Forward Chaining) cho phép hệ thống giải quyết tự động các bài toán từ đơn giản (tính một giá trị) đến phức tạp (giải hệ phương trình phi tuyến đa biến, chứng minh tính chất hình học).

# 1.3. Yêu cầu đối với một hệ quản trị cơ sở tri thức dạng COKB

Từ cơ sở lý thuyết trình bày ở mục 1.2, có thể thấy rằng một hệ quản trị cơ sở tri thức hoạt động trên mô hình COKB cần đáp ứng được ít nhất bốn nhóm yêu cầu chính sau đây.

**Thứ nhất, về biểu diễn tri thức**: hệ thống phải cho phép người dùng định nghĩa được đầy đủ 6 thành phần của mô hình COKB — từ việc khai báo khái niệm (Concept) với các thuộc tính, phương trình ràng buộc, luật dẫn, cho đến thiết lập quan hệ phân cấp kế thừa giữa các khái niệm. Đặc biệt, hệ thống cần hỗ trợ cấu trúc đệ quy (thuộc tính cũng là đối tượng tính toán) và cơ chế kế thừa tri thức từ lớp cha sang lớp con theo quan hệ IS-A.

**Thứ hai, về suy diễn tự động**: hệ thống cần tích hợp một bộ máy suy diễn (Inference Engine) có khả năng thực thi các quy tắc RC1–RC6, hỗ trợ cả suy diễn tiến lẫn giải hệ phương trình. Bộ máy suy diễn này phải hoạt động độc lập với miền tri thức cụ thể — nghĩa là cùng một engine có thể suy diễn trên tri thức hình học, tài chính hay y tế mà không cần viết lại mã nguồn.

**Thứ ba, về lưu trữ bền vững**: tri thức và dữ liệu cần được lưu trữ trên đĩa một cách an toàn, có khả năng phục hồi sau sự cố (crash recovery), hỗ trợ chỉ mục (indexing) để tìm kiếm nhanh trên tập dữ liệu lớn, và đảm bảo tính toàn vẹn dữ liệu thông qua cơ chế giao dịch (transaction).

**Thứ tư, về giao diện tương tác**: hệ thống cần cung cấp một ngôn ngữ truy vấn để người dùng có thể thao tác với tri thức (tạo, sửa, xóa, tìm kiếm, yêu cầu suy diễn) mà không cần lập trình trực tiếp. Ngoài ra, một công cụ trực quan hóa (IDE) sẽ giúp giảm rào cản tiếp cận cho người dùng không chuyên về lập trình.

# 1.4. Các công trình nghiên cứu liên quan

## 1.4.1. Các công trình nghiên cứu về mô hình COKB

Kể từ khi mô hình COKB được đề xuất, đã có nhiều công trình nghiên cứu nhằm hoàn thiện và mở rộng lý thuyết cho mô hình này.

Trong [3], nhóm tác giả đã nghiên cứu giải pháp suy luận tìm kiếm lời giải trên mô hình COKB dựa trên khái niệm **mẫu bài toán** (Problem Pattern). Công trình đưa ra các định nghĩa về mẫu bài toán cùng tiêu chuẩn lựa chọn mẫu trong một miền tri thức, nhờ đó giúp quá trình suy luận trở nên nhanh hơn bằng cách tái sử dụng các lời giải đã biết. Tuy nhiên, công trình chưa đề cập đến mô hình bài toán mẫu (Sample Problem) — một khái niệm có vai trò bổ sung trong các bài toán có tần suất xuất hiện thấp hơn.

Trong [14], nhóm tác giả tiếp tục phát triển hướng nghiên cứu này bằng cách đề xuất mô hình **bài toán mẫu** trên COKB, bao gồm các kỹ thuật tìm kiếm mẫu, áp dụng mẫu và cập nhật bài toán mẫu. Hai công trình [3] và [14] khi kết hợp lại đã tạo nên một bộ công cụ suy luận khá hoàn chỉnh về mặt lý thuyết cho việc giải quyết vấn đề trên COKB.

Các công trình [1], [13], [15] tập trung vào việc nghiên cứu chi tiết từng thành phần tri thức trong mô hình: [13] xem xét thành phần toán tử (Ops), [1] xem xét thành phần hàm (Funcs), và [15] nghiên cứu sự kết hợp giữa Funcs và Ops. Mặc dù mỗi công trình đều đạt được kết quả nhất định, phần lớn vẫn chỉ dừng lại ở các bài toán tính toán cơ bản trên số thực, chưa mở rộng đến các lớp vấn đề phức tạp hơn như rút gọn biểu thức hay tính toán trên cấu trúc dữ liệu đặc thù.

Luận văn của Mai Trung Thành [2] là công trình có tính tổng hợp cao nhất, đã hệ thống hóa 6 quy tắc suy luận (RC1–RC6) và xây dựng thuật giải tìm bao đóng F-Closure cho tập sự kiện. Đặc biệt, luận văn này đã thiết kế và cài đặt một bộ suy diễn (Inference Engine) bằng ngôn ngữ Maple, có khả năng giải quyết vấn đề tổng quát trên COKB và độc lập với miền tri thức cụ thể. Tuy nhiên, bộ suy diễn này được đóng gói dưới dạng thư viện Maple — một môi trường tính toán ký hiệu chuyên dụng, không phù hợp cho việc triển khai thành một hệ thống phần mềm độc lập phục vụ nhiều người dùng đồng thời.

**Nhận xét chung**: Các công trình nghiên cứu kể trên đã đóng góp đáng kể vào việc hoàn thiện cơ sở lý thuyết cho mô hình COKB. Tuy nhiên, về mặt ứng dụng, tất cả các bộ suy diễn được xây dựng đều mang tính thử nghiệm trong phạm vi hàn lâm, chưa có công trình nào hướng đến việc xây dựng một hệ quản trị tri thức hoàn chỉnh với đầy đủ các thành phần: lưu trữ, suy diễn, ngôn ngữ truy vấn và giao diện người dùng.

## 1.4.2. Các hệ quản trị cơ sở tri thức hiện có

Ngoài các công trình nghiên cứu trực tiếp trên COKB, hiện nay trên thế giới đã tồn tại một số hệ thống quản trị tri thức tiêu biểu, mỗi hệ thống phục vụ cho một mục đích và phương pháp biểu diễn khác nhau.

**CLIPS** [21] là hệ vỏ chuyên gia (Expert System Shell) được phát triển bởi NASA từ những năm 1980. CLIPS sử dụng thuật toán Rete để thực hiện suy diễn tiến trên hệ luật dẫn và đã được ứng dụng rộng rãi trong công nghiệp hàng không vũ trụ. Tuy nhiên, CLIPS không có lớp lưu trữ bền vững (dữ liệu chỉ tồn tại trong bộ nhớ), không hỗ trợ kiến trúc Client-Server và khả năng tính toán số học bị hạn chế.

**Jess** [22] là phiên bản Java của CLIPS, kế thừa phần lớn đặc điểm của hệ thống gốc. Dự án đã ngừng phát triển tích cực và không còn được cập nhật cho các nền tảng hiện đại.

**Drools** [23] (JBoss Rules Engine) sử dụng thuật toán Rete cải tiến (Phreak) và được ứng dụng rộng rãi trong quản lý quy tắc kinh doanh (Business Rule Management). Drools có hệ sinh thái mạnh mẽ trên nền tảng Java Enterprise, nhưng được thiết kế chủ yếu cho business logic — khả năng suy diễn toán học và giải hệ phương trình rất hạn chế.

**SWI-Prolog** [24] là một hệ thống lập trình logic hỗ trợ mạnh suy luận lùi (Backward Chaining) thông qua cơ chế unification. SWI-Prolog phù hợp cho các bài toán suy luận logic thuần túy, nhưng thiếu kiến trúc Client-Server, thiếu cơ chế lưu trữ bền vững và không có ngôn ngữ truy vấn dạng SQL.

**Protégé** [25] là công cụ mã nguồn mở của Đại học Stanford, chuyên dùng để xây dựng và chỉnh sửa Ontology theo chuẩn OWL/RDF. Protégé cung cấp giao diện trực quan rất tốt cho việc thiết kế ontology, nhưng bản chất nó là một trình soạn thảo, không phải là một hệ quản trị tri thức có khả năng suy diễn tính toán hay lưu trữ quy mô lớn.

**Cyc** [40] là dự án AI tham vọng nhất trong lịch sử, khởi động từ năm 1984 bởi Douglas Lenat, nhằm mã hóa toàn bộ tri thức thường thức (common sense) của con người. Cyc sử dụng ngôn ngữ CycL dựa trên logic vị từ bậc nhất và chứa hơn 1.5 triệu assertion. Tuy nhiên, Cyc được thiết kế cho suy luận logic định tính, thiếu khả năng tính toán định lượng trên đối tượng — tức không phù hợp cho các bài toán kỹ thuật cần giải phương trình hay tính toán số học phức tạp.

### Bảng 1.1: So sánh các hệ thống quản trị tri thức hiện có

| Tiêu chí | CLIPS | Jess | Drools | SWI-Prolog | Protégé | Cyc | **KBMS (đề xuất)** |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| Suy diễn tiến (Forward Chaining) | Có | Có | Có | Không | Hạn chế | Có | **Có** |
| Giải hệ phương trình | Không | Không | Không | Không | Không | Không | **Có** |
| Tính toán số học trên đối tượng | Hạn chế | Hạn chế | Hạn chế | Hạn chế | Không | Không | **Có** |
| Lưu trữ bền vững | Không | Không | DBMS ngoài | Không | File OWL | Có | **B+ Tree + WAL** |
| Kiến trúc Client-Server | Không | Không | Có | Không | Không | Có | **Có (TCP Binary)** |
| Ngôn ngữ truy vấn riêng | CLIPS DSL | Jess DSL | DRL | Prolog | SPARQL | CycL | **KBQL** |
| Phân quyền người dùng | Không | Không | Không | Không | Không | Hạn chế | **RBAC** |

# 1.5. Nhận định về hạn chế của các giải pháp hiện tại

Qua phân tích ở mục 1.4, có thể rút ra một số nhận định quan trọng về tình hình hiện tại của lĩnh vực quản trị cơ sở tri thức, đặc biệt đối với mô hình COKB.

**Về mặt lý thuyết**, các công trình nghiên cứu trên mô hình COKB đã đạt được nền tảng vững chắc — từ việc hình thức hóa 6 thành phần COKB, phân loại 6 quy tắc suy luận, cho đến xây dựng thuật giải tìm bao đóng và giải bài toán tổng quát. Tuy nhiên, các kết quả này vẫn chủ yếu tồn tại ở dạng lý thuyết trên giấy hoặc được cài đặt thử nghiệm trong môi trường Maple — một nền tảng toán học chuyên dụng, không phù hợp để triển khai thành phần mềm ứng dụng thực tế. Chưa có công trình nào đặt vấn đề xây dựng một hệ thống hoàn chỉnh bao gồm lưu trữ, mạng, suy diễn và giao diện người dùng cho mô hình COKB.

**Về mặt ứng dụng**, các hệ quản trị tri thức hiện có trên thế giới (CLIPS, Drools, SWI-Prolog, Protégé, Cyc) đều tồn tại ít nhất một trong các hạn chế sau:

- Không hỗ trợ tính toán số học và giải hệ phương trình trên đối tượng tri thức. Đây là hạn chế lớn nhất khi áp dụng cho mô hình COKB, bởi phần lớn tri thức trong COKB được mã hóa dưới dạng phương trình toán học (Rf) chứ không chỉ là luật logic IF-THEN.

- Không có lớp lưu trữ bền vững tích hợp sẵn, hoặc phải phụ thuộc vào hệ cơ sở dữ liệu bên ngoài. Điều này dẫn đến sự phức tạp trong triển khai và ảnh hưởng đến hiệu năng khi phải chuyển đổi qua lại giữa cấu trúc tri thức và cấu trúc dữ liệu quan hệ.

- Không được thiết kế để hỗ trợ cấu trúc đối tượng đệ quy đặc trưng của COKB — nơi mà thuộc tính của một đối tượng bản thân nó cũng là một đối tượng tính toán thuộc lớp khái niệm khác.

Tóm lại, hiện chưa có một hệ thống nào — dù trong phạm vi nghiên cứu học thuật hay trong các sản phẩm phần mềm thương mại — kết hợp được đồng thời ba khả năng: (1) biểu diễn tri thức theo mô hình COKB, (2) suy diễn tự động với khả năng tính toán số học, và (3) lưu trữ bền vững với hiệu năng cao.

# 1.6. Mục tiêu của đề tài

Trên cơ sở những hạn chế đã phân tích, đề tài đặt ra mục tiêu xây dựng một hệ quản trị cơ sở tri thức (KBMS) hoàn chỉnh dựa trên mô hình COKB, cụ thể bao gồm các mục tiêu sau.

**Mục tiêu 1 — Bộ máy lưu trữ (Storage Engine)**: Thiết kế và cài đặt một bộ máy lưu trữ chuyên biệt cho tri thức dạng COKB, sử dụng cấu trúc Slotted Page và chỉ mục cây B+ Tree để hỗ trợ truy xuất nhanh trên tập dữ liệu lớn. Hệ thống cần đảm bảo tính bền vững dữ liệu thông qua cơ chế ghi nhật ký trước (Write-Ahead Logging — WAL) và hỗ trợ phục hồi dữ liệu sau sự cố [5], [10].

**Mục tiêu 2 — Bộ máy suy diễn (Reasoning Engine)**: Xây dựng bộ máy suy diễn tiến dựa trên mạng Rete và thuật toán F-Closure, có khả năng thực thi đầy đủ 6 quy tắc suy luận (RC1–RC6) trên mô hình COKB. Bộ máy này phải hoạt động tổng quát — tức là có thể suy diễn trên bất kỳ miền tri thức nào (hình học, tài chính, y tế, kỹ thuật...) mà không cần thay đổi mã nguồn [1], [6], [9].

**Mục tiêu 3 — Ngôn ngữ truy vấn KBQL**: Thiết kế một ngôn ngữ truy vấn tri thức (Knowledge Base Query Language — KBQL) cùng bộ phân tích cú pháp (Lexer/Parser), cho phép người dùng thao tác với cơ sở tri thức thông qua các câu lệnh khai báo (DDL), thao tác dữ liệu (DML) và truy vấn suy diễn (DQL) [6].

**Mục tiêu 4 — Kiến trúc Client-Server và bảo mật**: Xây dựng hệ thống theo kiến trúc Client-Server với giao thức truyền tải nhị phân (Binary Protocol) qua TCP, hỗ trợ nhiều người dùng kết nối đồng thời. Hệ thống cần tích hợp cơ chế xác thực và phân quyền theo vai trò (RBAC).

**Mục tiêu 5 — Công cụ phát triển trực quan (Studio IDE)**: Phát triển một môi trường phát triển tích hợp giúp người dùng thiết kế, chỉnh sửa và kiểm chứng tri thức một cách trực quan, rút ngắn thời gian so với việc viết lệnh KBQL thủ công.

Tổng hợp lại, đề tài hướng đến việc chuyển hóa mô hình lý thuyết COKB — vốn chỉ tồn tại ở dạng nghiên cứu hàn lâm — thành một hệ thống phần mềm thực tế, có kiến trúc rõ ràng, hiệu năng đo lường được, và có khả năng ứng dụng vào nhiều lĩnh vực khác nhau.
