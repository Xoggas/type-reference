namespace TypeReferences.Editor
{
    using System;
    using System.Collections.Generic;
    using UnityEditor.IMGUI.Controls;
    using UnityEngine;

    /// <summary>A searchable, flat dropdown used to pick a <see cref="Type"/> in the Inspector.</summary>
    internal sealed class TypeSelectionDropdown : AdvancedDropdown
    {
        private const string NoneLabel = "None";

        private readonly List<Type> _types;
        private readonly TypeOptionsAttribute _options;
        private readonly Action<Type> _onTypeSelected;

        public TypeSelectionDropdown(
            AdvancedDropdownState state,
            List<Type> types,
            TypeOptionsAttribute options,
            Action<Type> onTypeSelected)
            : base(state)
        {
            _types = types;
            _options = options;
            _onTypeSelected = onTypeSelected;

            int height = options != null && options.DropdownHeight > 0
                ? Mathf.Clamp(options.DropdownHeight, 100, 600)
                : Mathf.Clamp(30 + types.Count * 20, 100, 400);

            minimumSize = new Vector2(Mathf.Max(minimumSize.x, 250), height);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Select Type");

            if (_options == null || _options.ShowNoneElement)
                root.AddChild(new TypeDropdownItem(null, NoneLabel));

            bool shortName = _options != null && _options.ShortName;

            foreach (var type in _types)
            {
                string displayName = shortName ? type.Name : type.FullName;
                root.AddChild(new TypeDropdownItem(type, displayName));
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is TypeDropdownItem typeItem)
                _onTypeSelected?.Invoke(typeItem.Type);
        }

        private sealed class TypeDropdownItem : AdvancedDropdownItem
        {
            public readonly Type Type;

            public TypeDropdownItem(Type type, string name) : base(name)
            {
                Type = type;
            }
        }
    }
}
