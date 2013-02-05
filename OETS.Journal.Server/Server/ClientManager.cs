using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.ComponentModel;
using System.Net.Sockets;
using System.Diagnostics;
using OETS.Shared;
using OETS.Shared.Structures;

namespace OETS.Server
{
    /// <summary>
    /// For each connected client we create a ClientManager object, that 
    /// is responsible for communicating with the client.
    /// </summary>
    public class ClientManager
    {
        public event TimedEventHandler Disconnected;
        public event TimedEventHandler CommandReceived;

        #region private members
        private Socket socket;
        private IPEndPoint endPoint;
        private SSocket sSocket;
        private string username;
        private bool authenticated = false;
        private bool pinged = false;
        private System.Threading.Timer m_PingTimer;
        #endregion

        #region properties

        public System.Threading.Timer PingTimer
        {
            get { return m_PingTimer; }
            set { m_PingTimer = value; }
        }

        public bool IsPinged
        {
            get { return pinged; }
            set { pinged = value; }
        }

        public IPEndPoint IPEndPoint
        {
            get { return endPoint; }
        }

        public IPAddress IPAddress
        {
            get { return endPoint.Address; }
        }

        public int Port
        {
            get { return endPoint.Port; }
        }

        public SSocket SSocket
        {
            get { return sSocket; }
        }

        public ClientKey ClientKey
        {
            get
            {
                ClientKey clientKey = new ClientKey(endPoint.Address, endPoint.Port);
                return clientKey;
            }
        }

        public string UserName
        {
            get { return username; }
            set { username = value; }
        }

        /// <summary>
        /// Set to true if the client was authenticated in the system.
        /// </summary>
        public bool Authenticated
        {
            get { return authenticated; }
            set { authenticated = value; }
        }
        #endregion properties

        #region constructor
        /// <summary>
        /// A ClientManager is responsible for managing a connection from a client.
        /// For each client connected there will be one separate ClientManager object.
        /// The socket used by the ClientManager uses asynchronous call to Receive and 
        /// send data.
        /// </summary>
        public ClientManager(ref Socket clientSocket)
        {
            if (clientSocket == null)
                throw new Exception("Client socket is not initialized!");

            // the endpoint stores information from the client side
            socket = clientSocket;
            endPoint = (IPEndPoint)socket.RemoteEndPoint;

            SockUtils.SetKeepAlive(socket, 600 * 1000, 60 * 1000);

            sSocket = new SSocket(ref socket);
            sSocket.Received += new EventHandler(OnChatSocketReceived);
            sSocket.Disconnected += new EventHandler(OnChatSocketDisconnected);            
            sSocket.Receive();

            m_PingTimer = new System.Threading.Timer(m_PingTimerCallback);
        }

        private void m_PingTimerCallback(object sender)
        {
            SocketServer.Instance.PingTimerCallback(this);
        }

        void OnChatSocketReceived(object sender, EventArgs e)
        {
            if (sSocket == null || !sSocket.Connected)
                return;

            if (CommandReceived != null)
                CommandReceived(this, new TimedEventArgs());

            //sSocket.Receive();
        }

        void OnChatSocketDisconnected(object sender, EventArgs e)
        {
            if (Disconnected != null)
            {
                // only fire the Disconnected event if the user was authenticated
                if (this.authenticated)
                    Disconnected(this, new TimedEventArgs());
            }

            Disconnect();
        }
        #endregion constructor

        #region public Disconnect
        /// <summary>
        /// Disconnect gracefully from the server. 
        /// </summary>
        public void Disconnect()
        {
            if (this.socket != null)
            {
                if (socket.Connected) socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
        }
        #endregion
    }   // ClientManager
}
