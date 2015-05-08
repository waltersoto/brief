using System.Collections;
using System.Collections.Generic;
using System.Linq; 
namespace Brief
{
    public class AppConnections : IEnumerable
    {
        private static volatile AppConnections _instance;
        private static readonly object SyncRoot = new object();

        private AppConnections()
        {
            _connections = new Dictionary<string, ConnectionString>();
        }

        private Dictionary<string, ConnectionString> _connections;

        public ConnectionString Default
        {
            get
            {
                if (_connections.Count > 0)
                {
                    return _connections.FirstOrDefault().Value;
                }

                return new ConnectionString();
            }
        }

        public bool Contains(string key)
        {
            return _connections.ContainsKey(key);
        }

        public bool IsEmpty => _connections.Count == 0;

        public ConnectionString this[string key]
        {
            set
            {

                if (_connections.ContainsKey(key))
                {
                    _connections[key] = value;
                }
                else
                {
                    _connections.Add(key, value);
                }
            }

            get
            {
                return _connections.ContainsKey(key) ? _connections[key] : new ConnectionString();
            }
        }
        public void Reset()
        {
            _connections = new Dictionary<string, ConnectionString>();
        }

        public static AppConnections Connection
        {
            get
            {
                if (_instance != null) return _instance;
                lock (SyncRoot)
                {
                    if (_instance == null)
                    {
                        _instance = new AppConnections();
                    }
                }

                return _instance;
            }
        }

        public override string ToString()
        {
            return  Default.ConnectionString;
        }


        #region IEnumerable Members


        public IEnumerator GetEnumerator()
        {
            return (_connections.Values as IEnumerable).GetEnumerator();
        }


        #endregion


    }

}
