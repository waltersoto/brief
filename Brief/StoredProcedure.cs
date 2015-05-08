
using System.Data;
using System.Data.SqlClient;


namespace Brief
{
    public class StoredProcedure : Command
    {

        private int _timeOut = -1;
        public StoredProcedure() { }

        public StoredProcedure(string name)
        {
            SqlCommand = new SqlCommand(name) { CommandType = CommandType.StoredProcedure};
        }

        public string Name
        {
            set
            {
                SqlCommand = new SqlCommand(value) { CommandType = CommandType.StoredProcedure};
                if (_timeOut > 0)
                {
                    SqlCommand.CommandTimeout = _timeOut;
                }
            }
        }

        public int TimeOut
        {
            set
            {
                if (SqlCommand != null)
                {
                    
                    SqlCommand.CommandTimeout = value;
                }
                else
                {
                    _timeOut = value;
                }
            }
        }


    }
}
