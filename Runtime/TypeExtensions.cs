namespace TypeReferences
{
    using System;

    internal static class TypeExtensions
    {
        /// <summary>Whether <paramref name="type"/> is a proper subtype of (or implements) <paramref name="baseType"/>.</summary>
        public static bool InheritsFrom(Type type, Type baseType)
        {
            return type != baseType && baseType.IsAssignableFrom(type);
        }
    }
}
