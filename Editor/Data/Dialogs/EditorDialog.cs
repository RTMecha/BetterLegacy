using BetterLegacy.Core.Data;
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
    // todo: replace original editor dialog with this

    /*
     
    DIALOGS:
    Multi Object Editor
    Object Editor = done
    Event Editor
    Checkpoint Editor
    Background Editor
    Metadata Editor
    Prefab Editor
    Settings Editor
    Multi Keyframe Editor
    Multi Keyframe Editor (Object) = ignore
    Marker Editor
    Prefab Selector
    
    New Level Template Dialog
    Screenshot Dialog
    Keybind Editor
    Player Editor
    Level Combiner
    Uploaded Dialog
    Documentation Dialog
    Prefab External Dialog
     */

    public class EditorDialog : Exists
    {
        public EditorDialog() { }

        public EditorDialog(string name)
        {
            Name = name;
            GameObject = GetLegacyDialog().Dialog.gameObject;
        }

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

        #endregion

        #region Methods

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
            //EditorManager.inst.SetDialogStatus(Name, active);
        }

        /// <summary>
        /// Gets the original dialog.
        /// </summary>
        /// <returns>Returns the vanilla dialog class.</returns>
        public EditorManager.EditorDialog GetLegacyDialog() => EditorManager.inst.GetDialog(Name);

        public override string ToString() => Name;

        #endregion
    }
}
