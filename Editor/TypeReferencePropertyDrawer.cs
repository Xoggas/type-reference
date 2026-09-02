namespace TypeReferences.Editor
{
    using System.Reflection;
    using UnityEditor;
    using UnityEditor.IMGUI.Controls;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(TypeReference))]
    internal sealed class TypeReferencePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var fieldRect = EditorGUI.PrefixLabel(position, label);

            var typeNameProperty = property.FindPropertyRelative("_typeNameAndAssembly");
            var options = fieldInfo.GetCustomAttribute<TypeOptionsAttribute>();

            string buttonLabel = string.IsNullOrEmpty(typeNameProperty.stringValue)
                ? "None"
                : GetDisplayName(typeNameProperty.stringValue, options != null && options.ShortName);

            if (EditorGUI.DropdownButton(fieldRect, new GUIContent(buttonLabel), FocusType.Keyboard))
                ShowDropdown(fieldRect, property, options);

            EditorGUI.EndProperty();
        }

        private void ShowDropdown(Rect fieldRect, SerializedProperty property, TypeOptionsAttribute options)
        {
            var types = TypeCollector.GetTypes(fieldInfo, options);
            var serializedObject = property.serializedObject;
            var propertyPath = property.propertyPath;

            var dropdown = new TypeSelectionDropdown(new AdvancedDropdownState(), types, options, selectedType =>
            {
                serializedObject.Update();

                var typeReferenceProperty = serializedObject.FindProperty(propertyPath);
                typeReferenceProperty.FindPropertyRelative("_typeNameAndAssembly").stringValue =
                    TypeReference.ToTypeNameAndAssembly(selectedType);
                typeReferenceProperty.FindPropertyRelative("GUID").stringValue = selectedType != null
                    ? TypeGuidLookup.GetGuidFromType(selectedType)
                    : string.Empty;

                serializedObject.ApplyModifiedProperties();
            });

            dropdown.Show(fieldRect);
        }

        private static string GetDisplayName(string typeNameAndAssembly, bool shortName)
        {
            int commaIndex = typeNameAndAssembly.IndexOf(',');
            string fullName = commaIndex >= 0 ? typeNameAndAssembly.Substring(0, commaIndex) : typeNameAndAssembly;

            if (!shortName)
                return fullName;

            int lastDot = fullName.LastIndexOf('.');
            return lastDot >= 0 ? fullName.Substring(lastDot + 1) : fullName;
        }
    }
}
