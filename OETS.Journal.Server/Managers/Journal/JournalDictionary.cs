using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using OETS.Shared.Structures;

namespace OETS.Server
{
    [Serializable]
    public class JournalDictionary : DictionaryBase
    {
        public journal_contentData this[String key]
        {
            get { return (journal_contentData)Dictionary[key]; }
            set { Dictionary[key] = value; }
        }

        public ICollection Keys
        {
            get { return Dictionary.Values; }
        }

        public void Add(String key, journal_contentData value)
        {
            Dictionary.Add(key, value);
        }

        public bool Contains(String key)
        {
            return Dictionary.Contains(key);
        }

        public void Remove(String key)
        {
            Dictionary.Remove(key);
        }
    }
}
