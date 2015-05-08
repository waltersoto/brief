
using System.Data.Common;
 

namespace Brief
{
    public class ConnectionString : DbConnectionStringBuilder
    {

        public ConnectionString(){ }
        public ConnectionString(bool useOdbcRules) : base(useOdbcRules) { }

        public ConnectionString(string connectionString):this(false,connectionString)  { }

        public ConnectionString(bool useOdbcRules, string connectionString) : base(useOdbcRules)
        {
            ConnectionString = connectionString;
        }

        
    }
}
