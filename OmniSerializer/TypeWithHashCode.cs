using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Orckestra.OmniSerializer.TypeSerializers;

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
                Type = System.Type.GetType(SerializedTypeResolver.RevertTypeReplacements(typeName), false);
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
                var fields = Helpers.GetFieldInfos(Type, false)
                                    .OrderBy(f => f.Name,
                                             StringComparer.Ordinal)
                                    .Select(x => x.Name + x.FieldType.AssemblyQualifiedName);
                string stringToHash = String.Format("{0}{1}",
                                                    Type.AssemblyQualifiedName,
                                                    String.Join(",",
                                                                fields));
                unchecked
                {
                    HashCode = 23;
                    foreach (char c in stringToHash)
                    {
                        HashCode = HashCode * 31 + c;
                    }
                }
            }
        }
        private string _shortTypeName;
        public string ShortTypeName
        {
            get
            {
                if (_shortTypeName == null)
                {
                    _shortTypeName = Type.AssemblyQualifiedName;
                    if (!Type.IsGenericType && Type.Namespace != null && Type.Namespace.StartsWith("System."))
                    {
                        // try to resolve the type using only the FullName
                        var resolvedType = Type.GetType(Type.FullName);
                        if (resolvedType != null)
                        {
                            _shortTypeName = Type.FullName;
                        }
                    }
                    _shortTypeName = SerializedTypeResolver.ApplyTypeReplacements(_shortTypeName);
                }
                return _shortTypeName;
            }
        }
    }
}