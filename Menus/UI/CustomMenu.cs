using BetterLegacy.Core.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SimpleJSON;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core;
using UnityEngine;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Menus.UI
{
    public class CustomMenu : MenuBase
    {
        public CustomMenu() : base(false)
        {

        }

        public override IEnumerator GenerateUI()
        {
            var canvas = UIManager.GenerateUICanvas(nameof(CustomMenu), null);
            this.canvas = canvas.Canvas.gameObject;

            var gameObject = Creator.NewUIObject("Layout", canvas.Canvas.transform);
            UIManager.SetRectTransform(gameObject.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);

            for (int i = 0; i < layouts.Count; i++)
            {
                var layout = layouts.ElementAt(i).Value;
                if (layout is MenuGridLayout gridLayout)
                {
                    SetupGridLayout(gridLayout, gameObject.transform);
                }
                if (layout is MenuHorizontalLayout horizontalLayout)
                {
                    SetupHorizontalLayout(horizontalLayout, gameObject.transform);
                }
                if (layout is MenuVerticalLayout verticalLayout)
                {
                    SetupVerticalLayout(verticalLayout, gameObject.transform);
                }
            }

            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                var parent = !string.IsNullOrEmpty(element.parentLayout) && layouts.ContainsKey(element.parentLayout) ? layouts[element.parentLayout].gameObject.transform : gameObject.transform;
                if (element is MenuButton menuButton)
                {
                    SetupButton(menuButton, parent);

                    while (element.isSpawning)
                        yield return null;

                    menuButton.clickable.onClick = p => { menuButton.ParseFunction(menuButton.funcJSON); };

                    continue;
                }
                SetupText(element, parent);

                while (element.isSpawning)
                    yield return null;

                element.clickable.onClick = p => { element.ParseFunction(element.funcJSON); };
            }

            isOpen = true;

            yield break;
        }

        public override void UpdateTheme()
        {
            if (useGameTheme && CoreHelper.InGame)
                Theme = CoreHelper.CurrentBeatmapTheme;

            base.UpdateTheme();
        }

        public bool useGameTheme;

        public override BeatmapTheme Theme { get; set; }

        public static CustomMenu Parse(JSONNode jn)
        {
            var customMenu = new CustomMenu();

            foreach (var keyValuePair in jn["layouts"].Linq)
            {
                if (customMenu.layouts.ContainsKey(keyValuePair.Key))
                    continue;

                var jnLayout = keyValuePair.Value;
                string layoutType = jnLayout["type"];
                switch (layoutType.ToLower())
                {
                    case "grid":
                        {
                            var gridLayout = MenuGridLayout.Parse(jnLayout);
                            gridLayout.name = keyValuePair.Key;
                            customMenu.layouts.Add(keyValuePair.Key, gridLayout);

                            break;
                        }
                    case "horizontal":
                        {
                            var horizontalLayout = MenuHorizontalLayout.Parse(jnLayout);
                            horizontalLayout.name = keyValuePair.Key;
                            customMenu.layouts.Add(keyValuePair.Key, horizontalLayout);

                            break;
                        }
                    case "vertical":
                        {
                            var verticalLayout = MenuVerticalLayout.Parse(jnLayout);
                            verticalLayout.name = keyValuePair.Key;
                            customMenu.layouts.Add(keyValuePair.Key, verticalLayout);

                            break;
                        }
                }
            }

            for (int i = 0; i < jn["elements"].Count; i++)
            {
                var jnElement = jn["elements"][i];
                string elementType = jnElement["type"];
                switch (elementType.ToLower())
                {
                    case "text":
                        {
                            customMenu.elements.Add(new MenuText
                            {
                                name = jnElement["name"],
                                parentLayout = jnElement["parent_layout"],
                                text = FontManager.inst.ReplaceProperties(jnElement["text"]),
                                rectJSON = jnElement["rect"],
                                color = jnElement["col"].AsInt,
                                textColor = jnElement["text_col"].AsInt,
                                selectedColor = jnElement["sel_col"].AsInt,
                                selectedTextColor = jnElement["sel_text_col"].AsInt,
                                length = jnElement["anim_length"].AsFloat,
                                funcJSON = jnElement["func"],
                            });

                            break;
                        }
                    case "button":
                        {
                            customMenu.elements.Add(new MenuButton
                            {
                                name = jnElement["name"],
                                parentLayout = jnElement["parent_layout"],
                                text = FontManager.inst.ReplaceProperties(jnElement["text"]),
                                selectionPosition = new Vector2Int(jnElement["select"]["x"].AsInt, jnElement["select"]["y"].AsInt),
                                rectJSON = jnElement["rect"],
                                color = jnElement["col"].AsInt,
                                textColor = jnElement["text_col"].AsInt,
                                selectedColor = jnElement["sel_col"].AsInt,
                                selectedTextColor = jnElement["sel_text_col"].AsInt,
                                length = jnElement["anim_length"].AsFloat,
                                funcJSON = jnElement["func"],
                            });

                            break;
                        }
                }
            }

            customMenu.Theme = BeatmapTheme.Parse(jn["theme"]);
            customMenu.useGameTheme = jn["game_theme"].AsBool;

            return customMenu;
        }
    }
}
