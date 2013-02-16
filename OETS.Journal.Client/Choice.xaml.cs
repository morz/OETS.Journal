using OETS.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OETS.Journal.Client
{

    /// <summary>
    /// Логика взаимодействия для Choice.xaml
    /// </summary>
    public partial class Choice : Window
    {
        #region Instance
        private static Choice m_instance;
        public static Choice Instance
        {
            get
            {
                if (m_instance == null)
                {
                    Mutex mutex = new Mutex();
                    mutex.WaitOne();

                    if (m_instance == null)
                        m_instance = new Choice();

                    mutex.Close();
                }

                return m_instance;
            }
        }
        #endregion
        private static bool IsRealClose;
        public Choice()
        {
            InitializeComponent();
            m_instance = this;
            MainWindow.Instance.strHostName = Dns.GetHostName();
            MainWindow.Instance.iPAddress = User32.GetLocalIpAddress();
            if (MainWindow.Instance.iPAddress == "NotInLocal")
                MainWindow.Instance.ShowError("Вычисления адреса", "Не возможно вычислисть ваш Адрес.\nЭто произошло по одной из причин:\n\t1) Вы не находитесь в одной подсети с новостным сервером.\n\t2) Произошол сбой.");
            else
                MainWindow.Instance.ConnectClient();
        }

        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!IsRealClose)
            {
                MainWindow.Instance.Close();
            }
        }

        private void ReadClick(object sender, RoutedEventArgs e)
        {
            IsRealClose = true;
            MainWindow.Instance.Show();
            this.Close();
            
        }
    }
}
