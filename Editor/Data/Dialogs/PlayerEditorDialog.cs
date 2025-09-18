using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class PlayerEditorDialog : EditorDialog, IContentUI
    {
        public PlayerEditorDialog() : base() { }

        #region Tabs

        /// <summary>
        /// Parent of the tab buttons.
        /// </summary>
        public Transform TabsParent { get; set; }

        /// <summary>
        /// Tabs of the Player Editor.
        /// </summary>
        public List<FunctionButtonStorage> TabButtons { get; set; } = new List<FunctionButtonStorage>();

        public List<PlayerEditorTab> Tabs { get; set; } = new List<PlayerEditorTab>();

        public PlayerEditorGlobalTab GlobalTab { get; set; } = new PlayerEditorGlobalTab();

        public PlayerEditorBaseTab BaseTab { get; set; } = new PlayerEditorBaseTab();

        public PlayerEditorGUITab GUITab { get; set; } = new PlayerEditorGUITab();

        public PlayerEditorObjectTab HeadTab { get; set; } = new PlayerEditorObjectTab();

        public PlayerEditorObjectTab BoostTab { get; set; } = new PlayerEditorObjectTab();

        public PlayerEditorSpawnerTab SpawnerTab { get; set; } = new PlayerEditorSpawnerTab();

        public PlayerEditorTailTab TailTab { get; set; } = new PlayerEditorTailTab();

        public PlayerEditorCustomObjectTab CustomObjectTab { get; set; } = new PlayerEditorCustomObjectTab();

        #endregion

        #region Content

        public InputField SearchField { get; set; }

        public Transform Content { get; set; }

        public GridLayoutGroup Grid { get; set; }

        public Scrollbar ContentScrollbar { get; set; }

        public string SearchTerm { get => SearchField.text; set => SearchField.text = value; }

        public List<PlayerEditorElement> elements = new List<PlayerEditorElement>();

        public List<PlayerEditorElement> EditorUIsActive => elements.Where(x => x.GameObject.activeSelf).ToList();

        GameObject labelPrefab;

        public void ClearContent()
        {
            // no clear
        }

        #endregion

        public override void Init()
        {
            if (init)
                return;

            init = true;

            var dialog = EditorPrefabHolder.Instance.Dialog.Duplicate(EditorManager.inst.dialogs, "PlayerEditorDialog");
            dialog.transform.AsRT().anchoredPosition = new Vector2(0f, 16f);
            dialog.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            var dialogStorage = dialog.GetComponent<EditorDialogStorage>();

            dialogStorage.topPanel.color = LSColors.HexToColor(BeatmapTheme.PLAYER_1_COLOR);
            dialogStorage.title.text = "- Player Editor -";

            dialog.transform.GetChild(1).AsRT().sizeDelta = new Vector2(765f, 54f);

            CoreHelper.Delete(dialog.transform.Find("Text").gameObject);

            EditorThemeManager.AddGraphic(dialog.GetComponent<Image>(), ThemeGroup.Background_1);

            var search = EditorLevelManager.inst.OpenLevelPopup.GameObject.transform.Find("search-box").gameObject.Duplicate(dialog.transform, "search");

            SearchField = search.transform.GetChild(0).GetComponent<InputField>();

            SearchField.SetTextWithoutNotify(string.Empty);
            SearchField.onValueChanged.NewListener(_val => CoroutineHelper.StartCoroutine(PlayerEditor.inst.RefreshEditor()));
            SearchField.GetPlaceholderText().text = "Search for value...";

            var spacer = dialog.transform.Find("spacer");
            var layout = Creator.NewUIObject("layout", spacer);
            TabsParent = layout.transform;
            UIManager.SetRectTransform(layout.transform.AsRT(), new Vector2(8f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(760f, 43.2f));
            var layoutHLG = layout.AddComponent<HorizontalLayoutGroup>();
            layoutHLG.childControlWidth = false;
            layoutHLG.childForceExpandWidth = false;
            layoutHLG.spacing = 2f;

            var enumNames = Enum.GetNames(typeof(PlayerEditor.Tab));
            for (int i = 0; i < enumNames.Length; i++)
                SetupTabButton(enumNames[i], i);

            //SetupTab("Global", 0);
            //SetupTab("Base", 1);
            //SetupTab("Control", 2);
            //SetupTab("GUI", 3);
            //SetupTab("Objects", 4);
            //SetupTab("Animations", 5);

            var scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(dialog.transform, "Scroll View");

            scrollView.transform.AsRT().sizeDelta = new Vector2(765f, 512f);

            EditorHelper.AddEditorDialog(PLAYER_EDITOR, dialog);

            base.InitDialog(PLAYER_EDITOR);

            var playerEditor = EditorHelper.AddEditorDropdown("Player Editor", "", "Edit", EditorSprites.PlayerSprite, () =>
            {
                Open();
                CoroutineHelper.StartCoroutine(PlayerEditor.inst.RefreshEditor());
            });
            EditorHelper.SetComplexity(playerEditor, Complexity.Advanced);

            Content = scrollView.transform.Find("Viewport/Content");
            CoreHelper.DestroyChildren(Content);

            labelPrefab = EditorManager.inst.folderButtonPrefab.transform.GetChild(0).gameObject;
            var boolInput = EditorPrefabHolder.Instance.Toggle;

            // Default
            {
                var gameObject = Creator.NewUIObject("handler", Content);
                gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 42f);

                var label = labelPrefab.Duplicate(gameObject.transform, "label");
                var labelText = label.GetComponent<Text>();
                labelText.text = "Cannot edit default Player models.";
                UIManager.SetRectTransform(label.transform.AsRT(), new Vector2(32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(762f, 32f));

                gameObject.SetActive(false);
            }

            Tabs.Add(GlobalTab);
            Tabs.Add(BaseTab);
            Tabs.Add(GUITab);
            Tabs.Add(HeadTab);
            Tabs.Add(BoostTab);
            Tabs.Add(SpawnerTab);
            Tabs.Add(TailTab);
            Tabs.Add(CustomObjectTab);

            #region Global

            GlobalTab.RespawnPlayers = SetupButton("Respawn Players", PlayerEditor.Tab.Global, editorTab: GlobalTab);
            GlobalTab.RespawnPlayers.ShowInDefault = true;
            GlobalTab.UpdateProperties = SetupButton("Update Properties", PlayerEditor.Tab.Global, editorTab: GlobalTab);
            GlobalTab.UpdateProperties.ShowInDefault = true;

            GlobalTab.SpawnPlayers = SetupBool("Spawn Players", PlayerEditor.Tab.Global, editorTab: GlobalTab);
            GlobalTab.SpawnPlayers.ShowInDefault = true;
            GlobalTab.RespawnImmediately = SetupBool("Respawn Immediately", PlayerEditor.Tab.Global, editorTab: GlobalTab);
            GlobalTab.RespawnImmediately.ShowInDefault = true;
            GlobalTab.AllowCustomPlayerModels = SetupBool("Allow Custom Player Models", PlayerEditor.Tab.Global, editorTab: GlobalTab);
            GlobalTab.AllowCustomPlayerModels.ShowInDefault = true;
            GlobalTab.AllowPlayerModelControls = SetupBool("Allow Player Model Controls", PlayerEditor.Tab.Global, editorTab: GlobalTab);
            GlobalTab.AllowPlayerModelControls.ShowInDefault = true;

            GlobalTab.Speed = SetupNumber("Global Speed", PlayerEditor.Tab.Global, editorTab: GlobalTab);
            GlobalTab.Speed.ShowInDefault = true;
            GlobalTab.LockBoost = SetupBool("Global Lock Boost", PlayerEditor.Tab.Global, editorTab: GlobalTab);
            GlobalTab.LockBoost.ShowInDefault = true;
            GlobalTab.GameMode = SetupDropdown("Global Gamemode", CoreHelper.StringToOptionData("Regular", "Platformer"), PlayerEditor.Tab.Global, editorTab: GlobalTab);
            GlobalTab.GameMode.ShowInDefault = true;
            GlobalTab.MaxHealth = SetupNumber("Global Max Health", PlayerEditor.Tab.Global, ValueType.Int, editorTab: GlobalTab);
            GlobalTab.MaxHealth.ShowInDefault = true;
            GlobalTab.MaxJumpCount = SetupNumber("Global Max Jump Count", PlayerEditor.Tab.Global, ValueType.Int, editorTab: GlobalTab);
            GlobalTab.MaxJumpCount.ShowInDefault = true;
            GlobalTab.MaxJumpBoostCount = SetupNumber("Global Max Jump Boost Count", PlayerEditor.Tab.Global, ValueType.Int, editorTab: GlobalTab);
            GlobalTab.MaxJumpBoostCount.ShowInDefault = true;
            GlobalTab.JumpGravity = SetupNumber("Global Jump Gravity", PlayerEditor.Tab.Global, editorTab: GlobalTab);
            GlobalTab.JumpGravity.ShowInDefault = true;
            GlobalTab.JumpIntensity = SetupNumber("Global Jump Intensity", PlayerEditor.Tab.Global, editorTab: GlobalTab);
            GlobalTab.JumpIntensity.ShowInDefault = true;

            GlobalTab.LimitPlayer = SetupBool("Limit Player", PlayerEditor.Tab.Global, editorTab: GlobalTab);
            GlobalTab.LimitPlayer.ShowInDefault = true;
            GlobalTab.LimitMoveSpeed = SetupVector2("Limit Move Speed", PlayerEditor.Tab.Global, editorTab: GlobalTab);
            GlobalTab.LimitMoveSpeed.ShowInDefault = true;
            GlobalTab.LimitBoostSpeed = SetupVector2("Limit Boost Speed", PlayerEditor.Tab.Global, editorTab: GlobalTab);
            GlobalTab.LimitBoostSpeed.ShowInDefault = true;
            GlobalTab.LimitBoostCooldown = SetupVector2("Limit Boost Cooldown", PlayerEditor.Tab.Global, editorTab: GlobalTab);
            GlobalTab.LimitBoostCooldown.ShowInDefault = true;
            GlobalTab.LimitBoostMinTime = SetupVector2("Limit Boost Min Time", PlayerEditor.Tab.Global, editorTab: GlobalTab);
            GlobalTab.LimitBoostMinTime.ShowInDefault = true;
            GlobalTab.LimitBoostMaxTime = SetupVector2("Limit Boost Max Time", PlayerEditor.Tab.Global, editorTab: GlobalTab);
            GlobalTab.LimitBoostMaxTime.ShowInDefault = true;
            GlobalTab.LimitHitCooldown = SetupVector2("Limit Hit Cooldown", PlayerEditor.Tab.Global, editorTab: GlobalTab);
            GlobalTab.LimitHitCooldown.ShowInDefault = true;

            #endregion

            #region Base

            BaseTab.ID = SetupButton("Base ID", PlayerEditor.Tab.Base, editorTab: BaseTab);
            BaseTab.ID.ShowInDefault = true;
            BaseTab.Name = SetupString("Base Name", PlayerEditor.Tab.Base, editorTab: BaseTab);
            BaseTab.Creator = SetupString("Base Creator", PlayerEditor.Tab.Base, editorTab: BaseTab);
            BaseTab.Version = SetupString("Base Version", PlayerEditor.Tab.Base, editorTab: BaseTab);
            BaseTab.EditControls = SetupBool("Edit Controls", PlayerEditor.Tab.Base, editorTab: BaseTab);
            BaseTab.EditControls.ShowInDefault = true;
            BaseTab.Health = SetupNumber("Base Health", PlayerEditor.Tab.Base, ValueType.Int, editorTab: BaseTab);
            BaseTab.Lives = SetupNumber("Base Lives", PlayerEditor.Tab.Base, ValueType.Int, editorTab: BaseTab);
            BaseTab.MoveSpeed = SetupNumber("Base Move Speed", PlayerEditor.Tab.Base, editorTab: BaseTab);
            BaseTab.BoostSpeed = SetupNumber("Base Boost Speed", PlayerEditor.Tab.Base, editorTab: BaseTab);
            BaseTab.BoostCooldown = SetupNumber("Base Boost Cooldown", PlayerEditor.Tab.Base, editorTab: BaseTab);
            BaseTab.MinBoostTime = SetupNumber("Base Min Boost Time", PlayerEditor.Tab.Base, editorTab: BaseTab);
            BaseTab.MaxBoostTime = SetupNumber("Base Max Boost Time", PlayerEditor.Tab.Base, editorTab: BaseTab);
            BaseTab.HitCooldown = SetupNumber("Base Hit Cooldown", PlayerEditor.Tab.Base, editorTab: BaseTab);
            BaseTab.RotateMode = SetupDropdown("Base Rotate Mode", CoreHelper.StringToOptionData("Face Direction", "None", "Flip X", "Flip Y", "Rotate Reset", "Rotate Flip X", "Rotate Flip Y"), PlayerEditor.Tab.Base, editorTab: BaseTab);
            BaseTab.RotationSpeed = SetupNumber("Base Rotation Speed", PlayerEditor.Tab.Base, editorTab: BaseTab);
            BaseTab.RotationCurve = SetupDropdown("Base Rotation Curve", EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList(), PlayerEditor.Tab.Base, editorTab: BaseTab);
            BaseTab.CollisionAccurate = SetupBool("Base Collision Accurate", PlayerEditor.Tab.Base, editorTab: BaseTab);
            BaseTab.SprintSneakActive = SetupBool("Base Sprint Sneak Active", PlayerEditor.Tab.Base, editorTab: BaseTab);
            BaseTab.SprintSpeed = SetupNumber("Base Sprint Speed", PlayerEditor.Tab.Base, editorTab: BaseTab);
            BaseTab.SneakSpeed = SetupNumber("Base Sneak Speed", PlayerEditor.Tab.Base, editorTab: BaseTab);
            BaseTab.CanBoost = SetupBool("Base Can Boost", PlayerEditor.Tab.Base, editorTab: BaseTab);

            BaseTab.JumpGravity = SetupNumber("Base Jump Gravity", PlayerEditor.Tab.Base, editorTab: BaseTab);
            BaseTab.JumpIntensity = SetupNumber("Base Jump Intensity", PlayerEditor.Tab.Base, editorTab: BaseTab);
            BaseTab.JumpCount = SetupNumber("Base Jump Count", PlayerEditor.Tab.Base, ValueType.Int, editorTab: BaseTab);
            BaseTab.JumpBoostCount = SetupNumber("Base Jump Boost Count", PlayerEditor.Tab.Base, ValueType.Int, editorTab: BaseTab);
            BaseTab.Bounciness = SetupNumber("Base Bounciness", PlayerEditor.Tab.Base, editorTab: BaseTab);

            BaseTab.StretchActive = SetupBool("Stretch Active", PlayerEditor.Tab.Base, editorTab: BaseTab);
            BaseTab.StretchAmount = SetupNumber("Stretch Amount", PlayerEditor.Tab.Base, editorTab: BaseTab);
            BaseTab.StretchEasing = SetupDropdown("Stretch Easing", EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList(), PlayerEditor.Tab.Base, editorTab: BaseTab);

            BaseTab.FacePosition = SetupVector2("Face Position", PlayerEditor.Tab.Base, editorTab: BaseTab);
            BaseTab.FaceControlActive = SetupBool("Face Control Active", PlayerEditor.Tab.Base, editorTab: BaseTab);

            BaseTab.TickModifiers = SetupModifiers("  Modifiers Per Tick", PlayerEditor.Tab.Base, editorTab: BaseTab);
            BaseTab.TickModifiers.ShowInDefault = true;

            BaseTab.ModelModifiers = SetupModifiers("  Model Modifiers", PlayerEditor.Tab.Base, editorTab: BaseTab);

            #endregion

            #region GUI

            GUITab.HealthActive = SetupBool("GUI Health Active", PlayerEditor.Tab.GUI, editorTab: GUITab);
            GUITab.HealthMode = SetupDropdown("GUI Health Mode", CoreHelper.StringToOptionData("Images", "Text", "Equals Bar", "Bar"), PlayerEditor.Tab.GUI, editorTab: GUITab);
            GUITab.HealthTopColor = SetupColor("GUI Health Top Color", PlayerEditor.Tab.GUI, editorTab: GUITab);
            GUITab.HealthTopCustomColor = SetupString("GUI Health Top Custom Color", PlayerEditor.Tab.GUI, editorTab: GUITab);
            GUITab.HealthTopOpacity = SetupNumber("GUI Health Top Opacity", PlayerEditor.Tab.GUI, editorTab: GUITab);
            GUITab.HealthBaseColor = SetupColor("GUI Health Base Color", PlayerEditor.Tab.GUI, editorTab: GUITab);
            GUITab.HealthBaseCustomColor = SetupString("GUI Health Base Custom Color", PlayerEditor.Tab.GUI, editorTab: GUITab);
            GUITab.HealthBaseOpacity = SetupNumber("GUI Health Base Opacity", PlayerEditor.Tab.GUI, editorTab: GUITab);

            #endregion

            #region Head

            SetupObjectTab(HeadTab, "Head", PlayerEditor.Tab.Head, false);

            #endregion

            #region Boost

            SetupObjectTab(BoostTab, "Boost", PlayerEditor.Tab.Boost);

            #endregion

            #region Spawners

            #region Pulse

            SpawnerTab.PulseActive = SetupBool("Pulse Active", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.PulseRotateToHead = SetupBool("Pulse Rotate to Head", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.PulseShape = SetupShape("Pulse Shape", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.PulseDuration = SetupNumber("Pulse Duration", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.PulseStartColor = SetupColor("Pulse Start Color", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.PulseStartCustomColor = SetupString("Pulse Start Custom Color", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.PulseStartOpacity = SetupNumber("Pulse Start Opacity", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.PulseEndColor = SetupColor("Pulse End Color", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.PulseEndCustomColor = SetupString("Pulse End Custom Color", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.PulseEndOpacity = SetupNumber("Pulse End Opacity", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.PulseColorEasing = SetupDropdown("Pulse Color Easing", EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList(), PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.PulseOpacityEasing = SetupDropdown("Pulse Opacity Easing", EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList(), PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.PulseDepth = SetupNumber("Pulse Depth", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.PulseStartPosition = SetupVector2("Pulse Start Position", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.PulseEndPosition = SetupVector2("Pulse End Position", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.PulsePositionEasing = SetupDropdown("Pulse Position Easing", EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList(), PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.PulseStartScale = SetupVector2("Pulse Start Scale", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.PulseEndScale = SetupVector2("Pulse End Scale", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.PulseScaleEasing = SetupDropdown("Pulse Scale Easing", EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList(), PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.PulseStartRotation = SetupNumber("Pulse Start Rotation", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.PulseEndRotation = SetupNumber("Pulse End Rotation", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.PulseRotationEasing = SetupDropdown("Pulse Rotation Easing", EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList(), PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);

            #endregion

            #region Bullet

            SpawnerTab.BulletActive = SetupBool("Bullet Active", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletAutoKill = SetupBool("Bullet AutoKill", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletLifetime = SetupNumber("Bullet Lifetime", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletConstant = SetupBool("Bullet Constant", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletHurtPlayers = SetupBool("Bullet Hurt Players", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletSpeed = SetupNumber("Bullet Speed", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletCooldown = SetupNumber("Bullet Cooldown", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletOrigin = SetupVector2("Bullet Origin", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletShape = SetupShape("Bullet Shape", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletStartColor = SetupColor("Bullet Start Color", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletStartCustomColor = SetupString("Bullet Start Custom Color", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletStartOpacity = SetupNumber("Bullet Start Opacity", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletEndColor = SetupColor("Bullet End Color", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletEndCustomColor = SetupString("Bullet End Custom Color", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletEndOpacity = SetupNumber("Bullet End Opacity", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletColorDuration = SetupNumber("Bullet Color Duration", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletColorEasing = SetupDropdown("Bullet Color Easing", EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList(), PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletOpacityDuration = SetupNumber("Bullet Opacity Duration", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletOpacityEasing = SetupDropdown("Bullet Opacity Easing", EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList(), PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletDepth = SetupNumber("Bullet Depth", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletStartPosition = SetupVector2("Bullet Start Position", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletEndPosition = SetupVector2("Bullet End Position", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletPositionDuration = SetupNumber("Bullet Position Duration", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletPositionEasing = SetupDropdown("Bullet Position Easing", EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList(), PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletStartScale = SetupVector2("Bullet Start Scale", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletEndScale = SetupVector2("Bullet End Scale", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletScaleDuration = SetupNumber("Bullet Scale Duration", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletScaleEasing = SetupDropdown("Bullet Scale Easing", EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList(), PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletStartRotation = SetupNumber("Bullet Start Rotation", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletEndRotation = SetupNumber("Bullet End Rotation", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletRotationDuration = SetupNumber("Bullet Rotation Duration", PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);
            SpawnerTab.BulletRotationEasing = SetupDropdown("Bullet Rotation Easing", EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList(), PlayerEditor.Tab.Spawners, editorTab: SpawnerTab);

            #endregion

            #endregion

            #region Tail

            TailTab.BaseDistance = SetupNumber("Tail Base Distance", PlayerEditor.Tab.Tail, editorTab: TailTab);
            TailTab.BaseMode = SetupDropdown("Tail Base Mode", CoreHelper.StringToOptionData("Legacy", "Dev+"), PlayerEditor.Tab.Tail, editorTab: TailTab);
            TailTab.BaseGrows = SetupBool("Tail Base Grows", PlayerEditor.Tab.Tail, editorTab: TailTab);
            TailTab.BaseTime = SetupNumber("Tail Base Time", PlayerEditor.Tab.Tail, editorTab: TailTab);

            SetupObjectTab(TailTab.BoostPart, "Tail Boost", PlayerEditor.Tab.Tail);

            #endregion

            #region Custom Objects

            CustomObjectTab.SelectCustomObject = SetupButton("Custom Objects", PlayerEditor.Tab.Custom, "Select a custom object.", editorTab: CustomObjectTab);
            CustomObjectTab.ID = SetupButton("ID", PlayerEditor.Tab.Custom, editorTab: CustomObjectTab);
            CustomObjectTab.Name = SetupString("Name", PlayerEditor.Tab.Custom, editorTab: CustomObjectTab);
            CustomObjectTab.Tags = SetupTags("Tags", PlayerEditor.Tab.Custom, editorTab: CustomObjectTab);
            SetupObjectTab(CustomObjectTab, string.Empty, PlayerEditor.Tab.Custom, false, false, false);

            CustomObjectTab.Parent = SetupDropdown("Parent", CoreHelper.StringToOptionData("Head", "Boost", "Boost Tail", "Tail 1", "Tail 2", "Tail 3", "Face"), PlayerEditor.Tab.Custom, editorTab: CustomObjectTab);
            CustomObjectTab.CustomParent = SetupString("Custom Parent", PlayerEditor.Tab.Custom, editorTab: CustomObjectTab);
            CustomObjectTab.PositionOffset = SetupNumber("Position Offset", PlayerEditor.Tab.Custom, editorTab: CustomObjectTab);
            CustomObjectTab.ScaleOffset = SetupNumber("Scale Offset", PlayerEditor.Tab.Custom, editorTab: CustomObjectTab);
            CustomObjectTab.ScaleParent = SetupBool("Scale Parent", PlayerEditor.Tab.Custom, editorTab: CustomObjectTab);
            CustomObjectTab.RotationOffset = SetupNumber("Rotation Offset", PlayerEditor.Tab.Custom, editorTab: CustomObjectTab);
            CustomObjectTab.RotationParent = SetupBool("Rotation Parent", PlayerEditor.Tab.Custom, editorTab: CustomObjectTab);
            CustomObjectTab.RequireAll = SetupBool("Require All", PlayerEditor.Tab.Custom, editorTab: CustomObjectTab);

            CustomObjectTab.VisibilitySettings = GenerateUIPart("Visibility", PlayerEditor.Tab.Custom, ValueType.Bool, editorTab: CustomObjectTab);

            var visibilityScrollRect = Creator.NewUIObject("ScrollRect", CustomObjectTab.VisibilitySettings.GameObject.transform);
            visibilityScrollRect.transform.localScale = Vector3.one;
            UIManager.SetRectTransform(visibilityScrollRect.transform.AsRT(), new Vector2(64f, 0f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(600f, 0f));
            var visibilityScrollRectSR = visibilityScrollRect.AddComponent<ScrollRect>();

            var visibilityMaskGO = Creator.NewUIObject("Mask", visibilityScrollRect.transform);
            visibilityMaskGO.transform.localScale = Vector3.one;
            visibilityMaskGO.transform.AsRT().anchoredPosition = new Vector2(0f, 0f);
            visibilityMaskGO.transform.AsRT().anchorMax = new Vector2(1f, 1f);
            visibilityMaskGO.transform.AsRT().anchorMin = new Vector2(0f, 0f);
            visibilityMaskGO.transform.AsRT().sizeDelta = new Vector2(0f, 0f);
            var visibilityMaskImage = visibilityMaskGO.AddComponent<Image>();
            visibilityMaskImage.color = new Color(1f, 1f, 1f, 0.04f);
            visibilityMaskGO.AddComponent<Mask>();

            var visibilityContentGO = Creator.NewUIObject("Content", visibilityMaskGO.transform);
            visibilityContentGO.transform.localScale = Vector3.one;
            UIManager.SetRectTransform(visibilityContentGO.transform.AsRT(), new Vector2(0f, -16f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(600f, 250f));

            var visibilityContentCSF = visibilityContentGO.AddComponent<ContentSizeFitter>();
            visibilityContentCSF.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            visibilityContentCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

            var visibilityContentVLG = visibilityContentGO.AddComponent<VerticalLayoutGroup>();
            visibilityContentVLG.childControlHeight = false;
            visibilityContentVLG.childForceExpandHeight = false;
            visibilityContentVLG.spacing = 4f;

            var visibilityContentLE = visibilityContentGO.AddComponent<LayoutElement>();
            visibilityContentLE.layoutPriority = 10000;
            visibilityContentLE.minWidth = 349;

            visibilityScrollRectSR.content = visibilityContentGO.transform.AsRT();

            CustomObjectTab.ViewAnimations = SetupButton("Animations", PlayerEditor.Tab.Custom, "View Animations", editorTab: CustomObjectTab);
            CustomObjectTab.Modifiers = SetupModifiers("  Modifiers", PlayerEditor.Tab.Tail, editorTab: CustomObjectTab);

            #endregion

            // Functions
            {
                var functionsSpacer = Creator.NewUIObject("spacer", dialog.transform);
                functionsSpacer.transform.AsRT().sizeDelta = new Vector2(765f, 54f);

                var functionsLayout = Creator.NewUIObject("layout", functionsSpacer.transform);
                UIManager.SetRectTransform(functionsLayout.transform.AsRT(), new Vector2(8f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(760f, 43.2f));
                var functionsLayoutHLG = functionsLayout.AddComponent<HorizontalLayoutGroup>();
                functionsLayoutHLG.childControlWidth = false;
                functionsLayoutHLG.childForceExpandWidth = false;
                functionsLayoutHLG.spacing = 2f;

                var select = EditorPrefabHolder.Instance.Function1Button.Duplicate(functionsLayout.transform, "function");
                select.transform.AsRT().sizeDelta = new Vector2(92f, 43.2f);
                var selectStorage = select.GetComponent<FunctionButtonStorage>();
                selectStorage.label.fontSize = 16;
                selectStorage.label.text = "Select";
                selectStorage.button.onClick.NewListener(() =>
                {
                    PlayerEditor.inst.ModelsPopup.Open();
                    CoroutineHelper.StartCoroutine(PlayerEditor.inst.RefreshModels());
                });

                EditorThemeManager.AddSelectable(selectStorage.button, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(selectStorage.label, ThemeGroup.Function_2_Text);

                var playerIndexObject = EditorPrefabHolder.Instance.Dropdown.Duplicate(functionsLayout.transform, "dropdown");
                playerIndexObject.transform.AsRT().sizeDelta = new Vector2(200f, 43.2f);
                var playerIndexDropdown = playerIndexObject.GetComponent<Dropdown>();
                playerIndexDropdown.options = new List<Dropdown.OptionData>();
                for (int i = 1; i <= 4; i++)
                    playerIndexDropdown.options.Add(new Dropdown.OptionData($"Player {i}"));
                playerIndexDropdown.SetValueWithoutNotify(0);
                playerIndexDropdown.onValueChanged.NewListener(_val =>
                {
                    PlayerEditor.inst.playerModelIndex = _val;
                    CoroutineHelper.StartCoroutine(PlayerEditor.inst.RefreshEditor());
                });

                EditorThemeManager.AddDropdown(playerIndexDropdown);

                var create = EditorPrefabHolder.Instance.Function1Button.Duplicate(functionsLayout.transform, "function");
                create.transform.AsRT().sizeDelta = new Vector2(92f, 43.2f);
                var createStorage = create.GetComponent<FunctionButtonStorage>();
                createStorage.label.fontSize = 16;
                createStorage.label.text = "Create";
                createStorage.button.onClick.NewListener(PlayerEditor.inst.CreateNewModel);

                EditorThemeManager.AddSelectable(createStorage.button, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(createStorage.label, ThemeGroup.Function_2_Text);

                var save = EditorPrefabHolder.Instance.Function1Button.Duplicate(functionsLayout.transform, "function");
                save.transform.AsRT().sizeDelta = new Vector2(92f, 43.2f);
                var saveStorage = save.GetComponent<FunctionButtonStorage>();
                saveStorage.label.fontSize = 16;
                saveStorage.label.text = "Save";
                saveStorage.button.onClick.NewListener(PlayerEditor.inst.Save);

                EditorThemeManager.AddSelectable(saveStorage.button, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(saveStorage.label, ThemeGroup.Function_2_Text);

                var load = EditorPrefabHolder.Instance.Function1Button.Duplicate(functionsLayout.transform, "function");
                load.transform.AsRT().sizeDelta = new Vector2(92f, 43.2f);
                var loadStorage = load.GetComponent<FunctionButtonStorage>();
                loadStorage.label.fontSize = 16;
                loadStorage.label.text = "Reload";
                loadStorage.button.onClick.NewListener(PlayerEditor.inst.Reload);

                EditorThemeManager.AddSelectable(loadStorage.button, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(loadStorage.label, ThemeGroup.Function_2_Text);

                var setToGlobal = EditorPrefabHolder.Instance.Function1Button.Duplicate(functionsLayout.transform, "function");
                setToGlobal.transform.AsRT().sizeDelta = new Vector2(92f, 43.2f);
                var setToGlobalStorage = setToGlobal.GetComponent<FunctionButtonStorage>();
                setToGlobalStorage.label.fontSize = 16;
                setToGlobalStorage.label.text = "Set to Global";
                setToGlobalStorage.button.onClick.NewListener(() => PlayerManager.PlayerIndexes[PlayerEditor.inst.playerModelIndex].Value = PlayersData.Current.playerModelsIndex[PlayerEditor.inst.playerModelIndex]);

                EditorThemeManager.AddSelectable(setToGlobalStorage.button, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(setToGlobalStorage.label, ThemeGroup.Function_2_Text);
            }

            LSHelpers.SetActiveChildren(Content, false);
        }

        #region Setup

        void SetupTabButton(string name, int tabIndex)
        {
            var tab = EditorPrefabHolder.Instance.Function1Button.Duplicate(TabsParent.transform, name);
            tab.transform.AsRT().sizeDelta = new Vector2(92f, 43.2f);
            var tabButton = tab.GetComponent<FunctionButtonStorage>();
            tabButton.label.fontSize = 16;
            tabButton.label.text = name;
            tabButton.button.onClick.NewListener(() =>
            {
                PlayerEditor.inst.CurrentTab = (PlayerEditor.Tab)tabIndex;
                CoroutineHelper.StartCoroutine(PlayerEditor.inst.RefreshEditor());
            });

            EditorThemeManager.AddSelectable(tabButton.button, EditorTheme.GetGroup($"Tab Color {tabIndex + 1}"));
            tab.AddComponent<ContrastColors>().Init(tabButton.label, tab.GetComponent<Image>());

            TabButtons.Add(tabButton);
        }

        GameObject SetupPart(string name, string text = null, PlayerEditorTab editorTab = null)
        {
            var gameObject = Creator.NewUIObject(name, Content);
            gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 42f);

            var label = labelPrefab.Duplicate(gameObject.transform, "label");
            var labelText = label.GetComponent<Text>();
            labelText.text = text ?? name;
            UIManager.SetRectTransform(label.transform.AsRT(), new Vector2(32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(762f, 32f));
            EditorThemeManager.AddLightText(labelText);

            return gameObject;
        }

        PlayerEditorElement GenerateUIPart(string name, PlayerEditor.Tab tab, ValueType valueType, int index = -1, string text = null, PlayerEditorTab editorTab = null)
        {
            var ui = new PlayerEditorElement
            {
                Name = name,
                GameObject = SetupPart(name, text),
                Tab = tab,
                ValueType = valueType,
            };
            AddElement(ui, tab, editorTab);
            return ui;
        }

        void AddElement(PlayerEditorElement element, PlayerEditor.Tab tab, PlayerEditorTab editorTab = null)
        {
            PlayerEditorTab tabUI = editorTab ?? tab switch
            {
                PlayerEditor.Tab.Global => GlobalTab,
                PlayerEditor.Tab.Base => BaseTab,
                PlayerEditor.Tab.GUI => GUITab,
                PlayerEditor.Tab.Head => HeadTab,
                PlayerEditor.Tab.Boost => BoostTab,
                PlayerEditor.Tab.Spawners => SpawnerTab,
                PlayerEditor.Tab.Tail => TailTab,
                PlayerEditor.Tab.Custom => CustomObjectTab,
                _ => null,
            };

            if (tabUI)
                tabUI.elements.Add(element);
            elements.Add(element);
        }

        public PlayerEditorToggle SetupBool(string name, PlayerEditor.Tab tab, PlayerEditorTab editorTab = null)
        {
            var gameObject = Creator.NewUIObject(name, Content);
            gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 42f);

            var label = labelPrefab.Duplicate(gameObject.transform, "label");
            var labelText = label.GetComponent<Text>();
            labelText.text = name;
            EditorThemeManager.AddLightText(labelText);
            UIManager.SetRectTransform(label.transform.AsRT(), new Vector2(32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(762f, 32f));

            var toggle = EditorPrefabHolder.Instance.Toggle.Duplicate(gameObject.transform, "toggle").GetComponent<Toggle>();
            toggle.transform.AsRT().anchoredPosition = new Vector2(725f, -21f);

            EditorThemeManager.AddToggle(toggle);

            var ui = new PlayerEditorToggle
            {
                Name = name,
                GameObject = gameObject,
                Toggle = toggle,
                Tab = tab,
                ValueType = ValueType.Bool,
            };
            AddElement(ui, tab, editorTab);
            return ui;
        }

        public PlayerEditorNumber SetupNumber(string name, PlayerEditor.Tab tab, ValueType valueType = ValueType.Float, PlayerEditorTab editorTab = null)
        {
            var gameObject = Creator.NewUIObject(name, Content);
            gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 42f);

            var label = labelPrefab.Duplicate(gameObject.transform, "label");
            var labelText = label.GetComponent<Text>();
            labelText.text = name;
            EditorThemeManager.AddLightText(labelText);
            UIManager.SetRectTransform(label.transform.AsRT(), new Vector2(32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(762f, 32f));

            var input = EditorPrefabHolder.Instance.NumberInputField.Duplicate(gameObject.transform, "input");
            UIManager.SetRectTransform(input.transform.AsRT(), new Vector2(214f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 32f));

            var inputFieldStorage = input.GetComponent<InputFieldStorage>();

            CoreHelper.Delete(inputFieldStorage.middleButton.gameObject);
            EditorThemeManager.AddInputField(inputFieldStorage);

            var ui = new PlayerEditorNumber
            {
                Name = name,
                GameObject = gameObject,
                Field = inputFieldStorage,
                Tab = tab,
                ValueType = valueType,
            };
            AddElement(ui, tab, editorTab);
            return ui;
        }

        public PlayerEditorDropdown SetupDropdown(string name, List<Dropdown.OptionData> options, PlayerEditor.Tab tab, PlayerEditorTab editorTab = null)
        {
            var gameObject = Creator.NewUIObject(name, Content);
            gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 42f);

            var label = labelPrefab.Duplicate(gameObject.transform, "label");
            var labelText = label.GetComponent<Text>();
            labelText.text = name;
            EditorThemeManager.AddLightText(labelText);
            UIManager.SetRectTransform(label.transform.AsRT(), new Vector2(32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(762f, 32f));

            var dropdown = EditorPrefabHolder.Instance.Dropdown.Duplicate(gameObject.transform, "dropdown").GetComponent<Dropdown>();
            RectValues.Default.AnchoredPosition(212f, 0f).SizeDelta(300f, 32f).AssignToRectTransform(dropdown.transform.AsRT());
            dropdown.onValueChanged.ClearAll();
            dropdown.options = options;
            dropdown.GetComponent<HideDropdownOptions>().DisabledOptions.Clear();

            EditorThemeManager.AddDropdown(dropdown);

            var ui = new PlayerEditorDropdown
            {
                Name = name,
                GameObject = gameObject,
                Dropdown = dropdown,
                Tab = tab,
                ValueType = ValueType.Enum,
            };
            AddElement(ui, tab, editorTab);
            return ui;
        }

        public PlayerEditorVector2 SetupVector2(string name, PlayerEditor.Tab tab, PlayerEditorTab editorTab = null)
        {
            var gameObject = Creator.NewUIObject(name, Content);
            gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 42f);

            var label = labelPrefab.Duplicate(gameObject.transform, "label");
            var labelText = label.GetComponent<Text>();
            labelText.text = name;
            EditorThemeManager.AddLightText(labelText);
            UIManager.SetRectTransform(label.transform.AsRT(), new Vector2(32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(762f, 32f));

            var inputX = EditorPrefabHolder.Instance.NumberInputField.Duplicate(gameObject.transform, "x");
            UIManager.SetRectTransform(inputX.transform.AsRT(), new Vector2(-4f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(230f, 32f));

            var inputXStorage = inputX.GetComponent<InputFieldStorage>();

            CoreHelper.Delete(inputXStorage.middleButton.gameObject);
            CoreHelper.Delete(inputXStorage.leftGreaterButton.gameObject);
            CoreHelper.Delete(inputXStorage.rightGreaterButton.gameObject);
            EditorThemeManager.AddInputField(inputXStorage);

            var inputY = EditorPrefabHolder.Instance.NumberInputField.Duplicate(gameObject.transform, "y");
            UIManager.SetRectTransform(inputY.transform.AsRT(), new Vector2(246f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(230f, 32f));

            var inputYStorage = inputY.GetComponent<InputFieldStorage>();

            CoreHelper.Delete(inputYStorage.middleButton.gameObject);
            CoreHelper.Delete(inputYStorage.leftGreaterButton.gameObject);
            CoreHelper.Delete(inputYStorage.rightGreaterButton.gameObject);
            EditorThemeManager.AddInputField(inputYStorage);

            var ui = new PlayerEditorVector2
            {
                Name = name,
                GameObject = gameObject,
                XField = inputXStorage,
                YField = inputYStorage,
                Tab = tab,
                ValueType = ValueType.Vector2,
            };
            AddElement(ui, tab, editorTab);
            return ui;
        }

        public PlayerEditorButton SetupButton(string name, PlayerEditor.Tab tab, string text = null, PlayerEditorTab editorTab = null)
        {
            var gameObject = SetupPart(name, text);

            RectValues.Default.AnchoredPosition(32f, 0f).SizeDelta(750f, 32f).AssignToRectTransform(gameObject.transform.GetChild(0).AsRT());

            var image = gameObject.AddComponent<Image>();
            var button = gameObject.AddComponent<Button>();
            button.image = image;

            EditorThemeManager.AddSelectable(button, ThemeGroup.List_Button_1);

            var ui = new PlayerEditorButton
            {
                Name = name,
                GameObject = gameObject,
                Button = button,
                Tab = tab,
                ValueType = ValueType.Function,
            };
            AddElement(ui, tab, editorTab);
            return ui;
        }

        public PlayerEditorString SetupString(string name, PlayerEditor.Tab tab, PlayerEditorTab editorTab = null)
        {
            var gameObject = SetupPart(name);

            var input = EditorPrefabHolder.Instance.NumberInputField.transform.Find("input").gameObject.Duplicate(gameObject.transform, "input");
            UIManager.SetRectTransform(input.transform.AsRT(), new Vector2(260f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(200f, 32f));

            var inputField = input.GetComponent<InputField>();
            EditorThemeManager.AddInputField(inputField);

            var ui = new PlayerEditorString
            {
                Name = name,
                GameObject = gameObject,
                Field = inputField,
                Tab = tab,
                ValueType = ValueType.String,
            };
            AddElement(ui, tab, editorTab);
            return ui;
        }

        public PlayerEditorColor SetupColor(string name, PlayerEditor.Tab tab, PlayerEditorTab editorTab = null)
        {
            var gameObject = SetupPart(name);

            var colorsLayout = Creator.NewUIObject("colors", gameObject.transform);
            colorsLayout.transform.AsRT().anchoredPosition = new Vector2(170f, -16f);
            colorsLayout.transform.AsRT().sizeDelta = new Vector2(400f, 100f);
            var layoutGLG = colorsLayout.AddComponent<GridLayoutGroup>();
            layoutGLG.cellSize = new Vector2(32f, 32f);
            layoutGLG.spacing = new Vector2(8f, 8f);

            gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 162f);

            var colorButtons = new List<Button>();
            for (int j = 0; j < 25; j++)
            {
                var color = EditorManager.inst.colorGUI.Duplicate(colorsLayout.transform, $"{j + 1}");
                EditorThemeManager.AddGraphic(color.GetComponent<Image>(), ThemeGroup.Null, true);
                EditorThemeManager.AddGraphic(color.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Background_1);
                colorButtons.Add(color.GetComponent<Button>());
            }

            var ui = new PlayerEditorColor
            {
                Name = name,
                GameObject = gameObject,
                ColorButtons = colorButtons,
                Tab = tab,
                ValueType = ValueType.Color,
            };
            AddElement(ui, tab, editorTab);
            return ui;
        }

        public PlayerEditorShape SetupShape(string name, PlayerEditor.Tab tab, PlayerEditorTab editorTab = null)
        {
            var gameObject = SetupPart(name);

            gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 92f);

            var shape = ObjEditor.inst.ObjectView.transform.Find("shape").gameObject.Duplicate(gameObject.transform, "shape");
            UIManager.SetRectTransform(shape.transform.AsRT(), new Vector2(568f, -16f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(351f, 32f));
            var shapeSettings = ObjEditor.inst.ObjectView.transform.Find("shapesettings").gameObject.Duplicate(gameObject.transform, "shapesettings");
            UIManager.SetRectTransform(shapeSettings.transform.AsRT(), new Vector2(568f, -54f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(351f, 32f));

            var text = shapeSettings.transform.Find("5");
            var textLayoutElement = text.gameObject.AddComponent<LayoutElement>();
            textLayoutElement.ignoreLayout = true;
            UIManager.SetRectTransform(text.AsRT(), new Vector2(0f, -24f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(351f, 74f));

            var shapeToggles = new List<Toggle>();
            var shapeOptionToggles = new List<List<Toggle>>();

            for (int i = 0; i < shape.transform.childCount; i++)
            {
                var toggle = shape.transform.GetChild(i).GetComponent<Toggle>();
                if (toggle)
                    shapeToggles.Add(toggle);
            }
            for (int i = 0; i < shapeSettings.transform.childCount; i++)
            {
                shapeOptionToggles.Add(new List<Toggle>());

                var shapeType = (ShapeType)i;
                if (shapeType == ShapeType.Text || shapeType == ShapeType.Image || shapeType == ShapeType.Polygon)
                    continue;

                var child = shapeSettings.transform.GetChild(i);
                for (int j = 0; j < child.childCount; j++)
                {
                    var toggle = child.GetChild(j).GetComponent<Toggle>();
                    if (toggle)
                        shapeOptionToggles[i].Add(toggle);
                }
            }

            var ui = new PlayerEditorShape
            {
                Name = name,
                GameObject = gameObject,
                ShapeToggles = shapeToggles,
                ShapeOptionToggles = shapeOptionToggles,
                Tab = tab,
                ValueType = ValueType.Float,
            };
            AddElement(ui, tab, editorTab);
            return ui;
        }

        public PlayerEditorModifiers SetupModifiers(string name, PlayerEditor.Tab tab, PlayerEditorTab editorTab = null)
        {
            var gameObject = SetupPart(name);
            gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 128f);
            RectValues.Default.AnchoredPosition(32f, 0f).SizeDelta(750f, 32f).AssignToRectTransform(gameObject.transform.GetChild(0).AsRT());

            var layout = gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 8f;

            var image = gameObject.AddComponent<Image>();
            EditorThemeManager.AddGraphic(image, ThemeGroup.Background_2, true);

            var dialog = new ModifiersEditorDialog();
            dialog.Init(gameObject.transform, false, false, false);
            dialog.showModifiersFunc = _val => gameObject.transform.AsRT().sizeDelta = new Vector2(750f, _val ? 620f : 128f);

            var ui = new PlayerEditorModifiers
            {
                Name = name,
                GameObject = gameObject,
                Modifiers = dialog,
                Tab = tab,
                ValueType = ValueType.Function,
            };
            AddElement(ui, tab, editorTab);
            return ui;
        }

        public PlayerEditorTags SetupTags(string name, PlayerEditor.Tab tab, PlayerEditorTab editorTab = null)
        {
            var gameObject = SetupPart(name);

            // Tags Scroll View/Viewport/Content
            var tagScrollView = Creator.NewUIObject("Tags Scroll View", gameObject.transform);
            var tagsScrollView = tagScrollView.transform.AsRT();
            tagsScrollView.sizeDelta = new Vector2(522f, 40f);

            var scroll = tagScrollView.AddComponent<ScrollRect>();
            scroll.scrollSensitivity = 20f;
            scroll.horizontal = true;
            scroll.vertical = false;

            var image = tagScrollView.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.01f);

            var mask = tagScrollView.AddComponent<Mask>();

            var tagViewport = Creator.NewUIObject("Viewport", tagScrollView.transform);
            RectValues.FullAnchored.AssignToRectTransform(tagViewport.transform.AsRT());

            var tagContent = Creator.NewUIObject("Content", tagViewport.transform);
            var tagsContent = tagContent.transform.AsRT();

            var tagContentGLG = tagContent.AddComponent<GridLayoutGroup>();
            tagContentGLG.cellSize = new Vector2(168f, 32f);
            tagContentGLG.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            tagContentGLG.constraintCount = 1;
            tagContentGLG.childAlignment = TextAnchor.MiddleLeft;
            tagContentGLG.spacing = new Vector2(8f, 0f);

            var tagContentCSF = tagContent.AddComponent<ContentSizeFitter>();
            tagContentCSF.horizontalFit = ContentSizeFitter.FitMode.MinSize;
            tagContentCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

            scroll.viewport = tagViewport.transform.AsRT();
            scroll.content = tagsContent;

            var ui = new PlayerEditorTags
            {
                Name = name,
                GameObject = gameObject,
                TagsScrollView = tagsScrollView,
                TagsContent = tagsContent,
                Tab = tab,
                ValueType = ValueType.Function,
            };
            AddElement(ui, tab, editorTab);
            return ui;
        }

        public void SetupObjectTab(PlayerEditorObjectTab objectTab, string name, PlayerEditor.Tab tab, bool doActive = true, bool doTrail = true, bool doParticles = true, bool doRemove = false)
        {
            if (!string.IsNullOrEmpty(name))
                name += " ";

            if (doRemove)
                objectTab.Remove = SetupButton($"{name}Remove", tab, editorTab: objectTab);

            if (doActive)
                objectTab.Active = SetupBool($"{name}Active", tab, editorTab: objectTab);
            objectTab.Shape = SetupShape($"{name}Shape", tab, editorTab: objectTab);
            objectTab.Position = SetupVector2($"{name}Position", tab, editorTab: objectTab);
            objectTab.Scale = SetupVector2($"{name}Scale", tab, editorTab: objectTab);
            objectTab.Rotation = SetupNumber($"{name}Rotation", tab, editorTab: objectTab);
            objectTab.Color = SetupColor($"{name}Color", tab, editorTab: objectTab);
            objectTab.CustomColor = SetupString($"{name}Custom Color", tab, editorTab: objectTab);
            objectTab.Opacity = SetupNumber($"{name}Opacity", tab, editorTab: objectTab);
            objectTab.Depth = SetupNumber($"{name}Depth", tab, editorTab: objectTab);

            if (doTrail)
            {
                objectTab.TrailEmitting = SetupBool($"{name}Trail Emitting", tab, editorTab: objectTab);
                objectTab.TrailTime = SetupNumber($"{name}Trail Time", tab, editorTab: objectTab);
                objectTab.TrailStartWidth = SetupNumber($"{name}Trail Start Width", tab, editorTab: objectTab);
                objectTab.TrailEndWidth = SetupNumber($"{name}Trail End Width", tab, editorTab: objectTab);
                objectTab.TrailStartColor = SetupColor($"{name}Trail Start Color", tab, editorTab: objectTab);
                objectTab.TrailStartCustomColor = SetupString($"{name}Trail Start Custom Color", tab, editorTab: objectTab);
                objectTab.TrailStartOpacity = SetupNumber($"{name}Trail Start Opacity", tab, editorTab: objectTab);
                objectTab.TrailEndColor = SetupColor($"{name}Trail End Color", tab, editorTab: objectTab);
                objectTab.TrailEndCustomColor = SetupString($"{name}Trail End Custom Color", tab, editorTab: objectTab);
                objectTab.TrailEndOpacity = SetupNumber($"{name}Trail End Opacity", tab, editorTab: objectTab);
                objectTab.TrailPositionOffset = SetupVector2($"{name}Trail Position Offset", tab, editorTab: objectTab);
            }

            if (doParticles)
            {
                objectTab.ParticlesEmitting = SetupBool($"{name}Particles Emitting", tab, editorTab: objectTab);
                objectTab.ParticlesShape = SetupShape($"{name}Particles Shape", tab, editorTab: objectTab);
                objectTab.ParticlesColor = SetupColor($"{name}Particles Color", tab, editorTab: objectTab);
                objectTab.ParticlesCustomColor = SetupString($"{name}Particles Custom Color", tab, editorTab: objectTab);
                objectTab.ParticlesStartOpacity = SetupNumber($"{name}Particles Start Opacity", tab, editorTab: objectTab);
                objectTab.ParticlesEndOpacity = SetupNumber($"{name}Particles End Opacity", tab, editorTab: objectTab);
                objectTab.ParticlesStartScale = SetupNumber($"{name}Particles Start Scale", tab, editorTab: objectTab);
                objectTab.ParticlesEndScale = SetupNumber($"{name}Particles End Scale", tab, editorTab: objectTab);
                objectTab.ParticlesRotation = SetupNumber($"{name}Particles Rotation", tab, editorTab: objectTab);
                objectTab.ParticlesLifetime = SetupNumber($"{name}Particles Lifetime", tab, editorTab: objectTab);
                objectTab.ParticlesSpeed = SetupNumber($"{name}Particles Speed", tab, editorTab: objectTab);
                objectTab.ParticlesAmount = SetupNumber($"{name}Particles Amount", tab, editorTab: objectTab);
                objectTab.ParticlesForce = SetupVector2($"{name}Particles Force", tab, editorTab: objectTab);
                objectTab.ParticlesTrailEmitting = SetupBool($"{name}Particles Trail Emitting", tab, editorTab: objectTab);
            }
        }

        #endregion
    }

    #region Tab Classes

    public class PlayerEditorGlobalTab : PlayerEditorTab
    {
        public PlayerEditorButton RespawnPlayers { get; set; } 

        public PlayerEditorButton UpdateProperties { get; set; }

        public PlayerEditorToggle SpawnPlayers { get; set; }
        public PlayerEditorToggle RespawnImmediately { get; set; }

        public PlayerEditorToggle AllowCustomPlayerModels { get; set; }
        public PlayerEditorToggle AllowPlayerModelControls { get; set; }

        public PlayerEditorNumber Speed { get; set; }

        public PlayerEditorToggle LockBoost { get; set; }

        public PlayerEditorDropdown GameMode { get; set; }

        public PlayerEditorNumber MaxHealth { get; set; }
        public PlayerEditorNumber MaxJumpCount { get; set; }
        public PlayerEditorNumber MaxJumpBoostCount { get; set; }
        public PlayerEditorNumber JumpGravity { get; set; }
        public PlayerEditorNumber JumpIntensity { get; set; }

        public PlayerEditorToggle LimitPlayer { get; set; }
        public PlayerEditorVector2 LimitMoveSpeed { get; set; }
        public PlayerEditorVector2 LimitBoostSpeed { get; set; }
        public PlayerEditorVector2 LimitBoostCooldown { get; set; }
        public PlayerEditorVector2 LimitBoostMinTime { get; set; }
        public PlayerEditorVector2 LimitBoostMaxTime { get; set; }
        public PlayerEditorVector2 LimitHitCooldown { get; set; }
    }

    public class PlayerEditorBaseTab : PlayerEditorTab
    {
        public PlayerEditorButton ID { get; set; }
        public PlayerEditorString Name { get; set; }
        public PlayerEditorString Creator { get; set; }
        public PlayerEditorString Version { get; set; }
        public PlayerEditorToggle EditControls { get; set; }
        public PlayerEditorNumber Health { get; set; }
        public PlayerEditorNumber Lives { get; set; }
        public PlayerEditorNumber MoveSpeed { get; set; }
        public PlayerEditorNumber BoostSpeed { get; set; }
        public PlayerEditorNumber BoostCooldown { get; set; }
        public PlayerEditorNumber MinBoostTime { get; set; }
        public PlayerEditorNumber MaxBoostTime { get; set; }
        public PlayerEditorNumber HitCooldown { get; set; }
        public PlayerEditorDropdown RotateMode { get; set; }
        public PlayerEditorNumber RotationSpeed { get; set; }
        public PlayerEditorDropdown RotationCurve { get; set; }
        public PlayerEditorToggle CollisionAccurate { get; set; }
        public PlayerEditorToggle SprintSneakActive { get; set; }
        public PlayerEditorNumber SprintSpeed { get; set; }
        public PlayerEditorNumber SneakSpeed { get; set; }

        public PlayerEditorToggle CanBoost { get; set; }

        public PlayerEditorNumber JumpGravity { get; set; }
        public PlayerEditorNumber JumpIntensity { get; set; }
        public PlayerEditorNumber JumpCount { get; set; }
        public PlayerEditorNumber JumpBoostCount { get; set; }
        public PlayerEditorNumber Bounciness { get; set; }

        public PlayerEditorToggle StretchActive { get; set; }
        public PlayerEditorNumber StretchAmount { get; set; }
        public PlayerEditorDropdown StretchEasing { get; set; }

        public PlayerEditorVector2 FacePosition { get; set; }
        public PlayerEditorToggle FaceControlActive { get; set; }

        public PlayerEditorModifiers TickModifiers { get; set; }

        public PlayerEditorModifiers ModelModifiers { get; set; }
    }

    public class PlayerEditorGUITab : PlayerEditorTab
    {
        public PlayerEditorToggle HealthActive { get; set; }
        public PlayerEditorDropdown HealthMode { get; set; }
        public PlayerEditorColor HealthTopColor { get; set; }
        public PlayerEditorString HealthTopCustomColor { get; set; }
        public PlayerEditorNumber HealthTopOpacity { get; set; }
        public PlayerEditorColor HealthBaseColor { get; set; }
        public PlayerEditorString HealthBaseCustomColor { get; set; }
        public PlayerEditorNumber HealthBaseOpacity { get; set; }
    }

    public class PlayerEditorObjectTab : PlayerEditorTab
    {
        public PlayerEditorButton Remove { get; set; }

        #region Main

        public PlayerEditorToggle Active { get; set; }
        public PlayerEditorShape Shape { get; set; }
        public PlayerEditorVector2 Position { get; set; }
        public PlayerEditorVector2 Scale { get; set; }
        public PlayerEditorNumber Rotation { get; set; }
        public PlayerEditorColor Color { get; set; }
        public PlayerEditorString CustomColor { get; set; }
        public PlayerEditorNumber Opacity { get; set; }
        public PlayerEditorNumber Depth { get; set; }

        #endregion

        #region Trail

        public PlayerEditorToggle TrailEmitting { get; set; }
        public PlayerEditorNumber TrailTime { get; set; }
        public PlayerEditorNumber TrailStartWidth { get; set; }
        public PlayerEditorNumber TrailEndWidth { get; set; }
        public PlayerEditorColor TrailStartColor { get; set; }
        public PlayerEditorString TrailStartCustomColor { get; set; }
        public PlayerEditorNumber TrailStartOpacity { get; set; }
        public PlayerEditorColor TrailEndColor { get; set; }
        public PlayerEditorString TrailEndCustomColor { get; set; }
        public PlayerEditorNumber TrailEndOpacity { get; set; }
        public PlayerEditorVector2 TrailPositionOffset { get; set; }

        #endregion

        #region Particles

        public PlayerEditorToggle ParticlesEmitting { get; set; }
        public PlayerEditorShape ParticlesShape { get; set; }
        public PlayerEditorColor ParticlesColor { get; set; }
        public PlayerEditorString ParticlesCustomColor { get; set; }
        public PlayerEditorNumber ParticlesStartOpacity { get; set; }
        public PlayerEditorNumber ParticlesEndOpacity { get; set; }
        public PlayerEditorNumber ParticlesStartScale { get; set; }
        public PlayerEditorNumber ParticlesEndScale { get; set; }
        public PlayerEditorNumber ParticlesRotation { get; set; }
        public PlayerEditorNumber ParticlesLifetime { get; set; }
        public PlayerEditorNumber ParticlesSpeed { get; set; }
        public PlayerEditorNumber ParticlesAmount { get; set; }
        public PlayerEditorVector2 ParticlesForce { get; set; }
        public PlayerEditorToggle ParticlesTrailEmitting { get; set; }

        #endregion
    }

    public class PlayerEditorSpawnerTab : PlayerEditorTab
    {
        #region Pulse

        public PlayerEditorToggle PulseActive { get; set; }
        public PlayerEditorToggle PulseRotateToHead { get; set; }
        public PlayerEditorShape PulseShape { get; set; }
        public PlayerEditorNumber PulseDuration { get; set; }
        public PlayerEditorColor PulseStartColor { get; set; }
        public PlayerEditorString PulseStartCustomColor { get; set; }
        public PlayerEditorNumber PulseStartOpacity { get; set; }
        public PlayerEditorColor PulseEndColor { get; set; }
        public PlayerEditorString PulseEndCustomColor { get; set; }
        public PlayerEditorNumber PulseEndOpacity { get; set; }
        public PlayerEditorDropdown PulseColorEasing { get; set; }
        public PlayerEditorDropdown PulseOpacityEasing { get; set; }
        public PlayerEditorNumber PulseDepth { get; set; }
        public PlayerEditorVector2 PulseStartPosition { get; set; }
        public PlayerEditorVector2 PulseEndPosition { get; set; }
        public PlayerEditorDropdown PulsePositionEasing { get; set; }
        public PlayerEditorVector2 PulseStartScale { get; set; }
        public PlayerEditorVector2 PulseEndScale { get; set; }
        public PlayerEditorDropdown PulseScaleEasing { get; set; }
        public PlayerEditorNumber PulseStartRotation { get; set; }
        public PlayerEditorNumber PulseEndRotation { get; set; }
        public PlayerEditorDropdown PulseRotationEasing { get; set; }

        #endregion

        #region Bullet

        public PlayerEditorToggle BulletActive { get; set; }
        public PlayerEditorToggle BulletAutoKill { get; set; }
        public PlayerEditorNumber BulletLifetime { get; set; }
        public PlayerEditorToggle BulletConstant { get; set; }
        public PlayerEditorToggle BulletHurtPlayers { get; set; }
        public PlayerEditorNumber BulletSpeed { get; set; }
        public PlayerEditorNumber BulletCooldown { get; set; }
        public PlayerEditorVector2 BulletOrigin { get; set; }

        public PlayerEditorShape BulletShape { get; set; }
        public PlayerEditorColor BulletStartColor { get; set; }
        public PlayerEditorString BulletStartCustomColor { get; set; }
        public PlayerEditorNumber BulletStartOpacity { get; set; }
        public PlayerEditorColor BulletEndColor { get; set; }
        public PlayerEditorString BulletEndCustomColor { get; set; }
        public PlayerEditorNumber BulletEndOpacity { get; set; }
        public PlayerEditorNumber BulletColorDuration { get; set; }
        public PlayerEditorDropdown BulletColorEasing { get; set; }
        public PlayerEditorNumber BulletOpacityDuration { get; set; }
        public PlayerEditorDropdown BulletOpacityEasing { get; set; }

        public PlayerEditorNumber BulletDepth { get; set; }
        public PlayerEditorVector2 BulletStartPosition { get; set; }
        public PlayerEditorVector2 BulletEndPosition { get; set; }
        public PlayerEditorNumber BulletPositionDuration { get; set; }
        public PlayerEditorDropdown BulletPositionEasing { get; set; }
        public PlayerEditorVector2 BulletStartScale { get; set; }
        public PlayerEditorVector2 BulletEndScale { get; set; }
        public PlayerEditorNumber BulletScaleDuration { get; set; }
        public PlayerEditorDropdown BulletScaleEasing { get; set; }
        public PlayerEditorNumber BulletStartRotation { get; set; }
        public PlayerEditorNumber BulletEndRotation { get; set; }
        public PlayerEditorNumber BulletRotationDuration { get; set; }
        public PlayerEditorDropdown BulletRotationEasing { get; set; }

        #endregion
    }

    public class PlayerEditorTailTab : PlayerEditorTab
    {
        public PlayerEditorNumber BaseDistance { get; set; }
        public PlayerEditorDropdown BaseMode { get; set; }
        public PlayerEditorToggle BaseGrows { get; set; }
        public PlayerEditorNumber BaseTime { get; set; }

        public PlayerEditorObjectTab BoostPart { get; set; } = new PlayerEditorObjectTab();

        public List<PlayerEditorObjectTab> TailParts { get; set; } = new List<PlayerEditorObjectTab>();

        public PlayerEditorButton AddTail { get; set; }

        public override void SetActive(Predicate<PlayerEditorElement> predicate)
        {
            base.SetActive(predicate);
            BoostPart.SetActive(predicate);
            for (int i = 0; i < TailParts.Count; i++)
                TailParts[i].SetActive(predicate);
        }

        public override void SetActive(bool active)
        {
            base.SetActive(active);
            BoostPart.SetActive(active);
            for (int i = 0; i < TailParts.Count; i++)
                TailParts[i].SetActive(active);
        }
    }

    public class PlayerEditorCustomObjectTab : PlayerEditorObjectTab
    {
        public PlayerEditorButton SelectCustomObject { get; set; }
        public PlayerEditorButton ID { get; set; }
        public PlayerEditorString Name { get; set; }
        public PlayerEditorTags Tags { get; set; }
        public PlayerEditorDropdown Parent { get; set; }
        public PlayerEditorString CustomParent { get; set; }
        public PlayerEditorNumber PositionOffset { get; set; }
        public PlayerEditorNumber ScaleOffset { get; set; }
        public PlayerEditorToggle ScaleParent { get; set; }
        public PlayerEditorNumber RotationOffset { get; set; }
        public PlayerEditorToggle RotationParent { get; set; }
        public PlayerEditorToggle RequireAll { get; set; }
        public PlayerEditorElement VisibilitySettings { get; set; }
        public PlayerEditorButton ViewAnimations { get; set; }
        public PlayerEditorModifiers Modifiers { get; set; }
    }

    public abstract class PlayerEditorTab : Exists
    {
        public List<PlayerEditorElement> elements = new List<PlayerEditorElement>();

        public virtual void SetActive(Predicate<PlayerEditorElement> predicate)
        {
            if (predicate == null)
                return;

            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (element.GameObject)
                    element.GameObject.SetActive(predicate(element));
            }
        }

        public virtual void SetActive(bool active)
        {
            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (element.GameObject)
                    element.GameObject.SetActive(active);
            }
        }

        public void ClearContent(Predicate<PlayerEditorElement> predicate)
        {
            if (predicate == null)
                return;

            for (int i = elements.Count - 1; i >= 0; i--)
            {
                var element = elements[i];
                if (!predicate(element))
                    continue;

                if (element.GameObject)
                    CoreHelper.Delete(element.GameObject);
                elements.RemoveAt(i);
            }
        }

        public void ClearContent()
        {
            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (element.GameObject)
                    CoreHelper.Delete(element.GameObject);
            }
            elements.Clear();
        }
    }

    #endregion

    #region Element Classes

    public class PlayerEditorTags : PlayerEditorElement, ITagDialog
    {
        public RectTransform TagsScrollView { get; set; }

        public RectTransform TagsContent { get; set; }

        public void Open() => PlayerEditor.inst.Dialog.Open();
    }

    public class PlayerEditorModifiers : PlayerEditorElement
    {
        public ModifiersEditorDialog Modifiers { get; set; }
    }

    public class PlayerEditorToggle : PlayerEditorElement
    {
        public Toggle Toggle { get; set; }
    }

    public class PlayerEditorNumber : PlayerEditorElement
    {
        public InputFieldStorage Field { get; set; }
    }

    public class PlayerEditorDropdown : PlayerEditorElement
    {
        public Dropdown Dropdown { get; set; }
    }

    public class PlayerEditorVector2 : PlayerEditorElement
    {
        public InputFieldStorage XField { get; set; }
        public InputFieldStorage YField { get; set; }
    }

    public class PlayerEditorButton : PlayerEditorElement
    {
        public Button Button { get; set; }
    }

    public class PlayerEditorString : PlayerEditorElement
    {
        public InputField Field { get; set; }
    }

    public class PlayerEditorColor : PlayerEditorElement
    {
        public List<Button> ColorButtons { get; set; }
    }

    public class PlayerEditorShape : PlayerEditorElement
    {
        public bool updatedShapes = false;
        public List<Toggle> ShapeToggles { get; set; } = new List<Toggle>();
        public List<List<Toggle>> ShapeOptionToggles { get; set; } = new List<List<Toggle>>();
    }

    public class PlayerEditorElement : Exists
    {
        public string Name { get; set; }
        public GameObject GameObject { get; set; }
        public PlayerEditor.Tab Tab { get; set; }
        public ValueType ValueType { get; set; }
        public bool ShowInDefault { get; set; }

        public bool IsActive(bool isDefault) => (!isDefault || ShowInDefault) && RTString.SearchString(PlayerEditor.inst.Dialog.SearchTerm, Name) && Tab == PlayerEditor.inst.CurrentTab;

        public override string ToString() => $"{Tab} - {Name}";
    }

    #endregion
}
