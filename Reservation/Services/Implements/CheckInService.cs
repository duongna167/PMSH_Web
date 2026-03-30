using System.Collections;
using System.Data;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using Microsoft.Data.SqlClient;

namespace Reservation.Services.Implements
{
    public class CheckInService
    {
        #region Public API

        /// <summary>
        /// mode: 0 = CI FIT; 1 = CI GROUP BY CONF.NO
        /// Web version:
        /// - không dùng MessageBox / frm*
        /// - mọi lựa chọn từ UI được truyền qua request
        /// - kết quả trả về bằng response model
        /// </summary>
        public static CheckInResponse CreateCheckIn(CheckInRequest request)
        {
            if (request == null)
                return CheckInResponse.Fail("Request is null.");

            ReservationModel reservation = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(request.ReservationId);
            if (reservation == null)
                return CheckInResponse.Fail("Reservation not found.");

            if (reservation.ProfileGroupId > 0 && request.NoOfRoom > 1)
                return CheckInResponse.SuccessResult(reservation.ID, "Reservation is group profile and cannot split in this flow.");

            if (!IsValidArrivalDate(reservation))
                return CheckInResponse.Fail("Arrival date must be equal to day.");

            CheckInContext context = BuildContext(request, reservation);
            ResolveRoomAndPayment(context);

            if (context.AssignedRoomId <= 0)
                return CheckInResponse.Fail("Room No. must be valid before check in.");

            LoadSharers(context);
            LoadSharerAlerts(context);
            ApplySharerDecision(context);

            ProcessTransactions pt = new();
            pt.OpenConnection();
            pt.BeginTransaction();

            try
            {
                PrepareOriginalValues(context);

                if (IsSingleRoomWithAssignedRoom(context))
                {
                    ProcessSingleRoomWithExistingRoom(context, pt);
                }
                else if (IsSingleRoomWithoutAssignedRoom(context))
                {
                    ProcessSingleRoomWithoutExistingRoom(context, pt);
                }
                else if (IsMultiRoomSplitCase(context))
                {
                    ProcessMultiRoomSplit(context, pt);
                }
                else
                {
                    throw new Exception("Unsupported check-in case.");
                }

                pt.CommitTransaction();
            }
            catch (Exception ex)
            {
                pt.CloseConnection();
                return CheckInResponse.Fail("Group check in not complete.", ex.Message);
            }
            finally
            {
                pt.CloseConnection();
            }

            if (context.Reservation.RoomId > 0 && context.Reservation.MainGuest == true && context.Mode == 1)
                ReservationBO.UpdateReservationStatus(null, context.Reservation.RoomId);

            return BuildSuccessResponse(context);
        }

        #endregion

        #region Context + validation

        private static bool IsValidArrivalDate(ReservationModel reservation)
        {
            return TextUtils.CompareDate(TextUtils.GetBusinessDate(), reservation.ArrivalDate) == 0;
        }

        private static CheckInContext BuildContext(CheckInRequest request, ReservationModel reservation)
        {
            return new CheckInContext
            {
                ReservationId = request.ReservationId,
                InputRoomId = request.RoomId,
                AssignedRoomId = request.RoomId,
                InputNoOfRoom = request.NoOfRoom,
                UserId = request.UserId,
                Mode = request.Mode,
                Reservation = reservation,
                SystemDate = TextUtils.GetSystemDate(),
                BusinessDate = TextUtils.GetBusinessDate(),
                PaymentMethod = request.PaymentMethod ?? string.Empty,
                CreditCardNo = request.CreditCardNo ?? string.Empty,
                ExpireDate = request.ExpireDate ?? TextUtils.GetSystemDate(),
                CheckInSharers = request.CheckInSharers,
                IgnoreSharerAlerts = request.IgnoreSharerAlerts,
                RequestedRoomIdFromUi = request.SelectedRoomId,
                ResultMessage = string.Empty
            };
        }

        private static void ResolveRoomAndPayment(CheckInContext context)
        {
            if (context.InputRoomId > 0)
                return;

            context.AssignedRoomId = context.RequestedRoomIdFromUi;
        }

        private static void PrepareOriginalValues(CheckInContext context)
        {
            context.OriginalReservationId = context.Reservation.ID;
            context.OriginalNoOfRoom = context.Reservation.NoOfRoom;
        }

        private static bool IsSingleRoomWithAssignedRoom(CheckInContext context)
        {
            return context.InputRoomId > 0 && (context.InputNoOfRoom == 1 || context.InputNoOfRoom == 0);
        }

        private static bool IsSingleRoomWithoutAssignedRoom(CheckInContext context)
        {
            return context.InputRoomId == 0 && (context.InputNoOfRoom == 1 || context.InputNoOfRoom == 0);
        }

        private static bool IsMultiRoomSplitCase(CheckInContext context)
        {
            return context.InputNoOfRoom > 1 && context.Reservation.ProfileGroupId == 0;
        }

        #endregion

        #region Sharers + alerts

        private static void LoadSharers(CheckInContext context)
        {
            SqlParameter[] param =
            [
                new SqlParameter("@ReservationID", context.ReservationId),
                new SqlParameter("@ShareRoom", context.Reservation.ShareRoom),
                new SqlParameter("@ArrivalDate", TextUtils.GetBusinessDate())
            ];

            context.Sharers = DataTableHelper.getTableData("spCheckReservationRoomSharer", param);
        }

        private static void LoadSharerAlerts(CheckInContext context)
        {
            context.SharerAlerts = [];

            if (context.Sharers == null || context.Sharers.Rows.Count <= 0)
                return;

            for (int pa = 0; pa < context.Sharers.Rows.Count; pa++)
            {
                int reservationId = int.Parse(context.Sharers.Rows[pa]["ID"].ToString());
                ArrayList arrAlert = ReservationUtil.GetReservationAlerts(reservationId);
                if (arrAlert.Count <= 0)
                    continue;

                for (int i = 0; i < arrAlert.Count; i++)
                {
                    ReservationAlertsModel alert = (ReservationAlertsModel)arrAlert[i];
                    if (alert.Area == "Check-In")
                    {
                        context.SharerAlerts.Add(new CheckInAlertInfo
                        {
                            ReservationId = reservationId,
                            AlertId = alert.ID,
                            Area = alert.Area
                        });
                    }
                }
            }
        }

        private static void ApplySharerDecision(CheckInContext context)
        {
            if (context.Mode != 0)
                return;

            if (context.SharerAlerts.Count > 0 && !context.IgnoreSharerAlerts)
            {
                context.CheckInSharers = false;
                return;
            }

            if (context.Sharers == null || context.Sharers.Rows.Count <= 0)
                context.CheckInSharers = false;
        }

        #endregion

        #region Case 1: NoOfRoom = 0/1 and already has room

        private static void ProcessSingleRoomWithExistingRoom(CheckInContext context, ProcessTransactions pt)
        {
            CheckInMainGuestExistingRoom(context, pt);

            if (context.CheckInSharers)
                CheckInSharersExistingRoom(context, pt);

            context.ResultMessage = "Check in completed successfully.";
        }

        private static void CheckInMainGuestExistingRoom(CheckInContext context, ProcessTransactions pt)
        {
            ReservationModel reservation = context.Reservation;

            reservation.ID = context.ReservationId;
            reservation.Status = 1;
            reservation.CheckInDate = context.SystemDate;
            pt.Update(reservation);

            UpdateRoomStatus(context.AssignedRoomId, pt);
            EnsureMainGuestFolio(reservation, context.ReservationId, pt);
            TransferDepositForMainGuest(reservation, context.ReservationId, pt);
            UpsertReservationOption(context.ReservationId, pt);
            UpdateMasterFolioIfNeeded(reservation, pt);
            TextUtils.InsertActivityLog("Reservation", context.ReservationId, "Status", "DUE IN", "CHECKED IN", "");
            ReservationUtil.IF_GI(reservation, context.ReservationId, pt);
        }

        private static void CheckInSharersExistingRoom(CheckInContext context, ProcessTransactions pt)
        {
            for (int i = 0; i < context.Sharers.Rows.Count; i++)
            {
                int sharerReservationId = TextUtils.ToInt(context.Sharers.Rows[i]["ID"].ToString());
                int sharerProfileId = TextUtils.ToInt(context.Sharers.Rows[i]["ProfileID"].ToString());
                string sharerName = context.Sharers.Rows[i]["Name"].ToString();

                string sqlRS = "UPDATE Reservation with (rowlock) SET Status = 1, " +
                               "CheckInDate = '" + TextUtils.GetSystemDate().ToString("yyyy/MM/dd HH:mm") + "' " +
                               "WHERE ID = " + sharerReservationId + " ";
                pt.UpdateCommand(sqlRS);

                EnsureSharerFolio(sharerReservationId, context.Reservation.ConfirmationNo, pt);
                TransferDepositForSharer(context.Reservation.ConfirmationNo, sharerReservationId, sharerProfileId, sharerName, pt);
                UpsertReservationOption(sharerReservationId, pt);
                TextUtils.InsertActivityLog("Reservation", sharerReservationId, "Status", "DUE IN", "CHECKED IN", "");
                ReservationUtil.IF_GI(null, sharerReservationId, pt);
            }
        }

        #endregion

        #region Case 2: NoOfRoom = 0/1 and no room before check-in

        private static void ProcessSingleRoomWithoutExistingRoom(CheckInContext context, ProcessTransactions pt)
        {
            AssignRoomToMainReservation(context, pt);
            CheckInMainGuestWithoutRoom(context, pt);

            if (context.CheckInSharers)
                CheckInSharersWithoutRoom(context, pt);
            else
                AssignRoomOnlyToSharers(context, pt);

            context.ResultMessage = "Check in completed successfully.";
        }

        private static void AssignRoomToMainReservation(CheckInContext context, ProcessTransactions pt)
        {
            ReservationModel reservation = context.Reservation;
            RoomModel room = (RoomModel)pt.FindByPK("Room", context.AssignedRoomId);
            RoomTypeModel roomType = (RoomTypeModel)pt.FindByPK("RoomType", room.RoomTypeID);

            reservation.ID = context.ReservationId;
            reservation.RoomId = context.AssignedRoomId;
            reservation.RoomNo = room.RoomNo;
            reservation.RoomTypeId = room.RoomTypeID;
            reservation.RoomType = roomType.Code;
            if (reservation.RateCodeId == 0)
                reservation.RtcId = reservation.RoomTypeId;
            reservation.Status = 1;
            reservation.CheckInDate = context.SystemDate;
            pt.Update(reservation);

            string sqlRR = "UPDATE ReservationRate with (rowlock) SET RoomID = " + context.AssignedRoomId + " , RoomNo = '" + reservation.RoomNo + "', " +
                           "RoomTypeID = " + reservation.RoomTypeId + ",RoomType = '" + reservation.RoomType + "', RTCID = " + reservation.RtcId + "  " +
                           "WHERE ID IN (SELECT ID FROM ReservationRate WITH (NOLOCK) WHERE ReservationID = " + context.ReservationId + ") ";
            pt.UpdateCommand(sqlRR);
        }

        private static void CheckInMainGuestWithoutRoom(CheckInContext context, ProcessTransactions pt)
        {
            UpdateRoomStatus(context.AssignedRoomId, pt);
            EnsureMainGuestFolio(context.Reservation, context.ReservationId, pt);
            TransferDepositForMainGuest(context.Reservation, context.ReservationId, pt);
            UpsertReservationOption(context.ReservationId, pt);
            UpdateMasterFolioIfNeeded(context.Reservation, pt);
            TextUtils.InsertActivityLog("Reservation", context.ReservationId, "Status", "DUE IN", "CHECKED IN", "");
            ReservationUtil.IF_GI(context.Reservation, context.ReservationId, pt);
        }

        private static void CheckInSharersWithoutRoom(CheckInContext context, ProcessTransactions pt)
        {
            for (int i = 0; i < context.Sharers.Rows.Count; i++)
            {
                int sharerReservationId = TextUtils.ToInt(context.Sharers.Rows[i]["ID"].ToString());
                int sharerProfileId = TextUtils.ToInt(context.Sharers.Rows[i]["ProfileID"].ToString());
                string sharerName = context.Sharers.Rows[i]["Name"].ToString();
                ReservationModel reservation = context.Reservation;

                string sqlRS = "UPDATE Reservation with (rowlock) SET Status = 1, RoomID = " + context.AssignedRoomId + ",RoomNo = '" + reservation.RoomNo + "', " +
                               "RoomTypeID = " + reservation.RoomTypeId + ", RoomType = '" + reservation.RoomType + "', RTCID = " + reservation.RtcId + ", " +
                               "CheckInDate = '" + context.SystemDate.ToString("yyyy/MM/dd HH:mm") + "' " +
                               "WHERE ID = " + sharerReservationId + " ";
                pt.UpdateCommand(sqlRS);

                string sqlRRS = "UPDATE ReservationRate with (rowlock) SET RoomID = " + context.AssignedRoomId + ",RoomNo = '" + reservation.RoomNo + "', " +
                                "RoomTypeID = " + reservation.RoomTypeId + ",RoomType = '" + reservation.RoomType + "', RTCID = " + reservation.RtcId + " " +
                                "WHERE ID IN (SELECT ID FROM ReservationRate WITH (NOLOCK) WHERE ReservationID = " + sharerReservationId + ") ";
                pt.UpdateCommand(sqlRRS);

                EnsureSharerFolio(sharerReservationId, reservation.ConfirmationNo, pt);
                TransferDepositForSharer(reservation.ConfirmationNo, sharerReservationId, sharerProfileId, sharerName, pt);
                UpsertReservationOption(sharerReservationId, pt);
                TextUtils.InsertActivityLog("Reservation", sharerReservationId, "Status", "DUE IN", "CHECKED IN", "");
                ReservationUtil.IF_GI(null, sharerReservationId, pt);
            }
        }

        private static void AssignRoomOnlyToSharers(CheckInContext context, ProcessTransactions pt)
        {
            for (int i = 0; i < context.Sharers.Rows.Count; i++)
            {
                int sharerReservationId = TextUtils.ToInt(context.Sharers.Rows[i]["ID"].ToString());
                ReservationModel reservation = context.Reservation;

                string sqlRS = "UPDATE Reservation with (rowlock) SET RoomID = " + context.AssignedRoomId + ",RoomNo = '" + reservation.RoomNo + "', " +
                               "RoomTypeID = " + reservation.RoomTypeId + ",RoomType = '" + reservation.RoomType + "' " +
                               "WHERE ID = " + sharerReservationId + " ";
                pt.UpdateCommand(sqlRS);

                string sqlRRS = "UPDATE ReservationRate with (rowlock) SET RoomID = " + context.AssignedRoomId + ",RoomNo = '" + reservation.RoomNo + "', " +
                                "RoomTypeID = " + reservation.RoomTypeId + ", RoomType = '" + reservation.RoomType + "' " +
                                "WHERE ID IN (SELECT ID FROM ReservationRate WITH (NOLOCK) WHERE ReservationID = " + sharerReservationId + ") ";
                pt.UpdateCommand(sqlRRS);
            }
        }

        #endregion

        #region Case 3: NoOfRoom > 1 and split to new reservation

        private static void ProcessMultiRoomSplit(CheckInContext context, ProcessTransactions pt)
        {
            int newProfileId = CreateSplitProfile(context, pt);
            InsertSplitReservation(context, newProfileId, pt);
            CopyReservationRateForSplit(context, pt);
            EnsureMainGuestFolio(context.Reservation, context.ReservationId, pt);
            ReservationBO.CopyTbReservationPackage(context.OriginalReservationId, context.ReservationId, context.UserId, pt);
            ReservationBO.CopyTbReservationFixedCharge(context.OriginalReservationId, context.ReservationId, context.UserId, pt);
            UpdateRoomStatus(context.AssignedRoomId, pt);
            TransferDepositForMainGuest(context.Reservation, context.ReservationId, pt);
            pt.DeleteByAttribute("ReservationAmountByCurrency", "ReservationID", context.ReservationId.ToString());
            ReservationBO.GetAmountByCurrency(context.ReservationId, context.UserId, pt);
            ReservationUtil.CreateReservationGroup(context.ReservationId, context.Reservation.ConfirmationNo, context.UserId, "", pt);
            UpsertReservationOption(context.ReservationId, pt);
            UpdateMasterFolioIfNeeded(context.Reservation, pt);
            TextUtils.InsertActivityLog("Reservation", context.ReservationId, "Status", "DUE IN", "CHECKED IN", "");
            ReservationUtil.IF_GI(context.Reservation, context.ReservationId, pt);
            context.ResultMessage = "Split reservation and check in completed successfully.";
        }

        private static int CreateSplitProfile(CheckInContext context, ProcessTransactions pt)
        {
            ProfileModel profile = (ProfileModel)pt.FindByPK("Profile", context.Reservation.ProfileIndividualId);
            profile.Code = ProfileBO.Instance.GenerateNo3("Code");
            profile.ReturnGuest = -1;
            profile.StayNo = 0;
            profile.GuestNo = profile.Occupation = profile.Birthplace = string.Empty;
            profile.BonusPoints = profile.GuestGroupID = 0;
            profile.ExpressCheckout = profile.PayTV = false;
            profile.CreditCard = profile.RateCode = string.Empty;
            profile.RoomNights = profile.BedNights = 0;
            profile.TotalTurnover = profile.LodgeTurnover = profile.LodgePackageTurover = profile.FBTurnover = profile.EventTurnover = profile.OtherTurnover = 0;
            profile.FirstReservation = Convert.ToDateTime("01/01/1900");
            profile.LastReservation = Convert.ToDateTime("01/01/1900");
            profile.WeddingAnniversary = Convert.ToDateTime("01/01/1900");
            profile.Firstvisit = Convert.ToDateTime("01/01/1900");
            profile.Expiry = Convert.ToDateTime("01/01/1900");
            profile.LastContact = Convert.ToDateTime("01/01/1900");
            return (int)pt.Insert(profile);
        }

        private static void InsertSplitReservation(CheckInContext context, int profileId, ProcessTransactions pt)
        {
            ReservationModel reservation = context.Reservation;
            RoomModel room = (RoomModel)pt.FindByPK("Room", context.AssignedRoomId);
            RoomTypeModel roomType = (RoomTypeModel)pt.FindByPK("RoomType", room.RoomTypeID);

            reservation.UserInsertId = context.UserId;
            reservation.CreateDate = context.SystemDate;
            reservation.UpdateDate = context.SystemDate;
            reservation.UserUpdateId = context.UserId;
            reservation.ReservationDate = context.BusinessDate;
            reservation.CheckInDate = context.SystemDate;
            reservation.Status = 1;
            reservation.MainGuest = true;
            reservation.PostingMaster = false;
            reservation.NoOfRoom = 1;
            reservation.RoomId = context.AssignedRoomId;
            reservation.RoomNo = room.RoomNo;
            reservation.RoomTypeId = room.RoomTypeID;
            reservation.RoomType = roomType.Code;
            if (reservation.RateCodeId == 0)
                reservation.RtcId = reservation.RoomTypeId;
            reservation.PaymentMethod = context.PaymentMethod;
            reservation.CreditCardNo = context.CreditCardNo;
            reservation.ExpirationDate = context.ExpireDate;
            reservation.ProfileIndividualId = profileId;

            context.ReservationId = (int)pt.Insert(reservation);

            reservation.ID = context.ReservationId;
            reservation.ReservationNo = context.ReservationId.ToString();
            reservation.ShareRoom = context.ReservationId;
            reservation.Relationship = context.OriginalReservationId;
            pt.Update(reservation);

            string sqlOriginal = "UPDATE Reservation with (rowlock) Set NoOfRoom = " + context.OriginalNoOfRoom + " - 1 WHERE ID = " + context.OriginalReservationId + " ";
            pt.UpdateCommand(sqlOriginal);
        }

        private static void CopyReservationRateForSplit(CheckInContext context, ProcessTransactions pt)
        {
            DataTable rates = TextUtils.getTable("spCheckReservationRate", new SqlParameter("@ReservationID", context.OriginalReservationId));

            for (int i = 0; i < rates.Rows.Count; i++)
            {
                ReservationRateModel rate = new ReservationRateModel();
                rate.ReservationID = context.ReservationId;
                rate.RateCodeID = Convert.ToInt32(rates.Rows[i]["RateCodeID"]);
                rate.RateDate = Convert.ToDateTime(rates.Rows[i]["RateDate"]);
                rate.RateDate = new DateTime(rate.RateDate.Year, rate.RateDate.Month, rate.RateDate.Day, 0, 0, 0);
                rate.Rate = Convert.ToDecimal(rates.Rows[i]["Rate"]);
                rate.CurrencyID = rates.Rows[i]["CurrencyID"].ToString();
                rate.FixedRate = bool.Parse(rates.Rows[i]["FixedRate"].ToString());
                rate.RoomID = context.AssignedRoomId;
                rate.RoomNo = ((RoomModel)pt.FindByPK("Room", context.AssignedRoomId)).RoomNo;
                rate.RoomTypeID = ((RoomModel)pt.FindByPK("Room", context.AssignedRoomId)).RoomTypeID;
                rate.RoomType = ((RoomTypeModel)pt.FindByPK("RoomType", rate.RoomTypeID)).Code;
                rate.RTCID = rate.RoomTypeID;
                rate.UserInsertID = context.UserId;
                rate.CreateDate = context.SystemDate;
                rate.UpdateDate = context.SystemDate;
                rate.UserUpdateID = context.UserId;
                rate.ID = (int)pt.Insert(rate);
            }
        }

        #endregion

        #region Shared business helpers

        private static void UpdateRoomStatus(int roomId, ProcessTransactions pt)
        {
            string sqlRms = "UPDATE Room with (rowlock) SET FOStatus = 1, HKFOStatus = 1 WHERE ID = " + roomId + " ";
            pt.UpdateCommand(sqlRms);
        }

        private static void EnsureMainGuestFolio(ReservationModel reservation, int reservationId, ProcessTransactions pt)
        {
            int folioId;

            if (reservation.ReservationNo != "0")
            {
                folioId = ReservationBO.GetFolioID(reservationId, 1, reservation.ConfirmationNo, pt);
                if (folioId == 0)
                    ReservationBO.CreateFolioNoRouting(reservationId, 1, pt);
                else
                    ReservationBO.UpdateFolioNoRouting(folioId, reservation.ProfileIndividualId, reservation.LastName, reservation.ConfirmationNo, 1, pt);

                ArrayList routingIds = ReservationBO.CheckRouting(reservationId, reservationId, reservation.ConfirmationNo, pt);
                if (routingIds != null)
                {
                    for (int i = 0; i < routingIds.Count; i++)
                    {
                        RoutingModel routing = (RoutingModel)routingIds[i];
                        folioId = ReservationBO.GetFolioID(routing.ToReservationID, routing.ToFolioNo, routing.ConfirmationNo, pt);
                        if (folioId == 0)
                        {
                            ReservationBO.CreateFolio(routing.ID, reservation.ConfirmationNo, pt);
                        }
                        else if (routing.ToFolioNo != -1)
                        {
                            ReservationBO.UpdateFolioNoRouting(folioId, routing.ProfileID, routing.AccountName, routing.ConfirmationNo, routing.ToFolioNo, pt);
                        }
                    }
                }
            }
            else
            {
                folioId = ReservationBO.GetFolioID(reservationId, -1, reservation.ConfirmationNo, pt);
                if (folioId == 0)
                    ReservationBO.CreateFolioNoRouting(reservationId, -1, pt);
                else
                    ReservationBO.UpdateFolioNoRouting(folioId, reservation.ProfileIndividualId, reservation.LastName, reservation.ConfirmationNo, -1, pt);
            }
        }

        private static void EnsureSharerFolio(int reservationId, string confirmationNo, ProcessTransactions pt)
        {
            int folioId = ReservationBO.GetFolioID(reservationId, 1, confirmationNo, pt);
            if (folioId == 0)
                ReservationBO.CreateFolioNoRouting(reservationId, 1, pt);

            ArrayList routingIds = ReservationBO.CheckRouting(reservationId, reservationId, confirmationNo, pt);
            if (routingIds == null)
                return;

            for (int j = 0; j < routingIds.Count; j++)
            {
                RoutingModel routing = (RoutingModel)routingIds[j];
                folioId = ReservationBO.GetFolioID(routing.ToReservationID, routing.ToFolioNo, routing.ConfirmationNo, pt);
                if (folioId == 0)
                    ReservationBO.CreateFolio(routing.ID, confirmationNo, pt);
            }
        }

        private static void TransferDepositForMainGuest(ReservationModel reservation, int reservationId, ProcessTransactions pt)
        {
            string err = string.Empty;
            CasheringUtils.TranferDeposit(pt, reservation.ConfirmationNo, reservationId, reservation.ProfileIndividualId, reservation.LastName, ref err);
        }

        private static void TransferDepositForSharer(string confirmationNo, int reservationId, int profileId, string name, ProcessTransactions pt)
        {
            string err = string.Empty;
            CasheringUtils.TranferDeposit(pt, confirmationNo, reservationId, profileId, name, ref err);
        }

        private static void UpsertReservationOption(int reservationId, ProcessTransactions pt)
        {
            int reservationOptionId = ReservationBO.GetReservationOptionID(reservationId, pt);
            if (reservationOptionId == 0)
            {
                ReservationOptionsModel option = new ReservationOptionsModel
                {
                    ReservationID = reservationId,
                    Billing = true
                };
                pt.Insert(option);
            }
            else
            {
                ReservationOptionsModel option = (ReservationOptionsModel)pt.FindByPK("ReservationOptions", reservationOptionId);
                option.ID = reservationOptionId;
                option.Billing = true;
                pt.Update(option);
            }
        }

        private static void UpdateMasterFolioIfNeeded(ReservationModel reservation, ProcessTransactions pt)
        {
            if (reservation.MainGuest == true && (reservation.ProfileAgentId > 0 || reservation.ProfileCompanyId > 0))
                CheckInUtil.CheckInMasterFolio(reservation.ConfirmationNo, pt);
        }

        private static CheckInResponse BuildSuccessResponse(CheckInContext context)
        {
            return new CheckInResponse
            {
                Success = true,
                ReservationId = context.ReservationId,
                AssignedRoomId = context.AssignedRoomId,
                Message = context.ResultMessage,
                SharerCheckedIn = context.CheckInSharers,
                Alerts = context.SharerAlerts
            };
        }

        #endregion

        #region Request/Response models

        public class CheckInRequest
        {
            public int ReservationId { get; set; }
            public int RoomId { get; set; }
            public int SelectedRoomId { get; set; }
            public int NoOfRoom { get; set; }
            public int UserId { get; set; }
            public int Mode { get; set; }
            public bool CheckInSharers { get; set; }
            public bool IgnoreSharerAlerts { get; set; }
            public string PaymentMethod { get; set; }
            public string CreditCardNo { get; set; }
            public DateTime? ExpireDate { get; set; }
        }

        public class CheckInResponse
        {
            public bool Success { get; set; }
            public int ReservationId { get; set; }
            public int AssignedRoomId { get; set; }
            public bool SharerCheckedIn { get; set; }
            public string Message { get; set; }
            public string Detail { get; set; }
            public List<CheckInAlertInfo> Alerts { get; set; } = [];

            public static CheckInResponse Fail(string message, string detail = "")
            {
                return new CheckInResponse
                {
                    Success = false,
                    Message = message,
                    Detail = detail
                };
            }

            public static CheckInResponse SuccessResult(int reservationId, string message)
            {
                return new CheckInResponse
                {
                    Success = true,
                    ReservationId = reservationId,
                    Message = message
                };
            }
        }

        public class CheckInAlertInfo
        {
            public int ReservationId { get; set; }
            public int AlertId { get; set; }
            public string Area { get; set; }
        }

        #endregion

        #region Internal context

        private sealed class CheckInContext
        {
            public int ReservationId { get; set; }
            public int InputRoomId { get; set; }
            public int RequestedRoomIdFromUi { get; set; }
            public int AssignedRoomId { get; set; }
            public int InputNoOfRoom { get; set; }
            public int UserId { get; set; }
            public int Mode { get; set; }
            public int OriginalReservationId { get; set; }
            public int OriginalNoOfRoom { get; set; }
            public bool CheckInSharers { get; set; }
            public bool IgnoreSharerAlerts { get; set; }
            public string PaymentMethod { get; set; }
            public string CreditCardNo { get; set; }
            public DateTime ExpireDate { get; set; }
            public DateTime SystemDate { get; set; }
            public DateTime BusinessDate { get; set; }
            public string ResultMessage { get; set; }
            public ReservationModel Reservation { get; set; }
            public DataTable Sharers { get; set; }
            public List<CheckInAlertInfo> SharerAlerts { get; set; } = [];
        }

        #endregion
    }
}
