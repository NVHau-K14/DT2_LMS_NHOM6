// ===========================================================================
// EduScore – vẽ biểu đồ Chart.js (wwwroot/lib/chart.js, không CDN)
//   <canvas data-bieu-do='{"loai":"bar","nhan":[...],"duLieu":[...],"mau":[...],
//                          "tenChuoi":"...","trucYToiDa":4,"soLe":2}'>
// Cấu hình do server dựng (CauHinhBieuDo) từ kết quả pipeline – JS chỉ VẼ, không tính số liệu.
// ===========================================================================
(function () {
    'use strict';

    if (typeof Chart === 'undefined') { return; }

    Chart.defaults.font.family = '"Segoe UI", system-ui, -apple-system, Roboto, Arial, sans-serif';
    Chart.defaults.color = '#6B7280';

    function dinhDang(giaTri, soLe) {
        return giaTri === null || giaTri === undefined ? '–' : Number(giaTri).toFixed(soLe || 0);
    }

    function veBieuDo(canvas) {
        var ch = JSON.parse(canvas.dataset.bieuDo);
        var laTron = ch.loai === 'doughnut';
        var laDuong = ch.loai === 'line';

        var chuoi = {
            label: ch.tenChuoi,
            data: ch.duLieu,
            backgroundColor: laDuong ? 'rgba(37, 99, 235, .12)' : ch.mau,
            borderColor: laDuong ? ch.mau[0] : (laTron ? '#fff' : ch.mau),
            borderWidth: laDuong ? 3 : (laTron ? 2 : 0),
            borderRadius: laTron || laDuong ? 0 : 6,
            maxBarThickness: 56
        };
        if (laDuong) {
            chuoi.fill = true;
            chuoi.tension = 0.3;
            chuoi.pointRadius = 5;
            chuoi.pointBackgroundColor = ch.mau[0];
        }

        var tuyChon = {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: laTron, position: 'right' },
                tooltip: {
                    callbacks: {
                        label: function (ctx) {
                            var v = laTron ? ctx.parsed : ctx.parsed.y;
                            return ' ' + ch.tenChuoi + ': ' + dinhDang(v, ch.soLe);
                        }
                    }
                }
            }
        };
        if (!laTron) {
            tuyChon.scales = {
                y: {
                    beginAtZero: true,
                    max: ch.trucYToiDa === null ? undefined : ch.trucYToiDa,
                    ticks: { precision: ch.soLe ? undefined : 0 },
                    grid: { color: '#EEF1F6' }
                },
                x: { grid: { display: false } }
            };
        }

        new Chart(canvas, { type: ch.loai, data: { labels: ch.nhan, datasets: [chuoi] }, options: tuyChon });
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('canvas[data-bieu-do]').forEach(veBieuDo);
    });
})();
