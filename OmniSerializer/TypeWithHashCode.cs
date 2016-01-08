using System;
using System.IO;
using System.Linq;

namespace Orckestra.OmniSerializer
{
    internal class TypeWithHashCode
    {
        public Type Type { get; private set; }
        public int HashCode { get; private set; }

        public TypeWithHashCode(string typeName)
        {
            try
            {
                Type = System.Type.GetType(typeName,
                                           false);
            }
            catch (FileLoadException)
            {
                // specified type was not found
                // we ignore this error because this case is handled elsewhere
                Type = null;
            }
            ComputeSerializationHashCode();
        }

        public TypeWithHashCode(Type type)
        {
            Type = type;
            ComputeSerializationHashCode();
        }

        private void ComputeSerializationHashCode()
        {
            if (Type != null)
            {
                var fields = Type.GetFields()
                                 .Select(x => x.Name + x.FieldType.AssemblyQualifiedName);
                HashCode = String.Format("{0}{1}",
                                         Type.AssemblyQualifiedName,
                                         String.Join(",",
                                                     fields))
                                 .GetHashCode();
            }
        }
    }
}