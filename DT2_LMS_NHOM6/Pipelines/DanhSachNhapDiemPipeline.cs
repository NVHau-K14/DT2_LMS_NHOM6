using MongoDB.Bson;

namespace DT2_LMS_NHOM6.Pipelines;

/// <summary>
/// DanhSachNhapDiem – danh sách SV của một lớp học phần kèm điểm thành phần và điểm đã quy đổi.
/// Dùng cho màn hình nhập điểm G-03, mẫu/xuất Excel bảng điểm lớp học phần.
/// maGV = null khi Admin xem (không giới hạn giảng viên).
///
///   1. $match   – SV có lần học thỏa (maMon, namHoc, hocKy[, giangVien.maGV]) – $elemMatch.
///   2. $unwind  – mỗi lần học một document.                                [barem: $unwind]
///   3. $match   – chỉ giữ đúng lần học của lớp học phần.
///   4–7. @quyDoi – TK, chữ, hệ 4, đạt.                                     [barem: $cond, $switch]
///   8. $project – các cột của bảng nhập điểm; maDK dùng cho arrayFilters khi lưu. [barem: $project]
///   9. $sort    – theo lớp sinh hoạt, MSSV.
/// </summary>
public static class DanhSachNhapDiemPipeline
{
    public const string Json = """
        [
          { "$match": { "monHoc": { "$elemMatch": "@dieuKienTrongPhanTu" } } },
          { "$unwind": "$monHoc" },
          { "$match": "@dieuKienSauUnwind" },
          "@quyDoi",
          { "$project": {
              "_id": 0,
              "maSV": 1,
              "hoTen": 1,
              "maLop": 1,
              "maDK": "$monHoc.maDK",
              "hinhThuc": "$monHoc.hinhThuc",
              "lanHoc": "$monHoc.lanHoc",
              "giangVien": "$monHoc.giangVien",
              "diem": "$monHoc.diem",
              "capNhatLuc": "$monHoc.capNhatLuc",
              "capNhatBoi": "$monHoc.capNhatBoi",
              "duDiem": "$monHoc.duDiem",
              "diemTK": "$monHoc.diemTK",
              "diemChu": "$monHoc.diemChu",
              "diemHe4": "$monHoc.diemHe4",
              "dat": "$monHoc.dat" } },
          { "$sort": { "maLop": 1, "maSV": 1 } }
        ]
        """;

    public static BsonDocument[] Tao(string? maGV, string maMon, string namHoc, int hocKy)
    {
        var dieuKien = DieuKienLanHoc.Tao(maGV, maMon, namHoc, hocKy);
        return PipelineJson.Tao(Json, new Dictionary<string, BsonValue>
        {
            ["dieuKienTrongPhanTu"] = dieuKien.TrongPhanTu,
            ["dieuKienSauUnwind"] = dieuKien.SauUnwind
        });
    }
}
