window.ValidationAdapter = (function () {

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
        $el.next(".invalid-feedback").text(message).show();
    }

    function markTomSelectInvalid($el, message) {
        const ts = $el[0].tomselect;
        if (!ts) return;

        const $wrapper = $(ts.wrapper);
        $wrapper.addClass("is-invalid");

        // Tìm container gần nhất (col / form-group)
        const $container = $wrapper.closest(
            ".col-md-6, .col-md-4, .col-md-2, .col-12, .form-group"
        );

        let $feedback = $container.find(".invalid-feedback").first();

        if ($feedback.length === 0) {
            console.warn("No invalid-feedback found for TomSelect:", $el.attr("name"));
            return;
        }

        $feedback.text(message).show();
    }


    function clearField(fieldEl) {
        const $el = $(fieldEl);

        // TomSelect
        if (fieldEl.tomselect) {
            const $wrapper = $(fieldEl.tomselect.wrapper);
            $wrapper.removeClass("is-invalid");

            $wrapper
                .closest(".col-md-6, .col-md-4, .col-12")
                .find(".invalid-feedback")
                .hide()
                .text("");
        }
        // Normal input
        else {
            $el.removeClass("is-invalid");
            $el.next(".invalid-feedback").hide().text("");
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
