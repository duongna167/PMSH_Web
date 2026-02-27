/*
 * TomSelect Helper
 * Author: (phuc)
 */

/**
 * TOMSELECT HELPER - Hướng dẫn sử dụng:
 * 1. Dùng với Class: initTomSelect('.tomselect-custom');
 * 2. Dùng với ID:    initTomSelect('#mySelect');
 * 3. Dùng với API:   initTomSelect('#mySelect', { valueField: 'ID', labelField: 'Name' }, '/api/url');
 * 4. Dùng ViewBag:   Chỉ cần render <option> trong HTML rồi gọi initTomSelect('#id');
 */

/**
 * Khởi tạo TomSelect cho danh sách selectors
 * @param {string|string[]} selectors - ID, Class hoặc Array các selector
 * @param {object} options - Cấu hình ghi đè của TomSelect
 */
/* ================= INIT ================= */

function initTomSelect(selectors, options = {}, apiUrl = null) {
    if (!Array.isArray(selectors)) selectors = [selectors];

    const baseOptions = {
        plugins: {
            dropdown_input: {},
        },
        dataAttr: 'data-data',
        create: false,
        valueField: 'value',
        labelField: 'text',
        searchField: ['text'],
        placeholder: 'Enter or select...',
        render: {
            option(data, escape) {
                return `<div class="p-2">${escape(data.text)}</div>`;
            },
            item(data, escape) {
                return `<div><span class="ts-item-text" data-tooltip="${escape(data.text)}">${escape(data.text)}</span></div>`;
            },
        },
    };

    selectors.forEach((selector) => {
        const elements = document.querySelectorAll(selector);

        if (!elements || elements.length === 0) return;
        document.querySelectorAll(selector).forEach((el) => {
            if (el.tomselect) return;
            //if (!el || !document.body.contains(el)) return;

            const tagName = el.tagName.toUpperCase();
            if (tagName !== 'SELECT' && tagName !== 'INPUT') {
                console.warn(`TomSelect Skip: Element ${selector} is a ${tagName}, not a SELECT/INPUT.`);
                return;
            }

            const isSelect = el.tagName === 'SELECT';
            const isMultiple = el.hasAttribute('multiple') || (options.maxItems && options.maxItems > 1);

            if (isSelect && !isMultiple) {
                const optionsArray = el.options ? Array.from(el.options) : [];
                const hasEmptyOption = optionsArray.some((o) => o.value === '');

                if (!hasEmptyOption) {
                    const emptyOption = document.createElement('option');
                    emptyOption.value = '';
                    emptyOption.text = '';
                    el.insertBefore(emptyOption, el.firstChild);
                }
            }

            const tsOptions = {
                ...baseOptions,
                ...options,
                plugins: {
                    ...baseOptions.plugins,
                    ...(options.plugins || {}),
                    clear_button: {
                        title: '',
                        html: (data) => {
                            // Đảm bảo class "clear-button" luôn tồn tại
                            const label = isMultiple ? 'Clear all' : 'Clear';
                            return `<div class="clear-button" data-tooltip="${label}">×</div>`;
                        },
                    },
                },
            };

            if (isMultiple) {
                tsOptions.plugins['remove_button'] = {
                    title: '', // Để trống title mặc định
                    label: `<span data-tooltip="Delete">×</span>`, // Thêm tooltip cho nút x từng item
                };
            }

            try {
                if (el.value === undefined) el.value = '';
                const ts = new TomSelect(el, tsOptions);

                // ---- SYNC DISABLED STATE (IMPORTANT) ----
                const shouldBeDisabled =
                    el.disabled === true || el.hasAttribute('disabled') || el.dataset.tsDisabled === '1';

                if (shouldBeDisabled) {
                    ts.disable(); // disable đúng chuẩn
                } else {
                    ts.enable(); // đảm bảo enable nếu không disabled
                }

                // Sự kiện khi xóa 1 item
                ts.on('item_remove', function () {
                    if (typeof window.hideTooltip === 'function') window.hideTooltip();
                    this.setTextboxValue(''); // Xóa từ khóa search
                    this.refreshOptions(false); // Hiện lại item trong dropdown
                });

                // Sự kiện khi nhấn nút Clear All
                ts.on('clear', function () {
                    if (typeof window.hideTooltip === 'function') window.hideTooltip();
                    this.setTextboxValue('');
                    this.refreshOptions(false); // Trả lại toàn bộ data
                });

                // Gán trực tiếp cho nút Clear Button để xóa mảng dứt khoát
                const clearBtn = ts.control.querySelector('.clear-button');
                if (clearBtn) {
                    clearBtn.onclick = (e) => {
                        e.preventDefault();
                        e.stopPropagation();
                        ts.setValue([]); // Xóa sạch mảng chọn
                        ts.refreshOptions(false);
                        if (typeof window.hideTooltip === 'function') window.hideTooltip();
                    };
                }

                // LOAD DỮ LIỆU TỪ API
                if (apiUrl) {
                    fetch(apiUrl)
                        .then((res) => res.json())
                        .then((data) => {
                            const vField = tsOptions.valueField || 'value';
                            const lField = tsOptions.labelField || 'text';
                            const formattedData = data.map((i) => ({
                                value: i[vField],
                                text: i[lField],
                            }));
                            ts.addOptions(formattedData);
                            ts.refreshOptions(false);
                        });
                }
            } catch (e) {
                console.error('Lỗi TomSelect:', e);
            }
        });
    });
}

/* ================= GET VALUE ================= */
/*  Usage:
    const owners = getTomSelectValue("#ownerCode");

    const model = {
        OwnerCode: owners.map(x => x.value),
        OwnerName: owners.map(x => x.name)
    };
*/

function getTomSelect(selector) {
    const el = document.querySelector(selector);
    return el && el.tomselect ? el.tomselect : null;
}

function getTomSelectData(selector) {
    const ts = getTomSelect(selector);
    if (!ts) return null;

    const value = ts.getValue();
    if (!value || value.length === 0) return null;

    const mapItem = (v) => {
        const opt = ts.options[v] || {};
        return {
            value: v,
            text: opt.text || '',
            data: { ...opt }, // giữ toàn bộ data-roomno, data-name,...
        };
    };

    // MULTI
    if (Array.isArray(value)) {
        return value.map(mapItem);
    }

    // SINGLE
    return mapItem(value);
}

function toBackendValue(selectData, field) {
    if (!selectData) return '';

    // MULTI
    if (Array.isArray(selectData)) {
        return selectData.map((x) => x.data?.[field] ?? x.value);
    }

    // SINGLE
    return selectData.data?.[field] ?? selectData.value;
}

// 1. Hàm Clear cho 1 ID
function clearTomSelect(selector) {
    if (document.querySelector(selector).tomselect) {
        document.querySelector(selector).tomselect.clear();
    }
}

// 2. Hàm SetValue cho 1 ID (Dùng cho Update/Edit)
function setTomSelectValue(selector, value) {
    if (document.querySelector(selector).tomselect) {
        document.querySelector(selector).tomselect.setValue(value);
    }
}

// 3. Hàm truyền nhiều ID vào để Clear cùng lúc
function clearMultipleTomSelect(ids) {
    if (!Array.isArray(ids)) ids = [ids];

    ids.forEach((id) => {
        const el = document.querySelector(id);
        if (!el || !el.tomselect) return;

        const ts = el.tomselect;

        ts.setValue(''); // set về empty option
        ts.setTextboxValue('');
        ts.refreshOptions(false);
    });
}

// Hàm detroy Tomselect khi đóng các modal
function destroyTomSelect(selectors) {
    selectors.forEach((selector) => {
        const el = document.querySelector(selector);
        if (el && el.tomselect) {
            el.tomselect.destroy();
        }
    });
}

async function loadDataToTomSelect(selector, apiUrl) {
    // 1. Khởi tạo TomSelect trước (dùng helper của bạn)
    initTomSelect(selector);

    const ts = getTomSelect(selector);
    if (!ts) return;

    try {
        // 2. Gọi API
        const response = await fetch(apiUrl);
        const data = await response.json();

        // 3. Format lại dữ liệu nếu API trả về ID/Name thay vì value/text
        const formattedData = data.map((item) => ({
            value: item.id || item.ID, // Linh hoạt theo API của bạn
            text: item.name || item.Name,
        }));

        // 4. Đổ dữ liệu vào và refresh
        ts.addOptions(formattedData);
        ts.refreshOptions(false);
    } catch (error) {
        console.error('Lỗi khi load API:', error);
    }
}
