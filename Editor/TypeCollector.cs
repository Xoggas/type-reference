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

            var types = CollectTypes(field, filter)
                .Distinct()
                .OrderBy(t => t.FullName, StringComparer.Ordinal)
                .ToList();

            Cache[field] = types;
            return types;
        }

        private static IEnumerable<Type> CollectTypes(FieldInfo field, TypeOptionsAttribute filter)
        {
            foreach (var assembly in GetRelevantAssemblies(field))
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

        // Only scans the field's own assembly and the assemblies it references (transitively), instead of
        // every assembly loaded in the domain (which, in a Unity project, includes hundreds of unrelated
        // Editor/package assemblies and makes scanning noticeably slow).
        private static IEnumerable<Assembly> GetRelevantAssemblies(FieldInfo field)
        {
            var declaringAssembly = field.DeclaringType?.Assembly;

            if (declaringAssembly == null)
                return AppDomain.CurrentDomain.GetAssemblies();

            var visited = new HashSet<string> { declaringAssembly.GetName().Name };
            var queue = new Queue<Assembly>();
            var result = new List<Assembly>();

            queue.Enqueue(declaringAssembly);

            while (queue.Count > 0)
            {
                var assembly = queue.Dequeue();
                result.Add(assembly);

                foreach (var referenceName in assembly.GetReferencedAssemblies())
                {
                    if (!visited.Add(referenceName.Name))
                        continue;

                    Assembly referencedAssembly;

                    try
                    {
                        referencedAssembly = Assembly.Load(referenceName);
                    }
                    catch
                    {
                        continue;
                    }

                    queue.Enqueue(referencedAssembly);
                }
            }

            return result;
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
