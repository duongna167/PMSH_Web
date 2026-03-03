/**
 * LoadingSpinnerManager
 * Quản lý hiển thị loading spinner toàn cục
 */
window.LoadingManager = (function () {
    const SPINNER_ID = 'loading_spinner_modal';

    // Hàm khởi tạo HTML khi file JS được nạp
    function _init() {
        if (document.getElementById(SPINNER_ID)) return;

        const spinnerHtml = `
            <div id="${SPINNER_ID}" style="
                position: fixed;
                top: 0;
                left: 0;
                width: 100%;
                height: 100%;
                background: rgba(255, 255, 255, 0.55);
                backdrop-filter: blur(2px);
                z-index: 9999;
                display: none;
                align-items: center;
                justify-content: center;
                pointer-events: all;">
                <div class="spinner-border text-primary" role="status" style="width: 3rem; height: 3rem;">
                    <span class="visually-hidden">Loading...</span>
                </div>
            </div>`;

        document.body.insertAdjacentHTML('beforeend', spinnerHtml);
    }

    // Hàm hiển thị
    function show() {
        const el = document.getElementById(SPINNER_ID);
        if (el) {
            el.style.display = 'flex';
        }
    }

    // Hàm đóng
    function hide() {
        const el = document.getElementById(SPINNER_ID);
        if (el) {
            el.style.display = 'none';
        }
    }

    // Tự động chạy init khi DOM sẵn sàng
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', _init);
    } else {
        _init();
    }

    // Export ra global để sử dụng
    return { show, hide };
})();