

using System.Collections.Generic;
using System.Data.SqlClient;

namespace Brief.Interfaces {
    public interface ICommand {
        Dictionary<string, object> Parameters { set; get; }
        SqlCommand SqlCommand { set; get; }
    }
}
