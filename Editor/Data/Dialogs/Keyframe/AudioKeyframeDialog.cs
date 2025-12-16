using BetterLegacy.Core;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class AudioKeyframeDialog : KeyframeDialog
    {
        public AudioKeyframeDialog() : base(EventLibrary.Indexes.AUDIO) { }

        public InputFieldStorage PitchField { get; set; }

        public InputFieldStorage VolumeField { get; set; }

        public InputFieldStorage PanStereoField { get; set; }

        public override void Init()
        {
            CreateNew(EventEditor.inst.dialogRight);

            base.Init();

            new LabelsElement("Pitch", "Volume").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var music = EditorPrefabHolder.Instance.Vector2InputFields.Duplicate(GameObject.transform, "music");
            var musicFields = music.GetComponent<Vector2InputFieldStorage>();

            new LabelsElement("Pan Stereo").Init(EditorElement.InitSettings.Default.Parent(GameObject.transform));
            var panStereo = EditorPrefabHolder.Instance.LayoutInputField.Duplicate(GameObject.transform, "panstereo");

            PitchField = musicFields.x;
            PitchField.Assign();

            VolumeField = musicFields.y;
            VolumeField.Assign();

            PanStereoField = panStereo.transform.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            PanStereoField.Assign();

            EditorThemeManager.ApplyInputField(PitchField);
            EditorThemeManager.ApplyInputField(VolumeField);
            EditorThemeManager.ApplyInputField(PanStereoField);
        }

        public override void Render()
        {
            base.Render();

            SetFloatInputField(PitchField, 0, 0.1f, 10f, 0.001f, 10f, allowNegative: false);
            SetFloatInputField(VolumeField, 1, max: 1f, allowNegative: false);
            SetFloatInputField(PanStereoField, 2);
        }
    }
}
