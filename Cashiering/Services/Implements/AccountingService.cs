using BaseBusiness.BO;
using BaseBusiness.Contants;
using BaseBusiness.Model;
using BaseBusiness.util;
using Cashiering.Dto;
using Cashiering.Services.Interfaces;
using DevExpress.Web;
using Microsoft.Data.SqlClient;

using System.Data;
using System.Data.SqlClient;
using SqlException = Microsoft.Data.SqlClient.SqlException;
using SqlParameter = Microsoft.Data.SqlClient.SqlParameter;


namespace Cashiering.Services.Implements
{
    public class AccountingService : IAccountingService
    {
        public DataTable AccountMaintence(string dateCheck, int arID, string folioNo, string isActive, string paymentOnly, string print, DateTime fromDate, DateTime toDate)
        {
            try
            {
                string fromDateString = fromDate.ToString("yyyy-MM-dd");
                string toDateString = toDate.ToString("yyyy-MM-dd");
                if (dateCheck == "")
                {
                    fromDateString = "";
                    toDateString = "";
                }
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@ARID", arID),
                    new SqlParameter("@FolioNo", folioNo),
                    new SqlParameter("@IsActive", isActive),
                    new SqlParameter("@PaymentOnly",paymentOnly),
                    new SqlParameter("@Print",print),
                    new SqlParameter("@FromDate",fromDateString ),
                    new SqlParameter("@ToDate",  toDateString),
                };

                DataTable myTable = DataTableHelper.getTableData("spARAccountReceivableTransSearch", param);
                return myTable;
            }
            catch (SqlException ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }

        public DataTable AccountSearch(string accountName, string accountNo, int accountType, string balance)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@AccountName", accountName ?? ""),

                     new SqlParameter("@AccountNo", accountNo ?? ""),
                    new SqlParameter("@AccountTypeID", accountType),
                     new SqlParameter("@Balance", balance ?? ""),



                };

                DataTable myTable = DataTableHelper.getTableData("spARAccountReceivableSearch", param);
                return myTable;
            }
            catch (SqlException ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }

        public DataTable InvoiceSearch(int folioID, int mode)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@FolioID", folioID),
                    new SqlParameter("@Mode", mode),

                };

                DataTable myTable = DataTableHelper.getTableData("spSearchTransactionInFolioByDev ", param);
                return myTable;
            }
            catch (SqlException ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }

        public DataTable SearchByCommmand(string sqlCommand)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@sqlCommand", sqlCommand),

                };

                DataTable myTable = DataTableHelper.getTableData("spSearchAllForTrans", param);
                return myTable;
            }
            catch (SqlException ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }

        public DataTable SearchInfoAR(string accountName, string accountNo, string folioNo, string isActive, string folioID)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@AccountName", accountName),
                    new SqlParameter("@AccountNo", accountNo),
                    new SqlParameter("@FolioNo", folioNo),
                    new SqlParameter("@IsActive", isActive),
                    new SqlParameter("@FolioID", folioID),

                };

                DataTable myTable = DataTableHelper.getTableData("spARSearchInfo", param);
                return myTable;
            }
            catch (SqlException ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }
        public DataTable AccountTypeData()
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {

                };

                DataTable myTable = DataTableHelper.getTableData("spSearchARAccountType", param);
                return myTable;
            }
            catch (SqlException ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }

        public DataTable AROpeningData()
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {


                };

                DataTable myTable = DataTableHelper.getTableData("spSearchAROldsBalances", param);
                return myTable;
            }
            catch (SqlException ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }
        public DataTable ARTracesData()
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                     new SqlParameter("@ids", ""),

                };

                DataTable myTable = DataTableHelper.getTableData("spARSearchTrace", param);
                return myTable;
            }
            catch (SqlException ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }
        public DataTable ARAccountReceivableSearch()
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                     new SqlParameter("@AccountName", ""),
                         new SqlParameter("@AccountNo", ""),
                             new SqlParameter("@AccountTypeID", ""),
                                 new SqlParameter("@Balance", ""),
                };

                DataTable myTable = DataTableHelper.getTableData("spARAccountReceivableSearch", param);
                return myTable;
            }
            catch (SqlException ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }

        public ApiResponseAddError<ValidationErrorDto> SaveARAccount(SaveARAccountRequestDto dto)
        {
            var errors = new List<ValidationErrorDto>();
            ProcessTransactions pt = new ProcessTransactions();
            DateTime businessDate = TextUtils.GetBussinessDateTime();

            try
            {
                // 1. Validation logic
                errors.AddRange(ValidateARAccount(dto));
                if (errors.Any()) return ValidationFail(errors);

                // 2. Tìm Profile gốc (Bắt buộc phải có Profile mới tạo được AR)
                ProfileModel profile = (ProfileModel)ProfileBO.Instance.FindByPrimaryKey(dto.ProfileId);
                if (profile == null || profile.ID == 0)
                    return ValidationFailNotFound("Profile");

                pt.OpenConnection();
                pt.BeginTransaction();

                ARAccountReceivableModel model;

                // 3. Xử lý Update hoặc Insert
                if (dto.Id > 0)
                {
                    model = (ARAccountReceivableModel)ARAccountReceivableBO.Instance.FindByPrimaryKey(dto.Id);
                    if (model == null || model.ID == 0)
                    {
                        pt.RollBack();
                        return ValidationFailNotFound("AR Account Receivable");
                    }
                    model.UpdatedBy = dto.UserName;
                    model.UpdatedDate = businessDate;
                }
                else
                {
                    model = new ARAccountReceivableModel
                    {
                        CreatedBy = dto.UserName,
                        CreatedDate = businessDate,
                        UpdatedBy = dto.UserName,
                        UpdatedDate = businessDate
                    };
                }

                // 4. Mapping dữ liệu từ DTO sang Model
                MapToModel(model, dto, profile);

                // 5. Lưu vào Database
                if (dto.Id > 0)
                    ARAccountReceivableBO.Instance.Update(model);
                else
                    ARAccountReceivableBO.Instance.Insert(model);

                pt.CommitTransaction();

                return new ApiResponseAddError<ValidationErrorDto>
                {
                    Success = true,
                    Message = dto.Id > 0 ? "AR Account updated successfully" : "AR Account created successfully"
                };
            }
            catch (Exception ex)
            {
                pt.RollBack();
                return new ApiResponseAddError<ValidationErrorDto>
                {
                    Success = false,
                    Message = "Unexpected error occurred",
                    Error = ex.Message
                };
            }
            finally
            {
                pt.CloseConnection();
            }
        }



        #region Helper Methods (Validation & Mapping)

        private List<ValidationErrorDto> ValidateARAccount(SaveARAccountRequestDto dto)
        {
            var errors = new List<ValidationErrorDto>();

            if (dto.ProfileId <= 0)
                errors.Add(new ValidationErrorDto { Field = "profileAddText", Message = "Please select a Profile" });

            if (string.IsNullOrWhiteSpace(dto.AccountNumber))
                errors.Add(new ValidationErrorDto { Field = "accountNumberAdd", Message = "Account Number is required" });

            if (dto.AccountType <= 0)
                errors.Add(new ValidationErrorDto { Field = "accountTypeAdd", Message = "Please select an Account Type" });

            return errors;
        }

        private void MapToModel(ARAccountReceivableModel model, SaveARAccountRequestDto dto, ProfileModel profile)
        {
            model.AccountNo = dto.AccountNumber;
            model.AccountTypeID = dto.AccountType;
            model.CreditLimit = dto.CreditLimit;
            model.CurrencyID = "VND"; // Theo yêu cầu code cũ là VND
            model.ProfileID = profile.ID;
            model.AccountName = profile.Account; // Lấy tên từ Profile
            model.ContactName = dto.Contact ?? "";
            model.TelePhone = dto.Phone ?? "";
            model.Fax = dto.Fax ?? "";
            model.Email = dto.Email ?? "";
            model.Address1 = dto.Address1 ?? "";
            model.Address2 = dto.Address2 ?? "";
            model.Address3 = dto.Address3 ?? "";
            model.CityID = dto.City;
            model.PostalCode = dto.PostalCode ?? "";
            model.CountryID = dto.Country;
            model.State = "";
            model.Description = dto.Description ?? "";
            model.StatusFlagged = dto.Flagged;
            model.StatusInactive = dto.Inactive;
            model.PaymentDueDays = dto.PaymentDue;
        }

        private static ApiResponseAddError<ValidationErrorDto> ValidationFail(List<ValidationErrorDto> errors)
        {
            return new ApiResponseAddError<ValidationErrorDto> { Success = false, Message = "Validation failed", Errors = errors };
        }

        private static ApiResponseAddError<ValidationErrorDto> ValidationFailNotFound(string entityName)
        {
            return new ApiResponseAddError<ValidationErrorDto> { Success = false, Message = $"{entityName} not found" };
        }

        #endregion
    }
}
