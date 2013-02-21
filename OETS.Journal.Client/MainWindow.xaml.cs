using OETS.Shared;
using OETS.Shared.Opcodes;
using OETS.Shared.Structures;
using System;
using System.Linq;
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
        JournalManager jm = JournalManager.Instance;

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
            m_instance = this;
            DateText.SelectedDate = DateTime.Now;
            jm.Load();            
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
                Choice.Instance.ProgressText.Content = "Подключаюсь к серверу.";
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
                    if (Choice.Instance.IsVisible)
                        Choice.Instance.ProgressText.Content = "Связь с сервером установлена.";
                }
                else
                {
                    if (Choice.Instance.IsVisible)
                        Choice.Instance.ProgressText.Content = "Не возможно установить связь с сервером.";
                }
            }));
        }
        #endregion

        #region OnConnectFailed
        void OnConnectFailed(object sender, TimedEventArgs ea)
        {

            this.Dispatcher.Invoke(DispatcherPriority.Background, (Action)(() =>
            {
                if (Choice.Instance.IsVisible)
                    Choice.Instance.ProgressText.Content = "Не возможно установить связь.";
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
                if (Choice.Instance.IsVisible)
                    Choice.Instance.ProgressText.Content = "Соединение разорвано.";
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
                    HandleSMSG_JOURNAL_ADD(ea);
                    break;
                case OpcoDes.SMSG_JOURNAL_MODIFY:
                    HandleSMSG_JOURNAL_MODIFY(ea);
                    break;
                case OpcoDes.SMSG_JOURNAL_REMOVE:
                    HandleSMSG_JOURNAL_REMOVE(ea);
                    break;
            }
        }
        #endregion

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

        #region HandleSMSG_JOURNAL_REMOVE
        private void HandleSMSG_JOURNAL_REMOVE(SSEventArgs ea)
        {
            SSocket ss = ea.SSocket;
            ResponsePacket pck = ss.Metadata as ResponsePacket;
            if (pck != null)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, (DispatcherOperationCallback)delegate(object o)
                {
                    int id = Convert.ToInt32(pck.Response);
                    if (jm.Journal.Contains(id))
                        jm.Remove(id);
                    jm.Save();
                    DateText_SelectedDateChanged(null, null);

                    return null;
                }, null);
            }
        }
        #endregion

        #region HandleSMSG_JOURNAL_ADD
        private void HandleSMSG_JOURNAL_ADD(SSEventArgs ea)
        {
            SSocket ss = ea.SSocket;
            JournalPacket pck = ss.Metadata as JournalPacket;
            if (pck != null)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, (DispatcherOperationCallback)delegate(object o)
                {
                    if (!jm.Journal.Contains(pck.Data.ID))
                        jm.Add(pck.Data.ID, pck.Data);
                    else
                        jm.Set(pck.Data.ID, pck.Data);
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
        #endregion

        #region HandleSMSG_JOURNAL_MODIFY
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

                    jm.Save();

                    DateText_SelectedDateChanged(null, null);

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
            JournalData.DataContext = jm.JournalData.Where(x => x.Date == DateText.SelectedDate.Value.ToShortDateString());
        }



    }
}
