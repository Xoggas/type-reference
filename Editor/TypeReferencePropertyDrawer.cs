namespace TypeReferences.Editor
{
    using System.Reflection;
    using UnityEditor;
    using UnityEditor.IMGUI.Controls;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    [CustomPropertyDrawer(typeof(TypeReference))]
    internal sealed class TypeReferencePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var options = fieldInfo.GetCustomAttribute<TypeOptionsAttribute>();

            var root = new VisualElement { name = "type-reference-field" };
            root.AddToClassList(BaseField<string>.ussClassName);
            root.AddToClassList(PopupField<string>.ussClassName);
            root.style.flexDirection = FlexDirection.Row;
            root.style.alignItems = Align.Center;
            root.style.minHeight = 18;

            var label = new Label(property.displayName);
            label.AddToClassList(BaseField<string>.labelUssClassName);
            label.style.overflow = Overflow.Hidden;
            label.style.textOverflow = TextOverflow.Ellipsis;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.flexShrink = 0;
            label.style.width = Length.Percent(40);
            root.Add(label);

            var button = new Button { text = "None" };
            button.AddToClassList(BaseField<string>.inputUssClassName);
            button.AddToClassList(PopupField<string>.inputUssClassName);
            button.style.flexGrow = 1;
            button.style.flexShrink = 1;
            button.style.marginLeft = 0;
            button.style.unityTextAlign = TextAnchor.MiddleLeft;
            button.style.overflow = Overflow.Hidden;
            button.style.whiteSpace = WhiteSpace.NoWrap;
            button.style.textOverflow = TextOverflow.Ellipsis;
            root.Add(button);

            void RefreshLabel(SerializedProperty prop)
            {
                string typeName = prop.FindPropertyRelative("_typeNameAndAssembly").stringValue;
                button.text = string.IsNullOrEmpty(typeName)
                    ? "None"
                    : GetDisplayName(typeName, options != null && options.ShortName);
            }

            RefreshLabel(property);
            root.TrackPropertyValue(property, RefreshLabel);

            button.clicked += () => ShowDropdown(button.worldBound, property, options);

            return root;
        }

        private void ShowDropdown(Rect activatorRect, SerializedProperty property, TypeOptionsAttribute options)
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

            dropdown.Show(activatorRect);
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
