// Nhập database/export/*.json bằng mongosh (dùng khi máy KHÔNG có MongoDB Database Tools).
// Được gọi bởi khoi-phuc.ps1:  mongosh <uri> --quiet --file nhap-json.mongosh.js   (thư mục hiện tại = database/)
// Tên database lấy từ biến môi trường EDUSCORE_DB (mặc định "eduscore").
const tenDb = process.env.EDUSCORE_DB || "eduscore";
const csdl = db.getSiblingDB(tenDb);

for (const ten of ["lophoc", "giangvien", "monhoc", "taikhoan", "sinhvien"]) {
  // relaxed: false → { "$oid" } thành ObjectId, { "$date" } thành Date, giữ nguyên _id
  const docs = EJSON.parse(fs.readFileSync(`export/${ten}.json`, "utf8"), { relaxed: false });
  csdl.getCollection(ten).drop();
  if (docs.length > 0) csdl.getCollection(ten).insertMany(docs);
  print(`  ${ten}: ${csdl.getCollection(ten).countDocuments()} document`);
}

// JSON không phân biệt 8.0 với 8 → mongosh lưu điểm nguyên thành int.
// Đổi lại điểm thành phần sang double cho giống dữ liệu gốc ($toDouble(null) = null).
csdl.sinhvien.updateMany({}, [{ $set: { monHoc: { $map: { input: "$monHoc", as: "m", in: {
  $mergeObjects: ["$$m", { diem: {
    cc: { $toDouble: "$$m.diem.cc" },
    gk: { $toDouble: "$$m.diem.gk" },
    ck: { $toDouble: "$$m.diem.ck" } } }] } } } } }]);
print(`Đã nhập 5 collection vào database "${tenDb}".`);
