using System;
using System.Collections;
using System.Collections.Generic;

namespace OmniSerializer.TypeSerializers
{
    internal class EqualityComparerSerializer : BinaryFormatterSerializer
    {
        public override bool Handles(Type type)
        {
            if (typeof(IEqualityComparer).IsAssignableFrom(type))
            {
                return true;
            }

            if (!type.IsGenericType)
            {
                return false;
            }

            var genTypeDef = type.GetGenericTypeDefinition();

            return genTypeDef == typeof(IEqualityComparer<>);
        }
    }
}