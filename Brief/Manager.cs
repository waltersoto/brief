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

        /// <summary>
        /// Manage data retrieval
        /// </summary>
        /// <param name="cs">ConnectionString</param>
        public Manager(ConnectionString cs)
        {
            _actions = new ManagerActions(cs);
        }

        /// <summary>
        /// Get actions with a command
        /// </summary>
        /// <param name="cmd">Command</param>
        /// <returns>ManagerActions</returns>
        public ManagerActions With(SqlCommand cmd)
        {
            _actions.With(cmd);
            return _actions;
        }

        /// <summary>
        /// Execute a list of commands as a transaction
        /// </summary>
        /// <param name="commandList">Command list</param>
        public void Transaction(List<SqlCommand> commandList)
        { 
            Transaction(commandList, null);
        }

        /// <summary>
        /// Execute a list of commands as a transaction
        /// </summary>
        /// <param name="commandList">Command list</param>
        /// <param name="rowsAffected">Action to received rows affected by each command</param>
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