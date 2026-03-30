using BaseBusiness.bc;
using BaseBusiness.Facade;
using BaseBusiness.Model;
using BaseBusiness.util;
using Microsoft.Data.SqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.BO
{
    public class ReservationBO : BaseBO
    {
        private ReservationFacade facade = ReservationFacade.Instance;
        protected static ReservationBO instance = new ReservationBO();

        protected ReservationBO()
        {
            this.baseFacade = facade;
        }

        public static ReservationBO Instance
        {
            get { return instance; }
        }
        public static int GetTopConfirmationNo()
        {
            string query = "select max(cast(ConfirmationNo as int)) as ConfirmationNo from Reservation where ConfirmationNo <> '' ";
            return instance.GetFirst<int>(query);
        }
        public static int GetTopID()
        {
            string query = "select top 1 ID from Reservation order by id desc";
            return instance.GetFirst<int>(query);
        }
        public static List<ReservationModel> GetList(DateTime toDate, DateTime fromDate)
        {
            // Định dạng ngày thành YYYY-MM-DD
            string toDateStr = toDate.ToString("yyyy-MM-dd");
            string fromDateStr = fromDate.ToString("yyyy-MM-dd");

            string query = $"SELECT * FROM Reservation WHERE CAST(ArrivalDate AS DATE) >= CAST('{toDateStr}' AS DATE) AND CAST(DepartureDate AS DATE) <= CAST('{fromDateStr}' AS DATE) ORDER BY id DESC";
            return instance.GetList<ReservationModel>(query);
        }

        public static List<ProfileModel> GetProfileIndividual()
        {
            string query = $"select * from Profile where Type = 0 order by id desc ";
            return instance.GetList<ProfileModel>(query);
        }
        public static List<ReservationModel> GetReservationMaster(int reservationID)
        {
            string query = $"select * from Reservation where Relationship = {reservationID} and ReservationNo = 0";
            return instance.GetList<ReservationModel>(query);
        }
        public static List<ReservationModel> GetReservationByConfirmationNo(string confirmationNo)
        {
            string query = $"select * from Reservation where ConfirmationNo = '{confirmationNo}'";
            return instance.GetList<ReservationModel>(query);
        }
        public static List<ReservationModel> GetReservationRoomSharer(int reservationID)
        {
            string query = $"select * from Reservation where ShareRoom = {reservationID} and MainGuest = 0";
            return instance.GetList<ReservationModel>(query);
        }
        public static List<ReservationModel> GetBalanceVND(int reservationID)
        {
            string query = $"SELECT * FROM Reservation WITH (NOLOCK) WHERE ID = {reservationID}";

            return instance.GetList<ReservationModel>(query);
        }

        public static List<object> GetRoomTypeAvailable(DateTime fromDate, DateTime toDate)
        {
            string query = $"-- Declare date variables with original format, using CONVERT for proper parsing\r\nDECLARE @fromDate DATETIME = '{fromDate:yyyy-MM-dd HH:mm:ss}', -- 103 = DD/MM/YYYY\r\n        @toDate   DATETIME ='{toDate:yyyy-MM-dd HH:mm:ss}';\r\n\r\nSET NOCOUNT ON;\r\n\r\n-- Swap dates if fromDate is later than toDate\r\nDECLARE @tempDate DATETIME;\r\nIF @fromDate > @toDate\r\nBEGIN\r\n    SET @tempDate = @fromDate;\r\n    SET @fromDate = @toDate;\r\n    SET @toDate = @tempDate;\r\nEND;\r\n\r\n-- B1: Create date list\r\nDECLARE @dateList TABLE ([Date] DATE);\r\nDECLARE @d DATE = @fromDate;\r\nWHILE @d <= @toDate\r\nBEGIN\r\n    INSERT INTO @dateList VALUES (@d);\r\n    SET @d = DATEADD(DAY, 1, @d);\r\nEND;\r\n\r\n-- B2: Calculate RoomType x Date availability\r\n;WITH RoomAvailability AS (\r\n    SELECT \r\n        rt.ID AS RoomTypeID,\r\n        rt.Code AS RoomType,\r\n        d.[Date],\r\n        AvailableRooms = \r\n            ISNULL(inv.TotalRoom, 0)\r\n            - ISNULL(ar.AssignedRooms, 0)\r\n            - ISNULL(ur.UnassignedRooms, 0)\r\n            - ISNULL(ooo.OOORooms, 0)\r\n    FROM @dateList d\r\n    CROSS JOIN RoomType rt\r\n    LEFT JOIN (\r\n        SELECT RoomTypeID, [Date], TotalRoom\r\n        FROM RoomInventory WITH (NOLOCK)\r\n    ) inv ON inv.RoomTypeID = rt.ID AND inv.[Date] = d.[Date]\r\n    LEFT JOIN (\r\n        SELECT \r\n            r.RoomTypeID, r.RateDate AS [Date], COUNT(DISTINCT r.RoomID) AS AssignedRooms\r\n        FROM ReservationRate r WITH (NOLOCK)\r\n        JOIN Reservation re WITH (NOLOCK) ON r.ReservationID = re.ID\r\n        JOIN ReservationType k WITH (NOLOCK) ON re.ReservationTypeID = k.ID\r\n        WHERE \r\n            re.Status NOT IN (3, 4) AND re.MainGuest = 1\r\n            AND re.ReservationNo > 0 AND r.RoomID > 0 AND k.Deduct = 1\r\n        GROUP BY r.RoomTypeID, r.RateDate\r\n    ) ar ON ar.RoomTypeID = rt.ID AND ar.[Date] = d.[Date]\r\n    LEFT JOIN (\r\n        SELECT \r\n            r.RoomTypeID, d2.[Date], SUM(r.NoOfRoom) AS UnassignedRooms\r\n        FROM Reservation r WITH (NOLOCK)\r\n        JOIN RoomType rt2 WITH (NOLOCK) ON r.RoomTypeID = rt2.ID\r\n        JOIN ReservationType k WITH (NOLOCK) ON r.ReservationTypeID = k.ID\r\n        CROSS APPLY (\r\n            SELECT DATEADD(DAY, number, r.ArrivalDate) AS [Date]\r\n            FROM master..spt_values\r\n            WHERE type = 'P' AND number <= DATEDIFF(DAY, r.ArrivalDate, r.DepartureDate) - 1\r\n        ) d2\r\n        WHERE \r\n            rt2.IsPseudo = 0 AND r.RoomID = 0 AND r.MainGuest = 1 \r\n            AND r.Status NOT IN (3, 4) AND k.Deduct = 1 AND r.ReservationNo > 0\r\n        GROUP BY r.RoomTypeID, d2.[Date]\r\n    ) ur ON ur.RoomTypeID = rt.ID AND ur.[Date] = d.[Date]\r\n    LEFT JOIN (\r\n        SELECT \r\n            c.RoomTypeID, d3.[Date], COUNT(DISTINCT b.RoomID) AS OOORooms\r\n        FROM BusinessBlock b WITH (NOLOCK)\r\n        JOIN Room c WITH (NOLOCK) ON b.RoomID = c.ID\r\n        CROSS APPLY (\r\n            SELECT DATEADD(DAY, number, b.FromDateOOO) AS [Date]\r\n            FROM master..spt_values\r\n            WHERE type = 'P' AND number <= DATEDIFF(DAY, b.FromDateOOO, b.ToDateOOO)\r\n        ) d3\r\n        GROUP BY c.RoomTypeID, d3.[Date]\r\n    ) ooo ON ooo.RoomTypeID = rt.ID AND ooo.[Date] = d.[Date]\r\n    WHERE rt.IsPseudo = 0\r\n)\r\nSELECT * INTO #RoomAvailability FROM RoomAvailability;\r\n\r\n-- B3: Pivot columns\r\nDECLARE @cols NVARCHAR(MAX) = '';\r\nSELECT @cols += QUOTENAME(CONVERT(VARCHAR(10), [Date], 120)) + ',' \r\nFROM @dateList \r\nWHERE [Date] <= @toDate\r\nORDER BY [Date];\r\n\r\n-- Check if @cols is empty\r\nIF LEN(@cols) = 0\r\nBEGIN\r\n    RAISERROR ('No dates available to pivot.', 16, 1);\r\n    DROP TABLE #RoomAvailability;\r\n    RETURN;\r\nEND;\r\n\r\nSET @cols = LEFT(@cols, LEN(@cols) - 1); -- Remove trailing comma\r\n\r\nDECLARE @sql NVARCHAR(MAX) = '\r\nSELECT RoomTypeID, RoomType, ' + @cols + '\r\nFROM (\r\n    SELECT RoomTypeID, RoomType, CONVERT(VARCHAR(10), [Date], 120) AS [Date], AvailableRooms\r\n    FROM #RoomAvailability\r\n) src\r\nPIVOT (\r\n    MAX(AvailableRooms) FOR [Date] IN (' + @cols + ')\r\n) p\r\nORDER BY RoomType;\r\n';\r\n\r\nEXEC sp_executesql @sql;\r\n\r\n-- Clean up\r\nDROP TABLE #RoomAvailability;";
            return instance.GetList<object>(query);
        }

        public static List<ReservationModel> GetReservationByBusinessDate(string date)
        {
            string query = $"select * from Reservation where cast(DepartureDate as Date) = cast('{date}' as date) and Status in (1,5)";
            return instance.GetList<ReservationModel>(query);
        }
        public static int CheckCheckIn(int ShareRoom)
        {
            DataTable dtCI = TextUtils.Select("SELECT ID from Reservation WITH (NOLOCK)" +
                                              "WHERE ShareRoom = " + ShareRoom + " " +
                                              "AND Mainguest = 1 " +
                                              "AND (Status = 0 OR Status = 5) ");
            if (dtCI.Rows.Count > 0)
                return 1;
            else
                return 0;
        }
        public static ArrayList GetReservationAlerts(int ReservationID)
        {
            try
            {
                string Area = "";
                ////Lấy thông tin bảng Rsv 
                //ReservationModel mR = (ReservationModel)ReservationBO.Instance.FindByPK(ReservationID);
                //if (mR.Status == 0 || mR.Status == 5)
                //    Area = "Reservation";
                //else if (mR.Status == 5 || mR.Status == 6)
                //    Area = "Check-In";
                //else if (mR.Status == 2)
                //    Area = "Check-Out";
                //Tim kiem Routing
                Expression expR = new Expression("ReservationID", ReservationID, "=");
                //expR = expR.And(new Expression("Area", Area, "="));
                ArrayList arrR = ReservationAlertsBO.Instance.FindByExpression(expR);
                //Kiểm tra điều kiện để trả về giá trị
                if (arrR.Count == 0)
                    return arrR;
                else
                {
                    return arrR;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public static ArrayList GetReservationAlert(int ReservationID)
        {
            try
            {
                string Area = "";
                ////Lấy thông tin bảng Rsv 
                //ReservationModel mR = (ReservationModel)ReservationBO.Instance.FindByPK(ReservationID);
                //if (mR.Status == 0 || mR.Status == 5)
                //    Area = "Reservation";
                //else if (mR.Status == 5 || mR.Status == 6)
                //    Area = "Check-In";
                //else if (mR.Status == 2)
                //    Area = "Check-Out";
                //Tim kiem Routing
                Expression expR = new Expression("ReservationID", ReservationID, "=");
                //expR = expR.And(new Expression("Area", Area, "="));
                ArrayList arrR = ReservationAlertsBO.Instance.FindByExpression(expR);
                //Kiểm tra điều kiện để trả về giá trị
                if (arrR.Count == 0)
                    return arrR;
                else
                {
                    return arrR;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public static void UpdateReservationStatus(ProcessTransactions pt, int RoomID)
        {
            #region Khai báo biến 
            DataTable dt = null;
            bool Reservation = false;
            bool CheckedIn = false;
            bool BusinessDate = false;
            bool DayUse = false;
            bool CheckedOut = false;
            bool CancelRsv = false;
            bool Waitlist = false;
            bool DueIn = false;
            bool DueOut = false;
            bool NoShow = false;
            string CurrResvStatus = "";
            #endregion            

            //Đổ dữ liệu ra model từ bảng room
            RoomModel mR = null;
            if (pt != null)
                mR = (RoomModel)pt.FindByPK("Room", RoomID);
            else
                mR = (RoomModel)RoomBO.Instance.FindByPrimaryKey(RoomID);
            DateTime _BusDate = TextUtils.GetBusinessDate();

            #region Liệt kê danh sách booking trong bảng ReservationRate 
            dt = TextUtils.Select("SELECT DISTINCT a.Status, b.ReservationID, b.RateDate, a.ArrivalDate, a.DepartureDate " +
                                  "FROM dbo.Reservation a WITH (NOLOCK), dbo.ReservationRate b WITH (NOLOCK) " +
                                  "WHERE a.ID = b.ReservationID " +
                                  "AND a.ReservationNo > 0 " +
                                  "AND a.MainGuest = 1 " +
                                  "AND b.RoomID = " + RoomID + " " +
                                  "AND (DATEDIFF(day, b.RateDate, '" + _BusDate.ToString("yyyy/MM/dd") + "') = 0 " +
                                  "OR DATEDIFF(day, b.RateDate,  '" + _BusDate.AddDays(1).ToString("yyyy/MM/dd") + "') = 0) ");
            #endregion

            if (dt.Rows.Count > 0)
            {
                #region Xác định trạng thái của phòng 
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    int status = Convert.ToInt32(dt.Rows[i]["Status"]);
                    if (status == 0)
                        Reservation = true;
                    else if (status == 1)
                    {
                        CheckedIn = true;
                        //BusinessDate
                        if (TextUtils.CompareDate(Convert.ToDateTime(dt.Rows[i]["ArrivalDate"]), _BusDate) == 0)
                            BusinessDate = true;
                        else
                            BusinessDate = false;
                        //DayUse
                        if (TextUtils.CompareDate(Convert.ToDateTime(dt.Rows[i]["ArrivalDate"]), _BusDate) == 0
                         && TextUtils.CompareDate(Convert.ToDateTime(dt.Rows[i]["ArrivalDate"]), Convert.ToDateTime(dt.Rows[i]["DepartureDate"])) == 0)
                            DayUse = true;
                        else
                            DayUse = false;
                    }
                    else if (status == 2)
                        CheckedOut = true;
                    else if (status == 3)
                        CancelRsv = true;
                    else if (status == 4)
                        Waitlist = true;
                    else if (status == 5)
                        DueIn = true;
                    else if (status == 6)
                        DueOut = true;
                    else if (status == 7)
                        NoShow = true;
                }
                #endregion

                #region Update trạng thái CurrResvStatus trong bảng room 
                //Departured/Arrival
                if ((CheckedOut == true || DueOut == true) && (Reservation == true || DueIn == true))
                {
                    mR.CurrResvStatus = 7;
                    if (pt != null)
                        pt.Update(mR);
                    else
                        RoomBO.Instance.Update(mR);
                }
                //Arrivals - (Displays the reservations due to arrive today, but not yet checked in)
                else if ((Reservation == true || DueIn == true) && (CheckedIn == false && CheckedOut == false && DueOut == false))
                {
                    mR.CurrResvStatus = 0;
                    if (pt != null)
                        pt.Update(mR);
                    else
                        RoomBO.Instance.Update(mR);
                }
                //Arrived - (Displays the reservations due in today and already checked in) 
                else if (CheckedIn == true && BusinessDate == true)
                {
                    mR.CurrResvStatus = 1;
                    if (pt != null)
                        pt.Update(mR);
                    else
                        RoomBO.Instance.Update(mR);
                }
                //Stayover. Displays the guests that stayed the previous evening and that are continuing their stay for the present system day (date).
                else if (CheckedIn == true && BusinessDate == false)
                {
                    mR.CurrResvStatus = 2;
                    if (pt != null)
                        pt.Update(mR);
                    else
                        RoomBO.Instance.Update(mR);
                }
                //Day Use. Displays the guests staying for the present system day (date). Arrival date and Departure date is equal to today's date.
                else if (CheckedIn == true && DayUse == true)
                {
                    mR.CurrResvStatus = 3;
                    if (pt != null)
                        pt.Update(mR);
                    else
                        RoomBO.Instance.Update(mR);
                }
                //Due Out - (Displays the guests due to check out. If a room is due out, that room is an available room to use for the current business date.)
                else if ((DueOut == true && CheckedOut == false) && (Reservation == false || DueIn == false))
                {
                    mR.CurrResvStatus = 4;
                    if (pt != null)
                        pt.Update(mR);
                    else
                        RoomBO.Instance.Update(mR);
                }
                //Departed - ( Displays the departures that have checked out for the day.)
                else if (CheckedOut == true && (Reservation == false || DueIn == false))
                {
                    mR.CurrResvStatus = 5;
                    if (pt != null)
                        pt.Update(mR);
                    else
                        RoomBO.Instance.Update(mR);
                }
                #endregion
            }
            else
            {
                //Not Reserved - (Displays all rooms that are not reserved for the day or in the future)         
                mR.CurrResvStatus = 6;
                if (pt != null)
                    pt.Update(mR);
                else
                    RoomBO.Instance.Update(mR);
            }
        }
        public static void GetRoomRevenue(int ReservationID, ProcessTransactions pt)
        {
            #region 1.Khai báo biến 
            ReservationModel mR = (ReservationModel)pt.FindByPK("Reservation", ReservationID);
            DataTable dtCS = null;
            DataTable dtFC = null;
            DataTable dtPkg = null;
            DataTable dtPkgInc = null;
            DataTable dtRR = null;
            decimal Rate = 0;
            //FC
            decimal AmountBeforTax = 0;
            decimal AmountAfterTax = 0;
            decimal RoomRevenueBeforeTax = 0;
            decimal RoomRevenueAfterTax = 0;
            //Pkg
            decimal Price = 0;
            decimal OriginPrice = 0;

            bool TaxInclude = false;
            bool _RoIsTaxInclude = false;
            int FC = 0;
            int PkgP = 0;
            int PkgC = 0;
            // code room revenue
            string _CodeRoomRevenue = "";
            #endregion

            #region 2.Nhặt ra code tiền phòng trong bảng ConfigSystem 
            //dtCS = TextUtils.Select("SELECT KeyValue FROM ConfigSystem " +
            //                 "WHERE KeyName = 'RoomCharge' " +
            //                 "OR KeyName = 'RoomChargeFITUSD' " +
            //                 "OR KeyName = 'RoomChargeFITVND' " +
            //                 "OR KeyName = 'RoomChargeVND' ");

            dtCS = TextUtils.Select("SELECT Code AS KeyValue FROM Transactions WITH (NOLOCK) WHERE TransactionGroupID = 6 ");
            for (int t = 0; t < dtCS.Rows.Count; t++)
            {
                if (_CodeRoomRevenue == "")
                    _CodeRoomRevenue = dtCS.Rows[t]["KeyValue"].ToString();
                else
                    _CodeRoomRevenue = _CodeRoomRevenue + "," + dtCS.Rows[t]["KeyValue"].ToString();
            }
            #endregion

            #region 3.Nhặt RoomRevenue trong bảng FixedCharge 
            dtFC = pt.Select("SELECT PostingRhythmID, CurrencyID, Amount, AmountAfterTax, Quantity, PostingDate, PostingDay FROM ReservationFixedCharge WITH (NOLOCK)" +
                                    "WHERE ReservationID = " + ReservationID + " " +
                                    "AND TransactionCode IN (" + _CodeRoomRevenue + ") ");

            #endregion

            #region 4.Nhặt RoomRevenue trong bảng ReservationPackage 
            dtPkg = pt.Select("SELECT Price, PriceAfterTax, CurrencyID, Quantity, PostingDate, PostingDay, CalculationRuleID, PostingRhythmID FROM ReservationPackage WITH (NOLOCK) " +
                                     "WHERE ReservationID = " + ReservationID + " " +
                                     "AND TransactionCode IN (" + _CodeRoomRevenue + ") ");
            #endregion

            #region 5.Xác định tiền Package Include trong tiền phòng 
            dtPkgInc = pt.Select("SELECT a.Quantity, a.Price,a.PriceAfterTax, a.CurrencyID, a.TransactionCode, a.CalculationRuleID, a.PostingRhythmID, PostingDate " +
                                 "FROM   dbo.ReservationPackage a WITH (NOLOCK), dbo.Package b WITH (NOLOCK) " +
                                 "WHERE  a.PackageID = b.ID " +
                                 "AND    b.IncludedInRate = 1 " +
                                 "AND    a.ReservationID = " + ReservationID + " ");
            #endregion

            #region 6.Process by date 
            dtRR = pt.Select("SELECT ID, RateDate, Rate, RateAfterTax, IsTaxInclude, DiscountRate, DiscountAmount, TransactionCode, CurrencyID " +
                                 "FROM ReservationRate WITH (NOLOCK)" +
                                 "WHERE ReservationID = " + ReservationID + " ");
            for (int i = 0; i < dtRR.Rows.Count; i++)
            {
                #region 6.1.Xác định tiền phòng trong bảng ReservationRate 
                _RoIsTaxInclude = bool.Parse(dtRR.Rows[i]["IsTaxInclude"].ToString());
                //* NoOfRoom
                if (_RoIsTaxInclude == false)
                {
                    if (mR.NoOfRoom > 0)
                        Rate = TextUtils.ToDecimal(dtRR.Rows[i]["Rate"].ToString()) * mR.NoOfRoom;
                    else
                        Rate = TextUtils.ToDecimal(dtRR.Rows[i]["Rate"].ToString());
                }
                else
                {
                    if (mR.NoOfRoom > 0)
                        Rate = TextUtils.ToDecimal(dtRR.Rows[i]["RateAfterTax"].ToString()) * mR.NoOfRoom;
                    else
                        Rate = TextUtils.ToDecimal(dtRR.Rows[i]["RateAfterTax"].ToString());
                }
                //Xác định tiền phòng/ngày đã trừ Discount
                Rate = Rate - ((Rate * TextUtils.ToDecimal(dtRR.Rows[i]["DiscountRate"].ToString())) / 100)
                            - (TextUtils.ToDecimal(dtRR.Rows[i]["DiscountAmount"].ToString())) * mR.NoOfRoom;
                #endregion

                #region 6.2.Xác định tiền phòng trong bảng ReservationFixcharge 
                for (int k = 0; k < dtFC.Rows.Count; k++)
                {
                    FC = 0;

                    #region Xác định thời điểm charge 
                    switch (dtFC.Rows[k]["PostingRhythmID"].ToString())
                    {
                        case "1"://Everyday
                            {
                                FC = 1;
                                break;
                            }
                        case "2"://Date(At date)
                            {
                                FC = 2;
                                break;
                            }
                        case "3"://Day(At Day Of use)
                            {
                                FC = 3;
                                break;
                            }
                        case "4"://At Check in
                            {
                                FC = 4;
                                break;
                            }
                        case "5"://At Check Out
                            {
                                FC = 5;
                                break;
                            }
                    }
                    #endregion

                    #region Nếu cùng TransactionCode - (Cấu trúc mới không phải chia ra hai trường hợp nữa --> CSS 
                    //if (dtRR.Rows[i]["TransactionCode"].ToString() == dtFC.Rows[k]["TransactionCode"].ToString())
                    //{
                    #region Every day 
                    if (FC == 1)
                    {
                        if (dtRR.Rows[i]["CurrencyID"].ToString() == dtFC.Rows[k]["CurrencyID"].ToString())
                        {
                            if (_RoIsTaxInclude == false)
                                Rate = Rate + (TextUtils.ToDecimal(dtFC.Rows[k]["Amount"].ToString()) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                            else
                                Rate = Rate + (TextUtils.ToDecimal(dtFC.Rows[k]["AmountAfterTax"].ToString()) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                        }
                        else
                        {
                            if (_RoIsTaxInclude == false)
                                Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtFC.Rows[k]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), TextUtils.ToDecimal(dtFC.Rows[k]["Amount"].ToString())) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                            else
                                Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtFC.Rows[k]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), TextUtils.ToDecimal(dtFC.Rows[k]["AmountAfterTax"].ToString())) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                        }
                    }
                    #endregion

                    #region Date(At date)
                    else if (FC == 2)
                    {
                        if (TextUtils.CompareDate(Convert.ToDateTime(dtRR.Rows[i]["RateDate"]), Convert.ToDateTime(dtFC.Rows[k]["PostingDate"])) == 0)
                            if (dtRR.Rows[i]["CurrencyID"].ToString() == dtFC.Rows[k]["CurrencyID"].ToString())
                            {
                                if (_RoIsTaxInclude == false)
                                    Rate = Rate + (TextUtils.ToDecimal(dtFC.Rows[k]["Amount"].ToString()) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                                else
                                    Rate = Rate + (TextUtils.ToDecimal(dtFC.Rows[k]["AmountAfterTax"].ToString()) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                            }
                            else
                            {
                                if (_RoIsTaxInclude == false)
                                    Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtFC.Rows[k]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), TextUtils.ToDecimal(dtFC.Rows[k]["Amount"].ToString())) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                                else
                                    Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtFC.Rows[k]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), TextUtils.ToDecimal(dtFC.Rows[k]["AmountAfterTax"].ToString())) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                            }
                    }
                    #endregion

                    #region Day(At Day Of use) 
                    else if (FC == 3)
                    {
                        string _Postingday = dtFC.Rows[k]["PostingDay"].ToString();
                        string[] _arrPD = _Postingday.Split(',');
                        for (int _pd = 0; _pd < _arrPD.Length; _pd++)
                        {
                            if (_arrPD[_pd] != "")
                            {
                                if (i == int.Parse(_arrPD[_pd].ToString()) - 1)
                                {
                                    if (dtRR.Rows[i]["CurrencyID"].ToString() == dtFC.Rows[k]["CurrencyID"].ToString())
                                    {
                                        if (_RoIsTaxInclude == false)
                                            Rate = Rate + (TextUtils.ToDecimal(dtFC.Rows[k]["Amount"].ToString()) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                                        else
                                            Rate = Rate + (TextUtils.ToDecimal(dtFC.Rows[k]["AmountAfterTax"].ToString()) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                                    }
                                    else
                                    {
                                        if (_RoIsTaxInclude == false)
                                            Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtFC.Rows[k]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), TextUtils.ToDecimal(dtFC.Rows[k]["Amount"].ToString())) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                                        else
                                            Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtFC.Rows[k]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), TextUtils.ToDecimal(dtFC.Rows[k]["AmountAfterTax"].ToString())) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                                    }
                                }
                            }
                        }
                    }
                    #endregion

                    #region At Check in 
                    else if (FC == 4)
                    {
                        if (i == 0)
                            if (dtRR.Rows[i]["CurrencyID"].ToString() == dtFC.Rows[k]["CurrencyID"].ToString())
                            {
                                if (_RoIsTaxInclude == false)
                                    Rate = Rate + (TextUtils.ToDecimal(dtFC.Rows[k]["Amount"].ToString()) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                                else
                                    Rate = Rate + (TextUtils.ToDecimal(dtFC.Rows[k]["AmountAfterTax"].ToString()) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                            }
                            else
                            {
                                if (_RoIsTaxInclude == false)
                                    Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtFC.Rows[k]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), TextUtils.ToDecimal(dtFC.Rows[k]["Amount"].ToString())) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                                else
                                    Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtFC.Rows[k]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), TextUtils.ToDecimal(dtFC.Rows[k]["AmountAfterTax"].ToString())) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                            }
                    }
                    #endregion

                    #region At Check Out 
                    else if (FC == 5)
                    {
                        if (i == dtRR.Rows.Count - 1)
                            if (dtRR.Rows[i]["CurrencyID"].ToString() == dtFC.Rows[k]["CurrencyID"].ToString())
                            {
                                if (_RoIsTaxInclude == false)
                                    Rate = Rate + (TextUtils.ToDecimal(dtFC.Rows[k]["Amount"].ToString()) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                                else
                                    Rate = Rate + (TextUtils.ToDecimal(dtFC.Rows[k]["AmountAfterTax"].ToString()) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                            }
                            else
                            {
                                if (_RoIsTaxInclude == false)
                                    Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtFC.Rows[k]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), TextUtils.ToDecimal(dtFC.Rows[k]["Amount"].ToString())) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                                else
                                    Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtFC.Rows[k]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), TextUtils.ToDecimal(dtFC.Rows[k]["AmountAfterTax"].ToString())) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                            }
                    }
                    #endregion
                    //}
                    #endregion

                    #region Nếu khác TransactionCode 
                    //else
                    //{
                    //    //Xác định xem Transaction cùng TaxInclude
                    //    TaxInclude = TextUtils.GetTaxInclude(dtRR.Rows[i]["TransactionCode"].ToString());

                    //    #region Every day 
                    //    if (FC == 1)
                    //    {
                    //        //kiểm tra loại tiền 
                    //        if (dtRR.Rows[i]["CurrencyID"].ToString() == dtFC.Rows[k]["CurrencyID"].ToString())
                    //        {
                    //            AmountBeforTax = 0;
                    //            AmountAfterTax = 0;
                    //            //Xác định tiền trước thuế và sau thuế
                    //            TextUtils.GetSourceAmount(dtFC.Rows[k]["TransactionCode"].ToString(), TextUtils.ToDecimal(dtFC.Rows[k]["Amount"].ToString()), ref AmountBeforTax, ref AmountAfterTax);

                    //            if (TaxInclude == true)
                    //                Rate = Rate + AmountAfterTax * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString());
                    //            else
                    //                Rate = Rate + AmountBeforTax * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString());
                    //        }
                    //        else
                    //        {
                    //            AmountBeforTax = 0;
                    //            AmountAfterTax = 0;
                    //            //Xác định tiền trước thuế và sau thuế
                    //            TextUtils.GetSourceAmount(dtFC.Rows[k]["TransactionCode"].ToString(), TextUtils.ExchangeCurrency(mR.ReservationDate, dtFC.Rows[k]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), TextUtils.ToDecimal(dtFC.Rows[k]["Amount"].ToString())), ref AmountBeforTax, ref AmountAfterTax);

                    //            if (TaxInclude == true)
                    //                Rate = Rate + (AmountAfterTax * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                    //            else
                    //                Rate = Rate + (AmountBeforTax * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                    //        }
                    //    }
                    //    #endregion

                    //    #region Date(At date) 
                    //    else if (FC == 2)
                    //    {
                    //        if (TextUtils.CompareDate(Convert.ToDateTime(dtRR.Rows[i]["RateDate"]), Convert.ToDateTime(dtFC.Rows[k]["PostingDate"])) == 0)
                    //        {
                    //            if (dtRR.Rows[i]["CurrencyID"].ToString() == dtFC.Rows[k]["CurrencyID"].ToString())
                    //            {
                    //                AmountBeforTax = 0;
                    //                AmountAfterTax = 0;
                    //                //Xác định tiền trước thuế và sau thuế
                    //                TextUtils.GetSourceAmount(dtFC.Rows[k]["TransactionCode"].ToString(), TextUtils.ToDecimal(dtFC.Rows[k]["Amount"].ToString()), ref AmountBeforTax, ref AmountAfterTax);

                    //                if (TaxInclude == true)
                    //                    Rate = Rate + (AmountAfterTax * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                    //                else
                    //                    Rate = Rate + (AmountBeforTax * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                    //            }
                    //            else
                    //            {
                    //                AmountBeforTax = 0;
                    //                AmountAfterTax = 0;
                    //                //Xác định tiền trước thuế và sau thuế
                    //                TextUtils.GetSourceAmount(dtFC.Rows[k]["TransactionCode"].ToString(), TextUtils.ExchangeCurrency(mR.ReservationDate, dtFC.Rows[k]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), TextUtils.ToDecimal(dtFC.Rows[k]["Amount"].ToString())), ref AmountBeforTax, ref AmountAfterTax);
                    //                if (TaxInclude == true)
                    //                    Rate = Rate + (AmountAfterTax * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                    //                else
                    //                    Rate = Rate + (AmountBeforTax * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));

                    //            }
                    //        }
                    //    }
                    //    #endregion

                    //    #region Day(At Day Of use) 
                    //    else if (FC == 3)
                    //    {

                    //    }
                    //    #endregion

                    //    #region At Check in 
                    //    else if (FC == 4)
                    //    {
                    //        if (i == 0)
                    //            if (dtRR.Rows[i]["CurrencyID"].ToString() == dtFC.Rows[k]["CurrencyID"].ToString())
                    //            {
                    //                AmountBeforTax = 0;
                    //                AmountAfterTax = 0;
                    //                //Xác định tiền trước thuế và sau thuế
                    //                TextUtils.GetSourceAmount(dtFC.Rows[k]["TransactionCode"].ToString(), TextUtils.ToDecimal(dtFC.Rows[k]["Amount"].ToString()), ref AmountBeforTax, ref AmountAfterTax);

                    //                if (TaxInclude == true)
                    //                    Rate = Rate + (AmountAfterTax * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                    //                else
                    //                    Rate = Rate + (AmountAfterTax * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                    //            }
                    //            else
                    //            {
                    //                AmountBeforTax = 0;
                    //                AmountAfterTax = 0;
                    //                //Xác định tiền trước thuế và sau thuế
                    //                TextUtils.GetSourceAmount(dtFC.Rows[k]["TransactionCode"].ToString(), TextUtils.ExchangeCurrency(mR.ReservationDate, dtFC.Rows[k]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), TextUtils.ToDecimal(dtFC.Rows[k]["Amount"].ToString())), ref AmountBeforTax, ref AmountAfterTax);
                    //                if (TaxInclude == true)
                    //                    Rate = Rate + (AmountAfterTax * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                    //                else
                    //                    Rate = Rate + (AmountBeforTax * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                    //            }
                    //    }
                    //    #endregion 

                    //    #region At Check Out 
                    //    else if (FC == 5)
                    //    {
                    //        if (i == dtRR.Rows.Count - 1)
                    //            if (dtRR.Rows[i]["CurrencyID"].ToString() == dtFC.Rows[k]["CurrencyID"].ToString())
                    //            {
                    //                AmountBeforTax = 0;
                    //                AmountAfterTax = 0;
                    //                //Xác định tiền trước thuế và sau thuế
                    //                TextUtils.GetSourceAmount(dtFC.Rows[k]["TransactionCode"].ToString(), TextUtils.ToDecimal(dtFC.Rows[k]["Amount"].ToString()), ref AmountBeforTax, ref AmountAfterTax);
                    //                if (TaxInclude == true)
                    //                    Rate = Rate + (AmountAfterTax * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                    //                else
                    //                    Rate = Rate + (AmountBeforTax * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                    //            }
                    //            else
                    //            {
                    //                AmountBeforTax = 0;
                    //                AmountAfterTax = 0;
                    //                //Xác định tiền trước thuế và sau thuế
                    //                TextUtils.GetSourceAmount(dtFC.Rows[k]["TransactionCode"].ToString(), TextUtils.ExchangeCurrency(mR.ReservationDate, dtFC.Rows[k]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), TextUtils.ToDecimal(dtFC.Rows[k]["Amount"].ToString())), ref AmountBeforTax, ref AmountAfterTax);
                    //                if (TaxInclude == true)
                    //                    Rate = Rate + (AmountAfterTax * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                    //                else
                    //                    Rate = Rate + (AmountBeforTax * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                    //            }
                    //    }
                    //    #endregion
                    //}
                    #endregion
                }
                #endregion

                #region 6.3.Nhặt ra tiền phòng trong bảng ReservationPackage 
                for (int l = 0; l < dtPkg.Rows.Count; l++)
                {
                    Price = 0;
                    //Xác định giá gốc * NoOfRoom                    
                    if (_RoIsTaxInclude == false)
                    {
                        if (mR.NoOfRoom > 0)
                            OriginPrice = Decimal.Parse(dtPkg.Rows[l]["Price"].ToString()) * mR.NoOfRoom;
                        else
                            OriginPrice = Decimal.Parse(dtPkg.Rows[l]["Price"].ToString());
                    }
                    else
                    {
                        if (mR.NoOfRoom > 0)
                            OriginPrice = Decimal.Parse(dtPkg.Rows[l]["PriceAfterTax"].ToString()) * mR.NoOfRoom;
                        else
                            OriginPrice = Decimal.Parse(dtPkg.Rows[l]["PriceAfterTax"].ToString());
                    }

                    #region Phương thức charge CanculationRule
                    switch (dtPkg.Rows[l]["CalculationRuleID"].ToString())
                    {
                        case "1"://Per/Person(từng người trong phòng)
                            {
                                PkgC = 1;
                                break;
                            }
                        case "2"://Per Adult
                            {
                                PkgC = 2;
                                break;
                            }
                        case "3"://Per Child
                            {
                                PkgC = 3;
                                break;
                            }
                        case "4"://Per Room
                            {
                                PkgC = 4;
                                break;
                            }
                        case "5"://Per C1
                            {
                                PkgC = 5;
                                break;
                            }
                        case "6"://Per C2
                            {
                                PkgC = 6;
                                break;
                            }
                        case "8"://Per A + C
                            {
                                PkgC = 8;
                                break;
                            }
                        case "9"://Per A + C + C1
                            {
                                PkgC = 9;
                                break;
                            }
                        case "10"://Per A + C1
                            {
                                PkgC = 10;
                                break;
                            }
                        case "11"://Per C + C1
                            {
                                PkgC = 11;
                                break;
                            }
                    }
                    //Xác định tiền phòng

                    //Per/Person(từng người trong phòng)
                    if (PkgC == 1)
                    {
                        if (mR.NoOfAdult > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfAdult.ToString());
                        if (mR.NoOfChild > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild.ToString()));
                        if (mR.NoOfChild1 > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild1.ToString()));
                        if (mR.NoOfChild2 > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild2.ToString()));
                        else if (mR.NoOfAdult == 0 && mR.NoOfChild == 0 && mR.NoOfChild1 == 0 && mR.NoOfChild2 == 0)
                        {
                            Price = 0;
                        }
                    }
                    //Per Adult
                    else if (PkgC == 2)
                    {
                        if (mR.NoOfAdult > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfAdult.ToString());
                        else if (mR.NoOfAdult == 0)
                            Price = 0;
                    }
                    //Per Child
                    else if (PkgC == 3)
                    {
                        if (mR.NoOfChild > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfChild.ToString());
                        else if (mR.NoOfChild == 0)
                            Price = 0;
                    }
                    //Per Room
                    else if (PkgC == 4)
                    {
                        Price = OriginPrice;
                    }
                    //Per Child1
                    else if (PkgC == 5)
                    {
                        if (mR.NoOfChild1 > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfChild1.ToString());
                        else if (mR.NoOfChild1 == 0)
                            Price = 0;
                    }
                    //Per Child2
                    else if (PkgC == 6)
                    {
                        if (mR.NoOfChild2 > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfChild2.ToString());
                        else if (mR.NoOfChild2 == 0)
                            Price = 0;
                    }
                    //Per A + C
                    else if (PkgC == 8)
                    {
                        if (mR.NoOfAdult > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfAdult.ToString());
                        if (mR.NoOfChild > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild.ToString()));
                        else if (mR.NoOfAdult == 0 && mR.NoOfChild == 0)
                            Price = 0;
                    }
                    //Per A + C + C1
                    else if (PkgC == 9)
                    {
                        if (mR.NoOfAdult > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfAdult.ToString());
                        if (mR.NoOfChild > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild.ToString()));
                        if (mR.NoOfChild1 > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild1.ToString()));
                        else if (mR.NoOfAdult == 0 && mR.NoOfChild == 0 && mR.NoOfChild1 == 0)
                            Price = 0;
                    }
                    //Per A + C1
                    else if (PkgC == 10)
                    {
                        if (mR.NoOfAdult > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfAdult.ToString());
                        if (mR.NoOfChild1 > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild1.ToString()));
                        else if (mR.NoOfAdult == 0 && mR.NoOfChild1 == 0)
                            Price = 0;
                    }
                    //Per C + C1
                    else if (PkgC == 11)
                    {
                        if (mR.NoOfChild > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfChild.ToString());
                        if (mR.NoOfChild1 > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild1.ToString()));
                        else if (mR.NoOfChild == 0 && mR.NoOfChild1 == 0)
                            Price = 0;
                    }
                    #endregion

                    #region Xác định thời điểm charge
                    switch (dtPkg.Rows[l]["PostingRhythmID"].ToString())
                    {
                        case "1"://Everyday
                            {
                                PkgP = 1;
                                break;
                            }
                        case "2"://Date(At date)
                            {
                                PkgP = 2;
                                break;
                            }
                        case "3"://Day(At Day Of use)
                            {
                                PkgP = 3;
                                break;
                            }
                        case "4"://At Check in
                            {
                                PkgP = 4;
                                break;
                            }
                        case "5"://At Check Out
                            {
                                PkgP = 5;
                                break;
                            }
                    }
                    #endregion

                    #region Nếu cùng transaction - (Cấu trúc mới không phải chia ra hai trường hợp nữa --> CSS

                    #region Every day
                    if (PkgP == 1)
                    {
                        if (dtRR.Rows[i]["CurrencyID"].ToString() == dtPkg.Rows[l]["CurrencyID"].ToString())
                            Rate = Rate + Price;
                        else
                        {
                            Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtPkg.Rows[l]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), Price) * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                        }
                    }
                    #endregion

                    #region Date(At date)
                    else if (PkgP == 2)
                    {
                        if (TextUtils.CompareDate(Convert.ToDateTime(dtRR.Rows[i]["RateDate"]), Convert.ToDateTime(dtPkg.Rows[l]["PostingDate"])) == 0)
                            if (dtRR.Rows[i]["CurrencyID"].ToString() == dtPkg.Rows[l]["CurrencyID"].ToString())
                                Rate = Rate + (Price * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                            else
                                Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtPkg.Rows[l]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), Price) * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                    }
                    #endregion

                    #region Day(At Day Of use)
                    else if (PkgP == 3)
                    {
                        string _Postingday = dtPkg.Rows[l]["PostingDay"].ToString();
                        string[] _arrPD = _Postingday.Split(',');
                        for (int _pd = 0; _pd < _arrPD.Length; _pd++)
                        {
                            if (_arrPD[_pd] != "")
                            {
                                if (i == int.Parse(_arrPD[_pd].ToString()) - 1)
                                {
                                    if (dtRR.Rows[i]["CurrencyID"].ToString() == dtPkg.Rows[l]["CurrencyID"].ToString())
                                        Rate = Rate + (Price * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                                    else
                                        Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtPkg.Rows[l]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), Price) * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                                }
                            }
                        }
                    }
                    #endregion

                    #region At Check in
                    else if (PkgP == 4)
                    {
                        if (i == 0)
                            if (dtRR.Rows[i]["CurrencyID"].ToString() == dtPkg.Rows[l]["CurrencyID"].ToString())
                                Rate = Rate + (Price * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                            else
                                Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtPkg.Rows[l]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), Price) * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                    }
                    #endregion

                    #region At Check Out
                    else if (PkgP == 5)
                    {
                        if (i == dtRR.Rows.Count - 1)
                            if (dtRR.Rows[i]["CurrencyID"].ToString() == dtPkg.Rows[l]["CurrencyID"].ToString())
                                Rate = Rate + (Price * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                            else
                                Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtPkg.Rows[l]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), Price) * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                    }
                    #endregion

                    #endregion

                    #region Nếu khác TransactionCode
                    //else
                    //{
                    //    //Xác định xem Transaction cùng TaxInclude
                    //    TaxInclude = TextUtils.GetTaxInclude(dtRR.Rows[i]["TransactionCode"].ToString());

                    //    #region Every day 
                    //    if (FC == 1)
                    //    {
                    //        //kiểm tra loại tiền 
                    //        if (dtRR.Rows[i]["CurrencyID"].ToString() == dtPkg.Rows[l]["CurrencyID"].ToString())
                    //        {
                    //            AmountBeforTax = 0;
                    //            AmountAfterTax = 0;
                    //            //Xác định tiền trước thuế và sau thuế
                    //            TextUtils.GetSourceAmount(dtPkg.Rows[l]["TransactionCode"].ToString(), Price, ref AmountBeforTax, ref AmountAfterTax);

                    //            if (TaxInclude == true)
                    //                Rate = Rate + AmountAfterTax * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString());
                    //            else
                    //                Rate = Rate + AmountBeforTax * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString());
                    //        }
                    //        else
                    //        {
                    //            AmountBeforTax = 0;
                    //            AmountAfterTax = 0;
                    //            //Xác định tiền trước thuế và sau thuế
                    //            TextUtils.GetSourceAmount(dtPkg.Rows[l]["TransactionCode"].ToString(), TextUtils.ExchangeCurrency(mR.ReservationDate, dtPkg.Rows[l]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), Price), ref AmountBeforTax, ref AmountAfterTax);

                    //            if (TaxInclude == true)
                    //                Rate = Rate + (AmountAfterTax * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                    //            else
                    //                Rate = Rate + (AmountBeforTax * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                    //        }
                    //    }
                    //    #endregion

                    //    #region Date(At date) 
                    //    else if (FC == 2)
                    //    {
                    //        if (TextUtils.CompareDate(Convert.ToDateTime(dtRR.Rows[i]["RateDate"]), Convert.ToDateTime(dtPkg.Rows[l]["PostingDate"])) == 0)
                    //        {
                    //            if (dtRR.Rows[i]["CurrencyID"].ToString() == dtPkg.Rows[l]["CurrencyID"].ToString())
                    //            {
                    //                AmountBeforTax = 0;
                    //                AmountAfterTax = 0;
                    //                //Xác định tiền trước thuế và sau thuế
                    //                TextUtils.GetSourceAmount(dtPkg.Rows[l]["TransactionCode"].ToString(), Price, ref AmountBeforTax, ref AmountAfterTax);

                    //                if (TaxInclude == true)
                    //                    Rate = Rate + (AmountAfterTax * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                    //                else
                    //                    Rate = Rate + (AmountBeforTax * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                    //            }
                    //            else
                    //            {
                    //                AmountBeforTax = 0;
                    //                AmountAfterTax = 0;
                    //                //Xác định tiền trước thuế và sau thuế
                    //                TextUtils.GetSourceAmount(dtPkg.Rows[l]["TransactionCode"].ToString(), TextUtils.ExchangeCurrency(mR.ReservationDate, dtPkg.Rows[l]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), Price), ref AmountBeforTax, ref AmountAfterTax);
                    //                if (TaxInclude == true)
                    //                    Rate = Rate + (AmountAfterTax * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                    //                else
                    //                    Rate = Rate + (AmountBeforTax * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));

                    //            }
                    //        }
                    //    }
                    //    #endregion

                    //    #region Day(At Day Of use) 
                    //    else if (FC == 3)
                    //    {

                    //    }
                    //    #endregion

                    //    #region At Check in 
                    //    else if (FC == 4)
                    //    {
                    //        if (i == 0)
                    //            if (dtRR.Rows[i]["CurrencyID"].ToString() == dtPkg.Rows[l]["CurrencyID"].ToString())
                    //            {
                    //                AmountBeforTax = 0;
                    //                AmountAfterTax = 0;
                    //                //Xác định tiền trước thuế và sau thuế
                    //                TextUtils.GetSourceAmount(dtPkg.Rows[l]["TransactionCode"].ToString(),Price, ref AmountBeforTax, ref AmountAfterTax);

                    //                if (TaxInclude == true)
                    //                    Rate = Rate + (AmountAfterTax * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                    //                else
                    //                    Rate = Rate + (AmountAfterTax * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                    //            }
                    //            else
                    //            {
                    //                AmountBeforTax = 0;
                    //                AmountAfterTax = 0;
                    //                //Xác định tiền trước thuế và sau thuế
                    //                TextUtils.GetSourceAmount(dtPkg.Rows[l]["TransactionCode"].ToString(), TextUtils.ExchangeCurrency(mR.ReservationDate, dtPkg.Rows[l]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), Price), ref AmountBeforTax, ref AmountAfterTax);
                    //                if (TaxInclude == true)
                    //                    Rate = Rate + (AmountAfterTax * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                    //                else
                    //                    Rate = Rate + (AmountBeforTax * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                    //            }
                    //    }
                    //    #endregion

                    //    #region At Check Out 
                    //    else if (FC == 5)
                    //    {
                    //        if (i == dtRR.Rows.Count - 1)
                    //            if (dtRR.Rows[i]["CurrencyID"].ToString() == dtPkg.Rows[l]["CurrencyID"].ToString())
                    //            {
                    //                AmountBeforTax = 0;
                    //                AmountAfterTax = 0;
                    //                //Xác định tiền trước thuế và sau thuế
                    //                TextUtils.GetSourceAmount(dtPkg.Rows[l]["TransactionCode"].ToString(), Price, ref AmountBeforTax, ref AmountAfterTax);
                    //                if (TaxInclude == true)
                    //                    Rate = Rate + (AmountAfterTax * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                    //                else
                    //                    Rate = Rate + (AmountBeforTax * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                    //            }
                    //            else
                    //            {
                    //                AmountBeforTax = 0;
                    //                AmountAfterTax = 0;
                    //                //Xác định tiền trước thuế và sau thuế
                    //                TextUtils.GetSourceAmount(dtPkg.Rows[l]["TransactionCode"].ToString(), TextUtils.ExchangeCurrency(mR.ReservationDate, dtPkg.Rows[l]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), Price), ref AmountBeforTax, ref AmountAfterTax);
                    //                if (TaxInclude == true)
                    //                    Rate = Rate + (AmountAfterTax * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                    //                else
                    //                    Rate = Rate + (AmountBeforTax * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                    //            }
                    //    }
                    //    #endregion
                    //}
                    #endregion

                }
                #endregion                                   

                #region 6.4.Xác định tiền Package Include trong tiền phòng 
                for (int m = 0; m < dtPkgInc.Rows.Count; m++)
                {
                    Price = 0;
                    //Xác định giá gốc * NoOfRoom                    
                    if (_RoIsTaxInclude == false)
                    {
                        if (mR.NoOfRoom > 0)
                            OriginPrice = Decimal.Parse(dtPkgInc.Rows[m]["Price"].ToString()) * mR.NoOfRoom;
                        else
                            OriginPrice = Decimal.Parse(dtPkgInc.Rows[m]["Price"].ToString());
                    }
                    else
                    {
                        if (mR.NoOfRoom > 0)
                            OriginPrice = Decimal.Parse(dtPkgInc.Rows[m]["PriceAfterTax"].ToString()) * mR.NoOfRoom;
                        else
                            OriginPrice = Decimal.Parse(dtPkgInc.Rows[m]["PriceAfterTax"].ToString());
                    }

                    #region Phương thức charge CanculationRule 
                    PkgC = 0;
                    switch (dtPkgInc.Rows[m]["CalculationRuleID"].ToString())
                    {
                        case "1"://Per/Person(từng người trong phòng)
                            {
                                PkgC = 1;
                                break;
                            }
                        case "2"://Per Adult
                            {
                                PkgC = 2;
                                break;
                            }
                        case "3"://Per Child
                            {
                                PkgC = 3;
                                break;
                            }
                        case "4"://Per Room
                            {
                                PkgC = 4;
                                break;
                            }
                        case "5"://Per C1
                            {
                                PkgC = 5;
                                break;
                            }
                        case "6"://Per C2
                            {
                                PkgC = 6;
                                break;
                            }
                        case "8"://Per A + C
                            {
                                PkgC = 8;
                                break;
                            }
                        case "9"://Per A + C + C1
                            {
                                PkgC = 9;
                                break;
                            }
                        case "10"://Per A + C1
                            {
                                PkgC = 10;
                                break;
                            }
                        case "11"://Per C + C1
                            {
                                PkgC = 11;
                                break;
                            }
                    }

                    //Xác định tiền phòng
                    //Per/Person(từng người trong phòng)
                    if (PkgC == 1)
                    {
                        if (mR.NoOfAdult > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfAdult.ToString());
                        if (mR.NoOfChild > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild.ToString()));
                        if (mR.NoOfChild1 > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild1.ToString()));
                        if (mR.NoOfChild2 > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild2.ToString()));
                        else if (mR.NoOfAdult == 0 && mR.NoOfChild == 0 && mR.NoOfChild1 == 0 && mR.NoOfChild2 == 0)
                            Price = 0;
                    }
                    //Per Adult
                    else if (PkgC == 2)
                    {
                        if (mR.NoOfAdult > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfAdult.ToString());
                        else if (mR.NoOfAdult == 0)
                            Price = 0;
                    }
                    //Per Child
                    else if (PkgC == 3)
                    {
                        if (mR.NoOfChild > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfChild.ToString());
                        else if (mR.NoOfChild == 0)
                            Price = 0;
                    }
                    //Per Room
                    else if (PkgC == 4)
                    {
                        Price = OriginPrice;
                    }
                    //Per Child1
                    else if (PkgC == 5)
                    {
                        if (mR.NoOfChild1 > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfChild1.ToString());
                        else if (mR.NoOfChild1 == 0)
                            Price = 0;
                    }
                    //Per Child2
                    else if (PkgC == 6)
                    {
                        if (mR.NoOfChild2 > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfChild2.ToString());
                        else if (mR.NoOfChild2 == 0)
                            Price = 0;
                    }
                    //Per A + C
                    else if (PkgC == 8)
                    {
                        if (mR.NoOfAdult > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfAdult.ToString());
                        if (mR.NoOfChild > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild.ToString()));
                        else if (mR.NoOfAdult == 0 && mR.NoOfChild == 0)
                            Price = 0;
                    }
                    //Per A + C + C1
                    else if (PkgC == 9)
                    {
                        if (mR.NoOfAdult > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfAdult.ToString());
                        if (mR.NoOfChild > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild.ToString()));
                        if (mR.NoOfChild1 > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild1.ToString()));
                        else if (mR.NoOfAdult == 0 && mR.NoOfChild == 0 && mR.NoOfChild1 == 0)
                            Price = 0;
                    }
                    //Per A + C1
                    else if (PkgC == 10)
                    {
                        if (mR.NoOfAdult > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfAdult.ToString());
                        if (mR.NoOfChild1 > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild1.ToString()));
                        else if (mR.NoOfAdult == 0 && mR.NoOfChild1 == 0)
                            Price = 0;
                    }
                    //Per C + C1
                    else if (PkgC == 11)
                    {
                        if (mR.NoOfChild > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfChild.ToString());
                        if (mR.NoOfChild1 > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild1.ToString()));
                        else if (mR.NoOfChild == 0 && mR.NoOfChild1 == 0)
                            Price = 0;
                    }
                    #endregion

                    #region Xác định thời điểm charge 
                    PkgP = 0;
                    switch (dtPkgInc.Rows[m]["PostingRhythmID"].ToString())
                    {
                        case "1"://Everyday
                            {
                                PkgP = 1;
                                break;
                            }
                        case "2"://Date(At date)
                            {
                                PkgP = 2;
                                break;
                            }
                        case "3"://Day(At Day Of use)
                            {
                                PkgP = 3;
                                break;
                            }
                        case "4"://At Check in
                            {
                                PkgP = 4;
                                break;
                            }
                        case "5"://At Check Out
                            {
                                PkgP = 5;
                                break;
                            }
                    }
                    #endregion

                    #region Trừ đi tiền Package có Include trong tiền phòng 
                    //Xác định xem Transaction cùng TaxInclude
                    ////TaxInclude = TextUtils.GetTaxInclude(dtRR.Rows[i]["TransactionCode"].ToString());

                    #region Every day 
                    if (PkgP == 1)
                    {
                        //kiểm tra loại tiền 
                        if (dtRR.Rows[i]["CurrencyID"].ToString() == dtPkgInc.Rows[m]["CurrencyID"].ToString())
                        {
                            AmountBeforTax = 0;
                            AmountAfterTax = 0;
                            //Xác định tiền trước thuế và sau thuế
                            //TextUtils.GetSourceAmount(dtPkgInc.Rows[m]["TransactionCode"].ToString(), Price, ref AmountBeforTax, ref AmountAfterTax);
                            ReservationBO.GetAmountSource(dtPkgInc.Rows[m]["TransactionCode"].ToString(), Price, _RoIsTaxInclude, ref AmountBeforTax, ref AmountAfterTax);

                            if (_RoIsTaxInclude == true)
                                Rate = Rate - AmountAfterTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString());
                            else
                                Rate = Rate - AmountBeforTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString());
                        }
                        else
                        {
                            AmountBeforTax = 0;
                            AmountAfterTax = 0;
                            //Xác định tiền trước thuế và sau thuế
                            //TextUtils.GetSourceAmount(dtPkgInc.Rows[m]["TransactionCode"].ToString(), TextUtils.ExchangeCurrency(mR.ReservationDate, dtPkgInc.Rows[m]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), Price), ref AmountBeforTax, ref AmountAfterTax);
                            ReservationBO.GetAmountSource(dtPkgInc.Rows[m]["TransactionCode"].ToString(), TextUtils.ExchangeCurrency(mR.ReservationDate, dtPkgInc.Rows[m]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), Price), _RoIsTaxInclude, ref AmountBeforTax, ref AmountAfterTax);

                            if (_RoIsTaxInclude == true)
                                Rate = Rate - (AmountAfterTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString()));
                            else
                                Rate = Rate - (AmountBeforTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString()));
                        }
                    }
                    #endregion

                    #region Date(At date) 
                    else if (PkgP == 2)
                    {
                        if (TextUtils.CompareDate(Convert.ToDateTime(dtRR.Rows[i]["RateDate"]), Convert.ToDateTime(dtPkgInc.Rows[m]["PostingDate"])) == 0)
                        {
                            if (dtRR.Rows[i]["CurrencyID"].ToString() == dtPkgInc.Rows[m]["CurrencyID"].ToString())
                            {
                                AmountBeforTax = 0;
                                AmountAfterTax = 0;
                                //Xác định tiền trước thuế và sau thuế
                                //TextUtils.GetSourceAmount(dtPkgInc.Rows[m]["TransactionCode"].ToString(), Price, ref AmountBeforTax, ref AmountAfterTax);
                                ReservationBO.GetAmountSource(dtPkgInc.Rows[m]["TransactionCode"].ToString(), Price, _RoIsTaxInclude, ref AmountBeforTax, ref AmountAfterTax);
                                if (_RoIsTaxInclude == true)
                                    Rate = Rate - (AmountAfterTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString()));
                                else
                                    Rate = Rate - (AmountBeforTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString()));
                            }
                            else
                            {
                                AmountBeforTax = 0;
                                AmountAfterTax = 0;
                                //Xác định tiền trước thuế và sau thuế
                                //TextUtils.GetSourceAmount(dtPkgInc.Rows[m]["TransactionCode"].ToString(), TextUtils.ExchangeCurrency(mR.ReservationDate, dtPkgInc.Rows[m]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), Price), ref AmountBeforTax, ref AmountAfterTax);
                                ReservationBO.GetAmountSource(dtPkgInc.Rows[m]["TransactionCode"].ToString(), TextUtils.ExchangeCurrency(mR.ReservationDate, dtPkgInc.Rows[m]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), Price), _RoIsTaxInclude, ref AmountBeforTax, ref AmountAfterTax);
                                if (_RoIsTaxInclude == true)
                                    Rate = Rate - (AmountAfterTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString()));
                                else
                                    Rate = Rate - (AmountBeforTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString()));

                            }
                        }
                    }
                    #endregion

                    #region Day(At Day Of use) 
                    else if (PkgP == 3)
                    {

                    }
                    #endregion

                    #region At Check in 
                    else if (PkgP == 4)
                    {
                        if (i == 0)
                            if (dtRR.Rows[i]["CurrencyID"].ToString() == dtPkgInc.Rows[m]["CurrencyID"].ToString())
                            {
                                AmountBeforTax = 0;
                                AmountAfterTax = 0;
                                //Xác định tiền trước thuế và sau thuế
                                //TextUtils.GetSourceAmount(dtPkgInc.Rows[m]["TransactionCode"].ToString(), Price, ref AmountBeforTax, ref AmountAfterTax);
                                ReservationBO.GetAmountSource(dtPkgInc.Rows[m]["TransactionCode"].ToString(), Price, _RoIsTaxInclude, ref AmountBeforTax, ref AmountAfterTax);
                                if (_RoIsTaxInclude == true)
                                    Rate = Rate - (AmountAfterTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString()));
                                else
                                    Rate = Rate - (AmountAfterTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString()));
                            }
                            else
                            {
                                AmountBeforTax = 0;
                                AmountAfterTax = 0;
                                //Xác định tiền trước thuế và sau thuế
                                //TextUtils.GetSourceAmount(dtPkgInc.Rows[m]["TransactionCode"].ToString(), TextUtils.ExchangeCurrency(mR.ReservationDate, dtPkgInc.Rows[m]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), Price), ref AmountBeforTax, ref AmountAfterTax);
                                ReservationBO.GetAmountSource(dtPkgInc.Rows[m]["TransactionCode"].ToString(), TextUtils.ExchangeCurrency(mR.ReservationDate, dtPkgInc.Rows[m]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), Price), _RoIsTaxInclude, ref AmountBeforTax, ref AmountAfterTax);
                                if (_RoIsTaxInclude == true)
                                    Rate = Rate - (AmountAfterTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString()));
                                else
                                    Rate = Rate - (AmountBeforTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString()));
                            }
                    }
                    #endregion

                    #region At Check Out 
                    else if (PkgP == 5)
                    {
                        if (i == dtRR.Rows.Count - 1)
                            if (dtRR.Rows[i]["CurrencyID"].ToString() == dtPkgInc.Rows[m]["CurrencyID"].ToString())
                            {
                                AmountBeforTax = 0;
                                AmountAfterTax = 0;
                                //Xác định tiền trước thuế và sau thuế
                                //TextUtils.GetSourceAmount(dtPkgInc.Rows[m]["TransactionCode"].ToString(), Price, ref AmountBeforTax, ref AmountAfterTax);
                                ReservationBO.GetAmountSource(dtPkgInc.Rows[m]["TransactionCode"].ToString(), Price, _RoIsTaxInclude, ref AmountBeforTax, ref AmountAfterTax);
                                if (_RoIsTaxInclude == true)
                                    Rate = Rate - (AmountAfterTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString()));
                                else
                                    Rate = Rate + (AmountBeforTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString()));
                            }
                            else
                            {
                                AmountBeforTax = 0;
                                AmountAfterTax = 0;
                                //Xác định tiền trước thuế và sau thuế
                                //TextUtils.GetSourceAmount(dtPkgInc.Rows[m]["TransactionCode"].ToString(), TextUtils.ExchangeCurrency(mR.ReservationDate, dtPkgInc.Rows[m]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), Price), ref AmountBeforTax, ref AmountAfterTax);
                                ReservationBO.GetAmountSource(dtPkgInc.Rows[m]["TransactionCode"].ToString(), TextUtils.ExchangeCurrency(mR.ReservationDate, dtPkgInc.Rows[m]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), Price), _RoIsTaxInclude, ref AmountBeforTax, ref AmountAfterTax);
                                if (_RoIsTaxInclude == true)
                                    Rate = Rate - (AmountAfterTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString()));
                                else
                                    Rate = Rate - (AmountBeforTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString()));
                            }
                    }
                    #endregion

                    #endregion
                }

                #endregion

                #region 6.5.Update lại RoomRevenue theo từng ngày trong bảng ReservationRate 
                RoomRevenueBeforeTax = 0;
                RoomRevenueAfterTax = 0;
                //Xác định tiền ++, net
                ReservationBO.GetAmountSource(dtRR.Rows[i]["TransactionCode"].ToString(), Rate, _RoIsTaxInclude, ref RoomRevenueBeforeTax, ref RoomRevenueAfterTax);
                //TextUtils.GetSourceAmount(dtRR.Rows[i]["TransactionCode"].ToString(), Rate, ref RoomRevenueBeforeTax, ref RoomRevenueAfterTax);
                //if (RoomRevenueBeforeTax >= 0 && RoomRevenueAfterTax >= 0)
                //{

                ReservationRateModel mRR = (ReservationRateModel)pt.FindByPK("ReservationRate", TextUtils.ToInt(dtRR.Rows[i]["ID"].ToString()));
                if (RoomRevenueBeforeTax >= 0)
                {
                    mRR.RoomRevenueBeforeTax = RoomRevenueBeforeTax;
                    mRR.RoomRevenueAfterTax = RoomRevenueAfterTax;
                }
                else
                {
                    mRR.RoomRevenueBeforeTax = 0;
                    mRR.RoomRevenueAfterTax = 0;
                }
                pt.Update(mRR);
                //}
                #endregion
            }
            #endregion
        }

        public static void GetAmountByCurrency(int ReservationID, int UserID, ProcessTransactions pt)
        {
            //Khai báo biến
            DataTable _dtRC = null;
            Decimal AmountBefore = 0;
            Decimal AmountAfter = 0;
            //Lấy Model của phiếu đặt phòng
            ReservationModel mR = (ReservationModel)pt.FindByPK("Reservation", ReservationID);
            //Xác định tiền của RC, FC, PK
            for (int i = 1; i < 4; i++)
            {
                //Tính tổng tiền trên form đặt phòng                 
                DataTable tdR = pt.getTable("spReservationAmountByCurrency", "tdR",
                            new SqlParameter("@ReservationID", ReservationID),
                            new SqlParameter("@Type", i));
                //Xác định RC
                if (i == 1)
                    _dtRC = tdR;

                #region Xác định tổng tiền của Room 
                if (i == 1 && tdR.Rows.Count > 0)
                {
                    for (int r = 0; r < tdR.Rows.Count; r++)
                    {
                        //Clear
                        Decimal RateBeforeTax = 0;
                        Decimal RateAfterTax = 0;
                        //--
                        if (mR.NoOfRoom > 0)
                        {
                            RateBeforeTax = TextUtils.ToDecimal(tdR.Rows[r]["Rate"].ToString()) * mR.NoOfRoom;

                            RateAfterTax = TextUtils.ToDecimal(tdR.Rows[r]["RateAfterTax"].ToString()) * mR.NoOfRoom;
                        }
                        else
                        {
                            RateBeforeTax = TextUtils.ToDecimal(tdR.Rows[r]["Rate"].ToString());
                            RateAfterTax = TextUtils.ToDecimal(tdR.Rows[r]["RateAfterTax"].ToString());
                        }
                        //Discount
                        RateBeforeTax = RateBeforeTax - ((RateBeforeTax * TextUtils.ToDecimal(tdR.Rows[r]["DiscountRate"].ToString())) / 100);

                        RateAfterTax = RateAfterTax - ((RateAfterTax * TextUtils.ToDecimal(tdR.Rows[r]["DiscountRate"].ToString())) / 100)
                                    - (TextUtils.ToDecimal(tdR.Rows[r]["DiscountAmount"].ToString()) * mR.NoOfRoom);

                        //Nếu có CK (Tiền) tính lại số tiền CK ++ theo net
                        decimal d1 = 0; decimal d2 = 0;
                        if (TextUtils.ToDecimal(tdR.Rows[r]["DiscountAmount"].ToString()) > 0 && RateAfterTax > 0)
                        {
                            //Gen giá sau thuế
                            ReservationBO.GetAmountSource(tdR.Rows[r]["TransactionCode"].ToString(), RateAfterTax, true, ref d1, ref d2);
                            RateBeforeTax = d1;
                        }
                        //Ghi dữ liệu vào bảng ReservationAmountByCurrency                      
                        ReservationBO.GenReservationAmountByCurrency(ReservationID, int.Parse(mR.ConfirmationNo.ToString()), tdR.Rows[r]["CurrencyID"].ToString(), RateBeforeTax, RateAfterTax, UserID, pt);
                    }
                }
                #endregion

                #region Xác định tổng tiền của Package 
                if (i == 2 && tdR.Rows.Count > 0)
                {
                    for (int p = 0; p < tdR.Rows.Count; p++)
                    {
                        AmountBefore = 0;
                        AmountAfter = 0;

                        #region CanculationRule && PostingRhythm 
                        Decimal Price = 0;
                        Decimal OriginPrice = 0;
                        //* NoOfRoom                        
                        if (bool.Parse(tdR.Rows[p]["IsTaxInclude"].ToString()) == false)
                        {
                            if (mR.NoOfRoom > 0)
                                OriginPrice = Decimal.Parse(tdR.Rows[p]["Price"].ToString()) * mR.NoOfRoom;
                            else
                                OriginPrice = Decimal.Parse(tdR.Rows[p]["Price"].ToString());
                        }
                        else
                        {
                            if (mR.NoOfRoom > 0)
                                OriginPrice = Decimal.Parse(tdR.Rows[p]["PriceAfterTax"].ToString()) * mR.NoOfRoom;
                            else
                                OriginPrice = Decimal.Parse(tdR.Rows[p]["PriceAfterTax"].ToString());
                        }
                        //1:Per/Person(từng người trong phòng)
                        if (int.Parse(tdR.Rows[p]["CalculationRuleID"].ToString()) == 1)
                        {
                            if (int.Parse(tdR.Rows[p]["NoOfAdult"].ToString()) > 0)
                                Price = OriginPrice * Decimal.Parse(tdR.Rows[p]["NoOfAdult"].ToString());
                            if (int.Parse(tdR.Rows[p]["NoOfChild"].ToString()) > 0)
                                Price = Price + (OriginPrice * Decimal.Parse(tdR.Rows[p]["NoOfChild"].ToString()));
                            if (int.Parse(tdR.Rows[p]["NoOfChild1"].ToString()) > 0)
                                Price = Price + (OriginPrice * Decimal.Parse(tdR.Rows[p]["NoOfChild1"].ToString()));
                            if (int.Parse(tdR.Rows[p]["NoOfChild2"].ToString()) > 0)
                                Price = Price + (OriginPrice * Decimal.Parse(tdR.Rows[p]["NoOfChild2"].ToString()));
                            else if (int.Parse(tdR.Rows[p]["NoOfAdult"].ToString()) == 0 && int.Parse(tdR.Rows[p]["NoOfChild"].ToString()) == 0 && int.Parse(tdR.Rows[p]["NoOfChild1"].ToString()) == 0 && int.Parse(tdR.Rows[p]["NoOfChild2"].ToString()) == 0)
                                Price = 0;
                        }
                        //2: Per Adult
                        else if (int.Parse(tdR.Rows[p]["CalculationRuleID"].ToString()) == 2)
                        {
                            if (int.Parse(tdR.Rows[p]["NoOfAdult"].ToString()) > 0)
                                Price = OriginPrice * Decimal.Parse(tdR.Rows[p]["NoOfAdult"].ToString());
                            else if (int.Parse(tdR.Rows[p]["NoOfAdult"].ToString()) == 0)
                                Price = 0;
                        }
                        //3:Per Child
                        else if (int.Parse(tdR.Rows[p]["CalculationRuleID"].ToString()) == 3)
                        {
                            if (int.Parse(tdR.Rows[p]["NoOfChild"].ToString()) > 0)
                                Price = OriginPrice * Decimal.Parse(tdR.Rows[p]["NoOfChild"].ToString());
                            else if (int.Parse(tdR.Rows[p]["NoOfChild"].ToString()) == 0)
                                Price = 0;
                        }
                        //4: Per Room
                        else if (int.Parse(tdR.Rows[p]["CalculationRuleID"].ToString()) == 4)
                            Price = OriginPrice;
                        //5:Per Child1
                        else if (int.Parse(tdR.Rows[p]["CalculationRuleID"].ToString()) == 5)
                        {
                            if (int.Parse(tdR.Rows[p]["NoOfChild1"].ToString()) > 0)
                                Price = OriginPrice * Decimal.Parse(tdR.Rows[p]["NoOfChild1"].ToString());
                            else if (int.Parse(tdR.Rows[p]["NoOfChild1"].ToString()) == 0)
                                Price = 0;
                        }
                        //6:Per Child2
                        else if (int.Parse(tdR.Rows[p]["CalculationRuleID"].ToString()) == 6)
                        {
                            if (int.Parse(tdR.Rows[p]["NoOfChild2"].ToString()) > 0)
                                Price = OriginPrice * Decimal.Parse(tdR.Rows[p]["NoOfChild2"].ToString());
                            else if (int.Parse(tdR.Rows[p]["NoOfChild2"].ToString()) == 0)
                                Price = 0;
                        }
                        //8:Per A + C
                        else if (int.Parse(tdR.Rows[p]["CalculationRuleID"].ToString()) == 8)
                        {
                            if (int.Parse(tdR.Rows[p]["NoOfAdult"].ToString()) > 0)
                                Price = OriginPrice * Decimal.Parse(tdR.Rows[p]["NoOfAdult"].ToString());
                            if (int.Parse(tdR.Rows[p]["NoOfChild"].ToString()) > 0)
                                Price = Price + (OriginPrice * Decimal.Parse(tdR.Rows[p]["NoOfChild"].ToString()));
                            else if (int.Parse(tdR.Rows[p]["NoOfAdult"].ToString()) == 0 && int.Parse(tdR.Rows[p]["NoOfChild"].ToString()) == 0)
                                Price = 0;
                        }
                        //9:Per A + C + C1
                        else if (int.Parse(tdR.Rows[p]["CalculationRuleID"].ToString()) == 9)
                        {
                            if (int.Parse(tdR.Rows[p]["NoOfAdult"].ToString()) > 0)
                                Price = OriginPrice * Decimal.Parse(tdR.Rows[p]["NoOfAdult"].ToString());
                            if (int.Parse(tdR.Rows[p]["NoOfChild"].ToString()) > 0)
                                Price = Price + (OriginPrice * Decimal.Parse(tdR.Rows[p]["NoOfChild"].ToString()));
                            if (int.Parse(tdR.Rows[p]["NoOfChild1"].ToString()) > 0)
                                Price = Price + (OriginPrice * Decimal.Parse(tdR.Rows[p]["NoOfChild1"].ToString()));
                            else if (int.Parse(tdR.Rows[p]["NoOfAdult"].ToString()) == 0 && int.Parse(tdR.Rows[p]["NoOfChild"].ToString()) == 0 && int.Parse(tdR.Rows[p]["NoOfChild1"].ToString()) == 0)
                                Price = 0;
                        }
                        //10:Per A + C1
                        else if (int.Parse(tdR.Rows[p]["CalculationRuleID"].ToString()) == 10)
                        {
                            if (int.Parse(tdR.Rows[p]["NoOfAdult"].ToString()) > 0)
                                Price = OriginPrice * Decimal.Parse(tdR.Rows[p]["NoOfAdult"].ToString());
                            if (int.Parse(tdR.Rows[p]["NoOfChild1"].ToString()) > 0)
                                Price = Price + (OriginPrice * Decimal.Parse(tdR.Rows[p]["NoOfChild1"].ToString()));
                            else if (int.Parse(tdR.Rows[p]["NoOfAdult"].ToString()) == 0 && int.Parse(tdR.Rows[p]["NoOfChild1"].ToString()) == 0)
                                Price = 0;
                        }
                        //11:Per C + C1
                        else if (int.Parse(tdR.Rows[p]["CalculationRuleID"].ToString()) == 11)
                        {
                            if (int.Parse(tdR.Rows[p]["NoOfChild"].ToString()) > 0)
                                Price = OriginPrice * Decimal.Parse(tdR.Rows[p]["NoOfChild"].ToString());
                            if (int.Parse(tdR.Rows[p]["NoOfChild1"].ToString()) > 0)
                                Price = Price + (OriginPrice * Decimal.Parse(tdR.Rows[p]["NoOfChild1"].ToString()));
                            else if (int.Parse(tdR.Rows[p]["NoOfChild"].ToString()) == 0 && int.Parse(tdR.Rows[p]["NoOfChild1"].ToString()) == 0)
                                Price = 0;
                        }
                        //1:Everyday
                        if (int.Parse(tdR.Rows[p]["PostingRhythmID"].ToString()) == 1)
                            Price = Price * Decimal.Parse(tdR.Rows[p]["Night"].ToString());
                        //3:Day(At Day Of use);
                        else if (int.Parse(tdR.Rows[p]["PostingRhythmID"].ToString()) == 3)
                        {
                            string _Postingday = tdR.Rows[p]["PostingDay"].ToString();
                            string[] _arrPD = _Postingday.Split(',');
                            int _Count = 0;
                            for (int _c = 0; _c < _arrPD.Length; _c++)
                            {
                                if (_arrPD[_c] != "")
                                    _Count = _Count + 1;
                            }
                            if (_Count != 0)
                                Price = Price * Decimal.Parse(_Count.ToString());
                        }
                        //2:Date(At date); 4:At Check in; 5:At Check Out
                        else
                            Price = Price;

                        #endregion

                        //Nhân với số lượng
                        Price = Price * Decimal.Parse(tdR.Rows[p]["Quantity"].ToString());

                        //Discount CSS 08.07.2011
                        ////if (_dtRC.Rows.Count > 0 && Price > 0)
                        ////{
                        ////    Price = Price - ((Price * TextUtils.ToDecimal(_dtRC.Rows[0]["DiscountRate"].ToString())) / 100)
                        ////                - (TextUtils.ToDecimal(_dtRC.Rows[0]["DiscountAmount"].ToString()) * mR.NoOfRoom);
                        ////}
                        ////else if (_dtRC.Rows.Count <= 0 && Price > 0)
                        ////{
                        ////    Price = Price - ((Price * mR.DiscountRate) / 100)
                        ////                - (mR.DiscountAmount * mR.NoOfRoom);
                        ////}

                        if (_dtRC.Rows.Count > 0 && Price > 0)
                        {
                            Price = Price - ((Price * TextUtils.ToDecimal(_dtRC.Rows[0]["DiscountRate"].ToString())) / 100);
                        }
                        else if (_dtRC.Rows.Count <= 0 && Price > 0)
                        {
                            Price = Price - ((Price * mR.DiscountRate) / 100);
                        }

                        //Co gia moi cho ghi du lieu
                        if (Price != 0)
                        {
                            //Xác định Amount ++ , net - Sửa ngày 05.11.10 theo cấu trúc mới -->CSS
                            ReservationBO.GetAmountSource(tdR.Rows[p]["TransactionCode"].ToString(), Price, bool.Parse(tdR.Rows[p]["IsTaxInclude"].ToString()), ref AmountBefore, ref AmountAfter);
                            //Ghi dữ liệu vào bảng ReservationAmountByCurrency
                            ReservationBO.GenReservationAmountByCurrency(ReservationID, int.Parse(mR.ConfirmationNo.ToString()), tdR.Rows[p]["CurrencyID"].ToString(), AmountBefore, AmountAfter, UserID, pt);
                        }
                    }
                }
                #endregion

                #region Xác định tổng tiền của FixedCharge 
                if (i == 3 && tdR.Rows.Count > 0)
                {
                    for (int f = 0; f < tdR.Rows.Count; f++)
                    {
                        AmountBefore = 0;
                        AmountAfter = 0;
                        Decimal Price = 0;

                        if (bool.Parse(tdR.Rows[f]["IsTaxInclude"].ToString()) == false)
                            Price = Decimal.Parse(tdR.Rows[f]["Price"].ToString());
                        else
                            Price = Decimal.Parse(tdR.Rows[f]["AmountAfterTax"].ToString());

                        //1:Everyday
                        if (int.Parse(tdR.Rows[f]["PostingRhythmID"].ToString()) == 1)
                            Price = Price * Decimal.Parse(tdR.Rows[f]["Night"].ToString());
                        //3:Day(At Day Of use);
                        else if (int.Parse(tdR.Rows[f]["PostingRhythmID"].ToString()) == 3)
                        {
                            string _Postingday = tdR.Rows[f]["PostingDay"].ToString();
                            string[] _arrPD = _Postingday.Split(',');
                            int _Count = 0;
                            for (int _c = 0; _c < _arrPD.Length; _c++)
                            {
                                if (_arrPD[_c] != "")
                                    _Count = _Count + 1;
                            }
                            if (_Count != 0)
                                Price = Price * Decimal.Parse(_Count.ToString());
                        }
                        //2:Date(At date); 4:At Check in; 5:At Check Out
                        else
                            Price = Price;

                        //Nhân với số lượng 
                        Price = Price * Decimal.Parse(tdR.Rows[f]["Quantity"].ToString());

                        //Co gia moi cho ghi du lieu
                        if (Price != 0)
                        {
                            //Xác định Amount ++ , net 
                            ReservationBO.GetAmountSource(tdR.Rows[f]["TransactionCode"].ToString(), Price, bool.Parse(tdR.Rows[f]["IsTaxInclude"].ToString()), ref AmountBefore, ref AmountAfter);
                            //Generates.GetSourceAmount(tdR.Rows[f]["TransactionCode"].ToString(), Price, ref AmountBefore, ref AmountAfter);

                            //Ghi dữ liệu vào bảng ReservationAmountByCurrency
                            ReservationBO.GenReservationAmountByCurrency(ReservationID, TextUtils.ToInt(mR.ConfirmationNo), tdR.Rows[f]["CurrencyID"].ToString(), AmountBefore, AmountAfter, UserID, pt);
                        }
                    }
                }
                #endregion

            }
        }
        public static void GenReservationAmountByCurrency(int ReservationID, int ConfirmationNo, string CurrencyID, Decimal AmountBeforTax, Decimal AmountAfterTax, int UserID, ProcessTransactions pt)
        {
            //Xác định xem đã tồn tại chưa 
            Expression expRA = new Expression("ReservationID", ReservationID, "=");
            ArrayList arrRA = pt.FindByExpression("ReservationAmountByCurrency", expRA);
            //Trường hợp Chưa có Insert mới
            if (arrRA.Count == 0)
            {
                ReservationAmountByCurrencyModel mRA = new ReservationAmountByCurrencyModel();
                mRA.ReservationID = ReservationID;
                mRA.ConfirmationNo = ConfirmationNo;
                mRA.CurrencyID = CurrencyID;
                mRA.AmountBeforTax = AmountBeforTax;
                mRA.AmountAfterTax = AmountAfterTax;
                mRA.UserInsertID = UserID;
                mRA.CreateDate = TextUtils.GetBusinessDate();
                mRA.UserUpdateID = UserID;
                mRA.UpdateDate = TextUtils.GetBusinessDate();
                pt.Insert(mRA);
            }
            //Đã tồn tại 
            else if (arrRA.Count > 0)
            {
                //Tìm kiếm xem loại Curr đã tồn tại trong bảng ReservationAmountByCurrency chưa
                Expression expRA1 = new Expression("ReservationID", ReservationID, "=");
                expRA1 = expRA1.And(new Expression("CurrencyID", CurrencyID, "="));
                ArrayList arrRA1 = pt.FindByExpression("ReservationAmountByCurrency", expRA1);
                //Insert
                if (arrRA1.Count == 0)
                {
                    ReservationAmountByCurrencyModel mRA = new ReservationAmountByCurrencyModel();
                    mRA.ReservationID = ReservationID;
                    mRA.ConfirmationNo = ConfirmationNo;
                    mRA.CurrencyID = CurrencyID;
                    mRA.AmountBeforTax = AmountBeforTax;
                    mRA.AmountAfterTax = AmountAfterTax;
                    mRA.UserInsertID = UserID;
                    mRA.CreateDate = TextUtils.GetBusinessDate();
                    mRA.UserUpdateID = UserID;
                    mRA.UpdateDate = TextUtils.GetBusinessDate();
                    pt.Insert(mRA);
                }
                //Update
                else
                {
                    ReservationAmountByCurrencyModel mRA = (ReservationAmountByCurrencyModel)pt.FindByPK("ReservationAmountByCurrency", ((ReservationAmountByCurrencyModel)arrRA1[0]).ID);
                    mRA.ID = ((ReservationAmountByCurrencyModel)arrRA1[0]).ID;
                    mRA.ConfirmationNo = ConfirmationNo;
                    mRA.AmountBeforTax = AmountBeforTax + ((ReservationAmountByCurrencyModel)arrRA1[0]).AmountBeforTax;
                    mRA.AmountAfterTax = AmountAfterTax + ((ReservationAmountByCurrencyModel)arrRA1[0]).AmountAfterTax;
                    mRA.UpdateDate = TextUtils.GetBusinessDate();
                    mRA.UserUpdateID = UserID;
                    pt.Update(mRA);
                }
            }
        }

        public static void GetAmountSource(string TransactionCode, decimal InputAmount, bool TaxInclude, ref decimal Amount, ref decimal AmountNet)
        {
            try
            {
                #region Lấy danh sách của Generate
                ArrayList arr = GenerateTransactionBO.Instance.FindByAttribute("TransactionCode", TransactionCode);
                #endregion

                #region Nếu có tồn tại trong generate
                if (arr.Count > 0)
                {
                    #region Nếu giá nhập vào là giá đã bao gồm SVC+VAT
                    if (TaxInclude == true)
                    {
                        Amount = GetAmount(arr, InputAmount);
                        AmountNet = InputAmount;
                    }
                    #endregion

                    #region Nếu giá đưa vào là giá ++
                    else
                    {
                        // Khai báo biến
                        GenerateTransactionModel mGT;
                        decimal s1 = 0, s2 = 0, s3 = 0;
                        decimal CurrentAmount = 0;
                        // Thực hiện
                        for (int i = 0; i < arr.Count; i++)
                        {
                            // Đổ dữ liệu vào model
                            mGT = (GenerateTransactionModel)arr[i];
                            // Lấy ra current amount
                            if (mGT.BaseAmount == 0)
                                CurrentAmount = (mGT.Percentage * InputAmount) / 100;
                            else if (mGT.BaseAmount == 1)
                                CurrentAmount = (mGT.Percentage * s1) / 100;
                            else if (mGT.BaseAmount == 2)
                                CurrentAmount = (mGT.Percentage * s2) / 100;
                            else
                                CurrentAmount = (mGT.Percentage * s3) / 100;
                            // Nhặt dữ liệu vào s1,s2,s3
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
                            // Tính giá sau thuế
                            AmountNet = AmountNet + CurrentAmount;
                        }
                        // Lấy giá trước thuế
                        Amount = InputAmount;
                    }
                    #endregion
                }
                #endregion

                #region Nếu không tồn tại trong generate
                else
                {
                    Amount = InputAmount;
                    AmountNet = InputAmount;
                }
                #endregion
            }
            catch (Exception ex)
            {

            }
        }
        protected static decimal GetAmount(ArrayList arr, decimal InputAmount)
        {
            #region Khai báo biến

            string s1 = "B0", s2 = "B0", s3 = "B0";
            string BaseAmount = "B";
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
        public static decimal GetNumber(string InputAmount)
        {
            return Convert.ToDecimal(InputAmount.Trim('B'));
        }
        #region 2.Routing và Folio 

        #region 2.1 Kiểm tra Routing để tạo Folio 
        /// <summary>
        /// Kiểm tra xem trong bảng Routing có cửa sổ Windown No thứ 2,3...8 không? Nếu có sẽ tạo Folio
        /// -- CSS, 25/12/2009
        /// </summary>
        /// <param name="FromReservationID"></param>
        /// <param name="ToReservationID"></param>
        /// <param name="WindownNo">Default ==1</param>
        /// <param name="pt"></param>
        public static ArrayList CheckRouting(int FromReservationID, int ToReservationID, string ConfirmationNo, ProcessTransactions pt)
        {

            //Tim kiem Routing
            Expression expR = new Expression("FromReservationID", FromReservationID, "=");
            expR = expR.Or(new Expression("ToFolioNo", "-1", "="));
            expR = expR.And(new Expression("ConfirmationNo", ConfirmationNo, "="));
            ArrayList arrR = pt.FindByExpression("Routing", expR);
            //Kiểm tra điều kiện để trả về giá trị              
            if (arrR.Count == 0)
                return null;
            else
                return arrR;
            //return ((RoutingModel)arrR[0]).ID;

        }
        //C2 Không dùng Transaction
        public static ArrayList CheckRouting(int FromReservationID, int ToReservationID)
        {
            //Tim kiem Routing
            Expression expR = new Expression("FromReservationID", FromReservationID, "=");
            expR = expR.And(new Expression("ToReservationID", ToReservationID, "="));
            ArrayList arrR = RoutingBO.Instance.FindByExpression(expR);
            //Kiểm tra điều kiện để trả về giá trị
            if (arrR.Count == 0)
                return null;
            else
                return arrR;
        }
        //Kiểm tra xe có Routing MasterFolio không ( Dùng cho tách BK)
        public static void CheckRouting(string ConfirmationNo, ref bool _Routing, ProcessTransactions pt)
        {
            //Tim kiem Routing
            Expression expR = new Expression("ConfirmationNo", ConfirmationNo, "=");
            expR = expR.And(new Expression("IsMasterFolio", "1", "="));
            ArrayList arrR = pt.FindByExpression("Routing", expR);
            //Kiểm tra điều kiện để trả về giá trị
            if (arrR.Count == 0)
                _Routing = false;
            else
            {
                _Routing = true;
            }
        }
        //No Transaction
        public static void CheckRouting(string ConfirmationNo, ref bool _Routing)
        {
            //Tim kiem Routing
            Expression expR = new Expression("ConfirmationNo", ConfirmationNo, "=");
            expR = expR.And(new Expression("IsMasterFolio", "1", "="));
            ArrayList arrR = RoutingBO.Instance.FindByExpression(expR);
            //Kiểm tra điều kiện để trả về giá trị
            if (arrR.Count == 0)
                _Routing = false;
            else
                _Routing = true;
        }
        #endregion

        #region 2.2 Lấy RoutingID nếu đã tồn tại
        /// <summary>
        /// Lấy RoutingID nếu đã tồn tại
        /// -- CSS, 04/12/2009
        /// </summary>
        /// <param name="ReservationID"></param>
        /// <param name="WindowNo"></param>
        /// <param name="pt"></param>
        /// <returns></returns>
        public static int GetRoutingID(int ReservationID, int WindowNo, ProcessTransactions pt)
        {
            //Tim kiem Folio
            Expression expR = new Expression("FromReservationID", ReservationID, "=");
            expR = expR.And(new Expression("ToFolioNo", WindowNo, "="));
            ArrayList arrR = pt.FindByExpression("Routing", expR);
            //Kiem tra dieu kien va tra ve ket qua
            if (arrR.Count == 0)
                return 0;
            else
                return ((RoutingModel)arrR[0]).ID;
        }
        #endregion

        #region 2.3 Tạo Routing
        /// <summary>
        /// Tạo Routing
        /// -- CSS, 04/12/2009
        /// </summary>
        /// <param name="ReservationID"></param>
        /// <returns></returns>
        public static int CreateRouting(int ReservationID, int WindowNo, DateTime ArrivalDate, DateTime DepartureDate, int ProfileIndividualID, ProcessTransactions pt)
        {
            RoutingModel mRo = new RoutingModel();
            mRo.Type = 0;
            mRo.FromReservationID = ReservationID;
            mRo.ToReservationID = ReservationID;
            mRo.ToFolioNo = WindowNo;
            mRo.ToRoomID = 0;
            mRo.ProfileID = ProfileIndividualID;
            if (ProfileIndividualID > 0)
                mRo.AccountName = ((ProfileModel)pt.FindByPK("Profile", ProfileIndividualID)).Account;
            else
                mRo.AccountName = "";
            mRo.TransactionCodes = "";
            mRo.Limit = 0;
            mRo.FromDate = ArrivalDate;
            mRo.ToDate = DepartureDate;
            mRo.EntireDate = true;
            mRo.IsDefault = true;
            mRo.IsMasterFolio = false;
            mRo.ID = (int)pt.Insert(mRo);

            #region Ghi dữ liệu vào History
            //ClassReservation.InsertHistory("INSERT", mRo,pt);
            #endregion

            return mRo.ID;
        }
        //C2 Dùng cho Quick CheckIn
        public static int CreateRouting(int FromReservationID, int ToReservationID, int WindowNo,
        DateTime ArrivalDate, DateTime DepartureDate, int ProfileID, string TransactionCode, ProcessTransactions pt)
        {
            RoutingModel mRo = new RoutingModel();
            mRo.Type = 0;
            mRo.FromReservationID = FromReservationID;
            mRo.ToReservationID = ToReservationID;
            mRo.ToFolioNo = WindowNo;
            mRo.ToRoomID = 0;
            mRo.ProfileID = ProfileID;
            if (ProfileID > 0)
                mRo.AccountName = ((ProfileModel)pt.FindByPK("Profile", ProfileID)).Account;
            else
                mRo.AccountName = "";
            mRo.TransactionCodes = TransactionCode;
            mRo.Limit = 0;
            mRo.FromDate = ArrivalDate;
            mRo.ToDate = DepartureDate;
            mRo.EntireDate = true;
            mRo.IsDefault = true;
            mRo.IsMasterFolio = false;
            return (int)pt.Insert(mRo);
        }
        #endregion

        #region 2.4 Lấy ra ID của Folio nếu đã tồn tại
        /// <summary>
        /// Lấy ra ID của Folio nếu đã tồn tại
        /// -- CSS, 25/12/2009
        /// </summary>
        /// <param name="ReservationID"></param>
        /// <param name="WindowNo"></param>
        /// <param name="ProfileID"></param>
        /// <param name="pt"></param>
        /// <returns></returns>
        public static int GetFolioID(int ReservationID, int WindowNo, string ConfirmationNo, ProcessTransactions pt)
        {
            //Tim kiem Folio
            Expression exp = new Expression("ReservationID", ReservationID, "=");
            exp = exp.And(new Expression("FolioNo", WindowNo, "="));
            exp = exp.And(new Expression("ConfirmationNo", ConfirmationNo, "="));
            exp = exp.And(new Expression("Status", 0, "="));
            ArrayList arr = pt.FindByExpression("Folio", exp);
            //Kiem tra dieu kien va tra ve ket qua
            if (arr.Count == 0)
                return 0;
            else
                return ((FolioModel)arr[0]).ID;
        }
        //C2 Không dùng Transaction
        public static int GetFolioID(int ReservationID, int WindowNo, string ConfirmationNo)
        {
            try
            {
                //Tim kiem Folio
                Expression exp = new Expression("ReservationID", ReservationID, "=");
                exp = exp.And(new Expression("FolioNo", WindowNo, "="));
                exp = exp.And(new Expression("ConfirmationNo", ConfirmationNo, "="));
                exp = exp.And(new Expression("Status", 0, "="));
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
        //C3 Nhặt ra Folio Master
        public static int GetFolioMasterID(int ReservationID, int IsMasterFolio, ProcessTransactions pt)
        {
            //Lấy ra ConfNo
            ReservationModel mR = (ReservationModel)pt.FindByPK("Reservation", ReservationID);
            //Tim kiem Folio                
            Expression exp = new Expression("ConfirmationNo", mR.ConfirmationNo, "=");
            exp = exp.And(new Expression("IsMasterFolio", IsMasterFolio, "="));
            ArrayList arr = pt.FindByExpression("Folio", exp);
            //Kiem tra dieu kien va tra ve ket qua
            if (arr.Count == 0)
                return 0;
            else
            {
                return ((FolioModel)arr[0]).ID;
            }
        }
        #endregion

        #region 2.5 Tạo Folio
        /// <summary>
        /// Tạo Folio
        /// -- CSS, 25/12/2009
        /// </summary>
        /// <param name="ReservationID">ID của Reservation</param>
        /// <param name="WindowNo">Số thứ tự của Window Folio </param>
        /// <returns></returns>
        //public static int CreateFolio(int ReservationID, int WindowNo, int ProfileID, ProcessTransaction pt)
        public static int CreateFolio(int RoutingID, string ConfirmationNo, ProcessTransactions pt)
        {

            RoutingModel mOR = (RoutingModel)pt.FindByPK("Routing", RoutingID);

            FolioModel mF = new FolioModel();
            mF.FolioDate = TextUtils.GetBusinessDate();
            mF.FolioNo = mOR.ToFolioNo;
            mF.ReservationID = mOR.ToReservationID;
            //mF.RoomID = mOR.ToRoomID;
            //if (mOR.ToRoomID != 0)
            //    mF.RoomNo = ((RoomModel)pt.FindByPK("Room", mOR.ToRoomID)).RoomNo;
            //else
            //    mF.RoomNo = "";
            mF.ProfileID = mOR.ProfileID;
            if (mOR.ProfileID > 0)
                mF.AccountName = ((ProfileModel)pt.FindByPK("Profile", mOR.ProfileID)).Account;
            else
                mF.AccountName = "";
            mF.Status = false;
            if (mOR.IsMasterFolio == false)
                mF.IsMasterFolio = false;
            else
                mF.IsMasterFolio = true;
            mF.ConfirmationNo = ConfirmationNo;//((ReservationModel)pt.FindByPK("Reservation",mOR.FromReservationID)).ConfirmationNo;

            mF.UserInsertID = Global.UserID;
            mF.CreateDate = TextUtils.GetSystemDate();
            mF.UserUpdateID = Global.UserID;
            mF.UpdateDate = mF.CreateDate;

            return (int)pt.Insert(mF);

            #region Code cũ
            //FolioModel mF = new FolioModel();
            //mF.FolioDate = TextUtils.GetBusinessDate();
            //mF.FolioNo = WindowNo;
            //mF.ReservationID = ReservationID;
            //mF.RoomID = 0;
            //mF.RoomNo = "";
            //mF.ProfileID = ProfileID;
            //if (ProfileID > 0)
            //    mF.AccountName = ((ProfileModel)pt.FindByPK("Profile",ProfileID)).Account;
            //else
            //    mF.AccountName = "";
            //mF.Status = false;
            //mF.FolioMaster = false;

            //mF.UserInsertID = Global.UserID;
            //mF.CreateDate =TextUtils.GetSystemDate();
            //mF.UserUpdateID = Global.UserID;
            //mF.UpdateDate = mF.CreateDate;

            //return (int)pt.Insert(mF);
            #endregion

        }
        //C2 Không dùng Transaction
        public static int CreateFolio(int RoutingID)
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

                mF.UserInsertID = Global.UserID;
                mF.CreateDate = TextUtils.GetSystemDate();
                mF.UserUpdateID = Global.UserID;
                mF.UpdateDate = mF.CreateDate;

                return (int)FolioBO.Instance.Insert(mF);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        //C3 Không có Routing
        public static int CreateFolio(int ReservationID, int FolioNo)
        {
            try
            {
                ReservationModel mOR = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(ReservationID);

                FolioModel mF = new FolioModel();
                mF.FolioDate = TextUtils.GetBusinessDate();
                mF.FolioNo = FolioNo;
                mF.ProfileID = mOR.ProfileIndividualId;
                mF.AccountName = mOR.LastName;
                //mF.RoomID = mOR.RoomID;
                //mF.RoomNo = mOR.RoomNo;
                mF.ReservationID = ReservationID;
                mF.Status = false;
                mF.IsMasterFolio = false;
                mF.ConfirmationNo = mOR.ConfirmationNo;
                mF.UserInsertID = Global.UserID;
                mF.CreateDate = TextUtils.GetSystemDate();
                mF.UserUpdateID = Global.UserID;
                mF.UpdateDate = mF.CreateDate;

                return (int)FolioBO.Instance.Insert(mF);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        //C4 Không có Routing, Transaction
        public static int CreateFolio(int ReservationID, int FolioNo, ProcessTransactions pt)
        {

            ReservationModel mOR = (ReservationModel)pt.FindByPK("Reservation", ReservationID);

            FolioModel mF = new FolioModel();
            mF.FolioDate = TextUtils.GetBusinessDate();
            mF.FolioNo = FolioNo;
            mF.ProfileID = mOR.ProfileIndividualId;
            mF.AccountName = mOR.LastName;
            //mF.RoomID = mOR.RoomID;
            //mF.RoomNo = mOR.RoomNo;
            mF.ReservationID = ReservationID;
            mF.Status = false;
            mF.IsMasterFolio = false;
            mF.ConfirmationNo = mOR.ConfirmationNo;
            mF.UserInsertID = Global.UserID;
            mF.CreateDate = TextUtils.GetSystemDate();
            mF.UserUpdateID = Global.UserID;
            mF.UpdateDate = mF.CreateDate;

            return (int)pt.Insert(mF);

        }

        /// <summary>
        /// Tạo Folio cho khách trường hợp ko có Routing
        /// -- CSS, 24/03/2010
        /// </summary>
        /// <param name="ReservationID"></param>
        /// <param name="pt"></param>
        /// <returns></returns>
        public static int CreateFolioNoRouting(int ReservationID, int _WinNo, ProcessTransactions pt)
        {
            ReservationModel mOR = (ReservationModel)pt.FindByPK("Reservation", ReservationID);
            FolioModel mF = new FolioModel();
            mF.FolioDate = TextUtils.GetBusinessDate();
            mF.FolioNo = _WinNo;//1;
            mF.ReservationID = ReservationID;
            mF.ProfileID = mOR.ProfileIndividualId;
            if (_WinNo < 0)
            {
                if (mOR.ProfileAgentId > 0)
                    mF.ProfileID = mOR.ProfileAgentId;
                else
                    mF.ProfileID = mOR.ProfileCompanyId;
            }
            mF.AccountName = mOR.LastName;
            if (mF.AccountName == "")
                if (mF.ProfileID > 0)
                    mF.AccountName = ((ProfileModel)pt.FindByPK("Profile", mF.ProfileID)).Account;
            mF.Status = false;
            if (_WinNo == -1)
                mF.IsMasterFolio = true;
            else
                mF.IsMasterFolio = false;
            mF.ConfirmationNo = mOR.ConfirmationNo;
            mF.UserInsertID = Global.UserID;
            mF.CreateDate = TextUtils.GetSystemDate();
            mF.UserUpdateID = Global.UserID;
            mF.UpdateDate = mF.CreateDate;

            return (int)pt.Insert(mF);
        }

        /// <summary>
        /// Tạo FolioMaster
        /// -- CSS, 23/03/2010
        /// </summary>
        /// <param name="ReservationID"></param>
        /// <param name="RoomID"></param>
        /// <param name="RoomNo"></param>
        /// <param name="WindowNo"></param>
        /// <param name="pt"></param>
        /// <returns></returns>
        public static int CreateFolioMasterID(int ReservationID, int RoomID, string RoomNo, ProcessTransactions pt)
        {
            ReservationModel mOR = (ReservationModel)pt.FindByPK("Reservation", ReservationID);
            FolioModel mF = new FolioModel();
            mF.FolioDate = TextUtils.GetBusinessDate();
            mF.FolioNo = -1;
            mF.ReservationID = ReservationID;
            if (mOR.ProfileAgentId > 0)
                mF.ProfileID = mOR.ProfileAgentId;
            else
                mF.ProfileID = mOR.ProfileCompanyId;
            mF.AccountName = mOR.LastName;
            if (mF.AccountName == "")
                if (mF.ProfileID > 0)
                    mF.AccountName = ((ProfileModel)pt.FindByPK("Profile", mF.ProfileID)).Account;
            mF.IsMasterFolio = true;
            mF.ConfirmationNo = mOR.ConfirmationNo;
            mF.UserInsertID = Global.UserID;
            mF.CreateDate = TextUtils.GetSystemDate();
            mF.UserUpdateID = Global.UserID;
            mF.UpdateDate = mF.CreateDate;

            return (int)pt.Insert(mF);
        }

        public static int UpdateFolioNoRouting(int FolioID, int ProfileIndividualID, string LastName, string ConfirmationNo, int FolioNo, ProcessTransactions pt)
        {
            FolioModel mF = (FolioModel)pt.FindByPK("Folio", FolioID);
            mF.FolioDate = TextUtils.GetBusinessDate();
            mF.FolioNo = FolioNo;
            mF.ProfileID = ProfileIndividualID;
            mF.AccountName = LastName;
            mF.ConfirmationNo = ConfirmationNo;
            mF.UserInsertID = mF.UserUpdateID = Global.UserID;
            mF.CreateDate = mF.UpdateDate = TextUtils.GetSystemDate();
            pt.Update(mF);
            return mF.ID;
        }
        #endregion

        #endregion


        public static int CopyTbReservationSpecial(int ReservationID, int pNewReservationID, int UserID, ProcessTransactions pt)
        {

            //dbo.ReservationPackage
            Expression expP = new Expression("ReservationID", ReservationID, "=");
            ArrayList arrP = pt.FindByExpression("ReservationSpecial", expP);
            //Kiểm tra điều kiện để trả về giá trị
            if (arrP.Count > 0)
            {
                for (int i = 0; i < arrP.Count; i++)
                {
                    ReservationSpecialModel mRS = (ReservationSpecialModel)pt.FindByPK("ReservationSpecial", ((ReservationSpecialModel)arrP[i]).ID);
                    mRS.ReservationID = pNewReservationID;
                    pt.Insert(mRS);

                    #region Ghi dữ liệu vào History
                    //ReservationBO.InsertHistory("INSERT", mRS);
                    #endregion
                }

                #region Ghi dữ liệu vào ReservationOption
                //int ReservationOptionID = ReservationBO.GetReservationOptionID(pNewReservationID, pt);
                //if (ReservationOptionID == 0)
                //{
                //    ReservationOptionsModel mRO = new ReservationOptionsModel();
                //    mRO.ReservationID = pNewReservationID;
                //    mRO.ItemInv = true;
                //    pt.Insert(mRO);
                //}
                //else
                //{
                //    ReservationOptionsModel mRO = (ReservationOptionsModel)pt.FindByPK("ReservationOptions", ReservationOptionID);
                //    mRO.ID = ReservationOptionID;
                //    mRO.ItemInv = true;
                //    pt.Update(mRO);
                //}
                #endregion
            }
            return 0;

        }
        public static int CopyTbReservationPackage(int ReservationID, int pNewReservationID, int UserID, ProcessTransactions pt)
        {
            //dbo.ReservationPackage
            Expression expP = new Expression("ReservationID", ReservationID, "=");
            ArrayList arrP = pt.FindByExpression("ReservationPackage", expP);
            //Kiểm tra điều kiện để trả về giá trị
            if (arrP.Count > 0)
            {
                for (int i = 0; i < arrP.Count; i++)
                {
                    ReservationPackageModel mP = (ReservationPackageModel)pt.FindByPK("ReservationPackage", ((ReservationPackageModel)arrP[i]).ID);
                    mP.ReservationID = pNewReservationID;
                    mP.CreateDate = TextUtils.GetSystemDate();
                    mP.UpdateDate = TextUtils.GetSystemDate();
                    mP.UserInsertID = UserID;
                    mP.UserUpdateID = UserID;
                    pt.Insert(mP);

                    #region Ghi dữ liệu vào History
                    //ReservationBO.InsertHistory("INSERT", mP);
                    #endregion
                }

                #region Ghi dữ liệu vào ReservationOption
                int ReservationOptionID = ReservationBO.GetReservationOptionID(pNewReservationID, pt);
                if (ReservationOptionID == 0)
                {
                    ReservationOptionsModel mRO = new ReservationOptionsModel();
                    mRO.ReservationID = pNewReservationID;
                    mRO.PackageOption = true;
                    pt.Insert(mRO);
                }
                else
                {
                    ReservationOptionsModel mRO = (ReservationOptionsModel)pt.FindByPK("ReservationOptions", ReservationOptionID);
                    mRO.ID = ReservationOptionID;
                    mRO.PackageOption = true;
                    pt.Update(mRO);
                }
                #endregion
            }
            return 0;
        }

        /// <summary>
        /// Copy thông tin của ReservationFixedCharge tới đặt phòng nếu có
        /// -- CSS, 25/12/2009
        /// </summary>
        /// <param name="ReservationID"></param>
        /// <param name="pNewReservationID"></param>
        /// <param name="pt"></param>
        /// <returns></returns>
        public static int CopyTbReservationFixedCharge(int ReservationID, int pNewReservationID, int UserID, ProcessTransactions pt)
        {

            //dbo.ReservationFixedCharge
            Expression expF = new Expression("ReservationID", ReservationID, "=");
            ArrayList arrF = pt.FindByExpression("ReservationFixedCharge", expF);
            //Kiểm tra điều kiện để trả về giá trị
            if (arrF.Count > 0)
            {
                for (int i = 0; i < arrF.Count; i++)
                {
                    ReservationFixedChargeModel mF = (ReservationFixedChargeModel)pt.FindByPK("ReservationFixedCharge", ((ReservationFixedChargeModel)arrF[i]).ID);
                    mF.ReservationID = pNewReservationID;
                    mF.CreateDate = TextUtils.GetSystemDate();
                    mF.UpdateDate = TextUtils.GetSystemDate();
                    mF.UserInsertID = UserID;
                    mF.UserUpdateID = UserID;
                    pt.Insert(mF);

                    #region Ghi dữ liệu vào History
                    //ClassReservation.InsertHistory("INSERT", mF);
                    #endregion
                }

                #region Ghi dữ liệu vào ReservationOption

                int ReservationOptionID = ReservationBO.GetReservationOptionID(pNewReservationID, pt);
                if (ReservationOptionID == 0)
                {
                    ReservationOptionsModel mRO = new ReservationOptionsModel();
                    mRO.ReservationID = pNewReservationID;
                    mRO.FixedCharges = true;
                    pt.Insert(mRO);
                }
                else
                {
                    ReservationOptionsModel mRO = (ReservationOptionsModel)pt.FindByPK("ReservationOptions", ReservationOptionID);
                    mRO.ID = ReservationOptionID;
                    mRO.FixedCharges = true;
                    pt.Update(mRO);
                }
                #endregion
            }
            return 0;

        }


        public static int GetReservationOptionID(int ReservationID, ProcessTransactions pt)
        {
            //Tim kiem ReservationOptions
            Expression exp = new Expression("ReservationID", ReservationID, "=");
            ArrayList arr = pt.FindByExpression("ReservationOptions", exp);
            //Kiem tra dieu kien va tra ve ket qua
            if (arr.Count == 0)
                return 0;
            else
                return ((ReservationOptionsModel)arr[0]).ID;
        }

        //No Transaction
        public static int GetReservationOptionID(int ReservationID)
        {

            //Tim kiem ReservationOptions
            Expression exp = new Expression("ReservationID", ReservationID, "=");
            ArrayList arr = ReservationOptionsBO.Instance.FindByExpression(exp);
            //Kiem tra dieu kien va tra ve ket qua
            if (arr.Count == 0)
                return 0;
            else
                return ((ReservationOptionsModel)arr[0]).ID;
        }

        /// <summary>
        /// Xác định xem có bao nhiêu Accompany trong phòng này để update vào trường AccompanyName trong bảng Reservation
        /// -- CSS, 27/03/2010
        /// </summary>
        /// <param name="ReservationID"></param>
        /// <param name="pt"></param>
        /// <returns></returns>
        public static void GetAccompanyName(int ReservationID, ProcessTransactions pt)
        {
            string AccompanyName = "";
            //Lấy dữ liệu trong bảng ReservationFixedCharge
            DataTable dtA = pt.Select("SELECT ReservationID,ProfileIndividualID FROM ReservationAccompany WITH (NOLOCK) where ReservationID = " + ReservationID + " ");
            for (int i = 0; i < dtA.Rows.Count; i++)
            {
                if (TextUtils.ToInt(dtA.Rows[i]["ProfileIndividualID"].ToString()) > 0)
                {
                    ProfileModel mP = (ProfileModel)pt.FindByPK("Profile", TextUtils.ToInt(dtA.Rows[i]["ProfileIndividualID"].ToString()));
                    if (AccompanyName != "")
                        AccompanyName = AccompanyName + ";" + mP.Account;
                    else
                        AccompanyName = mP.Account;
                }
            }
            //Ghi dữ liệu vào bảng Reservation
            if (dtA.Rows.Count > 0)
                TextUtils.UpdateTable("Reservation", "AccompanyName", AccompanyName, "ID", ReservationID.ToString(), pt);

        }
        //No Transaction
        public static void GetAccompanyName(int ReservationID)
        {
            string AccompanyName = "";
            //Lấy dữ liệu trong bảng ReservationFixedCharge
            DataTable dtA = TextUtils.Select("SELECT ReservationID,ProfileIndividualID FROM ReservationAccompany WITH (NOLOCK) where ReservationID = " + ReservationID + " ");
            for (int i = 0; i < dtA.Rows.Count; i++)
            {
                if (TextUtils.ToInt(dtA.Rows[i]["ProfileIndividualID"].ToString()) > 0)
                {
                    ProfileModel mP = (ProfileModel)ProfileBO.Instance.FindByPrimaryKey(TextUtils.ToInt(dtA.Rows[i]["ProfileIndividualID"].ToString()));
                    if (AccompanyName != "")
                        AccompanyName = AccompanyName + ";" + mP.Account;
                    else
                        AccompanyName = mP.Account;
                }
            }
            //Ghi dữ liệu vào bảng Reservation
            if (dtA.Rows.Count > 0)
                TextUtils.UpdateTable("Reservation", "AccompanyName", AccompanyName, "ID", ReservationID.ToString());
        }



        public static int GetFOStatus(int RoomID)
        {
            ProcessTransactions pt = new ProcessTransactions();
            pt.OpenConnection();
            pt.BeginTransaction();

            string[] paramName = new string[1];
            paramName[0] = "@sqlCommand";
            string[] paramValue = new string[1];
            paramValue[0] = "select FOStatus FROM Room WITH (NOLOCK) where ID = " + RoomID + " ";
            try
            {
                DataTable dt = pt.getTable("spGenSearchWithCommand", "Table", paramName, paramValue);
                pt.CommitTransaction();
                if (dt.Rows.Count > 0)
                    return int.Parse(dt.Rows[0][0].ToString());
                else
                    return 0;

            }
            catch (Exception ex)
            {
                pt.CloseConnection();
                throw new Exception(ex.Message);
            }
        }
        public static int GetHKStatusID(int RoomID)
        {
            ProcessTransactions pt = new ProcessTransactions();
            pt.OpenConnection();
            pt.BeginTransaction();

            string[] paramName = new string[1];
            paramName[0] = "@sqlCommand";
            string[] paramValue = new string[1];
            paramValue[0] = "select HKStatusID FROM Room WITH (NOLOCK) where ID = " + RoomID + " ";
            try
            {
                DataTable dt = pt.getTable("spGenSearchWithCommand", "Table", paramName, paramValue);
                pt.CommitTransaction();
                if (dt.Rows.Count > 0)
                    return int.Parse(dt.Rows[0][0].ToString());
                else
                    return 1;
            }
            catch (Exception ex)
            {
                pt.CloseConnection();
                throw new Exception(ex.Message);
            }
        }
        public static void IF_REC(int _RsvID, string _RoomNo)
        {
            if (IF_IN() == "0")
                return;

            string _des = "";
            _des = "REC|R#" + _RsvID + "|RN" + _RoomNo + "|";
            //Insert
            IF_Interface("REC", _des.Trim());
        }
        public static void IF_Interface(string _KeyValue, string _des)
        {
            InterfaceModel mIF = new InterfaceModel();
            mIF.KeyValue = _KeyValue;
            mIF.Description = _des;
            mIF.CreateDate = TextUtils.GetSystemDate();
            InterfaceBO.Instance.Insert(mIF);
        }
        public static string[] SplitStringStandard(string Name, int Type)
        {
            //0:First; 1:Last; 2:Midle; 3:Name; 4:TitleID
            string[] Result = new string[5];
            string[] Arr;
            //Tách chuỗi
            Arr = Name.Split(',');
            //Nếu nhập không đúng quy định
            if (Arr.Length == 1)
                return Result;
            else if (Arr.Length > 1)
            {
                //Nhặt ra LastName, MidleName
                Arr[0] = Arr[0].Trim();
                string[] LastName = Arr[0].Split(' ');
                if (LastName.Length > 0)
                {
                    //Nhặt ra LastName
                    Result[1] = LastName[0].Trim();
                    //Convert chữ cái đầu thành chữ IN HOA
                    Result[1] = char.ToUpper(LastName[0][0]) + LastName[0].Substring(1);

                    //Nhặt ra MidleName
                    for (int l = 1; l < LastName.Length; l++)
                    {
                        if (Result[2] != null)
                        {
                            //Conver chữ cái đầu sang chư IH
                            if (LastName[l] != null && LastName[l] != "")
                            {
                                LastName[l] = char.ToUpper(LastName[l][0]) + LastName[l].Substring(1);
                                Result[2] = Result[2].Trim() + " " + LastName[l];
                            }
                        }
                        else
                        {
                            //Conver chữ cái đầu sang chư IH
                            if (LastName[l] != null && LastName[l] != "")
                            {
                                LastName[l] = char.ToUpper(LastName[l][0]) + LastName[l].Substring(1);
                                Result[2] = LastName[l].Trim();
                            }
                        }
                    }
                    if (Result[2] == null)
                    {
                        Result[2] = "";
                        //Bỏ khoảng trắng
                        Result[2] = Result[2].Trim();
                    }
                }
                //Xác định Title           
                string title = Arr[Arr.Length - 1].Trim();
                title = title.Replace("(", ""); title = title.Replace(")", "");
                title = title.Replace("{", ""); title = title.Replace("}", "");
                title = title.Replace("[", ""); title = title.Replace("]", "");
                ArrayList arr1 = TitleBO.Instance.FindByAttribute("Code", title);
                if (arr1.Count > 0)
                    Result[4] = ((TitleModel)arr1[0]).ID.ToString();
                else
                    Result[4] = "0";
                //Nếu TitleID có tồn tại thì Remove nó trong Name
                if (Result[4] != "0")
                {
                    //Bỏ Titile nếu có
                    Name = Name.Substring(0, (Name.Length - Arr[Arr.Length - 1].Length) - 1);
                    //Bỏ LastName, MidleName
                    if (Arr.Length > 2)
                    {
                        string[] LM = Name.Split(',');
                        Name = Name.Remove(0, (LM[0].Length + 1));
                    }
                }
                else
                {
                    //Bỏ LastName, MidleName 
                    string[] LM = Name.Split(',');
                    Name = Name.Remove(0, (LM[0].Length + 1));
                }
                //Xác định Chuỗi còn lại trước khi Remove
                if (Arr.Length == 2 && Result[4] != "0")
                {
                    Result[0] = Name;
                    Result[0] = char.ToUpper(Name[0]) + Name.Substring(1);
                    Result[3] = Result[0].Trim() + ", " + ((TitleModel)TitleBO.Instance.FindByPrimaryKey(int.Parse(Result[4].ToString()))).Code;
                }
                else
                {
                    //Nhặt ra FirstName
                    Name = Name.Trim();
                    string[] FirstName = Name.Split(',');
                    //Nếu nhập sai Title thì remove title đi
                    if (FirstName.Length > 1 && Result[4] == "0")
                        Name = Name.Substring(0, (Name.Length - Arr[Arr.Length - 1].Length) - 1);
                    //Tách FirstName
                    FirstName = Name.Split(' ');
                    for (int f = 0; f < FirstName.Length; f++)
                    {
                        if (Result[0] != null)
                        {
                            //Conver chữ cái đầu sang chư IH
                            FirstName[f] = char.ToUpper(FirstName[f][0]) + FirstName[f].Substring(1);
                            Result[0] = Result[0].Trim() + " " + FirstName[f];
                        }
                        else
                        {
                            //Conver chữ cái đầu sang chữ IH
                            if (FirstName[f] != "")
                            {
                                FirstName[f] = char.ToUpper(FirstName[f][0]) + FirstName[f].Substring(1);
                                Result[0] = FirstName[f].Trim();
                            }
                            else
                            {
                                Result[0] = "";
                            }
                        }
                    }
                    //Nhặt ra Name
                    if (Result[4] == "0")
                        if (Result[2].Trim() != "")
                            Result[3] = Result[1].Trim() + " " + Result[2].Trim() + "," + " " + Result[0].Trim();
                        else
                            Result[3] = Result[1].Trim() + "," + " " + Result[0].Trim();
                    else
                        if (Result[2].Trim() != "")
                            Result[3] = Result[1].Trim() + " " + Result[2].Trim() + ", " + Result[0].Trim() + ", " + ((TitleModel)TitleBO.Instance.FindByPrimaryKey(int.Parse(Result[4].ToString()))).Code;
                        else
                            Result[3] = Result[1].Trim() + ", " + Result[0].Trim() + ", " + ((TitleModel)TitleBO.Instance.FindByPrimaryKey(int.Parse(Result[4].ToString()))).Code;
                }
            }
            return Result;
        }
        public static string IF_IN()
        {
            string _if_in = "";
            _if_in = TextUtils.Select("SELECT KeyValue FROM ConfigSystem WHERE KeyName ='IF_IN' ").Rows[0][0].ToString();
            return _if_in;
        }
    }
}
