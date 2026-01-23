using Administration.Services.Interfaces;
using BaseBusiness.util;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Administration.Services.Implements
{
    public class HouseKeepingAdminService: IHouseKeepingAdminService
    {
        public DataTable FacilityCodeData(string code, int isActive)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@Code", code),
                    new SqlParameter("@Inactive", isActive),
                };
                DataTable myTable = DataTableHelper.getTableData("spFrmhkpFacilityCodeSearch", param);
                return myTable;
            }
            catch (SqlException ex)
            {
                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }
        public DataTable FacilityCategoryData(string code, string name, int isActive)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@Code", code),
                    new SqlParameter("@Name", name),
                    new SqlParameter("@Inactive", isActive),
                };
                DataTable myTable = DataTableHelper.getTableData("spFrmhkpFacilityCategorySearch", param);
                return myTable;
            }
            catch (SqlException ex)
            {
                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }
        public DataTable SectionData(string code, string description, int isActive)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@Code", code),
                                        new SqlParameter("@Name", description),
                    new SqlParameter("@Inactive", isActive),
                };
                DataTable myTable = DataTableHelper.getTableData("spFrmhkpSectionSearch", param);
                return myTable;
            }
            catch (SqlException ex)
            {
                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }
        public DataTable HouseKeepingEmployeeData( string description, int isActive)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@Name", description),
                                        new SqlParameter("@Code", description),
                    new SqlParameter("@Inactive", isActive),
                };
                DataTable myTable = DataTableHelper.getTableData("spFrmhkpEmployeeSearch", param);
                return myTable;
            }
            catch (SqlException ex)
            {
                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }
    }
}
