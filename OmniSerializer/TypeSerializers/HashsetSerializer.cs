using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OmniSerializer.TypeSerializers
{
    sealed class HashsetSerializer : BinaryFormatterSerializer
    {
        public override bool Handles(Type type)
        {
            if (!type.IsGenericType)
                return false;

            var genTypeDef = type.GetGenericTypeDefinition();

            return genTypeDef == typeof(HashSet<>);
        }
    }
}