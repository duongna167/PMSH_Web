using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using BaseBusiness.Utils;
using DevExpress.ClipboardSource.SpreadsheetML;
using DevExpress.CodeParser;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NightAudit.Services.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace NightAudit.Controllers
{
    public class NightAuditController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<NightAuditController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IRoomRateService _iRoomRateService;
        public enum HistoryType { Gen_Post, Basic_Post, Night_Post, Early_Post, Advance_Post, Correct, Split, Tranferred, Deleted, Payment, Print };
        static int _HKP_VD_VC = 0;
        string _Error = "";
        int _IndexRunning = 0;
        DataTable dt_RoomType = null;
        bool _IsRunning = false;
        string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase).Substring(6);
        //  ProcessTransactions pt = null;


        string time = "";
        bool _IsOK = true;
        public string MasterCurrencyID = "VND";
        public const string _CURRENCY_1 = "VND";
        public const string _CURRENCY_2 = "USD";
        public DateTime SystemDate = DateTime.Now;

        public NightAuditController(ILogger<NightAuditController> logger,
                IMemoryCache cache, IConfiguration configuration, IRoomRateService iRoomRateService)
        {
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
            _iRoomRateService = iRoomRateService;
        }
        public IActionResult RoomRate()
        {
            return PartialView();
        }
        public IActionResult RunNightAudit()
        {
            return View();
        }

        private void SaveLog(string str)
        {
            FileStream file = new FileStream(path + "\\Log.txt", FileMode.OpenOrCreate, FileAccess.Write);
            StreamWriter sw = new StreamWriter(file);
            sw.BaseStream.Seek(0, SeekOrigin.End);
            sw.WriteLine(time.ToString() + " : " + str + "\r");
            sw.Flush();
            file.Close();
        }

        /// <summary>
        /// Cập nhật trạng thái đang chạy night
        /// </summary>
        private void _StartNightAudit(ref bool _IsOK)
        {
            try
            {
                _IsOK = true;
                TextUtils.ExcuteSQL("Update ConfigSystem Set KeyValue = '1' Where KeyName ='RunNightAudit'");
                _IsRunning = true;
            }
            catch (Exception ex)
            {
                _Error = ex.Message;
                _IsOK = false;
            }
        }

        /// <summary>
        /// Cập nhật lại trạng thái Night Audit khi chạy night xong
        /// -- CSS, 18/05/2011
        /// </summary>
        private void _EndNightAudit(ref bool _IsOK)
        {

            try
            {
                _IsOK = true;
                TextUtils.ExcuteSQL("Update ConfigSystem Set KeyValue = 0 Where KeyName ='RunNightAudit'");
                _IsRunning = false;
                SaveLog("Update trang thai ket thuc chay night audit !");
            }
            catch (Exception ex)
            {
                _Error = ex.Message;
                _IsOK = false;
            }
        }


        /// <summary>
        /// Insert vào log của người chạy
        /// CSS, 17/05/2011
        /// </summary>
        private void _NightAuditHistory(ref bool _IsOK, string userName, string userID, string computerName)
        {
            try
            {
                TextUtils.ExcuteSQL("Insert Into NightAuditHistory(BussinessDate,SystemDate, UserID, UserName,ComputerName, Status) Values " +
                   " ('" + TextUtils.GetBusinessDateTime().ToString("yyyy/MM/dd") + "', '" + TextUtils.GetSystemDate().ToString("yyyy/MM/dd HH:mm:ss") + "', '" + userID + "', '" + userName + "','" + computerName + "', 0)");
                _IsOK = true;
            }
            catch (Exception ex)
            {
                _Error = ex.Message;
                _IsOK = false;
            }
        }

        /// <summary>
        /// Kiểm tra danh sách khách chưa CI
        /// </summary>
        private void _CheckGuestCI(ref bool _IsNotCI, ref DataTable dataNotCheckIn)
        {
            DateTime businessDate = TextUtils.GetBusinessDateTime();
            string businessDateSql = businessDate.ToString("yyyy-MM-dd");
            string sql = $@" SELECT ConfirmationNo, RoomNo, ReservationNo, LastName, FirstName, ArrivalDate, DepartureDate, [Address] FROM dbo.Reservation WITH(NOLOCK) WHERE Status = 0 AND DATEDIFF(DAY, '{businessDateSql}', ArrivalDate) = 0 AND ReservationNo > 0 ";
            dataNotCheckIn = TextUtils.Select(sql);

            // dataNotCheckIn = pt.getTable("spNightAuditNotCheckInSearch", new SqlParameter("@ArrivalDate", pt.GetBusinessDateTime()), "tblNotCheckIn");
            if (dataNotCheckIn.Rows.Count > 0)
            {
                _IsNotCI = true;
                //if (MessageBox.Show(this, "Do you want to continue night audit?", TextUtils.Caption_Message, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                //{
                //    _IsNotCI = false;
                //}

                //this.BeginInvoke((MethodInvoker)delegate { frmNotCheckIn frm = new frmNotCheckIn(); frm.ShowDialog(); });

            }
        }

        /// <summary>
        /// Kiêm tra khách chưa check Out

        /// </summary>
        /// <param name="_IsNotCO"></param>
        private void _CheckGuestCO(ref bool _IsNotCO, ref DataTable dataNotCheckIn)
        {
            DateTime businessDate = TextUtils.GetBusinessDateTime();
            string businessDateSql = businessDate.ToString("yyyy-MM-dd");

            dataNotCheckIn = TextUtils.Select(
    "SELECT ConfirmationNo,RoomNo, ReservationNo, LastName, FirstName, ArrivalDate, DepartureDate, [Address] " +
    "FROM dbo.Reservation WITH(NOLOCK) " +
    "WHERE (Status = 1 Or Status = 6) " +
    "AND DATEDIFF(day, '" + businessDateSql + "', DepartureDate) = 0 " +
    "AND ReservationNo <> 0"
);
            //  dataNotCheckIn = pt.getTable("spNightAuditNotCheckOutSearch", new SqlParameter("@DeparturDate", TextUtils.GetBusinessDateTime()), "tblNotCheckOut");
            //Kiểm tra những người đến ngày checkout nhưng chưa check out
            if (dataNotCheckIn.Rows.Count > 0)
            {

                _IsNotCO = true;
            }
        }

        /// <summary>
        /// Backup DB trước khi chạy night
        /// -- CSS, 17/05/2011
        /// </summary>
        private void _BackupData_1(ref bool _IsOK)
        {
            try
            {

                string DatabaseName = "";
                string PathDatabaseName = "";
                string DatabaseAutoBackup = "0";

                DataTable dtCSName = TextUtils.Select("Select KeyValue From ConfigSystem Where KeyName ='DatabaseName'");
                if (dtCSName.Rows.Count > 0)
                    DatabaseName = dtCSName.Rows[0]["KeyValue"].ToString();

                DataTable dtCSValue = TextUtils.Select("Select KeyValue From ConfigSystem Where KeyName ='DatabasePathBackup'");
                if (dtCSValue.Rows.Count > 0)
                    PathDatabaseName = dtCSValue.Rows[0]["KeyValue"].ToString();

                DataTable dtAutoBackup = TextUtils.Select("Select KeyValue From ConfigSystem Where KeyName ='DatabaseAutoBackup'");
                if (dtAutoBackup.Rows.Count > 0)
                    DatabaseAutoBackup = dtAutoBackup.Rows[0]["KeyValue"].ToString();

                string[] ParaName = new string[2];
                string[] ParaValue = new string[2];

                ParaName[0] = "@DatabaseName";
                ParaValue[0] = DatabaseName;
                ParaName[1] = "@Path";
                ParaValue[1] = PathDatabaseName;
                if (DatabaseAutoBackup == "1")
                {
                    //TextUtils.ExecSPBackup("BackupDatabase", ParaName, ParaValue);

                }
            }
            catch (Exception ex)
            {
                _Error = ex.Message;
                _IsOK = false;
            }
        }

        /// <summary>
        /// Xóa dữ liệu trước khi chạy night - Trường hợp chạy lại Night
        /// </summary>
        private void _DeleteFolioDetail(ref bool _IsOK)
        {
            try
            {
                DateTime time = ((BusinessDateModel)BusinessDateBO.Instance.FindAll()[0]).BusinessDate;
                _IsOK = true;
                string strDelete = "DELETE FROM dbo.FolioDetail WHERE UserName ='$$' AND DATEDIFF(day,TransactionDate,'" + time.ToString("yyyy/MM/dd") + "')=0";
                TextUtils.ExcuteSQL(strDelete);
            }
            catch (Exception ex)
            {
                _Error = ex.Message;
                _IsOK = false;
            }
        }

        /// <summary>
        /// Xóa dữ liệu temp trng bảng đặt phòng và bảng liên quan
        /// </summary>
        private void _DeleteRsvTemp(ref bool _IsOK)
        {
            try
            {
                //Lấy danh sách các đặt phòng nháp không sử dụng có ReservationNo = -1
                DataTable _dtTemp = TextUtils.Select("SELECT ID FROM Reservation WITH (NOLOCK) WHERE ReservationNo = -1");
                if (_dtTemp.Rows.Count > 0)
                {
                    for (int i = 0; i < _dtTemp.Rows.Count; i++)
                    {
                        int _RsvID = TextUtils.ToInt(_dtTemp.Rows[i]["ID"].ToString());
                        TextUtils.ExcuteSQL("DELETE FROM dbo.Reservation WHERE ID = " + _RsvID);
                        TextUtils.ExcuteSQL("DELETE FROM dbo.ReservationFixedCharge WHERE ReservationID = " + _RsvID);
                        TextUtils.ExcuteSQL("DELETE FROM dbo.ReservationPackage WHERE ReservationID = " + _RsvID);
                        TextUtils.ExcuteSQL("DELETE FROM dbo.ReservationItemInventory WHERE ReservationID = " + _RsvID);
                        TextUtils.ExcuteSQL("DELETE FROM dbo.ReservationSpecial WHERE ReservationID = " + _RsvID);
                        TextUtils.ExcuteSQL("DELETE FROM dbo.ReservationOptions WHERE ReservationID = " + _RsvID);
                        TextUtils.ExcuteSQL("DELETE FROM dbo.ActivityLog WHERE TableName = 'Reservation' AND ObjectID = " + _RsvID);
                    }

                }
            }
            catch (Exception ex)
            {
                _Error = ex.Message;
                _IsOK = false;
            }
        }

        /// <summary>
        /// Đóng ca thu ngân
        /// </summary>
        private void _CloseShift(ref bool _IsOK)
        {
            try
            {
                _IsOK = true;

                string date = TextUtils.GetBusinessDate().ToString("yyyy/MM/dd").ToString().Substring(0, 10) + " " + TextUtils.GetSystemTime();// " 23:59:59";               
                string sqlUpdate_CloseCashier = "UPDATE Shift SET Status = 1, LogoutTime = '" + date + "' WHERE Status = 0  ";
                SqlHelper.ExecuteNonQuery(DBUtils.GetDBConnectionString(), CommandType.Text, sqlUpdate_CloseCashier);
            }
            catch (Exception ex)
            {
                _Error = ex.Message;
                _IsOK = false;
            }
        }

        /// <summary>
        /// Check xem đã post Advance Bill chưa
        /// -- CSS, 18/05/2011
        /// </summary>
        /// <param name="_RsvID"></param>
        /// <returns></returns>
        private bool _CheckAdvanceBill(int _RsvID, DateTime _BusDate)
        {
            DataTable dt = null;
            dt = TextUtils.Select("SELECT ID FROM AdvanceBill WHERE ReservationID =" + _RsvID + " ");
            if (dt.Rows.Count == 0)
            {
                return false;
            }
            else
            {
                dt = TextUtils.Select("SELECT ID FROM AdvanceBill " +
                                      "WHERE datediff(day, DateAdvanceBill, '" + _BusDate.ToString("yyyy/MM/dd") + "') > 0 " +
                                      "AND ReservationID =" + _RsvID + " ");
                if (dt.Rows.Count > 0)
                    return false;
                else
                    return true;
            }
        }

        private string _GetPackageID(DataTable dt)
        {
            string _PackageID = "0";
            if (dt != null)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    if (i == 0)
                        _PackageID = dt.Rows[i]["PackageID"].ToString();
                    else
                        _PackageID = _PackageID + "," + dt.Rows[i]["PackageID"].ToString();
                }
            }
            return _PackageID;
        }


        private int _GetFolioDefault(int _RsvID)
        {
            DataTable dt = TextUtils.Select("SELECT ID FROM Folio WITH (NOLOCK) WHERE ReservationID = " + _RsvID + " AND FolioNo = 1");
            if (dt.Rows.Count > 0)
                return TextUtils.ToInt(dt.Rows[0]["ID"].ToString());
            else
                return 0;
        }

        private bool _Check_PostingRhythm_Package(string PostingRhythmID, int PackageDetailID, DateTime BeginDate, DateTime EndDate, DateTime PostingDate, string PostingDay,
                                                  DateTime _BusDate, DateTime _Arr, DateTime _Dep, int _RsvID)
        {
            bool PostingStatus = false;
            int _PostingDay;
            DateTime BusinessDate = TextUtils.GetBusinessDate();
            _BusDate = new DateTime(_BusDate.Year, _BusDate.Month, _BusDate.Day, 0, 0, 0);
            //Lấy theo ngày trong Rsv
            DataTable dtRp = TextUtils.Select("SELECT BeginDate, EndDate FROM dbo.ReservationPackage WITH (NOLOCK) WHERE ReservationID =" + _RsvID + " AND PackageDetailID =" + PackageDetailID + "");
            if (dtRp != null)
            {
                if (dtRp.Rows.Count > 0)
                {
                    BeginDate = Convert.ToDateTime(dtRp.Rows[0]["BeginDate"]);
                    EndDate = Convert.ToDateTime(dtRp.Rows[0]["EndDate"]);
                }
            }
            //Continue
            switch (PostingRhythmID)
            {
                case "1"://Every Night
                    {
                        if (TextUtils.CompareDate(BeginDate, _BusDate) <= 0 && TextUtils.CompareDate(EndDate, _BusDate) >= 0)
                            PostingStatus = true;
                        else
                            PostingStatus = false;
                        break;
                    }
                case "2"://Date
                    {
                        PostingDate = new DateTime(PostingDate.Year, PostingDate.Month, PostingDate.Day, 0, 0, 0);
                        if (TextUtils.CompareDate(PostingDate, BusinessDate) == 0)
                            PostingStatus = true;
                        else
                            PostingStatus = false;
                        break;
                    }
                case "3"://Day
                    {
                        //C2 - New

                        string[] arr = PostingDay.Split(',');
                        PostingStatus = false;
                        for (int d = 0; d < arr.Length; d++)
                        {
                            _PostingDay = TextUtils.ToInt(arr[d]) - 1;
                            if (_PostingDay < 0)
                                _PostingDay = 0;
                            if (TextUtils.CompareDate(BeginDate.AddDays(_PostingDay), _BusDate) == 0)
                                PostingStatus = true;
                        }
                        break;
                    }
                case "4"://Checkin
                    {
                        if (TextUtils.CompareDate(_BusDate, _Arr) == 0)
                            PostingStatus = true;
                        else
                            PostingStatus = false;
                        break;
                    }
                case "5"://Check Out
                    {
                        if (TextUtils.CompareDate(_BusDate, _Dep.AddDays(-1)) == 0)
                            PostingStatus = true;
                        else
                            PostingStatus = false;
                        break;
                    }
            }
            return PostingStatus;
        }
        public DataRow GetDataRow(DataTable table, string nameColCheck, object valueColCheck)
        {
            if (null == table) return null;
            if (valueColCheck.ToString() == "") return null;
            DataRow mydatarow = null;
            string sSQL = table.Columns[nameColCheck].ColumnName + "='" + valueColCheck.ToString().Replace("'", "''") + "'";
            table.DefaultView.RowFilter = sSQL;
            if (table.DefaultView.Count > 0)
                mydatarow = table.DefaultView.ToTable().Rows[0];
            table.DefaultView.RowFilter = null;
            return mydatarow;
        }
        private string _GetTransactionCodeBB(string _RoomTypeID, string TransactionCode)
        {
            DataRow dr_RoomType;
            string rt = "";
            dr_RoomType = GetDataRow(dt_RoomType, "ID", _RoomTypeID);
            if (dr_RoomType != null)
            {
                DataTable dtDetail = TextUtils.Select("Select TransactionCode From NightAuditBB Where Code = '" + TransactionCode + "' And Zone = '" + dr_RoomType["ZoneCode"].ToString() + "'");
                if (dtDetail.Rows.Count > 0)
                {
                    rt = dtDetail.Rows[0]["TransactionCode"].ToString();
                }
                else
                {
                    rt = TransactionCode;
                }
            }
            return rt;
        }
        /// <summary>       
        /// Tach 1 chuoi cac TransactionCode thanh tung Transaction
        /// -- CSS, 24/05/2011
        /// </summary>
        /// <param name="strRouting"></param>
        /// <returns></returns>
        public string[] _GetArrayTransaction(string _RoutingCode)
        {
            string strReturn = "";
            string[] array = _RoutingCode.Split(',');
            for (int i = 0; i < array.Length; i++)
            {
                if (!array[i].Trim().Equals(""))
                {
                    DataTable tb = TextUtils.Select("Select * from RoutingCode Where Code =N'" + array[i].ToString().Trim() + "'");
                    if (tb.Rows.Count > 0)
                    {
                        strReturn = strReturn + tb.Rows[0]["TransactionCodes"].ToString().Trim();
                    }
                    else
                    {
                        strReturn = strReturn + array[i].Trim() + ",";
                    }
                }
            }
            string[] arrayReturn = strReturn.Split(',');

            return arrayReturn;
        }

        //C2 Không dùng Transaction
        private int _GetFolioID(int ReservationID, int WindowNo, string ConfirmationNo)
        {
            try
            {
                //Tim kiem Folio
                BaseBusiness.util.Expression exp = new BaseBusiness.util.Expression("ReservationID", ReservationID, "=");
                exp = exp.And(new BaseBusiness.util.Expression("FolioNo", WindowNo, "="));
                exp = exp.And(new BaseBusiness.util.Expression("ConfirmationNo", ConfirmationNo, "="));
                //exp = exp.And(new Expression("Status", 0, "="));
                ArrayList arr = FolioBO.Instance.FindByExpression(exp);
                //Kiem tra dieu kien va tra ve ket qua
                if (arr.Count == 0)
                    return 0;
                else
                {
                    return ((FolioModel)arr[0]).ID;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public int CreateFolio(int RoutingID, int UserID)
        {
            try
            {
                RoutingModel mOR = (RoutingModel)RoutingBO.Instance.FindByPrimaryKey(RoutingID);

                FolioModel mF = new FolioModel();
                mF.FolioDate = TextUtils.GetBusinessDate();
                mF.FolioNo = mOR.ToFolioNo;
                mF.ReservationID = mOR.ToReservationID;
                //mF.RoomID = mOR.ToRoomID;
                //if (mOR.ToRoomID != 0)
                //    mF.RoomNo = ((RoomModel)RoomBO.Instance.FindByPK(mOR.ToRoomID)).RoomNo;
                //else
                //    mF.RoomNo = "";
                mF.ProfileID = mOR.ProfileID;
                if (mOR.ProfileID > 0)
                    mF.AccountName = ((ProfileModel)ProfileBO.Instance.FindByPrimaryKey(mOR.ProfileID)).Account;
                else
                    mF.AccountName = "";
                mF.Status = false;
                if (mOR.IsMasterFolio == false)
                    mF.IsMasterFolio = false;
                else
                    mF.IsMasterFolio = true;
                if (mOR.FromReservationID > 0)
                    mF.ConfirmationNo = ((ReservationModel)ReservationBO.Instance.FindByPrimaryKey(mOR.FromReservationID)).ConfirmationNo;
                else
                    mF.ConfirmationNo = mOR.ConfirmationNo;

                mF.UserInsertID = UserID;
                mF.CreateDate = TextUtils.GetSystemDate();
                mF.UserUpdateID = UserID;
                mF.UpdateDate = mF.CreateDate;

                return (int)FolioBO.Instance.Insert(mF);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        private void _CheckRouting(string _TransactionCode, string _Confirm, int _RsvID, DateTime _BusDate,
                               ref int _ProfileID, ref string _Account, ref int _FolioID, ref int _WindowmNo, ref int _ToRsvID, int userID)
        {
            //0: Default; 
            //1: Routing to Room; 
            //2: Routing to WinDown; 
            //3: Routing MasterFolio
            int _type = 0;

            #region B1.Routing to Room
            DataTable _dtR1 = TextUtils.Select("SELECT ID, TransactionCodes, ToReservationID, ProfileID, AccountName, ToFolioNo " +
                                               "FROM Routing WITH (NOLOCK) " +
                                               "WHERE IsMasterFolio = 0 AND ConfirmationNo = '" + _Confirm + "' " +
                                               "AND FromReservationID = " + _RsvID + " " +
                                               "AND ToRoomID > 0 AND ToReservationID > 0 " +
                                               "AND DATEDIFF(day, FromDate, '" + _BusDate.ToString("yyyy/MM/dd") + "') >= 0 AND DATEDIFF(day, ToDate, '" + _BusDate.ToString("yyyy/MM/dd") + "') <= 0");
            if (_dtR1.Rows.Count > 0)
            {
                for (int i = 0; i < _dtR1.Rows.Count; i++)
                {
                    string[] _arr = _GetArrayTransaction(_dtR1.Rows[i]["TransactionCodes"].ToString());
                    //Nếu có trong danh sách Trans được routing
                    if (Array.IndexOf(_arr, _TransactionCode) >= 0)
                    {
                        _type = 1;
                        _WindowmNo = TextUtils.ToInt(_dtR1.Rows[i]["ToFolioNo"].ToString());
                        _ProfileID = TextUtils.ToInt(_dtR1.Rows[i]["ProfileID"].ToString());
                        _Account = _dtR1.Rows[i]["AccountName"].ToString();
                        _ToRsvID = TextUtils.ToInt(_dtR1.Rows[i]["ToReservationID"].ToString());
                        _Confirm = ((ReservationModel)ReservationBO.Instance.FindByPrimaryKey(_ToRsvID)).ConfirmationNo;
                        _FolioID = _GetFolioID(TextUtils.ToInt(_dtR1.Rows[i]["ToReservationID"].ToString()), _WindowmNo, _Confirm);
                        if (_FolioID == 0)
                            _FolioID = CreateFolio(TextUtils.ToInt(_dtR1.Rows[i]["ID"].ToString()), userID);
                        break;
                    }
                }
            }
            #endregion

            #region B2.Routing to WindownNo
            if (_type == 0)
            {
                DataTable _dtR2 = TextUtils.Select("SELECT ID, TransactionCodes, ToReservationID, ProfileID, AccountName, ToFolioNo " +
                                                  "FROM Routing WITH (NOLOCK) " +
                                                  "WHERE IsMasterFolio = 0 AND ConfirmationNo = '" + _Confirm + "' " +
                                                  "AND FromReservationID = " + _RsvID + " " +
                                                  "AND ToRoomID = 0 AND FromReservationID = ToReservationID " +
                                                  "AND DATEDIFF(day, FromDate, '" + _BusDate.ToString("yyyy/MM/dd") + "') >= 0 AND DATEDIFF(day, ToDate, '" + _BusDate.ToString("yyyy/MM/dd") + "') <= 0");
                if (_dtR2.Rows.Count > 0)
                {
                    for (int i = 0; i < _dtR2.Rows.Count; i++)
                    {
                        string[] _arr = _GetArrayTransaction(_dtR2.Rows[i]["TransactionCodes"].ToString());
                        //Nếu có trong danh sách Trans được routing
                        if (Array.IndexOf(_arr, _TransactionCode) >= 0)
                        {
                            _type = 2;
                            _WindowmNo = TextUtils.ToInt(_dtR2.Rows[i]["ToFolioNo"].ToString());
                            _ProfileID = TextUtils.ToInt(_dtR2.Rows[i]["ProfileID"].ToString());
                            _Account = _dtR2.Rows[i]["AccountName"].ToString();
                            _ToRsvID = TextUtils.ToInt(_dtR2.Rows[i]["ToReservationID"].ToString());
                            _FolioID = _GetFolioID(TextUtils.ToInt(_dtR2.Rows[i]["ToReservationID"].ToString()), _WindowmNo, _Confirm);
                            if (_FolioID == 0)
                                _FolioID = CreateFolio(TextUtils.ToInt(_dtR1.Rows[i]["ID"].ToString()), userID);
                            break;
                        }
                    }
                }
            }
            #endregion

            #region B3.Routing to MasterFolio
            if (_type == 0)
            {
                DataTable _dtR3 = TextUtils.Select("SELECT ID, TransactionCodes, ToReservationID, ProfileID, AccountName, ToFolioNo " +
                                                  "FROM Routing WITH (NOLOCK) " +
                                                  "WHERE IsMasterFolio = 1 AND ConfirmationNo = '" + _Confirm + "' " +
                                                  "AND DATEDIFF(day, FromDate, '" + _BusDate.ToString("yyyy/MM/dd") + "') >= 0 AND DATEDIFF(day, ToDate, '" + _BusDate.ToString("yyyy/MM/dd") + "') <= 0");

                if (_dtR3.Rows.Count > 0)
                {
                    for (int i = 0; i < _dtR3.Rows.Count; i++)
                    {
                        string[] _arr = _GetArrayTransaction(_dtR3.Rows[i]["TransactionCodes"].ToString());
                        //Nếu có trong danh sách Trans được routing
                        if (Array.IndexOf(_arr, _TransactionCode) >= 0)
                        {
                            _type = 3;
                            _WindowmNo = TextUtils.ToInt(_dtR3.Rows[i]["ToFolioNo"].ToString());
                            _ProfileID = TextUtils.ToInt(_dtR3.Rows[i]["ProfileID"].ToString());
                            _Account = _dtR3.Rows[i]["AccountName"].ToString();
                            _ToRsvID = TextUtils.ToInt(_dtR3.Rows[i]["ToReservationID"].ToString());
                            _FolioID = _GetFolioID(TextUtils.ToInt(_dtR3.Rows[i]["ToReservationID"].ToString()), _WindowmNo, _Confirm);
                            if (_FolioID == 0)
                                _FolioID = CreateFolio(TextUtils.ToInt(_dtR3.Rows[i]["ID"].ToString()), userID);
                            break;
                        }
                    }
                }
            }
            #endregion

            #region B4.Routing to Default
            if (_type == 0)
            {
                //Lấy số Folio, Window từ bảng Folio
                DataTable _dtdf = TextUtils.Select("SELECT ProfileID, AccountName, ID, FolioNo FROM Folio " +
                                                  "WHERE FolioNo = 1 " +
                                                  "AND ReservationID = " + _RsvID + " AND ConfirmationNo = '" + _Confirm + "' ");
                //Get Info
                if (_dtdf.Rows.Count > 0)
                {
                    _WindowmNo = TextUtils.ToInt(_dtdf.Rows[0]["FolioNo"].ToString());
                    _ProfileID = TextUtils.ToInt(_dtdf.Rows[0]["ProfileID"].ToString());
                    _Account = _dtdf.Rows[0]["AccountName"].ToString();
                    _ToRsvID = _RsvID;
                    _FolioID = TextUtils.ToInt(_dtdf.Rows[0]["ID"].ToString());
                }
                else
                {
                    ReservationModel mOR = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(_RsvID);
                    FolioModel mF = new FolioModel();
                    mF.FolioDate = TextUtils.GetBusinessDate();
                    mF.FolioNo = 1;
                    mF.ProfileID = mOR.ProfileIndividualId;
                    mF.AccountName = mOR.LastName;
                    mF.ReservationID = _RsvID;
                    mF.Status = false;
                    mF.IsMasterFolio = false;
                    mF.ConfirmationNo = mOR.ConfirmationNo;
                    mF.UserInsertID = mF.UserUpdateID = userID;
                    mF.CreateDate = mF.UpdateDate = TextUtils.GetSystemDate();
                    _FolioID = (int)FolioBO.Instance.Insert(mF);
                }
            }
            #endregion

        }

        /// <summary>
        /// Lấy ra ID của Reservation ảo của 1 số confirm
        /// <returns>Int</returns>
        public int GetOrCreateRsvMaster(DateTime _SysDate, string _ConfirmationNo, int _FromRsvID, ref string _Message, int userID)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                //Kiểm tra xem RsvMA đã có hay chưa
                BaseBusiness.util.Expression exp = new BaseBusiness.util.Expression("ConfirmationNo", _ConfirmationNo, "=");
                exp = exp.And(new BaseBusiness.util.Expression("ReservationNo", "0", "="));
                ArrayList arr = pt.FindByExpression("Reservation", exp);
                //Nếu có rồi thì lấy ra
                if ((arr != null) && (arr.Count > 0))
                    return ((ReservationModel)arr[0]).ID;
                //Nếu chưa có thì tạo mới
                else
                {
                    ReservationModel mR = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(_FromRsvID);
                    mR.Status = 0;
                    mR.MainGuest = false;
                    mR.PostingMaster = true;
                    mR.TotalAmount = 0;
                    mR.NoOfAdult = 0;
                    mR.NoOfChild = 0;
                    mR.NoOfChild1 = 0;
                    mR.NoOfChild2 = 0;
                    mR.NoOfRoom = 1;
                    mR.Rate = 0;
                    mR.CurrencyId = "USD";
                    mR.DropOffReqdId = 0;
                    mR.PickupReqdId = 0;
                    mR.RoomTypeId = 0;
                    mR.RtcId = 0;
                    mR.RoomType = "";
                    mR.RoomId = 0;
                    mR.RoomNo = "";
                    mR.UserInsertId = userID;
                    mR.UserUpdateId = userID;
                    mR.CreateDate = _SysDate;
                    mR.UpdateDate = _SysDate;
                    mR.ProfileIndividualId = 0;
                    mR.ReservationNo = "0";
                    mR.ShareRoom = 0;

                    mR.Status = 1;
                    pt.CommitTransaction();
                    return (int)ReservationBO.Instance.Insert(mR);
                }
            }
            catch (Exception ex)
            {
                pt.RollBack();
                _Message = ex.Message;
                return 0;
            }
        }

        /// <summary>
        /// Hàm lấy ra ID của Folio
        /// <returns></returns>
        public int GetOrCreateFolioID(DateTime _SysDate, DateTime _BusinessDate, string _ConfirmationNo, int _ReservationID,
                                             int _WindowNo, int _ProfileID, string _AccountName, ref int _ReservationID_Return, ref string _Message, int userID)
        {
            try
            {

                #region Kiểm tra đã có folio này hay chưa
                BaseBusiness.util.Expression exp;

                string sql = string.Empty;
                DataTable dt = null;
                if (_WindowNo < 0)
                {
                    sql = $"SELECT * FROM Folio " +
         $"WHERE ConfirmationNo = '{_ConfirmationNo}' " +
         $"AND FolioNo = '{_WindowNo}'";


                    //exp = new BaseBusiness.util.Expression("ConfirmationNo", _ConfirmationNo, "=");
                    //exp = exp.And(new BaseBusiness.util.Expression("FolioNo", _WindowNo, "="));
                }
                else
                {
                    sql = $"SELECT * FROM Folio " +
        $"WHERE ReservationID = '{_ReservationID}' " +
        $"AND FolioNo = '{_WindowNo}'";
                    //exp = new BaseBusiness.util.Expression("ReservationID", _ReservationID, "=");
                    //exp = exp.And(new BaseBusiness.util.Expression("FolioNo", _WindowNo, "="));
                }

                dt = TextUtils.Select(sql);

                ArrayList arr = new ArrayList();

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        arr.Add(MapFolio(row));
                    }
                }
                //ArrayList arr = pt.FindByExpression("Folio", exp);
                #endregion
                #region Nếu có rồi thì trả về ID thông tin
                if ((arr != null) && (arr.Count > 0))
                {
                    _ReservationID_Return = ((FolioModel)arr[0]).ReservationID;
                    return ((FolioModel)arr[0]).ID;
                }
                #endregion

                #region Nếu chưa có thì tạo mới
                else
                {
                    FolioModel mF = new FolioModel();
                    mF.ARNo = "";
                    mF.BalanceUSD = 0;
                    mF.ConfirmationNo = _ConfirmationNo;
                    mF.FolioDate = _BusinessDate;
                    mF.CreateDate = _SysDate;
                    mF.UpdateDate = _SysDate;
                    mF.UserInsertID = userID;
                    mF.UserUpdateID = userID;
                    mF.FolioNo = _WindowNo;
                    mF.ProfileID = _ProfileID;
                    mF.AccountName = _AccountName;
                    mF.Status = false;
                    if (_WindowNo < 0)
                    {
                        mF.IsMasterFolio = true;
                        mF.ReservationID = GetOrCreateRsvMaster(_SysDate, _ConfirmationNo, _ReservationID, ref _Message, userID);
                    }
                    else
                    {
                        mF.IsMasterFolio = false;
                        mF.ReservationID = _ReservationID;
                    }
                    if (mF.ReservationID > 0)
                    {
                        _ReservationID_Return = mF.ReservationID;
                        return (int)FolioBO.Instance.Insert(mF);
                    }
                    else
                        return 0;
                }
                #endregion
            }
            catch (Exception ex)
            {
                _Message = ex.Message;
                return 0;
            }
        }
        private FolioModel MapFolio(DataRow row)
        {
            if (row == null) return null;

            FolioModel model = new FolioModel();

            model.ID = row["ID"] != DBNull.Value ? Convert.ToInt32(row["ID"]) : 0;
            model.ARNo = row["ARNo"] != DBNull.Value ? row["ARNo"].ToString() : string.Empty;
            model.FolioDate = row["FolioDate"] != DBNull.Value ? Convert.ToDateTime(row["FolioDate"]) : DateTime.MinValue;
            model.FolioNo = row["FolioNo"] != DBNull.Value ? Convert.ToInt32(row["FolioNo"]) : 0;
            model.ReservationID = row["ReservationID"] != DBNull.Value ? Convert.ToInt32(row["ReservationID"]) : 0;
            model.ProfileID = row["ProfileID"] != DBNull.Value ? Convert.ToInt32(row["ProfileID"]) : 0;
            model.AccountName = row["AccountName"] != DBNull.Value ? row["AccountName"].ToString() : string.Empty;
            model.Status = row["Status"] != DBNull.Value && Convert.ToBoolean(row["Status"]);
            model.IsMasterFolio = row["IsMasterFolio"] != DBNull.Value && Convert.ToBoolean(row["IsMasterFolio"]);
            model.ConfirmationNo = row["ConfirmationNo"] != DBNull.Value ? row["ConfirmationNo"].ToString() : string.Empty;
            model.BalanceUSD = row["BalanceUSD"] != DBNull.Value ? Convert.ToDecimal(row["BalanceUSD"]) : 0m;
            model.BalanceVND = row["BalanceVND"] != DBNull.Value ? Convert.ToDecimal(row["BalanceVND"]) : 0m;
            model.IsPrintVAT = row["IsPrintVAT"] != DBNull.Value && Convert.ToBoolean(row["IsPrintVAT"]);
            model.CreateDate = row["CreateDate"] != DBNull.Value ? Convert.ToDateTime(row["CreateDate"]) : DateTime.MinValue;
            model.UpdateDate = row["UpdateDate"] != DBNull.Value ? Convert.ToDateTime(row["UpdateDate"]) : DateTime.MinValue;
            model.UserUpdateID = row["UserUpdateID"] != DBNull.Value ? Convert.ToInt32(row["UserUpdateID"]) : 0;
            model.UserInsertID = row["UserInsertID"] != DBNull.Value ? Convert.ToInt32(row["UserInsertID"]) : 0;

            return model;
        }

        public static decimal GetNumber(string InputAmount)
        {
            return Convert.ToDecimal(InputAmount.Trim('B'));
        }
        public static decimal GetAmount(ArrayList arr, decimal InputAmount)
        {
            #region Khai báo biến

            string s1 = "B0", s2 = "B0", s3 = "B0";
            //string BaseAmount = "B";
            string CurrentAmount = "";

            GenerateTransactionModel mGT;

            string result = "";

            #endregion

            for (int i = 0; i < arr.Count; i++)
            {
                #region Do du lieu vao Model
                mGT = (GenerateTransactionModel)arr[i];
                #endregion

                #region Lay ra CurrentAmount

                if (mGT.BaseAmount == 0)
                    CurrentAmount = "B" + Convert.ToString(Convert.ToDecimal(mGT.Percentage) / 100);
                else if (mGT.BaseAmount == 1)
                    CurrentAmount = "B" + (mGT.Percentage * GetNumber(s1)) / 100;
                else if (mGT.BaseAmount == 2)
                    CurrentAmount = "B" + (mGT.Percentage * GetNumber(s2)) / 100;
                else
                    CurrentAmount = "B" + (mGT.Percentage * GetNumber(s3)) / 100;

                #endregion

                #region Lay du lieu vao s1,s2,s3
                if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == false) && (mGT.Subtotal3 == false))
                {
                    s1 = "B" + (GetNumber(s1) + GetNumber(CurrentAmount));
                }
                else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == true) && (mGT.Subtotal3 == false))
                {
                    s1 = "B" + (GetNumber(s1) + GetNumber(CurrentAmount));
                    s2 = CurrentAmount;
                }
                else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == false) && (mGT.Subtotal3 == true))
                {
                    s1 = "B" + (GetNumber(s1) + GetNumber(CurrentAmount));
                    s3 = CurrentAmount;
                }
                else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == true) && (mGT.Subtotal3 == true))
                {
                    s1 = "B" + (GetNumber(s1) + GetNumber(CurrentAmount));
                    s2 = CurrentAmount;
                    s3 = CurrentAmount;
                }
                #endregion

                if (result.Equals(""))
                    result = CurrentAmount;
                else
                    result = "B" + (GetNumber(result) + GetNumber(CurrentAmount));
            }

            return InputAmount / GetNumber(result);
        }
        public static decimal GetAmountFormat(decimal Amount)
        {
            Decimal Result = Convert.ToDecimal(Amount.ToString("###,###,###.00"));
            return Result;
        }
        public bool UpdateBalance(int _ReservationID, int _FolioID, ref string _Message)
        {
            try
            {
                TextUtils.ExcuteSQL("Update Folio set BalanceVND=dbo.getBalanceOfFolio(" + _FolioID + ",'" + _CURRENCY_1 + "')," +
                                "BalanceUSD=dbo.getBalanceOfFolio(" + _FolioID + ",'" + _CURRENCY_2 + "') Where ID=" + _FolioID);
                TextUtils.ExcuteSQL("Update Reservation set BalanceVND=dbo.getBalanceOfGih(" + _ReservationID + ",'" + _CURRENCY_1 + "')," +
                                "BalanceUSD=dbo.getBalanceOfGih(" + _ReservationID + ",'" + _CURRENCY_2 + "') Where ID=" + _ReservationID);
                return true;
            }
            catch (Exception ex)
            {
                _Message = ex.Message;
                return false;
            }
        }
        public int GetActionType(HistoryType _ActionType)
        {
            if (_ActionType == HistoryType.Gen_Post)
                return 0;

            if (_ActionType == HistoryType.Basic_Post)
                return 1;

            if (_ActionType == HistoryType.Night_Post)
                return 2;

            if (_ActionType == HistoryType.Early_Post)
                return 3;

            if (_ActionType == HistoryType.Advance_Post)
                return 4;

            if (_ActionType == HistoryType.Correct)
                return 5;

            if (_ActionType == HistoryType.Split)
                return 6;

            if (_ActionType == HistoryType.Tranferred)
                return 7;

            if (_ActionType == HistoryType.Deleted)
                return 8;

            if (_ActionType == HistoryType.Payment)
                return 9;

            if (_ActionType == HistoryType.Print)
                return 10;
            return -1;
        }
        public void InsertHistory(DateTime _SysDate, DateTime _BusinessDate, int _Action_FolioID, int _AfterAction_FolioID, string _InvoiceNo,
                                                 HistoryType _ActionType, string _ActionText, string _ActionByUser, string _Code, string _Desc,
                                                 decimal _Amount, string _Supplement, string _ReasonCode, string _ReasonText, string _Terminal)
        {
            try
            {
                PostingHistoryModel mPH = new PostingHistoryModel();
                mPH.ActionType = GetActionType(_ActionType);
                if ((_ActionType == HistoryType.Advance_Post) || (_ActionType == HistoryType.Basic_Post) || (_ActionType == HistoryType.Early_Post)
                    || (_ActionType == HistoryType.Gen_Post) || (_ActionType == HistoryType.Night_Post) || (_ActionType == HistoryType.Payment))
                    mPH.ActionText = _ActionText + " - Amount($" + _Amount.ToString("###,###,###.##") + ")";
                else
                    mPH.ActionText = _ActionText;
                mPH.ActionDate = _SysDate;
                mPH.TransactionDate = _BusinessDate;
                mPH.ActionUser = _ActionByUser;
                mPH.InvoiceNo = _InvoiceNo;
                mPH.Action_FolioID = _Action_FolioID;
                mPH.AfterAction_FolioID = _AfterAction_FolioID;
                mPH.Amount = _Amount;
                mPH.Supplement = _Supplement;
                mPH.Code = _Code;
                mPH.Description = _Desc;
                mPH.ReasonCode = _ReasonCode;
                mPH.ReasonText = _ReasonText;
                mPH.Terminal = _Terminal;
                mPH.Machine = TextUtils.GetHostName();
                if (_ActionType != HistoryType.Night_Post)
                    mPH.Property = "PMS";
                else
                    mPH.Property = "NIGHT";

                PostingHistoryBO.Instance.Insert(mPH);
            }
            catch (Exception ex)
            {
                return;
            }
        }
        public static string GetActionText(HistoryType _ActionType, string _Code, string _Text)
        {
            if (_ActionType == HistoryType.Gen_Post)
                return "[POST_GEN] - " + _Code + " - " + _Text;

            if (_ActionType == HistoryType.Basic_Post)
                return "[POST] - " + _Code + " - " + _Text;

            if (_ActionType == HistoryType.Night_Post)
                return "[NIGHT_POST]" + _Code + " - " + _Text;

            if (_ActionType == HistoryType.Early_Post)
                return "[EARLY_CHECKOUT]" + _Code + " - " + _Text;

            if (_ActionType == HistoryType.Advance_Post)
                return "[ADVANCE_BILL]" + _Code + " - " + _Text;

            if (_ActionType == HistoryType.Correct)
                return "[CORRECT_TRANSACTION] - " + _Code + " " + _Text;

            if (_ActionType == HistoryType.Split)
                return "[SPLIT_TRANSACTION] - " + _Code + " from " + _Text;

            if (_ActionType == HistoryType.Tranferred)
                return "[TRANFERRED] - " + _Code + " - " + _Text;

            if (_ActionType == HistoryType.Deleted)
                return "[DELETED] - " + _Code + " - " + _Text;

            if (_ActionType == HistoryType.Payment)
                return "[PAY] - " + _Code + " - " + _Text;

            if (_ActionType == HistoryType.Print)
                return "[PRINT] - " + _Code + " - " + _Text;
            return "";
        }
        public bool PostingPackage(bool AutoPosting, DateTime _SysDate, DateTime _BusinessDate, int _ProID, string _ProCode, string _ConfirmNo, int _RsvID, int _RoomID, int _OriginRsvID, int _OriginFolioID,
                                        int _ProfileID, string _AccountName, int _Win, int _PkgID, string _PkgCode, string[] _TransCode, string[] _ArCode,
                                        decimal[] _Amount, bool[] _TaxInclude, int[] _Quan, string[] _CurrencyID, string _CurrencyLocal,
                                        string[] _Ref, string[] _Supp, ref decimal _AmountLocalReturn, ref string _Message, string Description, int RoomTypeID, string RoomType, int userID, string userName)
        {
            ProcessTransactions pt = new ProcessTransactions();

            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                #region Gán giá trị cho ngày hệ thống
                //_BusinessDate  = TextUtils.GetBusinessDateTime();
                #endregion


                #region Lấy ra số FolioID
                int _RsvID_Return = 0;
                int FolioID = GetOrCreateFolioID(_SysDate, _BusinessDate, _ConfirmNo, _RsvID, _Win, _ProfileID, _AccountName, ref _RsvID_Return, ref _Message, userID);
                _RsvID = _RsvID_Return;

                #endregion

                if (FolioID > 0)
                {
                    #region Khai báo Model

                    FolioDetailModel mFD_Group = new FolioDetailModel();
                    FolioDetailModel mFD_Subgroup = new FolioDetailModel();
                    FolioDetailModel mFD_Detail = new FolioDetailModel();
                    decimal Rate = 0;

                    #endregion

                    #region Gán thông tin của các biến static

                    #region Group
                    mFD_Group.ProfitCenterID = _ProID;
                    mFD_Group.ProfitCenterCode = _ProCode;
                    mFD_Group.Status = false;

                    mFD_Group.CurrencyID = _CurrencyLocal;
                    mFD_Group.CurrencyMaster = _CurrencyLocal;

                    mFD_Group.ReservationID = _RsvID;
                    mFD_Group.OriginReservationID = _OriginRsvID;

                    mFD_Group.RoomID = _RoomID;

                    mFD_Group.FolioID = FolioID;
                    mFD_Group.OriginFolioID = _OriginFolioID;

                    mFD_Group.TransactionDate = _BusinessDate;
                    mFD_Group.PackageID = _PkgID;

                    mFD_Group.UserID = userID;
                    mFD_Group.UserName = userName;
                    mFD_Group.CashierNo = userName;
                    mFD_Group.ShiftID = userID;

                    mFD_Group.UserInsertID = userID;
                    mFD_Group.UserUpdateID = userID;
                    mFD_Group.CreateDate = _SysDate;
                    mFD_Group.UpdateDate = _SysDate;

                    if (AutoPosting == true)
                    {
                        mFD_Group.UserID = 0;
                        mFD_Group.UserName = "$$";
                        mFD_Group.CashierNo = "";
                        mFD_Group.ShiftID = 0;
                    }
                    else
                    {
                        mFD_Group.UserID = userID;
                        mFD_Group.UserName = userName;
                        mFD_Group.CashierNo = userName;
                        mFD_Group.ShiftID = userID;
                    }

                    #endregion

                    #region Subgroup
                    mFD_Subgroup.ProfitCenterID = _ProID;
                    mFD_Subgroup.ProfitCenterCode = _ProCode;
                    mFD_Subgroup.Status = false;

                    mFD_Subgroup.CurrencyMaster = _CurrencyLocal;

                    mFD_Subgroup.ReservationID = _RsvID;
                    mFD_Subgroup.OriginReservationID = _OriginRsvID;

                    mFD_Subgroup.RoomID = _RoomID;

                    mFD_Subgroup.FolioID = FolioID;
                    mFD_Subgroup.OriginFolioID = _OriginFolioID;

                    mFD_Subgroup.TransactionDate = _BusinessDate;
                    mFD_Subgroup.PackageID = _PkgID;

                    mFD_Subgroup.UserID = userID;
                    mFD_Subgroup.UserName = userName;
                    mFD_Subgroup.CashierNo = userName;
                    mFD_Subgroup.ShiftID = userID;

                    mFD_Subgroup.UserInsertID = userID;
                    mFD_Subgroup.UserUpdateID = userID;
                    mFD_Subgroup.CreateDate = _SysDate;
                    mFD_Subgroup.UpdateDate = _SysDate;
                    if (AutoPosting == true)
                    {
                        mFD_Subgroup.UserID = 0;
                        mFD_Subgroup.UserName = "$$";
                        mFD_Subgroup.CashierNo = "";
                        mFD_Subgroup.ShiftID = 0;
                    }
                    else
                    {
                        mFD_Subgroup.UserID = userID;
                        mFD_Subgroup.UserName = userName;
                        mFD_Subgroup.CashierNo = userName;
                        mFD_Subgroup.ShiftID = userID;
                    }
                    #endregion

                    #region Detail
                    mFD_Detail.ProfitCenterID = _ProID;
                    mFD_Detail.ProfitCenterCode = _ProCode;
                    mFD_Detail.Status = false;
                    mFD_Detail.CurrencyMaster = _CurrencyLocal;

                    mFD_Detail.ReservationID = _RsvID;
                    mFD_Detail.OriginReservationID = _OriginRsvID;

                    mFD_Detail.RoomID = _RoomID;

                    mFD_Detail.FolioID = FolioID;
                    mFD_Detail.OriginFolioID = _OriginFolioID;

                    mFD_Detail.TransactionDate = _BusinessDate;
                    mFD_Detail.PackageID = _PkgID;

                    mFD_Detail.UserID = userID;
                    mFD_Detail.UserName = userName;
                    mFD_Detail.CashierNo = userName;
                    mFD_Detail.ShiftID = userID;

                    mFD_Detail.UserInsertID = userID;
                    mFD_Detail.UserUpdateID = userID;
                    mFD_Detail.CreateDate = _SysDate;
                    mFD_Detail.UpdateDate = _SysDate;

                    if (AutoPosting == true)
                    {
                        mFD_Detail.UserID = 0;
                        mFD_Detail.UserName = "$$";
                        mFD_Detail.CashierNo = "";
                        mFD_Detail.ShiftID = 0;
                    }
                    else
                    {
                        mFD_Detail.UserID = userID;
                        mFD_Detail.UserName = userName;
                        mFD_Detail.CashierNo = userName;
                        mFD_Detail.ShiftID = userID;
                    }
                    #endregion

                    #endregion

                    #region Lấy ra thông tin của Transaction Pkg
                    //TransactionsModel mT_Group = (TransactionsModel)pt.FindByAttribute("Transactions", "Code", _PkgCode)[0];
                    TransactionsModel mT_Group = (TransactionsModel)TransactionsBO.Instance.FindByAttribute("Code", _PkgCode)[0];
                    #endregion

                    #region Insert dòng tổng <Invoice>

                    mFD_Group.IsSplit = true;
                    mFD_Group.PostType = 3;
                    mFD_Group.RowState = 1;
                    mFD_Group.Quantity = 1;

                    mFD_Group.TransactionGroupID = mT_Group.TransactionGroupID;
                    mFD_Group.TransactionSubgroupID = mT_Group.TransactionSubGroupID;
                    mFD_Group.GroupCode = mT_Group.GroupCode;
                    mFD_Group.SubgroupCode = mT_Group.SubgroupCode;
                    mFD_Group.GroupType = mT_Group.GroupType;

                    mFD_Group.ArticleCode = "";
                    mFD_Group.TransactionCode = mT_Group.Code;

                    //if (Description == "PKG")
                    //    mFD_Group.Description = mT_Group.Description;
                    //else
                    //    mFD_Group.Description = Description; // mT_Group.Description;

                    mFD_Group.Description = Description;

                    mFD_Group.Reference = _Ref[0];

                    mFD_Group.RoomID = _RoomID;

                    mFD_Group.Price = 0;
                    mFD_Group.Amount = 0;
                    mFD_Group.AmountMaster = 0;
                    mFD_Group.AmountGross = 0;
                    mFD_Group.AmountMasterGross = 0;
                    mFD_Group.AmountBeforeTax = 0;
                    mFD_Group.AmountMasterBeforeTax = 0;

                    mFD_Group.CurrencyID = _CurrencyID[0];// _CurrencyLocal;
                    mFD_Group.CurrencyMaster = _CurrencyLocal;

                    mFD_Group.RoomTypeID = RoomTypeID;
                    mFD_Group.RoomType = RoomType;
                    mFD_Group.ID = (int)FolioDetailBO.Instance.Insert(mFD_Group);
                    mFD_Group.InvoiceNo = mFD_Group.ID.ToString();
                    mFD_Group.TransactionNo = mFD_Group.InvoiceNo;

                    #endregion

                    #region Thực hiện posting chi tiết
                    for (int i = 0; i < _TransCode.Length; i++)
                    {
                        if (_TransCode[i] != null && _TransCode[i] != "" && _Amount[i] > 0)
                        {
                            #region Lấy thông tin của Trans.Code
                            // TransactionsModel mT = (TransactionsModel)pt.FindByAttribute("Transactions", "Code", _TransCode[i])[0];
                            TransactionsModel mT = (TransactionsModel)TransactionsBO.Instance.FindByAttribute("Code", _TransCode[i])[0];
                            #endregion

                            #region Kiểm tra xem đã có Generate
                            //  ArrayList arr = new ArrayList(pt.FindByAttribute("GenerateTransaction", "TransactionCode", _TransCode[i]));
                            ArrayList arr = new ArrayList();
                            arr.AddRange(GenerateTransactionBO.Instance.FindByAttribute("TransactionCode", _TransCode[i]));

                            #endregion

                            #region Nếu chưa tồn tại trong Generate
                            if ((arr == null) || (arr.Count == 0))
                            {
                                mFD_Detail.CurrencyID = _CurrencyID[i];
                                mFD_Detail.IsSplit = false;
                                mFD_Detail.PostType = 3;
                                mFD_Detail.RowState = 2;

                                mFD_Detail.TransactionGroupID = mT.TransactionGroupID;
                                mFD_Detail.TransactionSubgroupID = mT.TransactionSubGroupID;
                                mFD_Detail.GroupCode = mT.GroupCode;
                                mFD_Detail.SubgroupCode = mT.SubgroupCode;
                                mFD_Detail.GroupType = mT.GroupType;

                                mFD_Detail.ArticleCode = _ArCode[i];
                                mFD_Detail.TransactionCode = mT.Code;
                                mFD_Detail.Description = mT.Description;

                                mFD_Detail.Quantity = _Quan[i];
                                //Làm tròn VND
                                if (_CurrencyID[i] == "VND")
                                {
                                    mFD_Detail.Amount = Math.Round(_Amount[i], 0);
                                    mFD_Detail.AmountBeforeTax = Math.Round(mFD_Detail.Amount, 0);
                                    mFD_Detail.Price = Math.Round(mFD_Detail.Amount / mFD_Detail.Quantity, 0);

                                    if (i == 0)
                                    {
                                        mFD_Detail.AmountMaster = Math.Round(pt.ExchangeCurrency(_BusinessDate, _CurrencyID[i], _CurrencyLocal, mFD_Detail.Amount), 0);
                                        Rate = Math.Round(mFD_Detail.AmountMaster / mFD_Detail.Amount, 0);
                                    }
                                    else
                                        mFD_Detail.AmountMaster = Math.Round(mFD_Detail.Amount * Rate, 0);

                                    mFD_Detail.AmountMasterBeforeTax = Math.Round(mFD_Detail.AmountMaster, 0);

                                    mFD_Detail.AmountGross = Math.Round(mFD_Detail.Amount, 0);
                                    mFD_Detail.AmountMasterGross = Math.Round(mFD_Detail.AmountMaster, 0);
                                }
                                else
                                {
                                    mFD_Detail.Amount = _Amount[i];
                                    mFD_Detail.AmountBeforeTax = mFD_Detail.Amount;
                                    mFD_Detail.Price = mFD_Detail.Amount / mFD_Detail.Quantity;

                                    if (i == 0)
                                    {
                                        mFD_Detail.AmountMaster = pt.ExchangeCurrency(_BusinessDate, _CurrencyID[i], _CurrencyLocal, mFD_Detail.Amount);
                                        Rate = mFD_Detail.AmountMaster / mFD_Detail.Amount;
                                    }
                                    else
                                        mFD_Detail.AmountMaster = mFD_Detail.Amount * Rate;

                                    mFD_Detail.AmountMasterBeforeTax = mFD_Detail.AmountMaster;

                                    mFD_Detail.AmountGross = mFD_Detail.Amount;
                                    mFD_Detail.AmountMasterGross = mFD_Detail.AmountMaster;
                                }

                                mFD_Detail.InvoiceNo = mFD_Group.InvoiceNo;
                                mFD_Detail.RoomType = RoomType;
                                mFD_Detail.RoomTypeID = RoomTypeID;

                                mFD_Detail.ID = (int)FolioDetailBO.Instance.Insert(mFD_Detail);
                                mFD_Detail.TransactionNo = mFD_Detail.ID.ToString();
                                FolioDetailBO.Instance.Update(mFD_Detail);

                                //Cập nhập thông tin Invoice
                                //Làm tròn VND
                                if (_CurrencyID[i] == "VND")
                                {
                                    mFD_Group.AmountBeforeTax = Math.Round(mFD_Group.AmountBeforeTax + mFD_Detail.AmountBeforeTax, 0);
                                    mFD_Group.AmountMasterBeforeTax = Math.Round(mFD_Group.AmountMasterBeforeTax + mFD_Detail.AmountMasterBeforeTax, 0);

                                    mFD_Group.Amount = Math.Round(mFD_Group.Amount + mFD_Detail.AmountMaster, 0);
                                    mFD_Group.AmountMaster = Math.Round(mFD_Group.AmountMaster + mFD_Detail.AmountMaster, 0);

                                    mFD_Group.AmountGross = Math.Round(mFD_Group.AmountGross + mFD_Detail.AmountGross, 0);
                                    mFD_Group.AmountMasterGross = Math.Round(mFD_Group.AmountMasterGross + mFD_Detail.AmountMasterGross, 0);
                                }
                                else
                                {
                                    mFD_Group.AmountBeforeTax = mFD_Group.AmountBeforeTax + mFD_Detail.AmountBeforeTax;
                                    mFD_Group.AmountMasterBeforeTax = mFD_Group.AmountMasterBeforeTax + mFD_Detail.AmountMasterBeforeTax;

                                    mFD_Group.Amount = mFD_Group.Amount + mFD_Detail.AmountMaster;
                                    mFD_Group.AmountMaster = mFD_Group.AmountMaster + mFD_Detail.AmountMaster;

                                    mFD_Group.AmountGross = mFD_Group.AmountGross + mFD_Detail.AmountGross;
                                    mFD_Group.AmountMasterGross = mFD_Group.AmountMasterGross + mFD_Detail.AmountMasterGross;
                                }
                            }
                            #endregion

                            #region Nếu có tồn tại generate -> thực hiện
                            else
                            {
                                #region Khai báo biến
                                decimal s1 = 0, s2 = 0, s3 = 0;
                                decimal CurrentAmount = 0;
                                decimal BaseAmount = _Amount[i];
                                GenerateTransactionModel mGT;
                                #endregion

                                #region Lấy ra thông tin giá trước thuế
                                if (_TaxInclude[i] == true)
                                    BaseAmount = GetAmount(arr, Convert.ToDecimal(BaseAmount));
                                #endregion

                                #region Insert dòng tổng
                                mFD_Subgroup.CurrencyID = _CurrencyID[i];
                                mFD_Subgroup.IsSplit = true;
                                mFD_Subgroup.PostType = 3;
                                mFD_Subgroup.RowState = 2;

                                mFD_Subgroup.Reference = _Ref[i];
                                mFD_Subgroup.Supplement = _Supp[i];

                                mFD_Subgroup.TransactionGroupID = mT.TransactionGroupID;
                                mFD_Subgroup.TransactionSubgroupID = mT.TransactionSubGroupID;
                                mFD_Subgroup.GroupCode = mT.GroupCode;
                                mFD_Subgroup.SubgroupCode = mT.SubgroupCode;
                                mFD_Subgroup.GroupType = mT.GroupType;

                                mFD_Subgroup.ArticleCode = _ArCode[i];
                                mFD_Subgroup.TransactionCode = mT.Code;
                                mFD_Subgroup.Description = mT.Description;//mT.Description;

                                mFD_Subgroup.Quantity = _Quan[i];

                                mFD_Subgroup.Price = 0;
                                mFD_Subgroup.Amount = 0;
                                mFD_Subgroup.AmountMaster = 0;
                                mFD_Subgroup.AmountBeforeTax = 0;
                                mFD_Subgroup.AmountMasterBeforeTax = 0;

                                mFD_Subgroup.RoomType = RoomType;
                                mFD_Subgroup.RoomTypeID = RoomTypeID;
                                mFD_Subgroup.ID = (int)FolioDetailBO.Instance.Insert(mFD_Subgroup); //Dong tong cap 2

                                mFD_Subgroup.InvoiceNo = mFD_Group.InvoiceNo;
                                mFD_Subgroup.TransactionNo = mFD_Subgroup.ID.ToString();
                                #endregion

                                for (int j = 0; j < arr.Count; j++)
                                {
                                    #region Đổ dữ liệu vào Model
                                    mGT = (GenerateTransactionModel)arr[j];
                                    #endregion

                                    #region Lấy ra CurrentAmount
                                    if (mGT.Type == 0)
                                    {
                                        if (mGT.BaseAmount == 0)
                                            CurrentAmount = (mGT.Percentage * BaseAmount) / 100;
                                        else if (mGT.BaseAmount == 1)
                                            CurrentAmount = (mGT.Percentage * s1) / 100;
                                        else if (mGT.BaseAmount == 2)
                                            CurrentAmount = (mGT.Percentage * s2) / 100;
                                        else
                                            CurrentAmount = (mGT.Percentage * s3) / 100;
                                    }
                                    else if (mGT.Type == 1)
                                    {
                                        CurrentAmount = mGT.Amount;
                                    }

                                    //CurrentAmount = GetAmountFormat(CurrentAmount);

                                    #endregion

                                    #region Lấy dữ liệu vào s1,s2,s3
                                    if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == false) && (mGT.Subtotal3 == false))
                                    {
                                        s1 = s1 + CurrentAmount;
                                    }
                                    else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == true) && (mGT.Subtotal3 == false))
                                    {
                                        s1 = s1 + CurrentAmount;
                                        s2 = CurrentAmount;
                                    }
                                    else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == false) && (mGT.Subtotal3 == true))
                                    {
                                        s1 = s1 + CurrentAmount;
                                        s3 = CurrentAmount;
                                    }
                                    else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == true) && (mGT.Subtotal3 == true))
                                    {
                                        s1 = s1 + CurrentAmount;
                                        s2 = CurrentAmount;
                                        s3 = CurrentAmount;
                                    }
                                    #endregion

                                    #region Đổ dữ liệu vào Model
                                    mFD_Detail.CurrencyID = _CurrencyID[i];
                                    mFD_Detail.IsSplit = false;
                                    mFD_Detail.PostType = 3;
                                    mFD_Detail.RowState = 3;

                                    mFD_Detail.TransactionGroupID = mGT.TransactionGroupID;
                                    mFD_Detail.TransactionSubgroupID = mGT.TransactionSubGroupID;
                                    mFD_Detail.GroupCode = mGT.GroupCode;
                                    mFD_Detail.SubgroupCode = mGT.SubgroupCode;
                                    mFD_Detail.GroupType = mGT.GroupType;

                                    mFD_Detail.TransactionCode = mGT.TransactionCodeDetail;
                                    mFD_Detail.Description = mGT.Description;

                                    if ((_TaxInclude[i] == true) && (j == arr.Count - 1))
                                        mFD_Detail.Amount = _Amount[i] - mFD_Subgroup.Amount;
                                    else
                                        mFD_Detail.Amount = GetAmountFormat(CurrentAmount);

                                    if (_CurrencyID[i] == "VND")
                                        mFD_Detail.Amount = Math.Round(mFD_Detail.Amount);

                                    mFD_Detail.Quantity = _Quan[i];
                                    //Làm tròn VND
                                    if (_CurrencyID[i] == "VND")
                                    {
                                        mFD_Detail.AmountBeforeTax = Math.Round(mFD_Detail.Amount, 0);
                                        mFD_Detail.Price = Math.Round(mFD_Detail.Amount / mFD_Detail.Quantity, 0);
                                        mFD_Detail.AmountGross = Math.Round(mFD_Detail.Amount, 0);
                                    }
                                    else
                                    {
                                        mFD_Detail.AmountBeforeTax = mFD_Detail.Amount;
                                        mFD_Detail.Price = mFD_Detail.Amount / mFD_Detail.Quantity;
                                        mFD_Detail.AmountGross = mFD_Detail.Amount;

                                    }

                                    if (_CurrencyID[i] == "VND")
                                    {
                                        if ((i == 0) && (j == 0))
                                        {
                                            // Tính ra tỷ giá nếu là dòng đầu
                                            mFD_Detail.AmountMaster = Math.Round(pt.ExchangeCurrency(_BusinessDate, _CurrencyID[i], _CurrencyLocal, mFD_Detail.Amount), 0);
                                            Rate = mFD_Detail.AmountMaster / mFD_Detail.Amount;
                                        }
                                        else
                                            mFD_Detail.AmountMaster = mFD_Detail.Amount * Rate;

                                        if (j == 0)
                                        {
                                            // Nếu là dòng đầu -> insert giá trước thuế.
                                            mFD_Subgroup.AmountBeforeTax = Math.Round(mFD_Detail.Amount, 0);
                                            mFD_Subgroup.AmountMasterBeforeTax = Math.Round(mFD_Detail.AmountMaster, 0);
                                        }

                                        mFD_Detail.AmountMasterBeforeTax = Math.Round(mFD_Detail.AmountMaster, 0);
                                        mFD_Detail.AmountMasterGross = Math.Round(mFD_Detail.AmountMaster, 0);
                                    }
                                    else
                                    {
                                        if ((i == 0) && (j == 0))
                                        {
                                            // Tính ra tỷ giá nếu là dòng đầu
                                            mFD_Detail.AmountMaster = pt.ExchangeCurrency(_BusinessDate, _CurrencyID[i], _CurrencyLocal, mFD_Detail.Amount);
                                            Rate = mFD_Detail.AmountMaster / mFD_Detail.Amount;
                                        }
                                        else
                                            mFD_Detail.AmountMaster = mFD_Detail.Amount * Rate;

                                        if (j == 0)
                                        {
                                            // Nếu là dòng đầu -> insert giá trước thuế.
                                            mFD_Subgroup.AmountBeforeTax = mFD_Detail.Amount;
                                            mFD_Subgroup.AmountMasterBeforeTax = mFD_Detail.AmountMaster;
                                        }

                                        mFD_Detail.AmountMasterBeforeTax = mFD_Detail.AmountMaster;
                                        mFD_Detail.AmountMasterGross = mFD_Detail.AmountMaster;

                                    }

                                    #endregion

                                    #region Insert Du lieu
                                    mFD_Detail.RoomTypeID = RoomTypeID;
                                    mFD_Detail.RoomType = RoomType;
                                    mFD_Detail.InvoiceNo = mFD_Subgroup.InvoiceNo;
                                    mFD_Detail.TransactionNo = mFD_Subgroup.TransactionNo;
                                    mFD_Detail.ID = (int)FolioDetailBO.Instance.Insert(mFD_Detail);
                                    if (_CurrencyID[i] == "VND")
                                    {
                                        mFD_Subgroup.AmountMaster = Math.Round(mFD_Subgroup.AmountMaster + mFD_Detail.AmountMaster, 0);
                                        mFD_Subgroup.Amount = Math.Round(mFD_Subgroup.Amount + mFD_Detail.Amount, 0);
                                    }
                                    else
                                    {
                                        mFD_Subgroup.AmountMaster = mFD_Subgroup.AmountMaster + mFD_Detail.AmountMaster;
                                        mFD_Subgroup.Amount = mFD_Subgroup.Amount + mFD_Detail.Amount;

                                    }
                                    #endregion
                                }
                                // Tính giá Gross
                                mFD_Subgroup.AmountGross = mFD_Subgroup.Amount;
                                mFD_Subgroup.AmountMasterGross = mFD_Subgroup.AmountMaster;
                                // Tính giá Net số tiền nhập vào là giá sau thuế
                                if (_CurrencyID[i] == "VND")
                                {
                                    if (_TaxInclude[i] == true)
                                    {
                                        mFD_Subgroup.Amount = Math.Round(_Amount[i], 0);
                                        mFD_Subgroup.AmountMaster = Math.Round(_Amount[i] * Rate, 0);
                                    }
                                    mFD_Subgroup.Price = Math.Round(mFD_Subgroup.Amount / mFD_Subgroup.Quantity, 0);
                                }
                                else
                                {
                                    if (_TaxInclude[i] == true)
                                    {
                                        mFD_Subgroup.Amount = _Amount[i];
                                        mFD_Subgroup.AmountMaster = _Amount[i] * Rate;
                                    }
                                    mFD_Subgroup.Price = mFD_Subgroup.Amount / mFD_Subgroup.Quantity;

                                }
                                // Update thông tin của subgroup
                                FolioDetailBO.Instance.Update(mFD_Subgroup);

                                //Làm tròn VND
                                // Cập nhật thông tin group
                                if (_CurrencyID[i] == "VND")
                                {
                                    mFD_Group.AmountBeforeTax = Math.Round(mFD_Group.AmountBeforeTax + mFD_Subgroup.AmountBeforeTax, 0);
                                    mFD_Group.AmountMasterBeforeTax = Math.Round(mFD_Group.AmountMasterBeforeTax + mFD_Subgroup.AmountMasterBeforeTax, 0);

                                    mFD_Group.Amount = Math.Round(mFD_Group.Amount + mFD_Subgroup.Amount, 0);
                                    mFD_Group.AmountMaster = Math.Round(mFD_Group.AmountMaster + mFD_Subgroup.AmountMaster, 0);

                                    mFD_Group.AmountGross = Math.Round(mFD_Group.AmountGross + mFD_Subgroup.AmountGross, 0);
                                    mFD_Group.AmountMasterGross = Math.Round(mFD_Group.AmountMasterGross + mFD_Subgroup.AmountMasterGross, 0);
                                }
                                else
                                {
                                    mFD_Group.AmountBeforeTax = mFD_Group.AmountBeforeTax + mFD_Subgroup.AmountBeforeTax;
                                    mFD_Group.AmountMasterBeforeTax = mFD_Group.AmountMasterBeforeTax + mFD_Subgroup.AmountMasterBeforeTax;

                                    mFD_Group.Amount = mFD_Group.Amount + mFD_Subgroup.Amount;
                                    mFD_Group.AmountMaster = mFD_Group.AmountMaster + mFD_Subgroup.AmountMaster;

                                    mFD_Group.AmountGross = mFD_Group.AmountGross + mFD_Subgroup.AmountGross;
                                    mFD_Group.AmountMasterGross = mFD_Group.AmountMasterGross + mFD_Subgroup.AmountMasterGross;

                                }
                            }
                            #endregion
                        }
                    }
                    #endregion

                    #region Commit va Return

                    mFD_Group.Price = mFD_Group.Amount;
                    FolioDetailBO.Instance.Update(mFD_Group);
                    UpdateBalance(_RsvID, FolioID, ref _Message);
                    InsertHistory(_SysDate, _BusinessDate, mFD_Group.FolioID, mFD_Group.FolioID, mFD_Group.InvoiceNo, HistoryType.Night_Post,
                        GetActionText(HistoryType.Night_Post, mFD_Group.TransactionCode, mFD_Group.Description),
                        "$$", mFD_Group.TransactionCode, mFD_Group.Description, mFD_Group.Amount, mFD_Group.Supplement, "", "", "");

                    //pt.CommitTransaction();
                    //pt.CloseConnection();
                    return true;

                    #endregion
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                //pt.RollBack();
                _Message = ex.Message;
                return false;
            }
        }


        //Ham XO Post tien online sang IPTV
        public void IF_XO(string _RoomNo, string _date, string _time, string _Amount, string _Total, string _Curr,
                            string _refe, string _descrip, string _RsvID, string _GuestID)
        {
            try
            {
                #region 1.Khai báo biến
                string _transCode = "";
                int _FolioID = 0;
                //string _datetime = "";
                string _des = "";
                #endregion

                #region 2.Process
                DataTable _dtC = TextUtils.Select("SELECT Desciption FROM ConfigSystem WHERE KeyName ='IF_IN' ");
                DataTable _dtF = TextUtils.Select("SELECT ID FROM Folio WHERE ReservationID = '" + _RsvID + "' AND FolioNo = 1 ");
                //Not Exits
                if (_dtC.Rows.Count > 0 && _dtF.Rows.Count > 0)
                {
                    _transCode = _dtC.Rows[0][0].ToString();
                    _FolioID = TextUtils.ToInt(_dtF.Rows[0][0].ToString());
                    //if (_transCode == "" || _FolioID == 0)
                    //    WriteLog(PathName + "\\Log_err.txt", " -- : PS - Ro.No " + _RoomNo + "- Connot find Transaction Code on table Configsystem or FolioID not exits");
                }

                //XO|RN2781|G#12345|F#88746|TC2524|BI350|BDBeach Comber Lunch|BA13850|DA110327|TI124753|
                string Currency = "";
                if (_Curr == "USD")
                    Currency = "USD&0VND";
                else
                    Currency = "VND&0USD";
                _des = "XO|RN" + _RoomNo
                           + "|G#" + _GuestID
                           + "|T#" + _FolioID
                           + "|TC" + _transCode
                           + "|BI" + _Amount
                           + "|CU" + _Curr
                           + "|BD" + _descrip
                           + "|BA" + _Total.ToString() + Currency
                           + "|DA" + Convert.ToDateTime(_date).ToString("yyMMdd")
                           + "|TI" + Convert.ToDateTime(_time).ToString("HH:mm:ss");
                _des = _des + "|";
                _des = _des.Replace("\r\n", " ");

                //WriteLog(PathName + "\\Log_Data.txt", _des);

                //Insert into Interface
                InterfaceModel model = new InterfaceModel();
                model.KeyValue = "XO";
                model.Description = _des;
                model.CreateDate = DateTime.Now;
                InterfaceBO.Instance.Insert(model);
                #endregion

            }
            catch (Exception ex)
            {
                //WriteLog(PathName + "\\Log_err.txt", " - PS: Ro.No - " + _RoomNo + ", RsvID - " + _RsvID + " - Err :" + ex.Message);
                //Anwer
                //IF_PA(_RoomNo, false, _date, _time, _refe);
            }
        }
        //// <summary>
        /// <summary>
        /// //Post tiền phòng lên folio
        /// </summary>
        /// <param name="_IsOK"></param>
        /// <param name="_dtR_1"></param>
        /// <param name="userID"></param>
        // -- CSS, 18/05/2011
        //// </summary>
        private void _PostRoomCharge(ref bool _IsOK, ref DataTable _dtR_1, int userID, string userName)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();


                _IsOK = true;
                //Lấy danh sách phòng cần post
                DataTable _dtR = TextUtils.Select("SELECT a.ID, ProfileIndividualID, LastName, ConfirmationNo, a.RateCode, a.NoOfAdult, a.NoOfChild, a.NoOfChild1, a.NoOfChild2, " +
                                                  "ArrivalDate, DepartureDate, RoomTypeID, RoomType, RoomID, RoomNo, a.DiscountRate, a.DiscountAmount " +
                                                  "FROM Reservation a WITH (NOLOCK), RoomType b WITH (NOLOCK) " +
                                                  "WHERE a.RoomTypeID = b.ID " +
                                                  "AND a.Status IN (1,6) AND a.ReservationNo > 0 AND b.IsPseudo = 0 " +
                                                  "Order By RoomNo ");
                if (_dtR.Rows.Count > 0)
                {
                    #region 1.Khai báo biến
                    _dtR_1 = _dtR;
                    //Lấy ra ngày BB
                    DateTime _BusDate = TextUtils.GetBusinessDate();
                    DateTime _SysDate = TextUtils.GetSystemDate();
                    //Xác định số tổng post vào code nào
                    string _RC_P = TextUtils.Select("Select KeyValue From ConfigSystem Where KeyName = 'RoomCharge_P'").Rows[0]["KeyValue"].ToString();
                    string _PckBB = TextUtils.Select("Select KeyValue From ConfigSystem Where KeyName ='PackageBB'").Rows[0]["KeyValue"].ToString();
                    string _PckBBChild = TextUtils.Select("Select KeyValue From ConfigSystem Where KeyName ='PackageBBChild'").Rows[0]["KeyValue"].ToString();
                    //Xác định TransactionCode (Tiền phòng)
                    string _TransCodeUSD = TextUtils.Select("Select KeyValue From ConfigSystem Where KeyName = 'RoomChargeFITUSD'").Rows[0]["KeyValue"].ToString();
                    string _TransCodeVND = TextUtils.Select("Select KeyValue From ConfigSystem Where KeyName = 'RoomChargeFITVND'").Rows[0]["KeyValue"].ToString();
                    string PackageModeCharge = TextUtils.Select("Select KeyValue From ConfigSystem Where KeyName = 'PackageIncludeType'").Rows[0]["KeyValue"].ToString();//Dùng để xác định chi lấy từ ReservationPackage hay PackageDetail 
                    #endregion

                    for (int i = 0; i < _dtR.Rows.Count; i++)
                    {
                        #region 3.Process
                        //Kiểm tra nếu đã post AdvanceBill thì ko xử lý
                        if (_CheckAdvanceBill(TextUtils.ToInt(_dtR.Rows[i]["ID"].ToString()), _BusDate) == false)
                        {
                            //Lấy thông tin bảng Rate
                            DataTable _dtRR = TextUtils.Select("SELECT * FROM ReservationRate WITH (NOLOCK) " +
                                                               "WHERE ReservationID = " + TextUtils.ToInt(_dtR.Rows[i]["ID"].ToString()) + " " +
                                                               "AND DATEDIFF(day, RateDate, '" + _BusDate.ToString("yyyy/MM/dd") + "') = 0 ");
                            if (_dtRR.Rows.Count > 0)
                            {
                                //Nếu có giá mới xử lý
                                if (TextUtils.ToDecimal(_dtRR.Rows[0]["RateAfterTax"].ToString()) > 0)
                                {
                                    #region 1.Khai báo biến
                                    //Rsv
                                    int _WindownNo = 0;
                                    int _RsvID = TextUtils.ToInt(_dtR.Rows[i]["ID"].ToString());
                                    int _ToRsvID = TextUtils.ToInt(_dtR.Rows[i]["ID"].ToString());
                                    int _ProfileID = TextUtils.ToInt(_dtR.Rows[i]["ProfileIndividualID"].ToString());
                                    string _GuestName = _dtR.Rows[i]["LastName"].ToString();
                                    string _ConfirmNo = _dtR.Rows[i]["ConfirmationNo"].ToString();
                                    //Rate
                                    int _RoomTypeID = TextUtils.ToInt(_dtRR.Rows[0]["RoomTypeID"].ToString());
                                    string _RoomType = _dtRR.Rows[0]["RoomType"].ToString();
                                    int _RoomID = TextUtils.ToInt(_dtRR.Rows[0]["RoomID"].ToString());
                                    string _RoomNo = _dtRR.Rows[0]["RoomNo"].ToString();
                                    //Return (ref value when post)
                                    string _Desc = "Tiền phòng/Room Charge - RT: " + _RoomType;
                                    decimal AmountMasterReturn = 0;
                                    string _err = "";
                                    //Lấy dữ liệu trong bảng RsvPck (chỉ lấy giá IncludeRate)
                                    DataTable _tbRPck = TextUtils.Select("Select a.PackageDetailID, a.PackageID, a.TransactionCode, a.Description, a.Price, a.PriceAfterTax, a.CurrencyID, " +
                                                                        "a.IsTaxInclude, a.CalculationRuleID, a.[Quantity], a.PostingRhythmID, b.TextInNightAudit, " +
                                                                        "a.PostingDay, a.PostingDate, a.[BeginDate], a.[EndDate] " +
                                                                        "FROM dbo.ReservationPackage a WITH (NOLOCK), dbo.Package b WITH (NOLOCK) " +
                                                                        "WHERE a.PackageID = b.ID AND b.IncludedInRate = 1 " +
                                                                        "AND a.ReservationID = " + _RsvID + " " +
                                                                        "AND Datediff(day,'" + _BusDate.ToString("MM/dd/yyyy") + "',a.BeginDate) <=0 " +
                                                                        "AND Datediff(day,'" + _BusDate.ToString("MM/dd/yyyy") + "',a.EndDate) >0 ");
                                    //Trường hợp lấy trong PackageDetail
                                    if (PackageModeCharge == "1")
                                    {
                                        _tbRPck = TextUtils.Select("SELECT a.ID AS [PackageDetailID], a.PackageID, a.TransCode AS [TransactionCode], a.[Description], a.[Price], a.[PriceAfterTax], a.CurrencyID, " +
                                                                "a.IsTaxInclude, a.CalculationRuleID, 1 as [Quantity], a.RhythmPostingID AS [PostingRhythmID], b.TextInNightAudit, " +
                                                                "a.PostingDay, a.PostingDate, a.StartDate AS [BeginDate], a.EndDate " +
                                                                "FROM dbo.PackageDetail a WITH (NOLOCK), dbo.Package b WITH (NOLOCK) " +
                                                                "WHERE a.PackageID = b.ID AND b.IncludedInRate = 1 " +
                                                                "AND Datediff(day,'" + _BusDate.ToString("yyyy/MM/dd") + "', a.StartDate) <= 0 " +
                                                                "AND Datediff(day,'" + _BusDate.ToString("yyyy/MM/dd") + "', a.EndDate) > 0 " +
                                                                "AND a.PackageID IN (" + _GetPackageID(_tbRPck) + ") ");
                                    }
                                    //Gán lại tên diễn giải hiển thị gói package
                                    if (_tbRPck.Rows.Count > 0)
                                        _Desc = _tbRPck.Rows[0]["TextInNightAudit"].ToString() + " - RT: " + _RoomType;
                                    //Xác định mảng
                                    string[] _TransactionCode = new string[_tbRPck.Rows.Count + 1];
                                    string[] _Description = new string[_tbRPck.Rows.Count + 1];
                                    string[] _ArticleCode = new string[_tbRPck.Rows.Count + 1];
                                    decimal[] _Amount = new decimal[_tbRPck.Rows.Count + 1];
                                    int[] _Quantity = new int[_tbRPck.Rows.Count + 1];
                                    string[] _CurrencyID = new string[_tbRPck.Rows.Count + 1];
                                    string[] _Reffrence = new string[_tbRPck.Rows.Count + 1];
                                    string[] _Supplement = new string[_tbRPck.Rows.Count + 1];
                                    bool[] _PostingStatus = new bool[_tbRPck.Rows.Count + 1];
                                    bool[] _IncludeRate = new bool[_tbRPck.Rows.Count + 1];
                                    bool[] _TaxInclude = new bool[_tbRPck.Rows.Count + 1];
                                    bool[] _IsTransactionPosting = new bool[_tbRPck.Rows.Count + 1];
                                    //Xác định folio default
                                    int _OriginFolioID = _GetFolioDefault(_RsvID);
                                    //Xác định transaction cho tiền phòng
                                    string _TransCodeRC = _dtRR.Rows[0]["TransactionCode"].ToString();
                                    string _CurrRC = _dtRR.Rows[0]["CurrencyID"].ToString();
                                    if (_TransCodeRC == "")
                                    {
                                        if (_CurrRC == "VND")
                                            _TransCodeRC = _TransCodeVND;
                                        else
                                            _TransCodeRC = _TransCodeUSD;
                                    }
                                    decimal _SumAmountPck = 0;
                                    #endregion

                                    #region 2.Xác định tiền Package
                                    if (_tbRPck.Rows.Count > 0)
                                    {
                                        for (int j = 0; j < _tbRPck.Rows.Count; j++)
                                        {
                                            Decimal _AmountPck = 0;

                                            #region 2.1.CanculationRule && PostingRhythm
                                            Decimal Price = 0;
                                            Decimal OriginPrice = 0;
                                            //Kiểm tra thời điểm charge
                                            if (_Check_PostingRhythm_Package(_tbRPck.Rows[j]["PostingRhythmID"].ToString(), TextUtils.ToInt(_tbRPck.Rows[j]["PackageDetailID"].ToString()),
                                                                            Convert.ToDateTime(_tbRPck.Rows[j]["BeginDate"]), Convert.ToDateTime(_tbRPck.Rows[j]["EndDate"]),
                                                                            Convert.ToDateTime(_tbRPck.Rows[j]["PostingDate"]), _tbRPck.Rows[j]["PostingDay"].ToString(),
                                                                            _BusDate, Convert.ToDateTime(_dtR.Rows[i]["ArrivalDate"]), Convert.ToDateTime(_dtR.Rows[i]["DepartureDate"]), _RsvID) == true)
                                            {
                                                //Set default
                                                OriginPrice = Decimal.Parse(_tbRPck.Rows[j]["PriceAfterTax"].ToString());

                                                //1:Per/Person(từng người trong phòng)
                                                if (int.Parse(_tbRPck.Rows[j]["CalculationRuleID"].ToString()) == 1)
                                                {
                                                    if (int.Parse(_dtRR.Rows[0]["NoOfAdult"].ToString()) > 0)
                                                        Price = OriginPrice * Decimal.Parse(_dtRR.Rows[0]["NoOfAdult"].ToString());
                                                    if (int.Parse(_dtRR.Rows[0]["NoOfChild"].ToString()) > 0)
                                                        Price = Price + (OriginPrice * Decimal.Parse(_dtRR.Rows[0]["NoOfChild"].ToString()));
                                                    if (int.Parse(_dtRR.Rows[0]["NoOfChild1"].ToString()) > 0)
                                                        Price = Price + (OriginPrice * Decimal.Parse(_dtRR.Rows[0]["NoOfChild1"].ToString()));
                                                    if (int.Parse(_dtRR.Rows[0]["NoOfChild2"].ToString()) > 0)
                                                        Price = Price + (OriginPrice * Decimal.Parse(_dtRR.Rows[0]["NoOfChild2"].ToString()));
                                                    else if (int.Parse(_dtRR.Rows[0]["NoOfAdult"].ToString()) == 0 && int.Parse(_dtRR.Rows[0]["NoOfChild"].ToString()) == 0 && int.Parse(_dtRR.Rows[0]["NoOfChild1"].ToString()) == 0 && int.Parse(_dtRR.Rows[0]["NoOfChild2"].ToString()) == 0)
                                                        Price = 0;
                                                }
                                                //2: Per Adult
                                                else if (int.Parse(_tbRPck.Rows[j]["CalculationRuleID"].ToString()) == 2)
                                                {
                                                    if (int.Parse(_dtRR.Rows[0]["NoOfAdult"].ToString()) > 0)
                                                        Price = OriginPrice * Decimal.Parse(_dtRR.Rows[0]["NoOfAdult"].ToString());
                                                    else if (int.Parse(_dtRR.Rows[0]["NoOfAdult"].ToString()) == 0)
                                                        Price = 0;
                                                }
                                                //3:Per Child
                                                else if (int.Parse(_tbRPck.Rows[j]["CalculationRuleID"].ToString()) == 3)
                                                {
                                                    if (int.Parse(_dtRR.Rows[0]["NoOfChild"].ToString()) > 0)
                                                        Price = OriginPrice * Decimal.Parse(_dtRR.Rows[0]["NoOfChild"].ToString());
                                                    else if (int.Parse(_dtRR.Rows[0]["NoOfChild"].ToString()) == 0)
                                                        Price = 0;
                                                }
                                                //4: Per Room
                                                else if (int.Parse(_tbRPck.Rows[j]["CalculationRuleID"].ToString()) == 4)
                                                    Price = OriginPrice;
                                                //5:Per Child1
                                                else if (int.Parse(_tbRPck.Rows[j]["CalculationRuleID"].ToString()) == 5)
                                                {
                                                    if (int.Parse(_dtRR.Rows[0]["NoOfChild1"].ToString()) > 0)
                                                        Price = OriginPrice * Decimal.Parse(_dtRR.Rows[0]["NoOfChild1"].ToString());
                                                    else if (int.Parse(_dtRR.Rows[0]["NoOfChild1"].ToString()) == 0)
                                                        Price = 0;
                                                }
                                                //6:Per Child2
                                                else if (int.Parse(_tbRPck.Rows[j]["CalculationRuleID"].ToString()) == 6)
                                                {
                                                    if (int.Parse(_dtRR.Rows[0]["NoOfChild2"].ToString()) > 0)
                                                        Price = OriginPrice * Decimal.Parse(_dtRR.Rows[0]["NoOfChild2"].ToString());
                                                    else if (int.Parse(_dtRR.Rows[0]["NoOfChild2"].ToString()) == 0)
                                                        Price = 0;
                                                }
                                                //8:Per A + C
                                                else if (int.Parse(_tbRPck.Rows[j]["CalculationRuleID"].ToString()) == 8)
                                                {
                                                    if (int.Parse(_dtRR.Rows[0]["NoOfAdult"].ToString()) > 0)
                                                        Price = OriginPrice * Decimal.Parse(_dtRR.Rows[0]["NoOfAdult"].ToString());
                                                    if (int.Parse(_dtRR.Rows[0]["NoOfChild"].ToString()) > 0)
                                                        Price = Price + (OriginPrice * Decimal.Parse(_dtRR.Rows[0]["NoOfChild"].ToString()));
                                                    else if (int.Parse(_dtRR.Rows[0]["NoOfAdult"].ToString()) == 0 && int.Parse(_dtRR.Rows[0]["NoOfChild"].ToString()) == 0)
                                                        Price = 0;
                                                }
                                                //9:Per A + C + C1
                                                else if (int.Parse(_tbRPck.Rows[j]["CalculationRuleID"].ToString()) == 9)
                                                {
                                                    if (int.Parse(_dtRR.Rows[0]["NoOfAdult"].ToString()) > 0)
                                                        Price = OriginPrice * Decimal.Parse(_dtRR.Rows[0]["NoOfAdult"].ToString());
                                                    if (int.Parse(_dtRR.Rows[0]["NoOfChild"].ToString()) > 0)
                                                        Price = Price + (OriginPrice * Decimal.Parse(_dtRR.Rows[0]["NoOfChild"].ToString()));
                                                    if (int.Parse(_dtRR.Rows[0]["NoOfChild1"].ToString()) > 0)
                                                        Price = Price + (OriginPrice * Decimal.Parse(_dtRR.Rows[0]["NoOfChild1"].ToString()));
                                                    else if (int.Parse(_dtRR.Rows[0]["NoOfAdult"].ToString()) == 0 && int.Parse(_dtRR.Rows[0]["NoOfChild"].ToString()) == 0 && int.Parse(_dtRR.Rows[0]["NoOfChild1"].ToString()) == 0)
                                                        Price = 0;
                                                }
                                                //10:Per A + C1
                                                else if (int.Parse(_tbRPck.Rows[j]["CalculationRuleID"].ToString()) == 10)
                                                {
                                                    if (int.Parse(_dtRR.Rows[0]["NoOfAdult"].ToString()) > 0)
                                                        Price = OriginPrice * Decimal.Parse(_dtRR.Rows[0]["NoOfAdult"].ToString());
                                                    if (int.Parse(_dtRR.Rows[0]["NoOfChild1"].ToString()) > 0)
                                                        Price = Price + (OriginPrice * Decimal.Parse(_dtRR.Rows[0]["NoOfChild1"].ToString()));
                                                    else if (int.Parse(_dtRR.Rows[0]["NoOfAdult"].ToString()) == 0 && int.Parse(_dtRR.Rows[0]["NoOfChild1"].ToString()) == 0)
                                                        Price = 0;
                                                }
                                                //11:Per C + C1
                                                else if (int.Parse(_tbRPck.Rows[j]["CalculationRuleID"].ToString()) == 11)
                                                {
                                                    if (int.Parse(_dtRR.Rows[0]["NoOfChild"].ToString()) > 0)
                                                        Price = OriginPrice * Decimal.Parse(_dtRR.Rows[0]["NoOfChild"].ToString());
                                                    if (int.Parse(_dtRR.Rows[0]["NoOfChild1"].ToString()) > 0)
                                                        Price = Price + (OriginPrice * Decimal.Parse(_dtRR.Rows[0]["NoOfChild1"].ToString()));
                                                    else if (int.Parse(_dtRR.Rows[0]["NoOfChild"].ToString()) == 0 && int.Parse(_dtRR.Rows[0]["NoOfChild1"].ToString()) == 0)
                                                        Price = 0;
                                                }

                                                //Nhân vơi số lượng
                                                Price = Price * Decimal.Parse(_tbRPck.Rows[j]["Quantity"].ToString());
                                            }
                                            #endregion

                                            #region 2.2.Convert loại tiền về cùng với loại tiền phòng
                                            if (_tbRPck.Rows[j]["CurrencyID"].ToString() != _CurrRC)
                                            {
                                                _AmountPck = pt.ExchangeCurrency(_BusDate, _tbRPck.Rows[j]["CurrencyID"].ToString(), _CurrRC, Price);
                                            }
                                            else
                                                _AmountPck = Price;
                                            #endregion

                                            #region 2.3.Gán giá trị vào mảng
                                            _TransactionCode[j + 1] = _GetTransactionCodeBB(_RoomTypeID.ToString(), _tbRPck.Rows[j]["TransactionCode"].ToString());
                                            _Description[j + 1] = _tbRPck.Rows[j]["Description"].ToString() + " - " + _RoomType;
                                            _ArticleCode[j + 1] = "";
                                            _Amount[j + 1] = _AmountPck;
                                            _Quantity[j + 1] = int.Parse(_tbRPck.Rows[j]["Quantity"].ToString());
                                            _CurrencyID[j + 1] = _CurrRC;
                                            _Reffrence[j + 1] = "R:" + _RoomNo + "; D:" + _BusDate.ToString("dd/MM") + "; C:" + _ConfirmNo;
                                            _PostingStatus[j + 1] = true;
                                            _IncludeRate[j + 1] = true;
                                            _IsTransactionPosting[j + 1] = true;
                                            _TaxInclude[j + 1] = true;// bool.Parse(_tbRPck.Rows[j]["IsTaxInclude"].ToString());
                                            #endregion

                                            _SumAmountPck = _SumAmountPck + _AmountPck;
                                        }
                                    }
                                    #endregion

                                    #region 3.Xác định tiền phòng
                                    //Clear
                                    Decimal RateAfterTax = 0;
                                    //Xác định tiền phòng
                                    RateAfterTax = TextUtils.ToDecimal(_dtRR.Rows[0]["RateAfterTax"].ToString());
                                    //Discount                                   
                                    RateAfterTax = RateAfterTax - ((RateAfterTax * TextUtils.ToDecimal(_dtRR.Rows[0]["DiscountRate"].ToString())) / 100)
                                                - TextUtils.ToDecimal(_dtRR.Rows[0]["DiscountAmount"].ToString());

                                    //Gán giá trị vào mảng
                                    _TransactionCode[0] = _TransCodeRC;
                                    _Description[0] = "Tiền phòng/Room Charge - RT:" + _RoomType;
                                    _ArticleCode[0] = "";
                                    _Amount[0] = RateAfterTax - _SumAmountPck;
                                    _Quantity[0] = 1;
                                    _CurrencyID[0] = _CurrRC;
                                    if (_dtR.Rows[i]["RateCode"].ToString() != "")
                                        _Reffrence[0] = "R:" + _RoomNo + "; D:" + _BusDate.ToString("dd/MM") + "; C:" + _ConfirmNo + "; R" + _dtR.Rows[i]["RateCode"].ToString();
                                    else
                                        _Reffrence[0] = "R:" + _RoomNo + "; D:" + _BusDate.ToString("dd/MM") + "; C:" + _ConfirmNo;
                                    _PostingStatus[0] = true;
                                    _IncludeRate[0] = true;
                                    _IsTransactionPosting[0] = true;
                                    _TaxInclude[0] = true;// bool.Parse(_dtRR.Rows[0]["IsTaxInclude"].ToString());                                  
                                    #endregion

                                    #region 4.Check Routing
                                    _CheckRouting(_TransCodeRC, _ConfirmNo, _RsvID, _BusDate, ref _ProfileID, ref _GuestName, ref _OriginFolioID, ref _WindownNo, ref _ToRsvID, userID);
                                    #endregion

                                    #region 5.Posting to folio
                                    PostingPackage(true, _SysDate, _BusDate, 0, "", _ConfirmNo, _ToRsvID, _RoomID, _RsvID, _OriginFolioID,
                                                            _ProfileID, _GuestName, _WindownNo, 0, _RC_P, _TransactionCode, _ArticleCode,
                                                            _Amount, _TaxInclude, _Quantity, _CurrencyID, MasterCurrencyID, _Reffrence, _Supplement,
                                                            ref AmountMasterReturn, ref _err, _Desc, _RoomTypeID, _RoomType, userID, userName);

                                    // IF_XO(_RoomNo, pt.GetBusinessDateTime().ToString(), pt.GetBusinessDateTime().ToString(), "0", _Amount[0].ToString(), MasterCurrencyID.ToString(), _Reffrence[0], _Supplement[0], _RsvID.ToString(), _ProfileID.ToString());

                                    #endregion
                                }
                            }
                        }
                        #endregion
                    }
                    //Ghi vào file log
                    SaveLog("Room Charge - OK");
                }
                else
                    _dtR_1 = _dtR;
            }

            catch (Exception ex)
            {
                SaveLog(ex.ToString());
                _Error = ex.Message;
                _IsOK = false;
            }
        }


        public bool PostingToFolio(bool AutoPosting, DateTime _SysDate, DateTime _BusinessDate, int _ProID, string _ProCode, string _ConfirmNo, int _RsvID, int _RoomID, int _OriginRsvID, int _OriginFolioID,
                                        int _ProfileID, string _AccountName, int _Win, string _TransCode, string _ArCode, string _Ref, string _Supp,
                                        decimal _Amount, bool _TaxInclude, int _Quan, string _CurrencyID, string _CurrencyLocal,
                                        ref decimal _AmountReturn, ref decimal _AmountLocalReturn, ref string _TransNoReturn, ref string _Message, int RoomTypeID, string RoomType, int userID, string userName)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                #region Gán giá trị cho ngày hệ thống
                // _BusinessDate = TextUtils.GetBusinessDateTime();
                #endregion

                #region Mở kết nối và bắt đầu 1 Transaction
                pt.OpenConnection();
                pt.BeginTransaction();
                #endregion

                #region Lấy ra thông tin của FolioID

                int _RsvID_Return = 0;
                int FolioID = GetOrCreateFolioID(_SysDate, _BusinessDate, _ConfirmNo, _RsvID, _Win, _ProfileID, _AccountName, ref _RsvID_Return, ref _Message, userID);
                _RsvID = _RsvID_Return;

                #endregion

                if (FolioID > 0)
                {
                    #region Khai báo Model

                    FolioDetailModel mFD_Detail = new FolioDetailModel();
                    FolioDetailModel mFD_Master = new FolioDetailModel();

                    #endregion

                    #region Lấy ra thông tin của TransCode
                    TransactionsModel mT = (TransactionsModel)TransactionsBO.Instance.FindByAttribute("Code", _TransCode)[0];
                    //TransactionsModel mT = (TransactionsModel)pt.FindByAttribute("Transactions", "Code", _TransCode)[0];
                    #endregion

                    #region Gán giá trị có các biến statictis

                    mFD_Detail.ProfitCenterID = _ProID;
                    mFD_Detail.ProfitCenterCode = _ProCode;
                    mFD_Detail.Status = false;

                    mFD_Detail.CurrencyID = _CurrencyID;
                    mFD_Detail.CurrencyMaster = _CurrencyLocal;

                    mFD_Detail.ReservationID = _RsvID;
                    mFD_Detail.OriginReservationID = _OriginRsvID;

                    mFD_Detail.RoomID = _RoomID;

                    mFD_Detail.FolioID = FolioID;
                    mFD_Detail.OriginFolioID = _OriginFolioID;

                    mFD_Detail.Quantity = _Quan;
                    mFD_Detail.TransactionDate = _BusinessDate;
                    mFD_Detail.PackageID = 0;

                    mFD_Detail.UserInsertID = userID;
                    mFD_Detail.UserUpdateID = userID;
                    mFD_Detail.CreateDate = _SysDate;
                    mFD_Detail.UpdateDate = _SysDate;
                    if (AutoPosting == true)
                    {
                        mFD_Detail.UserID = 0;
                        mFD_Detail.UserName = "$$";
                        mFD_Detail.CashierNo = "";
                        mFD_Detail.ShiftID = 0;
                    }
                    else
                    {
                        mFD_Detail.UserID = userID;
                        mFD_Detail.UserName = userName;
                        mFD_Detail.CashierNo = userName;
                        mFD_Detail.ShiftID = userID;
                    }
                    mFD_Master.ProfitCenterID = _ProID;
                    mFD_Master.ProfitCenterCode = _ProCode;
                    mFD_Master.Status = false;

                    mFD_Master.CurrencyID = _CurrencyID;
                    mFD_Master.CurrencyMaster = _CurrencyLocal;

                    mFD_Master.ReservationID = _RsvID;
                    mFD_Master.OriginReservationID = _OriginRsvID;
                    mFD_Master.RoomID = _RoomID;
                    mFD_Master.FolioID = FolioID;
                    mFD_Master.OriginFolioID = _OriginFolioID;

                    mFD_Master.Quantity = _Quan;
                    mFD_Master.TransactionDate = _BusinessDate;
                    mFD_Master.PackageID = 0;

                    mFD_Master.UserInsertID = userID;
                    mFD_Master.UserUpdateID = userID;
                    mFD_Master.CreateDate = _SysDate;
                    mFD_Master.UpdateDate = _SysDate;
                    if (AutoPosting == true)
                    {
                        mFD_Master.UserID = 0;
                        mFD_Master.UserName = "$$";
                        mFD_Master.CashierNo = "";
                        mFD_Master.ShiftID = 0;
                    }
                    else
                    {
                        mFD_Master.UserID = userID;
                        mFD_Master.UserName = userName;
                        mFD_Master.CashierNo = userName;
                        mFD_Master.ShiftID = userID;
                    }
                    #endregion

                    #region Kiểm tra xem Transaction này có ? trong Generate ?
                    ArrayList arr = new ArrayList();
                    arr.AddRange(GenerateTransactionBO.Instance.FindByAttribute("TransactionCode", _TransCode));

                    // ArrayList arr = new ArrayList(pt.FindByAttribute("GenerateTransaction", "TransactionCode", _TransCode));
                    #endregion

                    #region Nếu chưa tồn tại trong Generate.
                    if ((arr == null) || (arr.Count == 0))
                    {
                        //Gán thông tin cho các propertie còn lại
                        mFD_Detail.IsSplit = false;
                        mFD_Detail.Reference = _Ref;
                        mFD_Detail.Supplement = _Supp;

                        mFD_Detail.TransactionGroupID = mT.TransactionGroupID;
                        mFD_Detail.TransactionSubgroupID = mT.TransactionSubGroupID;
                        mFD_Detail.GroupCode = mT.GroupCode;
                        mFD_Detail.SubgroupCode = mT.SubgroupCode;
                        mFD_Detail.GroupType = mT.GroupType;
                        mFD_Detail.RoomID = _RoomID;
                        mFD_Detail.ArticleCode = _ArCode;
                        mFD_Detail.TransactionCode = mT.Code;
                        mFD_Detail.Description = mT.Description;

                        if (_CurrencyID == "VND")
                        {
                            mFD_Detail.Amount = Math.Round(_Amount, 0);
                            mFD_Detail.AmountBeforeTax = Math.Round(_Amount, 0);
                            mFD_Detail.Price = Math.Round(mFD_Detail.Amount / mFD_Detail.Quantity, 0);
                            mFD_Detail.AmountMaster = Math.Round(pt.ExchangeCurrency(_BusinessDate, _CurrencyID, _CurrencyLocal, _Amount), 0);
                            mFD_Detail.AmountMasterBeforeTax = Math.Round(mFD_Detail.AmountMaster, 0);
                            mFD_Detail.AmountGross = Math.Round(mFD_Detail.Amount, 0);
                            mFD_Detail.AmountMasterGross = Math.Round(mFD_Detail.AmountMaster, 0);
                        }
                        else
                        {
                            mFD_Detail.Amount = _Amount;
                            mFD_Detail.AmountBeforeTax = _Amount;
                            mFD_Detail.Price = mFD_Detail.Amount / mFD_Detail.Quantity;
                            mFD_Detail.AmountMaster = pt.ExchangeCurrency(_BusinessDate, _CurrencyID, _CurrencyLocal, _Amount);
                            mFD_Detail.AmountMasterBeforeTax = mFD_Detail.AmountMaster;
                            mFD_Detail.AmountGross = mFD_Detail.Amount;
                            mFD_Detail.AmountMasterGross = mFD_Detail.AmountMaster;
                        }
                        mFD_Detail.PostType = 1;
                        mFD_Detail.RowState = 1;
                        //Thực hiện post
                        mFD_Detail.RoomType = RoomType;
                        mFD_Detail.RoomTypeID = RoomTypeID;
                        mFD_Detail.ID = (int)pt.Insert(mFD_Detail);

                        mFD_Detail.InvoiceNo = mFD_Detail.ID.ToString();
                        mFD_Detail.TransactionNo = mFD_Detail.ID.ToString();

                        pt.Update(mFD_Detail);
                        //Update số dư
                        UpdateBalance(_RsvID, FolioID, ref _Message);
                        //Trả về thông tin
                        _AmountReturn = mFD_Detail.Amount;
                        _AmountLocalReturn = mFD_Detail.AmountMaster;
                        _TransNoReturn = mFD_Detail.TransactionNo;
                        // Ghi histoty
                        InsertHistory(_SysDate, _BusinessDate, mFD_Detail.FolioID, mFD_Detail.FolioID, mFD_Detail.InvoiceNo, HistoryType.Night_Post,
                            GetActionText(HistoryType.Night_Post, mFD_Detail.TransactionCode, mFD_Detail.Description),
                            "**", mFD_Detail.TransactionCode, mFD_Detail.Description, mFD_Detail.Amount, mFD_Detail.Supplement, "", "", "");
                    }
                    #endregion

                    #region Nếu đã tồn tại trong Generate -> l?y ra và th?c hi?n
                    else
                    {
                        #region Khai báo bi?n
                        decimal s1 = 0, s2 = 0, s3 = 0;
                        decimal CurrentAmount = 0;
                        decimal BaseAmount = _Amount;
                        decimal Rate = 0;
                        GenerateTransactionModel mGT;
                        #endregion

                        #region L?y ra thông tin c?a amount tru?c thu?
                        if (_TaxInclude == true)
                            BaseAmount = GetAmount(arr, Convert.ToDecimal(BaseAmount));
                        #endregion

                        #region Insert dòng tổng
                        mFD_Master.IsSplit = true;
                        mFD_Master.Reference = _Ref;
                        mFD_Master.Supplement = _Supp;

                        mFD_Master.TransactionGroupID = mT.TransactionGroupID;
                        mFD_Master.TransactionSubgroupID = mT.TransactionSubGroupID;
                        mFD_Master.GroupCode = mT.GroupCode;
                        mFD_Master.SubgroupCode = mT.SubgroupCode;
                        mFD_Master.GroupType = mT.GroupType;

                        mFD_Master.ArticleCode = _ArCode;
                        mFD_Master.TransactionCode = mT.Code;
                        mFD_Master.Description = mT.Description;//mT.Description;

                        mFD_Master.Quantity = _Quan;

                        mFD_Master.Price = 0;
                        mFD_Master.Amount = 0;
                        mFD_Master.AmountMaster = 0;
                        mFD_Master.AmountBeforeTax = 0;
                        mFD_Master.AmountMasterBeforeTax = 0;

                        mFD_Master.PostType = 2;
                        mFD_Master.RowState = 1;
                        mFD_Master.RoomID = _RoomID;
                        mFD_Master.RoomTypeID = RoomTypeID;
                        mFD_Master.RoomType = RoomType;
                        mFD_Master.ID = (int)pt.Insert(mFD_Master);

                        mFD_Master.InvoiceNo = mFD_Master.ID.ToString();
                        mFD_Master.TransactionNo = mFD_Master.ID.ToString();
                        #endregion

                        for (int j = 0; j < arr.Count; j++)
                        {
                            #region Đổ dữ liệu vào Model
                            mGT = (GenerateTransactionModel)arr[j];
                            #endregion

                            #region Lấy ra CurrentAmount
                            if (mGT.Type == 0)
                            {
                                if (mGT.BaseAmount == 0)
                                    CurrentAmount = (mGT.Percentage * BaseAmount) / 100;
                                else if (mGT.BaseAmount == 1)
                                    CurrentAmount = (mGT.Percentage * s1) / 100;
                                else if (mGT.BaseAmount == 2)
                                    CurrentAmount = (mGT.Percentage * s2) / 100;
                                else
                                    CurrentAmount = (mGT.Percentage * s3) / 100;
                            }
                            else if (mGT.Type == 1)
                            {
                                CurrentAmount = mGT.Amount;
                            }
                            #endregion

                            #region L?y d? li?u vào s1,s2,s3
                            if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == false) && (mGT.Subtotal3 == false))
                            {
                                s1 = s1 + CurrentAmount;
                            }
                            else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == true) && (mGT.Subtotal3 == false))
                            {
                                s1 = s1 + CurrentAmount;
                                s2 = CurrentAmount;
                            }
                            else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == false) && (mGT.Subtotal3 == true))
                            {
                                s1 = s1 + CurrentAmount;
                                s3 = CurrentAmount;
                            }
                            else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == true) && (mGT.Subtotal3 == true))
                            {
                                s1 = s1 + CurrentAmount;
                                s2 = CurrentAmount;
                                s3 = CurrentAmount;
                            }
                            #endregion

                            #region Ð? d? li?u vào model Model

                            mFD_Detail.Reference = _Ref;

                            mFD_Detail.IsSplit = false;
                            mFD_Detail.PostType = 2;
                            mFD_Detail.RowState = 2;

                            mFD_Detail.TransactionGroupID = mGT.TransactionGroupID;
                            mFD_Detail.TransactionSubgroupID = mGT.TransactionSubGroupID;
                            mFD_Detail.GroupCode = mGT.GroupCode;
                            mFD_Detail.SubgroupCode = mGT.SubgroupCode;
                            mFD_Detail.GroupType = mGT.GroupType;

                            mFD_Detail.TransactionCode = mGT.TransactionCodeDetail;
                            mFD_Detail.Description = mGT.Description;
                            //Làm tròn VND
                            if (_CurrencyID == "VND")
                            {
                                if ((_TaxInclude == true) && (j == arr.Count - 1))
                                    mFD_Detail.Amount = Math.Round(_Amount - mFD_Master.Amount, 0);
                                else
                                    mFD_Detail.Amount = Math.Round(GetAmountFormat(CurrentAmount), 0);
                                mFD_Detail.AmountBeforeTax = Math.Round(mFD_Detail.Amount, 0);
                                mFD_Detail.Price = Math.Round(mFD_Detail.Amount / mFD_Detail.Quantity, 0);
                                mFD_Detail.AmountGross = Math.Round(mFD_Detail.Amount, 0);
                                if (j == 0)
                                {
                                    // Tính ra t? giá n?u là dòng d?u
                                    mFD_Detail.AmountMaster = Math.Round(pt.ExchangeCurrency(_BusinessDate, _CurrencyID, _CurrencyLocal, mFD_Detail.Amount), 0);
                                    Rate = mFD_Detail.AmountMaster / mFD_Detail.Amount;
                                    // N?u là dòng d?u -> insert giá tru?c thu?.
                                    mFD_Master.AmountBeforeTax = Math.Round(mFD_Detail.Amount, 0);
                                    mFD_Master.AmountMasterBeforeTax = Math.Round(mFD_Detail.AmountMaster, 0);
                                }
                                else
                                    mFD_Detail.AmountMaster = Math.Round(mFD_Detail.Amount * Rate, 0);

                                mFD_Detail.AmountMasterBeforeTax = Math.Round(mFD_Detail.AmountMaster, 0);
                                mFD_Detail.AmountMasterGross = Math.Round(mFD_Detail.AmountMaster, 0);
                            }
                            else
                            {
                                if ((_TaxInclude == true) && (j == arr.Count - 1))
                                    mFD_Detail.Amount = _Amount - mFD_Master.Amount;
                                else
                                    mFD_Detail.Amount = GetAmountFormat(CurrentAmount);
                                mFD_Detail.AmountBeforeTax = mFD_Detail.Amount;
                                mFD_Detail.Price = mFD_Detail.Amount / mFD_Detail.Quantity;
                                mFD_Detail.AmountGross = mFD_Detail.Amount;
                                if (j == 0)
                                {
                                    // Tính ra t? giá n?u là dòng d?u
                                    mFD_Detail.AmountMaster = pt.ExchangeCurrency(_BusinessDate, _CurrencyID, _CurrencyLocal, mFD_Detail.Amount);
                                    Rate = mFD_Detail.AmountMaster / mFD_Detail.Amount;
                                    // N?u là dòng d?u -> insert giá tru?c thu?.
                                    mFD_Master.AmountBeforeTax = mFD_Detail.Amount;
                                    mFD_Master.AmountMasterBeforeTax = mFD_Detail.AmountMaster;
                                }
                                else
                                    mFD_Detail.AmountMaster = mFD_Detail.Amount * Rate;

                                mFD_Detail.AmountMasterBeforeTax = mFD_Detail.AmountMaster;
                                mFD_Detail.AmountMasterGross = mFD_Detail.AmountMaster;
                            }
                            #endregion

                            #region Insert Du lieu

                            mFD_Detail.InvoiceNo = mFD_Master.InvoiceNo;
                            mFD_Detail.TransactionNo = mFD_Master.TransactionNo;
                            mFD_Detail.RoomID = _RoomID;
                            mFD_Detail.RoomType = RoomType;
                            mFD_Detail.RoomTypeID = RoomTypeID;
                            mFD_Detail.ID = (int)pt.Insert(mFD_Detail);

                            mFD_Master.AmountMaster = mFD_Master.AmountMaster + mFD_Detail.AmountMaster;
                            mFD_Master.Amount = mFD_Master.Amount + mFD_Detail.Amount;

                            #endregion
                        }
                        // Tính giá Gross
                        // Làm tròn VND
                        if (_CurrencyID == "VND")
                        {
                            mFD_Master.AmountGross = Math.Round(mFD_Master.Amount, 0);
                            mFD_Master.AmountMasterGross = Math.Round(mFD_Master.AmountMaster, 0);
                            // Tính giá Net n?u s? ti?n nh?p vào là giá sau thu?
                            if (_TaxInclude == true)
                            {
                                mFD_Master.Amount = Math.Round(_Amount, 0);
                                mFD_Master.AmountMaster = Math.Round(_Amount * Rate, 0);
                            }
                            mFD_Master.Price = Math.Round(mFD_Master.Amount / mFD_Master.Quantity);
                        }
                        else
                        {
                            mFD_Master.AmountGross = mFD_Master.Amount;
                            mFD_Master.AmountMasterGross = mFD_Master.AmountMaster;
                            // Tính giá Net n?u s? ti?n nh?p vào là giá sau thu?
                            if (_TaxInclude == true)
                            {
                                mFD_Master.Amount = _Amount;
                                mFD_Master.AmountMaster = _Amount * Rate;
                            }
                            mFD_Master.Price = mFD_Master.Amount / mFD_Master.Quantity;
                        }
                        pt.Update(mFD_Master);
                        //Update số dư
                        UpdateBalance(_RsvID, FolioID, ref _Message);
                        //Trả về thông tin
                        _AmountReturn = mFD_Master.Amount;
                        _AmountLocalReturn = mFD_Master.AmountMaster;
                        _TransNoReturn = mFD_Master.TransactionNo;
                        // Ghi histoty
                        InsertHistory(_SysDate, _BusinessDate, mFD_Master.FolioID, mFD_Master.FolioID, mFD_Master.InvoiceNo, HistoryType.Night_Post,
                            GetActionText(HistoryType.Night_Post, mFD_Master.TransactionCode, mFD_Master.Description),
                            "$$", mFD_Master.TransactionCode, mFD_Master.Description, mFD_Master.Amount, mFD_Master.Supplement, "", "", "");

                    }
                    #endregion

                    #region Commit-Return
                    pt.CommitTransaction();
                    pt.CloseConnection();
                    return true;
                    #endregion
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                pt.CloseConnection();
                _Message = ex.Message;
                return false;
            }
        }
        /// <summary>
        /// Post tiền fixed charge lên folio
        /// -- CSS, 18/05/2011
        /// </summary>
        private void _PostFixedCharge(ref bool _IsOK, DataTable _dtRF, int userID, string userName)
        {
            try
            {
                _IsOK = true;
                //Danh sách phòng cần post
                if (_dtRF.Rows.Count > 0)
                {
                    #region 1.Khai báo biến
                    //Lấy ra ngày BB
                    DateTime _BusDate = TextUtils.GetBusinessDate();
                    DateTime _SysDate = TextUtils.GetSystemDate();
                    //Xác định xem fc có được post hay ko?
                    bool _IsPost = false;
                    #endregion

                    for (int i = 0; i < _dtRF.Rows.Count; i++)
                    {
                        #region 3.Process
                        //Kiểm tra nếu đã post AdvanceBill thì ko xử lý
                        if (_CheckAdvanceBill(TextUtils.ToInt(_dtRF.Rows[i]["ID"].ToString()), _BusDate) == false)
                        {
                            //Lấy thông tin bảng FixedCharge
                            DataTable _dtFC = TextUtils.Select("SELECT a.TransactionCode, a.ArticlesCode, a.Description, a.Amount, a.AmountAfterTax, " +
                                                               "a.IsTaxInclude, a.BeginDate, a.EndDate, a.Quantity,a.CurrencyID, " +
                                                               "a.PostingRhythmID, a.PostingDate, a.PostingDay " +
                                                               "FROM dbo.ReservationFixedCharge a " +
                                                               "WHERE a.ReservationID = " + _dtRF.Rows[i]["ID"].ToString() + " ");

                            if (_dtFC.Rows.Count > 0)
                            {
                                for (int j = 0; j < _dtFC.Rows.Count; j++)
                                {
                                    //Nếu có giá mới xử lý
                                    if (TextUtils.ToDecimal(_dtFC.Rows[j]["AmountAfterTax"].ToString()) > 0)
                                    {
                                        #region 3.1.Kiểm tra thời điểm charge
                                        switch (_dtFC.Rows[j]["PostingRhythmID"].ToString())
                                        {
                                            case "1"://Every Night
                                                {
                                                    if (TextUtils.CompareDate(Convert.ToDateTime(_dtFC.Rows[j]["BeginDate"]), _BusDate) <= 0
                                                     && TextUtils.CompareDate(Convert.ToDateTime(_dtFC.Rows[j]["EndDate"]), _BusDate) > 0)
                                                        _IsPost = true;
                                                    else
                                                        _IsPost = false;
                                                    break;
                                                }
                                            case "2"://At Date
                                                {
                                                    if (TextUtils.CompareDate(Convert.ToDateTime(_dtFC.Rows[j]["PostingDate"]), _BusDate) == 0)
                                                        _IsPost = true;
                                                    else
                                                        _IsPost = false;
                                                    break;
                                                }
                                            case "3"://At Day
                                                {
                                                    string[] PD = _dtFC.Rows[j]["PostingDay"].ToString().Split(',');
                                                    _IsPost = false;
                                                    DateTime ArrivalDate = Convert.ToDateTime(_dtRF.Rows[i]["ArrivalDate"]);
                                                    for (int d = 0; d < PD.Length; d++)
                                                    {
                                                        int AddDay = TextUtils.ToInt(PD[d]) - 1;
                                                        if (TextUtils.CompareDate(ArrivalDate.AddDays(AddDay), _BusDate) == 0)
                                                            _IsPost = true;
                                                    }

                                                    break;
                                                }
                                            case "4"://Checkin
                                                {
                                                    DateTime ArrivalDate = Convert.ToDateTime(_dtRF.Rows[i]["ArrivalDate"]);
                                                    if (TextUtils.CompareDate(_BusDate, ArrivalDate) == 0)
                                                        _IsPost = true;
                                                    else
                                                        _IsPost = false;
                                                    break;
                                                }
                                            case "5"://Check Out
                                                {
                                                    DateTime DepartureDate = Convert.ToDateTime(_dtRF.Rows[i]["DepartureDate"]);
                                                    if (TextUtils.CompareDate(_BusDate, DepartureDate.AddDays(-1)) == 0)
                                                        _IsPost = true;
                                                    else
                                                        _IsPost = false;
                                                    break;
                                                }
                                        }
                                        #endregion

                                        if (_IsPost == true)
                                        {
                                            #region 3.2.Khai báo biến
                                            //Rsv                                    
                                            int _WindownNo = 0;
                                            int _RsvID = TextUtils.ToInt(_dtRF.Rows[i]["ID"].ToString());
                                            int _ToRsvID = TextUtils.ToInt(_dtRF.Rows[i]["ID"].ToString());
                                            int _ProfileID = TextUtils.ToInt(_dtRF.Rows[i]["ProfileIndividualID"].ToString());
                                            string _GuestName = _dtRF.Rows[i]["LastName"].ToString();
                                            string _ConfirmNo = _dtRF.Rows[i]["ConfirmationNo"].ToString();
                                            //Room
                                            int _RoomTypeID = TextUtils.ToInt(_dtRF.Rows[i]["RoomTypeID"].ToString());
                                            string _RoomType = _dtRF.Rows[i]["RoomType"].ToString();
                                            int _RoomID = TextUtils.ToInt(_dtRF.Rows[i]["RoomID"].ToString());
                                            string _RoomNo = _dtRF.Rows[i]["RoomNo"].ToString();
                                            //Thông tin Reff
                                            string _Reffrence = "";
                                            if (_dtRF.Rows[i]["RateCode"].ToString() != "")
                                                _Reffrence = "R:" + _RoomNo + "; D:" + _BusDate.ToString("dd/MM") + "; C:" + _ConfirmNo + "; R" + _dtRF.Rows[i]["RateCode"].ToString();
                                            else
                                                _Reffrence = "R:" + _RoomNo + "; D:" + _BusDate.ToString("dd/MM") + "; C:" + _ConfirmNo + " ";
                                            //Thông tin trong bảng FC
                                            bool _TaxInclude = false;
                                            decimal _Amount = 0;
                                            _TaxInclude = bool.Parse(_dtFC.Rows[j]["IsTaxInclude"].ToString());
                                            if (_TaxInclude == true)
                                                _Amount = TextUtils.ToDecimal(_dtFC.Rows[j]["AmountAfterTax"].ToString());
                                            else
                                                _Amount = TextUtils.ToDecimal(_dtFC.Rows[j]["Amount"].ToString());
                                            string _CurrencyID = _dtFC.Rows[j]["CurrencyID"].ToString();
                                            string _TransactionCode = _dtFC.Rows[j]["TransactionCode"].ToString();
                                            string _ArticlesCode = _dtFC.Rows[j]["ArticlesCode"].ToString();
                                            int _Qty = Convert.ToInt16(_dtFC.Rows[j]["Quantity"].ToString());
                                            _Amount = _Amount * _Qty;
                                            //Other
                                            string _Supplement = "";
                                            decimal _AmountReturn = 0;
                                            decimal _AmountMasterReturn = 0;
                                            string _err = "";
                                            string _Desc = "";
                                            string _TransNoReturn = "";
                                            //Xác định folio default
                                            int _OriginFolioID = _GetFolioDefault(_RsvID);
                                            #endregion

                                            #region 3.3.Check Routing
                                            _CheckRouting(_TransactionCode, _ConfirmNo, _RsvID, _BusDate, ref _ProfileID, ref _GuestName, ref _OriginFolioID, ref _WindownNo, ref _ToRsvID, userID);
                                            #endregion

                                            #region 3.4.Posting to folio
                                            PostingToFolio(true, _SysDate, _BusDate, 0, "", _ConfirmNo, _ToRsvID, _RoomID, _RsvID, _OriginFolioID,
                                                                    _ProfileID, _GuestName, _WindownNo, _TransactionCode, _ArticlesCode, _Reffrence,
                                                                    _Supplement, _Amount, _TaxInclude, _Qty, _CurrencyID, MasterCurrencyID,
                                                                    ref _AmountReturn, ref _AmountMasterReturn, ref _TransNoReturn, ref _err, _RoomTypeID, _RoomType, userID, userName);

                                            IF_XO(_RoomNo, TextUtils.GetBusinessDateTime().ToString(), TextUtils.GetBusinessDateTime().ToString(), "0", _Amount.ToString(), MasterCurrencyID.ToString(), _Reffrence, _Supplement, _RsvID.ToString(), _ProfileID.ToString());

                                            #endregion
                                        }
                                    }
                                }
                                //Ghi vào file log
                                SaveLog("Fixed Charge - OK");
                            }
                        }
                        #endregion
                    }
                }
            }
            catch (Exception ex)
            {
                _Error = ex.Message;
                _IsOK = false;

            }
        }

        /// <summary>
        /// Kiểm tra thời điểm charge
        /// -- CSS, 02/06/11
        /// </summary>
        /// <param name="_PostingRhythm"></param>
        /// <param name="_dtPck"></param>
        /// <param name="_Row"></param>
        /// <param name="_BusDate"></param>
        /// <param name="_Arr"></param>
        /// <param name="_Dep"></param>
        /// <returns></returns>
        private bool _CheckPostingRhythm(string _PostingRhythm, DataTable _dtPck, int _Row, DateTime _BusDate, DateTime _Arr, DateTime _Dep)
        {
            bool _IsPost = false;
            switch (_PostingRhythm)
            {
                case "1"://Every Night
                    {
                        if (TextUtils.CompareDate(Convert.ToDateTime(_dtPck.Rows[_Row]["BeginDate"].ToString()), _BusDate) <= 0
                            && TextUtils.CompareDate(Convert.ToDateTime(_dtPck.Rows[_Row]["EndDate"].ToString()), _BusDate) > 0)
                        {
                            _IsPost = true;
                        }
                        break;
                    }
                case "2"://Date
                    {
                        if (TextUtils.CompareDate(Convert.ToDateTime(_dtPck.Rows[_Row]["PostingDate"].ToString()), _BusDate) == 0)
                            _IsPost = true;
                        break;
                    }
                case "3"://Day
                    {
                        //C2 - New
                        string str = _dtPck.Rows[_Row]["PostingDay"].ToString();
                        string[] arr = str.Split(',');
                        for (int i = 0; i < arr.Length; i++)
                        {
                            int _day = int.Parse(arr[i].ToString()) - 1;
                            if (TextUtils.CompareDate(Convert.ToDateTime(_dtPck.Rows[_Row]["BeginDate"].ToString()).AddDays(_day), _BusDate) == 0)
                            {
                                _IsPost = true;
                                break;
                            }
                        }
                        break;
                    }
                case "4"://Checkin
                    {
                        if (TextUtils.CompareDate(_Arr, _BusDate) == 0)
                            _IsPost = true;
                        break;
                    }
                case "5"://Check Out
                    {
                        if (TextUtils.CompareDate(_Dep.AddDays(-1), _BusDate) == 0)
                            _IsPost = true;
                        break;
                    }
            }
            return _IsPost;
        }


        /// <summary>
        /// Post tiền Package lên folio
        /// -- CSS, 18/05/2011
        /// </summary>
        private void _PostPackage(ref bool _IsOK, DataTable _dtRF, int userID, string userName)
        {
            try
            {
                _IsOK = true;
                //Danh sách phòng cần post
                if (_dtRF.Rows.Count > 0)
                {
                    #region 1.Khai báo biến
                    //Lấy ra ngày BB
                    DateTime _BusDate = TextUtils.GetBusinessDate();
                    DateTime _SysDate = TextUtils.GetSystemDate();
                    #endregion

                    for (int i = 0; i < _dtRF.Rows.Count; i++)
                    {
                        #region 3.Process
                        //Kiểm tra nếu đã post AdvanceBill thì ko xử lý
                        if (_CheckAdvanceBill(TextUtils.ToInt(_dtRF.Rows[i]["ID"].ToString()), _BusDate) == false)
                        {
                            //Lấy thông tin bảng ReservationPackage
                            DataTable _tbRPck = TextUtils.Select("SELECT a.PackageID, a.TransactionCode, a.Description, a.Price, a.PriceAfterTax, a.CurrencyID, " +
                                                                 "a.PostingRhythmID, a.PostingDate, a.PostingDay, BeginDate, EndDate, " +
                                                                 "a.IsTaxInclude, a.CalculationRuleID, a.Quantity, b.TransCodeAlt, b.Description AS [Pck_Description] " +
                                                                 "FROM ReservationPackage a WITH (NOLOCK), Package b WITH (NOLOCK) " +
                                                                 "WHERE a.PackageID = b.ID AND b.IncludedInRate = 0 " +
                                                                 "AND a.ReservationID = " + _dtRF.Rows[i]["ID"].ToString() + " ORDER BY  a.TransactionCode");
                            if (_tbRPck.Rows.Count > 0)
                            {
                                #region 3.1.Khai báo biến
                                //Rsv                                    
                                int _WindownNo = 0;
                                int _RsvID = TextUtils.ToInt(_dtRF.Rows[i]["ID"].ToString());
                                int _ToRsvID = TextUtils.ToInt(_dtRF.Rows[i]["ID"].ToString());
                                int _ProfileID = TextUtils.ToInt(_dtRF.Rows[i]["ProfileIndividualID"].ToString());
                                string _GuestName = _dtRF.Rows[i]["LastName"].ToString();
                                string _ConfirmNo = _dtRF.Rows[i]["ConfirmationNo"].ToString();
                                //Rate
                                int _RoomTypeID = TextUtils.ToInt(_dtRF.Rows[i]["RoomTypeID"].ToString());
                                string _RoomType = _dtRF.Rows[i]["RoomType"].ToString();
                                int _RoomID = TextUtils.ToInt(_dtRF.Rows[i]["RoomID"].ToString());
                                string _RoomNo = _dtRF.Rows[i]["RoomNo"].ToString();
                                //Return (ref value when post)
                                string _Desc = "";
                                decimal AmountMasterReturn = 0;
                                string _err = "";
                                //Xác định mảng
                                string[] _TransactionCode = new string[_tbRPck.Rows.Count];
                                string[] _Description = new string[_tbRPck.Rows.Count];
                                string[] _ArticleCode = new string[_tbRPck.Rows.Count];
                                decimal[] _Amount = new decimal[_tbRPck.Rows.Count];
                                int[] _Quantity = new int[_tbRPck.Rows.Count];
                                string[] _CurrencyID = new string[_tbRPck.Rows.Count];
                                string[] _Reffrence = new string[_tbRPck.Rows.Count];
                                string[] _Supplement = new string[_tbRPck.Rows.Count];
                                bool[] _PostingStatus = new bool[_tbRPck.Rows.Count];
                                bool[] _IncludeRate = new bool[_tbRPck.Rows.Count];
                                bool[] _TaxInclude = new bool[_tbRPck.Rows.Count];
                                bool[] _IsTransactionPosting = new bool[_tbRPck.Rows.Count];
                                //Xác định folio default
                                int _OriginFolioID = _GetFolioDefault(_RsvID);
                                //Xác định tiền Discount sẽ trừ cho TransactionCode nào
                                string _PckDis = TextUtils.Select("Select KeyValue From ConfigSystem Where KeyName ='PackageDiscount'").Rows[0]["KeyValue"].ToString();
                                //Xác định TransactionCode Của gói Pck
                                string _PckTransCode = _tbRPck.Rows[0]["TransCodeAlt"].ToString();
                                _Desc = _tbRPck.Rows[0]["Pck_Description"].ToString();
                                //Xác định xem post transaction nào
                                int _Count = 0;
                                #endregion

                                //Xác định tiền Package
                                for (int j = 0; j < _tbRPck.Rows.Count; j++)
                                {
                                    if (_CheckPostingRhythm(_tbRPck.Rows[j]["PostingRhythmID"].ToString(), _tbRPck, j, _BusDate, TextUtils.ToDate(_dtRF.Rows[i]["ArrivalDate"].ToString()), TextUtils.ToDate(_dtRF.Rows[i]["DepartureDate"].ToString())) == true)
                                    {
                                        //Xác định TransactionCode Của gói Pck
                                        _Desc = _tbRPck.Rows[j]["Pck_Description"].ToString();
                                        _PckTransCode = _tbRPck.Rows[j]["TransCodeAlt"].ToString();

                                        #region 1.CanculationRule
                                        Decimal Price = 0;
                                        Decimal OriginPrice = 0;
                                        if (bool.Parse(_tbRPck.Rows[j]["IsTaxInclude"].ToString()) == false)
                                            OriginPrice = Decimal.Parse(_tbRPck.Rows[j]["Price"].ToString());
                                        else
                                            OriginPrice = Decimal.Parse(_tbRPck.Rows[j]["PriceAfterTax"].ToString());

                                        //1:Per/Person(từng người trong phòng)
                                        if (int.Parse(_tbRPck.Rows[j]["CalculationRuleID"].ToString()) == 1)
                                        {
                                            if (int.Parse(_dtRF.Rows[i]["NoOfAdult"].ToString()) > 0)
                                                Price = OriginPrice * Decimal.Parse(_dtRF.Rows[i]["NoOfAdult"].ToString());
                                            if (int.Parse(_dtRF.Rows[i]["NoOfChild"].ToString()) > 0)
                                                Price = Price + (OriginPrice * Decimal.Parse(_dtRF.Rows[i]["NoOfChild"].ToString()));
                                            if (int.Parse(_dtRF.Rows[i]["NoOfChild1"].ToString()) > 0)
                                                Price = Price + (OriginPrice * Decimal.Parse(_dtRF.Rows[i]["NoOfChild1"].ToString()));
                                            if (int.Parse(_dtRF.Rows[i]["NoOfChild2"].ToString()) > 0)
                                                Price = Price + (OriginPrice * Decimal.Parse(_dtRF.Rows[i]["NoOfChild2"].ToString()));
                                            else if (int.Parse(_dtRF.Rows[i]["NoOfAdult"].ToString()) == 0 && int.Parse(_dtRF.Rows[i]["NoOfChild"].ToString()) == 0 && int.Parse(_dtRF.Rows[i]["NoOfChild1"].ToString()) == 0 && int.Parse(_dtRF.Rows[i]["NoOfChild2"].ToString()) == 0)
                                                Price = 0;
                                        }
                                        //2: Per Adult
                                        else if (int.Parse(_tbRPck.Rows[j]["CalculationRuleID"].ToString()) == 2)
                                        {
                                            if (int.Parse(_dtRF.Rows[i]["NoOfAdult"].ToString()) > 0)
                                                Price = OriginPrice * Decimal.Parse(_dtRF.Rows[i]["NoOfAdult"].ToString());
                                            else if (int.Parse(_dtRF.Rows[i]["NoOfAdult"].ToString()) == 0)
                                                Price = 0;
                                        }
                                        //3:Per Child
                                        else if (int.Parse(_tbRPck.Rows[j]["CalculationRuleID"].ToString()) == 3)
                                        {
                                            if (int.Parse(_dtRF.Rows[i]["NoOfChild"].ToString()) > 0)
                                                Price = OriginPrice * Decimal.Parse(_dtRF.Rows[i]["NoOfChild"].ToString());
                                            else if (int.Parse(_dtRF.Rows[i]["NoOfChild"].ToString()) == 0)
                                                Price = 0;
                                        }
                                        //4: Per Room
                                        else if (int.Parse(_tbRPck.Rows[j]["CalculationRuleID"].ToString()) == 4)
                                            Price = OriginPrice;
                                        //5:Per Child1
                                        else if (int.Parse(_tbRPck.Rows[j]["CalculationRuleID"].ToString()) == 5)
                                        {
                                            if (int.Parse(_dtRF.Rows[i]["NoOfChild1"].ToString()) > 0)
                                                Price = OriginPrice * Decimal.Parse(_dtRF.Rows[i]["NoOfChild1"].ToString());
                                            else if (int.Parse(_dtRF.Rows[i]["NoOfChild1"].ToString()) == 0)
                                                Price = 0;
                                        }
                                        //6:Per Child2
                                        else if (int.Parse(_tbRPck.Rows[j]["CalculationRuleID"].ToString()) == 6)
                                        {
                                            if (int.Parse(_dtRF.Rows[i]["NoOfChild2"].ToString()) > 0)
                                                Price = OriginPrice * Decimal.Parse(_dtRF.Rows[i]["NoOfChild2"].ToString());
                                            else if (int.Parse(_dtRF.Rows[i]["NoOfChild2"].ToString()) == 0)
                                                Price = 0;
                                        }
                                        //8:Per A + C
                                        else if (int.Parse(_tbRPck.Rows[j]["CalculationRuleID"].ToString()) == 8)
                                        {
                                            if (int.Parse(_dtRF.Rows[i]["NoOfAdult"].ToString()) > 0)
                                                Price = OriginPrice * Decimal.Parse(_dtRF.Rows[i]["NoOfAdult"].ToString());
                                            if (int.Parse(_dtRF.Rows[i]["NoOfChild"].ToString()) > 0)
                                                Price = Price + (OriginPrice * Decimal.Parse(_dtRF.Rows[i]["NoOfChild"].ToString()));
                                            else if (int.Parse(_dtRF.Rows[i]["NoOfAdult"].ToString()) == 0 && int.Parse(_dtRF.Rows[i]["NoOfChild"].ToString()) == 0)
                                                Price = 0;
                                        }
                                        //9:Per A + C + C1
                                        else if (int.Parse(_tbRPck.Rows[j]["CalculationRuleID"].ToString()) == 9)
                                        {
                                            if (int.Parse(_dtRF.Rows[i]["NoOfAdult"].ToString()) > 0)
                                                Price = OriginPrice * Decimal.Parse(_dtRF.Rows[i]["NoOfAdult"].ToString());
                                            if (int.Parse(_dtRF.Rows[i]["NoOfChild"].ToString()) > 0)
                                                Price = Price + (OriginPrice * Decimal.Parse(_dtRF.Rows[i]["NoOfChild"].ToString()));
                                            if (int.Parse(_dtRF.Rows[i]["NoOfChild1"].ToString()) > 0)
                                                Price = Price + (OriginPrice * Decimal.Parse(_dtRF.Rows[i]["NoOfChild1"].ToString()));
                                            else if (int.Parse(_dtRF.Rows[i]["NoOfAdult"].ToString()) == 0 && int.Parse(_dtRF.Rows[i]["NoOfChild"].ToString()) == 0 && int.Parse(_dtRF.Rows[i]["NoOfChild1"].ToString()) == 0)
                                                Price = 0;
                                        }
                                        //10:Per A + C1
                                        else if (int.Parse(_tbRPck.Rows[j]["CalculationRuleID"].ToString()) == 10)
                                        {
                                            if (int.Parse(_dtRF.Rows[i]["NoOfAdult"].ToString()) > 0)
                                                Price = OriginPrice * Decimal.Parse(_dtRF.Rows[i]["NoOfAdult"].ToString());
                                            if (int.Parse(_dtRF.Rows[i]["NoOfChild1"].ToString()) > 0)
                                                Price = Price + (OriginPrice * Decimal.Parse(_dtRF.Rows[i]["NoOfChild1"].ToString()));
                                            else if (int.Parse(_dtRF.Rows[i]["NoOfAdult"].ToString()) == 0 && int.Parse(_dtRF.Rows[i]["NoOfChild1"].ToString()) == 0)
                                                Price = 0;
                                        }
                                        //11:Per C + C1
                                        else if (int.Parse(_tbRPck.Rows[j]["CalculationRuleID"].ToString()) == 11)
                                        {
                                            if (int.Parse(_dtRF.Rows[i]["NoOfChild"].ToString()) > 0)
                                                Price = OriginPrice * Decimal.Parse(_dtRF.Rows[i]["NoOfChild"].ToString());
                                            if (int.Parse(_dtRF.Rows[i]["NoOfChild1"].ToString()) > 0)
                                                Price = Price + (OriginPrice * Decimal.Parse(_dtRF.Rows[i]["NoOfChild1"].ToString()));
                                            else if (int.Parse(_dtRF.Rows[i]["NoOfChild"].ToString()) == 0 && int.Parse(_dtRF.Rows[i]["NoOfChild1"].ToString()) == 0)
                                                Price = 0;
                                        }
                                        #endregion

                                        #region 2.Trừ tiền Discount nếu có
                                        //Nhân với số lượng
                                        Price = Price * Decimal.Parse(_tbRPck.Rows[j]["Quantity"].ToString());
                                        //Discount
                                        if (Price > 0)
                                        {
                                            Price = Price - ((Price * TextUtils.ToDecimal(_dtRF.Rows[i]["DiscountRate"].ToString())) / 100)
                                                        - (TextUtils.ToDecimal(_dtRF.Rows[i]["DiscountAmount"].ToString()));
                                        }
                                        #endregion

                                        #region 3.Gán giá trị vào mảng
                                        _TransactionCode[_Count] = _GetTransactionCodeBB(_RoomTypeID.ToString(), _tbRPck.Rows[j]["TransactionCode"].ToString());
                                        _Description[_Count] = _tbRPck.Rows[j]["Description"].ToString();
                                        _ArticleCode[_Count] = "";
                                        _Amount[_Count] = Price;
                                        _Quantity[_Count] = int.Parse(_tbRPck.Rows[j]["Quantity"].ToString()); ;
                                        _CurrencyID[_Count] = _tbRPck.Rows[j]["CurrencyID"].ToString();
                                        _Reffrence[_Count] = "R:" + _RoomNo + "; D:" + _BusDate.ToString("dd/MM") + "; C:" + _ConfirmNo;
                                        _PostingStatus[_Count] = true;
                                        _IncludeRate[_Count] = false;
                                        _IsTransactionPosting[_Count] = true;
                                        _TaxInclude[_Count] = bool.Parse(_tbRPck.Rows[j]["IsTaxInclude"].ToString());
                                        #endregion

                                        _Count = _Count + 1;
                                    }
                                }
                                //Có giá trị mới post
                                if (_Count > 0)
                                {
                                    #region 3.3.Check Routing
                                    _CheckRouting(_PckTransCode, _ConfirmNo, _RsvID, _BusDate, ref _ProfileID, ref _GuestName, ref _OriginFolioID, ref _WindownNo, ref _ToRsvID, userID);
                                    #endregion

                                    #region 3.4.Posting to folio
                                    PostingPackage(true, _SysDate, _BusDate, 0, "", _ConfirmNo, _ToRsvID, _RoomID, _RsvID, _OriginFolioID,
                                                            _ProfileID, _GuestName, _WindownNo, 0, _PckTransCode, _TransactionCode, _ArticleCode,
                                                            _Amount, _TaxInclude, _Quantity, _CurrencyID, MasterCurrencyID, _Reffrence, _Supplement,
                                                            ref AmountMasterReturn, ref _err, _Desc, _RoomTypeID, _RoomType, userID, userName);

                                    IF_XO(_RoomNo, TextUtils.GetBusinessDateTime().ToString(), TextUtils.GetBusinessDateTime().ToString(), "0", _Amount.ToString(), MasterCurrencyID.ToString(), _Reffrence[0], _Supplement[0], _RsvID.ToString(), _ProfileID.ToString());

                                    #endregion
                                }

                            }
                        }
                        #endregion
                    }
                    //Ghi vào file log
                    SaveLog("Package Posting - OK");
                }
            }
            catch (Exception ex)
            {
                _Error = ex.Message;
                _IsOK = false;

            }
        }

        public void _PostCommission(ref bool _IsOK, DataTable _dtRF, int userID, string userName)
        {
            dt_RoomType = TextUtils.Select("Select * From RoomType with (nolock)");
            try
            {
                _IsOK = true;
                //Danh sách phòng cần post
                if (_dtRF.Rows.Count > 0)
                {
                    #region 1.Khai báo biến
                    //Lấy ra ngày BB
                    DateTime _BusDate = TextUtils.GetBusinessDate();
                    DateTime _SysDate = TextUtils.GetSystemDate();
                    #endregion

                    for (int i = 0; i < _dtRF.Rows.Count; i++)
                    {
                        #region 3.Process
                        DataTable dtRsvCom = TextUtils.Select("select a.*, b.* from ReservationCommission a with (nolock) " +
                            "join CommissionDetail b with (nolock) on a.CommissionID = b.CommissionID " +
                            "where a.ReservationID = " + _dtRF.Rows[i]["ID"].ToString() + " and Datediff(day, b.StartDate, '" + _BusDate.ToString("yyyy-MM-dd") + "') >=0 " +
                            "and Datediff(day, b.EndDate, '" + _BusDate.ToString("yyyy-MM-dd") + "') <=0");

                        string x = "select a.*, b.* from ReservationCommission a with (nolock) " +
                            "join CommissionDetail b with (nolock) on a.CommissionID = b.CommissionID " +
                            "where a.ReservationID = " + _dtRF.Rows[i]["ID"].ToString() + " and Datediff(day, b.StartDate, '" + _BusDate.ToString("yyyy-MM-dd") + "') >=0 " +
                            "and Datediff(day, b.EndDate, '" + _BusDate.ToString("yyyy-MM-dd") + "') <=0";

                        if (dtRsvCom.Rows.Count > 0)
                        {
                            #region 3.1.Khai báo biến
                            //Rsv                                    
                            int _WindownNo = 0;
                            int _RsvID = TextUtils.ToInt(_dtRF.Rows[i]["ID"].ToString());
                            int _ToRsvID = TextUtils.ToInt(_dtRF.Rows[i]["ID"].ToString());
                            int _ProfileID = TextUtils.ToInt(_dtRF.Rows[i]["ProfileIndividualID"].ToString());
                            string _GuestName = _dtRF.Rows[i]["LastName"].ToString();
                            string _ConfirmNo = _dtRF.Rows[i]["ConfirmationNo"].ToString();
                            //Rate
                            int _RoomTypeID = TextUtils.ToInt(_dtRF.Rows[i]["RoomTypeID"].ToString());
                            string _RoomType = _dtRF.Rows[i]["RoomType"].ToString();
                            int _RoomID = TextUtils.ToInt(_dtRF.Rows[i]["RoomID"].ToString());
                            string _RoomNo = _dtRF.Rows[i]["RoomNo"].ToString();
                            //Return (ref value when post)
                            string _Desc = "";
                            decimal AmountMasterReturn = 0;
                            string _err = "";
                            //Xác định mảng
                            string[] _TransactionCode = new string[dtRsvCom.Rows.Count];
                            string[] _Description = new string[dtRsvCom.Rows.Count];
                            string[] _ArticleCode = new string[dtRsvCom.Rows.Count];
                            decimal[] _Amount = new decimal[dtRsvCom.Rows.Count];
                            int[] _Quantity = new int[dtRsvCom.Rows.Count];
                            string[] _CurrencyID = new string[dtRsvCom.Rows.Count];
                            string[] _Reffrence = new string[dtRsvCom.Rows.Count];
                            string[] _Supplement = new string[dtRsvCom.Rows.Count];
                            bool[] _PostingStatus = new bool[dtRsvCom.Rows.Count];
                            bool[] _IncludeRate = new bool[dtRsvCom.Rows.Count];
                            bool[] _TaxInclude = new bool[dtRsvCom.Rows.Count];
                            bool[] _IsTransactionPosting = new bool[dtRsvCom.Rows.Count];
                            //Xác định folio default
                            int _OriginFolioID = _GetFolioDefault(_RsvID);
                            //Xác định TransactionCode Của gói Pck
                            string _PckTransCode = dtRsvCom.Rows[0]["TransactionCode"].ToString();
                            _Desc = dtRsvCom.Rows[0]["Description"].ToString();
                            //Xác định xem post transaction nào
                            int _Count = 0;
                            #endregion

                            decimal _AmountCommission = 0;
                            decimal _PercentCommission = 0;
                            decimal _TotalAmount = 0; //tien hoa hong nhan dc
                            _TotalAmount = _AmountCommission = TextUtils.ToDecimal((string)dtRsvCom.Rows[0]["AmountAfterTax"]);
                            _PercentCommission = TextUtils.ToDecimal((string)dtRsvCom.Rows[0]["CommissionPercent"]);

                            if (_PercentCommission > 0)
                            {
                                //neu hoa hong la % thi se tinh % tren gia phong voi ngay bussinessdate
                                DataTable dtRate = TextUtils.Select("select a.RateAfterTax from ReservationRate a with (nolock) " +
                                    "where a.ReservationID = " + _dtRF.Rows[i]["ID"].ToString() + " and datediff(day, a.RateDate, '" + _BusDate.ToString("yyyy-MM-dd") + "') = 0");
                                if (dtRate.Rows.Count > 0)
                                {
                                    //MidpointRounding.AwayFromZero: lam tron (vi du: 12.45 -> 12.5)
                                    _TotalAmount = Math.Round(_PercentCommission * TextUtils.ToDecimal((string)dtRate.Rows[0][0]) / 100, 0, MidpointRounding.AwayFromZero);
                                }
                            }

                            //Xác định tiền hoa hong
                            for (int j = 0; j < dtRsvCom.Rows.Count; j++)
                            {
                                #region 3.Gán giá trị vào mảng
                                _TransactionCode[_Count] = _GetTransactionCodeBB(_RoomTypeID.ToString(), dtRsvCom.Rows[j]["TransactionCode"].ToString());
                                _Description[_Count] = dtRsvCom.Rows[j]["Description"].ToString();
                                _ArticleCode[_Count] = "";
                                _Amount[_Count] = _TotalAmount;
                                _Quantity[_Count] = 1;
                                _CurrencyID[_Count] = dtRsvCom.Rows[j]["CurrencyID"].ToString();
                                _Reffrence[_Count] = "R:" + _RoomNo + "; D:" + _BusDate.ToString("dd/MM") + "; C:" + _ConfirmNo;
                                _PostingStatus[_Count] = true;
                                _IncludeRate[_Count] = false;
                                _IsTransactionPosting[_Count] = true;
                                _TaxInclude[_Count] = true;
                                #endregion
                                _Count = _Count + 1;
                            }

                            //Có giá trị mới post
                            if (_Count > 0)
                            {
                                #region 3.3.Check Routing
                                _CheckRouting(_PckTransCode, _ConfirmNo, _RsvID, _BusDate, ref _ProfileID, ref _GuestName, ref _OriginFolioID, ref _WindownNo, ref _ToRsvID, userID);
                                #endregion

                                #region 3.4.Posting to folio
                                PostingPackage(true, _SysDate, _BusDate, 0, "", _ConfirmNo, _ToRsvID, _RoomID, _RsvID, _OriginFolioID,
                                                        _ProfileID, _GuestName, _WindownNo, 0, _PckTransCode, _TransactionCode, _ArticleCode,
                                                        _Amount, _TaxInclude, _Quantity, _CurrencyID, "VND", _Reffrence, _Supplement,
                                                        ref AmountMasterReturn, ref _err, _Desc, _RoomTypeID, _RoomType, userID, userName);
                                #endregion
                            }

                        }
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                _Error = ex.Message;
                _IsOK = false;

            }
        }

        /// <summary>
        /// Cập nhật trạng thái phòng của những phòng OOO, OOS
        /// -- CSS, 17/05/2011
        /// </summary>
        private void _UpdateRoomStatus_1(ref bool _IsOK, string userName)
        {
            try
            {
                _IsOK = true;

                //Update lại trạng thái OOO 
                string sql = "UPDATE [room] set [room].[HKStatusID] = (Select top 1 'status' = case  When a.OOOStatus = 1 then 5 when a.OOOStatus = 2 then 6 end  From BusinessBlock as a " +
                             "WHERE datediff(day, a.FromDateOOO,'" + TextUtils.GetBusinessDate().AddDays(1).ToString("yyyy/MM/dd") + "' )>=0 " +
                             "AND datediff(day, a.ToDateOOO,'" + TextUtils.GetBusinessDate().AddDays(1).ToString("yyyy/MM/dd") + "' )<0 " +
                             "AND a.RoomID= [room].[ID]  Order by a.ID ), " +
                             "RoomStatus = HKStatusID where ID in (Select RoomID From BusinessBlock where datediff(day, FromDateOOO, '" + TextUtils.GetBusinessDate().AddDays(1).ToString("yyyy/MM/dd") + "' )>=0 " +
                             "AND datediff(day, ToDateOOO,'" + TextUtils.GetBusinessDate().AddDays(1).ToString("yyyy/MM/dd") + "') < 0 AND RoomID>0)";
                SqlHelper.ExecuteNonQuery(DBUtils.GetDBConnectionString(), CommandType.Text, sql);

                //Lấy danh sách các phòng đến ngày hết hạn trạng thái OOO, OOS
                DataTable dtOOO = TextUtils.Select("Select * From BusinessBlock Where datediff(day,FromDateOOO,ToDateOOO)!=0 And OOOStatus in (1,2) AND DateDiff(day,ToDateOOO,'" + TextUtils.GetBusinessDate().AddDays(1).ToString("yyyy/MM/dd") + "')=0 ");
                if (dtOOO.Rows.Count > 0)
                {
                    for (int i = 0; i < dtOOO.Rows.Count; i++)
                    {
                        string RoomNo = dtOOO.Rows[i]["RoomNo"].ToString();
                        RoomModel md = (RoomModel)RoomBO.Instance.FindByPrimaryKey(TextUtils.ToInt(dtOOO.Rows[i]["RoomID"].ToString()));
                        if (md == null)
                        {
                            // (tùy chọn) log lại để kiểm tra dữ liệu
                            SaveLog($"Room not found. RoomNo = {RoomNo}");
                            continue;
                        }

                        RoomStatusHistoryBO.InsertHistory(md.RoomNo, md.HKStatusID.ToString(), dtOOO.Rows[i]["ReturnStatus"].ToString(), SystemDate, TextUtils.GetHostName(), "Night Audit", md.ID, "Room", "dad");

                        string sqlUpdate = "UPDATE Room SET HKStatusID = " + TextUtils.ToInt(dtOOO.Rows[i]["ReturnStatus"].ToString()) + ", RoomStatus = " + TextUtils.ToInt(dtOOO.Rows[i]["ReturnStatus"].ToString()) + "  WHERE ID = " + TextUtils.ToInt(dtOOO.Rows[i]["RoomID"].ToString()) + " ";
                        SqlHelper.ExecuteNonQuery(DBUtils.GetDBConnectionString(), CommandType.Text, sqlUpdate);
                    }
                }

                //Truong Hop Fromdate == Todate
                DataTable dtO = TextUtils.Select("Select * From BusinessBlock Where datediff(day,FromDateOOO,ToDateOOO)=0 And OOOStatus IN (1,2) AND DateDiff(day,ToDateOOO,'" + TextUtils.GetBusinessDate().ToString("yyyy/MM/dd") + "')=0 ");
                if (dtO.Rows.Count > 0)
                {
                    //Fromdate == Todate == BussinessDate
                    for (int i = 0; i < dtOOO.Rows.Count; i++)
                    {
                        string RoomNo = dtOOO.Rows[i]["RoomNo"].ToString();
                        RoomModel md = (RoomModel)RoomBO.Instance.FindByPrimaryKey(TextUtils.ToInt(dtOOO.Rows[i]["RoomID"].ToString()));
                        RoomStatusHistoryBO.InsertHistory(md.RoomNo, md.HKStatusID.ToString(), dtO.Rows[i]["ReturnStatus"].ToString(), SystemDate, TextUtils.GetHostName(), "Night Audit", md.ID, "Room", userName);

                        string sqlO = "UPDATE Room SET HKStatusID = " + TextUtils.ToInt(dtO.Rows[i]["ReturnStatus"].ToString()) + ", RoomStatus = " + TextUtils.ToInt(dtO.Rows[i]["ReturnStatus"].ToString()) + "  WHERE ID = " + TextUtils.ToInt(dtO.Rows[i]["RoomID"].ToString()) + " ";
                        SqlHelper.ExecuteNonQuery(DBUtils.GetDBConnectionString(), CommandType.Text, sqlO);
                    }
                }
            }
            catch (Exception ex)
            {
                _Error = ex.Message;
                _IsOK = false;
            }
        }

        /// <summary>
        /// Chuyển trạng thái phòng sang Dirty với phòng GIH
        /// -- CSS, 17/05/2011
        /// </summary>
        private void _ChangeRoomStatus_1(ref bool _IsOK)
        {
            try
            {
                _IsOK = true;
                //Detail: HKStatusID =1:Clean; HKStatusID=2:Dirty; HKStatusID =3: Pickup; HKStatusID = 4:Inspected 
                string sqlUpdate_R = "UPDATE Room SET HKStatusID = 2, RoomStatus = 2 WHERE FOStatus=1";//HKStatusID=1 AND FOStatus=1";
                SqlHelper.ExecuteNonQuery(DBUtils.GetDBConnectionString(), CommandType.Text, sqlUpdate_R);
                SaveLog("Change Room Status - OK");
            }
            catch (Exception ex)
            {
                _Error = ex.Message;
                _IsOK = false;
            }
        }

        /// <summary>
        /// Chuyển trạng thái phòng sang DUE IN đối với những phòng sẽ đến ngày hôm sau
        /// -- CSS, 17/05/2011
        /// </summary>
        private void _ChangeRsvStatus_1(ref bool _IsOK)
        {
            try
            {
                _IsOK = true;
                //Cap nhat trang thai cho khach DUEIN trong bang RESERVATION -- Status = 5 
                string sqlUpdate_DI = "UPDATE Reservation SET Status = 5 WHERE Status = 0 And DateDiff(day,ArrivalDate,'" + TextUtils.GetBusinessDate().AddDays(1).ToString("yyyy/MM/dd") + "')=0 ";
                SqlHelper.ExecuteNonQuery(DBUtils.GetDBConnectionString(), CommandType.Text, sqlUpdate_DI);
            }
            catch (Exception ex)
            {
                _Error = ex.Message;
                _IsOK = false;
            }
        }

        public void InsertActivityLog(string _tablename, int _ID, string _change, string _oldvalue, string _newvalue, string _description, int userID, string userName)
        {
            ActivityLogModel mAL = new ActivityLogModel();
            mAL.TableName = _tablename;
            mAL.ObjectID = _ID;
            mAL.UserID = userID;
            mAL.UserName = userName;
            mAL.ChangeDate = TextUtils.GetSystemDate();
            mAL.Change = _change;
            mAL.OldValue = _oldvalue;
            mAL.NewValue = _newvalue;
            mAL.Description = _description;
            ActivityLogBO.Instance.Insert(mAL);
        }
        /// <summary>
        /// Chuyển trạng thái phòng sang DUE OUT đối với những phòng sẽ đi vào ngày hôm sau
        /// -- CSS, 17/05/2011
        /// </summary>
        private void _ChangeRsvStatus_2(ref bool _IsOK, int userID, string userName)
        {
            try
            {
                _IsOK = true;
                DataTable _dtRsv = TextUtils.Select("Select ID From Reservation WITH (NOLOCK) Where Status = 1 And DateDiff(day,DepartureDate,'" + TextUtils.GetBusinessDate().AddDays(1).ToString("yyyy/MM/dd") + "')=0 ");
                if (_dtRsv.Rows.Count > 0)
                {
                    for (int i = 0; i < _dtRsv.Rows.Count; i++)
                    {
                        InsertActivityLog("Reservation", Convert.ToInt32(_dtRsv.Rows[i]["ID"]), "Status", "CHECKED IN", "DUE OUT", "", userID, userName);
                        TextUtils.ExcuteSQL("Update Reservation SET Status = 6 Where ID ='" + Convert.ToInt32(_dtRsv.Rows[i]["ID"]) + "'");
                    }

                }
            }
            catch (Exception ex)
            {
                _Error = ex.Message;
                _IsOK = false;
            }
        }
        /// <summary>
        /// Chuyển trạng thái phòng sang NOSHOW đối với những phòng đến ngày hnay nhưng không đến
        /// -- CSS, 17/05/2011
        /// </summary>
        private void _ChangeRsvStatus_3(ref bool _IsOK)
        {
            try
            {
                _IsOK = true;
                //Cap nhat trang thai phong cho khach NoShow -- Status = 7
                string sqlUpdate_NO =
                    "UPDATE Reservation " +
                    "SET Status = 7, NoShowStatus = 1 " +
                    "WHERE (Status = 0 OR Status = 5) " +
                    "AND CAST(ArrivalDate AS DATE) = '" +
                    TextUtils.GetBusinessDate().ToString("yyyy-MM-dd") + "'";

                SqlHelper.ExecuteNonQuery(DBUtils.GetDBConnectionString(), CommandType.Text, sqlUpdate_NO);
                //TextUtils.DailyCutOff();
            }
            catch (Exception ex)
            {
                _Error = ex.Message;
                _IsOK = false;
            }
        }
        /// <summary>
        /// Tính lại Balance cho Folio và Reservation
        /// -- CSS, 18/05/2011
        /// </summary>
        /// 

        private void _ChangeRsvStatus_4(ref bool _IsOK, string UserID)
        {
            try
            {
                _IsOK = true;

                DataTable dt = TextUtils.Select("Select ID From Room where RoomTypeID Not in (Select ID from RoomType where IsPseudo =1)");
                DataTable _dtR = null;
                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        _dtR = TextUtils.Select("SELECT Count(a.ID) AS ID " +
                                                "FROM ReservationRate a WITH (NOLOCK), Reservation b WITH (NOLOCK)" +
                                                "WHERE a.ReservationID = b.ID " +
                                                "AND a.RoomID > 0 " +
                                                $"AND a.RoomID = {TextUtils.ToInt(dt.Rows[i][0].ToString())}" +
                                                "AND ReservationNo > 0 " +
                                                $"AND DATEDIFF(DAY,RateDate,'{TextUtils.GetBusinessDate().ToString("yyyy/MM/dd")}') <= 0 " +
                                                "AND (b.Status = 1 OR b.Status = 6) ");
                        if (_dtR.Rows.Count > 0)
                        {
                            if (TextUtils.ToInt(_dtR.Rows[0][0].ToString()) == 0)
                            {
                                TextUtils.ExcuteSQL("UPDATE Room SET FOStatus = 0, HKFOStatus = 0, UserUpdateID = " + UserID + " WHERE ID = " + TextUtils.ToInt(dt.Rows[i][0].ToString()) + " ");
                                //MessageBox.Show("Changed FOStatus Ro.No. " + txtRoomNo.Text + " from occupied to vacant", TextUtils.Caption_Confirm);

                            }
                            else
                            {
                                TextUtils.ExcuteSQL("UPDATE Room SET FOStatus = 1, HKFOStatus = 1, UserUpdateID = " + UserID + " WHERE ID = " + TextUtils.ToInt(dt.Rows[i][0].ToString()) + " ");
                            }
                        }
                        else
                        {
                            TextUtils.ExcuteSQL("UPDATE Room SET FOStatus = 0, HKFOStatus = 0, UserUpdateID = " + UserID + " WHERE ID = " + TextUtils.ToInt(dt.Rows[i][0].ToString()) + " ");
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                _Error = ex.Message;
                _IsOK = false;
            }
        }
        /// <summary>
        /// Cap nhap so du hien thoi cua Folio
        /// </summary>
        /// <param name="FolioID">ID cua Folio</param>
        /// <param name="pt"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public bool UpdateBalanceCrahier(int _ReservationID, int _FolioID, ref string _Message)
        {
            try
            {
                TextUtils.ExcuteSQL("Update Folio set BalanceVND=dbo.getBalanceOfFolio(" + _FolioID + ",'" + _CURRENCY_1 + "')," +
                                "BalanceUSD=dbo.getBalanceOfFolio(" + _FolioID + ",'" + _CURRENCY_2 + "') Where ID=" + _FolioID);
                TextUtils.ExcuteSQL("Update Reservation set BalanceVND=dbo.getBalanceOfGih(" + _ReservationID + ",'" + _CURRENCY_1 + "')," +
                                "BalanceUSD=dbo.getBalanceOfGih(" + _ReservationID + ",'" + _CURRENCY_2 + "') Where ID=" + _ReservationID);
                return true;
            }
            catch (Exception ex)
            {
                _Message = ex.Message;
                return false;
            }
        }
        private void _FolioBalance(ref bool _IsOK)
        {
            try
            {
                _IsOK = true;
                DataTable dtRsv = TextUtils.Select("SELECT ID FROM Reservation WITH (NOLOCK) WHERE Status = 1");
                if (dtRsv.Rows.Count > 0)
                {
                    for (int i = 0; i < dtRsv.Rows.Count; i++)
                    {
                        DataTable dtFolio = TextUtils.Select("SELECT ID FROM Folio WITH (NOLOCK) WHERE Status = 0 AND ReservationID = " + int.Parse(dtRsv.Rows[i]["ID"].ToString()));
                        string str = "";
                        if (dtFolio.Rows.Count > 0)
                        {
                            for (int j = 0; j < dtFolio.Rows.Count; j++)
                            {
                                UpdateBalanceCrahier(int.Parse(dtRsv.Rows[i]["ID"].ToString()), int.Parse(dtFolio.Rows[j]["ID"].ToString()), ref str);
                            }
                        }
                    }
                    SaveLog("Cap nhat xong FolioBalance, ReservationBalance");
                }
            }
            catch (Exception ex)
            {
                _Error = ex.Message;
                _IsOK = false;
            }
        }

        /// <summary>
        /// Cập nhật trạng thái phòng Inspected --> Clean non-check
        /// -- CSS, 18/05/2011
        /// </summary>
        private void _ChangeRoomStatus_2(ref bool _IsOK, string userName)
        {
            try
            {
                _IsOK = true;
                /* Cập nhật phòng từ Clean(4) --> Clean None Check(1) (HKStatusID: 4 --> 1) */
                DataTable dtn = TextUtils.Select("Select a.ID, a.RoomNo FROM Room a WITH (NOLOCK), RoomType b WITH (NOLOCK) " +
                                                 "Where a.RoomTypeID = b.ID " +
                                                 "AND a.HKStatusID = 4 " +
                                                 "AND b.IsPseudo =0 ");
                if (dtn.Rows.Count > 0 && _HKP_VD_VC == 0) //Chỉ chuyển trạng thái cho những KS trong configsystem =0; =1 để nguyên
                {
                    for (int r = 0; r < dtn.Rows.Count; r++)
                    {
                        string RoomNo = dtn.Rows[r]["RoomNo"].ToString();
                        RoomModel md = (RoomModel)RoomBO.Instance.FindByPrimaryKey(TextUtils.ToInt(dtn.Rows[r]["ID"].ToString()));
                        RoomStatusHistoryBO.InsertHistory(md.RoomNo, md.HKStatusID.ToString(), "1", SystemDate, TextUtils.GetHostName(), "Night Audit", md.ID, "Room", userName);

                        TextUtils.ExcuteSQL("Update Room Set HKStatusID = 1 Where RoomNo ='" + md.RoomNo + "' ");// HKStatusID = 4 And RoomTypeCode <>'CONF' and RoomTypeCode <>'XXX' ");
                    }
                }
                SaveLog("Cap nhat xong trang thai cho cac phong VC --> VCN !");
            }
            catch (Exception ex)
            {
                _Error = ex.Message;
                _IsOK = false;
            }
        }
        /// <summary>
        /// Chuyển HKStatusID = DO đối với những phòng sẽ đi ngày hôm sau
        /// -- CSS, 18/05/2011
        /// </summary>
        private void _ChangeRoomStatus_3(ref bool _IsOK, string userName)
        {
            try
            {
                _IsOK = true;
                //DataTable dta = TextUtils.Select("Select MainGuest, RoomID, RoomNo " +
                //                                 "From Reservation WITH (NOLOCK) " +
                //                                 "Where Status = 6 And MainGuest = 1 And RoomNo > 0 AND PostingMaster = 0");

                DataTable dta = TextUtils.Select("Select MainGuest, RoomID, RoomNo " +
                                                "From Reservation WITH (NOLOCK) " +
                                                "Where Status = 6 And MainGuest = 1 And RoomNo <> '' AND PostingMaster = 0");
                if (dta.Rows.Count > 0)
                {
                    for (int r = 0; r < dta.Rows.Count; r++)
                    {
                        string RoomNo = dta.Rows[r]["RoomNo"].ToString();
                        RoomModel md = (RoomModel)RoomBO.Instance.FindByPrimaryKey(TextUtils.ToInt(dta.Rows[r]["RoomID"].ToString()));
                        RoomStatusHistoryBO.InsertHistory(md.RoomNo, md.HKStatusID.ToString(), "7", SystemDate, TextUtils.GetHostName(), "Night Audit", md.ID, "Room", userName);
                        TextUtils.ExcuteSQL("Update Room Set HKStatusID = 7 Where RoomNo ='" + md.RoomNo + "'");
                    }
                    SaveLog("Cap nhat xong trang thai phong DueOut!");
                }
            }
            catch (Exception ex)
            {
                _Error = ex.Message;
                _IsOK = false;
            }
        }

        /// <summary>
        /// Cập nhật thông tin giá, Person, Market, Source ... trong bảng Rsv từ bảng RsvRate
        /// -- CSS, 18/05/2011
        /// </summary>
        private void _UpdateReseration(ref bool _IsOK)
        {
            try
            {
                _IsOK = true;
                //Lấy ra danh sách phiếu đặt phòng cần cập nhật
                DataTable _dtRsv = TextUtils.Select("SELECT ID FROM Reservation WITH (NOLOCK)" +
                                                    "WHERE (Status = 1 OR Status =6) AND ReservationNo > 0 AND PostingMaster = 0 Order By RoomNo");
                if (_dtRsv.Rows.Count > 0)
                {
                    for (int i = 0; i < _dtRsv.Rows.Count; i++)
                    {
                        //thinh rao doan code nay
                        //Lấy thông tin trong bảng Rate
                        DataTable _dtRsv_D = TextUtils.Select("SELECT Rate, RateAfterTax, NoOfAdult, NoOfChild, NoOfChild1, NoOfChild2, " +
                                                              "RoomTypeID, RoomType, RoomID, RoomNo, MarketID, SourceID, " +
                                                              "DiscountRate, DiscountAmount " +
                                                              "FROM ReservationRate WITH (NOLOCK) " +
                                                              "WHERE ReservationID = " + int.Parse(_dtRsv.Rows[i]["ID"].ToString()) + " " +
                                                              "AND datediff(day, RateDate, '" + TextUtils.GetBusinessDate().AddDays(1).ToString("yyyy/MM/dd") + "')=0 ");

                        //thinh sua: lay MarketID cua bang Reservation
                        //Lấy thông tin trong bảng Rate
                        //DataTable _dtRsv_D = TextUtils.Select("SELECT a.Rate, a.RateAfterTax, a.NoOfAdult, a.NoOfChild, a.NoOfChild1, a.NoOfChild2, " +
                        //                                      "a.RoomTypeID, a.RoomType, a.RoomID, a.RoomNo, b.MarketID, a.SourceID, " +
                        //                                      "a.DiscountRate, a.DiscountAmount " +
                        //                                      "FROM ReservationRate a WITH (NOLOCK) join Reservation b on a.ReservationID = b.ID " +
                        //                                      "WHERE ReservationID = " + int.Parse(_dtRsv.Rows[i]["ID"].ToString()) + " " +
                        //                                      "AND datediff(day, RateDate, '" + TextUtils.GetBusinessDate().AddDays(1).ToString("yyyy/MM/dd") + "')=0 ");


                        if (_dtRsv_D.Rows.Count > 0)
                        {
                            string _Market = "";
                            string _Source = "";
                            if (int.Parse(_dtRsv_D.Rows[0]["MarketID"].ToString()) > 0)
                                _Market = ((MarketModel)MarketBO.Instance.FindByPrimaryKey(int.Parse(_dtRsv_D.Rows[0]["MarketID"].ToString()))).Code;
                            if (int.Parse(_dtRsv_D.Rows[0]["SourceID"].ToString()) > 0)
                                _Source = ((SourceModel)SourceBO.Instance.FindByPrimaryKey(int.Parse(_dtRsv_D.Rows[0]["SourceID"].ToString()))).Code;
                            //Update thông tin vào bảng Rsv
                            string sql_UD = "UPDATE Reservation SET " +
                                            "Rate = " + Convert.ToDecimal(_dtRsv_D.Rows[0]["Rate"]).ToString().Replace(",", ".") + " , " +
                                            "RateAfterTax = " + Convert.ToDecimal(_dtRsv_D.Rows[0]["RateAfterTax"]).ToString().Replace(",", ".") + ", " +
                                            "NoOfAdult = " + int.Parse(_dtRsv_D.Rows[0]["NoOfAdult"].ToString()) + ", " +
                                            "NoOfChild = " + int.Parse(_dtRsv_D.Rows[0]["NoOfChild"].ToString()) + ", " +
                                            "NoOfChild1 = " + int.Parse(_dtRsv_D.Rows[0]["NoOfChild1"].ToString()) + ", " +
                                            "NoOfChild2 = " + int.Parse(_dtRsv_D.Rows[0]["NoOfChild2"].ToString()) + ", " +
                                            "RoomTypeID = " + int.Parse(_dtRsv_D.Rows[0]["RoomTypeID"].ToString()) + ", " +
                                            "RoomType = '" + _dtRsv_D.Rows[0]["RoomType"].ToString() + "', " +
                                            "RoomID = " + _dtRsv_D.Rows[0]["RoomID"].ToString() + ", " +
                                            "RoomNo = '" + _dtRsv_D.Rows[0]["RoomNo"].ToString() + "', " +
                                            "MarketID = " + int.Parse(_dtRsv_D.Rows[0]["MarketID"].ToString()) + ", " +
                                            "MarketCode = '" + _Market + "', " +
                                            "SourceID = " + int.Parse(_dtRsv_D.Rows[0]["SourceID"].ToString()) + ", " +
                                            "SourceCode = '" + _Source + "', " +
                                            "DiscountRate = " + Convert.ToDecimal(_dtRsv_D.Rows[0]["DiscountRate"]).ToString().Replace(",", ".") + ", " +
                                            "DiscountAmount = " + Convert.ToDecimal(_dtRsv_D.Rows[0]["DiscountAmount"]).ToString().Replace(",", ".") + " " +
                                            "WHERE ID = " + TextUtils.ToInt(_dtRsv.Rows[i]["ID"].ToString()) + " ";
                            TextUtils.ExcuteSQL(sql_UD);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _Error = ex.Message;
                _IsOK = false;
            }
        }

        /// <summary>
        /// Tính lại return guest đối với khách đang In House
        /// -- CSS, 18/05/2011
        /// </summary>
        private void _ReturnGuest(ref bool _IsOK)
        {
            try
            {
                _IsOK = true;
                //Lấy ra danh sách phiếu đặt phòng cần cập nhật
                DataTable _dtRsv = TextUtils.Select("SELECT ProfileIndividualID FROM Reservation WITH (NOLOCK) " +
                                                    "WHERE (Status = 1 OR Status = 6) AND ReservationNo > 0 AND PostingMaster = 0 Order By RoomNo ");
                if (_dtRsv.Rows.Count > 0)
                {
                    for (int i = 0; i < _dtRsv.Rows.Count; i++)
                    {
                        DataTable _dtRG = TextUtils.Select("SELECT Count(ID) AS a " +
                                                           "FROM Reservation WITH (NOLOCK) " +
                                                           "WHERE ProfileIndividualID = " + TextUtils.ToInt(_dtRsv.Rows[i][0].ToString()) + "  " +
                                                           "AND ReservationNo > 0 AND Status IN (1,2,6) " +
                                                           "Order by Count(ID) DESC ");
                        if (_dtRG.Rows.Count > 0)
                        {
                            string sql_UD = "UPDATE Profile SET " +
                                             "ReturnGuest = " + (TextUtils.ToInt(_dtRG.Rows[0]["a"].ToString()) - 1) + ", " +
                                             "StayNo = " + TextUtils.ToInt(_dtRG.Rows[0]["a"].ToString()) + " " +
                                             "WHERE ID = " + TextUtils.ToInt(_dtRsv.Rows[i][0].ToString()) + " ";
                            TextUtils.ExcuteSQL(sql_UD);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _Error = ex.Message;
                _IsOK = false;
            }
        }
        private int GetAllBlocked(int _AllID, int _RTID, DateTime _date)
        {
            DataTable dt = TextUtils.Select("SELECT SUM(a.NoOfRoom) AS Rms " +
                                            "FROM dbo.Reservation a WITH (NOLOCK), dbo.ReservationRate b WITH (NOLOCK) " +
                                            "WHERE a.ID = b.ReservationID AND b.AllotmentID = " + _AllID + " AND b.RoomTypeID = " + _RTID + " " +
                                            "AND DATEDIFF(DAY,b.RateDate,'" + _date.ToString("yyyy/MM/dd") + "') = 0 " +
                                            "AND a.MainGuest = 1 AND a.ReservationNo > 0 AND a.Status NOT IN (3,4,7) ");
            if (dt.Rows.Count > 0)
                return TextUtils.ToInt(dt.Rows[0][0].ToString());
            else
                return 0;
        }
        public void CutOffDate(DateTime date, ref string _mess, string userName)
        {
            #region *.Constructor
            int _AllDetailID = 0;
            int _Qty = 0;
            int _RTID = 0;
            int _FromAllotmentID = 0;
            //int _ToAllotmentID = 0;
            DataTable dtAll = null;
            DateTime _SysDate = TextUtils.GetSystemDate();
            #endregion

            #region B1.Get all default
            //_ToAllotmentID = GetAllDefault();
            #endregion

            #region B2.Get list allotment to cutoff
            dtAll = TextUtils.Select("SELECT a.ID, a.AllotmentID, a.RoomTypeID, a.Quantity " +
                                     "FROM dbo.AllotmentDetail a WITH (NOLOCK), dbo.Allotment b  WITH (NOLOCK)  " +
                                     "WHERE a.AllotmentID = b.ID AND b.IsDefault = 0 AND a.CutOffDay = -1 " +
                                     "AND DATEDIFF(DAY,a.CutOffDate,'" + date.ToString("yyyy/MM/dd") + "') = 0 ");
            #endregion

            #region B3.Process
            if (dtAll.Rows.Count > 0)
            {
                ////if exits default to continue
                //if (_ToAllotmentID > 0)
                //{                 
                for (int i = 0; i < dtAll.Rows.Count; i++)
                {
                    #region 1.Get value
                    _AllDetailID = int.Parse(dtAll.Rows[i]["ID"].ToString());
                    _Qty = int.Parse(dtAll.Rows[i]["Quantity"].ToString());
                    _RTID = int.Parse(dtAll.Rows[i]["RoomTypeID"].ToString());
                    _FromAllotmentID = int.Parse(dtAll.Rows[i]["AllotmentID"].ToString());
                    #endregion

                    #region 2.Get Avail to transfer to default
                    int Posted = GetAllBlocked(_FromAllotmentID, _RTID, date);
                    #endregion

                    #region 3.Get quantity to transfer to default
                    _Qty = _Qty - Posted;
                    #endregion

                    if (_Qty > 0)
                    {
                        #region 1.Insert to table AllotmentTransfer
                        AllotmentTransferModel mATF = new AllotmentTransferModel();
                        mATF.FromAllotmentID = _FromAllotmentID;
                        mATF.ToAllotmentID = 0;// _ToAllotmentID;
                        mATF.CreateBy = mATF.UpdateBy = userName;
                        mATF.CreateDate = mATF.UpdateDate = _SysDate;
                        mATF.Description = "Cut of date: " + date.ToString("dd.MM.yyyy");
                        mATF.RoomTypeID = _RTID;
                        mATF.Quantity = _Qty;
                        mATF.FromDate = date;
                        mATF.ToDate = date;
                        //if (pt != null)
                        //    pt.Insert(mATF);
                        //else
                        AllotmentTransferBO.Instance.Insert(mATF);
                        #endregion

                        #region 2.Insert to table AllotmentDetail - To

                        #endregion

                        #region 3.Update AllotmentDetail - From
                        //if (pt != null)
                        //    pt.UpdateCommand("UPDATE dbo.AllotmentDetail SET Quantity = Quantity - " + _Qty + " WHERE ID = " + _AllDetailID + " ");
                        //else
                        TextUtils.ExcuteSQL("UPDATE dbo.AllotmentDetail SET Quantity = Quantity - " + _Qty + " WHERE ID = " + _AllDetailID + " ");
                        #endregion
                    }
                }

                #region **Delete db
                //if (pt != null)
                //    pt.UpdateCommand("DELETE dbo.AllotmentDetail WHERE Quantity = 0 ");
                //else
                TextUtils.ExcuteSQL("DELETE dbo.AllotmentDetail WHERE Quantity = 0 ");
                #endregion
                //}
                //else
                //    _mess = "Not exits allotment default to cutoff";
            }
            #endregion
        }
        #region
        public void CutOffDays(DateTime date, ref string _mess, string userName)
        {
            #region *.Constructor
            int _AllDetailID = 0;
            int _Qty = 0;
            int _RTID = 0;
            int _FromAllotmentID = 0;
            //int _ToAllotmentID = 0;
            DataTable dtAll = null;
            DateTime _SysDate = TextUtils.GetSystemDate();
            #endregion

            #region B1.Get all default
            //_ToAllotmentID = GetAllDefault();
            #endregion

            #region B2.Get list allotment to cutoff
            dtAll = TextUtils.Select("SELECT a.ID, a.AllotmentID, a.RoomTypeID, a.Quantity, a.AllotmentDate " +
                                     "FROM dbo.AllotmentDetail a WITH (NOLOCK), dbo.Allotment b  WITH (NOLOCK)  " +
                                     "WHERE a.AllotmentID = b.ID AND b.IsDefault = 0 AND a.CutOffDay <> -1 " +
                                     "AND DATEDIFF(day,'" + date.ToString("yyyy/MM/dd") + "', a.AllotmentDate) = a.CutOffDay ");
            #endregion

            #region B3.Process
            if (dtAll.Rows.Count > 0)
            {
                ////if exits default to continue
                //if (_ToAllotmentID > 0)
                //{
                for (int i = 0; i < dtAll.Rows.Count; i++)
                {
                    #region 1.Get value
                    _AllDetailID = int.Parse(dtAll.Rows[i]["ID"].ToString());
                    _Qty = int.Parse(dtAll.Rows[i]["Quantity"].ToString());
                    _RTID = int.Parse(dtAll.Rows[i]["RoomTypeID"].ToString());
                    _FromAllotmentID = int.Parse(dtAll.Rows[i]["AllotmentID"].ToString());
                    #endregion

                    #region 2.Get Avail to transfer to default
                    int Posted = GetAllBlocked(_FromAllotmentID, _RTID, Convert.ToDateTime(dtAll.Rows[i]["AllotmentDate"].ToString()));
                    #endregion

                    #region 3.Get quantity to transfer to default
                    _Qty = _Qty - Posted;
                    #endregion

                    if (_Qty > 0)
                    {
                        #region 1.Insert to table AllotmentTransfer
                        AllotmentTransferModel mATF = new AllotmentTransferModel();
                        mATF.FromAllotmentID = _FromAllotmentID;
                        mATF.ToAllotmentID = 0;//_ToAllotmentID;
                        mATF.CreateBy = mATF.UpdateBy = userName;
                        mATF.CreateDate = mATF.UpdateDate = _SysDate;
                        mATF.Description = "Cut of date: " + date.ToString("dd.MM.yyyy");
                        mATF.RoomTypeID = _RTID;
                        mATF.Quantity = _Qty;
                        mATF.FromDate = date;
                        mATF.ToDate = date;
                        //if (pt != null)
                        //    pt.Insert(mATF);
                        //else
                        AllotmentTransferBO.Instance.Insert(mATF);
                        #endregion

                        #region 2.Insert to table AllotmentDetail - To

                        #endregion

                        #region 3.Update AllotmentDetail - From
                        //if (pt != null)
                        //    pt.UpdateCommand("UPDATE dbo.AllotmentDetail SET Quantity = Quantity - " + _Qty + " WHERE ID = " + _AllDetailID + " ");
                        //else
                        TextUtils.ExcuteSQL("UPDATE dbo.AllotmentDetail SET Quantity = Quantity - " + _Qty + " WHERE ID = " + _AllDetailID + " ");
                        #endregion
                    }
                }

                //#region **Delete db
                //if (pt != null)
                //    pt.UpdateCommand("DELETE dbo.AllotmentDetail WHERE Quantity = 0 ");
                //else
                TextUtils.ExcuteSQL("DELETE dbo.AllotmentDetail WHERE Quantity = 0 ");
                #endregion
                //}
                //else
                //    _mess = "Not exits allotment default to cutoff";
            }
            #endregion
        }
        private void _CutoffAllotment(ref bool _IsOK, string userName)
        {
            try
            {
                _IsOK = true;
                string s = "";
                //Cut off Date
                CutOffDate(TextUtils.GetBusinessDate(), ref s, userName);
                //Cut off Day
                CutOffDays(TextUtils.GetBusinessDate(), ref s, userName);
            }
            catch (Exception ex)
            {
                _Error = ex.Message;
                _IsOK = false;
            }
        }

        private void _ChangeBusinessDate(ref bool _IsOK)
        {
            try
            {
                _IsOK = true;

                //if (MessageBox.Show("Are you sure to next date?", SQLCommands.Caption, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                //{
                string sqlUpdate = "UPDATE BusinessDate SET BusinessDate = '" + TextUtils.GetBusinessDate().AddDays(1).ToString("yyyy/MM/dd") + "'";
                SqlHelper.ExecuteNonQuery(DBUtils.GetDBConnectionString(), CommandType.Text, sqlUpdate);
                //}
                SaveLog("Change BusinessDate - OK");

            }
            catch (Exception ex)
            {
                _Error = ex.Message;
                _IsOK = false;
            }

            // Tự động In các báo cáo sau khi chạy Night Audit
            try
            {
                DataTable dtGetReport = new DataTable();
                dtGetReport = TextUtils.Select(@"select * from ReportAuto with (nolock) where [Status]=1");
                if (dtGetReport.Rows.Count > 0 && dtGetReport != null)
                {

                    for (int i = 0; i < dtGetReport.Rows.Count; i++)
                    {
                        string ReportName = "";
                        string SP = "";
                        DataTable Source = null;
                        TimeSpan aInterval = new System.TimeSpan(1, 0, 0, 0);
                        ReportName = dtGetReport.Rows[i]["Name"].ToString().Trim();
                        SP = dtGetReport.Rows[i]["StoreProc"].ToString().Trim();
                        if (ReportName != "")
                        {
                            #region GetDataSource
                            DataTable dtGetNumberPar = new DataTable();
                            dtGetNumberPar = TextUtils.Select(string.Format(@"select  
                                                                           'Parameter_name' = name,  
                                                                           'Type'   = type_name(user_type_id),  
                                                                           'Length'   = max_length,  
                                                                           'Prec'   = case when type_name(system_type_id) = 'uniqueidentifier' 
                                                                           then precision  
                                                                           else OdbcPrec(system_type_id, max_length, precision) end,  
                                                                           'Scale'   = OdbcScale(system_type_id, scale),  
                                                                           'Param_order'  = parameter_id,  
                                                                           'Collation'   = convert(sysname, 
                                                                            case when system_type_id in (35, 99, 167, 175, 231, 239)  
                                                                            then ServerProperty('collation') end)  
                                                                            from sys.parameters where object_id = object_id('{0}')", SP));
                            if (dtGetNumberPar.Rows.Count > 0 && dtGetNumberPar != null)
                            {
                                string[] paramName = new string[TextUtils.ToInt(dtGetNumberPar.Rows.Count.ToString())];
                                object[] paramValue = new object[TextUtils.ToInt(dtGetNumberPar.Rows.Count.ToString())];
                                for (int j = 0; j < dtGetNumberPar.Rows.Count; j++)
                                {
                                    if (dtGetNumberPar.Rows[j]["Type"].ToString().Trim() == "varchar")
                                    {
                                        paramName[j] = dtGetNumberPar.Rows[j]["Parameter_name"].ToString().Trim();
                                        paramValue[j] = "";
                                    }
                                    else if (dtGetNumberPar.Rows[j]["Type"].ToString().Trim() == "nvarchar")
                                    {
                                        paramName[j] = dtGetNumberPar.Rows[j]["Parameter_name"].ToString().Trim();
                                        paramValue[j] = "";
                                    }
                                    else if (dtGetNumberPar.Rows[j]["Type"].ToString().Trim() == "int")
                                    {
                                        paramName[j] = dtGetNumberPar.Rows[j]["Parameter_name"].ToString().Trim();
                                        paramValue[j] = 0;
                                    }
                                    else if (dtGetNumberPar.Rows[j]["Type"].ToString().Trim() == "datetime")
                                    {
                                        paramName[j] = dtGetNumberPar.Rows[j]["Parameter_name"].ToString().Trim();
                                        paramValue[j] = TextUtils.GetBusinessDate().Subtract(aInterval);
                                    }

                                    else if (dtGetNumberPar.Rows[j]["Type"].ToString().Trim() == "decimal")
                                    {
                                        paramName[j] = dtGetNumberPar.Rows[j]["Parameter_name"].ToString().Trim();
                                        paramValue[j] = 0;
                                    }
                                }
                                //Source = ProfileBO.Instance.LoadDataFromSP(SP, "tbSource", paramName, paramValue);
                            }
                            #endregion


                        }

                    }


                }
            }
            catch (Exception ex)
            {
                _Error = ex.Message;
            }

        }

        /// <summary>
        /// Insert tỷ giá ngày tiếp theo
        /// -- CSS, 18/05/2011
        /// </summary>
        private void _InsertExchangeRate(ref bool _IsOK, int userID)
        {
            try
            {
                _IsOK = true;


                DateTime _BusDate = ((BusinessDateModel)BusinessDateBO.Instance.FindAll()[0]).BusinessDate.AddDays(-1);
                DataTable dtExRate = TextUtils.Select("Select ID From dbo.ExchangeRate with (nolock) WHERE DATEDIFF(DAY,[DateTime],'" + _BusDate.ToString("yyyy/MM/dd") + "')=0 ");
                if (dtExRate != null)
                {
                    for (int ex_r = 0; ex_r < dtExRate.Rows.Count; ex_r++)
                    {
                        int _ExchID = TextUtils.ToInt((string)dtExRate.Rows[ex_r]["ID"]);
                        ExchangeRateModel model = (ExchangeRateModel)ExchangeRateBO.Instance.FindByPrimaryKey(_ExchID);
                        if (model != null)
                        {
                            model.DateTime = _BusDate.AddDays(1);
                            model.CreateDate = TextUtils.GetSystemDate();
                            model.UpdateDate = model.CreateDate;
                            model.UserInsertID = model.UserUpdateID = userID;
                            DataTable dt_check = TextUtils.Select("Select ID From dbo.ExchangeRate with (nolock) WHERE DATEDIFF(DAY,[DateTime],'" + _BusDate.AddDays(1).ToString("yyyy/MM/dd") + "')=0 " +
                                                                  "AND FromCurrencyID= '" + model.FromCurrencyID + "' AND ToCurrencyID = '" + model.ToCurrencyID + "' ");
                            if (dt_check != null)
                            {
                                if (dt_check.Rows.Count == 0)
                                    ExchangeRateBO.Instance.Insert(model);
                            }
                        }
                    }
                }

                SaveLog("Insert Exchange Rate - OK");

            }
            catch (Exception ex)
            {
                _Error = ex.Message;
                _IsOK = false;
            }
        }

        [HttpPost]
        public ActionResult EndNightAudit()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt = new ProcessTransactions();
                pt.OpenConnection();
                pt.BeginTransaction();

                _EndNightAudit(ref _IsOK);


                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Check out was successfully" });
            }
            catch (Exception ex)
            {
                pt.RollBack();
                return Json(new { code = 1, msg = ex.Message });
            }
            finally
            {
                pt.CloseConnection();
            }
        }
        #region run night audit
        [HttpPost]
        public ActionResult RunNightAuditProcess()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                string userName = Request.Form["userName"].ToString();
                string userID = Request.Form["userID"].ToString();
                string computerName = Dns.GetHostName();

                #region  khai báo các biến chung
                _Error = "";
                _IndexRunning = 0;
                _IsRunning = true;
                dt_RoomType = TextUtils.Select("Select * From RoomType with (nolock)");
                //Xác định danh sách phòng để post tiền

                #endregion

                #region B1. System checking.
                //Cập nhật trạng thái đang chạy Night Audit
                _StartNightAudit(ref _IsOK);
                if (!_IsOK)
                {
                    _EndNightAudit(ref _IsOK);
                    return Json(new { code = 1, msg = "Loi System Checking 1" });

                }

                // Insert vào log của người chạy
                _NightAuditHistory(ref _IsOK, userName, userID, computerName);
                if (!_IsOK)
                {
                    _EndNightAudit(ref _IsOK);
                    return Json(new { code = 1, msg = "Loi System Checking 2" });
                }

                #endregion

                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Check out was successfully" });
            }
            catch (Exception ex)
            {
                pt.RollBack();
                return Json(new { code = 1, msg = ex.Message });
            }
            finally
            {
                pt.CloseConnection();
            }
        }

        [HttpPost]
        public ActionResult CheckInNotCI()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();


                #region  khai báo các biến chung
                _Error = "";
                _IndexRunning = 0;
                _IsRunning = true;
                #endregion


                #region B2. Check in not CI.

                // Thực hiện
                bool _IsNotCI = false;
                DataTable dataNotCheckIn = null;
                _CheckGuestCI(ref _IsNotCI, ref dataNotCheckIn);
                if (_IsNotCI == true)
                {
                    _Error = "";
                    _EndNightAudit(ref _IsOK);
                    var dateColumns = new HashSet<string> { "ArrivalDate", "DepartureDate" }; // liệt kê các cột ngày cần format

                    var data = dataNotCheckIn.AsEnumerable()
                        .Select(row => row.Table.Columns.Cast<DataColumn>()
                            .ToDictionary(
                                col => col.ColumnName,
                                col =>
                                {
                                    var value = row[col];
                                    if (value is DBNull || value == null)
                                        return null;

                                    // Nếu là cột ngày và là kiểu DateTime
                                    if (dateColumns.Contains(col.ColumnName) && value is DateTime dt)
                                    {
                                        return dt.ToString("dd/MM/yyyy");
                                    }

                                    return value.ToString();
                                }
                            ))
                        .ToList();
                    return Json(new { code = 1, msg = "loi check in not CI", data = data });
                }

                #endregion

                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Check out was successfully" });
            }
            catch (Exception ex)
            {
                pt.RollBack();
                return Json(new { code = 1, msg = ex.Message });
            }
            finally
            {
                pt.CloseConnection();
            }
        }

        [HttpPost]
        public ActionResult CheckOutNotCO()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();


                #region  khai báo các biến chung
                _Error = "";
                _IndexRunning = 0;
                _IsRunning = true;
                #endregion




                #region B3. Check out not CO. 
                // Thực hiện
                bool _IsNotCO = false;
                DataTable dataNotCheckIn = null;
                _CheckGuestCO(ref _IsNotCO, ref dataNotCheckIn);
                if (_IsNotCO == true)
                {
                    _Error = "";
                    _EndNightAudit(ref _IsOK);
                    var dateColumns = new HashSet<string> { "ArrivalDate", "DepartureDate" }; // liệt kê các cột ngày cần format

                    var data = dataNotCheckIn.AsEnumerable()
                        .Select(row => row.Table.Columns.Cast<DataColumn>()
                            .ToDictionary(
                                col => col.ColumnName,
                                col =>
                                {
                                    var value = row[col];
                                    if (value is DBNull || value == null)
                                        return null;

                                    // Nếu là cột ngày và là kiểu DateTime
                                    if (dateColumns.Contains(col.ColumnName) && value is DateTime dt)
                                    {
                                        return dt.ToString("dd/MM/yyyy");
                                    }

                                    return value.ToString();
                                }
                            ))
                        .ToList();
                    return Json(new { code = 1, msg = "Loi check out not C0", data = data });
                }
                #endregion


                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Check out was successfully" });
            }
            catch (Exception ex)
            {
                pt.RollBack();
                return Json(new { code = 1, msg = ex.Message });
            }
            finally
            {
                pt.CloseConnection();
            }
        }

        [HttpPost]
        public ActionResult Preprocess()
        {


            try
            {

                #region  khai báo các biến chung
                _Error = "";
                _IndexRunning = 0;
                _IsRunning = true;
                #endregion


                #region B4. Preprocess.
                // Backup dữ liệu trước khi chạy NightAudit
                _BackupData_1(ref _IsOK);
                if (!_IsOK)
                {
                    _EndNightAudit(ref _IsOK);
                    return Json(new { code = 1, msg = "Loi preprocess 1" });
                }
                //Xóa dữ liệu chạy night trước nếu có
                _DeleteFolioDetail(ref _IsOK);
                if (!_IsOK)
                {
                    _EndNightAudit(ref _IsOK);
                    return Json(new { code = 1, msg = "Loi preprocess 2" });
                }
                //Xóa dữ liệu temp trong đặt phòng
                _DeleteRsvTemp(ref _IsOK);
                if (!_IsOK)
                {
                    _EndNightAudit(ref _IsOK);
                    return Json(new { code = 1, msg = "Loi preprocess 3" });
                }

                #endregion


                return Json(new { code = 0, msg = "Check out was successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { code = 1, msg = ex.Message });
            }
            finally
            {
            }
        }

        [HttpPost]
        public ActionResult CloseShift()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();



                #region  khai báo các biến chung
                _Error = "";
                _IndexRunning = 0;
                _IsRunning = true;
                #endregion

                #region B5. Close cashier.

                // Thực hiện
                _CloseShift(ref _IsOK);
                if (!_IsOK)
                {
                    _EndNightAudit(ref _IsOK);
                    return Json(new { code = 1, msg = "Loi close cashier" });
                }
                // Kết thúc.

                #endregion



                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Check out was successfully" });
            }
            catch (Exception ex)
            {
                pt.RollBack();
                return Json(new { code = 1, msg = ex.Message });
            }
            finally
            {
                pt.CloseConnection();
            }
        }

        [HttpPost]
        public ActionResult PostingRoomCharge()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                string userName = Request.Form["userName"].ToString();
                string userID = Request.Form["userID"].ToString();
                string computerName = Dns.GetHostName();

                #region  khai báo các biến chung
                _Error = "";
                _IndexRunning = 0;
                _IsRunning = true;
                //Xác định danh sách phòng để post tiền
                DataTable _dtR = null;

                #endregion






                #region B6. Posting Room Charge.
                // Thực hiện
                _PostRoomCharge(ref _IsOK, ref _dtR, int.Parse(userID), userName);
                if (!_IsOK)
                {
                    _EndNightAudit(ref _IsOK);
                    return Json(new { code = 1, msg = "Loi posting room charge" });
                }
                #endregion

                #region B7. Posting FixedCharge.
                // Thực hiện
                _PostFixedCharge(ref _IsOK, _dtR, int.Parse(userID), userName);
                if (!_IsOK)
                {
                    _EndNightAudit(ref _IsOK);
                    return Json(new { code = 1, msg = "Loi posint fixed charge" });
                }


                #endregion

                #region B8. Posting Package.
                // Thực hiện
                _PostPackage(ref _IsOK, _dtR, int.Parse(userID), userName);
                if (!_IsOK)
                {
                    _EndNightAudit(ref _IsOK);
                    return Json(new { code = 1, msg = "Loi posting package" });
                }


                #endregion

                #region B8.1 Posting hoa hong.

                ////// Bắt đầu
                ////SetStep_8("", 0, 1000);
                ////// Thực hiện
                ////_PostPackage(ref _IsOK, _dtR);
                ////if (!_IsOK) { _EndNightAudit(ref _IsOK); SetStep_8(_Error, 3, 500); return; }
                ////// Kết thúc.
                ////SetStep_8("", 2, 1000);


                DataTable dt = TextUtils.Select("SELECT a.ID, ProfileIndividualID, LastName, a.ConfirmationNo, a.RateCode, " +
                                                 "ArrivalDate, DepartureDate, RoomTypeID, RoomType, RoomID, RoomNo, a.DiscountRate, a.DiscountAmount " +
                                                 "FROM Reservation a WITH (NOLOCK), RoomType b WITH (NOLOCK), ReservationCommission c WITH (NOLOCK) " +
                                                 "WHERE a.RoomTypeID = b.ID " +
                                                 "AND a.ID = c.ReservationID " +
                                                 "AND a.Status IN (1,6) AND a.ReservationNo > 0 AND b.IsPseudo = 0 " +
                                                 //"and a.ID in (273324,276010,276007,276011,276013,276014,276008) "+
                                                 "Order By RoomNo ");
                bool _OK = true;
                _PostCommission(ref _OK, dt, int.Parse(userID), userName);

                if (!_IsOK)
                {
                    _EndNightAudit(ref _IsOK);
                    return Json(new { code = 0, msg = "Loi posting hoa hong" });
                }

                #endregion
                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Check out was successfully" });
            }
            catch (Exception ex)
            {
                pt.RollBack();
                return Json(new { code = 1, msg = ex.Message });
            }
            finally
            {
                pt.CloseConnection();
            }
        }



        [HttpPost]
        public ActionResult ChangeRoomStatus()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                string userName = Request.Form["userName"].ToString();
                string userID = Request.Form["userID"].ToString();
                string computerName = Dns.GetHostName();

                #region  khai báo các biến chung
                _Error = "";
                _IndexRunning = 0;
                _IsRunning = true;
                dt_RoomType = TextUtils.Select("Select * From RoomType with (nolock)");
                //Xác định danh sách phòng để post tiền
                #endregion



                #region B9. Change room status.
                // B9.1. Cập nhật trạng thái phòng của những phòng OOO, OOS
                _UpdateRoomStatus_1(ref _IsOK, userName);
                if (!_IsOK)
                {
                    _EndNightAudit(ref _IsOK);
                    return Json(new { code = 1, msg = "Loi Posting chnage room status 1" });
                }


                //B9.2. Chuyển trạng thái phòng sang trạng thái dirty đối với những phòng đang Occ
                _ChangeRoomStatus_1(ref _IsOK);
                if (!_IsOK)
                {
                    _EndNightAudit(ref _IsOK);
                    return Json(new { code = 1, msg = "Loi Posting chnage room status 2" });
                }

                // B9.3. Chuyển trạng thái phiếu đặt phòng về DI đối với phòng sẽ đến ngày hôm sau
                _ChangeRsvStatus_1(ref _IsOK);
                if (!_IsOK)
                {
                    _EndNightAudit(ref _IsOK);
                    return Json(new { code = 1, msg = "Loi Posting chnage room status 2" });
                }
                pt.CloseConnection();

                pt.OpenConnection();
                pt.BeginTransaction();
                // B9.4. Chuyển trạng thái phiếu đặt phòng về DO đối với phòng sẽ đi ngày hôm sau
                _ChangeRsvStatus_2(ref _IsOK, int.Parse(userID), userName);
                if (!_IsOK)
                {
                    _EndNightAudit(ref _IsOK);
                    return Json(new { code = 1, msg = "Loi Posting chnage room status 4" });
                }

                pt.CloseConnection();

                // B9.5. Chuyển trạng thái phiếu đặt phòng về NS đối với phòng đến ngày hnay nhưng không đến
                pt.OpenConnection();
                pt.BeginTransaction();
                _ChangeRsvStatus_3(ref _IsOK);
                if (!_IsOK)
                {
                    _EndNightAudit(ref _IsOK);
                    return Json(new { code = 1, msg = "Loi Posting chnage room status 5" });
                }
                pt.CloseConnection();
                pt.OpenConnection();
                pt.BeginTransaction();
                // B9.6. Xử lý xung đột trạng thái phòng - 27.09.2018
                _ChangeRsvStatus_4(ref _IsOK, userID);
                if (!_IsOK)
                {
                    _EndNightAudit(ref _IsOK);
                    return Json(new { code = 1, msg = "Loi Posting chnage room status 6" });
                }


                #endregion


                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Check out was successfully" });
            }
            catch (Exception ex)
            {
                pt.RollBack();
                return Json(new { code = 1, msg = ex.Message });
            }
            finally
            {
                pt.CloseConnection();
            }
        }

        [HttpPost]
        public ActionResult UpdateInformationSystem()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                string userName = Request.Form["userName"].ToString();
                string userID = Request.Form["userID"].ToString();
                string computerName = Dns.GetHostName();

                #region  khai báo các biến chung
                _Error = "";
                _IndexRunning = 0;
                _IsRunning = true;
                dt_RoomType = TextUtils.Select("Select * From RoomType with (nolock)");
                //Xác định danh sách phòng để post tiền
                #endregion



                #region B10.Update information system.

                // B10.1. Tính lại balance
                _FolioBalance(ref _IsOK);
                if (!_IsOK)
                {
                    _EndNightAudit(ref _IsOK);
                    return Json(new { code = 1, msg = "Loi update infomation system 1" });
                }

                // B10.3. Chuyển trạng thái những phòng sạch sang trạng thái Clean non-check
                _ChangeRoomStatus_2(ref _IsOK, userName);
                if (!_IsOK)
                {
                    _EndNightAudit(ref _IsOK);
                    return Json(new { code = 1, msg = "Loi update infomation system 2" });
                }

                // B10.4. Chuyển trạng thái phòng sang DO đối với những phòng sẽ đi vào ngày hôm sau
                _ChangeRoomStatus_3(ref _IsOK, userName);
                if (!_IsOK)
                {
                    _EndNightAudit(ref _IsOK);
                    return Json(new { code = 1, msg = "Loi update infomation system 3" });
                }


                // B10.5. Chuyển thông tin chi tiết ngày hôm sau lên bảng Reservation
                _UpdateReseration(ref _IsOK);
                if (!_IsOK)
                {
                    _EndNightAudit(ref _IsOK);
                    return Json(new { code = 1, msg = "Loi update infomation system 4" });
                }

                // B10.6. Tính lại Return Guest
                _ReturnGuest(ref _IsOK);
                if (!_IsOK)
                {
                    _EndNightAudit(ref _IsOK);
                    return Json(new { code = 1, msg = "Loi update infomation system 5" });

                }

                // B10.7. CutOff Allotment
                _CutoffAllotment(ref _IsOK, userName);
                if (!_IsOK)
                {
                    _EndNightAudit(ref _IsOK);
                    return Json(new { code = 1, msg = "Loi update infomation system 6" });
                }

                #endregion

                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Check out was successfully" });
            }
            catch (Exception ex)
            {
                pt.RollBack();
                return Json(new { code = 1, msg = ex.Message });
            }
            finally
            {
                pt.CloseConnection();
            }
        }

        public ActionResult ChangeBusinessDate()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                string userName = Request.Form["userName"].ToString();
                string userID = Request.Form["userID"].ToString();
                string computerName = Dns.GetHostName();

                #region  khai báo các biến chung
                _Error = "";
                _IndexRunning = 0;
                _IsRunning = true;
                dt_RoomType = TextUtils.Select("Select * From RoomType with (nolock)");
                //Xác định danh sách phòng để post tiền
                #endregion

                #region cập nhật booking có departure = business date về due out
                DateTime time = ((BusinessDateModel)BusinessDateBO.Instance.FindAll()[0]).BusinessDate
                              .AddDays(1);

                List<ReservationModel> reservationModels = ReservationBO.GetReservationByBusinessDate(time.ToString("yyyy-MM-dd"));

                if (reservationModels.Count > 0)
                {
                    foreach (var item in reservationModels)
                    {
                        item.Status = 6; // due out
                        ReservationBO.Instance.Update(item);
                    }
                }

                #endregion

                #region B11.Change business date
                // Thực hiện
                _ChangeBusinessDate(ref _IsOK);
                if (!_IsOK)
                {
                    _EndNightAudit(ref _IsOK);
                    return Json(new { code = 1, msg = "loi chnage business date" });
                }
                // Kết thúc.
                #endregion


                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Check out was successfully" });
            }
            catch (Exception ex)
            {
                pt.RollBack();
                return Json(new { code = 1, msg = ex.Message });
            }
            finally
            {
                pt.CloseConnection();
            }
        }

        [HttpPost]
        public ActionResult RecheckingSystem()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                string userName = Request.Form["userName"].ToString();
                string userID = Request.Form["userID"].ToString();
                string computerName = Dns.GetHostName();

                #region  khai báo các biến chung
                _Error = "";
                _IndexRunning = 0;
                _IsRunning = true;
                dt_RoomType = TextUtils.Select("Select * From RoomType with (nolock)");
                //Xác định danh sách phòng để post tiền
                #endregion



                #region B12.Rechecking system.
                // B12.1. Insert tỷ giá cho ngày tiếp theo
                _InsertExchangeRate(ref _IsOK, int.Parse(userID));
                if (!_IsOK)
                {
                    _EndNightAudit(ref _IsOK);
                    return Json(new { code = 1, msg = "Loi final" });
                }

                // B12.2. Update lại trạng thái chạy Night Audit khi đã chạy xong
                _EndNightAudit(ref _IsOK);

                // B12.3. Backup dữ liệu sau khi chạy NightAudit
                _BackupData_1(ref _IsOK);


                // B12.4. Save log
                SaveLog("===================Ket thuc chay Nightaudit ========================!");


                //B12.5. Kết thúc chạy

                pt.ExcuteSQL("Update NightAuditTaskList Set FinishDate = '" + pt.GetBusinessDateTime().AddDays(-1).ToString("yyyy/MM/dd HH:mm:ss") + "'");
                //Cập nhật lại ngày Bussiness Date
                time = ((BusinessDateModel)BusinessDateBO.Instance.FindAll()[0]).BusinessDate.ToString();



                //Thông báo
                _IsRunning = false;

                #endregion
                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Run Night Audit was successfully" });
            }
            catch (Exception ex)
            {
                pt.RollBack();
                return Json(new { code = 1, msg = ex.Message });
            }
            finally
            {
                pt.CloseConnection();
            }
        }
        #endregion

        #region Tuan_NA 2/2026
        [HttpGet]
        public IActionResult SearchRoomRate(DateTime? date, bool warning, bool reservation, bool checkIN, bool checkOut, bool dueIn, bool dueOut, bool cancel)
        {
            try
            {
                DateTime businessDate = TextUtils.GetBussinessDateTime();
                // nếu không truyền date -> lấy businessDate
                DateTime finalDate = date ?? businessDate;

                DataTable table = _iRoomRateService.SearchRoomRate(finalDate, warning, reservation, checkIN, checkOut, dueIn, dueOut, cancel);
                var result = (from d in table.AsEnumerable()
                              select d.Table.Columns.Cast<DataColumn>().ToDictionary(
                              col => col.ColumnName,
                              col =>
                              {
                                  var value = d[col.ColumnName];
                                  if (value == DBNull.Value) return null;

                                  // CreatedDate: KHÔNG ToString
                                  if (col.ColumnName == "ArrivalDate" || col.ColumnName == "DepartureDate")
                                      return value;

                                  // Các field khác: ToString
                                  return value.ToString();
                              }
                          )).ToList();
                return Json(result);

            }
            catch (Exception ex)
            {

                return Json(ex.Message);
            }
        }
        #endregion
    }
}
