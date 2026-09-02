namespace TypeReferences.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UnityEditor;

    /// <summary>Builds and caches the candidate list used by TypeReference fields.</summary>
    internal static class TypeCollector
    {
        private static readonly Dictionary<FieldInfo, IReadOnlyList<Type>> FieldCache =
            new Dictionary<FieldInfo, IReadOnlyList<Type>>();

        private static readonly Dictionary<Type, IReadOnlyList<Type>> DerivedTypeCache =
            new Dictionary<Type, IReadOnlyList<Type>>();

        private static IReadOnlyList<Type> _allLoadedTypes;

        public static IReadOnlyList<Type> GetTypes(FieldInfo field, TypeOptionsAttribute options)
        {
            if (field != null && FieldCache.TryGetValue(field, out var cached))
                return cached;

            var result = Collect(options)
                .Where(type => IsSelectable(type, options))
                .Concat(GetExplicitlyIncludedTypes(options))
                .Distinct()
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToArray();

            if (field != null)
                FieldCache[field] = result;

            return result;
        }

        private static IEnumerable<Type> Collect(TypeOptionsAttribute options)
        {
            if (!(options is InheritsAttribute inherits))
                return GetAllLoadedTypes();

            var candidates = GetSmallestCandidateSet(inherits.BaseTypes);

            if (!inherits.IncludeBaseType)
                return candidates;

            return candidates.Concat(inherits.BaseTypes);
        }

        private static IReadOnlyList<Type> GetSmallestCandidateSet(IReadOnlyList<Type> baseTypes)
        {
            IReadOnlyList<Type> smallest = Array.Empty<Type>();

            for (int i = 0; i < baseTypes.Count; i++)
            {
                var candidates = GetDerivedTypes(baseTypes[i]);

                if (i == 0 || candidates.Count < smallest.Count)
                    smallest = candidates;
            }

            return smallest;
        }

        private static IReadOnlyList<Type> GetDerivedTypes(Type baseType)
        {
            if (DerivedTypeCache.TryGetValue(baseType, out var cached))
                return cached;

            IReadOnlyList<Type> result;

            try
            {
                result = TypeCache.GetTypesDerivedFrom(baseType).ToArray();
            }
            catch
            {
                result = GetAllLoadedTypes()
                    .Where(type => type != baseType && baseType.IsAssignableFrom(type))
                    .ToArray();
            }

            DerivedTypeCache[baseType] = result;
            return result;
        }

        private static IReadOnlyList<Type> GetAllLoadedTypes()
        {
            if (_allLoadedTypes != null)
                return _allLoadedTypes;

            var result = new List<Type>();

#pragma warning disable UAC0005
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
#pragma warning restore UAC0005
            {
                if (assembly == null || assembly.IsDynamic)
                    continue;

                try
                {
                    result.AddRange(assembly.GetTypes());
                }
                catch (ReflectionTypeLoadException exception)
                {
                    result.AddRange(exception.Types.Where(type => type != null));
                }
                catch
                {
                    // A broken optional assembly must not break every TypeReference field.
                }
            }

            _allLoadedTypes = result;
            return result;
        }

        private static IEnumerable<Type> GetExplicitlyIncludedTypes(TypeOptionsAttribute options)
        {
            if (options?.IncludeTypes == null)
                yield break;

            foreach (var type in options.IncludeTypes)
            {
                if (type != null && !IsExplicitlyExcluded(type, options))
                    yield return type;
            }
        }

        private static bool IsSelectable(Type type, TypeOptionsAttribute options)
        {
            if (type == null || type.FullName == null || type.FullName.IndexOf('<') >= 0)
                return false;

            if (options != null && !options.AllowInternal && !type.IsVisible)
                return false;

            return options == null || options.MatchesRequirements(type);
        }

        private static bool IsExplicitlyExcluded(Type type, TypeOptionsAttribute options)
        {
            return options.ExcludeTypes != null && Array.IndexOf(options.ExcludeTypes, type) >= 0;
        }
    }
}
