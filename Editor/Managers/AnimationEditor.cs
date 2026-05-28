using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Settings;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Popups;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Editor class that manages custom animations.
    /// </summary>
    public class AnimationEditor : BaseManager<AnimationEditor, EditorManagerSettings>
    {
        /*
         TOOD:
        - Global animation library
         */

        #region Values

        /// <summary>
        /// Dialog of the editor.
        /// </summary>
        public AnimationEditorDialog Dialog { get; set; }

        /// <summary>
        /// Popup list of the editor.
        /// </summary>
        public ContentPopup Popup { get; set; }

        /// <summary>
        /// Currently selected animation.
        /// </summary>
        public PAAnimation CurrentAnimation { get; set; }

        /// <summary>
        /// The current list of animation groups.
        /// </summary>
        public List<AnimationGroup> CurrentAnimationListGroup { get; set; }

        /// <summary>
        /// The current list of animations.
        /// </summary>
        public List<PAAnimation> CurrentAnimationList { get; set; }

        /// <summary>
        /// Copied list of animation groups.
        /// </summary>
        public List<AnimationGroup> copiedAnimationGroups = new List<AnimationGroup>();

        /// <summary>
        /// Copied list of animations.
        /// </summary>
        public List<PAAnimation> copiedAnimations = new List<PAAnimation>();

        /// <summary>
        /// Currently selected object to apply animations to.
        /// </summary>
        public ITransformable CurrentObject { get; set; }

        /// <summary>
        /// Current interpolated time.
        /// </summary>
        public float CurrentTime
        {
            get => Dialog.Timeline.Cursor.value;
            set => Dialog.Timeline.Cursor.value = value;
        }

        /// <summary>
        /// Function to run when the return button is clicked.
        /// </summary>
        public Action currentOnReturn;

        /// <summary>
        /// Dynamic sequence for interpolated time.
        /// </summary>
        public RTTimer sequence;

        /// <summary>
        /// If the editor timeline is automatically playing.
        /// </summary>
        public bool playing;

        #endregion

        #region Functions

        public override void OnInit()
        {
            try
            {
                Dialog = new AnimationEditorDialog();
                Dialog.Init();

                // clear cached values when the editor closes.
                Dialog.GameObject.AddComponent<ActiveState>().onStateChanged = enabled =>
                {
                    if (enabled)
                        return;

                    playing = false;
                    CurrentObject?.ResetOffsets();
                    CurrentObject = null;
                    CurrentAnimation = null;
                };

                Popup = RTEditor.inst.GeneratePopup(EditorPopup.ANIMATIONS_POPUP, "Animations", Vector2.zero, new Vector2(600f, 400f));
                Popup.onRender = () =>
                {
                    if (AssetPack.TryReadFromFile("editor/ui/popups/animations_popup.json", out string uiFile))
                    {
                        var jn = JSON.Parse(uiFile);
                        RectValues.TryParse(jn["base"]["rect"], RectValues.Default.SizeDelta(600f, 400f)).AssignToRectTransform(Popup.GameObject.transform.AsRT());
                        RectValues.TryParse(jn["top_panel"]["rect"], RectValues.FullAnchored.AnchorMin(0, 1).Pivot(0f, 0f).SizeDelta(32f, 32f)).AssignToRectTransform(Popup.TopPanel);
                        RectValues.TryParse(jn["search"]["rect"], new RectValues(Vector2.zero, Vector2.one, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 32f))).AssignToRectTransform(Popup.GameObject.transform.Find("search-box").AsRT());
                        RectValues.TryParse(jn["scrollbar"]["rect"], new RectValues(Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(0f, 0.5f), new Vector2(32f, 0f))).AssignToRectTransform(Popup.GameObject.transform.Find("Scrollbar").AsRT());

                        var layoutValues = LayoutValues.Parse(jn["layout"]);
                        if (layoutValues is GridLayoutValues gridLayoutValues)
                            gridLayoutValues.AssignToLayout(Popup.Grid ? Popup.Grid : Popup.GameObject.transform.Find("mask/content").GetComponent<GridLayoutGroup>());

                        if (jn["title"] != null)
                        {
                            Popup.title = jn["title"]["text"] != null ? jn["title"]["text"] : "Animations";

                            var title = Popup.Title;
                            RectValues.TryParse(jn["title"]["rect"], RectValues.FullAnchored.AnchoredPosition(2f, 0f).SizeDelta(-12f, -8f)).AssignToRectTransform(title.rectTransform);
                            title.alignment = jn["title"]["alignment"] != null ? (TextAnchor)jn["title"]["alignment"].AsInt : TextAnchor.MiddleLeft;
                            title.fontSize = jn["title"]["font_size"] != null ? jn["title"]["font_size"].AsInt : 20;
                            title.fontStyle = (FontStyle)jn["title"]["font_style"].AsInt;
                            title.horizontalOverflow = jn["title"]["horizontal_overflow"] != null ? (HorizontalWrapMode)jn["title"]["horizontal_overflow"].AsInt : HorizontalWrapMode.Wrap;
                            title.verticalOverflow = jn["title"]["vertical_overflow"] != null ? (VerticalWrapMode)jn["title"]["vertical_overflow"].AsInt : VerticalWrapMode.Overflow;
                        }

                        if (jn["anim"] != null)
                            Popup.ReadAnimationJSON(jn["anim"]);

                        if (jn["drag_mode"] != null && Popup.Dragger)
                            Popup.Dragger.mode = (DraggableUI.DragMode)jn["drag_mode"].AsInt;
                    }
                };

                // allow for storing level animations.
                EditorHelper.AddEditorDropdown("View Animations", string.Empty, EditorHelper.VIEW_DROPDOWN, EditorSprites.PlaySprite, () =>
                {
                    if (EditorLevelManager.inst.HasLoadedLevel())
                        OpenPopup(GameData.Current.animationGroups, GameData.Current.animations);
                });
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }

        public override void OnTick()
        {
            if (CurrentObject == null || !Dialog.IsCurrent || !CurrentAnimation)
                return;

            // dynamically play the current animation using a timer.
            if (playing)
            {
                sequence.Update();
                if (sequence.time > CurrentAnimation.GetLength())
                {
                    sequence.Reset();
                    sequence.Update();
                    CoreHelper.Log($"Reset");
                }
                CurrentTime = sequence.time;
            }

            CurrentObject.InterpolateAnimation(CurrentAnimation, CurrentTime);
        }

        /// <summary>
        /// Applies animations to the selected objects.
        /// </summary>
        /// <param name="animations">List of animations to apply.</param>
        public void ApplyAnimationsToSelected(List<PAAnimation> animations)
        {
            foreach (var animation in animations)
                ApplyAnimationToSelected(animation);
        }

        /// <summary>
        /// Applies an animation to selected objects that match the animation ID.
        /// </summary>
        /// <param name="animation">Animation to apply.</param>
        /// <param name="update">If sequences should be recached.</param>
        public void ApplyAnimationToSelected(PAAnimation animation)
        {
            foreach (var timelineObject in EditorTimeline.inst.SelectedBeatmapObjects)
            {
                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                if (beatmapObject.animID == animation.ReferenceID)
                {
                    beatmapObject.CopyAnimatableData(animation);
                    timelineObject.RenderPosLength();
                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                }
            }
        }

        /// <summary>
        /// Converts the selected objects to just animations.
        /// </summary>
        /// <param name="animationGroup">Animation group reference.</param>
        public void ConvertSelectedToAnimationGroup(AnimationGroup animationGroup)
        {
            foreach (var timelineObject in EditorTimeline.inst.SelectedBeatmapObjects)
            {
                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                if (string.IsNullOrEmpty(beatmapObject.animID))
                    continue;
                animationGroup.animations.Add(ConvertToAnimation(beatmapObject));
            }
        }

        /// <summary>
        /// Converts the selected objects to just animations.
        /// </summary>
        /// <param name="animations">Animations list.</param>
        public void ConvertSelectedToAnimations(List<PAAnimation> animations)
        {
            foreach (var timelineObject in EditorTimeline.inst.SelectedBeatmapObjects)
                animations.Add(ConvertToAnimation(timelineObject.GetData<BeatmapObject>()));
        }

        /// <summary>
        /// Converts an object to just an animation.
        /// </summary>
        /// <param name="beatmapObject">Beatmap Object to convert.</param>
        /// <returns>Returns an animation based on the Beatmap Object.</returns>
        public PAAnimation ConvertToAnimation(BeatmapObject beatmapObject)
        {
            var animation = new PAAnimation(beatmapObject.name, "This is the default description!");
            animation.CopyAnimatableData(beatmapObject);
            animation.ReferenceID = beatmapObject.animID ?? string.Empty;
            return animation;
        }

        /// <summary>
        /// Creates a new animation.
        /// </summary>
        /// <returns>Returns a newly created animation.</returns>
        public PAAnimation CreateAnimation() => new PAAnimation("New Animation", "This is the default description!");

        /// <summary>
        /// Creates a new animation group.
        /// </summary>
        /// <returns>Returns a newly created animation group.</returns>
        public AnimationGroup CreateAnimationGroup() => new AnimationGroup("New Animation Group", "This is the default description!");

        #region Editor

        /// <summary>
        /// Toggles the playing state.
        /// </summary>
        public void TogglePlaying()
        {
            if (!playing)
                sequence.Reset();

            playing = !playing;
        }

        /// <summary>
        /// Starts playing the current animation.
        /// </summary>
        public void Play()
        {
            sequence.Reset();
            playing = true;
        }

        /// <summary>
        /// Stops playing the current animation.
        /// </summary>
        public void Stop() => playing = false;

        /// <summary>
        /// Opens the editor dialog and sets the animation to edit.
        /// </summary>
        /// <param name="animation">Animation to edit.</param>
        /// <param name="onReturn">Function to run when the return button is clicked.</param>
        public void OpenDialog(PAAnimation animation, Action onReturn = null)
        {
            Dialog.Open();
            RenderDialog(animation, onReturn);
        }

        /// <summary>
        /// Renders the editor dialog and sets the animation to edit.
        /// </summary>
        /// <param name="animation">Animation to edit.</param>
        /// <param name="onReturn">Function to run when the return button is clicked.</param>
        public void RenderDialog(PAAnimation animation, Action onReturn = null)
        {
            currentOnReturn = onReturn;

            CurrentAnimation = animation;

            Dialog.ReturnButton.gameObject.SetActive(onReturn != null);
            Dialog.ReturnButton.onClick.NewListener(() => onReturn?.Invoke());

            Dialog.IDText.text = $"ID: {animation.id}";
            var idClickable = Dialog.IDBase.gameObject.GetOrAddComponent<Clickable>();
            idClickable.onClick = pointerEventData =>
            {
                EditorManager.inst.DisplayNotification($"Copied ID from {animation.name}!", 2f, EditorManager.NotificationType.Success);
                LSText.CopyToClipboard(animation.id);
            };

            Dialog.ReferenceField.SetTextWithoutNotify(animation.ReferenceID);
            Dialog.ReferenceField.onValueChanged.NewListener(_val => animation.ReferenceID = _val);
            EditorContextMenu.AddContextMenu(Dialog.ReferenceField.gameObject,
                new ButtonElement(PlayerModel.IDLE_ANIM, () => Dialog.ReferenceField.text = PlayerModel.IDLE_ANIM),
                new ButtonElement(PlayerModel.SPAWN_ANIM, () => Dialog.ReferenceField.text = PlayerModel.SPAWN_ANIM),
                new ButtonElement(PlayerModel.BOOST_ANIM, () => Dialog.ReferenceField.text = PlayerModel.BOOST_ANIM),
                new ButtonElement(PlayerModel.HEAL_ANIM, () => Dialog.ReferenceField.text = PlayerModel.HEAL_ANIM),
                new ButtonElement(PlayerModel.HIT_ANIM, () => Dialog.ReferenceField.text = PlayerModel.HIT_ANIM),
                new ButtonElement(PlayerModel.DEATH_ANIM, () => Dialog.ReferenceField.text = PlayerModel.DEATH_ANIM),
                new ButtonElement(PlayerModel.SHOOT_ANIM, () => Dialog.ReferenceField.text = PlayerModel.SHOOT_ANIM),
                new ButtonElement(PlayerModel.JUMP_ANIM, () => Dialog.ReferenceField.text = PlayerModel.JUMP_ANIM));

            Dialog.NameField.SetTextWithoutNotify(animation.name);
            Dialog.NameField.onValueChanged.NewListener(_val => animation.name = _val);
            Dialog.DescriptionField.SetTextWithoutNotify(animation.description);
            Dialog.DescriptionField.onValueChanged.NewListener(_val => animation.description = _val);

            Dialog.StartTimeField.SetTextWithoutNotify(animation.StartTime.ToString());
            Dialog.StartTimeField.OnValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                    animation.StartTime = num;
            });

            TriggerHelper.IncreaseDecreaseButtons(Dialog.StartTimeField, max: float.MaxValue);
            TriggerHelper.AddEventTriggers(Dialog.StartTimeField.gameObject, TriggerHelper.ScrollDelta(Dialog.StartTimeField.inputField, max: float.MaxValue));

            Dialog.AnimatePositionToggle.SetIsOnWithoutNotify(animation.animatePosition);
            Dialog.AnimatePositionToggle.onValueChanged.NewListener(_val => animation.animatePosition = _val);
            Dialog.AnimateScaleToggle.SetIsOnWithoutNotify(animation.animateScale);
            Dialog.AnimateScaleToggle.onValueChanged.NewListener(_val => animation.animateScale = _val);
            Dialog.AnimateRotationToggle.SetIsOnWithoutNotify(animation.animateRotation);
            Dialog.AnimateRotationToggle.onValueChanged.NewListener(_val => animation.animateRotation = _val);
            Dialog.AnimateColorToggle.SetIsOnWithoutNotify(animation.animateColor);
            Dialog.AnimateColorToggle.onValueChanged.NewListener(_val => animation.animateColor = _val);
            Dialog.TransitionToggle.SetIsOnWithoutNotify(animation.transition);
            Dialog.TransitionToggle.onValueChanged.NewListener(_val => animation.transition = _val);

            RenderCustomUIDisplay(animation);

            Dialog.Timeline.RenderKeyframes(animation);
            Dialog.Timeline.RenderDialog(animation);
            Dialog.Timeline.ResizeKeyframeTimeline(animation);
        }

        /// <summary>
        /// Renders the custom keyframe UI.
        /// </summary>
        public void RenderCustomUIDisplay(PAAnimation animation)
        {
            Dialog.keyframeDialogs[0].InitCustomUI(
                animation.EditorData.GetDisplay("position/x", CustomValueDisplay.DefaultPositionXDisplay),
                animation.EditorData.GetDisplay("position/y", CustomValueDisplay.DefaultPositionYDisplay),
                animation.EditorData.GetDisplay("position/z", CustomValueDisplay.DefaultPositionZDisplay));

            Dialog.KeyframeDialogs[0].EventValuesParent.AsRT().sizeDelta = new Vector2(553f, 32f);
            var grp = Dialog.KeyframeDialogs[0].EventValuesParent.gameObject.GetComponent<GridLayoutGroup>();
            grp.cellSize = new Vector2(EditorHelper.CheckComplexity(EditorHelper.GetComplexity("position_keyframe/z_axis", Complexity.Advanced)) ? 122f : 183f, 40f);
            LayoutRebuilder.ForceRebuildLayoutImmediate(Dialog.KeyframeDialogs[0].EventValuesParent.AsRT());

            Dialog.keyframeDialogs[1].InitCustomUI(
                animation.EditorData.GetDisplay("scale/x", CustomValueDisplay.DefaultScaleXDisplay),
                animation.EditorData.GetDisplay("scale/y", CustomValueDisplay.DefaultScaleYDisplay));
            Dialog.keyframeDialogs[2].InitCustomUI(
                animation.EditorData.GetDisplay("rotation/x", CustomValueDisplay.DefaultRotationXDisplay),
                animation.EditorData.GetDisplay("rotation/y", CustomValueDisplay.DefaultRotationYDisplay),
                animation.EditorData.GetDisplay("rotation/z", CustomValueDisplay.DefaultRotationZDisplay));
        }

        #endregion

        #region List

        /// <summary>
        /// Renders the animation list popup.
        /// </summary>
        public void RenderPopup() => Popup.SearchField.onValueChanged.Invoke(Popup.SearchTerm);

        /// <summary>
        /// Opens the animation list popup.
        /// </summary>
        /// <param name="animationGroups">List of animation groups to display.</param>
        /// <param name="animations">List of animations to display.</param>
        /// <param name="onPlay">Function to run when the user wants to play the animation.</param>
        public void OpenPopup(List<AnimationGroup> animationGroups, List<PAAnimation> animations, Action<PAAnimation> onPlay = null, Action<PAAnimation> onSelect = null, ITransformable currentObject = null, Action onReturn = null)
        {
            Popup.Open();
            RenderPopup(animationGroups, animations, onPlay, onSelect, currentObject, onReturn);
        }

        /// <summary>
        /// Opens the animation list popup.
        /// </summary>
        /// <param name="animations">List of animations to display.</param>
        /// <param name="onPlay">Function to run when the user wants to play the animation.</param>
        public void OpenPopup(List<PAAnimation> animations, Action<PAAnimation> onPlay = null, Action<PAAnimation> onSelect = null, ITransformable currentObject = null, Action onReturn = null)
        {
            Popup.Open();
            RenderPopup(animations, onPlay, onSelect, currentObject, onReturn);
        }

        /// <summary>
        /// Renders the animation list popup.
        /// </summary>
        /// <param name="animations">List of animations to display.</param>
        /// <param name="onPlay">Function to run when the user wants to play the animation.</param>
        /// <param name="onSelect">Function to run when the animation is selected.</param>
        /// <param name="currentObject">The current transformable object reference to play the animation onto.</param>
        /// <param name="onReturn">Function to run when the user wants to return from the animation editor.</param>
        public void RenderPopup(List<PAAnimation> animations, Action<PAAnimation> onPlay = null, Action<PAAnimation> onSelect = null, ITransformable currentObject = null, Action onReturn = null) => RenderPopup(null, animations, onPlay, onSelect, currentObject, onReturn);

        /// <summary>
        /// Renders the animation list popup.
        /// </summary>
        /// <param name="animationGroups">List of animation groups to display.</param>
        /// <param name="animations">List of animations to display.</param>
        /// <param name="onPlay">Function to run when the user wants to play the animation.</param>
        /// <param name="onSelect">Function to run when the animation is selected.</param>
        /// <param name="currentObject">The current transformable object reference to play the animation onto.</param>
        /// <param name="onReturn">Function to run when the user wants to return from the animation editor.</param>
        public void RenderPopup(List<AnimationGroup> animationGroups, List<PAAnimation> animations, Action<PAAnimation> onPlay = null, Action<PAAnimation> onSelect = null, ITransformable currentObject = null, Action onReturn = null)
        {
            CurrentAnimationListGroup = animationGroups;
            CurrentAnimationList = animations;
            CurrentObject = currentObject;

            Popup.ClearContent();
            Popup.SearchField.onValueChanged.NewListener(_val => RenderPopup(animationGroups, animations, onPlay, onSelect, currentObject, onReturn));

            EditorContextMenu.AddContextMenu(Popup.GameObject,
                new ButtonElement("Create Animation", () =>
                {
                    animations.Add(CreateAnimation());
                    RenderPopup();
                }),
                new ButtonElement("Create Animation Group", () =>
                {
                    if (animationGroups == null)
                        return;
                    animationGroups.Add(CreateAnimationGroup());
                    RenderPopup();
                }, shouldGenerate: () => animationGroups != null),
                new ButtonElement("Create Animation From Object", () => EditorTimeline.inst.onSelectTimelineObject = timelineObject =>
                {
                    if (!timelineObject.isBeatmapObject)
                    {
                        EditorManager.inst.DisplayNotification("Cannot apply animation from non-beatmap object.", 2f, EditorManager.NotificationType.Warning);
                        return;
                    }

                    animations.Add(ConvertToAnimation(timelineObject.GetData<BeatmapObject>()));
                    RenderPopup();
                }),
                new ButtonElement("Create Animation From Selected", () =>
                {
                    ConvertSelectedToAnimations(animations);
                    RenderPopup();
                    EditorManager.inst.DisplayNotification($"Converted all selected objects to an animation!", 2f, EditorManager.NotificationType.Success);
                }),
                new ButtonElement("Create Group From Selected", () =>
                {
                    if (animationGroups == null)
                        return;
                    var animationGroup = CreateAnimationGroup();
                    ConvertSelectedToAnimationGroup(animationGroup);
                    if (animationGroup.animations.IsEmpty())
                    {
                        EditorManager.inst.DisplayNotification($"No animations could be made.", 2f, EditorManager.NotificationType.Success);
                        return;
                    }
                    animationGroups.Add(animationGroup);
                    RenderPopup();
                    EditorManager.inst.DisplayNotification($"Converted all selected objects to an animation!", 2f, EditorManager.NotificationType.Success);
                }, shouldGenerate: () => animationGroups != null),
                new SpacerElement(),
                new ButtonElement("Copy All Animations", () =>
                {
                    copiedAnimations = new List<PAAnimation>(animations.Select(x => x.Copy()));
                    EditorManager.inst.DisplayNotification($"Copied all animations.", 2f, EditorManager.NotificationType.Success);
                }),
                new ButtonElement("Copy All Animation Groups", () =>
                {
                    copiedAnimationGroups = new List<AnimationGroup>(animationGroups.Select(x => x.Copy()));
                    EditorManager.inst.DisplayNotification($"Copied all animation groups.", 2f, EditorManager.NotificationType.Success);
                }, shouldGenerate: () => animationGroups != null),
                new ButtonElement("Paste Animations", () =>
                {
                    if (copiedAnimations.IsEmpty())
                    {
                        EditorManager.inst.DisplayNotification($"No copied animations yet!", 2f, EditorManager.NotificationType.Warning);
                        return;
                    }

                    animations.AddRange(copiedAnimations.Select(x => x.Copy()));
                    RenderPopup();
                    EditorManager.inst.DisplayNotification($"Pasted animations.", 2f, EditorManager.NotificationType.Success);
                }),
                new ButtonElement("Paste Animation Groups", () =>
                {
                    if (animationGroups == null)
                        return;
                    if (copiedAnimationGroups.IsEmpty())
                    {
                        EditorManager.inst.DisplayNotification($"No copied animations yet!", 2f, EditorManager.NotificationType.Warning);
                        return;
                    }

                    animationGroups.AddRange(copiedAnimationGroups.Select(x => x.Copy()));
                    RenderPopup();
                    EditorManager.inst.DisplayNotification($"Pasted animation groups.", 2f, EditorManager.NotificationType.Success);
                }, shouldGenerate: () => animationGroups != null),
                new SpacerElement(),
                new ButtonElement("Clear Animations", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to clear all animations from the list? This cannot be undone?", () =>
                {
                    animations.Clear();
                    RenderPopup();
                    EditorManager.inst.DisplayNotification($"Cleared animations.", 2f, EditorManager.NotificationType.Success);
                })),
                new ButtonElement("Clear Animation Groups", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to clear all animation groups from the list? This cannot be undone?", () =>
                {
                    animationGroups.Clear();
                    RenderPopup();
                    EditorManager.inst.DisplayNotification($"Cleared animation groups.", 2f, EditorManager.NotificationType.Success);
                }), shouldGenerate: () => animationGroups != null));

            var add = EditorPrefabHolder.Instance.CreateAddButton(Popup.Content);
            add.Text = "Add new Animation";
            add.OnClick.ClearAll();
            var addContextMenu = add.gameObject.GetOrAddComponent<ContextClickable>();
            addContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button == PointerEventData.InputButton.Right)
                {
                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonElement("Create Animation", () =>
                        {
                            animations.Add(CreateAnimation());
                            RenderPopup();
                        }),
                        new ButtonElement("Create Animation Group", () =>
                        {
                            if (animationGroups == null)
                                return;
                            animationGroups.Add(CreateAnimationGroup());
                            RenderPopup();
                        }, shouldGenerate: () => animationGroups != null),
                        new ButtonElement("Create Animation From Object", () => EditorTimeline.inst.onSelectTimelineObject = timelineObject =>
                        {
                            if (!timelineObject.isBeatmapObject)
                            {
                                EditorManager.inst.DisplayNotification("Cannot apply animation from non-beatmap object.", 2f, EditorManager.NotificationType.Warning);
                                return;
                            }

                            animations.Add(ConvertToAnimation(timelineObject.GetData<BeatmapObject>()));
                            RenderPopup();
                        }),
                        new ButtonElement("Create Animation From Selected", () =>
                        {
                            ConvertSelectedToAnimations(animations);
                            RenderPopup();
                            EditorManager.inst.DisplayNotification($"Converted all selected objects to an animation!", 2f, EditorManager.NotificationType.Success);
                        }),
                        new ButtonElement("Create Group From Selected", () =>
                        {
                            if (animationGroups == null)
                                return;
                            var animationGroup = CreateAnimationGroup();
                            ConvertSelectedToAnimationGroup(animationGroup);
                            if (animationGroup.animations.IsEmpty())
                            {
                                EditorManager.inst.DisplayNotification($"No animations could be made.", 2f, EditorManager.NotificationType.Success);
                                return;
                            }
                            animationGroups.Add(animationGroup);
                            RenderPopup();
                            EditorManager.inst.DisplayNotification($"Converted all selected objects to an animation!", 2f, EditorManager.NotificationType.Success);
                        }, shouldGenerate: () => animationGroups != null),
                        new SpacerElement(),
                        new ButtonElement("Copy All Animations", () =>
                        {
                            copiedAnimations = new List<PAAnimation>(animations.Select(x => x.Copy()));
                            EditorManager.inst.DisplayNotification($"Copied all animations.", 2f, EditorManager.NotificationType.Success);
                        }),
                        new ButtonElement("Copy All Animation Groups", () =>
                        {
                            copiedAnimationGroups = new List<AnimationGroup>(animationGroups.Select(x => x.Copy()));
                            EditorManager.inst.DisplayNotification($"Copied all animation groups.", 2f, EditorManager.NotificationType.Success);
                        }, shouldGenerate: () => animationGroups != null),
                        new ButtonElement("Paste Animations", () =>
                        {
                            if (copiedAnimations.IsEmpty())
                            {
                                EditorManager.inst.DisplayNotification($"No copied animations yet!", 2f, EditorManager.NotificationType.Warning);
                                return;
                            }

                            animations.AddRange(copiedAnimations.Select(x => x.Copy()));
                            RenderPopup();
                            EditorManager.inst.DisplayNotification($"Pasted animations.", 2f, EditorManager.NotificationType.Success);
                        }),
                        new ButtonElement("Paste Animation Groups", () =>
                        {
                            if (animationGroups == null)
                                return;
                            if (copiedAnimationGroups.IsEmpty())
                            {
                                EditorManager.inst.DisplayNotification($"No copied animations yet!", 2f, EditorManager.NotificationType.Warning);
                                return;
                            }

                            animationGroups.AddRange(copiedAnimationGroups.Select(x => x.Copy()));
                            RenderPopup();
                            EditorManager.inst.DisplayNotification($"Pasted animation groups.", 2f, EditorManager.NotificationType.Success);
                        }, shouldGenerate: () => animationGroups != null));
                    return;
                }

                animations.Add(CreateAnimation());
                RenderPopup();
            };

            if (animationGroups != null)
                for (int i = 0; i < animationGroups.Count; i++)
                {
                    var index = i;
                    var animationGroup = animationGroups[i];

                    if (animationGroup.collapse && !RTString.SearchString(Popup.SearchTerm, animationGroup.name))
                        continue;

                    /// TODO: add AnimationGroupEditorDialog so you can view and edit the name and description correctly
                    Popup.GenerateListButton(animationGroup.collapse ? $"{animationGroup.name} [...]" : animationGroup.name, EditorSprites.ListSprite, pointerEventData =>
                    {
                        if (pointerEventData.button == PointerEventData.InputButton.Right)
                        {
                            var elements = new List<EditorElement>
                            {
                                new StringInputElement(animationGroup.name,
                                _val => animationGroup.name = _val,
                                _val => RenderPopup()),
                                ButtonElement.ToggleButton("Collapse", () => animationGroup.collapse, () =>
                                {
                                    animationGroup.collapse = !animationGroup.collapse;
                                    RenderPopup();
                                }),
                                new ButtonElement("Delete", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this animation group?", () =>
                                {
                                    animationGroups.RemoveAt(index);
                                    RenderPopup();
                                })),
                                new SpacerElement(),
                                new ButtonElement("Apply To Selected", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to apply the current animations to the selected objects? This will check for matching animation IDs.", () =>
                                {
                                    ApplyAnimationsToSelected(animationGroup.animations);
                                    EditorManager.inst.DisplayNotification($"Applied animations to all selected objects!", 2f, EditorManager.NotificationType.Success);
                                })),
                                new ButtonElement("Copy From Selected", () =>
                                {
                                    foreach (var timelineObject in EditorTimeline.inst.SelectedBeatmapObjects)
                                    {
                                        var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                        if (string.IsNullOrEmpty(beatmapObject.animID))
                                            continue;
                                        animationGroup.animations.Add(ConvertToAnimation(beatmapObject));
                                    }
                                    RenderPopup();
                                }),
                                new ButtonElement("Paste Animations", () =>
                                {
                                    animationGroup.animations.AddRange(copiedAnimations.Select(x => x.Copy()));
                                    RenderPopup();
                                }, shouldGenerate: () => !copiedAnimations.IsEmpty()),
                                new ButtonElement("Clear Animations", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to clear the animations from this group?", () =>
                                {
                                    animationGroup.animations.Clear();
                                    RenderPopup();
                                }), shouldGenerate: () => !animationGroup.animations.IsEmpty()),
                                new SpacerElement(),
                                new ButtonElement("Copy", () =>
                                {
                                    copiedAnimationGroups.Clear();
                                    copiedAnimationGroups.Add(animationGroup);
                                    EditorManager.inst.DisplayNotification($"Copied animation group.", 2f, EditorManager.NotificationType.Success);
                                }),
                                new ButtonElement("Copy All", () =>
                                {
                                    copiedAnimationGroups = new List<AnimationGroup>(animationGroups.Select(x => x.Copy()));
                                    EditorManager.inst.DisplayNotification($"Copied all animation groups.", 2f, EditorManager.NotificationType.Success);
                                }),
                                new ButtonElement("Paste", () =>
                                {
                                    if (copiedAnimationGroups.IsEmpty())
                                    {
                                        EditorManager.inst.DisplayNotification($"No copied animation groups yet!", 2f, EditorManager.NotificationType.Warning);
                                        return;
                                    }

                                    animationGroups.AddRange(copiedAnimationGroups.Select(x => x.Copy()));
                                    RenderPopup();
                                    EditorManager.inst.DisplayNotification($"Pasted animation groups.", 2f, EditorManager.NotificationType.Success);
                                }),
                                new SpacerElement(),
                                new ButtonElement("Add to Prefab", () => RTPrefabEditor.inst.onSelectPrefab = prefabPanel =>
                                {
                                    if (prefabPanel.IsExternal)
                                    {
                                        EditorManager.inst.DisplayNotification($"Cannot add the animation group to an external prefab!", 2f, EditorManager.NotificationType.Warning);
                                        return;
                                    }

                                    if (prefabPanel.Item.animationGroups.TryFindIndex(x => x.id == animationGroup.id, out int animationGroupIndex))
                                    {
                                        RTEditor.inst.ShowWarningPopup("The animation group already exists in this prefab, do you wish to update it?", () =>
                                        {
                                            var orig = prefabPanel.Item.animationGroups[animationGroupIndex];
                                            orig.CopyData(animationGroup);
                                            orig.PrefabID = prefabPanel.Item.id;
                                            orig.PrefabInstanceID = string.Empty;
                                            EditorManager.inst.DisplayNotification($"Added animation group to prefab!", 2f, EditorManager.NotificationType.Success);
                                        });
                                        return;
                                    }
                                    var animationGroupCopy = animationGroup.Copy(false);
                                    animationGroupCopy.PrefabID = prefabPanel.Item.id;
                                    animationGroupCopy.PrefabInstanceID = string.Empty;
                                    prefabPanel.Item.animationGroups.Add(animationGroupCopy);
                                    EditorManager.inst.DisplayNotification($"Added animation group to prefab!", 2f, EditorManager.NotificationType.Success);
                                }),
                                new SpacerElement(),
                            };
                            elements.AddRange(EditorContextMenu.GetMoveIndexFunctions(animationGroups, index, RenderPopup));

                            EditorContextMenu.inst.ShowContextMenu(elements);
                            return;
                        }

                        animationGroup.collapse = !animationGroup.collapse;
                        RenderPopup();
                    });

                    if (animationGroup.collapse)
                        continue;

                    RenderAnimationList(animationGroup.animations, onPlay, onSelect, onReturn);
                }

            RenderAnimationList(animations, onPlay, onSelect, onReturn);
        }

        void RenderAnimationList(List<PAAnimation> animations, Action<PAAnimation> onPlay, Action<PAAnimation> onSelect, Action onReturn)
        {
            for (int i = 0; i < animations.Count; i++)
            {
                var index = i;
                var animation = animations[i];

                if (!RTString.SearchString(Popup.SearchTerm, animation.name ?? string.Empty, animation.ReferenceID ?? string.Empty))
                    continue;

                Popup.GenerateListButton($"{animation.name} [ {animation.ReferenceID} ]", EditorSprites.PlaySprite, pointerEventData =>
                {
                    if (pointerEventData.button == PointerEventData.InputButton.Right)
                    {
                        var elements = new List<EditorElement>
                        {
                            new ButtonElement("Edit", () => OpenDialog(animation, onReturn)),
                            new ButtonElement("Play", () =>
                            {
                                if (onPlay == null)
                                {
                                    EditorManager.inst.DisplayNotification($"Cannot play the animation as no object is associated with it.", 2f, EditorManager.NotificationType.Warning);
                                    return;
                                }

                                onPlay.Invoke(animation);
                                EditorManager.inst.DisplayNotification($"Played animation!", 2f, EditorManager.NotificationType.Success);
                            }),
                            new ButtonElement("Delete", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this animation?", () =>
                            {
                                animations.RemoveAt(index);
                                RenderPopup();
                            })),
                            new SpacerElement(),
                            new ButtonElement("Apply To Selected", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to apply the current animations to the selected objects? This will check for matching animation IDs.", () =>
                            {
                                ApplyAnimationsToSelected(animations);
                                EditorManager.inst.DisplayNotification($"Applied animations to all selected objects!", 2f, EditorManager.NotificationType.Success);
                            })),
                            new ButtonElement("Apply To Object", () => EditorTimeline.inst.onSelectTimelineObject = timelineObject =>
                            {
                                if (!timelineObject.isBeatmapObject)
                                {
                                    EditorManager.inst.DisplayNotification("Cannot apply animation to non-beatmap object.", 2f, EditorManager.NotificationType.Warning);
                                    return;
                                }

                                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                beatmapObject.CopyAnimatableData(animation);
                                if (ObjectEditor.inst.Dialog.IsCurrent && EditorTimeline.inst.CurrentSelection.ID == beatmapObject.id)
                                    ObjectEditor.inst.Dialog.Timeline.RenderKeyframes(beatmapObject);
                                RTLevel.Current.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                            }),
                            new ButtonElement("Copy From Object", () => EditorTimeline.inst.onSelectTimelineObject = timelineObject =>
                            {
                                if (!timelineObject.isBeatmapObject)
                                {
                                    EditorManager.inst.DisplayNotification("Cannot apply animation from non-beatmap object.", 2f, EditorManager.NotificationType.Warning);
                                    return;
                                }

                                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                animation.ReferenceID = beatmapObject.animID;
                                animation.CopyAnimatableData(beatmapObject);
                                if (Dialog.IsCurrent && Dialog.Timeline.CurrentObject == animation)
                                    RenderDialog(animation, onReturn);
                            }),
                            new SpacerElement(),
                            new ButtonElement("Copy", () =>
                            {
                                copiedAnimations.Clear();
                                copiedAnimations.Add(animation.Copy());
                                EditorManager.inst.DisplayNotification($"Copied animation.", 2f, EditorManager.NotificationType.Success);
                            }),
                            new ButtonElement("Copy All", () =>
                            {
                                copiedAnimations = new List<PAAnimation>(animations.Select(x => x.Copy()));
                                EditorManager.inst.DisplayNotification($"Copied all animations.", 2f, EditorManager.NotificationType.Success);
                            }),
                            new ButtonElement("Paste", () =>
                            {
                                if (copiedAnimations.IsEmpty())
                                {
                                    EditorManager.inst.DisplayNotification($"No copied animations yet!", 2f, EditorManager.NotificationType.Warning);
                                    return;
                                }

                                animations.AddRange(copiedAnimations.Select(x => x.Copy()));
                                RenderPopup();
                                EditorManager.inst.DisplayNotification($"Pasted animations.", 2f, EditorManager.NotificationType.Success);
                            }),
                            new SpacerElement(),
                            new ButtonElement("Copy Keyframes", () => KeyframeTimeline.CopyAllKeyframes(animation)),
                            new SpacerElement(),
                            new ButtonElement("Add to Prefab", () => RTPrefabEditor.inst.onSelectPrefab = prefabPanel =>
                            {
                                if (prefabPanel.IsExternal)
                                {
                                    EditorManager.inst.DisplayNotification($"Cannot add the animation to an external prefab!", 2f, EditorManager.NotificationType.Warning);
                                    return;
                                }

                                if (prefabPanel.Item.animations.TryFindIndex(x => x.id == animation.id, out int animationIndex))
                                {
                                    RTEditor.inst.ShowWarningPopup("The animation already exists in this prefab, do you wish to update it?", () =>
                                    {
                                        var orig = prefabPanel.Item.animations[animationIndex];
                                        orig.CopyData(animation);
                                        orig.PrefabID = prefabPanel.Item.id;
                                        orig.PrefabInstanceID = string.Empty;
                                        EditorManager.inst.DisplayNotification($"Added animation to prefab!", 2f, EditorManager.NotificationType.Success);
                                    });
                                    return;
                                }
                                var animationCopy = animation.Copy(false);
                                animationCopy.PrefabID = prefabPanel.Item.id;
                                animationCopy.PrefabInstanceID = string.Empty;
                                prefabPanel.Item.animations.Add(animationCopy);
                                EditorManager.inst.DisplayNotification($"Added animation to prefab!", 2f, EditorManager.NotificationType.Success);
                            }),
                            new SpacerElement(),
                        };
                        elements.AddRange(EditorContextMenu.GetMoveIndexFunctions(animations, index, RenderPopup));

                        EditorContextMenu.inst.ShowContextMenu(elements);
                        return;
                    }

                    if (onSelect != null)
                    {
                        onSelect.Invoke(animation);
                        return;
                    }

                    OpenDialog(animation, onReturn);
                });
            }
        }

        #endregion

        #endregion
    }
}
