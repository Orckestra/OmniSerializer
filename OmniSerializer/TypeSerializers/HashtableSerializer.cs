using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OmniSerializer.TypeSerializers
{
    sealed class HashtableSerializer : IStaticTypeSerializer
    {
        public bool Handles(Type type)
        {
            return type == typeof(Hashtable);
        }

        public IEnumerable<Type> GetSubtypes(Type type)
        {
            return new[] { typeof(KeyValuePair<Object, Object>[]) };
        }

        public MethodInfo GetStaticWriter(Type type)
        {
            var containerType = this.GetType();

            return containerType.GetMethods(BindingFlags.Static | BindingFlags.Public)
                                .FirstOrDefault(mi => mi.Name == "WritePrimitive");
        }

        public MethodInfo GetStaticReader(Type type)
        {
            var containerType = this.GetType();

            return containerType.GetMethods(BindingFlags.Static | BindingFlags.Public)
                                .FirstOrDefault(mi => mi.Name == "ReadPrimitive");
        }

        public static void WritePrimitive(Serializer serializer, Stream stream, Hashtable value)
        {
            var kvpArray = new KeyValuePair<Object, Object>[value.Count];

            int i = 0;
            foreach (var key in value.Keys)
            {
                kvpArray[i++] = new KeyValuePair<Object, Object>(key, value[key]);
            }

            serializer.Serialize(stream, kvpArray);
        }

        public static void ReadPrimitive(Serializer serializer, Stream stream, out Hashtable value)
        {
            var kvpArray = (KeyValuePair<Object, Object>[])serializer.Deserialize(stream);

            value = new Hashtable();

            foreach (var kvp in kvpArray)
                value.Add(kvp.Key, kvp.Value);
        }
    }
}