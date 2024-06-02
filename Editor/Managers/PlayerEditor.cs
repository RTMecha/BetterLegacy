using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using BetterLegacy.Core;
using LSFunctions;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// The new Player editor.
    /// </summary>
    public class PlayerEditor : MonoBehaviour
    {
        public static PlayerEditor inst;

        public static void Init() => Creator.NewGameObject("PlayerEditor", EditorManager.inst.transform.parent).AddComponent<PlayerEditor>();

        void Awake()
        {
            inst = this;
            StartCoroutine(GenerateUI());
        }

        public enum Tab
        {
            Base, // includes stretch
            GUI,
            Boost,
            Pulse,
            Bullet,
            Tail, // All tail related parts go here
            CustomObjects
        }

        public IEnumerator GenerateUI()
        {
            var dialog = EditorManager.inst.GetDialog("Multi Keyframe Editor (Object)").Dialog.gameObject.Duplicate(EditorManager.inst.dialogs, "PlayerEditorDialog");

            dialog.transform.localScale = Vector3.one;
            dialog.transform.position = new Vector3(1537.5f, 714.945f, 0f) * EditorManager.inst.ScreenScale;
            dialog.transform.AsRT().sizeDelta = new Vector2(0f, 32f);

            var dialogTitle = dialog.transform.GetChild(0);
            dialogTitle.GetComponent<Image>().color = LSColors.HexToColor("E57373");
            dialogTitle.GetChild(0).GetComponent<Text>().text = "- Player Editor -";

            dialog.transform.GetChild(1).AsRT().sizeDelta = new Vector2(765f, 54f);

            yield break;
        }

        public IEnumerator Refresh()
        {
            for (int i = 0; i < editorUIs.Count; i++)
            {
                var ui = editorUIs[i];
                var active = CoreHelper.SearchString(searchTerm, ui.Name) && ui.Tab == CurrentTab;
                ui.GameObject.SetActive(active);
                if (!active)
                    continue;


            }

            yield break;
        }

        public void Save()
        {

        }

        public void Load()
        {

        }

        public Tab CurrentTab { get; set; }
        public string searchTerm;
        public List<PlayerEditorUI> editorUIs = new List<PlayerEditorUI>();

        public class PlayerEditorUI
        {
            public string Name { get; set; }
            public GameObject GameObject { get; set; }
            public Tab Tab { get; set; }
            public RTEditor.EditorProperty.ValueType ValueType { get; set; }
        }
    }
}
