using MongoDB.Bson;

namespace DT2_LMS_NHOM6.Pipelines;

/// <summary>
/// Các pipeline đếm / liệt kê nhỏ phục vụ màn hình danh mục (không tính điểm):
///
/// SiSoTheoLop – sĩ số mỗi lớp sinh hoạt (A-02). Sĩ số KHÔNG lưu trong lophoc mà luôn đếm từ sinhvien (mục 4).
///   1. $group – theo maLop, đếm số document.        [barem: $group]
///   2. $project – đổi _id thành maLop.              [barem: $project]
///
/// SoLanHocTheoMon – số lần học (phần tử monHoc) của mỗi môn (A-08):
/// dùng cho cảnh báo "Cập nhật tên cho N lần học" khi đổi tên môn và chặn xóa môn đã có người học.
///   1. $unwind – mỗi lần học một document.          [barem: $unwind]
///   2. $group  – theo monHoc.maMon, đếm lần học và số SV khác nhau ($addToSet).
///   3. $project – maMon, soLanHoc, soSV.
///
/// HocKyCoDuLieu – các học kỳ đã có lần học (ô chọn phạm vi báo cáo A-10), mới nhất trước:
///   1. $unwind – mỗi lần học một document.
///   2. $group  – theo (namHoc, hocKy).
///   3. $project – namHoc, hocKy.
///   4. $sort   – namHoc ↓, hocKy ↓.
///
/// LopHocPhanToanTruong – mọi lớp học phần (maMon, namHoc, hocKy, maGV) của toàn trường, cho Admin chọn
/// lớp học phần cần xuất bảng điểm ở A-11 (GĐ7):
///   1. $unwind – mỗi lần học một document.
///   2. $group  – theo (maMon, namHoc, hocKy, giangVien.maGV): tên môn, tên GV, sĩ số.
///   3. $project – trường phẳng.
///   4. $sort   – học kỳ mới nhất trước, rồi mã môn, mã GV.
/// </summary>
public static class ThongKeDanhMucPipeline
{
    public const string SiSoTheoLopJson = """
        [
          { "$group": { "_id": "$maLop", "siSo": { "$sum": 1 } } },
          { "$project": { "_id": 0, "maLop": "$_id", "siSo": 1 } }
        ]
        """;

    public const string SoLanHocTheoMonJson = """
        [
          { "$unwind": "$monHoc" },
          { "$group": {
              "_id": "$monHoc.maMon",
              "soLanHoc": { "$sum": 1 },
              "dsSV": { "$addToSet": "$maSV" } } },
          { "$project": { "_id": 0, "maMon": "$_id", "soLanHoc": 1, "soSV": { "$size": "$dsSV" } } }
        ]
        """;

    public const string HocKyCoDuLieuJson = """
        [
          { "$unwind": "$monHoc" },
          { "$group": { "_id": { "namHoc": "$monHoc.namHoc", "hocKy": "$monHoc.hocKy" } } },
          { "$project": { "_id": 0, "namHoc": "$_id.namHoc", "hocKy": "$_id.hocKy" } },
          { "$sort": { "namHoc": -1, "hocKy": -1 } }
        ]
        """;

    public const string LopHocPhanToanTruongJson = """
        [
          { "$unwind": "$monHoc" },
          { "$group": {
              "_id": { "maMon": "$monHoc.maMon", "namHoc": "$monHoc.namHoc", "hocKy": "$monHoc.hocKy", "maGV": "$monHoc.giangVien.maGV" },
              "tenMon": { "$first": "$monHoc.tenMon" },
              "tenGV": { "$first": "$monHoc.giangVien.hoTen" },
              "siSo": { "$sum": 1 } } },
          { "$project": {
              "_id": 0,
              "maMon": "$_id.maMon",
              "namHoc": "$_id.namHoc",
              "hocKy": "$_id.hocKy",
              "maGV": "$_id.maGV",
              "tenMon": 1,
              "tenGV": 1,
              "siSo": 1 } },
          { "$sort": { "namHoc": -1, "hocKy": -1, "maMon": 1, "maGV": 1 } }
        ]
        """;

    public static BsonDocument[] LopHocPhanToanTruong() => PipelineJson.Tao(LopHocPhanToanTruongJson);

    public static BsonDocument[] SiSoTheoLop() => PipelineJson.Tao(SiSoTheoLopJson);

    public static BsonDocument[] HocKyCoDuLieu() => PipelineJson.Tao(HocKyCoDuLieuJson);

    public static BsonDocument[] SoLanHocTheoMon() => PipelineJson.Tao(SoLanHocTheoMonJson);
}
