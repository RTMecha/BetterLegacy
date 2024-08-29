using BetterLegacy.Components.Editor;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

namespace BetterLegacy.Core.Helpers
{
    public static class TooltipHelper
    {
        public static Dictionary<string, List<HoverTooltip.Tooltip>> Tooltips { get; set; } = new Dictionary<string, List<HoverTooltip.Tooltip>>();

        public static void InitTooltips()
        {
            Tooltips.Clear();
            var jn = JSON.Parse(RTFile.ReadFromFile($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_tooltips.json"));
            for (int i = 0; i < jn["tooltip_groups"].Count; i++)
            {
                if (jn["tooltip_groups"][i]["name"] == null)
                    continue;

                var list = new List<HoverTooltip.Tooltip>();
                for (int j = 0; j < jn["tooltip_groups"][i]["tooltips"].Count; j++)
                {
                    var tooltipJN = jn["tooltip_groups"][i]["tooltips"][j];

                    List<string> keys = null;
                    if (tooltipJN["keys"] != null)
                    {
                        keys = new List<string>();
                        for (int k = 0; k < tooltipJN["keys"].Count; k++)
                            keys.Add(tooltipJN["keys"][k]);
                    }

                    int lang = 0;
                    if (tooltipJN["lang"] != null)
                        lang = tooltipJN["lang"].AsInt;

                    list.Add(NewTooltip(tooltipJN["desc"], tooltipJN["hint"], keys, (Language)lang));
                }

                var name = (string)jn["tooltip_groups"][i]["name"];
                if (!Tooltips.ContainsKey(name))
                    Tooltips.Add(name, list);
            }
        }

        public static void AssignTooltip(GameObject gameObject, string group, float time = 2f)
        {
            if (!Tooltips.ContainsKey(group))
                return;

            AddTooltip(gameObject, Tooltips[group], time);
        }

        public static void AddTooltip(GameObject gameObject, List<HoverTooltip.Tooltip> tooltips, float time)
        {
            var tooltip = gameObject.GetComponent<ShowTooltip>() ?? gameObject.AddComponent<ShowTooltip>();

            tooltip.time = time;
            tooltip.tooltips = tooltips;
        }

        public static void AssignTooltip(HoverTooltip hoverTooltip, string name)
        {
            if (!Tooltips.ContainsKey(name))
                return;

            hoverTooltip.tooltipLangauges.Clear();
            hoverTooltip.tooltipLangauges.AddRange(Tooltips[name]);
        }

        public static void AddHoverTooltip(GameObject gameObject, string desc, string hint, List<string> keys = null, Language language = Language.English, bool clear = false)
        {
            var hoverTooltip = gameObject.GetComponent<HoverTooltip>() ?? gameObject.AddComponent<HoverTooltip>();

            if (clear)
                hoverTooltip.tooltipLangauges.Clear();
            hoverTooltip.tooltipLangauges.Add(NewTooltip(desc, hint, keys, language));
        }

        public static void AddHoverTooltip(GameObject gameObject, List<HoverTooltip.Tooltip> tooltips, bool clear = true)
        {
            var hoverTooltip = gameObject.GetComponent<HoverTooltip>() ?? gameObject.AddComponent<HoverTooltip>();

            if (clear)
                hoverTooltip.tooltipLangauges = tooltips;
            else
                hoverTooltip.tooltipLangauges.AddRange(tooltips);
        }

        public static Tooltip NewTooltip(string desc, string hint, List<string> keys = null, Language lanuage = Language.English) => new Tooltip
        {
            desc = desc,
            hint = hint,
            keys = keys ?? new List<string>(),
            language = lanuage
        };

        public static HoverTooltip.Tooltip DeepCopy(HoverTooltip.Tooltip tooltip) => new HoverTooltip.Tooltip
        {
            desc = tooltip.desc,
            hint = tooltip.hint,
            keys = tooltip.keys.Clone(),
            language = tooltip.language
        };
    }

    public class Tooltip : HoverTooltip.Tooltip
    {
        public Tooltip()
        {

        }

        public Tooltip(HoverTooltip.Tooltip tooltip)
        {
            desc = tooltip.desc;
            hint = tooltip.hint;
            keys = tooltip.keys.Clone();
            language = (Language)tooltip.language;
        }

        public new Language language;

        public static Tooltip DeepCopy(Tooltip tooltip) => new Tooltip
        {
            desc = tooltip.desc,
            hint = tooltip.hint,
            keys = tooltip.keys.Clone(),
            language = tooltip.language
        };
    }
}
