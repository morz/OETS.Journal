using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Reflection;
using OETS.Shared.Util;
using System.Diagnostics;
using OETS.Shared.Structures;
using System.Collections.ObjectModel;
using OETS.Shared;
using OETS.Shared.Opcodes;


namespace OETS.Journal.Client
{
    /// <summary>
    /// Responsible for keeping the user list.
    /// </summary>
    public class JournalManager : IDisposable
    {
        #region private members
        private static JournalManager instance;
        private JournalDictionary _entries;
        private string dataFile;
        #endregion

        public ObservableCollection<JournalContentData> _journalData;
        public ObservableCollection<JournalContentData> JournalData
        {
            get
            {
                if( _journalData == null)
                    _journalData = new ObservableCollection<JournalContentData>();

                return _journalData;
            }
            set
            {
                _journalData = value;
            }
        }

        public void Dispose()
        {
            if (_entries != null)
            {
                _entries.Clear();
                _entries = null;
            }
        }

        #region properties
        public static JournalManager Instance
        {
            get
            {
                if (instance == null)
                {
                    Mutex mutex = new Mutex();
                    mutex.WaitOne();
                    if (instance == null)
                        instance = new JournalManager();
                    mutex.Close();
                }
                return instance;
            }
        }

        public JournalDictionary Journal
        {
            get { return _entries; }
        }

        public string DataFile
        {
            get { return dataFile; }
            set { dataFile = value; }
        }

        public journal_contentData this[int Id]
        {
            get { return (journal_contentData)_entries[Id]; }
        }
        #endregion

        #region JournalManager()
        private JournalManager()
        {
            dataFile = Environment.CurrentDirectory + @"\JournalManager.dat";
            //Load();
        }
        #endregion

        #region Load()
        public void Load()
        {
            lock (this)
            {
                try
                {
                    _entries = (JournalDictionary)BinarySerialization.Deserialize(dataFile);
                    LoadOb();                    
                }
                catch (Exception ex)
                {
                    _entries = new JournalDictionary();
                    BinarySerialization.Serialize(dataFile, _entries);
                    LoadOb(); 
                    Trace.Write(ex.StackTrace);
                }
            }
        }
        #endregion

        private void LoadOb()
        {
            lock (this)
            {
                JournalData.Clear();
                IEnumerator clientEnumerator = _entries.GetEnumerator();
                while (clientEnumerator.MoveNext())
                {
                    DictionaryEntry datas = (DictionaryEntry)clientEnumerator.Current;
                    journal_contentData entry = (journal_contentData)datas.Value;
                    if (entry.ID > 0)
                    {
                        JournalContentData data = new JournalContentData(entry);
                        if (!JournalData.Contains(data))
                            JournalData.Add(data);
                    }
                }

                var l1 =
                    from p in JournalData
                    where p.Enable == true
                    orderby p.Date descending
                    select p;
                JournalData = new ObservableCollection<JournalContentData>(l1);
            }
        }
        #region Save()
        public void Save()
        {
            lock (this)
            {
                try
                {
                    if (_entries == null)
                        _entries = new JournalDictionary();

                    BinarySerialization.Serialize(dataFile, _entries);
                    LoadOb();
                }
                catch (Exception ex)
                {
                    Trace.Write(ex.StackTrace);
                }
            }
        }
        #endregion

        #region add, contains, remove
        public bool Add(int Id, journal_contentData Data)
        {
            try
            {
                if (Contains(Id))
                    return false;

                _entries.Add(Id, Data);
                //Save();
                return true;
            }
            catch (Exception ex)
            {
                Trace.Write(ex.StackTrace);
                return false;
            }
        }

        public void Set(int Id, journal_contentData Data)
        {
            lock (this)
            {
                if (!Contains(Id))
                    return;

                _entries[Id] = Data;
                //Save();
            }
        }

        public bool Contains(int Id)
        {
            return _entries.Contains(Id);
        }

        public void Remove(int Id)
        {
            lock (this)
            {
                if (_entries.Contains(Id))
                {
                    _entries.Remove(Id);
                    //Save();
                }
            }
        }

        public void Clear()
        {
            _entries.Clear();
            Save();
        }
        #endregion

        public String GetJournalIds()
        {
            var l1 = JournalData.OrderBy(st => st.ID).ToList();
            ObservableCollection<JournalContentData> d = new ObservableCollection<JournalContentData>(l1);
            IEnumerator clientEnumerator = d.GetEnumerator();
            StringBuilder str = new StringBuilder();
            while (clientEnumerator.MoveNext())
            {
                //DictionaryEntry data = (DictionaryEntry)clientEnumerator.Current;
                JournalContentData entry = (JournalContentData)clientEnumerator.Current;
                if (entry.ID > 0)
                    str.Append(entry.ID + "-" + entry.ModifyDate + "-" + entry.Date + ";");
            }
            return str.ToString();
        }

        public bool SendJournalList()
        {
            lock (this)
            {
                try
                {

                    var l1 = JournalData.OrderBy(st => st.ID).ToList();
                    ObservableCollection<JournalContentData> d = new ObservableCollection<JournalContentData>(l1);
                    IEnumerator clientEnumerator = d.GetEnumerator();
                    StringBuilder str = new StringBuilder();
                    StringBuilder ids_str = new StringBuilder();
                    int count = 0;
                    while (clientEnumerator.MoveNext())
                    {
                        JournalContentData entry = (JournalContentData)clientEnumerator.Current;
                        if (entry.ID > 0)
                        {
                            ids_str.Append(entry.ID + ";");
                            str.Append(entry.ID + "-" + entry.ModifyDate + "-" + entry.Date + ";");
                            count++;
                            if (count == 10)
                            {
                                ResponsePacket pck = new OETS.Shared.ResponsePacket(Client.Instance.User, "SSocketServer",
                                    new Smc(Smc.ServiceProviderEnum.TripleDES).Encrypt(str.ToString()));
                                Client.Instance.SendCommand(Client.Instance.ServerIp, OpcoDes.CMSG_GETTING_JOURNAL, pck.GetType().FullName, pck);
                                count = 0;
                                str.Remove(0, str.Length);
                            }
                        }
                    }
                    if (str.Length > 0)
                    {
                        ResponsePacket pck = new ResponsePacket(Client.Instance.User, "SSocketServer",
                                    new Smc(Smc.ServiceProviderEnum.TripleDES).Encrypt(str.ToString()));
                        Client.Instance.SendCommand(Client.Instance.ServerIp, OpcoDes.CMSG_GETTING_JOURNAL, pck.GetType().FullName, pck);
                        count = 0;
                        str.Remove(0, str.Length);
                    }
                    if (ids_str.Length >= 0)
                    {
                        ResponsePacket pck = new ResponsePacket(Client.Instance.User, "SSocketServer",
                                    new Smc(Smc.ServiceProviderEnum.TripleDES).Encrypt(ids_str.ToString()));
                        Client.Instance.SendCommand(Client.Instance.ServerIp, OpcoDes.CMSG_GETTING_JOURNAL_2, pck.GetType().FullName, pck);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                    return false;
                }
            }
        }
    }
}
