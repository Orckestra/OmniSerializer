using System;

namespace OmniSerializer.Tests.TestObjects
{
    [Serializable]
    public struct StructForTesting
    {
        public int Value;
    }

    [Serializable]
    public struct StructForTestingWithString
    {
        public string StringValue;
        public IntGenericBaseClass IntWrapper;
    }
}
