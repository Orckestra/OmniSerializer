using System;

namespace Orckestra.Serialization
{
    public class CircularReferenceException : Exception
    {
        public CircularReferenceException()
            : base() {}

        public CircularReferenceException(string message)
            : base(message) {}
    }
}