using System;

namespace OmniSerializer.Tests.TestObjects
{
    [Serializable]
    public class ClassWithClassProperty
    {
        public IntGenericBaseClass Value { get; set; }
    }
}