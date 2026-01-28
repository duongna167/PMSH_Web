/*
 * How use to ?
   - With class : initTomSelect('.tomselect');
   - With id or ids : initTomSelect('#mySelect'); or initTomSelect(['#select1', '#select2']);

 * Select option ? single or multiple (add attribute multiple to select tag)
   - single:  <select id="mySelect" class="tomselect"> ---- 
              Override  js:
                  initTomSelect('#country', {
                    maxItems: 1
                  });

   - multiple: <select id="mySelect" class="tomselect" multiple> ----  
            + Select multiple values, but limit the number of selections :
               Override  js:
                  initTomSelect('#country', {
                    maxItems: 5 // limit to 5 selections
                  });
 * If tomselect is inside a modal đổi id modal
   -   #itemModal .modal-content,
    #itemModal .modal-body {
        overflow: visible !important;
    }
 
 */

/* =========================================================
 * TomSelect Helper
 * Author: (phuc)
 * Usage:
 *  initTomSelect('.tomselect');
 *  initTomSelect('#zone', { maxItems: 1 });
 *  initTomSelect(['#roomID', '#ownerCode']);
 *
 *  getTomSelectValue('#ownerCode');
 *  getTomSelectValue('#ownerCode', { asObject: true });
 * ========================================================= */

(function (window) {

    /* ================= INIT ================= */

    function initTomSelect(selectors, options = {}) {
        if (!Array.isArray(selectors)) selectors = [selectors];

        const baseOptions = {
            plugins: {
                dropdown_input: {}
            },
            create: false,
            valueField: 'value',
            labelField: 'text',
            searchField: ['text'],
            placeholder: 'Enter or select options...',
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
            document.querySelectorAll(selector).forEach(el => {

                if (el.tomselect) el.tomselect.destroy();

                const isMultiple =
                    el.hasAttribute('multiple') ||
                    (options.maxItems && options.maxItems > 1);

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
                    onChange(value) {
                        const $wrapper = $(this.wrapper); 

                        if (value && value.length > 0) {
                            $wrapper.removeClass("is-invalid");

                            $wrapper
                                .closest(".col-9, .form-group, .form-floating")
                                .find(".invalid-feedback")
                                .hide();
                        }
                    }
                  
                };

                new TomSelect(el, tsOptions);
             
            });
        }); 
    }

    /* ================= GET VALUE ================= */

    function getTomSelectValue(selector) {
        const el = document.querySelector(selector);
        if (!el || !el.tomselect) return null;

        const ts = el.tomselect;
        const val = ts.getValue();

        if (!val || val.length === 0) return null;

        // MULTI
        if (Array.isArray(val)) {
            return val.map(v => ({
                value: v,
                text: ts.options[v]?.text || "",
                data: ts.options[v]?.dataset || {}
            }));
        }

        // SINGLE
        const opt = ts.options[val] || {};
        return {
            value: val,
            text: ts.options[val]?.text || "",
            data: opt.dataset || {}
        };
    };


    /* ================= SET / CLEAR ================= */

    function setTomSelectValue(selector, value) {
        const el = document.querySelector(selector);
        if (!el) return;

        [...el.options].forEach(opt => {
            opt.selected = Array.isArray(value)
                ? value.includes(opt.value)
                : opt.value == value;
        });

        el.dispatchEvent(new Event("change", { bubbles: true }));
    }

    function clearTomSelect(selector) {
        const el = document.querySelector(selector);
        if (!el) return;

        // Bỏ chọn toàn bộ option
        el.value = null;
        [...el.options].forEach(opt => opt.selected = false);

        // Báo cho TomSelect (và validation) biết là có thay đổi
        el.dispatchEvent(new Event("change", { bubbles: true }));
    }



    /* ================= EXPORT ================= */

    window.initTomSelect = initTomSelect;
    window.getTomSelectValue = getTomSelectValue;
    window.setTomSelectValue = setTomSelectValue;
    window.clearTomSelect = clearTomSelect;

})(window);
