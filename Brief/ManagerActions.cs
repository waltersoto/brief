﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Brief
{ 

    public class ManagerActions : IDisposable
    {
        private const string SupportDataTable = "system.data.datatable";
        private const string SupportDataSet = "system.data.dataset";
        private const string SupportDataRow = "system.data.datarow";
        private const string SupportDataRowCollection = "system.data.datarowcollection";

     
        private SqlConnection _conn;
        private SqlCommand _cmd;

       
        internal ManagerActions(ConnectionString cs)
        {
            ConnectionString = cs;
        }

        private static bool HasColumn(IDataRecord r, string name)
        {
            for (var i = 0; i < r.FieldCount; i++)
            {
                if (r.GetName(i).Equals(name, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }    
            }
            return false;
        }

        private static T Cast<T>(object obj)
        {

            if (obj == null || obj == DBNull.Value)
            {
                return default(T);
            }
           
            T result;

            try
            {
                result = (T)Convert.ChangeType(obj, typeof(T));
            }
            catch (FormatException)
            {
                result = default(T);
            }

            return result;
            
        }

        private static T MapDataRecord<T>(IDataRecord dr, out bool found)
        {
            var result = Activator.CreateInstance<T>();
            var ps = result.GetType().GetProperties();
            found = false;

            foreach (var p in ps)
            {
                var m = (MapTo[])p.GetCustomAttributes(typeof(MapTo), false);
                var name = p.Name;
                if (m.Length > 0)
                {
                    name = m[0].MapColumn;
                }
                
                if(!HasColumn(dr,name)) continue; 
                if (dr[name] == DBNull.Value) continue;
                found = true;
                p.SetValue(result, dr[name], null);
            }

            return result;
        }

        private static T Map<T>(DataRow dr, out bool found)
        {
            var result = Activator.CreateInstance<T>();
            var ps = result.GetType().GetProperties();
            found = false;

            foreach (var p in ps)
            {
                var m = (MapTo[])p.GetCustomAttributes(typeof(MapTo), false);
                var name = p.Name;
                if (m.Length > 0)
                {
                    name = m[0].MapColumn;
                }

                if (!dr.Table.Columns.Contains(name)) continue;
                if (dr[name] == DBNull.Value) continue;
                found = true;
                p.SetValue(result, dr[name], null);
            }

            return result;
        }

        /// <summary>
        /// Command to execute
        /// </summary>
        /// <param name="cmd">Command</param>
        internal void With(SqlCommand cmd)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));
            _cmd = cmd;
        }
         
        /// <summary>
        /// Get a set of data
        /// </summary>
        /// <typeparam name="T">Set Type</typeparam>
        /// <remarks>
        /// Ex. 
        /// DataTable, DataSet, DataRow, 
        /// DataRowCollection, Data Model (a class with public properties)
        /// </remarks>
        /// <returns>T</returns>
        public T Get<T>()
        {
            T result;

            var support = typeof (T).FullName;

            switch (support.ToLower())
            {
                case SupportDataTable: 
                    result = (T)Convert.ChangeType(GetDataTable(_cmd), typeof(T));
                    break;
                case SupportDataSet: 
                    result = (T)Convert.ChangeType(GetDataSet(_cmd), typeof(T));
                    break;
                case SupportDataRow:
                    result = (T)Convert.ChangeType(GetRow(_cmd), typeof(T));
                    break;
                case SupportDataRowCollection:
                    result = (T)Convert.ChangeType(GetRowCollection(_cmd), typeof(T));
                    break;
                default:
                    result = (T)Convert.ChangeType(MapTo<T>(_cmd), typeof(T));
                    break;
            }

            return result;
        }

    
        /// <summary>
        /// Get list of items from command
        /// </summary>
        /// <typeparam name="T">Model Type</typeparam>
        /// <returns>T</returns>
        public List<T> GetListOf<T>()
        { 
            return MapToList<T>(_cmd);
        }

        /// <summary>
        /// Execute non query
        /// </summary>
        /// <returns>Number of affected rows</returns>
        public int Execute()
        {
            return ExecuteNonQuery(_cmd);
        }

        /// <summary>
        /// Get a reader
        /// </summary>
        /// <returns>IDataReader</returns>
        public IDataReader Reader()
        {
            return Read(_cmd);
        }

        public void ReadInto<T>(Action<T> callback)
        {
            var reader = Read(_cmd);

            try
            {
                while (reader.Read())
                {
                    bool result;
                    callback(MapDataRecord<T>(reader, out result));
                    if (reader.IsClosed)
                    {
                        break;
                    }
                }
            }
            finally
            {
                reader.Close();
                reader.Dispose();
            }
        }

        /// <summary>
        /// Execute a reader into a callback action
        /// </summary>
        /// <param name="callback">Action</param>
        public void ReadInto(Action<IDataRecord> callback)
        {
            var reader = Read(_cmd);

            try
            {
                while (reader.Read())
                {
                    callback(reader);
                    if (reader.IsClosed)
                    {
                        break;
                    }
                }
            }
            finally
            {
                reader.Close();
                reader.Dispose();
            } 
        }

        /// <summary>
        /// Get a list of output parameters 
        /// </summary>
        /// <returns></returns>
        public List<SqlParameter> OutputParameters()
        { 
            return SelectParamaters(_cmd,
                x =>
                    x.Direction == ParameterDirection.Output || 
                    x.Direction == ParameterDirection.InputOutput);
        }

        /// <summary>
        /// Get a list of output parameters casted as T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>Parameters</returns>
        public List<T> OutputParametersAs<T>()
        {
            return SelectParamaters(_cmd, 
                x =>
                    x.Direction == ParameterDirection.Output || 
                    x.Direction == ParameterDirection.InputOutput).Select(Cast<T>).ToList();
        }

        /// <summary>
        /// Get a list of return parameters
        /// </summary>
        /// <returns>Parameters</returns>
        public SqlParameter ReturnParameter()
        {
            return SelectParamaters(_cmd,
                x => x.Direction == ParameterDirection.ReturnValue).FirstOrDefault();
        }

        /// <summary>
        /// Get a list of return parameters as T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>Parameters</returns>
        public T ReturnParameterAs<T>()
        {
            return SelectParamaters(_cmd, 
                x => x.Direction == ParameterDirection.ReturnValue).Select(Cast<T>).FirstOrDefault();
        }

        /// <summary>
        /// Execute scalar as T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>T</returns>
        public T ScalarTo<T>()
        { 
            var obj = ExecuteScalar(_cmd);
            if (obj == null || obj == DBNull.Value)
            {
                return default(T);
            }
           
            return (T)Convert.ChangeType(obj, typeof(T)); 
        }

        /// <summary>
        /// Execute scalar
        /// </summary>
        /// <returns></returns>
        public object Scalar()
        {
            return ExecuteScalar(_cmd);
        }

        private void Connect()
        {
            if (_conn == null)
            {
                _conn = new SqlConnection(ConnectionString.ConnectionString);
            }

            if (_conn.State == ConnectionState.Open) return;

            _conn.Open();
            
            #if DEBUG
               Console.WriteLine("Opening connection...");
            #endif
        }

        #region "Commands"

        private IDataReader Read(SqlCommand cmd)
        {
            IDataReader reader;

            try
            {
                Connect();
                cmd.Connection = _conn;
               reader = cmd.ExecuteReader();
                
            }
            finally
            {
                cmd.Dispose();
            }

            return reader;

        }

        private List<SqlParameter> SelectParamaters(SqlCommand cmd,Func<SqlParameter,bool> where)
        {
            var l = new List<SqlParameter>();
            try
            {
                Connect();
                cmd.Connection = _conn;
                cmd.ExecuteNonQuery();

                l.AddRange(cmd.Parameters.Cast<SqlParameter>().Where(where));
            }
            finally
            {
                cmd.Dispose();
            }

            return l;
        } 

        private T MapTo<T>(SqlCommand cmd)
        {
            var o = default(T);
            
            var dr = GetRow(cmd);

            if (dr == null) return o;

            bool found;
            o = Map<T>(dr, out found);

            return o;

        }

        private List<T> MapToList<T>(SqlCommand cmd)
        {
            var l = new List<T>();

            var rows = GetRowCollection(cmd);
            
            foreach (DataRow dr in rows)
            {
                if (typeof(T).IsValueType || typeof(T) == typeof(string))
                {
                    if (dr.ItemArray.Any())
                    {
                        l.Add(Cast<T>(dr[0]));
                    }
                }
                else
                {
                    bool found;
                    var o = Map<T>(dr, out found);

                    if (found)
                    {
                        l.Add(o);
                    }

                }


            }
             

            return l;

        }

        private DataRowCollection GetRowCollection(SqlCommand cmd)
        {
            return GetDataTable(cmd).Rows;
        }

        private DataRow GetRow(SqlCommand cmd)
        {
            var dt = GetDataTable(cmd);
            return dt?.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        private DataTable GetDataTable(SqlCommand cmd)
        {
            var adapter = new SqlDataAdapter();
            var dt = new DataTable();
            try
            {
                Connect();
                cmd.Connection = _conn;
                adapter = new SqlDataAdapter(cmd);
                adapter.Fill(dt);
            } 
            finally
            {
                adapter.Dispose();
                cmd.Dispose();
            }

            return dt;
        }

        private DataSet GetDataSet(SqlCommand cmd)
        {
             
            var adapter = new SqlDataAdapter();
            var ds = new DataSet();
            try
            {
                Connect();
                cmd.Connection = _conn;
                adapter = new SqlDataAdapter(cmd);
                adapter.Fill(ds);
            }
            finally
            {
                adapter.Dispose();
                cmd.Dispose();
            } 
            return ds;

        }

        private object ExecuteScalar(SqlCommand cmd)
        {
            object result;

            try
            {
                Connect();
                cmd.Connection = _conn;
                result = cmd.ExecuteScalar();
            } 
            finally
            {
                cmd.Dispose(); 
            }

            return result;
        }

        private int ExecuteNonQuery(SqlCommand cmd)
        {
            int result;

            try
            {
                Connect();
                cmd.Connection = _conn;
                result = cmd.ExecuteNonQuery();
            }
            finally
            {
                cmd.Dispose();
            }

            return result;
        }

        #endregion


        #region "Dispose"
        public void Dispose()
        {
            Dispose(true);
        }

        private bool _disposed;
         
        protected virtual void Dispose(bool disposing)
        {
            #if DEBUG
              Console.WriteLine("Closing actions...");
            #endif
            if (_disposed) return;
            if (disposing)
            {
                if (_conn != null)
                {
                    _conn.Close();
                    _conn.Dispose();
         
                }
            }
            _disposed = true;
        }

        #endregion

        public ConnectionString ConnectionString { set; get; }

    }
}
