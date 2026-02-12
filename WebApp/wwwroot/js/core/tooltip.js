(function () {
    if (window.__GLOBAL_TOOLTIP_INITIALIZED__) return;
    window.__GLOBAL_TOOLTIP_INITIALIZED__ = true;

    const tooltip = document.createElement('div');
    tooltip.className = 'global-tooltip';
    Object.assign(tooltip.style, {
        position: 'fixed',
        zIndex: '999999',
        background: '#333',
        color: '#fff',
        fontSize: '12px',
        padding: '6px 10px',
        borderRadius: '4px',
        pointerEvents: 'none',
        whiteSpace: 'nowrap',
        opacity: '0',
        transition: 'opacity 0.15s ease'
    });
    document.body.appendChild(tooltip);

    let activeTarget = null;

    // Định nghĩa hàm global tại đây
    window.hideTooltip = function () {
        activeTarget = null;
        tooltip.style.opacity = '0';
    };

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
        if (!document.body.contains(activeTarget)) {
            window.hideTooltip();
            return;
        }
        positionTooltip(e);
    }

    function positionTooltip(e) {
        const offset = 12;
        const rect = tooltip.getBoundingClientRect();
        let left = e.clientX + offset;
        let top = e.clientY + offset;
        if (left + rect.width > window.innerWidth) left = e.clientX - rect.width - offset;
        if (top + rect.height > window.innerHeight) top = e.clientY - rect.height - offset;
        tooltip.style.left = left + 'px';
        tooltip.style.top = top + 'px';
    }

    document.addEventListener('mouseover', showTooltip);
    document.addEventListener('mousemove', moveTooltip);
    document.addEventListener('mouseout', function (e) {
        if (e.target.closest('[data-tooltip]')) window.hideTooltip();
    });
})();