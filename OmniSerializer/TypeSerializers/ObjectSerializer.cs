/*
 * Copyright 2015 Tomi Valkeinen
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Orckestra.OmniSerializer.TypeSerializers
{
	sealed class ObjectSerializer : IStaticTypeSerializer
	{
		public bool Handles(Type type)
		{
			return type == typeof(object);
		}

		public IEnumerable<Type> GetSubtypes(Type type)
		{
			return new Type[0];
		}

		public MethodInfo GetStaticWriter(Type type)
		{
			return typeof(ObjectSerializer).GetMethod("Serialize", BindingFlags.Static | BindingFlags.Public);
		}

		public MethodInfo GetStaticReader(Type type)
		{
			return typeof(ObjectSerializer).GetMethod("Deserialize", BindingFlags.Static | BindingFlags.Public);
		}

        private const string NullValue = "<null>";

        public static void Serialize(Serializer serializer, Stream stream, object ob)
		{
			if (ob == null)
			{
			    Primitives.WritePrimitive(stream, NullValue); // type
                Primitives.WritePrimitive(stream, (int)0); // hashcode
				return;
			}

		    ob = ResolveIEnumerable(ob);
            var type = ob.GetType();

            try
            {
                if (type.IsClass)
                {
                    ReferenceTracker.Current.TrackObject(ob);
                }

                var typeHash = SerializedTypeResolver.GetTypeFromFullName(type);
                Debug.Assert(typeHash != null && typeHash.Type != null);

                Primitives.WritePrimitive(stream, typeHash.ShortTypeName);  // type
                Primitives.WritePrimitive(stream, typeHash.HashCode);           // hashcode

                if (ob.GetType() != typeof(object))
                {
                    SerializeDelegate<object> del = serializer.GetSerializer(type);

                    del(serializer, stream, ob);
                }
            }
            finally
            {
                if (ob.GetType().IsClass)
                {
                    ReferenceTracker.Current.RemoveObject(ob);
                }
            }
		}

        /// <summary>
        /// Linq methods (Distinct, Where, etc) are not serializable.  This method convert these enumerables to an array[T]
        /// </summary>
        /// <param name="ob"></param>
        /// <returns></returns>
	    private static object ResolveIEnumerable(object ob)
        {
            var value = ob;

            var enumerableValue = value as IEnumerable;
            if (enumerableValue != null)
            {
                var objectType = ob.GetType();
                if (objectType.IsArray
                    || typeof(IList).IsAssignableFrom(objectType))
                {
                    return ob;
                }

                if (enumerableValue.GetType().DeclaringType == typeof(System.Linq.Enumerable)
                    || (!String.IsNullOrWhiteSpace(enumerableValue.GetType().Namespace) && enumerableValue.GetType().Namespace.StartsWith("System.Linq")))
                {
                    Type itemType = typeof(object);

                    var enumerableInterface = objectType.GetInterfaces().FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                    if (enumerableInterface != null)
                    {
                        itemType = enumerableInterface.GetGenericArguments()[0];
                    }

                    var converter = typeof(ObjectSerializer).GetMethod("ConvertEnumerableToArray",
                                                                       BindingFlags.Static | BindingFlags.NonPublic)
                                                            .MakeGenericMethod(itemType);
                    value = converter.Invoke(null,
                                             new object[]
                                             {
                                                         enumerableValue
                                             });
                }
            }

            return value;
        }

        /// <summary>
        /// Used by ResolveIEnumerable using reflection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <returns></returns>
        private static T[] ConvertEnumerableToArray<T>(IEnumerable items)
        {
            return items.Cast<T>()
                        .ToArray();
        }

		public static void Deserialize(Serializer serializer, Stream stream, out object ob)
        {
            string typeFullName;
            int hashCode;
            Primitives.ReadPrimitive(stream, out typeFullName);
            Primitives.ReadPrimitive(stream, out hashCode);

            if (typeFullName == NullValue)
            {
                ob = null;
                return;
            }

            var typeHash = SerializedTypeResolver.GetTypeFromFullName(typeFullName);
            if (typeHash == null || typeHash.Type == null)
            {
                throw new TypeNotFoundException(typeFullName);
            }

            if (typeHash.HashCode != hashCode)
            {
                throw new TypeWasModifiedSinceItWasSerializedException(typeHash.Type);
            }

            var data = serializer.GetOrGenerateTypeData(typeHash.Type);

            if (typeHash.Type == typeof(object))
            {
                ob = new object();
            }
            else
            {
                var del = serializer.GetDeserializeTrampolineFromTypeData(data);
                del(serializer, stream, out ob);
            }
        }
    }
}
