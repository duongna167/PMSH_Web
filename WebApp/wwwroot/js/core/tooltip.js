/**
 * ==============================================================================
 * Tooltip Custom Project
 * ==============================================================================
 *
 * MÔ TẢ:
 * - Tạo 1 tooltip global duy nhất append vào <body>
 * - Hiển thị tooltip cho mọi phần tử có attribute: data-tooltip
 * - Không bị ảnh hưởng overflow, modal, grid, scroll container
 * - Không phụ thuộc Bootstrap / DevExtreme
 *
 * ------------------------------------------------------------------------------
 * CÁCH SỬ DỤNG:
 *
 * 1. HTML:
 *    <span data-tooltip="Delete item">
 *        <i class="fas fa-times"></i>
 *    </span>
 *
 * 2. JS:
 *    Chỉ cần include file này sau khi load DOM:
 *
 *    <script src="/assets/js/project.tooltip.js"></script>
 *
 *    Không cần khởi tạo thêm gì.
 *
 * ==============================================================================
 */

(function () {

    // Tránh khởi tạo nhiều lần
    if (window.__GLOBAL_TOOLTIP_INITIALIZED__) return;
    window.__GLOBAL_TOOLTIP_INITIALIZED__ = true;

    // Tạo tooltip element
    const tooltip = document.createElement('div');
    tooltip.className = 'global-tooltip';
    tooltip.style.position = 'fixed';
    tooltip.style.zIndex = '999999';
    tooltip.style.background = '#333';
    tooltip.style.color = '#fff';
    tooltip.style.fontSize = '12px';
    tooltip.style.padding = '6px 10px';
    tooltip.style.borderRadius = '4px';
    tooltip.style.pointerEvents = 'none';
    tooltip.style.whiteSpace = 'nowrap';
    tooltip.style.opacity = '0';
    tooltip.style.transition = 'opacity 0.15s ease';

    document.body.appendChild(tooltip);

    let activeTarget = null;

    function showTooltip(e) {
        const target = e.target.closest('[data-tooltip]');
        if (!target) return;

        activeTarget = target;
        tooltip.textContent = target.getAttribute('data-tooltip');
        tooltip.style.opacity = '1';
        positionTooltip(e);
    }

    function moveTooltip(e) {
        if (!activeTarget) return;
        positionTooltip(e);
    }

    function hideTooltip() {
        activeTarget = null;
        tooltip.style.opacity = '0';
    }

    function positionTooltip(e) {
        const offset = 12;

        const rect = tooltip.getBoundingClientRect();

        let left = e.clientX + offset;
        let top = e.clientY + offset;

        // Tránh tràn phải
        if (left + rect.width > window.innerWidth) {
            left = e.clientX - rect.width - offset;
        }

        // Tránh tràn dưới
        if (top + rect.height > window.innerHeight) {
            top = e.clientY - rect.height - offset;
        }

        tooltip.style.left = left + 'px';
        tooltip.style.top = top + 'px';
    }

    document.addEventListener('mouseover', showTooltip);
    document.addEventListener('mousemove', moveTooltip);
    document.addEventListener('mouseout', function (e) {
        if (e.target.closest('[data-tooltip]')) {
            hideTooltip();
        }
    });

})();
