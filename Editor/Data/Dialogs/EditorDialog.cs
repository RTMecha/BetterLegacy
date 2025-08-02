using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    /// <summary>
    /// Represents a dialog window in the editor, where most things are edited.
    /// </summary>
    public class EditorDialog : Exists
    {
        public EditorDialog() { }

        public EditorDialog(string name) => InitDialog(name);

        #region Properties

        /// <summary>
        /// The editor dialog that is currently being viewed.
        /// </summary>
        public static EditorDialog CurrentDialog { get; set; }

        /// <summary>
        /// If this dialog is the currently open dialog.
        /// </summary>
        public bool IsCurrent => CurrentDialog && CurrentDialog.Name == Name;

        /// <summary>
        /// If the mouse cursor is over this dialog.
        /// </summary>
        public bool MouseOver { get; set; }

        /// <summary>
        /// Name of the editor dialog.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Game object of the editor dialog.
        /// </summary>
        public GameObject GameObject { get; set; }

        /// <summary>
        /// Background image of the editor dialogs' title.
        /// </summary>
        public Image TitlePanel { get; set; }

        /// <summary>
        /// Title of the editor dialog.
        /// </summary>
        public Text Title { get; set; }

        #endregion

        public bool init;

        #region Constants

        public const string MULTI_OBJECT_EDITOR = "Multi Object Editor";
        public const string OBJECT_EDITOR = "Object Editor";
        public const string EVENT_EDITOR = "Event Editor";
        public const string CHECKPOINT_EDITOR = "Checkpoint Editor";
        public const string BACKGROUND_EDITOR = "Background Editor";
        public const string METADATA_EDITOR = "Metadata Editor";
        public const string PREFAB_EDITOR = "Prefab Editor";
        public const string SETTINGS_EDITOR = "Settings Editor";
        public const string MULTI_KEYFRAME_EDITOR = "Multi Keyframe Editor";
        public const string MARKER_EDITOR = "Marker Editor";
        public const string PREFAB_SELECTOR = "Prefab Selector";
        public const string LEVEL_TEMPLATE_SELECTOR = "New Level Template Dialog";
        public const string SCREENSHOTS = "Screenshot Dialog";
        public const string KEYBIND_EDITOR = "Keybind Editor";
        public const string PLAYER_EDITOR = "Player Editor";
        public const string LEVEL_COMBINER = "Level Combiner";
        public const string UPLOADED_LEVELS = "Uploaded Dialog";
        public const string DOCUMENTATION = "Documentation Dialog";
        public const string PREFAB_EXTERNAL_EDITOR = "Prefab External Dialog";
        public const string PINNED_EDITOR_LAYER_DIALOG = "Pinned Editor Layer Dialog";
        public const string ACHIEVEMENT_EDITOR_DIALOG = "Achievement Editor Dialog";
        public const string LEVEL_COLLECTION_EDITOR = "Level Collection Editor";
        public const string LEVEL_INFO_EDITOR = "Level Info Editor";
        public const string LEVEL_PROPERTIES_EDITOR = "Level Properties Editor";

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the editor dialog UI.
        /// </summary>
        public virtual void Init()
        {
            if (init)
                return;

            init = true;
            RTEditor.inst.editorDialogs.Add(this);
        }

        /// <summary>
        /// Opens the editor dialog and sets it as the current dialog.
        /// </summary>
        public virtual void Open()
        {
            CurrentDialog?.Close();
            CurrentDialog = this;
            SetActive(true);
        }

        /// <summary>
        /// Closes the editor dialog.
        /// </summary>
        public virtual void Close()
        {
            CurrentDialog = null;
            SetActive(false);
        }

        /// <summary>
        /// Sets the active state of the editor dialog.
        /// </summary>
        /// <param name="active">The active state to set.</param>
        public virtual void SetActive(bool active)
        {
            if (GameObject)
                GameObject.SetActive(active);

            if (!active)
                MouseOver = false;
        }

        /// <summary>
        /// Initializes the editor dialog with a name.
        /// </summary>
        /// <param name="name">Name to initialize.</param>
        public virtual void InitDialog(string name)
        {
            Name = name;
            GameObject = GetLegacyDialog().Dialog.gameObject;
            SetupHover();
        }

        /// <summary>
        /// Gets the original dialog.
        /// </summary>
        /// <returns>Returns the vanilla dialog class.</returns>
        public EditorManager.EditorDialog GetLegacyDialog() => EditorManager.inst.GetDialog(Name);

        /// <summary>
        /// Sets up the mouse hover controller.
        /// </summary>
        public virtual void SetupHover()
        {
            if (!GameObject)
                return;

            var clickable = GameObject.AddComponent<Clickable>();
            clickable.onEnter = eventData => MouseOver = true;
            clickable.onExit = eventData => MouseOver = false;
        }

        public override string ToString() => Name;

        #endregion
    }
}
