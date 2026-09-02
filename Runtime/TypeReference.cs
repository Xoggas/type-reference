namespace TypeReferences
{
    using System;
    using UnityEngine;

    /// <summary>
    /// A serializable reference to a <see cref="System.Type"/> that can be assigned in the Unity Inspector.
    /// </summary>
    [Serializable]
    public sealed class TypeReference : ISerializationCallbackReceiver, IEquatable<TypeReference>
    {
        [SerializeField] private string _typeNameAndAssembly = string.Empty;

        // Casing kept for backwards compatibility with data serialized by SolidAlloy's TypeReferences package.
        [SerializeField] private string GUID = string.Empty;

        [SerializeField] private bool _suppressLogs;

        [NonSerialized] private Type _type;
        [NonSerialized] private bool _resolutionFailed;

        public TypeReference()
        {
        }

        public TypeReference(bool suppressLogs)
        {
            _suppressLogs = suppressLogs;
        }

        public TypeReference(Type type, bool suppressLogs = false)
        {
            _suppressLogs = suppressLogs;
            Type = type;
        }

        public TypeReference(string assemblyQualifiedTypeName, bool suppressLogs = false)
        {
            _suppressLogs = suppressLogs;
            Type = string.IsNullOrEmpty(assemblyQualifiedTypeName) ? null : Type.GetType(assemblyQualifiedTypeName);
        }

        /// <summary>The raw "FullName, AssemblyName" string this reference is stored as.</summary>
        public string TypeNameAndAssembly => _typeNameAndAssembly;

        /// <summary>Gets or sets the referenced type.</summary>
        public Type Type
        {
            get
            {
                if (_type == null && !_resolutionFailed && !string.IsNullOrEmpty(_typeNameAndAssembly))
                    _type = ResolveType();

                return _type;
            }
            set
            {
                if (value != null && value.FullName == null)
                    throw new ArgumentException($"'{value}' does not have a full name and cannot be referenced.", nameof(value));

                _type = value;
                _resolutionFailed = false;
                _typeNameAndAssembly = ToTypeNameAndAssembly(value);

#if UNITY_EDITOR
                GUID = value != null ? TypeGuidLookup.GetGuidFromType(value) : string.Empty;
#endif
            }
        }

        public static implicit operator Type(TypeReference typeReference) => typeReference?.Type;

        public static implicit operator TypeReference(Type type) => type == null ? null : new TypeReference(type);

        public override string ToString() => Type?.FullName ?? "None";

        public bool Equals(TypeReference other) => other != null && Type == other.Type;

        public override bool Equals(object obj) => obj is TypeReference other && Equals(other);

        public override int GetHashCode() => Type?.GetHashCode() ?? 0;

        public static string ToTypeNameAndAssembly(Type type)
        {
            if (type == null)
                return string.Empty;

            if (type.FullName == null)
                throw new ArgumentException($"'{type}' does not have a full name and cannot be referenced.", nameof(type));

            return $"{type.FullName}, {type.Assembly.GetName().Name}";
        }

        private Type ResolveType()
        {
            var type = Type.GetType(_typeNameAndAssembly);

#if UNITY_EDITOR
            if (type == null && !string.IsNullOrEmpty(GUID))
            {
                type = TypeGuidLookup.GetTypeFromGuid(GUID);

                if (type != null)
                    _typeNameAndAssembly = ToTypeNameAndAssembly(type);
            }
#endif

            if (type == null)
            {
                _resolutionFailed = true;

                if (!_suppressLogs)
                {
                    Debug.LogWarning(
                        $"[TypeReference] Type '{_typeNameAndAssembly}' could not be resolved. " +
                        "It may have been renamed, moved, or deleted.");
                }
            }

            return type;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            _type = null;
            _resolutionFailed = false;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }
    }
}
