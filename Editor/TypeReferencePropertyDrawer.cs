namespace TypeReferences.Editor
{
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    [CustomPropertyDrawer(typeof(TypeReference))]
    internal sealed class TypeReferencePropertyDrawer : PropertyDrawer
    {
        private const string TypeNamePropertyName = "_typeNameAndAssembly";
        private const string GuidPropertyName = "GUID";
        private const string NoneLabel = "None";
        private const string MixedValueLabel = "\u2014";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var options = GetOptions();
            var valueProperty = property.FindPropertyRelative(TypeNamePropertyName);
            var buttonRect = EditorGUI.PrefixLabel(position, label);
            var content = new GUIContent(GetDisplayValue(valueProperty, options));

            if (EditorGUI.DropdownButton(buttonRect, content, FocusType.Keyboard))
                OpenDropdown(buttonRect, property, options);

            EditorGUI.EndProperty();
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var options = GetOptions();
            var choices = new List<string>(1);
            var field = new DropdownField(property.displayName, choices, 0)
            {
                name = "type-reference-field",
                tooltip = property.tooltip
            };

            // A real BaseField lets the Inspector align this exactly like its native fields.
            field.AddToClassList(BaseField<string>.alignedFieldUssClassName);

            void Refresh(SerializedProperty trackedProperty)
            {
                var valueProperty = trackedProperty.FindPropertyRelative(TypeNamePropertyName);
                string displayValue = GetDisplayValue(valueProperty, options);

                choices.Clear();
                choices.Add(displayValue);
                field.SetValueWithoutNotify(displayValue);
            }

            Refresh(property);
            field.TrackPropertyValue(property, Refresh);

            field.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0)
                    return;

                evt.StopImmediatePropagation();
                OpenDropdown(field.worldBound, property, options);
            }, TrickleDown.TrickleDown);

            field.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode != KeyCode.Space &&
                    evt.keyCode != KeyCode.Return &&
                    evt.keyCode != KeyCode.KeypadEnter)
                {
                    return;
                }

                evt.StopImmediatePropagation();
                OpenDropdown(field.worldBound, property, options);
            }, TrickleDown.TrickleDown);

            return field;
        }

        private TypeOptionsAttribute GetOptions()
        {
            return fieldInfo?.GetCustomAttribute<TypeOptionsAttribute>();
        }

        private void OpenDropdown(Rect activatorRect, SerializedProperty property, TypeOptionsAttribute options)
        {
            var serializedObject = property.serializedObject;
            string propertyPath = property.propertyPath;
            string currentValue = property.FindPropertyRelative(TypeNamePropertyName).stringValue;
            var types = TypeCollector.GetTypes(fieldInfo, options);

            var dropdown = new TypeSelectionDropdown(
                types,
                options,
                currentValue,
                activatorRect.width,
                selectedType => ApplySelection(serializedObject, propertyPath, selectedType));

            UnityEditor.PopupWindow.Show(activatorRect, dropdown);
        }

        private static void ApplySelection(
            SerializedObject serializedObject,
            string propertyPath,
            System.Type selectedType)
        {
            if (serializedObject == null || serializedObject.targetObject == null)
                return;

            serializedObject.Update();

            var property = serializedObject.FindProperty(propertyPath);
            if (property == null)
                return;

            property.FindPropertyRelative(TypeNamePropertyName).stringValue =
                TypeReference.ToTypeNameAndAssembly(selectedType);
            property.FindPropertyRelative(GuidPropertyName).stringValue =
                selectedType == null ? string.Empty : TypeGuidLookup.GetGuidFromType(selectedType);

            serializedObject.ApplyModifiedProperties();
        }

        private static string GetDisplayValue(
            SerializedProperty valueProperty,
            TypeOptionsAttribute options)
        {
            if (valueProperty.hasMultipleDifferentValues)
                return MixedValueLabel;

            string value = valueProperty.stringValue;
            if (string.IsNullOrEmpty(value))
                return NoneLabel;

            int commaIndex = value.IndexOf(',');
            string fullName = commaIndex < 0 ? value : value.Substring(0, commaIndex);

            if (options == null || !options.ShortName)
                return fullName;

            int separatorIndex = fullName.LastIndexOf('.');
            return separatorIndex < 0 ? fullName : fullName.Substring(separatorIndex + 1);
        }
    }
}
