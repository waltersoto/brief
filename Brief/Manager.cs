using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Brief.Interfaces;

namespace Brief {
    public class Manager : IManager, IDisposable {
        private readonly ManagerActions actions;
        private bool disposed;

        public Manager() : this(AppConnections.Connection.Default) {
        }

        /// <summary>
        /// Manage data retrieval
        /// </summary>
        /// <param name="cs">ConnectionString</param> 
        public Manager(ConnectionString cs) {
            actions = new ManagerActions(cs);
        }

        /// <summary>
        /// Get actions with a command
        /// </summary>
        /// <param name="cmd">Command</param>
        /// <returns>ManagerActions</returns>
        public IManagerActions With(SqlCommand cmd) {
            actions.Command = cmd;
            return actions;
        }

        /// <summary>
        /// Execute a list of commands as a transaction
        /// </summary>
        /// <param name="commandList">Command list</param>
        public void Transaction(IEnumerable<SqlCommand> commandList) {
            Transaction(commandList, null);
        }

        /// <summary>
        /// Execute a list of commands as a transaction
        /// </summary>
        /// <param name="commandList">Command list</param>
        /// <param name="rowsAffected">Action to received rows affected by each command</param>
        public void Transaction(IEnumerable<SqlCommand> commandList, Action<int> rowsAffected) {
            using (var connection =
                new SqlConnection(actions.ConnectionString.ConnectionString)) {
                SqlTransaction transaction = null;

                try {
                    connection.Open();

                    transaction = connection.BeginTransaction();

                    foreach (var cmd in commandList.Where(cmd => cmd != null)) {
                        cmd.Connection = connection;
                        cmd.Transaction = transaction;
                        var r = cmd.ExecuteNonQuery();
                        cmd.Dispose();

                        rowsAffected?.Invoke(r);
                    }
                    transaction.Commit();
                } catch {
                    transaction?.Rollback();
                    throw;
                } finally {
                    connection.Close();
                }
            }

        }

        #region "Dispose"

        public void Dispose() {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing) {
            if (disposed) return;
            if (disposing) {
                actions.Dispose();

#if DEBUG
                Console.WriteLine("Closing connection...");
#endif
            }
            disposed = true;
        }

        #endregion
    }
}