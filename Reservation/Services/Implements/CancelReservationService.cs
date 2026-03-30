using System.Data;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using Reservation.Services.Interfaces;

namespace Reservation.Services.Implements
{
    public class CancelReservationService : ICancelReservationService
    {
        public class ProcessResult
        {
            // Wrapper đơn giản để hàm con trả kết quả về controller
            public bool Success { get; set; }
            public string Message { get; set; }

            public static ProcessResult Ok(string message)
                => new()
                { Success = true, Message = message };

            public static ProcessResult Fail(string message)
                => new()
                { Success = false, Message = message };
        }

        public ProcessResult ProcessProStayCancel(ProcessTransactions pt, ReservationModel currentRsv, string selectedIdsRaw, int userId, string userName, string reasonCancellation, string description)
        {
            // Phải có lý do mới được hủy
            if (string.IsNullOrWhiteSpace(reasonCancellation))
                return ProcessResult.Fail("Please choose a cancellation reason.");
            // Nếu lâm master thì phải kiểm tra guests đã được sử lý hết chưa
            if (currentRsv.ReservationNo == "0")
            {
                if (!CheckMaster(currentRsv.ConfirmationNo, currentRsv.ID, 0))
                    return ProcessResult.Fail("Cancel all guests before cancel master.");
            }
            // Xác đinkh reservation nào thực sự cần hủy
            // selectedIdsRaw được parse thành list user không truyền gì thì hủy current reservation
            List<int> selectedIds = ParseReservationIds(selectedIdsRaw);
            if (selectedIds.Count == 0)
            {
                selectedIds.Add(currentRsv.ID);
            }
            // chỉ dữ lại res cùng confno  với CurrRsvStatus để tránh nhầm id từ booking khác
            List<ReservationModel> targetReservation = [.. GetReservationsByIds(selectedIds).Where(x => x != null && x.ConfirmationNo == currentRsv.ConfirmationNo)];

            if (targetReservation.Count == 0)
                return ProcessResult.Fail("No reservation selected for cancellation");

            // Kiểm tra depo/payment nếu có tiền cọc đã thu thì không cho hủy
            foreach (var item in targetReservation)
            {
                var deposit = DepositPaymentBO.Instance.FindByAttribute("ReservationID", item.ID);
                if (deposit != null && deposit.Count > 0)
                {
                    return ProcessResult.Fail(
                        $"Payment in advance exists on reservation {item.ID}. You must balance th amount paid before cancellation.");
                }
            }
            // Hủy từng reservation được chọn
            foreach (var item in targetReservation)
            {
                if (!IsPreStayCancelableStatus(item.Status))
                    return ProcessResult.Fail($"Reserrvation {item.ID} has invalid status for cancel.");
                //giữu trạng thái cũ để log
                int oldStatus = item.Status;
                //đổi trạng thái 
                item.Status = 3;
                item.UpdateDate = item.SpecialUpdateDate = DateTime.Now;
                item.UserUpdateId = userId;
                item.UpdateBy = item.SpecialUpdateBy = userName;

                //Lưu vào db
                ReservationBO.Instance.Update(item);
                // ghi log
                ReservationUtil.InsertActivityLog(
                    item.ID,
                    userId,
                    userName,
                    "Status",
                    GetStatusText(oldStatus),
                    "Cancel",
                    ""
                );

                // Ghi bảng ReserrvationCacellation
                InsertReservationCancellation(
                    item.ID,
                    userId,
                    reasonCancellation,
                    description
                );
            }
            //    kiểm tra xem có cần update master sang CancelRsv hay không.
            //
            // Rule bám WinForms:
            // - nếu tất cả guest reservation thuộc confirmation này
            //   đều đã thành status 3
            // - thì master row (ReservationNo == "0") cũng đổi sang status 3
            UpdateMasterStatusIfAllGuestsCancelled(currentRsv.ConfirmationNo);
            return ProcessResult.Ok("Cancel Resserrvation successffully.");
        }

        public ProcessResult ProcessCheckedInReinstates(ProcessTransactions pt, ReservationModel rsv, int userId, string userName, bool changeRoomToDirty)
        {
            // =============================================================
            // FLOW NÀY DÙNG CHO RESERVATION ĐÃ CHECKED-IN
            // =============================================================
            // Đây không phải "cancel thường".
            // Nó là nghiệp vụ kiểu:
            // - undo / reinstate checked-in
            // - chuyển booking từ CheckedIn về DueIn hoặc Reserved
            //
            // Side-effect kèm theo:
            // - room status
            // - folio
            // - deposit
            // - interface
            // - telephone switch
            DateTime busDate = TextUtils.GetBusinessDate();
            DateTime arrivalDate = rsv.ArrivalDate.Date;
            // Nếu ngày busDate đã qua arr date thì không cho hủy check-in nữa
            if (busDate > arrivalDate)
                return ProcessResult.Fail("Checked-in booking cannot be cancellation after arrival date.");
            // Nếu đúng ngày mà đã phát sinh folio thì không cho reinstates
            if (busDate == arrivalDate && !ReservationUtil.CheckReinstateCI(rsv.ID))
                return ProcessResult.Fail("Checked-in booking cannot be cancelled because folio/changes exist.");
            //Rule master thì phải reinstatus guests trước
            if (rsv.ReservationNo == "0")
            {
                if (!CheckMaster(rsv.ConfirmationNo, rsv.ID, 1))
                    return ProcessResult.Fail("Reinstate all guests before reinstate master.");
            }
            // Nếu crr là mainguest mà vẫn con Roomsharer khác đang check-in thì hủy
            if (ReservationUtil.HasOtherCheckedInRoomSharer(rsv) && rsv.MainGuest)
                return ProcessResult.Fail("You must cancel room sharer first.");
            // Kiểm tra đây có phải guests cuối cùng của conf đang reinstate không
            bool isLastGuestReinstate = ReservationUtil.LastGuestReinstateCI(rsv.ConfirmationNo, 1, null);
            // Nếu là guest cuối cùng và master folio còn giao dịch
            // => không cho reinstate
            if (isLastGuestReinstate && ReservationUtil.CheckTransactionByMaster(rsv.ConfirmationNo, null))
                return ProcessResult.Fail("Charges exist on the master folio. Not cancel.");
            // 6) Xác định status đích sau reinstate
            // - cùng ngày đến => Due In
            // - khác ngày đến => Reserved
            int newStatus = (busDate == arrivalDate) ? 5 : 0;
            string newStatusText = newStatus == 5 ? "DUE IN" : "RESERVED";

            // 7) Update Reservation
            rsv.Status = newStatus;
            //Balance cũ
            decimal oldBalanceUsd = rsv.BalanceUSD;
            decimal oldBalanceVnd = rsv.BalanceVND;
            // Reset balance khi reinstate
            rsv.BalanceUSD = 0;
            rsv.BalanceVND = 0;

            // Audit fields
            rsv.UpdateDate = rsv.SpecialUpdateDate = DateTime.Now;
            rsv.UserUpdateId = userId;
            rsv.UpdateBy = rsv.SpecialUpdateBy = userName;

            ReservationBO.Instance.Update(rsv);

            // 8) Ghi log status change
            ReservationUtil.InsertActivityLog(
                rsv.ID,
                userId,
                userName,
                "Status",
                "CHECKED IN",
                newStatusText,
                "");

            // 9) Nếu là main guest và có RoomID
            //    thì update trạng thái front office của phòng
            if (rsv.MainGuest && rsv.RoomId > 0)
            {
                UpdateRoomFrontOfficeStatus(pt, rsv.RoomId, 0, 0);
            }

            // 10) Nếu user chọn đổi room sang dirty thì update housekeeping status
            if (changeRoomToDirty && rsv.RoomId > 0)
            {
                UpdateRoomHKStatusDirty(pt, rsv.RoomId);
            }

            // 11) Nếu có balance thì reset deposit process + xóa folio/folio detail
            //
            // Lưu ý:
            // Ở đây có một vấn đề logic tiềm ẩn:
            // ngay phía trên đã set BalanceUSD = 0 và BalanceVND = 0,
            // nên điều kiện bên dưới sẽ luôn false.
            // Nếu muốn kiểm tra balance cũ thì cần lưu oldBalance trước khi reset.
            if (oldBalanceUsd != 0 || oldBalanceVnd != 0)
            {
                ResetProcessedDeposit(pt, rsv.ID);
                DeleteFolioByReservation(pt, rsv.ID);
                DeleteFolioDetailByReservation(pt, rsv.ID);
            }

            // 12) Nếu là main guest hoặc master, và booking có company/agent,
            //     thì có thể cần cancel master folio
            if ((rsv.MainGuest || rsv.ReservationNo == "0")
                && (rsv.ProfileCompanyId > 0 || rsv.ProfileAgentId > 0))
            {
                if (isLastGuestReinstate)
                {
                    CancelMaster(rsv.ConfirmationNo, 5, 1, pt);
                }
            }

            // 13) Gọi interface đồng bộ ra hệ thống ngoài
            ReservationUtil.IF_RICI(rsv.ID, rsv.ProfileIndividualId, rsv.RoomNo);

            // 14) Cập nhật trạng thái reservation hiện tại của phòng
            if (rsv.RoomId > 0)
            {
                ReservationBO.UpdateReservationStatus(null, rsv.RoomId);
            }

            // 15) Đồng bộ telephone switch nếu là main guest
            if (rsv.MainGuest)
            {
                InsertToTelephoneSwitch(rsv.RoomNo, rsv.LastName, false, DateTime.Now);
            }

            return ProcessResult.Ok("Reinstate checked-in booking successfully.");

        }


        private static void UpdateMasterStatusIfAllGuestsCancelled(string confNo)
        {
            // - lấy toàn bộ reservation cùng ConfirmationNo
            // - chỉ xét guest rows (ReservationNo != "0")
            // - bỏ qua Waitlist và NoShow
            // - nếu tất cả guest đang có chung 1 status và status đó = 3 (Cancel)
            //   => update master row thành Cancel
            var allRows = PropertyUtils.ConvertToList<ReservationModel>(ReservationBO.Instance.FindByAttribute("ConfirmationNo", confNo));

            var guestStatus = allRows.Where(x => x.ReservationNo != "0" && x.Status != 4 && x.Status != 7)
                                    .Select(x => x.Status)
                                    .Distinct()
                                    .ToList();

            if (guestStatus.Count == 1 && guestStatus[0] == 3)
            {
                foreach (var master in allRows.Where(x => x.ReservationNo == "0"))
                {
                    master.Status = 3;
                    master.UpdateDate = DateTime.Now;
                    ReservationBO.Instance.Update(master);
                }
            }
        }

        private static List<ReservationModel> GetReservationsByIds(List<int> ids)
        {
            // Load nhiều reservation theo danh sách ID
            var result = new List<ReservationModel>();
            foreach (int id in ids)
            {
                var item = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(id);
                if (item != null)
                {
                    result.Add(item);
                }
            }
            return result;
        }

        private static List<int> ParseReservationIds(string raw)
        {
            // Convert chuỗi "1,2,3" hoặc "1;2;3" thành List<int>
            // Bỏ qua phần tử rỗng / không parse được / trùng lặp
            if (string.IsNullOrWhiteSpace(raw))
            {
                return [];
            }
            return [.. raw.Split([',',';'], StringSplitOptions.RemoveEmptyEntries)
                        .Select(x =>
                        {
                            int.TryParse(x.Trim(), out int id);
                            return id;
                        })
                        .Where(x => x>0)];
        }

        private static void InsertReservationCancellation(int reservationId, int userId, string reasonCancellation, string description)
        {
            // Sinh mã cancellation number
            string cancellationNo = ReservationCancellationBO.GetTopCancellatioNo();

            // Ghi nhận thông tin cancellation vào bảng riêng
            ReservationCancellationModel reservationCancellationModel = new()
            {
                ReservationID = reservationId,
                CancellationDate = DateTime.Now,
                CancellationNo = !string.IsNullOrEmpty(cancellationNo) ? cancellationNo : "0",
                ReasonCancellation = reasonCancellation,
                Description = description,
                CreateDate = DateTime.Now,
                UpdateDate = DateTime.Now,
                UserInsertID = userId,
                UserUpdateID = userId
            };

            ReservationCancellationBO.Instance.Insert(reservationCancellationModel);
        }

        public static bool CheckMaster(string _conf, int _rsvID, int _type)
        {
            bool _para = false;
            //CI chekc với type ForCheckedInReinstate
            if (_type == 1)
            {
                //Check Routing
                DataTable _dtRT = TextUtils.Select("SELECT ID FROM Routing WITH (NOLOCK) WHERE ToReservationID = " + _rsvID + " OR FromReservationID =" + _rsvID + " ");
                if (_dtRT.Rows.Count > 0)
                {
                    //Check Last Guest
                    DataTable _dt = TextUtils.Select("SELECT count(ID) FROM Reservation WITH (NOLOCK) WHERE ConfirmationNo ='" + _conf + "' AND (Status = 1 OR Status = 6) AND ReservationNo > 0 ");
                    int count = TextUtils.ToInt(_dt.Rows[0][0].ToString() ?? "0");
                    _para = count < 1;
                }
                else
                    _para = true;
            }
            //đặt phòng ForPreStayCancel
            else if (_type == 0)
            {
                //Check Routing
                DataTable _dtRT = TextUtils.Select("SELECT ID FROM Routing WITH (NOLOCK) WHERE ToReservationID = " + _rsvID + " OR FromReservationID =" + _rsvID + " ");
                if (_dtRT.Rows.Count > 0)
                {
                    //Check Last Guest
                    DataTable _dt = TextUtils.Select("SELECT count(ID) FROM Reservation WITH (NOLOCK) WHERE ConfirmationNo ='" + _conf + "' AND Status NOT IN (3,4,7) AND ReservationNo > 0 ");
                    int count = TextUtils.ToInt(_dt.Rows[0][0]?.ToString() ?? "0");
                    _para = count < 1;
                }
                else
                    _para = true;
            }
            return _para;
        }


        private static string GetStatusText(int status)
        {
            //Chuyển status từ int qua string
            return status switch
            {
                0 => "RESERVED",
                1 => "CHECKED IN",
                2 => "CHECKED OUT",
                3 => "CANCEL",
                4 => "WAITLIST",
                5 => "DUE IN",
                6 => "DUE OUT",
                7 => "NO SHOW",
                8 => "DAY USE",
                _ => status.ToString(),
            };
        }

        public static bool IsPreStayCancelableStatus(int status)
        {
            // Hàm này dùng để xác định:
            // status hiện tại có được đi vào flow "pre-stay cancel" hay không.
            //
            // Phiên bản hiện tại bám WinForms outer condition:
            // 0 = Reservation
            // 3 = CancelRsv
            // 4 = Waitlist
            // 5 = DueIn
            // 7 = NoShow
            //
            // Tuy nhiên về nghiệp vụ thuần:
            // - status 3 (đã cancel) và 7 (no-show) có thể không nên cho cancel lại.
            // Nếu muốn siết chặt business rule hơn, có thể đổi còn:
            // return status == 0 || status == 4 || status == 5;
            return status == 0 || status == 3 || status == 4 || status == 5 || status == 7;
        }

        private static void UpdateRoomFrontOfficeStatus(ProcessTransactions pt, int roomId, int foStatus, int hkFoStatus)
        {
            pt.UpdateCommand(
                "UPDATE Room WITH (ROWLOCK) " +
                "SET FOStatus = " + foStatus + ", HKFOStatus = " + hkFoStatus + " " +
                "WHERE ID = " + roomId);
        }

        private static void UpdateRoomHKStatusDirty(ProcessTransactions pt, int roomId)
        {
            pt.UpdateCommand(
                "UPDATE Room WITH (ROWLOCK) " +
                "SET HKStatusID = 2 " +
                "WHERE ID = " + roomId);
        }

        private static void ResetProcessedDeposit(ProcessTransactions pt, int reservationId)
        {
            pt.UpdateCommand(
                "UPDATE dbo.DepositPayment WITH (ROWLOCK) " +
                "SET IsProcess = 0 " +
                "WHERE ID IN (" +
                    "SELECT ID FROM dbo.DepositPayment WITH (NOLOCK) " +
                    "WHERE ReservationID = " + reservationId + " AND IsProcess = 1" +
                ")");
        }

        private static void DeleteFolioByReservation(ProcessTransactions pt, int reservationId)
        {
            pt.DeleteByAttribute("Folio", "ReservationID", reservationId.ToString());
        }

        private static void DeleteFolioDetailByReservation(ProcessTransactions pt, int reservationId)
        {
            pt.DeleteByAttribute("FolioDetail", "ReservationID", reservationId.ToString());
        }

        private static void CancelMaster(string confirmationNo, int status, int mode, ProcessTransactions pt)
        {
            // WinForms chỉ có chỗ gọi: ClassReservation.CancelMaster(confirmNo, 5, 1, pt)
            // chưa có body gốc. Tạm update các master rows theo status truyền vào.
            pt.UpdateCommand(
                "UPDATE Reservation WITH (ROWLOCK) " +
                "SET Status = " + status + ", UpdateDate = GETDATE() " +
                "WHERE ConfirmationNo = '" + confirmationNo + "' " +
                "AND ReservationNo = '0'");
        }

        private void InsertToTelephoneSwitch(string roomNo, string lastName, bool openClose, DateTime sysDate)
        {
            // Đây là call WinForms rõ ràng: TelephoneSwitchBO.insertToTelephoneSwitch(...)
            //TelephoneSwitchBO.insertToTelephoneSwitch(roomNo, lastName, openClose, sysDate);
        }
    }
}