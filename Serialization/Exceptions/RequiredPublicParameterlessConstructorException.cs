using System;

namespace Orckestra.Serialization
{
    public class RequiredPublicParameterlessConstructorException : Exception
    {
        public RequiredPublicParameterlessConstructorException(Type type)
            : base(String.Format("Type {0} requires a parameterless public contructor.",
                                 type)) {}
    }
}