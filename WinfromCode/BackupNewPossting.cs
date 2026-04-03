using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using BaseBusiness.bc;
using BaseBusiness.BO;
using BaseBusiness.Contants;
using BaseBusiness.Model;
using BaseBusiness.util;

namespace Billing.Dto
{
    public class PostingInvoiceRequestDto
    {
        public bool AutoPosting { get; set; }
        public DateTime SysDate { get; set; }
        public DateTime BusinessDate { get; set; }
        public int ProID { get; set; }
        public string ProCode { get; set; } = string.Empty;
        public string ConfirmNo { get; set; } = string.Empty;
        public int RsvID { get; set; }
        public int RoomID { get; set; }

        // Bắt buộc thêm để giữ đúng logic cũ của IPTV:
        // WinForms cũ dùng mR.RoomNo, không phải RoomID
        public string RoomNo { get; set; } = string.Empty;

        public int ProfileID { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public int Win { get; set; }
        public string InvoiceCode { get; set; } = string.Empty;
        public string InvoiceDesc { get; set; } = string.Empty;
        public string InvoiceRef { get; set; } = string.Empty;
        public string InvoiceSupp { get; set; } = string.Empty;
        public string InvoiceNo { get; set; } = string.Empty;
        public string CurrencyLocal { get; set; } = string.Empty;
        public int UserID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int ShiftID { get; set; }

        public List<PostingInvoiceItemDto> Items { get; set; } = new List<PostingInvoiceItemDto>();
    }

    public class PostingInvoiceItemDto
    {
        public string TransCode { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public string ArCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool TaxInclude { get; set; }
        public int Quan { get; set; }
        public string CurrencyID { get; set; } = string.Empty;
        public int RoomTypeID { get; set; }
        public string RoomType { get; set; } = string.Empty;
        public string Ref { get; set; } = string.Empty;
        public string Supp { get; set; } = string.Empty;

        // Giữ lại để phục vụ phần IPTV tổng hợp như logic cũ
        public decimal AmountNetForIptv { get; set; }

        // Giữ nếu phía UI/web đang cần build Ref như code cũ
        public string ArticleDesc { get; set; } = string.Empty;
    }
}

namespace Billing.Services.Implements
{
    using Billing.Dto;

    public class PostingService
    {
        private static readonly int ProfitCenterID = 2;
        private static readonly string ProfitCenterCode = "0";

        /// <summary>
        /// Hàm post invoice chính, chuyển toàn bộ input detail từ mảng sang DTO
        /// </summary>
        public static bool PostingInvoice(PostingInvoiceRequestDto request, ref string message)
        {
            ProcessTransactions pt = new();

            try
            {
                OpenAndBegin(pt);

                int rsvIdReturn = 0;
                int rsvId = request.RsvID;

                int folioId = GetFolio(
                    pt,
                    request.SysDate,
                    request.BusinessDate,
                    request.ConfirmNo,
                    rsvId,
                    request.Win,
                    request.ProfileID,
                    request.AccountName,
                    ref rsvIdReturn,
                    ref message);

                rsvId = rsvIdReturn;

                if (folioId <= 0) return false;

                FolioDetailModel mFD_Group = new();
                FolioDetailModel mFD_Subgroup = new();
                FolioDetailModel mFD_Detail = new();
                decimal rate = 0;

                InitGroupModel(
                    mFD_Group,
                    request.AutoPosting,
                    request.ProID,
                    request.ProCode,
                    request.InvoiceNo,
                    request.CurrencyLocal,
                    rsvId,
                    request.RoomID,
                    folioId,
                    request.BusinessDate,
                    request.SysDate,
                    request.UserID,
                    request.UserName,
                    request.ShiftID);

                InitSubgroupModel(
                    mFD_Subgroup,
                    request.AutoPosting,
                    request.ProID,
                    request.ProCode,
                    request.InvoiceNo,
                    request.CurrencyLocal,
                    rsvId,
                    request.RoomID,
                    folioId,
                    request.BusinessDate,
                    request.SysDate,
                    request.UserID,
                    request.UserName,
                    request.ShiftID);

                InitDetailModel(
                    mFD_Detail,
                    request.AutoPosting,
                    request.ProID,
                    request.ProCode,
                    request.InvoiceNo,
                    request.CurrencyLocal,
                    rsvId,
                    request.RoomID,
                    folioId,
                    request.BusinessDate,
                    request.SysDate,
                    request.UserID,
                    request.UserName,
                    request.ShiftID);

                TransactionsModel mT_Group = GetTransaction(pt, request.InvoiceCode);

                InsertInvoiceGroup(
                    pt,
                    mFD_Group,
                    mT_Group,
                    request.InvoiceDesc,
                    request.InvoiceRef,
                    request.InvoiceSupp,
                    request.InvoiceNo,
                    request.RoomID);

                ProcessAllDetails(
                    pt,
                    mFD_Group,
                    mFD_Subgroup,
                    mFD_Detail,
                    request.Items,
                    request.CurrencyLocal,
                    request.BusinessDate,
                    ref rate);

                FinalizeAndCommit(
                    pt,
                    mFD_Group,
                    request.SysDate,
                    request.BusinessDate,
                    rsvId,
                    folioId,
                    ref message);

                return true;
            }
            catch (Exception ex)
            {
                pt.CloseConnection();
                message = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Hàm service/webform thay thế logic btnPostInvoice_Click
        /// Không phụ thuộc WinForms control
        /// </summary>
        public static ApiResponse PostInvoiceFromRequest(PostingInvoiceRequestDto request)
        {
            string message = string.Empty;

            try
            {
                if (request == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Request is null"
                    };
                }

                var validate = ValidatePostingInvoiceRequest(request);
                if (validate.Success == false)
                    return validate;

                // Giữ logic cũ: nếu có item thì currency local lấy theo item đầu tiên
                if (request.Items != null && request.Items.Count > 0)
                    request.CurrencyLocal = request.Items[0].CurrencyID;

                bool result = PostingInvoice(request, ref message);
                if (result == false)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "ERR :" + message
                    };
                }

                #region Post interface to IPTV
                decimal totalAmount = 0;
                foreach (var item in request.Items)
                {
                    totalAmount += item.AmountNetForIptv;
                }

                int rsvIdReturn = 0;
                string folioMessage = "";
                string totalUSD = "";
                string totalVND = "";

                int folioId = CasheringUtils.GetOrCreateFolioID(
                    TextUtils.GetSystemDate(),
                    TextUtils.GetBusinessDateTime(),
                    request.ConfirmNo,
                    request.RsvID,
                    request.Win,
                    request.ProfileID,
                    "",
                    ref rsvIdReturn,
                    ref folioMessage);

                DataTable dtt = TextUtils.Select(
                    "Select dbo.getBalanceOfFolio(" + folioId + ",'USD') as USD, dbo.getBalanceOfFolio(" + folioId + ",'VND') as VND ");

                if (dtt.Rows.Count > 0)
                {
                    totalUSD = dtt.Rows[0]["USD"].ToString();
                    totalVND = dtt.Rows[0]["VND"].ToString();
                }

                // Giữ logic cũ: IF_XO dùng dòng đầu tiên
                var firstItem = request.Items[0];

                // Logic cũ dùng RoomNo chứ không phải RoomID
                ClassInterface.IF_XO(
                    request.RoomNo,
                    TextUtils.GetBusinessDateTime().ToString(),
                    TextUtils.GetBusinessDateTime().ToString(),
                    "0",
                    totalAmount.ToString(),
                    firstItem.CurrencyID,
                    firstItem.Ref,
                    firstItem.Desc,
                    request.RsvID.ToString(),
                    request.ProfileID.ToString());
                #endregion

                return new ApiResponse
                {
                    Success = true,
                    Message = "Posting completed !"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Success = false,
                    Message = "ERR :" + ex.Message,
                    Error = ex.ToString()
                };
            }
        }

        /// <summary>
        /// Optional helper:
        /// Nếu phía WebForm/API nhận raw rows rồi muốn map sang DTO theo đúng logic cũ của btnPostInvoice_Click
        /// </summary>
        public static PostingInvoiceRequestDto BuildPostingInvoiceRequest(
            DateTime sysDate,
            DateTime businessDate,
            string confirmNo,
            int reservationId,
            int roomId,
            string roomNo,
            int profileId,
            string accountName,
            int folioNo,
            string invoiceCode,
            string invoiceDesc,
            string invoiceRef,
            string invoiceSupp,
            string invoiceNo,
            string currencyLocal,
            int userId,
            string userName,
            int shiftId,
            IEnumerable<dynamic> rows,
            bool expressService,
            decimal expressValue)
        {
            var request = new PostingInvoiceRequestDto
            {
                AutoPosting = false,
                SysDate = sysDate,
                BusinessDate = businessDate,
                ProID = 0,
                ProCode = "",
                ConfirmNo = confirmNo,
                RsvID = reservationId,
                RoomID = roomId,
                RoomNo = roomNo,
                ProfileID = profileId,
                AccountName = accountName,
                Win = folioNo,
                InvoiceCode = invoiceCode,
                InvoiceDesc = invoiceDesc,
                InvoiceRef = invoiceRef,
                InvoiceSupp = invoiceSupp,
                InvoiceNo = invoiceNo,
                CurrencyLocal = currencyLocal,
                UserID = userId,
                UserName = userName,
                ShiftID = shiftId
            };

            foreach (var row in rows)
            {
                var item = new PostingInvoiceItemDto
                {
                    TransCode = Convert.ToString(row.Code) ?? "",
                    Desc = Convert.ToString(row.Description) ?? "",
                    ArCode = Convert.ToString(row.Article) ?? "",
                    ArticleDesc = Convert.ToString(row.ArDesc) ?? "",
                    Quan = Convert.ToInt32(row.Qty),
                    CurrencyID = Convert.ToString(row.Currency) ?? "",
                    Supp = Convert.ToString(row.Supp) ?? "",
                    RoomTypeID = Convert.ToInt32(row.RmID),
                    RoomType = Convert.ToString(row.RmName) ?? ""
                };

                // Giữ nguyên logic cũ của btnPostInvoice_Click
                if (item.CurrencyID == "VND")
                {
                    if (expressService == false)
                    {
                        item.TaxInclude = Convert.ToBoolean(row.TaxInc);
                        if (item.TaxInclude == false)
                            item.Amount = Math.Round(Convert.ToDecimal(row.Amount), 0);
                        else
                            item.Amount = Math.Round(Convert.ToDecimal(row.AmountNet), 0);
                    }
                    else
                    {
                        item.TaxInclude = true;
                        decimal amountNet = Convert.ToDecimal(row.AmountNet);
                        item.Amount = Math.Round(amountNet, 0)
                                    + Math.Round(amountNet * expressValue / 100, 0);
                    }
                }
                else
                {
                    if (expressService == false)
                    {
                        item.TaxInclude = Convert.ToBoolean(row.TaxInc);
                        if (item.TaxInclude == false)
                            item.Amount = Convert.ToDecimal(row.Amount);
                        else
                            item.Amount = Convert.ToDecimal(row.AmountNet);
                    }
                    else
                    {
                        item.TaxInclude = true;
                        decimal amountNet = Convert.ToDecimal(row.AmountNet);
                        item.Amount = amountNet + amountNet * expressValue / 100;
                    }
                }

                item.AmountNetForIptv = Convert.ToDecimal(row.AmountNet);

                if (!string.IsNullOrWhiteSpace(item.ArCode))
                    item.Ref = "A[" + item.ArCode + "]-" + item.ArticleDesc + ",";

                if (expressService == true)
                    item.Ref = (item.Ref ?? "") + " Exp(" + expressValue.ToString("###,###,###.##") + "%)";

                request.Items.Add(item);
            }

            if (request.Items.Count > 0)
                request.CurrencyLocal = request.Items[0].CurrencyID;

            return request;
        }

        private static void OpenAndBegin(ProcessTransactions pt)
        {
            pt.OpenConnection();
            pt.BeginTransaction();
        }

        private static int GetFolio(
            ProcessTransactions pt,
            DateTime sysDate,
            DateTime businessDate,
            string confirmNo,
            int rsvId,
            int win,
            int profileId,
            string accountName,
            ref int rsvIdReturn,
            ref string message)
        {
            return CasheringUtils.GetOrCreateFolioID(
                sysDate,
                businessDate,
                confirmNo,
                rsvId,
                win,
                profileId,
                accountName,
                ref rsvIdReturn,
                pt,
                ref message
            );
        }

        private static TransactionsModel GetTransaction(ProcessTransactions pt, string code)
        {
            return (TransactionsModel)pt.FindByAttribute("Transactions", "Code", code)[0];
        }

        private static void InsertInvoiceGroup(
            ProcessTransactions pt,
            FolioDetailModel mFD_Group,
            TransactionsModel mT_Group,
            string invoiceDesc,
            string invoiceRef,
            string invoiceSupp,
            string invoiceNo,
            int roomID)
        {
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

            if (invoiceNo.Length != 0)
                mFD_Group.Description = invoiceDesc + " #" + invoiceNo;
            else
                mFD_Group.Description = invoiceDesc;

            mFD_Group.Reference = invoiceRef;
            mFD_Group.Supplement = invoiceSupp;
            mFD_Group.RoomID = roomID;

            mFD_Group.Price = 0;
            mFD_Group.Amount = 0;
            mFD_Group.AmountMaster = 0;
            mFD_Group.AmountGross = 0;
            mFD_Group.AmountMasterGross = 0;
            mFD_Group.AmountBeforeTax = 0;
            mFD_Group.AmountMasterBeforeTax = 0;

            mFD_Group.ID = (int)pt.Insert(mFD_Group);
            mFD_Group.InvoiceNo = mFD_Group.ID.ToString();
            mFD_Group.TransactionNo = mFD_Group.InvoiceNo;
        }

        private static void ProcessAllDetails(
            ProcessTransactions pt,
            FolioDetailModel mFD_Group,
            FolioDetailModel mFD_Subgroup,
            FolioDetailModel mFD_Detail,
            List<PostingInvoiceItemDto> items,
            string currencyLocal,
            DateTime businessDate,
            ref decimal rate)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];

                if (!string.IsNullOrWhiteSpace(item.TransCode) && item.Amount > 0)
                {
                    TransactionsModel mT = GetTransaction(pt, item.TransCode);

                    List<BaseModel> arr = pt.FindByAttribute("GenerateTransaction", "TransactionCode", item.TransCode);
                    ArrayList arrList = new ArrayList(arr);

                    if (arr == null || arr.Count == 0)
                    {
                        ProcessNormalDetail(
                            pt,
                            mFD_Group,
                            mFD_Detail,
                            mT,
                            item,
                            i,
                            currencyLocal,
                            businessDate,
                            ref rate
                        );
                    }
                    else
                    {
                        ProcessGenerateDetail(
                            pt,
                            mFD_Group,
                            mFD_Subgroup,
                            mFD_Detail,
                            mT,
                            arrList,
                            item,
                            i,
                            currencyLocal,
                            businessDate,
                            ref rate
                        );
                    }
                }
            }
        }

        private static void ProcessNormalDetail(
            ProcessTransactions pt,
            FolioDetailModel mFD_Group,
            FolioDetailModel mFD_Detail,
            TransactionsModel mT,
            PostingInvoiceItemDto item,
            int i,
            string currencyLocal,
            DateTime businessDate,
            ref decimal rate)
        {
            mFD_Detail.RoomTypeID = item.RoomTypeID;
            mFD_Detail.RoomType = item.RoomType;
            mFD_Detail.CurrencyID = item.CurrencyID;
            mFD_Detail.IsSplit = false;
            mFD_Detail.PostType = 3;
            mFD_Detail.RowState = 2;

            mFD_Detail.TransactionGroupID = mT.TransactionGroupID;
            mFD_Detail.TransactionSubgroupID = mT.TransactionSubGroupID;
            mFD_Detail.GroupCode = mT.GroupCode;
            mFD_Detail.SubgroupCode = mT.SubgroupCode;
            mFD_Detail.GroupType = mT.GroupType;

            mFD_Detail.ArticleCode = item.ArCode;
            mFD_Detail.TransactionCode = mT.Code;
            mFD_Detail.Description = item.Desc;

            mFD_Detail.Quantity = item.Quan;

            mFD_Detail.Amount = item.Amount;
            mFD_Detail.AmountBeforeTax = mFD_Detail.Amount;
            mFD_Detail.Price = mFD_Detail.Amount / mFD_Detail.Quantity;

            if (i == 0)
            {
                mFD_Detail.AmountMaster = TextUtils.ExchangeCurrency(
                    businessDate,
                    item.CurrencyID,
                    currencyLocal,
                    mFD_Detail.Amount);

                rate = mFD_Detail.AmountMaster / mFD_Detail.Amount;
            }
            else
            {
                mFD_Detail.AmountMaster = mFD_Detail.Amount * rate;
            }

            mFD_Detail.AmountMasterBeforeTax = mFD_Detail.AmountMaster;
            mFD_Detail.AmountGross = mFD_Detail.Amount;
            mFD_Detail.AmountMasterGross = mFD_Detail.AmountMaster;

            mFD_Detail.InvoiceNo = mFD_Group.InvoiceNo;

            mFD_Detail.ID = (int)pt.Insert(mFD_Detail);
            mFD_Detail.TransactionNo = mFD_Detail.ID.ToString();
            pt.Update(mFD_Detail);

            mFD_Group.AmountBeforeTax += mFD_Detail.AmountBeforeTax;
            mFD_Group.AmountMasterBeforeTax += mFD_Detail.AmountMasterBeforeTax;

            mFD_Group.Amount += mFD_Detail.AmountMaster;
            mFD_Group.AmountMaster += mFD_Detail.AmountMaster;

            mFD_Group.AmountGross += mFD_Detail.AmountGross;
            mFD_Group.AmountMasterGross += mFD_Detail.AmountMasterGross;
        }

        private static void ProcessGenerateDetail(
            ProcessTransactions pt,
            FolioDetailModel mFD_Group,
            FolioDetailModel mFD_Subgroup,
            FolioDetailModel mFD_Detail,
            TransactionsModel mT,
            ArrayList arr,
            PostingInvoiceItemDto item,
            int i,
            string currencyLocal,
            DateTime businessDate,
            ref decimal rate)
        {
            decimal s1 = 0, s2 = 0, s3 = 0;
            decimal currentAmount = 0;
            decimal baseAmount = item.Amount;
            GenerateTransactionModel mGT;

            if (item.TaxInclude == true)
                baseAmount = CasheringUtils.GetAmount(arr, Convert.ToDecimal(baseAmount));

            mFD_Subgroup.RoomTypeID = item.RoomTypeID;
            mFD_Subgroup.RoomType = item.RoomType;
            mFD_Subgroup.CurrencyID = item.CurrencyID;
            mFD_Subgroup.IsSplit = true;
            mFD_Subgroup.PostType = 3;
            mFD_Subgroup.RowState = 2;

            mFD_Subgroup.Reference = item.Ref;
            mFD_Subgroup.Supplement = item.Supp;

            mFD_Subgroup.TransactionGroupID = mT.TransactionGroupID;
            mFD_Subgroup.TransactionSubgroupID = mT.TransactionSubGroupID;
            mFD_Subgroup.GroupCode = mT.GroupCode;
            mFD_Subgroup.SubgroupCode = mT.SubgroupCode;
            mFD_Subgroup.GroupType = mT.GroupType;

            mFD_Subgroup.ArticleCode = item.ArCode;
            mFD_Subgroup.TransactionCode = mT.Code;
            mFD_Subgroup.Description = item.Desc;

            mFD_Subgroup.Quantity = item.Quan;

            mFD_Subgroup.Price = 0;
            mFD_Subgroup.Amount = 0;
            mFD_Subgroup.AmountMaster = 0;
            mFD_Subgroup.AmountBeforeTax = 0;
            mFD_Subgroup.AmountMasterBeforeTax = 0;

            mFD_Subgroup.ID = (int)pt.Insert(mFD_Subgroup);

            mFD_Subgroup.InvoiceNo = mFD_Group.InvoiceNo;
            mFD_Subgroup.TransactionNo = mFD_Subgroup.ID.ToString();

            for (int j = 0; j < arr.Count; j++)
            {
                mGT = (GenerateTransactionModel?)arr[j] ?? new GenerateTransactionModel();

                if (mGT.Type == 0)
                {
                    if (mGT.BaseAmount == 0)
                        currentAmount = (mGT.Percentage * baseAmount) / 100;
                    else if (mGT.BaseAmount == 1)
                        currentAmount = (mGT.Percentage * s1) / 100;
                    else if (mGT.BaseAmount == 2)
                        currentAmount = (mGT.Percentage * s2) / 100;
                    else
                        currentAmount = (mGT.Percentage * s3) / 100;
                }
                else if (mGT.Type == 1)
                {
                    currentAmount = mGT.Amount;
                }

                if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == false) && (mGT.Subtotal3 == false))
                {
                    s1 += currentAmount;
                }
                else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == true) && (mGT.Subtotal3 == false))
                {
                    s1 += currentAmount;
                    s2 = currentAmount;
                }
                else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == false) && (mGT.Subtotal3 == true))
                {
                    s1 += currentAmount;
                    s3 = currentAmount;
                }
                else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == true) && (mGT.Subtotal3 == true))
                {
                    s1 += currentAmount;
                    s2 = currentAmount;
                    s3 = currentAmount;
                }

                mFD_Detail.RoomTypeID = item.RoomTypeID;
                mFD_Detail.RoomType = item.RoomType;
                mFD_Detail.CurrencyID = item.CurrencyID;
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

                if (item.TaxInclude == true && j == arr.Count - 1)
                    mFD_Detail.Amount = Math.Round(item.Amount - mFD_Subgroup.Amount, 0);
                else
                    mFD_Detail.Amount = CasheringUtils.GetAmountFormat(currentAmount);

                mFD_Detail.Quantity = item.Quan;
                mFD_Detail.AmountBeforeTax = Math.Round(mFD_Detail.Amount, 0);
                mFD_Detail.Price = Math.Round(mFD_Detail.Amount / mFD_Detail.Quantity, 0);
                mFD_Detail.AmountGross = Math.Round(mFD_Detail.Amount, 0);

                if ((i == 0) && (j == 0))
                {
                    mFD_Detail.AmountMaster = Math.Round(
                        TextUtils.ExchangeCurrency(businessDate, item.CurrencyID, currencyLocal, mFD_Detail.Amount), 0);
                    rate = mFD_Detail.AmountMaster / mFD_Detail.Amount;
                }
                else
                {
                    mFD_Detail.AmountMaster = Math.Round(mFD_Detail.Amount * rate, 0);
                }

                if (j == 0)
                {
                    mFD_Subgroup.AmountBeforeTax = Math.Round(mFD_Detail.Amount, 0);
                    mFD_Subgroup.AmountMasterBeforeTax = Math.Round(mFD_Detail.AmountMaster, 0);
                }

                mFD_Detail.AmountMasterBeforeTax = Math.Round(mFD_Detail.AmountMaster, 0);
                mFD_Detail.AmountMasterGross = Math.Round(mFD_Detail.AmountMaster, 0);

                mFD_Detail.InvoiceNo = mFD_Subgroup.InvoiceNo;
                mFD_Detail.TransactionNo = mFD_Subgroup.TransactionNo;
                mFD_Detail.ID = (int)pt.Insert(mFD_Detail);

                mFD_Subgroup.AmountMaster = Math.Round(mFD_Subgroup.AmountMaster + mFD_Detail.AmountMaster, 0);
                mFD_Subgroup.Amount = Math.Round(mFD_Subgroup.Amount + mFD_Detail.Amount, 0);
            }

            mFD_Subgroup.AmountGross = mFD_Subgroup.Amount;
            mFD_Subgroup.AmountMasterGross = mFD_Subgroup.AmountMaster;

            if (item.TaxInclude == true)
            {
                mFD_Subgroup.Amount = item.Amount;
                mFD_Subgroup.AmountMaster = Math.Round(item.Amount * rate, 0);
            }

            mFD_Subgroup.Price = Math.Round(mFD_Subgroup.Amount / mFD_Subgroup.Quantity, 0);
            pt.Update(mFD_Subgroup);

            mFD_Group.AmountBeforeTax = Math.Round(mFD_Group.AmountBeforeTax + mFD_Subgroup.AmountBeforeTax, 0);
            mFD_Group.AmountMasterBeforeTax = Math.Round(mFD_Group.AmountMasterBeforeTax + mFD_Subgroup.AmountMasterBeforeTax, 0);

            mFD_Group.Amount = Math.Round(mFD_Group.Amount + mFD_Subgroup.Amount, 0);
            mFD_Group.AmountMaster = Math.Round(mFD_Group.AmountMaster + mFD_Subgroup.AmountMaster, 0);

            mFD_Group.AmountGross = Math.Round(mFD_Group.AmountGross + mFD_Subgroup.AmountGross, 0);
            mFD_Group.AmountMasterGross = Math.Round(mFD_Group.AmountMasterGross + mFD_Subgroup.AmountMasterGross, 0);
        }

        private static void FinalizeAndCommit(
            ProcessTransactions pt,
            FolioDetailModel mFD_Group,
            DateTime sysDate,
            DateTime businessDate,
            int rsvId,
            int folioId,
            ref string message)
        {
            mFD_Group.Price = mFD_Group.Amount;
            pt.Update(mFD_Group);

            CasheringUtils.UpdateBalance(rsvId, folioId, pt, ref message);

            pt.CommitTransaction();
            pt.CloseConnection();
        }

        private static void InitGroupModel(
            FolioDetailModel mFD_Group,
            bool autoPosting,
            int proID,
            string proCode,
            string invoiceNo,
            string currencyLocal,
            int rsvId,
            int roomID,
            int folioId,
            DateTime businessDate,
            DateTime sysDate,
            int userID,
            string userName,
            int shiftID)
        {
            if (proID != 0)
            {
                mFD_Group.ProfitCenterID = proID;
                mFD_Group.ProfitCenterCode = proCode;
            }
            else
            {
                mFD_Group.ProfitCenterID = ProfitCenterID;
                mFD_Group.ProfitCenterCode = ProfitCenterCode;
            }

            mFD_Group.CheckNo = invoiceNo;
            mFD_Group.Status = false;
            mFD_Group.CurrencyID = currencyLocal;
            mFD_Group.CurrencyMaster = currencyLocal;
            mFD_Group.ReservationID = rsvId;
            mFD_Group.OriginReservationID = rsvId;
            mFD_Group.RoomID = roomID;
            mFD_Group.FolioID = folioId;
            mFD_Group.OriginFolioID = folioId;
            mFD_Group.TransactionDate = businessDate;
            mFD_Group.PackageID = 0;

            mFD_Group.UserID = userID;
            mFD_Group.UserName = userName;
            mFD_Group.CashierNo = userName;
            mFD_Group.ShiftID = shiftID;

            mFD_Group.UserInsertID = userID;
            mFD_Group.UserUpdateID = userID;
            mFD_Group.CreateDate = sysDate;
            mFD_Group.UpdateDate = sysDate;

            if (autoPosting == true)
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
                mFD_Group.ShiftID = shiftID;
            }
        }

        private static void InitSubgroupModel(
            FolioDetailModel mFD_Subgroup,
            bool autoPosting,
            int proID,
            string proCode,
            string invoiceNo,
            string currencyLocal,
            int rsvId,
            int roomID,
            int folioId,
            DateTime businessDate,
            DateTime sysDate,
            int userID,
            string userName,
            int shiftID)
        {
            if (proID != 0)
            {
                mFD_Subgroup.ProfitCenterID = proID;
                mFD_Subgroup.ProfitCenterCode = proCode;
            }
            else
            {
                mFD_Subgroup.ProfitCenterID = ProfitCenterID;
                mFD_Subgroup.ProfitCenterCode = ProfitCenterCode;
            }

            mFD_Subgroup.CheckNo = invoiceNo;
            mFD_Subgroup.Status = false;
            mFD_Subgroup.CurrencyMaster = currencyLocal;
            mFD_Subgroup.ReservationID = rsvId;
            mFD_Subgroup.OriginReservationID = rsvId;
            mFD_Subgroup.RoomID = roomID;
            mFD_Subgroup.FolioID = folioId;
            mFD_Subgroup.OriginFolioID = folioId;
            mFD_Subgroup.TransactionDate = businessDate;
            mFD_Subgroup.PackageID = 0;

            mFD_Subgroup.UserID = userID;
            mFD_Subgroup.UserName = userName;
            mFD_Subgroup.CashierNo = userName;
            mFD_Subgroup.ShiftID = shiftID;

            mFD_Subgroup.UserInsertID = userID;
            mFD_Subgroup.UserUpdateID = userID;
            mFD_Subgroup.CreateDate = sysDate;
            mFD_Subgroup.UpdateDate = sysDate;

            if (autoPosting == true)
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
                mFD_Subgroup.ShiftID = shiftID;
            }
        }

        private static void InitDetailModel(
            FolioDetailModel mFD_Detail,
            bool autoPosting,
            int proID,
            string proCode,
            string invoiceNo,
            string currencyLocal,
            int rsvId,
            int roomID,
            int folioId,
            DateTime businessDate,
            DateTime sysDate,
            int userID,
            string userName,
            int shiftID)
        {
            if (proID != 0)
            {
                mFD_Detail.ProfitCenterID = proID;
                mFD_Detail.ProfitCenterCode = proCode;
            }
            else
            {
                mFD_Detail.ProfitCenterID = ProfitCenterID;
                mFD_Detail.ProfitCenterCode = ProfitCenterCode;
            }

            mFD_Detail.CheckNo = invoiceNo;
            mFD_Detail.Status = false;
            mFD_Detail.CurrencyMaster = currencyLocal;
            mFD_Detail.ReservationID = rsvId;
            mFD_Detail.OriginReservationID = rsvId;
            mFD_Detail.RoomID = roomID;
            mFD_Detail.FolioID = folioId;
            mFD_Detail.OriginFolioID = folioId;
            mFD_Detail.TransactionDate = businessDate;
            mFD_Detail.PackageID = 0;

            mFD_Detail.UserID = userID;
            mFD_Detail.UserName = userName;
            mFD_Detail.CashierNo = userName;
            mFD_Detail.ShiftID = shiftID;

            mFD_Detail.UserInsertID = userID;
            mFD_Detail.UserUpdateID = userID;
            mFD_Detail.CreateDate = sysDate;
            mFD_Detail.UpdateDate = sysDate;

            if (autoPosting == true)
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
                mFD_Detail.ShiftID = shiftID;
            }
        }

        protected static ApiResponse ValidatePostingInvoice(string invoiceCode, string invoiceDesc, int postedRowCount)
        {
            if (postedRowCount == 0)
            {
                return new ApiResponse
                {
                    Success = false,
                    Message = "No transaction posted to invoice !!!"
                };
            }

            try
            {
                Expression exp = new Expression("GroupType", 2, "=");
                exp = exp.And(new Expression("IsActive", 1, "="));
                exp = exp.And(new Expression("Code", invoiceCode, "="));

                ArrayList arrTrans = TransactionsBO.Instance.FindByExpression(exp);

                if (arrTrans == null || arrTrans.Count == 0)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Invoice code not found !!!"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Success = false,
                    Message = "System error",
                    Error = ex.Message
                };
            }

            if (string.IsNullOrWhiteSpace(invoiceDesc))
            {
                return new ApiResponse
                {
                    Success = false,
                    Message = "Please enter description for invoice !!!"
                };
            }

            return new ApiResponse
            {
                Success = true,
                Message = "Valid"
            };
        }

        protected static ApiResponse ValidatePostingInvoiceRequest(PostingInvoiceRequestDto request)
        {
            if (request == null)
            {
                return new ApiResponse
                {
                    Success = false,
                    Message = "Request is null"
                };
            }

            return ValidatePostingInvoice(
                request.InvoiceCode,
                request.InvoiceDesc,
                request.Items == null ? 0 : request.Items.Count);
        }
    }
}