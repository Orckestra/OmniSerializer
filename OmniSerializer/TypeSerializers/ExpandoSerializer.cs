/*
 * Copyright 2015 Tomi Valkeinen
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Orckestra.OmniSerializer.TypeSerializers
{
    sealed class ExpandoSerializer : IStaticTypeSerializer
    {
        public bool Handles(Type type)
        {
            return type == typeof(ExpandoObject);
        }

        public IEnumerable<Type> GetSubtypes(Type type)
        {
            Debug.Assert(typeof(IDictionary<string, Object>).IsAssignableFrom(type));
            return new[]
                   {
                       typeof(KeyValuePair<string, object>)
                   };
        }

        public MethodInfo GetStaticWriter(Type type)
        {
            return typeof(ExpandoSerializer).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                            .Single(mi => mi.Name == "WritePrimitive");
        }

        public MethodInfo GetStaticReader(Type type)
        {
            return typeof(ExpandoSerializer).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                            .Single(mi => mi.Name == "ReadPrimitive");
        }

        public static void WritePrimitive(Serializer serializer, Stream stream, ExpandoObject value)
        {
            IDictionary<string, object> dict = value;
            var kvpArray = new KeyValuePair<string, object>[dict.Count];

            int i = 0;
            foreach (var kvp in dict)
            {
                kvpArray[i++] = kvp;
            }

            serializer.Serialize(stream,
                                 kvpArray);
        }

        public static void ReadPrimitive(Serializer serializer, Stream stream, out ExpandoObject value)
        {
            var kvpArray = (KeyValuePair<string, object>[]) serializer.Deserialize(stream);

            value = new ExpandoObject();
            IDictionary<string, object> dict = value;

            foreach (var kvp in kvpArray)
            {
                dict.Add(kvp.Key,
                         kvp.Value);
            }
        }
    }
}