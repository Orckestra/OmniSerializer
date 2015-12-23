using System;

namespace Serialization.Tests.TestData
{
    [Serializable]
    public class ClassWithClassProperty
    {
        public IntGenericBaseClass Value { get; set; }
    }
}