using BetterLegacy.Components.Editor;
using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor;
using BetterLegacy.Editor.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

using Axis = BetterLegacy.Components.Editor.SelectObject.Axis;

namespace BetterLegacy.Core.Managers
{
    public class GameStorageManager : MonoBehaviour
    {
        public static GameStorageManager inst;

        void Awake()
        {
            inst = this;
            postProcessLayer = Camera.main.gameObject.GetComponent<PostProcessLayer>();
            extraBG = GameObject.Find("ExtraBG").transform;
            video = extraBG.GetChild(0);

            timelinePlayer = GameManager.inst.timeline.transform.Find("Base/position").GetComponent<Image>();
            timelineLeftCap = GameManager.inst.timeline.transform.Find("Base/Image").GetComponent<Image>();
            timelineRightCap = GameManager.inst.timeline.transform.Find("Base/Image 1").GetComponent<Image>();
            timelineLine = GameManager.inst.timeline.transform.Find("Base").GetComponent<Image>();

            if (!CoreHelper.InEditor)
                return;

            objectDragger = Creator.NewGameObject("Dragger", GameManager.inst.transform.parent).transform;

            var rotator = ObjectManager.inst.objectPrefabs[1].options[4].transform.GetChild(0).gameObject.Duplicate(objectDragger, "Rotator");
            Destroy(rotator.GetComponent<SelectObjectInEditor>());
            rotator.tag = "Helper";
            rotator.transform.localScale = new Vector3(2f, 2f, 1f);
            var rotatorRenderer = rotator.GetComponent<Renderer>();
            rotatorRenderer.enabled = true;
            rotatorRenderer.material.color = new Color(0f, 0f, 1f);
            rotator.GetComponent<Collider2D>().enabled = true;
            objectRotator = rotator.AddComponent<SelectObjectRotator>();

            rotator.SetActive(true);

            objectDragger.gameObject.SetActive(false);

            objectScalerTop = CreateScaler(Axis.PosY, Color.green);
            objectScalerLeft = CreateScaler(Axis.PosX, Color.red);
            objectScalerBottom = CreateScaler(Axis.NegY, Color.green);
            objectScalerRight = CreateScaler(Axis.NegX, Color.red);
        }

        void Update()
        {
            if (!CoreHelper.InEditor)
                return;

            objectDragger.gameObject.SetActive(CoreHelper.InEditor && EditorManager.inst.isEditing &&
                ObjectEditor.inst.SelectedObjectCount == 1 &&
                (ObjectEditor.inst.CurrentSelection.IsBeatmapObject && ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>().objectType != BeatmapObject.ObjectType.Empty || ObjectEditor.inst.CurrentSelection.IsPrefabObject) &&
                SelectObject.Enabled);
        }

        SelectObjectScaler CreateScaler(Axis axis, Color color)
        {
            var scaler = ObjectManager.inst.objectPrefabs[3].options[0].transform.GetChild(0).gameObject.Duplicate(objectDragger, "Scaler");
            Destroy(scaler.GetComponent<SelectObjectInEditor>());
            scaler.tag = "Helper";
            scaler.transform.localScale = new Vector3(2f, 2f, 1f);
            scaler.GetComponent<Collider2D>().enabled = true;

            var scalerRenderer = scaler.GetComponent<Renderer>();
            scalerRenderer.enabled = true;
            scalerRenderer.material.color = color;

            var s = scaler.AddComponent<SelectObjectScaler>();
            s.axis = axis;

            scaler.SetActive(true);

            return s;
        }

        public SelectObjectRotator objectRotator;
        public SelectObjectScaler objectScalerTop;
        public SelectObjectScaler objectScalerLeft;
        public SelectObjectScaler objectScalerRight;
        public SelectObjectScaler objectScalerBottom;

        public Transform objectDragger;

        public Image timelinePlayer;
        public Image timelineLine;
        public Image timelineLeftCap;
        public Image timelineRightCap;
        public List<Image> checkpointImages = new List<Image>();
        public PostProcessLayer postProcessLayer;
        public Transform extraBG;
        public Transform video;

        public Dictionary<string, object> assets = new Dictionary<string, object>();
    }
}
