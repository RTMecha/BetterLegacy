using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using LSFunctions;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Popups;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Editor class that manages custom animations.
    /// </summary>
    public class AnimationEditor : MonoBehaviour
    {
        /*
         TOOD:
        - Copy & Paste animations
        - Autoplay animations, similar to animating beatmap objects at runtime
        - Animation library
         */

        #region Init

        /// <summary>
        /// The <see cref="AnimationEditor"/> global instance reference.
        /// </summary>
        public static AnimationEditor inst;

        /// <summary>
        /// Initializes <see cref="AnimationEditor"/>.
        /// </summary>
        public static void Init() => Creator.NewGameObject(nameof(AnimationEditor), EditorManager.inst.transform.parent).AddComponent<AnimationEditor>();

        void Awake()
        {
            inst = this;

            try
            {
                Dialog = new AnimationEditorDialog();
                Dialog.Init();

                Popup = RTEditor.inst.GeneratePopup(EditorPopup.ANIMATIONS_POPUP, "Animations", Vector2.zero, new Vector2(600f, 400f));
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }

        #endregion

        #region Values

        public AnimationEditorDialog Dialog { get; set; }

        public ContentPopup Popup { get; set; }

        public PAAnimation CurrentAnimation { get; set; }

        #endregion

        #region Methods

        public void Test()
        {
            var boostAnimation = new PAAnimation("Test", "Test description");
            boostAnimation.ReferenceID = "boost";
            boostAnimation.positionKeyframes.Add(new EventKeyframe(0.3f, new float[] { 10f, 0f, 0f }, "OutSine"));
            boostAnimation.positionKeyframes.Add(new EventKeyframe(0.4f, new float[] { 0f, 0f, 0f }, "InSine"));

            var idleAnimation = new PAAnimation("Test", "Test description");
            idleAnimation.ReferenceID = "idle";
            idleAnimation.positionKeyframes.Add(new EventKeyframe(1f, new float[] { 3f, 0f, 0f }, "InOutSine"));
            idleAnimation.positionKeyframes.Add(new EventKeyframe(2f, new float[] { 0f, 0f, 0f }, "InOutSine"));

            if (PlayerManager.Players.TryGetAt(0, out PAPlayer player) && player.RuntimePlayer && player.PlayerModel && player.RuntimePlayer.Model.customObjects.TryGetAt(0, out CustomPlayerObject customObject))
            {
                customObject.animations.Clear();
                customObject.animations.Add(boostAnimation);
                customObject.animations.Add(idleAnimation);
                player.RuntimePlayer.UpdateModel();
            }
        }

        #region Editor

        public void OpenDialog(PAAnimation animation)
        {
            Dialog.Open();
            RenderDialog(animation);
        }

        public void RenderDialog(PAAnimation animation)
        {
            CurrentAnimation = animation;

            Dialog.IDText.text = $"ID: {animation.id}";
            var idClickable = Dialog.IDBase.gameObject.GetOrAddComponent<Clickable>();
            idClickable.onClick = pointerEventData =>
            {
                EditorManager.inst.DisplayNotification($"Copied ID from {animation.name}!", 2f, EditorManager.NotificationType.Success);
                LSText.CopyToClipboard(animation.id);
            };

            Dialog.ReferenceField.SetTextWithoutNotify(animation.ReferenceID);
            Dialog.ReferenceField.onValueChanged.NewListener(_val => animation.ReferenceID = _val);
            var referenceContextMenu = Dialog.ReferenceField.gameObject.GetOrAddComponent<ContextClickable>();
            referenceContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction(PlayerModel.IDLE_ANIM, () => Dialog.ReferenceField.text = PlayerModel.IDLE_ANIM),
                    new ButtonFunction(PlayerModel.BOOST_ANIM, () => Dialog.ReferenceField.text = PlayerModel.BOOST_ANIM),
                    new ButtonFunction(PlayerModel.HEAL_ANIM, () => Dialog.ReferenceField.text = PlayerModel.HEAL_ANIM),
                    new ButtonFunction(PlayerModel.HIT_ANIM, () => Dialog.ReferenceField.text = PlayerModel.HIT_ANIM),
                    new ButtonFunction(PlayerModel.DEATH_ANIM, () => Dialog.ReferenceField.text = PlayerModel.DEATH_ANIM),
                    new ButtonFunction(PlayerModel.SHOOT_ANIM, () => Dialog.ReferenceField.text = PlayerModel.SHOOT_ANIM),
                    new ButtonFunction(PlayerModel.JUMP_ANIM, () => Dialog.ReferenceField.text = PlayerModel.JUMP_ANIM)
                    );
            };

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
                animation.EditorData.GetDisplay("position/x", CustomUIDisplay.DefaultPositionXDisplay),
                animation.EditorData.GetDisplay("position/y", CustomUIDisplay.DefaultPositionYDisplay),
                animation.EditorData.GetDisplay("position/z", CustomUIDisplay.DefaultPositionZDisplay));
            Dialog.keyframeDialogs[0].EventValueElements[2].GameObject.SetActive(RTEditor.ShowModdedUI);
            Dialog.keyframeDialogs[1].InitCustomUI(
                animation.EditorData.GetDisplay("scale/x", CustomUIDisplay.DefaultScaleXDisplay),
                animation.EditorData.GetDisplay("scale/y", CustomUIDisplay.DefaultScaleYDisplay));
            Dialog.keyframeDialogs[2].InitCustomUI(
                animation.EditorData.GetDisplay("rotation/x", CustomUIDisplay.DefaultRotationDisplay));
        }

        #endregion

        #region List

        /// <summary>
        /// Opens the animation list popup.
        /// </summary>
        /// <param name="animations">List of animations to display.</param>
        /// <param name="onPlay">Function to run when the user wants to play the animation.</param>
        public void OpenPopup(List<PAAnimation> animations, Action<PAAnimation> onPlay = null, Action<PAAnimation> onSelect = null)
        {
            Popup.Open();
            RenderPopup(animations, onPlay, onSelect);
        }

        /// <summary>
        /// Renders the animation list popup.
        /// </summary>
        /// <param name="animations">List of animations to display.</param>
        /// <param name="onPlay">Function to run when the user wants to play the animation.</param>
        public void RenderPopup(List<PAAnimation> animations, Action<PAAnimation> onPlay = null, Action<PAAnimation> onSelect = null)
        {
            Popup.ClearContent();
            Popup.SearchField.onValueChanged.NewListener(_val => RenderPopup(animations, onPlay, onSelect));

            var add = PrefabEditor.inst.CreatePrefab.Duplicate(Popup.Content);
            add.transform.AsRT().sizeDelta = new Vector2(350f, 32f);
            var addText = add.transform.Find("Text").GetComponent<Text>();
            addText.text = "Add new Animation";
            var addButton = add.GetComponent<Button>();
            addButton.onClick.NewListener(() =>
            {
                animations.Add(new PAAnimation("New Animation", "This is the default description!"));
                RenderPopup(animations, onPlay, onSelect);
            });

            EditorThemeManager.ApplyGraphic(addButton.image, ThemeGroup.Add, true);
            EditorThemeManager.ApplyGraphic(addText, ThemeGroup.Add_Text);

            for (int i = 0; i < animations.Count; i++)
            {
                var index = i;
                var animation = animations[i];

                if (!RTString.SearchString(Popup.SearchTerm, animation.name, animation.ReferenceID))
                    continue;

                var gameObject = EditorManager.inst.spriteFolderButtonPrefab.Duplicate(Popup.Content, "anim");
                var storage = gameObject.GetComponent<SpriteFunctionButtonStorage>();
                storage.button.onClick.ClearAll();
                var contextClickable = gameObject.GetOrAddComponent<ContextClickable>();
                contextClickable.onClick = pointerEventData =>
                {
                    if (pointerEventData.button == PointerEventData.InputButton.Right)
                    {
                        var buttonFunctions = new List<ButtonFunction>()
                            {
                                new ButtonFunction("Edit", () =>
                                {
                                    OpenDialog(animation);
                                }),
                                new ButtonFunction("Play", () =>
                                {
                                    if (onPlay == null)
                                    {
                                        EditorManager.inst.DisplayNotification($"Cannot play the animation as no object is associated with it.", 2f, EditorManager.NotificationType.Warning);
                                        return;
                                    }

                                    onPlay.Invoke(animation);
                                    EditorManager.inst.DisplayNotification($"Played animation!", 2f, EditorManager.NotificationType.Success);
                                }),
                                new ButtonFunction("Delete", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this animation?", () =>
                                {
                                    animations.RemoveAt(index);
                                    RenderPopup(animations, onPlay, onSelect);
                                    RTEditor.inst.HideWarningPopup();
                                }, RTEditor.inst.HideWarningPopup)),
                            };

                        EditorContextMenu.inst.ShowContextMenu(buttonFunctions);
                        return;
                    }

                    if (onSelect != null)
                    {
                        onSelect.Invoke(animation);
                        return;
                    }

                    OpenDialog(animation);
                };

                storage.image.sprite = EditorSprites.PlaySprite;
                storage.label.text = animation.name + $" [ {animation.ReferenceID} ]";

                EditorThemeManager.ApplyGraphic(storage.image, ThemeGroup.Light_Text);
                EditorThemeManager.ApplySelectable(storage.button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(storage.label);
            }
        }

        #endregion

        #endregion
    }
}
