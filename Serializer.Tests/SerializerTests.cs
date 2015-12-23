using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orckestra.Serialization;
using Serialization.Tests.TestData;

namespace Serialization.Tests
{
    [TestClass]
    public class SerializerTests
    {
        [TestMethod]
        public void SerializePrimitives()
        {
            SerializePrimitive((byte) 1);
            SerializePrimitive((sbyte) 1);
            SerializePrimitive((Int32) 1);
            SerializePrimitive((UInt32) 1);
            SerializePrimitive((Int16) 1);
            SerializePrimitive((UInt16) 1);
            SerializePrimitive((Int64) 1);
            SerializePrimitive((UInt64) 1);
            SerializePrimitive((BigInteger) 1);
            SerializePrimitive((Decimal) 1.1M);
            SerializePrimitive((Double) 1.1D);
            SerializePrimitive((Single) 1.1F);
            SerializePrimitive(new StructForTesting
                               {
                                   Value = 38
                               });
            SerializePrimitive(EnumForTesting.Two);
            SerializePrimitive("Test text");
            SerializePrimitive('c');
            SerializePrimitive(new Tuple<int, string>(1, "a"));
            SerializePrimitive(DateTime.UtcNow);

            SerializePrimitive((byte?)1);
            SerializePrimitive((sbyte?)1);
            SerializePrimitive((byte?)null);
            SerializePrimitive((sbyte?)null);
            SerializePrimitive((Int32?)1);
            SerializePrimitive((UInt32?)1);
            SerializePrimitive((Int32?)null);
            SerializePrimitive((UInt32?)null);
            SerializePrimitive((Int16?)1);
            SerializePrimitive((UInt16?)1);
            SerializePrimitive((Int16?)null);
            SerializePrimitive((UInt16?)null);
            SerializePrimitive((Int64?)1);
            SerializePrimitive((UInt64?)1);
            SerializePrimitive((Int64?)null);
            SerializePrimitive((UInt64?)null);
            SerializePrimitive((BigInteger?)1);
            SerializePrimitive((BigInteger?)null);
            SerializePrimitive((Decimal?)1.1M);
            SerializePrimitive((Decimal?)1.1M);
            SerializePrimitive((Double?)1.1D);
            SerializePrimitive((Double?)null);
            SerializePrimitive((Single?)1.1F);
            SerializePrimitive((Single?)null);
            SerializePrimitive((DateTime?)DateTime.UtcNow);
            SerializePrimitive((DateTime?)null);
        }

        private void SerializePrimitive<T>(T value)
        {
            SerializePrimitiveValue(value);
            SerializePrimitiveArray(value);
            SerializePrimitiveList(value);
        }

        private static void SerializePrimitiveValue<T>(T value)
        {
            using (var memoryStream = new MemoryStream())
            {
                Type targetType = typeof(T);
                var serialiser = new Orckestra.Serialization.Serializer();

                serialiser.Serialize(memoryStream,
                                     value);
                memoryStream.Position = 0;

                T deserializedValue = (T) serialiser.Deserialize(memoryStream);

                Assert.AreEqual(value,
                                deserializedValue,
                                string.Format("Type {0} does not have the same value after being deserialized.",
                                              targetType));
            }
        }

        private static void SerializePrimitiveArray<T>(T value)
        {
            using (var memoryStream = new MemoryStream())
            {
                var array = new T[1];
                array[0] = value;

                var serialiser = new Orckestra.Serialization.Serializer();

                serialiser.Serialize(memoryStream,
                                     array);
                memoryStream.Position = 0;

                T[] deserializedValue = (T[])serialiser.Deserialize(memoryStream);

                CollectionAssert.AreEquivalent(array,
                                               deserializedValue);
            }
        }

        private static void SerializePrimitiveList<T>(T value)
        {
            using (var memoryStream = new MemoryStream())
            {
                var list = new List<T>
                           {
                               value
                           };

                var serialiser = new Orckestra.Serialization.Serializer();

                serialiser.Serialize(memoryStream,
                                     list);
                memoryStream.Position = 0;

                List<T> deserializedValue = (List<T>)serialiser.Deserialize(memoryStream);

                CollectionAssert.AreEquivalent(list,
                                               deserializedValue);
            }
        }

        [TestMethod]
        public void SerializeTestClass()
        {
            ClassWithDifferentAccessModifiers classInstance = new ClassWithDifferentAccessModifiers
                                                              {
                                                                  PublicFieldValue = 1,
                                                                  InternalFieldValue = 3,
                                                                  PublicPropertyValue = 4,
                                                                  InternalPropertyValue = 6
                                                              };
            classInstance.SetPrivateFieldValue(2);
            classInstance.SetPrivatePropertyValue(5);

            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Orckestra.Serialization.Serializer();

                serialiser.Serialize(memoryStream,
                                     classInstance);
                memoryStream.Position = 0;

                var deserialiser = new Orckestra.Serialization.Serializer();
                var deserializedValue = (ClassWithDifferentAccessModifiers)deserialiser.Deserialize(memoryStream);

                Assert.AreEqual(classInstance.PublicFieldValue,
                                deserializedValue.PublicFieldValue);
                Assert.AreEqual(classInstance.GetPrivateFieldValue(),
                                deserializedValue.GetPrivateFieldValue());
                Assert.AreEqual(classInstance.InternalFieldValue,
                                deserializedValue.InternalFieldValue);
                Assert.AreEqual(classInstance.PublicPropertyValue,
                                deserializedValue.PublicPropertyValue);
                Assert.AreEqual(classInstance.GetPrivatePropertyValue(),
                                deserializedValue.GetPrivatePropertyValue());
                Assert.AreEqual(classInstance.InternalPropertyValue,
                                deserializedValue.InternalPropertyValue);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(CircularReferenceException))]
        public void SerializeCircularReference()
        {
            var instance1 = new CircularReference();
            var instance2 = new CircularReference();
            instance1.Parent = instance2;
            instance2.Child = instance1;

            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Orckestra.Serialization.Serializer();

                serialiser.Serialize(memoryStream,
                                     instance1);
                memoryStream.Position = 0;

                var deserializedValue = (CircularReference) serialiser.Deserialize(memoryStream);

                Assert.IsTrue(Object.ReferenceEquals(deserializedValue,
                                                     deserializedValue.Child));
            }
        }

        [TestMethod]
        public void SerializeTrackMultipleReference()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Orckestra.Serialization.Serializer();
                var instance = new IntGenericBaseClass(123);
                var list = new List<IntGenericBaseClass>
                           {
                               instance,
                               instance
                           };

                serialiser.Serialize(memoryStream,
                                     list);
                memoryStream.Position = 0;

                var deserializedValue = (List<IntGenericBaseClass>) serialiser.Deserialize(memoryStream);

                Assert.AreEqual(list.Count,
                                deserializedValue.Count);
                CollectionAssert.AreEquivalent(list,
                                               deserializedValue);
            }
        }

        [TestMethod]
        public void SerializeTrackSamePrimitiveMultipleTimes()
        {
            // this case exists to make sure that the ReferenceWatcher only tracks classes
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Orckestra.Serialization.Serializer();
                var instance = 3;
                var list = new List<int>
                           {
                               instance,
                               instance
                           };

                serialiser.Serialize(memoryStream,
                                     list);
                memoryStream.Position = 0;

                var deserializedValue = (List<int>) serialiser.Deserialize(memoryStream);

                Assert.AreEqual(list.Count,
                                deserializedValue.Count);
                CollectionAssert.AreEquivalent(list,
                                               deserializedValue);
            }
        }

        [TestMethod]
        public void SerializeClassWithoutSerializableAttribute()
        {
            using (var memoryStream = new MemoryStream())
            {
                var instance = new ClassWithoutSerializableAttribute
                               {
                                   PublicPropertyValue = 4
                               };
                var serialiser = new Orckestra.Serialization.Serializer();

                serialiser.Serialize(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserializedValue = (ClassWithoutSerializableAttribute) serialiser.Deserialize(memoryStream);

                Assert.AreEqual(instance.PublicPropertyValue,
                                deserializedValue.PublicPropertyValue);
            }
        }

        [TestMethod]
        public void SerializeClassWithGenericBase()
        {
            using (var memoryStream = new MemoryStream())
            {
                var instance = new IntGenericBaseClass()
                               {
                                   Value = 4
                               };
                var serialiser = new Orckestra.Serialization.Serializer();

                serialiser.Serialize(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserializedValue = (IntGenericBaseClass)serialiser.Deserialize(memoryStream);

                Assert.AreEqual(instance.Value,
                                deserializedValue.Value);
            }
        }

        [TestMethod]
        public void SerializeGenericClass()
        {
            using (var memoryStream = new MemoryStream())
            {
                var instance = new GenericBaseClass<int>()
                {
                    Value = 4
                };
                var serialiser = new Orckestra.Serialization.Serializer();

                serialiser.Serialize(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserializedValue = (GenericBaseClass<int>)serialiser.Deserialize(memoryStream);

                Assert.AreEqual(instance.Value,
                                deserializedValue.Value);
            }
        }

        [TestMethod]
        public void SerializeDictionaryStringObject()
        {
            using (var memoryStream = new MemoryStream())
            {
                var instance = new Dictionary<string, object>()
                {
                    {"Key1", 123},
                    {"Key2", "abc"},
                    {"Key3", new IntGenericBaseClass(3) },
                };
                var serialiser = new Orckestra.Serialization.Serializer();

                serialiser.Serialize(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserializedValue = (Dictionary<string, object>)serialiser.Deserialize(memoryStream);

                Assert.AreEqual(instance.Count,
                                deserializedValue.Count);
                CollectionAssert.AreEquivalent(instance.Keys,
                                               deserializedValue.Keys);
                foreach (var kvp in instance)
                {
                    Assert.AreEqual(kvp.Value,
                                    deserializedValue[kvp.Key]);
                }
            }
        }

        [TestMethod]
        public void SerializeCustomDictionary()
        {
            using (var memoryStream = new MemoryStream())
            {
                var instance = new CustomDictionary
                {
                    {"Key1", 123},
                    {"Key2", "abc"},
                    {"Key3", new IntGenericBaseClass(3) },
                };
                var serialiser = new Orckestra.Serialization.Serializer();

                serialiser.Serialize(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserializedValue = (CustomDictionary)serialiser.Deserialize(memoryStream);

                Assert.AreEqual(instance.Count,
                                deserializedValue.Count);
                CollectionAssert.AreEquivalent(instance.Keys,
                                               deserializedValue.Keys);
                foreach (var kvp in instance)
                {
                    Assert.AreEqual(kvp.Value,
                                    deserializedValue[kvp.Key]);
                }
            }
        }

        [TestMethod]
        public void SerializeCustomDictionaryWithComparer()
        {
            using (var memoryStream = new MemoryStream())
            {
                var instance = new CustomDictionary(StringComparer.CurrentCultureIgnoreCase)
                {
                    {"Key1", 123},
                    {"Key2", "abc"},
                    {"Key3", new IntGenericBaseClass(3) },
                };
                var serialiser = new Orckestra.Serialization.Serializer();

                serialiser.Serialize(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserializedValue = (CustomDictionary)serialiser.Deserialize(memoryStream);

                Assert.AreEqual(instance.Comparer.GetType(),
                                deserializedValue.Comparer.GetType());
                Assert.AreEqual(instance.Count,
                                deserializedValue.Count);
                CollectionAssert.AreEquivalent(instance.Keys,
                                               deserializedValue.Keys);
                foreach (var kvp in instance)
                {
                    Assert.AreEqual(kvp.Value,
                                    deserializedValue[kvp.Key]);
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(RequiredPublicParameterlessConstructorException))]
        public void SerializeCustomDictionaryWithoutPublicParameterlessConstructor()
        {
            using (var memoryStream = new MemoryStream())
            {
                var instance = new CustomDictionaryWithoutPublicParameterlessConstructor<object, object>(3)
                {
                    {"Key1", 123},
                    {"Key2", "abc"},
                    {"Key3", new IntGenericBaseClass(3) },
                };
                var serialiser = new Orckestra.Serialization.Serializer();

                serialiser.Serialize(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserializedValue = (CustomDictionaryWithoutPublicParameterlessConstructor<object, object>)serialiser.Deserialize(memoryStream);
            }
        }

        [TestMethod]
        public void SerializeCustomDictionaryWithAdditionalProperties()
        {
            using (var memoryStream = new MemoryStream())
            {
                var instance = new CustomDictionaryWithAdditionalProperties
                {
                    {"Key1", 123},
                    {"Key2", "abc"},
                    {"Key3", new IntGenericBaseClass(3) },
                };
                instance.SomeProperty = 849;
                var serialiser = new Orckestra.Serialization.Serializer();

                serialiser.Serialize(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserializedValue = (CustomDictionaryWithAdditionalProperties)serialiser.Deserialize(memoryStream);

                Assert.AreEqual(instance.SomeProperty,
                                deserializedValue.SomeProperty);
                Assert.AreEqual(instance.Count,
                                deserializedValue.Count);
                CollectionAssert.AreEquivalent(instance.Keys,
                                               deserializedValue.Keys);
                foreach (var kvp in instance)
                {
                    Assert.AreEqual(kvp.Value,
                                    deserializedValue[kvp.Key]);
                }
            }
        }

        [TestMethod]
        public void SerializeCustomDictionaryWithAdditionalPropertiesAndGenerics()
        {
            using (var memoryStream = new MemoryStream())
            {
                var instance = new CustomDictionaryWithAdditionalPropertiesAndGenerics<string, int>
                {
                    {"Key1", 123},
                    {"Key2", 456},
                    {"Key3", 789},
                };
                instance.SomeProperty = 849;
                var serialiser = new Orckestra.Serialization.Serializer();

                serialiser.Serialize(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserializedValue = (CustomDictionaryWithAdditionalPropertiesAndGenerics<string, int>)serialiser.Deserialize(memoryStream);

                Assert.AreEqual(instance.SomeProperty,
                                deserializedValue.SomeProperty);
                Assert.AreEqual(instance.Count,
                                deserializedValue.Count);
                CollectionAssert.AreEquivalent(instance.Keys,
                                               deserializedValue.Keys);
                foreach (var kvp in instance)
                {
                    Assert.AreEqual(kvp.Value,
                                    deserializedValue[kvp.Key]);
                }
            }
        }

        [TestMethod]
        public void SerializeTestClassWithAdditionalTypeRoots()
        {
            // this test exists to test the way that the type are written to the stream
            // by default the NetSerializer writes the TypeId in the stream instead of the type name
            // which is incompatible with our changes to automatically detect the root types

            ClassWithClassProperty classInstance = new ClassWithClassProperty
            {
                Value = new IntGenericBaseClass(123)
            };

            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Orckestra.Serialization.Serializer(new[]
                                                  {
                                                      typeof(ClassWithClassProperty),
                                                      typeof(StructForTesting),
                                                      typeof(IntGenericBaseClass),
                                                  });

                serialiser.Serialize(memoryStream,
                                     classInstance);
                memoryStream.Position = 0;

                var deserialiser = new Orckestra.Serialization.Serializer(new []
                                                  {
                                                      typeof(IntGenericBaseClass),
                                                      typeof(StructForTesting),
                                                      typeof(ClassWithClassProperty),
                                                  });
                var deserializedValue = (ClassWithClassProperty)deserialiser.Deserialize(memoryStream);

                Assert.AreEqual(classInstance.Value.Value,
                                deserializedValue.Value.Value);
            }
        }

        [TestMethod]
        public void SerializeListWithMultipleTypes()
        {
            var list = new List<IHierarchy>
            {
                new ChildIntHierarchy(123),
                new ChildStringHierarchy("abc"),
            };

            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Orckestra.Serialization.Serializer();

                serialiser.Serialize(memoryStream,
                                     list);
                memoryStream.Position = 0;

                var deserialiser = new Orckestra.Serialization.Serializer();
                var deserializedValue = (List<IHierarchy>)deserialiser.Deserialize(memoryStream);

                Assert.AreEqual(list.Count,
                                deserializedValue.Count);
                Assert.AreEqual(list.OfType<ChildIntHierarchy>().FirstOrDefault().Value,
                                deserializedValue.OfType<ChildIntHierarchy>().FirstOrDefault().Value);
                Assert.AreEqual(list.OfType<ChildStringHierarchy>().FirstOrDefault().Value,
                                deserializedValue.OfType<ChildStringHierarchy>().FirstOrDefault().Value);
            }
        }

        [TestMethod]
        public void SerializeObjectWithListAsIEnumerable()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Orckestra.Serialization.Serializer();
                var instance = new ClassWithIEnumerable<int>();
                instance.Items = new List<int>
                                 {
                                     1,
                                     2,
                                     3
                                 };

                serialiser.Serialize(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserialiser = new Orckestra.Serialization.Serializer();
                var deserializedValue = (ClassWithIEnumerable<int>)deserialiser.Deserialize(memoryStream);

                Assert.AreEqual(instance.Items.Count(),
                                deserializedValue.Items.Count());
                CollectionAssert.AreEquivalent(instance.Items.ToList(),
                                               deserializedValue.Items.ToList());
            }
        }

        [TestMethod]
        public void SerializeObjectWithHashsetAsIEnumerable()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Orckestra.Serialization.Serializer();
                var instance = new ClassWithIEnumerable<int>();
                instance.Items = new HashSet<int>
                                 {
                                     1,
                                     2,
                                     3
                                 };

                serialiser.Serialize(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserialiser = new Orckestra.Serialization.Serializer();
                var deserializedValue = (ClassWithIEnumerable<int>)deserialiser.Deserialize(memoryStream);

                Assert.AreEqual(instance.Items.Count(),
                                deserializedValue.Items.Count());
                CollectionAssert.AreEquivalent(instance.Items.ToList(),
                                               deserializedValue.Items.ToList());
            }
        }

        [TestMethod]
        public void SerializeObjectWithEnumProperty()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Orckestra.Serialization.Serializer();
                var instance = new GenericBaseClass<EnumForTesting>();
                instance.Value = EnumForTesting.Two;

                serialiser.Serialize(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserialiser = new Orckestra.Serialization.Serializer();
                var deserializedValue = (GenericBaseClass<EnumForTesting>)deserialiser.Deserialize(memoryStream);

                Assert.AreEqual(instance.Value,
                                deserializedValue.Value);
            }
        }

        [TestMethod]
        public void SerializeHashtable()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Orckestra.Serialization.Serializer();
                var instance = new Hashtable
                               {
                                   {1, 2},
                                   {"a", "b"},
                               };

                serialiser.Serialize(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserialiser = new Orckestra.Serialization.Serializer();
                var deserializedValue = (Hashtable)deserialiser.Deserialize(memoryStream);

                Assert.AreEqual(instance.Count,
                                deserializedValue.Count);
                CollectionAssert.AreEquivalent(instance,
                                               deserializedValue);
            }
        }

        [TestMethod]
        public void SerializeObjectWithArrayAsIEnumerable()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Orckestra.Serialization.Serializer();
                var instance = new ClassWithIEnumerable<int>();
                instance.Items = new int[]
                                 {
                                     1,
                                     2,
                                     3
                                 };

                serialiser.Serialize(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserialiser = new Orckestra.Serialization.Serializer();
                var deserializedValue = (ClassWithIEnumerable<int>)deserialiser.Deserialize(memoryStream);

                Assert.AreEqual(instance.Items.Count(),
                                deserializedValue.Items.Count());
                CollectionAssert.AreEquivalent(instance.Items.ToList(),
                                               deserializedValue.Items.ToList());
            }
        }

        [TestMethod]
        public void SerializeObjectWithDistinctIEnumerable()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Orckestra.Serialization.Serializer();
                var instance = new ClassWithIEnumerable<int>();
                instance.Items = new List<int>
                                 {
                                     1,
                                     1,
                                     2,
                                     3
                                 }.Distinct();

                serialiser.Serialize(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserialiser = new Orckestra.Serialization.Serializer();
                var deserializedValue = (ClassWithIEnumerable<int>)deserialiser.Deserialize(memoryStream);

                Assert.AreEqual(instance.Items.Count(),
                                deserializedValue.Items.Count());
                CollectionAssert.AreEquivalent(instance.Items.ToList(),
                                               deserializedValue.Items.ToList());
            }
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void SerializeSimpleFunc()
        {
            var testData = new List<int> { 1, 2, 3, 4, 5, 6 };

            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Orckestra.Serialization.Serializer();
                System.Func<int, bool> instance = x => x > 3;

                serialiser.Serialize(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserialiser = new Orckestra.Serialization.Serializer();
                var deserializedValue = (System.Func<int, bool>)deserialiser.Deserialize(memoryStream);

                Assert.IsNotNull(deserializedValue);

                Assert.AreEqual(testData.Count(x => instance(x)),
                                testData.Count(x => deserializedValue(x)));
            }
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void SerializeSimpleExpression()
        {
            var testData = new List<int> { 1, 2, 3, 4, 5, 6 };

            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Orckestra.Serialization.Serializer();
                Expression<System.Func<int, bool>> instance = x => x > 3;

                serialiser.Serialize(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserialiser = new Orckestra.Serialization.Serializer();
                var deserializedValue = (Expression<System.Func<int, bool>>)deserialiser.Deserialize(memoryStream);

                Assert.IsNotNull(deserializedValue);

                Assert.AreEqual(testData.Count(instance.Compile()),
                                testData.Count(deserializedValue.Compile()));
            }
        }

        // 








        //[TestMethod]
        //public void zzSerializeClassWithoutRootType()
        //{
        //    using (var memoryStream = new MemoryStream())
        //    {
        //        var instance = new GenericBaseClass<int>
        //        {
        //            Value = 4
        //        };
        //        var serialiser = new Serializer();

        //        serialiser.Serialize(memoryStream,
        //                             instance);
        //        memoryStream.Position = 0;

        //        var deserializedValue = (GenericBaseClass<int>)serialiser.Deserialize(memoryStream);

        //        Assert.AreEqual(instance.Value,
        //                        deserializedValue.Value);
        //    }
        //}
    }
}