using System;
using System.Collections.Generic;

namespace CSharpRaft.Samples
{
    class KeyValueDB
    {
        Dictionary<string, string> data;
        object mutex;

        public KeyValueDB()
        {
            mutex = new object();
            data = new Dictionary<string, string>();
        }

        // Retrieves the value for a given key.
        public string Get(string key)
        {
            lock (mutex)
            {
                if (this.data.ContainsKey(key))
                {
                    return this.data[key];
                }
                else
                {
                    return "error: key is not exist";
                }
            }
        }

        // Sets the value for a given key.
        public void Put(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new Exception("Key can not be null");
            }
            if (!this.data.ContainsKey(key))
            {
                this.data[key] = value;
            }
        }

    }
}