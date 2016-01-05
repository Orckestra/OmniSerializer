using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OmniSerializer.Exceptions;
using OmniSerializer.Tests.TestObjects;

namespace OmniSerializer.Tests
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
            SerializePrimitive(new StructForTestingWithString
                               {
                                    StringValue = "abc",
                                    IntWrapper = new IntGenericBaseClass(123)
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
                var serialiser = new Serializer();

                serialiser.SerializeObject(memoryStream,
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

                var serialiser = new Serializer();

                serialiser.SerializeObject(memoryStream,
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

                var serialiser = new Serializer();

                serialiser.SerializeObject(memoryStream,
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
                var serialiser = new Serializer();

                serialiser.SerializeObject(memoryStream,
                                     classInstance);
                memoryStream.Position = 0;

                var deserialiser = new Serializer();
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
        [ExpectedException(typeof(ObjectExistsInCurrentSerializationGraphException))]
        public void SerializeCircularReference()
        {
            var instance1 = new CircularReference();
            var instance2 = new CircularReference();
            instance1.Parent = instance2;
            instance2.Child = instance1;

            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Serializer();

                serialiser.SerializeObject(memoryStream,
                                     instance1);
                memoryStream.Position = 0;

                var deserializedValue = (CircularReference) serialiser.Deserialize(memoryStream);

                Assert.IsTrue(ReferenceEquals(deserializedValue,
                                              deserializedValue.Child));
            }
        }

        [TestMethod]
        public void SerializeTrackMultipleReference()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Serializer();
                var instance = new IntGenericBaseClass(123);
                var list = new List<IntGenericBaseClass>
                           {
                               instance,
                               instance
                           };

                serialiser.SerializeObject(memoryStream,
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
                var serialiser = new Serializer();
                var instance = 3;
                var list = new List<int>
                           {
                               instance,
                               instance
                           };

                serialiser.SerializeObject(memoryStream,
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
                var serialiser = new Serializer();

                serialiser.SerializeObject(memoryStream,
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
                var serialiser = new Serializer();

                serialiser.SerializeObject(memoryStream,
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
                var serialiser = new Serializer();

                serialiser.SerializeObject(memoryStream,
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
                var serialiser = new Serializer();

                serialiser.SerializeObject(memoryStream,
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
                var serialiser = new Serializer();

                serialiser.SerializeObject(memoryStream,
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
                var serialiser = new Serializer();

                serialiser.SerializeObject(memoryStream,
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
                var serialiser = new Serializer();

                serialiser.SerializeObject(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                serialiser.Deserialize(memoryStream);
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
                var serialiser = new Serializer();

                serialiser.SerializeObject(memoryStream,
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
        public void SerializeCustomDictionaryWithDictionaryProperty()
        {
            using (var memoryStream = new MemoryStream())
            {
                var instance = new CustomDictionaryWithDictionaryProperty<string, object>
                {
                    {"Key1", 123},
                    {"Key2", "abc"},
                    {"Key3", new IntGenericBaseClass(3) },
                };
                instance.SwitchedDictionary = new CustomDictionaryWithAdditionalPropertiesAndGenerics<object, string>
                {
                    { 123, "Key1"},
                    { "abc", "Key2"},
                    { new IntGenericBaseClass(3), "Key3" },
                };
                var serialiser = new Serializer();

                serialiser.SerializeObject(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserializedValue = (CustomDictionaryWithDictionaryProperty<string, object>)serialiser.Deserialize(memoryStream);

                CompareDictionaries(instance,
                                    deserializedValue);
                CompareDictionaries(instance.SwitchedDictionary,
                                    deserializedValue.SwitchedDictionary);
            }
        }

        private static void CompareDictionaries<TKey, TValue>(Dictionary<TKey, TValue> instance, Dictionary<TKey, TValue> deserializedValue)
        {
            Assert.AreEqual(instance.Count,
                            deserializedValue.Count);
            Assert.AreEqual(instance.Comparer.GetType(),
                            deserializedValue.Comparer.GetType());
            CollectionAssert.AreEquivalent(instance.Keys,
                                           deserializedValue.Keys);
            foreach (var key in instance.Keys)
            {
                Assert.AreEqual(instance[key],
                                deserializedValue[key]);
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
                var serialiser = new Serializer();

                serialiser.SerializeObject(memoryStream,
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
                var serialiser = new Serializer(new[]
                                                  {
                                                      typeof(ClassWithClassProperty),
                                                      typeof(StructForTesting),
                                                      typeof(IntGenericBaseClass),
                                                  });

                serialiser.SerializeObject(memoryStream,
                                     classInstance);
                memoryStream.Position = 0;

                var deserialiser = new Serializer(new []
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
                var serialiser = new Serializer();

                serialiser.SerializeObject(memoryStream,
                                     list);
                memoryStream.Position = 0;

                var deserialiser = new Serializer();
                var deserializedValue = (List<IHierarchy>)deserialiser.Deserialize(memoryStream);

                Assert.AreEqual(list.Count,
                                deserializedValue.Count);
                Assert.AreEqual(list.OfType<ChildIntHierarchy>().First().Value,
                                deserializedValue.OfType<ChildIntHierarchy>().First().Value);
                Assert.AreEqual(list.OfType<ChildStringHierarchy>().First().Value,
                                deserializedValue.OfType<ChildStringHierarchy>().First().Value);
            }
        }

        [TestMethod]
        public void SerializeObjectWithListAsIEnumerable()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Serializer();
                var instance = new ClassWithIEnumerable<int> { Items = new List<int> { 1, 2, 3 } };

                serialiser.SerializeObject(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserialiser = new Serializer();
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
                var serialiser = new Serializer();
                var instance = new ClassWithIEnumerable<int>
                               {
                                   Items = new HashSet<int> { 1, 2, 3 }
                               };

                serialiser.SerializeObject(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserialiser = new Serializer();
                var deserializedValue = (ClassWithIEnumerable<int>)deserialiser.Deserialize(memoryStream);

                Assert.AreEqual(instance.Items.Count(),
                                deserializedValue.Items.Count());
                CollectionAssert.AreEquivalent(instance.Items.ToList(),
                                               deserializedValue.Items.ToList());
            }
        }

        [TestMethod]
        public void SerializeHashSetWithEqualityComparer()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Serializer();
                var instance = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) { "a", "b", "C" };

                serialiser.SerializeObject(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserialiser = new Serializer();
                var deserializedValue = (HashSet<string>)deserialiser.Deserialize(memoryStream);

                Assert.AreEqual(instance.Comparer.GetType(),
                                deserializedValue.Comparer.GetType());
                Assert.AreEqual(instance.Count(),
                                deserializedValue.Count());
                CollectionAssert.AreEquivalent(instance.ToList(),
                                               deserializedValue.ToList());
            }
        }

        [TestMethod]
        public void SerializeObjectWithEnumProperty()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Serializer();
                var instance = new GenericBaseClass<EnumForTesting> { Value = EnumForTesting.Two };

                serialiser.SerializeObject(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserialiser = new Serializer();
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
                var serialiser = new Serializer();
                var instance = new Hashtable
                               {
                                   {1, 2},
                                   {"a", "b"},
                               };

                serialiser.SerializeObject(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserialiser = new Serializer();
                var deserializedValue = (Hashtable)deserialiser.Deserialize(memoryStream);

                Assert.AreEqual(instance.Count,
                                deserializedValue.Count);
                CollectionAssert.AreEquivalent(instance,
                                               deserializedValue);
            }
        }

        [TestMethod]
        public void SerializeHashtableWithEqualityComparer()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Serializer();
                var instance = new Hashtable(StringComparer.CurrentCultureIgnoreCase)
                               {
                                   {"e", 2},
                                   {"a", "b"},
                               };

                serialiser.SerializeObject(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserialiser = new Serializer();
                var deserializedValue = (Hashtable)deserialiser.Deserialize(memoryStream);

                Assert.IsNotNull(GetHashTableComparer(instance));
                Assert.IsNotNull(GetHashTableComparer(deserializedValue));
                Assert.AreEqual(GetHashTableComparer(instance).GetType(),
                                GetHashTableComparer(deserializedValue).GetType());
                Assert.AreEqual(instance.Count,
                                deserializedValue.Count);
                CollectionAssert.AreEquivalent(instance,
                                               deserializedValue);
            }
        }

        private object GetHashTableComparer(Hashtable ht)
        {
            if (ht == null)
            {
                return null;
            }

            return ht.GetType()
                     .GetProperty("EqualityComparer",
                                  BindingFlags.NonPublic | BindingFlags.GetProperty | BindingFlags.Instance)
                     .GetValue(ht);
        }

        [TestMethod]
        public void SerializeObjectWithArrayAsIEnumerable()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Serializer();
                var instance = new ClassWithIEnumerable<int> { Items = new [] { 1, 2, 3 } };

                serialiser.SerializeObject(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserialiser = new Serializer();
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
                var serialiser = new Serializer();
                var instance = new ClassWithIEnumerable<int> { Items = new List<int> { 1, 1, 2, 3 }.Distinct() };

                serialiser.SerializeObject(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserialiser = new Serializer();
                var deserializedValue = (ClassWithIEnumerable<int>)deserialiser.Deserialize(memoryStream);

                Assert.AreEqual(instance.Items.Count(),
                                deserializedValue.Items.Count());
                CollectionAssert.AreEquivalent(instance.Items.ToList(),
                                               deserializedValue.Items.ToList());
            }
        }

        [TestMethod]
        public void SerializeObjectWithWhereIEnumerable()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Serializer();
                var instance = new ClassWithIEnumerable<int> { Items = new List<int> { 1, 2, 3 }.Where(x => x > 1) };

                serialiser.SerializeObject(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserialiser = new Serializer();
                var deserializedValue = (ClassWithIEnumerable<int>)deserialiser.Deserialize(memoryStream);

                Assert.AreEqual(instance.Items.Count(),
                                deserializedValue.Items.Count());
                CollectionAssert.AreEquivalent(instance.Items.ToList(),
                                               deserializedValue.Items.ToList());
            }
        }

        [TestMethod]
        public void SerializeObjectWithOrderByIEnumerable()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Serializer();
                var instance = new ClassWithIEnumerable<int> { Items = new List<int> { 1, 2, 3 }.OrderBy(x => x) };

                serialiser.SerializeObject(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserialiser = new Serializer();
                var deserializedValue = (ClassWithIEnumerable<int>)deserialiser.Deserialize(memoryStream);

                Assert.AreEqual(instance.Items.Count(),
                                deserializedValue.Items.Count());
                CollectionAssert.AreEquivalent(instance.Items.ToList(),
                                               deserializedValue.Items.ToList());
            }
        }

        [TestMethod]
        public void SerializeObjectWithDefaultIfEmptyIEnumerable()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Serializer();
                var instance = new ClassWithIEnumerable<int> { Items = new List<int> { 1, 2, 3 }.DefaultIfEmpty(123) };

                serialiser.SerializeObject(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserialiser = new Serializer();
                var deserializedValue = (ClassWithIEnumerable<int>)deserialiser.Deserialize(memoryStream);

                Assert.AreEqual(instance.Items.Count(),
                                deserializedValue.Items.Count());
                CollectionAssert.AreEquivalent(instance.Items.ToList(),
                                               deserializedValue.Items.ToList());
            }
        }

        [TestMethod]
        public void SerializeObjectWithExceptIEnumerable()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Serializer();
                var instance = new ClassWithIEnumerable<int> { Items = new List<int> { 1, 2, 3 }.Except(new[] { 2 }) };

                serialiser.SerializeObject(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserialiser = new Serializer();
                var deserializedValue = (ClassWithIEnumerable<int>)deserialiser.Deserialize(memoryStream);

                Assert.AreEqual(instance.Items.Count(),
                                deserializedValue.Items.Count());
                CollectionAssert.AreEquivalent(instance.Items.ToList(),
                                               deserializedValue.Items.ToList());
            }
        }

        [TestMethod]
        public void SerializeObjectWithUnionIEnumerable()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Serializer();
                var instance = new ClassWithIEnumerable<int> { Items = new List<int> { 1, 2, 3 }.Union(new[] { 4 }) };

                serialiser.SerializeObject(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserialiser = new Serializer();
                var deserializedValue = (ClassWithIEnumerable<int>)deserialiser.Deserialize(memoryStream);

                Assert.AreEqual(instance.Items.Count(),
                                deserializedValue.Items.Count());
                CollectionAssert.AreEquivalent(instance.Items.ToList(),
                                               deserializedValue.Items.ToList());
            }
        }

        [TestMethod]
        public void SerializeObjectWithIntersectIEnumerable()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Serializer();
                var instance = new ClassWithIEnumerable<int> { Items = new List<int> { 1, 2, 3 }.Intersect(new[] { 2 }) };

                serialiser.SerializeObject(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserialiser = new Serializer();
                var deserializedValue = (ClassWithIEnumerable<int>)deserialiser.Deserialize(memoryStream);

                Assert.AreEqual(instance.Items.Count(),
                                deserializedValue.Items.Count());
                CollectionAssert.AreEquivalent(instance.Items.ToList(),
                                               deserializedValue.Items.ToList());
            }
        }

        [TestMethod]
        public void SerializeObjectWithOfTypeIEnumerable()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Serializer();
                var instance = new ClassWithIEnumerable<int> { Items = new List<int> { 1, 2, 3 }.OfType<int>() };

                serialiser.SerializeObject(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserialiser = new Serializer();
                var deserializedValue = (ClassWithIEnumerable<int>)deserialiser.Deserialize(memoryStream);

                Assert.AreEqual(instance.Items.Count(),
                                deserializedValue.Items.Count());
                CollectionAssert.AreEquivalent(instance.Items.ToList(),
                                               deserializedValue.Items.ToList());
            }
        }

        [TestMethod]
        public void SerializeObjectWithSkipByIEnumerable()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Serializer();
                var instance = new ClassWithIEnumerable<int> { Items = new List<int> { 1, 2, 3 }.Skip(1) };

                serialiser.SerializeObject(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserialiser = new Serializer();
                var deserializedValue = (ClassWithIEnumerable<int>)deserialiser.Deserialize(memoryStream);

                Assert.AreEqual(instance.Items.Count(),
                                deserializedValue.Items.Count());
                CollectionAssert.AreEquivalent(instance.Items.ToList(),
                                               deserializedValue.Items.ToList());
            }
        }

        [TestMethod]
        public void SerializeObjectWithTakeByIEnumerable()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Serializer();
                var instance = new ClassWithIEnumerable<int> { Items = new List<int> { 1, 2, 3 }.Take(1) };

                serialiser.SerializeObject(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserialiser = new Serializer();
                var deserializedValue = (ClassWithIEnumerable<int>)deserialiser.Deserialize(memoryStream);

                Assert.AreEqual(instance.Items.Count(),
                                deserializedValue.Items.Count());
                CollectionAssert.AreEquivalent(instance.Items.ToList(),
                                               deserializedValue.Items.ToList());
            }
        }

        [TestMethod]
        public void SerializeObjectWithSelectByIEnumerable()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Serializer();
                var instance = new ClassWithIEnumerable<int> { Items = new List<int> { 1, 2, 3 }.Select(x => x * 2) };

                serialiser.SerializeObject(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserialiser = new Serializer();
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
                var serialiser = new Serializer();
                System.Func<int, bool> instance = x => x > 3;

                serialiser.SerializeObject(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserialiser = new Serializer();
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
                var serialiser = new Serializer();
                Expression<System.Func<int, bool>> instance = x => x > 3;

                serialiser.SerializeObject(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserialiser = new Serializer();
                var deserializedValue = (Expression<System.Func<int, bool>>)deserialiser.Deserialize(memoryStream);

                Assert.IsNotNull(deserializedValue);

                Assert.AreEqual(testData.Count(instance.Compile()),
                                testData.Count(deserializedValue.Compile()));
            }
        }

        [TestMethod]
        [ExpectedException(typeof(AnonymousTypesCannotBeSerializedException))]
        public void SerializeAnonymousObject()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Serializer();
                var instance = new { Property1 = "hello", Property2 = 123 };

                serialiser.SerializeObject(memoryStream,
                                     instance);
                Assert.Fail();
            }
        }

        [TestMethod]
        public void SerializeClassWithDynamicObject()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Serializer();
                ClassWithDynamicProperty instance = new ClassWithDynamicProperty { Value = 123 };

                serialiser.SerializeObject(memoryStream,
                                           instance);
                memoryStream.Position = 0;

                var deserialiser = new Serializer();
                var deserializedValue = (ClassWithDynamicProperty)deserialiser.Deserialize(memoryStream);

                Assert.IsNotNull(deserializedValue);

                Assert.AreEqual(instance.Value,
                                deserializedValue.Value);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(TypeNotFoundException))]
        public void DeserializeUnknownType()
        {
            byte[] bytes;
            var instance = new IntGenericBaseClass(123);
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Serializer();

                serialiser.SerializeObject(memoryStream,
                                           instance);
                bytes = memoryStream.ToArray();
            }

            bytes[10] = (byte)'z'; // change the type to something invalid

            using (var memoryStream = new MemoryStream(bytes))
            {
                var deserialiser = new Serializer();
                var deserializedValue = (IntGenericBaseClass)deserialiser.Deserialize(memoryStream);

                Assert.IsNull(deserializedValue);
                Assert.Fail();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(TypeWasModifiedSinceItWasSerializedException))]
        public void DeserializeTypeThatWasModified()
        {
            byte[] bytes;
            var instance = new IntGenericBaseClass(123);
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Serializer();

                serialiser.SerializeObject(memoryStream,
                                           instance);
                bytes = memoryStream.ToArray();
            }

            var needle = typeof(IntGenericBaseClass).AssemblyQualifiedName;
            var index = System.Text.Encoding.ASCII.GetString(bytes).IndexOf(needle);

            // this is a hackish way to change the hashcode of a serialized object
            // if the way/order (currently TypeName + Hash) that an object is serialized changes the line below will need to be modified to target a byte of the hashcode
            bytes[index + needle.Length + 1] = (bytes[index + needle.Length + 1] == 255) ? (byte)0 : (byte)(bytes[index + needle.Length] + 1); // change the hashcode to something invalid

            using (var memoryStream = new MemoryStream(bytes))
            {
                var deserialiser = new Serializer();
                var deserializedValue = (IntGenericBaseClass)deserialiser.Deserialize(memoryStream);

                Assert.IsNull(deserializedValue);
                Assert.Fail();
            }
        }

        [TestMethod]
        public void SerializeExpandoObject()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Serializer();
                dynamic instance = new ExpandoObject();
                instance.Property1 = 123;
                instance.Property2 = "abc";
                instance.Property3 = new IntGenericBaseClass(349);

                serialiser.SerializeObject(memoryStream,
                                           instance);
                memoryStream.Position = 0;

                var deserialiser = new Serializer();
                var deserializedValue = (dynamic)deserialiser.Deserialize(memoryStream);

                Assert.IsNotNull(deserializedValue);

                Assert.AreEqual(instance.Property1,
                                deserializedValue.Property1);
                Assert.AreEqual(instance.Property2,
                                deserializedValue.Property2);
                Assert.AreEqual(instance.Property3,
                                deserializedValue.Property3);
            }
        }

        [TestMethod]
        public void SerializeTypedQueue()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Serializer();
                var instance = new Queue<int>();
                instance.Enqueue(1);
                instance.Enqueue(2);
                instance.Enqueue(3);

                serialiser.SerializeObject(memoryStream,
                                           instance);
                memoryStream.Position = 0;

                var deserialiser = new Serializer();
                var deserializedValue = (Queue<int>)deserialiser.Deserialize(memoryStream);

                Assert.IsNotNull(deserializedValue);

                Assert.AreEqual(instance.Count,
                                deserializedValue.Count);
                CollectionAssert.AreEquivalent(instance.ToArray(),
                                               deserializedValue.ToArray());
            }
        }

        [TestMethod]
        public void SerializeUntypedQueue()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Serializer();
                var instance = new Queue();
                instance.Enqueue(1);
                instance.Enqueue(2);
                instance.Enqueue("abc");
                instance.Enqueue(new IntGenericBaseClass(123));
                
                serialiser.SerializeObject(memoryStream,
                                           instance);
                memoryStream.Position = 0;

                var deserialiser = new Serializer();
                var deserializedValue = (Queue)deserialiser.Deserialize(memoryStream);

                Assert.IsNotNull(deserializedValue);

                Assert.AreEqual(instance.Count,
                                deserializedValue.Count);
                CollectionAssert.AreEquivalent(instance.ToArray(),
                                               deserializedValue.ToArray());
            }
        }

        [TestMethod]
        public void SerializeTypedStack()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Serializer();
                var instance = new Stack<int>();
                instance.Push(1);
                instance.Push(2);
                instance.Push(3);

                serialiser.SerializeObject(memoryStream,
                                           instance);
                memoryStream.Position = 0;

                var deserialiser = new Serializer();
                var deserializedValue = (Stack<int>)deserialiser.Deserialize(memoryStream);

                Assert.IsNotNull(deserializedValue);

                Assert.AreEqual(instance.Count,
                                deserializedValue.Count);
                CollectionAssert.AreEquivalent(instance.ToArray(),
                                               deserializedValue.ToArray());
            }
        }

        [TestMethod]
        public void SerializeUntypedStack()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Serializer();
                var instance = new Stack();
                instance.Push(1);
                instance.Push(2);
                instance.Push("abc");
                instance.Push(new IntGenericBaseClass(123));

                serialiser.SerializeObject(memoryStream,
                                           instance);
                memoryStream.Position = 0;

                var deserialiser = new Serializer();
                var deserializedValue = (Stack)deserialiser.Deserialize(memoryStream);

                Assert.IsNotNull(deserializedValue);

                Assert.AreEqual(instance.Count,
                                deserializedValue.Count);
                CollectionAssert.AreEquivalent(instance.ToArray(),
                                               deserializedValue.ToArray());
            }
        }

        [TestMethod]
        public void SerializeInParallel()
        {
            SerializerTestHelper.ClearTypeDataMap(); // empty the serialization Type to TypeData dictionary to start from a fresh state.

            for (int i = 0; i < 100; i++)
            {
                Parallel.For(0,
                             1000,
                             k =>
                             {
                                 using (var memoryStream = new MemoryStream())
                                 {
                                     var serialiser = new Serializer();
                                     var instance = new ClassWithIEnumerable<int> { Items = new List<int> { 1, 1, 2, 3 }.Distinct() };

                                     serialiser.SerializeObject(memoryStream,
                                                          instance);
                                     memoryStream.Position = 0;

                                     var deserialiser = new Serializer();
                                     var deserializedValue = (ClassWithIEnumerable<int>)deserialiser.Deserialize(memoryStream);

                                     Assert.AreEqual(instance.Items.Count(),
                                                     deserializedValue.Items.Count());
                                     CollectionAssert.AreEquivalent(instance.Items.ToList(),
                                                                    deserializedValue.Items.ToList());
                                 }
                             });
            }
        }

        [TestMethod]
        public void SerializeEnumEqualityComparer()
        {
            using (var memoryStream = new MemoryStream())
            {
                var serialiser = new Serializer();
                var instance = new Dictionary<EnumForTesting, int> { { EnumForTesting.One, 1 }, { EnumForTesting.Two, 2 } };

                serialiser.SerializeObject(memoryStream,
                                     instance);
                memoryStream.Position = 0;

                var deserialiser = new Serializer();
                var deserializedValue = (Dictionary<EnumForTesting, int>)deserialiser.Deserialize(memoryStream);

                Assert.IsNotNull(deserializedValue);

                CompareDictionaries(instance,
                                    deserializedValue);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectExistsInCurrentSerializationGraphException))]
        public void TrackObjectsWithSameHashcode()
        {
            var object1 = new IntGenericBaseClass(123);
            var object2 = new IntGenericBaseClass(123);
            Assert.AreEqual(object1, object2);

            using (var tracker = new ReferenceTracker())
            {
                tracker.TrackObject(object1);
                tracker.TrackObject(object2);
            }
        }
    }
}