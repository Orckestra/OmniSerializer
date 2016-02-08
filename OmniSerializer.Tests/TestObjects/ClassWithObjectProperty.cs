using System;

namespace Orckestra.OmniSerializer.Tests.TestObjects
{
    [Serializable]
    public class ClassWithObjectProperty
    {
        public Object Obj { get; set; }
    }
}