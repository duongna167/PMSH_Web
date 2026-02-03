/**
 * ==== Khởi tạo Bootstrap Datepicker cho các input có class 'date-bootstrap'
 * init: initBootstrapDatepicker,  example:  initBootstrapDatepicker('.date-bootstrap');
 * 
 * ===== Thêm giá trị BusinessDate (định dạng ISO) vào các input có class 'date-bootstrap'
 * setBusinessDate: setBusinessDateToInputs,
 * 
 * ===== lấy giá trị ngày từ input bootstrap datepicker theo định dạng ISO để gửi về server
 * getDateIso,
 * 
 * =====
 * loadBusinessDate
 * 
 */

function initBootstrapDatepicker(selector, options = {}) {
    const defaultOptions = {
        format: 'dd/mm/yyyy',
        autoclose: true,
        todayHighlight: true,
        clearBtn: true,
        orientation: "bottom auto"
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