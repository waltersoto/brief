using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace Brief
{
    public class Manager : IDisposable
    {
        private readonly ManagerActions _actions;
        private bool _disposed;

        public Manager() : this(AppConnections.Connection.Default)
        {
        }

        public Manager(ConnectionString cs)
        {
            _actions = new ManagerActions(cs);
        }


        public ManagerActions With(SqlCommand cmd)
        {
            _actions.With(cmd);
            return _actions;
        }

        public void Transaction(List<SqlCommand> commandList)
        { 
            Transaction(commandList, null);
        }

        public void Transaction(List<SqlCommand> commandList, Action<int> rowsAffected)
        {
            using (var connection =
                new SqlConnection(_actions.ConnectionString.ConnectionString))
            {
                SqlTransaction transaction = null;

                try
                {
                    connection.Open();

                    transaction = connection.BeginTransaction();

                    foreach (var cmd in commandList.Where(cmd => cmd != null))
                    {
                        cmd.Connection = connection;
                        cmd.Transaction = transaction;
                        var r = cmd.ExecuteNonQuery();
                        cmd.Dispose();
                       
                        rowsAffected?.Invoke(r);
                    }


                    transaction.Commit();
                }
                catch
                {
                    transaction?.Rollback();
                    throw;
                }
                finally
                {
                    connection.Close();
                }
            }
 
        }

        #region "Dispose"

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                _actions.Dispose();

#if DEBUG
                Console.WriteLine("Closing connection...");
#endif
            }
            _disposed = true;
        }

        #endregion
    }
}