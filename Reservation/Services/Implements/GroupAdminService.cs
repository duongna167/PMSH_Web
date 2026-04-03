using BaseBusiness.BO;
using BaseBusiness.util;
using DevExpress.XtraGauges.Core.Base;
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
    public class GroupAdminService : IGroupAdminService
    {
        public DataTable SearchGroupAdmin(string ConfirmationNo, string DisplayStattus, string Name, string RoomNo)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@ConfirmationNo", ConfirmationNo),
                     new SqlParameter("@Sorting", 1),
                    new SqlParameter("@DisplayStatus", DisplayStattus),

                     new SqlParameter("@Name", Name ?? ""),
                     new SqlParameter("@RoomNo", RoomNo ?? ""),
                    new SqlParameter("@MasterFolio", ""),

                };

                DataTable myTable = DataTableHelper.getTableData("spReservationSearchConfNo", param);


                return myTable;
            }
            catch (SqlException ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }

        public DataTable SearchGroupAdmin2(string ConfirmationNo,int Sorting, string DisplayStattus, string Name, string RoomNo)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@ConfirmationNo", ConfirmationNo),
                     new SqlParameter("@Sorting", Sorting),
                    new SqlParameter("@DisplayStatus", DisplayStattus),

                     new SqlParameter("@Name", Name ?? ""),
                     new SqlParameter("@RoomNo", RoomNo ?? ""),
                    new SqlParameter("@MasterFolio", ""),

                };

                DataTable myTable = DataTableHelper.getTableData("spReservationSearchConfNo", param);


                return myTable;
            }
            catch (SqlException ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }

        public DataTable SearchGroupCheckInRoom(string ConfirmationNo, string Inspected, string Clean, string AllRooms, string CleanAndInspected)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@ConfirmationNo", ConfirmationNo),
                     new SqlParameter("@Inspected", Inspected),
                                         new SqlParameter("@Clean", Clean),

                     new SqlParameter("@AllRooms", AllRooms),
                     new SqlParameter("@CleanAndInspected", CleanAndInspected),
                };

                DataTable myTable = DataTableHelper.getTableData("spGroupCheckInByConfirmationNo", param);


                return myTable;
            }
            catch (SqlException ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }

        public DataTable spReservationSearchByConfirmationNo(string ConfirmationNo)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@ConfirmationNo", ConfirmationNo),

                };

                DataTable myTable = DataTableHelper.getTableData("spReservationSearchByConfirmationNo", param);
                return myTable;
            }
            catch (SqlException ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }
    }
}
