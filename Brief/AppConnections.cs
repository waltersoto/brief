using System.Collections;
using System.Collections.Generic;
using System.Linq; 
namespace Brief
{
    public class AppConnections : IEnumerable
    {
        private static volatile AppConnections instance;
        private static readonly object SyncRoot = new object();

        private AppConnections()
        {
            connections = new Dictionary<string, ConnectionString>();
        }

        private Dictionary<string, ConnectionString> connections;

        public ConnectionString Default => connections.Count > 0 ? 
                                           connections.FirstOrDefault().Value : 
                                           new ConnectionString();

        public bool Contains(string key)
        {
            return connections.ContainsKey(key);
        }

        public bool IsEmpty => connections.Count == 0;

        public ConnectionString this[string key]
        {
            set
            {
                if (connections.ContainsKey(key))
                {
                    connections[key] = value;
                }
                else
                {
                    connections.Add(key, value);
                }
            }

            get
            {
                return connections.ContainsKey(key) ? connections[key] : new ConnectionString();
            }
        }
        public void Reset()
        {
            connections = new Dictionary<string, ConnectionString>();
        }

        public static AppConnections Connection
        {
            get
            {
                if (instance != null) return instance;
                lock (SyncRoot)
                {
                    if (instance == null)
                    {
                        instance = new AppConnections();
                    }
                }

                return instance;
            }
        }

        public override string ToString()
        {
            return  Default.ConnectionString;
        }


        #region IEnumerable Members


        public IEnumerator GetEnumerator()
        {
            return (connections.Values as IEnumerable).GetEnumerator();
        }


        #endregion


    }

}
