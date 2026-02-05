/**
 * ==============================================================================
 * BỘ CÔNG CỤ DATEPICKER - ONES SYSTEM (Bootstrap Datepicker Wrapper)
 * ==============================================================================
 * * MÔ TẢ: 
 * Tự động render UI, khởi tạo Datepicker và load ngày kinh doanh từ hệ thống.
 * * * CÁCH SỬ DỤNG:
 * 1. HTML: <div class="date-input" data-id="fromDate" data-label="Từ ngày"></div>
 * 2. JS:   initDateInput(function() { 
 * // Hàm này chạy sau khi đã có ngày hệ thống
 * searchData(); 
 * });
 * * * DANH SÁCH CÁC HÀM CHÍNH:
 * - initDateInput(done): Render UI + Load Business Date.
 * - getDateIso(selector): Lấy ngày yyyy-mm-dd để gửi API.
 * - setBusinessDateToInputs(isoDate): Đổ ngày ISO vào input.
 * ==============================================================================
 */

/**
 * Khởi tạo toàn diện: Render HTML -> Init Picker -> Load Business Date
 * @param {function} done - Callback chạy sau khi đã có ngày từ Server
 */
function initDateInput(done = null) {
    $('.date-input').each(function () {
        const $el = $(this);
        if ($el.children().length > 0) return; 

        const id = $el.data('id') || 'date-' + Math.floor(Math.random() * 1000);
        const label = $el.data('label') || '';
        const placeholder = $el.data('placeholder') || 'dd/mm/yyyy';

        const html = `
            <div class="d-flex align-items-center">
                ${label ? `<label class="fw-bold me-2 mb-0">${label}</label>` : ''}
                <div class="input-group input-group-sm date-group" style="width: 150px;">
                    <input id="${id}" class="form-control date-bootstrap" 
                           placeholder="${placeholder}" readonly />
                    <span class="input-group-text calendar-btn" style="cursor:pointer">
                        <i class="fa-solid fa-calendar-days"></i>
                    </span>
                </div>
            </div>`;
        $el.html(html);
    });

    initBootstrapDatepicker('.date-bootstrap');

    loadBusinessDate(done);
}

function initBootstrapDatepicker(selector, options = {}) {
    const $el = $(selector);
    const $modal = $el.closest('.modal');
    const defaultOptions = {
        format: 'dd/mm/yyyy',
        autoclose: true,
        todayHighlight: true,
        clearBtn: true,
        orientation: "bottom auto",
        container: $modal.length ? $modal : 'body'
    };

    $(selector).datepicker({ ...defaultOptions, ...options });
}

function parseIsoDateNoTimezone(iso) {
    if (!iso) return null;
    const [y, m, d] = iso.substring(0, 10).split('-');
    return new Date(y, m - 1, d);
}

function setBusinessDateToInputs(businessDateIso, selector = '.date-bootstrap') {
    const date = parseIsoDateNoTimezone(businessDateIso);
    if (!date) return;

    $(selector).each(function () {
        $(this).datepicker('setDate', date);
    });
}

function getDateIso(selector) {
    const d = $(selector).datepicker('getDate');
    return d ? d.toISOString().slice(0, 10) : null;
}

function loadBusinessDate(done) {
    $.ajax({
        url: "/Reservation/GetBusinessDate",
        type: "GET",
        dataType: "json",
        success: function (res) {
            // res có thể là string hoặc object
            const businessDate = res.businessDate ?? res;
            setBusinessDateToInputs(businessDate);

            if (typeof done === 'function') done(businessDate);
        },
        error: function () {
            console.error("Cannot load BusinessDate");
        }
    });
}

$(document).on('click', '.calendar-btn', function () {
    $(this).siblings('input.date-bootstrap').datepicker('show');
});