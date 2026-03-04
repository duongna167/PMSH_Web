window.ValidationAdapter = (function () {

    /**
     * tự xóa lỗi 
     * 1. Đối với Input/Textarea thường(Sự kiện 'input')
        $(document).on("input", ".is-invalid", function () {
            window.ValidationAdapter.clearField(this);
        });

        // 2. Đối với TomSelect (Sự kiện 'change' trên thẻ select)
        $(document).on("change", "select.is-invalid, select.tomselect-custom", function () {
            window.ValidationAdapter.clearField(this);
        });

        // 3. Đối với Date Input (Lắng nghe thay đổi trên Hidden input)
        // DateInput của bạn cập nhật giá trị vào hidden input và trigger 'change'
        $(document).on("change", "[data-date-input] input[type='hidden']", function () {
            // Khi hidden thay đổi, ta xóa lỗi cho toàn bộ container Date đó
            window.ValidationAdapter.clearField(this);
        });

        // Backup cho Date UI nếu người dùng gõ tay
        $(document).on("input", ".date-ui", function () {
            window.ValidationAdapter.clearField(this);
        });
     */



    // Tự động tìm hoặc tạo mới thẻ invalid-feedback
    function getOrCreateFeedback($field) {
        // Xác định "điểm mốc" để chèn lỗi (nếu là group, date hoặc tomselect wrapper thì chèn sau khối đó)
        const $group = $field.closest(".input-group, [data-date-input], .ts-wrapper");
        const $base = $group.length > 0 ? $group : $field;

        // Tìm xem đã có thẻ feedback nào đi kèm chưa
        let $feedback = $base.next(".invalid-feedback");

        // Nếu chưa có thì tự động tạo và chèn vào ngay sau thành phần base
        if ($feedback.length === 0) {
            $feedback = $('<div class="invalid-feedback"></div>');
            $base.after($feedback);
        }

        return $feedback;
    }

    function apply(errors, formSelector) {
        clear(formSelector);
        const $container = $(formSelector);

        errors.forEach(err => {
            if (err.field === "general") return;

            // 1. Tìm field (Name -> ID -> Hậu tố ID)
            let $field = $container.find(`[name='${err.field}']`);
            if ($field.length === 0) $field = $container.find(`#${err.field}`);
            if ($field.length === 0) $field = $container.find(`[id$='_${err.field.toLowerCase()}']`);

            if ($field.length === 0) return;

            // 2. Đánh dấu lỗi cho UI
            $field.addClass("is-invalid");

            // Xử lý Input Group (như các trường có icon/button đi kèm)
            const $group = $field.closest(".input-group");
            if ($group.length > 0) $group.addClass("is-invalid");

            // Xử lý TomSelect
            if ($field[0].tomselect) {
                $($field[0].tomselect.wrapper).addClass("is-invalid");
            }

            // 3. Tự động chèn và hiển thị message
            const $feedback = getOrCreateFeedback($field);
            $feedback.html(err.message).show();
        });
    }

    function clear(formSelector) {
        const $form = $(formSelector);
        $form.find(".is-invalid").removeClass("is-invalid");
        $form.find(".invalid-feedback").hide().text("");

        $form.find("select.tomselect-custom").each(function () {
            if (this.tomselect) $(this.tomselect.wrapper).removeClass("is-invalid");
        });
    }

    function clearField(fieldEl) {
        const $field = $(fieldEl);
        // Xóa class lỗi cho chính nó và group cha (nếu có)
        $field.removeClass("is-invalid");
        $field.closest(".input-group, [data-date-input]").removeClass("is-invalid");

        if (fieldEl.tomselect) $(fieldEl.tomselect.wrapper).removeClass("is-invalid");

        // Tìm feedback liên quan để ẩn
        const $feedback = getOrCreateFeedback($field);
        $feedback.hide().text("");
    }

    return { apply, clear, clearField };
})();