// ===========================================================================
// G-03 Nhập điểm (bảng do server render sẵn; JS chỉ xử lý ô nhập và gọi server):
//   - ô nhập: chấp nhận "8,5" / "8.5" → chuẩn hóa dấu chấm; 0–10; tối đa 1 chữ số thập phân; trống = null
//   - dòng đã sửa chưa lưu được tô màu; cảnh báo khi rời trang còn dòng chưa lưu
//   - Lưu từng dòng : POST /GiangVien/NhapDiem/LuuDong   (updateOne + $set + arrayFilters)
//   - Lưu tất cả    : POST /GiangVien/NhapDiem/LuuTatCa  (bulkWrite)
//   - lọc theo lớp, "chỉ hiện chưa đủ điểm"; thanh tiến độ tính theo lớp đang lọc
// TK / Chữ / Hệ 4 KHÔNG tính ở đây: server chạy lại pipeline rồi trả về, JS chỉ đặt vào ô.
// Server luôn kiểm tra lại toàn bộ điểm (DiemNhap.TryParse).
// ===========================================================================
(function () {
    'use strict';

    var DANG_DIEM = /^\d{1,2}(\.\d)?$/;

    /** Chuẩn hóa chuỗi người dùng gõ: bỏ khoảng trắng, "," → "." */
    function chuanHoa(s) { return (s || '').trim().replace(',', '.'); }

    /** Ô hợp lệ: trống, hoặc số 0–10 tối đa 1 chữ số thập phân. */
    function hopLe(s) {
        s = chuanHoa(s);
        return s === '' || (DANG_DIEM.test(s) && parseFloat(s) <= 10);
    }

    /** So sánh theo giá trị số ("8" = "8.0"), trống = null. */
    function giaTri(s) {
        s = chuanHoa(s);
        return s === '' ? null : parseFloat(s);
    }

    document.addEventListener('DOMContentLoaded', function () {
        var bang = document.getElementById('bang-nhap-diem');
        if (!bang) { return; }

        var dsDong = Array.prototype.slice.call(bang.querySelectorAll('tbody tr[data-ma-dk]'));
        var nutLuuTatCa = document.getElementById('nut-luu-tat-ca');
        var soDongSua = document.getElementById('so-dong-sua');
        var locLop = document.getElementById('loc-lop');
        var locChuaDu = document.getElementById('loc-chua-du');

        function cacO(dong) { return dong.querySelectorAll('input[data-thanh-phan]'); }

        /** Cập nhật trạng thái một dòng: ô lỗi, ô đã đổi, dòng đã sửa, nút Lưu. */
        function capNhatDong(dong) {
            var daSua = false, coLoi = false;
            cacO(dong).forEach(function (o) {
                var loi = !hopLe(o.value);
                var doi = !loi && giaTri(o.value) !== giaTri(o.dataset.goc);
                o.classList.toggle('is-invalid', loi);
                o.classList.toggle('da-doi', doi);
                o.title = loi ? 'Điểm 0–10, tối đa 1 chữ số thập phân' : '';
                coLoi = coLoi || loi;
                daSua = daSua || doi || (loi && chuanHoa(o.value) !== chuanHoa(o.dataset.goc));
            });
            dong.classList.toggle('dong-da-sua', daSua);
            dong.dataset.coLoi = coLoi ? 'true' : 'false';
            dong.querySelector('[data-luu-dong]').disabled = !daSua || coLoi;
        }

        function dongDaSua() { return dsDong.filter(function (d) { return d.classList.contains('dong-da-sua'); }); }

        function capNhatNutLuuTatCa() {
            var n = dongDaSua().length;
            soDongSua.textContent = n;
            nutLuuTatCa.disabled = n === 0;
        }

        /** Tiến độ = số dòng đủ 3 điểm (cờ duDiem do pipeline trả về) / số SV của lớp đang lọc
         *  ("Tất cả lớp" = cả lớp học phần) – chỉ là đếm để vẽ thanh; ô "chưa đủ điểm" không ảnh hưởng. */
        function capNhatTienDo() {
            var lop = locLop.value;
            var dsTinh = dsDong.filter(function (d) { return lop === '' || d.dataset.lop === lop; });
            var siSo = dsTinh.length;
            var du = dsTinh.filter(function (d) { return d.dataset.duDiem === 'true'; }).length;
            var pt = siSo === 0 ? 0 : Math.floor(du * 100 / siSo);
            document.getElementById('so-du-diem').textContent = du;
            document.getElementById('si-so').textContent = siSo;
            var thanh = document.getElementById('thanh-tien-do');
            thanh.querySelector('.progress-bar').style.width = pt + '%';
            thanh.classList.toggle('xong', pt === 100);
        }

        function apDungLoc() {
            var lop = locLop.value;
            var chiChuaDu = locChuaDu.checked;
            dsDong.forEach(function (d) {
                var an = (lop !== '' && d.dataset.lop !== lop) || (chiChuaDu && d.dataset.duDiem === 'true');
                d.classList.toggle('d-none', an);
            });
        }

        /** Đặt dữ liệu mới (từ pipeline) vào dòng: ô nhập, giá trị gốc, TK, Chữ, Hệ 4, cờ đủ điểm. */
        function datDuLieuDong(dong, d) {
            [['cc', d.cc], ['gk', d.gk], ['ck', d.ck]].forEach(function (cap) {
                var o = dong.querySelector('input[data-thanh-phan="' + cap[0] + '"]');
                var s = cap[1] === null || cap[1] === undefined ? '' : String(cap[1]);
                o.value = s;
                o.dataset.goc = s;
            });
            dong.querySelector('[data-cot="tk"]').textContent = d.diemTK;
            var badge = dong.querySelector('[data-cot="chu"] .badge');
            badge.className = 'badge badge-diem ' + d.lopBadgeChu;
            badge.textContent = d.diemChu || '–';
            dong.querySelector('[data-cot="he4"]').textContent = d.diemHe4;
            dong.dataset.duDiem = d.duDiem ? 'true' : 'false';
            capNhatDong(dong);
        }

        function duLieuGui(dong) {
            var o = function (tp) { return chuanHoa(dong.querySelector('input[data-thanh-phan="' + tp + '"]').value); };
            return { maSV: dong.dataset.maSv, maDK: dong.dataset.maDk, cc: o('cc'), gk: o('gk'), ck: o('ck') };
        }

        function guiJson(url, duLieu) {
            return fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': EduScore.layAntiforgeryToken()
                },
                body: JSON.stringify(duLieu)
            }).then(function (r) {
                return r.json().catch(function () { return { thanhCong: false, thongBao: 'Lỗi máy chủ (' + r.status + ').' }; });
            }).catch(function () {
                return { thanhCong: false, thongBao: 'Không kết nối được máy chủ.' };
            });
        }

        // ---------- Sự kiện ô nhập ----------
        dsDong.forEach(function (dong) {
            cacO(dong).forEach(function (o) {
                o.addEventListener('input', function () { capNhatDong(dong); capNhatNutLuuTatCa(); });
                // Rời ô: đổi "8,5" thành "8.5" ngay trên ô
                o.addEventListener('blur', function () {
                    if (hopLe(o.value)) { o.value = chuanHoa(o.value); }
                });
                // Enter = lưu dòng
                o.addEventListener('keydown', function (e) {
                    if (e.key === 'Enter') {
                        e.preventDefault();
                        var nut = dong.querySelector('[data-luu-dong]');
                        if (!nut.disabled) { nut.click(); }
                    }
                });
            });

            // ---------- Lưu từng dòng ----------
            var nutLuu = dong.querySelector('[data-luu-dong]');
            nutLuu.addEventListener('click', function () {
                nutLuu.disabled = true;
                guiJson('/GiangVien/NhapDiem/LuuDong', duLieuGui(dong)).then(function (kq) {
                    if (kq.thanhCong && kq.dong && kq.dong.length) {
                        datDuLieuDong(dong, kq.dong[0]);
                        capNhatTienDo();
                        apDungLoc();
                    } else {
                        capNhatDong(dong);
                    }
                    capNhatNutLuuTatCa();
                    toast(kq.thongBao, kq.thanhCong ? 'success' : 'danger', kq.mongoCommand);
                });
            });
        });

        // ---------- Lưu tất cả ----------
        nutLuuTatCa.addEventListener('click', function () {
            var cacDong = dongDaSua();
            if (cacDong.some(function (d) { return d.dataset.coLoi === 'true'; })) {
                toast('Có ô điểm không hợp lệ (tô đỏ). Vui lòng sửa trước khi lưu.', 'warning');
                return;
            }
            nutLuuTatCa.disabled = true;
            guiJson('/GiangVien/NhapDiem/LuuTatCa', {
                maMon: bang.dataset.maMon,
                namHoc: bang.dataset.namHoc,
                hocKy: parseInt(bang.dataset.hocKy, 10),
                dong: cacDong.map(duLieuGui)
            }).then(function (kq) {
                if (kq.dong && kq.dong.length) {
                    // Tải lại dữ liệu cả bảng từ pipeline DanhSachNhapDiem
                    var theoMaDK = {};
                    kq.dong.forEach(function (d) { theoMaDK[d.maDK] = d; });
                    dsDong.forEach(function (dong) {
                        var d = theoMaDK[dong.dataset.maDk];
                        if (d) { datDuLieuDong(dong, d); }
                    });
                    capNhatTienDo();
                    apDungLoc();
                }
                capNhatNutLuuTatCa();
                toast(kq.thongBao, kq.thanhCong ? 'success' : 'danger', kq.mongoCommand);
            });
        });

        // ---------- Lọc ----------
        locLop.addEventListener('change', function () { apDungLoc(); capNhatTienDo(); });
        locChuaDu.addEventListener('change', apDungLoc);

        // ---------- Cảnh báo rời trang khi còn dòng chưa lưu ----------
        window.addEventListener('beforeunload', function (e) {
            if (dongDaSua().length > 0) {
                e.preventDefault();
                e.returnValue = '';
            }
        });

        apDungLoc();
        capNhatTienDo();
    });
})();
