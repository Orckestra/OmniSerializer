using System;
using System.Runtime.Serialization;

namespace Orckestra.OmniSerializer
{
    /// <summary>
    /// Exception raised when the definition of an object was modified since its serialization.
    /// </summary>
    [Serializable]
    public class TypeWasModifiedSinceItWasSerializedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TypeWasModifiedSinceItWasSerializedException"/> class.
        /// </summary>
        /// <param name="type">The type that was modified since it was serialized.</param>
        public TypeWasModifiedSinceItWasSerializedException(Type type)
            : base(string.Format("Type {0} was modified since it was serialized.",
                                 type)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeWasModifiedSinceItWasSerializedException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
        protected TypeWasModifiedSinceItWasSerializedException(SerializationInfo info, StreamingContext context)
            : base(info,
                   context) { }
    }
}