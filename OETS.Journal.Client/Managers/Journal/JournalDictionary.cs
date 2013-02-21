using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using OETS.Shared.Structures;

namespace OETS.Journal.Client
{
    [Serializable]
    public class JournalDictionary : DictionaryBase
    {
        public journal_contentData this[int key]
        {
            get { return (journal_contentData)Dictionary[key]; }
            set { Dictionary[key] = value; }
        }

        public ICollection Keys
        {
            get { return Dictionary.Values; }
        }

        public void Add(int key, journal_contentData value)
        {
            Dictionary.Add(key, value);
        }

        public bool Contains(int key)
        {
            return Dictionary.Contains(key);
        }

        public void Remove(int key)
        {
            Dictionary.Remove(key);
        }
    }
}
