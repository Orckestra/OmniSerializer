/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Orckestra.OmniSerializer.TypeSerializers;

namespace Orckestra.OmniSerializer
{
	delegate void SerializeDelegate<T>(Serializer serializer, Stream stream, T ob);
	delegate void DeserializeDelegate<T>(Serializer serializer, Stream stream, out T ob);

	public class Serializer
	{
		readonly static ITypeSerializer[] s_typeSerializers = new ITypeSerializer[] {
			new ObjectSerializer(),
			new PrimitivesSerializer(),
            new TriDimArrayCustomSerializer(), // serializer from test project
			new ArraySerializer(),
			new EnumSerializer(),
			//new DictionarySerializer(), // the UniversalDictionarySerializer replace the DictionarySerializer 
			new NullableSerializer(),

            // additional serializers
            new UniversalDictionarySerializer(), 
            new HashsetSerializer(),
            new HashtableSerializer(),
            new EqualityComparerSerializer(),
            new ExpandoSerializer(),
            // /additional serializers

            new GenericSerializer(), // the GenericSerializer must be the last one
		};

        private static readonly ConcurrentDictionary<Type, TypeData> TypeDataMap = new ConcurrentDictionary<Type, TypeData>();
        private static readonly object ModifyLock = new object();


        /// <summary>
        /// Initialize Serializer
        /// </summary>
        public Serializer()
            : this(new Type[0])
	    {
        }

        /// <summary>
        /// Initialize Serializer
        /// </summary>
        /// <param name="rootTypes">Types to be (de)serialized</param>
        public Serializer(IEnumerable<Type> rootTypes)
		{
			lock (ModifyLock)
			{
				AddTypesInternal(new[] { typeof(object) }.Concat(rootTypes));

				GenerateWriters(typeof(object));
				GenerateReaders(typeof(object));
			}
		}

        List<Type> AddTypesInternal(IEnumerable<Type> roots)
		{
			AssertLocked();

			var stack = new Stack<Type>(roots);
			var addedTypes = new List<Type>();

			while (stack.Count > 0)
			{
				var type = stack.Pop();

				if (TypeDataMap.ContainsKey(type))
					continue;

				if (type.IsAbstract || type.IsInterface)
					continue;

				if (type.ContainsGenericParameters)
					throw new NotSupportedException(String.Format("Type {0} contains generic parameters", type.FullName));
                
				ITypeSerializer serializer = GetTypeSerializer(type);

				var data = new TypeData(type, serializer);
                TypeDataMap.TryAdd(type, data);

				addedTypes.Add(type);

				foreach (var t in serializer.GetSubtypes(type))
				{
					if (TypeDataMap.ContainsKey(t) == false)
						stack.Push(t);
				}
			}

			return addedTypes;
		}

        internal void ClearTypeDataMap()
        {
            TypeDataMap.Clear();
        }

        [Conditional("DEBUG")]
        void AssertLocked()
        {
            Debug.Assert(System.Threading.Monitor.IsEntered(ModifyLock));
        }

        [Conditional("DEBUG")]
        void AssertUnlocked()
        {
            Debug.Assert(!System.Threading.Monitor.IsEntered(ModifyLock));
        }

        public void SerializeObject(Stream stream, object ob)
        {
            using (var watcher = new ReferenceTracker())
            {
                Serialize(stream, ob);
            }
        }

		internal void Serialize(Stream stream, object ob)
		{
            ObjectSerializer.Serialize(this, stream, ob);
		}

		public object Deserialize(Stream stream)
		{
			object ob;
			ObjectSerializer.Deserialize(this, stream, out ob);
			return ob;
		}

		public void SerializeDirect<T>(Stream stream, T value)
		{
			var del = (SerializeDelegate<T>)TypeDataMap[typeof(T)].WriterDirectDelegate;

			if (del == null)
			{
				lock (ModifyLock)
                {
                    del = (SerializeDelegate<T>)TypeDataMap[typeof(T)].WriterDirectDelegate;
                    if (del == null)
                    {
                        del = (SerializeDelegate<T>)GenerateDirectWriterDelegate(typeof(T));
                    }
                }
			}

			del(this, stream, value);
		}

		public void DeserializeDirect<T>(Stream stream, out T value)
		{
			var del = (DeserializeDelegate<T>)TypeDataMap[typeof(T)].ReaderDirectDelegate;

			if (del == null)
			{
				lock (ModifyLock)
                {
                    del = (DeserializeDelegate<T>)TypeDataMap[typeof(T)].ReaderDirectDelegate;

                    if (del == null)
                    {
                        del = (DeserializeDelegate<T>)GenerateDirectReaderDelegate(typeof(T));
                    }
                }
			}

			del(this, stream, out value);
		}

		internal SerializeDelegate<object> GetSerializer(Type type)
		{
		    var data = GetOrGenerateTypeData(type);
			
			if (data.WriterTrampolineDelegate != null)
			{
				return data.WriterTrampolineDelegate;
			}

			lock (ModifyLock)
			{
                if (data.WriterTrampolineDelegate != null)
                {
				    return data.WriterTrampolineDelegate;
                }
                return GenerateWriterTrampoline(type);
			}
		}

	    internal TypeData GetOrGenerateTypeData(Type type)
	    {
	        TypeData data;
	        if (!TypeDataMap.TryGetValue(type,
	                                     out data))
	        {
	            AssertUnlocked();
	            lock (ModifyLock)
	            {
                    if (!TypeDataMap.TryGetValue(type,
                                                 out data))
                    {
                        AddTypesInternal(new[]
                                     {
                                         type
                                     });
                    }
	            }
	            data = TypeDataMap[type];
	        }
	        return data;
	    }

	    internal DeserializeDelegate<object> GetDeserializeTrampolineFromTypeData(TypeData data)
		{
			if (data.ReaderTrampolineDelegate != null)
				return data.ReaderTrampolineDelegate;

			lock (ModifyLock)
			{
				return GenerateReaderTrampoline(data.Type);
			}
		}

		ITypeSerializer GetTypeSerializer(Type type)
		{
			var serializer = s_typeSerializers.FirstOrDefault(h => h.Handles(type));

			if (serializer == null)
				throw new NotSupportedException(String.Format("No serializer for {0}", type.FullName));

			return serializer;
		}

		internal TypeData GetIndirectData(Type type)
		{
			TypeData data;

			if (!TypeDataMap.TryGetValue(type, out data) || data.CanCallDirect == false)
				return TypeDataMap[typeof(object)];

			return data;
		}

		internal MethodInfo GetDirectWriter(Type type)
		{
			return TypeDataMap[type].WriterMethodInfo;
		}

		internal MethodInfo GetDirectReader(Type type)
		{
			return TypeDataMap[type].ReaderMethodInfo;
		}

		HashSet<Type> Collect(Type rootType)
		{
			var l = new HashSet<Type>();
			Stack<Type> stack = new Stack<Type>();

			stack.Push(rootType);

			while (stack.Count > 0)
			{
				var type = stack.Pop();

				if (type.IsAbstract || type.IsInterface)
					continue;

				if (type.ContainsGenericParameters)
					throw new NotSupportedException(String.Format("Type {0} contains generic parameters", type.FullName));

				ITypeSerializer serializer = TypeDataMap[type].TypeSerializer;

				foreach (var t in serializer.GetSubtypes(type))
				{
					if (l.Contains(t) == false)
						stack.Push(t);
				}

				l.Add(type);
			}

			return l;
		}

		void GenerateWriterStub(Type type)
		{
			AssertLocked();

			var data = TypeDataMap[type];

			ITypeSerializer serializer = data.TypeSerializer;

			MethodInfo writer;

			if (serializer is IStaticTypeSerializer)
			{
				var sts = (IStaticTypeSerializer)serializer;

				writer = sts.GetStaticWriter(type);

				Debug.Assert(writer != null);
			}
			else if (serializer is IDynamicTypeSerializer)
			{
				// TODO: make it possible for dyn serializers to not have Serializer param
				writer = Helpers.GenerateDynamicSerializerStub(type);
			}
			else
			{
				throw new Exception();
			}

			data.WriterMethodInfo = writer;
		}

		void GenerateWriterBody(Type type)
		{
			AssertLocked();

			var data = TypeDataMap[type];

			ITypeSerializer serializer = data.TypeSerializer;

			var writer = data.WriterMethodInfo as DynamicMethod;
			if (writer == null)
				return;

			var dynSer = (IDynamicTypeSerializer)serializer;

			dynSer.GenerateWriterMethod(this, type, writer.GetILGenerator());
		}

		void GenerateWriters(Type rootType)
		{
			AssertLocked();

			if (TypeDataMap[rootType].WriterMethodInfo != null)
				return;

			List<Type> types = Collect(rootType).Where(t => TypeDataMap[t].WriterMethodInfo == null).ToList();

			foreach (var type in types)
				GenerateWriterStub(type);

			foreach (var type in types)
				GenerateWriterBody(type);
        }

		SerializeDelegate<object> GenerateWriterTrampoline(Type type)
		{
			AssertLocked();

			var data = TypeDataMap[type];

			if (data.WriterTrampolineDelegate != null)
				return data.WriterTrampolineDelegate;

			GenerateWriters(type);

			data.WriterTrampolineDelegate = (SerializeDelegate<object>)Helpers.CreateSerializeDelegate(typeof(object), data);
			return data.WriterTrampolineDelegate;
		}

		Delegate GenerateDirectWriterDelegate(Type type)
		{
			AssertLocked();

			var data = TypeDataMap[type];

			if (data.WriterDirectDelegate != null)
				return data.WriterDirectDelegate;

			GenerateWriters(type);

			data.WriterDirectDelegate = Helpers.CreateSerializeDelegate(type, data);
			return data.WriterDirectDelegate;
		}

		void GenerateReaderStub(Type type)
		{
			AssertLocked();

            var data = TypeDataMap[type];

            ITypeSerializer serializer = data.TypeSerializer;

            MethodInfo reader;

            if (serializer is IStaticTypeSerializer)
            {
                var sts = (IStaticTypeSerializer)serializer;

                reader = sts.GetStaticReader(type);

                Debug.Assert(reader != null);
            }
            else if (serializer is IDynamicTypeSerializer)
            {
                // TODO: make it possible for dyn serializers to not have Serializer param
                reader = Helpers.GenerateDynamicDeserializerStub(type);
            }
            else
            {
                throw new Exception();
            }

            data.ReaderMethodInfo = reader;
		}

		void GenerateReaderBody(Type type)
		{
			AssertLocked();

            var data = TypeDataMap[type];

            ITypeSerializer serializer = data.TypeSerializer;

            var reader = data.ReaderMethodInfo as DynamicMethod;
            if (reader == null)
                return;

            var dynSer = (IDynamicTypeSerializer)serializer;

            dynSer.GenerateReaderMethod(this, type, reader.GetILGenerator());
        }

		void GenerateReaders(Type rootType)
		{
			AssertLocked();

			if (TypeDataMap[rootType].ReaderMethodInfo != null)
				return;

            List<Type> types = Collect(rootType).Where(t => TypeDataMap[t].ReaderMethodInfo == null).ToList();

            foreach (var type in types)
                GenerateReaderStub(type);

            foreach (var type in types)
                GenerateReaderBody(type);
		}

		DeserializeDelegate<object> GenerateReaderTrampoline(Type type)
		{
			AssertLocked();

			var data = TypeDataMap[type];

			if (data.ReaderTrampolineDelegate != null)
				return data.ReaderTrampolineDelegate;

			GenerateReaders(type);

			data.ReaderTrampolineDelegate = (DeserializeDelegate<object>)Helpers.CreateDeserializeDelegate(typeof(object), data);
			return data.ReaderTrampolineDelegate;
		}

		Delegate GenerateDirectReaderDelegate(Type type)
		{
			AssertLocked();

			var data = TypeDataMap[type];

			if (data.ReaderDirectDelegate != null)
				return data.ReaderDirectDelegate;

			GenerateReaders(type);

			data.ReaderDirectDelegate = Helpers.CreateDeserializeDelegate(type, data);
			return data.ReaderDirectDelegate;
		}



#if GENERATE_DEBUGGING_ASSEMBLY

		public static void GenerateDebugAssembly(IEnumerable<Type> rootTypes)
		{
			new Serializer(rootTypes, true);
		}

		Serializer(IEnumerable<Type> rootTypes, bool debugAssembly)
		{
			var ab = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("NetSerializerDebug"), AssemblyBuilderAccess.RunAndSave);
			var modb = ab.DefineDynamicModule("NetSerializerDebug.dll");
			var tb = modb.DefineType("NetSerializer", TypeAttributes.Public);

			lock (ModifyLock)
			{
				var addedTypes = AddTypesInternal(new[] { typeof(object) }.Concat(rootTypes));

				/* generate stubs */
				foreach (var type in addedTypes)
					GenerateDebugStubs(type, tb);

				foreach (var type in addedTypes)
					GenerateDebugBodies(type);
			}

			tb.CreateType();
			ab.Save("NetSerializerDebug.dll");
		}

		void GenerateDebugStubs(Type type, TypeBuilder tb)
		{
			var data = TypeDataMap[type];

			ITypeSerializer serializer = data.TypeSerializer;

			MethodInfo writer;
			MethodInfo reader;
			bool writerNeedsInstance, readerNeedsInstance;

			if (serializer is IStaticTypeSerializer)
			{
				var sts = (IStaticTypeSerializer)serializer;

				writer = sts.GetStaticWriter(type);
				reader = sts.GetStaticReader(type);

				writerNeedsInstance = writer.GetParameters().Length == 3;
				readerNeedsInstance = reader.GetParameters().Length == 3;
			}
			else if (serializer is IDynamicTypeSerializer)
			{
				writer = Helpers.GenerateStaticSerializerStub(tb, type);
				reader = Helpers.GenerateStaticDeserializerStub(tb, type);

				writerNeedsInstance = readerNeedsInstance = true;
			}
			else
			{
				throw new Exception();
			}

			data.WriterMethodInfo = writer;
			data.WriterNeedsInstanceDebug = writerNeedsInstance;

			data.ReaderMethodInfo = reader;
			data.ReaderNeedsInstanceDebug = readerNeedsInstance;
		}

		void GenerateDebugBodies(Type type)
		{
			var data = TypeDataMap[type];

			ITypeSerializer serializer = data.TypeSerializer;

			var dynSer = serializer as IDynamicTypeSerializer;
			if (dynSer == null)
				return;

			var writer = data.WriterMethodInfo as MethodBuilder;
			if (writer == null)
				throw new Exception();

			var reader = data.ReaderMethodInfo as MethodBuilder;
			if (reader == null)
				throw new Exception();

			dynSer.GenerateWriterMethod(this, type, writer.GetILGenerator());
			dynSer.GenerateReaderMethod(this, type, reader.GetILGenerator());
		}
#endif
	}
}
