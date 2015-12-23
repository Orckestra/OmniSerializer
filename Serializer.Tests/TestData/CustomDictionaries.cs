using System;
using System.Collections.Generic;

namespace Serialization.Tests.TestData
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
}