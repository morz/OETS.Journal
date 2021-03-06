using NLog;
using OETS.Shared.Structures;
using OETS.Shared.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;


namespace OETS.Server
{
    public class JournalManager : IDisposable
    {
        #region private members
        private static JournalManager instance;
        private JournalDictionary journalEntries;
        private string dataFile;
        private static readonly Logger s_log = LogManager.GetCurrentClassLogger();
        private static int m_highGuid;
        public List<int> m_freeGuids = new List<int>();
        #endregion

        public ObservableCollection<JournalContentData> _journalData;
        public ObservableCollection<JournalContentData> JournalData
        {
            get
            {
                if( _journalData== null)
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
            if (journalEntries != null)
            {
                journalEntries.Clear();
                journalEntries = null;
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

        public JournalDictionary JournalEntries
        {
            get { return journalEntries; }
        }

        public string DataFile
        {
            get { return dataFile; }
            set { dataFile = value; }
        }

        public journal_contentData this[int Id]
        {
            get { return (journal_contentData)journalEntries[Id]; }
        }
        #endregion

        #region JournalManager()
        private JournalManager()
        {
            s_log.Info("�������� ������ �� ������� `{0}`...", "JournalManager");
            dataFile = Environment.CurrentDirectory + @"\DataBase\" + "JournalManager.dat";
            //Load();
        }
        #endregion


        #region GenerateId()
        public int GenerateId()
        {
            int guid;
            if (m_freeGuids.Count > 0)
            {
                guid = m_freeGuids.GetRandom();
                m_freeGuids.Remove(guid);
            }
            else
                guid = ++m_highGuid;

            s_log.Info("������������ ����� ID ��� �������. ID:{0}", guid);
            return guid;
        }
        #endregion

        #region AddFreeGuid(int guid)
        public void AddFreeGuid(int guid)
        {
            if (guid == m_highGuid)
                m_highGuid--;
            else
                m_freeGuids.Add(guid);

            s_log.Info("�������� ��������� ID ��� �������. ID:{0}", guid);
        }
        #endregion

        #region GenerateFreeGuids()
        public bool GenerateFreeGuids()
        {
            try
            {
                int last_guid = 0;

                if (JournalData.Count == 0)
                    return true;

                var en = JournalData.OrderBy(x => x.ID).GetEnumerator();
                while(en.MoveNext())
                {
                    var entry = en.Current;
                    int id = entry.ID;
                    if (id > last_guid + 1)
                    {
                        int i = last_guid + 1;
                        for (; i < id; ++i)
                            m_freeGuids.Add(i);
                    }
                    last_guid = id;
                }
                if (m_freeGuids.Count > 0)
                    s_log.Info("���������� ��������� ID ��� �������. �� ����������: {0}", m_freeGuids.Count);
                else
                    s_log.Info("�� ���������� ��������� ID ��� �������.");

                return true;
            }
            catch (Exception exc)
            {
                LogUtil.ErrorException(exc, false, "GenerateFreeGuids");
                return false;
            }
        }
        #endregion


        #region Load()
        public bool Load()
        {
            lock (this)
            {
                try
                {
                    journalEntries = (JournalDictionary)BinarySerialization.Deserialize(dataFile);
                    return LoadOb();                    
                }
                catch (Exception ex)
                {
                    journalEntries = new JournalDictionary();
                    BinarySerialization.Serialize(dataFile, journalEntries);
                    Trace.Write(ex.StackTrace);
                    return false;
                }
            }
        }
        #endregion

        private bool LoadOb()
        {
            JournalData.Clear();
            s_log.Info("���������� �������: {0}", journalEntries.Count);
            if (!this.GenerateFreeGuids())
                return false;
            IEnumerator clientEnumerator = journalEntries.GetEnumerator();
            while (clientEnumerator.MoveNext())
            {
                DictionaryEntry datas = (DictionaryEntry)clientEnumerator.Current;
                journal_contentData entry = (journal_contentData)datas.Value;
                if (entry.ID>0)
                {
                    JournalContentData data = new JournalContentData(entry);
                    if (entry.ID > m_highGuid)
                        m_highGuid = entry.ID;
                    if (!JournalData.Contains(data))
                        JournalData.Add(data);
                }
            }
            s_log.Info("������� ����������: {0}/{1}", JournalData.Count, journalEntries.Count);

            var l1 =
                from p in JournalData
                where p.Enable == true 
                orderby p.Date descending 
                select p;
            JournalData = new ObservableCollection<JournalContentData>(l1);
            return true;
        }
        #region Save()
        public void Save()
        {
            lock (this)
            {
                try
                {
                    if (journalEntries == null)
                        journalEntries = new JournalDictionary();

                    BinarySerialization.Serialize(dataFile, journalEntries);
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
        public bool Add(journal_contentData Data)
        {
            lock (this)
            {
                try
                {
                    if (Contains(Data.ID))
                        return false;
                    int new_Id = GenerateId();
                    Data.ID = new_Id;
                    journalEntries.Add(new_Id, Data);
                    Save();
                    return true;
                }
                catch (Exception ex)
                {
                    Trace.Write(ex.StackTrace);
                    GenerateFreeGuids();
                    return false;
                }
            }
        }
        public bool Add(ref journal_contentData Data)
        {
            lock (this)
            {
                try
                {
                    if (Contains(Data.ID))
                        return false;
                    int new_Id = GenerateId();
                    Data.ID = new_Id;
                    journalEntries.Add(new_Id, Data);
                    Save();
                    return true;
                }
                catch (Exception ex)
                {
                    Trace.Write(ex.StackTrace);
                    GenerateFreeGuids();
                    return false;
                }
            }
        }

        public bool Set(journal_contentData Data)
        {
            lock (this)
            {
                try
                {
                    if (!Contains(Data.ID))
                        return false;

                    journalEntries[Data.ID] = Data;
                    Save();
                    return true;
                }
                catch (Exception exc)
                {
                    LogUtil.ErrorException(exc, false, "Set");
                    s_log.ErrorException("Set", exc);
                    return false;
                }
            }
        }

        public bool Contains(int Id)
        {
            return journalEntries.Contains(Id);
        }

        #region FindByID(int id)
        public journal_contentData FindByID(int id)
        {
            try
            {
                if (!Contains(id))
                    return new journal_contentData();

                return (journal_contentData)journalEntries[id];
            }
            catch
            {
                return new journal_contentData();
            }
        }
        #endregion

        public bool Remove(int Id)
        {
            if (!Contains(Id))
            {
                s_log.Info("������ � ID {0} �� ����������!", Id);
                    return false;
            }
            AddFreeGuid(Id);
            s_log.Info("������� � ID {0} �������!", Id);
            journalEntries.Remove(Id);
            Save();
            return true;
        }

        public void Clear()
        {
            journalEntries.Clear();
            Save();
        }
        #endregion

        public String GetJournalEntriesIds()
        {
            var l1 = JournalData.OrderBy(st => st.ID).ToList();
            ObservableCollection<JournalContentData> d = new ObservableCollection<JournalContentData>(l1);
            IEnumerator jeEnumerator = d.GetEnumerator();
            StringBuilder str = new StringBuilder();
            while (jeEnumerator.MoveNext())
            {
                JournalContentData entry = (JournalContentData)jeEnumerator.Current;
                if (entry.ID > 0)
                    str.Append(entry.ID + "-" + entry.ModifyDate + "-" + entry.Date + ";");
            }
            return str.ToString();
        }
    }
}
