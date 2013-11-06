using NLog;
using OETS.Shared;
using OETS.Shared.Opcodes;
using OETS.Shared.Structures;
using OETS.Shared.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace OETS.Server
{
    public enum ServerStatus
    {
        Stopped,
        Started
    }
    /// <summary>
    /// Singleton.
    /// </summary>
	public class SocketServer
    {
        #region events
        public event TimedEventHandler Started;
        public event TimedEventHandler Stopped;
        public event ClientEventHandler ClientDisconnected;
        public event ClientEventHandler LoginSuccess;
        public event ClientEventHandler LoginFailed;
        #endregion

        #region private members
        private static readonly Logger s_log = LogManager.GetCurrentClassLogger();
        private object lockObject = new Object();
        private static SocketServer instance;
        private Hashtable clients;
        private Queue<ClientManager> cmToDisconnect;
        private ServerStatus status = ServerStatus.Stopped;
        private IPEndPoint endPoint;
        private Socket _tcpListen;
        private DateTime dtConnectedOn;
        private static Dictionary<OpcoDes, Action<ClientManager, TimedEventArgs>> m_CommandHandler = new Dictionary<OpcoDes, Action<ClientManager, TimedEventArgs>>();
        
		#endregion

        #region Instance
        /// <summary>
        /// Returns the one and only server instance.
        /// </summary>
        public static SocketServer Instance
        {
            get
            {
                if (instance == null)
                {
                    Mutex mutex = new Mutex();
                    mutex.WaitOne();

                    if (instance == null)
                        instance = new SocketServer();

                    mutex.Close();
                }

                return instance;
            }
        }
        #endregion

        #region properties
        /// <summary>
        /// Returns the server IP address.
        /// </summary>
        public IPAddress Ip
        {
            get { return endPoint.Address; }
        }

        /// <summary>
        /// Returns the server receiving port.
        /// </summary>
        public int Port
        {
            get { return endPoint.Port; }
        }

        /// <summary>
        /// Returns the current status of the server.
        /// </summary>
        public ServerStatus Status
        {
            get { return status; }
        }

        /// <summary>
        /// Return connected date and time (when the server went online).
        /// </summary>
        public DateTime ConnectedOn
        {
            get { return dtConnectedOn; }
        }

        /// <summary>
        /// Returns number of clients connected.
        /// </summary>
        public int ClientCount
        {
            get { return clients.Count; }
        }

        /// <summary>
        /// Returns the ClientManager object associated with the clientKey.
        /// The communication between the server and the client is handled 
        /// by a ClientManager object identified by a ClientKey.
        /// </summary>
        public ClientManager this[ClientKey clientKey]
        {
            get { return (ClientManager)clients[clientKey]; }
        }
        #endregion
        
        #region SocketServer
        /// <summary>
        ///  Private constructor, to make this a singleton object.
        /// </summary>
		private SocketServer()
		{
            try
            {
                m_CommandHandler[OpcoDes.CMSG_REQUEST_USER_LOGIN] = HandleCMSG_REQUEST_USER_LOGIN;
                m_CommandHandler[OpcoDes.CMSG_PONG] = HandleCMSG_PONG;
                m_CommandHandler[OpcoDes.CMSG_SEND_JOURNAL_ENTRY] = HandleCMSG_SEND_JOURNAL_ENTRY;
                m_CommandHandler[OpcoDes.CMSG_GETTING_JOURNAL] = HandleCMSG_GETTING_JOURNAL;
                m_CommandHandler[OpcoDes.CMSG_GETTING_JOURNAL_2] = HandleCMSG_GETTING_JOURNAL_2;
            }
            catch (Exception ex)
            {
                LogUtil.FatalException(ex, false, "Ошибка создания класса SocketServer");
                Console.ReadKey(true);
                Environment.Exit(1);
            }
        }
		#endregion
        
        #region CommandHandlers

        private void HandleCMSG_SEND_JOURNAL_ENTRY(ClientManager cm, TimedEventArgs ea)
        {
            SSocket chatSocket = cm.SSocket;
            JournalPacket pck = (JournalPacket)chatSocket.Metadata;

            journal_contentData jd = pck.Data;
            if (JournalManager.Instance.Contains(jd.ID))
            {
                s_log.Info("Получили модифицированную запись с ID:" + jd.ID);
                JournalManager.Instance.Set(jd);

            }
            else if (JournalManager.Instance.Add(ref jd))
            {
                s_log.Info("Получили новую запись с ID:" + jd.ID);
            }

            JournalPacket data = new JournalPacket(jd);
            lock (clients.SyncRoot)
            {
                IEnumerator clientEnumerator = clients.GetEnumerator();
                while (clientEnumerator.MoveNext())
                {
                    DictionaryEntry entry = (DictionaryEntry)clientEnumerator.Current;
                    ClientManager c = (ClientManager)entry.Value;

                    SSocket ss = c.SSocket;
                    SendCommand(c, (pck.Data.ID != -1 ? OpcoDes.SMSG_JOURNAL_MODIFY : OpcoDes.SMSG_JOURNAL_ADD), data);
                }
            }
        }

        private void HandleCMSG_GETTING_JOURNAL(ClientManager cm, TimedEventArgs ea)
        {
            try
            {
                SSocket sSocket = cm.SSocket;
                if (!sSocket.Connected)
                    return;

                ResponsePacket d = (ResponsePacket)sSocket.Metadata;
                string news = new Smc(Smc.ServiceProviderEnum.TripleDES).Decrypt(d.Response);

                s_log.Debug("CMSG_GETTING_JOURNAL: {0} :: {1}:[{2}]", cm.ClientKey, ea.EventTime, news.Split(';').Length.ToString()/* + "::" + d.Response*/);
                
                string[] IdJournal = news.Split(';');
                for (int i = 0; i < IdJournal.Length; ++i)
                {
                    if (IdJournal[i] != "")
                    {
                        int _id = Convert.ToInt32(IdJournal[i].Split('-')[0]);
                        if (JournalManager.Instance.Contains(_id))
                        {
                            journal_contentData nct = JournalManager.Instance.FindByID(_id);

                            if (nct.ID > 0)
                            {
                                string md = IdJournal[i].Split('-')[1];
                                string pd = IdJournal[i].Split('-')[2];

                                if (string.IsNullOrEmpty(md) || md != nct.ModifyDate ||
                                    string.IsNullOrEmpty(pd) || pd != nct.Date)
                                {
                                    JournalPacket pck = new JournalPacket(nct);
                                    SendCommand(cm, OpcoDes.SMSG_JOURNAL_MODIFY_SYNC, pck);
                                    s_log.Trace("У клиента {0} :: Изменены записи: [{1}]", cm.ClientKey, _id);
                                }
                            }
                        }
                        else
                        {
                            ResponsePacket pck = new ResponsePacket("SocketServer", cm.UserName, _id.ToString());
                            SendCommand(cm, OpcoDes.SMSG_JOURNAL_REMOVE_SYNC, pck);
                            s_log.Trace("У клиента {0} :: Удалены записи: [{1}]", cm.ClientKey, _id);
                        }
                    }
                }
                SendResponse(cm, OpcoDes.SMSG_JOURNAL_SYNC_END, "SYNC_1");
            }
            catch (Exception exc)
            {
                LogUtil.ErrorException(exc, false, "HandleCMSG_GETTING_JOURNAL");
            }
        }
        
        private void HandleCMSG_GETTING_JOURNAL_2(ClientManager cm, TimedEventArgs ea)
        {
            DictionaryB idList = new DictionaryB();

            try
            {
                SSocket sSocket = cm.SSocket;
                if (!sSocket.Connected)
                    return;

                ResponsePacket d = (ResponsePacket)sSocket.Metadata;
                string news = new Smc(Smc.ServiceProviderEnum.TripleDES).Decrypt(d.Response);

                s_log.Debug("CMSG_GETTING_JOURNAL_2: {0} :: {1}:[{2}]", cm.ClientKey, ea.EventTime, news.Split(';').Length.ToString()/* + "::" + d.Response*/);

                string[] IdNews = news.Split(';');
                for (int i = 0; i < IdNews.Length; ++i)
                {
                    if (IdNews[i] != "")
                    {
                        int _id = Convert.ToInt32(IdNews[i]);
                        idList[_id] = _id.ToString();
                    }
                }

                StringBuilder IDS = new StringBuilder();
                IEnumerator en = JournalManager.Instance.JournalData.OrderBy(x=>x.ID).GetEnumerator();
                while (en.MoveNext())
                {
                    var entry = en.Current as JournalContentData;
                    if (!idList.Contains(entry.ID))
                    {
                        IDS.Append(entry.ID + ";");
                        JournalPacket pck = new JournalPacket(entry);
                        SendCommand(cm, OpcoDes.SMSG_JOURNAL_ADD_SYNC, pck); 
                    }
                }
                SendResponse(cm, OpcoDes.SMSG_JOURNAL_SYNC_END, "SYNC_2");
                if (IDS.Length > 0)
                {
                    s_log.Trace("У клиента {0} :: Добавлены записи: [{1}]", cm.ClientKey, IDS.ToString());
                    IDS.Remove(0, IDS.Length);
                }
                IDS = null;
            }
            catch (Exception exc)
            {
                LogUtil.ErrorException(exc, false, "HandleCMSG_GETTING_JOURNAL_2");
            }
            finally
            {
            }
        }

        private void HandleCMSG_REQUEST_USER_LOGIN(ClientManager cm, TimedEventArgs ea)
        {
            SSocket chatSocket = cm.SSocket;
            ResponsePacket pck = (ResponsePacket)chatSocket.Metadata;
            string error = string.Empty;
            bool alreadyLoggedIn = false;

            // set the name that the user choosed
            cm.UserName = pck.Response;

            if (cm.UserName.Length > 0)
            {
                cm.PingTimer.Change(10 * 1000, 10 * 1000);
                IEnumerator clientEnumerator = clients.GetEnumerator();
                // iterate through all users and check that there isn't another 
                // connection with the same user name 
                while (clientEnumerator.MoveNext())
                {
                    DictionaryEntry entry = (DictionaryEntry)clientEnumerator.Current;
                    ClientManager aClient = (ClientManager)entry.Value;

                    if (aClient.UserName == cm.UserName && !aClient.Equals(cm))
                    {
                        if (LoginFailed != null)
                            LoginFailed(this, new ClientEventArgs(cm, "Уже в сети!"));

                        alreadyLoggedIn = true;

                        cmToDisconnect.Enqueue(cm);
                        cm.SSocket.Sent += DisconnectAfterSend;

                        SendError(cm, cm.UserName + " уже в сети", "LOGIN");

                        break;
                    }
                }

                if (!alreadyLoggedIn)
                {
                    cm.Authenticated = true;

                    s_log.Debug("Клиент " + cm.IPAddress + " был авторизирован");

                    if (LoginSuccess != null)
                        LoginSuccess(this, new ClientEventArgs(cm));

                    SendResponse(cm, OpcoDes.SMSG_USER_AUTHENTICATED, cm.UserName);
                }
            }
            else
            {
                if (LoginFailed != null)
                    LoginFailed(this, new ClientEventArgs(cm, "Клиент " + cm.ClientKey + " не зарегистрирован."));

                cmToDisconnect.Enqueue(cm);
                cm.SSocket.Sent += DisconnectAfterSend;
                SendError(cm, "Ваш компьютер не может получить доступ к новостному серверу.\nВозможные причины:\n1) Ваш IP адрес не зарегистрирован в программном комплексе.\n2) Вы сменили IP адрес.\n3) Вы нуб и Вам просто не везёт.", "AUTH");
            }
        }

        private void HandleCMSG_PONG(ClientManager cm, TimedEventArgs ea)
        {
            SSocket sSocket = cm.SSocket;
            if (!sSocket.Connected)
                return;

            try
            {
                PingPacket metadata = sSocket.Metadata as PingPacket;
                if (metadata != null)
                {
                    cm.IsPinged = metadata.Data.values;
                }
                else
                    cm.IsPinged = false;
                s_log.Trace("CMSG_PONG: {0} :: {1}", cm.ClientKey, metadata.Data.values.ToString());
            }
            catch (Exception exc)
            {
                LogUtil.ErrorException(exc, false, "HandleCMSG_PONG");
            }
            finally
            {
            }
        }

        #endregion

        #region SENDS

        #region SendError
        private void SendError(ClientManager cm, string msg, string category)
        {
            SSocket chatSocket = cm.SSocket;

            ErrorPacket pck = new ErrorPacket(msg, category);

            if (!chatSocket.Connected)
                return;

            SendCommand(cm, OpcoDes.SMSG_ERROR, pck);
        }
        #endregion

       
        #region SendResponse
        /// <summary>
        /// Using the ClientManager parameter send back a response to the client.
        /// </summary>
        private void SendResponse(ClientManager cm, OpcoDes command, string response)
        {
            SSocket sSocket = cm.SSocket;
            ResponsePacket pck = new ResponsePacket();

            pck.From = "SSocketServer";
            pck.To = cm.UserName;
            pck.Response = response;

            SendCommand(cm, command, pck);
        }
        #endregion

        private AutoResetEvent mutexSent = new AutoResetEvent(false);

        public void SendCommand(ClientManager cm, OpcoDes command, object metadata)
        {
            SSocket sSocket = cm.SSocket;

            if (sSocket == null)
                return;
            sSocket.Sent += new EventHandler(SignalMutexSent);
            //mutexSent = new AutoResetEvent(true);
            try
            {
                sSocket.Command = command;
                sSocket.Metatype = metadata.GetType().FullName;
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
                mutexSent.Set();
            }
            sSocket.Sent -= new EventHandler(SignalMutexSent);
        }


        #endregion

        #region Start
        /// <summary>
        /// Starts the server (create socket, bind, listen). 
        /// Accepts incoming connections asynchronously from clients.
        /// Events: Started
        /// </summary>
        /// <remarks>
        /// If serviceMode is true does not display any MessageBox in case of error, because
        /// that blocks execution until the MessageBox is dismissed.
        /// </remarks>
        public bool Start()
        {
            bool started = true;

            if (status == ServerStatus.Started)
                return started;

            lock (lockObject)
            {
                int maxConnections = global::OETS.Journal.Server.Properties.Settings.Default.maxConnections;
                string serverIp = global::OETS.Journal.Server.Properties.Settings.Default.ServerIP;
                int serverPort = global::OETS.Journal.Server.Properties.Settings.Default.ServerPORT;

                if (clients == null) clients = Hashtable.Synchronized(new Hashtable(maxConnections));
                else clients.Clear();

                if (cmToDisconnect == null) cmToDisconnect = new Queue<ClientManager>();

                try
                {
                    var address = Utility.ParseOrResolve(serverIp);

                    endPoint = new IPEndPoint(address, serverPort);

                    IPUtil.VerifyEndpointAddress(endPoint);

                    // create the server socket
                    _tcpListen = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    // bind to the [ip, port] and place in listening state
                    _tcpListen.Bind(endPoint);
                    _tcpListen.Listen((int)SocketOptionName.MaxConnections);

                    SocketHelpers.SetListenSocketOptions(_tcpListen);

                    // save the time we went online, and set the server status as started
                    dtConnectedOn = DateTime.Now;
                    status = ServerStatus.Started;

                    // fire the Started event
                    if (Started != null)
                        Started(this, new TimedEventArgs());

                    s_log.Info("Принимает соединения на {0}", endPoint.ToString());

                    //m_ClientCheckTimer = new Timer(m_ClientCheckTimerCallback);
                    //m_ClientCheckTimer.Change(61000, 61000);                    
                    StartAccept(null);
                    // accept incoming connection asynchronously
                    //socket.BeginAccept(new AsyncCallback(HandleIncommingClient), null);
                }
                catch (Exception exc)
                {
                    started = false;
                    LogUtil.ErrorException(exc, false, "Ошибка запуска сервера.");
                    Console.ReadKey(true);
                    Environment.Exit(1);
                }
            }   // lock

            return started;
        }
        public void PingTimerCallback(ClientManager cm)
        {
            try
            {
                cm.IsPinged = false;
                SSocket sSocket = cm.SSocket;

                if (!sSocket.Connected)
                    return;

                ping_template data = new ping_template(DateTime.Now.ToString(), cm.IsPinged);
                PingPacket pck = new PingPacket(data);
                SendCommand(cm, OpcoDes.SMSG_PING, pck);
                s_log.Trace("SMSG_PING: {0} ", data.ToString(cm.IPEndPoint.ToString()));
            }
            catch (Exception exc)
            {
                LogUtil.ErrorException(exc, false, "m_PingTimerCallback");
            }
        }

        private void m_ClientCheckTimerCallback(object sender)
        {
            lock (clients.SyncRoot)
            {
                try
                {
                    s_log.Debug("m_ClientCheckTimerCallback");
                    IEnumerator clientEnumerator = clients.GetEnumerator();
                    while (clientEnumerator.MoveNext())
                    {
                        DictionaryEntry entry = (DictionaryEntry)clientEnumerator.Current;
                        ClientManager cm = (ClientManager)entry.Value;
                        if (!cm.IsPinged)
                            DisconnectClientManager(cm);
                    }
                }
                catch (Exception exc)
                {
                    LogUtil.ErrorException(exc, false, "m_ClientCheckTimerCallback");
                }
            }
        }

        protected void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += AcceptEventCompleted;
            }
            else
            {
                // socket must be cleared since the context object is being reused
                acceptEventArg.AcceptSocket = null;
            }

            bool willRaiseEvent = _tcpListen.AcceptAsync(acceptEventArg);
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
            }
        }

        private void AcceptEventCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs args)
        {
            try
            {
                Socket clientSocket = args.AcceptSocket;
                ClientManager cm = new ClientManager(ref clientSocket);

                lock (clients.SyncRoot)
                {
                    cm.Disconnected += OnClientManagerClientDisconnected;
                    cm.CommandReceived += OnClientManagerCommandReceived;
                    clients[cm.ClientKey] = cm;
                }

                StartAccept(args);

            }
            catch (ObjectDisposedException exc)
            {
                LogUtil.ErrorException(exc, false, "ProcessAccept1");
            }
            catch (SocketException exc)
            {
                LogUtil.ErrorException(exc, false, "ProcessAccept2");
            }
            catch (Exception exc)
            {
                LogUtil.ErrorException(exc, false, "ProcessAccept3");
            }
        }

        #endregion

        #region Stop
        /// <summary>
        /// Send ServerDisconnected command to each connected client, and disconnects 
        /// the listening socket.
        /// Events: Stopped
        /// </summary>
        public void Stop()
        {
            if (status == ServerStatus.Stopped)
                return;

            lock (clients.SyncRoot)
            {
                IEnumerator clientEnumerator = clients.GetEnumerator();
                while (clientEnumerator.MoveNext())
                {
                    DictionaryEntry entry = (DictionaryEntry)clientEnumerator.Current;
                    ClientManager cm = (ClientManager)entry.Value;

                    // before disconnecting each ClientManager we need to remove the eventhandlers. 
                    // This is because the Disconnected eventhandler will modify the clients hashtable 
                    // (which will result in an exceptionbecause we are enumerating 
                    // at the same time the collection).
                    cm.Disconnected -= OnClientManagerClientDisconnected;
                    cm.CommandReceived -= OnClientManagerCommandReceived;

                    // send a ServerDisconnected command to each connected client
                    SendResponse(cm, OpcoDes.SMSG_SERVER_STOPED, "Сервер был остановлен.");

                    // disconnect the ClientManager object
                    cm.Disconnect();

                    if (ClientDisconnected != null)
                        ClientDisconnected(this, new ClientEventArgs(cm));
                }

                // clear the clients collection
                clients.Clear();
            }

            try
            {
                if (_tcpListen != null)
                {
                    if (_tcpListen.Connected) _tcpListen.Shutdown(SocketShutdown.Both);
                    _tcpListen.Close();
                    _tcpListen = null;
                }
            }
            catch (Exception exc)
            {
                LogUtil.ErrorException(exc, false, "HandleIncommingClient");
            }

            status = ServerStatus.Stopped;

            // fire the Stopped event
            if (Stopped != null)
                Stopped(this, new TimedEventArgs());

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        #endregion

        #region OnClientManagerClientDisconnected
        /// <summary>
        /// Remove the client from the server's list of clients.
        /// Events: ClientDisconnected
        /// </summary>
        private void OnClientManagerClientDisconnected(object sender, TimedEventArgs ea)
        {
            ClientManager cm = sender as ClientManager;
            if (cm == null)
                return;

            ClientKey clientKey = new ClientKey(cm.IPAddress, cm.Port);

            lock (clients.SyncRoot)
            {
                if (clients.Contains(clientKey))
                    clients.Remove(clientKey);

                if (ClientDisconnected != null)
                    ClientDisconnected(this, new ClientEventArgs(cm));

               }
        }
        #endregion

        #region OnClientManagerCommandReceived

        /// <summary>
        /// A command was received from the client. Process the command 
        /// and act as necessary.
        /// </summary>
        private void OnClientManagerCommandReceived(object sender, TimedEventArgs ea)
        {
            ClientManager cm = sender as ClientManager;
            if (cm == null)
                return;

            if (m_CommandHandler.ContainsKey(cm.SSocket.Command))
                m_CommandHandler[cm.SSocket.Command](cm, ea);
        }
        #endregion

        #region DisconnectClientManager
        /// <summary>
        /// Disconnects a ClientManager and removes it from the list of clients.
        /// </summary>
        private void DisconnectClientManager(ClientManager cm)
        {
            ClientKey clientKey = new ClientKey(cm.IPAddress, cm.Port);

            lock (clients.SyncRoot)
            {
                cm.Disconnect();

                if (clients.Contains(clientKey))
                    clients.Remove(clientKey);
            }
        }
        #endregion

        private void DisconnectAfterSend(object sender, EventArgs ea)
        {
            lock (lockObject)
            {
                while (cmToDisconnect.Count > 0)
                {
                    ClientManager cm = cmToDisconnect.Dequeue();
                    DisconnectClientManager(cm);
                }
            }
        }

        private void SignalMutexSent(object sender, EventArgs e)
        {
            mutexSent.Set();
        }
    }	// Server

    public class IPUtil
    {
        /// <summary>
        /// Verifies that an endpoint exists as an address on the local network interfaces.
        /// </summary>
        /// <param name="endPoint">the endpoint to verify</param>
        public static void VerifyEndpointAddress(IPEndPoint endPoint)
        {
            if (!endPoint.Address.Equals(IPAddress.Any) &&
                !endPoint.Address.Equals(IPAddress.Loopback))
            {
                var endpointAddr = endPoint.Address;
                var interfaces = NetworkInterface.GetAllNetworkInterfaces();

                if (interfaces.Length > 0)
                {
                    foreach (NetworkInterface iface in interfaces)
                    {
                        UnicastIPAddressInformationCollection uniAddresses = iface.GetIPProperties().UnicastAddresses;

                        if (uniAddresses.Where(ipInfo => ipInfo.Address.Equals(endpointAddr)).Any())
                        {
                            return;
                        }
                    }

                    throw new Exception(endPoint.ToString());
                }
                throw new Exception("Нет заданного интерфейса");
            }
        }
    }
}
