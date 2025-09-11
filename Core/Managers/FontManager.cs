using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

using BetterLegacy.Configs;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime.Objects.Visual;

namespace BetterLegacy.Core.Managers
{
    /// <summary>
    /// This class is used to store fonts from the customfonts.asset file.
    /// </summary>
    public class FontManager : MonoBehaviour
    {
        public static FontManager inst;
        public static string className = "[<color=#A100FF>FontManager</color>] \n";

        public Dictionary<string, Font> allFonts = new Dictionary<string, Font>();
        public Dictionary<string, TMP_FontAsset> allFontAssets = new Dictionary<string, TMP_FontAsset>();
        public bool loadedFiles = false;

        public string defaultFont = "Inconsolata Variable";

        public Font DefaultFont
        {
            get
            {
                var key = EditorConfig.Instance.EditorFont.Value.ToString().Replace("_", " ");
                if (allFonts.TryGetValue(key, out Font font))
                    return font;

                if (allFonts.TryGetValue(defaultFont, out Font defaultFontValue))
                    return defaultFontValue;

                Debug.Log($"{className}Font doesn't exist.");
                return Font.GetDefault();
            }
        }

        /// <summary>
        /// Inits FontManager.
        /// </summary>
        public static void Init() => Creator.NewGameObject(nameof(FontManager), SystemManager.inst.transform).AddComponent<FontManager>();

        void Awake()
        {
            inst = this;
            StartCoroutine(SetupCustomFonts());
        }

        void Update()
        {
            if (!CoreHelper.Playing && !CoreHelper.Reversing && !GameData.Current)
                return;

            if (!AudioManager.inst.CurrentAudioSource.clip || !CoreConfig.Instance.AllowCustomTextFormatting.Value)
                return;

            var beatmapObjects = GameData.Current.beatmapObjects;

            for (int i = 0; i < beatmapObjects.Count; i++)
            {
                var beatmapObject = beatmapObjects[i];

                try
                {
                    if (beatmapObject.ShapeType == ShapeType.Text && beatmapObject.Alive && beatmapObject.objectType != BeatmapObject.ObjectType.Empty &&
                        beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject is TextObject textObject)
                        textObject.SetText(RTString.FormatText(beatmapObject, textObject.text));
                }
                catch
                {

                }
            }
        }

        public void ChangeAllFontsInEditor()
        {
            if (!EditorManager.inst)
                return;

            var fonts = Resources.FindObjectsOfTypeAll<Text>().Where(x => x.font.name == "Inconsolata-Regular").ToArray();

            var defaultFont = DefaultFont;

            for (int i = 0; i < fonts.Length; i++)
                fonts[i].font = defaultFont;
        }

        public IEnumerator SetupCustomFonts()
        {
            var refer = MaterialReferenceManager.instance;
            var dictionary = (Dictionary<int, TMP_FontAsset>)refer.GetType().GetField("m_FontAssetReferenceLookup", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(refer);

            var path = RTFile.GetAsset($"customfonts{FileFormat.ASSET.Dot()}");
            if (!RTFile.FileExists(path))
            {
                Debug.LogError($"{className}customfonts{FileFormat.ASSET.Dot()} does not exist in the BepInEx/plugins/Assets folder.");
                yield break;
            }

            var assetBundle = AssetBundle.LoadFromFile(path);
            foreach (var asset in assetBundle.GetAllAssetNames())
            {
                string fontName = asset.Remove("assets/fonts/").Remove("assets/font/");
                var font = assetBundle.LoadAsset<Font>(fontName);

                if (font == null)
                {
                    Debug.LogError($"{className}The font ({fontName}) does not exist in the asset bundle for some reason.");
                    continue;
                }

                var fontCopy = Instantiate(font);
                fontCopy.name = ChangeName(fontName);

                allFonts[fontCopy.name] = fontCopy;
            }
            assetBundle.Unload(false);

            foreach (var font in allFonts)
            {
                var e = TMP_FontAsset.CreateFontAsset(font.Value);
                e.name = font.Key;

                var random1 = TMP_TextUtilities.GetSimpleHashCode(e.name);
                e.hashCode = random1;
                e.materialHashCode = random1;

                if (dictionary.ContainsKey(e.hashCode))
                {
                    Debug.LogError($"{className}Could not convert the font to TextMeshPro Font Asset. Hashcode: {e.hashCode}");
                    continue;
                }

                MaterialReferenceManager.AddFontAsset(e);

                allFontAssets[font.Key] = e;
            }

            if (CoreConfig.Instance.DebugInfoStartup.Value)
                DebugInfo.Init();

            loadedFiles = true;

            yield break;
        }

        string ChangeName(string name) => name switch
        {
            #region Symbol

            "giedi ancient autobot.otf" => "Ancient Autobot",
            "transformersmovie-y9ad.ttf" => "Transformers Movie",
            "webdings.ttf" => "Webdings", // unused
            "wingding.ttf" => "Wingdings", // unused
            "undertale-wingdings.ttf" => "Determination Wingdings",

            #endregion

            #region English Fonts

            "about_friend_extended_v2_by_matthewtheprep_ddribq5.otf" => "About Friend",
            "adamwarrenpro-bold.ttf" => "Adam Warren Pro Bold",
            "adamwarrenpro-bolditalic.ttf" => "Adam Warren Pro BoldItalic",
            "adamwarrenpro.ttf" => "Adam Warren Pro",
            "angsaz.ttf" => "Angsana Z",
            "arrhythmia-font.ttf" => "Arrhythmia",
            "arial.ttf" => "Arial", // unused
            "arialbd.ttf" => "Arial Bold", // unused
            "arialbi.ttf" => "Arial Bold Italic", // unused
            "ariali.ttf" => "Arial Italic", // unused
            "ariblk.ttf" => "Arial Black", // unused
            "badabb__.ttf" => "BadaBoom BB",
            "calibri.ttf" => "Calibri", // unused
            "calibrii.ttf" => "Calibri Italic", // unused
            "calibril.ttf" => "Calibri Light", // unused
            "calibrili.ttf" => "Calibri Light Italic", // unused
            "calibriz.ttf" => "Calibri Bold Italic", // unused
            "cambria.ttc" => "Cambria", // unused
            "cambriab.ttf" => "Cambria Bold", // unused
            "cambriaz.ttf" => "Cambria Bold Italic", // unused
            "candara.ttf" => "Candara", // unused
            "candarab.ttf" => "Candara Bold", // unused
            "candarai.ttf" => "Candara Italic", // unused
            "candaral.ttf" => "Candara Light", // unused
            "candarali.ttf" => "Candara Light Italic", // unused
            "candaraz.ttf" => "Candara Bold Italic", // unused
            "construction.ttf" => "Construction",
            "comic.ttf" => "Comic Sans",
            "comicbd.ttf" => "Comic Sans Bold",
            "comici.ttf" => "Comic Sans Italic",
            "comicz.ttf" => "Comic Sans Bold Italic",
            "ldfcomicsans-jj7l.ttf" => "Comic Sans",
            "ldfcomicsansbold-zgma.ttf" => "Comic Sans Bold",
            "ldfcomicsanshairline-5pml.ttf" => "Comic Sans Hairline",
            "ldfcomicsanslight-6dzo.ttf" => "Comic Sans Light",
            "dtm-mono.otf" => "Determination Mono",
            "dtm-sans.otf" => "determination sans",
            "filedeletion-yw6m5.ttf" => "File Deletion",
            "flowcircular-regular.ttf" => "Flow Circular",
            "fredokaone-regular.ttf" => "Fredoka One",
            "hachicro.ttf" => "Hachicro",
            "inconsolata-variablefont_wdth,wght.ttf" => "Inconsolata Variable",
            "impact.ttf" => "Impact", // unused
            "komikah_.ttf" => "Komika Hand",
            "komikahb.ttf" => "Komika Hand Bold",
            "komikask.ttf" => "Komika Slick",
            "komikasl.ttf" => "Komika Slim",
            "komikhbi.ttf" => "Komika Hand BoldItalic",
            "komikhi_.ttf" => "Komika Hand Italic",
            "komikj__.ttf" => "Komika Jam",
            "komikji_.ttf" => "Komika Jam Italic",
            "komikski.ttf" => "Komika Slick Italic",
            "komiksli.ttf" => "Komika Slim Italic",
            "bionicle language.ttf" => "Matoran Language 1",
            "mata nui.ttf" => "Matoran Language 2",
            "minecraftbold-nmk1.otf" => "Minecraft Text Bold",
            "minecraftbolditalic-1y1e.otf" => "Minecraft Text BoldItalic",
            "minecraftitalic-r8mo.otf" => "Minecraft Text Italic",
            "minecraftregular-bmg3.otf" => "Minecraft Text",
            "minercraftory.ttf" => "Minecraftory",
            "micross.ttf" => "Sans Serif", // unused
            "monsterfriendback.otf" => "Monster Friend Back",
            "monsterfriendfore.otf" => "Monster Friend Fore",
            "oxygene1.ttf" => "Oxygene",
            "piraka theory gf.ttf" => "Piraka Theory",
            "persons unknown.otf" => "Persons Unknown",
            "plastiquekingdom.ttf" => "PlastiqueKingdom",
            "piraka.ttf" => "Piraka",
            "pusab___.otf" => "Pusab",
            "rahkshi font.ttf" => "Rahkshi",
            "revuebt-regular 1.otf" => "Revue 1",
            "revuebt-regular.otf" => "Revue",
            "times.ttf" => "Times New Roman", // unused
            "timesbd.ttf" => "Times New Roman Bold", // unused
            "timesbi.ttf" => "Times New Roman Bold Italic", // unused
            "timesi.ttf" => "Times New Roman Italic", // unused
            "transdings-waoo.ttf" => "Transdings",
            "nexa bold.otf" => "Nexa Bold",
            "nexabook.otf" => "Nexa Book",
            "sans mita aprilia.ttf" => "Sans Sans",
            "spookyhollow.ttf" => "SpookyHollow",
            "arcadeclassic.ttf" => "Arcade Classic",
            "skytree.ttf" => "Skytree",
            "skytree_mono.ttf" => "Skytree Mono",

            // vg ported
            "majormonodisplay-regular.ttf" => "MajorMonoDisplay",
            "poorstory-regular.ttf" => "Poorstory",
            "hellovetica regular.ttf" => "Hellovetica",

            #endregion

            #region Thai Fonts

            "angsa.ttf" => "Angsana",
            "angsab.ttf" => "Angsana Bold",
            "angsai.ttf" => "Angsana Italic",
            "angsananewbolditalic.ttf" => "Angsana Bold Italic",
            "krr manga s.otf" => "Manga",
            "pixellet.ttf" => "Pixellet",
            "ploypilinfont.ttf" => "Ploypilin",

            #endregion

            #region Russian Fonts

            "18vag rounded m bold.ttf" => "VAG Rounded",
            "milk(rus by lyajka) regular.ttf" => "LemonMilkRus",
            "1 nevrouz m regular.ttf" => "Nevrouz",
            "robotomono-bold.ttf" => "Roboto Mono Bold",
            "robotomono-italic.ttf" => "Roboto Mono Italic",
            "robotomono-light.ttf" => "Roboto Mono Light",
            "robotomono-light_0.ttf" => "Roboto Mono Light 1",
            "robotomono-lightitalic.ttf" => "Roboto Mono Light Italic",
            "robotomono-lightitalic_0.ttf" => "Roboto Mono Light Italic 1",
            "robotomono-thin.ttf" => "Roboto Mono Thin",
            "robotomono-thin_0.ttf" => "Roboto Mono Thin 1",
            "robotomono-thinitalic.ttf" => "Roboto Mono Thin Italic",
            "robotomono-thinitalic_0.ttf" => "Roboto Mono Thin Italic 1",

            #endregion

            #region Japanese Fonts

            "dotgothic16-regular.ttf" => "DotGothic16",
            "jkg-l_3.ttf" => "Jkg",
            "monomaniacone-regular.ttf" => "Monomaniac One",
            "rocknrollone-regular.ttf" => "RocknRoll One",
            "ronde-b_square.otf" => "Ronde-B",
            "yokomoji.otf" => "Yokomoji",

            #endregion

            #region Korean Fonts

            "himelody-regular.ttf" => "HiMelody",
            "jua-regular.ttf" => "Jua",
            "kiranghaerang-regular.ttf" => "KirangHaerang",
            "notosanskr-variablefont_wght.ttf" => "NotoSansKR",

            #endregion

            #region Chinese Fonts

            "azppt regular.ttf" => "AZPPT",
            "hanyisentypagoda regular.ttf" => "HanyiSentyPagoda",
            "notosanstc-variablefont.ttf" => "NotoSansTC",
            "notosanshk-variablefont.ttf" => "NotoSansHK",
            "notosanssc-variablefont.ttf" => "NotoSansSC",
            "yrdzst medium.ttf" => "YRDZST",

            #endregion

            #region Tagalog Fonts

            "notosanstagalog-regular.ttf" => "NotoSansTagalog", // unused
            "tagdoc93.ttf" => "TagDoc93", // unused

            #endregion

            _ => name,
        };
    }
}
