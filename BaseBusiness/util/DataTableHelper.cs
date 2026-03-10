using BaseBusiness.bc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace BaseBusiness.util
{
    public class DataTableHelper
    {
        static public DataTable getTableData(string procedureName)
        {
            DataTable table = new DataTable();
            SqlConnection mySqlConnectionConnFromDB = null;
            try
            {
                string str = DBUtils.GetDBConnectionString();
                mySqlConnectionConnFromDB = new SqlConnection(str);
                using (var mySqlCommand = new SqlCommand(procedureName, mySqlConnectionConnFromDB))
                using (var mySqlDataAdapter = new SqlDataAdapter(mySqlCommand))
                {
                    mySqlConnectionConnFromDB.Open();
                    mySqlCommand.CommandType = CommandType.StoredProcedure;
                    mySqlDataAdapter.Fill(table);
                }
            }
            catch (SqlException se)
            {
                if (se.Class == 20)
                    throw new Exception("DB Connection Error");
                else
                    throw new Exception(se.Message);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (mySqlConnectionConnFromDB != null)
                {
                    mySqlConnectionConnFromDB.Close();
                    mySqlConnectionConnFromDB.Dispose();
                }
            }
            return table;
        }
        public static int ExecuteNonQuery(string storedProcedure, SqlParameter[] parameters)
        {
            string str = DBUtils.GetDBConnectionString();
            using (var connection = new SqlConnection(str)) // Define your connection string
            {
                using (var command = new SqlCommand(storedProcedure, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddRange(parameters);

                    connection.Open();
                    return command.ExecuteNonQuery(); // Returns the number of rows affected
                }
            }
        }
        public static int ExecuteNonQueryText(string sql, SqlParameter[] parameters)
        {
            string str = DBUtils.GetDBConnectionString();
            using (var connection = new SqlConnection(str))
            {
                using (var command = new SqlCommand(sql, connection))
                {
                    command.CommandType = CommandType.Text; // 👉 khác với SP
                    if (parameters != null)
                        command.Parameters.AddRange(parameters);

                    connection.Open();
                    return command.ExecuteNonQuery(); // trả về số rows ảnh hưởng
                }
            }
        }

        static public DataTable getTableData(string procedureName, SqlParameter[] mySqlParameter)
        {
            DataTable table = new DataTable();
            SqlConnection mySqlConnectionConnFromDB = null;
            try
            {
                string str = DBUtils.GetDBConnectionString();
                mySqlConnectionConnFromDB = new SqlConnection(str);
                using (var mySqlCommand = new SqlCommand(procedureName, mySqlConnectionConnFromDB))
                using (var mySqlDataAdapter = new SqlDataAdapter(mySqlCommand))
                {
                    mySqlConnectionConnFromDB.Open();
                    mySqlCommand.CommandType = CommandType.StoredProcedure;
                    mySqlCommand.CommandTimeout = 300;
                    if (mySqlParameter != null)
                    {
                        for (int i = 0; i < mySqlParameter.Length; i++)
                            mySqlCommand.Parameters.Add(mySqlParameter[i]);
                    }
                    mySqlDataAdapter.Fill(table);
                }
            }
            catch (SqlException se)
            {
                if (se.Class == 20)
                    throw new Exception("DB Connection Error");
                else
                    throw new Exception(se.Message);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (mySqlConnectionConnFromDB != null)
                {
                    mySqlConnectionConnFromDB.Close();
                    mySqlConnectionConnFromDB.Dispose();
                }
            }
            return table;
        }
        public static DataSet GetDataSet(string procedureName, SqlParameter[] mySqlParameter)
        {
            DataSet dataSet = new DataSet();
            SqlConnection mySqlConnectionConnFromDB = null;
            try
            {
                string str = DBUtils.GetDBConnectionString();
                mySqlConnectionConnFromDB = new SqlConnection(str);
                using (var mySqlCommand = new SqlCommand(procedureName, mySqlConnectionConnFromDB))
                using (var mySqlDataAdapter = new SqlDataAdapter(mySqlCommand))
                {
                    mySqlConnectionConnFromDB.Open();
                    mySqlCommand.CommandType = CommandType.StoredProcedure;
                    if (mySqlParameter != null)
                    {
                        for (int i = 0; i < mySqlParameter.Length; i++)
                            mySqlCommand.Parameters.Add(mySqlParameter[i]);
                    }
                    mySqlDataAdapter.Fill(dataSet);
                }
            }
            catch (SqlException se)
            {
                if (se.Class == 20)
                    throw new Exception("DB Connection Error");
                else
                    throw new Exception(se.Message);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (mySqlConnectionConnFromDB != null)
                {
                    mySqlConnectionConnFromDB.Close();
                    mySqlConnectionConnFromDB.Dispose();
                }
            }
            return dataSet;
        }
        public static int ExecuteInsertAndReturnId(string sql, SqlParameter[] parameters)
        {
            string str = DBUtils.GetDBConnectionString();
            using (var connection = new SqlConnection(str))
            {
                using (var command = new SqlCommand(sql, connection))
                {
                    command.CommandType = CommandType.Text; // vì có SELECT/OUTPUT
                    if (parameters != null)
                        command.Parameters.AddRange(parameters);

                    connection.Open();
                    object result = command.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }
        public static DataTable ExecuteQuery(string sql, SqlParameter[] parameters = null)
        {
            DataTable table = new DataTable();
            string str = DBUtils.GetDBConnectionString();

            using (var connection = new SqlConnection(str))
            using (var command = new SqlCommand(sql, connection))
            using (var adapter = new SqlDataAdapter(command))
            {
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }

                connection.Open();
                adapter.Fill(table);
            }

            return table;
        }


    }
}
