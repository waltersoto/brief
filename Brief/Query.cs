
using System.Data;
using System.Data.SqlClient;

namespace Brief
{
    public class Query : Command
    {

        private int timeOut = -1;
        public Query() { }

        public Query(string queryTxt)
        {
            SqlCommand = new SqlCommand(queryTxt) { CommandType = CommandType.Text };
        }

        public string Text
        {
            set
            {
                SqlCommand = new SqlCommand(value) { CommandType = CommandType.Text };
                if (timeOut > 0)
                {
                    SqlCommand.CommandTimeout = timeOut;
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
                    timeOut = value;
                }
            }
        }


    }
}
