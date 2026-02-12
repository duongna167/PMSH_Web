using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using Microsoft.Data.SqlClient;
using Reservation.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reservation.Services.Implements
{
    public class GroupReservationService : IGroupReservationService
    {
        public DataTable GetGroupReservation(DateTime fromDate, DateTime toDate, int noOfRoom)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@FromDate", fromDate),
                    new SqlParameter("@ToDate", toDate),
                    new SqlParameter("@NoOfRoom", noOfRoom.ToString()),

                };

                DataTable myTable = DataTableHelper.getTableData("spRptGroupReservationReport", param);
                return myTable;
            }
            catch (SqlException ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }
        public DataTable GetRoomTypeAvailableDetail(DateTime fromDate)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@Date", fromDate),

                };

                DataTable myTable = DataTableHelper.getTableData("spAvailableRoomDetail", param);
                return myTable;
            }
            catch (SqlException ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }
        public decimal CalculatePriceFromNet(decimal priceAfter, string transactionCode)
        {
            try
            {
                decimal svc = 0;
                decimal vat = 0;
                decimal room = 0;

                List<GenerateTransactionModel> generateTransactionModels = PropertyUtils
                    .ConvertToList<GenerateTransactionModel>(GenerateTransactionBO.Instance.FindAll())
                    .Where(x => x.TransactionCode == transactionCode)
                    .ToList();

                #region Lấy phần trăm room, svc, vat
                foreach (var item in generateTransactionModels)
                {
                    if (item.SubgroupCode == "RR")
                        room = item.Percentage;
                    if (item.SubgroupCode == "SVC")
                        svc = item.Percentage;
                    if (item.SubgroupCode == "Tax")
                        vat = item.Percentage;
                }
                #endregion

                decimal svcRate = svc / 100m;
                decimal vatRate = vat / 100m;

                decimal originalPrice = priceAfter / ((1 + svcRate) * (1 + vatRate));
                return originalPrice;
            }
            catch (SqlException ex)
            {
                throw new Exception($"Error: {ex.Message}", ex);
            }
        }

    }
}
