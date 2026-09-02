namespace TypeReferences.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using UnityEditor.IMGUI.Controls;
    using UnityEngine;

    /// <summary>A searchable, namespace-grouped dropdown used to pick a <see cref="Type"/> in the Inspector.</summary>
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
                : 300;

            minimumSize = new Vector2(minimumSize.x, height);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Select Type");

            if (_options == null || _options.ShowNoneElement)
                root.AddChild(new TypeDropdownItem(null, NoneLabel));

            var namespaceNodes = new Dictionary<string, AdvancedDropdownItem>();
            bool shortName = _options != null && _options.ShortName;

            foreach (var type in _types)
            {
                var parent = GetOrCreateNamespaceNode(root, namespaceNodes, type.Namespace);
                string displayName = shortName ? type.Name : type.FullName;
                parent.AddChild(new TypeDropdownItem(type, displayName));
            }

            return root;
        }

        private static AdvancedDropdownItem GetOrCreateNamespaceNode(
            AdvancedDropdownItem root,
            Dictionary<string, AdvancedDropdownItem> cache,
            string @namespace)
        {
            if (string.IsNullOrEmpty(@namespace))
                return root;

            if (cache.TryGetValue(@namespace, out var existingNode))
                return existingNode;

            var segments = @namespace.Split('.');
            var parent = root;
            var pathBuilder = new StringBuilder();

            foreach (var segment in segments)
            {
                if (pathBuilder.Length > 0)
                    pathBuilder.Append('.');

                pathBuilder.Append(segment);
                string path = pathBuilder.ToString();

                if (!cache.TryGetValue(path, out var node))
                {
                    node = new AdvancedDropdownItem(segment);
                    parent.AddChild(node);
                    cache[path] = node;
                }

                parent = node;
            }

            return parent;
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
