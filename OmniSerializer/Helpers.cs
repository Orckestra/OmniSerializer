/*
 * Copyright 2015 Tomi Valkeinen
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Orckestra.OmniSerializer
{
	static class Helpers
	{
        public static void ValidateIfSerializable(this Type type)
        {
            if (typeof(MulticastDelegate).IsAssignableFrom(type))
            {
                throw new NotSupportedException(String.Format("Delegate type {0} is not serializable.", type.FullName));
            }
            if (typeof(Expression).IsAssignableFrom(type))
            {
                throw new NotSupportedException(String.Format("Expression of type {0} is not serializable.", type.FullName));
            }

            if (type.Namespace != null
                && (type.Namespace.StartsWith("System") || type.Namespace.StartsWith("Microsoft"))
                && !type.IsDictionary())
            {
                // We do not serialize System.* or Microsoft.* types that are not serializable.
                // Microsoft had a reason to not mark them as serializable so we do not even try to serialize them.  
                // You can create a custom serializer that handles the object creation/serialization/deserialization if you have a use case where you need to serialize a built-in type.

                if (!type.IsSerializable)
                {
                    throw new NotSupportedException(String.Format(".Net type {0} is not marked as Serializable",
                                                                  type.FullName));
                }

                if (typeof(System.Runtime.Serialization.ISerializable).IsAssignableFrom(type))
                {
                    throw new NotSupportedException(String.Format("Cannot serialize {0}: ISerializable not supported",
                                                                  type.FullName));
                }
            }
        }


	    public static bool IsAnonymousType(this Type type)
	    {
	        return Attribute.IsDefined(type,
	                                   typeof(CompilerGeneratedAttribute),
	                                   false)
	               && (type.IsGenericType && type.Name.Contains("AnonymousType") 
                       || type.IsGenericType && type.Name.Contains("AnonType"))
	               && (type.Name.StartsWith("<>", StringComparison.OrdinalIgnoreCase) 
                       || type.Name.StartsWith("VB$", StringComparison.OrdinalIgnoreCase))
	               && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic
                   && type.IsSealed;
	    }

        public static bool IsDictionary(this Type type)
        {
            if (!type.IsGenericType)
                return false;

            var genTypeDef = type.GetGenericTypeDefinition();

            return genTypeDef == typeof(Dictionary<,>);
        }

        public static IEnumerable<FieldInfo> GetFieldInfos(Type type)
		{
		    type.ValidateIfSerializable();
            //Debug.Assert(type.IsSerializable);

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
				.Where(fi => (fi.Attributes & FieldAttributes.NotSerialized) == 0)
				.OrderBy(f => f.Name, StringComparer.Ordinal);

			if (type.BaseType == null)
			{
				return fields;
			}
			else
			{
				var baseFields = GetFieldInfos(type.BaseType);
				return baseFields.Concat(fields);
			}
		}

		public static DynamicMethod GenerateDynamicSerializerStub(Type type)
		{
			var dm = new DynamicMethod("Serialize", null,
				new Type[] { typeof(Serializer), typeof(Stream), type },
				typeof(Serializer), true);

			dm.DefineParameter(1, ParameterAttributes.None, "serializer");
			dm.DefineParameter(2, ParameterAttributes.None, "stream");
			dm.DefineParameter(3, ParameterAttributes.None, "value");

			return dm;
		}

		public static DynamicMethod GenerateDynamicDeserializerStub(Type type)
		{
			var dm = new DynamicMethod("Deserialize", null,
				new Type[] { typeof(Serializer), typeof(Stream), type.MakeByRefType() },
				typeof(Serializer), true);
			dm.DefineParameter(1, ParameterAttributes.None, "serializer");
			dm.DefineParameter(2, ParameterAttributes.None, "stream");
			dm.DefineParameter(3, ParameterAttributes.Out, "value");

			return dm;
		}

#if GENERATE_DEBUGGING_ASSEMBLY
		public static MethodBuilder GenerateStaticSerializerStub(TypeBuilder tb, Type type)
		{
			var mb = tb.DefineMethod("Serialize", MethodAttributes.Public | MethodAttributes.Static, null,
				new Type[] { typeof(Serializer), typeof(Stream), type });
			mb.DefineParameter(1, ParameterAttributes.None, "serializer");
			mb.DefineParameter(2, ParameterAttributes.None, "stream");
			mb.DefineParameter(3, ParameterAttributes.None, "value");
			return mb;
		}

		public static MethodBuilder GenerateStaticDeserializerStub(TypeBuilder tb, Type type)
		{
			var mb = tb.DefineMethod("Deserialize", MethodAttributes.Public | MethodAttributes.Static, null,
				new Type[] { typeof(Serializer), typeof(Stream), type.MakeByRefType() });
			mb.DefineParameter(1, ParameterAttributes.None, "serializer");
			mb.DefineParameter(2, ParameterAttributes.None, "stream");
			mb.DefineParameter(3, ParameterAttributes.Out, "value");
			return mb;
		}
#endif

		/// <summary>
		/// Create delegate that calls writer either directly, or via a trampoline
		/// </summary>
		public static Delegate CreateSerializeDelegate(Type paramType, TypeData data)
		{
			Type writerType = data.Type;

			if (paramType != writerType && paramType != typeof(object))
				throw new Exception();

			bool needTypeConv = paramType != writerType;
			bool needsInstanceParameter = data.WriterNeedsInstance;

			var delegateType = typeof(SerializeDelegate<>).MakeGenericType(paramType);

			// Can we call the writer directly?

			if (!needTypeConv && needsInstanceParameter)
			{
				var dynamicWriter = data.WriterMethodInfo as DynamicMethod;

				if (dynamicWriter != null)
					return dynamicWriter.CreateDelegate(delegateType);
				else
					return Delegate.CreateDelegate(delegateType, data.WriterMethodInfo);
			}

			// Create a trampoline

			var wrapper = Helpers.GenerateDynamicSerializerStub(paramType);
			var il = wrapper.GetILGenerator();

			if (needsInstanceParameter)
				il.Emit(OpCodes.Ldarg_0);

			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldarg_2);
			if (needTypeConv)
				il.Emit(writerType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, writerType);

			// XXX tailcall causes slowdowns with large valuetypes
			//il.Emit(OpCodes.Tailcall);
			il.Emit(OpCodes.Call, data.WriterMethodInfo);

			il.Emit(OpCodes.Ret);

			return wrapper.CreateDelegate(delegateType);
		}

		/// <summary>
		/// Create delegate that calls reader either directly, or via a trampoline
		/// </summary>
		public static Delegate CreateDeserializeDelegate(Type paramType, TypeData data)
		{
			Type readerType = data.Type;

			if (paramType != readerType && paramType != typeof(object))
				throw new Exception();

			bool needTypeConv = paramType != readerType;
			bool needsInstanceParameter = data.ReaderNeedsInstance;

			var delegateType = typeof(DeserializeDelegate<>).MakeGenericType(paramType);

			// Can we call the reader directly?

			if (!needTypeConv && needsInstanceParameter)
			{
				var dynamicReader = data.ReaderMethodInfo as DynamicMethod;

				if (dynamicReader != null)
					return dynamicReader.CreateDelegate(delegateType);
				else
					return Delegate.CreateDelegate(delegateType, data.ReaderMethodInfo);
			}

			// Create a trampoline

			var wrapper = GenerateDynamicDeserializerStub(paramType);
			var il = wrapper.GetILGenerator();

			if (needsInstanceParameter)
				il.Emit(OpCodes.Ldarg_0);

			if (needTypeConv && readerType.IsValueType)
			{
				var local = il.DeclareLocal(readerType);

				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldloca_S, local);

				il.Emit(OpCodes.Call, data.ReaderMethodInfo);

				// write result object to out object
				il.Emit(OpCodes.Ldarg_2);
				il.Emit(OpCodes.Ldloc_0);
				il.Emit(OpCodes.Box, readerType);
				il.Emit(OpCodes.Stind_Ref);
			}
			else
			{
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldarg_2);

				// XXX tailcall causes slowdowns with large valuetypes
				//il.Emit(OpCodes.Tailcall);
				il.Emit(OpCodes.Call, data.ReaderMethodInfo);
			}

			il.Emit(OpCodes.Ret);

			return wrapper.CreateDelegate(delegateType);
		}
	}
}
