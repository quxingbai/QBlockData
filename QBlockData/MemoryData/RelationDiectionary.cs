using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBlockData.MemoryData
{
    public class RelationDiectionary<K, V>
    {
        public class RelationSourceChangeData
        {
            public K Key { get; set; }
            public V Value { get; set; }
            public V OldValue { get; set; }
            public String Flag { get; set; }
            public K? NewKey { get; set; }

            public RelationSourceChangeData(K Key, V Value, V OldValue, String Flag, K? NewKey)
            {
                this.Key = Key;
                this.Value = Value;
                this.OldValue = OldValue;
                this.Flag = Flag;
                this.NewKey = NewKey;
            }

        }
        public static readonly string FLAG_ADD = "Add";
        public static readonly string FLAG_DELETE = "Delete";
        public static readonly string FLAG_UPDATE = "ValueSet";
        public static readonly string FLAG_KEYSET = "KeySet";
        private Dictionary<K, V> _Diectionary = new Dictionary<K, V>();
        private List<Action<RelationDiectionary<K, V>, RelationSourceChangeData>> ChangeListens = new();

        public V this[K key] => _Diectionary[key];

        private void InvokeChangeListens(K Key, V Value, V OldValue, String Flag, K NewKey)
        {
            var listens = ChangeListens.ToList();
            foreach (var l in listens)
            {
                l.Invoke(this, new RelationSourceChangeData(Key, Value, OldValue, Flag, NewKey));
            }
        }

        private void InvokeChangeListens(K Key, V Value, V OldValue, String Flag )
        {
            InvokeChangeListens(Key, Value, OldValue, Flag, Key);
        }

        public RelationDiectionary()
        {

        }
        public RelationDiectionary(Action<RelationDiectionary<K, V>> Inited)
        {
            Inited(this);
        }
        public bool ContainsKey(K key) => _Diectionary.ContainsKey(key);

        public virtual void AddOrUpdate(K key, V value)
        {
            string flag = null;
            V oldValue = value;
            if (_Diectionary.ContainsKey(key))
            {
                flag = FLAG_UPDATE;
                _Diectionary[key] = value;
            }
            else
            {
                flag = FLAG_ADD;
                oldValue=_Diectionary[key];
                _Diectionary.Add(key, value);
            }
            InvokeChangeListens(key, value,oldValue, flag);

        }
        public virtual void TryDelete(K key)
        {
            if (_Diectionary.ContainsKey(key))
            {
                var value = _Diectionary[key];
                _Diectionary.Remove(key);
                InvokeChangeListens(key, value, value,FLAG_DELETE);
            }
        }
        public virtual void UpdateKey(K key, K NewKey)
        {
            var value = _Diectionary[key];
            _Diectionary.Remove(key);
            _Diectionary.Add(NewKey, value);
            InvokeChangeListens(key, value,value, FLAG_DELETE, NewKey);
        }
        public virtual void RegistListen(Action<RelationDiectionary<K, V>, RelationSourceChangeData> Listen)
        {
            this.ChangeListens.Add(Listen);
        }
    }
}
