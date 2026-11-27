using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace FrontDesk.Controllers
{
    /// <summary>
    /// Menu FRONTDESK — port từ WinForm:
    ///  - Trace Search      (frmTraceSearch, mode 0 = tất cả trace)
    ///  - Account Management (frmAccounts: tạo booking Posting Master cho khách / dummy)
    ///  - Report of staying (frmSearchStaying, _V2, _NN, V4, V5)
    /// </summary>
    public partial class FrontDeskController
    {
        private int FdkUserId => HttpContext.Session.GetInt32("UserID") ?? 0;
        private string FdkUserName => HttpContext.Session.GetString("LoginName") ?? "";

        /// <summary>DataTable → list dictionary; ngày dd/MM/yyyy (năm ≤ 1900 → rỗng), giữ kiểu số.</summary>
        private static List<Dictionary<string, object>> FdkRows(DataTable dt, Func<DataColumn, DateTime, string> fmtDate = null)
        {
            var list = new List<Dictionary<string, object>>();
            if (dt == null) return list;
            foreach (DataRow r in dt.Rows)
            {
                var d = new Dictionary<string, object>();
                foreach (DataColumn c in dt.Columns)
                {
                    object v = r[c];
                    if (v == null || v == DBNull.Value) d[c.ColumnName] = null;
                    else if (v is DateTime t) d[c.ColumnName] = t.Year <= 1900 ? "" : (fmtDate != null ? fmtDate(c, t) : t.ToString("dd/MM/yyyy"));
                    else d[c.ColumnName] = v;
                }
                list.Add(d);
            }
            return list;
        }

        private static string FdkRsvStatus(int s) => s switch
        {
            0 or 5 => "RESERVED",
            1 or 6 => "CHECKED IN",
            2 => "CHECKED OUT",
            _ => ""
        };

        #region Trace Search (frmTraceSearch)

        public IActionResult TraceSearch()
        {
            ViewBag.Departments = PropertyUtils.ConvertToList<DepartmentModel>(DepartmentBO.Instance.FindAll());
            return PartialView();
        }

        /// <summary>WinForm LoadData: spReservationTracesSearch, @Date = "" khi bỏ tick ngày, @ReservationID = "" (mode 0).</summary>
        [HttpGet]
        public IActionResult TraceSearchData(string name, string resolved, string departmentIds, int useDate, DateTime? date)
        {
            try
            {
                var prm = new[]
                {
                    new SqlParameter("@Name", (name ?? "").Trim()),
                    new SqlParameter("@Resolved", resolved ?? ""),
                    new SqlParameter("@Department", TextUtils.GetSplitString(departmentIds ?? "")),
                    new SqlParameter("@Date", useDate == 1 && date.HasValue ? date.Value.ToString("yyyy-MM-dd") : ""),
                    new SqlParameter("@ReservationID", "")
                };
                DataTable dt = DataTableHelper.getTableData("spReservationTracesSearch", prm);
                var rows = FdkRows(dt, (c, t) =>
                    c.ColumnName.Equals("FromDate", StringComparison.OrdinalIgnoreCase) || c.ColumnName.Equals("ToDate", StringComparison.OrdinalIgnoreCase)
                        ? t.ToString("dd/MM/yy") : t.ToString("dd.MM.yyyy HH:mm"));
                return Json(rows);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { code = 1, msg = ex.Message });
            }
        }

        /// <summary>WinForm grdGrid_Click: nội dung trace + Arrival / Departure / Room / Status của booking.</summary>
        [HttpGet]
        public IActionResult TraceSearchDetail(int id)
        {
            try
            {
                var t = (ReservationTracesModel)ReservationTracesBO.Instance.FindByPrimaryKey(id);
                if (t == null || t.ID == 0) return Json(new { code = 1, msg = "Trace not found" });
                string arrival = "", departure = "", room = "", status = "";
                if (t.ReservationID > 0)
                {
                    var r = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(t.ReservationID);
                    if (r != null && r.ID > 0)
                    {
                        arrival = r.ArrivalDate.ToString("dd/MM/yyyy");
                        departure = r.DepartureDate.ToString("dd/MM/yyyy");
                        status = FdkRsvStatus(r.Status);
                        if (r.RoomId > 0)
                        {
                            var rm = (RoomModel)RoomBO.Instance.FindByPrimaryKey(r.RoomId);
                            room = rm?.RoomNo ?? "";
                        }
                    }
                }
                return Json(new
                {
                    code = 0,
                    id = t.ID,
                    reservationID = t.ReservationID,
                    text = t.TracesText ?? "",
                    fromDate = t.FromDate.ToString("yyyy-MM-dd"),
                    toDate = t.ToDate.ToString("yyyy-MM-dd"),
                    time = string.IsNullOrWhiteSpace(t.TracesTime) ? "00:00" : t.TracesTime,
                    departmentID = t.DepartmentID,
                    resolved = t.Resolved,
                    arrival, departure, room, status
                });
            }
            catch (Exception ex)
            {
                return Json(new { code = 1, msg = ex.Message });
            }
        }

        /// <summary>New (frmTraces) khi id = 0, Edit (frmTracesEdit) khi id > 0.</summary>
        [HttpPost]
        public IActionResult TraceSearchSave(int id, int reservationID, DateTime fromDate, DateTime toDate, string time, int departmentID, string text)
        {
            try
            {
                if (departmentID <= 0) return Json(new { code = 1, msg = "Please choose department" });
                if (string.IsNullOrWhiteSpace(text)) return Json(new { code = 1, msg = "Trace text cant not be blank" });
                if (toDate.Date < fromDate.Date) return Json(new { code = 1, msg = "To Date must be on or after From Date" });
                DateTime now = TextUtils.GetSystemDate();

                if (id > 0)
                {
                    var t = (ReservationTracesModel)ReservationTracesBO.Instance.FindByPrimaryKey(id);
                    if (t == null || t.ID == 0) return Json(new { code = 1, msg = "Trace not found" });
                    string oldVal = $"[FromDate]{t.FromDate:dd/MM/yyyy},[ToDate]{t.ToDate:dd/MM/yyyy},[TraceTime]{t.TracesTime},[DepartmentID]{t.DepartmentID},[TraceText]{t.TracesText}";
                    t.FromDate = fromDate.Date;
                    t.ToDate = toDate.Date;
                    t.TracesTime = string.IsNullOrWhiteSpace(time) ? "00:00" : time;
                    t.DepartmentID = departmentID;
                    t.TracesText = text;
                    t.UserUpdateID = FdkUserId;
                    t.UpdateDate = now;
                    ReservationTracesBO.Instance.Update(t);
                    ReservationUtil.InsertActivityLog(t.ReservationID, FdkUserId, FdkUserName, "Trace", oldVal,
                        $"[FromDate]{t.FromDate:dd/MM/yyyy},[ToDate]{t.ToDate:dd/MM/yyyy},[TraceTime]{t.TracesTime},[DepartmentID]{t.DepartmentID},[TraceText]{t.TracesText}", "Edit Trace:" + t.ID);
                    return Json(new { code = 0, msg = "Trace was updated successfully" });
                }

                var rsv = reservationID > 0 ? (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(reservationID) : null;
                if (rsv == null || rsv.ID == 0) return Json(new { code = 1, msg = "Please select a reservation" });
                var n = new ReservationTracesModel
                {
                    FromDate = fromDate.Date,
                    ToDate = toDate.Date,
                    TracesTime = string.IsNullOrWhiteSpace(time) ? "00:00" : time,
                    DepartmentID = departmentID,
                    TracesText = text,
                    Resolved = 0,
                    ProfileID = rsv.ProfileIndividualId,
                    ReservationID = rsv.ID,
                    UserInsertID = FdkUserId,
                    UserUpdateID = FdkUserId,
                    CreateDate = now,
                    UpdateDate = now,
                    ResolvedBy = "",
                    ResolvedDate = new DateTime(1900, 1, 1),
                    IsDelete = false
                };
                long newId = ReservationTracesBO.Instance.Insert(n);
                FdkSetTracesFlag(rsv.ID, true);
                ReservationUtil.InsertActivityLog(rsv.ID, FdkUserId, FdkUserName, "Trace", "",
                    $"[FromDate]{n.FromDate:dd/MM/yyyy},[ToDate]{n.ToDate:dd/MM/yyyy},[TraceTime]{n.TracesTime},[DepartmentID]{n.DepartmentID},[TraceText]{n.TracesText}", "New Trace:" + newId);
                return Json(new { code = 0, msg = "Trace was created successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { code = 1, msg = ex.Message });
            }
        }

        /// <summary>WinForm btnDelete: IsDelete = 1; hết trace của booking thì tắt nút Traces (ClearButton).</summary>
        [HttpPost]
        public IActionResult TraceSearchDelete(int id)
        {
            try
            {
                var t = (ReservationTracesModel)ReservationTracesBO.Instance.FindByPrimaryKey(id);
                if (t == null || t.ID == 0) return Json(new { code = 1, msg = "Trace not found" });
                DataTableHelper.ExecuteNonQueryText("UPDATE ReservationTraces SET IsDelete = 1 WHERE ID = @ID", new[] { new SqlParameter("@ID", id) });
                if (t.ReservationID > 0)
                {
                    var left = TextUtils.Select($"SELECT COUNT(1) AS C FROM ReservationTraces WITH (NOLOCK) WHERE ReservationID = {t.ReservationID} AND ISNULL(IsDelete, 0) = 0");
                    if (left.Rows.Count > 0 && Convert.ToInt32(left.Rows[0]["C"]) == 0)
                        FdkSetTracesFlag(t.ReservationID, false);
                    ReservationUtil.InsertActivityLog(t.ReservationID, FdkUserId, FdkUserName, "Delete", "Trace:" + t.ID, "", "Trace");
                }
                return Json(new { code = 0, msg = "Trace was deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { code = 1, msg = ex.Message });
            }
        }

        /// <summary>WinForm btnResolve: Resolved = 1, ResolvedBy = user, ResolvedDate = giờ hệ thống.</summary>
        [HttpPost]
        public IActionResult TraceSearchResolve(List<int> ids)
        {
            try
            {
                ids = (ids ?? new List<int>()).Where(x => x > 0).Distinct().ToList();
                if (ids.Count == 0) return Json(new { code = 1, msg = "Please choose at least 1 trace" });
                DateTime now = TextUtils.GetSystemDate();
                foreach (int tid in ids)
                {
                    DataTableHelper.ExecuteNonQueryText(
                        "UPDATE ReservationTraces SET Resolved = 1, ResolvedBy = @By, ResolvedDate = @Date WHERE ID = @ID",
                        new[] { new SqlParameter("@By", FdkUserName), new SqlParameter("@Date", now), new SqlParameter("@ID", tid) });
                }
                return Json(new { code = 0, msg = "Resolved is Success" });
            }
            catch (Exception ex)
            {
                return Json(new { code = 1, msg = ex.Message });
            }
        }

        private static void FdkSetTracesFlag(int reservationId, bool on)
        {
            DataTableHelper.ExecuteNonQueryText(@"
                IF EXISTS (SELECT 1 FROM ReservationOptions WHERE ReservationID = @ID)
                    UPDATE ReservationOptions SET Traces = @On WHERE ReservationID = @ID
                ELSE IF @On = 1
                    INSERT INTO ReservationOptions (ReservationID, Traces) VALUES (@ID, 1)",
                new[] { new SqlParameter("@ID", reservationId), new SqlParameter("@On", on ? 1 : 0) });
        }

        #endregion

        #region Account Management (frmAccounts)

        public IActionResult AccountManagement()
        {
            return PartialView();
        }

        /// <summary>Danh sách chọn cho các nút ↓ (Name / Room Type / Market / Source).</summary>
        [HttpGet]
        public IActionResult AccountLookup(string kind, string q)
        {
            try
            {
                q = (q ?? "").Trim().Replace("'", "''");
                string sql = kind switch
                {
                    "profile" => $@"SELECT TOP 300 ID, Code, Account, ISNULL(Firstname,'') AS Firstname, Type,
                                        CASE Type WHEN 0 THEN 'Individual' WHEN 1 THEN 'Agent' WHEN 2 THEN 'Company'
                                                  WHEN 3 THEN 'Source' WHEN 4 THEN 'Group' WHEN 5 THEN 'Contact' ELSE '' END AS TypeName
                                     FROM Profile WITH (NOLOCK)
                                    WHERE (Account LIKE N'%{q}%' OR Code LIKE N'%{q}%')
                                    ORDER BY Account",
                    "roomtype" => "SELECT ID, Code, Name FROM RoomType WITH (NOLOCK) ORDER BY Code",
                    "market" => "SELECT ID, Code, Name FROM Market WITH (NOLOCK) ORDER BY Code",
                    "source" => "SELECT ID, Code, Name FROM Source WITH (NOLOCK) ORDER BY Code",
                    _ => null
                };
                if (sql == null) return Json(new List<object>());
                return Json(FdkRows(TextUtils.Select(sql)));
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { code = 1, msg = ex.Message });
            }
        }

        public class AccountSaveRequest
        {
            public int ProfileID { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public int RoomTypeID { get; set; }
            public string RoomType { get; set; }
            public int RoomID { get; set; }
            public string RoomNo { get; set; }
            public int MarketID { get; set; }
            public string Market { get; set; }
            public int SourceID { get; set; }
            public string Source { get; set; }
        }

        /// <summary>Model mới: chuỗi null → "", ngày chưa gán → 01/01/1900 (giống model WinForm, tránh lỗi NOT NULL / SqlDateTime).</summary>
        private static T FdkNewModel<T>() where T : new()
        {
            var m = new T();
            foreach (var p in typeof(T).GetProperties().Where(p => p.CanWrite))
            {
                if (p.PropertyType == typeof(string) && p.GetValue(m) == null) p.SetValue(m, "");
                else if (p.PropertyType == typeof(DateTime) && (DateTime)p.GetValue(m) == DateTime.MinValue) p.SetValue(m, new DateTime(1900, 1, 1));
            }
            return m;
        }

        /// <summary>WinForm GeneralInsert: tạo booking Posting Master; Arrival = Business Date thì check in luôn + tạo Folio 1 + bật Billing.</summary>
        [HttpPost]
        public IActionResult AccountSave([FromBody] AccountSaveRequest req)
        {
            if (req == null) return Json(new { code = 1, msg = "Invalid data" });

            // CheckInSert
            var errs = new List<string>();
            if (req.ProfileID <= 0) errs.Add("Chooice Guest");
            if (string.IsNullOrWhiteSpace(req.RoomType)) errs.Add("Room Type not blank");
            if (req.ToDate.Date < req.FromDate.Date) errs.Add("Departure >= Arrival");
            if (errs.Count > 0) return Json(new { code = 1, msg = string.Join("<br/>", errs) });

            var gp = (ProfileModel)ProfileBO.Instance.FindByPrimaryKey(req.ProfileID);
            if (gp == null || gp.ID == 0) return Json(new { code = 1, msg = "Chooice Guest" });

            int userId = FdkUserId;
            string userName = FdkUserName;
            DateTime sysDate = TextUtils.GetSystemDate();
            DateTime businessDate = TextUtils.GetBusinessDate();
            string maxConfirm = (ReservationBO.GetTopConfirmationNo() + 1).ToString();

            var pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                var mR = FdkNewModel<ReservationModel>();
                mR.ReservationDate = sysDate;

                // Profile: Individual → dùng luôn; loại khác → tạo profile "Room, Dummy" làm khách
                if (gp.Type == 0)
                {
                    mR.ProfileIndividualId = gp.ID;
                    mR.LastName = gp.Account ?? "";
                    mR.FirstName = gp.Firstname ?? "";
                }
                else
                {
                    var cr = pt.Select("SELECT TOP 1 MAX(CONVERT(int, Code)) AS Code FROM Profile WITH (NOLOCK) WHERE ISNUMERIC(Code) = 1");
                    string maxCode = cr.Rows.Count > 0 ? cr.Rows[0]["Code"].ToString() : "0";
                    int leg = maxCode.Length;
                    string code = "0000" + (Convert.ToUInt32(string.IsNullOrEmpty(maxCode) ? "0" : maxCode) + 1);
                    code = code.Remove(0, Math.Max(0, code.Length - leg));

                    var mP = (ProfileModel)pt.FindByPK("Profile", gp.ID);
                    mP.Code = code;
                    mP.Account = "Room, Dummy";
                    mP.Firstname = "Dummy";
                    mP.LastName = "Room";
                    mP.UserInsertID = mP.UserUpdateID = userId;
                    mP.CreateDate = mP.UpdateDate = sysDate;
                    mP.ReturnGuest = -1;
                    mP.StayNo = 0;
                    mP.GuestNo = mP.Occupation = mP.Birthplace = "";
                    mP.BonusPoints = mP.GuestGroupID = 0;
                    mP.ExpressCheckout = mP.PayTV = false;
                    mP.CreditCard = mP.RateCode = "";
                    mP.RoomNights = mP.BedNights = 0;
                    mP.TotalTurnover = mP.LodgeTurnover = mP.LodgePackageTurover = mP.FBTurnover = mP.EventTurnover = mP.OtherTurnover = 0;
                    var d1900 = new DateTime(1900, 1, 1);
                    mP.FirstReservation = mP.LastReservation = mP.WeddingAnniversary = mP.Firstvisit = mP.Expiry = mP.LastContact = d1900;
                    int dummyId = (int)pt.Insert(mP);

                    mR.ProfileIndividualId = dummyId;
                    mR.LastName = mP.Account;
                    mR.FirstName = mP.Firstname;
                }
                switch (gp.Type)
                {
                    case 1: mR.ProfileAgentId = gp.ID; mR.AgentName = gp.Account; mR.ProfileComment = gp.Description ?? ""; break;
                    case 2: mR.ProfileCompanyId = gp.ID; mR.CompanyName = gp.Account; mR.ProfileComment = gp.Description ?? ""; break;
                    case 3: mR.ProfileSourceId = gp.ID; mR.SourceName = gp.Account; mR.ProfileComment = gp.Description ?? ""; break;
                    case 4:
                        mR.ProfileGroupId = gp.ID; mR.GroupName = gp.Account; mR.ProfileComment = gp.Description ?? "";
                        // WinForm gán cả Contact khi Type = 4
                        mR.ProfileContactId = gp.ID; mR.ContactName = gp.Account;
                        break;
                }

                mR.ArrivalDate = mR.OriginalArrivalDate = req.FromDate.Date;
                mR.DepartureDate = mR.OriginalDepartureDate = req.ToDate.Date;
                mR.NoOfNight = 0;
                mR.NoOfAdult = 0;
                mR.NoOfChild = 0;
                mR.NoOfRoom = 1;
                mR.RoomTypeId = req.RoomTypeID;
                mR.RoomType = req.RoomType ?? "";
                mR.RtcId = req.RoomTypeID;
                if (!string.IsNullOrWhiteSpace(req.RoomNo)) { mR.RoomId = req.RoomID; mR.RoomNo = req.RoomNo; }
                mR.BusinessBlockId = 0;
                mR.Eta = "14:00";
                mR.Etd = "12:00";
                mR.CheckInDate = sysDate;
                mR.CheckOutDate = req.ToDate.Date;
                mR.ReservationTypeId = 0;
                if (!string.IsNullOrWhiteSpace(req.Market)) { mR.MarketId = req.MarketID; mR.MarketCode = req.Market; }
                if (!string.IsNullOrWhiteSpace(req.Source)) { mR.SourceId = req.SourceID; mR.SourceCode = req.Source; }
                mR.NoPost = false;
                mR.ConfirmationStatus = true;
                mR.Status = 0;
                mR.MainGuest = false;
                mR.PostingMaster = true;
                mR.ExpirationDate = DateTime.Now;
                mR.PickupArrivalDate = sysDate;
                mR.DropOffDepartureDate = sysDate;
                mR.UserInsertId = mR.UserUpdateId = userId;
                mR.CreateBy = mR.SpecialUpdateBy = mR.UpdateBy = userName;
                mR.CreateDate = mR.UpdateDate = mR.SpecialUpdateDate = sysDate;
                mR.ReservationNo = "";
                mR.CurrencyId = "USD";

                int reservationId = (int)pt.Insert(mR);
                mR.ID = reservationId;
                mR.ReservationNo = reservationId.ToString();
                mR.ShareRoom = mR.Relationship = reservationId;
                mR.ConfirmationNo = string.IsNullOrEmpty(maxConfirm) ? reservationId.ToString() : maxConfirm;

                bool checkIn = req.FromDate.Date == businessDate.Date;
                if (checkIn) mR.Status = 1;
                pt.Update(mR);

                if (checkIn)
                {
                    // Folio 1 cho khách
                    ReservationBO.CreateFolio(reservationId, 1, pt);

                    // ReservationOptions.Billing = true
                    var ro = pt.Select($"SELECT TOP 1 ID FROM ReservationOptions WITH (NOLOCK) WHERE ReservationID = {reservationId}");
                    if (ro.Rows.Count == 0)
                    {
                        var mRO = new ReservationOptionsModel { ReservationID = reservationId, Billing = true };
                        pt.Insert(mRO);
                    }
                    else
                    {
                        var mRO = (ReservationOptionsModel)pt.FindByPK("ReservationOptions", Convert.ToInt64(ro.Rows[0]["ID"]));
                        mRO.Billing = true;
                        pt.Update(mRO);
                    }
                }

                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Insert successful", confirmationNo = mR.ConfirmationNo });
            }
            catch (Exception ex)
            {
                try { pt.RollBack(); } catch { }
                return Json(new { code = 1, msg = ex.Message });
            }
            finally
            {
                pt.CloseConnection();
            }
        }

        #endregion

        #region Report of staying (frmSearchStaying, _V2, _NN, V4, V5)

        public IActionResult ReportOfStaying(int form = 1)
        {
            ViewBag.Form = form < 1 || form > 5 ? 1 : form;
            return PartialView();
        }

        /// <summary>
        /// form 1, 2: spGuestStayOver_New1 (@FromDate, @ToDate)
        /// form 3   : spGuestStayOver_New1 + @Country (ALL / VNM / NG) — frmSearchStaying_NN
        /// form 4   : SearchStayingV4; form 5: SearchStayingV5
        /// </summary>
        [HttpGet]
        public IActionResult ReportOfStayingData(int form, DateTime fromDate, DateTime toDate, string country)
        {
            try
            {
                string sp = form switch { 4 => "SearchStayingV4", 5 => "SearchStayingV5", _ => "spGuestStayOver_New1" };
                var prm = new List<SqlParameter>
                {
                    new SqlParameter("@FromDate", fromDate.Date),
                    new SqlParameter("@ToDate", toDate.Date)
                };
                if (form == 3) prm.Add(new SqlParameter("@Country", string.IsNullOrEmpty(country) ? "ALL" : country));
                DataTable dt = DataTableHelper.getTableData(sp, prm.ToArray());
                return Json(new
                {
                    code = 0,
                    columns = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList(),
                    rows = FdkRows(dt)
                });
            }
            catch (Exception ex)
            {
                return Json(new { code = 1, msg = ex.Message });
            }
        }

        #endregion
    }
}
