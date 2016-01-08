using System;
using System.Collections.Generic;

namespace Orckestra.OmniSerializer.Tests.TestObjects
{
    [Serializable]
    public class ClassWithIEnumerable<T>
    {
        public IEnumerable<T> Items { get; set; }
    }
}