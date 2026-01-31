window.FormModal = (function () {

    function open(modalId, formSelector, options = {}) {
        resetForm(formSelector);
        if (options.onOpen) options.onOpen();
        $(modalId).modal("show");
    }

    function close(modalId, formSelector) {
        resetForm(formSelector);
        $(modalId).modal("hide");
    }

    function resetForm(formSelector) {
        const $form = $(formSelector);
        if ($form.length === 0) return;

        $form[0].reset();

        $form.find("select").each(function () {
            if (this.tomselect) {
                this.tomselect.clear();
            }
        });

        ValidationAdapter.clear(formSelector);
    }


    return { open, close };

})();
