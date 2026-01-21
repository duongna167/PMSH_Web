using BaseBusiness.exception;
using BaseBusiness.util;
using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace BaseBusiness.bc
{
	public class BaseBO
	{
		protected BaseFacade baseFacade = null;

		protected BaseBO()
		{
		}

		public virtual void PreSavingChecking(BaseModel model)
		{
            DateTime goodDT = GlobalConstant.MIN_DATE;
            DateTime badDT = new DateTime(1, 1, 1);
            foreach (PropertyInfo p in model.GetType().GetProperties())
            {
                if (p.PropertyType.FullName == "System.DateTime"
                    && DateTime.Parse(p.GetValue(model, null).ToString()).CompareTo(badDT) == 0)
                    p.SetValue(model, goodDT, null);
            }
			return;
		}

		public virtual long Insert(BaseModel model)
		{
			try
			{
				PreSavingChecking(model);
				return baseFacade.Insert(model);
			}
            catch (BOException boe)
            {
                throw boe;
            }
			catch (Exception ex)
			{
				throw new BOException("Could not update to database: " + ex.Message);
			}
		}

		public virtual long Insert(BaseModel model, SqlConnection conn, SqlTransaction tx)
		{
			try
			{
				PreSavingChecking(model);
				return baseFacade.Insert(model, conn, tx);
			}
			catch (BOException boe)
			{
				throw boe;
			}
			catch (Exception ex)
			{
				throw new BOException("Could not update to database: " + ex.Message);
			}
		}

		public virtual void Update(BaseModel model)
		{
			try
			{
				PreSavingChecking(model);
				baseFacade.Update(model);
			}
            catch (BOException boe)
            {
                throw boe;
            }
			catch (Exception ex)
			{
				throw new BOException("Could not update to database: " + ex.Message);
			}
		}

		public virtual void DeleteByExpression(Expression exp)
		{
			try
			{
				baseFacade.DeleteByExpression(exp);
			}
            catch (BOException boe)
            {
                throw boe;
            }
			catch (Exception ex)
			{
				throw new BOException("Could not delete object: " + ex.Message);
			}
		}

		public virtual void Delete(long IDValue)
		{
			try
			{
				baseFacade.Delete(IDValue);
			}
            catch (BOException boe)
            {
                throw boe;
            }
			catch (Exception ex)
			{
				throw new BOException("Could not delete object: " + ex.Message);
			}
		}

		public virtual void Delete(ArrayList list)
		{
			try
			{
				baseFacade.Delete(list);
			}
            catch (BOException boe)
            {
                throw boe;
            }
			catch (Exception ex)
			{
				throw new BOException("Could not delete object: " + ex.Message);
			}
		}

		public BaseModel FindByPrimaryKey(long value)
		{
			try
			{
				return baseFacade.FindByPrimaryKey(value);
			}
            catch (BOException boe)
            {
                throw boe;
            }
			catch (Exception fx)
			{
				throw new BOException("Could not find the object: " + fx.Message);
			}
		}

		public ArrayList FindByPrimaryKey(ArrayList list) //list of PKs
		{
			try
			{
				return baseFacade.FindByPrimaryKey(list);
			}
            catch (BOException boe)
            {
                throw boe;
            }
			catch (Exception fx)
			{
				throw new BOException("Could not find the object: " + fx.Message);
			}
		}

		public ArrayList FindByPrimaryKey(string list) //list of PKs separated with comma
		{
			try
			{
				return baseFacade.FindByPrimaryKey(list);
			}
			catch (Exception fx)
			{
				throw new BOException("Could not find the object: " + fx.Message);
			}
		}

		public ArrayList FindByExpression(Expression exp)
		{
			try
			{
				return baseFacade.FindByExpression(exp);
			}
			catch (Exception e)
			{
				throw new BOException("Could not find the object: " + e.Message);
			}
		}

		public ArrayList FindByExpression(Expression exp, string orderby)
		{
			try
			{
				return baseFacade.FindByExpression(exp,orderby);
			}
			catch (Exception e)
			{
				throw new BOException("Could not find the object: " + e.Message);
			}
		}
		public ArrayList FindByExpression(Expression exp, long pageNo, long pageSize)
		{
			try
			{
				return baseFacade.FindByExpression(exp, pageNo, pageSize);
			}
			catch (Exception e)
			{
				throw new BOException("Could not find the object: " + e.Message);
			}
		}
		public ArrayList FindByExpression(Expression exp, long pageNo, long pageSize,string orderBy)
		{
			try
			{
				return baseFacade.FindByExpression(exp,pageNo,pageSize,orderBy);
			}
			catch (Exception e)
			{
				throw new BOException("Could not find the object: " + e.Message);
			}
		}
        public ArrayList FindByExpression(int top, Expression exp, string orderby)
        {
            try
            {
                return baseFacade.FindByExpression(top, exp, orderby);
            }
            catch (Exception e)
            {
                throw new BOException("Could not find the object: " + e.Message);
            }
        }

        public ArrayList FindByExpression(int top, Expression exp)
        {
            try
            {
                return baseFacade.FindByExpression(top, exp);
            }
            catch (Exception e)
            {
                throw new BOException("Could not find the object: " + e.Message);
            }
        }

		public ArrayList FindByAttribute(string field, string value)
		{
			try
			{
				return baseFacade.FindByAttribute(field, value);
			}
			catch (Exception e)
			{
				throw new BOException("Could not find the object: " + e.Message);
			}
		}

		public ArrayList FindByAttribute(string field, long value)
		{
			try
			{
				return baseFacade.FindByAttribute(field, value);
			}
			catch (Exception e)
			{
				throw new BOException("Could not find the object: " + e.Message);
			}
		}

		public ArrayList FindAll()
		{
			try
			{
				return baseFacade.FindAll();
			}
			catch (Exception fx)
			{
				throw new BOException("Can not find" + fx.Message);
			}
		}

		public virtual void DeleteByAttribute(string name, string value)
		{
			try
			{
				baseFacade.DeleteByAttribute(name, value);
			}
			catch (Exception fx)
			{
				throw new BOException("Can not delete any entity with condition " + name + " = " + value + ": " + fx.Message);
			}
		}

		public virtual void DeleteByAttribute(string name, Int64 value)
		{
			try
			{
				baseFacade.DeleteByAttribute(name, value);
			}
			catch (Exception fx)
			{
				throw new BOException("Can not delete any entity with condition " + name + " = " + value + ": " + fx.Message);
			}
		}

		public Hashtable LazyLoad()
		{
			try
			{
				return baseFacade.LazyLoad();
			}
			catch (Exception fx)
			{
				throw new BOException("Could not find the object: " + fx.Message);
			}
		}

		public Hashtable LazyLoad(string name)
		{
			try
			{
				return baseFacade.LazyLoad(name);
			}
			catch (Exception fx)
			{
				throw new BOException("Could not find the object: " + fx.Message);
			}
		}

		public Hashtable LazyLoad(string field1, string field2)
		{
			try
			{
				return baseFacade.LazyLoad(field1, field2);
			}
			catch (Exception fx)
			{
				throw new BOException("Could not find the object: " + fx.Message);
			}
		}

		public bool CheckExist(string field, Int64 value)
		{
			try
			{
				return baseFacade.CheckExist(field, value);
			}
			catch (Exception fx)
			{
				throw new BOException("Could not find the object: " + fx.Message);
			}
		}
		public ArrayList FindHierarchicallyByPK(int PK, string parentFieldName) 
		{
			try
			{
				return baseFacade.FindHierarchicallyByPK(PK,parentFieldName);
			}
			catch (FacadeException fx)
			{
				throw new BOException(fx.Message);
			}
		}
		public long CountRecord(Expression exp)
		{
			try
			{
				return baseFacade.CountRecord(exp);
			}
			catch (FacadeException fx)
			{
				throw new BOException(fx.Message);
			}
		}

        public List<T> GetList<T>(string query, object parameters = null)
        {
            try
            {
                return baseFacade.GetList<T>(query, parameters);
            }
            catch (Exception e)
            {
                throw new BOException("Could not find the object: " + e.Message);
            }
        }
    
		public T GetFirst<T>(string query, object parameters = null)
		{
            try
            {
                return baseFacade.GetFirst<T>(query, parameters);
            }
            catch (Exception e)
            {
                throw new BOException("Could not find the object: " + e.Message);
            }
        }

        public int Execute<T>(string query, List<T> parameters)
        {
            try
            {
                return baseFacade.Execute<T>(query, parameters);
            }
            catch (Exception ex)
            {
                throw new BOException("Could not find the object: " + ex.Message);
            }
        }

        public void ExecuteFast<T>(string query, List<T> parameters)
        {
            try
            {
                baseFacade.ExecuteFast<T>(query, parameters);
            }
            catch (Exception ex)
            {
                throw new BOException("Could not find the object: " + ex.Message);
            }
        }
        public virtual string InsertStringId(BaseModel model)
        {
            try
            {
                PreSavingChecking(model);
                return baseFacade.InsertStringId(model); // g?i xu?ng BaseFacade x? lý
            }
            catch (BOException boe)
            {
                throw boe;
            }
            catch (Exception ex)
            {
                throw new BOException("Could not update to database: " + ex.Message);
            }
        }
        public virtual string InsertStringNoneId(BaseModel model)
        {
            try
            {
                PreSavingChecking(model);
                return baseFacade.InsertStringNoneId(model); // g?i xu?ng BaseFacade x? lý
            }
            catch (BOException boe)
            {
                throw boe;
            }
            catch (Exception ex)
            {
                throw new BOException("Could not update to database: " + ex.Message);
            }
        }
        public virtual string InsertStringId(BaseModel model, SqlConnection conn, SqlTransaction tx)
        {
            try
            {
                PreSavingChecking(model);
                return baseFacade.InsertStringId(model, conn, tx);
            }
            catch (BOException boe)
            {
                throw boe;
            }
            catch (Exception ex)
            {
                throw new BOException("Could not update to database: " + ex.Message);
            }
        }
        public virtual string UpdateStringId(BaseModel model)
        {
            try
            {
                PreSavingChecking(model);
                return baseFacade.UpdateStringId(model); // g?i xu?ng BaseFacade x? lý
            }
            catch (BOException boe)
            {
                throw boe;
            }
            catch (Exception ex)
            {
                throw new BOException("Could not update to database: " + ex.Message);
            }
        }

        public virtual string UpdateStringId(BaseModel model, SqlConnection conn, SqlTransaction tx)
        {
            try
            {
                PreSavingChecking(model);
                return baseFacade.UpdateStringId(model, conn, tx); // g?i xu?ng BaseFacade x? lý
            }
            catch (BOException boe)
            {
                throw boe;
            }
            catch (Exception ex)
            {
                throw new BOException("Could not update to database: " + ex.Message);
            }
        }
        public virtual string DeleteStringId(BaseModel model)
        {
            try
            {
                return baseFacade.DeleteStringId(model); // g?i xu?ng Facade x? lý
            }
            catch (BOException boe)
            {
                throw boe;
            }
            catch (Exception ex)
            {
                throw new BOException("Could not delete from database: " + ex.Message);
            }
        }

        public virtual string DeleteStringId(BaseModel model, SqlConnection conn, SqlTransaction tx)
        {
            try
            {
                return baseFacade.DeleteStringId(model, conn, tx); // g?i xu?ng Facade x? lý
            }
            catch (BOException boe)
            {
                throw boe;
            }
            catch (Exception ex)
            {
                throw new BOException("Could not delete from database: " + ex.Message);
            }
        }
        public bool IsDuplicate(string field, object value, long id = 0)
        {
            if (baseFacade == null) return false;
            return baseFacade.CheckDuplicate(field, value, id);
        }

    }
}