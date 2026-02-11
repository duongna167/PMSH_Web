using System;
using System.Collections;
using System.Configuration;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Configuration;
using BaseBusiness.bc;
using System.Security.Cryptography;
namespace BaseBusiness.util
{
	/// <summary>
	/// Summary description for DBUtils.
	/// </summary>
	public class DBUtils
	{
		//private static readonly IConfiguration _configuration;
		//private static readonly ConnectionStrings _myConfiguration;
		public DBUtils()
		{
			//
			// TODO: Add constructor logic here
			//
		}
        public static string passPhrase = "Pas5pr@se";        // can be any string
        public static string saltValue = "s@1tValue";        // can be any string
        public static string hashAlgorithm = "SHA1";             // can be "MD5"
        public static int passwordIterations = 2;                  // can be any number
        public static string initVector = "@CSS@CSS@CSS@CSS"; // must be 16 bytes
        public static int keySize = 256;
        public static string GetDBConnectionString()
		{
			try
			{
				string connectionString = new AppConfiguration().ConnectionString;
				return connectionString.ToString();
			}
			catch
			{
				throw new Exception("Can not connect to DB");
			}
		}

        public static string GetDBFastConnectionString()
        {
            try
            {
                string fastConnectionString = new AppConfiguration().FastConnectionString;
                return fastConnectionString.ToString();
            }
            catch
            {
                throw new Exception("Can not connect to DB");
            }
        }

        public static string SQLInsert(BaseModel model)
		{
			string tableName = model.GetType().Name;
			tableName = tableName.Substring(0, tableName.Length - 5);

			string s1 = "", s2 = "";
			foreach(PropertyInfo p in model.GetType().GetProperties())
			{
				//if (!p.Name.Equals(tableName + "ID"))
				if (!p.Name.Equals("ID"))
				{
					s1 += p.Name + ",";
					s2 += "@" + p.Name + ",";
				}
			}
			s1 = s1.Substring(0, s1.Length - 1);
			s2 = s2.Substring(0, s2.Length - 1);
			return string.Format("INSERT INTO {0} ({1}) VALUES ({2})  SELECT @@IDENTITY AS 'ID'", tableName, s1, s2);
		}

		public static string SQLUpdate(BaseModel model)
		{
			string tableName = model.GetType().Name;
			tableName = tableName.Substring(0, tableName.Length - 5);

			string Update = "UPDATE " + tableName + " SET ";
			foreach(PropertyInfo p in model.GetType().GetProperties())
			{
				//if (!p.Name.Equals(tableName + "ID"))
				if (!p.Name.Equals("ID"))
				{
					Update += p.Name + "=@" + p.Name + ",";
				}
			}
			Update = Update.Substring(0, Update.Length - 1);
			//Update = Update + " WHERE " + tableName + "ID=" + model.GetType().GetProperty(tableName + "ID").GetValue(model, null).ToString();
			Update = Update + " WHERE ID=" + model.GetType().GetProperty("ID").GetValue(model, null).ToString();
			return Update;
		}

		public static string SQLTop(string sql, long top)
		{
			int index = sql.ToUpper().IndexOf("SELECT") + 6;
			return "SELECT TOP " + top + " " + sql.Substring(index);
		}


		//some limitation: 
		//support SQL Server and inner join, joined field is the same for both tables
		public static string SQLJoin(string table1, string table2, string joinedField, Expression exp)
		{
			string sql = string.Format(
						"SELECT DISTINCT A.* FROM {0} AS A INNER JOIN {1} AS B " +
						"ON (A.{2} = B.{2})",
						table1, table2, joinedField);
			if (exp != null)
				sql += " WHERE " + exp.ToString();
			return sql;
		}
        public static string Decrypt(string cipherText,
                             string passPhrase,
                             string saltValue,
                             string hashAlgorithm,
                             int passwordIterations,
                             string initVector,
                             int keySize)
        {
            byte[] initVectorBytes = Encoding.ASCII.GetBytes(initVector);
            byte[] saltValueBytes = Encoding.ASCII.GetBytes(saltValue);

            // Convert our ciphertext into a byte array.
            byte[] cipherTextBytes = Convert.FromBase64String(cipherText);

            PasswordDeriveBytes password = new PasswordDeriveBytes(
                                                            passPhrase,
                                                            saltValueBytes,
                                                            hashAlgorithm,
                                                            passwordIterations);

            byte[] keyBytes = password.GetBytes(keySize / 8);

            RijndaelManaged symmetricKey = new RijndaelManaged();
            symmetricKey.Mode = CipherMode.CBC;

            ICryptoTransform decryptor = symmetricKey.CreateDecryptor(
                                                             keyBytes,
                                                             initVectorBytes);
            MemoryStream memoryStream = new MemoryStream(cipherTextBytes);
            CryptoStream cryptoStream = new CryptoStream(memoryStream,
                                                          decryptor,
                                                          CryptoStreamMode.Read);

            byte[] plainTextBytes = new byte[cipherTextBytes.Length];
            int decryptedByteCount = cryptoStream.Read(plainTextBytes,
                                                       0,
                                                       plainTextBytes.Length);
            memoryStream.Close();
            cryptoStream.Close();
            string plainText = Encoding.UTF8.GetString(plainTextBytes,
                                                       0,
                                                       decryptedByteCount);

            // Return decrypted string.   
            return plainText;
        }
        public static string Encrypt(string plainText,
                                    string passPhrase,
                                    string saltValue,
                                    string hashAlgorithm,
                                    int passwordIterations,
                                    string initVector,
                                    int keySize)
        {
            byte[] initVectorBytes = Encoding.ASCII.GetBytes(initVector);
            byte[] saltValueBytes = Encoding.ASCII.GetBytes(saltValue);

            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);

            PasswordDeriveBytes password = new PasswordDeriveBytes(
                                                            passPhrase,
                                                            saltValueBytes,
                                                            hashAlgorithm,
                                                            passwordIterations);

            byte[] keyBytes = password.GetBytes(keySize / 8);
            RijndaelManaged symmetricKey = new RijndaelManaged();

            symmetricKey.Mode = CipherMode.CBC;
            ICryptoTransform encryptor = symmetricKey.CreateEncryptor(
                                                             keyBytes,
                                                             initVectorBytes);

            MemoryStream memoryStream = new MemoryStream();
            CryptoStream cryptoStream = new CryptoStream(memoryStream,
                                                         encryptor,
                                                         CryptoStreamMode.Write);
            cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
            cryptoStream.FlushFinalBlock();

            byte[] cipherTextBytes = memoryStream.ToArray();

            memoryStream.Close();
            cryptoStream.Close();
            string cipherText = Convert.ToBase64String(cipherTextBytes);

            return cipherText;
        }
        public static SqlDbType ConvertToSQLType(Type type)
		{
			if (type == typeof (string))
			{
				return SqlDbType.NVarChar;
			}
			if (type == typeof (Int16))
			{
				return SqlDbType.TinyInt;
			}
			if (type == typeof (int))
			{
				return SqlDbType.Int;
			}
			if (type == typeof (long))
			{
				return SqlDbType.BigInt;
			}
			if (type == typeof (Int64))
			{
				return SqlDbType.BigInt;
			}
			if (type == typeof (DateTime))
			{
				return SqlDbType.DateTime;
			}
			if (type == typeof (Decimal))
			{
				return SqlDbType.Decimal;
			}
			if (type == typeof (float))
			{
				return SqlDbType.Float;
			}
            if (type == typeof(TimeSpan))
            {
                return SqlDbType.Time;
            }
			return SqlDbType.NVarChar;
		}

		public string GenerateCode(string table, string code, string prefix)
		{
			string lastCode = string.Empty;
			using (SqlConnection conn = new SqlConnection(GetDBConnectionString()))
			{
				conn.Open();
				SqlCommand cmd = new SqlCommand(string.Format("SELECT TOP 1 {0} FROM {1} ORDER BY {0} DESC", code, table), conn);
				lastCode = Convert.ToString(cmd.ExecuteScalar());
			}

			int newNumber = 1;
			if (!string.IsNullOrEmpty(lastCode))
			{
				int index = lastCode.Length - 1;
				while (index >= 0 && char.IsDigit(lastCode[index]))
				{
					index--;
				}

				if (index < lastCode.Length - 1)
				{
					int.TryParse(lastCode.Substring(index + 1), out newNumber);
				}
			}

			return string.Format("{0}{1}", prefix, (newNumber + 1).ToString().PadLeft(6, '0'));
		}
        public bool Checktable(string tableName, string columnName, string value)
        {
            bool exists = false;
            string query = $"SELECT 1 FROM {tableName} WHERE {columnName} = @Value";

            using (SqlConnection conn = new SqlConnection(GetDBConnectionString()))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Value", value);

                var result = cmd.ExecuteScalar();
                if (result != null)
                {
                    exists = true;
                }
            }

            return exists;
        }
        public string returnTable(string tableName, string columnName, string returnvale,string value)
        {
            string result = null; // ??t giá tr? m?c ??nh cho k?t qu?
            string query = $"SELECT {returnvale} FROM {tableName} WHERE {columnName} = @Value";

            using (SqlConnection conn = new SqlConnection(GetDBConnectionString()))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Value", value);

                var queryResult = cmd.ExecuteScalar();
                if (queryResult != null)
                {
                    result = queryResult.ToString(); // L?y giá tr? và chuy?n ??i sang chu?i
                }
            }

            return result; // Tr? v? giá tr? ho?c null
        }

        public string GenerateCode2(string table, string code, string prefix)
        {
            string lastCode = string.Empty;
            using (SqlConnection conn = new SqlConnection(GetDBConnectionString()))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(string.Format("SELECT TOP 1 {0} FROM {1} ORDER BY {0} DESC", code, table), conn);
                lastCode = Convert.ToString(cmd.ExecuteScalar());
            }

            return lastCode;
        }


        public static string SQLSelect(string tableName, Expression exp)
		{
			string sql = "SELECT * FROM " + tableName + " WITH (NOLOCK) ";
			if (exp != null)
				sql += " WHERE " + exp.ToString();
			return sql;
		}
        public static bool TestExternalConnection(string serverName, string databaseName, string login, string password)
        {
            try
            {
                string connString = $"Server={serverName};Database={databaseName};User Id={login};Password={password};Trusted_Connection=False;Encrypt=False;";
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    return true; // K?t n?i thành công
                }
            }
            catch
            {
                return false; // K?t n?i th?t b?i
            }
        }


    }

    public class ConnectionStrings
	{		
		public string DefaultConnection { get; set; }
	}

}
