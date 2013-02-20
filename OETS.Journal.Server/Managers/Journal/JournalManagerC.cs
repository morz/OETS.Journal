using NLog;
using OETS.Shared.Structures;
using OETS.Shared.Util;
using System;
using System.Collections;
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

        public journal_contentData this[String Id]
        {
            get { return (journal_contentData)journalEntries[Id]; }
        }
        #endregion

        #region JournalManager()
        private JournalManager()
        {
            s_log.Info("Загрузка данных из таблицы `{0}`...", "JournalManager");
            dataFile = Environment.CurrentDirectory + @"\DataBase\" + "JournalManager.dat";
            //Load();
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
            s_log.Info("Обнаружено записей: {0}", journalEntries.Count);
            IEnumerator clientEnumerator = journalEntries.GetEnumerator();
            while (clientEnumerator.MoveNext())
            {
                DictionaryEntry datas = (DictionaryEntry)clientEnumerator.Current;
                journal_contentData entry = (journal_contentData)datas.Value;
                if (!String.IsNullOrEmpty(entry.ID))
                {
                    JournalContentData data = new JournalContentData(entry);
                    if (!JournalData.Contains(data))
                        JournalData.Add(data);
                }
            }
            s_log.Info("Записей загруженно: {0}/{1}", JournalData.Count, journalEntries.Count);

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
        public bool Add(String Id, journal_contentData Data)
        {
            try
            {
                if (Contains(Data.ID))
                    return false;

                journalEntries.Add(Id, Data);
                Save();
                return true;
            }
            catch (Exception ex)
            {
                Trace.Write(ex.StackTrace);
                return false;
            }
        }

        public bool Set(String Id, journal_contentData Data)
        {
            try
            {
                if (!Contains(Data.ID))
                    return false;

                journalEntries[Id] = Data;
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

        public bool Contains(String Id)
        {
            return journalEntries.Contains(Id);
        }

        public void Remove(String Id)
        {
            if (journalEntries.Contains(Id))
            {
                journalEntries.Remove(Id);
                Save();
            }
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
                if (!String.IsNullOrEmpty(entry.ID))
                    str.Append(entry.ID + "-" + entry.ModifyDate + "-" + entry.Date + ";");
            }
            return str.ToString();
        }
    }
}
