// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Hàm gọi để lấy business-date

function getAllBusinessDate(callback) {
  return $.ajax({
    url: "/Reservation/GetBusinessDate",
    type: "get",
    dataType: "json",
    success: function (result) {
      const businessDate = result.split("T")[0];
      $("[data-business-date]").each(function () {
        $(this).val(businessDate);
      });

      if (typeof callback === "function") {
        callback(businessDate);
      }
    },
    error: function (xhr) {
      console.error(" AJAX ERROR", xhr.status);
    },
  });
}
//Hàm validation
/**
 * Áp dụng lỗi JSON vào form bất kỳ với Bootstrap validation
 * @param {Array} errors - Array {field, message} từ backend
 * @param {string} formSelector - selector của form/modal
 */
function applyValidationErrors(errors, formSelector) {

    const $form = $(formSelector);

    // Reset lỗi cũ
    $form.find(".is-invalid").removeClass("is-invalid");
    $form.find(".invalid-feedback").text("").hide();

    if (!errors || !errors.length) return;

    let firstInvalidElement = null;

    errors.forEach(err => {

        const $field = $form.find(`[name='${err.field}']`);
        if (!$field.length) return;

        let $errorTarget = null; // element add is-invalid
        let $feedback = null;    // nơi hiển thị message

        //  TomSelect
        if ($field[0].tomselect) {
            $errorTarget = $field.next(".ts-wrapper");
            $feedback = findFeedback($errorTarget);
        }
        //  Input / textarea / select thường
        else {
            $errorTarget = $field;
            $feedback = findFeedback($field);
        }

        if ($errorTarget) {
            $errorTarget.addClass("is-invalid");

            if (!firstInvalidElement) {
                firstInvalidElement = $errorTarget;
            }
        }

        if ($feedback) {
            $feedback.text(err.message).show();
        }
    });

    //  Focus field lỗi đầu tiên
    if (firstInvalidElement) {
        focusElement(firstInvalidElement);
    }
}

// Tìm invalid-feedback gần nhất, KHÔNG phụ thuộc bootstrap
function findFeedback($el) {
    // Ưu tiên: sibling → parent → gần nhất trong form
    return (
        $el.siblings(".invalid-feedback").first().length
            ? $el.siblings(".invalid-feedback").first()
            : $el.closest("[class]").find(".invalid-feedback").first()
    );
}

// Focus đúng element (kể cả TomSelect)
function focusElement($el) {
    if ($el.hasClass("ts-wrapper")) {
        // TomSelect
        const select = $el.prev("select")[0];
        if (select && select.tomselect) {
            select.tomselect.focus();
        }
    } else {
        $el.focus();
    }
}



$(document).ready(function () {
  getAllBusinessDate();
  var tooltipTriggerList = [].slice.call(
    document.querySelectorAll('[data-bs-toggle="tooltip"]')
  );
  var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
    return new bootstrap.Tooltip(tooltipTriggerEl);
  });
});
