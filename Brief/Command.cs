using System;
using System.Collections.Generic;
using System.Data.SqlClient; 

namespace Brief
{
    public abstract class Command : IDisposable
    {
        protected Command()
        {
            Parameters = new Dictionary<string, object>();
        }

        public void Dispose()
        {
            sqlCommand?.Dispose();
        }

        public Dictionary<string, object> Parameters
        {
            set;
            get;
        }


        private SqlCommand sqlCommand;

        public SqlCommand SqlCommand
        {
            set { sqlCommand = value; }
            get
            {
                if (sqlCommand == null) return sqlCommand;


                if (sqlCommand.Parameters.Count >= 1 || Parameters.Count <= 0) return sqlCommand;

                foreach (KeyValuePair<string, object> p in Parameters)
                {
                    sqlCommand.Parameters.Add(new SqlParameter(p.Key, p.Value));
                }

                return sqlCommand;
            }
        }

        #region "Implicit casting to SqlCommand"

        public static implicit operator SqlCommand(Command self)
        { 
            return self.SqlCommand;
        }

        #endregion



    }
}
