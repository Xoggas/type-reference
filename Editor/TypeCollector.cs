namespace TypeReferences.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Collects the types eligible for a <see cref="TypeReference"/> field's dropdown, given the field's
    /// <see cref="TypeOptionsAttribute"/> (or <see cref="InheritsAttribute"/>) filter. Results are cached
    /// per field for the lifetime of the current domain.
    /// </summary>
    internal static class TypeCollector
    {
        private static readonly Dictionary<FieldInfo, List<Type>> Cache = new Dictionary<FieldInfo, List<Type>>();

        public static List<Type> GetTypes(FieldInfo field, TypeOptionsAttribute filter)
        {
            if (Cache.TryGetValue(field, out var cached))
                return cached;

            var types = CollectTypes(filter).Distinct().OrderBy(t => t.FullName, StringComparer.Ordinal).ToList();
            Cache[field] = types;
            return types;
        }

        private static IEnumerable<Type> CollectTypes(TypeOptionsAttribute filter)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;

                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types.Where(t => t != null).ToArray();
                }

                foreach (var type in types)
                {
                    if (IsSelectable(type, filter))
                        yield return type;
                }
            }

            if (filter?.IncludeTypes == null)
                yield break;

            foreach (var type in filter.IncludeTypes)
            {
                if (type != null)
                    yield return type;
            }
        }

        private static bool IsSelectable(Type type, TypeOptionsAttribute filter)
        {
            if (type.FullName == null || type.FullName.IndexOf('<') >= 0)
                return false;

            bool allowInternal = filter != null && filter.AllowInternal;

            if (!allowInternal && !type.IsVisible)
                return false;

            return filter == null || filter.MatchesRequirements(type);
        }
    }
}
