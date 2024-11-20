using BetterLegacy.Components;
using BetterLegacy.Components.Editor;
using BetterLegacy.Configs;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor;
using BetterLegacy.Editor.Managers;
using LSFunctions;
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

            levelAnimationController = gameObject.AddComponent<AnimationController>();

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

            objectDragger.gameObject.SetActive(SelectObject.Enabled && CoreHelper.InEditor && EditorManager.inst.isEditing &&
                ObjectEditor.inst.SelectedObjectCount == 1 &&
                (ObjectEditor.inst.CurrentSelection.IsBeatmapObject && ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>().objectType != BeatmapObject.ObjectType.Empty || ObjectEditor.inst.CurrentSelection.IsPrefabObject));
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

        /// <summary>
        /// The main animation controller for the level.
        /// </summary>
        public AnimationController levelAnimationController;

        static RTAnimation introAnimation;

        public void PlayIntro()
        {
            GameManager.inst.introAnimator.enabled = false;
            levelAnimationController.Play(IntroAnimation);
        }

        const float INTRO_SPEED = 0.3f;

        public static void SetIntroBGOpacity(float x)
        {
            if (GameManager.inst && GameManager.inst.introBG)
                GameManager.inst.introBG.color = LSColors.fadeColor(GameManager.inst.introBG.color, x);
        }

        public static void SetIntroBlurActive(float x)
        {
            if (GameManager.inst && GameManager.inst.introMain && GameManager.inst.introMain.transform.TryFind("blur", out Transform blur))
                blur.gameObject.SetActive(x != 0f);
        }

        public static void SetIntroTitlePosition(Vector2 pos)
        {
            if (GameManager.inst && GameManager.inst.introTitle && GameManager.inst.introTitle.rectTransform)
                GameManager.inst.introTitle.rectTransform.anchoredPosition = pos;
        }

        public static void SetIntroArtistPosition(Vector2 pos)
        {
            if (GameManager.inst && GameManager.inst.introArtist && GameManager.inst.introArtist.rectTransform)
                GameManager.inst.introArtist.rectTransform.anchoredPosition = pos;
        }

        /// <summary>
        /// Mod version of the level intro.
        /// </summary>
        public static RTAnimation IntroAnimation
        {
            get
            {
                if (introAnimation == null)
                {
                    introAnimation = new RTAnimation("INTRO ANIMATION")
                    {
                        animationHandlers = new List<AnimationHandlerBase>
                        {
                            new AnimationHandler<float>(new List<IKeyframe<float>>
                            {
                                // Fade In
                                new FloatKeyframe(0f, 1f, Ease.Linear),
                                new FloatKeyframe(0.41666666f * INTRO_SPEED, 1f, Ease.Linear),
                                new FloatKeyframe(1.5f * INTRO_SPEED, 0f, Ease.SineIn),
                            }, SetIntroBGOpacity),
                            new AnimationHandler<float>(new List<IKeyframe<float>>
                            {
                                // Fade In
                                new FloatKeyframe(0f, 1f, Ease.Linear),
                                new FloatKeyframe(0.29166666f * INTRO_SPEED, 0f, Ease.Instant),
                                new FloatKeyframe(0.3f * INTRO_SPEED, 0f, Ease.Linear),
                            }, SetIntroBlurActive),

                            new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                            {
                                // Fade In
                                new Vector2Keyframe(0f, new Vector2(-1000f, 80f), Ease.Linear),
                                new Vector2Keyframe(1.5f * INTRO_SPEED, new Vector2(36f, 80f), Ease.BackOut),
                                // Fade Out
                                new Vector2Keyframe(15f * INTRO_SPEED, new Vector2(36f, 80f), Ease.Linear),
                                new Vector2Keyframe(15.6666667f * INTRO_SPEED, new Vector2(-1000f, 80f), Ease.BackIn),
                            }, SetIntroTitlePosition),
                            new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                            {
                                // Fade In
                                new Vector2Keyframe(0f, new Vector2(-1000f, 32f), Ease.Linear),
                                new Vector2Keyframe(0.8333333f * INTRO_SPEED, new Vector2(-1000f, 32f), Ease.Linear),
                                new Vector2Keyframe(1.5f * INTRO_SPEED, new Vector2(36f, 32f), Ease.BackOut),
                                // Fade Out
                                new Vector2Keyframe(15f * INTRO_SPEED, new Vector2(36f, 32f), Ease.Linear),
                                new Vector2Keyframe(15.6666667f * INTRO_SPEED, new Vector2(-1000f, 32f), Ease.BackIn),
                            }, SetIntroArtistPosition),
                        }
                    };
                    introAnimation.onComplete = () => AnimationManager.inst.Remove(introAnimation.id);
                }
                return introAnimation;

            }
        }
    }
}
