using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Text;
using System.Net;
using System.Net.Sockets;
using OETS.Shared;
using OETS.Shared.Util;
using OETS.Shared.Opcodes;

namespace OETS.Journal.Client
{
    public class Client : IDisposable
    {
        #region events
        public event TimedEventHandler Connected;
        public event TimedEventHandler ConnectFailed;
        public event TimedEventHandler Disconnected;
        public event TimedEventHandler ServerDisconnected;
        public event ClientEventHandler CommandReceived;
        #endregion

        #region private static members
        private static Client instance;
        #endregion 

        #region private members
        private SSocket sSocket;
        private DateTime dtConnectedOn;
        private string user;
        private string ip;
        #endregion 
        
        #region IDisposable Members
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                if (sSocket != null)
                {
                    sSocket.Dispose();
                    sSocket = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);  // remove from finalization queue
        }
        #endregion

        #region Instance
        public static Client Instance
        {
            get
            {
                if (instance == null)
                {
                    Mutex mutex = new Mutex();
                    mutex.WaitOne();

                    if (instance == null)
                        instance = new Client();

                    mutex.Close();
                }

                return instance;
            }
        }
        #endregion

        #region properties
        /// <summary>
        /// Returns true if the socket is in connected state.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                if (sSocket != null && sSocket.Connected)
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Get the server's IP address.
        /// </summary>
        public IPAddress ServerIp
        {
            get
            {
                if (sSocket != null) 
                {
                    IPEndPoint ipEndPoint = sSocket.RemoteEndPoint;
                    if (ipEndPoint != null) 
                        return ipEndPoint.Address;
                }

                return IPAddress.None;
            }
        }

        /// <summary>
        /// Get the server's port.
        /// </summary>
        public int ServerPort
        {
            get
            {
                if (sSocket != null)
                {
                    IPEndPoint ipEndPoint = sSocket.RemoteEndPoint;
                    if (ipEndPoint != null)
                        return ipEndPoint.Port;
                }

                return IPEndPoint.MinPort;
            }
        }

        public DateTime ConnectedOn
        {
            get { return dtConnectedOn; }
        }

        public string User
        {
            get { return user; }
        }
        #endregion

        #region private constructor
        public Client()
        { }
        #endregion

        #region Connect(string user, string ip)
        /// <summary>
        /// Connects to the server asynchronously. On succesfull connect the 
        /// Connected event will be fired, else the ConnectFailed.
        /// </summary>
        public void Connect(string user, string ip)
        {
            if (this.IsConnected)
                return;

            this.user = user;
            this.ip = ip;

            string serverIp = Properties.Settings.Default.ServerIp;
            int serverPort = Properties.Settings.Default.ServerPort;
            IPEndPoint serverEndPoint = null;

            try
            { serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort); }
            catch (FormatException)
            { serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), serverPort); }
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);


            SocketAsyncEventArgs acceptEventArg = null;
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += AcceptEventCompleted;
                acceptEventArg.RemoteEndPoint = serverEndPoint;
                acceptEventArg.AcceptSocket = socket;
            }
            bool willRaiseEvent = socket.ConnectAsync(acceptEventArg);
            if (!willRaiseEvent)
            {
                ProcessConnect(acceptEventArg);
            }
            //socket.BeginConnect(serverEndPoint, new AsyncCallback(OnConnect), socket);
        }

        private void AcceptEventCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessConnect(e);
        }

        private void ProcessConnect(SocketAsyncEventArgs args)
        {
            try
            {
                Socket socket = args.AcceptSocket;
                if (socket == null || !socket.Connected)
                {
                    if (ConnectFailed != null)
                        ConnectFailed(this, new TimedEventArgs());
                    return;
                }

                // send keep alive after 10 minute of inactivity
                SockUtils.SetKeepAlive(socket, 600 * 1000, 60 * 1000);

                // the socket was connected, create the ChatSocket used for communication
                this.sSocket = new SSocket(ref socket);
                this.sSocket.Received += new EventHandler(OnChatSocketReceived);
                this.sSocket.Disconnected += new EventHandler(OnChatSocketDisconnected);

                // set the connected time
                this.dtConnectedOn = DateTime.Now;

                // fire the Connected event
                if (Connected != null)
                    Connected(this, new TimedEventArgs());

                sSocket.Receive();

                SendRequestLogin();
            }
            catch
            {
                if (ConnectFailed != null)
                    ConnectFailed(this, new TimedEventArgs());
            }
        }
        #endregion

        #region OnConnect(IAsyncResult ar)
        /// <summary>
        /// Ends the pending connection request to the server and requests login.
        /// </summary>
        private void OnConnect(IAsyncResult ar)
        {
            try
            {
                Socket socket = (Socket)ar.AsyncState;
                socket.EndConnect(ar);                
                // send keep alive after 10 minute of inactivity
                SockUtils.SetKeepAlive(socket, 600 * 1000, 60 * 1000);  

                // the socket was connected, create the ChatSocket used for communication
                this.sSocket = new SSocket(ref socket);
                this.sSocket.Received += new EventHandler(OnChatSocketReceived);
                this.sSocket.Disconnected += new EventHandler(OnChatSocketDisconnected);

                // set the connected time
                this.dtConnectedOn = DateTime.Now;

                // fire the Connected event
                if (Connected != null)
                    Connected(this, new TimedEventArgs());                

                sSocket.Receive();

                SendRequestLogin();
            }
            catch (Exception exc)
            {
                if (ConnectFailed != null)
                    ConnectFailed(this, new TimedEventArgs());

                if (!SockUtils.HandleSocketError(exc))
                    throw exc;
            }
        }
        #endregion

        #region SendRequestLogin()
        public void SendRequestLogin()
        {
            ResponsePacket metadata = new ResponsePacket();
            string metatype = metadata.GetType().FullName;

            metadata.From = this.user;
            metadata.To = "SSocketServer";
            metadata.Response = this.ip;

            SendCommand(ServerIp, OpcoDes.CMSG_REQUEST_USER_LOGIN, metatype, metadata);
        }
        #endregion

        #region OnChatSocketDisconnected
        void OnChatSocketDisconnected(object sender, EventArgs e)
        {
            SSocket cs = sender as SSocket;

            if (cs == null)
                return;

            cs.Close();
            cs.Dispose();
            cs = null;

            if (Disconnected != null)
                Disconnected(this, new TimedEventArgs());
        }
        #endregion

        #region OnChatSocketReceived
        private void OnChatSocketReceived(object sender, EventArgs e)
        {
            SSocket cs = sender as SSocket;

            if (cs == null)
                return;

            if (CommandReceived != null)
            {
                // Because a constant flow of packets can be received
                // we need to pass to the event a deep copy of the ChatSocket
                // or else the ChatSocket object members may be modified before 
                // the eventhandler processes them.                        
                // TODO: ChatSocket needs to use monitors
                SSocket chatSocketCopy = new SSocket(cs);
                CommandReceived(this, new SSEventArgs(chatSocketCopy));
            }

            // certain commands from the server require automatic response from the Client
            // ex: after user authenticated => request user list
            switch (cs.Command)
            {
                case OpcoDes.SMSG_SERVER_DISCONNECTED:
                    Disconnect();
                    if (ServerDisconnected != null)
                        ServerDisconnected(this, new TimedEventArgs());
                    break;

                default:
                    //sSocket.Receive();
                    break;
            }
        }
        #endregion

        #region Disconnect()
        /// <summary>
        /// Disconnect from the server.
        /// Events fired:
        ///     Disconnected
        /// </summary>
        public void Disconnect()
        {
            if (sSocket != null)
            {
                sSocket.Received -= new EventHandler(OnChatSocketReceived);
                sSocket.Disconnected -= new EventHandler(OnChatSocketDisconnected);

                sSocket.Close();
                sSocket.Dispose();
                sSocket = null;
            }

            if (Disconnected != null)
                Disconnected(this, new TimedEventArgs());
        }
        #endregion

        private AutoResetEvent mutexSent;

        #region SendCommand
        public void SendCommand(IPAddress target, OpcoDes command, string metatype, object metadata)
        {
            if (sSocket == null)
                return;
            sSocket.Sent += new EventHandler(SignalMutexSent);
            mutexSent = new AutoResetEvent(false);
            try
            {
                sSocket.Target = target;
                sSocket.Command = command;
                sSocket.Metatype = metatype;
                sSocket.Metadata = metadata;
                sSocket.Send();
                mutexSent.WaitOne();

            }
            catch (Exception exc)
            {
                Trace.Write(exc.Message);
            }
            finally
            {
                mutexSent.Close();
            }
            sSocket.Sent -= new EventHandler(SignalMutexSent);
        }
        #endregion

        #region SignalMutexSent
        private void SignalMutexSent(object sender, EventArgs e)
        {
            mutexSent.Set();
        }
        #endregion
    }   // Client
}
