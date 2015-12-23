/*
 * Copyright 2015 Tomi Valkeinen
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Orckestra.Serialization.TypeSerializers
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

		public static void Serialize(Serializer serializer, Stream stream, object ob)
		{
			if (ob == null)
			{
				Primitives.WritePrimitive(stream, (uint)0);
				return;
			}

		    ob = FixIEnumerable(ob);

            if (ob.GetType().IsClass)
            {
                ReferenceWatcher.Current.TrackObject(ob);
            }

			var type = ob.GetType();

			SerializeDelegate<object> del;

			uint id = serializer.GetTypeIdAndSerializer(type, out del);

            //Primitives.WritePrimitive(stream, id);
            Primitives.WritePrimitive(stream, type.AssemblyQualifiedName);

            del(serializer, stream, ob);

            if (ob.GetType().IsClass)
            {
                ReferenceWatcher.Current.RemoveObject(ob);
            }
		}

        /// <summary>
        /// Linq methods (Distinct, Where, etc) are not serializable.  This method convert these enumerables to an array[T]
        /// </summary>
        /// <param name="ob"></param>
        /// <returns></returns>
	    private static object FixIEnumerable(object ob)
        {
            var value = ob;

            var enumerableValue = value as IEnumerable;
            if (enumerableValue != null)
            {
                if (enumerableValue.GetType().DeclaringType == typeof(System.Linq.Enumerable))
                {
                    if (enumerableValue.GetType().IsGenericType)
                    {
                        var args = enumerableValue.GetType().GetGenericArguments();
                        if (args.Length == 1)
                        {
                            var converter = typeof(ObjectSerializer).GetMethod("ConvertEnumerableToArray",
                                                                               BindingFlags.Static | BindingFlags.NonPublic)
                                                                    .MakeGenericMethod(args);
                            value = converter.Invoke(null,
                                                     new object[]
                                                     {
                                                         enumerableValue
                                                     });
                        }
                    }
                }
            }

            return value;
        }

        private static T[] ConvertEnumerableToArray<T>(IEnumerable items)
        {
            return items.Cast<T>()
                        .ToArray();
        }

		public static void Deserialize(Serializer serializer, Stream stream, out object ob)
		{
			//uint id;

            //Primitives.ReadPrimitive(stream, out id);

            //if (id == 0)
            //{
            //    ob = null;
            //    return;
            //}

		    string typeFullName;
		    Primitives.ReadPrimitive(stream, out typeFullName);

            if (String.IsNullOrWhiteSpace(typeFullName))
            {
                ob = null;
                return;
            }

            var type = GetTypeFromFullName(typeFullName);

            if (type == null)
            {
                throw new TypeNotFoundException(typeFullName);
            }

		    var data = serializer.GetOrGenerateTypeData(type);

			var del = serializer.GetDeserializeTrampolineFromId(data.TypeID);
			del(serializer, stream, out ob);
		}

	    private static readonly ConcurrentDictionary<string, Type> TypesFromString = new ConcurrentDictionary<string, Type>(); 
	    private static Type GetTypeFromFullName(string typeFullName)
	    {
	        return TypesFromString.GetOrAdd(typeFullName,
	                                         Type.GetType);
	    }
	}
}
