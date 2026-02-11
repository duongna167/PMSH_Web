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
            } // date input
            else if (isDateInput($field)) {
                markDateInputInvalid($field, err.message);
            } // field normal
            else {
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

        //date input
        if ($el.closest("[data-date-input]").length > 0) {
            const parts = getDateInputElements($el);
            parts.textInput.removeClass("is-invalid");
            parts.feedback.hide().text("");
            return;
        }

        // tomselect
        if (fieldEl.tomselect) {
            const $wrapper = $(fieldEl.tomselect.wrapper);
            $wrapper.removeClass("is-invalid");

            findFieldContainer($wrapper)
                .find(".invalid-feedback")
                .hide()
                .text("");
        // normal
        } else {
            $el.removeClass("is-invalid");

            findFieldContainer($el)
                .find(".invalid-feedback")
                .hide()
                .text("");
        }
    }

    // truyền id thẻ form vd: <form id="ooos_formContainer">
    function clear(formSelector) {
        const $form = $(formSelector);

        // input thường
        $form.find(".is-invalid").removeClass("is-invalid");

        // TomSelect wrapper
        $form.find("select.tomselect-custom").each(function () {
            if (this.tomselect) {
                $(this.tomselect.wrapper).removeClass("is-invalid");
            }
        });

        $(formSelector)
            .find(".invalid-feedback")
            .hide()
            .text("");
    }

    function isDateInput($el) {
        return $el.closest("[data-date-input]").length > 0;
    }

    function getDateInputElements($el) {
        const $wrapper = $el.closest("[data-date-input]");
        return {
            wrapper: $wrapper,
            textInput: $wrapper.find("input.date-ui").first(),
            feedback: $wrapper.next(".invalid-feedback")
        };
    }

    function markDateInputInvalid($el, message) {
        const parts = getDateInputElements($el);
        if (!parts.wrapper || parts.wrapper.length === 0) return;

        // mark input invalid
        parts.textInput.addClass("is-invalid");

        // show message
        if (parts.feedback && parts.feedback.length > 0) {
            parts.feedback.text(message).show();
        } else {
            console.warn("No invalid-feedback found for date input:", $el.attr("name"));
        }
    }



    return { apply, clear, clearField };

})();
