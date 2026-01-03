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
        /// Name of the element.
        /// </summary>
        public string name;

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
        /// Size delta of the element.
        /// </summary>
        public Vector2? sizeDelta;

        /// <summary>
        /// Layout element values of the element.
        /// </summary>
        public LayoutElementValues? layoutElementValues;

        /// <summary>
        /// The default name of the element.
        /// </summary>
        public abstract string DefaultName { get; }

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
        /// Gets the name of the editor element.
        /// </summary>
        /// <param name="initSettings">Initialize settings.</param>
        /// <returns>Returns the name the editor element should use.</returns>
        public virtual string GetName(InitSettings initSettings) => !string.IsNullOrEmpty(initSettings.name) ? initSettings.name : DefaultName;

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
            GameObject = GetPrefab(initSettings).Duplicate(initSettings.parent, GetName(initSettings), GetSiblingIndex(initSettings));
            if (initSettings.complexity.HasValue)
                EditorHelper.SetComplexity(GameObject, initSettings.complexity.Value, initSettings.onlySpecificComplexity, initSettings.visible, autoSpecify: false);
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

        /// <summary>
        /// Applies the editor element values to a game object.
        /// </summary>
        /// <param name="gameObject">Game object to apply to.</param>
        /// <param name="initSettings">Initialize settings.</param>
        public abstract void Apply(GameObject gameObject, InitSettings initSettings);

        /// <summary>
        /// Applies rect values to a rect transform.
        /// </summary>
        /// <param name="initSettings">Initialize settings.</param>
        public void ApplyRect(InitSettings initSettings)
        {
            if (GameObject)
                ApplyRect(GameObject.transform.AsRT(), initSettings);
        }

        /// <summary>
        /// Applies rect values to a rect transform.
        /// </summary>
        /// <param name="rectTransform">Rect transform to apply to.</param>
        /// <param name="initSettings">Initialize settings.</param>
        public void ApplyRect(RectTransform rectTransform, InitSettings initSettings)
        {
            if (sizeDelta.HasValue)
                rectTransform.sizeDelta = sizeDelta.Value;
            if (initSettings.rectValues.HasValue)
                initSettings.rectValues.Value.AssignToRectTransform(rectTransform);
        }

        /// <summary>
        /// Applies layout element values to a layout element.
        /// </summary>
        public void ApplyLayoutElement()
        {
            var layoutElement = GameObject.GetComponent<LayoutElement>();
            if (!layoutElement && layoutElementValues.HasValue)
                layoutElement = GameObject.AddComponent<LayoutElement>();
            ApplyLayoutElement(layoutElement);
        }

        /// <summary>
        /// Applies layout element values to a layout element.
        /// </summary>
        /// <param name="layoutElement">Layout element to apply.</param>
        public void ApplyLayoutElement(LayoutElement layoutElement)
        {
            if (layoutElement && layoutElementValues.HasValue)
                layoutElementValues.Value.AssignToLayoutElement(layoutElement);
        }

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
            /// Complexity of the editor element.
            /// </summary>
            public Complexity? complexity;

            /// <summary>
            /// If the editor element only uses the specific complexity.
            /// </summary>
            public bool onlySpecificComplexity;

            /// <summary>
            /// Visible check function of the editor element.
            /// </summary>
            public Func<bool> visible;

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

            /// <summary>
            /// Sets the complexity of the editor element.
            /// </summary>
            /// <param name="complexity">Complexity to set.</param>
            /// <returns>Returns the current <see cref="InitSettings"/></returns>
            public InitSettings Complexity(Complexity complexity)
            {
                this.complexity = complexity;
                return this;
            }

            /// <summary>
            /// Sets the onlySpecificComplexity value of the editor element.
            /// </summary>
            /// <param name="onlySpecificComplexity">Setting to set.</param>
            /// <returns>Returns the current <see cref="InitSettings"/></returns>
            public InitSettings OnlySpecificComplexity(bool onlySpecificComplexity)
            {
                this.onlySpecificComplexity = onlySpecificComplexity;
                return this;
            }

            /// <summary>
            /// Sets the visible check function of the editor element.
            /// </summary>
            /// <param name="visible">Visible check function to set.</param>
            /// <returns>Returns the current <see cref="InitSettings"/></returns>
            public InitSettings Visible(Func<bool> visible)
            {
                this.visible = visible;
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
        public LayoutGroupElement(InitSettings initSettings, LayoutValues layoutValues, params EditorElement[] editorElements)
        {
            this.layoutValues = layoutValues;
            subElements = editorElements.ToList();
            Init(initSettings);
        }
        
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

        public override string DefaultName => "layout element";

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
                initSettings.prefab.Duplicate(initSettings.parent, GetName(initSettings), GetSiblingIndex(initSettings)) :
                Creator.NewUIObject(GetName(initSettings), initSettings.parent, GetSiblingIndex(initSettings));

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

            if (initSettings.complexity.HasValue)
                EditorHelper.SetComplexity(GameObject, initSettings.complexity.Value, initSettings.onlySpecificComplexity, initSettings.visible, autoSpecify: false);
            Apply(GameObject, initSettings);
            InitSubElements(InitSettings.Default.OnClick(initSettings.onClick));
        }

        public override void Apply(GameObject gameObject, InitSettings initSettings)
        {
            GameObject.transform.AsRT().sizeDelta = size;
            ApplyRect(initSettings);
            ApplyLayoutElement();

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

        public override string DefaultName => "Scroll View";

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

        public SpacerElement(Func<bool> shouldGenerate) : base(null, shouldGenerate) { }

        #region Values

        public Vector2 size = new Vector2(0f, 4f);

        public ThemeGroup themeGroup = ThemeGroup.Background_3;

        public override string DefaultName => "spacer element";

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

        public ButtonElement(Type buttonType, string name, Action action, string tooltipGroup = null, Func<bool> shouldGenerate = null) : base(tooltipGroup, shouldGenerate)
        {
            this.buttonType = buttonType;
            this.name = name;
            this.action = action;
        }

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

        public TextAnchor? labelAlignment;

        public ThemeGroup buttonThemeGroup = ThemeGroup.Function_2;
        public ThemeGroup graphicThemeGroup = ThemeGroup.Function_2_Text;

        public ButtonAdapter buttonAdapter;

        public override string DefaultName => "button element";

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
            ApplyRect(initSettings);
            ApplyLayoutElement();

            switch (buttonType)
            {
                case Type.Label2: {
                        var labelButton = GameObject.GetOrAddComponent<FunctionButtonStorage>();
                        buttonAdapter = new ButtonAdapter(labelButton);

                        buttonAdapter.Apply(this, initSettings);

                        labelButton.label.alignment = labelAlignment ?? TextAnchor.MiddleLeft;
                        labelButton.Text = Name;
                        labelButton.label.rectTransform.sizeDelta = new Vector2(-12f, 0f);

                        if (!initSettings.applyThemes)
                            break;

                        if (EditorThemeManager.IsSelectable(buttonThemeGroup))
                            EditorThemeManager.ApplySelectable(labelButton.button, buttonThemeGroup);
                        else
                            EditorThemeManager.ApplyGraphic(labelButton.button.image, buttonThemeGroup);
                        EditorThemeManager.ApplyGraphic(labelButton.label, graphicThemeGroup);
                        break;
                    }
                case Type.Icon: {
                        var iconButton = GameObject.GetOrAddComponent<DeleteButtonStorage>();
                        buttonAdapter = new ButtonAdapter(iconButton);

                        var layoutElement = GameObject.GetComponent<LayoutElement>();
                        if (layoutElement)
                            layoutElement.ignoreLayout = false;

                        buttonAdapter.Apply(this, initSettings);

                        iconButton.Sprite = sprite;

                        if (!initSettings.applyThemes)
                            break;

                        if (EditorThemeManager.IsSelectable(buttonThemeGroup))
                            EditorThemeManager.ApplySelectable(iconButton.button, buttonThemeGroup);
                        else
                            EditorThemeManager.ApplyGraphic(iconButton.button.image, buttonThemeGroup);
                        EditorThemeManager.ApplyGraphic(iconButton.image, graphicThemeGroup);
                        break;
                    }
                case Type.Sprite: {
                        var button = GameObject.GetOrAddComponent<Button>();
                        buttonAdapter = new ButtonAdapter(button);
                        
                        var layoutElement = GameObject.GetComponent<LayoutElement>();
                        if (layoutElement)
                            layoutElement.ignoreLayout = false;

                        buttonAdapter.Apply(this, initSettings);

                        button.image.sprite = sprite;

                        if (!initSettings.applyThemes)
                            break;

                        EditorThemeManager.ApplySelectable(button, buttonThemeGroup, false);
                        break;
                    }
                default: {
                        var labelButton = GameObject.GetOrAddComponent<FunctionButtonStorage>();
                        buttonAdapter = new ButtonAdapter(labelButton);

                        buttonAdapter.Apply(this, initSettings);

                        labelButton.label.alignment = labelAlignment ?? TextAnchor.MiddleLeft;
                        labelButton.Text = Name;
                        labelButton.label.rectTransform.sizeDelta = new Vector2(-12f, 0f);

                        if (!initSettings.applyThemes)
                            break;

                        if (EditorThemeManager.IsSelectable(buttonThemeGroup))
                            EditorThemeManager.ApplySelectable(labelButton.button, buttonThemeGroup);
                        else
                            EditorThemeManager.ApplyGraphic(labelButton.button.image, buttonThemeGroup);
                        EditorThemeManager.ApplyGraphic(labelButton.label, graphicThemeGroup);
                        break;
                    }
            }

            InitTooltip();
        }

        public override void Apply(Button component, InitSettings initSettings) => Apply(component.gameObject, initSettings);

        public static ButtonElement Label1Button(string name, Action action, string tooltipGroup = null, Func<bool> shouldGenerate = null, ThemeGroup buttonThemeGroup = ThemeGroup.Function_1, ThemeGroup graphicThemeGroup = ThemeGroup.Function_1_Text, TextAnchor? labelAlignment = default, LayoutElementValues? layoutElementValues = default) => new ButtonElement(Type.Label1, name, action, tooltipGroup, shouldGenerate)
        {
            layoutElementValues = layoutElementValues,
            labelAlignment = labelAlignment,
            buttonThemeGroup = buttonThemeGroup,
            graphicThemeGroup = graphicThemeGroup,
        };

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

        public override string DefaultName => "input element";

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

            ApplyRect(initSettings);
            ApplyLayoutElement();

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

        public NumberInputElement(Func<string> getValue, Action<string> onValueChanged)
        {
            this.getValue = getValue;
            this.onValueChanged = onValueChanged;
            onEndEdit = default;
            placeholder = "Set value...";
            arrowHandler = new ArrowHandlerFloat();
        }

        public NumberInputElement(Func<string> getValue, Action<string> onValueChanged, ArrowHandler arrowHandler)
        {
            this.getValue = getValue;
            this.onValueChanged = onValueChanged;
            onEndEdit = default;
            placeholder = "Set value...";
            if (arrowHandler)
                this.arrowHandler = arrowHandler;
        }

        public NumberInputElement(Func<string> getValue, Action<string> onValueChanged, Action<string> onEndEdit, ArrowHandler arrowHandler)
        {
            this.getValue = getValue;
            this.onValueChanged = onValueChanged;
            this.onEndEdit = onEndEdit;
            placeholder = "Set value...";
            if (arrowHandler)
                this.arrowHandler = arrowHandler;
        }

        public NumberInputElement(Func<string> getValue, Action<string> onValueChanged, string placeholder, ArrowHandler arrowHandler)
        {
            this.getValue = getValue;
            this.onValueChanged = onValueChanged;
            onEndEdit = default;
            this.placeholder = placeholder;
            if (arrowHandler)
                this.arrowHandler = arrowHandler;
        }

        public NumberInputElement(Func<string> getValue, Action<string> onValueChanged, Action<string> onEndEdit, string placeholder, ArrowHandler arrowHandler)
        {
            this.getValue = getValue;
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

        public override string DefaultName => "number input element";

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

            ApplyRect(initSettings);
            ApplyLayoutElement();

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

            if (numberInputField.inputField.image)
                numberInputField.inputField.image.fillCenter = true;
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

        #region Sub Classes

        public class ArrowHandler : Exists
        {
            public bool standardArrowFunctions = true;

            public Action<string> leftGreaterArrowClicked;

            public Action<string> leftArrowClicked;

            public Action<string> middleClicked;

            public Action<string> rightArrowClicked;

            public Action<string> rightGreaterArrowClicked;

            public Action<string> subClicked;

            public Action<string> addClicked;

            public Sprite leftGreaterSprite;

            public Sprite leftSprite;

            public Sprite middleSprite;

            public Sprite rightSprite;

            public Sprite rightGreaterSprite;

            public Sprite subSprite;

            public Sprite addSprite;

            public virtual void Apply(InputFieldStorage numberInputField)
            {
                if (leftGreaterSprite)
                    numberInputField.leftGreaterButton.image.sprite = leftGreaterSprite;
                if (leftSprite)
                    numberInputField.leftButton.image.sprite = leftSprite;
                if (middleSprite)
                    numberInputField.middleButton.image.sprite = middleSprite;
                if (rightSprite)
                    numberInputField.rightButton.image.sprite = rightSprite;
                if (rightGreaterSprite)
                    numberInputField.rightGreaterButton.image.sprite = rightGreaterSprite;
                if (subSprite)
                    numberInputField.subButton.image.sprite = subSprite;
                if (addSprite)
                    numberInputField.addButton.image.sprite = addSprite;

                numberInputField.middleButton.gameObject.SetActive(middleClicked != null);
                if (middleClicked != null)
                    numberInputField.middleButton.onClick.NewListener(() => middleClicked?.Invoke(numberInputField.Text));
                numberInputField.addButton.gameObject.SetActive(addClicked != null);
                if (addClicked != null)
                    numberInputField.addButton.onClick.NewListener(() => addClicked?.Invoke(numberInputField.Text));
                numberInputField.subButton.gameObject.SetActive(subClicked != null);
                if (subClicked != null)
                    numberInputField.subButton.onClick.NewListener(() => subClicked?.Invoke(numberInputField.Text));

                if (standardArrowFunctions)
                    return;

                numberInputField.leftGreaterButton.gameObject.SetActive(leftGreaterArrowClicked != null);
                if (leftGreaterArrowClicked != null)
                    numberInputField.leftGreaterButton.onClick.NewListener(() => leftGreaterArrowClicked?.Invoke(numberInputField.Text));
                numberInputField.leftButton.gameObject.SetActive(leftArrowClicked != null);
                if (leftArrowClicked != null)
                    numberInputField.leftButton.onClick.NewListener(() => leftArrowClicked?.Invoke(numberInputField.Text));
                numberInputField.rightButton.gameObject.SetActive(rightArrowClicked != null);
                if (rightArrowClicked != null)
                    numberInputField.rightButton.onClick.NewListener(() => rightArrowClicked?.Invoke(numberInputField.Text));
                numberInputField.rightGreaterButton.gameObject.SetActive(rightGreaterArrowClicked != null);
                if (rightGreaterArrowClicked != null)
                    numberInputField.rightGreaterButton.onClick.NewListener(() => rightGreaterArrowClicked?.Invoke(numberInputField.Text));
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
                TriggerHelper.AddEventTriggers(numberInputField.inputField.gameObject,
                    TriggerHelper.ScrollDelta(numberInputField.inputField, amount, multiply, min, max));
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
                TriggerHelper.AddEventTriggers(numberInputField.inputField.gameObject,
                    TriggerHelper.ScrollDeltaInt(numberInputField.inputField, amount, min, max));
                if (standardArrowFunctions)
                    TriggerHelper.IncreaseDecreaseButtonsInt(numberInputField, amount, multiply, min, max);
            }
        }

        #endregion
    }

    /// <summary>
    /// Represents a dropdown element in the editor.
    /// </summary>
    public class DropdownElement : EditorElement<Dropdown>
    {
        #region Constructors

        public DropdownElement() { }

        public DropdownElement(List<Dropdown.OptionData> options) => this.options = options;

        #endregion

        #region Values

        public Dropdown dropdown;
        public List<Dropdown.OptionData> options;
        public int value;
        public Func<int> getValue;
        public int Value => getValue != null ? getValue.Invoke() : value;
        public Action<int> onValueChanged;

        public override string DefaultName => "dropdown element";

        #endregion

        #region Functions

        public override GameObject GetPrefab(InitSettings initSettings) => initSettings.prefab ? initSettings.prefab : EditorPrefabHolder.Instance.Dropdown;

        public override void Apply(Dropdown component, InitSettings initSettings)
        {
            dropdown = component;
            if (options != null)
                dropdown.options = options;
            dropdown.SetValueWithoutNotify(Value);
            dropdown.onValueChanged.NewListener(_val =>
            {
                onValueChanged?.Invoke(_val);
                value = _val;
            });

            if (initSettings.rectValues.HasValue)
                initSettings.rectValues.Value.AssignToRectTransform(dropdown.transform.AsRT());

            if (initSettings.applyThemes)
                EditorThemeManager.ApplyDropdown(dropdown);
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

        public Text uiText;

        public override string DefaultName => "label";

        #endregion

        #region Functions

        public override GameObject GetPrefab(InitSettings initSettings) => initSettings.prefab ? initSettings.prefab : EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject;

        public override void Apply(GameObject gameObject, InitSettings initSettings) => Apply(gameObject.GetComponent<Text>(), initSettings);

        public override void Apply(Text component, InitSettings initSettings)
        {
            if (!component)
                return;

            GameObject = component.gameObject;
            uiText = component;
            component.text = this;
            component.alignment = alignment;
            component.fontSize = fontSize;
            component.fontStyle = fontStyle;
            component.horizontalOverflow = horizontalWrap;
            component.verticalOverflow = verticalWrap;

            ApplyRect(initSettings);
            ApplyLayoutElement();
        }

        public void SetText(string text)
        {
            this.text = text;
            if (uiText)
                uiText.text = text;
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

        public LabelsElement(LayoutValues layoutValues, params LabelElement[] labels) : this(labels) => this.layoutValues = layoutValues;

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

        public ThemeGroup themeGroup = ThemeGroup.Light_Text;

        public LayoutValues layoutValues;

        public override string DefaultName => "labels";

        #endregion

        #region Functions

        public override GameObject GetPrefab(InitSettings initSettings) => initSettings.prefab ? initSettings.prefab : EditorPrefabHolder.Instance.Labels;

        public override void Apply(GameObject gameObject, InitSettings initSettings)
        {
            CoreHelper.DestroyChildren(gameObject.transform);

            if (initSettings.rectValues.HasValue)
                initSettings.rectValues.Value.AssignToRectTransform(GameObject.transform.AsRT());

            if (layoutValues && layoutValues is HorizontalOrVerticalLayoutValues hvLayoutValues)
            {
                var hvLayoutGroup = gameObject.GetComponent<HorizontalOrVerticalLayoutGroup>();
                if (hvLayoutGroup)
                    hvLayoutValues.AssignToLayout(hvLayoutGroup);
            }
            else if (layoutValues && layoutValues is GridLayoutValues gridLayoutValues)
            {
                var gridLayoutGroup = gameObject.GetComponent<GridLayoutGroup>();
                if (gridLayoutGroup)
                    gridLayoutValues.AssignToLayout(gridLayoutGroup);
            }

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

    public class ColorGroupElement : EditorElement
    {
        #region Constructors

        public ColorGroupElement() { }

        public ColorGroupElement(int count) => this.count = count;

        public ColorGroupElement(int count, Vector2 sizeDelta, Vector2 cellSize, Vector2 spacing) : this(count)
        {
            this.sizeDelta = sizeDelta;
            this.cellSize = cellSize;
            this.spacing = spacing;
        }

        #endregion

        #region Values

        public Toggle this[int index]
        {
            get => toggles[index];
            set => toggles[index] = value;
        }

        public int count = 9;

        public List<Toggle> toggles = new List<Toggle>();

        public GridLayoutGroup gridLayout;

        public Vector2? cellSize;

        public Vector2? spacing;

        public ThemeGroup backgroundColor = ThemeGroup.Background_1;

        public override string DefaultName => "colors";

        #endregion

        #region Functions

        public override GameObject GetPrefab(InitSettings initSettings) => initSettings.prefab ? initSettings.prefab : EditorPrefabHolder.Instance.ColorsLayout;

        public override void Apply(GameObject gameObject, InitSettings initSettings)
        {
            CoreHelper.DestroyChildren(gameObject.transform);

            ApplyRect(initSettings);
            ApplyLayoutElement();

            gridLayout = gameObject.GetComponent<GridLayoutGroup>();
            if (cellSize.HasValue)
                gridLayout.cellSize = cellSize.Value;
            if (spacing.HasValue)
                gridLayout.spacing = spacing.Value;

            var prefab = EditorPrefabHolder.Instance.ColorsLayout.transform.GetChild(0).gameObject;
            for (int i = 0; i < count; i++)
            {
                var col = prefab.Duplicate(gameObject.transform, (i + 1).ToString());
                var toggle = col.GetComponent<Toggle>();
                toggles.Add(toggle);

                if (!initSettings.applyThemes)
                    continue;

                EditorThemeManager.ApplyGraphic(toggle.image, ThemeGroup.Null, true);
                EditorThemeManager.ApplyGraphic(toggle.graphic, backgroundColor);
            }
        }

        #endregion
    }
}
