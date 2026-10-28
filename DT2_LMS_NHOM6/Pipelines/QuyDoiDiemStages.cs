using MongoDB.Bson;

namespace DT2_LMS_NHOM6.Pipelines;

/// <summary>
/// NGUỒN DUY NHẤT của công thức tính điểm (thiết kế mục 3). Mọi pipeline khác ghép các cụm này
/// vào bằng chỗ giữ "@quyDoi" và "@gpaXepLoai" – không pipeline nào tự viết lại công thức.
///
/// KHÔNG dùng toán tử làm tròn có sẵn của MongoDB: nó làm tròn kiểu "banker's" (half-to-even),
/// 84.5 → 84 → 8.4 (sai, phải là 8.5).
/// Thay bằng floor(x + 0.5000001): làm tròn nửa lên; 0.0000001 chống sai số dấu phẩy động.
/// </summary>
public static class QuyDoiDiemStages
{
    // ───────────── Cụm "@quyDoi": 4 stage, đặt ngay sau { $unwind: "$monHoc" } ─────────────

    // Stage 1 – duDiem: true khi cả 3 điểm thành phần CC, GK, CK đều khác null.
    // $ifNull(x, null) để trường bị thiếu cũng được coi là null. Không đủ điểm → không tính vào GPA nào.
    private const string StageDuDiem = """
        { "$addFields": { "monHoc.duDiem": { "$and": [
            { "$ne": [ { "$ifNull": [ "$monHoc.diem.cc", null ] }, null ] },
            { "$ne": [ { "$ifNull": [ "$monHoc.diem.gk", null ] }, null ] },
            { "$ne": [ { "$ifNull": [ "$monHoc.diem.ck", null ] }, null ] } ] } } }
        """;

    // Stage 2 – diemTK (barem: $cond): đủ điểm thì
    //   tong   = cc*tsCC + gk*tsGK + ck*tsCK         (trọng số là số nguyên %, tong trong 0–1000)
    //   diemTK = floor(tong / 10 + 0.5000001) / 10   (làm tròn 1 chữ số, nửa lên: 845 → 84.5 → 85 → 8.5)
    // chưa đủ điểm thì null.
    private const string StageDiemTK = """
        { "$addFields": { "monHoc.diemTK": { "$cond": [ "$monHoc.duDiem",
            { "$divide": [ { "$floor": { "$add": [
                { "$divide": [ { "$add": [
                    { "$multiply": [ "$monHoc.diem.cc", "$monHoc.trongSo.cc" ] },
                    { "$multiply": [ "$monHoc.diem.gk", "$monHoc.trongSo.gk" ] },
                    { "$multiply": [ "$monHoc.diem.ck", "$monHoc.trongSo.ck" ] } ] }, 10 ] },
                0.5000001 ] } }, 10 ] },
            null ] } } }
        """;

    // Stage 3 – diemChu (barem: $switch): xét từ ngưỡng cao xuống thấp (mục 3.2).
    // 8.5–10 A · 7.0–8.4 B · 5.5–6.9 C · 4.0–5.4 D · < 4.0 F · chưa có TK → null.
    private const string StageDiemChu = """
        { "$addFields": { "monHoc.diemChu": { "$switch": { "branches": [
            { "case": { "$eq": [ "$monHoc.diemTK", null ] }, "then": null },
            { "case": { "$gte": [ "$monHoc.diemTK", 8.5 ] }, "then": "A" },
            { "case": { "$gte": [ "$monHoc.diemTK", 7.0 ] }, "then": "B" },
            { "case": { "$gte": [ "$monHoc.diemTK", 5.5 ] }, "then": "C" },
            { "case": { "$gte": [ "$monHoc.diemTK", 4.0 ] }, "then": "D" } ],
            "default": "F" } } } }
        """;

    // Stage 4 – diemHe4 ($switch: A=4, B=3, C=2, D=1, F=0, chưa có → null)
    //           và dat ($cond: chưa có TK → null; TK ≥ 4.0 → true, ngược lại false).
    private const string StageHe4VaDat = """
        { "$addFields": {
            "monHoc.diemHe4": { "$switch": { "branches": [
                { "case": { "$eq": [ "$monHoc.diemChu", "A" ] }, "then": 4 },
                { "case": { "$eq": [ "$monHoc.diemChu", "B" ] }, "then": 3 },
                { "case": { "$eq": [ "$monHoc.diemChu", "C" ] }, "then": 2 },
                { "case": { "$eq": [ "$monHoc.diemChu", "D" ] }, "then": 1 },
                { "case": { "$eq": [ "$monHoc.diemChu", "F" ] }, "then": 0 } ],
                "default": null } },
            "monHoc.dat": { "$cond": [ { "$eq": [ "$monHoc.diemTK", null ] }, null,
                                       { "$gte": [ "$monHoc.diemTK", 4.0 ] } ] } } }
        """;

    // ───────────── Cụm "@gpaXepLoai": 2 stage, đặt sau $group ─────────────
    // Yêu cầu document đầu vào có sẵn 2 trường do $group tính:
    //   tongDiemTC    = Σ(diemHe4 × soTinChi) của các lần học được tính GPA
    //   tongTinChiGpa = Σ soTinChi của các lần học đó

    // Stage 1 – gpa = tongDiemTC / tongTinChiGpa, làm tròn 2 chữ số nửa lên: floor(x*100 + 0.5000001) / 100.
    // Không có tín chỉ nào được tính → gpa = null ($cond tránh chia cho 0).
    private const string StageGpa = """
        { "$addFields": { "gpa": { "$cond": [ { "$gt": [ "$tongTinChiGpa", 0 ] },
            { "$divide": [ { "$floor": { "$add": [
                { "$multiply": [ { "$divide": [ "$tongDiemTC", "$tongTinChiGpa" ] }, 100 ] },
                0.5000001 ] } }, 100 ] },
            null ] } } }
        """;

    // Stage 2 – xepLoai ($switch) theo GPA ĐÃ LÀM TRÒN (mục 3.4); gpa null → "Chưa xếp loại".
    private const string StageXepLoai = """
        { "$addFields": { "xepLoai": { "$switch": { "branches": [
            { "case": { "$eq": [ "$gpa", null ] }, "then": "Chưa xếp loại" },
            { "case": { "$gte": [ "$gpa", 3.6 ] }, "then": "Xuất sắc" },
            { "case": { "$gte": [ "$gpa", 3.2 ] }, "then": "Giỏi" },
            { "case": { "$gte": [ "$gpa", 2.5 ] }, "then": "Khá" },
            { "case": { "$gte": [ "$gpa", 2.0 ] }, "then": "Trung bình" },
            { "case": { "$gte": [ "$gpa", 1.0 ] }, "then": "Yếu" } ],
            "default": "Kém" } } } }
        """;

    /// <summary>4 stage quy đổi: duDiem → diemTK → diemChu → diemHe4 + dat (thứ tự bắt buộc vì stage sau dùng kết quả stage trước).</summary>
    public static BsonDocument[] QuyDoi() =>
        [Parse(StageDuDiem), Parse(StageDiemTK), Parse(StageDiemChu), Parse(StageHe4VaDat)];

    /// <summary>2 stage tính gpa (làm tròn floor) rồi xepLoai.</summary>
    public static BsonDocument[] GpaXepLoai() => [Parse(StageGpa), Parse(StageXepLoai)];

    private static BsonDocument Parse(string json) => BsonDocument.Parse(json);
}
