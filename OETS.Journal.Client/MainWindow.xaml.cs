using OETS.Shared;
using OETS.Shared.Opcodes;
using OETS.Shared.Structures;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace OETS.Journal.Client
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private Client client = Client.Instance;
        public string strHostName, iPAddress;
        JournalManager jm;

        #region Instance
        private static MainWindow m_instance;
        public static MainWindow Instance
        {
            get
            {
                if (m_instance == null)
                {
                    Mutex mutex = new Mutex();
                    mutex.WaitOne();

                    if (m_instance == null)
                        m_instance = new MainWindow();

                    mutex.Close();
                }

                return m_instance;
            }
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            if (m_instance == null)
                m_instance = this;
            m_instance.Closing += m_instance_Closing;

            jm = JournalManager.Instance;
            jm.Load();

            strHostName = Dns.GetHostName();
            iPAddress = User32.GetLocalIpAddress();
            if (iPAddress == "NotInLocal")
                ShowError("Вычисления адреса", "Не возможно вычислисть ваш Адрес.\nЭто произошло по одной из причин:\n\t1) Вы не находитесь в одной подсети с новостным сервером.\n\t2) Произошол сбой.");
            else
                ConnectClient();
        }

        void m_instance_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            jm.Dispose();
            if (client != null && client.IsConnected)
            {
                UnSubscribeClientFromEvents();
                client.Disconnect();
                client.Dispose();
                client = null;
            }
        }

        public void ShowError(string cat = null, string msg = null)
        {
            if (cat == "REPLY")
            {
                System.Windows.MessageBox.Show(msg, cat);
                return;
            }
            if (System.Windows.MessageBox.Show(msg + "\nОбратитесь к системному администратору для устранения ошибки.\nПрограмма будет автоматически закрыта!", "Ошибка " + cat, MessageBoxButton.OK, MessageBoxImage.Error) == MessageBoxResult.OK)
            {
                this.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                {
                    this.Close();
                }));
            }
        }

        public void ConnectClient()
        {
            this.Dispatcher.Invoke(DispatcherPriority.Background, (Action)(() =>
            {
                //Choice.Instance.ProgressText.Content = "Подключаюсь к серверу.";
                Trace.WriteLine("[OETS.Client] Поключаюсь к серверу");

            }));
            SubscribeClientToEvents();
            client.Connect(strHostName, iPAddress);
        }
        private void SubscribeClientToEvents()
        {
            if (client == null) client = Client.Instance;
            client.Connected += OnConnected;
            client.ConnectFailed += OnConnectFailed;
            client.Disconnected += OnDisconnected;
            client.ServerDisconnected += OnServerDisconnected;
            client.CommandReceived += OnCommandReceived;
        }
        private void UnSubscribeClientFromEvents()
        {
            if (client == null) client = Client.Instance;
            client.Connected -= OnConnected;
            client.ConnectFailed -= OnConnectFailed;
            client.Disconnected -= OnDisconnected;
            client.ServerDisconnected -= OnServerDisconnected;
            client.CommandReceived -= OnCommandReceived;
        }

        #region OnConnected
        //private static bool IsStart = true;
        void OnConnected(object sender, TimedEventArgs ea)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Background, (Action)(() =>
            {

                if (client.IsConnected)
                {
                //    if (Choice.Instance.IsVisible)
                //        Choice.Instance.ProgressText.Content = "Связь с сервером установлена.";
                    Trace.WriteLine("[OETS.Client] Связь с сервером установлена.");
                }
                else
                {
                    Trace.WriteLine("[OETS.Client] Не возможно установить связь с сервером.");
                //    if (Choice.Instance.IsVisible)
                //        Choice.Instance.ProgressText.Content = "Не возможно установить связь с сервером.";
                }
            }));
        }
        #endregion

        #region OnConnectFailed
        void OnConnectFailed(object sender, TimedEventArgs ea)
        {

            this.Dispatcher.Invoke(DispatcherPriority.Background, (Action)(() =>
            {
                //if (Choice.Instance.IsVisible)
                //    Choice.Instance.ProgressText.Content = "Не возможно установить связь.";
                Trace.WriteLine("[OETS.Client] Не возможно установить связь.");
            }));
            UnSubscribeClientFromEvents();
            if (client.IsConnected)
                client.Disconnect();

            Thread.CurrentThread.IsBackground = true;
            Thread.Sleep(10000);
            ConnectClient();
        }
        #endregion

        #region OnDisconnected
        void OnDisconnected(object sender, TimedEventArgs ea)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Background, (Action)(() =>
            {
            //    if (Choice.Instance.IsVisible)
            //        Choice.Instance.ProgressText.Content = "Соединение разорвано.";
                Trace.WriteLine("[OETS.Client] Соединение разорвано.");
            }));

            UnSubscribeClientFromEvents();
            if (client.IsConnected)
                client.Disconnect();

            Thread.CurrentThread.IsBackground = true;
            Thread.Sleep(10000);
            ConnectClient();
        }
        #endregion

        #region OnServerDisconnected
        void OnServerDisconnected(object sender, TimedEventArgs ea)
        {
            Trace.WriteLine("[OETS.Client] Соединение принудительно разорвано.");
        }
        #endregion

        #region OnCommandReceived
        void OnCommandReceived(object sender, SSEventArgs ea)
        {
            SSocket SSocket = ea.SSocket;

            switch (SSocket.Command)
            {
                case OpcoDes.SMSG_USER_AUTHENTICATED:
                    HandleSMSG_USER_AUTHENTICATED(ea);
                    break;
                case OpcoDes.SMSG_PING:
                    HandleSMSG_PING(ea);
                    break;
                case OpcoDes.SMSG_ERROR:
                    HandleSMSG_ERROR(ea);
                    break;
                case OpcoDes.SMSG_SERVER_STOPED:
                    HandleSMSG_SERVER_STOPED(ea);
                    break;
                case OpcoDes.SMSG_JOURNAL_ADD:
                case OpcoDes.SMSG_JOURNAL_ADD_SYNC:
                    HandleSMSG_JOURNAL_ADD(ea);
                    break;
                case OpcoDes.SMSG_JOURNAL_MODIFY:
                case OpcoDes.SMSG_JOURNAL_MODIFY_SYNC:
                    HandleSMSG_JOURNAL_MODIFY(ea);
                    break;
                case OpcoDes.SMSG_JOURNAL_REMOVE:
                case OpcoDes.SMSG_JOURNAL_REMOVE_SYNC:
                    HandleSMSG_JOURNAL_REMOVE(ea);
                    break;
                case OpcoDes.SMSG_JOURNAL_SYNC_END:
                    HandleSMSG_JOURNAL_SYNC_END(ea);
                    break;
            }
        }
        #endregion

        #region CommandHandlers

        private void HandleSMSG_USER_AUTHENTICATED(SSEventArgs ea)
        {
            if (client.IsConnected)
            {
                jm.SendJournalList();
            }
        }

        private void HandleSMSG_SERVER_STOPED(SSEventArgs ea)
        {
            var sSocket = ea.SSocket;
            var pck = (ResponsePacket)sSocket.Metadata;
            if (pck != null)
            {
            }
                
        }

        private void HandleSMSG_PING(SSEventArgs ea)
        {
            ping_template data = new ping_template(ea.EventTime.ToString(), true);  // send TRUE!!!
            PingPacket pck = new PingPacket(data);
            client.SendCommand(client.ServerIp, OpcoDes.CMSG_PONG, pck.GetType().FullName, pck);
        }

        private void HandleSMSG_ERROR(SSEventArgs ea)
        {
            SSocket sSocket = ea.SSocket;
            ErrorPacket pck = (ErrorPacket)sSocket.Metadata;

            if (pck != null)
            {
                Thread.Sleep(200);
                ShowError(pck.Data.ERRORCAT, pck.Data.ERRORMSG);
            }
        }

        private void HandleSMSG_JOURNAL_REMOVE(SSEventArgs ea)
        {
            SSocket ss = ea.SSocket;
            ResponsePacket pck = ss.Metadata as ResponsePacket;
            if (pck != null)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, (DispatcherOperationCallback)delegate(object o)
                {
                    int id = Convert.ToInt32(pck.Response);
                    jm.Remove(id);

                    if (ss.Command != OpcoDes.SMSG_JOURNAL_REMOVE)
                        return null;

                    jm.Save();
                    DateText_SelectedDateChanged(null, null);

                    return null;
                }, null);
            }
        }
        
        private void HandleSMSG_JOURNAL_ADD(SSEventArgs ea)
        {
            SSocket ss = ea.SSocket;
            
            JournalPacket pck = ss.Metadata as JournalPacket;
            
            if (pck != null)
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Background, (DispatcherOperationCallback)delegate(object o)
                {
                    var t = jm.Add(pck.Data.ID, pck.Data);

                    if (ss.Command != OpcoDes.SMSG_JOURNAL_ADD)
                        return null;

                    jm.Save();

                    if (JournalAdd.Instance.IsVisible)
                    {
                        MessageBox.Show("Запись успешно добавлена!");
                        JournalAdd.Instance.EntryID = pck.Data.ID;
                    }
                    DateText_SelectedDateChanged(null, null);
                    return null;
                }, null);
            }
        }

        private void HandleSMSG_JOURNAL_MODIFY(SSEventArgs ea)
        {
            SSocket ss = ea.SSocket;
            JournalPacket pck = ss.Metadata as JournalPacket;
            if (pck != null)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, (DispatcherOperationCallback)delegate(object o)
                {
                    if (jm.Journal.Contains(pck.Data.ID))
                        jm.Set(pck.Data.ID, pck.Data);
                    else
                        jm.Add(pck.Data.ID, pck.Data);

                    if (ss.Command != OpcoDes.SMSG_JOURNAL_MODIFY)
                        return null;

                    jm.Save();

                    DateText_SelectedDateChanged(null, null);

                    return null;
                }, null);
            }
        }

        private void HandleSMSG_JOURNAL_SYNC_END(SSEventArgs ea)
        {
            SSocket ss = ea.SSocket;
            ResponsePacket pck = ss.Metadata as ResponsePacket;
            if (pck.Response == "OK")
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Background, (DispatcherOperationCallback)delegate(object o)
                {
                    jm.Save();

                    DateText_SelectedDateChanged(null, null);
                    MessageBox.Show("SYNC COMPLETE!");

                    return null;
                }, null);
            }
        }

        #endregion

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            new JournalAdd().ShowDialog();
        }

        private void onKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11 && (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt)) == (ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt))
            {
                MessageBox.Show("OK");
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            DateText.SelectedDate = DateText.SelectedDate.Value.AddDays(1);
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            DateText.SelectedDate = DateText.SelectedDate.Value.AddDays(-1);
        }

        public void DateText_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            JournalData.DataContext = jm.JournalData.Where(x => x.Date == DateText.SelectedDate.Value.ToShortDateString());
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            DateText.SelectedDate = DateTime.Now;
            JournalData.DataContext = jm.JournalData.Where(x => x.Date == DateText.SelectedDate.Value.ToShortDateString());
        }



    }
}
