using BaseBusiness.bc;
using BaseBusiness.Facade;
using BaseBusiness.Model;
using BaseBusiness.util;
using DevExpress.Xpo.DB.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.BO
{
    public class ProfileBO : BaseBO
    {
        private ProfileFacade facade = ProfileFacade.Instance;
        protected static ProfileBO instance = new ProfileBO();

        protected ProfileBO()
        {
            this.baseFacade = facade;
        }

        public static ProfileBO Instance
        {
            get { return instance; }
        }
        public static DataTable GetAllProfile(string code, string account, string firstName, string keyWord, string city, string type, bool showSaleInCharge)
        {
            if (string.IsNullOrEmpty(code))
            {
                code = "";
            }
            if (string.IsNullOrEmpty(account))
            {
                account = "";
            }
            if (string.IsNullOrEmpty(firstName))
            {
                firstName = "";
            }
            if (string.IsNullOrEmpty(keyWord))
            {
                keyWord = "";
            }
            if (string.IsNullOrEmpty(city))
            {
                city = "";
            }

            string typeS = "";
            if (type == "0")
            {
                typeS = "1";
            }
            if (string.IsNullOrEmpty(type))
            {
                typeS = "";
            }
            //Company
            else if (type == "1")
            {
                typeS = "2";
            }
            //Source
            else if (type == "2")
            {
                typeS = "3";
            }
            //Individual
            else if (type == "3")
            {
                typeS = "0";
            }
            //Group
            else if (type == "4")
            {
                typeS = "4";
            }
            //Contact
            else if (type == "5")
            {
                typeS = "5";
            }
            //All
            else if (type == "6")
            {
                typeS = "";
            }
            string _saleincharge = "";
            if (showSaleInCharge == true)
                _saleincharge = "true";
            SqlParameter[] param = new SqlParameter[]
                         {
                    new SqlParameter("@Code", code),
                    new SqlParameter("@Account", account),
                    new SqlParameter("@FirstName", firstName),
                    new SqlParameter("@Keyword", keyWord),
                    new SqlParameter("@City", city),
                    new SqlParameter("@Type", typeS),
                    new SqlParameter("@ShowSaleInCharge", _saleincharge),
            };
            DataTable myTable = DataTableHelper.getTableData("spProfileSearch_ALL", param);
            return myTable;
        }

        public static (DataTable, int) GetAllProfileTest(string code, string account, string firstName, string keyWord, string city, string type, bool showSaleInCharge, int skip, int take)
        {
            if (string.IsNullOrEmpty(code))
            {
                code = "";
            }
            if (string.IsNullOrEmpty(account))
            {
                account = "";
            }
            if (string.IsNullOrEmpty(firstName))
            {
                firstName = "";
            }
            if (string.IsNullOrEmpty(keyWord))
            {
                keyWord = "";
            }
            if (string.IsNullOrEmpty(city))
            {
                city = "";
            }

            string typeS = "";
            if (type == "0")
            {
                typeS = "0";
            }
            if (string.IsNullOrEmpty(type))
            {
                typeS = "";
            }
            //Company
            else if (type == "1")
            {
                typeS = "1";
            }
            //Source
            else if (type == "2")
            {
                typeS = "2";
            }
            //Individual
            else if (type == "3")
            {
                typeS = "3";
            }
            //Group
            else if (type == "4")
            {
                typeS = "4";
            }
            //Contact
            else if (type == "5")
            {
                typeS = "5";
            }
            //All
            else if (type == "6")
            {
                typeS = "";
            }
            string _saleincharge = "";
            if (showSaleInCharge == true)
                _saleincharge = "true";

            //string _saleincharge = showSaleInCharge ? "1" : "";

            SqlParameter[] param = new SqlParameter[]
                         {
                    new SqlParameter("@Code", code),
                    new SqlParameter("@Account", account),
                    new SqlParameter("@FirstName", firstName),
                    new SqlParameter("@Keyword", keyWord),
                    new SqlParameter("@City", city),
                    new SqlParameter("@Type", typeS),
                    new SqlParameter("@ShowSaleInCharge", _saleincharge),
                    new SqlParameter("@Skip", skip),
                    new SqlParameter("@Take", take)
            };
            DataSet ds = DataTableHelper.GetDataSet("spProfileSearch_Test", param);
            DataTable myTable = ds.Tables[0];
            int totalCount = Convert.ToInt32(ds.Tables[1].Rows[0][0]);
            return (myTable, totalCount);
        }

        public static (DataTable, int) GetAllProfile2(string code, string account, string firstName, string keyWord, string city, int type, bool showSaleInCharge, int page, int pageSize)
        {
            code = code ?? "";
            account = account ?? "";
            firstName = firstName ?? "";
            keyWord = keyWord ?? "";
            city = city ?? "";

            //string typeS = type switch
            //{
            //    0 => "1", // Corporate
            //    1 => "2", // Company
            //    2 => "3", // Source
            //    3 => "0", // Individual
            //    4 => "4", // Group
            //    5 => "5", // Contact
            //    6 => "",  // All
            //    _ => ""
            //};

            string _saleInCharge = showSaleInCharge ? "true" : "";

            SqlParameter[] param = new SqlParameter[]
            {
        new SqlParameter("@Code", code),
        new SqlParameter("@Account", account),
        new SqlParameter("@FirstName", firstName),
        new SqlParameter("@Keyword", keyWord),
        new SqlParameter("@City", city),
        new SqlParameter("@Type", type),
        new SqlParameter("@ShowSaleInCharge", _saleInCharge),
        new SqlParameter("@Page", page),
        new SqlParameter("@PageSize", pageSize)
            };

            DataSet dataSet = DataTableHelper.GetDataSet("spProfileSearch_ALL2", param);
            DataTable myTable = dataSet.Tables[0];
            int totalCount = Convert.ToInt32(dataSet.Tables[1].Rows[0][0]);
            return (myTable, totalCount);
        }

        public static DataTable GetAllProfileUpdate(DateTime date, string confirmationNo, string roomNo, string name)
        {
            DateTime businessDate = TextUtils.GetBusinessDate();

            if (date == null)
            {
                date = businessDate;
            }

            if (string.IsNullOrEmpty(confirmationNo))
            {
                confirmationNo = "";
            }
            if (string.IsNullOrEmpty(roomNo))
            {
                roomNo = "";
            }
            if (string.IsNullOrEmpty(name))
            {
                name = "";
            }

            SqlParameter[] param = new SqlParameter[]
                         {
                    new SqlParameter("@Date", date),
                    new SqlParameter("@ConfirmationNo", confirmationNo),
                    new SqlParameter("@RoomNo", roomNo),
                    new SqlParameter("@Name", name)
            };
            DataTable myTable = DataTableHelper.getTableData("spProfileUpdateDateSearch", param);
            return myTable;
        }

        public static List<ProfileModel> GetListProfileByBOD(DateTime fromDate, DateTime toDate)
        {
            // Định dạng ngày thành YYYY-MM-DD
            string toDateStr = toDate.ToString("yyyy-MM-dd");
            string fromDateStr = fromDate.ToString("yyyy-MM-dd");

            string query = $"SELECT * FROM Profile WHERE CAST(DateOfBirth AS DATE) >= CAST('{fromDateStr}' AS DATE) AND CAST(DateOfBirth AS DATE) <= CAST('{toDateStr}' AS DATE) ORDER BY id DESC";
            return instance.GetList<ProfileModel>(query);
        }
        public string GenerateNo3(string code)
        {
            IConfiguration config = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false)
        .Build();

            string strcon = config.GetConnectionString("DefaultConnection");
            string tableName = "Profile";
            string sql = "SELECT TOP 1 MAX(Convert(int," + code + ")) FROM " + tableName + " with (nolock) ";
            string lastBillNo = "";
            SqlConnection conn = new SqlConnection(strcon);
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;
            SqlDataReader reader = null;
            ArrayList result = new ArrayList();
            try
            {
                conn.Open();
                reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                if (reader.Read())
                {
                    lastBillNo = reader[0].ToString();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                conn.Close();
            }
            if (lastBillNo.Length == 0)
            {
                return "00001";
            }
            else
            {
                string digitPart = "", stringPart = lastBillNo, newDigitPart;
                int i = lastBillNo.Length - 1;
                while (i >= 0)
                {
                    try
                    {
                        Convert.ToInt32(lastBillNo.Substring(i, 1));
                        digitPart = lastBillNo.Substring(i, 1) + digitPart;
                        i--;
                    }
                    catch
                    {
                        break;
                    }
                }
                if (digitPart.Length > 0)
                {
                    stringPart = lastBillNo.Substring(0, i + 1);
                    newDigitPart = Convert.ToString(Convert.ToInt32(digitPart) + 1);
                    switch (newDigitPart.Length)
                    {
                        case 1:
                            newDigitPart = "0000" + newDigitPart;
                            break;
                        case 2:
                            newDigitPart = "000" + newDigitPart;
                            break;
                        case 3:
                            newDigitPart = "00" + newDigitPart;
                            break;
                        case 4:
                            newDigitPart = "0" + newDigitPart;
                            break;
                    }
                    return stringPart + newDigitPart;
                }
                else
                {
                    return lastBillNo + "00001";
                }

            }


        }


        // TuanDB: 18/3/2026 with chatGPT giữ nghiệp vụ gần GenerateNo3 nhưng tránh lỗi convert và dễ dùng hơn
        public string GenerateNo4(string columnName)
        {
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            string strcon = config.GetConnectionString("DefaultConnection");
            string tableName = "Profile";

            // Chỉ lấy các giá trị code hiện có, không convert ở SQL để tránh lỗi dữ liệu lẫn chữ.
            string sql = $"SELECT {columnName} FROM {tableName} WITH (NOLOCK) WHERE {columnName} IS NOT NULL AND LTRIM(RTRIM({columnName})) <> ''";

            List<string> codes = new List<string>();

            using (SqlConnection conn = new SqlConnection(strcon))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.CommandType = CommandType.Text;

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        codes.Add(reader[0].ToString());
                    }
                }
            }

            // Không có dữ liệu thì trả về mặc định như logic cũ
            if (codes.Count == 0)
            {
                return "00001";
            }

            string bestPrefix = "";
            int bestNumber = 0;
            int bestDigitLength = 5;

            foreach (string code in codes)
            {
                if (string.IsNullOrWhiteSpace(code))
                    continue;

                string trimmedCode = code.Trim();

                // Tách phần số ở cuối chuỗi
                int index = trimmedCode.Length - 1;
                while (index >= 0 && char.IsDigit(trimmedCode[index]))
                {
                    index--;
                }

                string prefix = trimmedCode.Substring(0, index + 1);
                string digitPart = trimmedCode.Substring(index + 1);

                // Nếu không có số cuối chuỗi thì bỏ qua ở vòng so sánh,
                // vì logic cũ chỉ tăng được khi có phần số
                if (string.IsNullOrEmpty(digitPart))
                    continue;

                if (!int.TryParse(digitPart, out int number))
                    continue;

                // Chọn mã có giá trị số lớn nhất
                if (number > bestNumber)
                {
                    bestNumber = number;
                    bestPrefix = prefix;
                    bestDigitLength = digitPart.Length;
                }
            }

            // Nếu toàn bộ dữ liệu không có phần số cuối chuỗi
            // thì giữ hành vi gần giống no3: lấy chuỗi đầu tiên + 00001
            if (bestNumber == 0 && string.IsNullOrEmpty(bestPrefix))
            {
                string firstCode = codes.First().Trim();
                return firstCode + "00001";
            }

            int nextNumber = bestNumber + 1;
            string newDigitPart = nextNumber.ToString().PadLeft(bestDigitLength, '0');

            return bestPrefix + newDigitPart;
        }

        public bool IsDuplicateCode(string code, long id = 0)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            return IsDuplicateCode(
             "Profile",
             "Code",
             code.Trim(),
             id
            );

        }

    }
}
