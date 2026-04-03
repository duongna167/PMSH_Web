using BaseBusiness.BO;
using BaseBusiness.bc;
using BaseBusiness.Contants;
using BaseBusiness.Model;
using BaseBusiness.util;
using Billing.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Billing.Controllers
{
    public partial class BillingController
    {
        private sealed class PostingDetailContext
        {
            public string ConfirmNo { get; set; } = string.Empty;
            public int ReservationID { get; set; }
            public int ProfileID { get; set; }
            public string AccountName { get; set; } = string.Empty;
            public int FolioNo { get; set; }
            public string RoomNo { get; set; } = string.Empty;
            public string CurrencyLocal { get; set; } = "VND";
            public ReservationModel Reservation { get; set; }
        }

        /// <summary>
        /// Entry point cho PostingSave mới: tách riêng nhánh detail và invoice.
        /// </summary>
        [HttpPost]
        public IActionResult PostingSave([FromBody] PostingSaveRequestDto request)
        {
            if (request == null)
            {
                return BadRequest(new { success = false, message = "No data received." });
            }

            string mode = (request.Mode ?? string.Empty).Trim().ToLowerInvariant();
            if (mode == "invoice" || request.InvoiceRequest != null)
            {
                return PostingSaveInvoice(request.InvoiceRequest);
            }

            return PostingSaveDetail(request);
        }

        /// <summary>
        /// Xử lý post invoice qua service mới dùng DTO tương thích WinForm.
        /// </summary>
        private IActionResult PostingSaveInvoice(PostingInvoiceRequestDto request)
        {
            if (request == null)
            {
                return BadRequest(new { success = false, message = "No invoice data received." });
            }

            try
            {
                NormalizeInvoiceRequest(request);

                ApiResponse response = _iPostingInvoiceService.PostInvoiceFromRequest(request);
                if (response.Success == false)
                {
                    int statusCode = string.IsNullOrWhiteSpace(response.Error)
                        ? StatusCodes.Status400BadRequest
                        : StatusCodes.Status500InternalServerError;

                    return StatusCode(statusCode, new
                    {
                        success = false,
                        message = response.Message ?? "Posting invoice failed.",
                        error = response.Error
                    });
                }

                string invoiceNo = FindLatestInvoiceNo(request.RsvID, request.Win, request.ConfirmNo);

                return Ok(new
                {
                    success = true,
                    message = response.Message ?? "Posting completed !",
                    invoiceNo
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// Xử lý post detail theo flow bám GenerateTransWithOutTaxInclude của WinForm.
        /// </summary>
        private IActionResult PostingSaveDetail(PostingRequest request)
        {
            ProcessTransactions pt = new ProcessTransactions();

            try
            {
                if (request?.Details == null || request.Details.Count == 0)
                {
                    return BadRequest(new { success = false, message = "No data received." });
                }

                if (request.Details.Any(m => m.PostType == 3))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invoice posting must use invoice mode."
                    });
                }

                FolioDetailModel firstItem = request.Details.First();
                PostingDetailContext context = ResolvePostingDetailContext(request, firstItem);
                DateTime sysDate = TextUtils.GetSystemDate();
                DateTime businessDate = TextUtils.GetBusinessDateTime();
                string lastInvoiceNo = string.Empty;

                pt.OpenConnection();
                pt.BeginTransaction();

                foreach (FolioDetailModel item in request.Details)
                {
                    lastInvoiceNo = PostDetailItem(context, item, request.ChkExpressService, pt, sysDate, businessDate);
                }

                pt.CommitTransaction();

                return Ok(new
                {
                    success = true,
                    message = "Posted successfully!",
                    invoiceNo = lastInvoiceNo
                });
            }
            catch (Exception ex)
            {
                if (pt.Transaction != null && pt.Transaction.Connection != null)
                {
                    pt.RollBack();
                }

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = ex.Message
                });
            }
            finally
            {
                pt.CloseConnection();
            }
        }

        /// <summary>
        /// Post một dòng detail và tự xử lý trường hợp basic post hoặc generate split, kể cả express service.
        /// </summary>
        private string PostDetailItem(
            PostingDetailContext context,
            FolioDetailModel sourceItem,
            bool hasExpressService,
            ProcessTransactions pt,
            DateTime sysDate,
            DateTime businessDate)
        {
            if (string.IsNullOrWhiteSpace(sourceItem.TransactionCode))
            {
                throw new Exception("Transaction code is required.");
            }

            int userId = sourceItem.UserID > 0 ? sourceItem.UserID : sourceItem.UserInsertID;
            string userName = !string.IsNullOrWhiteSpace(sourceItem.UserName)
                ? sourceItem.UserName
                : sourceItem.CashierNo ?? string.Empty;

            if (userId <= 0)
            {
                throw new Exception("UserID is required for posting detail.");
            }

            if (sourceItem.Quantity <= 0)
            {
                sourceItem.Quantity = 1;
            }

            string message = string.Empty;
            TransactionsModel transaction = GetPostingTransaction(sourceItem.TransactionCode);
            bool taxInclude = transaction.TaxInclude || hasExpressService;

            if (!string.IsNullOrWhiteSpace(sourceItem.RoomType) && sourceItem.RoomTypeID <= 0)
            {
                var roomTypeList = RoomTypeBO.Instance.FindByAttribute("Code", sourceItem.RoomType);
                if (roomTypeList != null && roomTypeList.Count > 0)
                {
                    RoomTypeModel roomTypeInfo = (RoomTypeModel)roomTypeList[0];
                    sourceItem.RoomTypeID = roomTypeInfo.ID;
                }
            }

            int roomId = ResolvePostingRoomId(context.Reservation, sourceItem);
            int reservationIdReturn = 0;
            int folioId = GetOrCreatePostingFolioId(
                sysDate,
                businessDate,
                context,
                userId,
                pt,
                ref reservationIdReturn,
                ref message);

            if (folioId <= 0)
            {
                throw new Exception(string.IsNullOrWhiteSpace(message) ? "Could not resolve folio." : message);
            }

            string currencyId = string.IsNullOrWhiteSpace(sourceItem.CurrencyID)
                ? context.CurrencyLocal
                : sourceItem.CurrencyID;

            decimal amountBeforeTax = sourceItem.AmountBeforeTax > 0
                ? sourceItem.AmountBeforeTax
                : sourceItem.Amount;

            decimal amountNet = sourceItem.Amount > 0
                ? sourceItem.Amount
                : amountBeforeTax;

            if (currencyId == "VND")
            {
                amountBeforeTax = Math.Round(amountBeforeTax, 0);
                amountNet = Math.Round(amountNet, 0);
            }

            decimal amountParam = taxInclude ? amountNet : amountBeforeTax;
            if (amountParam <= 0)
            {
                throw new Exception("Amount must be greater than zero.");
            }

            List<BaseModel> generateConfigs = pt.FindByAttribute("GenerateTransaction", "TransactionCode", sourceItem.TransactionCode);
            if (generateConfigs == null || generateConfigs.Count == 0)
            {
                FolioDetailModel detailLine = CreatePostingDetailBaseModel(
                    sourceItem,
                    context,
                    reservationIdReturn,
                    folioId,
                    roomId,
                    currencyId,
                    sysDate,
                    businessDate);

                detailLine.IsSplit = false;
                detailLine.Reference = sourceItem.Reference ?? string.Empty;
                detailLine.Supplement = sourceItem.Supplement ?? string.Empty;
                detailLine.TransactionGroupID = transaction.TransactionGroupID;
                detailLine.TransactionSubgroupID = transaction.TransactionSubGroupID;
                detailLine.GroupCode = transaction.GroupCode;
                detailLine.SubgroupCode = transaction.SubgroupCode;
                detailLine.GroupType = transaction.GroupType;
                detailLine.ArticleCode = sourceItem.ArticleCode ?? string.Empty;
                detailLine.TransactionCode = transaction.Code;
                detailLine.Description = !string.IsNullOrWhiteSpace(sourceItem.Description)
                    ? sourceItem.Description
                    : transaction.Description;
                detailLine.Amount = amountParam;
                detailLine.AmountBeforeTax = amountParam;
                detailLine.Price = detailLine.Amount / detailLine.Quantity;
                detailLine.AmountMaster = TextUtils.ExchangeCurrency(
                    businessDate,
                    currencyId,
                    context.CurrencyLocal,
                    detailLine.Amount);
                detailLine.AmountMasterBeforeTax = detailLine.AmountMaster;
                detailLine.AmountGross = detailLine.Amount;
                detailLine.AmountMasterGross = detailLine.AmountMaster;
                detailLine.PostType = 1;
                detailLine.RowState = 1;

                detailLine.ID = (int)pt.Insert(detailLine);
                detailLine.InvoiceNo = detailLine.ID.ToString();
                detailLine.TransactionNo = detailLine.ID.ToString();
                pt.Update(detailLine);

                EnsurePostingBalance(reservationIdReturn, folioId, pt);
                InsertPostingHistory(detailLine, "[POST_GEN]");
                PostDetailToIptv(context, folioId, amountNet, currencyId, detailLine.Reference, detailLine.Description);

                return detailLine.InvoiceNo;
            }

            ArrayList generateList = new ArrayList(generateConfigs);
            decimal subtotal1 = 0;
            decimal subtotal2 = 0;
            decimal subtotal3 = 0;
            decimal currentAmount = 0;
            decimal baseAmount = amountParam;
            decimal exchangeRate = 0;

            if (taxInclude)
            {
                baseAmount = CasheringUtils.GetAmount(generateList, baseAmount);
            }

            FolioDetailModel masterLine = CreatePostingDetailBaseModel(
                sourceItem,
                context,
                reservationIdReturn,
                folioId,
                roomId,
                currencyId,
                sysDate,
                businessDate);

            masterLine.IsSplit = true;
            masterLine.Reference = sourceItem.Reference ?? string.Empty;
            masterLine.Supplement = sourceItem.Supplement ?? string.Empty;
            masterLine.TransactionGroupID = transaction.TransactionGroupID;
            masterLine.TransactionSubgroupID = transaction.TransactionSubGroupID;
            masterLine.GroupCode = transaction.GroupCode;
            masterLine.SubgroupCode = transaction.SubgroupCode;
            masterLine.GroupType = transaction.GroupType;
            masterLine.ArticleCode = sourceItem.ArticleCode ?? string.Empty;
            masterLine.TransactionCode = transaction.Code;
            masterLine.Description = !string.IsNullOrWhiteSpace(sourceItem.Description)
                ? sourceItem.Description
                : transaction.Description;
            masterLine.Price = 0;
            masterLine.Amount = 0;
            masterLine.AmountMaster = 0;
            masterLine.AmountBeforeTax = 0;
            masterLine.AmountMasterBeforeTax = 0;
            masterLine.AmountGross = 0;
            masterLine.AmountMasterGross = 0;
            masterLine.PostType = 2;
            masterLine.RowState = 1;

            masterLine.ID = (int)pt.Insert(masterLine);
            masterLine.InvoiceNo = masterLine.ID.ToString();
            masterLine.TransactionNo = masterLine.ID.ToString();

            for (int index = 0; index < generateList.Count; index++)
            {
                GenerateTransactionModel generateItem = (GenerateTransactionModel)generateList[index];

                if (generateItem.Type == 0)
                {
                    if (generateItem.BaseAmount == 0)
                    {
                        currentAmount = (generateItem.Percentage * baseAmount) / 100;
                    }
                    else if (generateItem.BaseAmount == 1)
                    {
                        currentAmount = (generateItem.Percentage * subtotal1) / 100;
                    }
                    else if (generateItem.BaseAmount == 2)
                    {
                        currentAmount = (generateItem.Percentage * subtotal2) / 100;
                    }
                    else
                    {
                        currentAmount = (generateItem.Percentage * subtotal3) / 100;
                    }
                }
                else if (generateItem.Type == 1)
                {
                    currentAmount = generateItem.Amount;
                }

                currentAmount = CasheringUtils.GetAmountFormat(currentAmount);

                if (generateItem.Subtotal1 == true && generateItem.Subtotal2 == false && generateItem.Subtotal3 == false)
                {
                    subtotal1 += currentAmount;
                }
                else if (generateItem.Subtotal1 == true && generateItem.Subtotal2 == true && generateItem.Subtotal3 == false)
                {
                    subtotal1 += currentAmount;
                    subtotal2 = currentAmount;
                }
                else if (generateItem.Subtotal1 == true && generateItem.Subtotal2 == false && generateItem.Subtotal3 == true)
                {
                    subtotal1 += currentAmount;
                    subtotal3 = currentAmount;
                }
                else if (generateItem.Subtotal1 == true && generateItem.Subtotal2 == true && generateItem.Subtotal3 == true)
                {
                    subtotal1 += currentAmount;
                    subtotal2 = currentAmount;
                    subtotal3 = currentAmount;
                }

                FolioDetailModel splitLine = CreatePostingDetailBaseModel(
                    sourceItem,
                    context,
                    reservationIdReturn,
                    folioId,
                    roomId,
                    currencyId,
                    sysDate,
                    businessDate);

                splitLine.IsSplit = false;
                splitLine.PostType = 2;
                splitLine.RowState = 2;
                splitLine.TransactionGroupID = generateItem.TransactionGroupID;
                splitLine.TransactionSubgroupID = generateItem.TransactionSubGroupID;
                splitLine.GroupCode = generateItem.GroupCode;
                splitLine.SubgroupCode = generateItem.SubgroupCode;
                splitLine.GroupType = generateItem.GroupType;
                splitLine.TransactionCode = generateItem.TransactionCodeDetail;
                splitLine.Description = generateItem.Description;

                if (taxInclude && index == generateList.Count - 1)
                {
                    splitLine.Amount = amountParam - masterLine.Amount;
                }
                else
                {
                    splitLine.Amount = CasheringUtils.GetAmountFormat(currentAmount);
                }

                splitLine.AmountBeforeTax = splitLine.Amount;
                splitLine.Price = splitLine.Amount / splitLine.Quantity;
                splitLine.AmountGross = splitLine.Amount;

                if (index == 0)
                {
                    splitLine.AmountMaster = TextUtils.ExchangeCurrency(
                        businessDate,
                        currencyId,
                        context.CurrencyLocal,
                        splitLine.Amount);

                    exchangeRate = splitLine.Amount == 0
                        ? 0
                        : splitLine.AmountMaster / splitLine.Amount;

                    masterLine.AmountBeforeTax = splitLine.Amount;
                    masterLine.AmountMasterBeforeTax = splitLine.AmountMaster;
                }
                else
                {
                    splitLine.AmountMaster = splitLine.Amount * exchangeRate;
                }

                splitLine.AmountMasterBeforeTax = splitLine.AmountMaster;
                splitLine.AmountMasterGross = splitLine.AmountMaster;
                splitLine.InvoiceNo = masterLine.InvoiceNo;
                splitLine.TransactionNo = masterLine.TransactionNo;
                splitLine.ID = (int)pt.Insert(splitLine);

                masterLine.AmountMaster += splitLine.AmountMaster;
                masterLine.Amount += splitLine.Amount;
            }

            masterLine.AmountGross = masterLine.Amount;
            masterLine.AmountMasterGross = masterLine.AmountMaster;

            if (taxInclude)
            {
                masterLine.Amount = amountParam;
                masterLine.AmountMaster = amountParam * exchangeRate;
            }

            masterLine.Price = masterLine.Amount / masterLine.Quantity;
            pt.Update(masterLine);

            EnsurePostingBalance(reservationIdReturn, folioId, pt);
            InsertPostingHistory(masterLine, "[POST_GEN]");
            PostDetailToIptv(context, folioId, amountNet, currencyId, masterLine.Reference, masterLine.Description);

            return masterLine.InvoiceNo;
        }

        /// <summary>
        /// Gom đủ context posting từ request, folio hiện tại và reservation để giống dữ liệu WinForm.
        /// </summary>
        private PostingDetailContext ResolvePostingDetailContext(PostingRequest request, FolioDetailModel firstItem)
        {
            int reservationId = firstItem.ReservationID;
            ReservationModel reservation = reservationId > 0
                ? (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(reservationId)
                : null;

            FolioModel targetFolio = FindPostingFolio(request, reservationId);

            return new PostingDetailContext
            {
                ConfirmNo = FirstNotEmpty(
                    request.ConfirmNo,
                    targetFolio?.ConfirmationNo,
                    reservation?.ConfirmationNo),
                ReservationID = reservationId,
                ProfileID = request.ProfileID > 0
                    ? request.ProfileID
                    : targetFolio?.ProfileID > 0
                        ? targetFolio.ProfileID
                        : reservation?.ProfileIndividualId ?? 0,
                AccountName = FirstNotEmpty(
                    request.AccountName,
                    targetFolio?.AccountName,
                    reservation?.LastName),
                FolioNo = request.FolioNo,
                RoomNo = FirstNotEmpty(
                    request.RoomNo,
                    reservation?.RoomNo),
                CurrencyLocal = string.IsNullOrWhiteSpace(TextUtils.GetMasterCurrency())
                    ? "VND"
                    : TextUtils.GetMasterCurrency(),
                Reservation = reservation
            };
        }

        /// <summary>
        /// Tìm folio đích theo window đang chọn, ưu tiên folio hiện tại trên màn Billing.
        /// </summary>
        private FolioModel FindPostingFolio(PostingRequest request, int reservationId)
        {
            if (request.CurrentFolioID > 0)
            {
                FolioModel currentFolio = (FolioModel)FolioBO.Instance.FindByPrimaryKey(request.CurrentFolioID);
                if (currentFolio != null && GetDisplayFolioNo(currentFolio) == request.FolioNo)
                {
                    return currentFolio;
                }
            }

            if (reservationId <= 0)
            {
                return null;
            }

            List<FolioModel> folios = FolioBO.GetFolioNo(reservationId);
            return folios.FirstOrDefault(folio => GetDisplayFolioNo(folio) == request.FolioNo);
        }

        /// <summary>
        /// Quy đổi folio DB sang số window hiển thị trên FE, master folio dùng số âm.
        /// </summary>
        private int GetDisplayFolioNo(FolioModel folio)
        {
            if (folio == null)
            {
                return 0;
            }

            return folio.IsMasterFolio
                ? -Math.Abs(folio.FolioNo)
                : folio.FolioNo;
        }

        /// <summary>
        /// Lấy transaction cấu hình của mã đang post và fail sớm nếu mã không hợp lệ.
        /// </summary>
        private TransactionsModel GetPostingTransaction(string transactionCode)
        {
            var transactionList = TransactionsBO.Instance.FindByAttribute("Code", transactionCode);
            if (transactionList == null || transactionList.Count == 0)
            {
                throw new Exception("Transaction not found: " + transactionCode);
            }

            return (TransactionsModel)transactionList[0];
        }

        /// <summary>
        /// Chọn RoomID đúng theo RoomType hiện tại, giữ lại nhánh map room type của WinForm.
        /// </summary>
        private int ResolvePostingRoomId(ReservationModel reservation, FolioDetailModel sourceItem)
        {
            int roomId = sourceItem.RoomID > 0
                ? sourceItem.RoomID
                : reservation?.RoomId ?? 0;

            if (reservation != null && sourceItem.RoomTypeID > 0 && reservation.RoomTypeId != sourceItem.RoomTypeID)
            {
                int roomIdByType = CasheringUtils.GetRoomByRoomType(sourceItem.RoomTypeID);
                if (roomIdByType > 0)
                {
                    roomId = roomIdByType;
                }
            }

            return roomId;
        }

        /// <summary>
        /// Tạo base model dùng chung cho các dòng posting detail/master/split với metadata từ request web.
        /// </summary>
        private FolioDetailModel CreatePostingDetailBaseModel(
            FolioDetailModel sourceItem,
            PostingDetailContext context,
            int reservationId,
            int folioId,
            int roomId,
            string currencyId,
            DateTime sysDate,
            DateTime businessDate)
        {
            int userId = sourceItem.UserID > 0 ? sourceItem.UserID : sourceItem.UserInsertID;
            string userName = !string.IsNullOrWhiteSpace(sourceItem.UserName)
                ? sourceItem.UserName
                : sourceItem.CashierNo ?? string.Empty;
            string cashierNo = !string.IsNullOrWhiteSpace(sourceItem.CashierNo)
                ? sourceItem.CashierNo
                : userName;

            return new FolioDetailModel
            {
                UserID = userId,
                UserName = userName,
                ShiftID = sourceItem.ShiftID,
                CashierNo = cashierNo,
                ReservationID = reservationId,
                OriginReservationID = reservationId,
                FolioID = folioId,
                OriginFolioID = folioId,
                InvoiceNo = string.Empty,
                TransactionNo = string.Empty,
                ReceiptNo = string.Empty,
                TransactionDate = businessDate,
                ProfitCenterID = sourceItem.ProfitCenterID > 0 ? sourceItem.ProfitCenterID : 2,
                ProfitCenterCode = !string.IsNullOrWhiteSpace(sourceItem.ProfitCenterCode)
                    ? sourceItem.ProfitCenterCode
                    : "0",
                Status = false,
                Quantity = sourceItem.Quantity > 0 ? sourceItem.Quantity : 1,
                CurrencyID = currencyId,
                CurrencyMaster = context.CurrencyLocal,
                PackageID = 0,
                RoomType = sourceItem.RoomType ?? string.Empty,
                RoomTypeID = sourceItem.RoomTypeID,
                UserInsertID = sourceItem.UserInsertID > 0 ? sourceItem.UserInsertID : userId,
                CreateDate = sysDate,
                UserUpdateID = sourceItem.UserUpdateID > 0 ? sourceItem.UserUpdateID : userId,
                UpdateDate = sysDate,
                RoomID = roomId,
                Property = sourceItem.Property ?? string.Empty,
                CheckNo = sourceItem.CheckNo ?? string.Empty,
                OriginARNo = sourceItem.OriginARNo ?? string.Empty,
                IsPostedAR = false,
                ARTransID = 0,
                IsTransfer = false
            };
        }

        /// <summary>
        /// Cập nhật lại balance folio và reservation bằng util gốc để giữ đúng nghiệp vụ cũ.
        /// </summary>
        private void EnsurePostingBalance(int reservationId, int folioId, ProcessTransactions pt)
        {
            string message = string.Empty;
            if (CasheringUtils.UpdateBalance(reservationId, folioId, pt, ref message) == false)
            {
                throw new Exception(string.IsNullOrWhiteSpace(message) ? "Could not update folio balance." : message);
            }
        }

        /// <summary>
        /// Ghi posting history cho dòng vừa post để giữ log vận hành như flow cũ.
        /// </summary>
        private void InsertPostingHistory(FolioDetailModel model, string actionPrefix)
        {
            PostingHistoryBO.Instance.Insert(new PostingHistoryModel
            {
                ActionType = 0,
                ActionText = $"{actionPrefix} - {model.TransactionCode} - {model.Description}",
                ActionDate = DateTime.Now,
                ActionUser = model.UserName,
                Amount = model.Amount,
                InvoiceNo = model.InvoiceNo,
                Code = model.TransactionCode,
                Description = model.Description,
                TransactionDate = model.TransactionDate,
                Machine = Environment.MachineName,
                Action_FolioID = model.FolioID,
                AfterAction_FolioID = model.FolioID,
                Property = "PMS"
            });
        }

        /// <summary>
        /// Gửi interface IPTV sau khi detail post thành công, dùng AmountNet giống WinForm.
        /// </summary>
        private void PostDetailToIptv(
            PostingDetailContext context,
            int folioId,
            decimal amountNet,
            string currencyId,
            string reference,
            string description)
        {
            if (folioId <= 0)
            {
                return;
            }

            DataTable dtBalance = TextUtils.Select(
                "Select dbo.getBalanceOfFolio(" + folioId + ",'USD') as USD, dbo.getBalanceOfFolio(" + folioId + ",'VND') as VND ");

            if (dtBalance.Rows.Count > 0)
            {
                _ = dtBalance.Rows[0]["USD"].ToString();
                _ = dtBalance.Rows[0]["VND"].ToString();
            }

            CasheringUtils.IF_XO(
                context.RoomNo ?? string.Empty,
                TextUtils.GetBusinessDateTime().ToString(),
                TextUtils.GetBusinessDateTime().ToString(),
                "0",
                amountNet.ToString(),
                currencyId,
                reference ?? string.Empty,
                description ?? string.Empty,
                context.ReservationID.ToString(),
                context.ProfileID.ToString());
        }

        /// <summary>
        /// Lấy hoặc tạo folio đích theo logic window/master folio của WinForm nhưng dùng user từ request.
        /// </summary>
        private int GetOrCreatePostingFolioId(
            DateTime sysDate,
            DateTime businessDate,
            PostingDetailContext context,
            int userId,
            ProcessTransactions pt,
            ref int reservationIdReturn,
            ref string message)
        {
            try
            {
                Expression expression;
                if (context.FolioNo < 0)
                {
                    expression = new Expression("ConfirmationNo", context.ConfirmNo, "=");
                    expression = expression.And(new Expression("FolioNo", context.FolioNo, "="));
                }
                else
                {
                    expression = new Expression("ReservationID", context.ReservationID, "=");
                    expression = expression.And(new Expression("FolioNo", context.FolioNo, "="));
                }

                ArrayList existingFolios = pt.FindByExpression("Folio", expression);
                if (existingFolios != null && existingFolios.Count > 0)
                {
                    FolioModel folio = (FolioModel)existingFolios[0];
                    reservationIdReturn = folio.ReservationID;
                    if (folio.Status == false)
                    {
                        return folio.ID;
                    }

                    message = "Folio is locked.";
                    return -1;
                }

                FolioModel newFolio = new FolioModel
                {
                    ARNo = string.Empty,
                    BalanceVND = 0,
                    BalanceUSD = 0,
                    ConfirmationNo = context.ConfirmNo,
                    FolioDate = businessDate,
                    CreateDate = sysDate,
                    UpdateDate = sysDate,
                    UserInsertID = userId,
                    UserUpdateID = userId,
                    FolioNo = context.FolioNo,
                    ProfileID = context.ProfileID,
                    AccountName = context.AccountName,
                    Status = false
                };

                if (context.FolioNo < 0)
                {
                    newFolio.IsMasterFolio = true;
                    newFolio.ReservationID = GetOrCreatePostingReservationMaster(
                        sysDate,
                        context.ConfirmNo,
                        context.ReservationID,
                        userId,
                        pt,
                        ref message);
                }
                else
                {
                    newFolio.IsMasterFolio = false;
                    newFolio.ReservationID = context.ReservationID;
                }

                if (newFolio.ReservationID <= 0)
                {
                    return 0;
                }

                reservationIdReturn = newFolio.ReservationID;
                return (int)pt.Insert(newFolio);
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return 0;
            }
        }

        /// <summary>
        /// Tạo reservation master khi post vào master folio mà reservation ảo chưa tồn tại.
        /// </summary>
        private int GetOrCreatePostingReservationMaster(
            DateTime sysDate,
            string confirmationNo,
            int fromReservationId,
            int userId,
            ProcessTransactions pt,
            ref string message)
        {
            try
            {
                Expression expression = new Expression("ConfirmationNo", confirmationNo, "=");
                expression = expression.And(new Expression("ReservationNo", "0", "="));

                ArrayList existingReservations = pt.FindByExpression("Reservation", expression);
                if (existingReservations != null && existingReservations.Count > 0)
                {
                    return ((ReservationModel)existingReservations[0]).ID;
                }

                ReservationModel reservation = (ReservationModel)pt.FindByPK("Reservation", fromReservationId);
                reservation.Status = 0;
                reservation.MainGuest = false;
                reservation.PostingMaster = true;
                reservation.TotalAmount = 0;
                reservation.NoOfAdult = 0;
                reservation.NoOfChild = 0;
                reservation.NoOfChild1 = 0;
                reservation.NoOfChild2 = 0;
                reservation.NoOfRoom = 1;
                reservation.Rate = 0;
                reservation.CurrencyId = "USD";
                reservation.DropOffReqdId = 0;
                reservation.PickupReqdId = 0;
                reservation.RoomTypeId = 0;
                reservation.RtcId = 0;
                reservation.RoomType = string.Empty;
                reservation.RoomId = 0;
                reservation.RoomNo = string.Empty;
                reservation.UserInsertId = userId;
                reservation.UserUpdateId = userId;
                reservation.CreateDate = sysDate;
                reservation.UpdateDate = sysDate;
                reservation.ProfileIndividualId = 0;
                reservation.LastName = string.Empty;
                reservation.ReservationNo = "0";
                reservation.ShareRoom = 0;
                reservation.Status = 1;

                return (int)pt.Insert(reservation);
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return 0;
            }
        }

        /// <summary>
        /// Chuẩn hóa request invoice trước khi đẩy xuống service mới.
        /// </summary>
        private void NormalizeInvoiceRequest(PostingInvoiceRequestDto request)
        {
            request.AutoPosting = false;
            request.SysDate = DateTime.Now;
            request.BusinessDate = TextUtils.GetBusinessDateTime();
            request.ProCode ??= string.Empty;
            request.AccountName ??= string.Empty;
            request.Items ??= new List<PostingInvoiceItemDto>();

            if (string.IsNullOrWhiteSpace(request.CurrencyLocal) && request.Items.Count > 0)
            {
                request.CurrencyLocal = request.Items[0].CurrencyID;
            }

            if (string.IsNullOrWhiteSpace(request.CurrencyLocal))
            {
                request.CurrencyLocal = TextUtils.GetMasterCurrency();
            }

            foreach (PostingInvoiceItemDto item in request.Items)
            {
                if (string.IsNullOrWhiteSpace(item.CurrencyID))
                {
                    item.CurrencyID = request.CurrencyLocal;
                }

                if (item.RoomTypeID <= 0 && !string.IsNullOrWhiteSpace(item.RoomType))
                {
                    var roomTypeList = RoomTypeBO.Instance.FindByAttribute("Code", item.RoomType);
                    if (roomTypeList != null && roomTypeList.Count > 0)
                    {
                        RoomTypeModel roomTypeInfo = (RoomTypeModel)roomTypeList[0];
                        item.RoomTypeID = roomTypeInfo.ID;
                    }
                }

                if (item.AmountNetForIptv == 0)
                {
                    item.AmountNetForIptv = item.Amount;
                }
            }
        }

        /// <summary>
        /// Tìm invoice number vừa tạo để FE hiển thị lại sau khi post thành công.
        /// </summary>
        private string FindLatestInvoiceNo(int reservationId, int folioNo, string confirmNo)
        {
            string whereClause;
            if (folioNo < 0)
            {
                string effectiveConfirmNo = FirstNotEmpty(confirmNo, GetReservationConfirmationNo(reservationId));
                whereClause = string.Format(
                    "f.ConfirmationNo = N'{0}' AND f.FolioNo = {1} AND ISNULL(f.IsMasterFolio, 0) = 1",
                    EscapeSqlLiteral(effectiveConfirmNo),
                    folioNo);
            }
            else
            {
                whereClause = string.Format(
                    "f.ReservationID = {0} AND f.FolioNo = {1} AND ISNULL(f.IsMasterFolio, 0) = 0",
                    reservationId,
                    folioNo);
            }

            string sql = string.Format(@"
                SELECT TOP 1 fd.InvoiceNo
                FROM FolioDetail fd WITH (NOLOCK)
                INNER JOIN Folio f WITH (NOLOCK) ON f.ID = fd.FolioID
                WHERE {0}
                  AND fd.PostType = 3
                  AND fd.RowState = 1
                ORDER BY fd.ID DESC", whereClause);

            DataTable dt = TextUtils.Select(sql);
            if (dt != null && dt.Rows.Count > 0)
            {
                return dt.Rows[0]["InvoiceNo"]?.ToString() ?? string.Empty;
            }

            return string.Empty;
        }

        /// <summary>
        /// Lấy confirmation number từ reservation hiện tại khi request chưa gửi lên.
        /// </summary>
        private string GetReservationConfirmationNo(int reservationId)
        {
            if (reservationId <= 0)
            {
                return string.Empty;
            }

            ReservationModel reservation = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(reservationId);
            return reservation?.ConfirmationNo ?? string.Empty;
        }

        /// <summary>
        /// Trả về giá trị chuỗi đầu tiên có dữ liệu để gom fallback cho request web.
        /// </summary>
        private string FirstNotEmpty(params string[] values)
        {
            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Escape giá trị chuỗi trước khi nội suy vào câu SQL đơn giản.
        /// </summary>
        private string EscapeSqlLiteral(string value)
        {
            return (value ?? string.Empty).Replace("'", "''");
        }
    }
}
