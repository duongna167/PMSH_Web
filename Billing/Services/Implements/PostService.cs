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
        public decimal CalculatePricePlusPlus(string transactionCode, decimal netPrice)
        {
            try
            {
                decimal svcPercent = 0;
                decimal vatPercent = 0;

                var transactionConfigs = PropertyUtils.ConvertToList<GenerateTransactionModel>(GenerateTransactionBO.Instance.FindAll())
                                        .Where(x => x.TransactionCode == transactionCode).ToList();

                if (transactionConfigs.Count > 0)
                {
                    foreach (var item in transactionConfigs)
                    {
                        if (item.SubgroupCode == "SVC") svcPercent = item.Percentage;
                        if (item.SubgroupCode == "Tax") vatPercent = item.Percentage;
                    }
                }

                decimal amountSVC = netPrice * (svcPercent / 100m);

                decimal amountVAT = (netPrice + amountSVC) * (vatPercent / 100m);

                decimal grossPrice = netPrice + amountSVC + amountVAT;

                return Math.Round(grossPrice, 2); //làm tròn 2 số thập phân
            }
            catch (Exception ex)
            {
                throw new Exception($"Error CalculatePricePlusPlus: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Phuc Hàm tính từ Giá ++ (Gross) về Giá Net
        /// </summary>
        public decimal CalculatePriceNet(string transactionCode, decimal grossPrice)
        {
            try
            {
                decimal svcPercent = 0;
                decimal vatPercent = 0;

                var transactionConfigs = PropertyUtils.ConvertToList<GenerateTransactionModel>(GenerateTransactionBO.Instance.FindAll())
                                        .Where(x => x.TransactionCode == transactionCode).ToList();

                if (transactionConfigs.Count > 0)
                {
                    foreach (var item in transactionConfigs)
                    {
                        if (item.SubgroupCode == "SVC") svcPercent = item.Percentage;
                        if (item.SubgroupCode == "Tax") vatPercent = item.Percentage;
                    }
                }

                // (Net + SVC) = Gross / (1 + VAT%)
                decimal priceWithoutVAT = grossPrice / (1 + (vatPercent / 100m));

                // Net = (Net + SVC) / (1 + SVC%)
                decimal netPrice = priceWithoutVAT / (1 + (svcPercent / 100m));

                return Math.Round(netPrice, 2);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error CalculateNet: {ex.Message}", ex);
            }
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
