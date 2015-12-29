using System;
using System.Collections.Generic;

namespace OmniSerializer.Tests.TestObjects
{
    [Serializable]
    public class ClassWithIEnumerable<T>
    {
        public IEnumerable<T> Items { get; set; }
    }
}