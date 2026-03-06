/* ============================================================
   GLOBAL LOADING MANAGER
   Version: 1.0
   Cách dùng:
   - LoadingGlobal.init(): Khởi tạo các thành phần HTML cần thiết cho loading.
   - LoadingGlobal.show() / hide(): Hiển thị/Ẩn lớp phủ loading toàn màn hình.
        + Thực hiện các tác vụ như Lưu dữ liệu, Xóa dữ liệu hoặc Tải trang mới.
          VD: function saveData() {
                // 1. Hiện loading toàn màn hình
                LoadingGlobal.show(); 

                $.ajax({
                    url: '/Allotment/Save',
                    type: 'POST',
                    data: payload,
                    success: function(res) {
                        // Xử lý thành công
                    },
                    complete: function() {
                        // 2. Luôn ẩn loading khi kết thúc (dù thành công hay lỗi)
                        LoadingGlobal.hide(); 
                    }
                });
            }
   -  LoadingGlobal.showElement() / hideElement(): Hiển thị/Ẩn lớp phủ loading cho thành phần cụ thể (Grid, Div, Modal)
        + Sử dụng cho DevExtreme Grid
            VD: reloadMainGrid: function() {
                    // Lấy phần tử DOM của Grid (Lưu ý: phải là DOM thật, không phải object Grid)
                    const gridElement = document.querySelector("#allot_search_gridAllotInfor");
    
                    // Hiện loading riêng cho bảng này
                    LoadingGlobal.showElement(gridElement); 

                    $.get('/Allotment/GetData', function(data) {
                        // ... Render dữ liệu ...
        
                        // Ẩn loading của bảng
                        LoadingGlobal.hideElement(gridElement); 
                    });
                }. 
   - (Nếu không muốn hiện loading)
         $.ajax({
            url: '/Allotment/AutoSave',
            global: false, // Dòng này sẽ ngăn chặn việc kích hoạt ajaxStart/ajaxStop tự động
            success: function() { }
        });

   ============================================================ */

const LoadingGlobal = (function () {

    /* =========================
       CONFIG
    ========================== */
    const config = {
        delayBeforeShow: 200,      // Tránh flicker nếu API quá nhanh
        minimumVisibleTime: 400,   // Spinner phải hiển thị tối thiểu
        zIndex: 9999
    };

    let activeRequests = 0;
    let showTimer = null;
    let hideTimer = null;
    let visibleSince = 0;

    /* =========================
       INIT GLOBAL ELEMENT
    ========================== */
    function createGlobalLoader() {

        if (document.getElementById("global-loading-overlay")) return;

        const overlay = document.createElement("div");
        overlay.id = "global-loading-overlay";
        overlay.innerHTML = `
            <div class="loading-backdrop"></div>
            <div class="loading-spinner">
                <div class="spinner-border text-primary" role="status"></div>
            </div>
        `;

        document.body.appendChild(overlay);
    }

    /* =========================
       SHOW GLOBAL
    ========================== */
    function show() {

        activeRequests++;

        if (activeRequests > 1) return;

        clearTimeout(hideTimer);

        showTimer = setTimeout(() => {
            const overlay = document.getElementById("global-loading-overlay");
            if (!overlay) return;

            overlay.classList.add("active");
            visibleSince = Date.now();
        }, config.delayBeforeShow);
    }

    /* =========================
       HIDE GLOBAL
    ========================== */
    function hide() {

        if (activeRequests > 0) {
            activeRequests--;
        }

        if (activeRequests !== 0) return;

        clearTimeout(showTimer);

        const overlay = document.getElementById("global-loading-overlay");
        if (!overlay || !overlay.classList.contains("active")) return;

        const elapsed = Date.now() - visibleSince;
        const remaining = config.minimumVisibleTime - elapsed;

        hideTimer = setTimeout(() => {
            overlay.classList.remove("active");
        }, remaining > 0 ? remaining : 0);
    }

    /* =========================
       ELEMENT LOADING
    ========================== */
    function showElement(element) {

        if (!element) return;

        if (!element.classList.contains("loading-container")) {
            element.classList.add("loading-container");
        }

        let overlay = element.querySelector(".element-loading-overlay");

        if (!overlay) {
            overlay = document.createElement("div");
            overlay.className = "element-loading-overlay";
            overlay.innerHTML = `
                <div class="spinner-border text-primary" role="status"></div>
            `;
            element.appendChild(overlay);
        }

        overlay.classList.add("active");
    }

    function hideElement(element) {
        if (!element) return;

        const overlay = element.querySelector(".element-loading-overlay");
        if (overlay) {
            overlay.remove();
        }
    }

    /* =========================
       FETCH WRAPPER (Optional)
    ========================== */
    function wrapFetch() {
        const originalFetch = window.fetch;

        window.fetch = function (...args) {
            show();
            return originalFetch.apply(this, args)
                .finally(() => hide());
        };
    }

    /* =========================
       INIT
    ========================== */
    function init() {
        createGlobalLoader();
    }

    return {
        init,
        show,
        hide,
        showElement,
        hideElement,
        wrapFetch
    };

})();