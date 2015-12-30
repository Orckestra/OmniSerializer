using System;

namespace OmniSerializer.Tests.TestObjects
{
    [Serializable]
    public class GenericBaseClass<T>
    {
        public T Value { get; set; }
    }

    [Serializable]
    public class IntGenericBaseClass : GenericBaseClass<int>
    {
        public IntGenericBaseClass()
        {
            
        }

        public IntGenericBaseClass(int value)
        {
            Value = value;
        }

        public override bool Equals(object obj)
        {
            var other = obj as IntGenericBaseClass;
            if (other == null)
            {
                return false;
            }

            return Value == other.Value;
        }

        public override int GetHashCode()
        {
            return "IntGenericBaseClass".GetHashCode() + Value.GetHashCode();
        }
    }
}