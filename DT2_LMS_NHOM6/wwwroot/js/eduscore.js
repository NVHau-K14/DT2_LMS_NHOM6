// ===========================================================================
// EduScore – JavaScript dùng chung (vanilla, không thư viện ngoài ngoài Bootstrap)
//   - toast(message, type, mongoCommand?)  : thông báo, kèm "Lệnh MongoDB đã chạy"
//   - hộp xác nhận xóa dùng chung          : nút có data-xac-nhan-xoa
//   - modal thêm / sửa dùng chung          : nút có data-mo-modal + data-gia-tri (JSON)
//   - thu gọn / mở sidebar
//   - sắp xếp bảng phía trình duyệt        : table[data-sap-xep-bang] + th[data-sap-xep]
//   - nút sao chép                         : nút có data-sao-chep="#id"
//   - ô chọn file nhãn tiếng Việt          : input[data-chon-file] + label[for] (partial _ChonFile)
// Không dựng HTML bằng chuỗi: toast nhân bản <template id="mau-toast"> trong _Toast.cshtml.
// ===========================================================================
(function () {
    'use strict';

    var ICON_THEO_LOAI = {
        success: 'bi-check-circle-fill',
        danger: 'bi-x-circle-fill',
        warning: 'bi-exclamation-triangle-fill',
        info: 'bi-info-circle-fill'
    };

    /**
     * Hiện toast.
     * @param {string} message       Nội dung thông báo
     * @param {string} [type]        success | danger | warning | info (mặc định success)
     * @param {string} [mongoCommand] Chuỗi lệnh mongosh đã chạy (thiết kế mục 4.9)
     */
    function toast(message, type, mongoCommand) {
        type = ICON_THEO_LOAI[type] ? type : 'success';
        var mau = document.getElementById('mau-toast');
        var khu = document.getElementById('khu-toast');
        if (!mau || !khu) { return; }

        var el = mau.content.firstElementChild.cloneNode(true);
        el.classList.add('toast-' + type);
        el.querySelector('.toast-icon').classList.add(ICON_THEO_LOAI[type]);
        el.querySelector('.toast-noi-dung').textContent = message;

        var coLenh = !!mongoCommand;
        if (coLenh) {
            var khoiMongo = el.querySelector('.toast-mongo');
            var khoiThuGon = el.querySelector('.toast-mongo-khoi');
            khoiMongo.classList.remove('d-none');
            el.querySelector('.toast-mongo-lenh').textContent = mongoCommand;

            el.querySelector('.toast-mongo-nut').addEventListener('click', function () {
                bootstrap.Collapse.getOrCreateInstance(khoiThuGon).toggle();
            });

            var nutSaoChep = el.querySelector('.toast-sao-chep');
            nutSaoChep.addEventListener('click', function () {
                saoChep(mongoCommand).then(function () {
                    nutSaoChep.textContent = 'Đã sao chép';
                });
            });
        }

        khu.appendChild(el);
        // Toast có lệnh MongoDB giữ lâu hơn để kịp đọc / sao chép
        var t = bootstrap.Toast.getOrCreateInstance(el, { delay: coLenh ? 15000 : 5000 });
        el.addEventListener('hidden.bs.toast', function () { el.remove(); });
        t.show();
    }

    /** Sao chép văn bản vào clipboard (có phương án dự phòng khi không có navigator.clipboard). */
    function saoChep(vanBan) {
        if (navigator.clipboard && window.isSecureContext) {
            return navigator.clipboard.writeText(vanBan);
        }
        var o = document.createElement('textarea');
        o.value = vanBan;
        o.style.position = 'fixed';
        o.style.opacity = '0';
        document.body.appendChild(o);
        o.select();
        try { document.execCommand('copy'); } finally { o.remove(); }
        return Promise.resolve();
    }

    /** Lấy antiforgery token của trang (dùng cho fetch POST). */
    function layAntiforgeryToken() {
        var o = document.querySelector('input[name="__RequestVerificationToken"]');
        return o ? o.value : '';
    }

    // ---------- Hộp xác nhận xóa dùng chung ----------
    function khoiTaoXacNhanXoa() {
        var modalEl = document.getElementById('modal-xac-nhan-xoa');
        if (!modalEl) { return; }
        var form = document.getElementById('form-xac-nhan-xoa');
        var noiDung = document.getElementById('xac-nhan-xoa-noi-dung');

        document.addEventListener('click', function (e) {
            var nut = e.target.closest('[data-xac-nhan-xoa]');
            if (!nut) { return; }
            e.preventDefault();
            form.action = nut.dataset.url;
            noiDung.textContent = nut.dataset.noiDung || 'Bạn có chắc chắn muốn xóa mục này?';
            document.getElementById('modal-xac-nhan-xoa-tieu-de').textContent = nut.dataset.tieuDe || 'Xác nhận xóa';
            document.getElementById('xac-nhan-xoa-nut').textContent = nut.dataset.nut || 'Xóa';
            document.getElementById('xac-nhan-xoa-ghi-chu').textContent = nut.dataset.ghiChu || 'Thao tác này không thể hoàn tác.';
            bootstrap.Modal.getOrCreateInstance(modalEl).show();
        });
    }

    // ---------- Modal thêm / sửa dùng chung ----------
    // Nút mở: <button data-mo-modal="#modal-lop" data-tieu-de="Sửa lớp" data-gia-tri='{"laSua":true,"maLop":"..."}'>
    //   - data-gia-tri: JSON giá trị form (tên khóa trùng tên thuộc tính, không phân biệt hoa thường);
    //     ô nhập có name "Form.MaLop" nhận giá trị khóa "maLop".
    //   - Ô có data-khoa-khi-sua bị readonly khi laSua = true (mã lớp, mã GV, mã môn).
    //   - Modal có data-mo-san="true" (server trả lại lỗi validation) được mở ngay khi tải trang.
    // Chỉ điền giá trị vào ô nhập có sẵn – không dựng HTML bằng chuỗi.
    function datCheDoSua(modalEl, laSua) {
        modalEl.querySelectorAll('[data-khoa-khi-sua]').forEach(function (o) { o.readOnly = laSua; });
    }

    function xoaLoiValidation(modalEl) {
        modalEl.querySelectorAll('.field-validation-error').forEach(function (o) {
            o.textContent = '';
            o.classList.replace('field-validation-error', 'field-validation-valid');
        });
        modalEl.querySelectorAll('.input-validation-error, .is-invalid').forEach(function (o) {
            o.classList.remove('input-validation-error', 'is-invalid');
        });
        modalEl.querySelectorAll('.validation-summary-errors').forEach(function (o) {
            o.classList.replace('validation-summary-errors', 'validation-summary-valid');
            var ds = o.querySelector('ul');
            if (ds) { ds.replaceChildren(); }
        });
    }

    function khoiTaoModalForm() {
        document.addEventListener('click', function (e) {
            var nut = e.target.closest('[data-mo-modal]');
            if (!nut) { return; }
            e.preventDefault();
            var modalEl = document.querySelector(nut.dataset.moModal);
            var form = modalEl.querySelector('form');
            var duLieu = JSON.parse(nut.dataset.giaTri || '{}');

            // Bảng tra khóa không phân biệt hoa thường: "malop" → giá trị
            var giaTri = {};
            Object.keys(duLieu).forEach(function (k) { giaTri[k.toLowerCase()] = duLieu[k]; });

            Array.prototype.forEach.call(form.elements, function (o) {
                if (!o.name || o.name.indexOf('Form.') !== 0) { return; }
                var khoa = o.name.substring(5).toLowerCase();
                if (!(khoa in giaTri)) { return; }
                var v = giaTri[khoa];
                o.value = (v === null || v === undefined) ? '' : String(v);
            });

            var laSua = giaTri.lasua === true;
            datCheDoSua(modalEl, laSua);
            xoaLoiValidation(modalEl);
            var tieuDe = modalEl.querySelector('[data-tieu-de-modal]');
            if (tieuDe && nut.dataset.tieuDe) { tieuDe.textContent = nut.dataset.tieuDe; }

            // Cho trang riêng xử lý thêm (ví dụ cảnh báo đổi tên môn)
            modalEl.dispatchEvent(new CustomEvent('eduscore:mo-modal', { detail: { duLieu: duLieu, nut: nut } }));
            bootstrap.Modal.getOrCreateInstance(modalEl).show();
        });

        document.querySelectorAll('.modal[data-mo-san="true"]').forEach(function (modalEl) {
            var laSua = modalEl.querySelector('input[name="Form.LaSua"]');
            datCheDoSua(modalEl, !!laSua && laSua.value.toLowerCase() === 'true');
            bootstrap.Modal.getOrCreateInstance(modalEl).show();
        });
    }

    // ---------- Sidebar ----------
    var KHOA_SIDEBAR = 'eduscore.sidebarThuGon';
    var MAN_HINH_RONG = window.matchMedia('(min-width: 992px)');

    function khoiTaoSidebar() {
        document.querySelectorAll('[data-sidebar-bat]').forEach(function (nut) {
            nut.addEventListener('click', function () {
                if (MAN_HINH_RONG.matches) {
                    // Màn hình rộng: thu gọn còn icon, ghi nhớ lựa chọn
                    var thuGon = document.body.classList.toggle('sidebar-thu-gon');
                    try { localStorage.setItem(KHOA_SIDEBAR, thuGon ? '1' : '0'); } catch (e) { }
                } else {
                    // Màn hình hẹp: trượt sidebar ra / vào
                    document.body.classList.toggle('sidebar-mo');
                }
            });
        });
        document.querySelectorAll('[data-sidebar-dong]').forEach(function (nen) {
            nen.addEventListener('click', function () { document.body.classList.remove('sidebar-mo'); });
        });
        MAN_HINH_RONG.addEventListener('change', function () { document.body.classList.remove('sidebar-mo'); });
    }

    // ---------- Toast từ TempData (sau redirect) ----------
    function hienToastTempData() {
        var o = document.getElementById('toast-tempdata');
        if (o) { toast(o.dataset.noiDung, o.dataset.loai, o.dataset.mongo); }
    }

    // ---------- Ô chọn tự gửi form khi đổi giá trị (lọc học kỳ...) ----------
    // <select data-tu-gui> nằm trong form GET
    function khoiTaoTuGui() {
        document.querySelectorAll('select[data-tu-gui]').forEach(function (o) {
            o.addEventListener('change', function () { o.form.submit(); });
        });
    }

    // ---------- Sắp xếp bảng phía trình duyệt (báo cáo học lực) ----------
    // <table data-sap-xep-bang>, tiêu đề cột <th data-sap-xep="so|chu">; ô có data-gia-tri thì
    // sắp theo giá trị đó (GPA, thứ tự xếp loại), không thì theo chữ hiển thị. Ô data-stt được đánh số lại.
    // Chỉ đổi thứ tự các <tr> có sẵn – không dựng HTML.
    function khoiTaoSapXepBang() {
        document.querySelectorAll('table[data-sap-xep-bang]').forEach(function (bang) {
            var tbody = bang.tBodies[0];
            bang.querySelectorAll('th[data-sap-xep]').forEach(function (th) {
                th.classList.add('cot-sap-xep');
                th.tabIndex = 0;
                function sapXep() {
                    var cot = th.cellIndex;
                    var laSo = th.dataset.sapXep === 'so';
                    var tang = !th.classList.contains('sap-tang');
                    bang.querySelectorAll('th[data-sap-xep]').forEach(function (o) { o.classList.remove('sap-tang', 'sap-giam'); });
                    th.classList.add(tang ? 'sap-tang' : 'sap-giam');

                    function khoa(tr) {
                        var o = tr.cells[cot];
                        var v = o.dataset.giaTri !== undefined ? o.dataset.giaTri : o.textContent.trim();
                        return laSo ? parseFloat(v) || 0 : v;
                    }
                    var dong = Array.prototype.slice.call(tbody.rows);
                    dong.sort(function (a, b) {
                        var x = khoa(a), y = khoa(b);
                        var kq = laSo ? x - y : String(x).localeCompare(String(y), 'vi');
                        return tang ? kq : -kq;
                    });
                    dong.forEach(function (tr, i) {
                        tbody.appendChild(tr);
                        var stt = tr.querySelector('[data-stt]');
                        if (stt) { stt.textContent = i + 1; }
                    });
                }
                th.addEventListener('click', sapXep);
                th.addEventListener('keydown', function (e) { if (e.key === 'Enter') { sapXep(); } });
            });
        });
    }

    // ---------- Nút sao chép: <button data-sao-chep="#id-khoi-van-ban"> ----------
    function khoiTaoSaoChep() {
        document.addEventListener('click', function (e) {
            var nut = e.target.closest('[data-sao-chep]');
            if (!nut) { return; }
            var nguon = document.querySelector(nut.dataset.saoChep);
            if (!nguon) { return; }
            var nhanCu = nut.innerHTML;
            saoChep(nguon.textContent).then(function () {
                nut.textContent = 'Đã sao chép';
                setTimeout(function () { nut.innerHTML = nhanCu; }, 2000);
            });
        });
    }

    // ---------- Ô chọn file: ghi tên file đã chọn vào nhãn [data-ten-file] của label[for=id] ----------
    function khoiTaoChonFile() {
        document.addEventListener('change', function (e) {
            var o = e.target;
            if (!o.matches || !o.matches('input[type="file"][data-chon-file]')) { return; }
            var nhan = document.querySelector('label[for="' + o.id + '"] [data-ten-file]');
            if (!nhan) { return; }
            var coFile = o.files && o.files.length > 0;
            nhan.textContent = coFile ? o.files[0].name : 'Chưa chọn file';
            nhan.classList.toggle('text-secondary', !coFile);
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        khoiTaoTuGui();
        khoiTaoChonFile();
        khoiTaoSapXepBang();
        khoiTaoSaoChep();
        khoiTaoSidebar();
        khoiTaoXacNhanXoa();
        khoiTaoModalForm();
        hienToastTempData();
    });

    // Xuất ra phạm vi toàn cục để các trang gọi
    window.toast = toast;
    window.EduScore = { toast: toast, saoChep: saoChep, layAntiforgeryToken: layAntiforgeryToken };
})();
