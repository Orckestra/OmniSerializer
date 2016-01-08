using System;

namespace Orckestra.OmniSerializer.Tests.TestObjects
{
    public interface IHierarchy
    {
    }

    [Serializable]
    public class BaseHierarchy<T> : IHierarchy
    {
        public T Value { get; set; }
    }

    [Serializable]
    public class ChildIntHierarchy : BaseHierarchy<int>
    {
        public ChildIntHierarchy(int value)
        {
            Value = value;
        }
    }

    [Serializable]
    public class ChildStringHierarchy : BaseHierarchy<string>
    {
        public ChildStringHierarchy(string value)
        {
            Value = value;
        }
    }
}