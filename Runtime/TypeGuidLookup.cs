#if UNITY_EDITOR
namespace TypeReferences
{
    using System;
    using System.Collections.Generic;
    using UnityEditor;

    /// <summary>
    /// Caches the mapping between a <see cref="Type"/> and the GUID of the script asset that declares it,
    /// so a <see cref="TypeReference"/> can recover from a class rename.
    /// </summary>
    public static class TypeGuidLookup
    {
        private static Dictionary<Type, string> _typeToGuid;
        private static Dictionary<string, Type> _guidToType;

        public static string GetGuidFromType(Type type)
        {
            EnsureCacheBuilt();
            return _typeToGuid.TryGetValue(type, out var guid) ? guid : string.Empty;
        }

        public static Type GetTypeFromGuid(string guid)
        {
            EnsureCacheBuilt();

            if (_guidToType.TryGetValue(guid, out var cachedType))
                return cachedType;

            var path = AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(path))
                return null;

            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            var type = script != null ? script.GetClass() : null;

            if (type != null)
                CacheEntry(type, guid);

            return type;
        }

        private static void EnsureCacheBuilt()
        {
            if (_typeToGuid != null)
                return;

            _typeToGuid = new Dictionary<Type, string>();
            _guidToType = new Dictionary<string, Type>();

            foreach (var guid in AssetDatabase.FindAssets("t:MonoScript"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                var type = script != null ? script.GetClass() : null;

                if (type != null)
                    CacheEntry(type, guid);
            }
        }

        private static void CacheEntry(Type type, string guid)
        {
            _typeToGuid[type] = guid;
            _guidToType[guid] = type;
        }
    }
}
#endif
