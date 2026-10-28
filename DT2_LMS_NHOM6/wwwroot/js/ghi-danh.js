// ===========================================================================
// A-09 Ghi danh – chỉ xử lý giao diện chọn sinh viên (danh sách SV do server render sẵn):
//   - đổi chế độ "Cả lớp" / "Từng sinh viên" (ẩn / hiện khối tương ứng)
//   - ô tìm: lọc các dòng theo MSSV, họ tên, lớp (không gọi server)
//   - danh sách "Đã chọn": nhân bản <template id="mau-sv-da-chon">, bấm × để bỏ chọn
// Xem trước / xác nhận đều gửi form lên server – JS không tự nhận diện hình thức học.
// ===========================================================================
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        var form = document.getElementById('form-ghi-danh');
        if (!form) { return; }

        var khoiCaLop = document.getElementById('khoi-ca-lop');
        var khoiTungSV = document.getElementById('khoi-tung-sv');
        var oTim = document.getElementById('tim-sv');
        var dsDong = Array.prototype.slice.call(document.querySelectorAll('#ds-chon-sv .chon-sv-dong'));
        var khuDaChon = document.getElementById('ds-da-chon');
        var mau = document.getElementById('mau-sv-da-chon');
        var soDaChon = document.getElementById('so-sv-da-chon');

        // ---------- Chế độ ghi danh ----------
        function capNhatCheDo() {
            var laCaLop = form.querySelector('input[name="CheDo"]:checked').value === 'lop';
            khoiCaLop.classList.toggle('d-none', !laCaLop);
            khoiTungSV.classList.toggle('d-none', laCaLop);
        }
        form.querySelectorAll('[data-che-do]').forEach(function (o) { o.addEventListener('change', capNhatCheDo); });

        // ---------- Tìm sinh viên ----------
        oTim.addEventListener('input', function () {
            var tuKhoa = oTim.value.trim().toLowerCase();
            dsDong.forEach(function (dong) {
                dong.classList.toggle('d-none', tuKhoa !== '' && dong.dataset.tim.indexOf(tuKhoa) === -1);
            });
        });
        // Enter trong ô tìm không gửi form
        oTim.addEventListener('keydown', function (e) { if (e.key === 'Enter') { e.preventDefault(); } });

        // ---------- Danh sách đã chọn ----------
        function veDaChon() {
            var dsChon = dsDong
                .map(function (dong) { return dong.querySelector('input[type="checkbox"]'); })
                .filter(function (o) { return o.checked; });

            khuDaChon.replaceChildren();
            dsChon.forEach(function (o) {
                var chip = mau.content.firstElementChild.cloneNode(true);
                chip.querySelector('.chip-sv-ten').textContent = o.value + ' – ' + o.dataset.hoTen;
                chip.querySelector('button').addEventListener('click', function () {
                    o.checked = false;
                    veDaChon();
                });
                khuDaChon.appendChild(chip);
            });
            soDaChon.textContent = dsChon.length;
        }
        dsDong.forEach(function (dong) {
            dong.querySelector('input[type="checkbox"]').addEventListener('change', veDaChon);
        });

        // Chế độ "Cả lớp": bỏ các ô SV khỏi query string cho gọn (server cũng bỏ qua chúng)
        form.addEventListener('submit', function () {
            var laCaLop = form.querySelector('input[name="CheDo"]:checked').value === 'lop';
            dsDong.forEach(function (dong) { dong.querySelector('input[type="checkbox"]').disabled = laCaLop; });
            document.getElementById('MaLop').disabled = !laCaLop;
        });
        // Quay lại trang bằng nút Back (trình duyệt giữ trạng thái) → mở lại các ô đã tắt
        window.addEventListener('pageshow', function () {
            form.querySelectorAll('input:disabled, select:disabled').forEach(function (o) { o.disabled = false; });
        });

        veDaChon();
    });
})();
