using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OmniSerializer.TypeSerializers
{
    sealed class HashsetSerializer : IStaticTypeSerializer
    {
        public bool Handles(Type type)
        {
            if (!type.IsGenericType)
                return false;

            var genTypeDef = type.GetGenericTypeDefinition();

            return genTypeDef == typeof(HashSet<>);
        }

        public IEnumerable<Type> GetSubtypes(Type type)
        {
            var genArgs = type.GetGenericArguments();

            var serializedType = typeof(List<>).MakeGenericType(genArgs).MakeArrayType();

            return new[] { serializedType };
        }

        public MethodInfo GetStaticWriter(Type type)
        {
            Debug.Assert(type.IsGenericType);

            if (!type.IsGenericType)
                throw new Exception();

            var genTypeDef = type.GetGenericTypeDefinition();

            Debug.Assert(genTypeDef == typeof(HashSet<>));

            var containerType = this.GetType();

            var writer = GetGenWriter(containerType, genTypeDef);

            var genArgs = type.GetGenericArguments();

            writer = writer.MakeGenericMethod(genArgs);

            return writer;
        }

        public MethodInfo GetStaticReader(Type type)
        {
            Debug.Assert(type.IsGenericType);

            if (!type.IsGenericType)
                throw new Exception();

            var genTypeDef = type.GetGenericTypeDefinition();

            Debug.Assert(genTypeDef == typeof(HashSet<>));

            var containerType = this.GetType();

            var reader = GetGenReader(containerType, genTypeDef);

            var genArgs = type.GetGenericArguments();

            reader = reader.MakeGenericMethod(genArgs);

            return reader;
        }

        static MethodInfo GetGenWriter(Type containerType, Type genType)
        {
            var mis = containerType.GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(mi => mi.IsGenericMethod && mi.Name == "WritePrimitive");

            foreach (var mi in mis)
            {
                var p = mi.GetParameters();

                if (p.Length != 3)
                    continue;

                if (p[1].ParameterType != typeof(Stream))
                    continue;

                var paramType = p[2].ParameterType;

                if (paramType.IsGenericType == false)
                    continue;

                var genParamType = paramType.GetGenericTypeDefinition();

                if (genType == genParamType)
                    return mi;
            }

            return null;
        }

        static MethodInfo GetGenReader(Type containerType, Type genType)
        {
            var mis = containerType.GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(mi => mi.IsGenericMethod && mi.Name == "ReadPrimitive");

            foreach (var mi in mis)
            {
                var p = mi.GetParameters();

                if (p.Length != 3)
                    continue;

                if (p[1].ParameterType != typeof(Stream))
                    continue;

                var paramType = p[2].ParameterType;

                if (paramType.IsByRef == false)
                    continue;

                paramType = paramType.GetElementType();

                if (paramType.IsGenericType == false)
                    continue;

                var genParamType = paramType.GetGenericTypeDefinition();

                if (genType == genParamType)
                    return mi;
            }

            return null;
        }

        public static void WritePrimitive<TValue>(Serializer serializer, Stream stream, HashSet<TValue> value)
        {
            var kvpArray = new TValue[value.Count];

            int i = 0;
            foreach (var kvp in value)
                kvpArray[i++] = kvp;

            serializer.Serialize(stream, kvpArray);
        }

        public static void ReadPrimitive<TValue>(Serializer serializer, Stream stream, out HashSet<TValue> value)
        {
            var kvpArray = (TValue[])serializer.Deserialize(stream);

            value = new HashSet<TValue>();

            foreach (TValue kvp in kvpArray)
                value.Add(kvp);
        }
    }
}