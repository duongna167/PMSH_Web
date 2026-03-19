
using BaseBusiness.BO;
using BaseBusiness.Model;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data;
//using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlDataAdapter = Microsoft.Data.SqlClient.SqlDataAdapter;
using SqlException = Microsoft.Data.SqlClient.SqlException;
using SqlParameter = Microsoft.Data.SqlClient.SqlParameter;
using SqlTransaction = Microsoft.Data.SqlClient.SqlTransaction;
namespace BaseBusiness.util
{
	/// <summary>
	/// Summary description for TextUtils.
	/// </summary>
	public class TextUtils
	{
		public TextUtils()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		public string HistoryContent = "";
		int CountTransaction = 0;

		protected string strcon;
		private readonly SqlConnection cnn;
		private Microsoft.Data.SqlClient.SqlTransaction tran;
		//private SqlDataAdapter da;
		public SqlConnection Connection => cnn;
		public SqlTransaction Transaction => tran;

		public static DateTime dtNull = DateTime.Parse("01/01/1900");

		public static string ToString(Object obj)
		{
			return (obj == null) ? "" : obj.ToString();
		}
		public static int CompareDate(DateTime date1, DateTime date2)
		{
			if (date1.Day == date2.Day && date1.Month == date2.Month && date1.Year == date2.Year)
				return 0;
			if (date1.Year < date2.Year || (date1.Year == date2.Year && date1.Month < date2.Month) || (date1.Year == date2.Year && date1.Month == date2.Month && date1.Day < date2.Day))
				return -1;
			else
				return 1;

		}
		public static string[] GetArrayTransaction(string strRouting)
		{
			string strReturn = "";
			string[] array = strRouting.Trim().Split(',');
			for (int i = 0; i < array.Length; i++)
			{
				if (!array[i].Trim().Equals(""))
				{
					DataTable tb = Select("Select * from RoutingCode Where Code =N'" + array[i].ToString().Trim() + "'");
					if (tb.Rows.Count > 0)
					{
						if (strReturn == "")
							strReturn = tb.Rows[0]["TransactionCodes"].ToString().Trim();
						else
							strReturn = strReturn + "," + tb.Rows[0]["TransactionCodes"].ToString().Trim();
					}
					else
					{
						if (strReturn == "")
							strReturn = array[i].Trim();
						else
							strReturn = strReturn + "," + array[i].Trim();
					}
				}
			}
			string[] arrayReturn = strReturn.Split(',');

			return arrayReturn;
		}
		public static DateTime GetBussinessDateTime()
		{
			var businessDate = PropertyUtils
								.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll())![0]
								.BusinessDate;
			return businessDate;
		}

		public static decimal ExchangeCurrency(DateTime date, string FromCurrencyID, string ToCurrencyID, decimal Amount)
		{
			try
			{
				// S?a: Ch? truy?n tên SP và các SqlParameter
				DataTable dt = getTable("spExchangeCurrency",
					new SqlParameter("@DateTime", date),
					new SqlParameter("@FromCurrency", FromCurrencyID),
					new SqlParameter("@ToCurrency", ToCurrencyID),
					new SqlParameter("@Amount", Amount));

				if (dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value)
				{
					return Convert.ToDecimal(dt.Rows[0][0]);
				}

				return 0;
			}
			catch
			{
				return 0;
			}
		}

		private void SetHistory(string Table, string Function, string Content)
		{
			HistoryContent += "" + (CountTransaction + 1) + "." + Function + " " + Table + " : " + Content + "\r\n";
			CountTransaction++;
		}

		private static readonly string _connectionString;

		// Static constructor: ch?y m?t l?n duy nh?t khi class ???c dùng l?n ??u
		static TextUtils()
		{
			try
			{
				var builder = new ConfigurationBuilder()
					.SetBasePath(Directory.GetCurrentDirectory())
					.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

				IConfiguration configuration = builder.Build();

				_connectionString = configuration.GetConnectionString("DefaultConnection");

				if (string.IsNullOrEmpty(_connectionString))
				{
					throw new Exception("Can't not find Connection String 'DefaultConnection' trong appsettings.json");
				}
			}
			catch (Exception ex)
			{
				// Nếu không load ???c config, n�m l?i r� r�ng ?? d? debug
				throw new Exception("L?i kh?i t?o Connection String: " + ex.Message, ex);
			}
		}
		public static DataTable getTable2(
			string spName,
			string nameSetToTable,
			params SqlParameter[] parameters)
		{
			DataSet ds = new DataSet();

			using (SqlConnection conn = new SqlConnection(_connectionString))
			using (SqlCommand cmd = new SqlCommand(spName, conn))
			{
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.Parameters.AddRange(parameters);

				using (SqlDataAdapter da = new SqlDataAdapter(cmd))
				{
					da.Fill(ds, nameSetToTable);
				}
			}

			return ds.Tables[nameSetToTable];
		}

		public static DataTable getTable(string spName, params SqlParameter[] parameters)
		{
			DataTable dt = new DataTable();

			using (SqlConnection conn = new SqlConnection(_connectionString))
			using (SqlCommand cmd = new SqlCommand(spName, conn))
			{
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.Parameters.AddRange(parameters);

				using (SqlDataAdapter da = new SqlDataAdapter(cmd))
				{
					da.Fill(dt);
				}
			}

			return dt;
		}
		public static int CreateFolioAtNight(int ReservationID, int RoomID, string RoomNo, string ConfirmationNo, int WindowNo, int ProfileID, string ProfileName)
		{
			try
			{
				FolioModel mF = new FolioModel();
				mF.Status = false;
				mF.CreateDate = GetSystemDate();
				mF.FolioDate = GetBusinessDate();
				//mF.IsMasterFolio = false;
				mF.ConfirmationNo = ConfirmationNo;// ((ReservationModel)ReservationBO.Instance.FindByPK(ReservationID)).ConfirmationNo;
				mF.FolioNo = WindowNo;
				mF.ReservationID = ReservationID;
				mF.ProfileID = ProfileID;
				mF.AccountName = ProfileName;
				mF.UpdateDate = mF.CreateDate;
				mF.UserInsertID = Global.UserID;
				mF.UserUpdateID = Global.UserID;

				return (int)FolioBO.Instance.Insert(mF);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message);
			}
		}
		public static string GetHostName()
		{
			return System.Environment.MachineName; //System.Net.Dns.GetHostName();

		}
		public static int ToInt(string x)
		{
			try
			{
				return int.Parse(x);
			}
			catch (Exception e)
			{
				return -1;
			}
		}
		public static Decimal ToDecimal(string x)
		{
			try
			{
				return Decimal.Parse(x);
			}
			catch (Exception e)
			{
				return 0;
			}
		}

		public static string DateFormatEnVi(DateTime dtDate, int EnVi, string dtFormat)
		{
			//If Eng date, EnVi=1, if Vi date EnVi=0
			//dtFormat: dd/MM/yyyy,....
			CultureInfo culture;
			try
			{
				if (EnVi == 1)
					culture = new CultureInfo("en-us");
				else
					culture = new CultureInfo("vi-VN");

				return dtDate.ToString(dtFormat, culture);

			}
			catch (Exception e)
			{
				return "";
			}
		}

		public static long ToLong(string x)
		{
			try
			{
				return long.Parse(x);
			}
			catch (Exception e)
			{
				return -1;
			}
		}

		//TUANLA add this function
		public static System.Boolean IsDate(string strDate)
		{
			try
			{
				if (strDate.Length < 7)
					return false;
				System.DateTime dt = System.DateTime.Parse(strDate, new CultureInfo("vi-VN", true));
				return true;
			}
			catch
			{
				return false;
			}
		}
		public static System.Boolean IsNumeric(System.Object Expression)
		{
			if (Expression == null || Expression is DateTime)
				return false;

			if (Expression is Int16 || Expression is Int32 || Expression is Int64 || Expression is Decimal || Expression is Single || Expression is Double || Expression is Boolean)
				return true;

			try
			{
				if (Expression is string)
					Double.Parse(Expression as string);
				else
					Double.Parse(Expression.ToString());
				return true;
			}
			catch { } // just dismiss errors but return false
			return false;
		}

		public static Decimal ToReal(string x)
		{
			try
			{
				return Decimal.Parse(x);
			}
			catch (Exception e)
			{
				return 0;
			}
		}

		//		public static int EncodeIP(string ip)
		//		{
		//			string[] abc = ip.Split('.');
		//			int str1, str2, str3, str4, result;
		//			str1 = int.Parse(abc[0]) << 24;
		//			str2 = int.Parse(abc[1]) << 16;
		//			str3 = int.Parse(abc[2]) << 8;
		//			str4 = int.Parse(abc[3]);
		//			result = (int) (str1 + str2 + str3 + str4 - 2147483648);
		//
		//			return result;
		//		}
		//
		//		public static string DecodeIP(int hexIP)
		//		{
		//			long str1, str2, str3, str4;
		//			long temp = hexIP + 2147483648;
		//			str1 = temp & 0xFF;
		//			str2 = (temp >> 8) & 0xFF;
		//			str3 = (temp >> 16) & 0xFF;
		//			str4 = (temp >> 24) & 0xFF;
		//			return str4 + "." + str3 + "." + str2 + "." + str1;
		//		}

		private static DateTime EMPTY_DATE = new DateTime(1, 1, 1);
		private static DateTime VABS_MAX_DATE = DateTime.Parse("1/1/2099");
		private static DateTime MIN_DATE = DateTime.MinValue;
		private static DateTime MAX_DATE = DateTime.MaxValue;

		public static string FormatDate(DateTime date, string format)
		{
			//if(date.Equals("1/1/2099") || date.Equals("1/1/1")) return "N/A";
			//if (date.Equals(EMPTY_DATE) || date.Equals(MIN_DATE) || date.Equals(VABS_MAX_DATE) || date.Equals(MAX_DATE)) return "N/A";
			if (date.Year < 1000 || date.Year >= 2099) return "N/A";
			return date.ToString(format, DateTimeFormatInfo.InvariantInfo);
		}

		public static string FormatDate(DateTime date)
		{
			return FormatDate(date, "MMM dd, yyyy");
		}

		public static string FormatDateTime(DateTime date)
		{
			//return FormatDate(date, "U");
			return FormatDate(date, "MMM dd, yyyy HH:mm:ss");
		}

		public static string FormatDateToMonthAndYear(string date)
		{
			return FormatDate(ToDate(date), "MMM, yyyy");
		}

		public static string FormatDate(string date)
		{
			return FormatDate(ToDate(date), "MMM dd, yyyy");
		}
		public static string FormatDate4Invoice(DateTime date)
		{
			return FormatDate(date, "dd-MMM-yyyy");
		}
		public static string FormatDate4InvoicePeriod(DateTime date)
		{
			return FormatDate(date, "yyyy-MMM-dd").ToUpper();
		}
		public static DateTime ToDate(string date)
		{
			return DateTime.Parse(date, new CultureInfo("en-US", true));
		}

		public static string ToString1(DateTime date)
		{
			//return date.ToString("MMM, dd yyyy", new CultureInfo("en-US", true));
			return date.ToString("MM/dd/yyyy", new CultureInfo("en-US", true));
		}

		public static string ToStringVN(DateTime date)
		{
			return date.ToString("dd/MM/yyyy", new CultureInfo("vi-VN", true));
		}

		public static string GetFullMonth(DateTime date)
		{
			//return FormatDate(date, "U");
			return FormatDate(date, "MMMM");
		}

		public static string FormatDateToMonthNDay(DateTime date)
		{
			return FormatDate(date, "MMM dd");
		}

		public static string FormatDateToMonthNDayVN(DateTime date)
		{
			return "ngày " + date.ToString("dd MMMMMMM", new CultureInfo("vi-VN", true));
		}

		/// <summary>
		/// ////////////////////////////////////////////////////////////////////////////
		/// </summary>
		private static string[] Number_Patterns =
			new string[] { "{0:#,##0}", "{0:#,##0.0}", "{0:#,##0.00}", "{0:#,##0}.000", "{0:#,##0.0000}", "{0:#,##0.00000;#,##0.00000; }" };

		private static string[] Currency_Patterns =
			new string[] { "{0:$#,##0;($#,##0); }", "{0:$#,##0.0;($#,##0.0); }", "{0:$#,##0.00;($#,##0.00); }", "{0:$#,##0.000;($#,##0.000); }", "{0:$#,##0.0000;($#,##0.0000); }", "{0:$#,##0.00000;($#,##0.00000); }" };

		private static string[] VNCurrency_Patterns =
			new string[] { "{0:#,##0;(#,##0); }", "{0:#,##0.0;(#,##0.0); }", "{0:#,##0.00;(#,##0.00); }", "{0:#,##0.000;(#,##0.000); }", "{0:#,##0.0000;(#,##0.0000); }", "{0:#,##0.00000;(#,##0.00000); }" };


		public static string FormatNumber(Decimal x, int digits)
		{
			return String.Format(Number_Patterns[digits], x);
		}

		public static string FormatCurrency(Decimal x, int digits)
		{
			return String.Format(Currency_Patterns[digits], x);
			//return String.Format(VNCurrency_Patterns[digits], x);

		}

		public static string FormatNumberZeroToDash(Decimal x)
		{
			return String.Format("{0:#,##0.00;(#,##0.00); }", x);
		}

		public static string FormatPercentNumber(Decimal x)
		{
			return String.Format("{0:#0.00}%", x);
		}



		public static string SpaceIfEqualZero(Decimal x)
		{
			//return String.Format("{0:#0.00}%", x);
			if (x == 0)
				return "";
			else
				return x.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
		}
		public static DataTable Select(string strComm)
		{
			using (SqlConnection cnn = new SqlConnection(DBUtils.GetDBConnectionString()))
			{
				try
				{
					using (SqlCommand cmd = new SqlCommand(strComm, cnn))
					{
						cmd.CommandType = CommandType.Text;
						cmd.CommandTimeout = 0;

						using (SqlDataAdapter da = new SqlDataAdapter(cmd))
						{
							DataSet ds = new DataSet();
							cnn.Open();
							da.Fill(ds);
							return ds.Tables.Count > 0 ? ds.Tables[0] : new DataTable();
						}
					}
				}
				catch (SqlException se)
				{
					throw new Exception("Select error: " + se.Message);
				}
			}
		}
		public static string SalaryToString(Decimal x, params object[] optionalParamArray)
		{
			string displayType = "", strReturn = "";
			try
			{
				displayType = optionalParamArray.GetValue(0).ToString();
			}
			catch
			{
				displayType = "";
			}
			if (x == 0)
			{
				if (displayType == "1")
					strReturn = "N/A";
				else
					strReturn = "";

			}
			else
			{
				strReturn = FormatNumber(x, 0);
			}

			return strReturn.Replace(".", ",");
		}

		public static string SalaryList(string strx)
		{
			string displayType = "", strReturn = "";
			if (strx.Trim() == "") strx = "0";
			Decimal x = Decimal.Parse(strx);
			if (x == 0)
			{

				strReturn = "";

			}
			else
			{
				strReturn = FormatNumber(x, 0);
			}

			return strReturn.Replace(".", ",");
		}
		public static Decimal RoundForPort(Decimal x, Decimal nRound)
		{
			if (Math.Abs(x) <= nRound)
				return 0;
			else
				return x;

		}
		public static ArrayList SplitPrefixes(string rawPref)
		{
			ArrayList prefList = new ArrayList();
			try
			{
				string[] prefParts = rawPref.Split(';');
				for (int i = 0; i < prefParts.Length; i++)
				{
					string[] temp = prefParts[i].Split('-');
					//string t = temp[0];string t1 = prefParts[i];
					if (temp.Length == 1) //no "-"
					{
						if (!prefList.Contains(temp[0].Trim())) prefList.Add(temp[0].Trim());
					}
					else if (temp.Length == 2) //one "-"
					{
						int noOfPrefs = int.Parse(temp[1]) - int.Parse(temp[0]);
						for (int j = 0; j <= noOfPrefs; j++)
						{
							if (!prefList.Contains((int.Parse(temp[0]) + j).ToString().Trim()))
								prefList.Add((int.Parse(temp[0]) + j).ToString().Trim());
						}
					}
					else//more than one "-"
					{
						prefList.Clear();
					}
				}
			}
			catch
			{
				return prefList;
			}
			return prefList;
		}

		public static string[] SplitBillingIncrement(string rawIncr, out bool isValid)
		{
			isValid = false;
			Double result;
			string[] incrParts = rawIncr.Split('+');
			if (incrParts.Length != 2) return incrParts;
			for (int i = 0; i < incrParts.Length; i++)
			{
				if (!Double.TryParse(incrParts[i].ToString(), NumberStyles.Number, NumberFormatInfo.CurrentInfo, out result))
					return incrParts;
				if (result != int.Parse(result.ToString()) || result <= 0 || result > 60)
					return incrParts;
			}
			isValid = true;
			return incrParts;
		}

		public static string Error(string msg)
		{
			return "ERROR: " + msg;
		}

		public static string Warn(string msg)
		{
			return "WARNING: " + msg;
		}

		public static string ToHTML(NameValueCollection list)
		{
			StringBuilder r = new StringBuilder();
			foreach (string key in list.AllKeys)
			{
				r.Append("<span class=subtitle2>" + key + "</span>: " + list.Get(key) + "<br>");
			}
			return r.ToString();
		}

		public static bool NotNull(object obj)
		{
			if (obj == null) return false;
			if (obj is string & ((string)obj).Trim().Length == 0) return false;
			return true;
		}
		public static DateTime GetFirstDayOfWeek(DateTime day)
		{
			switch (day.DayOfWeek)
			{
				case DayOfWeek.Tuesday:
					return day.AddDays(-1);
				case DayOfWeek.Wednesday:
					return day.AddDays(-2);
				case DayOfWeek.Thursday:
					return day.AddDays(-3);
				case DayOfWeek.Friday:
					return day.AddDays(-4);
				case DayOfWeek.Saturday:
					return day.AddDays(-5);
				case DayOfWeek.Sunday:
					return day.AddDays(-6);
			}
			return day;
		}
		//Method to parse Text into HTML
		public static string parsetext(string text, bool allow)
		{
			//Create a StringBuilder object from the string input
			//parameter
			StringBuilder sb = new StringBuilder(text);
			//Replace all double white spaces with a single white space
			//and &nbsp;
			sb.Replace("  ", " &nbsp;");
			//Check if HTML tags are not allowed
			if (!allow)
			{
				//Convert the brackets into HTML equivalents
				sb.Replace("<", "&lt;");
				sb.Replace(">", "&gt;");
				//Convert the double quote
				sb.Replace("\"", "&quot;");
			}
			//Create a StringReader from the processed string of
			//the StringBuilder object
			StringReader sr = new StringReader(sb.ToString());
			StringWriter sw = new StringWriter();
			//Loop while next character exists
			while (sr.Peek() > -1)
			{
				//Read a line from the string and store it to a temp
				//variable
				string temp = sr.ReadLine();
				//write the string with the HTML break tag
				//Note here write method writes to a Internal StringBuilder
				//object created automatically
				sw.Write(temp + "<br>");
			}
			//Return the final processed text
			return sw.GetStringBuilder().ToString();
		}

		public static string To_DateFormat(string dtInput, string cultureInput)
		{
			try
			{
				if (cultureInput.ToUpper().Equals("US"))
				{
					CultureInfo culture = new CultureInfo("en-us");
					return DateTime.Parse(dtInput).ToString("dd-MMMM-yyyy", culture);
				}
				else
				{
					return DateTime.Parse(dtInput).ToString("dd/MM/yyyy");
				}

			}
			catch (Exception e)
			{
				return dtInput;
			}
		}

		public static string DecimalToStringNon(Decimal x)
		{
			//return String.Format("{0:#0.00}%", x);
			return x.ToString("000", System.Globalization.CultureInfo.InvariantCulture);
		}
		public static string FormatCurrencyVND(Decimal x)
		{

			//    return String.Format(CultureInfo.CreateSpecificCulture("en-us"),VNCurrency_Patterns[0], x);
			//return (x.ToString("0,0.00", CultureInfo.InvariantCulture));
			return (x.ToString("#,0.##", CultureInfo.InvariantCulture));
			//
			// String.Format("{0:0.##}"

		}
		public static string DecimalToString(Decimal x)
		{
			string outValue = "";
			// return String.Format(CultureInfo.CreateSpecificCulture("en-us"), VNCurrency_Patterns[0], x);
			//string specifier = "#,#.00#;(#,#.00#)";
			//return (x*-1).ToString(specifier);
			// return (x.ToString("0,0.00", CultureInfo.InvariantCulture));
			if (x == 0)
				outValue = "";
			else
				outValue = FormatCurrencyVND(x);
			return outValue;

		}
		public static string DecimalToStringWithZero(Decimal x)
		{
			string outValue = "";
			// return String.Format(CultureInfo.CreateSpecificCulture("en-us"), VNCurrency_Patterns[0], x);
			//string specifier = "#,#.00#;(#,#.00#)";
			//return (x*-1).ToString(specifier);
			// return (x.ToString("0,0.00", CultureInfo.InvariantCulture));
			if (x == 0)
				outValue = "0";
			else
				outValue = FormatCurrencyVND(x);
			return outValue;

		}

		public static string showDate(DateTime dt)
		{
			return dt.CompareTo(GlobalConstant.MIN_DATE) > 0 ? dt.ToString("dd/MM/yyyy") : string.Empty;
		}

		public static string DataTableToJSON(DataTable table)
		{
			var JSONString = new StringBuilder();
			if (table.Rows.Count > 0)
			{
				JSONString.Append("[");
				for (int i = 0; i < table.Rows.Count; i++)
				{
					JSONString.Append("{");
					for (int j = 0; j < table.Columns.Count; j++)
					{
						if (j < table.Columns.Count - 1)
						{
							JSONString.Append("\"" + table.Columns[j].ColumnName.ToString() + "\":" + "\"" + table.Rows[i][j].ToString() + "\",");
						}
						else if (j == table.Columns.Count - 1)
						{
							JSONString.Append("\"" + table.Columns[j].ColumnName.ToString() + "\":" + "\"" + table.Rows[i][j].ToString() + "\"");
						}
					}
					if (i == table.Rows.Count - 1)
					{
						JSONString.Append("}");
					}
					else
					{
						JSONString.Append("},");
					}
				}
				JSONString.Append("]");
			}
			return JSONString.ToString();
		}
		public static void ExcuteSQL(string strSQL)
		{
			SqlConnection cn = new SqlConnection(DBUtils.GetDBConnectionString());
			SqlCommand cmd = new SqlCommand(strSQL, cn);
			cmd.CommandType = CommandType.Text;
			cmd.CommandTimeout = 0;
			cn.Open();
			cmd.CommandText = strSQL;
			cmd.ExecuteNonQuery();
			cn.Close();
		}
		public static DateTime GetBusinessDateTime()
		{
			try
			{
				#region Lay ra ngay he thong ,gio he thong
				DateTime B_Date = GetBusinessDate();
				DateTime S_Date = GetSystemDate();
				#endregion

				#region Gan Time
				DateTime dt = new DateTime(B_Date.Year, B_Date.Month, B_Date.Day, S_Date.Hour, S_Date.Minute, S_Date.Second, S_Date.Millisecond);
				#endregion

				return dt;
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message);
			}
		}
		public static DateTime GetSystemDate()
		{
			try
			{
				return Convert.ToDateTime(Select("SELECT GETDATE() AS SystemDate").Rows[0][0]);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message);
			}
		}
		public static DateTime GetBusinessDate()
		{
			DateTime DateTimeValue = DateTime.Today;
			DateTimeValue = ((BusinessDateModel)BusinessDateBO.Instance.FindAll()[0]).BusinessDate;
			return DateTimeValue;
			//return Global.GetBusinessDate;
		}
		public static string GetSystemTime()
		{
			try
			{
				DateTime S_Date = GetSystemDate();
				string st = S_Date.Hour + ":" + S_Date.Minute + ":" + S_Date.Second;
				return st;
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message);
			}

		}

		public static void InsertActivityLog(string _tablename, int _ID, string _change, string _oldvalue, string _newvalue, string _description)
		{
			ActivityLogModel mAL = new ActivityLogModel();
			mAL.TableName = _tablename;
			mAL.ObjectID = _ID;
			mAL.UserID = Global.UserID;
			mAL.UserName = Global.UserName;
			mAL.ChangeDate = GetSystemDate();
			mAL.Change = _change;
			mAL.OldValue = _oldvalue;
			mAL.NewValue = _newvalue;
			mAL.Description = _description;
			ActivityLogBO.Instance.Insert(mAL);
		}
		public static void FillValues<T>(ActivityLogModel log, T oldModel, T newModel)
		{
			var oldValues = new List<string>();
			var newValues = new List<string>();

			PropertyInfo[] properties = typeof(T).GetProperties();

			string[] ignoreProps = { "ID", "UserUpdateID", "UpdateDate", "CreateDate", "UpdatedDate",
		"CreatedDate", "UserInsertID", "CreateBy", "UpdateBy", "CreatedBy", "UpdatedBy", "ImagePath"};

			foreach (var prop in properties)
			{
				if (ignoreProps.Contains(prop.Name)) continue;

				object oldVal = oldModel != null ? prop.GetValue(oldModel) : null;
				object newVal = newModel != null ? prop.GetValue(newModel) : null;

				string oldStr = oldVal?.ToString()?.Trim() ?? "";
				string newStr = newVal?.ToString()?.Trim() ?? "";

				if (oldStr != newStr)
				{
					if (oldModel == null)
					{
						if (string.IsNullOrEmpty(newStr) || newStr == "0" || newStr == "0,00" || newStr.ToLower() == "false")
							continue;
					}

					oldValues.Add($"{prop.Name}: {(string.IsNullOrEmpty(oldStr) ? "(null)" : oldStr)}");
					newValues.Add($"{prop.Name}: {(string.IsNullOrEmpty(newStr) ? "(null)" : newStr)}");
				}
			}

			log.OldValue = string.Join(" | ", oldValues);
			log.NewValue = string.Join(" | ", newValues);

			if (string.IsNullOrEmpty(log.NewValue))
			{
				log.Description = "No significant changes detected.";
			}
		}

		public static string GetSplitString(string Name)
		{
			string paraName;
			string[] arrConfirmationNo = null;
			paraName = "";
			if (Name != "")
			{
				arrConfirmationNo = Name.Split(',');
				if (arrConfirmationNo.Length > 0)
				{
					for (int i = 0; i < arrConfirmationNo.Length; i++)
					{
						if (i != 0)
							paraName = paraName + "'" + "," + "'" + arrConfirmationNo[i].ToString().Trim();
						else
							paraName = arrConfirmationNo[i].ToString().Trim();
					}
				}
			}
			return paraName;
		}

		public static int ExecuteScalarInt(string strSQL)
		{
			using (SqlConnection cn = new SqlConnection(_connectionString))
			{
				try
				{
					using (SqlCommand cmd = new SqlCommand(strSQL, cn))
					{
						cmd.CommandType = CommandType.Text;
						cmd.CommandTimeout = 0;
						cn.Open();

						object result = cmd.ExecuteScalar();

						return (result == null || result == DBNull.Value) ? 0 : Convert.ToInt32(result);
					}
				}
				catch (SqlException se)
				{
					throw new Exception("ExecuteScalarInt error: " + se.Message);
				}
			}
		}

		/// <summary>
		/// Update dữ liệu vào DB
		/// -- CSS, 01/11/2010
		/// </summary>
		/// <param name="command"></param>
		public static void UpdateDataBase(string command)
		{
			SqlConnection cnn = new SqlConnection(DBUtils.GetDBConnectionString());
			try
			{
				SqlCommand cmd = new SqlCommand();
				cnn.Open();
				cmd = new SqlCommand("spSearchAllForTrans", cnn);
				//cmd.CommandTimeout = 6000;
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.Parameters.Add(new SqlParameter("@sqlCommand", command));
				cmd.ExecuteNonQuery();
			}
			catch (SqlException ex)
			{
				throw new Exception("Update error :" + ex.Message);
			}
			finally
			{
				cnn.Close();
			}
		}


		public static void UpdateCommand(string command)
		{
			SqlConnection cnn = new SqlConnection(DBUtils.GetDBConnectionString());
			try
			{
				SqlCommand cmd = new SqlCommand("spSearchAllForTrans", cnn);
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.Parameters.Add(new SqlParameter("@sqlCommand", command));

				// Ghi váo history để sư dụng tra cứu DeadLock.
				// Excute
				cmd.ExecuteNonQuery();
			}
			catch (SqlException se)
			{
				throw new Exception("Update error :" + se.Message);
			}
			catch (Exception ex)
			{
				throw new Exception("Update error :" + ex.Message);
			}
		}

	}

}

