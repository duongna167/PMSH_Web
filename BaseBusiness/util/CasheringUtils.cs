using System.Collections;
using System.Data;
using System.Reflection;
using System.Reflection.Metadata;
using BaseBusiness.bc;
using BaseBusiness.BO;
using BaseBusiness.Model;
using DevExpress.CodeParser;
using Microsoft.Data.SqlClient;

namespace BaseBusiness.util
{
    public class CasheringUtils
    {
        #region Khai báo các biến liên quan

        public const string _CURRENCY_1 = "VND";
        public const string _CURRENCY_2 = "USD";
        public const string _CURRENCY_1_FOMART = "###,###,###.##";
        public const string _CURRENCY_2_FOMART = "###,###,##0.00";

        #endregion

        #region Các hàm liên quan đến việc cập nhập Balance

        /// <summary>
        /// 
        /// </summary>
        /// <param name="procedureName"></param>
        /// <param name="ParamName"></param>
        /// <param name="ParamValue"></param>
        public static void ExecuteSP(string _ProcedureName, string[] _ParamName, object[] _ParamValue)
        {
            SqlConnection Conn = new SqlConnection(DBUtils.GetDBConnectionString());
            try
            {
                SqlCommand Command = new SqlCommand(_ProcedureName, Conn);
                Command.CommandType = System.Data.CommandType.StoredProcedure;
                if (_ParamName != null)
                {
                    for (int i = 0; i < _ParamName.Length; i++)
                        Command.Parameters.Add(new SqlParameter(_ParamName[i], _ParamValue[i]));
                }
                Command.Connection.Open();
                Command.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                if (Conn.State == ConnectionState.Open)
                    Conn.Close();
            }
        }

        /// <summary>
        /// Hàm cập nhập thông qua 1 câu lệnh
        /// </summary>
        /// <param name="_Command"></param>
        public static void UpdateCommand(string _Command)
        {
            try
            {
                string[] PName = new string[1];
                object[] pValue = new object[1];
                PName[0] = "@SqlCommand";
                pValue[0] = _Command;
                ExecuteSP("spSearchAllForTrans", PName, pValue);
            }
            catch (SqlException ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Cap nhap so du hien thoi cua Folio
        /// </summary>
        /// <param name="FolioID">ID cua Folio</param>
        /// <param name="pt"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public static bool UpdateBalance(int _ReservationID, int _FolioID, ProcessTransactions _pt, ref string _Message)
        {
            try
            {
                string updateFolioSql =
                    "Update Folio set BalanceVND=dbo.getBalanceOfFolio(" + _FolioID + ",'" + _CURRENCY_1 + "')," +
                    "BalanceUSD=dbo.getBalanceOfFolio(" + _FolioID + ",'" + _CURRENCY_2 + "') Where ID=" + _FolioID;

                string updateReservationSql =
                    "Update Reservation set BalanceVND=dbo.getBalanceOfGih(" + _ReservationID + ",'" + _CURRENCY_1 + "')," +
                    "BalanceUSD=dbo.getBalanceOfGih(" + _ReservationID + ",'" + _CURRENCY_2 + "') Where ID=" + _ReservationID;

                try
                {
                    _pt.UpdateCommand(updateFolioSql);
                }
                catch (Exception ex)
                {
                    throw new Exception($"UpdateBalance[UpdateFolio] failed. Sql={updateFolioSql}. Error={ex.Message}", ex);
                }

                try
                {
                    _pt.UpdateCommand(updateReservationSql);
                }
                catch (Exception ex)
                {
                    throw new Exception($"UpdateBalance[UpdateReservation] failed. Sql={updateReservationSql}. Error={ex.Message}", ex);
                }
                return true;
            }
            catch (Exception ex)
            {
                _Message = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Cap nhap so du hien thoi cua Folio
        /// </summary>
        /// <param name="FolioID">ID cua Folio</param>
        /// <param name="pt"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public static bool UpdateBalance(int _ReservationID, int _FolioID, ref string _Message)
        {
            try
            {
                UpdateCommand("Update Folio set BalanceVND=dbo.getBalanceOfFolio(" + _FolioID + ",'" + _CURRENCY_1 + "')," +
                               "BalanceUSD=dbo.getBalanceOfFolio(" + _FolioID + ",'" + _CURRENCY_2 + "') Where ID=" + _FolioID);

                UpdateCommand("Update Reservation set BalanceVND=dbo.getBalanceOfGih(" + _ReservationID + ",'" + _CURRENCY_1 + "')," +
                                "BalanceUSD=dbo.getBalanceOfGih(" + _ReservationID + ",'" + _CURRENCY_2 + "') Where ID=" + _ReservationID);
                return true;
            }
            catch (Exception ex)
            {
                _Message = ex.Message;
                return false;
            }
        }

        #endregion

        #region Các hàm liên quan đến việc Posting

        /// <summary>
        /// Chức năng trợ giúp cho Generate
        /// </summary>
        /// <param name="InputAmount">Chuỗi nhập vào</param>
        /// <returns>Kết quả trả về</returns>
        protected static decimal GetNumber(string InputAmount)
        {
            return Convert.ToDecimal(InputAmount.Trim('B'));
        }

        /// <summary>
        /// Ham doi Decimal ve decimal da duoc Format
        /// </summary>
        /// <param name="Amount"></param>
        /// <returns></returns>
        public static decimal GetAmountFormat(decimal Amount)
        {
            Decimal Result = Convert.ToDecimal(Amount.ToString("###,###,###.00"));
            return Result;
        }

        /// <summary>
        /// Tính ra giá trước thuế
        /// </summary>
        /// <returns></returns>
        public static decimal GetAmount(ArrayList arr, decimal InputAmount)
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

        /// <summary>
        /// Lấy thông tin về số tiền.
        /// </summary>
        /// <param name="TransactionCode"></param>
        /// <param name="InputAmount"></param>
        /// <param name="TaxInclude"></param>
        /// <param name="Amount"></param>
        /// <param name="AmountNet"></param>
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
                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Lấy ra trạng thái TaxInclude của 1 Transaction
        /// </summary>
        /// <param name="_TransCode"></param>
        /// <returns></returns>
        protected static bool CheckTaxInclude(string _TransCode)
        {
            try
            {
                return Convert.ToBoolean(TextUtils.Select("Select TaxInclude from Transactions WITH (NOLOCK) where Code='" + _TransCode + "'").Rows[0][0]);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private static int ProfitCenterID = 2;
        private static string ProfitCenterCode = "0";

        /// <summary>
        /// Lỗi nhưng chưa dùng đến commit lại 
        /// </summary>
        // public static bool GenerateTrans(bool AutoPosting, DateTime _SysDate, DateTime _BusinessDate, int _ProID, string _ProCode, string _ConfirmNo, int _RsvID,
        //                                 int _ProfileID, string _AccountName, int _Win, string _TransCode, string _ArCode, string _Ref, string _Supp,
        //                                 decimal _Amount, int _Quan, string _CurrencyID, string _CurrencyLocal,
        //                                 ref decimal _AmountReturn, ref decimal _AmountLocalReturn, ref string _TransNoReturn, ref string _Message)
        // {
        //     ProcessTransactions pt = new ProcessTransactions();
        //     try
        //     {
        //         #region Mở kết nối và bắt đầu 1 Transaction
        //         pt.OpenConnection();
        //         pt.BeginTransaction();
        //         #endregion

        //         #region Lấy ra thông tin của FolioID

        //         int _RsvID_Return = 0;
        //         int FolioID = GetOrCreateFolioID(_SysDate, _BusinessDate, _ConfirmNo, _RsvID, _Win, _ProfileID, _AccountName, ref _RsvID_Return, pt, ref _Message);
        //         _RsvID = _RsvID_Return;

        //         #endregion

        //         if (FolioID > 0)
        //         {
        //             #region Khai báo Model

        //             FolioDetailModel mFD_Detail = new FolioDetailModel();
        //             FolioDetailModel mFD_Master = new FolioDetailModel();

        //             #endregion

        //             #region Lấy ra thông tin của TransCode
        //             TransactionsModel mT = (TransactionsModel)pt.FindByAttribute("Transactions", "Code", _TransCode)[0];
        //             #endregion

        //             #region Gán giá trị cho các biến Static

        //             if (_ProID == 0)
        //             {
        //                 mFD_Detail.ProfitCenterID = _ProID;
        //                 mFD_Detail.ProfitCenterCode = _ProCode;
        //             }
        //             else
        //             {
        //                 mFD_Detail.ProfitCenterID = ProfitCenterID;
        //                 mFD_Detail.ProfitCenterCode = ProfitCenterCode;
        //             }

        //             mFD_Detail.Status = false;

        //             mFD_Detail.CurrencyID = _CurrencyID;
        //             mFD_Detail.CurrencyMaster = _CurrencyLocal;

        //             mFD_Detail.ReservationID = _RsvID;
        //             mFD_Detail.OriginReservationID = _RsvID;

        //             mFD_Detail.FolioID = FolioID;
        //             mFD_Detail.OriginFolioID = mFD_Detail.FolioID;

        //             mFD_Detail.Quantity = _Quan;
        //             mFD_Detail.TransactionDate = _BusinessDate;
        //             mFD_Detail.PackageID = 0;

        //             mFD_Detail.UserInsertID = Global.UserID;
        //             mFD_Detail.UserUpdateID = Global.UserID;
        //             mFD_Detail.CreateDate = _SysDate;
        //             mFD_Detail.UpdateDate = _SysDate;
        //             if (AutoPosting == true)
        //             {
        //                 mFD_Detail.UserID = 0;
        //                 mFD_Detail.UserName = "$$";
        //                 mFD_Detail.CashierNo = "";
        //                 mFD_Detail.ShiftID = 0;
        //             }
        //             else
        //             {
        //                 mFD_Detail.UserID = Global.UserID;
        //                 mFD_Detail.UserName = Global.UserName;
        //                 mFD_Detail.CashierNo = Global.UserName;
        //                 mFD_Detail.ShiftID = Global.ShiftID;
        //             }

        //             if (_ProID == 0)
        //             {
        //                 mFD_Master.ProfitCenterID = _ProID;
        //                 mFD_Master.ProfitCenterCode = _ProCode;
        //             }
        //             else
        //             {
        //                 mFD_Master.ProfitCenterID = ProfitCenterID;
        //                 mFD_Master.ProfitCenterCode = ProfitCenterCode;
        //             }

        //             mFD_Master.Status = false;

        //             mFD_Master.CurrencyID = _CurrencyID;
        //             mFD_Master.CurrencyMaster = _CurrencyLocal;

        //             mFD_Master.ReservationID = _RsvID;
        //             mFD_Master.OriginReservationID = _RsvID;

        //             mFD_Master.FolioID = FolioID;
        //             mFD_Master.OriginFolioID = mFD_Master.FolioID;

        //             mFD_Master.Quantity = _Quan;
        //             mFD_Master.TransactionDate = _BusinessDate;
        //             mFD_Master.PackageID = 0;

        //             mFD_Master.UserInsertID = Global.UserID;
        //             mFD_Master.UserUpdateID = Global.UserID;
        //             mFD_Master.CreateDate = _SysDate;
        //             mFD_Master.UpdateDate = _SysDate;
        //             if (AutoPosting == true)
        //             {
        //                 mFD_Master.UserID = 0;
        //                 mFD_Master.UserName = "$$";
        //                 mFD_Master.CashierNo = "";
        //                 mFD_Master.ShiftID = 0;
        //             }
        //             else
        //             {
        //                 mFD_Master.UserID = Global.UserID;
        //                 mFD_Master.UserName = Global.UserName;
        //                 mFD_Master.CashierNo = Global.UserName;
        //                 mFD_Master.ShiftID = Global.ShiftID;
        //             }
        //             #endregion

        //             #region Kiểm tra xem Transaction này có ở trong Generate ?
        //             List<BaseModel> arr = pt.FindByAttribute("GenerateTransaction", "TransactionCode", _TransCode);
        //             #endregion

        //             #region Nếu chưa tồn tại trong Generate.
        //             if ((arr == null) || (arr.Count == 0))
        //             {
        //                 //Gán thông tin cho các propertie còn lại
        //                 mFD_Detail.IsSplit = false;
        //                 mFD_Detail.Reference = _Ref;
        //                 mFD_Detail.Supplement = _Supp;

        //                 mFD_Detail.TransactionGroupID = mT.TransactionGroupID;
        //                 mFD_Detail.TransactionSubgroupID = mT.TransactionSubGroupID;
        //                 mFD_Detail.GroupCode = mT.GroupCode;
        //                 mFD_Detail.SubgroupCode = mT.SubgroupCode;
        //                 mFD_Detail.GroupType = mT.GroupType;

        //                 mFD_Detail.ArticleCode = _ArCode;
        //                 mFD_Detail.TransactionCode = mT.Code;
        //                 mFD_Detail.Description = mT.Description;//mT.Description;
        //                 mFD_Detail.Amount = _Amount;
        //                 mFD_Detail.AmountBeforeTax = _Amount;
        //                 mFD_Detail.Price = mFD_Detail.Amount / mFD_Detail.Quantity;

        //                 mFD_Detail.AmountMaster = TextUtils.ExchangeCurrency(_BusinessDate, _CurrencyID, _CurrencyLocal, _Amount);
        //                 mFD_Detail.AmountMasterBeforeTax = mFD_Detail.AmountMaster;

        //                 mFD_Detail.AmountGross = mFD_Detail.Amount;
        //                 mFD_Detail.AmountMasterGross = mFD_Detail.AmountMaster;

        //                 mFD_Detail.PostType = 1;
        //                 mFD_Detail.RowState = 1;

        //                 //Thực hiện Post
        //                 mFD_Detail.ID = (int)pt.Insert(mFD_Detail);

        //                 mFD_Detail.InvoiceNo = mFD_Detail.ID.ToString();
        //                 mFD_Detail.TransactionNo = mFD_Detail.ID.ToString();

        //                 pt.Update(mFD_Detail);
        //                 //Update số dư.
        //                 UpdateBalance(_RsvID, FolioID, pt, ref _Message);

        //                 // Ghi histoty
        //                 if (mFD_Detail.GroupType != 1)
        //                     ActionPosting.InsertHistory(pt, _SysDate, _BusinessDate, mFD_Detail.FolioID, mFD_Detail.FolioID, mFD_Detail.InvoiceNo, ActionPosting.HistoryType.Basic_Post,
        //                         ActionPosting.GetActionText(ActionPosting.HistoryType.Basic_Post, mFD_Detail.TransactionCode, mFD_Detail.Description),
        //                         Global.UserName, mFD_Detail.TransactionCode, mFD_Detail.Description, mFD_Detail.Amount, mFD_Detail.Supplement, "", "", "");
        //                 else
        //                     ActionPosting.InsertHistory(pt, _SysDate, _BusinessDate, mFD_Detail.FolioID, mFD_Detail.FolioID, mFD_Detail.InvoiceNo, ActionPosting.HistoryType.Payment,
        //                         ActionPosting.GetActionText(ActionPosting.HistoryType.Payment, mFD_Detail.TransactionCode, mFD_Detail.Description),
        //                         Global.UserName, mFD_Detail.TransactionCode, mFD_Detail.Description, mFD_Detail.Amount, mFD_Detail.Supplement, "", "", "");

        //                 //Trả về thông tin
        //                 _AmountReturn = mFD_Detail.Amount;
        //                 _AmountLocalReturn = mFD_Detail.AmountMaster;
        //                 _TransNoReturn = mFD_Detail.TransactionNo;
        //             }
        //             #endregion

        //             #region Nếu đã tồn tại trong Generate -> lấy ra và thực hiện
        //             else
        //             {
        //                 #region Khai báo biến
        //                 decimal s1 = 0, s2 = 0, s3 = 0;
        //                 decimal CurrentAmount = 0;
        //                 decimal BaseAmount = _Amount;
        //                 decimal Rate = 0;
        //                 GenerateTransactionModel mGT;
        //                 #endregion

        //                 #region Lấy ra thông tin của amount trước thuế
        //                 if (mT.TaxInclude == true)
        //                     BaseAmount = GetAmount(arr, Convert.ToDecimal(BaseAmount));
        //                 #endregion

        //                 #region Insert dòng tổng

        //                 mFD_Master.IsSplit = true;
        //                 mFD_Master.Reference = _Ref;
        //                 mFD_Master.Supplement = _Supp;

        //                 mFD_Master.TransactionGroupID = mT.TransactionGroupID;
        //                 mFD_Master.TransactionSubgroupID = mT.TransactionSubGroupID;
        //                 mFD_Master.GroupCode = mT.GroupCode;
        //                 mFD_Master.SubgroupCode = mT.SubgroupCode;
        //                 mFD_Master.GroupType = mT.GroupType;

        //                 mFD_Master.ArticleCode = _ArCode;
        //                 mFD_Master.TransactionCode = mT.Code;
        //                 mFD_Master.Description = mT.Description;//mT.Description;

        //                 mFD_Master.Quantity = _Quan;

        //                 mFD_Master.Price = 0;
        //                 mFD_Master.Amount = 0;
        //                 mFD_Master.AmountMaster = 0;
        //                 mFD_Master.AmountBeforeTax = 0;
        //                 mFD_Master.AmountMasterBeforeTax = 0;

        //                 mFD_Master.PostType = 2;
        //                 mFD_Master.RowState = 1;

        //                 mFD_Master.ID = (int)pt.Insert(mFD_Master);

        //                 mFD_Master.InvoiceNo = mFD_Master.ID.ToString();
        //                 mFD_Master.TransactionNo = mFD_Master.ID.ToString();
        //                 #endregion

        //                 for (int j = 0; j < arr.Count; j++)
        //                 {
        //                     #region Đổ dữ liệu vào Model
        //                     mGT = (GenerateTransactionModel)arr[j];
        //                     #endregion

        //                     #region Lấy ra CurrentAmount
        //                     if (mGT.Type == 0)
        //                     {
        //                         if (mGT.BaseAmount == 0)
        //                             CurrentAmount = (mGT.Percentage * BaseAmount) / 100;
        //                         else if (mGT.BaseAmount == 1)
        //                             CurrentAmount = (mGT.Percentage * s1) / 100;
        //                         else if (mGT.BaseAmount == 2)
        //                             CurrentAmount = (mGT.Percentage * s2) / 100;
        //                         else
        //                             CurrentAmount = (mGT.Percentage * s3) / 100;
        //                     }
        //                     else if (mGT.Type == 1)
        //                     {
        //                         CurrentAmount = mGT.Amount;
        //                     }
        //                     CurrentAmount = GetAmountFormat(CurrentAmount);
        //                     #endregion

        //                     #region Lấy dữ liệu vào s1,s2,s3
        //                     if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == false) && (mGT.Subtotal3 == false))
        //                     {
        //                         s1 = s1 + CurrentAmount;
        //                     }
        //                     else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == true) && (mGT.Subtotal3 == false))
        //                     {
        //                         s1 = s1 + CurrentAmount;
        //                         s2 = CurrentAmount;
        //                     }
        //                     else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == false) && (mGT.Subtotal3 == true))
        //                     {
        //                         s1 = s1 + CurrentAmount;
        //                         s3 = CurrentAmount;
        //                     }
        //                     else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == true) && (mGT.Subtotal3 == true))
        //                     {
        //                         s1 = s1 + CurrentAmount;
        //                         s2 = CurrentAmount;
        //                         s3 = CurrentAmount;
        //                     }
        //                     #endregion

        //                     #region Đổ dữ liệu vào model Model

        //                     mFD_Detail.IsSplit = false;
        //                     mFD_Detail.PostType = 2;
        //                     mFD_Detail.RowState = 2;

        //                     mFD_Detail.TransactionGroupID = mGT.TransactionGroupID;
        //                     mFD_Detail.TransactionSubgroupID = mGT.TransactionSubGroupID;
        //                     mFD_Detail.GroupCode = mGT.GroupCode;
        //                     mFD_Detail.SubgroupCode = mGT.SubgroupCode;
        //                     mFD_Detail.GroupType = mGT.GroupType;

        //                     mFD_Detail.TransactionCode = mGT.TransactionCodeDetail;
        //                     mFD_Detail.Description = mGT.Description;

        //                     if ((mT.TaxInclude == true) && (j == arr.Count - 1))
        //                         mFD_Detail.Amount = _Amount - mFD_Master.Amount;
        //                     else
        //                         mFD_Detail.Amount = GetAmountFormat(CurrentAmount);
        //                     mFD_Detail.AmountBeforeTax = mFD_Detail.Amount;
        //                     mFD_Detail.Price = mFD_Detail.Amount / mFD_Detail.Quantity;
        //                     mFD_Detail.AmountGross = mFD_Detail.Amount;
        //                     if (j == 0)
        //                     {
        //                         // Tính ra tỉ giá nếu là dòng dầu
        //                         mFD_Detail.AmountMaster = TextUtils.ExchangeCurrency(_BusinessDate, _CurrencyID, _CurrencyLocal, mFD_Detail.Amount);
        //                         Rate = mFD_Detail.AmountMaster / mFD_Detail.Amount;
        //                         // Nếu là dòng đầu -> insert giá trước thuế.
        //                         mFD_Master.AmountBeforeTax = mFD_Detail.Amount;
        //                         mFD_Master.AmountMasterBeforeTax = mFD_Detail.AmountMaster;
        //                     }
        //                     else
        //                         mFD_Detail.AmountMaster = mFD_Detail.Amount * Rate;

        //                     mFD_Detail.AmountMasterBeforeTax = mFD_Detail.AmountMaster;
        //                     mFD_Detail.AmountMasterGross = mFD_Detail.AmountMaster;

        //                     #endregion

        //                     #region Insert Du lieu

        //                     mFD_Detail.InvoiceNo = mFD_Master.InvoiceNo;
        //                     mFD_Detail.TransactionNo = mFD_Master.TransactionNo;
        //                     mFD_Detail.ID = (int)pt.Insert(mFD_Detail);

        //                     mFD_Master.AmountMaster = mFD_Master.AmountMaster + mFD_Detail.AmountMaster;
        //                     mFD_Master.Amount = mFD_Master.Amount + mFD_Detail.Amount;

        //                     #endregion
        //                 }
        //                 // Tính giá Gross
        //                 mFD_Master.AmountGross = mFD_Master.Amount;
        //                 mFD_Master.AmountMasterGross = mFD_Master.AmountMaster;
        //                 // Tính giá Net nếu số tiền nhập vào là giá sau thuế
        //                 if (mT.TaxInclude == true)
        //                 {
        //                     mFD_Master.Amount = _Amount;
        //                     mFD_Master.AmountMaster = _Amount * Rate;
        //                 }
        //                 mFD_Master.Price = mFD_Master.Amount / mFD_Master.Quantity;

        //                 pt.Update(mFD_Master);
        //                 //Update số dư.
        //                 UpdateBalance(_RsvID, FolioID, pt, ref _Message);

        //                 // Ghi histoty
        //                 if (mFD_Master.GroupType != 1)
        //                     ActionPosting.InsertHistory(pt, _SysDate, _BusinessDate, mFD_Master.FolioID, mFD_Master.FolioID, mFD_Master.InvoiceNo, ActionPosting.HistoryType.Gen_Post,
        //                         ActionPosting.GetActionText(ActionPosting.HistoryType.Gen_Post, mFD_Master.TransactionCode, mFD_Master.Description),
        //                         Global.UserName, mFD_Master.TransactionCode, mFD_Master.Description, mFD_Master.Amount, mFD_Master.Supplement, "", "", "");
        //                 else
        //                     ActionPosting.InsertHistory(pt, _SysDate, _BusinessDate, mFD_Master.FolioID, mFD_Master.FolioID, mFD_Master.InvoiceNo, ActionPosting.HistoryType.Payment,
        //                         ActionPosting.GetActionText(ActionPosting.HistoryType.Payment, mFD_Master.TransactionCode, mFD_Master.Description),
        //                         Global.UserName, mFD_Master.TransactionCode, mFD_Master.Description, mFD_Master.Amount, mFD_Master.Supplement, "", "", "");
        //                 //Trả về thông tin
        //                 _AmountReturn = mFD_Master.Amount;
        //                 _AmountLocalReturn = mFD_Master.AmountMaster;
        //                 _TransNoReturn = mFD_Master.TransactionNo;
        //             }
        //             #endregion

        //             #region Commit-Return
        //             pt.CommitTransaction();
        //             pt.CloseConnection();
        //             return true;
        //             #endregion
        //         }
        //         else if (FolioID == -1)
        //         {
        //             _Message = "Folio is locked.";
        //             return false;
        //         }
        //         else
        //         {
        //             return false;
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         pt.CloseConnection();
        //         _Message = ex.Message;
        //         return false;
        //     }
        // }

        // public static bool GenerateTrans(bool AutoPosting, DateTime _SysDate, DateTime _BusinessDate, int _ProID, string _ProCode, string _ConfirmNo, int _RsvID,
        //                                 int _ProfileID, string _AccountName, int _Win, string _TransCode, string _ArCode, string _Ref, string _Supp,
        //                                 decimal _Amount, int _Quan, string _CurrencyID, string _CurrencyLocal, bool _IsAR, string _ARNo,
        //                                 ref decimal _AmountReturn, ref decimal _AmountLocalReturn, ref string _TransNoReturn, ref string _Message, ReservationModel mResv)
        // {
        //     if (_Amount == 0)
        //     {
        //         _Message = "Please input amount!";
        //         return false;
        //     }

        //     ProcessTransactions pt = new ProcessTransactions();
        //     try
        //     {
        //         bool _IsOK = false;
        //         //dat --> Su dung cho booking
        //         if (_ProID < 20)
        //             _ProID = _RsvID;

        //         #region Mở kết nối và bắt đầu 1 Transaction
        //         pt.OpenConnection();
        //         pt.BeginTransaction();
        //         #endregion

        //         #region Lấy ra thông tin của FolioID

        //         int _RsvID_Return = 0;
        //         int FolioID = GetOrCreateFolioID(_SysDate, _BusinessDate, _ConfirmNo, _RsvID, _Win, _ProfileID, _AccountName, ref _RsvID_Return, pt, ref _Message);
        //         _RsvID = _RsvID_Return;

        //         #endregion

        //         if (FolioID > 0)
        //         {
        //             #region Khai báo Model

        //             FolioDetailModel mFD_Detail = new FolioDetailModel();
        //             FolioDetailModel mFD_Master = new FolioDetailModel();

        //             #endregion

        //             #region Lấy ra thông tin của TransCode
        //             TransactionsModel mT = (TransactionsModel)pt.FindByAttribute("Transactions", "Code", _TransCode)[0];
        //             #endregion

        //             #region Gán giá trị cho các biến Static

        //             //if (_ProID == 0)
        //             //{
        //             //    mFD_Detail.ProfitCenterID = _ProID;
        //             //    mFD_Detail.ProfitCenterCode = _ProCode;
        //             //}
        //             //else
        //             //{
        //             mFD_Detail.ProfitCenterID = ProfitCenterID;
        //             mFD_Detail.ProfitCenterCode = ProfitCenterCode;
        //             //}

        //             mFD_Detail.Status = false;

        //             mFD_Detail.CurrencyID = _CurrencyID;
        //             mFD_Detail.CurrencyMaster = _CurrencyLocal;

        //             mFD_Detail.ReservationID = _RsvID;
        //             mFD_Detail.OriginReservationID = _ProID;
        //             if (mFD_Detail.OriginReservationID <= 0)
        //                 mFD_Detail.OriginReservationID = _RsvID;

        //             mFD_Detail.FolioID = FolioID;
        //             mFD_Detail.OriginFolioID = mFD_Detail.FolioID;

        //             mFD_Detail.Quantity = _Quan;
        //             mFD_Detail.TransactionDate = _BusinessDate;
        //             mFD_Detail.PackageID = 0;

        //             mFD_Detail.UserInsertID = Global.UserID;
        //             mFD_Detail.UserUpdateID = Global.UserID;
        //             mFD_Detail.CreateDate = _SysDate;
        //             mFD_Detail.UpdateDate = _SysDate;
        //             if (AutoPosting == true)
        //             {
        //                 mFD_Detail.UserID = 0;
        //                 mFD_Detail.UserName = "$$";
        //                 mFD_Detail.CashierNo = "";
        //                 mFD_Detail.ShiftID = 0;
        //             }
        //             else
        //             {
        //                 mFD_Detail.UserID = Global.UserID;
        //                 mFD_Detail.UserName = Global.UserName;
        //                 mFD_Detail.CashierNo = Global.UserName;
        //                 mFD_Detail.ShiftID = Global.ShiftID;
        //             }

        //             if (_ProID == 0)
        //             {
        //                 mFD_Master.ProfitCenterID = _ProID;
        //                 mFD_Master.ProfitCenterCode = _ProCode;
        //             }
        //             else
        //             {
        //                 mFD_Master.ProfitCenterID = ProfitCenterID;
        //                 mFD_Master.ProfitCenterCode = ProfitCenterCode;
        //             }

        //             mFD_Master.Status = false;

        //             mFD_Master.CurrencyID = _CurrencyID;
        //             mFD_Master.CurrencyMaster = _CurrencyLocal;

        //             mFD_Master.ReservationID = _RsvID;
        //             //Payment vao master bat chon booking
        //             mFD_Master.OriginReservationID = _ProID;
        //             if (mFD_Master.OriginReservationID <= 0)
        //                 mFD_Master.OriginReservationID = _RsvID;

        //             mFD_Master.FolioID = FolioID;
        //             mFD_Master.OriginFolioID = mFD_Master.FolioID;

        //             mFD_Master.Quantity = _Quan;
        //             mFD_Master.TransactionDate = _BusinessDate;
        //             mFD_Master.PackageID = 0;

        //             mFD_Master.UserInsertID = Global.UserID;
        //             mFD_Master.UserUpdateID = Global.UserID;
        //             mFD_Master.CreateDate = _SysDate;
        //             mFD_Master.UpdateDate = _SysDate;
        //             if (AutoPosting == true)
        //             {
        //                 mFD_Master.UserID = 0;
        //                 mFD_Master.UserName = "$$";
        //                 mFD_Master.CashierNo = "";
        //                 mFD_Master.ShiftID = 0;
        //             }
        //             else
        //             {
        //                 mFD_Master.UserID = Global.UserID;
        //                 mFD_Master.UserName = Global.UserName;
        //                 mFD_Master.CashierNo = Global.UserName;
        //                 mFD_Master.ShiftID = Global.ShiftID;
        //             }
        //             #endregion

        //             #region Kiểm tra xem Transaction này có ở trong Generate ?
        //             List<BaseModel> arr = pt.FindByAttribute("GenerateTransaction", "TransactionCode", _TransCode);
        //             #endregion

        //             #region Nếu chưa tồn tại trong Generate.
        //             if ((arr == null) || (arr.Count == 0))
        //             {
        //                 //Gán thông tin cho các propertie còn lại
        //                 mFD_Detail.IsSplit = false;
        //                 mFD_Detail.Reference = _Ref;
        //                 mFD_Detail.Supplement = _Supp;

        //                 mFD_Detail.TransactionGroupID = mT.TransactionGroupID;
        //                 mFD_Detail.TransactionSubgroupID = mT.TransactionSubGroupID;
        //                 mFD_Detail.GroupCode = mT.GroupCode;
        //                 mFD_Detail.SubgroupCode = mT.SubgroupCode;
        //                 mFD_Detail.GroupType = mT.GroupType;

        //                 mFD_Detail.ArticleCode = _ArCode;
        //                 mFD_Detail.TransactionCode = mT.Code;
        //                 mFD_Detail.Description = mT.Description;//mT.Description;
        //                 mFD_Detail.Amount = _Amount;
        //                 mFD_Detail.AmountBeforeTax = _Amount;
        //                 mFD_Detail.Price = mFD_Detail.Amount / mFD_Detail.Quantity;

        //                 mFD_Detail.AmountMaster = TextUtils.ExchangeCurrency(_BusinessDate, _CurrencyID, _CurrencyLocal, _Amount);
        //                 mFD_Detail.AmountMasterBeforeTax = mFD_Detail.AmountMaster;

        //                 mFD_Detail.AmountGross = mFD_Detail.Amount;
        //                 mFD_Detail.AmountMasterGross = mFD_Detail.AmountMaster;

        //                 mFD_Detail.PostType = 1;
        //                 mFD_Detail.RowState = 1;

        //                 //Thực hiện Post
        //                 mFD_Detail.ID = (int)pt.Insert(mFD_Detail);

        //                 mFD_Detail.InvoiceNo = mFD_Detail.ID.ToString();
        //                 mFD_Detail.TransactionNo = mFD_Detail.ID.ToString();

        //                 pt.Update(mFD_Detail);

        //                 ////Update số dư.
        //                 //UpdateBalance(_RsvID, FolioID, pt, ref _Message);
        //                 _IsOK = true;

        //                 // Ghi histoty
        //                 if (mFD_Detail.GroupType != 1)
        //                     ActionPosting.InsertHistory(pt, _SysDate, _BusinessDate, mFD_Detail.FolioID, mFD_Detail.FolioID, mFD_Detail.InvoiceNo, ActionPosting.HistoryType.Basic_Post,
        //                         ActionPosting.GetActionText(ActionPosting.HistoryType.Basic_Post, mFD_Detail.TransactionCode, mFD_Detail.Description),
        //                         Global.UserName, mFD_Detail.TransactionCode, mFD_Detail.Description, mFD_Detail.Amount, mFD_Detail.Supplement, "", "", "");
        //                 else
        //                     ActionPosting.InsertHistory(pt, _SysDate, _BusinessDate, mFD_Detail.FolioID, mFD_Detail.FolioID, mFD_Detail.InvoiceNo, ActionPosting.HistoryType.Payment,
        //                         ActionPosting.GetActionText(ActionPosting.HistoryType.Payment, mFD_Detail.TransactionCode, mFD_Detail.Description),
        //                         Global.UserName, mFD_Detail.TransactionCode, mFD_Detail.Description, mFD_Detail.Amount, mFD_Detail.Supplement, "", "", "");

        //                 //Trả về thông tin
        //                 _AmountReturn = mFD_Detail.Amount;
        //                 _AmountLocalReturn = mFD_Detail.AmountMaster;
        //                 _TransNoReturn = mFD_Detail.TransactionNo;
        //             }
        //             #endregion

        //             #region Nếu đã tồn tại trong Generate -> lấy ra và thực hiện
        //             else
        //             {
        //                 #region Khai báo biến
        //                 decimal s1 = 0, s2 = 0, s3 = 0;
        //                 decimal CurrentAmount = 0;
        //                 decimal BaseAmount = _Amount;
        //                 decimal Rate = 0;
        //                 GenerateTransactionModel mGT;
        //                 #endregion

        //                 #region Lấy ra thông tin của amount trước thuế
        //                 if (mT.TaxInclude == true)
        //                     BaseAmount = GetAmount(arr, Convert.ToDecimal(BaseAmount));
        //                 #endregion

        //                 #region Insert dòng tổng

        //                 mFD_Master.IsSplit = true;
        //                 mFD_Master.Reference = _Ref;
        //                 mFD_Master.Supplement = _Supp;

        //                 mFD_Master.TransactionGroupID = mT.TransactionGroupID;
        //                 mFD_Master.TransactionSubgroupID = mT.TransactionSubGroupID;
        //                 mFD_Master.GroupCode = mT.GroupCode;
        //                 mFD_Master.SubgroupCode = mT.SubgroupCode;
        //                 mFD_Master.GroupType = mT.GroupType;

        //                 mFD_Master.ArticleCode = _ArCode;
        //                 mFD_Master.TransactionCode = mT.Code;
        //                 mFD_Master.Description = mT.Description;//mT.Description;

        //                 mFD_Master.Quantity = _Quan;

        //                 mFD_Master.Price = 0;
        //                 mFD_Master.Amount = 0;
        //                 mFD_Master.AmountMaster = 0;
        //                 mFD_Master.AmountBeforeTax = 0;
        //                 mFD_Master.AmountMasterBeforeTax = 0;

        //                 mFD_Master.PostType = 2;
        //                 mFD_Master.RowState = 1;

        //                 mFD_Master.ID = (int)pt.Insert(mFD_Master);

        //                 mFD_Master.InvoiceNo = mFD_Master.ID.ToString();
        //                 mFD_Master.TransactionNo = mFD_Master.ID.ToString();
        //                 #endregion

        //                 for (int j = 0; j < arr.Count; j++)
        //                 {
        //                     #region Đổ dữ liệu vào Model
        //                     mGT = (GenerateTransactionModel)arr[j];
        //                     #endregion

        //                     #region Lấy ra CurrentAmount
        //                     if (mGT.Type == 0)
        //                     {
        //                         if (mGT.BaseAmount == 0)
        //                             CurrentAmount = (mGT.Percentage * BaseAmount) / 100;
        //                         else if (mGT.BaseAmount == 1)
        //                             CurrentAmount = (mGT.Percentage * s1) / 100;
        //                         else if (mGT.BaseAmount == 2)
        //                             CurrentAmount = (mGT.Percentage * s2) / 100;
        //                         else
        //                             CurrentAmount = (mGT.Percentage * s3) / 100;
        //                     }
        //                     else if (mGT.Type == 1)
        //                     {
        //                         CurrentAmount = mGT.Amount;
        //                     }
        //                     CurrentAmount = GetAmountFormat(CurrentAmount);
        //                     #endregion

        //                     #region Lấy dữ liệu vào s1,s2,s3
        //                     if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == false) && (mGT.Subtotal3 == false))
        //                     {
        //                         s1 = s1 + CurrentAmount;
        //                     }
        //                     else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == true) && (mGT.Subtotal3 == false))
        //                     {
        //                         s1 = s1 + CurrentAmount;
        //                         s2 = CurrentAmount;
        //                     }
        //                     else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == false) && (mGT.Subtotal3 == true))
        //                     {
        //                         s1 = s1 + CurrentAmount;
        //                         s3 = CurrentAmount;
        //                     }
        //                     else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == true) && (mGT.Subtotal3 == true))
        //                     {
        //                         s1 = s1 + CurrentAmount;
        //                         s2 = CurrentAmount;
        //                         s3 = CurrentAmount;
        //                     }
        //                     #endregion

        //                     #region Đổ dữ liệu vào model Model

        //                     mFD_Detail.IsSplit = false;
        //                     mFD_Detail.PostType = 2;
        //                     mFD_Detail.RowState = 2;

        //                     mFD_Detail.TransactionGroupID = mGT.TransactionGroupID;
        //                     mFD_Detail.TransactionSubgroupID = mGT.TransactionSubGroupID;
        //                     mFD_Detail.GroupCode = mGT.GroupCode;
        //                     mFD_Detail.SubgroupCode = mGT.SubgroupCode;
        //                     mFD_Detail.GroupType = mGT.GroupType;

        //                     mFD_Detail.TransactionCode = mGT.TransactionCodeDetail;
        //                     mFD_Detail.Description = mGT.Description;

        //                     if ((mT.TaxInclude == true) && (j == arr.Count - 1))
        //                         mFD_Detail.Amount = _Amount - mFD_Master.Amount;
        //                     else
        //                         mFD_Detail.Amount = GetAmountFormat(CurrentAmount);
        //                     mFD_Detail.AmountBeforeTax = mFD_Detail.Amount;
        //                     mFD_Detail.Price = mFD_Detail.Amount / mFD_Detail.Quantity;
        //                     mFD_Detail.AmountGross = mFD_Detail.Amount;
        //                     if (j == 0)
        //                     {
        //                         // Tính ra tỉ giá nếu là dòng dầu
        //                         mFD_Detail.AmountMaster = TextUtils.ExchangeCurrency(_BusinessDate, _CurrencyID, _CurrencyLocal, mFD_Detail.Amount);
        //                         Rate = mFD_Detail.AmountMaster / mFD_Detail.Amount;
        //                         // Nếu là dòng đầu -> insert giá trước thuế.
        //                         mFD_Master.AmountBeforeTax = mFD_Detail.Amount;
        //                         mFD_Master.AmountMasterBeforeTax = mFD_Detail.AmountMaster;
        //                     }
        //                     else
        //                         mFD_Detail.AmountMaster = mFD_Detail.Amount * Rate;

        //                     mFD_Detail.AmountMasterBeforeTax = mFD_Detail.AmountMaster;
        //                     mFD_Detail.AmountMasterGross = mFD_Detail.AmountMaster;

        //                     #endregion

        //                     #region Insert Du lieu

        //                     mFD_Detail.InvoiceNo = mFD_Master.InvoiceNo;
        //                     mFD_Detail.TransactionNo = mFD_Master.TransactionNo;
        //                     mFD_Detail.ID = (int)pt.Insert(mFD_Detail);

        //                     mFD_Master.AmountMaster = mFD_Master.AmountMaster + mFD_Detail.AmountMaster;
        //                     mFD_Master.Amount = mFD_Master.Amount + mFD_Detail.Amount;

        //                     #endregion
        //                 }
        //                 // Tính giá Gross
        //                 mFD_Master.AmountGross = mFD_Master.Amount;
        //                 mFD_Master.AmountMasterGross = mFD_Master.AmountMaster;
        //                 // Tính giá Net nếu số tiền nhập vào là giá sau thuế
        //                 if (mT.TaxInclude == true)
        //                 {
        //                     mFD_Master.Amount = _Amount;
        //                     mFD_Master.AmountMaster = _Amount * Rate;
        //                 }
        //                 mFD_Master.Price = mFD_Master.Amount / mFD_Master.Quantity;

        //                 pt.Update(mFD_Master);

        //                 ////Update số dư.
        //                 //UpdateBalance(_RsvID, FolioID, pt, ref _Message);
        //                 _IsOK = true;

        //                 // Ghi histoty
        //                 if (mFD_Master.GroupType != 1)
        //                     ActionPosting.InsertHistory(pt, _SysDate, _BusinessDate, mFD_Master.FolioID, mFD_Master.FolioID, mFD_Master.InvoiceNo, ActionPosting.HistoryType.Gen_Post,
        //                         ActionPosting.GetActionText(ActionPosting.HistoryType.Gen_Post, mFD_Master.TransactionCode, mFD_Master.Description),
        //                         Global.UserName, mFD_Master.TransactionCode, mFD_Master.Description, mFD_Master.Amount, mFD_Master.Supplement, "", "", "");
        //                 else
        //                     ActionPosting.InsertHistory(pt, _SysDate, _BusinessDate, mFD_Master.FolioID, mFD_Master.FolioID, mFD_Master.InvoiceNo, ActionPosting.HistoryType.Payment,
        //                         ActionPosting.GetActionText(ActionPosting.HistoryType.Payment, mFD_Master.TransactionCode, mFD_Master.Description),
        //                         Global.UserName, mFD_Master.TransactionCode, mFD_Master.Description, mFD_Master.Amount, mFD_Master.Supplement, "", "", "");
        //                 //Trả về thông tin
        //                 _AmountReturn = mFD_Master.Amount;
        //                 _AmountLocalReturn = mFD_Master.AmountMaster;
        //                 _TransNoReturn = mFD_Master.TransactionNo;
        //             }
        //             #endregion

        //             #region Transfer to AR --> Xu ly AR
        //             //Bo modul AR --> Xu ly cong no ben Billing
        //             if (_IsAR == true && _ARNo != "")
        //             {
        //                 TranferAR_CityLedger(_SysDate, _BusinessDate, _ARNo, pt, FolioID, _TransCode, mT.Description, (-1 * _Amount), _CurrencyID, _Ref, _Supp, ref _Message);
        //             }

        //             //Kieem tra dieu kien
        //             /*
        //             if (mResv != null)
        //             {
        //                 if (mResv.Status == 2 || mResv.Type == 1)
        //                 {
        //                     if (_PaymentCheck(_TransCode) == true)
        //                     {
        //                         string[] pn = new string[2];
        //                         object[] pv = new object[2];
        //                         pn[0] = "@FolioID";
        //                         pv[0] = FolioID;
        //                         pn[1] = "@CurrencyID";
        //                         pv[1] = _CurrencyID;
        //                         DataTable dtAR = LicenseBO.Instance.LoadDataFromSP("spARGetBalance", "Source", pn, pv);
        //                         decimal _ARBalance = 0;
        //                         decimal _CityAmount = 0;
        //                         string _TransCode_AR = "";
        //                         if (dtAR.Rows.Count > 0)
        //                         {
        //                             _ARBalance = TextUtils.ToDecimal(dtAR.Rows[0]["Amount"]);
        //                             _TransCode_AR = dtAR.Rows[0]["TransactionCode"].ToString();
        //                             if (_ARBalance != 0)
        //                             {
        //                                 //Khach tra tien
        //                                 if (_Amount < 0)
        //                                 {
        //                                     //Khách nợ
        //                                     if (_ARBalance < 0)
        //                                     {
        //                                         if ((-1 * _Amount) - (-1 * _ARBalance) > 0)
        //                                             _CityAmount = -1 * _ARBalance;
        //                                         else
        //                                             _CityAmount = -1 * _Amount;
        //                                     }
        //                                     //Nợ khách
        //                                     else
        //                                         _CityAmount = -1 * _Amount;
        //                                 }
        //                             }
        //                             //Xu ly tiep
        //                             if (_CityAmount != 0)
        //                             {
        //                                 FolioPayment(FolioID, _CityAmount, _RsvID, "", _TransCode_AR, _BusinessDate, _CurrencyID, "", "", pt);
        //                             }
        //                         }
        //                     }
        //                 }
        //             }
        //              */
        //             #endregion

        //             #region update so du
        //             if (_IsOK == true)
        //                 UpdateBalance(_RsvID, FolioID, pt, ref _Message);
        //             #endregion

        //             #region Commit-Return
        //             pt.CommitTransaction();
        //             pt.CloseConnection();
        //             return true;
        //             #endregion
        //         }
        //         else if (FolioID == -1)
        //         {
        //             _Message = "Folio is locked.";
        //             return false;
        //         }
        //         else
        //         {
        //             return false;
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         pt.CloseConnection();
        //         _Message = ex.Message;
        //         return false;
        //     }
        // }
        // public static bool GenerateTrans(bool AutoPosting, DateTime _SysDate, DateTime _BusinessDate, int _ProID, string _ProCode, string _ConfirmNo, int _RsvID,
        //                                 int _ProfileID, string _AccountName, int _Win, string _TransCode, string _ArCode, string _Ref, string _Supp,
        //                                 decimal _Amount, int _Quan, string _CurrencyID, string _CurrencyLocal, bool _IsAR, string _ARNo,
        //                                 ref decimal _AmountReturn, ref decimal _AmountLocalReturn, ref string _TransNoReturn, ref string _Message, ReservationModel mResv, ProcessTransactions pt)
        // {
        //     try
        //     {
        //         bool _IsOK = false;
        //         //dat --> Su dung cho booking
        //         if (_ProID < 20)
        //             _ProID = _RsvID;

        //         #region Lấy ra thông tin của FolioID
        //         int _RsvID_Return = 0;
        //         int FolioID = GetOrCreateFolioID(_SysDate, _BusinessDate, _ConfirmNo, _RsvID, _Win, _ProfileID, _AccountName, ref _RsvID_Return, pt, ref _Message);
        //         _RsvID = _RsvID_Return;
        //         #endregion

        //         if (FolioID > 0)
        //         {
        //             #region Khai báo Model

        //             FolioDetailModel mFD_Detail = new FolioDetailModel();
        //             FolioDetailModel mFD_Master = new FolioDetailModel();

        //             #endregion

        //             #region Lấy ra thông tin của TransCode
        //             TransactionsModel mT = (TransactionsModel)pt.FindByAttribute("Transactions", "Code", _TransCode)[0];
        //             #endregion

        //             #region Gán giá trị cho các biến Static

        //             //if (_ProID == 0)
        //             //{
        //             //    mFD_Detail.ProfitCenterID = _ProID;
        //             //    mFD_Detail.ProfitCenterCode = _ProCode;
        //             //}
        //             //else
        //             //{
        //             mFD_Detail.ProfitCenterID = ProfitCenterID;
        //             mFD_Detail.ProfitCenterCode = ProfitCenterCode;
        //             //}

        //             mFD_Detail.Status = false;

        //             mFD_Detail.CurrencyID = _CurrencyID;
        //             mFD_Detail.CurrencyMaster = _CurrencyLocal;

        //             mFD_Detail.ReservationID = _RsvID;
        //             mFD_Detail.OriginReservationID = _ProID;
        //             if (mFD_Detail.OriginReservationID <= 0)
        //                 mFD_Detail.OriginReservationID = _RsvID;

        //             mFD_Detail.FolioID = FolioID;
        //             mFD_Detail.OriginFolioID = mFD_Detail.FolioID;

        //             mFD_Detail.Quantity = _Quan;
        //             mFD_Detail.TransactionDate = _BusinessDate;
        //             mFD_Detail.PackageID = 0;

        //             mFD_Detail.UserInsertID = Global.UserID;
        //             mFD_Detail.UserUpdateID = Global.UserID;
        //             mFD_Detail.CreateDate = _SysDate;
        //             mFD_Detail.UpdateDate = _SysDate;
        //             if (AutoPosting == true)
        //             {
        //                 mFD_Detail.UserID = 0;
        //                 mFD_Detail.UserName = "$$";
        //                 mFD_Detail.CashierNo = "";
        //                 mFD_Detail.ShiftID = 0;
        //             }
        //             else
        //             {
        //                 mFD_Detail.UserID = Global.UserID;
        //                 mFD_Detail.UserName = Global.UserName;
        //                 mFD_Detail.CashierNo = Global.UserName;
        //                 mFD_Detail.ShiftID = Global.ShiftID;
        //             }

        //             if (_ProID == 0)
        //             {
        //                 mFD_Master.ProfitCenterID = _ProID;
        //                 mFD_Master.ProfitCenterCode = _ProCode;
        //             }
        //             else
        //             {
        //                 mFD_Master.ProfitCenterID = ProfitCenterID;
        //                 mFD_Master.ProfitCenterCode = ProfitCenterCode;
        //             }

        //             mFD_Master.Status = false;

        //             mFD_Master.CurrencyID = _CurrencyID;
        //             mFD_Master.CurrencyMaster = _CurrencyLocal;

        //             mFD_Master.ReservationID = _RsvID;
        //             //Payment vao master bat chon booking
        //             mFD_Master.OriginReservationID = _ProID;
        //             if (mFD_Master.OriginReservationID <= 0)
        //                 mFD_Master.OriginReservationID = _RsvID;

        //             mFD_Master.FolioID = FolioID;
        //             mFD_Master.OriginFolioID = mFD_Master.FolioID;

        //             mFD_Master.Quantity = _Quan;
        //             mFD_Master.TransactionDate = _BusinessDate;
        //             mFD_Master.PackageID = 0;

        //             mFD_Master.UserInsertID = Global.UserID;
        //             mFD_Master.UserUpdateID = Global.UserID;
        //             mFD_Master.CreateDate = _SysDate;
        //             mFD_Master.UpdateDate = _SysDate;
        //             if (AutoPosting == true)
        //             {
        //                 mFD_Master.UserID = 0;
        //                 mFD_Master.UserName = "$$";
        //                 mFD_Master.CashierNo = "";
        //                 mFD_Master.ShiftID = 0;
        //             }
        //             else
        //             {
        //                 mFD_Master.UserID = Global.UserID;
        //                 mFD_Master.UserName = Global.UserName;
        //                 mFD_Master.CashierNo = Global.UserName;
        //                 mFD_Master.ShiftID = Global.ShiftID;
        //             }
        //             #endregion

        //             #region Kiểm tra xem Transaction này có ở trong Generate ?
        //             List<BaseModel> arr = pt.FindByAttribute("GenerateTransaction", "TransactionCode", _TransCode);
        //             #endregion

        //             #region Nếu chưa tồn tại trong Generate.
        //             if ((arr == null) || (arr.Count == 0))
        //             {
        //                 //Gán thông tin cho các propertie còn lại
        //                 mFD_Detail.IsSplit = false;
        //                 mFD_Detail.Reference = _Ref;
        //                 mFD_Detail.Supplement = _Supp;

        //                 mFD_Detail.TransactionGroupID = mT.TransactionGroupID;
        //                 mFD_Detail.TransactionSubgroupID = mT.TransactionSubGroupID;
        //                 mFD_Detail.GroupCode = mT.GroupCode;
        //                 mFD_Detail.SubgroupCode = mT.SubgroupCode;
        //                 mFD_Detail.GroupType = mT.GroupType;

        //                 mFD_Detail.ArticleCode = _ArCode;
        //                 mFD_Detail.TransactionCode = mT.Code;
        //                 mFD_Detail.Description = mT.Description;//mT.Description;
        //                 mFD_Detail.Amount = _Amount;
        //                 mFD_Detail.AmountBeforeTax = _Amount;
        //                 mFD_Detail.Price = mFD_Detail.Amount / mFD_Detail.Quantity;

        //                 mFD_Detail.AmountMaster = TextUtils.ExchangeCurrency(_BusinessDate, _CurrencyID, _CurrencyLocal, _Amount);
        //                 mFD_Detail.AmountMasterBeforeTax = mFD_Detail.AmountMaster;

        //                 mFD_Detail.AmountGross = mFD_Detail.Amount;
        //                 mFD_Detail.AmountMasterGross = mFD_Detail.AmountMaster;

        //                 mFD_Detail.PostType = 1;
        //                 mFD_Detail.RowState = 1;

        //                 //Thực hiện Post
        //                 mFD_Detail.ID = (int)pt.Insert(mFD_Detail);

        //                 mFD_Detail.InvoiceNo = mFD_Detail.ID.ToString();
        //                 mFD_Detail.TransactionNo = mFD_Detail.ID.ToString();

        //                 pt.Update(mFD_Detail);

        //                 ////Update số dư.
        //                 //UpdateBalance(_RsvID, FolioID, pt, ref _Message);

        //                 //Trả về thông tin
        //                 _AmountReturn = mFD_Detail.Amount;
        //                 _AmountLocalReturn = mFD_Detail.AmountMaster;
        //                 _TransNoReturn = mFD_Detail.TransactionNo;
        //                 _IsOK = true;

        //                 // Ghi histoty
        //                 if (mFD_Detail.GroupType != 1)
        //                     ActionPosting.InsertHistory(pt, _SysDate, _BusinessDate, mFD_Detail.FolioID, mFD_Detail.FolioID, mFD_Detail.InvoiceNo, ActionPosting.HistoryType.Basic_Post,
        //                         ActionPosting.GetActionText(ActionPosting.HistoryType.Basic_Post, mFD_Detail.TransactionCode, mFD_Detail.Description),
        //                         Global.UserName, mFD_Detail.TransactionCode, mFD_Detail.Description, mFD_Detail.Amount, mFD_Detail.Supplement, "", "", "");
        //                 else
        //                     ActionPosting.InsertHistory(pt, _SysDate, _BusinessDate, mFD_Detail.FolioID, mFD_Detail.FolioID, mFD_Detail.InvoiceNo, ActionPosting.HistoryType.Payment,
        //                         ActionPosting.GetActionText(ActionPosting.HistoryType.Payment, mFD_Detail.TransactionCode, mFD_Detail.Description),
        //                         Global.UserName, mFD_Detail.TransactionCode, mFD_Detail.Description, mFD_Detail.Amount, mFD_Detail.Supplement, "", "", "");
        //             }
        //             #endregion

        //             #region Nếu đã tồn tại trong Generate -> lấy ra và thực hiện
        //             else
        //             {
        //                 #region Khai báo biến
        //                 decimal s1 = 0, s2 = 0, s3 = 0;
        //                 decimal CurrentAmount = 0;
        //                 decimal BaseAmount = _Amount;
        //                 decimal Rate = 0;
        //                 GenerateTransactionModel mGT;
        //                 #endregion

        //                 #region Lấy ra thông tin của amount trước thuế
        //                 if (mT.TaxInclude == true)
        //                     BaseAmount = GetAmount(arr, Convert.ToDecimal(BaseAmount));
        //                 #endregion

        //                 #region Insert dòng tổng

        //                 mFD_Master.IsSplit = true;
        //                 mFD_Master.Reference = _Ref;
        //                 mFD_Master.Supplement = _Supp;

        //                 mFD_Master.TransactionGroupID = mT.TransactionGroupID;
        //                 mFD_Master.TransactionSubgroupID = mT.TransactionSubGroupID;
        //                 mFD_Master.GroupCode = mT.GroupCode;
        //                 mFD_Master.SubgroupCode = mT.SubgroupCode;
        //                 mFD_Master.GroupType = mT.GroupType;

        //                 mFD_Master.ArticleCode = _ArCode;
        //                 mFD_Master.TransactionCode = mT.Code;
        //                 mFD_Master.Description = mT.Description;//mT.Description;

        //                 mFD_Master.Quantity = _Quan;

        //                 mFD_Master.Price = 0;
        //                 mFD_Master.Amount = 0;
        //                 mFD_Master.AmountMaster = 0;
        //                 mFD_Master.AmountBeforeTax = 0;
        //                 mFD_Master.AmountMasterBeforeTax = 0;

        //                 mFD_Master.PostType = 2;
        //                 mFD_Master.RowState = 1;

        //                 mFD_Master.ID = (int)pt.Insert(mFD_Master);

        //                 mFD_Master.InvoiceNo = mFD_Master.ID.ToString();
        //                 mFD_Master.TransactionNo = mFD_Master.ID.ToString();
        //                 #endregion

        //                 for (int j = 0; j < arr.Count; j++)
        //                 {
        //                     #region Đổ dữ liệu vào Model
        //                     mGT = (GenerateTransactionModel)arr[j];
        //                     #endregion

        //                     #region Lấy ra CurrentAmount
        //                     if (mGT.Type == 0)
        //                     {
        //                         if (mGT.BaseAmount == 0)
        //                             CurrentAmount = (mGT.Percentage * BaseAmount) / 100;
        //                         else if (mGT.BaseAmount == 1)
        //                             CurrentAmount = (mGT.Percentage * s1) / 100;
        //                         else if (mGT.BaseAmount == 2)
        //                             CurrentAmount = (mGT.Percentage * s2) / 100;
        //                         else
        //                             CurrentAmount = (mGT.Percentage * s3) / 100;
        //                     }
        //                     else if (mGT.Type == 1)
        //                     {
        //                         CurrentAmount = mGT.Amount;
        //                     }
        //                     CurrentAmount = GetAmountFormat(CurrentAmount);
        //                     #endregion

        //                     #region Lấy dữ liệu vào s1,s2,s3
        //                     if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == false) && (mGT.Subtotal3 == false))
        //                     {
        //                         s1 = s1 + CurrentAmount;
        //                     }
        //                     else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == true) && (mGT.Subtotal3 == false))
        //                     {
        //                         s1 = s1 + CurrentAmount;
        //                         s2 = CurrentAmount;
        //                     }
        //                     else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == false) && (mGT.Subtotal3 == true))
        //                     {
        //                         s1 = s1 + CurrentAmount;
        //                         s3 = CurrentAmount;
        //                     }
        //                     else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == true) && (mGT.Subtotal3 == true))
        //                     {
        //                         s1 = s1 + CurrentAmount;
        //                         s2 = CurrentAmount;
        //                         s3 = CurrentAmount;
        //                     }
        //                     #endregion

        //                     #region Đổ dữ liệu vào model Model

        //                     mFD_Detail.IsSplit = false;
        //                     mFD_Detail.PostType = 2;
        //                     mFD_Detail.RowState = 2;

        //                     mFD_Detail.TransactionGroupID = mGT.TransactionGroupID;
        //                     mFD_Detail.TransactionSubgroupID = mGT.TransactionSubGroupID;
        //                     mFD_Detail.GroupCode = mGT.GroupCode;
        //                     mFD_Detail.SubgroupCode = mGT.SubgroupCode;
        //                     mFD_Detail.GroupType = mGT.GroupType;

        //                     mFD_Detail.TransactionCode = mGT.TransactionCodeDetail;
        //                     mFD_Detail.Description = mGT.Description;

        //                     if ((mT.TaxInclude == true) && (j == arr.Count - 1))
        //                         mFD_Detail.Amount = _Amount - mFD_Master.Amount;
        //                     else
        //                         mFD_Detail.Amount = GetAmountFormat(CurrentAmount);
        //                     mFD_Detail.AmountBeforeTax = mFD_Detail.Amount;
        //                     mFD_Detail.Price = mFD_Detail.Amount / mFD_Detail.Quantity;
        //                     mFD_Detail.AmountGross = mFD_Detail.Amount;
        //                     if (j == 0)
        //                     {
        //                         // Tính ra tỉ giá nếu là dòng dầu
        //                         mFD_Detail.AmountMaster = TextUtils.ExchangeCurrency(_BusinessDate, _CurrencyID, _CurrencyLocal, mFD_Detail.Amount);
        //                         Rate = mFD_Detail.AmountMaster / mFD_Detail.Amount;
        //                         // Nếu là dòng đầu -> insert giá trước thuế.
        //                         mFD_Master.AmountBeforeTax = mFD_Detail.Amount;
        //                         mFD_Master.AmountMasterBeforeTax = mFD_Detail.AmountMaster;
        //                     }
        //                     else
        //                         mFD_Detail.AmountMaster = mFD_Detail.Amount * Rate;

        //                     mFD_Detail.AmountMasterBeforeTax = mFD_Detail.AmountMaster;
        //                     mFD_Detail.AmountMasterGross = mFD_Detail.AmountMaster;

        //                     #endregion

        //                     #region Insert Du lieu

        //                     mFD_Detail.InvoiceNo = mFD_Master.InvoiceNo;
        //                     mFD_Detail.TransactionNo = mFD_Master.TransactionNo;
        //                     mFD_Detail.ID = (int)pt.Insert(mFD_Detail);

        //                     mFD_Master.AmountMaster = mFD_Master.AmountMaster + mFD_Detail.AmountMaster;
        //                     mFD_Master.Amount = mFD_Master.Amount + mFD_Detail.Amount;

        //                     #endregion
        //                 }
        //                 // Tính giá Gross
        //                 mFD_Master.AmountGross = mFD_Master.Amount;
        //                 mFD_Master.AmountMasterGross = mFD_Master.AmountMaster;
        //                 // Tính giá Net nếu số tiền nhập vào là giá sau thuế
        //                 if (mT.TaxInclude == true)
        //                 {
        //                     mFD_Master.Amount = _Amount;
        //                     mFD_Master.AmountMaster = _Amount * Rate;
        //                 }
        //                 mFD_Master.Price = mFD_Master.Amount / mFD_Master.Quantity;

        //                 pt.Update(mFD_Master);

        //                 ////Update số dư.
        //                 //UpdateBalance(_RsvID, FolioID, pt, ref _Message);

        //                 //Trả về thông tin
        //                 _AmountReturn = mFD_Master.Amount;
        //                 _AmountLocalReturn = mFD_Master.AmountMaster;
        //                 _TransNoReturn = mFD_Master.TransactionNo;

        //                 _IsOK = true;

        //                 // Ghi histoty
        //                 if (mFD_Master.GroupType != 1)
        //                     ActionPosting.InsertHistory(pt, _SysDate, _BusinessDate, mFD_Master.FolioID, mFD_Master.FolioID, mFD_Master.InvoiceNo, ActionPosting.HistoryType.Gen_Post,
        //                         ActionPosting.GetActionText(ActionPosting.HistoryType.Gen_Post, mFD_Master.TransactionCode, mFD_Master.Description),
        //                         Global.UserName, mFD_Master.TransactionCode, mFD_Master.Description, mFD_Master.Amount, mFD_Master.Supplement, "", "", "");
        //                 else
        //                     ActionPosting.InsertHistory(pt, _SysDate, _BusinessDate, mFD_Master.FolioID, mFD_Master.FolioID, mFD_Master.InvoiceNo, ActionPosting.HistoryType.Payment,
        //                         ActionPosting.GetActionText(ActionPosting.HistoryType.Payment, mFD_Master.TransactionCode, mFD_Master.Description),
        //                         Global.UserName, mFD_Master.TransactionCode, mFD_Master.Description, mFD_Master.Amount, mFD_Master.Supplement, "", "", "");
        //             }
        //             #endregion

        //             #region Transfer to AR --> Xu ly AR
        //             //Bo modul AR --> Xu ly cong no ben Billing
        //             if (_IsAR == true && _ARNo != "")
        //             {
        //                 TranferAR_CityLedger(_SysDate, _BusinessDate, _ARNo, pt, FolioID, _TransCode, mT.Description, (-1 * _Amount), _CurrencyID, _Ref, _Supp, ref _Message);
        //             }

        //             //Kieem tra dieu kien
        //             /*
        //             if (mResv != null)
        //             {
        //                 if (mResv.Status == 2 || mResv.Type == 1)
        //                 {
        //                     if (_PaymentCheck(_TransCode) == true)
        //                     {
        //                         string[] pn = new string[2];
        //                         object[] pv = new object[2];
        //                         pn[0] = "@FolioID";
        //                         pv[0] = FolioID;
        //                         pn[1] = "@CurrencyID";
        //                         pv[1] = _CurrencyID;
        //                         DataTable dtAR = LicenseBO.Instance.LoadDataFromSP("spARGetBalance", "Source", pn, pv);
        //                         decimal _ARBalance = 0;
        //                         decimal _CityAmount = 0;
        //                         string _TransCode_AR = "";
        //                         if (dtAR.Rows.Count > 0)
        //                         {
        //                             _ARBalance = TextUtils.ToDecimal(dtAR.Rows[0]["Amount"]);
        //                             _TransCode_AR = dtAR.Rows[0]["TransactionCode"].ToString();
        //                             if (_ARBalance != 0)
        //                             {
        //                                 //Khach tra tien
        //                                 if (_Amount < 0)
        //                                 {
        //                                     //Khách nợ
        //                                     if (_ARBalance < 0)
        //                                     {
        //                                         if ((-1 * _Amount) - (-1 * _ARBalance) > 0)
        //                                             _CityAmount = -1 * _ARBalance;
        //                                         else
        //                                             _CityAmount = -1 * _Amount;
        //                                     }
        //                                     //Nợ khách
        //                                     else
        //                                         _CityAmount = -1 * _Amount;
        //                                 }
        //                             }
        //                             //Xu ly tiep
        //                             if (_CityAmount != 0)
        //                             {
        //                                 FolioPayment(FolioID, _CityAmount, _RsvID, "", _TransCode_AR, _BusinessDate, _CurrencyID, "", "", pt);
        //                             }
        //                         }
        //                     }
        //                 }
        //             }
        //             */
        //             #endregion

        //             #region update so du
        //             if (_IsOK == true)
        //                 UpdateBalance(_RsvID, FolioID, pt, ref _Message);
        //             #endregion

        //             #region Return
        //             return true;
        //             #endregion
        //         }
        //         else if (FolioID == -1)
        //         {
        //             _Message = "Folio is locked.";
        //             return false;
        //         }
        //         else
        //         {
        //             return false;
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         _Message = ex.Message;
        //         return false;
        //     }
        // }


        // public static bool PostingInvoice(bool AutoPosting, DateTime _SysDate, DateTime _BusinessDate, int _ProID, string _ProCode, string _ConfirmNo,
        //                                 int _RsvID, int _RoomID, int[] _RoomTypeID, string[] _RoomType, int _ProfileID, string _AccountName,
        //                                 int _Win, string _InvoiceCode, string _InvoiceDesc, string _InvoiceRef, string _InvoiceSupp, string _InvoiceNo, string[] _TransCode,
        //                                 string[] _Desc, string[] _ArCode, decimal[] _Amount, bool[] _TaxInclude,
        //                                 int[] _Quan, string[] _CurrencyID, string _CurrencyLocal, string[] _Ref, string[] _Supp, ref string _Message)
        // {
        //     ProcessTransactions pt = new ProcessTransactions();
        //     try
        //     {
        //         #region Mở kết nối cà thực hiện
        //         pt.OpenConnection();
        //         pt.BeginTransaction();
        //         #endregion

        //         #region Lấy ra số FolioID
        //         int _RsvID_Return = 0;
        //         int FolioID = GetOrCreateFolioID(_SysDate, _BusinessDate, _ConfirmNo, _RsvID, _Win, _ProfileID, _AccountName, ref _RsvID_Return, pt, ref _Message);
        //         _RsvID = _RsvID_Return;

        //         #endregion

        //         if (FolioID > 0)
        //         {
        //             #region Khai báo Model

        //             FolioDetailModel mFD_Group = new FolioDetailModel();
        //             FolioDetailModel mFD_Subgroup = new FolioDetailModel();
        //             FolioDetailModel mFD_Detail = new FolioDetailModel();
        //             decimal Rate = 0;

        //             #endregion

        //             #region Gán thông tin của các biến static

        //             #region Group
        //             if (_ProID != 0)
        //             {
        //                 mFD_Group.ProfitCenterID = _ProID;
        //                 mFD_Group.ProfitCenterCode = _ProCode;
        //             }
        //             else
        //             {
        //                 mFD_Group.ProfitCenterID = ProfitCenterID;
        //                 mFD_Group.ProfitCenterCode = ProfitCenterCode;
        //             }
        //             // mới thêm để lấy checkNo
        //             mFD_Group.CheckNo = _InvoiceNo;

        //             mFD_Group.Status = false;

        //             mFD_Group.CurrencyID = _CurrencyLocal;
        //             mFD_Group.CurrencyMaster = _CurrencyLocal;

        //             mFD_Group.ReservationID = _RsvID;
        //             mFD_Group.OriginReservationID = _RsvID;

        //             mFD_Group.RoomID = _RoomID;

        //             mFD_Group.FolioID = FolioID;
        //             mFD_Group.OriginFolioID = FolioID;

        //             mFD_Group.TransactionDate = _BusinessDate;
        //             mFD_Group.PackageID = 0;

        //             mFD_Group.UserID = Global.UserID;
        //             mFD_Group.UserName = Global.UserName;
        //             mFD_Group.CashierNo = Global.UserName;
        //             mFD_Group.ShiftID = Global.ShiftID;

        //             mFD_Group.UserInsertID = Global.UserID;
        //             mFD_Group.UserUpdateID = Global.UserID;
        //             mFD_Group.CreateDate = _SysDate;
        //             mFD_Group.UpdateDate = _SysDate;

        //             if (AutoPosting == true)
        //             {
        //                 mFD_Group.UserID = 0;
        //                 mFD_Group.UserName = "$$";
        //                 mFD_Group.CashierNo = "";
        //                 mFD_Group.ShiftID = 0;
        //             }
        //             else
        //             {
        //                 mFD_Group.UserID = Global.UserID;
        //                 mFD_Group.UserName = Global.UserName;
        //                 mFD_Group.CashierNo = Global.UserName;
        //                 mFD_Group.ShiftID = Global.ShiftID;
        //             }

        //             #endregion

        //             #region Subgroup
        //             if (_ProID != 0)
        //             {
        //                 mFD_Subgroup.ProfitCenterID = _ProID;
        //                 mFD_Subgroup.ProfitCenterCode = _ProCode;
        //             }
        //             else
        //             {
        //                 mFD_Subgroup.ProfitCenterID = ProfitCenterID;
        //                 mFD_Subgroup.ProfitCenterCode = ProfitCenterCode;
        //             }
        //             // mới thêm để lấy checkNo
        //             mFD_Subgroup.CheckNo = _InvoiceNo;
        //             mFD_Subgroup.Status = false;

        //             mFD_Subgroup.CurrencyMaster = _CurrencyLocal;

        //             mFD_Subgroup.ReservationID = _RsvID;
        //             mFD_Subgroup.OriginReservationID = _RsvID;

        //             mFD_Subgroup.RoomID = _RoomID;

        //             mFD_Subgroup.FolioID = FolioID;
        //             mFD_Subgroup.OriginFolioID = FolioID;

        //             mFD_Subgroup.TransactionDate = _BusinessDate;
        //             mFD_Subgroup.PackageID = 0;

        //             mFD_Subgroup.UserID = Global.UserID;
        //             mFD_Subgroup.UserName = Global.UserName;
        //             mFD_Subgroup.CashierNo = Global.UserName;
        //             mFD_Subgroup.ShiftID = Global.ShiftID;

        //             mFD_Subgroup.UserInsertID = Global.UserID;
        //             mFD_Subgroup.UserUpdateID = Global.UserID;
        //             mFD_Subgroup.CreateDate = _SysDate;
        //             mFD_Subgroup.UpdateDate = _SysDate;
        //             if (AutoPosting == true)
        //             {
        //                 mFD_Subgroup.UserID = 0;
        //                 mFD_Subgroup.UserName = "$$";
        //                 mFD_Subgroup.CashierNo = "";
        //                 mFD_Subgroup.ShiftID = 0;
        //             }
        //             else
        //             {
        //                 mFD_Subgroup.UserID = Global.UserID;
        //                 mFD_Subgroup.UserName = Global.UserName;
        //                 mFD_Subgroup.CashierNo = Global.UserName;
        //                 mFD_Subgroup.ShiftID = Global.ShiftID;
        //             }
        //             #endregion

        //             #region Detail
        //             if (_ProID != 0)
        //             {
        //                 mFD_Detail.ProfitCenterID = _ProID;
        //                 mFD_Detail.ProfitCenterCode = _ProCode;
        //             }
        //             else
        //             {
        //                 mFD_Detail.ProfitCenterID = ProfitCenterID;
        //                 mFD_Detail.ProfitCenterCode = ProfitCenterCode;
        //             }
        //             // mới thêm để lấy checkNo
        //             mFD_Detail.CheckNo = _InvoiceNo;

        //             mFD_Detail.Status = false;
        //             mFD_Detail.CurrencyMaster = _CurrencyLocal;

        //             mFD_Detail.ReservationID = _RsvID;
        //             mFD_Detail.OriginReservationID = _RsvID;

        //             mFD_Detail.RoomID = _RoomID;

        //             mFD_Detail.FolioID = FolioID;
        //             mFD_Detail.OriginFolioID = FolioID;

        //             mFD_Detail.TransactionDate = _BusinessDate;
        //             mFD_Detail.PackageID = 0;

        //             mFD_Detail.UserID = Global.UserID;
        //             mFD_Detail.UserName = Global.UserName;
        //             mFD_Detail.CashierNo = Global.UserName;
        //             mFD_Detail.ShiftID = Global.ShiftID;

        //             mFD_Detail.UserInsertID = Global.UserID;
        //             mFD_Detail.UserUpdateID = Global.UserID;
        //             mFD_Detail.CreateDate = _SysDate;
        //             mFD_Detail.UpdateDate = _SysDate;

        //             if (AutoPosting == true)
        //             {
        //                 mFD_Detail.UserID = 0;
        //                 mFD_Detail.UserName = "$$";
        //                 mFD_Detail.CashierNo = "";
        //                 mFD_Detail.ShiftID = 0;
        //             }
        //             else
        //             {
        //                 mFD_Detail.UserID = Global.UserID;
        //                 mFD_Detail.UserName = Global.UserName;
        //                 mFD_Detail.CashierNo = Global.UserName;
        //                 mFD_Detail.ShiftID = Global.ShiftID;
        //             }
        //             #endregion

        //             #endregion

        //             #region Lấy ra thông tin của Transaction Pkg
        //             TransactionsModel mT_Group = (TransactionsModel)pt.FindByAttribute("Transactions", "Code", _InvoiceCode)[0];
        //             #endregion

        //             #region Insert dòng tổng <Invoice>

        //             mFD_Group.IsSplit = true;
        //             mFD_Group.PostType = 3;
        //             mFD_Group.RowState = 1;
        //             mFD_Group.Quantity = 1;

        //             mFD_Group.TransactionGroupID = mT_Group.TransactionGroupID;
        //             mFD_Group.TransactionSubgroupID = mT_Group.TransactionSubGroupID;
        //             mFD_Group.GroupCode = mT_Group.GroupCode;
        //             mFD_Group.SubgroupCode = mT_Group.SubgroupCode;
        //             mFD_Group.GroupType = mT_Group.GroupType;

        //             mFD_Group.ArticleCode = "";
        //             mFD_Group.TransactionCode = mT_Group.Code;

        //             //if (Description == "PKG")
        //             //    mFD_Group.Description = mT_Group.Description;
        //             //else
        //             //    mFD_Group.Description = Description; // mT_Group.Description;

        //             if (_InvoiceNo.Length != 0)
        //                 mFD_Group.Description = _InvoiceDesc + " #" + _InvoiceNo;
        //             else
        //                 mFD_Group.Description = _InvoiceDesc;

        //             mFD_Group.Reference = _InvoiceRef;
        //             mFD_Group.Supplement = _InvoiceSupp;

        //             mFD_Group.RoomID = _RoomID;

        //             mFD_Group.Price = 0;
        //             mFD_Group.Amount = 0;
        //             mFD_Group.AmountMaster = 0;
        //             mFD_Group.AmountGross = 0;
        //             mFD_Group.AmountMasterGross = 0;
        //             mFD_Group.AmountBeforeTax = 0;
        //             mFD_Group.AmountMasterBeforeTax = 0;

        //             mFD_Group.CurrencyID = _CurrencyLocal;
        //             mFD_Group.CurrencyMaster = _CurrencyLocal;

        //             mFD_Group.RoomTypeID = 0;
        //             mFD_Group.RoomType = "";
        //             mFD_Group.ID = (int)pt.Insert(mFD_Group);
        //             mFD_Group.InvoiceNo = mFD_Group.ID.ToString();
        //             mFD_Group.TransactionNo = mFD_Group.InvoiceNo;

        //             #endregion

        //             #region Thực hiện posting chi tiết
        //             for (int i = 0; i < _TransCode.Length; i++)
        //             {
        //                 if (_TransCode[i] != null && _Amount[i] > 0)
        //                 {
        //                     #region Lấy thông tin của Trans.Code
        //                     TransactionsModel mT = (TransactionsModel)pt.FindByAttribute("Transactions", "Code", _TransCode[i])[0];
        //                     #endregion

        //                     #region Kiểm tra xem đã có Generate
        //                     List<BaseModel> arr = pt.FindByAttribute("GenerateTransaction", "TransactionCode", _TransCode[i]);
        //                     #endregion

        //                     #region Nếu chưa tồn tại trong Generate
        //                     if ((arr == null) || (arr.Count == 0))
        //                     {
        //                         mFD_Detail.RoomTypeID = _RoomTypeID[i];
        //                         mFD_Detail.RoomType = _RoomType[i];
        //                         mFD_Detail.CurrencyID = _CurrencyID[i];
        //                         mFD_Detail.IsSplit = false;
        //                         mFD_Detail.PostType = 3;
        //                         mFD_Detail.RowState = 2;

        //                         mFD_Detail.TransactionGroupID = mT.TransactionGroupID;
        //                         mFD_Detail.TransactionSubgroupID = mT.TransactionSubGroupID;
        //                         mFD_Detail.GroupCode = mT.GroupCode;
        //                         mFD_Detail.SubgroupCode = mT.SubgroupCode;
        //                         mFD_Detail.GroupType = mT.GroupType;

        //                         mFD_Detail.ArticleCode = _ArCode[i];
        //                         mFD_Detail.TransactionCode = mT.Code;
        //                         mFD_Detail.Description = _Desc[i];

        //                         mFD_Detail.Quantity = _Quan[i];

        //                         mFD_Detail.Amount = _Amount[i];
        //                         mFD_Detail.AmountBeforeTax = mFD_Detail.Amount;
        //                         mFD_Detail.Price = mFD_Detail.Amount / mFD_Detail.Quantity;

        //                         if (i == 0)
        //                         {
        //                             mFD_Detail.AmountMaster = TextUtils.ExchangeCurrency(_BusinessDate, _CurrencyID[i], _CurrencyLocal, mFD_Detail.Amount);
        //                             Rate = mFD_Detail.AmountMaster / mFD_Detail.Amount;
        //                         }
        //                         else
        //                             mFD_Detail.AmountMaster = mFD_Detail.Amount * Rate;

        //                         mFD_Detail.AmountMasterBeforeTax = mFD_Detail.AmountMaster;

        //                         mFD_Detail.AmountGross = mFD_Detail.Amount;
        //                         mFD_Detail.AmountMasterGross = mFD_Detail.AmountMaster;


        //                         mFD_Detail.InvoiceNo = mFD_Group.InvoiceNo;

        //                         mFD_Detail.ID = (int)pt.Insert(mFD_Detail);
        //                         mFD_Detail.TransactionNo = mFD_Detail.ID.ToString();
        //                         pt.Update(mFD_Detail);


        //                         //Cập nhập thông tin Invoice
        //                         mFD_Group.AmountBeforeTax = mFD_Group.AmountBeforeTax + mFD_Detail.AmountBeforeTax;
        //                         mFD_Group.AmountMasterBeforeTax = mFD_Group.AmountMasterBeforeTax + mFD_Detail.AmountMasterBeforeTax;

        //                         mFD_Group.Amount = mFD_Group.Amount + mFD_Detail.AmountMaster;
        //                         mFD_Group.AmountMaster = mFD_Group.AmountMaster + mFD_Detail.AmountMaster;

        //                         mFD_Group.AmountGross = mFD_Group.AmountGross + mFD_Detail.AmountGross;
        //                         mFD_Group.AmountMasterGross = mFD_Group.AmountMasterGross + mFD_Detail.AmountMasterGross;
        //                     }
        //                     #endregion

        //                     #region Nếu có tồn tại generate -> thực hiện
        //                     else
        //                     {
        //                         #region Khai báo biến
        //                         decimal s1 = 0, s2 = 0, s3 = 0;
        //                         decimal CurrentAmount = 0;
        //                         decimal BaseAmount = _Amount[i];
        //                         GenerateTransactionModel mGT;
        //                         #endregion

        //                         #region Lấy ra thông tin giá trước thuế
        //                         if (_TaxInclude[i] == true)
        //                             BaseAmount = GetAmount(arr, Convert.ToDecimal(BaseAmount));
        //                         #endregion

        //                         #region Insert dòng tổng
        //                         mFD_Subgroup.RoomTypeID = _RoomTypeID[i];
        //                         mFD_Subgroup.RoomType = _RoomType[i];
        //                         mFD_Subgroup.CurrencyID = _CurrencyID[i];
        //                         mFD_Subgroup.IsSplit = true;
        //                         mFD_Subgroup.PostType = 3;
        //                         mFD_Subgroup.RowState = 2;

        //                         mFD_Subgroup.Reference = _Ref[i];
        //                         mFD_Subgroup.Supplement = _Supp[i];

        //                         mFD_Subgroup.TransactionGroupID = mT.TransactionGroupID;
        //                         mFD_Subgroup.TransactionSubgroupID = mT.TransactionSubGroupID;
        //                         mFD_Subgroup.GroupCode = mT.GroupCode;
        //                         mFD_Subgroup.SubgroupCode = mT.SubgroupCode;
        //                         mFD_Subgroup.GroupType = mT.GroupType;

        //                         mFD_Subgroup.ArticleCode = _ArCode[i];
        //                         mFD_Subgroup.TransactionCode = mT.Code;
        //                         mFD_Subgroup.Description = _Desc[i];//mT.Description;

        //                         mFD_Subgroup.Quantity = _Quan[i];

        //                         mFD_Subgroup.Price = 0;
        //                         mFD_Subgroup.Amount = 0;
        //                         mFD_Subgroup.AmountMaster = 0;
        //                         mFD_Subgroup.AmountBeforeTax = 0;
        //                         mFD_Subgroup.AmountMasterBeforeTax = 0;

        //                         mFD_Subgroup.ID = (int)pt.Insert(mFD_Subgroup); //Dong tong cap 2

        //                         mFD_Subgroup.InvoiceNo = mFD_Group.InvoiceNo;
        //                         mFD_Subgroup.TransactionNo = mFD_Subgroup.ID.ToString();
        //                         #endregion

        //                         for (int j = 0; j < arr.Count; j++)
        //                         {
        //                             #region Đổ dữ liệu vào Model
        //                             mGT = (GenerateTransactionModel)arr[j];
        //                             #endregion

        //                             #region Lấy ra CurrentAmount
        //                             if (mGT.Type == 0)
        //                             {
        //                                 if (mGT.BaseAmount == 0)
        //                                     CurrentAmount = (mGT.Percentage * BaseAmount) / 100;
        //                                 else if (mGT.BaseAmount == 1)
        //                                     CurrentAmount = (mGT.Percentage * s1) / 100;
        //                                 else if (mGT.BaseAmount == 2)
        //                                     CurrentAmount = (mGT.Percentage * s2) / 100;
        //                                 else
        //                                     CurrentAmount = (mGT.Percentage * s3) / 100;
        //                             }
        //                             else if (mGT.Type == 1)
        //                             {
        //                                 CurrentAmount = mGT.Amount;
        //                             }

        //                             //CurrentAmount = GetAmountFormat(CurrentAmount);

        //                             #endregion

        //                             #region Lấy dữ liệu vào s1,s2,s3
        //                             if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == false) && (mGT.Subtotal3 == false))
        //                             {
        //                                 s1 = s1 + CurrentAmount;
        //                             }
        //                             else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == true) && (mGT.Subtotal3 == false))
        //                             {
        //                                 s1 = s1 + CurrentAmount;
        //                                 s2 = CurrentAmount;
        //                             }
        //                             else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == false) && (mGT.Subtotal3 == true))
        //                             {
        //                                 s1 = s1 + CurrentAmount;
        //                                 s3 = CurrentAmount;
        //                             }
        //                             else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == true) && (mGT.Subtotal3 == true))
        //                             {
        //                                 s1 = s1 + CurrentAmount;
        //                                 s2 = CurrentAmount;
        //                                 s3 = CurrentAmount;
        //                             }
        //                             #endregion

        //                             #region Đổ dữ liệu vào Model
        //                             mFD_Detail.RoomTypeID = _RoomTypeID[i];
        //                             mFD_Detail.RoomType = _RoomType[i];
        //                             mFD_Detail.CurrencyID = _CurrencyID[i];
        //                             mFD_Detail.IsSplit = false;
        //                             mFD_Detail.PostType = 3;
        //                             mFD_Detail.RowState = 3;

        //                             mFD_Detail.TransactionGroupID = mGT.TransactionGroupID;
        //                             mFD_Detail.TransactionSubgroupID = mGT.TransactionSubGroupID;
        //                             mFD_Detail.GroupCode = mGT.GroupCode;
        //                             mFD_Detail.SubgroupCode = mGT.SubgroupCode;
        //                             mFD_Detail.GroupType = mGT.GroupType;

        //                             mFD_Detail.TransactionCode = mGT.TransactionCodeDetail;
        //                             mFD_Detail.Description = mGT.Description;

        //                             if ((_TaxInclude[i] == true) && (j == arr.Count - 1))
        //                                 mFD_Detail.Amount = Math.Round(_Amount[i] - mFD_Subgroup.Amount, 0);
        //                             else
        //                                 mFD_Detail.Amount = GetAmountFormat(CurrentAmount);

        //                             mFD_Detail.Quantity = _Quan[i];
        //                             mFD_Detail.AmountBeforeTax = Math.Round(mFD_Detail.Amount, 0);
        //                             mFD_Detail.Price = Math.Round(mFD_Detail.Amount / mFD_Detail.Quantity, 0);
        //                             mFD_Detail.AmountGross = Math.Round(mFD_Detail.Amount, 0);

        //                             if ((i == 0) && (j == 0))
        //                             {
        //                                 // Tính ra tỷ giá nếu là dòng đầu
        //                                 mFD_Detail.AmountMaster = Math.Round(TextUtils.ExchangeCurrency(_BusinessDate, _CurrencyID[i], _CurrencyLocal, mFD_Detail.Amount), 0);
        //                                 Rate = mFD_Detail.AmountMaster / mFD_Detail.Amount;
        //                             }
        //                             else
        //                                 mFD_Detail.AmountMaster = Math.Round(mFD_Detail.Amount * Rate, 0);

        //                             if (j == 0)
        //                             {
        //                                 // Nếu là dòng đầu -> insert giá trước thuế.
        //                                 mFD_Subgroup.AmountBeforeTax = Math.Round(mFD_Detail.Amount, 0);
        //                                 mFD_Subgroup.AmountMasterBeforeTax = Math.Round(mFD_Detail.AmountMaster, 0);
        //                             }

        //                             mFD_Detail.AmountMasterBeforeTax = Math.Round(mFD_Detail.AmountMaster, 0);
        //                             mFD_Detail.AmountMasterGross = Math.Round(mFD_Detail.AmountMaster, 0);

        //                             #endregion

        //                             #region Insert Du lieu
        //                             mFD_Detail.InvoiceNo = mFD_Subgroup.InvoiceNo;
        //                             mFD_Detail.TransactionNo = mFD_Subgroup.TransactionNo;
        //                             mFD_Detail.ID = (int)pt.Insert(mFD_Detail);
        //                             mFD_Subgroup.AmountMaster = Math.Round(mFD_Subgroup.AmountMaster + mFD_Detail.AmountMaster, 0);
        //                             mFD_Subgroup.Amount = Math.Round(mFD_Subgroup.Amount + mFD_Detail.Amount, 0);

        //                             #endregion
        //                         }
        //                         // Tính giá Gross
        //                         mFD_Subgroup.AmountGross = mFD_Subgroup.Amount;
        //                         mFD_Subgroup.AmountMasterGross = mFD_Subgroup.AmountMaster;
        //                         // Tính giá Net số tiền nhập vào là giá sau thuế
        //                         if (_TaxInclude[i] == true)
        //                         {
        //                             mFD_Subgroup.Amount = _Amount[i];
        //                             mFD_Subgroup.AmountMaster = Math.Round(_Amount[i] * Rate, 0);
        //                         }
        //                         mFD_Subgroup.Price = Math.Round(mFD_Subgroup.Amount / mFD_Subgroup.Quantity, 0);
        //                         // Update thông tin của subgroup
        //                         pt.Update(mFD_Subgroup);
        //                         // Cập nhật thông tin group
        //                         mFD_Group.AmountBeforeTax = Math.Round(mFD_Group.AmountBeforeTax + mFD_Subgroup.AmountBeforeTax, 0);
        //                         mFD_Group.AmountMasterBeforeTax = Math.Round(mFD_Group.AmountMasterBeforeTax + mFD_Subgroup.AmountMasterBeforeTax, 0);

        //                         mFD_Group.Amount = Math.Round(mFD_Group.Amount + mFD_Subgroup.Amount, 0);
        //                         mFD_Group.AmountMaster = Math.Round(mFD_Group.AmountMaster + mFD_Subgroup.AmountMaster, 0);

        //                         mFD_Group.AmountGross = Math.Round(mFD_Group.AmountGross + mFD_Subgroup.AmountGross, 0);
        //                         mFD_Group.AmountMasterGross = Math.Round(mFD_Group.AmountMasterGross + mFD_Subgroup.AmountMasterGross, 0);
        //                     }
        //                     #endregion
        //                 }
        //             }
        //             #endregion

        //             #region Commit va Return

        //             mFD_Group.Price = mFD_Group.Amount;
        //             pt.Update(mFD_Group);
        //             UpdateBalance(_RsvID, FolioID, pt, ref _Message);
        //             // Ghi histoty
        //             ActionPosting.InsertHistory(pt, _SysDate, _BusinessDate, mFD_Group.FolioID, mFD_Group.FolioID, mFD_Group.InvoiceNo, ActionPosting.HistoryType.Gen_Post,
        //                 ActionPosting.GetActionText(ActionPosting.HistoryType.Gen_Post, mFD_Group.TransactionCode, mFD_Group.Description),
        //                 Global.UserName, mFD_Group.TransactionCode, mFD_Group.Description, mFD_Group.Amount, mFD_Group.Supplement, "", "", "");

        //             pt.CommitTransaction();
        //             pt.CloseConnection();
        //             return true;

        //             #endregion
        //         }
        //         else
        //         {
        //             return false;
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         pt.CloseConnection();
        //         _Message = ex.Message;
        //         return false;
        //     }
        // }

        /// <summary>
        /// Hàm chuyển khoản thanh toán từ FO -> AR Trans
        /// </summary>
        /// <param name="_SysDate"></param>
        /// <param name="_BusinessDate"></param>
        /// <param name="_ArNo"></param>
        /// <param name="_FolioID"></param>
        /// <param name="_Amount"></param>
        /// <param name="_CurrencyID"></param>
        /// <param name="_CurrencyLimit"></param>
        /// <param name="_Ref"></param>
        /// <param name="_Supp"></param>
        /// <param name="pt"></param>
        /// <param name="_Message"></param>
        /// <returns></returns>
        public static bool TranferAR(DateTime _SysDate, DateTime _BusinessDate, string _ArNo, int _FolioID, string _FolioName, decimal _Amount, string _CurrencyID, string _CurrencyLimit, string _Ref, string _Supp, ProcessTransactions pt, ref string _Message)
        {
            try
            {
                AccountReceivableTransModel mAR = new AccountReceivableTransModel();
                mAR.ARNo = _ArNo;
                mAR.FolioID = _FolioID;
                mAR.FolioName = _FolioName;
                mAR.TransactionDate = _BusinessDate;
                mAR.Description = "";

                mAR.Amount = _Amount;
                mAR.AmountMaster = _Amount;
                mAR.CurrencyID = _CurrencyID;
                mAR.CurrencyMaster = _CurrencyID;

                mAR.AmountCurrencyLimit = TextUtils.ExchangeCurrency(_BusinessDate, _CurrencyID, _CurrencyLimit, _Amount);
                mAR.CurrencyLimit = _CurrencyLimit;

                mAR.Reference = _Ref;
                mAR.Supplement = _Supp;
                mAR.IsTranferFO = true;

                mAR.CreateDate = _SysDate;
                mAR.UpdateDate = _SysDate;
                mAR.UserInsertID = Global.UserID;
                mAR.UserUpdateID = Global.UserID;

                pt.Insert(mAR);
                return true;
            }
            catch (Exception ex)
            {
                return false;
                _Message = ex.Message;
            }
        }

        /// <summary>
        /// Hàm lấy ra ID của Folio
        /// </summary>
        /// <param name="_SysDate"></param>
        /// <param name="_BusinessDate"></param>
        /// <param name="_ConfirmationNo"></param>
        /// <param name="_ReservationID"></param>
        /// <param name="_WindowNo"></param>
        /// <param name="_ProfileID"></param>
        /// <param name="_AccountName"></param>
        /// <param name="_Message"></param>
        /// <returns></returns>
        public static int GetOrCreateFolioID(DateTime _SysDate, DateTime _BusinessDate, string _ConfirmationNo, int _ReservationID,
                                             int _WindowNo, int _ProfileID, string _AccountName, ref int _ReservationID_Return, ref string _Message)
        {
            try
            {

                #region Kiểm tra đã có folio này hay chưa
                Expression exp;
                if (_WindowNo < 0)
                {
                    exp = new Expression("ConfirmationNo", _ConfirmationNo, "=");
                    exp = exp.And(new Expression("FolioNo", _WindowNo, "="));
                }
                else
                {
                    exp = new Expression("ReservationID", _ReservationID, "=");
                    exp = exp.And(new Expression("FolioNo", _WindowNo, "="));
                }
                ArrayList arr = FolioBO.Instance.FindByExpression(exp);
                #endregion

                #region Nếu có rồi thì trả về ID thông tin
                if ((arr != null) && (arr.Count > 0))
                {
                    _ReservationID_Return = ((FolioModel)arr[0]).ReservationID;
                    if (((FolioModel)arr[0]).Status == false)
                        return ((FolioModel)arr[0]).ID;
                    else
                        return -1;
                }
                #endregion

                #region Nếu chưa có thì tạo mới
                else
                {
                    FolioModel mF = new FolioModel();
                    mF.ARNo = "";
                    mF.BalanceVND = 0;
                    mF.BalanceUSD = 0;
                    mF.ConfirmationNo = _ConfirmationNo;
                    mF.FolioDate = _BusinessDate;
                    mF.CreateDate = _SysDate;
                    mF.UpdateDate = _SysDate;
                    mF.UserInsertID = Global.UserID;
                    mF.UserUpdateID = Global.UserID;
                    mF.FolioNo = _WindowNo;
                    mF.ProfileID = _ProfileID;
                    mF.AccountName = _AccountName;
                    mF.Status = false;
                    if (_WindowNo < 0)
                    {
                        mF.IsMasterFolio = true;
                        mF.ReservationID = GetOrCreateRsvMaster(_SysDate, _ConfirmationNo, _ReservationID, ref _Message);
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

        /// <summary>
        /// Hàm lấy ra ID của Folio
        /// </summary>
        /// <param name="_SysDate"></param>
        /// <param name="_BusinessDate"></param>
        /// <param name="_ConfirmationNo"></param>
        /// <param name="_ReservationID"></param>
        /// <param name="_WindowNo"></param>
        /// <param name="_ProfileID"></param>
        /// <param name="_AccountName"></param>
        /// <param name="_Message"></param>
        /// <returns></returns>
        public static int GetOrCreateFolioID(DateTime _SysDate, DateTime _BusinessDate, string _ConfirmationNo, int _ReservationID,
                                             int _WindowNo, int _ProfileID, string _AccountName, ref int _ReservationID_Return, ProcessTransactions pt, ref string _Message)
        {
            try
            {

                #region Kiểm tra đã có folio này hay chưa
                Expression exp;
                if (_WindowNo < 0)
                {
                    exp = new Expression("ConfirmationNo", _ConfirmationNo, "=");
                    exp = exp.And(new Expression("FolioNo", _WindowNo, "="));
                }
                else
                {
                    exp = new Expression("ReservationID", _ReservationID, "=");
                    exp = exp.And(new Expression("FolioNo", _WindowNo, "="));
                }
                ArrayList arr = pt.FindByExpression("Folio", exp);
                #endregion

                #region Nếu có rồi thì trả về ID thông tin
                if ((arr != null) && (arr.Count > 0))
                {
                    _ReservationID_Return = ((FolioModel)arr[0]).ReservationID;

                    if (((FolioModel)arr[0]).Status == false)
                        return ((FolioModel)arr[0]).ID;
                    else
                        return -1;
                }
                #endregion

                #region Nếu chưa có thì tạo mới
                else
                {
                    FolioModel mF = new FolioModel();
                    mF.ARNo = "";
                    mF.BalanceVND = 0;
                    mF.BalanceUSD = 0;
                    mF.ConfirmationNo = _ConfirmationNo;
                    mF.FolioDate = _BusinessDate;
                    mF.CreateDate = _SysDate;
                    mF.UpdateDate = _SysDate;
                    mF.UserInsertID = Global.UserID;
                    mF.UserUpdateID = Global.UserID;
                    mF.FolioNo = _WindowNo;
                    mF.ProfileID = _ProfileID;
                    mF.AccountName = _AccountName;
                    mF.Status = false;
                    if (_WindowNo < 0)
                    {
                        mF.IsMasterFolio = true;
                        mF.ReservationID = GetOrCreateRsvMaster(_SysDate, _ConfirmationNo, _ReservationID, pt, ref _Message);
                    }
                    else
                    {
                        mF.IsMasterFolio = false;
                        mF.ReservationID = _ReservationID;
                    }
                    if (mF.ReservationID > 0)
                    {
                        _ReservationID_Return = mF.ReservationID;
                        return (int)pt.Insert(mF);
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

        /// <summary>
        /// Lấy ra ID của Reservation ảo của 1 số confirm
        /// </summary>
        /// <param name="_SysDate"></param>
        /// <param name="_ConfirmationNo"></param>
        /// <param name="_FromRsvID"></param>
        /// <returns>Int</returns>
        public static int GetOrCreateRsvMaster(DateTime _SysDate, string _ConfirmationNo, int _FromRsvID, ref string _Message)
        {
            try
            {
                //Kiểm tra xem RsvMA đã có hay chưa
                Expression exp = new Expression("ConfirmationNo", _ConfirmationNo, "=");
                exp = exp.And(new Expression("ReservationNo", "0", "="));
                ArrayList arr = ReservationBO.Instance.FindByExpression(exp);
                //Nếu có rồi thì lấy ra
                if ((arr != null) && (arr.Count > 0))
                    return ((ReservationModel)arr[0]).ID;
                //Nếu chưa có thì tạo mới
                else
                {
                    ReservationModel mR = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(_FromRsvID);
                    // Nhặt RoomType ảo
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
                    mR.UserInsertId = Global.UserID;
                    mR.UserUpdateId = Global.UserID;
                    mR.CreateDate = _SysDate;
                    mR.UpdateDate = _SysDate;
                    mR.ProfileIndividualId = 0;
                    mR.LastName = "";
                    mR.ReservationNo = "0";
                    mR.ShareRoom = 0;

                    mR.Status = 1;

                    return (int)ReservationBO.Instance.Insert(mR);
                }
            }
            catch (Exception ex)
            {
                _Message = ex.Message;
                return 0;
            }
        }

        /// <summary>
        /// Lấy ra ID của Reservation ảo của 1 số confirm
        /// </summary>
        /// <param name="_SysDate"></param>
        /// <param name="_ConfirmationNo"></param>
        /// <param name="_FromRsvID"></param>
        /// <returns>Int</returns>
        public static int GetOrCreateRsvMaster(DateTime _SysDate, string _ConfirmationNo, int _FromRsvID, ProcessTransactions pt, ref string _Message)
        {
            try
            {
                //Kiểm tra xem RsvMA đã có hay chưa
                Expression exp = new Expression("ConfirmationNo", _ConfirmationNo, "=");
                exp = exp.And(new Expression("ReservationNo", "0", "="));
                ArrayList arr = pt.FindByExpression("Reservation", exp);
                //Nếu có rồi thì lấy ra
                if ((arr != null) && (arr.Count > 0))
                    return ((ReservationModel)arr[0]).ID;
                //Nếu chưa có thì tạo mới
                else
                {
                    ReservationModel mR = (ReservationModel)pt.FindByPK("Reservation", _FromRsvID);
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
                    mR.UserInsertId = Global.UserID;
                    mR.UserUpdateId = Global.UserID;
                    mR.CreateDate = _SysDate;
                    mR.UpdateDate = _SysDate;
                    mR.ProfileIndividualId = 0;
                    mR.LastName = "";
                    mR.ReservationNo = "0";
                    mR.ShareRoom = 0;

                    mR.Status = 1;
                    return (int)pt.Insert(mR);
                }
            }
            catch (Exception ex)
            {
                _Message = ex.Message;
                return 0;
            }
        }

        public static int GetRoomByRoomType(int RoomTypeID)
        {
            int _RoomID = 0;
            if (RoomTypeID > 0)
            {
                DataTable dtR = TextUtils.Select("SELECT TOP 1 ID FROM dbo.Room WITH (NOLOCK) WHERE RoomTypeID =" + RoomTypeID + "");
                if (dtR != null)
                {
                    if (dtR.Rows.Count > 0)
                        _RoomID = TextUtils.ToInt(dtR.Rows[0]["ID"].ToString() ?? string.Empty);
                }
            }
            return _RoomID;
        }
        #endregion

        #region Các hàm chức năng  khác

        public static void CopyModel(BaseModel _FromModel, ref BaseModel _ToModel)
        {
            PropertyInfo[] _PropertiesFrom = _FromModel.GetType().GetProperties();
            PropertyInfo[] _PropertiesTo = _ToModel.GetType().GetProperties();
            Object value = null;
            for (int i = 0; i < _PropertiesFrom.Length; i++)
            {
                value = _PropertiesFrom[i].GetValue(_FromModel, null);
                _PropertiesTo[i].SetValue(_ToModel, value, null);
            }
        }
        public static void CopyModel(FolioDetailModel _FromModel, ref FolioDetailModel _ToModel)
        {
            PropertyInfo[] _PropertiesFrom = _FromModel.GetType().GetProperties();
            PropertyInfo[] _PropertiesTo = _ToModel.GetType().GetProperties();
            Object value = null;
            for (int i = 0; i < _PropertiesFrom.Length; i++)
            {
                value = _PropertiesFrom[i].GetValue(_FromModel, null);
                _PropertiesTo[i].SetValue(_ToModel, value, null);
            }
        }
        public static void CopyModel(FolioDetailModel _FromModel, ref FolioDetailModel _ToModel1, ref FolioDetailModel _ToModel2)
        {
            PropertyInfo[] _PropertiesFrom = _FromModel.GetType().GetProperties();
            PropertyInfo[] _PropertiesTo1 = _ToModel1.GetType().GetProperties();
            PropertyInfo[] _PropertiesTo2 = _ToModel2.GetType().GetProperties();
            Object value = null;
            for (int i = 0; i < _PropertiesFrom.Length; i++)
            {
                value = _PropertiesFrom[i].GetValue(_FromModel, null);
                _PropertiesTo1[i].SetValue(_ToModel1, value, null);
                _PropertiesTo2[i].SetValue(_ToModel2, value, null);
            }
        }

        /// <summary>
        /// Chuyển các khoản Deposit sang Folio
        /// </summary>
        /// <param name="pt">Class xử lí Transacion</param>
        /// <param name="ReservationID">ID của Reservation</param>
        /// <param name="err">Thông tin lỗi nếu có phát sinh.</param>
        /// <returns>Boolean</returns>
        public static bool TranferDeposit(ProcessTransactions pt, string _ConfirmationNo, int _ReservationID, int _ProfileID, string _AccountName, ref string _err)
        {
            #region Kiểm tra điều kiện - Nếu không có khoản Deposit nào thì không tranfer ( return).
            //ArrayList arr = pt.FindByAttribute("DepositPayment", "ReservationID", _ReservationID.ToString());
            SqlParameter[] param =
               [
                   new SqlParameter("@ReservationID", _ReservationID),
                ];
            DataTable dtDeposit = DataTableHelper.getTableData("spGetDepositTransfer", param);
            #endregion

            #region Nếu tồn tại deposit thì chuyển sang Folio của khách
            //if (arr.Count == 0)
            if (dtDeposit.Rows.Count > 0)
            {

                #region Khai báo biến
                //DepositPaymentModel mDP;
                int _FolioID = 0;
                int _MasterFolioID = 0;
                bool _IsMaster = false;
                string error = "";
                //DataTable dtRsv = pt.Select("select ReservationNo from Reservation" + CSHUtils.LoadMode + "where ID=" + _ReservationID);
                //string _ReservationNo = dtRsv.Rows[0][0].ToString();                    
                #endregion

                #region Get Folio
                DataTable dtRsv = pt.Select("SELECT ID FROM Reservation WITH (NOLOCK) WHERE ConfirmationNo = '" + _ConfirmationNo + "' AND ReservationNo =0 ");
                //Master Folio
                if (dtRsv.Rows.Count > 0)
                {
                    _MasterFolioID = ReservationBO.GetFolioID(int.Parse(dtRsv.Rows[0][0].ToString()), -1, _ConfirmationNo, pt);
                    _IsMaster = true;
                }
                //Folio Default
                _FolioID = ReservationBO.GetFolioID(_ReservationID, 1, _ConfirmationNo, pt);
                #endregion

                DateTime _SysDate = TextUtils.GetSystemDate();
                DateTime _BusDate = TextUtils.GetBusinessDate();
                if (_BusDate == null)
                    _BusDate = _SysDate;
                string _CurrencyLocal = TextUtils.GetMasterCurrency();
                decimal _Am1 = 0, _Am2 = 0; string _Trans = "";

                for (int i = 0; i < dtDeposit.Rows.Count; i++)
                {
                    //Transfer to Folio Master 
                    if (bool.Parse(dtDeposit.Rows[i]["IsMasterFolio"].ToString()) == true && _IsMaster == true)
                    {
                        if (_MasterFolioID == 0)
                            _MasterFolioID = ReservationBO.CreateFolioNoRouting(int.Parse(dtRsv.Rows[0][0].ToString()), -1, pt);

                        GenerateTransDeposit(pt, _MasterFolioID, _SysDate, _BusDate, Global.UserID, Global.UserName, "", Global.ShiftID, 0, "",
                            _ConfirmationNo, _ReservationID, _ProfileID, _AccountName, -1, dtDeposit.Rows[i]["TransactionCode"].ToString(), "",
                            dtDeposit.Rows[i]["Reference"].ToString(), dtDeposit.Rows[i]["Supplement"].ToString(),
                            -1 * decimal.Parse(dtDeposit.Rows[i]["Amount"].ToString()), 1, dtDeposit.Rows[i]["CurrencyID"].ToString(), _CurrencyLocal,
                            ref _Am1, ref _Am2, ref _Trans, ref error);
                    }
                    //Transfer to Folio Guest
                    else
                    {
                        GenerateTransDeposit(pt, _FolioID, _SysDate, _BusDate, Global.UserID, Global.UserName, "", Global.ShiftID, 0, "",
                            _ConfirmationNo, _ReservationID, _ProfileID, _AccountName, 1, dtDeposit.Rows[i]["TransactionCode"].ToString(), "",
                            dtDeposit.Rows[i]["Reference"].ToString(), dtDeposit.Rows[i]["Supplement"].ToString(),
                            (-1 * Convert.ToDecimal(dtDeposit.Rows[i]["Amount"].ToString())), 1, dtDeposit.Rows[i]["CurrencyID"].ToString(), _CurrencyLocal,
                            ref _Am1, ref _Am2, ref _Trans, ref error);
                    }
                }

                #region 2.Code old
                //for (int i = 0; i < arr.Count; i++)
                //{
                //    mDP = (DepositPaymentModel)arr[i];
                //    //Nếu chưa transfer
                //    if (mDP.IsProcess == false)
                //    {
                //        //Transfer to Folio Master 
                //        if (mDP.IsMasterFolio == true && _IsMaster == true)
                //        {
                //            if (_MasterFolioID == 0)
                //                _MasterFolioID = ClassReservation.CreateFolioNoRouting(int.Parse(dtRsv.Rows[0][0].ToString()), -1, pt);

                //            GenerateTransDeposit(pt, _MasterFolioID, _SysDate, mDP.TransactionDate, mDP.UserID, mDP.UserName, mDP.CashierNo, mDP.ShiftID,
                //            0, "", _ConfirmationNo, _ReservationID, _ProfileID, _AccountName, -1, mDP.PaymentCode, "", mDP.Reference, mDP.Supplement,
                //            -1 * mDP.Amount, 1, mDP.CurrencyID, _CurrencyLocal, ref _Am1, ref _Am2, ref _Trans, ref error);
                //        }
                //        //Transfer to Folio Guest
                //        else
                //        {
                //            GenerateTransDeposit(pt, _FolioID, _SysDate, mDP.TransactionDate, mDP.UserID, mDP.UserName, mDP.CashierNo, mDP.ShiftID,
                //            0, "", _ConfirmationNo, _ReservationID, _ProfileID, _AccountName, 1, mDP.PaymentCode, "", mDP.Reference, mDP.Supplement,
                //            -1 * mDP.Amount, 1, mDP.CurrencyID, _CurrencyLocal, ref _Am1, ref _Am2, ref _Trans, ref error);
                //        }
                //    }
                //}
                #endregion

                // Update DepositPayment
                pt.UpdateCommand("Update DepositPayment set IsProcess=1 where ID IN (SELECT ID FROM DepositPayment WITH (NOLOCK) WHERE ReservationID= " + _ReservationID + " AND TransactionCode ='" + dtDeposit.Rows[0]["TransactionCode"].ToString() + "') ");
            }
            //Return
            return true;
            #endregion
        }


        //Chưa xử lí History
        private static bool GenerateTransDeposit(ProcessTransactions pt, int FolioID, DateTime _SysDate, DateTime _PostDate, int _UserID, string _UserName, string _CashierNo, int _ShiftID,
                                         int _ProID, string _ProCode, string _ConfirmNo, int _RsvID, int _ProfileID, string _AccountName, int _Win, string _TransCode,
                                         string _ArCode, string _Ref, string _Supp, decimal _Amount, int _Quan, string _CurrencyID, string _CurrencyLocal,
                                         ref decimal _AmountReturn, ref decimal _AmountLocalReturn, ref string _TransNoReturn, ref string _Message)
        {

            #region Lấy ra thông tin của FolioID
            /* bỏ CSS
                int _RsvID_Return = 0;
                int FolioID = GetOrCreateFolioID(_SysDate, _SysDate, _ConfirmNo, _RsvID, _Win, _ProfileID, _AccountName, ref _RsvID_Return, pt, ref _Message);
                _RsvID = _RsvID_Return;
                */
            #endregion

            if (FolioID > 0)
            {
                #region Khai báo Model

                FolioDetailModel mFD_Detail = new FolioDetailModel();
                FolioDetailModel mFD_Master = new FolioDetailModel();

                #endregion

                #region Lấy ra thông tin của TransCode
                TransactionsModel mT = (TransactionsModel)TransactionsBO.Instance.FindByAttribute("Code", _TransCode)[0];
                #endregion

                #region Gán giá trị cho các biến Static

                mFD_Detail.ProfitCenterID = _ProID;
                mFD_Detail.ProfitCenterCode = _ProCode;
                mFD_Detail.Status = false;

                mFD_Detail.CurrencyID = _CurrencyID;
                mFD_Detail.CurrencyMaster = _CurrencyLocal;

                mFD_Detail.ReservationID = _RsvID;
                mFD_Detail.OriginReservationID = _RsvID;

                mFD_Detail.FolioID = FolioID;
                mFD_Detail.OriginFolioID = mFD_Detail.FolioID;

                mFD_Detail.Quantity = _Quan;
                mFD_Detail.TransactionDate = _PostDate;
                mFD_Detail.PackageID = 0;

                mFD_Detail.UserInsertID = _UserID;
                mFD_Detail.UserUpdateID = _UserID;
                mFD_Detail.CreateDate = _SysDate;
                mFD_Detail.UpdateDate = _SysDate;

                mFD_Detail.UserID = _UserID;
                mFD_Detail.UserName = _UserName;
                mFD_Detail.CashierNo = _CashierNo;
                mFD_Detail.ShiftID = _ShiftID;

                mFD_Master.ProfitCenterID = _ProID;
                mFD_Master.ProfitCenterCode = _ProCode;
                mFD_Master.Status = false;

                mFD_Master.CurrencyID = _CurrencyID;
                mFD_Master.CurrencyMaster = _CurrencyLocal;

                mFD_Master.ReservationID = _RsvID;
                mFD_Master.OriginReservationID = _RsvID;

                mFD_Master.FolioID = FolioID;
                mFD_Master.OriginFolioID = mFD_Master.FolioID;

                mFD_Master.Quantity = _Quan;
                mFD_Master.TransactionDate = _PostDate;
                mFD_Master.PackageID = 0;

                mFD_Master.UserInsertID = _UserID;
                mFD_Master.UserUpdateID = _UserID;
                mFD_Master.CreateDate = _SysDate;
                mFD_Master.UpdateDate = _SysDate;

                mFD_Master.UserID = _UserID;
                mFD_Master.UserName = _UserName;
                mFD_Master.CashierNo = _CashierNo;
                mFD_Master.ShiftID = _ShiftID;

                #endregion

                #region Kiểm tra xem Transaction này có ở trong Generate ?
                ArrayList arr = GenerateTransactionBO.Instance.FindByAttribute("TransactionCode", _TransCode);
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

                    mFD_Detail.ArticleCode = _ArCode;
                    mFD_Detail.TransactionCode = mT.Code;
                    mFD_Detail.Description = mT.Description;
                    mFD_Detail.Amount = _Amount;
                    mFD_Detail.AmountBeforeTax = _Amount;
                    mFD_Detail.Price = mFD_Detail.Amount / mFD_Detail.Quantity;

                    mFD_Detail.AmountMaster = TextUtils.ExchangeCurrency(_PostDate, _CurrencyID, _CurrencyLocal, _Amount);
                    mFD_Detail.AmountMasterBeforeTax = mFD_Detail.AmountMaster;

                    mFD_Detail.AmountGross = mFD_Detail.Amount;
                    mFD_Detail.AmountMasterGross = mFD_Detail.AmountMaster;

                    mFD_Detail.PostType = 1;
                    mFD_Detail.RowState = 1;
                    //Thực hiện Post
                    mFD_Detail.ID = (int)pt.Insert(mFD_Detail);

                    mFD_Detail.InvoiceNo = mFD_Detail.ID.ToString();
                    mFD_Detail.TransactionNo = mFD_Detail.ID.ToString();

                    pt.Update(mFD_Detail);
                    //Update số dư.
                    UpdateBalance(_RsvID, FolioID, pt, ref _Message);
                    //Trả về thông tin
                    _AmountReturn = mFD_Detail.Amount;
                    _AmountLocalReturn = mFD_Detail.AmountMaster;
                    _TransNoReturn = mFD_Detail.TransactionNo;
                }
                #endregion

                #region Nếu đã tồn tại trong Generate -> lấy ra và thực hiện
                else
                {
                    #region Khai báo biến
                    decimal s1 = 0, s2 = 0, s3 = 0;
                    decimal CurrentAmount = 0;
                    decimal BaseAmount = _Amount;
                    decimal Rate = 0;
                    GenerateTransactionModel mGT;
                    #endregion

                    #region Lấy ra thông tin của amount trước thuế
                    if (mT.TaxInclude == true)
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
                    mFD_Master.Description = mT.Description;

                    mFD_Master.Quantity = _Quan;

                    mFD_Master.Price = 0;
                    mFD_Master.Amount = 0;
                    mFD_Master.AmountMaster = 0;
                    mFD_Master.AmountBeforeTax = 0;
                    mFD_Master.AmountMasterBeforeTax = 0;

                    mFD_Master.PostType = 2;
                    mFD_Master.RowState = 1;

                    mFD_Master.ID = (int)pt.Insert(mFD_Master);

                    mFD_Master.InvoiceNo = mFD_Master.ID.ToString();
                    mFD_Master.TransactionNo = mFD_Master.ID.ToString();
                    #endregion

                    for (int j = 0; j < arr.Count; j++)
                    {
                        #region Đổ dữ liệu vào Model
                        mGT = (GenerateTransactionModel)arr[j];
                        #endregion

                        #region Lấy ra  CurrentAmount
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

                        #region Đổ dữ liệu vào model Model

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

                        mFD_Detail.Amount = GetAmountFormat(CurrentAmount);
                        mFD_Detail.AmountBeforeTax = mFD_Detail.Amount;

                        mFD_Detail.Price = mFD_Detail.Amount / mFD_Detail.Quantity;

                        if (j == 0)
                        {
                            mFD_Detail.AmountMaster = TextUtils.ExchangeCurrency(_PostDate, _CurrencyID, _CurrencyLocal, mFD_Detail.Amount);

                            mFD_Master.AmountBeforeTax = mFD_Detail.Amount;
                            mFD_Master.AmountMasterBeforeTax = mFD_Detail.AmountMaster;

                            Rate = mFD_Detail.AmountMaster / mFD_Detail.Amount;
                        }
                        else
                            mFD_Detail.AmountMaster = mFD_Detail.Amount * Rate;

                        mFD_Detail.AmountMasterBeforeTax = mFD_Detail.AmountMaster;
                        //Gross
                        mFD_Detail.AmountGross = mFD_Detail.Amount;
                        mFD_Detail.AmountMasterGross = mFD_Detail.AmountMaster;
                        #endregion

                        #region Insert Du lieu

                        mFD_Detail.InvoiceNo = mFD_Master.InvoiceNo;
                        mFD_Detail.TransactionNo = mFD_Master.TransactionNo;
                        mFD_Detail.ID = (int)pt.Insert(mFD_Detail);

                        mFD_Master.AmountMaster = mFD_Master.AmountMaster + mFD_Detail.AmountMaster;
                        mFD_Master.Amount = mFD_Master.Amount + mFD_Detail.Amount;

                        #endregion
                    }
                    //Tính giá Gross
                    mFD_Master.AmountGross = mFD_Master.Amount;
                    mFD_Master.AmountMasterGross = mFD_Master.AmountMaster;
                    //Tính giá Net nếu số tiền nhập vào là giá sau thuế
                    if (mT.TaxInclude == true)
                    {
                        mFD_Master.Amount = _Amount;
                        mFD_Master.AmountMaster = _Amount * Rate;
                    }

                    mFD_Master.Price = mFD_Master.Amount / mFD_Master.Quantity;

                    pt.Update(mFD_Master);
                    //Update số dư.
                    UpdateBalance(_RsvID, FolioID, pt, ref _Message);
                    //Trả về thông tin
                    _AmountReturn = mFD_Master.Amount;
                    _AmountLocalReturn = mFD_Master.AmountMaster;
                    _TransNoReturn = mFD_Master.TransactionNo;
                }
                #endregion

                #region Commit-Return
                return true;
                #endregion
            }
            else
            {
                return false;
            }
        }

        public static int GetNumOfDay(DateTime Departure, DateTime Arrival)
        {
            TimeSpan span = Departure - Arrival;
            int num = (int)(span.TotalDays);
            if (num == 0)
            {
                return 1;
            }
            return num;
        }

        // public static void _ExportExcel(DevExpress.XtraGrid.Views.Grid.GridView grvData)
        // {
        //     string filepath = System.IO.Path.GetTempFileName();
        //     filepath = filepath.Remove(filepath.LastIndexOf('.') + 1);
        //     filepath = String.Concat(filepath, "xls");

        //     grvData.OptionsPrint.AutoWidth = false;
        //     grvData.OptionsPrint.ExpandAllDetails = false;
        //     grvData.OptionsPrint.PrintDetails = true;

        //     grvData.OptionsPrint.UsePrintStyles = true;
        //     try
        //     { grvData.ExportToExcelOld(filepath); }
        //     catch
        //     { grvData.ExportToXls(filepath); }

        //     System.Diagnostics.ProcessStartInfo startInfo =
        //          new System.Diagnostics.ProcessStartInfo("Excel.exe", String.Format("/r \"{0}\"", filepath));

        //     System.Diagnostics.Process.Start(startInfo);
        // }

        // public static void OpenFile(string fileName)
        // {
        //     if (MessageBox.Show("Do you want to open file?", "Export to excel... ", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        //     {
        //         try
        //         {
        //             System.Diagnostics.Process process = new System.Diagnostics.Process();
        //             process.StartInfo.FileName = fileName;
        //             process.StartInfo.Verb = "Open";
        //             process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
        //             process.Start();
        //         }
        //         catch
        //         {
        //             MessageBox.Show("File not found.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        //         }
        //     }

        // }

        // public static bool LockFolio(int FolioID, bool Status)
        // {
        //     ProcessTransactions pt = new ProcessTransactions();
        //     try
        //     {
        //         if (Permissions.CheckExistsValue("cash_Manager_Billing_LockFolio") == true)
        //         {
        //             pt.OpenConnection();
        //             pt.BeginTransaction();
        //             pt.UpdateCommand("Update Folio set Status=" + Convert.ToInt32(Status) + " Where ID=" + FolioID);
        //             pt.CommitTransaction();
        //             pt.CloseConnection();
        //             return true;
        //         }
        //         else
        //             return false;
        //     }
        //     catch (Exception ex)
        //     {
        //         pt.CloseConnection();
        //         MessageBox.Show("#108 :" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //         return false;
        //     }
        // }

        // public static bool LockFolioByReservation(int ReservationID, bool Status)
        // {
        //     ProcessTransactions pt = new ProcessTransactions();
        //     try
        //     {
        //         if (Permissions.CheckExistsValue("cash_Manager_Billing_LockFolio") == true)
        //         {
        //             pt.OpenConnection();
        //             pt.BeginTransaction();
        //             pt.UpdateCommand("Update Folio set Status=" + Convert.ToInt32(Status) + " Where ReservationID=" + ReservationID);
        //             pt.CommitTransaction();
        //             pt.CloseConnection();
        //             return true;
        //         }
        //         else
        //             return false;
        //     }
        //     catch (Exception ex)
        //     {
        //         pt.CloseConnection();
        //         MessageBox.Show("ERROR :" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //         return false;
        //     }
        // }

        public static bool LockFolioByReservation(ProcessTransactions pt, int ReservationID, bool Status, ref string _Message)
        {
            try
            {
                pt.UpdateCommand("Update Folio set Status=" + Convert.ToInt32(Status) + " Where ReservationID=" + ReservationID);
                return true;
            }
            catch (Exception ex)
            {
                _Message = "Lock-Unlock :" + ex.Message;
                return false;
            }
        }

        // public static bool LockFolio(FolioModel mF, bool Status)
        // {
        //     ProcessTransactions pt = new ProcessTransactions();
        //     try
        //     {
        //         if (Permissions.CheckExistsValue("cash_Manager_Billing_LockFolio") == true)
        //         {
        //             mF.Status = Status;
        //             FolioBO.Instance.Update(mF);
        //             return true;
        //         }
        //         else
        //         {
        //             return false;
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         MessageBox.Show("#108 :" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //         return false;
        //     }
        // }

        // public static bool UnLockFolio(FolioModel mF, bool Status)
        // {
        //     ProcessTransactions pt = new ProcessTransactions();
        //     try
        //     {
        //         if (Permissions.CheckExistsValue("cash_Manager_Billing_UnLockFolio") == true)
        //         {
        //             mF.Status = Status;
        //             FolioBO.Instance.Update(mF);
        //             return true;
        //         }
        //         else
        //         {
        //             return false;
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         MessageBox.Show("#108 :" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //         return false;
        //     }
        // }


        // public static bool CheckCashier()
        // {
        //     if (Global.CashierNo.Length == 0)
        //     {
        //         frmCashierLogin frm = new frmCashierLogin();
        //         frm.ShowDialog();
        //         return frm.IsProcess;
        //     }
        //     else
        //         return true;
        // }

        /// <summary>
        /// Hàm chuyển khoản thanh toán từ FO -> AR Trans
        /// </summary>       
        /// <returns></returns>
        // public static bool TranferAR_CityLedger(DateTime _SysDate, DateTime _BusinessDate, string _ArNo, ProcessTransactions pt, int FolioID, string _TransCode, string _Description,
        //     decimal _Amount, string _CurrencyID, string _Ref, string _Supp, ref string _Message)
        // {
        //     try
        //     {
        //         //Chuyển khoản thanh toán sang công nợ
        //         DataTable dtf = TextUtils.Select("Select ID, AccountNo From ARAccountReceivable with (nolock) Where AccountNo = '" + _ArNo + "'");
        //         if (dtf.Rows.Count > 0)
        //         {
        //             int _ARTransID = 0;
        //             DataTable dta = TextUtils.Select("Select ID From ARAccountReceivableTrans with (nolock) Where AccountReceivableID = " + TextUtils.ToInt(dtf.Rows[0]["ID"]) + "  " +
        //                                             "And FolioID =" + FolioID + " And CurrencyID ='" + _CurrencyID + "' " +
        //                                             "AND DATEDIFF(DAY, TransactionDate,'" + _BusinessDate.ToString("yyyy/MM/dd") + "') = 0");
        //             if (dta.Rows.Count == 0)
        //             {
        //                 //Insert
        //                 ARAccountReceivableTransModel model = new()
        //                 {
        //                     AccountReceivableID = Convert.ToInt32(dtf.Rows[0]["ID"]),
        //                     AccountNo = dtf.Rows[0]["AccountNo"].ToString(),
        //                     FolioID = FolioID,
        //                     TransactionDate = _BusinessDate,
        //                     TransactionCode = _TransCode,
        //                     Description = _Description,
        //                     Amount = _Amount,
        //                     Paid = 0,
        //                     Balance = _Amount,
        //                     CurrencyID = _CurrencyID,
        //                     Supplement = _Supp,
        //                     Reference = _Ref,
        //                     IsTransferred = false,
        //                     IsTranferFO = true,
        //                     IsAdjusted = false,
        //                     IsPrinted = false,
        //                     CheckedOutDate = _BusinessDate
        //                 };
        //                 model.CreatedBy = model.UpdatedBy = Global.UserName;
        //                 model.CreatedDate = model.UpdatedDate = _SysDate;
        //                 model.IsActive = true;
        //                 _ARTransID = TextUtils.ToInt(pt.Insert(model));
        //             }
        //             else
        //             {
        //                 //Update
        //                 ARAccountReceivableTransModel model = (ARAccountReceivableTransModel)pt.FindByPK("ARAccountReceivableTrans", TextUtils.ToInt(dta.Rows[0]["ID"]));
        //                 if (model != null)
        //                 {
        //                     _ARTransID = model.ID;
        //                     model.Amount = model.Amount + _Amount;
        //                     model.Balance = model.Amount + model.Paid;
        //                     pt.Update(model);
        //                 }
        //             }
        //             //Update dòng giao dịch đã chuyển sang AR
        //             if (_ARTransID > 0)
        //                 pt.UpdateCommand("UPDATE dbo.FolioDetail WITH (ROWLOCK) SET ARTransID = " + _ARTransID + ", OriginARNo = '" + _ArNo + "' WHERE FolioID = " + FolioID + " AND ISNULL(ARTransID,0) = 0");
        //         }
        //         return true;
        //     }
        //     catch (Exception ex)
        //     {
        //         _Message = ex.Message;
        //         return false;
        //     }
        // }

        // public static bool _PaymentCheck(string _PaymentCode)
        // {
        //     bool isok = true;
        //     //Get Code deposit
        //     DataTable dt = TextUtils.Select("SELECT TOP 1 KeyValue FROM dbo.ConfigSystem with (nolock) WHERE KeyName = 'TRANSCODE_AR' ");
        //     string _code = "";
        //     if (dt.Rows.Count > 0)
        //     {
        //         _code = dt.Rows[0]["KeyValue"].ToString().Trim();
        //         if (_code != "")
        //         {
        //             string[] _split = _code.Split(',');
        //             for (int i = 0; i < _split.Length; i++)
        //             {
        //                 if (_split[i].ToString() == _PaymentCode)
        //                 {
        //                     isok = false;
        //                     break;
        //                 }
        //             }
        //         }
        //     }
        //     return isok;
        // }


        // static int _MAX_INVOICE_LEN = 5;
        // static private string GetStringByPrefix(string _Prefix, int _Length)
        // {
        //     return Convert.ToString("00000000000000000000000000000000000000000").Substring(0, _Length);
        // }
        // public static string ToTrimCrystal(string str)
        // {
        //     return str.Replace(System.Environment.NewLine, "; ").Replace("\n", "; ").Replace('"', ' ');
        // }
        // static public string GenerateInvoice(string _Prefix)
        // {
        //     try
        //     {
        //         if (_Prefix.Length == 0) { return ""; }
        //         string _SQL_GEN_INVOICE_NEW = "SELECT TOP 1 a.VoucherCode FROM PrintVoucherHistory a with (nolock) WHERE a.VoucherCode LIKE '%" + _Prefix + "%' ORDER BY a.VoucherCode DESC";
        //         // Lấy ra thông tin để so sánh.
        //         DataTable source = TextUtils.Select(string.Format(_SQL_GEN_INVOICE_NEW, _Prefix.Trim().Length + _MAX_INVOICE_LEN, _Prefix.Replace("_", "[_]")));
        //         // Nếu chưa tồn tại.
        //         if (source.Rows.Count == 0) { return _Prefix + GetStringByPrefix("0", _MAX_INVOICE_LEN - 1) + "1"; }
        //         else
        //         {
        //             string Value = "";
        //             try
        //             { Value = Convert.ToString(Convert.ToInt32(source.Rows[0][0].ToString().Substring(_Prefix.Length, source.Rows[0][0].ToString().Length - _Prefix.Length)) + 1); }
        //             catch
        //             { Value = ""; }

        //             return _Prefix + GetStringByPrefix("0", _MAX_INVOICE_LEN - Value.ToString().Length) + Value.ToString();
        //         }
        //     }
        //     catch (System.Exception ex)
        //     {
        //         MessageBox.Show("[Generate invoice]> " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //         return "";
        //     }
        // }
        // static public void PrintReceipt(int FolioDetailID, bool isPrintToWord, bool Preview, bool _IsA4)
        // {
        //     try
        //     {
        //         #region Get transaction info

        //         FolioDetailModel mDI = (FolioDetailModel)FolioDetailBO.Instance.FindByPK(FolioDetailID);
        //         string sql = "SELECT Amount as [AmountIncludedTax] FROM dbo.FolioDetail with (nolock) WHERE ID='{0}'";
        //         decimal amount = Convert.ToDecimal(TextUtils.Select(string.Format(sql, mDI.ID)).Rows[0][0]);

        //         #endregion

        //         #region Process print Payment order

        //         #endregion

        //         #region Get contract-folio info

        //         DataTable dtProfile = TextUtils.Select("SELECT top 1 b.Account, c.RoomNo, b.* FROM dbo.Reservation a with (nolock) JOIN dbo.[Profile] b with (nolock) ON a.ProfileIndividualID=b.ID JOIN Room c with (nolock) ON a.RoomID = c.ID WHERE a.ID=" + mDI.ReservationID);

        //         DataTable dtLicense = TextUtils.Select("select top 1 * from License with (nolock)");
        //         DataTable dtMapppingAccount = TextUtils.Select("SELECT TOP 1 a.* from TransactionMappingZone a with (nolock) JOIN " +
        //                                                        "dbo.FolioDetail b with (nolock) ON a.TransactionsCode=b.TransactionCode JOIN  " +
        //                                                        "dbo.Room c ON a.ZoneID=c.ZoneID AND b.RoomID=c.ID " +
        //                                                        "WHERE b.ID = " + FolioDetailID + " ");
        //         DataTable dtCode = TextUtils.Select("SELECT * FROM PrintReceiptByZone with (nolock) WHERE TransactionCode = '" + mDI.TransactionCode + "'");
        //         string Ma_PT = "XXX"; string Ten_PT = "PHIẾU THU";
        //         string Ma_PC = "XXX"; string Ten_PC = "PHIẾU CHI";
        //         if (dtCode.Rows.Count > 0)
        //         {
        //             Ma_PT = dtCode.Rows[0]["Ma_PT"].ToString();
        //             Ten_PT = dtCode.Rows[0]["Ten_PT"].ToString();
        //             Ma_PC = dtCode.Rows[0]["Ma_PC"].ToString();
        //             Ten_PC = dtCode.Rows[0]["Ten_PC"].ToString();
        //         }
        //         #endregion

        //         #region Get profile info
        //         ProfileModel mP;
        //         if (dtProfile.Rows.Count > 0)
        //         {
        //             mP = (ProfileModel)ProfileBO.Instance.FindByPK(TextUtils.ToInt(dtProfile.Rows[0]["ID"]));
        //         }
        //         #endregion

        //         if (isPrintToWord)
        //         {
        //             #region Export to word
        //             //Print_PaymentRefund(mFD.TransactionDate,
        //             //    mP == null ? "" : mP.FullName,
        //             //    mP == null ? "" : mP.Address,
        //             //    mFD.CustomerName,
        //             //    mFD.CustomerAddress,
        //             //    mFD.CashierName,
        //             //    mFD.Description,
        //             //    amount,//mFD.AmountIncludedTax,
        //             //    "VND",
        //             //    mFD.Reference,
        //             //    mFD.AccountDebit,
        //             //    mFD.AccountCredit,
        //             //    dtLicense.Rows.Count == 0 ? "" : dtLicense.Rows[0][0].ToString(),
        //             //    dtLicense.Rows.Count == 0 ? "" : dtLicense.Rows[0]["Address"].ToString(),
        //             //    dtLicense.Rows.Count == 0 ? "" : dtLicense.Rows[0]["TaxCode"].ToString(),
        //             //    dtLicense.Rows.Count == 0 ? "" : dtLicense.Rows[0]["AccountantGeneralName"].ToString(),
        //             //    dtLicense.Rows.Count == 0 ? "" : dtLicense.Rows[0]["Treasurer"].ToString(),
        //             //    dtLicense.Rows.Count == 0 ? "" : dtLicense.Rows[0]["GeneralDirector"].ToString(),
        //             //    dtLicense.Rows.Count == 0 ? "" : dtLicense.Rows[0]["Logo"].ToString()
        //             //    );
        //             #endregion
        //         }
        //         else
        //         {
        //             #region Print Crystal
        //             string[] FormulaFields = new string[19];
        //             string[] FormulaFieldsValue = new string[19];
        //             FormulaFields[0] = "Company"; FormulaFieldsValue[0] = dtLicense.Rows.Count == 0 ? "" : dtLicense.Rows[0]["Name"].ToString();
        //             FormulaFields[1] = "Address"; FormulaFieldsValue[1] = dtLicense.Rows.Count == 0 ? "" : dtLicense.Rows[0]["Address"].ToString();
        //             FormulaFields[2] = "TaxCode"; FormulaFieldsValue[2] = dtLicense.Rows.Count == 0 ? "" : dtLicense.Rows[0]["TaxCode"].ToString();
        //             FormulaFields[3] = "txtLogo"; FormulaFieldsValue[3] = dtLicense.Rows.Count == 0 ? "" : Application.StartupPath + @"\Pictures\" + dtLicense.Rows[0]["Logo"].ToString();
        //             string x = Application.StartupPath + @"\Pictures\" + dtLicense.Rows[0]["Logo"].ToString();
        //             FormulaFields[4] = "txtGeneralDirector"; FormulaFieldsValue[4] = dtLicense.Rows.Count == 0 ? "" : dtLicense.Rows[0]["GeneralDirector"].ToString();
        //             FormulaFields[5] = "txtAccountantGeneralName"; FormulaFieldsValue[5] = dtLicense.Rows.Count == 0 ? "" : dtLicense.Rows[0]["AccountantGeneralName"].ToString();
        //             FormulaFields[6] = "txtTreasurer"; FormulaFieldsValue[6] = dtLicense.Rows.Count == 0 ? "" : dtLicense.Rows[0]["Treasurer"].ToString();
        //             FormulaFields[7] = "txtCustomerName"; FormulaFieldsValue[7] = dtProfile.Rows.Count == 0 ? "" : ToTrimCrystal(dtProfile.Rows[0]["Account"].ToString());
        //             FormulaFields[8] = "txtCashierName"; FormulaFieldsValue[8] = ToTrimCrystal(Global.FullName);
        //             if (mDI.Amount < 0)
        //             {
        //                 FormulaFields[9] = "txtHeader"; FormulaFieldsValue[9] = Ten_PT;
        //             }
        //             else
        //             {
        //                 FormulaFields[9] = "txtHeader"; FormulaFieldsValue[9] = Ten_PC;
        //             }
        //             FormulaFields[10] = "txtTransactionDate"; FormulaFieldsValue[10] = "Ngày " + mDI.TransactionDate.Day + " Tháng " + mDI.TransactionDate.Month + " Năm " + mDI.TransactionDate.Year;

        //             DataTable dtVoucherCode = TextUtils.Select("select top 1 VoucherCode from PrintVoucherHistory with (nolock) where FolioDetailID = " + FolioDetailID + "");
        //             string _VoucherCode = "";
        //             if (dtVoucherCode.Rows.Count == 0)
        //             {
        //                 if (mDI.Amount < 0)
        //                     _VoucherCode = GenerateInvoice(Ma_PT);
        //                 else
        //                     _VoucherCode = GenerateInvoice(Ma_PC);
        //             }
        //             else
        //             {
        //                 _VoucherCode = dtVoucherCode.Rows[0]["VoucherCode"].ToString().Trim();
        //             }
        //             FormulaFields[11] = "txtReferenceNo"; FormulaFieldsValue[11] = _VoucherCode == "" ? mDI.Reference : _VoucherCode;
        //             FormulaFields[12] = "txtAccountDebit"; FormulaFieldsValue[12] = dtMapppingAccount.Rows.Count == 0 ? "" : dtMapppingAccount.Rows[0]["AccountDebit"].ToString();
        //             FormulaFields[13] = "txtAccountCredit"; FormulaFieldsValue[13] = dtMapppingAccount.Rows.Count == 0 ? "" : dtMapppingAccount.Rows[0]["AccountCredit"].ToString();
        //             FormulaFields[14] = "txtFullName"; FormulaFieldsValue[14] = dtProfile.Rows.Count == 0 ? "" : ToTrimCrystal(dtProfile.Rows[0]["Account"].ToString()) + "-" + dtProfile.Rows[0]["RoomNo"].ToString();
        //             FormulaFields[15] = "txtAddress"; FormulaFieldsValue[15] = dtProfile.Rows.Count == 0 ? "" : ToTrimCrystal(dtProfile.Rows[0]["Address"].ToString());
        //             FormulaFields[16] = "txtDescription"; FormulaFieldsValue[16] = ToTrimCrystal(mDI.Reference);
        //             FormulaFields[17] = "txtAmount"; FormulaFieldsValue[17] = Convert.ToDecimal(amount > 0 ? amount : -1 * amount).ToString("###,###,###");
        //             string tt = TextUtils.NumericToString(amount > 0 ? amount : -1 * amount).Trim();
        //             FormulaFields[18] = "txtAmountToWord"; FormulaFieldsValue[18] = tt.Substring(0, 1).ToUpper() + tt.Substring(1, tt.Length - 1);

        //             if (Preview == true)
        //             {
        //                 frmPrintReport frm = null;
        //                 if (_IsA4 == true)
        //                     frm = new frmPrintReport(CrystalDecisions.Shared.PaperSize.PaperA4);
        //                 else
        //                     frm = new frmPrintReport(CrystalDecisions.Shared.PaperSize.PaperA5);

        //                 frm.Source = null;
        //                 if (mDI.Amount < 0)
        //                 {
        //                     frm.ReportName = "rpt_Reception_Vourcher.rpt";
        //                     if (_IsA4 == true)
        //                         frm.ReportName = "rpt_Reception_Vourcher_A4.rpt";
        //                 }
        //                 else
        //                 {
        //                     frm.ReportName = "rpt_Reception_Vourcher_PC.rpt";
        //                     if (_IsA4 == true)
        //                         frm.ReportName = "rpt_Reception_Vourcher_PC_A4.rpt";
        //                 }
        //                 frm.FormulaFields = FormulaFields;
        //                 frm.FormulaFieldsValue = FormulaFieldsValue;
        //                 frm.NumberOfCopy = 3;
        //                 frm.BindData();
        //                 frm.Show();
        //             }
        //             else
        //             {
        //                 frmPrintReport frm = new frmPrintReport(CrystalDecisions.Shared.PaperSize.PaperA5);
        //                 frm.Source = null;
        //                 //frm.ReportName = "rpt_Reception_Vourcher.rpt";
        //                 if (mDI.Amount < 0)
        //                     frm.ReportName = "rpt_Reception_Vourcher.rpt";
        //                 else
        //                     frm.ReportName = "rpt_Reception_Vourcher_PC.rpt";
        //                 frm.FormulaFields = FormulaFields;
        //                 frm.FormulaFieldsValue = FormulaFieldsValue;
        //                 frm.BindData();
        //                 PrintDialog PrintDialog1 = new PrintDialog();
        //                 // PrintDialog1.PrinterSettings.Copies = 3;
        //                 DialogResult result = PrintDialog1.ShowDialog();
        //                 if (result != DialogResult.OK)
        //                     return;
        //                 frm.NumberOfCopy = PrintDialog1.PrinterSettings.Copies;
        //                 string Printname = PrintDialog1.PrinterSettings.PrinterName;
        //                 frm.PrintReport(Printname, false);

        //                 #region Ghi lịch sử in

        //                 DataTable dtVoucher = TextUtils.Select("select ID,PrintCount from PrintVoucherHistory where FolioDetailID = " + FolioDetailID + "");
        //                 if (dtVoucher.Rows.Count == 0)
        //                 {
        //                     PrintVoucherHistoryModel _pmodel = new PrintVoucherHistoryModel();
        //                     _pmodel.FolioDetailID = FolioDetailID;
        //                     _pmodel.DepositIncurringID = 0;
        //                     _pmodel.PrintedBy = Global.UserName;
        //                     _pmodel.PrintedDate = TextUtils.GetSystemDate();
        //                     _pmodel.VoucherCode = _VoucherCode;
        //                     _pmodel.PrintCount = 1;
        //                     PrintVoucherHistoryBO.Instance.Insert(_pmodel);
        //                 }
        //                 else
        //                 {
        //                     PrintVoucherHistoryModel _pmodel = (PrintVoucherHistoryModel)PrintVoucherHistoryBO.Instance.FindByPK(TextUtils.ToInt(dtVoucher.Rows[0]["ID"]));
        //                     _pmodel.PrintedBy = Global.UserName;
        //                     _pmodel.PrintedDate = TextUtils.GetSystemDate();
        //                     _pmodel.PrintCount = TextUtils.ToInt(dtVoucher.Rows[0]["PrintCount"]) + 1;
        //                     PrintVoucherHistoryBO.Instance.Update(_pmodel);
        //                 }
        //                 #endregion

        //                 MessageBox.Show("Print Succeed", TextUtils.Caption, MessageBoxButtons.OK);
        //             }
        //             #endregion
        //         }

        //     }
        //     catch (System.Exception ex)
        //     {
        //         MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //         return;
        //     }
        // }

        // #endregion

        // #region Hàm xuất Excel cũ

        // private void exportDataTableToExcel(DataTable dt, string filePath)
        // {

        //     //  Excel file Path   
        //     string myFile = filePath;
        //     System.Data.DataRow _dr = default(System.Data.DataRow);
        //     int colIndex = 0;
        //     int rowIndex = 0;
        //     // Open the file and write the headers    
        //     System.IO.StreamWriter fs = new System.IO.StreamWriter(myFile, false);
        //     fs.WriteLine("");
        //     fs.WriteLine("");
        //     fs.WriteLine("");
        //     // Create the styles for the worksheet    
        //     fs.WriteLine(" ");
        //     // Style for the column headers    
        //     fs.WriteLine(" ");
        //     fs.WriteLine(" ");
        //     fs.WriteLine(" ");
        //     fs.WriteLine(" ");
        //     fs.WriteLine(" ");
        //     // Style for the column information    
        //     fs.WriteLine(" ");
        //     fs.WriteLine(" ");
        //     fs.WriteLine(" ");
        //     fs.WriteLine(" ");
        //     // Write the worksheet contents    
        //     fs.WriteLine("");
        //     fs.WriteLine(" ");
        //     fs.WriteLine(" ");
        //     colIndex = 0;
        //     //Write the column names        
        //     foreach (DataColumn dc in dt.Columns)
        //     {
        //         colIndex = colIndex + 1;
        //         fs.WriteLine(string.Format(" " + "{0}", dc.ColumnName));
        //     }
        //     fs.WriteLine(" ");
        //     // Write contents for each cell          
        //     string cellText = null;
        //     foreach (DataRow dr in dt.Rows)
        //     {
        //         rowIndex = rowIndex + 1;
        //         colIndex = 0;
        //         foreach (DataColumn dc in dt.Columns)
        //         {
        //             colIndex = colIndex + 1;
        //             cellText = dr[dc.ColumnName].ToString();
        //             // Check for null cell and change it to empty to. avoid error            
        //             //if (cellText == Microsoft.Office.Interop.ExcelConstants.vbNullString)
        //             //    cellText = "";             
        //             //fs.WriteLine(string.Format(" " + "{0}", cellText.ToString));
        //             fs.WriteLine(string.Format(" " + "{0}", cellText.ToString()));
        //         }
        //     }

        //     // Close up the document    
        //     fs.WriteLine(" ");
        //     fs.WriteLine("");
        //     fs.WriteLine("");
        //     fs.Close();
        // }
        // /// <summary>
        // ///1) include COM reference to Microsoft Excel Object library
        // /// add namespace...
        // /// 2) using Excel = Microsoft.Office.Interop.Excel;
        // /// </summary>
        // /// <param name="dt"></param>
        // public static void Excel_FromDataTable(DataTable dt)
        // {
        //     // Create an Excel object and add workbook...
        //     Microsoft.Office.Interop.Excel.ApplicationClass excel = new Microsoft.Office.Interop.Excel.ApplicationClass();
        //     Microsoft.Office.Interop.Excel.Workbook workbook = excel.Application.Workbooks.Add(true);
        //     // true for object template???

        //     // Add column headings...
        //     int iCol = 0;
        //     foreach (DataColumn c in dt.Columns)
        //     {
        //         iCol++;
        //         excel.Cells[1, iCol] = c.ColumnName;
        //     }
        //     // for each row of data...
        //     int iRow = 0;
        //     foreach (DataRow r in dt.Rows)
        //     {
        //         iRow++;
        //         // add each row's cell data...
        //         iCol = 0;
        //         foreach (DataColumn c in dt.Columns)
        //         {
        //             iCol++;
        //             excel.Cells[iRow + 1, iCol] = r[c.ColumnName];
        //         }
        //     }
        //     // Global missing reference for objects we are not defining...
        //     object missing = System.Reflection.Missing.Value;

        //     // If wanting to Save the workbook...
        //     string fileName1 = ClassReservation.ShowSaveFileDialog("Microsoft Excel Document", "Microsoft Excel|*.xls", "SC");

        //     workbook.SaveAs(fileName1,
        //         Microsoft.Office.Interop.Excel.XlFileFormat.xlXMLSpreadsheet, missing, missing,
        //         false, false, Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlNoChange,
        //         missing, missing, missing, missing, missing);

        //     // If wanting to make Excel visible and activate the worksheet...
        //     excel.Visible = true;
        //     Microsoft.Office.Interop.Excel.Worksheet worksheet = (Microsoft.Office.Interop.Excel.Worksheet)excel.ActiveSheet;
        //     ((Microsoft.Office.Interop.Excel._Worksheet)worksheet).Activate();
        //     // If wanting excel to shutdown...
        //     //((Microsoft.Office.Interop.Excel._Application)excel).Quit();


        // }

        // #endregion

        // public static bool GenerateTransWithOutTaxInclude(bool AutoPosting, bool TaxInclude, DateTime _SysDate, DateTime _BusinessDate, int _ProID, string _ProCode, string _ConfirmNo, int _RsvID,
        //                                int _RmID, int _RmTypeID, string _RmTypeCode, int _ProfileID, string _AccountName, int _Win, string _TransCode, string _TransDesc, string _ArCode, string _Ref,
        //                                string _Supp, decimal _Amount, int _Quan, string _CurrencyID, string _CurrencyLocal,
        //                                ref decimal _AmountReturn, ref decimal _AmountLocalReturn, ref string _TransNoReturn, ref string _Message, bool _IsPostedAR)
        // {
        //     ProcessTransactions pt = new ProcessTransactions();
        //     try
        //     {
        //         #region Mở kết nối và bắt đầu 1 Transaction
        //         pt.OpenConnection();
        //         pt.BeginTransaction();
        //         #endregion

        //         #region Lấy ra thông tin của FolioID

        //         int _RsvID_Return = 0;
        //         int FolioID = GetOrCreateFolioID(_SysDate, _BusinessDate, _ConfirmNo, _RsvID, _Win, _ProfileID, _AccountName, ref _RsvID_Return, pt, ref _Message);
        //         _RsvID = _RsvID_Return;

        //         #endregion

        //         if (FolioID > 0)
        //         {
        //             #region Khai báo Model

        //             FolioDetailModel mFD_Detail = new FolioDetailModel();
        //             FolioDetailModel mFD_Master = new FolioDetailModel();

        //             #endregion

        //             #region Lấy ra thông tin của TransCode
        //             TransactionsModel mT = new TransactionsModel();
        //             ArrayList lst = pt.FindByAttribute("Transactions", "Code", _TransCode);
        //             if (lst.Count > 0)
        //             {
        //                 mT = (TransactionsModel)lst[0];
        //             }
        //             else
        //             {
        //                 _Message = String.Format("Cant not find Adjustment Code {0} in Transactions", _TransCode);
        //                 return false;
        //             }

        //             #endregion

        //             #region Gán giá trị cho các biến Static
        //             //CSS(06.10.11)
        //             if (_IsPostedAR == true)
        //             {
        //                 mFD_Detail.IsPostedAR = true;
        //                 mFD_Master.IsPostedAR = true;
        //             }
        //             else
        //             {
        //                 mFD_Detail.IsPostedAR = false;
        //                 mFD_Master.IsPostedAR = false;
        //             }


        //             if (_ProID != 0)
        //             {
        //                 mFD_Detail.ProfitCenterID = _ProID;
        //                 mFD_Detail.ProfitCenterCode = _ProCode;
        //             }
        //             else
        //             {
        //                 mFD_Detail.ProfitCenterID = ProfitCenterID;
        //                 mFD_Detail.ProfitCenterCode = ProfitCenterCode;
        //             }
        //             mFD_Detail.RoomID = _RmID;
        //             mFD_Detail.RoomTypeID = _RmTypeID;
        //             mFD_Detail.RoomType = _RmTypeCode;
        //             mFD_Detail.Status = false;

        //             mFD_Detail.CurrencyID = _CurrencyID;
        //             mFD_Detail.CurrencyMaster = _CurrencyLocal;

        //             mFD_Detail.ReservationID = _RsvID;
        //             mFD_Detail.OriginReservationID = _RsvID;

        //             mFD_Detail.FolioID = FolioID;
        //             mFD_Detail.OriginFolioID = mFD_Detail.FolioID;

        //             mFD_Detail.Quantity = _Quan;
        //             mFD_Detail.TransactionDate = _BusinessDate;
        //             mFD_Detail.PackageID = 0;

        //             mFD_Detail.UserInsertID = Global.UserID;
        //             mFD_Detail.UserUpdateID = Global.UserID;
        //             mFD_Detail.CreateDate = _SysDate;
        //             mFD_Detail.UpdateDate = _SysDate;
        //             if (AutoPosting == true)
        //             {
        //                 mFD_Detail.UserID = 0;
        //                 mFD_Detail.UserName = "$$";
        //                 mFD_Detail.CashierNo = "";
        //                 mFD_Detail.ShiftID = 0;
        //             }
        //             else
        //             {
        //                 mFD_Detail.UserID = Global.UserID;
        //                 mFD_Detail.UserName = Global.UserName;
        //                 mFD_Detail.CashierNo = Global.UserName;
        //                 mFD_Detail.ShiftID = Global.ShiftID;
        //             }
        //             if (_ProID != 0)
        //             {
        //                 mFD_Master.ProfitCenterID = _ProID;
        //                 mFD_Master.ProfitCenterCode = _ProCode;
        //             }
        //             else
        //             {
        //                 mFD_Master.ProfitCenterID = ProfitCenterID;
        //                 mFD_Master.ProfitCenterCode = ProfitCenterCode;
        //             }

        //             mFD_Master.Status = false;

        //             mFD_Master.CurrencyID = _CurrencyID;
        //             mFD_Master.CurrencyMaster = _CurrencyLocal;

        //             mFD_Master.RoomID = _RmID;
        //             mFD_Master.RoomTypeID = _RmTypeID;
        //             mFD_Master.RoomType = _RmTypeCode;

        //             mFD_Master.ReservationID = _RsvID;
        //             mFD_Master.OriginReservationID = _RsvID;

        //             mFD_Master.FolioID = FolioID;
        //             mFD_Master.OriginFolioID = mFD_Master.FolioID;

        //             mFD_Master.Quantity = _Quan;
        //             mFD_Master.TransactionDate = _BusinessDate;
        //             mFD_Master.PackageID = 0;

        //             mFD_Master.UserInsertID = Global.UserID;
        //             mFD_Master.UserUpdateID = Global.UserID;
        //             mFD_Master.CreateDate = _SysDate;
        //             mFD_Master.UpdateDate = _SysDate;
        //             if (AutoPosting == true)
        //             {
        //                 mFD_Master.UserID = 0;
        //                 mFD_Master.UserName = "$$";
        //                 mFD_Master.CashierNo = "";
        //                 mFD_Master.ShiftID = 0;
        //             }
        //             else
        //             {
        //                 mFD_Master.UserID = Global.UserID;
        //                 mFD_Master.UserName = Global.UserName;
        //                 mFD_Master.CashierNo = Global.UserName;
        //                 mFD_Master.ShiftID = Global.ShiftID;
        //             }
        //             #endregion

        //             #region Kiểm tra xem Transaction này có ở trong Generate ?
        //             ArrayList arr = pt.FindByAttribute("GenerateTransaction", "TransactionCode", _TransCode);
        //             #endregion

        //             #region Nếu chưa tồn tại trong Generate.
        //             if ((arr == null) || (arr.Count == 0))
        //             {

        //                 //Gán thông tin cho các propertie còn lại
        //                 mFD_Detail.IsSplit = false;
        //                 mFD_Detail.Reference = _Ref;
        //                 mFD_Detail.Supplement = _Supp;

        //                 mFD_Detail.TransactionGroupID = mT.TransactionGroupID;
        //                 mFD_Detail.TransactionSubgroupID = mT.TransactionSubGroupID;
        //                 mFD_Detail.GroupCode = mT.GroupCode;
        //                 mFD_Detail.SubgroupCode = mT.SubgroupCode;
        //                 mFD_Detail.GroupType = mT.GroupType;

        //                 mFD_Detail.ArticleCode = _ArCode;
        //                 mFD_Detail.TransactionCode = mT.Code;
        //                 if (_TransDesc.Length > 0)
        //                     mFD_Detail.Description = _TransDesc;//mT.Description;
        //                 else
        //                     mFD_Detail.Description = mT.Description;
        //                 mFD_Detail.Amount = _Amount;
        //                 mFD_Detail.AmountBeforeTax = _Amount;
        //                 mFD_Detail.Price = mFD_Detail.Amount / mFD_Detail.Quantity;

        //                 mFD_Detail.AmountMaster = TextUtils.ExchangeCurrency(_BusinessDate, _CurrencyID, _CurrencyLocal, _Amount);
        //                 mFD_Detail.AmountMasterBeforeTax = mFD_Detail.AmountMaster;

        //                 mFD_Detail.AmountGross = mFD_Detail.Amount;
        //                 mFD_Detail.AmountMasterGross = mFD_Detail.AmountMaster;

        //                 mFD_Detail.PostType = 1;
        //                 mFD_Detail.RowState = 1;

        //                 //Thực hiện Post
        //                 mFD_Detail.ID = (int)pt.Insert(mFD_Detail);

        //                 mFD_Detail.InvoiceNo = mFD_Detail.ID.ToString();
        //                 mFD_Detail.TransactionNo = mFD_Detail.ID.ToString();

        //                 pt.Update(mFD_Detail);
        //                 //Update số dư.
        //                 UpdateBalance(_RsvID, FolioID, pt, ref _Message);

        //                 // Ghi histoty
        //                 if (mFD_Detail.GroupType != 1)
        //                     ActionPosting.InsertHistory(pt, _SysDate, _BusinessDate, mFD_Detail.FolioID, mFD_Detail.FolioID, mFD_Detail.InvoiceNo, ActionPosting.HistoryType.Basic_Post,
        //                         ActionPosting.GetActionText(ActionPosting.HistoryType.Basic_Post, mFD_Detail.TransactionCode, mFD_Detail.Description),
        //                         Global.UserName, mFD_Detail.TransactionCode, mFD_Detail.Description, mFD_Detail.Amount, mFD_Detail.Supplement, "", "", "");
        //                 else
        //                     ActionPosting.InsertHistory(pt, _SysDate, _BusinessDate, mFD_Detail.FolioID, mFD_Detail.FolioID, mFD_Detail.InvoiceNo, ActionPosting.HistoryType.Payment,
        //                         ActionPosting.GetActionText(ActionPosting.HistoryType.Payment, mFD_Detail.TransactionCode, mFD_Detail.Description),
        //                         Global.UserName, mFD_Detail.TransactionCode, mFD_Detail.Description, mFD_Detail.Amount, mFD_Detail.Supplement, "", "", "");
        //                 //Trả về thông tin
        //                 _AmountReturn = mFD_Detail.Amount;
        //                 _AmountLocalReturn = mFD_Detail.AmountMaster;
        //                 _TransNoReturn = mFD_Detail.TransactionNo;
        //             }
        //             #endregion

        //             #region Nếu đã tồn tại trong Generate -> lấy ra và thực hiện
        //             else
        //             {
        //                 #region Khai báo biến
        //                 decimal s1 = 0, s2 = 0, s3 = 0;
        //                 decimal CurrentAmount = 0;
        //                 decimal BaseAmount = _Amount;
        //                 decimal Rate = 0;
        //                 GenerateTransactionModel mGT;
        //                 #endregion

        //                 #region Lấy ra thông tin của amount trước thuế
        //                 if (TaxInclude == true)
        //                     BaseAmount = GetAmount(arr, Convert.ToDecimal(BaseAmount));
        //                 #endregion

        //                 #region Insert dòng tổng

        //                 mFD_Master.IsSplit = true;
        //                 mFD_Master.Reference = _Ref;
        //                 mFD_Master.Supplement = _Supp;

        //                 mFD_Master.TransactionGroupID = mT.TransactionGroupID;
        //                 mFD_Master.TransactionSubgroupID = mT.TransactionSubGroupID;
        //                 mFD_Master.GroupCode = mT.GroupCode;
        //                 mFD_Master.SubgroupCode = mT.SubgroupCode;
        //                 mFD_Master.GroupType = mT.GroupType;

        //                 mFD_Master.ArticleCode = _ArCode;
        //                 mFD_Master.TransactionCode = mT.Code;
        //                 if (_TransDesc.Length > 0)
        //                     mFD_Master.Description = _TransDesc;//mT.Description;
        //                 else
        //                     mFD_Master.Description = mT.Description;

        //                 mFD_Master.Quantity = _Quan;

        //                 mFD_Master.Price = 0;
        //                 mFD_Master.Amount = 0;
        //                 mFD_Master.AmountMaster = 0;
        //                 mFD_Master.AmountBeforeTax = 0;
        //                 mFD_Master.AmountMasterBeforeTax = 0;

        //                 mFD_Master.PostType = 2;
        //                 mFD_Master.RowState = 1;

        //                 mFD_Master.ID = (int)pt.Insert(mFD_Master);

        //                 mFD_Master.InvoiceNo = mFD_Master.ID.ToString();
        //                 mFD_Master.TransactionNo = mFD_Master.ID.ToString();
        //                 #endregion

        //                 for (int j = 0; j < arr.Count; j++)
        //                 {
        //                     #region Đổ dữ liệu vào Model
        //                     mGT = (GenerateTransactionModel)arr[j];
        //                     #endregion

        //                     #region Lấy ra  CurrentAmount
        //                     if (mGT.Type == 0)
        //                     {
        //                         if (mGT.BaseAmount == 0)
        //                             CurrentAmount = (mGT.Percentage * BaseAmount) / 100;
        //                         else if (mGT.BaseAmount == 1)
        //                             CurrentAmount = (mGT.Percentage * s1) / 100;
        //                         else if (mGT.BaseAmount == 2)
        //                             CurrentAmount = (mGT.Percentage * s2) / 100;
        //                         else
        //                             CurrentAmount = (mGT.Percentage * s3) / 100;
        //                     }
        //                     else if (mGT.Type == 1)
        //                     {
        //                         CurrentAmount = mGT.Amount;
        //                     }
        //                     CurrentAmount = GetAmountFormat(CurrentAmount);
        //                     #endregion

        //                     #region Lấy dữ liệu vào s1,s2,s3
        //                     if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == false) && (mGT.Subtotal3 == false))
        //                     {
        //                         s1 = s1 + CurrentAmount;
        //                     }
        //                     else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == true) && (mGT.Subtotal3 == false))
        //                     {
        //                         s1 = s1 + CurrentAmount;
        //                         s2 = CurrentAmount;
        //                     }
        //                     else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == false) && (mGT.Subtotal3 == true))
        //                     {
        //                         s1 = s1 + CurrentAmount;
        //                         s3 = CurrentAmount;
        //                     }
        //                     else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == true) && (mGT.Subtotal3 == true))
        //                     {
        //                         s1 = s1 + CurrentAmount;
        //                         s2 = CurrentAmount;
        //                         s3 = CurrentAmount;
        //                     }
        //                     #endregion

        //                     #region Đổ dữ liệu vào model Model

        //                     mFD_Detail.IsSplit = false;
        //                     mFD_Detail.PostType = 2;
        //                     mFD_Detail.RowState = 2;

        //                     mFD_Detail.TransactionGroupID = mGT.TransactionGroupID;
        //                     mFD_Detail.TransactionSubgroupID = mGT.TransactionSubGroupID;
        //                     mFD_Detail.GroupCode = mGT.GroupCode;
        //                     mFD_Detail.SubgroupCode = mGT.SubgroupCode;
        //                     mFD_Detail.GroupType = mGT.GroupType;

        //                     mFD_Detail.TransactionCode = mGT.TransactionCodeDetail;
        //                     mFD_Detail.Description = mGT.Description;

        //                     if ((TaxInclude == true) && (j == arr.Count - 1))
        //                         mFD_Detail.Amount = _Amount - mFD_Master.Amount;
        //                     else
        //                         mFD_Detail.Amount = GetAmountFormat(CurrentAmount);
        //                     mFD_Detail.AmountBeforeTax = mFD_Detail.Amount;
        //                     mFD_Detail.Price = mFD_Detail.Amount / mFD_Detail.Quantity;
        //                     mFD_Detail.AmountGross = mFD_Detail.Amount;
        //                     if (j == 0)
        //                     {
        //                         // Tính ra tỉ giá nếu là dòng dầu
        //                         mFD_Detail.AmountMaster = TextUtils.ExchangeCurrency(_BusinessDate, _CurrencyID, _CurrencyLocal, mFD_Detail.Amount);
        //                         Rate = mFD_Detail.AmountMaster / mFD_Detail.Amount;
        //                         // Nếu là dòng đầu -> insert giá trước thuế.
        //                         mFD_Master.AmountBeforeTax = mFD_Detail.Amount;
        //                         mFD_Master.AmountMasterBeforeTax = mFD_Detail.AmountMaster;
        //                     }
        //                     else
        //                         mFD_Detail.AmountMaster = mFD_Detail.Amount * Rate;

        //                     mFD_Detail.AmountMasterBeforeTax = mFD_Detail.AmountMaster;
        //                     mFD_Detail.AmountMasterGross = mFD_Detail.AmountMaster;

        //                     #endregion

        //                     #region Insert Du lieu

        //                     mFD_Detail.InvoiceNo = mFD_Master.InvoiceNo;
        //                     mFD_Detail.TransactionNo = mFD_Master.TransactionNo;
        //                     mFD_Detail.ID = (int)pt.Insert(mFD_Detail);

        //                     mFD_Master.AmountMaster = mFD_Master.AmountMaster + mFD_Detail.AmountMaster;
        //                     mFD_Master.Amount = mFD_Master.Amount + mFD_Detail.Amount;

        //                     #endregion
        //                 }
        //                 // Tính giá Gross
        //                 mFD_Master.AmountGross = mFD_Master.Amount;
        //                 mFD_Master.AmountMasterGross = mFD_Master.AmountMaster;
        //                 // Tính giá Net nếu số tiền nhập vào là giá sau thuế
        //                 if (TaxInclude == true)
        //                 {
        //                     mFD_Master.Amount = _Amount;
        //                     mFD_Master.AmountMaster = _Amount * Rate;
        //                 }
        //                 mFD_Master.Price = mFD_Master.Amount / mFD_Master.Quantity;

        //                 pt.Update(mFD_Master);
        //                 //Update số dư.
        //                 UpdateBalance(_RsvID, FolioID, pt, ref _Message);

        //                 // Ghi histoty
        //                 if (mFD_Master.GroupType != 1)
        //                     ActionPosting.InsertHistory(pt, _SysDate, _BusinessDate, mFD_Master.FolioID, mFD_Master.FolioID, mFD_Master.InvoiceNo, ActionPosting.HistoryType.Gen_Post,
        //                         ActionPosting.GetActionText(ActionPosting.HistoryType.Gen_Post, mFD_Master.TransactionCode, mFD_Master.Description),
        //                         Global.UserName, mFD_Master.TransactionCode, mFD_Master.Description, mFD_Master.Amount, mFD_Master.Supplement, "", "", "");
        //                 else
        //                     ActionPosting.InsertHistory(pt, _SysDate, _BusinessDate, mFD_Master.FolioID, mFD_Master.FolioID, mFD_Master.InvoiceNo, ActionPosting.HistoryType.Payment,
        //                         ActionPosting.GetActionText(ActionPosting.HistoryType.Payment, mFD_Master.TransactionCode, mFD_Master.Description),
        //                         Global.UserName, mFD_Master.TransactionCode, mFD_Master.Description, mFD_Master.Amount, mFD_Master.Supplement, "", "", "");

        //                 //Trả về thông tin
        //                 _AmountReturn = mFD_Master.Amount;
        //                 _AmountLocalReturn = mFD_Master.AmountMaster;
        //                 _TransNoReturn = mFD_Master.TransactionNo;
        //             }
        //             #endregion

        //             #region Commit-Return
        //             pt.CommitTransaction();
        //             pt.CloseConnection();
        //             return true;
        //             #endregion
        //         }
        //         else if (FolioID == -1)
        //         {
        //             _Message = "Folio is locked.";
        //             return false;
        //         }
        //         else
        //         {
        //             return false;
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         pt.CloseConnection();
        //         _Message = ex.Message;
        //         return false;
        //     }
        // }
        #endregion

        //Ham XO Post tien online sang IPTV
        public static void IF_XO(string _RoomNo, string _date, string _time, string _Amount, string _Total, string _Curr,
                            string _refe, string _descrip, string _RsvID, string _GuestID, int _folioId = 0)
        {
            try
            {
                #region 1.Khai báo biến
                string _transCode = "";
                int _FolioID = _folioId;
                //string _datetime = "";
                string _des = "";
                #endregion

                #region 2.Process
                DataTable _dtC = TextUtils.Select("SELECT Desciption FROM ConfigSystem WHERE KeyName ='IF_IN' ");

                DataTable _dtF = null;
                if (_FolioID <= 0)
                {
                    _dtF = TextUtils.Select("SELECT ID FROM Folio WITH (NOLOCK) WHERE ReservationID = '" + _RsvID + "' AND FolioNo = 1 ");
                }
                //Not Exits
                if (_dtC.Rows.Count > 0)
                {
                    _transCode = _dtC.Rows[0][0].ToString();
                    if (_FolioID <= 0 && _dtF != null && _dtF.Rows.Count > 0)
                    {
                        _FolioID = TextUtils.ToInt(_dtF.Rows[0][0].ToString());
                    }
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

    }

}
