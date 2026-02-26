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
        const typeClass = title === "Warning" ? "toast-warning" : (isSuccess ? "toast-success" : "toast-danger");

        const toastHtml = `
            <div id="${toastId}" class="toast minimal-toast ${typeClass}" role="alert" aria-live="assertive" aria-atomic="true" data-bs-autohide="false">
                <div class="toast-header">
                    <span class="title-text">${title}</span>
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