    var _folioOption = { folioId: 0, invoiceNos: [] };

    /**
     * Mở Folio Option (frmCshFolioOption).
     * invoiceNos: InvoiceNo các giao dịch đang chọn (khi mở từ màn Billing) -> dùng cho "Folio Information <selected transaction>".
     */
    function openModalFolioOption(folioId, invoiceNos) {
        folioId = parseInt(folioId, 10) || 0;
        const selectedInvoices = (invoiceNos || []).map(function (x) { return String(x).trim(); }).filter(Boolean);
        if (folioId <= 0) {
            showToast('Warning', 'Please choose a folio', false);
            return;
        }
        $.ajax({
            url: '/Billing/GetFolioOptionInfo',
            type: 'GET',
            data: { folioId: folioId },
            success: function (res) {
                if (!res || res.code !== 0) {
                    showToast('Error', (res && res.msg) || 'Cannot load folio.', false);
                    return;
                }
                _folioOption = { folioId: res.folioId, invoiceNos: selectedInvoices };
                $('#folioOptionTitle').text('Folio Options - Folio No [' + res.folioId + ']');
                $('#folioOptionInfo').html('Window: <b>' + escapeFolioOption(res.window) + '</b> &nbsp;|&nbsp; Account: <b>' + escapeFolioOption(res.accountName || '') + '</b>');
                $('#foVietnamese').prop('checked', false);
                // SetMode(1) bên WinForm: mở từ Billing có chọn giao dịch -> chọn sẵn kiểu selected transaction
                $(selectedInvoices.length ? '#foStyle_BILLING_INFOR_BY_SELECTED' : '#foStyle_BILLING_INFOR').prop('checked', true);
                $('#foVat_FOLIO_VAT_GUEST').prop('checked', true);
                $('#foPrintForGuest').prop('checked', true);
                $('#foGuestVND').prop('checked', true);
                $('#foLocalVND').prop('checked', true);
                refreshFolioOptionPanels();
                $('#modalFolioOption').modal('show');
            },
            error: function () { showToast('Error', 'Cannot load folio.', false); }
        });
    }

    function closeModalFolioOption() {
        $('#modalFolioOption').modal('hide');
    }

    // pnlVAT.Enabled = rdbFolioVat.Checked; pnlGuest / pnlLocal theo Print for Guest / Local
    function refreshFolioOptionPanels() {
        const isVat = $('#foStyle_FOLIO_VAT').is(':checked');
        const forGuest = $('#foPrintForGuest').is(':checked');
        setFolioOptionEnabled($('#foVatPanel').find('input'), isVat);
        if (isVat) {
            setFolioOptionEnabled($('#foGuestPanel input'), forGuest);
            setFolioOptionEnabled($('#foLocalPanel input'), !forGuest);
        }
    }

    function setFolioOptionEnabled($inputs, enabled) {
        $inputs.prop('disabled', !enabled).closest('.fo-opt').toggleClass('is-disabled', !enabled);
    }

    $(document).on('change', '#modalFolioOption input[name=foStyle], #modalFolioOption input[name=foPrintFor]', refreshFolioOptionPanels);

    function getFolioOptionStyle() {
        const style = $('#modalFolioOption input[name=foStyle]:checked').val();
        if (style === 'FOLIO_VAT') return $('#modalFolioOption input[name=foVat]:checked').val();
        return style;
    }

    function getFolioOptionCashier() {
        let cashier = '';
        try { cashier = JSON.parse(localStorage.getItem('Loginname') || '""') || ''; } catch (e) { }
        return cashier;
    }

    function folioOptionPreview() {
        const style = getFolioOptionStyle();
        if (!style) return;
        if (style.indexOf('FOLIO_VAT_') === 0) {
            openFolioVatInfo(style);
            return;
        }
        if (style === 'BILLING_INFOR_BY_SELECTED' && _folioOption.invoiceNos.length === 0) {
            showToast('Warning', 'Please select transactions on Billing screen first.', false);
            return;
        }
        const cashier = getFolioOptionCashier();
        $.ajax({
            url: '/Billing/FolioOptionPrint',
            type: 'POST',
            data: {
                folioId: _folioOption.folioId,
                style: style,
                vietnamese: $('#foVietnamese').is(':checked'),
                cashier: cashier,
                user: cashier,
                invoiceNos: (_folioOption.invoiceNos || []).join(',')
            },
            success: function (res) {
                if (!res || res.code !== 0) {
                    showToast('Error', (res && res.msg) || 'Cannot print folio.', false);
                    return;
                }
                const renderer = FOLIO_OPTION_RENDERERS[res.data.style];
                if (!renderer) {
                    showToast('Error', 'This print style is not available.', false);
                    return;
                }
                openFolioPrintWindow(renderer(res.data), false);
            },
            error: function () { showToast('Error', 'Cannot print folio.', false); }
        });
    }

    // ===== Folio VAT: hộp thoại thông tin hóa đơn (frmCshVATInfor) =====
    var _folioVatStyle = '';

    function openFolioVatInfo(style) {
        // WinForm: Print for Local mà không chọn loại tiền -> không làm gì
        if ($('#foPrintForLocal').is(':checked') && !$('#foLocalVND').is(':checked')) return;
        _folioVatStyle = style;
        $.ajax({
            url: '/Billing/GetFolioVatInfo',
            type: 'GET',
            data: { folioId: _folioOption.folioId },
            success: function (res) {
                if (!res || res.code !== 0) {
                    showToast('Error', (res && res.msg) || 'Cannot load VAT information.', false);
                    return;
                }
                const d = res.data || {};
                const noName = style === 'FOLIO_VAT_GUEST_NONAME';
                const summary = style === 'FOLIO_VAT_GUEST_SUMMARY';
                $('#fvFolioNo').val(d.folioNo || '');
                $('#fvArrival').val(d.arrival || '');
                $('#fvDeparture').val(d.departure || '');
                $('#fvAdult').val(d.adult || '');
                $('#fvGuestName').val(noName ? '' : (d.guestName || '')).prop('disabled', noName);
                $('#fvCompany').val(d.company || '');
                $('#fvTaxNumber').val(d.taxNumber || '');
                $('#fvAddress').val(d.address || '');
                $('#fvInvoiceNo').val('');
                $('#fvSerialNo').val('');
                $('#fvDescription').val(summary ? 'Dịch vụ khách sạn' : '');
                $('#fvDescriptionLabel').text(summary ? 'Description *' : 'Description');
                $('#fvPrintedWarn').text(res.printedMessage || '').toggle(!res.allowPrinting);
                $('#modalFolioVatInfo').modal('show');
            },
            error: function () { showToast('Error', 'Cannot load VAT information.', false); }
        });
    }

    function folioVatInfoOk() {
        if (_folioVatStyle === 'FOLIO_VAT_GUEST_SUMMARY' && !$('#fvDescription').val().trim()) {
            showToast('Warning', 'Please input description.', false);
            return;
        }
        $.ajax({
            url: '/Billing/FolioOptionPrintVat',
            type: 'POST',
            data: {
                folioId: _folioOption.folioId,
                style: _folioVatStyle,
                printForLocal: $('#foPrintForLocal').is(':checked'),
                cashier: getFolioOptionCashier(),
                'info.FolioNo': $('#fvFolioNo').val(),
                'info.Arrival': $('#fvArrival').val(),
                'info.Departure': $('#fvDeparture').val(),
                'info.Adult': $('#fvAdult').val(),
                'info.GuestName': $('#fvGuestName').val(),
                'info.Company': $('#fvCompany').val(),
                'info.TaxNumber': $('#fvTaxNumber').val(),
                'info.Address': $('#fvAddress').val(),
                'info.InvoiceNo': $('#fvInvoiceNo').val(),
                'info.SerialNo': $('#fvSerialNo').val(),
                'info.Description': $('#fvDescription').val()
            },
            success: function (res) {
                if (!res || res.code !== 0) {
                    showToast('Error', (res && res.msg) || 'Cannot print VAT invoice.', false);
                    return;
                }
                $('#modalFolioVatInfo').modal('hide');
                openFolioPrintWindow(renderFolioVat(res.data), false);
            },
            error: function () { showToast('Error', 'Cannot print VAT invoice.', false); }
        });
    }

    // ===== Print History (frmPrintVATSearch) =====
    function openFolioPrintHistory() {
        $.ajax({
            url: '/Billing/GetFolioPrintVatHistory',
            type: 'GET',
            data: { folioId: _folioOption.folioId },
            success: function (res) {
                if (!res || res.code !== 0) {
                    showToast('Error', (res && res.msg) || 'Cannot load print history.', false);
                    return;
                }
                const e = escapeFolioOption;
                const cols = res.columns || [];
                const rows = res.rows || [];
                let html = '<table class="table table-sm table-bordered mb-0"><thead><tr>';
                cols.forEach(function (c) { html += '<th>' + e(c) + '</th>'; });
                html += '</tr></thead><tbody>';
                if (rows.length === 0) {
                    html += '<tr><td colspan="' + Math.max(cols.length, 1) + '" class="text-center text-muted">No data</td></tr>';
                }
                rows.forEach(function (r) {
                    html += '<tr>' + r.map(function (v) { return '<td>' + e(v) + '</td>'; }).join('') + '</tr>';
                });
                html += '</tbody></table>';
                $('#folioPrintHistoryTitle').text('Print History - Folio No [' + _folioOption.folioId + ']');
                $('#folioPrintHistoryBody').html(html);
                $('#modalFolioPrintHistory').modal('show');
            },
            error: function () { showToast('Error', 'Cannot load print history.', false); }
        });
    }

    /**
     * Hóa đơn VAT (PrintFolio_GuestVAT[_Detail]_VND / PrintFolio_GuestVAT_Local_VND).
     * Guest VAT Detail có thêm cột Đơn giá. AllowPrinting = false -> chỉ xem, không cho in.
     */
    function renderFolioVat(d) {
        const e = escapeFolioOption;
        const n = function (v) { v = Number(v) || 0; return Math.round(v).toLocaleString('en-US'); };
        const detail = d.style === 'FOLIO_VAT_GUEST_DETAIL' && d.mode === 'GUEST';
        const v5 = d.lineVAT5 || [0, 0, 0, 0, 0], v10 = d.lineVAT10 || [0, 0, 0, 0, 0], ln = d.line || [0, 0, 0, 0, 0];

        let html = '<div class="hotel">' + e(d.hotelName) + '</div>';
        html += '<div class="title">HÓA ĐƠN DỊCH VỤ / VAT INVOICE' + (d.mode === 'LOCAL' ? ' (LOCAL)' : '') + '</div>';
        if (!d.allowPrinting) {
            html += '<div style="border:1px solid #f59e0b;background:#fef3c7;color:#92400e;padding:6px 10px;border-radius:6px;margin-bottom:10px">' + e(d.printedMessage || 'Folio were printed invoice VAT.') + '</div>';
        }
        html += '<table class="meta">' +
            '<tr><td class="k">Tên khách / Guest</td><td class="v">' + e(d.guestName) + '</td><td class="k">Phòng / Room</td><td class="v">' + e(d.room) + '</td></tr>' +
            '<tr><td class="k">Công ty / Company</td><td class="v">' + e(d.company) + '</td><td class="k">Ngày đến / Arrival</td><td class="v">' + e(d.arrival) + '</td></tr>' +
            '<tr><td class="k">Mã số thuế / Tax No</td><td class="v">' + e(d.taxNumber) + '</td><td class="k">Ngày đi / Departure</td><td class="v">' + e(d.departure) + '</td></tr>' +
            '<tr><td class="k">Địa chỉ / Address</td><td class="v">' + e(d.address) + '</td><td class="k">Số khách / Adult</td><td class="v">' + e(d.adult) + '</td></tr>' +
            '<tr><td class="k">Folio No</td><td class="v">' + e(d.folioNo) + '</td><td class="k">Confirm No</td><td class="v">' + e(d.confirmNo) + '</td></tr>' +
            '<tr><td class="k">CRS No</td><td class="v">' + e(d.crsNo) + '</td><td class="k">Loại tiền / Currency</td><td class="v">' + e(d.currency) + '</td></tr>' +
            (d.invoiceNo || d.serialNo ? '<tr><td class="k">Invoice No</td><td class="v">' + e(d.invoiceNo) + '</td><td class="k">Serial No</td><td class="v">' + e(d.serialNo) + '</td></tr>' : '') +
            '</table>';

        html += '<table class="lines"><thead><tr>' +
            '<th style="width:90px">Ngày/Date</th><th>Diễn giải/Description</th>' +
            '<th class="num" style="width:80px">SL/Qty</th>' +
            (detail ? '<th class="num" style="width:100px">Đơn giá/Price</th>' : '') +
            '<th class="num" style="width:110px">Thành tiền/Amount</th>' +
            '<th class="num" style="width:90px">Phí DV/SVC</th>' +
            '<th class="num" style="width:90px">VAT</th>' +
            '<th class="num" style="width:110px">Tổng/Total</th>' +
            '</tr></thead><tbody>';
        (d.rows || []).forEach(function (r) {
            const qty = Number(r.quantity) ? (n(r.quantity) + (r.unitQty ? ' ' + e(r.unitQty) : '')) : '';
            html += '<tr><td>' + e(r.date) + '</td><td>' + e(r.description) + '</td>' +
                '<td class="num">' + qty + '</td>' +
                (detail ? '<td class="num">' + (Number(r.price) ? n(r.price) : '') + '</td>' : '') +
                '<td class="num">' + n(r.amount) + '</td><td class="num">' + n(r.svc) + '</td>' +
                '<td class="num">' + n((Number(r.vat5) || 0) + (Number(r.vat10) || 0)) + '</td><td class="num">' + n(r.totalAmount) + '</td></tr>';
        });
        html += '</tbody></table>';

        // Khối tổng theo thuế suất (txtNet_VAT5 .. txtTotalGross)
        html += '<table class="lines" style="margin-top:10px;width:auto;margin-left:auto"><thead><tr>' +
            '<th></th><th class="num">Chưa thuế/Net</th><th class="num">Phí DV/SVC</th><th class="num">VAT</th><th class="num">Tổng/Gross</th></tr></thead><tbody>' +
            '<tr><td>VAT 5%</td><td class="num">' + n(v5[0]) + '</td><td class="num">' + n(v5[1]) + '</td><td class="num">' + n(v5[2]) + '</td><td class="num">' + n(v5[4]) + '</td></tr>' +
            '<tr><td>VAT 10%</td><td class="num">' + n(v10[0]) + '</td><td class="num">' + n(v10[1]) + '</td><td class="num">' + n(v10[3]) + '</td><td class="num">' + n(v10[4]) + '</td></tr>' +
            '<tr style="font-weight:700"><td>Tổng cộng/Total</td><td class="num">' + n(ln[0]) + '</td><td class="num">' + n(ln[1]) + '</td><td class="num">' + n((Number(ln[2]) || 0) + (Number(ln[3]) || 0)) + '</td><td class="num">' + n(ln[4]) + '</td></tr>' +
            '</tbody></table>';

        html += '<div class="words">' +
            '<div>Tỷ giá / Exchange rate: <b>' + e(d.exchange) + '</b></div>' +
            '<div>Tổng tiền thanh toán (VND) / Total amount: <b>' + e(d.amountVND) + '</b></div>' +
            '<div>Số tiền bằng chữ / In words: <i>' + e(d.amountVNDToWord) + '</i></div>' +
            (d.description && d.style !== 'FOLIO_VAT_GUEST_SUMMARY' ? '<div>Ghi chú / Note: ' + e(d.description) + '</div>' : '') +
            '</div>';
        html += '<div class="sign"><div>Khách hàng / Guest<div class="line"></div></div><div>Thu ngân / Cashier<div class="line">' + e(d.cashier) + '</div></div></div>';
        html += '<div class="foot"><span>' + e(d.date) + '</span><span>' + e(d.hotelName) + '</span></div>';

        let page = folioPrintPage('VAT Invoice - Folio ' + (d.folioNo || ''), html);
        if (!d.allowPrinting) {
            // CheckValidPrintVAT(false): folio đã in hóa đơn VAT -> chỉ cho xem
            page = page.replace('<div class="no-print"><button onclick="window.print()">Print</button></div>', '')
                .replace('</style>', '@media print { body { display: none; } }</style>');
        }
        return page;
    }

    function escapeFolioOption(s) {
        return String(s == null ? '' : s)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    function openFolioPrintWindow(html, autoPrint) {
        const w = window.open('', '_blank', 'width=900,height=1000');
        if (!w) {
            showToast('Warning', 'Trình duyệt đang chặn cửa sổ in, hãy cho phép popup.', false);
            return;
        }
        w.document.open();
        w.document.write(html);
        w.document.close();
        if (autoPrint) {
            // document.write không chắc bắn onload -> đợi render xong rồi in
            setTimeout(function () { try { w.focus(); w.print(); } catch (e) { } }, 400);
        }
    }

    // ===== Khung trang in chung (A4) =====
    function folioPrintPage(title, body) {
        return '<!doctype html><html><head><meta charset="utf-8"><title>' + escapeFolioOption(title) + '</title>' +
            '<style>' +
            '@page { size: A4; margin: 12mm 12mm 14mm; }' +
            'body { font-family: "Segoe UI", Arial, sans-serif; font-size: 12px; color: #111; margin: 0; padding: 16px; }' +
            '@media print { body { padding: 0; } .no-print { display: none; } }' +
            '.no-print { text-align: right; margin-bottom: 10px; }' +
            '.no-print button { padding: 6px 16px; border: 0; border-radius: 6px; background: #63af90; color: #fff; font-weight: 600; cursor: pointer; }' +
            '.hotel { text-align: center; font-size: 16px; font-weight: 700; }' +
            '.title { text-align: center; font-size: 15px; font-weight: 700; margin: 6px 0 14px; letter-spacing: .5px; }' +
            '.meta { width: 100%; border-collapse: collapse; margin-bottom: 12px; }' +
            '.meta td { padding: 2px 4px; vertical-align: top; }' +
            '.meta td.k { width: 110px; color: #444; white-space: nowrap; }' +
            '.meta td.v { font-weight: 600; }' +
            'table.lines { width: 100%; border-collapse: collapse; }' +
            'table.lines th { border-top: 1px solid #000; border-bottom: 1px solid #000; padding: 5px 4px; text-align: left; font-weight: 700; }' +
            'table.lines td { padding: 3px 4px; vertical-align: top; }' +
            'table.lines .num { text-align: right; white-space: nowrap; }' +
            'table.lines tbody tr:last-child td { border-bottom: 1px solid #000; }' +
            'table.sum { width: 100%; border-collapse: collapse; margin-top: 8px; }' +
            'table.sum td { padding: 2px 4px; }' +
            'table.sum .num { text-align: right; white-space: nowrap; width: 130px; }' +
            'table.sum tr.sep td.num, table.sum tr.sep td.lbl { border-bottom: 1px solid #000; }' +
            'table.sum tr.total td { font-weight: 700; padding-top: 4px; }' +
            '.words { margin-top: 14px; line-height: 1.7; }' +
            '.sign { display: flex; justify-content: space-between; margin-top: 40px; text-align: center; }' +
            '.sign div { width: 40%; }' +
            '.sign .line { margin-top: 60px; border-top: 1px dotted #000; padding-top: 4px; }' +
            '.foot { margin-top: 18px; font-size: 10.5px; color: #555; display: flex; justify-content: space-between; }' +
            '</style></head><body>' +
            '<div class="no-print"><button onclick="window.print()">Print</button></div>' +
            body + '</body></html>';
    }

    // ===== Renderer cho từng kiểu in =====
    var FOLIO_OPTION_RENDERERS = {
        BILLING_INFOR: function (d) { return renderFolioBillingFamily(d, { title: ['FOLIO INFORMATION', 'THÔNG TIN THANH TOÁN'] }); },
        BILLING_INFOR_QUANTITY: function (d) { return renderFolioBillingFamily(d, { title: ['FOLIO INFORMATION', 'THÔNG TIN THANH TOÁN'], quantity: true }); },
        BILLING_INFOR_BY_SELECTED: function (d) { return renderFolioBillingFamily(d, { title: ['FOLIO INFORMATION (SELECTED TRANSACTION)', 'THÔNG TIN THANH TOÁN (GIAO DỊCH ĐÃ CHỌN)'] }); },
        FOLIO_BY_DATE: function (d) { return renderFolioBillingFamily(d, { title: ['FOLIO INFORMATION BY DATE', ''], groupBy: 'date' }); },
        FOLIO_SUMMARY: function (d) { return renderFolioBillingFamily(d, { title: ['FOLIO SUMMARY', ''], groupBy: 'trans' }); },
        FOLIO_DETAIL: renderFolioDetail
    };

    // Định dạng số giống WinForm: VND "###,###,###" (0 -> rỗng), USD "###,###,###.00"
    function fmtFolioVND(v) { v = Number(v) || 0; return v === 0 ? '' : Math.round(v).toLocaleString('en-US'); }
    function fmtFolioUSD(v) { v = Number(v) || 0; return v === 0 ? '' : v.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }

    function folioLabel(vn, en, vi) { return vn && vi ? vi + ' / ' + en : en; }

    // Khối thông tin khách / folio dùng chung
    function renderFolioHeaderBlock(h, vn, title) {
        const e = escapeFolioOption;
        const L = function (en, vi) { return folioLabel(vn, en, vi); };
        let html = '<div class="hotel">' + e(h.hotelName) + '</div>';
        html += '<div class="title">' + e(vn && title[1] ? title[1] + ' / ' + title[0] : title[0]) + '</div>';
        html += '<table class="meta"><tr>' +
            '<td class="k">' + L('Name', 'Tên khách') + '</td><td class="v">' + e(h.name) + '</td>' +
            '<td class="k">' + L('Folio No', 'Số folio') + '</td><td class="v">' + e(h.folioNo) + '</td></tr>' +
            '<tr><td class="k">' + L('Address', 'Địa chỉ') + '</td><td class="v">' + e(h.address) + '</td>' +
            '<td class="k">' + L('Confirm No', 'Số xác nhận') + '</td><td class="v">' + e(h.confirmNo) + '</td></tr>' +
            '<tr><td class="k">' + L('Company', 'Công ty') + '</td><td class="v">' + e(h.company) + '</td>' +
            '<td class="k">' + L('Room', 'Phòng') + '</td><td class="v">' + e(h.room) + '</td></tr>' +
            '<tr><td class="k">' + L('Group', 'Đoàn') + '</td><td class="v">' + e(h.group) + '</td>' +
            '<td class="k">' + L('Arrival', 'Ngày đến') + '</td><td class="v">' + e(h.arrival) + '</td></tr>' +
            '<tr><td class="k">' + L('Membership', 'Thẻ thành viên') + '</td><td class="v">' + e(h.memberShip) + '</td>' +
            '<td class="k">' + L('Departure', 'Ngày đi') + '</td><td class="v">' + e(h.departure) + '</td></tr>' +
            '<tr><td class="k">' + L('A/R No', 'Số A/R') + '</td><td class="v">' + e(h.ar) + '</td>' +
            '<td class="k">' + L('Cashier', 'Thu ngân') + '</td><td class="v">' + e(h.cashier) + '</td></tr>' +
            '</table>';
        return html;
    }

    function renderFolioSummaryBlock(d) {
        const e = escapeFolioOption;
        let html = '<table class="sum">';
        (d.summary || []).forEach(function (s) {
            const cls = s.isSeparator ? 'sep' : (s.isTotal ? 'total' : '');
            html += '<tr class="' + cls + '"><td style="width:85px">' + e(s.date) + '</td>' +
                '<td class="lbl">' + (s.isSeparator ? '' : e(s.label)) + '</td>' +
                '<td class="num">' + e(s.vnd) + '</td><td class="num" style="width:110px">' + e(s.usd) + '</td></tr>';
        });
        html += '</table>';
        return html;
    }

    function renderFolioTotalsAndSign(d, vn) {
        const e = escapeFolioOption;
        const h = d.header || {};
        const L = function (en, vi) { return folioLabel(vn, en, vi); };
        let html = '<div class="words">' +
            '<div>' + L('Exchange rate', 'Tỷ giá') + ': <b>' + e(d.exchange) + '</b></div>' +
            '<div>' + L('Total VND', 'Tổng VND') + ': <b>' + e(d.amountVND) + '</b>' + (d.amountTextVND ? ' <i>(' + e(d.amountTextVND) + ')</i>' : '') + '</div>' +
            '<div>' + L('Total USD', 'Tổng USD') + ': <b>' + e(d.amountUSD) + '</b>' + (d.amountTextUSD ? ' <i>(' + e(d.amountTextUSD) + ')</i>' : '') + '</div>' +
            '</div>';
        html += '<div class="sign">' +
            '<div>' + L('Guest signature', 'Chữ ký khách hàng') + '<div class="line"></div></div>' +
            '<div>' + L('Cashier', 'Thu ngân') + '<div class="line">' + e(h.cashier) + '</div></div>' +
            '</div>';
        html += '<div class="foot"><span>' + e(h.dateTime) + '</span><span>' + e(h.hotelName) + '</span></div>';
        return html;
    }

    /**
     * Họ Billing Information (BILLING_INFOR / QUANTITY / BY_SELECTED / FOLIO_BY_DATE / FOLIO_SUMMARY).
     * opt.quantity: thêm cột Qty / Price; opt.groupBy: 'date' (nhóm theo ngày) | 'trans' (cộng theo giao dịch).
     */
    function renderFolioBillingFamily(d, opt) {
        const h = d.header || {};
        const vn = !!d.vietnamese;
        const e = escapeFolioOption;
        const rows = d.rows || [];
        let html = renderFolioHeaderBlock(h, vn, opt.title);

        const colCount = opt.quantity ? 6 : 4;
        html += '<table class="lines"><thead><tr>' +
            '<th style="width:85px">' + (opt.groupBy === 'trans' ? (vn ? 'Mã/Code' : 'Code') : (vn ? 'Ngày/Date' : 'Date')) + '</th>' +
            '<th>' + (vn ? 'Diễn giải/Description' : 'Description') + '</th>' +
            (opt.quantity ? '<th class="num" style="width:50px">' + (vn ? 'SL/Qty' : 'Qty') + '</th><th class="num" style="width:100px">' + (vn ? 'Đơn giá/Price' : 'Price') + '</th>' : '') +
            '<th class="num" style="width:130px">VND</th>' +
            '<th class="num" style="width:110px">USD</th>' +
            '</tr></thead><tbody>';

        if (rows.length === 0) {
            html += '<tr><td colspan="' + colCount + '" style="text-align:center;color:#777;padding:10px">' + (vn ? 'Không có giao dịch' : 'No transaction') + '</td></tr>';
        } else if (opt.groupBy === 'date') {
            // Nhóm theo ngày, có cộng từng ngày
            const groups = [];
            rows.forEach(function (r) {
                let g = groups.length ? groups[groups.length - 1] : null;
                if (!g || g.key !== r.date) { g = { key: r.date, items: [], vnd: 0, usd: 0 }; groups.push(g); }
                g.items.push(r); g.vnd += Number(r.valueVND) || 0; g.usd += Number(r.valueUSD) || 0;
            });
            groups.forEach(function (g) {
                html += '<tr class="grp"><td colspan="' + colCount + '"><b>' + e(g.key) + '</b></td></tr>';
                g.items.forEach(function (r) {
                    html += '<tr><td></td><td>' + e(r.text) + '</td><td class="num">' + e(r.vnd) + '</td><td class="num">' + e(r.usd) + '</td></tr>';
                });
                html += '<tr class="sub"><td></td><td style="text-align:right">Total ' + e(g.key) + '</td>' +
                    '<td class="num">' + fmtFolioVND(g.vnd) + '</td><td class="num">' + fmtFolioUSD(g.usd) + '</td></tr>';
            });
        } else if (opt.groupBy === 'trans') {
            // Cộng dồn theo mã giao dịch
            const map = {}; const order = [];
            rows.forEach(function (r) {
                const key = (r.code || '') + '|' + (r.text || '');
                if (!map[key]) { map[key] = { code: r.code, text: r.text, vnd: 0, usd: 0, count: 0 }; order.push(key); }
                map[key].vnd += Number(r.valueVND) || 0; map[key].usd += Number(r.valueUSD) || 0; map[key].count++;
            });
            order.forEach(function (k) {
                const g = map[k];
                html += '<tr><td>' + e(g.code) + '</td><td>' + e(g.text) + (g.count > 1 ? ' <span style="color:#666">(x' + g.count + ')</span>' : '') + '</td>' +
                    '<td class="num">' + fmtFolioVND(g.vnd) + '</td><td class="num">' + fmtFolioUSD(g.usd) + '</td></tr>';
            });
        } else {
            rows.forEach(function (r) {
                html += '<tr><td>' + e(r.date) + '</td><td>' + e(r.text) + '</td>' +
                    (opt.quantity ? '<td class="num">' + e(r.quantity) + '</td><td class="num">' + e(r.price) + '</td>' : '') +
                    '<td class="num">' + e(r.vnd) + '</td><td class="num">' + e(r.usd) + '</td></tr>';
            });
        }
        html += '</tbody></table>';

        html += renderFolioSummaryBlock(d);
        html += renderFolioTotalsAndSign(d, vn);
        html += '<style>table.lines tr.grp td { padding-top: 8px; } table.lines tr.sub td { border-top: 1px dotted #999; font-weight: 600; }</style>';
        return folioPrintPage(opt.title[0] + ' - Folio ' + (h.folioNo || ''), html);
    }

    /** FOLIO_DETAIL: liệt kê cả Debit và Credit, khối tổng Balance / Total.Incl / Net / SVC / VAT theo VND và USD. */
    function renderFolioDetail(d) {
        const h = d.header || {};
        const e = escapeFolioOption;
        let html = '<div class="hotel">' + e(h.hotelName) + '</div>';
        html += '<div class="title">FOLIO DETAIL</div>';
        html += '<table class="meta"><tr>' +
            '<td class="k">Name</td><td class="v">' + e(h.name) + '</td><td class="k">Folio No</td><td class="v">' + e(h.folioNo) + '</td></tr>' +
            '<tr><td class="k">Address</td><td class="v">' + e(h.address) + '</td><td class="k">Confirm No</td><td class="v">' + e(h.confirmNo) + '</td></tr>' +
            '<tr><td class="k">Company</td><td class="v">' + e(h.company) + '</td><td class="k">Room</td><td class="v">' + e(h.room) + '</td></tr>' +
            '<tr><td class="k">Group</td><td class="v">' + e(h.group) + '</td><td class="k">Arrival</td><td class="v">' + e(h.arrival) + '</td></tr>' +
            '<tr><td class="k">Cashier</td><td class="v">' + e(h.cashier) + '</td><td class="k">Departure</td><td class="v">' + e(h.departure) + '</td></tr>' +
            '<tr><td class="k">User</td><td class="v">' + e(h.user) + '</td><td class="k"></td><td class="v"></td></tr>' +
            '</table>';
        if (h.text1 || h.text2) {
            html += '<div style="margin:-4px 0 10px;white-space:pre-line">' + e(h.text1) + (h.text2 ? '\n' + e(h.text2) : '') + '</div>';
        }

        html += '<table class="lines"><thead><tr>' +
            '<th style="width:85px">Date</th><th style="width:70px">Inv.No</th><th>Description</th>' +
            '<th class="num" style="width:110px">Debit VND</th><th class="num" style="width:110px">Credit VND</th>' +
            '<th class="num" style="width:85px">Debit USD</th><th class="num" style="width:85px">Credit USD</th>' +
            '</tr></thead><tbody>';
        const rows = d.rows || [];
        if (rows.length === 0) html += '<tr><td colspan="7" style="text-align:center;color:#777;padding:10px">No transaction</td></tr>';
        rows.forEach(function (r) {
            html += '<tr><td>' + e(r.date) + '</td><td>' + e(r.invoiceNo) + '</td><td>' + e(r.text) + '</td>' +
                '<td class="num">' + (r.isCredit ? '' : e(r.vnd)) + '</td><td class="num">' + (r.isCredit ? e(r.vnd) : '') + '</td>' +
                '<td class="num">' + (r.isCredit ? '' : e(r.usd)) + '</td><td class="num">' + (r.isCredit ? e(r.usd) : '') + '</td></tr>';
        });
        html += '</tbody></table>';

        html += '<table class="sum" style="width:auto;margin-left:auto">';
        (d.summary || []).forEach(function (s) {
            html += '<tr class="' + (s.isTotal ? 'total' : '') + '"><td class="lbl" style="padding-right:24px">' + e(s.label) + '</td>' +
                '<td class="num">' + e(s.vnd) + '</td><td>VND</td>' +
                '<td class="num">' + e(s.usd) + '</td><td>USD</td></tr>';
        });
        html += '</table>';

        html += '<div class="sign"><div>Guest signature<div class="line"></div></div><div>Cashier<div class="line">' + e(h.cashier) + '</div></div></div>';
        html += '<div class="foot"><span>' + e(h.dateTime) + '</span><span>' + e(h.hotelName) + '</span></div>';
        return folioPrintPage('Folio Detail - Folio ' + (h.folioNo || ''), html);
    }
