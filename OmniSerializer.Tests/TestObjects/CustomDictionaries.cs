using System;
using System.Collections.Generic;

namespace Orckestra.OmniSerializer.Tests.TestObjects
{
    [Serializable]
    public class CustomDictionary : Dictionary<string, object>
    {
        public CustomDictionary()
            : base()
        {
            
        }
        public CustomDictionary(IEqualityComparer<string> comparer)
            : base(comparer)
        {
        }
    }

    [Serializable]
    public class CustomDictionaryWithAdditionalProperties : Dictionary<string, object>
    {
        public int SomeProperty { get; set; }
    }

    [Serializable]
    public class CustomDictionaryWithAdditionalPropertiesAndGenerics<TKey, TValue> : Dictionary<TKey, TValue>
    {
        public int SomeProperty { get; set; }
    }

    [Serializable]
    public class CustomDictionaryWithoutPublicParameterlessConstructor<TKey, TValue> : Dictionary<TKey, TValue>
    {
        public CustomDictionaryWithoutPublicParameterlessConstructor(int capacity)
            : base(capacity)
        {
            
        }
    }

    [Serializable]
    public class CustomDictionaryWithDictionaryProperty<TKey, TValue> : Dictionary<TKey, TValue>
    {
        public Dictionary<TValue, TKey> SwitchedDictionary { get; set; }
    }
}