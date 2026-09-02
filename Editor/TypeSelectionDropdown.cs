namespace TypeReferences.Editor
{
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary>
    /// Flat, searchable type picker. ListView virtualizes its rows, so the amount of created UI does not
    /// grow with the number of available types.
    /// </summary>
    internal sealed class TypeSelectionDropdown : PopupWindowContent
    {
        private const float DefaultWidth = 360f;
        private const float MinimumWidth = 260f;
        private const float PopupChromeHeight = 28f;
        private const float RowHeight = 20f;
        private const float DefaultMaximumHeight = 420f;
        private const float EmptyResultsHeight = 40f;

        private readonly List<Type> _allItems;
        private readonly List<Type> _filteredItems;
        private readonly Action<Type> _onSelected;
        private readonly float _width;
        private readonly float _height;
        private readonly string _currentValue;
        private readonly bool _useShortNames;
        private readonly bool _hasFixedHeight;

        private ListView _listView;
        private Label _emptyLabel;

        public TypeSelectionDropdown(
            IReadOnlyList<Type> types,
            TypeOptionsAttribute options,
            string currentValue,
            float activatorWidth,
            Action<Type> onSelected)
        {
            _allItems = BuildItems(types, options == null || options.ShowNoneElement);
            _filteredItems = new List<Type>(_allItems);
            _onSelected = onSelected;
            _currentValue = currentValue ?? string.Empty;
            _useShortNames = options != null && options.ShortName;
            _width = Mathf.Max(MinimumWidth, Mathf.Max(DefaultWidth, activatorWidth));
            _hasFixedHeight = options != null && options.DropdownHeight > 0;

            _height = _hasFixedHeight
                ? Mathf.Clamp(options.DropdownHeight, 100f, 600f)
                : CalculateContentHeight(_allItems.Count);
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(_width, _height);
        }

        public override void OnOpen()
        {
            BuildUi(editorWindow.rootVisualElement);
        }

        public override void OnGUI(Rect rect)
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                editorWindow.Close();
                Event.current.Use();
            }
        }

        private void BuildUi(VisualElement root)
        {
            root.style.flexDirection = FlexDirection.Column;

            var search = new ToolbarSearchField
            {
                name = "type-search"
            };
            search.style.flexShrink = 0;
            search.style.marginLeft = 4;
            search.style.marginRight = 4;
            search.style.marginTop = 4;
            search.style.marginBottom = 4;
            search.RegisterValueChangedCallback(evt => ApplyFilter(evt.newValue));
            search.RegisterCallback<KeyDownEvent>(OnSearchKeyDown);
            root.Add(search);

            _listView = new ListView
            {
                name = "type-list",
                itemsSource = _filteredItems,
                fixedItemHeight = RowHeight,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                selectionType = SelectionType.Single,
                makeItem = MakeItem,
                bindItem = BindItem
            };
            _listView.style.flexGrow = 1;
            _listView.style.minHeight = 0;
            _listView.itemsChosen += ChooseFirst;
            root.Add(_listView);

            _emptyLabel = new Label("No types found");
            _emptyLabel.style.display = _filteredItems.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _emptyLabel.style.flexGrow = 1;
            _emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            root.Add(_emptyLabel);

            root.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode != KeyCode.Escape)
                    return;

                evt.StopImmediatePropagation();
                editorWindow.Close();
            }, TrickleDown.TrickleDown);

            SelectCurrentItem();
            search.schedule.Execute(search.Focus);
        }

        private VisualElement MakeItem()
        {
            var label = new Label();
            label.style.flexGrow = 1;
            label.style.paddingLeft = 4;
            label.style.paddingRight = 4;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.overflow = Overflow.Hidden;
            label.style.textOverflow = TextOverflow.Ellipsis;
            label.style.whiteSpace = WhiteSpace.NoWrap;
            label.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.button == 0)
                    Select(label.userData as Type);
            });
            return label;
        }

        private void BindItem(VisualElement element, int index)
        {
            var type = _filteredItems[index];
            var label = (Label)element;
            label.text = GetDisplayName(type);
            label.tooltip = type?.FullName ?? "None";
            label.userData = type;
        }

        private void ApplyFilter(string query)
        {
            _filteredItems.Clear();

            if (string.IsNullOrWhiteSpace(query))
            {
                _filteredItems.AddRange(_allItems);
            }
            else
            {
                string trimmedQuery = query.Trim();

                foreach (var type in _allItems)
                {
                    string searchName = type?.FullName ?? "None";

                    if (searchName.IndexOf(trimmedQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                        _filteredItems.Add(type);
                }
            }

            _listView.Rebuild();
            _emptyLabel.style.display = _filteredItems.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _listView.style.display = _filteredItems.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;

            if (_filteredItems.Count > 0)
                _listView.SetSelectionWithoutNotify(new[] { 0 });

            ResizeToContent();
        }

        private void OnSearchKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.DownArrow && _filteredItems.Count > 0)
            {
                evt.StopImmediatePropagation();
                _listView.SetSelectionWithoutNotify(new[] { 0 });
                _listView.Focus();
                return;
            }

            if ((evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) &&
                _filteredItems.Count > 0)
            {
                evt.StopImmediatePropagation();
                Select(_filteredItems[0]);
            }
        }

        private void SelectCurrentItem()
        {
            int index = FindCurrentItemIndex();
            if (index < 0)
                return;

            _listView.SetSelectionWithoutNotify(new[] { index });
            _listView.schedule.Execute(() => _listView.ScrollToItem(index));
        }

        private void ChooseFirst(IEnumerable<object> selectedItems)
        {
            foreach (var selectedItem in selectedItems)
            {
                Select(selectedItem as Type);
                return;
            }
        }

        private void Select(Type type)
        {
            _onSelected?.Invoke(type);
            editorWindow.Close();
        }

        private int FindCurrentItemIndex()
        {
            if (string.IsNullOrEmpty(_currentValue))
                return _allItems.IndexOf(null);

            int commaIndex = _currentValue.IndexOf(',');
            string typeName = commaIndex < 0
                ? _currentValue
                : _currentValue.Substring(0, commaIndex);
            string assemblyName = commaIndex < 0
                ? string.Empty
                : _currentValue.Substring(commaIndex + 1).Trim();

            return _allItems.FindIndex(type =>
                type != null &&
                type.FullName == typeName &&
                (assemblyName.Length == 0 || type.Assembly.GetName().Name == assemblyName));
        }

        private string GetDisplayName(Type type)
        {
            if (type == null)
                return "None";

            return _useShortNames ? type.Name : type.FullName ?? type.Name;
        }

        private void ResizeToContent()
        {
            if (_hasFixedHeight || editorWindow == null)
                return;

            float height = CalculateContentHeight(_filteredItems.Count);
            var position = editorWindow.position;

            if (Mathf.Approximately(position.height, height))
                return;

            position.height = height;
            editorWindow.position = position;
        }

        private static float CalculateContentHeight(int itemCount)
        {
            float listHeight = itemCount == 0
                ? EmptyResultsHeight
                : itemCount * RowHeight;

            return Mathf.Min(PopupChromeHeight + listHeight, DefaultMaximumHeight);
        }

        private static List<Type> BuildItems(IReadOnlyList<Type> types, bool includeNone)
        {
            var items = new List<Type>(types.Count + (includeNone ? 1 : 0));

            if (includeNone)
                items.Add(null);

            items.AddRange(types);

            return items;
        }
    }
}
