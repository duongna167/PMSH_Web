// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Hàm gọi để lấy business-date

function formatDate(date, format = "DD/MM/YYYY") {
  if (!date) return "";

  const d = new Date(date);
  if (isNaN(d)) return "";

  const day = String(d.getDate()).padStart(2, "0");
  const month = String(d.getMonth() + 1).padStart(2, "0");
  const year = d.getFullYear();

  return format.replace("DD", day).replace("MM", month).replace("YYYY", year);
}

function getAllBusinessDate(callback, displayFormat = "DD/MM/YYYY") {
  return $.ajax({
    url: "/Reservation/GetBusinessDate",
    type: "get",
    dataType: "json",
    success: function (result) {
      const businessDateISO = result.split("T")[0]; // YYYY-MM-DD
      const businessDateFormatted = formatDate(businessDateISO, displayFormat);

      $("[data-business-date]").each(function () {
        // Set cho input type="date" → luôn dùng ISO
        if (this.type === "date") {
          $(this).val(businessDateISO);
        } else {
          // Input text / span / label
          $(this).val?.(businessDateFormatted);
          $(this).text?.(businessDateFormatted);
        }
      });

      if (typeof callback === "function") {
        callback({
          iso: businessDateISO,
          formatted: businessDateFormatted,
        });
      }
    },
    error: function (xhr) {
      console.error("AJAX ERROR", xhr.status);
    },
  });
}

/*
function applyValidationErrors(errors, formSelector) {
  // Reset các lỗi cũ
  $(
    `${formSelector} .form-control, ${formSelector} select, ${formSelector} textarea`,
  ).removeClass("is-invalid");
  $(`${formSelector} .invalid-feedback`).text("");

  if (!errors || errors.length === 0) return;

  errors.forEach(function (err) {
    // Tìm input/select/textarea theo name
    let $field = $(`${formSelector} [name='${err.field}']`);
    if ($field.length) {
      $field.addClass("is-invalid");
      // Gán nội dung vào invalid-feedback ngay sau field
      $field.siblings(".invalid-feedback").text(err.message);
    }
  });
}
*/

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

  errors.forEach((err) => {
    const $field = $form.find(`[name='${err.field}']`);
    if (!$field.length) return;

    let $errorTarget = null; // element add is-invalid
    let $feedback = null; // nơi hiển thị message

    //  TomSelect
      if ($field[0].tomselect) {
          $errorTarget = $($field[0].tomselect.wrapper);
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
  return $el.siblings(".invalid-feedback").first().length
    ? $el.siblings(".invalid-feedback").first()
    : $el.closest("[class]").find(".invalid-feedback").first();
}

// Focus đúng element (kể cả TomSelect)
function focusElement($el) {
    //if ($el.hasClass("ts-wrapper")) {
    //    const select = $el.prev("select")[0];
    //    if (select?.tomselect) {
    //        select.tomselect.focus();
    //    }
    //} else {
        $el.focus();
    //}
}


//Init Input Date
(function () {
  async function getBusinessDateFromServer() {
    return await $.ajax({
      url: "/Reservation/GetBusinessDate",
      type: "get",
      dataType: "json",
    });
  }

  class DateInput {
    constructor(root) {
      this.$root = $(root); // Thẻ CHA
      this.$ui = this.$root.find(".date-ui"); // Thẻ CON
      // Kiểm tra nếu thẻ cha có thuộc tính business-date
      this.source = this.$root.data("source") || "default";

      this.isBusinessDate = this.$root.is("[business-date]");

      // TỰ ĐỘNG THÊM CLASS KHI KHỞI TẠO
      this.$root.addClass("date-input-group"); // Ví dụ class cho cha
      this.$ui.addClass("ms-input "); // Ví dụ class cho con

      this.name = this.$root.data("name");
      this.format = (this.$root.data("format") || "dd/mm/yyyy").toLowerCase();
      this.defaultISO = this.$root.data("default");
      // 1. Thêm class cho cha để làm mốc căn tọa độ
      this.$root.addClass("ms-input-wrap");

      // 2. Chèn SVG của bạn vào cuối thẻ cha
      const svgIcon = `
        <div class="ms-svg">
            <svg xmlns="http://www.w3.org/2000/svg" enable-background="new 0 0 24 24" height="20px" viewBox="0 0 24 24" width="20px" fill="currentColor">
                <g><rect fill="none" height="24" width="24"></rect></g>
                <g><path d="M17,2c-0.55,0-1,0.45-1,1v1H8V3c0-0.55-0.45-1-1-1S6,2.45,6,3v1H5C3.89,4,3.01,4.9,3.01,6L3,20c0,1.1,0.89,2,2,2h14 c1.1,0,2-0.9,2-2V6c0-1.1-0.9-2-2-2h-1V3C18,2.45,17.55,2,17,2z M19,20H5V10h14V20z M11,13c0-0.55,0.45-1,1-1s1,0.45,1,1 s-0.45,1-1,1S11,13.55,11,13z M7,13c0-0.55,0.45-1,1-1s1,0.45,1,1s-0.45,1-1,1S7,13.55,7,13z M15,13c0-0.55,0.45-1,1-1s1,0.45,1,1 s-0.45,1-1,1S15,13.55,15,13z M11,17c0-0.55,0.45-1,1-1s1,0.45,1,1s-0.45,1-1,1S11,17.55,11,17z M7,17c0-0.55,0.45-1,1-1 s1,0.45,1,1s-0.45,1-1,1S7,17.55,7,17z M15,17c0-0.55,0.45-1,1-1s1,0.45,1,1s-0.45,1-1,1S15,17.55,15,17z"></path></g>
            </svg>
        </div>`;
      this.$root.append(svgIcon);
      // Tạo input hidden để lưu giá trị YYYY-MM-DD
      this.$hidden = $('<input type="hidden">')
        .attr("name", this.name)
        .attr("id", this.name)
        .appendTo(this.$root);

      this.initCalendar();

      // LOGIC TỰ ĐỘNG CHÈN NGÀY
      if (this.isBusinessDate) {
        this.loadAndSetBusinessDate();
      } else if (this.defaultISO) {
        this.setISO(this.defaultISO);
      }
    }

    initCalendar() {
      const self = this;

      // Khởi tạo thư viện (Sẽ sinh ra HTML có class .datepicker-container)
      this.$ui.datepicker({
        autoHide: true,
        format: self.format,
        zIndex: 2048,

        // Thêm class tạm thời vào CHA khi đang mở lịch
        show: function () {
          self.$root.addClass("datepicker-active");
        },
        hide: function () {
          self.$root.removeClass("datepicker-active");
        },

        // Khi người dùng click chọn ngày (li.picked)
        pick: function (e) {
          const date = e.date;
          const y = date.getFullYear();
          const m = String(date.getMonth() + 1).padStart(2, "0");
          const d = String(date.getDate()).padStart(2, "0");

          self.$hidden.val(`${y}-${m}-${d}`);
        },
      });
      // BẮT SỰ KIỆN CLICK VÀO ICON ĐỂ MỞ LỊCH
      this.$root.find(".ms-svg").on("click", function (e) {
        e.stopPropagation();

        // KIỂM TRA: Nếu input có thuộc tính disabled hoặc readonly thì không làm gì cả
        if (self.$ui.is(":disabled") || self.$ui.prop("readonly")) {
          return;
        }

        self.$ui.datepicker("show");
      });

      // Chống đóng lịch khi click vào input (nổi bọt)
      this.$ui.on("click", (e) => e.stopPropagation());
    }

    async loadAndSetBusinessDate() {
      try {
        const result = await getBusinessDateFromServer();
        const businessDateISO = result.split("T")[0];
        console.log(
          `[BusinessDate] Autoload cho ${this.name}: ${businessDateISO}`,
        );
        this.setISO(businessDateISO);
      } catch (err) {
        console.error("Không thể lấy Business Date:", err);
      }
    }

    setISO(iso) {
      if (!iso || typeof iso !== "string") return; // Đảm bảo iso là chuỗi

      const datePart = iso.split("T")[0]; // Lấy "2025-10-25"
      const parts = datePart.split("-");

      if (parts.length === 3) {
        // Tạo đối tượng ngày tháng chuẩn
        const year = parseInt(parts[0]);
        const month = parseInt(parts[1]) - 1;
        const day = parseInt(parts[2]);
        const dateObj = new Date(year, month, day);

        if (!isNaN(dateObj.getTime())) {
          // Cập nhật Plugin (Hiển thị text)
          this.$ui.datepicker("setDate", dateObj);

          // Cập nhật Hidden Input (Giá trị để submit)
          if (this.$hidden) {
            this.$hidden.val(datePart);
          }
        }
      }
    }
  }

  window.initDateInputs = function (root) {
    const $root = root ? $(root) : $(document);
    const tasks = [];

    $root.find("[data-date-input]").each(function () {
      if ($(this).data("date-initialized")) return;
      const instance = new DateInput(this);
      this.dateInput = instance; // native DOM
      $(this).data("dateInput", instance); // jQuery-safe
      $(this).data("date-initialized", true);
    });
  };
})();

/**
 * Reload Business Date cho tất cả hoặc 1 date input cụ thể
 * @param {string|null} name - name của date input (hidden input)
 */
window.reloadBusinessDate = async function (name = null) {
  const $targets = name
    ? $(`[data-date-input][data-name="${name}"]`)
    : $("[data-date-input]");

  if (!$targets.length) {
    console.warn("[reloadBusinessDate] Không tìm thấy date input");
    return;
  }

  for (const el of $targets) {
    const instance = $(el).data("dateInput");
    if (instance && typeof instance.loadAndSetBusinessDate === "function") {
      await instance.loadAndSetBusinessDate();
    }
  }
};

// Thêm helper này (có thể đặt ở global scope hoặc trong file chính)
function waitForBusinessDate(name = "fromDate", timeoutMs = 8000) {
  return new Promise((resolve, reject) => {
    const $hidden = $(`input[type=hidden][name="${name}"]`);

    if ($hidden.val() && /^\d{4}-\d{2}-\d{2}$/.test($hidden.val())) {
      return resolve($hidden.val());
    }

    const start = Date.now();
    const interval = setInterval(() => {
      const val = $hidden.val();
      if (val && /^\d{4}-\d{2}-\d{2}$/.test(val)) {
        clearInterval(interval);
        resolve(val);
      }
      if (Date.now() - start > timeoutMs) {
        clearInterval(interval);
        reject(new Error(`Timeout chờ business date cho ${name}`));
      }
    }, 100);
  });
}

/**
 * UI.setHiddenDate
 * ----------------
 * Set giá trị cho input hidden (YYYY-MM-DD) và tự động đồng bộ
 * lại input hiển thị (datepicker UI) nếu đã được khởi tạo.
 *
 * Mục đích:
 * - Dùng cho dữ liệu ngày trả về từ AJAX / API
 * - Không phụ thuộc vào DateInput instance
 * - Gọi được trước hoặc sau khi datepicker init
 *
 * @param {string} name
 *   Tên của date input (trùng với:
 *   - data-name="..."
 *   - name="..."
 *   - id="..." của input hidden)
 *
 * @param {string} iso
 *   Chuỗi ngày dạng:
 *   - "YYYY-MM-DD"
 *   - "YYYY-MM-DDTHH:mm:ss"
 *
 * @example
 *   UI.setHiddenDate("arrivalInvoice", "2022-11-04T00:00:00");
 *   UI.setHiddenDate("departureInvoice", "2022-11-05");
 */
window.UI = window.UI || {};

UI.setHiddenDate = function (name, iso) {
  if (!name || !iso || typeof iso !== "string") return;

  // 1. Chuẩn hóa ISO → yyyy-mm-dd
  const datePart = iso.split("T")[0];
  const parts = datePart.split("-");

  if (parts.length !== 3) return;

  const year = parseInt(parts[0], 10);
  const month = parseInt(parts[1], 10) - 1;
  const day = parseInt(parts[2], 10);

  const dateObj = new Date(year, month, day);
  if (isNaN(dateObj.getTime())) return;

  // 2. Set giá trị cho input hidden
  const $hidden = $(`input[type="hidden"][name="${name}"]`);
  if (!$hidden.length) return;

  $hidden.val(datePart);

  // 3. Đồng bộ lại UI datepicker nếu đã init
  const $wrapper = $hidden.closest("[data-date-input]");
  const $ui = $wrapper.find(".date-ui");

  if ($ui.length && $ui.data("datepicker")) {
    $ui.datepicker("setDate", dateObj);
  }
};

/**
 * UI.addDaysIso
 * ----------------
 * Nhận ISO
 *  Trả ISO
 *  Không liên quan UI
 *  Không phụ thuộc locale
 * Mục đích:
 * - Dùng cho tính toán ngày Night
 * */
UI.addDaysIso = function (isoDate, days) {
  const [y, m, d] = isoDate.split("-").map(Number);
  const date = new Date(y, m - 1, d + 1);
  date.setDate(date.getDate() + days);
  return date.toISOString().split("T")[0];
};

// Hàm nào cần hãy gọi để luôn sẵn sàng
$(document).ready(async function () {
  var tooltipTriggerList = [].slice.call(
    document.querySelectorAll('[data-bs-toggle="tooltip"]'),
  );
  var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
    return new bootstrap.Tooltip(tooltipTriggerEl);
  });
  await window.initDateInputs();
});
