using System;

namespace Orckestra.Serialization
{
    public class TypeNotFoundException : Exception
    {
        public TypeNotFoundException()
            : base() {}

        public TypeNotFoundException(string message)
            : base(message) {}
    }
}