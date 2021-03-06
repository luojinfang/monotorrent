using System;
using System.Collections.Generic;
using System.Text;

namespace MonoTorrent.Client.Connections
{
    public static class ConnectionFactory
    {
        private static object locker = new object();
        private static Dictionary<string, Type> trackerTypes = new Dictionary<string, Type>();
       
        static ConnectionFactory()
        {
            RegisterTypeForProtocol("ipv4", typeof(IPV4Connection));
            RegisterTypeForProtocol("ipv6", typeof(IPV6Connection));
            RegisterTypeForProtocol("http", typeof(HttpConnection));
        }

        public static void RegisterTypeForProtocol(string protocol, Type connectionType)
        {
            if (string.IsNullOrEmpty(protocol))
                throw new ArgumentException("cannot be null or empty", "protocol");
            if (connectionType == null)
                throw new ArgumentNullException("connectionType");

            lock (locker)
                trackerTypes[protocol] = connectionType;
        }

        public static IConnection Create(Uri connectionUri)
        {
            if (connectionUri == null)
                throw new ArgumentNullException("connectionUrl");

            if (connectionUri.Scheme == "ipv4" && connectionUri.Port == -1)
                return null;

            Type type;
            lock (locker)
                if (!trackerTypes.TryGetValue(connectionUri.Scheme, out type))
                    return null;

            try
            {
                return (IConnection)Activator.CreateInstance(type, connectionUri);
            }
            catch
            {
                return null;
            }
        }
    }
}
