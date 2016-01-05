using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OmniSerializer.TypeSerializers
{
    sealed class HashtableSerializer : BinaryFormatterSerializer
    {
        public override bool Handles(Type type)
        {
            return typeof(Hashtable).IsAssignableFrom(type);
        }
    }
}