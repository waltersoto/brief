using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Brief
{ 

    public class ManagerActions : IDisposable
    {
        private const string SUPPORT_DATA_TABLE = "system.data.datatable";
        private const string SUPPORT_DATA_SET = "system.data.dataset";
        private const string SUPPORT_DATA_ROW = "system.data.datarow";
        private const string SUPPORT_DATA_ROW_COLLECTION = "system.data.datarowcollection";
        private const string SUPPORT_IDATARECORD = "system.data.idatarecord";

        private const char BACK_SLASH = '\\';
        private const char FORWARD_SLASH = '/';
        private const char DBL_QUOTE = '"';


        private SqlConnection conn;
        private SqlCommand cmd;

       
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


        private static bool IsNumeric(object val)
        {
            if (val == null) return false;

            return val is int
                   || val is uint
                   || val is float
                   || val is double
                   || val is decimal
                   || val is long
                   || val is ulong
                   || val is sbyte
                   || val is byte
                   || val is short
                   || val is ushort;
        }

        private static string JsonEscape(string s,bool number)
        { 
            if (string.IsNullOrEmpty(s))
            {
                return "";
            }

            int i;
            int len = s.Length;
            var sb = new StringBuilder(len + 2);

            for (i = 0; i < len; i += 1)
            {
                char c = s[i];
                switch (c)
                {
                    case BACK_SLASH:
                    case DBL_QUOTE:
                        sb.Append('\\');
                        sb.Append(c);
                        break;
                    case FORWARD_SLASH:
                        sb.Append('\\');
                        sb.Append(c);
                        break;

                    default:
                        sb.Append(c);
                        break;
                }
            }
            return number? sb.ToString() : $"'{sb}'";
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

        private static T MapDataRecord<T>(IDataRecord dr)
        {
            var result = Activator.CreateInstance<T>();
            PropertyInfo[] ps = result.GetType().GetProperties();
         

            foreach (PropertyInfo p in ps)
            {
                var m = (MapTo[])p.GetCustomAttributes(typeof(MapTo), false);
                string name = p.Name;
                if (m.Length > 0)
                {
                    name = m[0].MapColumn;
                }
                
                if(!HasColumn(dr,name)) continue; 
                if (dr[name] == DBNull.Value) continue;
             
                p.SetValue(result, dr[name], null);
            }

            return result;
        }

        private static T Map<T>(DataRow dr, out bool found)
        {
            var result = Activator.CreateInstance<T>();
            PropertyInfo[] ps = result.GetType().GetProperties();
            found = false;

            foreach (PropertyInfo p in ps)
            {
                var m = (MapTo[])p.GetCustomAttributes(typeof(MapTo), false);
                string name = p.Name;
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
        /// <param name="command">Command</param>
        internal void With(SqlCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            cmd = command;
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

            string support = typeof (T).FullName;

            switch (support.ToLower())
            {
                case SUPPORT_DATA_TABLE: 
                    result = (T)Convert.ChangeType(GetDataTable(cmd), typeof(T));
                    break;
                case SUPPORT_DATA_SET: 
                    result = (T)Convert.ChangeType(GetDataSet(cmd), typeof(T));
                    break;
                case SUPPORT_DATA_ROW:
                    result = (T)Convert.ChangeType(GetRow(cmd), typeof(T));
                    break;
                case SUPPORT_DATA_ROW_COLLECTION:
                    result = (T)Convert.ChangeType(GetRowCollection(cmd), typeof(T));
                    break;
                default:
                    result = (T)Convert.ChangeType(MapTo<T>(cmd), typeof(T));
                    break;
            }

            return result;
        }

        /// <summary>
        /// Return dataset as a json string
        /// </summary>
        /// <returns>Json string</returns>
        public string JsonString()
        {
           
            var l = new List<string>();
            ReadInto(d =>
            {
                var t = new List<string>();
                
                for (var i = 0; i < d.FieldCount; i++)
                {
                    t.Add($"{d.GetName(i)}:{JsonEscape(d[i].ToString(),IsNumeric(d[i]))}");
                } 
                l.Add($"{{{string.Join(",",t)}}}");

            });

            return $"[{string.Join(string.Concat(",",Environment.NewLine),l)}]";
        }
    
        /// <summary>
        /// Get list of items from command
        /// </summary>
        /// <typeparam name="T">Model Type</typeparam>
        /// <returns>T</returns>
        public List<T> GetListOf<T>()
        { 
            return MapToList<T>(cmd);
        }

        /// <summary>
        /// Execute non query
        /// </summary>
        /// <returns>Number of affected rows</returns>
        public int Execute()
        {
            return ExecuteNonQuery(cmd);
        }

        /// <summary>
        /// Get a reader
        /// </summary>
        /// <returns>IDataReader</returns>
        public IDataReader Reader()
        {
            return Read(cmd);
        }

        public void ReadInto<T>(Action<T> callback)
        {
            IDataReader reader = Read(cmd);
            
            try
            {
                while (reader.Read())
                {
                    if (typeof (T).FullName.ToLower() == SUPPORT_IDATARECORD)
                    {
                        callback((T)reader);
                    }
                    else
                    {
                        callback(MapDataRecord<T>(reader));
                    }

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
           ReadInto<IDataRecord>(callback);
        }

        /// <summary>
        /// Get a list of output parameters 
        /// </summary>
        /// <returns></returns>
        public List<SqlParameter> OutputParameters()
        { 
            return SelectParamaters(cmd,
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
            return SelectParamaters(cmd, 
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
            return SelectParamaters(cmd,
                x => x.Direction == ParameterDirection.ReturnValue).FirstOrDefault();
        }

        /// <summary>
        /// Get a list of return parameters as T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>Parameters</returns>
        public T ReturnParameterAs<T>()
        {
            return SelectParamaters(cmd, 
                x => x.Direction == ParameterDirection.ReturnValue).Select(Cast<T>).FirstOrDefault();
        }

        /// <summary>
        /// Execute scalar as T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>T</returns>
        public T ScalarTo<T>()
        { 
            object obj = ExecuteScalar(cmd);
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
            return ExecuteScalar(cmd);
        }

        private void Connect()
        {
            if (conn == null)
            {
                conn = new SqlConnection(ConnectionString.ConnectionString);
            }

            if (conn.State == ConnectionState.Open) return;

            conn.Open();
            
            #if DEBUG
               Console.WriteLine("Opening connection...");
            #endif
        }

        #region "Commands"

        private IDataReader Read(SqlCommand command)
        {
            IDataReader reader;

            try
            {
                Connect();
                command.Connection = conn;
               reader = command.ExecuteReader();
                
            }
            finally
            {
                command.Dispose();
            }

            return reader;

        }

        private List<SqlParameter> SelectParamaters(SqlCommand command,Func<SqlParameter,bool> where)
        {
            var l = new List<SqlParameter>();
            try
            {
                Connect();
                command.Connection = conn;
                command.ExecuteNonQuery();

                l.AddRange(command.Parameters.Cast<SqlParameter>().Where(where));
            }
            finally
            {
                command.Dispose();
            }

            return l;
        } 

        private T MapTo<T>(SqlCommand command)
        {
            T o = default(T);
            
            DataRow dr = GetRow(command);

            if (dr == null) return o;

            bool found;
            o = Map<T>(dr, out found);

            return o;

        }

        private List<T> MapToList<T>(SqlCommand command)
        {
            var l = new List<T>();

            DataRowCollection rows = GetRowCollection(command);
            
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

        private DataRowCollection GetRowCollection(SqlCommand command)
        {
            return GetDataTable(command).Rows;
        }

        private DataRow GetRow(SqlCommand command)
        {
            DataTable dt = GetDataTable(command);
            return dt?.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        private DataTable GetDataTable(SqlCommand command)
        {
            var adapter = new SqlDataAdapter();
            var dt = new DataTable();
            try
            {
                Connect();
                command.Connection = conn;
                adapter = new SqlDataAdapter(command);
                adapter.Fill(dt);
            } 
            finally
            {
                adapter.Dispose();
                command.Dispose();
            }

            return dt;
        }

        private DataSet GetDataSet(SqlCommand command)
        {
             
            var adapter = new SqlDataAdapter();
            var ds = new DataSet();
            try
            {
                Connect();
                command.Connection = conn;
                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
            }
            finally
            {
                adapter.Dispose();
                command.Dispose();
            } 
            return ds;

        }

        private object ExecuteScalar(SqlCommand command)
        {
            object result;

            try
            {
                Connect();
                command.Connection = conn;
                result = command.ExecuteScalar();
            } 
            finally
            {
                command.Dispose(); 
            }

            return result;
        }

        private int ExecuteNonQuery(SqlCommand command)
        {
            int result;

            try
            {
                Connect();
                command.Connection = conn;
                result = command.ExecuteNonQuery();
            }
            finally
            {
                command.Dispose();
            }

            return result;
        }

        #endregion


        #region "Dispose"
        public void Dispose()
        {
            Dispose(true);
        }

        private bool disposed;
         
        protected virtual void Dispose(bool disposing)
        {
            #if DEBUG
              Console.WriteLine("Closing actions...");
            #endif
            if (disposed) return;
            if (disposing)
            {
                if (conn != null)
                {
                    conn.Close();
                    conn.Dispose();
         
                }
            }
            disposed = true;
        }

        #endregion

        public ConnectionString ConnectionString { set; get; }

    }
}
