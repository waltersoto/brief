

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Brief.Interfaces {
    public interface IManagerActions {
        T Get<T>();
        string JsonString();
        IEnumerable<T> GetListOf<T>();
        int Execute();
        IDataReader Reader();
        void ReadInto<T>(Action<T> callback);
        void ReadInto(Action<IDataRecord> callback);
        IEnumerable<SqlParameter> OutputParameters();
        IEnumerable<T> OutputParametersAs<T>();
        SqlParameter ReturnParameter();
        T ReturnParameterAs<T>();
        T ScalarTo<T>();
        object Scalar();
        ConnectionString ConnectionString { set; get; }
        SqlCommand Command { get; }
    }
}
