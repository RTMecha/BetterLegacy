using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data
{
    #region Base

    /// <summary>
    /// Represents an element in the editor.
    /// </summary>
    public abstract class EditorElement : Exists
    {
        public EditorElement() { }

        public EditorElement(string tooltipGroup, Func<bool> shouldGenerate)
        {
            this.tooltipGroup = tooltipGroup;
            this.shouldGenerate = shouldGenerate;
        }

        /// <summary>
        /// Tooltip group of the editor element.
        /// </summary>
        public string tooltipGroup;

        /// <summary>
        /// Function to run when checking if the editor element should generate.
        /// </summary>
        readonly Func<bool> shouldGenerate;

        /// <summary>
        /// If the editor element should generate.
        /// </summary>
        public bool ShouldGenerate => shouldGenerate == null || shouldGenerate.Invoke();

        /// <summary>
        /// Game object of the editor element.
        /// </summary>
        public GameObject GameObject { get; set; }

        /// <summary>
        /// Local sibling index separate from <see cref="InitSettings"/>. Use if the init settings is used for several items.
        /// </summary>
        public int siblingIndex = -1;

        /// <summary>
        /// Labels element reference.
        /// </summary>
        public EditorElement labelsElement;

        /// <summary>
        /// Sub-elements inside of the element.
        /// </summary>
        public List<EditorElement> subElements = new List<EditorElement>();

        /// <summary>
        /// The elements' relative init settings.
        /// </summary>
        public InitSettings? initSettings;

        /// <summary>
        /// Sets the active state of the editor element.
        /// </summary>
        /// <param name="state">Active state to set.</param>
        public void SetActive(bool state)
        {
            if (GameObject)
                GameObject.SetActive(state);
            labelsElement?.SetActive(state);
        }

        /// <summary>
        /// Gets the prefab for the editor element to spawn.
        /// </summary>
        /// <param name="initSettings">Initialize settings.</param>
        /// <returns>Returns the game object to instantiate.</returns>
        public virtual GameObject GetPrefab(InitSettings initSettings) => initSettings.prefab;

        /// <summary>
        /// Gets the sibling index of the editor element.
        /// </summary>
        /// <param name="initSettings">Initialize settings.</param>
        /// <returns>Returns the sibling index the editor element should use.</returns>
        public virtual int GetSiblingIndex(InitSettings initSettings) => siblingIndex >= 0 ? siblingIndex : initSettings.siblingIndex;

        /// <summary>
        /// Initializes the editor element.
        /// </summary>
        /// <param name="initSettings">Initialize settings.</param>
        public virtual void Init(InitSettings initSettings)
        {
            if (this.initSettings.HasValue)
                initSettings = this.initSettings.Value;

            if (!initSettings.parent)
                return;

            CoreHelper.Delete(GameObject);
            GameObject = GetPrefab(initSettings).Duplicate(initSettings.parent, !string.IsNullOrEmpty(initSettings.name) ? initSettings.name : "element", GetSiblingIndex(initSettings));
            Apply(GameObject, initSettings);
        }

        /// <summary>
        /// Initializes all sub elements.
        /// </summary>
        public virtual void InitSubElements() => InitSubElements(InitSettings.Default);

        /// <summary>
        /// Initializes all sub elements.
        /// </summary>
        /// <param name="initSettings">Initialize settings.</param>
        public virtual void InitSubElements(InitSettings initSettings)
        {
            initSettings = initSettings.Parent(GameObject.transform);
            for (int i = 0; i < subElements.Count; i++)
                subElements[i].Init(initSettings);
        }

        /// <summary>
        /// Initializes the tooltip group.
        /// </summary>
        /// <param name="gameObject">Game object to set the tooltip to.</param>
        public void InitTooltip()
        {
            if (!string.IsNullOrEmpty(tooltipGroup) && GameObject)
                TooltipHelper.AssignTooltip(GameObject, tooltipGroup);
        }

        public abstract void Apply(GameObject gameObject, InitSettings initSettings);

        /// <summary>
        /// Initialize settings for <see cref="EditorElement"/>.
        /// </summary>
        public struct InitSettings
        {
            public static InitSettings Default => new InitSettings()
            {
                parent = null,
                name = string.Empty,
                siblingIndex = -1,
                applyThemes = true,
            };

            /// <summary>
            /// Prefab to spawn.
            /// </summary>
            public GameObject prefab;

            /// <summary>
            /// Parent of the editor element.
            /// </summary>
            public Transform parent;

            /// <summary>
            /// On click function of the editor element.
            /// </summary>
            public Action<PointerEventData> onClick;

            /// <summary>
            /// Name of the editor element.
            /// </summary>
            public string name;

            /// <summary>
            /// Sibling index of the editor element.
            /// </summary>
            public int siblingIndex;

            /// <summary>
            /// If editor themes should be applied to the editor element.
            /// </summary>
            public bool applyThemes;

            /// <summary>
            /// RectTransform values of the editor element.
            /// </summary>
            public RectValues? rectValues;

            /// <summary>
            /// Sets the prefab of the editor element.
            /// </summary>
            /// <param name="prefab">Prefab to set.</param>
            /// <returns>Returns the current <see cref="InitSettings"/></returns>
            public InitSettings Prefab(GameObject prefab)
            {
                this.prefab = prefab;
                return this;
            }

            /// <summary>
            /// Sets the parent of the editor element.
            /// </summary>
            /// <param name="parent">Parent to set.</param>
            /// <returns>Returns the current <see cref="InitSettings"/></returns>
            public InitSettings Parent(Transform parent)
            {
                this.parent = parent;
                return this;
            }

            /// <summary>
            /// Sets the on click function of the editor element.
            /// </summary>
            /// <param name="onClick">Function to set.</param>
            /// <returns>Returns the current <see cref="InitSettings"/></returns>
            public InitSettings OnClick(Action<PointerEventData> onClick)
            {
                this.onClick = onClick;
                return this;
            }

            /// <summary>
            /// Sets the name of the editor element.
            /// </summary>
            /// <param name="name">Name to set.</param>
            /// <returns>Returns the current <see cref="InitSettings"/></returns>
            public InitSettings Name(string name)
            {
                this.name = name;
                return this;
            }

            /// <summary>
            /// Sets the sibling index of the editor element.
            /// </summary>
            /// <param name="siblingIndex">Sibling index to set.</param>
            /// <returns>Returns the current <see cref="InitSettings"/></returns>
            public InitSettings SiblingIndex(int siblingIndex)
            {
                this.siblingIndex = siblingIndex;
                return this;
            }

            /// <summary>
            /// Sets the apply themes of the editor element.
            /// </summary>
            /// <param name="applyThemes">Apply themes to set.</param>
            /// <returns>Returns the current <see cref="InitSettings"/></returns>
            public InitSettings ApplyThemes(bool applyThemes)
            {
                this.applyThemes = applyThemes;
                return this;
            }

            /// <summary>
            /// Sets the rect of the editor element.
            /// </summary>
            /// <param name="rectValues">Rect values to set.</param>
            /// <returns>Returns the current <see cref="InitSettings"/></returns>
            public InitSettings Rect(RectValues rectValues)
            {
                this.rectValues = rectValues;
                return this;
            }
        }
    }

    /// <summary>
    /// Represents an element in the editor.
    /// </summary>
    /// <typeparam name="T">Type of the component the element represents.</typeparam>
    public abstract class EditorElement<T> : EditorElement where T : Component
    {
        public EditorElement() { }

        public EditorElement(string tooltipGroup, Func<bool> shouldGenerate) : base(tooltipGroup, shouldGenerate) { }

        public override void Apply(GameObject gameObject, InitSettings initSettings) => Apply(gameObject.GetComponent<T>(), initSettings);

        public abstract void Apply(T component, InitSettings initSettings);
    }

    /// <summary>
    /// Represents a group of elements in the editor.
    /// </summary>
    public class EditorElementGroup : Exists
    {
        public EditorElementGroup(Func<bool> shouldGenerate) => this.shouldGenerate = shouldGenerate;

        public EditorElementGroup(Func<bool> shouldGenerate, EditorElement editorElement) : this(shouldGenerate, new List<EditorElement> { editorElement }) { }

        public EditorElementGroup(Func<bool> shouldGenerate, List<EditorElement> elements)
        {
            this.shouldGenerate = shouldGenerate;
            this.elements = elements;
        }

        public EditorElementGroup(Func<bool> shouldGenerate, params EditorElement[] elements)
        {
            this.shouldGenerate = shouldGenerate;
            this.elements = elements.ToList();
        }

        public EditorElementGroup(Func<bool> shouldGenerate, Func<List<EditorElement>> getElements)
        {
            this.shouldGenerate = shouldGenerate;
            this.getElements = getElements;
        }

        public EditorElementGroup() : this(null, new List<EditorElement>()) { }

        public List<EditorElement> elements;

        public Func<List<EditorElement>> getElements;

        List<EditorElement> cachedElements;

        public List<EditorElement> Elements
        {
            get
            {
                if (getElements != null)
                {
                    cachedElements = getElements.Invoke();
                    return cachedElements;
                }
                return elements;
            }
        }

        /// <summary>
        /// Sets the active state of the editor elements in the group.
        /// </summary>
        /// <param name="state">Active state to set.</param>
        public void SetActive(bool state)
        {
            if (cachedElements != null)
                for (int i = 0; i < cachedElements.Count; i++)
                    cachedElements[i].SetActive(state);
            if (elements != null)
                for (int i = 0; i < elements.Count; i++)
                    elements[i].SetActive(state);
        }

        /// <summary>
        /// Function to run when checking if the editor element group should generate.
        /// </summary>
        readonly Func<bool> shouldGenerate;

        /// <summary>
        /// If the editor element group should generate.
        /// </summary>
        public bool ShouldGenerate => shouldGenerate == null || shouldGenerate.Invoke();
    }

    #endregion

    /// <summary>
    /// Represents a layout group element in the editor.
    /// </summary>
    public class LayoutGroupElement : EditorElement
    {
        public LayoutGroupElement(LayoutValues layoutValues, params EditorElement[] editorElements)
        {
            this.layoutValues = layoutValues;
            subElements = editorElements.ToList();
        }

        #region Values

        public Vector2 size = new Vector2(0f, 32f);

        public LayoutValues layoutValues;
        public LayoutGroup layoutGroup;
        GridLayoutGroup grid;
        VerticalLayoutGroup vertical;
        HorizontalLayoutGroup horizontal;

        #endregion

        #region Functions

        public override void Init(InitSettings initSettings)
        {
            if (this.initSettings.HasValue)
                initSettings = this.initSettings.Value;

            if (!initSettings.parent)
                return;

            CoreHelper.Delete(GameObject);
            GameObject = initSettings.prefab ?
                initSettings.prefab.Duplicate(initSettings.parent, !string.IsNullOrEmpty(initSettings.name) ? initSettings.name : "layout element", GetSiblingIndex(initSettings)) :
                Creator.NewUIObject(!string.IsNullOrEmpty(initSettings.name) ? initSettings.name : "layout element", initSettings.parent, GetSiblingIndex(initSettings));

            switch (layoutValues.type)
            {
                case LayoutValues.Type.Grid: {
                        grid = GameObject.AddComponent<GridLayoutGroup>();
                        layoutGroup = grid;
                        break;
                    }
                case LayoutValues.Type.Vertical: {
                        vertical = GameObject.AddComponent<VerticalLayoutGroup>();
                        layoutGroup = vertical;
                        break;
                    }
                case LayoutValues.Type.Horizontal: {
                        horizontal = GameObject.AddComponent<HorizontalLayoutGroup>();
                        layoutGroup = horizontal;
                        break;
                    }
            }

            Apply(GameObject, initSettings);
            InitSubElements(InitSettings.Default.OnClick(initSettings.onClick));
        }

        public override void Apply(GameObject gameObject, InitSettings initSettings)
        {
            GameObject.transform.AsRT().sizeDelta = size;

            if (grid && layoutValues is GridLayoutValues gridLayoutValues)
                gridLayoutValues.AssignToLayout(grid);
            if (layoutValues is HorizontalOrVerticalLayoutValues verticalLayoutValues)
                verticalLayoutValues.AssignToLayout(vertical);
            if (layoutValues is HorizontalOrVerticalLayoutValues horizontalLayoutValues)
                horizontalLayoutValues.AssignToLayout(horizontal);
        }

        #endregion
    }

    /// <summary>
    /// Represents a scroll view element in the editor.
    /// </summary>
    public class ScrollViewElement : EditorElement<ScrollRect>
    {
        public ScrollViewElement(Direction direction)
        {
            horizontal = direction == Direction.Horizontal;
            vertical = direction == Direction.Vertical;
        }

        #region Values

        public RectTransform Content { get; set; }

        public bool horizontal;

        public bool vertical;

        #endregion

        #region Functions

        public override GameObject GetPrefab(InitSettings initSettings) => initSettings.prefab ? initSettings.prefab : EditorPrefabHolder.Instance.ScrollView;

        public override void Apply(ScrollRect component, InitSettings initSettings)
        {
            component.horizontal = horizontal;
            component.vertical = vertical;
            Content = component.transform.Find("Viewport/Content").AsRT();

            if (initSettings.rectValues.HasValue)
                initSettings.rectValues.Value.AssignToRectTransform(component.transform.AsRT());

            if (!initSettings.applyThemes)
                return;

            EditorThemeManager.ApplyScrollbar(component.verticalScrollbar);
        }

        #endregion

        public enum Direction
        {
            Horizontal,
            Vertical,
        }
    }

    /// <summary>
    /// Represents a spacer element in the editor.
    /// </summary>
    public class SpacerElement : EditorElement<Image>
    {
        public SpacerElement() { }

        #region Values

        public Vector2 size = new Vector2(0f, 4f);

        public ThemeGroup themeGroup = ThemeGroup.Background_3;

        #endregion

        #region Functions

        public override void Init(InitSettings initSettings)
        {
            if (this.initSettings.HasValue)
                initSettings = this.initSettings.Value;

            if (!initSettings.parent)
                return;

            CoreHelper.Delete(GameObject);
            GameObject = initSettings.prefab ?
                initSettings.prefab.Duplicate(initSettings.parent, !string.IsNullOrEmpty(initSettings.name) ? initSettings.name : "spacer element", GetSiblingIndex(initSettings)) :
                Creator.NewUIObject(!string.IsNullOrEmpty(initSettings.name) ? initSettings.name : "spacer element", initSettings.parent, GetSiblingIndex(initSettings));
            Apply(initSettings.prefab ? GameObject.GetOrAddComponent<Image>() : GameObject.AddComponent<Image>(), initSettings);
        }

        public override void Apply(Image component, InitSettings initSettings)
        {
            if (initSettings.rectValues.HasValue)
                initSettings.rectValues.Value.AssignToRectTransform(GameObject.transform.AsRT());
            else
                component.rectTransform.sizeDelta = size;
            if (initSettings.applyThemes)
                EditorThemeManager.ApplyGraphic(component, themeGroup);
        }

        #endregion
    }

    /// <summary>
    /// Represents a button element in the editor.
    /// </summary>
    public class ButtonElement : EditorElement<Button>
    {
        #region Constructors

        public ButtonElement(string name, Action action, string tooltipGroup = null, Func<bool> shouldGenerate = null) : base(tooltipGroup, shouldGenerate)
        {
            this.name = name;
            this.action = action;
        }

        public ButtonElement(string name, Action<PointerEventData> onClick, string tooltipGroup = null, Func<bool> shouldGenerate = null) : base(tooltipGroup, shouldGenerate)
        {
            this.name = name;
            this.onClick = onClick;
        }
        
        public ButtonElement(Func<string> getName, Action action, string tooltipGroup = null, Func<bool> shouldGenerate = null) : base(tooltipGroup, shouldGenerate)
        {
            this.getName = getName;
            this.action = action;
        }

        public ButtonElement(Func<string> getName, Action<PointerEventData> onClick, string tooltipGroup = null, Func<bool> shouldGenerate = null) : base(tooltipGroup, shouldGenerate)
        {
            this.getName = getName;
            this.onClick = onClick;
        }

        #endregion

        #region Values

        /// <summary>
        /// Name of the button.
        /// </summary>
        public string name;
        /// <summary>
        /// Function to run on click.
        /// </summary>
        public Action action;
        /// <summary>
        /// Function to run on click.
        /// </summary>
        public Action<PointerEventData> onClick;
        /// <summary>
        /// Name to get per initialization.
        /// </summary>
        public Func<string> getName;
        /// <summary>
        /// Name to display on the button.
        /// </summary>
        public string Name => getName != null ? getName.Invoke() : name;

        public Sprite sprite;

        /// <summary>
        /// The type of the button.<br></br>
        /// 0 = non-highlighting or <see cref="EditorPrefabHolder.Function1Button"/><br></br>
        /// 1 = highlighting or <see cref="EditorPrefabHolder.Function2Button"/>
        /// </summary>
        public Type buttonType = Type.Label2;

        public enum Type
        {
            Label1,
            Label2,
            Icon,
            Sprite,
        }

        public ThemeGroup buttonThemeGroup = ThemeGroup.Function_2;
        public ThemeGroup graphicThemeGroup = ThemeGroup.Function_2_Text;

        public ButtonAdapter buttonAdapter;

        #endregion

        #region Functions

        public override GameObject GetPrefab(InitSettings initSettings) => initSettings.prefab ? initSettings.prefab : buttonType switch
        {
            Type.Label2 => EditorPrefabHolder.Instance.Function2Button,
            Type.Icon => EditorPrefabHolder.Instance.DeleteButton,
            Type.Sprite => EditorPrefabHolder.Instance.SpriteButton,
            _ => EditorPrefabHolder.Instance.Function1Button,
        };

        public override void Apply(GameObject gameObject, InitSettings initSettings)
        {
            if (this.initSettings.HasValue)
                initSettings = this.initSettings.Value;

            GameObject = gameObject;

            switch (buttonType)
            {
                case Type.Label2: {
                        var labelButton = GameObject.GetOrAddComponent<FunctionButtonStorage>();
                        buttonAdapter = new ButtonAdapter(labelButton);

                        if (initSettings.rectValues.HasValue)
                            initSettings.rectValues.Value.AssignToRectTransform(GameObject.transform.AsRT());

                        buttonAdapter.Apply(this, initSettings);

                        labelButton.label.alignment = TextAnchor.MiddleLeft;
                        labelButton.Text = Name;
                        labelButton.label.rectTransform.sizeDelta = new Vector2(-12f, 0f);

                        if (!initSettings.applyThemes)
                            break;

                        EditorThemeManager.ApplySelectable(labelButton.button, buttonThemeGroup);
                        EditorThemeManager.ApplyGraphic(labelButton.label, graphicThemeGroup);
                        break;
                    }
                case Type.Icon: {
                        var iconButton = GameObject.GetOrAddComponent<DeleteButtonStorage>();
                        buttonAdapter = new ButtonAdapter(iconButton);

                        if (initSettings.rectValues.HasValue)
                            initSettings.rectValues.Value.AssignToRectTransform(GameObject.transform.AsRT());

                        buttonAdapter.Apply(this, initSettings);

                        iconButton.Sprite = sprite;

                        if (!initSettings.applyThemes)
                            break;

                        EditorThemeManager.ApplySelectable(iconButton.button, buttonThemeGroup);
                        EditorThemeManager.ApplyGraphic(iconButton.image, graphicThemeGroup);
                        break;
                    }
                case Type.Sprite: {
                        var button = GameObject.GetOrAddComponent<Button>();
                        buttonAdapter = new ButtonAdapter(button);

                        if (initSettings.rectValues.HasValue)
                            initSettings.rectValues.Value.AssignToRectTransform(GameObject.transform.AsRT());

                        buttonAdapter.Apply(this, initSettings);

                        button.image.sprite = sprite;

                        if (!initSettings.applyThemes)
                            break;

                        EditorThemeManager.ApplySelectable(button, buttonThemeGroup);
                        break;
                    }
                default: {
                        var labelButton = GameObject.GetOrAddComponent<FunctionButtonStorage>();
                        buttonAdapter = new ButtonAdapter(labelButton);

                        if (initSettings.rectValues.HasValue)
                            initSettings.rectValues.Value.AssignToRectTransform(GameObject.transform.AsRT());

                        buttonAdapter.Apply(this, initSettings);

                        labelButton.label.alignment = TextAnchor.MiddleLeft;
                        labelButton.Text = Name;
                        labelButton.label.rectTransform.sizeDelta = new Vector2(-12f, 0f);

                        if (!initSettings.applyThemes)
                            break;

                        EditorThemeManager.ApplyGraphic(labelButton.button.image, buttonThemeGroup);
                        EditorThemeManager.ApplyGraphic(labelButton.label, graphicThemeGroup);
                        break;
                    }
            }

            InitTooltip();
        }

        public override void Apply(Button component, InitSettings initSettings) => Apply(component.gameObject, initSettings);

        public static ButtonElement ToggleButton(string name, Func<bool> onState, Action action, string tooltipGroup = null, Func<bool> shouldGenerate = null) => new ButtonElement(() => $"{name} [{(onState?.Invoke() ?? false ? "On" : "Off")}]", action, tooltipGroup, shouldGenerate);

        public static ButtonElement ToggleButton(string name, Func<bool> onState, Action<PointerEventData> onClick, string tooltipGroup = null, Func<bool> shouldGenerate = null) => new ButtonElement(() => $"{name} [{(onState?.Invoke() ?? false ? "On" : "Off")}]", onClick, tooltipGroup, shouldGenerate);

        public static ButtonElement SelectionButton(Func<bool> selectedState, string name, Action action, string tooltipGroup = null, Func<bool> shouldGenerate = null) => new ButtonElement(() => (selectedState?.Invoke() ?? false ? "> " : string.Empty) + name, action, tooltipGroup, shouldGenerate);

        public static ButtonElement SelectionButton(Func<bool> selectedState, string name, Action<PointerEventData> onClick, string tooltipGroup = null, Func<bool> shouldGenerate = null) => new ButtonElement(() => (selectedState?.Invoke() ?? false ? "> " : string.Empty) + name, onClick, tooltipGroup, shouldGenerate);

        #endregion

        public class ButtonAdapter : Exists
        {
            public ButtonAdapter() { }

            public ButtonAdapter(Button button) => this.button = button;

            public ButtonAdapter(FunctionButtonStorage labelButton) : this(labelButton.button) => this.labelButton = labelButton;

            public ButtonAdapter(DeleteButtonStorage iconButton) : this(iconButton.button) => this.iconButton = iconButton;

            public Button.ButtonClickedEvent OnClick
            {
                get => button.onClick;
                set => button.onClick = value;
            }

            public Button button;
            public FunctionButtonStorage labelButton;
            public DeleteButtonStorage iconButton;

            public void Apply(ButtonElement buttonElement, InitSettings initSettings)
            {
                if (buttonElement.onClick != null)
                {
                    OnClick.ClearAll();
                    var contextClickable = buttonElement.GameObject.GetOrAddComponent<ContextClickable>();
                    contextClickable.onClick = pointerEventData =>
                    {
                        buttonElement.onClick.Invoke(pointerEventData);
                        initSettings.onClick?.Invoke(pointerEventData);
                    };
                }
                else
                    OnClick.NewListener(() =>
                    {
                        buttonElement.action?.Invoke();
                        initSettings.onClick?.Invoke(null);
                    });
            }
        }
    }

    /// <summary>
    /// Represents a string input element in the editor.
    /// </summary>
    public class StringInputElement : EditorElement<InputField>
    {
        #region Constructors

        public StringInputElement() { }

        public StringInputElement(string value, Action<string> onValueChanged)
        {
            this.value = value;
            this.onValueChanged = onValueChanged;
            onEndEdit = default;
            placeholder = "Set text...";
        }
        
        public StringInputElement(string value, Action<string> onValueChanged, Action<string> onEndEdit)
        {
            this.value = value;
            this.onValueChanged = onValueChanged;
            this.onEndEdit = onEndEdit;
            placeholder = "Set text...";
        }
        
        public StringInputElement(string value, Action<string> onValueChanged, string placeholder)
        {
            this.value = value;
            this.onValueChanged = onValueChanged;
            onEndEdit = default;
            this.placeholder = placeholder;
        }
        
        public StringInputElement(string value, Action<string> onValueChanged, Action<string> onEndEdit, string placeholder)
        {
            this.value = value;
            this.onValueChanged = onValueChanged;
            this.onEndEdit = onEndEdit;
            this.placeholder = placeholder;
        }

        #endregion

        #region Values

        public string value;

        public Func<string> getValue;

        public string Value => getValue != null ? getValue.Invoke() : value;

        public Action<string> onValueChanged;

        public Action<string> onEndEdit;

        public InputField inputField;

        public string placeholder = "Set text...";

        public ThemeGroup themeGroup = ThemeGroup.Input_Field;

        public ThemeGroup textThemeGroup = ThemeGroup.Input_Field_Text;

        #endregion

        #region Functions

        public override GameObject GetPrefab(InitSettings initSettings) => initSettings.prefab ? initSettings.prefab : EditorPrefabHolder.Instance.StringInputField;

        public override void Apply(InputField component, InitSettings initSettings)
        {
            inputField = component;
            inputField.SetTextWithoutNotify(Value);
            inputField.onValueChanged.NewListener(_val => onValueChanged?.Invoke(_val));
            inputField.onEndEdit.NewListener(_val => onEndEdit?.Invoke(_val));
            if (!string.IsNullOrEmpty(placeholder))
                inputField.GetPlaceholderText().text = placeholder;

            if (!initSettings.applyThemes)
                return;

            EditorThemeManager.ApplyGraphic(inputField.image, themeGroup, true);
            EditorThemeManager.ApplyGraphic(inputField.textComponent, textThemeGroup);
        }

        #endregion
    }

    /// <summary>
    /// Represents a number input element in the editor.
    /// </summary>
    public class NumberInputElement : EditorElement<InputFieldStorage>
    {
        #region Constructors

        public NumberInputElement()
        {
            value = default;
            onValueChanged = default;
            onEndEdit = default;
            placeholder = "Set value...";
            arrowHandler = new ArrowHandlerFloat();
        }

        public NumberInputElement(string value, Action<string> onValueChanged)
        {
            this.value = value;
            this.onValueChanged = onValueChanged;
            onEndEdit = default;
            placeholder = "Set value...";
            arrowHandler = new ArrowHandlerFloat();
        }
        
        public NumberInputElement(string value, Action<string> onValueChanged, ArrowHandler arrowHandler)
        {
            this.value = value;
            this.onValueChanged = onValueChanged;
            onEndEdit = default;
            placeholder = "Set value...";
            if (arrowHandler)
                this.arrowHandler = arrowHandler;
        }

        public NumberInputElement(string value, Action<string> onValueChanged, Action<string> onEndEdit, ArrowHandler arrowHandler)
        {
            this.value = value;
            this.onValueChanged = onValueChanged;
            this.onEndEdit = onEndEdit;
            placeholder = "Set value...";
            if (arrowHandler)
                this.arrowHandler = arrowHandler;
        }

        public NumberInputElement(string value, Action<string> onValueChanged, string placeholder, ArrowHandler arrowHandler)
        {
            this.value = value;
            this.onValueChanged = onValueChanged;
            onEndEdit = default;
            this.placeholder = placeholder;
            if (arrowHandler)
                this.arrowHandler = arrowHandler;
        }

        public NumberInputElement(string value, Action<string> onValueChanged, Action<string> onEndEdit, string placeholder, ArrowHandler arrowHandler)
        {
            this.value = value;
            this.onValueChanged = onValueChanged;
            this.onEndEdit = onEndEdit;
            this.placeholder = placeholder;
            if (arrowHandler)
                this.arrowHandler = arrowHandler;
        }

        #endregion

        #region Values

        public string value;

        public Func<string> getValue;

        public string Value => getValue != null ? getValue.Invoke() : value;

        public Action<string> onValueChanged;

        public Action<string> onEndEdit;

        public float scrollAmount = 0.1f;

        public float scrollMultiply = 10f;

        public float scrollMin = 0f;

        public float scrollMax = 0f;

        public ArrowHandler arrowHandler;

        public InputFieldStorage numberInputField;

        public string placeholder = "Set value...";

        public ThemeGroup themeGroup = ThemeGroup.Input_Field;

        public ThemeGroup textThemeGroup = ThemeGroup.Input_Field_Text;

        public ThemeGroup buttonThemeGroup = ThemeGroup.Function_2;

        #endregion

        #region Functions

        public override GameObject GetPrefab(InitSettings initSettings) => initSettings.prefab ? initSettings.prefab : EditorPrefabHolder.Instance.NumberInputField;

        public override void Apply(InputFieldStorage component, InitSettings initSettings)
        {
            numberInputField = component;
            numberInputField.SetTextWithoutNotify(Value);
            numberInputField.OnValueChanged.NewListener(_val => onValueChanged?.Invoke(_val));
            numberInputField.OnEndEdit.NewListener(_val => onEndEdit?.Invoke(_val));
            TriggerHelper.AddEventTriggers(numberInputField.inputField.gameObject,
                TriggerHelper.ScrollDelta(numberInputField.inputField, scrollAmount, scrollMultiply, scrollMin, scrollMax));
            if (!string.IsNullOrEmpty(placeholder))
                numberInputField.inputField.GetPlaceholderText().text = placeholder;

            arrowHandler?.Apply(numberInputField);

            if (!arrowHandler)
            {
                numberInputField.middleButton.gameObject.SetActive(false);
                numberInputField.addButton.gameObject.SetActive(false);
                numberInputField.subButton.gameObject.SetActive(false);

                numberInputField.leftButton.gameObject.SetActive(false);
                numberInputField.rightButton.gameObject.SetActive(false);
                numberInputField.leftGreaterButton.gameObject.SetActive(false);
                numberInputField.rightGreaterButton.gameObject.SetActive(false);
            }

            if (!initSettings.applyThemes)
                return;

            EditorThemeManager.ApplyGraphic(numberInputField.inputField.image, themeGroup, true);
            EditorThemeManager.ApplyGraphic(numberInputField.inputField.textComponent, textThemeGroup);

            if (!arrowHandler)
                return;

            if (numberInputField.subButton)
                EditorThemeManager.ApplySelectable(numberInputField.subButton, buttonThemeGroup, false);
            if (numberInputField.addButton)
                EditorThemeManager.ApplySelectable(numberInputField.addButton, buttonThemeGroup, false);
            if (numberInputField.leftGreaterButton)
                EditorThemeManager.ApplySelectable(numberInputField.leftGreaterButton, buttonThemeGroup, false);
            if (numberInputField.leftButton)
                EditorThemeManager.ApplySelectable(numberInputField.leftButton, buttonThemeGroup, false);
            if (numberInputField.middleButton)
                EditorThemeManager.ApplySelectable(numberInputField.middleButton, buttonThemeGroup, false);
            if (numberInputField.rightButton)
                EditorThemeManager.ApplySelectable(numberInputField.rightButton, buttonThemeGroup, false);
            if (numberInputField.rightGreaterButton)
                EditorThemeManager.ApplySelectable(numberInputField.rightGreaterButton, buttonThemeGroup, false);
        }

        #endregion

        public class ArrowHandler : Exists
        {
            public bool standardArrowFunctions = true;

            public Action leftArrowClicked;

            public Action rightArrowClicked;

            public Action leftGreaterArrowClicked;

            public Action rightGreaterArrowClicked;

            public Action middleClicked;

            public Action subClicked;

            public Action addClicked;

            public virtual void Apply(InputFieldStorage numberInputField)
            {
                numberInputField.middleButton.gameObject.SetActive(middleClicked != null);
                if (middleClicked != null)
                    numberInputField.middleButton.onClick.NewListener(() => middleClicked?.Invoke());
                numberInputField.addButton.gameObject.SetActive(addClicked != null);
                if (addClicked != null)
                    numberInputField.addButton.onClick.NewListener(() => addClicked?.Invoke());
                numberInputField.subButton.gameObject.SetActive(subClicked != null);
                if (subClicked != null)
                    numberInputField.subButton.onClick.NewListener(() => subClicked?.Invoke());

                if (standardArrowFunctions)
                    return;

                numberInputField.leftButton.gameObject.SetActive(leftArrowClicked != null);
                if (leftArrowClicked != null)
                    numberInputField.leftButton.onClick.NewListener(() => rightArrowClicked?.Invoke());
                numberInputField.rightButton.gameObject.SetActive(rightArrowClicked != null);
                if (rightArrowClicked != null)
                    numberInputField.rightButton.onClick.NewListener(() => rightArrowClicked?.Invoke());
                numberInputField.leftGreaterButton.gameObject.SetActive(leftGreaterArrowClicked != null);
                if (leftGreaterArrowClicked != null)
                    numberInputField.leftGreaterButton.onClick.NewListener(() => leftGreaterArrowClicked?.Invoke());
                numberInputField.rightGreaterButton.gameObject.SetActive(rightGreaterArrowClicked != null);
                if (rightGreaterArrowClicked != null)
                    numberInputField.rightGreaterButton.onClick.NewListener(() => rightGreaterArrowClicked?.Invoke());
            }
        }

        public class ArrowHandlerFloat : ArrowHandler
        {
            public float amount = 0.1f;

            public float multiply = 10f;

            public float min;

            public float max;

            public override void Apply(InputFieldStorage numberInputField)
            {
                base.Apply(numberInputField);
                if (standardArrowFunctions)
                    TriggerHelper.IncreaseDecreaseButtons(numberInputField, amount, multiply, min, max);
            }
        }

        public class ArrowHandlerInt : ArrowHandler
        {
            public int amount = 1;

            public int multiply = 10;

            public int min;

            public int max;

            public override void Apply(InputFieldStorage numberInputField)
            {
                base.Apply(numberInputField);
                if (standardArrowFunctions)
                    TriggerHelper.IncreaseDecreaseButtonsInt(numberInputField, amount, multiply, min, max);
            }
        }
    }

    /// <summary>
    /// Represents a dropdown element in the editor.
    /// </summary>
    public class DropdownElement : EditorElement<Dropdown>
    {
        public DropdownElement() { }

        #region Values

        public List<Dropdown.OptionData> options;
        public int value;
        public Func<int> getValue;
        public int Value => getValue != null ? getValue.Invoke() : value;
        public Action<int> onValueChanged;

        #endregion

        #region Functions

        public override GameObject GetPrefab(InitSettings initSettings) => initSettings.prefab ? initSettings.prefab : EditorPrefabHolder.Instance.Dropdown;

        public override void Apply(Dropdown component, InitSettings initSettings)
        {
            if (options != null)
                component.options = options;
            component.SetValueWithoutNotify(Value);
            component.onValueChanged.NewListener(_val =>
            {
                onValueChanged?.Invoke(_val);
                value = _val;
            });

            if (initSettings.rectValues.HasValue)
                initSettings.rectValues.Value.AssignToRectTransform(component.transform.AsRT());

            if (initSettings.applyThemes)
                EditorThemeManager.ApplyDropdown(component);
        }

        #endregion
    }

    /// <summary>
    /// Represents a label element in the editor.
    /// </summary>
    public class LabelElement : EditorElement<Text>
    {
        #region Constructors

        public LabelElement() { }
        public LabelElement(string text) => this.text = text;

        #endregion

        #region Values

        public TextAnchor alignment = TextAnchor.MiddleLeft;
        public string text;
        public int fontSize = 20;
        public FontStyle fontStyle = FontStyle.Normal;
        public HorizontalWrapMode horizontalWrap = HorizontalWrapMode.Wrap;
        public VerticalWrapMode verticalWrap = VerticalWrapMode.Overflow;

        public Vector2? sizeDelta;

        public Text uiText;

        #endregion

        #region Functions

        public override GameObject GetPrefab(InitSettings initSettings) => initSettings.prefab ? initSettings.prefab : EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject;

        public override void Apply(GameObject gameObject, InitSettings initSettings) => Apply(gameObject.GetComponent<Text>(), initSettings);

        public override void Apply(Text component, InitSettings initSettings)
        {
            if (!component)
                return;

            component.text = this;
            component.alignment = alignment;
            component.fontSize = fontSize;
            component.fontStyle = fontStyle;
            component.horizontalOverflow = horizontalWrap;
            component.verticalOverflow = verticalWrap;

            if (sizeDelta != null && sizeDelta.HasValue)
                component.rectTransform.sizeDelta = sizeDelta.Value;
        }

        public static implicit operator string(LabelElement label) => label.text;
        public static implicit operator LabelElement(string text) => new LabelElement(text);

        public static implicit operator LabelElement(Text text) => new LabelElement(text.text)
        {
            alignment = text.alignment,
            fontSize = text.fontSize,
            fontStyle = text.fontStyle,
            horizontalWrap = text.horizontalOverflow,
            verticalWrap = text.verticalOverflow,
            sizeDelta = text.rectTransform.sizeDelta,
        };

        #endregion
    }

    /// <summary>
    /// Represents a labels element in the editor.
    /// </summary>
    public class LabelsElement : EditorElement
    {
        #region Constructors

        public LabelsElement() { }

        public LabelsElement(params LabelElement[] labels) => this.labels = labels.ToList();

        public LabelsElement(InitSettings initSettings, params LabelElement[] labels)
        {
            this.labels = labels.ToList();
            Init(initSettings);
        }

        #endregion

        #region Values

        public LabelElement this[int index]
        {
            get => labels[index];
            set => labels[index] = value;
        }

        public int Count => labels.Count;

        public List<LabelElement> labels = new List<LabelElement>();

        public ThemeGroup themeGroup;

        #endregion

        #region Functions

        public override GameObject GetPrefab(InitSettings initSettings) => initSettings.prefab ? initSettings.prefab : EditorPrefabHolder.Instance.Labels;

        public override void Apply(GameObject gameObject, InitSettings initSettings)
        {
            CoreHelper.DestroyChildren(gameObject.transform);

            if (initSettings.rectValues.HasValue)
                initSettings.rectValues.Value.AssignToRectTransform(GameObject.transform.AsRT());

            var labelPrefab = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject;

            for (int i = 0; i < Count; i++)
            {
                var label = labels[i];
                var g = labelPrefab.Duplicate(GameObject.transform, labelPrefab.name);

                var labelText = g.GetComponent<Text>();
                label.Apply(labelText, InitSettings.Default.Parent(gameObject.transform));

                if (initSettings.applyThemes)
                    EditorThemeManager.ApplyGraphic(labelText, themeGroup);
            }
        }

        #endregion
    }
}
