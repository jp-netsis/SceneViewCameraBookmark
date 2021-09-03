using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace jp.netsis.Utility
{
    [Serializable]
    public class JsonDictionary<TKey, TValue> : ISerializationCallbackReceiver
    {
        [Serializable]
        private struct KeyValuePair
        {
            public TKey   key;
            public TValue value;
        }

        [SerializeField] KeyValuePair[] dictionary = default;

        Dictionary<TKey, TValue> _dictionary;

        public Dictionary<TKey, TValue> Dictionary => _dictionary;

        public JsonDictionary( Dictionary<TKey, TValue> dictionary )
        {
            _dictionary = dictionary;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            dictionary = _dictionary
                    .Select( x => new KeyValuePair { key = x.Key, value = x.Value } )
                    .ToArray();
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            _dictionary = dictionary.ToDictionary( x => x.key, x => x.value );
            dictionary   = null;
        }
    }
}
