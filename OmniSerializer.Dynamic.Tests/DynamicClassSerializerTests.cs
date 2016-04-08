using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Orckestra.OmniSerializer.Dynamic.Tests
{ // The purpose of this project is to test the deserialization process when an object has been modified.
    // We need to used an unsigned Dll to do this dynamically without hardcoding a byte array .

    [TestClass]
    [Serializable]
    public class DynamicClassSerializerTests
    {
        private readonly string testAppDomainName = "TestDomain";

        [TestMethod]
        [ExpectedException(typeof(TypeWasModifiedSinceItWasSerializedException))]
        public void DeserializeShouldThrowIfTheObjectHasChanged()
        {
            try
            {
                var serializedData = SerializeObjectWithOneField();
                SerializerTestHelper.ClearTypeDataMap();

                var bytes = serializedData.ToArray();
                using (var memoryStream = new MemoryStream(bytes, 0, bytes.Length, false, true))
                {
                    var serialiser = new Serializer();
                    var instance = (TargetClass) serialiser.Deserialize(memoryStream);
                    Assert.AreEqual(123,
                                    instance.field0);
                }
            }
            finally
            {
                SerializerTestHelper.ClearTypeDataMap();
            }
        }
        
        private byte[] SerializeObjectWithOneField()
        {
            var testDomain = CreateTestAppDomain(new string[0]);
            testDomain.DoCallBack(new CrossAppDomainDelegate(() =>
                                                             {
                                                                 var type = CreateType(1);
                                                                 //var instance = Activator.CreateInstance(AppDomain.CurrentDomain, type.Assembly.FullName, type.FullName);
                                                                 var instance = Activator.CreateInstance(type, 1);
                                                                 Assert.AreEqual(0, type.GetType().GetFields().Length);

                                                                 using (var memoryStream = new MemoryStream())
                                                                 {
                                                                     var serialiser = new Serializer();
                                                                     serialiser.SerializeObject(memoryStream, instance);
                                                                     AppDomain.CurrentDomain.SetData("serializedData", memoryStream.ToArray());
                                                                 }
                                                             }));

            var serializedData = (byte[])testDomain.GetData("serializedData");
            AppDomain.Unload(testDomain);

            return serializedData;
        }

        private AppDomain CreateTestAppDomain(string[] appDomainInitializerArguments)
        {
            return AppDomain.CreateDomain(testAppDomainName,
                                          null,
                                          new AppDomainSetup
                                          {
                                              ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                                              AppDomainInitializerArguments = appDomainInitializerArguments
                                          });
        }

        private static Type CreateType(int numberOfFields)
        {
            AssemblyName assemblyName = new AssemblyName(typeof(TargetClass).Assembly.FullName);
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName,
                                                                                            AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name,
                                                                              assemblyName.Name + ".dll");
            TypeBuilder typeBuilder = moduleBuilder.DefineType(typeof(TargetClass).FullName,
                                                               TypeAttributes.Public);

            FieldBuilder fieldBuilder = typeBuilder.DefineField("field" + 0,
                                                                typeof(int),
                                                                FieldAttributes.Private);

            Type[] constructorArgs = { typeof(int) };
            ConstructorBuilder constructor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, constructorArgs);
            ILGenerator constructorIL = constructor.GetILGenerator();
            constructorIL.Emit(OpCodes.Ldarg_0);
            ConstructorInfo superConstructor = typeof(Object).GetConstructor(new Type[0]);
            constructorIL.Emit(OpCodes.Call, superConstructor);
            constructorIL.Emit(OpCodes.Ldarg_0);
            constructorIL.Emit(OpCodes.Ldarg_1);
            constructorIL.Emit(OpCodes.Stfld, fieldBuilder);
            constructorIL.Emit(OpCodes.Ret);

            // Create the MyMethod method.
            MethodBuilder myMethodBuilder = typeBuilder.DefineMethod("MyMethod",
                                                                     MethodAttributes.Public, typeof(int), null);
            ILGenerator methodIL = myMethodBuilder.GetILGenerator();
            methodIL.Emit(OpCodes.Ldarg_0);
            methodIL.Emit(OpCodes.Ldfld, fieldBuilder);
            methodIL.Emit(OpCodes.Ret);


            return typeBuilder.CreateType();
        }
    }

    public class TargetClass
    {
        public int field0;
        public int field1;
    }
}