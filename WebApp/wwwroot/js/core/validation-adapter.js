window.ValidationAdapter = (function () {

    function findFieldContainer($el) {
        return $el.closest(
            ".form-group, .form-floating, .input-group, .col, [class^='col-']"
        );
    }
    function apply(errors, formSelector) {
        clear(formSelector);

        errors.forEach(err => {
            if (err.field === "general") return;

            const $field = $(`${formSelector} [name='${err.field}']`);
            if ($field.length === 0) return;

            // TomSelect
            if ($field[0].tomselect) {
                markTomSelectInvalid($field, err.message);
            } else {
                markInputInvalid($field, err.message);
            }
        });
    }

    function markInputInvalid($el, message) {
        $el.addClass("is-invalid");

        const $container = findFieldContainer($el);
        const $feedback = $container.find(".invalid-feedback").first();

        if ($feedback.length === 0) return;

        $feedback.text(message).show();
    }

    function markTomSelectInvalid($el, message) {
        const ts = $el[0].tomselect;
        if (!ts) return;

        const $wrapper = $(ts.wrapper);
        $wrapper.addClass("is-invalid");

        const $container = findFieldContainer($wrapper);

        const $feedback = $container.find(".invalid-feedback").first();

        if ($feedback.length === 0) {
            console.warn("No invalid-feedback found for TomSelect:", $el.attr("name"));
            return;
        }

        $feedback.text(message).show();
    }

    function clearField(fieldEl) {
        const $el = $(fieldEl);

        if (fieldEl.tomselect) {
            const $wrapper = $(fieldEl.tomselect.wrapper);
            $wrapper.removeClass("is-invalid");

            findFieldContainer($wrapper)
                .find(".invalid-feedback")
                .hide()
                .text("");
        } else {
            $el.removeClass("is-invalid");

            findFieldContainer($el)
                .find(".invalid-feedback")
                .hide()
                .text("");
        }
    }

    function clear(formSelector) {
        $(formSelector)
            .find(".is-invalid")
            .removeClass("is-invalid");

        $(formSelector)
            .find(".invalid-feedback")
            .hide()
            .text("");
    }

    return { apply, clear, clearField };

})();
