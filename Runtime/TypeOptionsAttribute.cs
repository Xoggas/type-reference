namespace TypeReferences
{
    using System;
    using System.Linq;
    using UnityEngine;

    /// <summary>
    /// Base attribute for constraining which types are selectable in the dropdown of a
    /// <see cref="TypeReference"/> field. Use <see cref="InheritsAttribute"/> to also constrain by base type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class TypeOptionsAttribute : PropertyAttribute
    {
        /// <summary>Shows a "None" entry in the dropdown so the reference can be cleared. Default: <c>true</c>.</summary>
        public bool ShowNoneElement = true;

        /// <summary>Adds extra types to the dropdown that would not otherwise match the filter.</summary>
        public Type[] IncludeTypes;

        /// <summary>Removes specific types from the dropdown.</summary>
        public Type[] ExcludeTypes;

        /// <summary>Displays the short type name instead of the full (namespace-qualified) one. Default: <c>false</c>.</summary>
        public bool ShortName;

        /// <summary>Only shows types that Unity can serialize. Default: <c>false</c>.</summary>
        public bool SerializableOnly;

        /// <summary>Includes non-public types in the dropdown. Default: <c>false</c>.</summary>
        public bool AllowInternal;

        /// <summary>Overrides the dropdown height in pixels (clamped to 100-600).</summary>
        public int DropdownHeight;

        public virtual bool MatchesRequirements(Type type)
        {
            bool passesExcluded = ExcludeTypes == null || !ExcludeTypes.Contains(type);
            bool passesSerializable = !SerializableOnly || IsUnitySerializable(type);
            return passesExcluded && passesSerializable;
        }

        private static bool IsUnitySerializable(Type type)
        {
            return typeof(UnityEngine.Object).IsAssignableFrom(type)
                   || type.IsEnum
                   || Attribute.IsDefined(type, typeof(SerializableAttribute));
        }
    }
}
