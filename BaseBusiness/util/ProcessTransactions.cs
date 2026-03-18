using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Reflection;
using System.Text;
using BaseBusiness.bc;
using BaseBusiness.Utils;
using System.Linq;
using log4net;
using System.Xml;
using BaseBusiness.BO;
using BaseBusiness.Model;


namespace BaseBusiness.util
{
    public class ProcessTransactions
    {
        protected BaseFacade baseFacade = null;
        public string HistoryContent = "";
        int CountTransaction = 0;
        string Header_UpdateCommand = "UPDATE_COM : ";
        int[] NUMBER_OF_EXCEPTION = { 1025, -2 };
        int NUMBER_OF_DEADLOCK = 1025;

        #region Khai bao cac bien dung chung
        protected string strcon;
        private readonly SqlConnection cnn;
        private SqlTransaction tran;
        private SqlCommand cmd;
        //private SqlDataAdapter da;
        public SqlConnection Connection => cnn;
        public SqlTransaction Transaction => tran;
        private EventsLogErrorModel mELE;
        string Header_Update = "UPDATE : ";
        string[] DESCRIPTION_OF_EXCEPTION = { "Server busy or network is slower !!!", "Time Out or Row is Locked !!!" };

        #endregion

        #region Constructor

        public ProcessTransactions()
        {
            cnn = new SqlConnection(DBUtils.GetDBConnectionString());
        }

        public ProcessTransactions(string _ConnectString)
        {
            cnn = new SqlConnection(_ConnectString);
        }
        #endregion

        #region Phuong thuc su dung them

        /// <summary>
        /// Phương thức lấy ra tên của Model của table
        /// </summary>
        /// <param name="tableName">Tên của table</param>
        /// <returns>Tên của Model</returns>
        /// Author :Nguyễn Trung Kiên
        /// Date:29/9/2009
        public static string getClassName(string tableName)
        {
            return tableName + "Model";
        }

        /// <summary>
        /// Phương thức đổ dũ liệu từ DataRow vào Model
        /// -- Nguyễn Trung Kiên - 29/9/2009 --
        /// </summary>
        /// <param name="dr">DataRow</param>
        /// <param name="model">Tên Model</param>
        /// <returns>Object</returns>
        private static object PopulateObject(DataRow dr, object model)
        {
            PropertyInfo[] propertiesName = model.GetType().GetProperties();

            for (int i = 0; i < propertiesName.Length; i++)
            {
                Object value = dr[propertiesName[i].Name];
                if (value != DBNull.Value)
                {
                    propertiesName[i].SetValue(model, value, null);
                }
            }

            return model;
        }

        /// <summary>
        /// Phương thức đổ dũ liệu từ DataRow vào Model
        /// -- Nguyễn Trung Kiên - 29/9/2009 --
        /// </summary>
        /// <param name="dr">DataRow</param>
        /// <param name="model">Tên Model</param>
        /// <returns>Object</returns>
        private static object PopulateObject(DataRow dr, string fullname)
        {
            Object model = Activator.CreateInstance(Type.GetType(fullname));
            return PopulateObject(dr, model);
        }

        /// <summary>
        /// Phương thức đổ dũ liệu từ DataRow vào Model
        /// -- Nguyễn Trung Kiên - 29/9/2009 --
        /// </summary>
        /// <param name="dr">DataRow</param>
        /// <param name="model">Tên Model</param>
        /// <returns>Object</returns>
        public static BaseModel PopulateModel(DataRow dr, string name)
        {
            //return (BaseModel)PopulateObject(dr, "eDongPOS." + name);
            return (BaseModel)PopulateObject(dr, "BaseBusiness.Model." + name);
        }

        #endregion

        #region Cac method thuc hien Insert,delete,update,select

        /// <summary>
        /// Lấy về dữ liệu thông qua 1 câu command
        /// -- Nguyễn Trung Kiên - 29/9/2009 --
        /// </summary>
        /// <param name="strComm">Chuỗi Command để Excute</param>
        /// <returns>DataTable</returns>
        public DataTable Select(string strComm)
        {
            try
            {
                cmd = new SqlCommand(strComm, cnn, tran);
                cmd.CommandType = CommandType.Text;

                cmd.ExecuteNonQuery();

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataSet ds = new DataSet();
                da.Fill(ds);
                return ds.Tables[0];
            }
            catch (SqlException se)
            {
                throw new Exception("Sellect error :" + se.Message);
            }
        }

        /// <summary>
        /// Lấy dữ liệu đổ về thông qua Store Procedure với only parameter
        /// -- Nguyễn Trung Kiên - 29/9/2009 --
        /// </summary>
        /// <param name="procedureName">Tên Store Procedure</param>
        /// <param name="mySqlParameter">Parameter</param>
        /// <param name="nameSetToTable">Tên của table lấy ra</param>
        /// <returns>DataTable</returns>
        public DataTable getTable(string procedureName, SqlParameter mySqlParameter, string nameSetToTable)
        {
            DataTable table = new DataTable();
            try
            {

                cmd = new SqlCommand(procedureName, cnn, tran);
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataSet myDataSet = new DataSet();
                if (mySqlParameter != null)
                    cmd.Parameters.Add(mySqlParameter);
                cmd.ExecuteNonQuery();
                da.Fill(myDataSet, nameSetToTable);
                table = myDataSet.Tables[nameSetToTable];
            }
            catch (SqlException ex)
            {
                tran.Rollback();
                throw new Exception(ex.Message);
            }
            return table;
        }

        /// <summary>
        /// Lấy dữ liệu đổ về thông qua Store Procedure với only parameter
        /// -- Nguyễn Trung Kiên - 29/9/2009 --
        /// </summary>
        /// <param name="procedureName">Tên Store Procedure</param>
        /// <param name="nameSetToTable">Tên của table lấy ra</param>
        /// <param name="mySqlParameter">Mảng các parameter</param>
        /// <returns></returns>
        public DataTable getTable(string procedureName, string nameSetToTable, params SqlParameter[] mySqlParameter)
        {
            DataTable table = new DataTable();
            try
            {
                cmd = new SqlCommand(procedureName, cnn, tran);
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataSet myDataSet = new DataSet();
                for (int i = 0; i < mySqlParameter.Length; i++)
                    cmd.Parameters.Add(mySqlParameter[i]);
                cmd.ExecuteNonQuery();
                da.Fill(myDataSet, nameSetToTable);
                table = myDataSet.Tables[nameSetToTable];
            }
            catch (SqlException ex)
            {
                tran.Rollback();
                throw new Exception(ex.Message);
            }
            return table;
        }

        /// <summary>
        /// Lấy dữ liệu đổ về thông qua Store Procedure với only parameter
        /// -- Nguyễn Trung Kiên - 29/9/2009 --
        /// </summary>
        /// <param name="procedureName">Tên Store Procedure</param>
        /// <param name="nameSetToTable">Tên của table lấy ra</param>
        /// <param name="paramName">Danh sách tên param</param>
        /// <param name="paramValue">Danh sách giá trị các param</param>
        /// <returns>DataTable</returns>
        public DataTable getTable(string procedureName, string nameSetToTable, string[] paramName, object[] paramValue)
        {
            DataTable table = new DataTable();
            try
            {
                cmd = new SqlCommand(procedureName, cnn, tran);
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataSet myDataSet = new DataSet();
                SqlParameter sqlParam;
                for (int i = 0; i < paramName.Length; i++)
                {
                    sqlParam = new SqlParameter(paramName[i], paramValue[i]);
                    cmd.Parameters.Add(sqlParam);
                }
                cmd.ExecuteNonQuery();
                da.Fill(myDataSet, nameSetToTable);
                table = myDataSet.Tables[nameSetToTable];
            }
            catch (SqlException ex)
            {
                tran.Rollback();
                throw new Exception(ex.Message);
            }
            return table;
        }

        public void ExecSP(string procedureName, SqlParameter mySqlParameter)
        {
            try
            {
                cmd = new SqlCommand(procedureName, cnn, tran);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(mySqlParameter);
                cmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                tran.Rollback();
                throw new Exception(ex.Message);
            }
        }


        public void ExcuteSQL(string strSQL)
        {
            try
            {
                SqlCommand cmd = new SqlCommand(strSQL, cnn, tran);
                cmd.CommandType = CommandType.Text;
                cmd.CommandTimeout = 0;
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                tran.Rollback();
                throw new Exception(ex.Message);
            }

        }

        /// <summary>
        /// Tìm kiếm theo thuộc tính trả về mảng Model
        /// -- Nguyễn Trung Kiên - 29/9/2009 --
        /// </summary>
        /// <param name="tableName">Tên của Table</param>
        /// <param name="fieldName">Danh sách tên của parameter</param>
        /// <param name="fieldValue">Danh sách giá trị của parameter</param>
        /// <returns>ArrayList</returns>
        public List<BaseModel> FindByAttribute(string tableName, string fieldName, string fieldValue)
        {
            List<BaseModel> result = new List<BaseModel>();

            try
            {
                string sql = $"SELECT * FROM {tableName} WITH (NOLOCK) WHERE {fieldName} = @fieldValue";

                using (SqlCommand cmd = new SqlCommand(sql, cnn, tran))
                {
                    cmd.CommandTimeout = 6000;
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@fieldValue", fieldValue);

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataSet ds = new DataSet();
                        da.Fill(ds, "TABLE");

                        string className = getClassName(tableName);
                        for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                        {
                            DataRow dr = ds.Tables[0].Rows[i];
                            result.Add(PopulateModel(dr, className));
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /*
		 * 
		 * TuanLA@2023-08-21 add new ConvertToDataType function
		 * this function detect the type of object & convert it into correct type
		 */
        private static object ConvertToDataType(string input)
        {
            if (int.TryParse(input, out int intValue))
                return intValue;
            if (long.TryParse(input, out long longValue))
                return longValue;
            if (double.TryParse(input, out double doubleValue))
                return doubleValue;
            if (decimal.TryParse(input, out decimal decimalValue))
                return decimalValue;
            if (DateTime.TryParse(input, out DateTime dateTimeValue))
                return dateTimeValue;
            if (bool.TryParse(input, out bool boolValue))
                return boolValue;
            if (Guid.TryParse(input, out Guid guidValue))
                return guidValue;
            if (TimeSpan.TryParse(input, out TimeSpan timeSpanValue))
                return timeSpanValue;

            // Return string if none of the conversions match
            return input;
        }

        /*
		 * 
		 * TuanLA@2023-08-21 add new ExecuteSQLWithParameters function
		 * this function gets the result by passing the values through SQL Parameters
		 * The limit of this function just support passing 2100 parameters only. Therefore, it is limited to IN or NOT IN statements
		 */
        protected ArrayList ExecuteSQLWithParameters(string tableName, Expression exp, string orderBy = "", int top = 0)
        {
            ArrayList result = new ArrayList();
            try
            {
                string sql = $"SELECT " + (top > 0 ? "TOP " + top : string.Empty) + $" * FROM {tableName} WHERE {exp.ToSQLParametersString()}" + (string.IsNullOrEmpty(orderBy) ? string.Empty : " ORDER BY " + orderBy);

                using (SqlCommand cmd = new SqlCommand(sql, cnn, tran))
                {
                    cmd.CommandTimeout = 6000;
                    cmd.CommandType = CommandType.Text;
                    foreach (Expression expression in exp.ToList())
                    {
                        if (expression.op.ToString().Contains("IN"))
                        {
                            //Passing the List of Object into Parameters
                            cmd.AddArrayParameters($"{expression.exp1}", expression.exp2.ToString().Trim('(', ')', ' ')
                                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(s => s.Trim())
                                    .ToArray());
                        }
                        else
                        {
                            //Passing the value into Parameters
                            cmd.Parameters.AddWithValue($"@{expression.exp1}", ConvertToDataType(expression.exp2.ToString()));
                        }
                    }

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataSet ds = new DataSet();
                    da.Fill(ds, "TABLE");
                    //begin get data
                    string classname = getClassName(tableName);
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        DataRow dr = ds.Tables[0].Rows[i];
                        result.Add(PopulateModel(dr, getClassName(tableName)));
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Tìm kiếm theo Expression trả về mảng Model
        /// -- Nguyễn Trung Kiên - 29/9/2009 --
        /// </summary>
        /// <param name="tableName">Tên của Table</param>
        /// <param name="exp">Expression</param>
        /// <returns>ArrayList</returns>
        public ArrayList FindByExpression(string tableName, Expression exp)
        {
            if (exp.ToString().Contains(" FROM "))
            {
                try
                {
                    ArrayList result = new ArrayList();
                    string sql = DBUtils.SQLSelect(tableName, exp);
                    cmd = new SqlCommand(sql, cnn, tran);
                    cmd.CommandTimeout = 6000;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = sql;

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataSet ds = new DataSet();
                    da.Fill(ds, "TABLE");
                    //begin get data
                    string classname = getClassName(tableName);
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        DataRow dr = ds.Tables[0].Rows[i];
                        result.Add(PopulateModel(dr, getClassName(tableName)));
                    }
                    //end and return
                    return result;
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
            else
            {
                return ExecuteSQLWithParameters(tableName, exp);
            }
        }

        /// <summary>
        /// Tìm kiếm theo PrimaryKey
        /// -- Nguyễn Trung Kiên - 29/9/2009 --
        /// </summary>
        /// <param name="tableName">Tên của Table</param>
        /// <param name="ID">PK</param>
        /// <returns>Model</returns>
        public BaseModel FindByPK(string tableName, Int64 ID)
        {
            try
            {
                string sql = $"SELECT * FROM {tableName}  WITH (NOLOCK) WHERE ID = @ID";

                using (SqlCommand cmd = new SqlCommand(sql, cnn, tran))
                {
                    cmd.CommandTimeout = 6000;
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@ID", ID);

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataSet ds = new DataSet();
                        da.Fill(ds, "TABLE");

                        if (ds.Tables[0].Rows.Count > 0)
                        {
                            DataRow dr = ds.Tables[0].Rows[0];
                            return PopulateModel(dr, getClassName(tableName));
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


        public BaseModel FindByPrimaryKey(long value)
        {
            try
            {
                return baseFacade.FindByPrimaryKey(value);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public List<BaseModel> FindAll(string tableName)
        {
            List<BaseModel> result = new List<BaseModel>();

            try
            {
                string sql = $"SELECT * FROM {tableName} WITH (NOLOCK)";

                using (SqlCommand cmd = new SqlCommand(sql, cnn, tran))
                {
                    cmd.CommandTimeout = 6000;
                    cmd.CommandType = CommandType.Text;

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataSet ds = new DataSet();
                        da.Fill(ds, "TABLE");

                        string className = getClassName(tableName);
                        foreach (DataRow dr in ds.Tables[0].Rows)
                        {
                            result.Add(PopulateModel(dr, className));
                        }
                    }
                }

                return result.Count > 0 ? result : null;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


        /// <summary>
        /// Hàm insert dữ liệu sử dụng Model
        /// -- Nguyễn Trung Kiên - 29/9/2009 --
        /// </summary>
        /// <param name="baseModel">Model</param>
        /// <returns>Decimal</returns>
        private static readonly ILog logger = LogManager.GetLogger("DatabaseLogger");
        //public decimal Insert(BaseModel baseModel)
        //{
        //    #region Khai bao cac bien cac bien connection
        //    string TableName = baseModel.GetType().Name.Substring(0, baseModel.GetType().Name.Length - 5);
        //    string sql = DBUtils.SQLInsert(baseModel);
        //    cmd = new SqlCommand(sql, cnn, tran);
        //    cmd.CommandType = CommandType.Text;
        //    PropertyInfo[] propertiesName = baseModel.GetType().GetProperties();
        //    object value;
        //    #endregion

        //    #region Gan gia tri cac command goc
        //    for (int i = 0; i < propertiesName.Length; i++)
        //    {
        //        value = propertiesName[i].GetValue(baseModel, null);

        //        if (!propertiesName[i].Name.Equals("ID"))
        //        {
        //            if (value != null)
        //            {
        //                cmd.Parameters.Add("@" + propertiesName[i].Name, DBUtils.ConvertToSQLType(propertiesName[i].PropertyType)).Value = value;
        //            }
        //            else
        //                cmd.Parameters.Add("@" + propertiesName[i].Name, DBUtils.ConvertToSQLType(propertiesName[i].PropertyType)).Value = "";
        //        }
        //    }
        //    #endregion
        //    try
        //    {
        //        return (decimal)cmd.ExecuteScalar();
        //    }
        //    catch (Exception se)
        //    {
        //        tran.Rollback();
        //        throw new Exception("Insert " + baseModel.GetType().Name + " error :" + se.Message);
        //    }
        //}
        //public decimal Insert(BaseModel baseModel)
        //{
        //    long ID = 0;
        //    string TableName = baseModel.GetType().Name.Substring(0, baseModel.GetType().Name.Length - 5);
        //    string sql = DBUtils.SQLInsert(baseModel);
        //    string sqlText = sql;
        //    SqlCommand cmd = new SqlCommand(sql, cnn, tran);
        //    cmd.CommandType = CommandType.Text;
        //    ThreadContext.Properties["ActionType"] = "INSERT";
        //    ThreadContext.Properties["TableName"] = baseModel.GetType().Name.Replace("Model", string.Empty);

        //    PropertyInfo[] propertiesName = baseModel.GetType().GetProperties();
        //    object value;

        //    //for (int i = 0; i < propertiesName.Length; i++)
        //    //{
        //    //    value = propertiesName[i].GetValue(baseModel, null);
        //    //    if (!propertiesName[i].Name.Equals("ID"))
        //    //    {
        //    //        cmd.Parameters.Add("@" + propertiesName[i].Name, DBUtils.ConvertToSQLType(propertiesName[i].PropertyType)).Value = value ?? DBNull.Value; // Use DBNull for null values
        //    //        //sqlText = sqlText.Replace("@" + propertiesName[i].Name + ",", cmd.Parameters[i].Value.ToString() + ",");
        //    //        //sqlText = sqlText.Replace("@" + propertiesName[i].Name + ")", cmd.Parameters[i].Value.ToString() + ")");
        //    //    }
        //    //}
        //    for (int i = 0; i < propertiesName.Length; i++)
        //    {
        //        value = propertiesName[i].GetValue(baseModel, null);

        //        if (!propertiesName[i].Name.Equals("ID"))
        //        {
        //            if (value != null)
        //            {
        //                cmd.Parameters.Add("@" + propertiesName[i].Name, DBUtils.ConvertToSQLType(propertiesName[i].PropertyType)).Value = value;
        //                sqlText = sqlText.Replace("@" + propertiesName[i].Name + ",", cmd.Parameters[i].Value.ToString() + ",");
        //                sqlText = sqlText.Replace("@" + propertiesName[i].Name + ")", cmd.Parameters[i].Value.ToString() + ")");
        //            }
        //            else
        //                cmd.Parameters.Add("@" + propertiesName[i].Name, DBUtils.ConvertToSQLType(propertiesName[i].PropertyType)).Value = "";
        //                sqlText = sqlText.Replace("@" + propertiesName[i].Name + ",", "'',");
        //                sqlText = sqlText.Replace("@" + propertiesName[i].Name + ")", "'')");
        //        }
        //    }
        //    try
        //        {
        //        return (decimal)cmd.ExecuteScalar();
        //    }
        //    catch (Exception se)
        //    {
        //        logger.Error(sqlText + "=> Error:" + se.Message);
        //        tran.Rollback();
        //        throw new Exception("Insert " + baseModel.GetType().Name + " error :" + se.Message);
        //    }
        //    finally
        //    {
        //        ThreadContext.Properties["KeyID"] = ID.ToString();
        //        if (logger.IsDebugEnabled)
        //            logger.Info(sqlText + "=> ID=" + ID);
        //    }
        //}
        public decimal Insert(BaseModel baseModel)
        {
            long ID = 0;
            string TableName = baseModel.GetType().Name.Substring(0, baseModel.GetType().Name.Length - 5);
            string sql = DBUtils.SQLInsert(baseModel);
            string sqlText = sql;
            SqlCommand cmd = new SqlCommand(sql, cnn, tran);
            cmd.CommandType = CommandType.Text;
            ThreadContext.Properties["ActionType"] = "INSERT";
            ThreadContext.Properties["TableName"] = baseModel.GetType().Name.Replace("Model", string.Empty);

            PropertyInfo[] propertiesName = baseModel.GetType().GetProperties();
            object value;

            for (int i = 0; i < propertiesName.Length; i++)
            {
                value = propertiesName[i].GetValue(baseModel, null);

                if (!propertiesName[i].Name.Equals("ID"))
                {
                    string paramName = "@" + propertiesName[i].Name;
                    var dbType = DBUtils.ConvertToSQLType(propertiesName[i].PropertyType);

                    cmd.Parameters.Add(paramName, dbType).Value = value ?? DBNull.Value;

                    // Replace parameter placeholders in sqlText for logging purposes
                    if (value != null)
                    {
                        sqlText = sqlText.Replace(paramName + ",", value.ToString() + ",");
                        sqlText = sqlText.Replace(paramName + ")", value.ToString() + ")");
                    }
                    else
                    {
                        sqlText = sqlText.Replace(paramName + ",", "NULL,");
                        sqlText = sqlText.Replace(paramName + ")", "NULL)");
                    }
                }
            }

            try
            {
                ID = Convert.ToInt64(cmd.ExecuteScalar());
                return ID;
            }
            catch (Exception se)
            {
                logger.Error(sqlText + " => Error: " + se.Message);
                tran.Rollback();
                throw new Exception("Insert " + baseModel.GetType().Name + " error: " + se.Message);
            }
            finally
            {
                ThreadContext.Properties["KeyID"] = ID.ToString();
                if (logger.IsDebugEnabled)
                    logger.Info(sqlText + " => ID=" + ID);
            }
        }

        public decimal Insert(BaseModel baseModel, string otherCnn)
        {
            #region Khai bao cac bien cac bien connection
            var othercnn = new SqlConnection(otherCnn);
            string TableName = baseModel.GetType().Name.Substring(0, baseModel.GetType().Name.Length - 5);
            string sql = DBUtils.SQLInsert(baseModel);
            cmd = new SqlCommand(sql, othercnn, tran);
            cmd.CommandType = CommandType.Text;
            PropertyInfo[] propertiesName = baseModel.GetType().GetProperties();
            object value;
            #endregion

            #region Gan gia tri cac command goc
            for (int i = 0; i < propertiesName.Length; i++)
            {
                value = propertiesName[i].GetValue(baseModel, null);

                if (!propertiesName[i].Name.Equals("ID"))
                {
                    if (value != null)
                    {
                        cmd.Parameters.Add("@" + propertiesName[i].Name, DBUtils.ConvertToSQLType(propertiesName[i].PropertyType)).Value = value;

                    }

                    else
                    {
                        cmd.Parameters.Add("@" + propertiesName[i].Name, DBUtils.ConvertToSQLType(propertiesName[i].PropertyType)).Value = "";
                    }
                }
            }
            #endregion
            try
            {
                return (decimal)cmd.ExecuteScalar();
            }
            catch (SqlException se)
            {
                tran.Rollback();
                throw new Exception("Insert " + baseModel.GetType().Name + " error :" + se.Message);
            }
        }

        /// <summary>
        /// Update dữ liệu thông qua Model
        /// -- Nguyễn Trung Kiên - 29/9/2009 --
        /// </summary>
        /// <param name="baseModel">Model</param>
        public void Update(BaseModel baseModel)
        {
            #region Khai bao cac bien connection
            SqlConnection conn = new SqlConnection(strcon);
            string TableName = baseModel.GetType().Name.Substring(0, baseModel.GetType().Name.Length - 5);
            string sql = DBUtils.SQLUpdate(baseModel);
            string sqlText = sql;

            ThreadContext.Properties["ActionType"] = "UPDATE";
            ThreadContext.Properties["TableName"] = baseModel.GetType().Name.Replace("Model", string.Empty);
            cmd = new SqlCommand(sql, cnn, tran);
            cmd.CommandType = CommandType.Text;
            PropertyInfo[] propertiesName = baseModel.GetType().GetProperties();
            object value;
            #endregion

            #region Gan cac bien vao command goc
            for (int i = 0; i < propertiesName.Length; i++)
            {
                SqlDbType dbType = DBUtils.ConvertToSQLType(propertiesName[i].PropertyType);
                value = propertiesName[i].GetValue(baseModel, null);
                //if (value != null)
                //    cmd.Parameters.Add("@" + propertiesName[i].Name, dbType).Value = value;

                //else
                //    cmd.Parameters.Add("@" + propertiesName[i].Name, dbType).Value = "";
                if (value != null)
                {
                    cmd.Parameters.Add("@" + propertiesName[i].Name, dbType).Value = value;
                    sqlText = sqlText.Replace("@" + propertiesName[i].Name + ",", cmd.Parameters[i].Value.ToString() + ",");
                    sqlText = sqlText.Replace("@" + propertiesName[i].Name + ")", cmd.Parameters[i].Value.ToString() + ")");
                }
                else
                {
                    cmd.Parameters.Add("@" + propertiesName[i].Name, dbType).Value = "";
                    sqlText = sqlText.Replace("@" + propertiesName[i].Name + ",", "'',");
                    sqlText = sqlText.Replace("@" + propertiesName[i].Name + ")", "'')");
                }
            }
            #endregion

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SqlException se)
            {
                logger.Error(sqlText + "=> Error:" + se.Message);
                tran.Rollback();

                throw new Exception("Update " + baseModel.GetType().Name + " error :" + se.Message);
            }
            finally
            {
                var ID = baseModel.GetType()
                      .GetProperties()
                      .FirstOrDefault(p => p.Name.Equals("ID", StringComparison.OrdinalIgnoreCase))
                      ?.GetValue(baseModel) ?? "";
                ThreadContext.Properties["KeyID"] = ID.ToString();
                if (logger.IsDebugEnabled)
                    logger.Info(sqlText);

            }
        }


        /// <summary>
        /// Hàm Update dữ liệu 
        /// </summary>
        /// <param name="TableName">Tên bảng</param>
        /// <param name="FieldExpression">Danh sách trường được lấy làm điều kiện Update</param>
        /// <param name="ValueExpression">Danh sách giá trị trường được lấy làm điều kiện Update</param>
        /// <param name="FieldChange">Danh sách trường được Update</param>
        /// <param name="ValueChange">Danh sách giá trị được Update</param>
        public void UpdateAttribute(string TableName, string[] FieldExpression, object[] ValueExpression, string[] FieldChange, object[] ValueChange)
        {
            string[] str = new string[] { "", "", "", "", "" };
            //Khai bao chuoi command
            string command = "Update " + TableName + " Set ";
            //Nhat Value
            for (int i = 0; i < FieldChange.Length; i++)
            {
                if (i != FieldChange.Length - 1)
                    command = command + FieldChange[i] + "=@" + FieldChange[i] + ",";
                else
                    command = command + FieldChange[i] + "=@" + FieldChange[i];
            }
            //Nhat Dieu Kien
            command = command + " where ";
            if (FieldExpression.Length == 1)
            {
                command = command + FieldExpression[0] + "=@" + FieldExpression[0];
            }
            else
            {
                for (int j = 0; j < FieldExpression.Length; j++)
                {
                    if (j != FieldExpression.Length - 1)
                        command = command + FieldExpression[j] + "=@" + FieldExpression[j] + " And ";
                    else
                        command = command + FieldExpression[j] + "=@" + FieldExpression[j];
                }
            }
            //Khai bao doi tuong command
            try
            {
                cmd = new SqlCommand(command, cnn, tran);
                cmd.CommandType = CommandType.Text;
                SqlParameter param;
                for (int i = 0; i < FieldChange.Length; i++)
                {
                    param = new SqlParameter(FieldChange[i], ValueChange[i]);
                    cmd.Parameters.Add(param);
                }
                for (int j = 0; j < FieldExpression.Length; j++)
                {
                    param = new SqlParameter(FieldExpression[j], ValueExpression[j]);
                    cmd.Parameters.Add(param);
                }

                cmd.ExecuteNonQuery();
            }
            catch (SqlException se)
            {
                tran.Rollback();
                throw new Exception("Update error :" + se.Message);
            }
        }

        /// <summary>
        /// Xóa dữ liệu thông qua model
        /// -- Tuấn Le Anh - 24/01/2023 --
        /// </summary>
        /// <param name="baseModel">Model</param>
        public void Delete(BaseModel baseModel)
        {
            string tableName = baseModel.GetType().Name.Substring(0, baseModel.GetType().Name.Length - 5);
            string sql = "delete from " + tableName + " where ID=" + baseModel.GetType().GetProperty("ID").GetValue(baseModel, null).ToString();
            cmd = new SqlCommand(sql, cnn, tran);

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SqlException se)
            {
                tran.Rollback();
                throw new Exception("Delete " + tableName + " error :" + se.Message);
            }
        }

        /// <summary>
        /// Xóa dữ liệu thông qua Primary Key
        /// -- Nguyễn Trung Kiên - 29/9/2009 --
        /// </summary>
        /// <param name="tableName">Tên Table</param>
        /// <param name="PKID">Primary Key</param>
        public void Delete(string tableName, int PKID)
        {
            string sql = "delete from " + tableName + " where ID=" + PKID;
            cmd = new SqlCommand(sql, cnn, tran);

            try
            {
                if (logger.IsDebugEnabled && sql.IndexOf("SELECT") == -1)
                    logger.Info(sql);
                cmd.ExecuteNonQuery();
            }
            catch (SqlException se)
            {
                logger.Error(sql + "=> Error:" + se.Message);
                tran.Rollback();
                throw new Exception("Delete " + tableName + " error :" + se.Message);
            }
            finally
            {
                var idRegex = new System.Text.RegularExpressions.Regex(@"ID\s*=\s*(\d+)");
                var match = idRegex.Match(sql);
                var ID = match.Success ? match.Groups[1].Value : "";  // Extract ID if found, else blank
                ThreadContext.Properties["KeyID"] = ID.ToString();

                // Extract ActionType (INSERT, UPDATE, DELETE, SELECT)
                var actionRegex = new System.Text.RegularExpressions.Regex(@"^(INSERT|UPDATE|DELETE|SELECT)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                var actionMatch = actionRegex.Match(sql);
                var actionType = actionMatch.Success ? actionMatch.Groups[1].Value.ToUpper() : "UNKNOWN";  // Extract action type or set as UNKNOWN

                ThreadContext.Properties["ActionType"] = actionType;

                // Extract TableName using regex (assuming table name follows the action keyword)

                ThreadContext.Properties["TableName"] = tableName;

                if (logger.IsDebugEnabled)
                    logger.Info(sql);

            }
        }

        /// <summary>
        /// Xóa dữ liệu theo Attribute
        /// -- Nguyễn Trung Kiên - 29/9/2009 --
        /// </summary>
        /// <param name="tableName">Tên table</param>
        /// <param name="FieldName">Tên Fiels</param>
        /// <param name="FieldValue">Giá trị của field</param>
        public void DeleteByAttribute(string tableName, string FieldName, string FieldValue)
        {
            string sql = "delete from " + tableName + " where " + FieldName + "=" + FieldValue;
            cmd = new SqlCommand(sql, cnn, tran);

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SqlException se)
            {
                tran.Rollback();
                throw new Exception("Delete " + tableName + " error :" + se.Message);
            }
        }

        public virtual void DeleteByExpression(string tableName, Expression exp)
        {
            string sql = "delete from " + tableName + " where " + exp.ToString();
            cmd = new SqlCommand(sql, cnn, tran);

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SqlException se)
            {
                tran.Rollback();
                throw new Exception("Delete " + tableName + " error :" + se.Message);
            }
        }

        #endregion

        #region Cac method thuc hien voi store procedure
        public static void ExcuteNonQuery(string SPName, string ParamName, object ParamValue)
        { }

        /// <summary>
        /// Lay ve ngay thang cua he thong.
        /// </summary>
        /// <returns></returns>
        public DateTime GetSystemDate()
        {
            try
            {
                return Convert.ToDateTime(this.Select("SELECT GETDATE() AS SystemDate").Rows[0][0]);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion

        #region Mo Dong ket noi va Transaction

        /// <summary>
        /// Mở kết nối để thực hiện transacion
        /// -- Nguyễn Trung Kiên - 29/9/2009 --
        /// </summary>
        public void OpenConnection()
        { cnn.Open(); return; }

        /// <summary>
        /// Bắt đầu thực hiện Transaction
        /// -- Nguyễn Trung Kiên - 29/9/2009 --
        /// </summary>
        public void BeginTransaction()
        { tran = cnn.BeginTransaction(); return; }

        /// <summary>
        /// Xác nhận lại các giao dịch
        /// -- Nguyễn Trung Kiên - 29/9/2009 --
        /// </summary>
        public void CommitTransaction()
        {
            tran.Commit();
            return;
        }

        /// <summary>
        /// Hủy bỏ các giao dịch
        /// -- NHT --
        /// </summary>
        public void RollBack()
        {
            tran.Rollback();
            return;
        }

        /// <summary>
        /// Đóng kết nối
        /// -- Nguyễn Trung Kiên - 29/9/2009 --
        /// </summary>
        public void CloseConnection()
        {
            if (cnn.State == ConnectionState.Open)
                cnn.Close();
        }

        #endregion

        /// <summary>
        /// Lấy ngày Business Date
        /// -- Duongna --, 03/09/09
        /// <returns> BusinessDate </returns>
        public DateTime GetBusinessDate()
        {
            DateTime DateTimeValue = DateTime.Today;
            DateTimeValue = ((BusinessDateModel)BusinessDateBO.Instance.FindAll()[0]).BusinessDate;
            return DateTimeValue;
            //return Global.GetBusinessDate;
        }

        public DateTime GetBusinessDateTime()
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
        public decimal ExchangeCurrency(DateTime date, string FromCurrencyID, string ToCurrencyID, decimal Amount)
        {
            try
            {
                return Convert.ToDecimal(getTable("spExchangeCurrency", "Table", new SqlParameter("@DateTime", date),
                      new SqlParameter("@FromCurrency", FromCurrencyID),
                      new SqlParameter("@ToCurrency", ToCurrencyID),
                      new SqlParameter("@Amount", Amount)).Rows[0][0].ToString());
            }
            catch { return 0; }
        }
        private void SetHistory(string Table, string Function, string Content)
        {
            HistoryContent += "" + (CountTransaction + 1) + "." + Function + " " + Table + " : " + Content + "\r\n";
            CountTransaction++;
        }
        private void InsertHistory()
        {
            try
            {
                mELE = new EventsLogErrorModel();
                mELE.MessageCode = "DeadLock";
                mELE.ComputerName = System.Net.Dns.GetHostName();
                mELE.ErrorDate = DateTime.Now;
                mELE.EventName = "";
                mELE.FormName = "";
                mELE.ErrorContent = HistoryContent;
                EventsLogErrorBO.Instance.Insert(mELE);
            }
            catch
            { return; }
        }
        public void UpdateCommand(string command)
        {
            try
            {
                cmd = new SqlCommand("spSearchAllForTrans", cnn, tran);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@sqlCommand", command));

                // Ghi váo history để sư dụng tra cứu DeadLock.
                SetHistory("", Header_UpdateCommand, command);
                // Excute
                cmd.ExecuteNonQuery();
            }
            catch (SqlException se)
            {
                // Rollback Transaction.
                RollBack();
                // Lấy ra index nếu lỗi đã được định nghĩa.
                int Index = Array.IndexOf(NUMBER_OF_EXCEPTION, se.Number);
                // Nếu mã lỗi đã được định nghĩa
                if (Index != -1)
                {
                    // Nếu là lỗi Deadlock -> Insert vào Log.
                    if (se.Number == NUMBER_OF_DEADLOCK)
                    { InsertHistory(); }
                    // Throw 1 lỗi đã được định nghĩa.
                    throw new Exception(Header_Update + DESCRIPTION_OF_EXCEPTION[Index]);
                }
                // Nếu mã lỗi chưa được định nghĩa -> Throw 1 exception.
                else { throw new Exception(Header_Update + se.Message); }
            }
            catch (Exception ex)
            { RollBack(); throw new Exception(Header_Update + ex.Message); }
        }

        public void UpdateDataBase(string command)
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

        public string GetSystemTime()
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
    }

}
