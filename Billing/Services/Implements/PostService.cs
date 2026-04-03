using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using Billing.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Billing.Services.Implements
{
    public class PostService : IPostService
    {
        public decimal CalculateNet(string transactionCode, decimal price)
        {
            try
            {
                decimal svc = 0;
                decimal vat = 0;
                decimal room = 0;
                List<GenerateTransactionModel> generateTransactionModels = PropertyUtils.ConvertToList<GenerateTransactionModel>(GenerateTransactionBO.Instance.FindAll()).
                Where(x => x.TransactionCode == transactionCode).ToList();
                #region lấy ra phần trăm giá room, svc và vat
                if (generateTransactionModels.Count > 0)
                {
                    foreach (var item in generateTransactionModels)
                    {
                        if (item.SubgroupCode == "R_MISC")
                        {
                            room = item.Percentage;
                        }
                        if (item.SubgroupCode == "SVC")
                        {
                            svc = item.Percentage;
                        }
                        if (item.SubgroupCode == "Tax")
                        {
                            vat = item.Percentage;
                        }
                    }
                }
                #endregion

                #region tính giá trị net fiexed charge
                decimal vatPrice = price * (vat / 100); 
                decimal svcPrice = price * (svc / 100); 
                decimal priceAfter = price + vatPrice + svcPrice;
                #endregion

                return priceAfter;
            }
            catch (SqlException ex)
            {

                throw new Exception($"Error: {ex.Message}", ex);
            }
        }

        public decimal CalculatePrice(string transactionCode, decimal price)
        {
            try
            {
                decimal svc = 0;
                decimal vat = 0;
                List<GenerateTransactionModel> generateTransactionModels = PropertyUtils.ConvertToList<GenerateTransactionModel>(GenerateTransactionBO.Instance.FindAll()).
                Where(x => x.TransactionCode == transactionCode).ToList();
                #region lấy ra phần trăm svc và vat
                if (generateTransactionModels.Count > 0)
                {
                    foreach (var item in generateTransactionModels)
                    {

                        if (item.SubgroupCode == "SVC")
                        {
                            svc = item.Percentage;
                        }
                        if (item.SubgroupCode == "Tax")
                        {
                            vat = item.Percentage;
                        }
                    }
                }
                #endregion

                #region tính giá trị net fiexed charge
                decimal vatPrice = price * (vat / 100) / (1 + (vat / 100));
                decimal svcPrice = (price - vatPrice) * (svc / 100) / (1 + (svc / 100));
                decimal priceAfter = price - vatPrice - vatPrice;
                #endregion

                return priceAfter;
            }
            catch (SqlException ex)
            {

                throw new Exception($"Error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Phuc Hàm tính từ Giá Net ra Giá ++ (Gross)
        /// </summary>
        public decimal CalculatePricePlusPlus(string transactionCode, decimal netPrice, string currency = "VND")
        {
            try
            {
                var (svcPercent, vatPercent) = GetTaxConfig(transactionCode);

                decimal svc = netPrice * svcPercent / 100m;
                decimal vat = (netPrice + svc) * vatPercent / 100m;

                decimal gross = netPrice + svc + vat;

                return currency == "VND"
                    ? Math.Round(gross, 0)
                    : Math.Round(gross, 2);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error CalculatePricePlusPlus: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Phuc Hàm tính từ Giá ++ (Gross) về Giá Net
        /// </summary>
        public decimal CalculatePriceNet(string transactionCode, decimal grossPrice, string currency = "VND")
        {
            try
            {
                var (svcPercent, vatPercent) = GetTaxConfig(transactionCode);

                decimal divisor = (1 + svcPercent / 100m) * (1 + vatPercent / 100m);

                decimal net = grossPrice / divisor;

                return currency == "VND"
                    ? Math.Round(net, 0)
                    : Math.Round(net, 2);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error CalculatePriceNet: {ex.Message}", ex);
            }
        }

        private (decimal svcPercent, decimal vatPercent) GetTaxConfig(string transactionCode)
        {
            decimal svcPercent = 0;
            decimal vatPercent = 0;

            var configs = PropertyUtils.ConvertToList<GenerateTransactionModel>(
                GenerateTransactionBO.Instance.FindByAttribute("TransactionCode", transactionCode)
            );

            foreach (var item in configs)
            {
                var sub = (item.SubgroupCode ?? "").ToUpper().Trim();

                if (sub.Contains("SVC") || sub.Contains("SV") || sub.Contains("SC"))
                    svcPercent = item.Percentage;

                if (sub.Contains("VAT") || sub.Contains("TAX"))
                    vatPercent = item.Percentage;
            }

            return (svcPercent, vatPercent);
        }

        public DataTable TransactionDetail(int invoiceNo)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@InvoiceNo", invoiceNo)
            };

            DataTable myTable = DataTableHelper.getTableData("spSearchTransactionDetailInFolioByDev", param);
            return myTable;
        }
    }
}
