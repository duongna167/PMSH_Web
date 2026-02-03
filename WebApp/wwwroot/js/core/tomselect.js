/*
 * TomSelect Helper
 * Author: (phuc)
 * How use to ?
   - With class : initTomSelect('.tomselect');
   - With id or ids : initTomSelect('#mySelect'); or initTomSelect(['#select1', '#select2']);

 * Select option ? single or multiple (add attribute multiple to select tag)
   - single:  <select id="mySelect" class="tomselect-custom"> ---- 

   - multiple: <select id="mySelect" class="tomselect-custom" multiple> ----  
            + Select multiple values, but limit the number of selections :
               Override  js:
                  initTomSelect('#country');
 * 
 */
/**
 * Khởi tạo TomSelect cho danh sách selectors
 * @param {string|string[]} selectors - ID, Class hoặc Array các selector
 * @param {object} options - Cấu hình ghi đè của TomSelect
 */
    /* ================= INIT ================= */

    function initTomSelect(selectors, options = {}) {
        if (!Array.isArray(selectors)) selectors = [selectors];

        const baseOptions = {
            plugins: {
                dropdown_input: {}
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
                    return `<div title="${escape(data.text)}">${escape(data.text)}</div>`;
                }
            }
        };

        selectors.forEach(selector => {
            const elements = document.querySelectorAll(selector);

            if (!elements || elements.length === 0) return;
            document.querySelectorAll(selector).forEach(el => {

                if (el.tomselect) return;
                //if (!el || !document.body.contains(el)) return;

                const tagName = el.tagName.toUpperCase();
                if (tagName !== 'SELECT' && tagName !== 'INPUT') {
                    console.warn(`TomSelect Skip: Element ${selector} is a ${tagName}, not a SELECT/INPUT.`);
                    return;
                }

                const isSelect = el.tagName === 'SELECT';
                const isMultiple =
                    el.hasAttribute('multiple') ||
                    (options.maxItems && options.maxItems > 1);

                if (isSelect && !isMultiple) {
                    const optionsArray = el.options ? Array.from(el.options) : [];
                    const hasEmptyOption = optionsArray.some(o => o.value === "");

                    if (!hasEmptyOption) {
                        const emptyOption = document.createElement("option");
                        emptyOption.value = "";
                        emptyOption.text = "";
                        el.insertBefore(emptyOption, el.firstChild);
                    }
                }

                const tsOptions = {
                    ...baseOptions,
                    ...options,
                    plugins: {
                        ...baseOptions.plugins,
                        ...(options.plugins || {}),
                        ...(isMultiple ? {
                            remove_button: { title: 'Remove' }
                        } : {}),
                        clear_button: {
                            title: isMultiple ? 'Clear all' : 'Clear'
                        }
                    },

                };
                try {
                    if (el.value === undefined) el.value = '';

                    const ts = new TomSelect(el, tsOptions);
                    ts.on('clear', function () {
                        this.close();
                        if (this.control_input) this.control_input.blur();
                        this.refreshOptions(false);
                    });
                } catch (e) {
                    // log
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
                text: opt.text || "",
                data: { ...opt }   // giữ toàn bộ data-roomno, data-name,...
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
        if (!selectData) return "";

        // MULTI
        if (Array.isArray(selectData)) {
            return selectData.map(x => x.data?.[field] ?? x.value);
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
        ids.forEach(id => {
            const el = document.querySelector(id);
            if (el && el.tomselect) {
                el.tomselect.clear();
            }
        });
    }
