using System;
using System.Runtime.Serialization;

namespace Orckestra.OmniSerializer
{
    /// <summary>
    /// Exception raised when a type cannot be found.
    /// </summary>
    [Serializable]
    public class TypeNotFoundException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TypeNotFoundException"/> class.
        /// </summary>
        public TypeNotFoundException()
            : base() {}

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeNotFoundException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public TypeNotFoundException(string message)
            : base(message) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeNotFoundException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
        protected TypeNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info,
                   context) {}
    }
}