using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Administration.Services.Interfaces
{
    public interface IHouseKeepingAdminService
    {
        public DataTable FacilityCodeData(string code, int isActive);
        public DataTable FacilityCategoryData(string code, string name, int isActive);
        public DataTable SectionData(string code, string description, int isActive);
        public DataTable HouseKeepingEmployeeData(string description, int isActive);
    }
}
