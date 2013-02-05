using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace OETS.Server
{
    public struct ClientKey
    {
        public IPAddress ip;
        public int port;

        public ClientKey(IPAddress ip, int port)
        {
            this.ip = ip;
            this.port = port;
        }

        public override string ToString()
        {
            return this.ip.ToString() + ":" + this.port.ToString();
        }

        /// <summary>
        /// From the string representation reconstruct a ClientKey struct
        /// </summary>
        public static ClientKey FromString(string keyString)
        {
            string[] keys = keyString.Split(':');

            if (keys.Length != 2)
                return new ClientKey(IPAddress.Broadcast, 0);

            return new ClientKey(IPAddress.Parse(keys[0]), Int32.Parse(keys[1]));
        }
    }   // ClientKey
}
