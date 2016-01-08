using System;

namespace Orckestra.OmniSerializer.Tests.TestObjects
{
    [Serializable]
    public class CircularReference
    {
        public CircularReference Parent { get; set; }
        public CircularReference Child { get; set; }
    }
}