window.NewReservation = {
    init: async function () {
        console.log('NewReservation.init() is running...'); // Để debug
        // Reset trạng thái
        let selectedCodesFromResult = [];
        let checkedIds = [];
        let selectedRowsData = [];
        let checkEdit = false;
        let idReservation = 0;
        let roomNowuk = null;

        $('#btnOption').hide();
        $('#btnClear').show();

        // ĐỌC DỮ LIỆU TỪ data-reservation của div
        const modalBody = $('#newReservationModalBody');
        let result = null;

        if (modalBody.length) {
            const jsonString = modalBody.attr('data-reservation'); // Lấy dưới dạng string
            console.log('Raw data-reservation:', jsonString);

            if (jsonString) {
                try {
                    result = JSON.parse(jsonString); // TỰ parse
                    console.log('Parsed result:', result);
                } catch (e) {
                    console.error('Lỗi parse JSON từ data-reservation:', e);
                }
            }
        }

        if (result && result.id && result.id !== 0) {
            checkEdit = true;
            idReservation = result.id;
            setUpReservation(result);
            $('#btnOption').show();
            $('#btnClear').hide();
            roomNowuk = result.roomNo || null;
        } else {
            // Mode tạo mới
            roomNowuk = null;
            // reset thêm nếu cần
        }

        getBusinessDate();
        setUpIconClear();
        // await setTomSelect();

        // Các init chung khác...
    },
};
