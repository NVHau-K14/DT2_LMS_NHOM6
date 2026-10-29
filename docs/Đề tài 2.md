**ĐỀ TÀI 02: HỆ THỐNG QUẢN LÝ VÀ CHẤM ĐIỂM HỌC TẬP TRỰC TUYẾN (LMS)** 

● **Hệ quản trị CSDL chính:** MongoDB (Document Store) 

● **Số lượng thành viên:** 03 sinh viên 

● **Yêu cầu sản phẩm:** Cổng học tập trực tuyến quản lý thông tin sinh viên, lớp học, điểm môn học; sinh viên tra cứu bảng điểm và giảng viên xuất báo cáo học lực. 

**1\. BAREM ĐÁNH GIÁ CHI TIẾT (THANG ĐIỂM 10.0)** 

● **1\. Báo cáo & Thiết kế CSDL (2.5đ):** 

○ *1.0đ:* Giải trình ưu điểm của Document Store trong việc lưu trữ hồ sơ học tập có cấu trúc lồng nhau. 

○ *1.5đ:* Bản vẽ JSON Schema hồ sơ sinh viên chứa mảng con các Object môn học, tín chỉ và điểm số. 

● **2\. Chức năng CRUD Nền tảng (2.5đ):** Giao diện/API quản lý lớp học, hồ sơ sinh viên và cập nhật điểm môn học. 

● **3\. Chức năng nâng cao đặc trưng (2.5đ):** 

○ Viết toán tử cập nhật trực tiếp vào mảng lồng nhau (\$push, arrayFilters, \$set). ○ Sử dụng **Aggregation Framework** (bắt buộc dùng \$unwind, \$group, \$project, \$cond/\$switch) để tính điểm trung bình tích lũy (GPA) có trọng số và xếp loại học lực hoàn toàn ở tầng CSDL. 

● **4\. Kiểm thử hiệu năng & Demo trực quan (2.5đ):** 

○ *1.0đ (Dummy Data):* Bộ dữ liệu mẫu đạt chuẩn định mức bên dưới. 

○ *1.5đ (Demo sống):* Thực hiện trơn tru các bước trong kịch bản demo bảo vệ. 

**2\. QUY ĐỊNH ĐỊNH MỨC BỘ DỮ LIỆU MẪU (DUMMY DATA)** 

● **Collection sinhvien:** Tối thiểu **100 Document** sinh viên (chia đều từ 4–6 lớp khác nhau). ● Mỗi sinh viên bắt buộc nhúng mảng chứa **4–8 môn học** có đầy đủ điểm số, số tín chỉ và giảng viên phụ trách. 

**3\. PHÂN CÔNG CÔNG VIỆC NHÓM 3 SINH VIÊN** 

● **SV1 (Data Architect & CRUD Lead):** Thiết kế Schema sinh viên lồng mảng môn học; viết API CRUD sinh viên và API cập nhật điểm trực tiếp vào mảng con (\$push, \$set). ● **SV2 (Advanced Query Specialist):** Viết Aggregation Pipeline dùng \$unwind, \$group tính GPA tích lũy và \$cond/\$switch phân loại học lực; tối ưu hóa pipeline.  
● **SV3 (Fullstack Integrator & DB Tester):** Nạp bộ dữ liệu 100 sinh viên mẫu vào MongoDB; kiểm thử độ chính xác của hàm tính GPA; thiết kế giao diện Portal Giảng viên/Sinh viên và điều phối Demo. 

**4\. KỊCH BẢN DEMO SỐNG BẢO VỆ** 

1\. **SV1** nhập điểm môn mới trên Web \$\\rightarrow\$ **SV3** mở Compass chứng minh điểm chui vào đúng mảng con của Document. 

2\. **SV2** chạy chức năng xuất báo cáo học lực \$\\rightarrow\$ Chứng minh GPA được tính tự động từ Aggregation Pipeline. 

3\. **SV3** demo chức năng Import/Export bảng điểm và giải thích dữ liệu sinh viên trong Compass.