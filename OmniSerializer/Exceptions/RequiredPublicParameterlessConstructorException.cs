using System;
using System.Runtime.Serialization;

namespace OmniSerializer
{
    /// <summary>
    /// Exception raised when an object requires a parameterless public constructor to be serialized.
    /// </summary>
    [Serializable]
    public class RequiredPublicParameterlessConstructorException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequiredPublicParameterlessConstructorException"/> class.
        /// </summary>
        /// <param name="type">The type that requires a parameterless public constructor.</param>
        public RequiredPublicParameterlessConstructorException(Type type)
            : base(String.Format("Type {0} requires a parameterless public contructor.",
                                 type)) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="RequiredPublicParameterlessConstructorException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
        protected RequiredPublicParameterlessConstructorException(SerializationInfo info, StreamingContext context)
            : base(info,
                   context) {}
    }
}