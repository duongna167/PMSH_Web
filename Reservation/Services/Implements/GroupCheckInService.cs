using System.Collections;
using System.Data;
using System.Linq;
using BaseBusiness.Model;
using BaseBusiness.util;
using Microsoft.Data.SqlClient;
using Reservation.Services.Interfaces;
using static Reservation.Dto.CheckInGroupDTO;

namespace Reservation.Services.Implements
{
    /// <summary>
    /// Service xử lý nghiệp vụ check-in nhóm.
    /// Logic bám theo code gốc:
    /// - ProfileGroupId: lọc danh sách theo group rồi gọi CreateCheckIn cho từng item
    /// - ConfirmationNo: xử lý theo danh sách chọn, check alert Check-In và block sharer nếu main guest bị bỏ qua do alert
    /// - Luôn gọi engine check-in với mode = 1 như flow group cũ
    /// </summary>
    public class GroupCheckInService(IReservationService reservation) : IGroupCheckInService
    {
        private readonly IReservationService _reservationService = reservation;

        #region Public API

        public CheckInResult CheckIn(CheckInRequest request)
        {
            if (request == null)
            {
                return BuildErrorResult("Request is null.");
            }

            if (request.ProfileGroupId.HasValue && request.ProfileGroupId.Value > 0)
            {
                return CheckInByProfileGroup(request);
            }

            if (!string.IsNullOrWhiteSpace(request.ConfirmationNo))
            {
                return CheckInByConfirmation(request);
            }

            return BuildErrorResult("ProfileGroupId or ConfirmationNo is required.");
        }

        public CheckInResult CheckInByProfileGroup(CheckInRequest request)
        {
            if (request == null)
            {
                return BuildErrorResult("Request is null.");
            }

            if (!request.ProfileGroupId.HasValue || request.ProfileGroupId.Value <= 0)
            {
                return BuildErrorResult("ProfileGroupId is invalid.");
            }

            if (!IsValidFilterType(request.FilterType))
            {
                return BuildErrorResult("FilterType is invalid.");
            }

            try
            {
                List<ReservationCheckInDto> items = GetReservationsByProfileGroup(request.ProfileGroupId.Value);
                return ProcessItems(items, request, applyAlertRule: false);
            }
            catch (Exception ex)
            {
                return BuildErrorResult("Group chek in not complete.", ex.Message);
            }
        }

        public CheckInResult CheckInByConfirmation(CheckInRequest request)
        {
            if (request == null)
            {
                return BuildErrorResult("Request is null.");
            }

            if (string.IsNullOrWhiteSpace(request.ConfirmationNo))
            {
                return BuildErrorResult("ConfirmationNo is invalid.");
            }

            if (!int.TryParse(request.ConfirmationNo, out _))
            {
                return BuildErrorResult("ConfirmationNo must be numeric.");
            }

            if (!IsValidFilterType(request.FilterType))
            {
                return BuildErrorResult("FilterType is invalid.");
            }

            try
            {
                List<ReservationCheckInDto> items = GetReservationsByConfirmation(request.ConfirmationNo, request.SelectedReservationIds);
                return ProcessItems(items, request, applyAlertRule: true);
            }
            catch (Exception ex)
            {
                return BuildErrorResult("Group chek in not complete.", ex.Message);
            }
        }

        #endregion

        #region Processing

        private CheckInResult ProcessItems(
            List<ReservationCheckInDto> items,
            CheckInRequest request,
            bool applyAlertRule)
        {
            if (items == null || items.Count == 0)
            {
                return BuildNoAvailableResult();
            }

            // Validate: nếu toàn bộ reservation chưa được assign phòng thì báo lỗi sớm
            bool allHaveNoRoom = items.All(i => !HasRoom(i));
            if (allHaveNoRoom)
            {
                return BuildErrorResult(
                    "No rooms have been assigned. Please assign rooms to reservations before checking in.",
                    $"{items.Count} reservation(s) have no room assigned.");
            }

            CheckInResult result = new()
            {
                Success = false,
                TotalRequested = items.Count,
                TotalCheckedIn = 0
            };

            int blockedShareRoom = 0;

            foreach (ReservationCheckInDto item in items)
            {
                CheckInItemResult itemResult = ProcessSingleItem(item, request, ref blockedShareRoom, applyAlertRule);
                result.Items.Add(itemResult);
                result.Messages.Add(itemResult.Message ?? string.Empty);

                if (itemResult.Success)
                {
                    result.TotalCheckedIn++;
                }
            }

            result.Success = result.TotalCheckedIn > 0;

            if (!result.Success)
            {
                // Kiểm tra xem lý do thất bại chủ yếu là do chưa có phòng không
                int noRoomCount = result.Items.Count(i => !i.Success && i.Message != null && i.Message.Contains("no room"));
                result.Message = noRoomCount == result.Items.Count
                    ? $"Check-in failed: {noRoomCount} reservation(s) have no room assigned. Please assign rooms first."
                    : "Group check-in completed with errors. See details for each reservation.";
            }
            else
            {
                result.Message = result.TotalCheckedIn == result.TotalRequested
                    ? "Group check-in completed successfully."
                    : $"Group check-in partially completed: {result.TotalCheckedIn}/{result.TotalRequested} reservation(s) checked in.";
            }

            return result;
        }

        private CheckInItemResult ProcessSingleItem(
            ReservationCheckInDto item,
            CheckInRequest request,
            ref int blockedShareRoom,
            bool applyAlertRule)
        {
            if (!HasRoom(item))
            {
                return BuildFail(item, "Reservation has no room assigned.");
            }

            if (!MatchRoomStatus(item.HKStatusId, request.FilterType))
            {
                return BuildFail(item, "Room status is not valid for selected filter.");
            }

            if (applyAlertRule &&
                !request.IgnoreAlert &&
                TryGetFirstCheckInAlertMessage(item.ReservationId, out string alertMessage))
            {
                if (item.IsMainGuest)
                {
                    blockedShareRoom = item.ShareRoom;
                }

                return BuildFail(
                    item,
                    string.IsNullOrWhiteSpace(alertMessage)
                        ? "Reservation has check-in alert."
                        : "Reservation has check-in alert: " + alertMessage);
            }

            if (applyAlertRule && IsBlockedByMainGuest(item, blockedShareRoom))
            {
                return BuildFail(item, "Main guest was not checked in, sharer is blocked.");
            }

            CheckInService.CheckInRequest createCheckInRequest = new()
            {
                ReservationId = item.ReservationId,
                RoomId = item.RoomId,
                SelectedRoomId = item.RoomId,
                NoOfRoom = item.NoOfRoom,
                UserId = request.UserId,
                Mode = 1,
                CheckInSharers = false,
                IgnoreSharerAlerts = false,
                PaymentMethod = string.Empty,
                CreditCardNo = string.Empty,
                ExpireDate = null
            };

            CheckInService.CheckInResponse response = CheckInService.CreateCheckIn(createCheckInRequest);
            if (!response.Success)
            {
                return BuildFail(item, CombineResponseMessage(response));
            }

            string successMessage = string.IsNullOrWhiteSpace(response.Message)
                ? "Checked In.Room:" + item.RoomNo + "."
                : response.Message;

            return BuildSuccess(item, successMessage);
        }

        private static bool HasRoom(ReservationCheckInDto item)
        {
            return item != null && item.RoomId > 0;
        }

        private static bool MatchRoomStatus(int hkStatusId, int filterType)
        {
            return filterType switch
            {
                1 => hkStatusId == 4,
                2 => hkStatusId == 1,
                3 => hkStatusId == 1 || hkStatusId == 4,
                4 => true,
                _ => false
            };
        }

        private static bool IsValidFilterType(int filterType)
        {
            return filterType is >= 1 and <= 4;
        }

        private static bool IsBlockedByMainGuest(ReservationCheckInDto item, int blockedShareRoom)
        {
            if (item == null)
            {
                return false;
            }

            return !item.IsMainGuest
                   && blockedShareRoom > 0
                   && item.ShareRoom == blockedShareRoom;
        }

        #endregion

        #region Data Query

        private static bool TryGetFirstCheckInAlertMessage(int reservationId, out string message)
        {
            message = string.Empty;
            ArrayList alerts = ReservationUtil.GetReservationAlerts(reservationId);

            if (alerts == null || alerts.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < alerts.Count; i++)
            {
                if (alerts[i] is ReservationAlertsModel alert && alert.Area == "Check-In")
                {
                    message = alert.Description ?? string.Empty;
                    return true;
                }
            }

            return false;
        }

        private List<ReservationCheckInDto> GetReservationsByProfileGroup(int profileGroupId)
        {
            SqlParameter[] param =
            [
                new SqlParameter("@Status", "5"),
                new SqlParameter("@ProfileGroupID", profileGroupId)
            ];

            DataTable dt = DataTableHelper.getTableData("spGroupReservationToDelete", param);
            List<ReservationCheckInDto> result = [];

            foreach (DataRow row in dt.Rows)
            {
                result.Add(new ReservationCheckInDto
                {
                    ReservationId = row["ReservationID"] == DBNull.Value ? 0 : Convert.ToInt32(row["ReservationID"]),
                    RoomId = row["RoomID"] == DBNull.Value ? 0 : Convert.ToInt32(row["RoomID"]),
                    NoOfRoom = row["Rms"] == DBNull.Value ? 0 : Convert.ToInt32(row["Rms"]),
                    RoomNo = row["Room"] == DBNull.Value ? string.Empty : row["Room"].ToString() ?? string.Empty,
                    HKStatusId = row["HKStatusID"] == DBNull.Value ? 0 : Convert.ToInt32(row["HKStatusID"]),
                    IsMainGuest = row.Table.Columns.Contains("IsMainGuest")
                        && row["IsMainGuest"] != DBNull.Value
                        && Convert.ToBoolean(row["IsMainGuest"]),
                    ShareRoom = row.Table.Columns.Contains("ShareRoom") && row["ShareRoom"] != DBNull.Value
                        ? Convert.ToInt32(row["ShareRoom"])
                        : 0
                });
            }

            return result;
        }

        private List<ReservationCheckInDto> GetReservationsByConfirmation(
            string confirmationNo,
            List<int> selectedReservationIds)
        {
            DataTable dt = _reservationService.ResConfNoList(Convert.ToInt32(confirmationNo));
            List<ReservationCheckInDto> result = [];

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

            if (selectedReservationIds == null || selectedReservationIds.Count == 0)
            {
                return result;
            }

            Dictionary<int, ReservationCheckInDto> itemByReservationId = result
                .GroupBy(x => x.ReservationId)
                .ToDictionary(x => x.Key, x => x.First());

            List<ReservationCheckInDto> orderedResult = [];

            foreach (int reservationId in selectedReservationIds)
            {
                if (itemByReservationId.TryGetValue(reservationId, out ReservationCheckInDto? item) && item != null)
                {
                    orderedResult.Add(item);
                }
            }

            return orderedResult;
        }

        #endregion

        #region Result Builder

        private static string CombineResponseMessage(CheckInService.CheckInResponse response)
        {
            if (response == null)
            {
                return "Group chek in not complete.";
            }

            if (string.IsNullOrWhiteSpace(response.Detail))
            {
                return response.Message ?? "Group chek in not complete.";
            }

            if (string.IsNullOrWhiteSpace(response.Message))
            {
                return response.Detail;
            }

            return response.Message + " " + response.Detail;
        }

        private static CheckInItemResult BuildFail(ReservationCheckInDto item, string message)
        {
            return new CheckInItemResult
            {
                ReservationId = item?.ReservationId ?? 0,
                RoomNo = item?.RoomNo ?? string.Empty,
                Success = false,
                Message = message
            };
        }

        private static CheckInItemResult BuildSuccess(ReservationCheckInDto item, string message)
        {
            return new CheckInItemResult
            {
                ReservationId = item?.ReservationId ?? 0,
                RoomNo = item?.RoomNo ?? string.Empty,
                Success = true,
                Message = message
            };
        }

        private static CheckInResult BuildErrorResult(string message, string? detail = null)
        {
            CheckInResult result = new()
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

        private static CheckInResult BuildNoAvailableResult()
        {
            CheckInResult result = new()
            {
                Success = false,
                TotalRequested = 0,
                TotalCheckedIn = 0,
                Message = "Room is not available for your requests."
            };

            result.Messages.Add(result.Message);
            return result;
        }

        #endregion
    }
}
