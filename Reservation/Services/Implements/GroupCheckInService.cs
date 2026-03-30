using System.Data;
using BaseBusiness.util;
using DevExpress.DataAccess.Wizard.Model;
using Microsoft.Data.SqlClient;
using Reservation.Services.Interfaces;
using static Reservation.Dto.CheckInGroupDTO;

namespace Reservation.Services.Implements
{
    /// <summary>
    /// Service xử lý toàn bộ nghiệp vụ check-in nhóm.
    ///
    /// Logic được chuyển từ mã WinForms:
    /// - mode == 0: check-in theo ProfileGroupID
    /// - mode == 1: check-in theo ConfirmationNo
    /// - kiểm tra RoomID phải có
    /// - lọc theo HKStatusId (Clean / Inspected / All)
    /// - kiểm tra alert khu vực Check-In
    /// - nếu main guest bị chặn thì sharer cùng ShareRoom cũng bị chặn
    /// - gọi hàm tạo check-in thực tế
    /// </summary>
    public class GroupCheckInService(IReservationService reservation)
    {
        private readonly IReservationService _reservationService = reservation;

        /// Check-in theo ProfileGroupId.
        // public CheckInResult CheckInByProfileGroup(CheckInRequest request)
        // {
        //     // Validate dữ liệu đầu vào
        //     if (request == null)
        //     {
        //         return BuildErrorResult("Request is null.");
        //     }

        //     if (!request.ProfileGroupId.HasValue || request.ProfileGroupId.Value <= 0)
        //     {
        //         return BuildErrorResult("ProfileGroupId is invalid.");
        //     }

        //     // Lấy danh sách res của group

        // }
        private CheckInItemResult ProcessSingleItem(
               ReservationCheckInDto item,
               CheckInRequest request,
               ref int blockedShareRoom)
        {
            // 1) Bắt buộc phải có phòng
            if (!HasRoom(item))
            {
                return BuildFail(item, "Reservation has no room assigned.");
            }

            // 2) Phòng phải đạt điều kiện housekeeping theo filter
            if (!MatchRoomStatus(item.HKStatusId, request.FilterType))
            {
                return BuildFail(item, "Room status is not valid for selected filter.");
            }

            // 3) Nếu main guest trước đó bị chặn thì sharer tương ứng cũng bị chặn
            if (IsBlockedByMainGuest(item, blockedShareRoom))
            {
                return BuildFail(item, "Main guest was not checked in, sharer is blocked.");
            }

            // 4) Kiểm tra alert khu vực Check-In
            var alerts = ReservationUtil.GetReservationAlerts(item.ReservationId);
            var hasAlert = alerts != null && alerts.Count > 0;

            if (hasAlert && !request.IgnoreAlert)
            {
                // Nếu item hiện tại là main guest và bị chặn vì alert
                // thì lưu ShareRoom để chặn các sharer sau đó
                if (item.IsMainGuest)
                {
                    blockedShareRoom = item.ShareRoom;
                }

                // Trả thông báo rõ hơn
                var firstAlertMessage = alerts.FirstOrDefault()?.Message;
                return BuildFail(
                    item,
                    string.IsNullOrWhiteSpace(firstAlertMessage)
                        ? "Reservation has check-in alert."
                        : "Reservation has check-in alert: " + firstAlertMessage);
            }

            // 5) Thực hiện check-in thật
            CheckInService.CreateCheckIn(item, request.UserId);

            return BuildSuccess(item, "Checked In. Room: " + item.RoomNo + ".");
        }

        private static bool HasRoom(ReservationCheckInDto item)
        {
            return item != null && item.RoomId > 0;
        }
        private static bool MatchRoomStatus(int hkStatusId, int filterType)
        {
            return filterType switch
            {
                // InspectedOnly
                1 => hkStatusId == 4,
                // CleanOnly
                2 => hkStatusId == 1,
                // CleanOrInspected
                3 => hkStatusId == 1 || hkStatusId == 4,
                // All
                4 => true,
                _ => false,
            };
        }
        private static bool IsBlockedByMainGuest(ReservationCheckInDto item, int blockedShareRoom)
        {
            if (item == null)
            {
                return false;
            }

            // Chỉ chặn các guest không phải main guest
            // và có cùng ShareRoom với group bị block
            return !item.IsMainGuest
                   && blockedShareRoom > 0
                   && item.ShareRoom == blockedShareRoom;
        }
        private List<ReservationCheckInDto> GetReservationsByProfileGroup(int profileGroupId)
        {
            SqlParameter[] param =
                [
                    new SqlParameter("@Status", "5"),
                    new SqlParameter("@ProfileGroupID", profileGroupId)
                ];
            var result = new List<ReservationCheckInDto>();

            var dt = DataTableHelper.getTableData("spGroupReservationToDelete", param);
            foreach (DataRow row in dt.Rows)
            {
                result.Add(new ReservationCheckInDto
                {
                    ReservationId = row["ReservationID"] == DBNull.Value ? 0 : Convert.ToInt32(row["ReservationID"]),
                    RoomId = row["RoomID"] == DBNull.Value ? 0 : Convert.ToInt32(row["RoomID"]),
                    NoOfRoom = row["Rms"] == DBNull.Value ? 0 : Convert.ToInt32(row["Rms"]),
                    RoomNo = row["Room"] == DBNull.Value ? string.Empty : row["Room"].ToString() ?? string.Empty,
                    HKStatusId = row["HKStatusID"] == DBNull.Value ? 0 : Convert.ToInt32(row["HKStatusID"]),

                    // Nếu store hiện tại không trả 2 cột này,
                    // bạn có thể sửa store hoặc query bổ sung
                    IsMainGuest = row.Table.Columns.Contains("IsMainGuest") && row["IsMainGuest"] != DBNull.Value && Convert.ToBoolean(row["IsMainGuest"]),

                    ShareRoom = row.Table.Columns.Contains("ShareRoom") && row["ShareRoom"] != DBNull.Value
                        ? Convert.ToInt32(row["ShareRoom"])
                        : 0
                });

            }
            return result;
        }


        /// <summary>
        /// Lấy danh sách reservation theo ConfirmationNo.
        /// </summary>
        private List<ReservationCheckInDto> GetReservationsByConfirmation(
            string confirmationNo,
            List<int> selectedReservationIds)
        {
            var dt = _reservationService.ResConfNoList(Convert.ToInt32(confirmationNo));
            var result = new List<ReservationCheckInDto>();

            foreach (DataRow row in dt.Rows)
            {
                result.Add(new ReservationCheckInDto
                {
                    ReservationId = row["ID"] == DBNull.Value ? 0 : Convert.ToInt32(row["ID"]),
                    RoomId = row["RoomID"] == DBNull.Value ? 0 : Convert.ToInt32(row["RoomID"]),
                    NoOfRoom = row["Nbr"] == DBNull.Value ? 0 : Convert.ToInt32(row["Nbr"]),
                    RoomNo = row["RoNo"] == DBNull.Value ? string.Empty : row["RoNo"].ToString() ?? string.Empty,
                    HKStatusId = row["HKStatusID"] == DBNull.Value ? 0 : Convert.ToInt32(row["HKStatusID"]),
                    IsMainGuest = row["MG"] != DBNull.Value && row["MG"].ToString() == "X",
                    ShareRoom = row["SR"] == DBNull.Value ? 0 : Convert.ToInt32(row["SR"])
                });
            }

            if (selectedReservationIds != null && selectedReservationIds.Count > 0)
            {
                result = [.. result.Where(x => selectedReservationIds.Contains(x.ReservationId))];
            }

            return result;
        }

        /// <summary>
        /// Tạo kết quả fail cho 1 item 
        /// </summary>
        private static CheckInItemResult BuildFail(ReservationCheckInDto item, string message)
        {
            return new CheckInItemResult
            {
                ReservationId = item == null ? 0 : item.ReservationId,
                RoomNo = item == null ? string.Empty : item.RoomNo,
                Success = false,
                Message = message
            };
        }

        /// <summary>
        /// Tạo kết quả success cho một item.
        /// </summary>
        private static CheckInItemResult BuildSuccess(ReservationCheckInDto item, string message)
        {
            return new CheckInItemResult
            {
                ReservationId = item == null ? 0 : item.ReservationId,
                RoomNo = item == null ? string.Empty : item.RoomNo,
                Success = true,
                Message = message
            };
        }
        /// <summary>
        /// Tạo kết quả lỗi tổng.
        /// </summary>
        private static CheckInResult BuildErrorResult(string message, string detail = null)
        {
            var result = new CheckInResult
            {
                Success = false,
                TotalRequested = 0,
                TotalCheckedIn = 0,
                Message = message
            };

            result.Messages.Add(message);

            if (!string.IsNullOrWhiteSpace(detail))
            {
                result.Messages.Add(detail);
            }

            return result;
        }
    }

    /// Extension hỗ trợ kiểm tra DataReader có cột hay không.
    /// Dùng để tránh lỗi nếu store hiện tại chưa trả đủ cột.
    /// </summary>
    internal static class DataReaderExtensions
    {
        public static bool HasColumn(this IDataRecord reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}