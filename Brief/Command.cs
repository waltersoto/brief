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
            _sqlCommand?.Dispose();
        }

        public Dictionary<string, object> Parameters
        {
            set;
            get;
        }


        private SqlCommand _sqlCommand;

        public SqlCommand SqlCommand
        {
            set { _sqlCommand = value; }
            get
            {
                if (_sqlCommand == null) return _sqlCommand;


                if (_sqlCommand.Parameters.Count >= 1 || Parameters.Count <= 0) return _sqlCommand;

                foreach (var p in Parameters)
                {
                    _sqlCommand.Parameters.Add(new SqlParameter(p.Key, p.Value));
                }

                return _sqlCommand;
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
