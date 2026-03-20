using BaseBusiness.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Billing.Services.Interfaces
{
    public interface IPostService
    {
        /// <summary>
        /// DatVP: Tính net cho fixed charge
        /// </summary>
        /// <param name="transactionCode"> code transaction</param>
        /// <param name="price">giá trị tiền</param>
        /// <returns>Giá trị net</returns>
        decimal CalculateNet(string transactionCode, decimal price);


        /// <summary>
        /// DatVP: Tính price cho fixed charge
        /// </summary>
        /// <param name="transactionCode"> code transaction</param>
        /// <param name="price">giá trị tiền</param>
        /// <returns>Giá trị net</returns>
        decimal CalculatePrice(string transactionCode, decimal price);

        decimal CalculatePriceNet(string transactionCode, decimal grossPrice);
        decimal CalculatePricePlusPlus(string transactionCode, decimal netPrice);

        DataTable TransactionDetail(int invoiceNo);

    }
}
