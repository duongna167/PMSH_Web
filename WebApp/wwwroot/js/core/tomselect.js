/*
 * TomSelect Helper
 * Author: (phuc)
 */

/**
 * TOMSELECT HELPER - Hướng dẫn sử dụng:
 * 1. Dùng với Class: initTomSelect('.tomselect-custom');
 * 2. Dùng với ID:    initTomSelect('#mySelect');
 * 3. Dùng với API:   <select id="com_txtCity" class="tomselect-custom" data-api="/Profile/GetAllCity"></select>;
 *                    initTomSelect('.tomselect-custom');  
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
        closeAfterSelect: true,
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

            const hostModal = el.closest('.modal');
            const tsOptions = {
                ...baseOptions,
                ...options,
                dropdownParent: options.dropdownParent || hostModal || document.body,
                plugins: {
                    ...baseOptions.plugins,
                    ...(options.plugins || {}),
                    clear_button: {
                        title: '',
                        html: (data) => {
                            if (el.disabled || el.hasAttribute('disabled')) {
                                return '';
                            }
                            // Đảm bảo class "clear-button" luôn tồn tại
                            const label = isMultiple ? 'Clear all' : 'Clear';
                            return `<div class="clear-button" data-tooltip="${label}">×</div>`;
                        },
                    },
                },
            };

            // Danh sách ngắn (Zone, Room Type, Section, Floor, Status...): hiển thị như combo chọn 1.
            // Thẻ <select multiple> được giữ nguyên để .val() và getTomSelectData() vẫn trả về mảng (tối đa 1 phần tử),
            // nên các màn đang đọc giá trị dạng danh sách không phải sửa. Bỏ trống = tất cả; nút × để xóa lựa chọn.
            const isSingleChoice = el.hasAttribute('data-single-choice');
            if (isSingleChoice) {
                tsOptions.maxItems = 1;
                tsOptions.placeholder = el.getAttribute('data-placeholder') || 'All';
            }

            if (isMultiple && !isSingleChoice) {
                tsOptions.plugins['remove_button'] = {
                    title: 'Delete', // Tooltip cho nút x từng item
                    label: '×', // Chỉ để ký tự thuần (plugin escape HTML, không dùng thẻ span ở đây)
                };
            }

            try {
                if (el.value === undefined) el.value = '';
                const ts = new TomSelect(el, tsOptions);
                bindTomSelectDropdown(ts);

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
                const api = el.dataset.api;
                if (api) {
                    loadDataToTomSelect(el, api);
                }

            } catch (e) {
                console.error('Lỗi TomSelect:', e);
            }
        });
    });
}

function pinTomSelectDropdown(ts) {
    if (!ts || !ts.dropdown || !ts.control) return;
    const r = ts.control.getBoundingClientRect();
    const dd = ts.dropdown;
    const space = Math.max(120, window.innerHeight - r.bottom - 8);
    dd.style.position = 'fixed';
    dd.style.top = r.bottom + 2 + 'px';
    dd.style.left = Math.max(8, r.left) + 'px';
    dd.style.width = r.width + 'px';
    dd.style.minWidth = r.width + 'px';
    dd.style.maxHeight = space + 'px';
    dd.style.overflowY = 'auto';
    dd.style.zIndex = '10650';
    dd.style.pointerEvents = 'auto';
}

function bindTomSelectDropdown(ts) {
    if (!ts || ts._pmshDdBound) return;
    ts._pmshDdBound = true;
    if (ts.dropdown) {
        ts.dropdown.addEventListener('mousedown', function (e) {
            e.preventDefault();
            e.stopPropagation();
        });
        ts.dropdown.addEventListener('click', function (e) {
            e.stopPropagation();
        });
    }
    ts.on('dropdown_open', function () {
        pinTomSelectDropdown(ts);
        requestAnimationFrame(function () {
            pinTomSelectDropdown(ts);
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

/**
 * City list follows Country. Catalog cities are stored per CountryID
 * (typically VN provinces). Foreign countries get an empty list; typing
 * a city name is allowed (create).
 */
function bindCitySelectToCountry(countrySelector, citySelector) {
    const countryTs = getTomSelect(countrySelector);
    const cityTs = getTomSelect(citySelector);
    if (!countryTs || !cityTs) {
        return { load: function () {} };
    }

    cityTs.settings.create = true;

    const mapCity = (item) => ({
        value: String(item.id || item.ID || ''),
        text: (item.code || item.Code) + '-' + (item.name || item.Name),
        data: item
    });

    const applyCityValue = (cityValue) => {
        if (cityValue == null || cityValue === '' || cityValue === 0 || cityValue === '0') return;
        const v = String(cityValue);
        if (cityTs.options[v]) {
            cityTs.setValue(v, true);
            return;
        }
        const match = Object.values(cityTs.options).find((o) => {
            const d = o.data || {};
            return String(d.code || d.Code || '') === v
                || String(d.id || d.ID || '') === v
                || String(o.text || '') === v;
        });
        if (match) {
            cityTs.setValue(match.value, true);
            return;
        }
        cityTs.addOption({ value: v, text: v });
        cityTs.setValue(v, true);
    };

    const load = (countryId, cityValue) => {
        const id = parseInt(countryId, 10) || 0;
        if (id <= 0) {
            cityTs.clear(true);
            cityTs.clearOptions();
            return;
        }
        loadDataToTomSelect(citySelector, '/Profile/GetAllCity?countryId=' + id, mapCity, function () {
            if (cityValue != null && cityValue !== '') applyCityValue(cityValue);
        });
    };

    if (!countryTs._cityCountryBound) {
        countryTs._cityCountryBound = true;
        countryTs.on('change', function (val) {
            if (countryTs._suspendCityReload) return;
            load(val);
        });
    }

    return { load };
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
// 2,5. Hàm SetValue cho 1 ID (Dùng cho Update/Edit)
function setTomSelectValueMulti(selector, value) {
    value = value.split(',').filter((x) => x);
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

/**
 * Load dữ liệu linh hoạt từ API hoặc Mảng dữ liệu có sẵn
 * @param {string} selector - Selector của element
 * @param {string|Array} source - Link API (string) hoặc Mảng dữ liệu (Array)
 * @param {function} mapFn - (Tùy chọn) Hàm để tự định nghĩa cấu trúc dữ liệu
 * 
 * VD: loadDataToTomSelect("#allot_setup_profileID", "/api/profiles", (item) => ({
          value: item.ID,
          text: `${item.Code} - ${item.Name}`,
          /hoặc  text: item.Code, /
          data: item
       }));

 * Lấy trường khác trong api 
   const selectedRoom = getTomSelectData("#allot_trans_roomTypeID");
    if (selectedRoom) {
        const maxQty = selectedRoom.data.MaxInventory; 
        const roomGrade = selectedRoom.data.GradeName;
    } 

 * Dùng với dữ liệu ở mảng
    const myData = [
        { ID: 1, Name: 'Hà Nội' },
        { ID: 2, Name: 'Sài Gòn' }
    ];
    loadDataToTomSelect("#mySelect", myData);


 */

/**
 * Trả về Promise resolve khi dữ liệu đã nạp xong vào TomSelect (kể cả lỗi / bị request mới thay thế),
 * để có thể chờ: await loadDataToTomSelect(...) hoặc Promise.all([...]).
 * Promise không bao giờ reject. Cách gọi cũ (không dùng giá trị trả về) vẫn hoạt động như trước.
 */
function loadDataToTomSelect(selector, source, mapFn = null, callback = null) {
    return new Promise((resolve) => {
        _loadDataToTomSelectImpl(selector, source, mapFn, callback, resolve);
    });
}

function _loadDataToTomSelectImpl(selector, source, mapFn, callback, done) {
    const el = document.querySelector(selector);
    if (!el || !el.tomselect) {
        console.warn(`[TomSelect] ${selector} chưa init.`);
        done({ success: false, error: 'Not initialized' });
        return;
    }

    const ts = el.tomselect;

    // Date.now() có thể trùng khi gọi liên tiếp trong cùng 1ms -> thêm bộ đếm
    const requestId = Date.now() + '_' + (++_loadDataToTomSelectImpl._seq);
    ts._lastLoadId = requestId;

    // Request đang chạy bị thay thế: báo xong theo request mới nhất
    const supersede = () => {
        if (ts._lastFinishedId === ts._lastLoadId) {
            done({ success: true, superseded: true });
            return;
        }
        const waiters = ts._loadWaiters || (ts._loadWaiters = []);
        waiters.push(done);
    };
    const finish = (result) => {
        ts._lastFinishedId = requestId;
        done(result);
        const waiters = ts._loadWaiters || [];
        ts._loadWaiters = [];
        waiters.forEach(w => w(result));
    };

    try {
        ts.clear(true);          // silent clear
        ts.clearOptions();

        const handleData = (rawData) => {
            if (ts._lastLoadId !== requestId) return supersede();

            if (!Array.isArray(rawData) || rawData.length === 0) {
                ts.refreshOptions(false);

                const emptyResult = { ts, data: [], success: true, empty: true };
                try { callback?.(emptyResult); } finally { finish(emptyResult); }
                return;
            }

            const formattedData = rawData.map(item => {
                if (typeof mapFn === 'function') return mapFn(item);

                const value = item?.id ?? item?.ID ?? item?.Code ?? '';
                const text = item?.name ?? item?.Name ?? item?.Description ?? value;

                return {
                    value,
                    text,
                    data: item ?? {}
                };
            });

            ts.addOptions(formattedData);
            ts.refreshOptions(false);

            const okResult = { ts, data: formattedData, success: true, empty: false };
            try { callback?.(okResult); } finally { finish(okResult); }
        };

        // ===== LOCAL =====
        if (Array.isArray(source)) {
            handleData(source);
            return;
        }

        // ===== API =====
        if (typeof source === 'string') {
            fetch(source)
                .then(res => {
                    if (!res.ok) throw new Error(`HTTP ${res.status}`);
                    return res.json();
                })
                .then(data => handleData(data))
                .catch(err => {
                    if (ts._lastLoadId !== requestId) return supersede();
                    if (ts._lastFinishedId === requestId) return; // lỗi phát sinh từ callback, đã finish

                    console.error(`[TomSelect] Load error (${selector})`, err);

                    const errResult = { ts, data: [], success: false, error: err };
                    try { callback?.(errResult); } finally { finish(errResult); }
                });

            return;
        }

        // ===== INVALID SOURCE =====
        console.warn(`[TomSelect] Source không hợp lệ (${selector})`);

        const invalidResult = { ts, data: [], success: false, error: 'Invalid source' };
        try { callback?.(invalidResult); } finally { finish(invalidResult); }

    } catch (error) {
        if (ts._lastLoadId !== requestId) return supersede();
        if (ts._lastFinishedId === requestId) return; // lỗi phát sinh từ callback, đã finish

        console.error(`[TomSelect] Load Data Error (${selector}):`, error);

        const errResult = { ts, data: [], success: false, error: error };
        try { callback?.(errResult); } finally { finish(errResult); }
    }
}
_loadDataToTomSelectImpl._seq = 0;
/**
 * Hàm thần thánh mod được full dữ liệu tomselect (Chat GPT)
 *  VD:
 bindTomSelectData({
    selector: '#trans_transactionsGernew',
    data: transactionsArray, // mảng dữ liệu truyền vào
    valueField: 'code',
    labelField: 'text',
    searchField: ['text', 'description'],
    tomSelectOptions: {
        placeholder: 'Please choose Group',
        render: {
        option(data, escape) {
            return `<div><b>${escape(data.code ?? '')}</b> - ${escape(data.description ?? '')}</div>`;
        },
        item(data, escape) {
            return `<div>${escape(data.code ?? '')}</div>`;
        }
        }
    },
    mapFn: (x) => ({
        code: x.code,                               // ✅ field làm value
        text: x.description ? `${x.code} - ${x.description}` : x.code, // ✅ label
        description: x.description,
        // có thể giữ thêm field khác để dùng render/search
        raw: x
    })
});


    * Lưu ý:
     Dùng mapFn Linh hoạt nhất, có thể tạo text phức tạp, có thể giữ thêm field để render
        Có thể lấy dữ liệu đã chọn đầy đủ không cần gọi API lần nữa, truy xuất rất nhanh, dùng được cho render hoặc logic khác
        VD:
        const ts = document.querySelector('#trans_transactionsGernew').tomselect;

        ts.on("change", function(value) {

        const option = ts.options[value];   // object option
        const data = option.raw;            // object gốc

        console.log(data);

        });

     Dùng valueField + labelField: Không cần map dữ liệu, chỉ định field nào dùng làm value và text.


 */
async function bindTomSelectData({
    selector,
    data, // Array | () => Array | Promise<Array>

    // TomSelect init (placeholder, render, plugins, ...)
    tomSelectOptions = {},

    // Cách lấy value/text (chọn 1 trong 2):
    mapFn = null, //Cách 1: (item) => ({ value, text, ...anyFields })
    valueField = 'value', // Cách 2:nếu mapFn trả object, field làm value
    labelField = 'text', //       field làm label hiển thị
    searchField = null, //       vd: ['text','description'] hoặc 'text'

    // Lifecycle
    destroyBeforeInit = true,
    initIfMissing = true,

    // Clear/Default
    clearSelected = true,
    addDefaultOption = null, // { value:'0', text:'...', setValue:true }
    keepTextboxEmpty = true,

    postProcess = null,
}) {
    if (!selector) throw new Error('selector is required');

    // destroy nếu có
    const existing = getTomSelect(selector);
    if (existing && destroyBeforeInit) existing.destroy();

    // Chuẩn bị options init TomSelect với valueField/labelField/searchField
    const initOptions = {
        valueField,
        labelField,
        ...(searchField ? { searchField } : {}),
        ...tomSelectOptions,
    };

    if (initIfMissing) initTomSelect(selector, initOptions);

    const el = document.querySelector(selector);
    const ts = el?.tomselect;
    if (!ts) {
        console.warn(`[TomSelect] ${selector} chưa được khởi tạo TomSelect.`);
        return Promise.resolve({ ts: null, raw: null, formatted: [] });
    }

    const resolveData = typeof data === 'function' ? data() : data;

    try {
        const raw = await Promise.resolve(resolveData);
        const rows = Array.isArray(raw) ? raw : [];

        // Format data
        let formatted;
        if (typeof mapFn === 'function') {
            formatted = rows.map(mapFn).filter((x) => x && x[valueField]);
        } else {
            // fallback map (ít dùng khi bạn muốn custom)
            formatted = rows
                .map((x_1) => ({
                    value: x_1.id ?? x_1.ID ?? x_1.code ?? x_1.Code ?? '',
                    text: x_1.name ?? x_1.Name ?? x_1.description ?? x_1.Description ?? '',
                    data: x_1,
                }))
                .filter((x_2) => x_2.value);
        }

        if (clearSelected) ts.clear(true);
        ts.clearOptions();

        if (addDefaultOption?.value != null) {
            const opt = {
                [valueField]: String(addDefaultOption.value),
                [labelField]: addDefaultOption.text ?? '',
            };
            ts.addOption(opt);
            if (addDefaultOption.setValue) ts.setValue(String(addDefaultOption.value), true);
        }

        ts.addOptions(formatted);

        if (keepTextboxEmpty) ts.setTextboxValue('');
        ts.refreshOptions(false);

        if (typeof postProcess === 'function') postProcess({ ts, raw: rows, formatted });
        return { ts, raw: rows, formatted };
    } catch (err) {
        console.error(`[TomSelect] bindTomSelectData error (${selector}):`, err);
        return { ts, raw: null, formatted: [] };
    }
}
