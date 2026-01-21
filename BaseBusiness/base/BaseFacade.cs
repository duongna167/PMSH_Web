using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Transactions;
using System.Xml;
using BaseBusiness.exception;
using BaseBusiness.util;
using BaseBusiness.Utils;
using Dapper;
using log4net;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;

namespace BaseBusiness.bc
{
    public class BaseFacade
    {
        protected string strcon;
        protected string className;
        protected string tableName;
        protected BaseModel baseModel = new BaseModel();
        //private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly ILog logger = LogManager.GetLogger("DatabaseLogger");

        protected BaseFacade(string conn)
        {
            strcon = conn;
            className = this.GetType().Name;
            className = className.Substring(0, className.Length - 6) + "Model";
            tableName = className.Substring(0, className.Length - 5);
            ThreadContext.Properties["TableName"] = tableName;
        }

        public BaseFacade(BaseModel baseModel, string conn)
        {
            strcon = conn;
            this.baseModel = baseModel;
            className = this.GetType().Name;
            className = className.Substring(0, className.Length - 6) + "Model";
            tableName = className.Substring(0, className.Length - 5);
            ThreadContext.Properties["TableName"] = tableName;
        }

        //**************************************************************************		
        public string ClassName//TGT trick
        {
            set { className = value; }
        }

        public virtual BaseModel FindByPrimaryKey(long value)
        {
            return FindModel(string.Format("SELECT * FROM {0} WITH (NOLOCK) WHERE ID = {1} ", tableName, value));
        }


        public virtual ArrayList FindByPrimaryKey(ArrayList list) //list of PKs
        {
            return FindByPrimaryKey(PropertyUtils.ToListWithComma(list));
        }

        public virtual ArrayList FindByPrimaryKey(string list) //string of PKs separated by comma
        {

            return ExecuteSQL(string.Format("SELECT * FROM {0} WITH (NOLOCK) WHERE ID IN ({1})", tableName, list));
        }

        public BaseModel FindByUK(string field, string value)
        {
            //for speed
            if (value.IndexOf('\'') >= 0)
            {
                value = value.Replace("'", "\''");
            }
            return FindModel(string.Format("SELECT * FROM {0}  WITH (NOLOCK) WHERE {1} = '{2}'", tableName, field, value));
        }


        public ArrayList FindAll()
        {
            return ExecuteSQL("SELECT * FROM " + tableName + baseModel.GetOrderBy() + " WITH (NOLOCK)");
        }

        public ArrayList FindByAttribute(string field, string value)
        {
            return FindByExpression(new Expression(field, value));
        }

        public ArrayList FindByAttribute(string field, long value)
        {
            return FindByExpression(new Expression(field, value));
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
        protected ArrayList ExecuteSQLWithParameters(Expression exp, string orderBy = "", int top = 0)
        {
            ArrayList result = new ArrayList();
            string sql = $"SELECT " + (top > 0 ? "TOP " + top : string.Empty) + $" * FROM {tableName} WHERE {exp.ToSQLParametersString()}" + (string.IsNullOrEmpty(orderBy) ? baseModel.GetOrderBy() : " ORDER BY " + orderBy);
            try
            {
                using (SqlConnection connection = new SqlConnection(strcon))
                {
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        command.CommandTimeout = 6000;
                        command.CommandType = CommandType.Text;
                        foreach (Expression expression in exp.ToList())
                        {
                            if (expression.op.ToString().Contains("IN"))
                            {
                                //Passing the List of Object into Parameters
                                command.AddArrayParameters($"{expression.exp1}", expression.exp2.ToString().Trim('(', ')', ' ')
                                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(s => s.Trim())
                                        .ToArray());
                            }
                            else
                            {
                                //Passing the value into Parameters
                                command.Parameters.AddWithValue($"@{expression.exp1}", ConvertToDataType(expression.exp2.ToString()));
                            }
                        }

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(PopulateModel(reader, className));
                            }
                        }

                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                logger.Error(sql + "=> Error:" + ex.Message);
                throw new Exception(ex.Message);
            }
        }


        /*
		 * TuanLA@2023-08-21
		 * Modify this function to support passing the values through SQL Parameters. 
		 * If Expression contains subquery, it will use by the old way
		 * If not, it will use the new function ExecuteSQLWithParameters
		 */
        public ArrayList FindByExpression(Expression exp)
        {
            //if (exp.ToString().Contains(" FROM "))
            //{
            return ExecuteSQL(string.Format("SELECT * FROM {0} WITH (NOLOCK) WHERE {1}" + baseModel.GetOrderBy(), tableName, exp.ToString()));
            //}
            //else
            //{
            //	return ExecuteSQLWithParameters(exp);
            //}
        }

        /*
		 * TuanLA@2023-08-21
		 * Modify this function to support passing the values through SQL Parameters. 
		 * If Expression contains subquery, it will use by the old way
		 * If not, it will use the new function ExecuteSQLWithParameters
		 */
        public ArrayList FindByExpression(Expression exp, string orderBy)
        {
            //if (exp.ToString().Contains(" FROM "))
            //{
            return ExecuteSQL(string.Format("SELECT * FROM {0} WITH (NOLOCK) WHERE {1} ORDER BY {2}", tableName, exp.ToString(), orderBy));
            //}
            //else
            //{
            //	return ExecuteSQLWithParameters(exp, orderBy);
            //}
        }

        /*
		 * TuanLA@2023-08-23
		 * Modify this function to support passing the values through SQL Parameters. 
		 * If Expression contains subquery, it will use by the old way
		 * If not, it will use the new function ExecuteSQLWithParameters
		 */
        public ArrayList FindByExpression(int top, Expression exp, string orderBy)
        {
            //if (exp.ToString().Contains(" FROM "))
            //{
            string sql = string.Format("SELECT * FROM {0} WHERE {1} ORDER BY {2}", tableName, exp.ToString(), orderBy);
            if (top > 0)
            {
                sql = DBUtils.SQLTop(sql, top);
            }
            return ExecuteSQL(sql);
            //}
            //else
            //{
            //	return ExecuteSQLWithParameters(exp, orderBy, top);
            //}

        }

        /*
		 * TuanLA@2023-08-23
		 * Modify this function to support passing the values through SQL Parameters. 
		 * If Expression contains subquery, it will use by the old way
		 * If not, it will use the new function ExecuteSQLWithParameters
		 */
        public ArrayList FindByExpression(int top, Expression exp)
        {
            //if (exp.ToString().Contains(" FROM "))
            //{
            string sql = string.Format("SELECT * FROM {0} WHERE {1} ", tableName, exp.ToString());
            if (top > 0)
            {
                sql = DBUtils.SQLTop(sql, top);
            }
            return ExecuteSQL(sql);
            //}
            //else
            //{
            //	return ExecuteSQLWithParameters(exp, string.Empty, top);
            //}
        }

        public ArrayList FindByExpression(Expression exp, long pageNo, long pageSize)
        {
            string sql = string.Format("SELECT * FROM {0} WITH (NOLOCK) WHERE {1}" + baseModel.GetOrderBy(), tableName, exp.ToString());

            if (pageNo > 0)
                return ExecuteSQL(sql, pageNo, pageSize);
            else
                return FindByExpression(exp);
        }


        public ArrayList FindByExpression(Expression exp, long pageNo, long pageSize, string orderBy)
        {
            string sql = string.Format("SELECT * FROM {0} WITH (NOLOCK) WHERE {1} ORDER BY {2}" + baseModel.GetOrderBy(), tableName, exp.ToString(), orderBy);
            if (pageNo > 0)
                return ExecuteSQL(sql, pageNo, pageSize);
            else
                return FindByExpression(exp);
        }

        public Hashtable LazyLoad()
        {
            return LazyLoad(tableName + "Name");
        }

        public Hashtable LazyLoad(string name)
        {
            return LazyLoad("ID", name);
        }

        public Hashtable LazyLoad(string field1, string field2)
        {
            string sql = string.Format("SELECT {0} AS f1, {1} AS f2 FROM {2}", field1, field2, tableName);
            return LazyLoadToHashtable(sql);
        }

        //the sql must have only 2 fields [f1] [f2] in SELECT
        protected Hashtable LazyLoadToHashtable(string sql)
        {
            SqlConnection conn = new SqlConnection(strcon);
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;
            try
            {
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                Hashtable result = new Hashtable();
                while (reader.Read())
                {
                    if (!result.Contains(reader["f1"]))
                        result.Add(reader["f1"], reader["f2"]);
                }
                return result;
            }
            catch (Exception ex)
            {
                logger.Error(sql + "=> Error:" + ex.Message);
                throw new Exception("Cannot excecute query: " + ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        protected BaseModel FindModel(string sql)
        {
            SqlConnection conn = new SqlConnection(strcon);
            //log.Debug(sql);
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = sql;
            try
            {
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                if (reader.Read())
                {
                    return PopulateModel(reader, className);
                }
                return null;
            }
            catch (Exception ex)
            {
                logger.Error(sql + "=> Error:" + ex.Message);
                throw new Exception(ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        public virtual long Insert(BaseModel baseModel)
        {
            long ID = 0;
            SqlConnection conn = new SqlConnection(strcon);
            string sql = DBUtils.SQLInsert(baseModel);
            string sqlText = sql;
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandTimeout = 6000;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = sql;
            ThreadContext.Properties["ActionType"] = "INSERT";
            ThreadContext.Properties["TableName"] = baseModel.GetType().Name.Replace("Model", string.Empty);
            foreach (PropertyInfo p in baseModel.GetType().GetProperties())
            {
                //ignore ID column, it is automatically increased
                //if (p.Name.Equals(tableName + "ID")) continue;
                if (p.Name.Equals("ID")) continue;

                object value = p.GetValue(baseModel, null);
                if (value != null)
                {
                    cmd.Parameters.Add("@" + p.Name, DBUtils.ConvertToSQLType(p.PropertyType)).Value = value;
                    sqlText = sqlText.Replace("@" + p.Name + ",", value.ToString() + ",");
                    sqlText = sqlText.Replace("@" + p.Name + ")", value.ToString() + ")");

                }
                else
                {
                    cmd.Parameters.Add("@" + p.Name, DBUtils.ConvertToSQLType(p.PropertyType)).Value = "";
                    sqlText = sqlText.Replace("@" + p.Name + ",", "'',");
                    sqlText = sqlText.Replace("@" + p.Name + ")", "'')");
                }
            }
            try
             {
                conn.Open();
                ID = Convert.ToInt64(cmd.ExecuteScalar());
                return ID;
            }
            catch (SqlException e)
            {
                logger.Error(sqlText + "=> Error:" + e.Message);
                throw new Exception(e.Message);
            }
            finally
            {
                ThreadContext.Properties["KeyID"] = ID.ToString();
                if (logger.IsDebugEnabled)
                    logger.Info(sqlText + "=> ID=" + ID);

                conn.Close();
            }

        }

        /* Override Insert method in BaseFacade to use available SQL connection */
        public long Insert(BaseModel model, SqlConnection conn, SqlTransaction tx)
        {
            long ID = 0;
            string sql = DBUtils.SQLInsert(model);
            SqlCommand cmd = conn.CreateCommand();
            cmd.Transaction = tx;

            cmd.CommandTimeout = 6000;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = sql;
            string sqlText = sql;
            ThreadContext.Properties["ActionType"] = "INSERT";
            ThreadContext.Properties["TableName"] = baseModel.GetType().Name.Replace("Model", string.Empty);
            foreach (PropertyInfo p in model.GetType().GetProperties())
            {
                //ignore ID column, it is automatically increased
                //if (p.Name.Equals(tableName + "ID")) continue;
                if (p.Name.Equals("ID")) continue;

                object value = p.GetValue(model, null);
                if (value != null)
                {
                    cmd.Parameters.Add("@" + p.Name, DBUtils.ConvertToSQLType(p.PropertyType)).Value = value;
                    sqlText = sqlText.Replace("@" + p.Name + ",", value.ToString() + ",");
                    sqlText = sqlText.Replace("@" + p.Name + ")", value.ToString() + ")");
                }
                else
                {
                    cmd.Parameters.Add("@" + p.Name, DBUtils.ConvertToSQLType(p.PropertyType)).Value = "";
                    sqlText = sqlText.Replace("@" + p.Name + ",", "'',");
                    sqlText = sqlText.Replace("@" + p.Name + ")", "'')");
                }
            }
            try
            {
                ID = Convert.ToInt64(cmd.ExecuteScalar());
                return ID;
            }
            catch (SqlException e)
            {
                logger.Error(sqlText + "=> Error:" + e.Message);
                throw new Exception(e.Message);
            }
            finally
            {
                ThreadContext.Properties["KeyID"] = ID.ToString();
                if (logger.IsDebugEnabled)
                    logger.Info(sqlText);

            }
        }

        protected virtual int UpdateField(ArrayList list, string field, int value)
        {
            if (list == null || list.Count == 0) return 0;
            //string sql = string.Format("UPDATE {0} SET {1} = {2} WHERE {0}ID IN ({3})", tableName, field, value, PropertyUtils.ToListWithComma(list));
            string sql = string.Format("UPDATE {0} SET {1} = {2} WHERE ID IN ({3})", tableName, field, value, PropertyUtils.ToListWithComma(list));
            return ExecuteNonQuerySQL(sql);
        }

        protected virtual int UpdateField(ArrayList list, string field, string value)
        {
            if (list == null || list.Count == 0) return 0;
            //string sql = string.Format("UPDATE {0} SET {1} = '{2}' WHERE {0}ID IN ({3})", tableName, field, value, PropertyUtils.ToListWithComma(list));
            string sql = string.Format("UPDATE {0} SET {1} = '{2}' WHERE ID IN ({3})", tableName, field, value, PropertyUtils.ToListWithComma(list));
            return ExecuteNonQuerySQL(sql);
        }

        protected virtual int UpdateField(string field, int value, Expression exp)
        {
            string sql = string.Format("UPDATE {0} SET {1} = {2} WHERE " + exp.ToString(), tableName, field, value);
            return ExecuteNonQuerySQL(sql);
        }

        protected virtual int UpdateField(string field, string value, Expression exp)
        {
            string sql = string.Format("UPDATE {0} SET {1} = '{2}' WHERE " + exp.ToString(), tableName, field, value);
            return ExecuteNonQuerySQL(sql);
        }

        public virtual int Update(BaseModel baseModel)
        {
            //string ID = string.Empty;
            SqlConnection conn = new SqlConnection(strcon);
            string sql = DBUtils.SQLUpdate(baseModel);
            string sqlText = sql;
            SqlCommand cmd = new SqlCommand(sql, conn);

            ThreadContext.Properties["ActionType"] = "UPDATE";
            ThreadContext.Properties["TableName"] = baseModel.GetType().Name.Replace("Model", string.Empty);
            cmd.CommandType = CommandType.Text;
            PropertyInfo[] propertiesName = baseModel.GetType().GetProperties();
            for (int i = 0; i < propertiesName.Length; i++)
            {
                SqlDbType dbType = DBUtils.ConvertToSQLType(propertiesName[i].PropertyType);
                object value = propertiesName[i].GetValue(baseModel, null);
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
            try
            {
                conn.Open();
                return cmd.ExecuteNonQuery();
            }
            catch (SqlException se)
            {
                logger.Error(sqlText + "=> Error:" + se.Message);
                throw new Exception(se.Message);
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
                conn.Close();
            }
        }

        protected int ExecuteNonQuerySQL(string sql)
        {
            SqlConnection conn = new SqlConnection(strcon);
            SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 6000;
            try
            {
                if (logger.IsDebugEnabled && sql.IndexOf("SELECT") == -1)
                    logger.Info(sql);
                cmd.Connection.Open();
                return cmd.ExecuteNonQuery();
            }
            catch (SqlException se)
            {
                logger.Error(sql + "=> Error:" + se.Message);
                throw new Exception(se.Message);
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
                var tableRegex = new System.Text.RegularExpressions.Regex(@"\b(INSERT INTO|UPDATE|DELETE FROM|FROM)\s+([^\s]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                var tableMatch = tableRegex.Match(sql);
                var tableName = tableMatch.Success ? tableMatch.Groups[2].Value : "UnknownTable";  // Extract table name if found, else "UnknownTable"

                ThreadContext.Properties["TableName"] = tableName;

                if (logger.IsDebugEnabled)
                    logger.Info(sql);
                conn.Close();
            }
        }

        /* Used for transaction */
        protected int ExecuteNonQuerySQL(string sql, SqlConnection conn, SqlTransaction tx)
        {
            try
            {
                SqlCommand cmd = new SqlCommand(sql, conn, tx);
                var ID = baseModel.GetType()
                      .GetProperties()
                      .FirstOrDefault(p => p.Name.Equals("ID", StringComparison.OrdinalIgnoreCase))
                      ?.GetValue(baseModel) ?? "";
                ThreadContext.Properties["KeyID"] = ID.ToString();
                if (logger.IsDebugEnabled)
                    logger.Info(sql);
                cmd.CommandType = CommandType.Text;
                cmd.CommandTimeout = 6000;
                return cmd.ExecuteNonQuery();
            }
            catch (SqlException se)
            {
                logger.Error(sql + "=> Error:" + se.Message);
                throw new Exception(se.Message);
            }
        }


        public virtual void Delete(long IDValue)
        {
            //string sql = string.Format("DELETE FROM {0} WHERE {0}ID = {1}", tableName, IDValue);
            string sql = string.Format("DELETE FROM {0} WHERE ID = {1}", tableName, IDValue);
            ExecuteNonQuerySQL(sql);
        }

        public virtual void Delete(long IDValue, SqlConnection conn, SqlTransaction tx)
        {
            //string sql = string.Format("DELETE FROM {0} WHERE {0}ID = {1}", tableName, IDValue);
            string sql = string.Format("DELETE FROM {0} WHERE ID = {1}", tableName, IDValue);
            ExecuteNonQuerySQL(sql, conn, tx);
        }

        public virtual void Delete(ArrayList listID)
        {
            //string sql = string.Format("DELETE FROM {0} WHERE {0}ID IN ({1})", tableName, PropertyUtils.ToListWithComma(listID));
            string sql = string.Format("DELETE FROM {0} WHERE ID IN ({1})", tableName, PropertyUtils.ToListWithComma(listID));
            ExecuteNonQuerySQL(sql);
        }

        public virtual void DeleteByExpression(Expression exp)
        {
            string sql = "DELETE FROM " + tableName + " WHERE " + exp.ToString();
            ExecuteNonQuerySQL(sql);
        }

        /* Used for transaction */
        public virtual void DeleteByExpression(Expression exp, SqlConnection conn, SqlTransaction tx)
        {
            string sql = "DELETE FROM " + tableName + " WHERE " + exp.ToString();
            ExecuteNonQuerySQL(sql, conn, tx);
        }

        public void DeleteByAttribute(string name, string value)
        {
            DeleteByExpression(new Expression(name, value));
        }

        public void DeleteByAttribute(string name, long value)
        {
            DeleteByExpression(new Expression(name, value));
        }

        protected ArrayList ExecuteSQL(string sql)
        {
            SqlConnection conn = new SqlConnection(strcon);
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 6000;
            SqlDataReader reader = null;
            try
            {
                if (logger.IsDebugEnabled && sql.IndexOf("SELECT") == -1)
                    logger.Info(sql);

                conn.Open();
                reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                ArrayList result = new ArrayList();
                while (reader.Read())
                {
                    result.Add(PopulateModel(reader, className));
                }
                return result;
            }
            catch (Exception ex)
            {
                logger.Error(sql + "=> Error:" + ex.Message);
                throw new Exception(ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        /* Use for transaction */
        protected ArrayList ExecuteSQL(string sql, SqlConnection conn, SqlTransaction tx)
        {
            SqlCommand cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 6000;
            SqlDataReader reader = null;
            try
            {
                if (logger.IsDebugEnabled)
                    logger.Info(sql);
                reader = cmd.ExecuteReader(CommandBehavior.Default);
                ArrayList result = new ArrayList();
                while (reader.Read())
                {
                    result.Add(PopulateModel(reader, className));
                }
                return result;
            }
            catch (Exception ex)
            {
                logger.Error(sql + "=> Error:" + ex.Message);
                throw new Exception(ex.Message);
            }
            finally
            {
                if (reader != null) reader.Close();
            }
        }

        /* Execute SQL statement with not full query fields */
        protected ArrayList ExecuteSQL(string sql, ArrayList propertyList)
        {
            SqlConnection conn = new SqlConnection(strcon);
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 6000;
            SqlDataReader reader = null;
            try
            {
                if (logger.IsDebugEnabled)
                    logger.Info(sql);

                conn.Open();
                reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                ArrayList result = new ArrayList();
                while (reader.Read())
                {
                    result.Add(PopulateModel(reader, className, propertyList));
                }
                return result;
            }
            catch (Exception ex)
            {
                logger.Error(sql + "=> Error:" + ex.Message);
                throw new Exception(ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        //pageNo, pageSize starts from 1
        protected ArrayList ExecuteSQL(string sql, long pageNo, long pageSize)
        {
            //startRow starts from 1
            long startRow = pageSize * (pageNo - 1) + 1;
            long endRow = pageSize * pageNo;

            SqlConnection conn = new SqlConnection(strcon);

            SqlCommand cmd = conn.CreateCommand();


            sql = DBUtils.SQLTop(sql, endRow);
            int index = sql.ToUpper().IndexOf("ORDER BY");
            if (index == -1)
            {
                //sql += " ORDER BY " + tableName + "ID";
                sql += " ORDER BY ID";
            }

            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 6000;
            SqlDataReader reader = null;
            try
            {
                conn.Open();
                reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                ArrayList result = new ArrayList();
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i < startRow)
                    {
                        continue;
                    }
                    else if (i > endRow)
                    {
                        return result;
                    }
                    else
                    {
                        result.Add(PopulateModel(reader, className));
                    }

                }
                return result;
            }
            catch (Exception ex)
            {
                logger.Error(sql + "=> Error:" + ex.Message);
                throw new Exception(ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }
        protected ArrayList ExecuteSQLNoTop(string sql, long pageNo, long pageSize)
        {
            //startRow starts from 1
            long startRow = pageSize * (pageNo - 1) + 1;
            long endRow = pageSize * pageNo;

            SqlConnection conn = new SqlConnection(strcon);

            SqlCommand cmd = conn.CreateCommand();
            int index = sql.ToUpper().IndexOf("ORDER BY");
            if (index == -1)
            {
                //sql += " ORDER BY " + tableName + "ID";
                sql += " ORDER BY ID";
            }

            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 6000;
            SqlDataReader reader = null;
            try
            {
                conn.Open();
                reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                ArrayList result = new ArrayList();
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i < startRow)
                    {
                        continue;
                    }
                    else if (i > endRow)
                    {
                        return result;
                    }
                    else
                    {
                        result.Add(PopulateModel(reader, className));
                    }

                }
                return result;
            }
            catch (Exception ex)
            {
                logger.Error(sql + "=> Error:" + ex.Message);
                throw new Exception(ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }
        public long CountRecord(Expression exp)
        {
            string sql = $"SELECT COUNT(*) FROM {tableName} WHERE {exp}";

            using (SqlConnection conn = new SqlConnection(strcon))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandTimeout = 6000;

                try
                {
                    conn.Open();
                    object result = cmd.ExecuteScalar();
                    return Convert.ToInt64(result);
                }
                catch (Exception ex)
                {
                    logger.Error(sql + "=> Error:" + ex.Message);
                    throw new Exception(ex.Message);
                }
            }
        }
        protected virtual BaseModel PopulateModel(SqlDataReader reader, string className)
        {
            return PropertyUtils.PopulateModel(reader, className);
        }

        //New for VIVA
        //Populate only field of model in ArrayList
        protected virtual BaseModel PopulateModel(SqlDataReader reader, string className, ArrayList listProperty)
        {
            return PropertyUtils.PopulateModel(reader, className, listProperty);
        }


        public bool CheckExist(string field, long value)
        {
            string sql = $"SELECT TOP 1 {field} FROM {tableName} WHERE {field} = {value}";

            using (SqlConnection conn = new SqlConnection(strcon))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandTimeout = 6000;

                try
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        return reader.HasRows;
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(sql + "=> Error:" + ex.Message);
                    throw new Exception(ex.Message);
                }
            }
        }


        public object SelectTOP(string field, string order)
        {
            string sql = string.Format("SELECT TOP 1 {0} AS T FROM {1} ORDER BY {0} {2}", field, tableName, order);
            return ExecuteScalar(sql);
        }

        protected object ExecuteScalar(string sql)
        {
            SqlConnection conn = new SqlConnection(strcon);
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 6000;
            try
            {
                conn.Open();
                return cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                logger.Error(sql + "=> Error:" + ex.Message);
                throw new Exception(ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        public int SelectCount(string sql)
        {
            return int.Parse(ExecuteScalar(sql).ToString());
        }
        public virtual ArrayList FindHierarchicallyByPK(int PK, string parentFieldName)
        {
            ArrayList arr = new ArrayList();
            arr.Add(FindByPrimaryKey(PK));
            ArrayList chilrenArr = FindByAttribute(parentFieldName, PK);
            if (chilrenArr.Count > 0)
            {
                foreach (BaseModel child in chilrenArr)
                {
                    int childPK = Convert.ToInt32(child.GetType().GetProperty(tableName + "ID").GetValue(child, null));
                    arr.AddRange(FindHierarchicallyByPK(childPK, parentFieldName));
                }
            }
            return arr;
        }

        //ORM
        public List<T> GetList<T>(string query, object parameters = null)
        {
            List<T> result = new List<T>();
            using (SqlConnection con = new SqlConnection(strcon))
            {
                con.Open();
                result = con.Query<T>(query, parameters).ToList();
            }
            return result;
        }

        public T GetFirst<T>(string query, object parameters = null)
        {
            T result = default(T);
            using (SqlConnection con = new SqlConnection(strcon))
            {
                con.Open();
                result = con.Query<T>(query, parameters).FirstOrDefault();
            }
            return result;
        }

        public int Execute<T>(string query, List<T> parameters)
        {
            using (var con = new SqlConnection(strcon))
            {
                con.Open();
                return con.Execute(query, parameters);
            }
        }

        public void ExecuteFast<T>(string query, List<T> parameters)
        {
            using (var con = new SqlConnection(DBUtils.GetDBFastConnectionString()))
            {
                con.Open();
                using (var trans = con.BeginTransaction())
                {
                    try
                    {
                        foreach (var item in parameters)
                        {
                            con.Execute(query, item, transaction: trans);
                        }
                        trans.Commit();
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        throw new Exception(ex.Message);
                    }
                }
            }
        }
        public virtual string InsertStringId(BaseModel model)
        {
            using (SqlConnection conn = new SqlConnection(strcon))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    try
                    {
                        string result = InsertStringId(model, conn, tx);
                        tx.Commit();
                        return result;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }
        public virtual string InsertStringNoneId(BaseModel model)
        {
            using (SqlConnection conn = new SqlConnection(strcon))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    try
                    {
                        string result = InsertStringNoneId(model, conn, tx);
                        tx.Commit();
                        return result;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }
        public virtual string InsertStringId(BaseModel model, SqlConnection conn, SqlTransaction tx)
        {
            try
            {
                // L?y tên b?ng t? BaseModel
                string tableName = model.GetTableName();

                // L?y danh sách property ?? build SQL
                var props = model.GetType().GetProperties()
                                 .Where(p => p.CanRead)
                                 .ToList();

                List<string> columns = new List<string>();
                List<string> values = new List<string>();
                DynamicParameters parameters = new DynamicParameters();

                foreach (var p in props)
                {
                    var val = p.GetValue(model, null);
                    if (val != null)
                    {
                        columns.Add(p.Name);
                        values.Add("@" + p.Name);
                        parameters.Add("@" + p.Name, val);
                    }
                }

                string sql = $"INSERT INTO {tableName} ({string.Join(",", columns)}) " +
                             $"VALUES ({string.Join(",", values)}); " +
                             $"SELECT @{model.GetPrimaryKeyName()};"; // tr? v? PK string

                // Th?c thi v?i Dapper
                string insertedId = conn.ExecuteScalar<string>(sql, parameters, tx);
                return insertedId;
            }
            catch (Exception ex)
            {
                throw new FacadeException("InsertStringId failed: " + ex.Message);
            }
        }
        public virtual string InsertStringNoneId(BaseModel model, SqlConnection conn, SqlTransaction tx)
        {
            try
            {
                // L?y tên b?ng t? BaseModel
                string tableName = model.GetTableName();

                // L?y danh sách property ?? build SQL
                var props = model.GetType().GetProperties()
                                 .Where(p => p.CanRead)
                                 .ToList();

                List<string> columns = new List<string>();
                List<string> values = new List<string>();
                DynamicParameters parameters = new DynamicParameters();

                foreach (var p in props)
                {
                    var val = p.GetValue(model, null);
                    if (val != null)
                    {
                        columns.Add(p.Name);
                        values.Add("@" + p.Name);
                        parameters.Add("@" + p.Name, val);
                    }
                }

                string sql = $"INSERT INTO {tableName} ({string.Join(",", columns)}) " +
                             $"VALUES ({string.Join(",", values)}); "+
                        $"SELECT @{columns[0]};"; // tr? v? PK string


                // Th?c thi v?i Dapper
                string insertedId = conn.ExecuteScalar<string>(sql, parameters, tx);
                return insertedId;
            }
            catch (Exception ex)
            {
                throw new FacadeException("InsertStringId failed: " + ex.Message);
            }
        }
        public virtual string UpdateStringId(BaseModel model)
        {
            using (SqlConnection conn = new SqlConnection(strcon))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    try
                    {
                        string result = UpdateStringId(model, conn, tx);
                        tx.Commit();
                        return result;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public virtual string UpdateStringId(BaseModel model, SqlConnection conn, SqlTransaction tx)
        {
            try
            {
                string tableName = model.GetTableName();
                string primaryKey = model.GetPrimaryKeyName();

                var props = model.GetType().GetProperties()
                                 .Where(p => p.CanRead && p.Name != primaryKey)
                                 .ToList();

                List<string> setClauses = new List<string>();
                DynamicParameters parameters = new DynamicParameters();

                foreach (var p in props)
                {
                    var val = p.GetValue(model, null);
                    setClauses.Add($"{p.Name} = @{p.Name}");
                    parameters.Add("@" + p.Name, val);
                }

                var pkValue = model.GetType().GetProperty(primaryKey).GetValue(model, null);
                parameters.Add("@" + primaryKey, pkValue);

                string sql = $"UPDATE {tableName} SET {string.Join(", ", setClauses)} WHERE {primaryKey} = @{primaryKey}";

                conn.Execute(sql, parameters, tx);

                // Tr? v? PK d??i d?ng string
                return pkValue?.ToString() ?? "";
            }
            catch (Exception ex)
            {
                throw new FacadeException("UpdateStringId failed: " + ex.Message);
            }
        }
        public virtual string DeleteStringId(BaseModel model)
        {
            using (SqlConnection conn = new SqlConnection(strcon))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    try
                    {
                        string result = DeleteStringId(model, conn, tx);
                        tx.Commit();
                        return result;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public virtual string DeleteStringId(BaseModel model, SqlConnection conn, SqlTransaction tx)
        {
            try
            {
                string tableName = model.GetTableName();
                string pkName = model.GetPrimaryKeyName();

                string sql = $"DELETE FROM {tableName} WHERE {pkName} = @{pkName}";

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@" + pkName, model.GetStringID()); // GetID() tr? v? string PK

                int rowsAffected = conn.Execute(sql, parameters, tx);

                if (rowsAffected == 0)
                    throw new FacadeException("No record found to delete");

                return model.GetPrimaryKeyName(); // tr? v? ID ?ã xóa
            }
            catch (Exception ex)
            {
                throw new FacadeException("DeleteStringId failed: " + ex.Message);
            }
        }
        public bool CheckDuplicate(string field, object value, long excludeId = 0)
        {
            string sql = $"SELECT TOP 1 {field} FROM {tableName} WHERE {field} = @Value AND ID <> @ExcludeId";

            using (SqlConnection conn = new SqlConnection(strcon))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandTimeout = 6000;

                cmd.Parameters.AddWithValue("@Value", value ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ExcludeId", excludeId);

                try
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        return reader.HasRows;
                    }
                }
                catch (Exception ex)
                {
                    logger.Error($"{sql} (Value: {value}) => Error: {ex.Message}");
                    throw new Exception(ex.Message);
                }
            }
        }

    }
}