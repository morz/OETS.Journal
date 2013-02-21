using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using OETS.Shared.Structures;

namespace OETS.Server
{
    public class DictionaryB : DictionaryBase
    {
        public void Dispose()
        {
            Dictionary.Clear();
        }

        public string this[int key]
        {
            get { return (string)Dictionary[key]; }
            set { Dictionary[key] = value; }
        }

        public ICollection Keys
        {
            get { return Dictionary.Values; }
        }

        public void Add(int key, string value)
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
