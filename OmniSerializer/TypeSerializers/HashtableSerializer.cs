using System;
using System.Collections;

namespace Orckestra.OmniSerializer.TypeSerializers
{
    sealed class HashtableSerializer : BinaryFormatterSerializer
    {
        public override bool Handles(Type type)
        {
            return typeof(Hashtable).IsAssignableFrom(type);
        }
    }
}