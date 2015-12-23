using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Orckestra.Serialization.TypeSerializers
{
    /// <summary>
    /// This serializer allows a class that inherits from Dictionary to be serialized.  It also serialize the comparer and properties/fields not defined on the Dictionary class.
    /// </summary>
    sealed class UniversalDictionarySerializer : IDynamicTypeSerializer
    {
		public bool Handles(Type type)
		{
            return GetDictionaryType(type) != null;
		}

        private Type GetDictionaryType(Type type)
        {
            var targetType = type;

            do
            {
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    return targetType;
                }

                targetType = targetType.BaseType;
            } while (targetType != null);

            return null;
        }

        /// <summary>
        /// Returns the fields of the Type that are not declared on the Dictionary
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private IEnumerable<FieldInfo> GetFieldInfos(Type type)
        {
            var dictType = GetDictionaryType(type);
            var dictFields = Helpers.GetFieldInfos(dictType)
                                    .Where(x => !(x.FieldType.IsGenericType && x.FieldType.GetGenericTypeDefinition() == typeof(IEqualityComparer<>)))
                                    .ToArray();
            var allFields = Helpers.GetFieldInfos(type);

            var filteredFields = allFields.Except(dictFields); 
            return filteredFields;
        }

		public IEnumerable<Type> GetSubtypes(Type type)
		{
            var fields = GetFieldInfos(type);

			foreach (var field in fields)
				yield return field.FieldType;

            // add types from the dictionary
            var dictType = GetDictionaryType(type);
            var genArgs = dictType.GetGenericArguments();
            var serializedType = typeof(KeyValuePair<,>).MakeGenericType(genArgs).MakeArrayType();
            yield return serializedType;
		}

		public void GenerateWriterMethod(Serializer serializer, Type type, ILGenerator il)
		{
			// arg0: Serializer, arg1: Stream, arg2: value

            var fields = GetFieldInfos(type);

			foreach (var field in fields)
			{
				// Note: the user defined value type is not passed as reference. could cause perf problems with big structs

				var fieldType = field.FieldType;

				var data = serializer.GetIndirectData(fieldType);

				if (data.WriterNeedsInstance)
					il.Emit(OpCodes.Ldarg_0);

				il.Emit(OpCodes.Ldarg_1);
				if (type.IsValueType)
					il.Emit(OpCodes.Ldarga_S, 2);
				else
					il.Emit(OpCodes.Ldarg_2);
				il.Emit(OpCodes.Ldfld, field);

				il.Emit(OpCodes.Call, data.WriterMethodInfo);
			}

            GenerateDictionaryWriterMethod(type, il);

			il.Emit(OpCodes.Ret);
		}

        private void GenerateDictionaryWriterMethod(Type type, ILGenerator il)
        {
            var dictType = GetDictionaryType(type);
            DictionarySerializer dictionarySerializer = new DictionarySerializer();
            Debug.Assert(dictionarySerializer.Handles(dictType));

            // Serializer serializer, Stream stream, Dictionary<TKey, TValue> value

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);

            il.Emit(OpCodes.Call, dictionarySerializer.GetStaticWriter(dictType));
        }

        public void GenerateReaderMethod(Serializer serializer, Type type, ILGenerator il)
		{
			// arg0: Serializer, arg1: stream, arg2: out value

			if (type.IsClass)
			{
			    var constructor = type.GetConstructor(new Type[0]);
                if (constructor == null)
                {
                    throw new RequiredPublicParameterlessConstructorException(type);
                }

                // instantiate empty class
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Newobj, constructor);
                il.Emit(OpCodes.Stind_Ref);
			}

            var fields = GetFieldInfos(type);

			foreach (var field in fields)
			{
				var fieldType = field.FieldType;

				var data = serializer.GetIndirectData(fieldType);

				if (data.ReaderNeedsInstance)
					il.Emit(OpCodes.Ldarg_0);

				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldarg_2);
				if (type.IsClass)
					il.Emit(OpCodes.Ldind_Ref);
				il.Emit(OpCodes.Ldflda, field);

				il.Emit(OpCodes.Call, data.ReaderMethodInfo);
			}

            GenerateDictionaryReaderMethod(type, il);

			if (typeof(System.Runtime.Serialization.IDeserializationCallback).IsAssignableFrom(type))
			{
				var miOnDeserialization = typeof(System.Runtime.Serialization.IDeserializationCallback).GetMethod("OnDeserialization",
										BindingFlags.Instance | BindingFlags.Public,
										null, new[] { typeof(Object) }, null);

				il.Emit(OpCodes.Ldarg_2);
				il.Emit(OpCodes.Ldnull);
				il.Emit(OpCodes.Constrained, type);
				il.Emit(OpCodes.Callvirt, miOnDeserialization);
			}

			il.Emit(OpCodes.Ret);
		}

        private void GenerateDictionaryReaderMethod(Type type, ILGenerator il)
        {
            var dictType = GetDictionaryType(type);

            // Serializer serializer, Stream stream, Dictionary<TKey, TValue> value

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldind_Ref);

            var dictReaderMethod = GetType().GetMethods(BindingFlags.Static | BindingFlags.Public)
                                            .Single(mi => mi.IsGenericMethod && mi.Name == "ReadDictionaryValues");

            var genArgs = dictType.GetGenericArguments();
            dictReaderMethod = dictReaderMethod.MakeGenericMethod(new[] { type }.Concat(genArgs).ToArray());

            il.Emit(OpCodes.Call, dictReaderMethod);
        }

        public static void ReadDictionaryValues<TT, TKey, TValue>(Serializer serializer, Stream stream, TT value)
            where TT: Dictionary<TKey, TValue>
        {
            var kvpArray = (KeyValuePair<TKey, TValue>[])serializer.Deserialize(stream);

            Debug.Assert(value != null);

            foreach (var kvp in kvpArray)
                value.Add(kvp.Key, kvp.Value);
            
        }
	}
}
