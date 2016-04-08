using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Orckestra.OmniSerializer.TypeSerializers
{
    public static class SerializedTypeResolver
    {
        private static readonly ConcurrentDictionary<string, TypeWithHashCode> TypesFromString = new ConcurrentDictionary<string, TypeWithHashCode>();
        internal static TypeWithHashCode GetTypeFromFullName(string typeFullName)
        {
            return TypesFromString.GetOrAdd(typeFullName,
                                            name => new TypeWithHashCode(name));
        }

        internal static TypeWithHashCode GetTypeFromFullName(Type type)
        {
            return TypesFromString.GetOrAdd(type.AssemblyQualifiedName,
                                            name => new TypeWithHashCode(type));
        }

        public static string GetShortNameFromType(Type type)
        {
            return GetTypeFromFullName(type).ShortTypeName;
        }

        private class TypeReplacement
        {
            public string ShortName { get; set; }
            public string LongName { get; set; }
        }

        private static readonly Lazy<List<TypeReplacement>> TypeReplacements = new Lazy<List<TypeReplacement>>(CreateTypeReplacements);

        private static List<TypeReplacement> CreateTypeReplacements()
        {
            var replacements = new List<TypeReplacement>();

            var strType = typeof(string);

            // Shorten the type name written to the stream.
            // Reading a string from the MemoryStream is expensive and having a smaller name provides a nice performance increase (~4-5 times) while having a smaller payload

            // the order of the replacements is important
            replacements.Add(new TypeReplacement { ShortName = "~mcl~", LongName = strType.AssemblyQualifiedName.Substring(strType.FullName.Length) });
            replacements.Add(new TypeReplacement { ShortName = "~v~", LongName = ", Version=" });
            replacements.Add(new TypeReplacement { ShortName = "~c=n~", LongName = ", Culture=neutral" });
            replacements.Add(new TypeReplacement { ShortName = "~o.pkt~", LongName = ", PublicKeyToken=8867db2659d5042" });
            replacements.Add(new TypeReplacement { ShortName = "~pkt~", LongName = ", PublicKeyToken=" });
            replacements.Add(new TypeReplacement { ShortName = "~o.o.s~", LongName = "Orckestra.Overture.ServiceModel" });
            replacements.Add(new TypeReplacement { ShortName = "~o.o.e~", LongName = "Orckestra.Overture.Entities" });
            replacements.Add(new TypeReplacement { ShortName = "~o.o~", LongName = "Orckestra.Overture" });
            replacements.Add(new TypeReplacement { ShortName = "~o~", LongName = "Orckestra" });
            replacements.Add(new TypeReplacement { ShortName = "~SCG~", LongName = "System.Collections.Generic." });
            replacements.Add(new TypeReplacement { ShortName = "~SC~", LongName = "System.Collections." });
            replacements.Add(new TypeReplacement { ShortName = "~S~", LongName = "System." });
            replacements.Add(new TypeReplacement { ShortName = "~P~", LongName = "Product" });

            return replacements;
        }

        public static string ApplyTypeReplacements(string typeName)
        {
            foreach (var tr in TypeReplacements.Value)
            {
                typeName = typeName.Replace(tr.LongName, tr.ShortName);
            }
            return typeName;
        }

        public static string RevertTypeReplacements(string typeName)
        {
            for (var i = TypeReplacements.Value.Count - 1; i >= 0; i--)
            {
                var tr = TypeReplacements.Value[i];
                typeName = typeName.Replace(tr.ShortName, tr.LongName);
            }
            return typeName;
        }
    }
}