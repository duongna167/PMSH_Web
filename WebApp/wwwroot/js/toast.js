//function showToast(title, message, isSuccess) {
//    const toast = new bootstrap.Toast($('#nameToast')[0]);
//    $('#toastTitle').text(title);
//    $('#toastMessage').text(message);

//    // Xóa hết các class màu trước
//    $('#nameToast').removeClass('bg-success bg-danger bg-warning text-white');

//    // Thêm class dựa vào title và isSuccess
//    if (title === "Warning") {
//        $('#nameToast').addClass('bg-warning text-dark'); // vàng, chữ màu tối
//    } else {
//        $('#nameToast').addClass(isSuccess ? 'bg-success text-white' : 'bg-danger text-white');
//    }

//    toast.show();
//}

const ToastUI = (function () {

    function injectStyle() {
        if (document.getElementById("minimal-toast-style")) return;

        const style = document.createElement("style");
        style.id = "minimal-toast-style";
        style.innerHTML = `
            /* Container cố định ở góc trên bên phải */
            .toast-container {
                z-index: 9999;
                position: fixed;
                top: 20px;
                right: 20px;
            }

            .minimal-toast {
                background: #ffffff !important;
                border-radius: 8px;
                box-shadow: 0 10px 30px rgba(0, 0, 0, 0.15);
                padding: 0;
                overflow: hidden;
                min-width: 320px;
                position: relative;
                margin-bottom: 10px;
                border: 1px solid #eee !important;
                transition: transform 0.2s ease;
            }

            .minimal-toast .toast-header {
                background: transparent !important;
                border: none !important;
                padding: 12px 16px 4px 16px;
                font-weight: 700;
                font-size: 15px;
                display: flex;
                justify-content: space-between;
                align-items: center;
            }

            .minimal-toast .toast-body {
                padding: 0 16px 16px 16px;
                font-size: 13px;
                color: #4b5563;
                font-weight: 500;
            }

            /* Màu sắc trạng thái */
            .toast-success { border-left: 5px solid #10b981 !important; }
            .toast-success .toast-header { color: #10b981 !important; }
            
            .toast-danger  { border-left: 5px solid #ef4444 !important; }
            .toast-danger .toast-header { color: #ef4444 !important; }
            
            .toast-warning { border-left: 5px solid #f59e0b !important; }
            .toast-warning .toast-header { color: #f59e0b !important; }

            /* Thanh Progress bar */
            .toast-progress {
                position: absolute;
                bottom: 0;
                left: 0;
                height: 4px;
                width: 100%;
                background: rgba(0,0,0,0.03);
            }
            .toast-progress-value {
                height: 100%;
                width: 100%;
                transform-origin: left;
            }

            .toast-success .toast-progress-value { background: #10b981; }
            .toast-danger .toast-progress-value  { background: #ef4444; }
            .toast-warning .toast-progress-value { background: #f59e0b; }

            .btn-close-custom {
                cursor: pointer;
                opacity: 0.5;
                font-size: 18px;
                line-height: 1;
            }
            .btn-close-custom:hover { opacity: 1; }
        `;
        document.head.appendChild(style);
    }

    function show(title, message, isSuccess) {
        if ($('.toast-container').length === 0) {
            $('body').append('<div class="toast-container"></div>');
        }

        // --- BỔ SUNG: KIỂM TRA TRÙNG LẶP (ANTI-SPAM) ---
        let isDuplicate = false;
        $('.minimal-toast').each(function () {
            const $existingToast = $(this);
            // Kiểm tra nếu nội dung thông báo giống hệt cái đang hiển thị
            if ($existingToast.find('.toast-body').text() === message) {
                // Rung nhẹ cái cũ để gây chú ý
                $existingToast.css('transform', 'scale(1.02)');
                setTimeout(() => $existingToast.css('transform', 'scale(1)'), 150);

                isDuplicate = true;
                return false; // Thoát vòng lặp each
            }
        });

        if (isDuplicate) return; // Không tạo toast mới nếu trùng nội dung
        // ----------------------------------------------

        const toastId = 'toast_' + Date.now();
        const rawTitle = String(title ?? "").trim();
        const isWarningToast = /^warning$/i.test(rawTitle);
        const typeClass = isWarningToast ? "toast-warning" : (isSuccess ? "toast-success" : "toast-danger");
        const headerTitle = isWarningToast ? "Warning" : rawTitle;

        const toastHtml = `
            <div id="${toastId}" class="toast minimal-toast ${typeClass}" role="alert" aria-live="assertive" aria-atomic="true" data-bs-autohide="false">
                <div class="toast-header">
                    <span class="title-text">${headerTitle}</span>
                    <span class="btn-close-custom" data-bs-dismiss="toast">&times;</span>
                </div>
                <div class="toast-body">${message}</div>
                <div class="toast-progress">
                    <div class="toast-progress-value"></div>
                </div>
            </div>
        `;

        $('.toast-container').append(toastHtml);
        const $toastEl = $('#' + toastId);
        const bsToast = new bootstrap.Toast($toastEl[0], { autohide: false });

        const $progress = $toastEl.find('.toast-progress-value');
        $progress.css({
            'width': '100%',
            'transition': 'none'
        });

        bsToast.show();

        // Đồng bộ thời gian Progress Bar và đóng Toast
        const duration = 3000;

        setTimeout(() => {
            $progress.css({
                'width': '0%',
                'transition': `width ${duration}ms linear`
            });

            // Tự động ẩn sau đúng thời gian chạy của bar
            const autoHideTimeout = setTimeout(() => {
                if ($toastEl.length) {
                    bsToast.hide();
                }
            }, duration);

            // Lưu timeout ID vào element để dọn dẹp nếu người dùng đóng tay trước
            $toastEl.data('hideTimeout', autoHideTimeout);
        }, 50);

        // Xóa khỏi DOM sau khi ẩn hoàn toàn
        $toastEl[0].addEventListener('hidden.bs.toast', function () {
            clearTimeout($toastEl.data('hideTimeout'));
            $toastEl.remove();
        });
    }

    injectStyle();
    return { show };
})();

/**
 * Hàm gọi thông báo toàn cục
 */
function showToast(title, message, isSuccess) {
    ToastUI.show(title, message, isSuccess);
}

// ========== Custom Confirm Dialog ==========
// Override window.confirm với dialog đẹp hơn.
//
// Hoạt động theo cơ chế RE-TRIGGER — không cần sửa code cũ dùng if(!confirm()) return;
//   1. Lần gọi đầu: hiển thị dialog, trả về false → hàm hiện tại thoát sớm
//   2. User click OK → re-trigger click vào phần tử vừa được click (gốc)
//   3. Lần gọi lại: trả về true → action thực thi
// Hỗ trợ chuỗi confirm (A? rồi B?) tự động qua bộ đếm.
//
// Dùng: if (!confirm("msg")) return;   ← KHÔNG cần thay đổi gì
//        confirm("msg") cũng hoạt động nếu cần gọi độc lập (sẽ trả false lần 1)
(function () {
    function injectStyle() {
        if (document.getElementById('cc-style')) return;
        const style = document.createElement('style');
        style.id = 'cc-style';
        style.innerHTML = `
            .cc-overlay {
                position: fixed; inset: 0;
                background: rgba(0,0,0,0.35);
                z-index: 99999;
                display: flex; align-items: center; justify-content: center;
                animation: cc-fade-in 0.15s ease;
            }
            @keyframes cc-fade-in { from { opacity:0; } to { opacity:1; } }

            .cc-box {
                background: #fff;
                border-radius: 10px;
                box-shadow: 0 20px 50px rgba(0,0,0,0.2);
                width: 340px;
                overflow: hidden;
                border-top: 4px solid #f59e0b;
                animation: cc-slide-in 0.18s ease;
            }
            @keyframes cc-slide-in {
                from { opacity:0; transform: scale(0.93) translateY(8px); }
                to   { opacity:1; transform: scale(1) translateY(0); }
            }

            .cc-body { padding: 20px 20px 8px; }

            .cc-title {
                font-weight: 700; font-size: 14px; color: #f59e0b;
                display: flex; align-items: center; gap: 8px; margin-bottom: 10px;
            }
            .cc-title svg { flex-shrink: 0; }

            .cc-message { font-size: 13px; color: #4b5563; line-height: 1.6; }

            .cc-footer {
                display: flex; gap: 8px;
                padding: 14px 20px 18px; justify-content: flex-end;
            }
            .cc-btn {
                padding: 7px 20px; border-radius: 6px;
                border: none; font-size: 13px; font-weight: 600;
                cursor: pointer; transition: background 0.15s, transform 0.1s;
            }
            .cc-btn:active { transform: scale(0.97); }
            .cc-btn-cancel { background: #f3f4f6; color: #374151; }
            .cc-btn-cancel:hover { background: #e5e7eb; }
            .cc-btn-ok { background: #ef4444; color: #fff; }
            .cc-btn-ok:hover { background: #dc2626; }
        `;
        document.head.appendChild(style);
    }

    function showDialog(message, title) {
        injectStyle();
        return new Promise(function (resolve) {
            var el = document.createElement('div');
            el.className = 'cc-overlay';
            el.innerHTML =
                '<div class="cc-box">' +
                  '<div class="cc-body">' +
                    '<div class="cc-title">' +
                      '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#f59e0b" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">' +
                        '<path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/>' +
                        '<line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/>' +
                      '</svg>' +
                      (title || 'Confirm') +
                    '</div>' +
                    '<div class="cc-message">' + message + '</div>' +
                  '</div>' +
                  '<div class="cc-footer">' +
                    '<button class="cc-btn cc-btn-cancel">Cancel</button>' +
                    '<button class="cc-btn cc-btn-ok">OK</button>' +
                  '</div>' +
                '</div>';
            document.body.appendChild(el);

            var done = function (result) {
                el.style.animation = 'cc-fade-in 0.12s ease reverse forwards';
                setTimeout(function () { el.remove(); }, 120);
                resolve(result);
            };

            el.querySelector('.cc-btn-ok').addEventListener('click', function () { done(true); });
            el.querySelector('.cc-btn-cancel').addEventListener('click', function () { done(false); });
            el.addEventListener('click', function (e) { if (e.target === el) done(false); });

            var onKey = function (e) {
                if (e.key === 'Escape') { document.removeEventListener('keydown', onKey); done(false); }
                if (e.key === 'Enter')  { document.removeEventListener('keydown', onKey); done(true); }
            };
            document.addEventListener('keydown', onKey);
        });
    }

    // --- Re-trigger state ---
    var _lastClickTarget = null;   // element that was clicked by user
    var _isRetriggering  = false;  // true while we re-fire the click programmatically
    var _approvedUntil   = 0;      // how many confirms to auto-approve on next run
    var _confirmIndex    = 0;      // position counter within current execution

    // Capture every real user click so we know which element to re-trigger
    document.addEventListener('click', function (e) {
        if (!_isRetriggering) {
            _lastClickTarget = e.target;
            _approvedUntil   = 0;
            _confirmIndex    = 0;
        }
    }, true); // capture phase — runs before any handler

    window.confirm = function (message, title) {
        var myIndex = _confirmIndex++;

        // This confirm was already approved in a previous pass — skip it
        if (myIndex < _approvedUntil) {
            return true;
        }

        // This is the confirm that needs user input
        var approvedSoFar = myIndex;
        _confirmIndex = 0; // reset so next re-trigger starts from 0

        var target = _lastClickTarget;

        showDialog(message, title).then(function (result) {
            if (result && target) {
                _approvedUntil  = approvedSoFar + 1; // approve one more next time
                _confirmIndex   = 0;
                _isRetriggering = true;
                try {
                    // Re-fire the original click — triggers jQuery handlers AND onclick attrs
                    if (typeof jQuery !== 'undefined') {
                        jQuery(target).trigger('click');
                    } else {
                        target.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true }));
                    }
                } catch (ex) { /* ignore errors during retrigger */ }
                _isRetriggering = false;
            } else {
                // User cancelled — reset everything
                _approvedUntil = 0;
                _confirmIndex  = 0;
            }
        });

        return false; // block current execution
    };
})();