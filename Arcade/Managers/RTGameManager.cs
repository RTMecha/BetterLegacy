using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Managers;
using LSFunctions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

using Axis = BetterLegacy.Editor.Components.SelectObject.Axis;

namespace BetterLegacy.Arcade.Managers
{
    public class RTGameManager : MonoBehaviour
    {
        public static RTGameManager inst;

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
            checkpointAnimParent = GameManager.inst.CheckpointAnimator.transform;
            checkpointAnimTop = checkpointAnimParent.Find("top").GetComponent<Image>();
            checkpointAnimBottom = checkpointAnimParent.Find("bottom").GetComponent<Image>();
            checkpointAnimLeft = checkpointAnimParent.Find("left").GetComponent<Image>();
            checkpointAnimRight = checkpointAnimParent.Find("right").GetComponent<Image>();

            GameManager.inst.CheckpointAnimator.enabled = false;

            InitCheckpointAnimation();

            if (!CoreHelper.InEditor)
                return;

            objectDragger = Creator.NewGameObject("Dragger", GameManager.inst.transform.parent).transform;

            var rotator = ObjectManager.inst.objectPrefabs[1].options[4].transform.GetChild(0).gameObject.Duplicate(objectDragger, "Rotator");
            Destroy(rotator.GetComponent<SelectObjectInEditor>());
            rotator.tag = Tags.HELPER;
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
            if (GameManager.inst && GameManager.inst.introMain && GameManager.inst.introMain.transform.TryFind("blur", out Transform blur))
                blur.gameObject.SetActive(false);

            if (!CoreHelper.InEditor)
                return;

            objectDragger.gameObject.SetActive(SelectObject.Enabled && CoreHelper.InEditor && EditorManager.inst.isEditing &&
                EditorTimeline.inst.SelectedObjectCount == 1 &&
                (EditorTimeline.inst.CurrentSelection.isBeatmapObject && EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>().objectType != BeatmapObject.ObjectType.Empty || EditorTimeline.inst.CurrentSelection.isPrefabObject));
        }

        SelectObjectScaler CreateScaler(Axis axis, Color color)
        {
            var scaler = ObjectManager.inst.objectPrefabs[3].options[0].transform.GetChild(0).gameObject.Duplicate(objectDragger, "Scaler");
            Destroy(scaler.GetComponent<SelectObjectInEditor>());
            scaler.tag = Tags.HELPER;
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

        /// <summary>
        /// Sets the camera's render area.
        /// </summary>
        /// <param name="rect">Rect to set.</param>
        public void SetCameraArea(Rect rect)
        {
            EventManager.inst.cam.rect = rect;
            EventManager.inst.camPer.rect = rect;
        }

        #region Checkpoints

        /// <summary>
        /// The currently activated checkpoint.
        /// </summary>
        public Checkpoint ActiveCheckpoint { get; set; }

        List<Checkpoint> Checkpoints => GameData.Current?.data?.checkpoints;

        int nextCheckpointIndex;

        RTAnimation checkpointAnimation;

        #region Methods

        /// <summary>
        /// Updates the checkpoint conditions.
        /// </summary>
        public void UpdateCheckpoints()
        {
            var checkpoints = Checkpoints;
            if (checkpoints != null && nextCheckpointIndex >= 0 && nextCheckpointIndex < checkpoints.Count && AudioManager.inst.CurrentAudioSource.time > (double)checkpoints[nextCheckpointIndex].time && CoreHelper.InEditorPreview)
                SetCheckpoint(nextCheckpointIndex);
        }

        /// <summary>
        /// Creates a new checkpoint and sets it as the currently active checkpoint. Used for modifiers.
        /// </summary>
        /// <param name="time">Time of the checkpoint to rewind to when reversing to it.</param>
        /// <param name="position">Position to spawn the players at.</param>
        public void SetCheckpoint(float time, Vector2 position) => SetCheckpoint(new Checkpoint(false, "Modifier Checkpoint", Mathf.Clamp(time, 0f, AudioManager.inst.CurrentAudioSource.clip.length), position));

        /// <summary>
        /// Sets the currently active checkpoint based on an index.
        /// </summary>
        /// <param name="index">Index of the checkpoint.</param>
        /// <param name="playAnimation">If the animation should play.</param>
        /// <param name="spawnPlayers">If players should be respawned.</param>
        public void SetCheckpoint(int index, bool playAnimation = true, bool spawnPlayers = true)
        {
            var checkpoints = Checkpoints;
            if (index < 0 || index >= checkpoints.Count)
                return;

            CoreHelper.Log($"Set checkpoint: {index}");
            SetCheckpoint(checkpoints[index], index + 1, playAnimation, spawnPlayers);
        }

        /// <summary>
        /// Sets the currently active checkpoint based on an index.
        /// </summary>
        /// <param name="checkpoint">Checkpoint to set active.</param>
        /// <param name="nextIndex">Index of the next checkpoint to activate. If left at -1, it will not update the next index.</param>
        /// <param name="playAnimation">If the animation should play.</param>
        /// <param name="spawnPlayers">If players should be respawned.</param>
        public void SetCheckpoint(Checkpoint checkpoint, int nextIndex = -1, bool playAnimation = true, bool spawnPlayers = true)
        {
            ActiveCheckpoint = checkpoint;
            if (nextIndex >= 0)
                nextCheckpointIndex = nextIndex;

            if (spawnPlayers)
                PlayerManager.SpawnPlayers(ActiveCheckpoint.pos);
            if (playAnimation)
            {
                GameManager.inst.playingCheckpointAnimation = true;
                StartCoroutine(IPlayCheckpointAnimation());
            }
        }

        /// <summary>
        /// Resets the active checkpoint.
        /// </summary>
        /// <param name="baseOnTime">If true, reset to last checkpoint. Otherwise, reset to first.</param>
        public void ResetCheckpoint(bool baseOnTime = false)
        {
            if (Checkpoints == null || (CoreHelper.InEditor && !EditorManager.inst.hasLoadedLevel))
                return;

            CoreHelper.Log($"Reset Checkpoints | Based on time: {baseOnTime}");
            int index = 0;
            if (baseOnTime)
                index = GameData.Current.data.GetLastCheckpointIndex();

            ActiveCheckpoint = Checkpoints[index];
            nextCheckpointIndex = index + 1;
        }

        /// <summary>
        /// Reverses to the active checkpoint.
        /// </summary>
        public void ReverseToCheckpoint() => CoreHelper.StartCoroutine(IReverseToCheckpoint());

        /// <summary>
        /// Plays the checkpoint sound and animation.
        /// </summary>
        public IEnumerator IPlayCheckpointAnimation()
        {
            if (CoreConfig.Instance.PlayCheckpointSound.Value)
                SoundManager.inst.PlaySound(DefaultSounds.checkpoint);
            if (CoreConfig.Instance.PlayCheckpointAnimation.Value)
            {
                GameManager.inst.playingCheckpointAnimation = true;
                levelAnimationController.Play(checkpointAnimation);
            }

            yield break;
        }

        IEnumerator IReverseToCheckpoint()
        {
            if (GameManager.inst.isReversing)
                yield break;

            GameManager.inst.playingCheckpointAnimation = true;
            GameManager.inst.isReversing = true;

            var checkpoint = ActiveCheckpoint ?? GameData.Current.data.GetLastCheckpoint();

            var animation = new RTAnimation("Reverse");
            animation.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, AudioManager.inst.CurrentAudioSource.pitch, Ease.Linear),
                    new FloatKeyframe(1f, -1.5f, Ease.CircIn)
                }, x =>
                {
                    if (AudioManager.inst.CurrentAudioSource.time > 1f)
                        AudioManager.inst.SetPitch(x);
                    else
                        AudioManager.inst.SetMusicTime(1f);
                }),
            };

            animation.onComplete = () => AnimationManager.inst.Remove(animation.id);

            AnimationManager.inst.Play(animation);

            SoundManager.inst.PlaySound(DefaultSounds.rewind);

            yield return new WaitForSeconds(2f);

            float time = Mathf.Clamp(checkpoint.time + 0.01f, 0.1f, AudioManager.inst.CurrentAudioSource.clip.length);
            if (!CoreHelper.InEditor && (PlayerManager.Is1Life || PlayerManager.IsNoHit))
                time = 0.1f;

            AudioManager.inst.SetMusicTime(time);
            GameManager.inst.gameState = GameManager.State.Playing;

            AudioManager.inst.CurrentAudioSource.Play();
            AudioManager.inst.SetPitch(CoreHelper.Pitch);

            GameManager.inst.UpdateEventSequenceTime();
            GameManager.inst.isReversing = false;

            yield return new WaitForSeconds(0.1f);

            PlayerManager.SpawnPlayers(checkpoint.pos);
            GameManager.inst.playingCheckpointAnimation = false;
            checkpoint = null;

            yield break;
        }

        void InitCheckpointAnimation()
        {
            checkpointAnimTop.rectTransform.sizeDelta = Vector2.zero;
            checkpointAnimBottom.rectTransform.sizeDelta = Vector2.zero;
            checkpointAnimLeft.rectTransform.sizeDelta = Vector2.zero;
            checkpointAnimRight.rectTransform.sizeDelta = Vector2.zero;
            checkpointAnimation = new RTAnimation("Got Checkpoint");
            checkpointAnimation.animationHandlers = new List<AnimationHandlerBase>
            {
                CheckpointAnimationHandler(checkpointAnimTop, true),
                CheckpointAnimationHandler(checkpointAnimBottom, true),
                CheckpointAnimationHandler(checkpointAnimLeft, false),
                CheckpointAnimationHandler(checkpointAnimRight, false),
            };
            checkpointAnimation.onComplete = () =>
            {
                levelAnimationController.Remove(checkpointAnimation.id);
                GameManager.inst.playingCheckpointAnimation = false;
            };
        }

        AnimationHandler<float> CheckpointAnimationHandler(Image image, bool vertical) => new AnimationHandler<float>(new List<IKeyframe<float>>
        {
            new FloatKeyframe(0f, 0f, Ease.Linear),
            new FloatKeyframe(0.05f, 12f, Ease.SineOut),
            new FloatKeyframe(0.08f, 8f, Ease.SineInOut),
            new FloatKeyframe(0.15f, 0f, Ease.SineIn),
        }, x =>
        {
            image.color = ThemeManager.inst.Current.guiColor;
            image.rectTransform.sizeDelta = new Vector2(!vertical ? x : 0f, vertical ? x : 0f);
        }, interpolateOnComplete: true);

        #endregion

        #endregion

        #region Scene Assets

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

        public Transform checkpointAnimParent;
        public Image checkpointAnimTop;
        public Image checkpointAnimBottom;
        public Image checkpointAnimLeft;
        public Image checkpointAnimRight;

        public Dictionary<string, object> assets = new Dictionary<string, object>();

        #endregion

        #region Level Animations

        /// <summary>
        /// The main animation controller for the level.
        /// </summary>
        public AnimationController levelAnimationController;

        static RTAnimation introAnimation;

        public void PlayIntro()
        {
            GameManager.inst.introAnimator.enabled = false;
            levelAnimationController.Play(IntroAnimation);
            doIntroFadeInternal = doIntroFade;
        }

        const float INTRO_SPEED = 0.3f;

        public static bool doIntroFade = true;
        bool doIntroFadeInternal = true;

        public static void SetIntroBGOpacity(float x)
        {
            if (GameManager.inst && GameManager.inst.introBG)
                GameManager.inst.introBG.color = LSColors.fadeColor(GameManager.inst.introBG.color, inst.doIntroFadeInternal ? x : 0f);
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
                                new FloatKeyframe(4f * INTRO_SPEED, 0f, Ease.SineIn),
                                new FloatKeyframe(15.6666667f * INTRO_SPEED, 0f, Ease.Linear),
                            }, SetIntroBGOpacity),

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
                    introAnimation.onComplete = () =>
                    {
                        AnimationManager.inst.Remove(introAnimation.id);
                        SetIntroBGOpacity(0f);
                        SetIntroTitlePosition(new Vector2(-1000f, 80f));
                        SetIntroArtistPosition(new Vector2(-1000f, 32f));
                    };
                }
                return introAnimation;

            }
        }

        #endregion
    }
}
