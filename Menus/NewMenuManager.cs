using BetterLegacy.Components;
using BetterLegacy.Core;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BetterLegacy.Core.Data;
using System.Collections;

namespace BetterLegacy.Menus
{
    public class NewMenuManager : MonoBehaviour
    {
        public static NewMenuManager inst;
        public static void Init() => Creator.NewGameObject(nameof(NewMenuManager), SystemManager.inst.transform).AddComponent<NewMenuManager>();

        public MenuBase CurrentMenu { get; set; }

        void Awake() => inst = this;

        void Update()
        {
            if (CurrentMenu == null)
                return;

            CurrentMenu.UpdateControls();
            CurrentMenu.UpdateTheme();
        }

        public void Test()
        {
            CurrentMenu = new ArcadeMenuTest();
        }
    }

    public class ArcadeMenuTest : MenuBase
    {
        public override IEnumerator GenerateUI()
        {
            var canvas = UIManager.GenerateUICanvas("Arcade Menu", null);

            var gameObject = Creator.NewUIObject("Layout", canvas.Canvas.transform);
            gameObject.transform.AsRT().anchoredPosition = Vector2.zero;

            var text = CreateText("Test Text", gameObject.transform, "This is a test!", new Vector2(0f, 200f), new Vector2(200f, 64f));
            elements.Add(text);

            while (text.isSpawning)
                yield return null;

            var button1 = CreateButton("Test 1", gameObject.transform, new Vector2Int(0, 0), "Test 1", new Vector2(-100f, 0f), new Vector2(100f, 64f));
            button1.clickable.onClick = pointerEventData =>
            {
                if (!isOpen)
                    return;

                AudioManager.inst.PlaySound("blip");
            };
            elements.Add(button1);

            while (button1.isSpawning)
                yield return null;

            var button2 = CreateButton("Test 2", gameObject.transform, new Vector2Int(1, 0), "Test 2", new Vector2(100f, 0f), new Vector2(100f, 64f));
            button2.clickable.onClick = pointerEventData =>
            {
                if (!isOpen)
                    return;

                AudioManager.inst.PlaySound("blip");
            };
            elements.Add(button2);

            while (button2.isSpawning)
                yield return null;

            var button3 = CreateButton("Test 3", gameObject.transform, new Vector2Int(0, 1), "Test 3", new Vector2(0f, -100f), new Vector2(100f, 64f));
            button3.clickable.onClick = pointerEventData =>
            {
                if (!isOpen)
                    return;

                AudioManager.inst.PlaySound("blip");
            };
            elements.Add(button3);

            while (button3.isSpawning)
                yield return null;

            isOpen = true;
            yield break;
        }

        public override BeatmapTheme Theme { get; set; } = new BeatmapTheme()
        {
            name = "Default Theme",
            backgroundColor = new Color(0.9f, 0.9f, 0.9f),
            guiColor = new Color(0.5f, 0.5f, 0.5f),
            guiAccentColor = new Color(1f, 0.6f, 0f),
            playerColors = new List<Color>
            {
                new Color(0.5f, 0.5f, 0.5f), // 0
                new Color(0.5f, 0.5f, 0.5f), // 1
                new Color(0.5f, 0.5f, 0.5f), // 2
                new Color(0.5f, 0.5f, 0.5f), // 3
            },
            objectColors = new List<Color>
            {
                new Color(0.5f, 0.5f, 0.5f), // 0
                new Color(0.5f, 0.5f, 0.5f), // 1
                new Color(0.5f, 0.5f, 0.5f), // 2
                new Color(0.5f, 0.5f, 0.5f), // 3
                new Color(0.5f, 0.5f, 0.5f), // 4
                new Color(0.5f, 0.5f, 0.5f), // 5
                new Color(0.5f, 0.5f, 0.5f), // 6
                new Color(0.5f, 0.5f, 0.5f), // 7
                new Color(0.5f, 0.5f, 0.5f), // 8
                new Color(0.5f, 0.5f, 0.5f), // 9
                new Color(0.5f, 0.5f, 0.5f), // 10
                new Color(0.5f, 0.5f, 0.5f), // 11
                new Color(0.5f, 0.5f, 0.5f), // 12
                new Color(0.5f, 0.5f, 0.5f), // 13
                new Color(0.5f, 0.5f, 0.5f), // 14
                new Color(0.5f, 0.5f, 0.5f), // 15
                new Color(0.5f, 0.5f, 0.5f), // 16
                new Color(0.5f, 0.5f, 0.5f), // 17
            },
            effectColors = new List<Color>
            {
                new Color(0.5f, 0.5f, 0.5f), // 0
                new Color(0.5f, 0.5f, 0.5f), // 1
                new Color(0.5f, 0.5f, 0.5f), // 2
                new Color(0.5f, 0.5f, 0.5f), // 3
                new Color(0.5f, 0.5f, 0.5f), // 4
                new Color(0.5f, 0.5f, 0.5f), // 5
                new Color(0.5f, 0.5f, 0.5f), // 6
                new Color(0.5f, 0.5f, 0.5f), // 7
                new Color(0.5f, 0.5f, 0.5f), // 8
                new Color(0.5f, 0.5f, 0.5f), // 9
                new Color(0.5f, 0.5f, 0.5f), // 10
                new Color(0.5f, 0.5f, 0.5f), // 11
                new Color(0.5f, 0.5f, 0.5f), // 12
                new Color(0.5f, 0.5f, 0.5f), // 13
                new Color(0.5f, 0.5f, 0.5f), // 14
                new Color(0.5f, 0.5f, 0.5f), // 15
                new Color(0.5f, 0.5f, 0.5f), // 16
                new Color(0.5f, 0.5f, 0.5f), // 17
            },
            backgroundColors = new List<Color>
            {
                new Color(0.5f, 0.5f, 0.5f), // 0
                new Color(0.5f, 0.5f, 0.5f), // 1
                new Color(0.5f, 0.5f, 0.5f), // 2
                new Color(0.5f, 0.5f, 0.5f), // 3
                new Color(0.5f, 0.5f, 0.5f), // 4
                new Color(0.5f, 0.5f, 0.5f), // 5
                new Color(0.5f, 0.5f, 0.5f), // 6
                new Color(0.5f, 0.5f, 0.5f), // 7
                new Color(0.5f, 0.5f, 0.5f), // 8
            },
        };
    }
}
