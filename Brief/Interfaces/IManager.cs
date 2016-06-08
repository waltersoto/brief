

using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Brief.Interfaces {
    public interface IManager {
        IManagerActions With(SqlCommand cmd);
        void Transaction(IEnumerable<SqlCommand> commandList);
        void Transaction(IEnumerable<SqlCommand> commandList, Action<int> rowsAffected);
    }
}
