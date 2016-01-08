using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace Orckestra.OmniSerializer.TypeSerializers
{
    /// <summary>
    /// Serializer that uses BinaryFormatter to serialize/deserialize objects.
    /// The objects to deserialize must be marked with <see cref="SerializableAttribute"/>.
    /// </summary>
    internal abstract class BinaryFormatterSerializer : IStaticTypeSerializer
    {
        public abstract bool Handles(Type type);

        public IEnumerable<Type> GetSubtypes(Type type)
        {
            yield return typeof(byte[]);
        }

        public MethodInfo GetStaticWriter(Type type)
        {
            return typeof(BinaryFormatterSerializer).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                                    .Single(mi => mi.Name == "WritePrimitive");
        }

        public MethodInfo GetStaticReader(Type type)
        {
            return typeof(BinaryFormatterSerializer).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                                    .Single(mi => mi.Name == "ReadPrimitive");
        }

        public static void WritePrimitive(Serializer serializer, Stream stream, object value)
        {
            using (var memoryStream = new MemoryStream())
            {
                var binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(memoryStream,
                                          value);
                serializer.Serialize(stream,
                                         memoryStream.ToArray());
            }
        }

        public static void ReadPrimitive(Serializer serializer, Stream stream, out object value)
        {
            var bytes = (byte[]) serializer.Deserialize(stream);

            using (var memoryStream = new MemoryStream(bytes))
            {
                var binaryFormatter = new BinaryFormatter();
                value = binaryFormatter.Deserialize(memoryStream);
            }
        }
    }
}