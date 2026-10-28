// ===========================================================================
// EduScore – JavaScript dùng chung (vanilla, không thư viện ngoài ngoài Bootstrap)
//   - toast(message, type, mongoCommand?)  : thông báo, kèm "Lệnh MongoDB đã chạy"
//   - hộp xác nhận xóa dùng chung          : nút có data-xac-nhan-xoa
//   - thu gọn / mở sidebar
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

    /** Lấy antiforgery token của trang (dùng cho fetch POST ở các giai đoạn sau). */
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

    document.addEventListener('DOMContentLoaded', function () {
        khoiTaoSidebar();
        khoiTaoXacNhanXoa();
        hienToastTempData();
    });

    // Xuất ra phạm vi toàn cục để các trang gọi
    window.toast = toast;
    window.EduScore = { toast: toast, saoChep: saoChep, layAntiforgeryToken: layAntiforgeryToken };
})();
