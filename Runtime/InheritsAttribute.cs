namespace TypeReferences
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Constrains a <see cref="TypeReference"/> field's dropdown to types that inherit a base class
    /// and/or implement one or more interfaces.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class InheritsAttribute : TypeOptionsAttribute
    {
        private readonly Type[] _baseTypes;

        /// <param name="baseType">Type that selectable types must inherit from or implement.</param>
        /// <param name="additionalBaseTypes">Additional types selectable types must also inherit/implement.</param>
        public InheritsAttribute(Type baseType, params Type[] additionalBaseTypes)
        {
            if (baseType == null)
                throw new ArgumentNullException(nameof(baseType));

            if (additionalBaseTypes == null || additionalBaseTypes.Length == 0)
            {
                _baseTypes = new[] { baseType };
            }
            else
            {
                _baseTypes = new Type[additionalBaseTypes.Length + 1];
                _baseTypes[0] = baseType;
                additionalBaseTypes.CopyTo(_baseTypes, 1);
            }
        }

        /// <summary>Allows the base type itself to be selected from the dropdown. Default: <c>false</c>.</summary>
        public bool IncludeBaseType { get; set; }

        /// <summary>Allows abstract classes and interfaces to be selected from the dropdown. Default: <c>false</c>.</summary>
        public bool AllowAbstract { get; set; }

        /// <summary>Gets the base types used to filter the dropdown.</summary>
        public IReadOnlyList<Type> BaseTypes => _baseTypes;

        public override bool MatchesRequirements(Type type)
        {
            bool isBaseType = Array.IndexOf(_baseTypes, type) >= 0;

            if (isBaseType)
                return IncludeBaseType && base.MatchesRequirements(type);

            bool passesAbstractConstraint = AllowAbstract || !type.IsAbstract;
            bool inheritsAllBaseTypes = _baseTypes.All(baseType => TypeExtensions.InheritsFrom(type, baseType));

            return passesAbstractConstraint && inheritsAllBaseTypes && base.MatchesRequirements(type);
        }
    }
}
