using OETS.Shared;
using OETS.Shared.Opcodes;
using OETS.Shared.Structures;
using System;
using System.Collections.Generic;
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
    public partial class MainWindow : Window
    {
        private Client client = Client.Instance;
        private string strHostName, iPAddress;

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

            strHostName = Dns.GetHostName();
            iPAddress = User32.GetLocalIpAddress();
            if (iPAddress == "NotInLocal")
                ShowError("Вычисления адреса", "Не возможно вычислисть ваш Адрес.\nЭто произошло по одной из причин:\n\t1) Вы не находитесь в одной подсети с новостным сервером.\n\t2) Произошол сбой.");
            else
                ConnectClient();

        }

        void ShowError(string cat = null, string msg = null)
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
                    WelcomeText.Content = String.Format("Я подключён к {0}:{1}", client.ServerIp, client.ServerPort);   
                else
                    WelcomeText.Content = "Я не подключён";   
            }));
        }
        #endregion

        #region OnConnectFailed
        void OnConnectFailed(object sender, TimedEventArgs ea)
        {
            OnDisconnected(sender, ea);
        }
        #endregion

        #region OnDisconnected
        void OnDisconnected(object sender, TimedEventArgs ea)
        {
            UnSubscribeClientFromEvents();
            if (client.IsConnected)
                client.Disconnect();

            this.Dispatcher.Invoke(DispatcherPriority.Background, (Action)(() =>
            {
                WelcomeText.Content = "Я отключён"; 
            }));

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
            }
        }
        #endregion

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
            ResponsePacket metadata = new ResponsePacket();
            string metatype = metadata.GetType().FullName;

            metadata.From = "CLIENT";
            metadata.To = "SSocketServer";
            metadata.Response = "PING?";

            client.SendCommand(client.ServerIp, OpcoDes.CMSG_TEST, metatype, metadata);
        }


    }
}
