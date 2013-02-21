using OETS.Shared;
using OETS.Shared.Opcodes;
using OETS.Shared.Structures;
using OETS.Shared.Util;
using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Логика взаимодействия для JournalAdd.xaml
    /// </summary>
    public partial class JournalAdd : Window
    {
        #region Instance
        private static JournalAdd m_instance;
        public static JournalAdd Instance
        {
            get
            {
                if (m_instance == null)
                {
                    Mutex mutex = new Mutex();
                    mutex.WaitOne();

                    if (m_instance == null)
                        m_instance = new JournalAdd();

                    mutex.Close();
                }

                return m_instance;
            }
        }
        #endregion

        public Int32 EntryID = -1;
        public JournalAdd()
        {
            InitializeComponent();
            m_instance = this;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var scm = new Smc(Smc.ServiceProviderEnum.DES);
            
            var jd = new journal_contentData();
            jd.CodeOborude = codeOborude.Text;
            jd.Date = DateTime.Now.ToShortDateString();
            jd.Description = descriptionText.Text;
            jd.ModifyDate = DateTime.Now.ToShortDateString();
            jd.Enable = true;
            jd.Family = familyText.Text;
            jd.NameOborude = nameText.Text;
            jd.Smena = smenaText.Text;
            jd.Status = statusText.Text;
            jd.ID = EntryID;
            JournalPacket metadata = new JournalPacket(jd);
            string metatype = metadata.GetType().FullName;
            Client.Instance.SendCommand(Client.Instance.ServerIp, (EntryID != -1 ? OpcoDes.CMSG_SEND_JOURNAL_ENTRY : OpcoDes.CMSG_SEND_JOURNAL_ENTRY), metatype, metadata);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
