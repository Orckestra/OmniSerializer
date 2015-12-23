using System;

namespace Serialization.Tests.TestData
{
    [Serializable]
    public class CircularReference
    {
        public CircularReference Parent { get; set; }
        public CircularReference Child { get; set; }
    }
}