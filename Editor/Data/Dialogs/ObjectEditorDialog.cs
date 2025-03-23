using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;

namespace BetterLegacy.Editor.Data.Dialogs
{
    /// <summary>
    /// Represents the object editor dialog for editing a <see cref="Core.Data.Beatmap.BeatmapObject"/>.
    /// </summary>
    public class ObjectEditorDialog : EditorDialog
    {
        public ObjectEditorDialog() : base(OBJECT_EDITOR) { }

        #region Object Values

        public RectTransform Content { get; set; }

        #region Top Properties

        public RectTransform IDBase { get; set; }
        public Text IDText { get; set; }
        public Text LDMLabel { get; set; }
        public Toggle LDMToggle { get; set; }

        #endregion

        #region Name Area

        public InputField NameField { get; set; }
        public Dropdown ObjectTypeDropdown { get; set; }
        public RectTransform TagsScrollView { get; set; }
        public RectTransform TagsContent { get; set; }

        #endregion

        #region Start Time / Autokill

        public InputFieldStorage StartTimeField { get; set; }

        public Dropdown AutokillDropdown { get; set; }
        public InputField AutokillField { get; set; }
        public Button AutokillSetButton { get; set; }
        public Toggle CollapseToggle { get; set; }

        #endregion

        #region Parent

        public FunctionButtonStorage ParentButton { get; set; }
        public HoverTooltip ParentInfo { get; set; }
        public Button ParentMoreButton { get; set; }
        public GameObject ParentSettingsParent { get; set; }
        public Toggle ParentDesyncToggle { get; set; }
        public Button ParentSearchButton { get; set; }
        public Button ParentClearButton { get; set; }
        public Button ParentPickerButton { get; set; }

        public List<ParentSetting> ParentSettings { get; set; } = new List<ParentSetting>();

        #endregion

        #region Origin

        public RectTransform OriginParent { get; set; }
        public InputFieldStorage OriginXField { get; set; }
        public InputFieldStorage OriginYField { get; set; }

        public List<Toggle> OriginXToggles { get; set; } = new List<Toggle>();
        public List<Toggle> OriginYToggles { get; set; } = new List<Toggle>();

        #endregion

        #region Gradient / Shape

        public Text GradientShapesLabel { get; set; }

        public RectTransform GradientParent { get; set; }
        public List<Toggle> GradientToggles { get; set; } = new List<Toggle>();
        public InputFieldStorage GradientScale { get; set; }
        public InputFieldStorage GradientRotation { get; set; }

        public RectTransform ShapeTypesParent { get; set; }
        public RectTransform ShapeOptionsParent { get; set; }

        #endregion

        #region Render Depth / Type

        public RectTransform DepthParent { get; set; }
        public InputFieldStorage DepthField { get; set; }
        public Slider DepthSlider { get; set; }
        public Button DepthSliderLeftButton { get; set; }
        public Button DepthSliderRightButton { get; set; }
        public Dropdown RenderTypeDropdown { get; set; }

        #endregion

        #region Editor Settings

        public RectTransform EditorSettingsParent { get; set; }
        public Slider BinSlider { get; set; }
        public InputField EditorLayerField { get; set; }

        #endregion

        #region Prefab

        public GameObject CollapsePrefabLabel { get; set; }
        public FunctionButtonStorage CollapsePrefabButton { get; set; }
        public GameObject AssignPrefabLabel { get; set; }
        public FunctionButtonStorage AssignPrefabButton { get; set; }
        public FunctionButtonStorage RemovePrefabButton { get; set; }

        #endregion

        #region Unity Explorer

        public Text UnityExplorerLabel { get; set; }

        public FunctionButtonStorage InspectBeatmapObjectButton { get; set; }
        public FunctionButtonStorage InspectLevelObjectButton { get; set; }
        public FunctionButtonStorage InspectTimelineObjectButton { get; set; }

        #endregion

        #endregion

        #region Keyframe Editors

        /// <summary>
        /// The currently open object keyframe editor.
        /// </summary>
        public KeyframeDialog CurrentKeyframeDialog { get; set; }

        /// <summary>
        /// A list containing all the event keyframe editors.
        /// </summary>
        public List<KeyframeDialog> keyframeDialogs = new List<KeyframeDialog>();

        #endregion

        public override void Init()
        {
            if (init)
                return;

            base.Init();

            Content = ObjEditor.inst.ObjectView.transform.AsRT();

            #region Top Properties

            IDBase = Content.Find("id").AsRT();
            IDText = IDBase.Find("text").GetComponent<Text>();
            LDMLabel = IDBase.Find("title").GetComponent<Text>();
            LDMToggle = IDBase.Find("ldm").GetComponent<Toggle>();

            #endregion

            #region Name Area

            NameField = Content.Find("name/name").GetComponent<InputField>();
            ObjectTypeDropdown = Content.Find("name/object-type").GetComponent<Dropdown>();
            TagsScrollView = Content.Find("Tags Scroll View").AsRT();
            TagsContent = Content.Find("Tags Scroll View/Viewport/Content").AsRT();

            #endregion

            #region Start Time

            StartTimeField = Content.Find("time").gameObject.AddComponent<InputFieldStorage>();
            StartTimeField.Assign(StartTimeField.gameObject);

            #endregion

            #region Autokill

            AutokillDropdown = Content.Find("autokill/tod-dropdown").GetComponent<Dropdown>();
            AutokillField = Content.Find("autokill/tod-value").GetComponent<InputField>();
            AutokillSetButton = Content.Find("autokill/|").GetComponent<Button>();
            CollapseToggle = Content.Find("autokill/collapse").GetComponent<Toggle>();

            #endregion

            #region Parent

            ParentButton = Content.Find("parent/text").gameObject.AddComponent<FunctionButtonStorage>();
            ParentButton.button = ParentButton.GetComponent<Button>();
            ParentButton.label = ParentButton.transform.Find("text").GetComponent<Text>();
            ParentInfo = ParentButton.GetComponent<HoverTooltip>();
            ParentMoreButton = Content.Find("parent/more").GetComponent<Button>();
            ParentSettingsParent = Content.Find("parent_more").gameObject;
            ParentDesyncToggle = ParentSettingsParent.transform.Find("spawn_once").GetComponent<Toggle>();
            ParentSearchButton = Content.Find("parent/parent").GetComponent<Button>();
            ParentClearButton = Content.Find("parent/clear parent").GetComponent<Button>();
            ParentPickerButton = Content.Find("parent/parent picker").GetComponent<Button>();

            for (int i = 0; i < 3; i++)
            {
                var name = i switch
                {
                    0 => "pos",
                    1 => "sca",
                    _ => "rot"
                };

                var row = ParentSettingsParent.transform.Find($"{name}_row");
                var parentSetting = new ParentSetting();
                parentSetting.row = row;
                parentSetting.label = row.Find("text").GetComponent<Text>();
                parentSetting.activeToggle = row.Find(name).GetComponent<Toggle>();
                parentSetting.offsetField = row.Find($"{name}_offset").GetComponent<InputField>();
                parentSetting.additiveToggle = row.Find($"{name}_add").GetComponent<Toggle>();
                parentSetting.parallaxField = row.Find($"{name}_parallax").GetComponent<InputField>();
                ParentSettings.Add(parentSetting);
            }

            #endregion

            #region Origin

            OriginParent = Content.Find("origin").AsRT();
            OriginXField = OriginParent.Find("x").gameObject.GetComponent<InputFieldStorage>();
            if (!OriginXField.inputField.gameObject.GetComponent<InputFieldSwapper>())
            {
                var ifh = OriginXField.inputField.gameObject.AddComponent<InputFieldSwapper>();
                ifh.Init(OriginXField.inputField, InputFieldSwapper.Type.Num);
            }

            OriginYField = OriginParent.Find("y").gameObject.GetComponent<InputFieldStorage>();
            if (!OriginYField.inputField.gameObject.GetComponent<InputFieldSwapper>())
            {
                var ifh = OriginYField.inputField.gameObject.AddComponent<InputFieldSwapper>();
                ifh.Init(OriginYField.inputField, InputFieldSwapper.Type.Num);
            }

            for (int i = 0; i < 3; i++)
            {
                OriginXToggles.Add(OriginParent.Find("origin-x").GetChild(i).GetComponent<Toggle>());
                OriginYToggles.Add(OriginParent.Find("origin-y").GetChild(i).GetComponent<Toggle>());
            }

            #endregion

            #region Gradient / Shape

            GradientParent = Content.Find("gradienttype").AsRT();
            GradientShapesLabel = Content.GetChild(GradientParent.GetSiblingIndex() - 1).GetChild(0).GetComponent<Text>();
            for (int i = 0; i < GradientParent.childCount; i++)
                GradientToggles.Add(GradientParent.GetChild(i).GetComponent<Toggle>());
            GradientScale = Content.Find("gradientscale").GetComponent<InputFieldStorage>();
            GradientRotation = Content.Find("gradientrotation").GetComponent<InputFieldStorage>();

            ShapeTypesParent = Content.Find("shape").AsRT();
            ShapeOptionsParent = Content.Find("shapesettings").AsRT();

            #endregion

            #region Render Depth / Type

            DepthParent = Content.Find("depth").AsRT();
            DepthField = Content.Find("depth input/depth").GetComponent<InputFieldStorage>();

            if (!DepthField.inputField.GetComponent<InputFieldSwapper>())
            {
                var ifh = DepthField.inputField.gameObject.AddComponent<InputFieldSwapper>();
                ifh.Init(DepthField.inputField, InputFieldSwapper.Type.Num);
            }

            DepthSlider = Content.Find("depth/depth").GetComponent<Slider>();
            DepthSliderLeftButton = DepthParent.Find("<").GetComponent<Button>();
            DepthSliderRightButton = DepthParent.Find(">").GetComponent<Button>();
            RenderTypeDropdown = Content.Find("rendertype").GetComponent<Dropdown>();

            #endregion

            #region Editor Settings

            EditorSettingsParent = Content.Find("editor").AsRT();
            EditorLayerField = EditorSettingsParent.Find("layers")?.GetComponent<InputField>();
            EditorLayerField.image = EditorLayerField.GetComponent<Image>();
            BinSlider = EditorSettingsParent.Find("bin")?.GetComponent<Slider>();

            #endregion

            #region Prefab

            CollapsePrefabLabel = Content.Find("collapselabel").gameObject;
            CollapsePrefabButton = Content.Find("applyprefab").gameObject.GetOrAddComponent<FunctionButtonStorage>();
            CollapsePrefabButton.label = CollapsePrefabButton.transform.Find("Text").GetComponent<Text>();
            CollapsePrefabButton.button = CollapsePrefabButton.GetComponent<Button>();

            AssignPrefabLabel = Content.Find("assignlabel").gameObject;
            AssignPrefabButton = Content.Find("assign prefab").gameObject.GetOrAddComponent<FunctionButtonStorage>();
            AssignPrefabButton.label = AssignPrefabButton.transform.Find("Text").GetComponent<Text>();
            AssignPrefabButton.button = AssignPrefabButton.GetComponent<Button>();

            RemovePrefabButton = Content.Find("remove prefab").gameObject.GetOrAddComponent<FunctionButtonStorage>();
            RemovePrefabButton.label = RemovePrefabButton.transform.Find("Text").GetComponent<Text>();
            RemovePrefabButton.button = RemovePrefabButton.GetComponent<Button>();

            #endregion

            #region Unity Explorer

            if (ModCompatibility.UnityExplorerInstalled)
            {
                UnityExplorerLabel = Content.Find("unity explorer label").GetChild(0).GetComponent<Text>();
                InspectBeatmapObjectButton = Content.Find("inspectbeatmapobject").GetComponent<FunctionButtonStorage>();
                InspectLevelObjectButton = Content.Find("inspectlevelobject").GetComponent<FunctionButtonStorage>();
                InspectTimelineObjectButton = Content.Find("inspecttimelineobject").GetComponent<FunctionButtonStorage>();
            }

            #endregion

            for (int i = 0; i < ObjEditor.inst.KeyframeDialogs.Count; i++)
            {
                var keyframeDialog = new KeyframeDialog(i);
                keyframeDialog.GameObject = ObjEditor.inst.KeyframeDialogs[i];
                keyframeDialog.isMulti = i == 4;
                keyframeDialog.isObjectKeyframe = true;
                keyframeDialog.Init();
                keyframeDialogs.Add(keyframeDialog);
            }
        }

        /// <summary>
        /// Opens an object keyframe editor.
        /// </summary>
        /// <param name="type">The type of object keyframe.</param>
        public void OpenKeyframeDialog(int type)
        {
            for (int i = 0; i < keyframeDialogs.Count; i++)
            {
                var active = i == type;
                keyframeDialogs[i].SetActive(active);
                if (active)
                    CurrentKeyframeDialog = keyframeDialogs[i];
            }
        }

        /// <summary>
        /// Checks if <see cref="CurrentKeyframeDialog"/> is of a specific keyframe type.
        /// </summary>
        /// <param name="type">The type of object keyframe.</param>
        /// <returns>Returns true if the current keyframe dialog type matches the specific type, otherwise returns false.</returns>
        public bool IsCurrentKeyframeType(int type) => CurrentKeyframeDialog && CurrentKeyframeDialog.type == type;

        /// <summary>
        /// Closes the keyframe dialogs.
        /// </summary>
        public void CloseKeyframeDialogs()
        {
            for (int i = 0; i < keyframeDialogs.Count; i++)
                keyframeDialogs[i].SetActive(false);
            CurrentKeyframeDialog = null;
        }
    }
}
