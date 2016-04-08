using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace Orckestra.OmniSerializer.TypeSerializers
{
    internal class EqualityComparerSerializer : IStaticTypeSerializer
    {
        private static readonly ConcurrentDictionary<Type, bool> TypesHandled = new ConcurrentDictionary<Type, bool>();

        public bool Handles(Type type)
        {
            return TypesHandled.GetOrAdd(type, IsTypeHandled);
        }

        private static bool IsTypeHandled(Type type)
        {
            if (!type.IsClass)
            {
                return false;
            }

            if (typeof(IEqualityComparer).IsAssignableFrom(type))
            {
                return true;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEqualityComparer<>))
            {
                return true;
            }

            foreach (var i in type.GetInterfaces())
            {
                if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEqualityComparer<>))
                {
                    return true;
                }
            }
            return false;
        }

        public IEnumerable<Type> GetSubtypes(Type type)
        {
            yield return typeof(byte[]);
        }

        public MethodInfo GetStaticWriter(Type type)
        {
            var type1 = typeof(EqualityComparerSerializer);
            return type1.GetMethods(BindingFlags.Static | BindingFlags.Public)
                                                    .Single(mi => mi.Name == "WritePrimitive");
        }

        public MethodInfo GetStaticReader(Type type)
        {
            return typeof(EqualityComparerSerializer).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                                    .Single(mi => mi.Name == "ReadPrimitive");
        }

        // Instead of serializing every comparer using the BinaryFormatter we keep a list of the most used built-in comparers
        // and we only write an Id in the stream to identify the correct comparer to use when reading the stream.
        // This provides a good performance increase when using the build-in comparers.
        private static readonly Lazy<List<BaseHardCodedComparer>> ComparerMap = new Lazy<List<BaseHardCodedComparer>>(() =>
                                                                                                            {
                                                                                                                // never reuse an Id
                                                                                                                var list = new List<BaseHardCodedComparer>();
                                                                                                                // 0 is the binary formatter
                                                                                                                list.Add(new HardCodedComparer(1, StringComparer.CurrentCulture));
                                                                                                                list.Add(new HardCodedComparer(2, StringComparer.CurrentCultureIgnoreCase));
                                                                                                                list.Add(new HardCodedComparer(3, StringComparer.InvariantCulture));
                                                                                                                list.Add(new HardCodedComparer(4, StringComparer.InvariantCultureIgnoreCase));
                                                                                                                list.Add(new HardCodedComparer(5, StringComparer.Ordinal));
                                                                                                                list.Add(new HardCodedComparer(6, StringComparer.OrdinalIgnoreCase));
                                                                                                                list.Add(new DefaultEqualityComparer(7));

                                                                                                                return list;
                                                                                                            });

        public static void WritePrimitive(Serializer serializer, Stream stream, object value)
        {
            foreach (var baseComparerOoo in ComparerMap.Value)
            {
                if (baseComparerOoo.CanHandle(value))
                {
                    baseComparerOoo.Handle(value, stream);
                    return;
                }
            }

            Primitives.WritePrimitive(stream, 0);

            using (var memoryStream = new MemoryStream())
            {
                var binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(memoryStream, value);
                serializer.Serialize(stream, memoryStream.ToArray());
            }
        }

        public static void ReadPrimitive(Serializer serializer, Stream stream, out object value)
        {
            byte comparerId = 0;
            Primitives.ReadPrimitive(stream, out comparerId);

            if (comparerId == 0)
            {
                var bytes = (byte[])serializer.Deserialize(stream);

                using (var memoryStream = new MemoryStream(bytes))
                {
                    var binaryFormatter = new BinaryFormatter();
                    value = binaryFormatter.Deserialize(memoryStream);
                }
                return;
            }

            foreach (var baseComparerOoo in ComparerMap.Value)
            {
                if (baseComparerOoo.Id == comparerId)
                {
                    value = baseComparerOoo.GetComparer(stream);
                    return;
                }
            }

            throw new TypeWasModifiedSinceItWasSerializedException("Could not find dictionary comparer with id: " + comparerId);
        }

        private abstract class BaseHardCodedComparer
        {
            protected BaseHardCodedComparer(byte id)
            {
                Id = id;
            }

            public byte Id { get; private set; }
            public abstract object GetComparer(Stream stream);
            public abstract bool CanHandle(object value);
            public abstract void Handle(object value, Stream stream);
        }

        private class HardCodedComparer : BaseHardCodedComparer
        {
            private readonly IEqualityComparer _comparer;

            public HardCodedComparer(byte id, IEqualityComparer comparer)
                : base(id)
            {
                _comparer = comparer;
            }

            public override object GetComparer(Stream stream)
            {
                return _comparer;
            }

            public override bool CanHandle(object value)
            {
                return Equals(value, _comparer);
            }

            public override void Handle(object value, Stream stream)
            {
                Primitives.WritePrimitive(stream, Id);
            }
        }

        /// <summary>
        /// Handles the type's default EqualityComparer.
        /// This class will write to the stream the following info:
        /// |Comparer Id|Generic Argument Type Name|
        /// Ex: 7System.Int
        /// </summary>
        private class DefaultEqualityComparer : BaseHardCodedComparer
        {
            private static readonly ConcurrentDictionary<Type, bool> CanHandleType = new ConcurrentDictionary<Type, bool>();
            private static readonly ConcurrentDictionary<Type, object> DefaultComparerForType = new ConcurrentDictionary<Type, object>();
            private static readonly ConcurrentDictionary<string, Type> TypeFromName = new ConcurrentDictionary<string, Type>();

            public DefaultEqualityComparer(byte id)
                : base(id)
            { }

            public override object GetComparer(Stream stream)
            {
                string typeName;
                Primitives.ReadPrimitive(stream, out typeName);

                var type = SerializedTypeResolver.GetTypeFromFullName(typeName);
                if (type == null)
                {
                    throw new TypeWasModifiedSinceItWasSerializedException("Could not find type with name: " + typeName);
                }

                return GetDefaultComparer(type.Type);
            }

            public override bool CanHandle(object value)
            {
                if (value == null)
                {
                    return false;
                }

                var type = value.GetType();
                return CanHandleType.GetOrAdd(type,
                                              t =>
                                              {
                                                  if (!t.IsGenericType || t.GetGenericArguments().Length != 1)
                                                  {
                                                      return false;
                                                  }
                                                  return Equals(value, GetDefaultComparer(t.GetGenericArguments()[0]));
                                              });
            }

            private object GetDefaultComparer(Type type)
            {
                return DefaultComparerForType.GetOrAdd(type,
                                                       t =>
                                                       {
                                                           var genericType = typeof(EqualityComparer<>).MakeGenericType(t);
                                                           var property = genericType.GetProperty("Default", BindingFlags.Static | BindingFlags.Public);
                                                           return property.GetValue(null);
                                                       });
            }

            public override void Handle(object value, Stream stream)
            {
                Primitives.WritePrimitive(stream, Id);
                var type = value.GetType().GetGenericArguments()[0];
                Primitives.WritePrimitive(stream, SerializedTypeResolver.GetTypeFromFullName(type).ShortTypeName);
            }
        }
    }
}