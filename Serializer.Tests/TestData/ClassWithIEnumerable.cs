using System;
using System.Collections.Generic;

namespace Serialization.Tests.TestData
{
    [Serializable]
    public class ClassWithIEnumerable<T>
    {
        public IEnumerable<T> Items { get; set; }
    }
}