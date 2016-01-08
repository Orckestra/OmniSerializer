using System;

namespace Orckestra.OmniSerializer.Tests.TestObjects
{
    [Serializable]
    public class ClassWithClassProperty
    {
        public IntGenericBaseClass Value { get; set; }
    }
}