using OETS.Shared;
using OETS.Shared.Opcodes;
using OETS.Shared.Structures;
using OETS.Shared.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public JournalAdd()
        {
            InitializeComponent();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var scm = new Smc(Smc.ServiceProviderEnum.RC2);
            
            var jd = new journal_contentData();
            jd.CodeOborude = codeOborude.Text;
            jd.Date = DateTime.Now.ToShortDateString();
            jd.Description = descriptionText.Text;
            jd.ModifyDate = DateTime.Now.ToLongDateString();
            jd.Enable = true;
            jd.Family = familyText.Text;
            jd.NameOborude = nameText.Text;
            jd.Smena = smenaText.Text;
            jd.Status = statusText.Text;
            jd.ID = scm.Encrypt(String.Format("{0}-{1}-{2}", jd.Date, codeOborude.Text, familyText.Text));
            JournalPacket metadata = new JournalPacket(jd);
            string metatype = metadata.GetType().FullName;

            Client.Instance.SendCommand(Client.Instance.ServerIp, OpcoDes.CMSG_SEND_JOURNAL_ENTRY, metatype, metadata);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
