using BetterLegacy.Core;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class ObjectEditorDialog : EditorDialog
    {
        #region Object Properties

        public RectTransform Content { get; set; }

        public override void Init()
        {
            if (init)
                return;

            base.Init();

            Name = OBJECT_EDITOR;
            GameObject = GetLegacyDialog().Dialog.gameObject;
            Content = ObjEditor.inst.ObjectView.transform.AsRT();

            #region Top Properties

            IDBase = Content.Find("id").AsRT();
            IDText = IDBase.Find("text").GetComponent<Text>();
            LDMToggle = IDBase.Find("ldm").GetComponent<Toggle>();

            #endregion

            #region Name Area

            NameField = Content.Find("name/name").GetComponent<InputField>();
            ObjectTypeDropdown = Content.Find("name/object-type").GetComponent<Dropdown>();
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
            ParentButton.text = ParentButton.transform.Find("text").GetComponent<Text>();
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
            OriginXField = OriginParent.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            OriginXField.Assign(OriginXField.gameObject);
            OriginYField = OriginParent.Find("y").gameObject.GetOrAddComponent<InputFieldStorage>();
            OriginYField.Assign(OriginYField.gameObject);

            for (int i = 0; i < 3; i++)
            {
                OriginXToggles.Add(OriginParent.Find("origin-x").GetChild(i).GetComponent<Toggle>());
                OriginYToggles.Add(OriginParent.Find("origin-y").GetChild(i).GetComponent<Toggle>());
            }

            #endregion

            #region Gradient / Shape

            GradientParent = Content.Find("gradienttype").AsRT();
            for (int i = 0; i < GradientParent.childCount; i++)
                GradientToggles.Add(GradientParent.GetChild(i).GetComponent<Toggle>());
            ShapeTypesParent = Content.Find("shape").AsRT();
            ShapeOptionsParent = Content.Find("shapesettings").AsRT();

            #endregion

            #region Render Depth / Type

            DepthParent = Content.Find("depth").AsRT();
            DepthField = Content.Find("depth input/depth").gameObject.GetOrAddComponent<InputFieldStorage>();
            DepthField.Assign(DepthField.gameObject);
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
            CollapsePrefabButton.text = CollapsePrefabButton.transform.Find("Text").GetComponent<Text>();
            CollapsePrefabButton.button = CollapsePrefabButton.GetComponent<Button>();

            AssignPrefabLabel = Content.Find("assignlabel").gameObject;
            AssignPrefabButton = Content.Find("assign prefab").gameObject.GetOrAddComponent<FunctionButtonStorage>();
            AssignPrefabButton.text = AssignPrefabButton.transform.Find("Text").GetComponent<Text>();
            AssignPrefabButton.button = AssignPrefabButton.GetComponent<Button>();
            RemovePrefabButton = Content.Find("remove prefab").gameObject.GetOrAddComponent<FunctionButtonStorage>();
            RemovePrefabButton.text = RemovePrefabButton.transform.Find("Text").GetComponent<Text>();
            RemovePrefabButton.button = RemovePrefabButton.GetComponent<Button>();

            for (int i = 0; i < ObjEditor.inst.KeyframeDialogs.Count; i++)
            {
                var keyframeDialog = new KeyframeDialog();
                keyframeDialog.GameObject = ObjEditor.inst.KeyframeDialogs[i];
                keyframeDialogs.Add(keyframeDialog);
            }

            #endregion
        }

        #region Top Properties

        public RectTransform IDBase { get; set; }
        public Text IDText { get; set; }
        public Toggle LDMToggle { get; set; }

        #endregion

        #region Name Area

        public InputField NameField { get; set; }
        public Dropdown ObjectTypeDropdown { get; set; }
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

        public RectTransform GradientParent { get; set; }
        public List<Toggle> GradientToggles { get; set; } = new List<Toggle>();
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

        #endregion

        public List<KeyframeDialog> keyframeDialogs = new List<KeyframeDialog>();
    }

}
