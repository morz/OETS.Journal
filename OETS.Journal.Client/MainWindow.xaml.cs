using OETS.Shared;
using OETS.Shared.Opcodes;
using OETS.Shared.Structures;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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

        private ObservableCollection<JournalContentData> _journalEntriesData;
        public ObservableCollection<JournalContentData> JournalEntriesData
        {
            get
            {
                if (this._journalEntriesData == null)
                    this._journalEntriesData = new ObservableCollection<JournalContentData>();

                return this._journalEntriesData;
            }
            set
            {
                this._journalEntriesData = value;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            m_instance = this;
            DateText.Content = DateTime.Now.ToShortDateString();
            var jd = new journal_contentData();
            jd.CodeOborude = "003.993.330";
            jd.Date = DateTime.Now.ToShortDateString();
            jd.Description = "Ничего неделал, само работает";
            jd.Enable = true;
            jd.Family = "TEST";
            jd.ID = "OSkjksjhaA";
            jd.ModifyDate = "";
            jd.NameOborude = "";
            jd.Smena = "A";
            jd.Status = "РНЗ";

            for(int i = 0; i < 10; ++i)
                JournalEntriesData.Add(new JournalContentData(jd));

            JournalData.DataContext = JournalEntriesData;
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
        private static bool IsStart = true;
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
                case OpcoDes.SMSG_PING:
                    HandleSMSG_PING(ea);
                    break;
                case OpcoDes.SMSG_ERROR:
                    HandleSMSG_ERROR(ea);
                    break;
                case OpcoDes.SMSG_SERVER_STOPED:
                    HandleSMSG_SERVER_STOPED(ea);
                    break;
            }
        }
        #endregion
       
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

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            new JournalAdd().ShowDialog();
        }


    }
}
