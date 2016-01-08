using System;
using System.Collections.Generic;

namespace Orckestra.OmniSerializer.TypeSerializers
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