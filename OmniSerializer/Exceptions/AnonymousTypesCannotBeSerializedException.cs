using System;
using System.Runtime.Serialization;

namespace OmniSerializer.Exceptions
{
    /// <summary>
    /// Exception raised when trying to serialize an anonymous type.
    /// </summary>
    [Serializable]
    public class AnonymousTypesCannotBeSerializedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AnonymousTypesCannotBeSerializedException"/> class.
        /// </summary>
        /// <param name="type">The anonymous type that cannot be serialized.</param>
        public AnonymousTypesCannotBeSerializedException(Type type)
            : base(string.Format("Anonymous type {0} cannot be serialized.",
                                 type)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AnonymousTypesCannotBeSerializedException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
        protected AnonymousTypesCannotBeSerializedException(SerializationInfo info, StreamingContext context)
            : base(info,
                   context) { }
    }
}