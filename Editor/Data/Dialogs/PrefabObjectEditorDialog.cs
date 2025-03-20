using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Prefabs;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class PrefabObjectEditorDialog : EditorDialog
    {
        public PrefabObjectEditorDialog() : base(PREFAB_SELECTOR) { }

        #region Instance Values

        public RectTransform Left { get; set; }

        public RectTransform LeftContent { get; set; }

        #region Start Time / Autokill

        public InputFieldStorage StartTimeField { get; set; }

        public Dropdown AutokillDropdown { get; set; }
        public InputField AutokillField { get; set; }
        public Button AutokillSetButton { get; set; }
        public Toggle CollapseToggle { get; set; }

        #endregion

        #region Editor

        public InputField LayersField { get; set; }

        #endregion

        #region Repeat

        public InputFieldStorage RepeatCountField { get; set; }

        public InputFieldStorage RepeatOffsetTimeField { get; set; }

        public InputFieldStorage SpeedField { get; set; }

        #endregion

        #endregion

        #region Global Values

        public RectTransform Right { get; set; }

        public InputFieldStorage OffsetField { get; set; }

        public InputField NameField { get; set; }

        public FunctionButtonStorage PrefabTypeSelectorButton { get; set; }

        public Button SavePrefabButton { get; set; }

        public Text ObjectCountText { get; set; }

        public Text PrefabObjectCountText { get; set; }

        public Text TimelineObjectCountText { get; set; }

        #endregion

        public override void Init()
        {
            if (init)
                return;

            base.Init();


            #region Instance Values

            Left = GameObject.transform.Find("data/left").AsRT();
            LeftContent = Left.Find("Scroll View/Viewport/Content").AsRT();

            StartTimeField = LeftContent.Find("time").gameObject.AddComponent<InputFieldStorage>();
            StartTimeField.Assign(StartTimeField.gameObject);

            AutokillDropdown = LeftContent.Find("tod-dropdown").GetComponent<Dropdown>();
            AutokillField = LeftContent.Find("akoffset").GetComponent<InputField>();
            AutokillField.characterValidation = InputField.CharacterValidation.None;
            AutokillField.contentType = InputField.ContentType.Standard;
            AutokillField.characterLimit = 0;
            AutokillSetButton = AutokillField.transform.Find("|").GetComponent<Button>();
            CollapseToggle = StartTimeField.transform.Find("collapse").GetComponent<Toggle>();

            LayersField = LeftContent.Find("editor/layers").GetComponent<InputField>();

            RepeatCountField = LeftContent.Find("repeat/x").gameObject.GetOrAddComponent<InputFieldStorage>();
            RepeatCountField.Assign(RepeatCountField.gameObject);
            RepeatCountField.inputField.characterValidation = InputField.CharacterValidation.Integer;
            RepeatCountField.inputField.contentType = InputField.ContentType.Standard;
            RepeatCountField.inputField.characterLimit = 5;

            RepeatOffsetTimeField = LeftContent.Find("repeat/y").gameObject.GetOrAddComponent<InputFieldStorage>();
            RepeatOffsetTimeField.Assign(RepeatOffsetTimeField.gameObject);
            RepeatOffsetTimeField.inputField.characterValidation = InputField.CharacterValidation.Decimal;
            RepeatOffsetTimeField.inputField.contentType = InputField.ContentType.Standard;
            RepeatOffsetTimeField.inputField.characterLimit = 0;

            SpeedField = LeftContent.Find("speed").gameObject.GetOrAddComponent<InputFieldStorage>();
            SpeedField.Assign(SpeedField.gameObject);
            SpeedField.inputField.characterValidation = InputField.CharacterValidation.Decimal;
            SpeedField.inputField.contentType = InputField.ContentType.Standard;
            SpeedField.inputField.characterLimit = 0;

            #endregion

            #region Global Values

            Right = GameObject.transform.Find("data/right").AsRT();

            OffsetField = Right.Find("time").gameObject.AddComponent<InputFieldStorage>();
            OffsetField.Assign(OffsetField.gameObject);

            NameField = Right.Find("name").GetComponent<InputField>();
            PrefabTypeSelectorButton = Right.Find("type").GetComponent<FunctionButtonStorage>();

            SavePrefabButton = Right.Find("save prefab").GetComponent<Button>();

            ObjectCountText = Right.Find("object count label").GetChild(0).GetComponent<Text>();
            PrefabObjectCountText = Right.Find("prefab object count label").GetChild(0).GetComponent<Text>();
            TimelineObjectCountText = Right.Find("timeline object count label").GetChild(0).GetComponent<Text>();

            #endregion
        }
    }
}
