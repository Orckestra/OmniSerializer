using System;
using System.Runtime.Serialization;

namespace Orckestra.OmniSerializer
{
    /// <summary>
    /// Exception raised when an object has already been serialized in the current object graph.
    /// This is usally caused by a circular reference.
    /// </summary>
    [Serializable]
    public class ObjectExistsInCurrentSerializationGraphException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectExistsInCurrentSerializationGraphException"/> class.
        /// </summary>
        public ObjectExistsInCurrentSerializationGraphException()
            : base() {}

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectExistsInCurrentSerializationGraphException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public ObjectExistsInCurrentSerializationGraphException(string message)
            : base(message) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectExistsInCurrentSerializationGraphException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
        protected ObjectExistsInCurrentSerializationGraphException(SerializationInfo info, StreamingContext context)
            : base(info,
                   context) {}
    }
}