using System.Collections;
using System.Data;
using BaseBusiness.BO;
using BaseBusiness.Model;
using Microsoft.Data.SqlClient;

namespace BaseBusiness.util
{
    /// <summary>
    /// Summary description for TextUtils.
    /// </summary>
    public class ReservationUtil
    {
        #region Static Void CreateReservationGroup gọi nhiều
        public static void CreateReservationGroup(int ReservationID, string ConfirmationNo, int UserID, string Comment, ProcessTransactions pt)
        {
            int ReservationGroupID = 0;

            #region Kiểm tra và ghi dữ liệu vào bảng ReservationGroup
            DataTable dtRG = pt.Select("SELECT MIN(ArrivalDate) AS FirstArrival, " +
                                       "MAX(DepartureDate) AS LastDeparture, " +
                                       "SUM(NoOfRoom) AS TotalRoom, " +
                                       "SUM(CASE WHEN NoOfRoom <> 0 THEN (NoOfAdult*NoOfRoom) ELSE NoOfAdult END) AS TotalAdult, " +
                                       "SUM(CASE WHEN NoOfRoom <> 0 THEN (NoOfChild*NoOfRoom) ELSE NoOfChild END) AS TotalChild, " +
                                       "SUM(CASE WHEN NoOfRoom <> 0 THEN (NoOfChild1*NoOfRoom) ELSE NoOfChild1 END) AS TotalChild1, " +
                                       "SUM(CASE WHEN NoOfRoom <> 0 THEN (NoOfChild2*NoOfRoom) ELSE NoOfChild2 END) AS TotalChild2, " +
                                       "SUM(BalanceUSD) AS TotalReservationBalance " +
                                       "FROM Reservation WITH (NOLOCK) " +
                                       "WHERE ConfirmationNo = '" + ConfirmationNo + "' " +
                                       "AND Status NOT IN (3,4,7) AND RoomType <> 'XXX' " +
                                       "AND (ReservationNo > 0 OR ID = " + ReservationID + ") ");
            //Tìm kiếm xem trong bảng ReservationGroup đã tồn tại hay chưa
            Expression expRG = new Expression("ConfirmationNo", ConfirmationNo, "=");
            ArrayList arrRG = pt.FindByExpression("ReservationGroup", expRG);
            //Insert
            if (arrRG.Count == 0)
            {
                if (TextUtils.ToInt(dtRG.Rows[0]["TotalRoom"].ToString()) > 0)
                {
                    ReservationGroupModel mRG = new ReservationGroupModel();
                    mRG.ConfirmationNo = int.Parse(ConfirmationNo.ToString());
                    mRG.FirstArrival = Convert.ToDateTime(dtRG.Rows[0]["FirstArrival"]);
                    mRG.LastDeparture = Convert.ToDateTime(dtRG.Rows[0]["LastDeparture"]);
                    mRG.TotalRoom = TextUtils.ToInt(dtRG.Rows[0]["TotalRoom"]?.ToString() ?? "0");
                    mRG.TotalAdult = TextUtils.ToInt(dtRG.Rows[0]["TotalAdult"]?.ToString() ?? "0");
                    mRG.TotalChild = TextUtils.ToInt(dtRG.Rows[0]["TotalChild"]?.ToString() ?? "0");
                    mRG.TotalChild1 = TextUtils.ToInt(dtRG.Rows[0]["TotalChild1"]?.ToString() ?? "0");
                    mRG.TotalChild2 = TextUtils.ToInt(dtRG.Rows[0]["TotalChild2"]?.ToString() ?? "0");
                    mRG.TotalReservationBalance = TextUtils.ToDecimal(dtRG.Rows[0]["TotalReservationBalance"]?.ToString() ?? "0");
                    mRG.Comment = Comment;
                    mRG.UserInsertID = UserID;
                    mRG.CreateDate = TextUtils.GetBussinessDateTime();
                    mRG.UserUpdateID = UserID;
                    mRG.UpdateDate = mRG.CreateDate;
                    mRG.OptionDate = Convert.ToDateTime("1900/1/1");
                    mRG.OptionDateDesc = "";
                    ReservationGroupID = (int)pt.Insert(mRG);
                }
            }
            //Update
            else if (arrRG.Count > 0)
            {
                if (TextUtils.ToInt(dtRG.Rows[0]["TotalRoom"].ToString()) > 0)
                {
                    ReservationGroupModel mRG = (ReservationGroupModel)pt.FindByPK("ReservationGroup", ((ReservationGroupModel)arrRG[0]).ID);
                    mRG.FirstArrival = Convert.ToDateTime(dtRG.Rows[0]["FirstArrival"]);
                    mRG.LastDeparture = Convert.ToDateTime(dtRG.Rows[0]["LastDeparture"]);
                    mRG.TotalRoom = TextUtils.ToInt(dtRG.Rows[0]["TotalRoom"]?.ToString() ?? "0");
                    mRG.TotalAdult = TextUtils.ToInt(dtRG.Rows[0]["TotalAdult"]?.ToString() ?? "0");
                    mRG.TotalChild = TextUtils.ToInt(dtRG.Rows[0]["TotalChild"]?.ToString() ?? "0");
                    mRG.TotalChild1 = TextUtils.ToInt(dtRG.Rows[0]["TotalChild1"]?.ToString() ?? "0");
                    mRG.TotalChild2 = TextUtils.ToInt(dtRG.Rows[0]["TotalChild2"]?.ToString() ?? "0");
                    mRG.TotalReservationBalance = TextUtils.ToDecimal(dtRG.Rows[0]["TotalReservationBalance"]?.ToString() ?? "0");
                    if (Comment != "")
                        mRG.Comment = Comment;
                    mRG.UserUpdateID = UserID;
                    mRG.UpdateDate = TextUtils.GetBussinessDateTime();
                    //mRG.ID = ((ReservationGroupModel)arrRG[0]).ID;
                    pt.Update(mRG);
                    ReservationGroupID = mRG.ID;
                }
            }
            #endregion

            #region Kiểm tra và Ghi dữ liệu vào bảng ReservationGroupAmountByCurrency
            DataTable dtRGA = pt.Select("SELECT SUM(a.AmountBeforTax) AmountBeforTax, " +
                                        "SUM(a.AmountAfterTax) AmountAfterTax, " +
                                        "a.CurrencyID " +
                                        "FROM ReservationAmountByCurrency a WITH (NOLOCK), Reservation b WITH (NOLOCK) " +
                                        "WHERE a.ReservationID = b.ID " +
                                        "AND a.ConfirmationNo = '" + ConfirmationNo + "' " +
                                        "AND b.Status <> 3 AND b.Status <> 4 AND b.Status <> 7 AND RoomType <> 'XXX' " +
                                        "AND (b.ReservationNo > 0 OR b.ID = " + ReservationID + ") " +
                                        "GROUP BY a.CurrencyID ");
            if (dtRGA.Rows.Count > 0)
            {
                //Xóa dữ liệu trước khi Insert
                pt.DeleteByAttribute("ReservationGroupAmountByCurrency", "ReservationGroupID", ReservationGroupID.ToString());
                for (int i = 0; i < dtRGA.Rows.Count; i++)
                {
                    ReservationGroupAmountByCurrencyModel mRGA = new()
                    {
                        ReservationGroupID = ReservationGroupID,
                        CurrencyID = dtRGA.Rows[i]["CurrencyID"].ToString(),
                        AmountBeforTax = TextUtils.ToDecimal(dtRGA.Rows[i]["AmountBeforTax"]?.ToString() ?? "0"),
                        AmountAfterTax = TextUtils.ToDecimal(dtRGA.Rows[i]["AmountAfterTax"]?.ToString() ?? "0")
                    };
                    mRGA.UserInsertID = mRGA.UserUpdateID = UserID;
                    mRGA.CreateDate = mRGA.UpdateDate = TextUtils.GetBussinessDateTime();
                    pt.Insert(mRGA);
                }
            }
            #endregion
        }
        //No Transaction
        public static void CreateReservationGroup(int ReservationID, string ConfirmationNo, string Comment, int userID)
        {
            int ReservationGroupID = 0;

            #region Kiểm tra và ghi dữ liệu vào bảng ReservationGroup 
            DataTable dtRG = TextUtils.Select("SELECT MIN(ArrivalDate) AS FirstArrival, " +
                                       "MAX(DepartureDate) AS LastDeparture, " +
                                       "SUM(NoOfRoom) AS TotalRoom, " +
                                       "SUM(CASE WHEN NoOfRoom <> 0 THEN (NoOfAdult*NoOfRoom) ELSE NoOfAdult END) AS TotalAdult, " +
                                       "SUM(CASE WHEN NoOfRoom <> 0 THEN (NoOfChild*NoOfRoom) ELSE NoOfChild END) AS TotalChild, " +
                                       "SUM(CASE WHEN NoOfRoom <> 0 THEN (NoOfChild1*NoOfRoom) ELSE NoOfChild1 END) AS TotalChild1, " +
                                       "SUM(CASE WHEN NoOfRoom <> 0 THEN (NoOfChild2*NoOfRoom) ELSE NoOfChild2 END) AS TotalChild2, " +
                                       "SUM(BalanceUSD) AS TotalReservationBalance " +
                                       "FROM Reservation WITH (NOLOCK)" +
                                       "WHERE ConfirmationNo = '" + ConfirmationNo + "' " +
                                       "AND Status <> 3 AND Status <> 4 AND Status <> 7 AND RoomType <> 'XXX' " +
                                       "AND (ReservationNo > 0 OR ID = " + ReservationID + ") ");
            //Tìm kiếm xem trong bảng ReservationGroup đã tồn tại hay chưa
            Expression expRG = new Expression("ConfirmationNo", ConfirmationNo, "=");
            ArrayList arrRG = ReservationGroupBO.Instance.FindByExpression(expRG);
            //Insert
            if (arrRG.Count == 0)
            {
                if (TextUtils.ToInt(dtRG.Rows[0]["TotalRoom"].ToString()) > 0)
                {
                    ReservationGroupModel mRG = new ReservationGroupModel();
                    mRG.ConfirmationNo = int.Parse(ConfirmationNo.ToString());
                    mRG.FirstArrival = Convert.ToDateTime(dtRG.Rows[0]["FirstArrival"]);
                    mRG.LastDeparture = Convert.ToDateTime(dtRG.Rows[0]["LastDeparture"]);
                    mRG.TotalRoom = TextUtils.ToInt(dtRG.Rows[0]["TotalRoom"]?.ToString() ?? "0");
                    mRG.TotalAdult = TextUtils.ToInt(dtRG.Rows[0]["TotalAdult"]?.ToString() ?? "0");
                    mRG.TotalChild = TextUtils.ToInt(dtRG.Rows[0]["TotalChild"]?.ToString() ?? "0");
                    mRG.TotalChild1 = TextUtils.ToInt(dtRG.Rows[0]["TotalChild1"]?.ToString() ?? "0");
                    mRG.TotalChild2 = TextUtils.ToInt(dtRG.Rows[0]["TotalChild2"]?.ToString() ?? "0");
                    mRG.TotalReservationBalance = TextUtils.ToDecimal(dtRG.Rows[0]["TotalReservationBalance"]?.ToString() ?? "0");
                    mRG.Comment = Comment;
                    mRG.UserInsertID = mRG.UserUpdateID;
                    mRG.CreateDate = mRG.UpdateDate = TextUtils.GetSystemDate();
                    mRG.OptionDate = Convert.ToDateTime("1900/1/1");
                    mRG.OptionDateDesc = "";
                    ReservationGroupID = (int)ReservationGroupBO.Instance.Insert(mRG);
                }
            }
            //Update
            else if (arrRG.Count > 0)
            {
                if (TextUtils.ToInt(dtRG.Rows[0]["TotalRoom"].ToString()) > 0)
                {
                    ReservationGroupModel mRG = (ReservationGroupModel)ReservationGroupBO.Instance.FindByPrimaryKey(((ReservationGroupModel)arrRG[0]).ID);
                    mRG.FirstArrival = Convert.ToDateTime(dtRG.Rows[0]["FirstArrival"]);
                    mRG.LastDeparture = Convert.ToDateTime(dtRG.Rows[0]["LastDeparture"]);
                    mRG.TotalRoom = TextUtils.ToInt(dtRG.Rows[0]["TotalRoom"]?.ToString() ?? "0");
                    mRG.TotalAdult = TextUtils.ToInt(dtRG.Rows[0]["TotalAdult"]?.ToString() ?? "0");
                    mRG.TotalChild = TextUtils.ToInt(dtRG.Rows[0]["TotalChild"]?.ToString() ?? "0");
                    mRG.TotalChild1 = TextUtils.ToInt(dtRG.Rows[0]["TotalChild1"]?.ToString() ?? "0");
                    mRG.TotalChild2 = TextUtils.ToInt(dtRG.Rows[0]["TotalChild2"]?.ToString() ?? "0");
                    mRG.TotalReservationBalance = TextUtils.ToDecimal(dtRG.Rows[0]["TotalReservationBalance"]?.ToString() ?? "0");
                    if (Comment != "")
                        mRG.Comment = Comment;
                    mRG.UserUpdateID = userID;
                    mRG.UpdateDate = TextUtils.GetBussinessDateTime();
                    mRG.ID = ((ReservationGroupModel)arrRG[0]).ID;
                    ReservationGroupBO.Instance.Update(mRG);
                    ReservationGroupID = mRG.ID;
                }
            }
            #endregion

            #region Kiểm tra và Ghi dữ liệu vào bảng ReservationGroupAmountByCurrency 
            DataTable dtRGA = TextUtils.Select("SELECT SUM(a.AmountBeforTax) AmountBeforTax, " +
                                        "SUM(a.AmountAfterTax) AmountAfterTax, " +
                                        "a.CurrencyID " +
                                        "FROM ReservationAmountByCurrency a WITH (NOLOCK), Reservation b WITH (NOLOCK)" +
                                        "WHERE a.ReservationID = b.ID " +
                                        "AND a.ConfirmationNo = '" + ConfirmationNo + "' " +
                                        "AND b.Status <> 3 AND b.Status <> 4 AND b.Status <> 7 AND RoomType <> 'XXX' " +
                                        "AND (b.ReservationNo > 0 OR b.ID = " + ReservationID + ") " +
                                        "GROUP BY a.CurrencyID ");
            if (dtRGA.Rows.Count > 0)
            {
                //Xóa dữ liệu trước khi Insert
                TextUtils.UpdateDataBase("DELETE ReservationGroupAmountByCurrency WHERE ID IN (SELECT ID FROM ReservationGroupAmountByCurrency WITH (NOLOCK) WHERE ReservationGroupID = " + ReservationGroupID + ") ");
                for (int i = 0; i < dtRGA.Rows.Count; i++)
                {
                    ReservationGroupAmountByCurrencyModel mRGA = new ReservationGroupAmountByCurrencyModel();
                    mRGA.ReservationGroupID = ReservationGroupID;
                    mRGA.CurrencyID = dtRGA.Rows[i]["CurrencyID"].ToString();
                    mRGA.AmountBeforTax = TextUtils.ToDecimal(dtRGA.Rows[i]["AmountBeforTax"]?.ToString() ?? "0");
                    mRGA.AmountAfterTax = TextUtils.ToDecimal(dtRGA.Rows[i]["AmountAfterTax"]?.ToString() ?? "0");
                    mRGA.UserInsertID = mRGA.UserUpdateID = userID;
                    mRGA.CreateDate = mRGA.UpdateDate = TextUtils.GetSystemDate();
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(mRGA, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));

                    ReservationGroupAmountByCurrencyBO.Instance.Insert(mRGA);
                }
            }
            #endregion
        }

        /// <summary>
        /// Tạo Profile với profile đã có
        /// -- CSS, 27/03/2010
        /// </summary>
        /// <param name="ProfileIndividualID">ID Profile truyền vào</param>
        /// <param name="UserID">UserID người tạo</param>
        /// <param name="pt"> Transaction</param>
        /// <returns></returns>
        public static int CreateProfile(int ProfileIndividualID, int UserID, ProcessTransactions pt)
        {
            int pProfileID = 0;
            string _maxcode = ProfileBO.Instance.GenerateNo4("Code");
            //DataTable CR = pt.Select("Select Top 1 MAX(Convert(int,Code)) AS Code FROM Profile WITH (NOLOCK)");
            ProfileModel mP = (ProfileModel)pt.FindByPK("Profile", ProfileIndividualID);
            if (_maxcode != "")
            {
                //int leg = CR.Rows[0]["Code"].ToString().Length;
                //mP.Code = "0000" + Convert.ToString(Convert.ToUInt32(CR.Rows[0]["Code"].ToString()) + 1);
                //mP.Code = mP.Code.Remove(0, mP.Code.Length - leg);
                mP.Code = _maxcode;
                mP.CreateDate = mP.UpdateDate = TextUtils.GetSystemDate();
                mP.UserInsertID = mP.UserUpdateID = UserID;
                mP.IdentityCard = "";
                mP.PassPort = "";
                mP.Address = "";
                mP.IsBlackList = false;
                mP.BlackListReason = "";
                mP.DateOfBirth = Convert.ToDateTime("01/01/1900");
                #region 1.&&
                mP.ReturnGuest = -1;
                mP.StayNo = 0;
                mP.GuestNo = mP.Occupation = mP.Birthplace = "";
                mP.BonusPoints = mP.GuestGroupID = 0;
                mP.ExpressCheckout = mP.PayTV = false;
                mP.CreditCard = mP.RateCode = "";
                mP.RoomNights = mP.BedNights = 0;
                mP.TotalTurnover = mP.LodgeTurnover = mP.LodgePackageTurover = mP.FBTurnover = mP.EventTurnover = mP.OtherTurnover = 0;
                mP.FirstReservation = Convert.ToDateTime("01/01/1900");
                mP.LastReservation = Convert.ToDateTime("01/01/1900");
                mP.WeddingAnniversary = Convert.ToDateTime("01/01/1900");
                mP.Firstvisit = Convert.ToDateTime("01/01/1900");
                mP.Expiry = Convert.ToDateTime("01/01/1900");
                mP.LastContact = Convert.ToDateTime("01/01/1900");
                mP.Telephone = ""; mP.HandPhone = ""; mP.Email = ""; mP.Fax = ""; mP.Website = "";
                mP.TaxCode = ""; mP.HomeAddress = ""; mP.FullAccount = "";
                mP.VIPID = 0; mP.VIPReason = "";
                #endregion
                pProfileID = (int)pt.Insert(mP);
            }
            return pProfileID;
        }
        //No Transaction
        public static int CreateProfile(int ProfileIndividualID, int UserID)
        {
            int pProfileID = 0;
            string _maxCode = " ";
            //_maxCode = ProfileBO.Instance.GenerateNo3("Code");
            //DataTable CR = TextUtils.Select("Select Top 1 MAX(Convert(int,Code)) AS Code FROM Profile WITH (NOLOCK)");
            ProfileModel mP = (ProfileModel)ProfileBO.Instance.FindByPrimaryKey(ProfileIndividualID);
            if (_maxCode != "")
            {
                //int leg = CR.Rows[0]["Code"].ToString().Length;
                //mP.Code = "0000" + Convert.ToString(Convert.ToUInt32(CR.Rows[0]["Code"].ToString()) + 1);
                //mP.Code = mP.Code.Remove(0, mP.Code.Length - leg);
                mP.Code = _maxCode;
                mP.CreateDate = mP.UpdateDate = TextUtils.GetSystemDate();
                mP.UserInsertID = mP.UserUpdateID = UserID;
                mP.IdentityCard = "";
                mP.PassPort = "";
                mP.Address = "";
                mP.IsBlackList = false;
                mP.BlackListReason = "";
                mP.DateOfBirth = Convert.ToDateTime("01/01/1900");
                #region 1.&&
                mP.StayNo = 0;
                mP.ReturnGuest = -1;
                mP.GuestNo = mP.Occupation = mP.Birthplace = "";
                mP.BonusPoints = mP.GuestGroupID = 0;
                mP.ExpressCheckout = mP.PayTV = false;
                mP.CreditCard = mP.RateCode = "";
                mP.RoomNights = mP.BedNights = 0;
                mP.TotalTurnover = mP.LodgeTurnover = mP.LodgePackageTurover = mP.FBTurnover = mP.EventTurnover = mP.OtherTurnover = 0;
                mP.FirstReservation = Convert.ToDateTime("01/01/1900");
                mP.LastReservation = Convert.ToDateTime("01/01/1900");
                mP.WeddingAnniversary = Convert.ToDateTime("01/01/1900");
                mP.Firstvisit = Convert.ToDateTime("01/01/1900");
                mP.Expiry = Convert.ToDateTime("01/01/1900");
                mP.LastContact = Convert.ToDateTime("01/01/1900");
                mP.Telephone = ""; mP.HandPhone = ""; mP.Email = ""; mP.Fax = ""; mP.Website = "";
                mP.TaxCode = ""; mP.HomeAddress = ""; mP.FullAccount = "";
                mP.VIPID = 0; mP.VIPReason = "";
                #endregion
                pProfileID = (int)ProfileBO.Instance.Insert(mP);
            }
            return pProfileID;
        }

        #region Phần GetRoomShareName
        /// <summary>
        /// Xác định tên của từng người trong phòng để update vào bảng Reservation (Trường ShareRoomName)
        /// -- CSS, 27/03/2010
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public static void GetRoomShareName(int ShareRoom, ProcessTransactions pt)
        {
            #region Xác định xem có bao nhiêu phiếu đặt phòng trong phòng 
            Expression expSR = new Expression("ShareRoom", ShareRoom, "=");
            ArrayList arrSR = pt.FindByExpression("Reservation", expSR);
            if (arrSR.Count > 0)
            {
                for (int sr = 0; sr < arrSR.Count; sr++)
                {
                    ReservationModel mR = (ReservationModel)pt.FindByPK("Reservation", ((ReservationModel)arrSR[sr]).ID);
                    //Nhặt ra Name cần Update cho từng BK
                    string ShareRoomName = "";
                    Expression expSR1 = new Expression("ShareRoom", ShareRoom, "=");
                    expSR1 = expSR1.And(new Expression("ID", ((ReservationModel)arrSR[sr]).ID, "<>"));
                    ArrayList arrSR1 = pt.FindByExpression("Reservation", expSR1);
                    for (int sr1 = 0; sr1 < arrSR1.Count; sr1++)
                    {
                        if (ShareRoomName != "")
                            ShareRoomName = ShareRoomName + "; " + ((ReservationModel)arrSR1[sr1]).LastName;
                        else
                            ShareRoomName = ((ReservationModel)arrSR1[sr1]).LastName;
                    }
                    //Update vao BK - C1
                    mR.ShareRoomName = ShareRoomName;
                    pt.Update(mR);
                    //Update vao BK - C2(Lỗi khi tên có ký tự đặc biệt)                            
                    ////pt.UpdateCommand("UPDATE Reservation SET ShareRoomName = N'" + ShareRoomName + "' " +
                    ////                 "WHERE ID = " + ((ReservationModel)arrSR[sr]).ID + " ");
                }
            }
            #endregion
        }
        //No Transaction
        public static void GetRoomShareName(int ShareRoom)
        {
            // Xác định xem có bao nhiêu phiếu đặt phòng trong phòng 
            Expression expSR = new Expression("ShareRoom", ShareRoom, "=");
            ArrayList arrSR = ReservationBO.Instance.FindByExpression(expSR);
            if (arrSR.Count > 0)
            {
                for (int sr = 0; sr < arrSR.Count; sr++)
                {
                    ReservationModel mR = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(((ReservationModel)arrSR[sr]).ID);
                    //Nhặt ra Name cần Update cho từng BK
                    string ShareRoomName = "";
                    Expression expSR1 = new Expression("ShareRoom", ShareRoom, "=");
                    expSR1 = expSR1.And(new Expression("ID", ((ReservationModel)arrSR[sr]).ID, "<>"));
                    ArrayList arrSR1 = ReservationBO.Instance.FindByExpression(expSR1);
                    for (int sr1 = 0; sr1 < arrSR1.Count; sr1++)
                    {
                        if (ShareRoomName != "")
                            ShareRoomName = ShareRoomName + "; " + ((ReservationModel)arrSR1[sr1]).LastName;
                        else
                            ShareRoomName = ((ReservationModel)arrSR1[sr1]).LastName;
                    }
                    //Update vao BK - C1
                    mR.ShareRoomName = ShareRoomName;
                    ReservationBO.Instance.Update(mR);
                }
            }
        }

        #endregion

        #region 16.Interface

        /// <summary>
        /// Xác định xem có insert vào interface hay ko?
        /// -- 0: không Insert; 1: Insert
        /// --CSS, 18/04/2011
        /// </summary>
        /// <returns> IF_IN </returns>
        public static string IF_IN()
        {
            string _if_in = "";
            _if_in = TextUtils.Select("SELECT KeyValue FROM ConfigSystem WHERE KeyName ='IF_IN' ").Rows[0][0].ToString();
            return _if_in;
        }

        /// <summary>
        /// Insert data
        /// </summary>
        /// <param name="_KeyValue"></param>
        /// <param name="_des"></param>
        public static void IF_Interface(string _KeyValue, string _des)
        {
            InterfaceModel mIF = new InterfaceModel();
            mIF.KeyValue = _KeyValue;
            mIF.Description = _des;
            mIF.CreateDate = TextUtils.GetSystemDate();
            InterfaceBO.Instance.Insert(mIF);
        }

        /// <summary>
        /// Insert data
        /// </summary>
        /// <param name="_KeyValue"></param>
        /// <param name="_des"></param>
        public static void IF_Interface(string _KeyValue, string _des, ProcessTransactions pt)
        {
            InterfaceModel mIF = new InterfaceModel();
            mIF.KeyValue = _KeyValue;
            mIF.Description = _des;
            mIF.CreateDate = TextUtils.GetSystemDate();
            if (pt != null)
                pt.Insert(mIF);
            else
                InterfaceBO.Instance.Insert(mIF);
        }

        /// <summary>
        /// Guest Check-in; 
        /// GI|R#1234|G#8874|GA110327|GD110329|GTMr.|GFDat|GNPhan Duy|RN7425|GP123|
        /// </summary>
        /// <param name="mR"></param>
        public static void IF_GI(ReservationModel mR, int _RsvID, ProcessTransactions pt)
        {
            if (IF_IN() == "0")
                return;
            if (mR == null)
            {
                if (pt != null)
                    mR = (ReservationModel)pt.FindByPK("Reservation", _RsvID);
                else
                    mR = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(_RsvID);
            }

            string _des = "";
            string _guest = "";
            string _mainguest = "";
            if (mR.MainGuest == true)
                _mainguest = "|GS0";
            else
                _mainguest = "|GS1";
            string[] _nam = ReservationBO.SplitStringStandard(mR.LastName, 0);
            if (_nam[0] != null)
            {
                if (mR.Title.Trim() != "")
                    _guest = "|GT" + mR.Title + "|GF" + mR.FirstName + "|GN" + _nam[1].ToString() + " " + _nam[2].ToString().Trim();
                else
                    _guest = "|GF" + mR.FirstName + "|GN" + _nam[1].ToString() + " " + _nam[2].ToString().Trim();
            }

            _des = "GI|R#" + mR.ID + "|G#" + mR.ProfileIndividualId + "|GA" + mR.ArrivalDate.ToString("yyMMdd")
                + "|GD" + mR.DepartureDate.ToString("yyMMdd") + _guest.Trim() + "|RN" + mR.RoomNo + _mainguest + "|GP" + mR.PinCode;
            if (mR.GroupCode.Trim() != "")
                _des = _des + "|GG" + "[" + mR.ConfirmationNo + "]" + mR.GroupCode;
            else
                _des = _des + "|GG" + "[" + mR.ConfirmationNo + "]";
            if (mR.Country != "")
                _des = _des + "|GL" + mR.Country;

            _des = _des + "|";
            //Insert
            IF_Interface("GI", _des);
        }

        /// <summary>
        /// Guest Check Out
        /// </summary>
        /// <param name="_RsvID"></param>
        public static void IF_GO(int _RsvID, int _GuestID, string _RoomNo, bool _IsMainGuest)
        {
            if (IF_IN() == "0")
                return;
            string _guest = "";
            _guest = "GO|R#" + _RsvID + "|G#" + _GuestID + "|RN" + _RoomNo;
            if (_IsMainGuest == true)
                _guest = _guest + "|GS0" + "|";
            else
                _guest = _guest + "|GS1" + "|";

            //Insert
            IF_Interface("GO", _guest);
        }

        /// <summary>
        /// Guest data change; 
        /// _type: 0: Change Profile; 1: Change Name Rsv; 2: Change mainguest
        /// GC|R#1234|G#8874|GTDr.|GLVNM|
        /// (bỏ lấy hàm dưới)13.12.11 (Mr Duong sửa bỏ điều kiện check Title name...)
        /// </summary>
        /// <param name="mP"></param>
        /// <param name="mOP"></param>
        public static void IF_GC_CSS(ProfileModel mP, int _NewProID, ProfileModel mOP, int _OldProID, int _type, ProcessTransactions pt)
        {
            if (IF_IN() == "0")
                return;
            if (_type == 0)
            {
                string _des = "";
                _des = "GC|G#" + mP.ID;
                if (mP == null)
                    mP = (ProfileModel)pt.FindByPK("Profile", _NewProID);
                if (mOP == null)
                    mOP = (ProfileModel)pt.FindByPK("Profile", _OldProID);

                if (mP.TitleID != mOP.TitleID)
                    _des = _des + "|GT" + ((TitleModel)TitleBO.Instance.FindByPrimaryKey(mP.TitleID)).Code;

                if (mP.Firstname != mOP.Firstname)
                    _des = _des + "|GF" + mP.Firstname;

                if (mP.LastName != mOP.LastName || mP.MiddleName != mOP.MiddleName)
                    _des = _des + "|GN" + mP.LastName + " " + mP.MiddleName;

                if (mP.NationalityID != mOP.NationalityID)
                    _des = _des + "|GL" + ((NationalityModel)NationalityBO.Instance.FindByPrimaryKey(mP.NationalityID)).Code;

                if (_des != "GC|G#" + mP.ID)
                {
                    _des = _des + "|";
                    //Insert
                    IF_Interface("GC", _des.Trim());
                }
            }

            else if (_type == 1)
            {
                string _des = "GC|G#" + mP.ID;
                _des = _des + "|GT" + ((TitleModel)TitleBO.Instance.FindByPrimaryKey(mP.TitleID)).Code;
                _des = _des + "|GF" + mP.Firstname;
                _des = _des + "|GN" + mP.LastName + " " + mP.MiddleName;
                _des = _des + "|";
                //Insert
                IF_Interface("GC", _des.Trim());
            }
        }

        public static void IF_GC(ProfileModel mP, int _NewProID, ProfileModel mOP, int _OldProID, int _type, ProcessTransactions pt)
        {
            if (IF_IN() == "0")
                return;
            //Get RoomNo
            DataTable dt = TextUtils.Select("SELECT RoomNo, ID FROM dbo.Reservation WITH (NOLOCK) WHERE ProfileIndividualID = " + mP.ID + " AND Status IN (0,5,1,6) ORDER BY ArrivalDate");
            string _RoomNo = "", _ID = "";
            if (dt.Rows.Count > 0)
            {
                _RoomNo = dt.Rows[0]["RoomNo"].ToString();
            }

            if (_type == 0)
            {
                string _des = "";
                _des = "GC|RN" + _RoomNo + "|G#" + mP.ID;
                //_des = "GC|G#" + mP.ID;

                if (mP == null)
                    mP = (ProfileModel)pt.FindByPK("Profile", _NewProID);
                if (mOP == null)
                    mOP = (ProfileModel)pt.FindByPK("Profile", _OldProID);

                string _nat = "";
                if (mP.NationalityID > 0)
                    _nat = ((NationalityModel)NationalityBO.Instance.FindByPrimaryKey(mP.NationalityID)).Code;

                if (mP.TitleID != mOP.TitleID
                    || mP.Firstname != mOP.Firstname
                    || mP.LastName != mOP.LastName
                    || mP.MiddleName != mOP.MiddleName
                    || mP.NationalityID != mOP.NationalityID
                    || mP.ID != mOP.ID)
                {
                    if (mP.TitleID != 0)
                        _des = _des + "|GT" + ((TitleModel)TitleBO.Instance.FindByPrimaryKey(mP.TitleID)).Code + "|GF" + mP.Firstname
                            + "|GN" + mP.LastName + " " + mP.MiddleName + "|GL" + _nat;
                    else
                        _des = _des + "|GF" + mP.Firstname + "|GN" + mP.LastName + " " + mP.MiddleName
                            + "|GL" + _nat;
                }

                ////if (mP.TitleID != mOP.TitleID)
                //_des = _des + "|GT" + ((TitleModel)TitleBO.Instance.FindByPrimaryKey(mP.TitleID)).Code;

                ////if (mP.Firstname != mOP.Firstname)
                //_des = _des + "|GF" + mP.Firstname;

                ////if (mP.LastName != mOP.LastName || mP.MiddleName != mOP.MiddleName)
                //_des = _des + "|GN" + mP.LastName + " " + mP.MiddleName;

                //if (mP.NationalityID != mOP.NationalityID)
                //    if (mP.NationalityID > 0)
                //        _des = _des + "|GL" + ((NationalityModel)NationalityBO.Instance.FindByPrimaryKey(mP.NationalityID)).Code;
                //    else
                //        _des = _des + "|GL";


                //if (_des != "GC|G#" + mP.ID)
                if (_des != "GC|RN" + _RoomNo + "|G#" + mP.ID)
                {
                    _des = _des + "|";
                    //Insert
                    IF_Interface("GC", _des.Trim());
                }
            }

            else if (_type == 1)
            {
                string _title = "";
                if (mP.TitleID > 0)
                    _title = ((TitleModel)TitleBO.Instance.FindByPrimaryKey(mP.TitleID)).Code;
                string _des = "GC|RN" + _RoomNo;
                _des = _des + "|G#" + mP.ID;
                _des = _des + "|GT" + _title;
                _des = _des + "|GF" + mP.Firstname;
                _des = _des + "|GN" + mP.LastName + " " + mP.MiddleName;
                _des = _des + "|";
                //Insert
                IF_Interface("GC", _des.Trim());
            }
        }

        /// <summary>
        /// Change Main guest, Room Share
        /// </summary>
        /// <param name="_RsvID"></param>
        /// <param name="_GuestID"></param>
        /// <param name="_RoomNo"></param>
        /// <param name="_IsMG"></param>
        public static void IF_GC(int _RsvID, int _GuestID, string _RoomNo, int _IsMG)
        {
            if (IF_IN() == "0")
                return;

            string _des = "";
            //_des = "GC|R#" + _RsvID + "|G#" + _GuestID + "|RN" + _RoomNo + "|GS" + _IsMG + "|";
            _des = "GC|RN" + _RoomNo + "|R#" + _RsvID + "|G#" + _GuestID + "|GS" + _IsMG + "|";
            //Insert
            IF_Interface("GC", _des.Trim());
        }

        /// <summary>
        /// Reservation Cancel; 
        /// REA|R#4456|
        /// </summary>
        /// <param name="mR"></param>
        public static void IF_REA(int _RsvID)
        {
            if (IF_IN() == "0")
                return;
            string _des = "";
            _des = "REA|R#" + _RsvID + "|";
            //Insert
            IF_Interface("REA", _des);
        }

        /// <summary>
        /// Reservaion Data Change;
        /// _type: 0: change Rsv; 1: Assign Room; 2: Change Mainguest
        /// ---------------------------------------
        /// REC|R#1234|G#8874|GTDr.|GLVNM|GD110330|; 
        /// REC|R#1234|G#8922|GTMs.|GFHoa|GNVuong Kieu|GLKOR|
        /// Assign: REC|R#1234|RN7533|
        /// --------------------------------------
        /// </summary>
        /// <param name="mR"></param>
        /// <param name="_NewRsvID"></param>
        /// <param name="mOR"></param>
        /// <param name="_OldRsvID"></param>
        /// <param name="pt"></param>
        /// <param name="_type"></param>
        public static void IF_REC(ReservationModel mR, int _NewRsvID, ReservationModel mOR, int _OldRsvID, string _RoomNo, int _isMG, int _type)
        {
            if (IF_IN() == "0")
                return;
            //Change Rsv
            if (_type == 0)
            {
                if (mR == null)
                    mR = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(_NewRsvID);
                if (mOR == null)
                    mOR = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(_OldRsvID);

                string _des = "";
                _des = "REC|R#" + mR.ID + "|G#" + mR.ProfileIndividualId;

                if (mR.Title != mOR.Title)
                    _des = _des + "|GT" + mR.Title;

                if (mR.Country != mOR.Country)
                    _des = _des + "|GL" + mR.Country;

                if (mR.LastName != mOR.LastName || mR.ProfileIndividualId != mOR.ProfileIndividualId)
                {
                    string _guest = "";
                    string[] _nam = ReservationBO.SplitStringStandard(mR.LastName, 0);
                    if (_nam[0] != null)
                    {
                        _guest = "|GF" + mR.FirstName + "|GN" + _nam[1].ToString() + " " + _nam[2].ToString().Trim();
                    }

                    _des = _des + _guest.Trim();
                }
                if (TextUtils.CompareDate(mR.ArrivalDate, mOR.ArrivalDate) != 0)
                    _des = _des + "|GA" + mR.ArrivalDate.ToString("yyMMdd");
                if (TextUtils.CompareDate(mR.DepartureDate, mOR.DepartureDate) != 0)
                    _des = _des + "|GD" + mR.DepartureDate.ToString("yyMMdd");

                if (mR.GroupCode.Trim() != "")
                    _des = _des + "|GG" + "[" + mR.ConfirmationNo + "]" + mR.GroupCode;
                else
                    _des = _des + "|GG" + "[" + mR.ConfirmationNo + "]";

                if (mR.RoomNo != mOR.RoomNo)
                    _des = _des + "|RN" + mR.RoomNo;

                if (_des != "REC|R#" + mR.ID + "|G#" + mR.ProfileIndividualId)
                {
                    _des = _des + "|";
                    //Insert
                    IF_Interface("REC", _des.Trim());
                }
            }

            //Assign
            else if (_type == 1)
            {
                string _des = "REC|R#" + _NewRsvID + "|RN" + _RoomNo + "|";

                //Insert
                IF_Interface("REC", _des.Trim());
            }

            //Change main guets
            else if (_type == 2)
            {
                string _des = "REC|R#" + _NewRsvID + "|GS" + _isMG + "|";

                //Insert
                IF_Interface("REC", _des.Trim());
            }
            //Change Arrival, Dep
            else if (_type == 3)
            {

            }
        }

        /// <summary>
        /// Assign, UnAssign Room
        /// </summary>
        /// <param name="_RsvID"></param>
        /// <param name="_RoomNo"></param>
        public static void IF_REC(int _RsvID, string _RoomNo)
        {
            if (IF_IN() == "0")
                return;

            string _des = "";
            _des = "REC|R#" + _RsvID + "|RN" + _RoomNo + "|";
            //Insert
            IF_Interface("REC", _des.Trim());
        }
        /// <summary>
        /// Change Arr, Dep
        /// </summary>
        /// <param name="_RsvID"></param>
        /// <param name="_Arr"></param>
        /// <param name="_Dep"></param>
        public static void IF_REC(int _RsvID, int _GuesID, DateTime _Arr, DateTime _Dep)
        {
            if (IF_IN() == "0")
                return;

            string _des = "";
            _des = "REC|R#" + _RsvID + "|G#" + _GuesID + "|GA" + _Arr.ToString("yyMMdd") + "|GD" + _Dep.ToString("yyMMdd") + "|";
            //Insert
            IF_Interface("REC", _des.Trim());
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mR"></param>
        /// <param name="_RsvID"></param>
        public static void IF_REN(ReservationModel mR, int _RsvID)
        {
            //If is insert (Config)
            if (IF_IN() == "0")
                return;

            if (mR == null)
                mR = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(_RsvID);

            string _des = "REN|R#" + mR.ID + "|G#" + mR.ProfileIndividualId + "|GA" + mR.ArrivalDate.ToString("yyMMdd") + "|GD" + mR.DepartureDate.ToString("yyMMdd");
            string _guest = "";

            string[] _nam = ReservationBO.SplitStringStandard(mR.LastName, 0);
            if (_nam[0] != null)
            {
                if (mR.Title.Trim() != "")
                    _guest = "|GT" + mR.Title + "|GF" + mR.FirstName + "|GN" + _nam[1].ToString() + " " + _nam[2].ToString().Trim();
                else
                    _guest = "|GF" + mR.FirstName + "|GN" + _nam[1].ToString() + " " + _nam[2].ToString().Trim();
            }
            _des = _des + _guest;
            if (mR.RoomNo != "")
                _des = _des + "|RN" + mR.RoomNo;
            if (mR.MainGuest == true)
                _des = _des + "|GS0";
            if (mR.GroupCode.Trim() != "")
                _des = _des + "|GG" + "[" + mR.ConfirmationNo + "]" + mR.GroupCode;
            else
                _des = _des + "|GG" + "[" + mR.ConfirmationNo + "]";

            _des = _des + "|";
            //Insert
            IF_Interface("REN", _des.Trim());
        }

        /// <summary>
        /// Reinstate Check in; 
        /// RIS|R#2378|
        /// </summary>
        /// <param name="mR"></param>
        public static void IF_RICI(int _RsvID, int _GuestID, string _RoomNo)
        {
            //If is insert (Config)
            if (IF_IN() == "0")
                return;
            string _des = "RICI|R#" + _RsvID + "|G#" + _GuestID + "|RN" + _RoomNo + "|";
            //Insert
            IF_Interface("RICI", _des.Trim());
        }
        /// <summary>
        /// Hủy check out
        /// </summary>
        /// <param name="_RsvID"></param>
        /// <param name="_GuestID"></param>
        /// <param name="_RoomNo"></param>
        public static void IF_RICO(int _RsvID, int _GuestID, string _RoomNo)
        {
            //If is insert (Config)
            if (IF_IN() == "0")
                return;
            string _des = "RICO|R#" + _RsvID + "|G#" + _GuestID + "|RN" + _RoomNo + "|";
            //Insert
            IF_Interface("RICO", _des.Trim());
        }
        /// <summary>
        /// Reinstate Cancellation
        /// </summary>
        /// <param name="_RsvID"></param>
        /// <param name="_GuestID"></param>
        public static void IF_RICA(int _RsvID, int _GuestID)
        {
            //If is insert (Config)
            if (IF_IN() == "0")
                return;
            string _des = "RICA|R#" + _RsvID + "|G#" + _GuestID + "|";
            //Insert
            IF_Interface("RICA", _des.Trim());
        }

        /// <summary>
        /// Room Move Guest; 
        /// RG|R#1234|RN7132|
        /// </summary>
        /// <param name="mR"></param>
        public static void IF_RG(int _RsvID, string _RoomNo, int _ProfileID, bool _MG)
        {
            //If is insert (Config)
            if (IF_IN() == "0")
                return;
            string _mainguest = "";
            if (_MG == true)
                _mainguest = "|GS0";
            else
                _mainguest = "|GS1";

            string _des = "RG|R#" + _RsvID + "|RN" + _RoomNo + "|G#" + _ProfileID + _mainguest + "|";
            //Insert
            IF_Interface("RG", _des.Trim());
        }

        /// <summary>
        /// Room Move All Guest; 
        /// RR|RO7132|RN8425|
        /// </summary>
        /// <param name="mR"></param>
        public static void IF_RR(int _RsvID, string _RNo, string _RO, int _ProfileID, bool _MG)
        {
            //If is insert (Config)
            if (IF_IN() == "0")
                return;
            string _mainguest = "";
            if (_MG == true)
                _mainguest = "|GS0";
            else
                _mainguest = "|GS1";

            string _des = "RR|R#" + _RsvID + "|RO" + _RO + "|RN" + _RNo + "|G#" + _ProfileID + _mainguest + "|";
            //Insert
            IF_Interface("RR", _des.Trim());
        }

        /// <summary>
        /// Guest message text and other details - online
        /// _type: 0:[New, change];  1:[Change status]
        /// </summary>
        /// <param name="_mM"></param>
        public static void IF_XL(MessageModel _mM, int _MesID, int _GuestID, string _RoomNo, int _type, ProcessTransactions pt)
        {
            //If is insert (Config)
            if (IF_IN() == "0")
                return;
            string _des = "";
            if (_mM == null)
            {
                if (pt == null)
                    _mM = (MessageModel)MessageBO.Instance.FindByPrimaryKey(_MesID);
                else
                    _mM = (MessageModel)pt.FindByPK("Message", _MesID);
            }
            //New
            if (_type == 0)
            {
                _des = "XL|MI" + _MesID + "|G#" + _GuestID + "|MT" + _mM.Message
                            + "|DA" + (_mM.CreateDate?.ToString("yyMMdd") ?? "") + "|TI" + (_mM.CreateDate?.ToString("HH:mm:ss") ?? "");
                if (_RoomNo != "")
                    _des = _des + "|RN" + _RoomNo;
            }
            //Change Status
            else if (_type == 1)
                _des = "XL|MI" + _MesID + "|MS1";

            _des = _des + "|";
            //Insert
            IF_Interface("XL", _des.Trim());
        }

        /// <summary>
        /// Guest Message Text and other detail
        /// </summary>
        /// <param name="_mM"></param>
        public static void IF_XT(MessageModel _mM, int _MesID, int _GuestID, string _RoomNo, ProcessTransactions pt)
        {
            //If is insert (Config)
            if (IF_IN() == "0")
                return;
            if (_mM == null)
            {
                if (pt == null)
                    _mM = (MessageModel)MessageBO.Instance.FindByPrimaryKey(_MesID);
                else
                    _mM = (MessageModel)pt.FindByPK("Message", _MesID);
            }
            string _des = "XT|MI" + _MesID + "|G#" + _GuestID + "|MT" + _mM.Message
                        + "|DA" + (_mM.CreateDate?.ToString("yyMMdd") ?? "") + "|TI" + (_mM.CreateDate?.ToString("HH:mm:ss") ?? "");
            if (_RoomNo != "")
                _des = _des + "|RN" + _RoomNo;

            _des = _des + "|";

            //Insert
            IF_Interface("XT", _des.Trim());
        }

        /// <summary>
        /// Guest Message Delete
        /// </summary>
        /// <param name="_mM"></param>
        public static void IF_XD(MessageModel _mM, int _MesID, int _GuestID, string _RoomNo, ProcessTransactions pt)
        {
            //If is insert (Config)
            if (IF_IN() == "0")
                return;
            if (_mM == null)
            {
                if (pt == null)
                    _mM = (MessageModel)MessageBO.Instance.FindByPrimaryKey(_MesID);
                else
                    _mM = (MessageModel)pt.FindByPK("Message", _MesID);
            }
            string _des = "XD|MI" + _MesID;
            if (_RoomNo != "")
                _des = _des + "|RN" + _RoomNo;
            if (_GuestID != 0)
                _des = _des + "|G#" + _GuestID;

            _des = _des + "|";
            //Insert
            IF_Interface("XD", _des.Trim());
        }


        /// <summary>
        /// Create New Card for guest
        /// SCN|R#1234|G#8874|GA110327|GD110329|GNPhan Duy|RN7425|CA12345678|
        /// CA --> Ma the
        /// </summary>
        /// <param name="mR"></param>
        public static void IF_SCN(int Rsv_ID, int Rsv_ProfileID, string Rsv_GuestName, string Rsv_RoomNo, string CardID, DateTime ArrDate, DateTime DepDate, int ShareRoom)
        {
            if (IF_IN() == "0")
                return;

            string _des = "SCN|R#" + Rsv_ID + "|G#" + Rsv_ProfileID + "|GA" + ArrDate.ToString("yyMMdd")
                + "|GD" + DepDate.ToString("yyMMdd") + "|GN" + Rsv_GuestName.Trim() + "|RN" + Rsv_RoomNo + "|CA" + CardID + "|SR" + ShareRoom;

            _des = _des + "|";
            //Insert
            IF_Interface("SCN", _des);
        }

        /// <summary>
        /// Cancel Card for guest
        /// SCC|R#1234|G#8874|GA110327|GD110329|GNPhan Duy|RN7425|CA12345678|
        /// CA --> Ma the
        /// </summary>
        /// <param name="mR"></param>
        public static void IF_SCC(int Rsv_ID, int Rsv_ProfileID, string Rsv_GuestName, string Rsv_RoomNo, string CardID, DateTime ArrDate, DateTime DepDate, int ShareRoom)
        {
            if (IF_IN() == "0")
                return;

            string _des = "SCC|R#" + Rsv_ID + "|G#" + Rsv_ProfileID + "|GA" + ArrDate.ToString("yyMMdd")
                + "|GD" + DepDate.ToString("yyMMdd") + "|GN" + Rsv_GuestName.Trim() + "|RN" + Rsv_RoomNo + "|CA" + CardID + "|SR" + ShareRoom;

            _des = _des + "|";
            //Insert
            IF_Interface("SCC", _des);
        }

        /// <summary>
        /// Create package
        /// SCP|R#1234|VA1|GA110327|SA0|CA12345678|DA151223|
        /// CA --> Ma the
        /// </summary>
        /// <param name="mR"></param>
        public static void IF_SCP(int Rsv_ID, int VAP, int Safari, string CardID, DateTime BusDate, ProcessTransactions pt)
        {
            if (IF_IN() == "0")
                return;

            string _des = "SCP|R#" + Rsv_ID + "|VA" + VAP + "|SA" + Safari + "|CA" + CardID + "|DA" + BusDate.ToString("yyMMdd");

            _des = _des + "|";
            //Insert
            IF_Interface("SCP", _des, pt);
        }

        public static void IF_Process_SMC(ReservationModel mR, ProcessTransactions pt)
        {
            if (IF_IN() == "0")
                return;

            string[] paramName = new string[3];
            object[] paramValue = new object[3];
            paramName[0] = "@ReservationID";
            paramValue[0] = mR.ID;
            paramName[1] = "@ShareRoom";
            paramValue[1] = mR.ShareRoom;
            paramName[2] = "@Type";
            paramValue[2] = 0;
            SqlParameter[] param = [
                    new SqlParameter("@ReservationID", mR.ID),
                    new SqlParameter("@ShareRoom", mR.ShareRoom),
                    new SqlParameter("@Type", 0)
                    ];
            DataTable Souce = DataTableHelper.getTableData("spIF_Process_SMC", param);
            if (Souce.Rows.Count == 0)
            {
                //Lay danh sach khach de huy ko cho qua VAP - SAFARI
                SqlParameter[] param2 = [
                    new SqlParameter("@ReservationID", mR.ID),
                    new SqlParameter("@ShareRoom", mR.ShareRoom),
                    new SqlParameter("@Type", 1)
                    ];
                Souce = DataTableHelper.getTableData("spIF_Process_SMC", param);
            }
            //Xu ly
            if (Souce.Rows.Count > 0)
            {
                DateTime _BusDate = TextUtils.GetBussinessDateTime();
                for (int i = 0; i < Souce.Rows.Count; i++)
                {
                    IF_SCP(TextUtils.ToInt(Souce.Rows[i]["RsvID"]?.ToString() ?? "0"),
                           TextUtils.ToInt(Souce.Rows[i]["VAP"]?.ToString() ?? "0"),
                           TextUtils.ToInt(Souce.Rows[i]["Safari"]?.ToString() ?? "0"),
                           Souce.Rows[i]["CardID"].ToString(),
                           _BusDate,
                           pt);
                }
            }
        }

        /// <summary>
        /// false: Not cancel
        /// true: cancel
        /// </summary>
        /// <param name="ResvID"></param>
        /// <returns></returns>
        public static bool CheckReinstateCI(int ResvID)
        {
            string _deposit = _GetDepositCode();
            DataTable dt = TextUtils.Select("SELECT COUNT(ID) FROM dbo.FolioDetail WITH (NOLOCK) WHERE [Status] = 0 AND ReservationID = " + ResvID + " AND TransactionCode <>  '" + _deposit + "' ");
            int count = TextUtils.ToInt(dt.Rows[0][0]?.ToString() ?? "0");
            return count <= 0;
        }

        /// <summary>
        /// true: OK
        /// false: not ok
        /// </summary>
        /// <returns></returns>

        public static string _GetDepositCode()
        {
            string _str = "";
            DataTable dt = TextUtils.Select("SELECT dbo.fnGetConfigsystem('DEPOSIT')");
            if (dt != null)
                if (dt.Rows.Count > 0)
                    _str = dt.Rows[0][0].ToString() ?? "";

            return _str;
        }

        /// <summary>
        /// Xử lý bữa ăn của khách 
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="ReservationID"></param>
        /// <returns></returns>
        public static void ProcessMeal(ProcessTransactions pt, ReservationModel mR, bool _IsDelete)
        {
            //if (Global.HotelID == 2)
            //{
            DataTable tbSource = null;
            if (mR.MainGuest == true)
            {
                tbSource = TextUtils.Select("SELECT DISTINCT a.BeginDate, a.EndDate, b.Breakfast, b.Lunch, b.Dinner, a.PackageID " +
                                                      "FROM dbo.ReservationPackage a WITH (NOLOCK), dbo.Package b WITH (NOLOCK) " +
                                                      "WHERE a.PackageID = b.ID AND ReservationID =" + mR.ID + " " +
                                                      "ORDER BY a.BeginDate, b.Breakfast DESC ");
            }
            else
            {
                tbSource = TextUtils.Select("SELECT DISTINCT a.BeginDate, a.EndDate, b.Breakfast, b.Lunch, b.Dinner, a.PackageID " +
                                                     "FROM dbo.ReservationPackage a WITH (NOLOCK), dbo.Package b WITH (NOLOCK) " +
                                                     "WHERE a.PackageID = b.ID AND ReservationID  IN (SELECT ID FROM dbo.Reservation WITH (NOLOCK) WHERE MainGuest =1 AND ShareRoom = " + mR.ShareRoom + ") " +
                                                     "ORDER BY a.BeginDate, b.Breakfast DESC ");
            }
            string _BusDate = TextUtils.GetBussinessDateTime().ToString("yyyy/MM/dd");
            //Nếu có pck xử lý bữa ăn
            if (tbSource.Rows.Count > 0)
            {
                for (int i = 0; i < tbSource.Rows.Count; i++)
                {
                    string _begindate = Convert.ToDateTime(tbSource.Rows[i]["BeginDate"]).ToString("yyyy/MM/dd");
                    string _enddate = Convert.ToDateTime(tbSource.Rows[i]["EndDate"]).AddDays(-1).ToString("yyyy/MM/dd");
                    int _Breakfast = TextUtils.ToInt(tbSource.Rows[i]["Breakfast"]?.ToString() ?? "0");
                    int _lunch = TextUtils.ToInt(tbSource.Rows[i]["Lunch"]?.ToString() ?? "0");
                    int _dinner = TextUtils.ToInt(tbSource.Rows[i]["Dinner"]?.ToString() ?? "0");

                    #region 1.Update meal to ReservationRate
                    if (pt != null)
                    {
                        pt.UpdateCommand("UPDATE dbo.ReservationRate WITH (ROWLOCK) SET Breakfast = " + _Breakfast + ", Lunch =" + _lunch + ",Dinner =" + _dinner + " " +
                                         "WHERE ISNULL(FixedMeal,0) =0 AND ReservationID IN (SELECT ID FROM dbo.Reservation with (nolock) where ShareRoom = " + mR.ShareRoom + ") " +
                                         "AND DATEDIFF(DAY,RateDate,'" + _BusDate + "') <= 0 " +
                                         "AND DATEDIFF(DAY,RateDate,'" + _begindate + "') <= 0 " +
                                         "AND DATEDIFF(DAY,RateDate,'" + _enddate + "') >= 0 ");
                    }
                    else
                    {
                        pt.UpdateCommand("UPDATE dbo.ReservationRate WITH (ROWLOCK) SET Breakfast = " + _Breakfast + ", Lunch =" + _lunch + ",Dinner =" + _dinner + " " +
                                                "WHERE ISNULL(FixedMeal,0) =0 AND ReservationID IN (SELECT ID FROM dbo.Reservation with (nolock) where ShareRoom = " + mR.ShareRoom + ") " +
                                                "AND DATEDIFF(DAY,RateDate,'" + _BusDate + "') <= 0 " +
                                                "AND DATEDIFF(DAY,RateDate,'" + _begindate + "') <= 0 " +
                                                "AND DATEDIFF(DAY,RateDate,'" + _enddate + "') >= 0 ");
                    }
                    #endregion

                    #region 2.Ngày đến bằng ngày bắt đầu pck --> Chỉ được ăn tối
                    if (TextUtils.CompareDate(mR.ArrivalDate, Convert.ToDateTime(tbSource.Rows[i]["BeginDate"])) == 0)
                    {
                        //Nếu được ăn mới update
                        if (_Breakfast == 1 || _lunch == 1 || _dinner == 1)
                        {
                            if (pt != null)
                            {
                                pt.UpdateCommand("UPDATE dbo.ReservationRate WITH (ROWLOCK) SET Breakfast = 0, Lunch =0,Dinner =" + _dinner + " " +
                                                 "WHERE ISNULL(FixedMeal,0) =0 AND ReservationID IN (SELECT ID FROM dbo.Reservation with (nolock) where ShareRoom = " + mR.ShareRoom + ") " +
                                                 "AND DATEDIFF(DAY,RateDate,'" + mR.ArrivalDate.ToString("yyyy/MM/dd") + "') = 0 ");
                            }
                            else
                            {
                                pt.UpdateCommand("UPDATE dbo.ReservationRate WITH (ROWLOCK) SET Breakfast = 0, Lunch =0,Dinner =" + _dinner + " " +
                                                "WHERE ISNULL(FixedMeal,0) =0 AND ReservationID IN (SELECT ID FROM dbo.Reservation with (nolock) where ShareRoom = " + mR.ShareRoom + ") " +
                                                "AND DATEDIFF(DAY,RateDate,'" + mR.ArrivalDate.ToString("yyyy/MM/dd") + "') = 0 ");
                            }
                        }
                    }
                    #endregion

                    #region 4.Ngày đi bằng ngày kết thúc pck --> Chỉ được ăn sáng, trưa
                    if (TextUtils.CompareDate(mR.DepartureDate, Convert.ToDateTime(tbSource.Rows[i]["EndDate"])) == 0)
                    {
                        //Nếu được ăn mới update
                        if (_Breakfast == 1 || _lunch == 1 || _dinner == 1)
                        {
                            if (pt != null)
                            {
                                pt.UpdateCommand("UPDATE dbo.Reservation WITH (ROWLOCK) SET Breakfast = " + _Breakfast + ", Lunch =" + _lunch + ",Dinner =0 " +
                                                 "WHERE ISNULL(FixedMeal,0) =0 AND ID IN (SELECT ID FROM dbo.Reservation with (nolock) where ShareRoom = " + mR.ShareRoom + ") " +
                                                 "AND DATEDIFF(DAY,DepartureDate,'" + mR.DepartureDate.ToString("yyyy/MM/dd") + "') = 0 ");
                            }
                            else
                            {
                                TextUtils.UpdateCommand("UPDATE dbo.Reservation WITH (ROWLOCK) SET Breakfast = " + _Breakfast + ", Lunch =" + _lunch + ",Dinner =0 " +
                                                 "WHERE ISNULL(FixedMeal,0) =0 AND ID IN (SELECT ID FROM dbo.Reservation with (nolock) where ShareRoom = " + mR.ShareRoom + ") " +
                                                 "AND DATEDIFF(DAY,DepartureDate,'" + mR.DepartureDate.ToString("yyyy/MM/dd") + "') = 0 ");
                            }
                        }
                    }
                    #endregion
                }
            }
            //Trường hợp xóa và không phải tạo mới rsv
            else if (_IsDelete == true && tbSource.Rows.Count == 0)
            {
                #region Clear meal on ReservationRate
                if (pt != null)
                {
                    pt.UpdateCommand("UPDATE dbo.ReservationRate WITH (ROWLOCK) SET Breakfast = 0, Lunch =0,Dinner =0 " +
                                     "WHERE ISNULL(FixedMeal,0) =0 AND ReservationID IN (SELECT ID FROM dbo.Reservation with (nolock) where ShareRoom = " + mR.ShareRoom + ") " +
                                     "AND DATEDIFF(DAY,RateDate,'" + _BusDate + "') <= 0 ");
                }
                else
                {
                    TextUtils.UpdateCommand("UPDATE dbo.ReservationRate WITH (ROWLOCK) SET Breakfast = 0, Lunch =0,Dinner =0 " +
                                    "WHERE ISNULL(FixedMeal,0) =0 AND ReservationID IN (SELECT ID FROM dbo.Reservation with (nolock) where ShareRoom = " + mR.ShareRoom + ") " +
                                    "AND DATEDIFF(DAY,RateDate,'" + _BusDate + "') <= 0 ");
                }
                #endregion
            }
            //}
        }


        #endregion
        /// <summary>
        /// Kiểm tra số lượng đã được Share
        /// -- CSS, 21/05/2010
        /// </summary>
        /// <param name="ShareRoom"></param>
        /// <returns></returns>
        public static int CheckShareRoom(int ShareRoom)
        {
            Expression expR = new Expression("ShareRoom", ShareRoom, "=");
            //expR = expR.And(new Expression("Type", Type, "="));
            ArrayList arrR = ReservationBO.Instance.FindByExpression(expR);
            if (arrR.Count > 0)
                return arrR.Count;
            else
                return 0;
        }

        public static int CheckShareRoomTotal(int ShareRoom)
        {
            DataTable dtR = TextUtils.Select("SELECT SUM(NoOfAdult + NoOfChild + NoOfChild1 + NoOfChild2) as TotalPerson " +
                                             "FROM Reservation WITH (NOLOCK)" +
                                             "WHERE ShareRoom = " + ShareRoom + " ");
            if (dtR.Rows.Count > 0)
                return int.Parse(dtR.Rows[0][0].ToString());
            else
                return 0;
        }

        #region GenCreateRoomShare Transaction And no Transaction
        /// <summary>
        /// Tạo Room Share cho phiếu đặt phòng với same thông tin khách chính
        /// -- CSS, 27/03/2010
        /// </summary>
        public static void GenCreateRoomShare(int ReservationID, int UserID)
        {
            int pID;
            DateTime pArrival;
            DateTime _BusDate = TextUtils.GetBusinessDateTime();
            //CSS, 26/03/2010
            if (ReservationID > 0)
            {
                ReservationModel mOR = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(ReservationID);

                ProcessTransactions pt = new ProcessTransactions();
                pt.OpenConnection();
                pt.BeginTransaction();
                try
                {
                    pArrival = mOR.ArrivalDate;

                    #region Tạo mới thông tin Profile cho RS
                    int _ProfileIndividualID = CreateProfile(mOR.ProfileIndividualId, UserID, pt);
                    #endregion

                    #region Tạo mới phiếu đặt phòng cho RS

                    #region Cập nhật dữ liệu vào bảng Reservation
                    mOR.UserInsertId = mOR.UserUpdateId = UserID;
                    mOR.CreateDate = mOR.UpdateDate = mOR.SpecialUpdateDate = DateTime.Now;
                    mOR.CreateBy = mOR.UpdateBy = mOR.SpecialUpdateBy = Global.UserName;
                    mOR.ReservationDate = _BusDate;
                    //trường hợp ngày đến = BB Status = DI
                    if (TextUtils.CompareDate(mOR.ArrivalDate, _BusDate) == 0)
                    {
                        //Trường hợp CL và WL thì giữ nguyên trạng thái
                        if (mOR.Status != 3 && mOR.Status != 4)
                            mOR.Status = 5;
                    }
                    else
                    {
                        //Trường hợp CL và WL thì giữ nguyên trạng thái
                        if (mOR.Status != 3 && mOR.Status != 4)
                            mOR.Status = 0;
                    }
                    //Trường hợp ngày đến < Ngày BB lấy ngày đến = BB
                    if (TextUtils.CompareDate(mOR.ArrivalDate, _BusDate) == -1)
                    {
                        mOR.ArrivalDate = _BusDate;
                        //Trường hợp CL và WL thì giữ nguyên trạng thái
                        if (mOR.Status != 3 && mOR.Status != 4)
                        {
                            mOR.Status = 5;
                        }
                        mOR.NoOfNight = TextUtils.NumberOfDay("d", mOR.DepartureDate, mOR.ArrivalDate);
                    }
                    mOR.PostingMaster = false;
                    mOR.NoOfRoom = 0;
                    mOR.Packages = "";
                    mOR.FixedCharge = "";
                    mOR.ItemInventory = "";
                    mOR.AccompanyName = "";
                    mOR.Specials = "";
                    mOR.RoutingTransaction = "";
                    mOR.RoutingToProfile = "";
                    mOR.Vip = "";
                    mOR.VipId = 0;
                    mOR.MemberType = "";
                    mOR.MemberNo = "";
                    mOR.MemberLevel = "";
                    mOR.Email = "";
                    mOR.Phone = "";
                    mOR.Address = "";
                    mOR.MainGuest = false;
                    mOR.RateCodeId = 0;
                    mOR.Rate = 0;
                    mOR.RateAfterTax = 0;
                    mOR.TotalAmount = 0;
                    mOR.NoOfAdult = 0;
                    mOR.NoOfChild = 0;
                    mOR.NoOfChild1 = 0;
                    mOR.NoOfChild2 = 0;
                    mOR.BalanceUSD = 0;
                    mOR.BalanceVND = 0;
                    mOR.DiscountAmount = mOR.DiscountRate = 0;
                    mOR.DiscountReason = "";
                    mOR.ProfileIndividualId = _ProfileIndividualID;
                    mOR.ShareRoom = mOR.ShareRoom;

                    pID = (int)pt.Insert(mOR);
                    //Update ReservationNo vào bảng Rsv
                    mOR.ID = pID;
                    mOR.ReservationNo = pID.ToString();

                    pt.Update(mOR);

                    #endregion

                    #region Cập nhật dữ liệu vào bảng ReservationRate
                    //Trường hợp ngày đến và đi không thay đổi so với MG
                    if (TextUtils.CompareDate(pArrival, _BusDate) >= 0)
                    {
                        //Select dữ liệu từ bảng ReservationRate    
                        DataTable CRR = pt.getTable("spCheckReservationRate", "tbRsvR",
                                       new SqlParameter("@ReservationID", ReservationID));
                        for (int i = 0; i < CRR.Rows.Count; i++)
                        {
                            ReservationRateModel mRR = (ReservationRateModel)pt.FindByPK("ReservationRate", TextUtils.ToInt(CRR.Rows[i]["ID"].ToString()));
                            mRR.ReservationID = pID;
                            mRR.RateCodeID = 0;
                            mRR.Rate = 0;
                            mRR.RateAfterTax = 0;
                            mRR.DiscountRate = 0;
                            mRR.DiscountAmount = 0;
                            mRR.NoOfAdult = 0;
                            mRR.NoOfChild = 0;
                            mRR.NoOfChild1 = 0;
                            mRR.NoOfChild2 = 0;
                            mRR.RoomRevenueBeforeTax = 0;
                            mRR.RoomRevenueAfterTax = 0;
                            mRR.UserInsertID = UserID;
                            mRR.CreateDate = DateTime.Now;
                            mRR.UpdateDate = DateTime.Now;
                            mRR.UserUpdateID = UserID;
                            int pRsvRateID = (int)pt.Insert(mRR);

                            //Gán ID vào model
                            mRR.ID = pRsvRateID;
                        }
                    }
                    // Trường hợp nếu ngày đến có thay đổi so với MG
                    else
                    {
                        for (int i = 0; i < mOR.NoOfNight; i++)
                        {
                            ReservationRateModel mRR = new ReservationRateModel();
                            mRR.ReservationID = pID;
                            mRR.RateCodeID = 0;
                            mRR.RateDate = mOR.ArrivalDate.AddDays(i);
                            mRR.RateDate = new DateTime(mRR.RateDate.Year, mRR.RateDate.Month, mRR.RateDate.Day, 0, 0, 0);
                            mRR.Rate = 0;
                            mRR.RoomID = mOR.RoomId;
                            mRR.RoomNo = mOR.RoomNo;
                            mRR.RoomTypeID = mOR.RoomTypeId;
                            mRR.RoomType = mOR.RoomType;
                            mRR.MarketID = mOR.MarketId;
                            mRR.SourceID = mOR.SourceId;
                            mRR.AllotmentID = mOR.AllotmentId;
                            mRR.CurrencyID = TextUtils.GetMasterCurrency();
                            mRR.UserInsertID = mRR.UserUpdateID = UserID;
                            mRR.CreateDate = mRR.UpdateDate = DateTime.Now;
                            int pRsvRateID = (int)pt.Insert(mRR);

                            //Gán ID vào model
                            mRR.ID = pRsvRateID;
                        }
                    }
                    #endregion

                    #endregion

                    #region Ghi dữ liệu vào ReservationOption

                    //Cập nhật RS cho rsv gốc
                    int ReservationOptionID = ReservationBO.GetReservationOptionID(ReservationID, pt);
                    if (ReservationOptionID == 0)
                    {
                        ReservationOptionsModel mRO = new ReservationOptionsModel();
                        mRO.ReservationID = ReservationID;
                        mRO.Shares = true;
                        if (mOR.ProfileGroupId > 0)
                            mRO.GroupOptions = true;
                        pt.Insert(mRO);
                    }
                    else
                    {
                        ReservationOptionsModel mRO = (ReservationOptionsModel)pt.FindByPK("ReservationOptions", ReservationOptionID);
                        mRO.ID = ReservationOptionID;
                        mRO.Shares = true;
                        if (mOR.ProfileGroupId > 0)
                            mRO.GroupOptions = true;
                        pt.Update(mRO);
                    }
                    //Trường hợp có Routing thì ghi hoặc sửa dữ liệu trong bảng ReservationOptions
                    bool _Routing = false;
                    ReservationBO.CheckRouting(mOR.ConfirmationNo, ref _Routing, pt);
                    if (_Routing == true)
                    {
                        int pReservationOptionID = ReservationBO.GetReservationOptionID(pID, pt);
                        if (pReservationOptionID == 0)
                        {
                            ReservationOptionsModel mRO = new ReservationOptionsModel();
                            mRO.ReservationID = pID;
                            //mRO.Shares = true;                           
                            mRO.Routing = true;
                            pt.Insert(mRO);
                        }
                        else
                        {
                            ReservationOptionsModel mRO = (ReservationOptionsModel)pt.FindByPK("ReservationOptions", pReservationOptionID);
                            mRO.ID = pReservationOptionID;
                            //mRO.Shares = true;
                            mRO.Routing = true;
                            pt.Update(mRO);
                        }
                    }
                    #endregion

                    #region Update lại tên của khách ở cùng phòng vào từng đặt phòng (Trường ShareRoomName)
                    GetRoomShareName(mOR.ShareRoom, pt);
                    #endregion

                    #region Update lại tên của khách ở cùng phòng vào từng đặt phòng (Trường AccompanyName)
                    ReservationBO.GetAccompanyName(ReservationID, pt);
                    #endregion

                    #region Ghi dữ liệu vào bảng ReservationAmountByCurrency
                    ////Xóa dữ liệu trước khi Insert
                    //pt.DeleteByAttribute("ReservationAmountByCurrency", "ReservationID", pID.ToString());
                    ////Tính lại số liệu rồi ghi dữ liệu 
                    //ClassReservation.GetAmountByCurrency(pID, UserID, pt);
                    #endregion

                    #region Ghi dữ liệu vào bảng ReservationGroup và ReservationGroupAmountByCurrency
                    ////Chú ý phải thực hiện ghi dữ liệu vào bảng ReservationAmountByCurrency trước                            
                    //ClassReservation.CreateReservationGroup(pID, mOR.ConfirmationNo, UserID, "", pt);
                    #endregion

                    #region Interface
                    //if (ClassReservation.GetDateNoOfDay(mOR.ArrivalDate, _BusDate) <= 3)
                    IF_REN(mOR, ReservationID);
                    #endregion

                    //Xử lý bưa ăn của khách theo Package
                    ProcessMeal(pt, mOR, false);

                    //Nếu không bị lỗi - ghi dữ liệu vào bảng
                    pt.CommitTransaction();
                }
                catch (Exception ex)
                {
                    //Đóng connection
                    pt.CloseConnection();
                    throw new Exception(ex.Message);
                }
                //Nếu bị lỗi Rollback lại dữ liệu đã ghi
                finally
                {
                    pt.CloseConnection();
                }
            }
        }

        //No transaction
        public static void GenCreateRoomShareNoTransaction(int ReservationID, int UserID)
        {
            int pID;
            DateTime pArrival;
            DateTime _BusDate = TextUtils.GetBusinessDateTime();
            if (ReservationID > 0)
            {
                ReservationModel mOR = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(ReservationID);
                try
                {
                    pArrival = mOR.ArrivalDate;

                    #region Tạo mới thông tin Profile cho RS
                    int _ProfileIndividualID = CreateProfile(mOR.ProfileIndividualId, UserID);
                    #endregion

                    #region Tạo mới phiếu đặt phòng cho RS

                    #region Cập nhật dữ liệu vào bảng Reservation
                    mOR.UserInsertId = mOR.UserUpdateId = UserID;
                    mOR.CreateDate = mOR.UpdateDate = mOR.SpecialUpdateDate = DateTime.Now;
                    mOR.CreateBy = mOR.UpdateBy = mOR.SpecialUpdateBy = Global.UserName;
                    mOR.ReservationDate = _BusDate;
                    //trường hợp ngày đến = BB Status = DI
                    if (TextUtils.CompareDate(mOR.ArrivalDate, _BusDate) == 0)
                    {
                        //Trường hợp CL và WL thì giữ nguyên trạng thái
                        if (mOR.Status != 3 && mOR.Status != 4)
                            mOR.Status = 5;
                    }
                    else
                    {
                        //Trường hợp CL và WL thì giữ nguyên trạng thái
                        if (mOR.Status != 3 && mOR.Status != 4)
                            mOR.Status = 0;
                    }
                    //Trường hợp ngày đến < Ngày BB lấy ngày đến = BB
                    if (TextUtils.CompareDate(mOR.ArrivalDate, _BusDate) == -1)
                    {
                        mOR.ArrivalDate = _BusDate;
                        //Trường hợp CL và WL thì giữ nguyên trạng thái
                        if (mOR.Status != 3 && mOR.Status != 4)
                        {
                            mOR.Status = 5;
                        }
                        mOR.NoOfNight = TextUtils.NumberOfDay("d", mOR.DepartureDate, mOR.ArrivalDate);
                    }
                    mOR.PostingMaster = false;
                    mOR.NoOfRoom = 0;
                    mOR.Packages = "";
                    mOR.FixedCharge = "";
                    mOR.ItemInventory = "";
                    mOR.AccompanyName = "";
                    mOR.Specials = "";
                    mOR.RoutingTransaction = "";
                    mOR.RoutingToProfile = "";
                    mOR.Vip = "";
                    mOR.VipId = 0;
                    mOR.MemberType = "";
                    mOR.MemberNo = "";
                    mOR.MemberLevel = "";
                    mOR.Email = "";
                    mOR.Phone = "";
                    mOR.Address = "";
                    mOR.MainGuest = false;
                    mOR.RateCodeId = 0;
                    mOR.Rate = 0;
                    mOR.RateAfterTax = 0;
                    mOR.TotalAmount = 0;
                    mOR.NoOfAdult = 0;
                    mOR.NoOfChild = 0;
                    mOR.NoOfChild1 = 0;
                    mOR.NoOfChild2 = 0;
                    mOR.BalanceUSD = 0;
                    mOR.BalanceVND = 0;
                    mOR.DiscountAmount = mOR.DiscountRate = 0;
                    mOR.DiscountReason = "";
                    mOR.ProfileIndividualId = _ProfileIndividualID;
                    mOR.ShareRoom = mOR.ShareRoom;
                    pID = (int)ReservationBO.Instance.Insert(mOR);
                    //Update ReservationNo vào bảng Rsv
                    mOR.ID = pID;
                    mOR.PinCode = pID.ToString();
                    mOR.ReservationNo = pID.ToString();
                    ReservationBO.Instance.Update(mOR);
                    #endregion

                    #region Cập nhật dữ liệu vào bảng ReservationRate
                    //Trường hợp ngày đến và đi không thay đổi so với MG
                    if (TextUtils.CompareDate(pArrival, _BusDate) >= 0)
                    {
                        //Select dữ liệu từ bảng ReservationRate    
                        DataTable CRR = TextUtils.getTable("spCheckReservationRate",
                                       new SqlParameter("@ReservationID", ReservationID));
                        for (int i = 0; i < CRR.Rows.Count; i++)
                        {
                            ReservationRateModel mRR = (ReservationRateModel)ReservationRateBO.Instance.FindByPrimaryKey(TextUtils.ToInt(CRR.Rows[i]["ID"].ToString()));
                            mRR.ReservationID = pID;
                            mRR.RateCodeID = 0;
                            mRR.Rate = 0;
                            mRR.RateAfterTax = 0;
                            mRR.DiscountRate = 0;
                            mRR.DiscountAmount = 0;
                            mRR.NoOfAdult = 0;
                            mRR.NoOfChild = 0;
                            mRR.NoOfChild1 = 0;
                            mRR.NoOfChild2 = 0;
                            mRR.RoomRevenueBeforeTax = 0;
                            mRR.RoomRevenueAfterTax = 0;
                            mRR.UserInsertID = UserID;
                            mRR.CreateDate = DateTime.Now;
                            mRR.UpdateDate = DateTime.Now;
                            mRR.UserUpdateID = UserID;
                            int pRsvRateID = (int)ReservationRateBO.Instance.Insert(mRR);
                            //Gán ID vào model
                            mRR.ID = pRsvRateID;
                        }
                    }
                    // Trường hợp nếu ngày đến có thay đổi so với MG
                    else
                    {
                        for (int i = 0; i < mOR.NoOfNight; i++)
                        {
                            ReservationRateModel mRR = new ReservationRateModel();
                            mRR.ReservationID = pID;
                            mRR.RateCodeID = 0;
                            mRR.RateDate = mOR.ArrivalDate.AddDays(i);
                            mRR.RateDate = new DateTime(mRR.RateDate.Year, mRR.RateDate.Month, mRR.RateDate.Day, 0, 0, 0);
                            mRR.Rate = 0;
                            mRR.RoomID = mOR.RoomId;
                            mRR.RoomNo = mOR.RoomNo;
                            mRR.RoomTypeID = mOR.RoomTypeId;
                            mRR.RoomType = mOR.RoomType;
                            mRR.MarketID = mOR.MarketId;
                            mRR.SourceID = mOR.SourceId;
                            mRR.AllotmentID = mOR.AllotmentId;
                            mRR.CurrencyID = TextUtils.GetMasterCurrency();
                            mRR.UserInsertID = mRR.UserUpdateID = UserID;
                            mRR.CreateDate = mRR.UpdateDate = DateTime.Now;
                            int pRsvRateID = (int)ReservationRateBO.Instance.Insert(mRR);
                            //Gán ID vào model
                            mRR.ID = pRsvRateID;
                        }
                    }
                    #endregion

                    #endregion

                    #region Ghi dữ liệu vào ReservationOption

                    //Cập nhật RS cho rsv gốc
                    int ReservationOptionID = ReservationBO.GetReservationOptionID(ReservationID);
                    if (ReservationOptionID == 0)
                    {
                        ReservationOptionsModel mRO = new ReservationOptionsModel();
                        mRO.ReservationID = ReservationID;
                        mRO.Shares = true;
                        if (mOR.ProfileGroupId > 0)
                            mRO.GroupOptions = true;
                        ReservationOptionsBO.Instance.Insert(mRO);
                    }
                    else
                    {
                        ReservationOptionsModel mRO = (ReservationOptionsModel)ReservationOptionsBO.Instance.FindByPrimaryKey(ReservationOptionID);
                        mRO.ID = ReservationOptionID;
                        mRO.Shares = true;
                        if (mOR.ProfileGroupId > 0)
                            mRO.GroupOptions = true;
                        ReservationOptionsBO.Instance.Update(mRO);
                    }
                    //Trường hợp có Routing thì ghi hoặc sửa dữ liệu trong bảng ReservationOptions
                    bool _Routing = false;
                    ReservationBO.CheckRouting(mOR.ConfirmationNo, ref _Routing);
                    if (_Routing == true)
                    {
                        int pReservationOptionID = ReservationBO.GetReservationOptionID(pID);
                        if (pReservationOptionID == 0)
                        {
                            ReservationOptionsModel mRO = new ReservationOptionsModel();
                            mRO.ReservationID = pID;
                            //mRO.Shares = true;                           
                            mRO.Routing = true;
                            ReservationOptionsBO.Instance.Insert(mRO);
                        }
                        else
                        {
                            ReservationOptionsModel mRO = (ReservationOptionsModel)ReservationOptionsBO.Instance.FindByPrimaryKey(pReservationOptionID);
                            mRO.ID = pReservationOptionID;
                            mRO.Routing = true;
                            ReservationOptionsBO.Instance.Update(mRO);
                        }
                    }
                    #endregion

                    #region Update lại tên của khách ở cùng phòng vào từng đặt phòng (Trường ShareRoomName)
                    GetRoomShareName(mOR.ShareRoom);
                    #endregion

                    #region Update lại tên của khách ở cùng phòng vào từng đặt phòng (Trường AccompanyName)
                    ReservationBO.GetAccompanyName(ReservationID);
                    #endregion

                    #region Ghi dữ liệu vào bảng ReservationAmountByCurrency - Bỏ
                    ////Xóa dữ liệu trước khi Insert
                    //ReservationAmountByCurrencyBO.Instance.DeleteByAttribute("ReservationID", pID.ToString());
                    ////Tính lại số liệu rồi ghi dữ liệu 
                    //CreateReservationAmountByCurrency(pID, UserID);
                    #endregion

                    #region Ghi dữ liệu vào bảng ReservationGroup và ReservationGroupAmountByCurrency - Bỏ
                    ////Chú ý phải thực hiện ghi dữ liệu vào bảng ReservationAmountByCurrency trước                            
                    //ClassReservation.CreateReservationGroup(pID, mOR.ConfirmationNo, "");
                    #endregion

                    #region Interface
                    //if (ClassReservation.GetDateNoOfDay(mOR.ArrivalDate, _BusDate) <= 3)
                    IF_REN(mOR, ReservationID);
                    #endregion

                    //Xử lý bưa ăn của khách theo Package
                    ProcessMeal(null, mOR, false);

                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
        }

        public static bool HasOtherCheckedInRoomSharer(ReservationModel rsv)
        {
            // TODO:
            // map sang query kiểu:
            // ShareRoom = current.ShareRoom
            // AND Status = CheckedIn
            // AND ID <> current.ID
            Expression expC = new Expression("ShareRoom", rsv.ShareRoom, "=");
            expC = expC.And(new Expression("Status", 1, "="));
            expC = expC.And(new Expression("ID", rsv.ID, "<>"));
            ArrayList arrc = ReservationBO.Instance.FindByExpression(expC);
            if (arrc.Count > 0)
            {
                return false;
            }
            return true;
        }

        public static bool CheckTransactionByMaster(string _conf, ProcessTransactions? pt)
        {
            string _deposit = _GetDepositCode();
            DataTable _dt = TextUtils.Select("SELECT COUNT(a.ID) FROM dbo.FolioDetail a WITH (NOLOCK), dbo.Reservation b with (nolock) " +
                                "WHERE a.ReservationID = b.ID and  a.[Status] = 0 and b.ConfirmationNo = '" + _conf + "' and b.ReservationNo = '0' " +
                                "AND a.TransactionCode <> '" + _deposit + "'");
            //return
            int count = TextUtils.ToInt(_dt.Rows[0][0]?.ToString() ?? "0");
            if (count > 0)
                return true;
            else
                return false;
        }


        /// <summary>
        /// _type =1: Cancel CI
        /// _type =0: Cancel Rsv
        /// </summary>
        /// <param name="_conf"></param>
        /// <param name="_type"></param>
        /// <param name="pt"></param>
        /// <returns></returns>
        public static bool LastGuestReinstateCI(string _conf, int _type, ProcessTransactions? pt)
        {
            bool _para = false;
            // Hủy CI
            if (_type == 1)
            {
                DataTable _dt;

                if (pt != null)
                    _dt = pt.Select("SELECT count(ID) FROM Reservation WITH (NOLOCK) WHERE ConfirmationNo ='" + _conf + "' AND (Status = 1 OR Status = 6) AND ReservationNo > 0 ");
                else
                    _dt = TextUtils.Select("SELECT count(ID) FROM Reservation WITH (NOLOCK) WHERE ConfirmationNo ='" + _conf + "' AND (Status = 1 OR Status = 6) AND ReservationNo > 0 ");
                int count = TextUtils.ToInt(_dt.Rows[0][0]?.ToString() ?? "0");

                _para = count <= 1;
            }
            // Hủy đặt phòng
            else if (_type == 0)
            {
                DataTable _dt;
                if (pt != null)
                    _dt = pt.Select("SELECT count(ID) FROM Reservation WITH (NOLOCK) WHERE ConfirmationNo ='" + _conf + "' AND (Status = 0 OR Status = 5) AND ReservationNo > 0 ");
                else
                    _dt = TextUtils.Select("SELECT count(ID) FROM Reservation WITH (NOLOCK) WHERE ConfirmationNo ='" + _conf + "' AND (Status = 0 OR Status = 5) AND ReservationNo > 0 ");

                int count = TextUtils.ToInt(_dt.Rows[0][0]?.ToString() ?? "0");
                _para = count == 0;
            }
            return _para;
        }


        #endregion

        #endregion

    }
}