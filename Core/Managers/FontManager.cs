using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Core.Optimization.Objects;
using BetterLegacy.Core.Optimization.Objects.Visual;
using LSFunctions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
            if (!CoreHelper.Playing && !CoreHelper.Reversing && !GameData.IsValid)
                return;

            if (!AudioManager.inst.CurrentAudioSource.clip || !CoreConfig.Instance.AllowCustomTextFormatting.Value)
                return;

            var beatmapObjects = GameData.Current.beatmapObjects;

            var currentAudioTime = AudioManager.inst.CurrentAudioSource.time;
            var currentAudioLength = AudioManager.inst.CurrentAudioSource.clip.length;

            for (int i = 0; i < beatmapObjects.Count; i++)
            {
                var beatmapObject = beatmapObjects[i];

                try
                {
                    if (beatmapObject.shape == 4 && beatmapObject.Alive && beatmapObject.objectType != BeatmapObject.ObjectType.Empty &&
                        Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.visualObject is TextObject textObject)
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
            {
                fonts[i].font = defaultFont;
            }
        }

        public IEnumerator SetupCustomFonts()
        {
            var refer = MaterialReferenceManager.instance;
            var dictionary = (Dictionary<int, TMP_FontAsset>)refer.GetType().GetField("m_FontAssetReferenceLookup", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(refer);

            if (!RTFile.FileExists(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/customfonts.asset"))
            {
                Debug.LogError($"{className}customfonts.asset does not exist in the BepInEx/plugins/Assets folder.");
                yield break;
            }

            var assetBundle = GetAssetBundle(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets", "customfonts.asset");
            foreach (var asset in assetBundle.GetAllAssetNames())
            {
                string fontName = asset.Replace("assets/fonts/", "").Replace("assets/font/", "");
                var font = assetBundle.LoadAsset<Font>(fontName);

                if (font == null)
                {
                    Debug.LogError($"{className}The font ({fontName}) does not exist in the asset bundle for some reason.");
                    continue;
                }

                var fontCopy = Instantiate(font);
                fontCopy.name = ChangeName(fontName);

                if (allFonts.ContainsKey(fontCopy.name))
                {
                    Debug.LogError($"{className}The font ({fontName}) was already in the font dictionary.");
                    continue;
                }

                allFonts.Add(fontCopy.name, fontCopy);
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

                if (allFontAssets.ContainsKey(font.Key))
                {
                    Debug.LogError($"{className}The font asset ({font.Key}) was already in the font asset dictionary.");
                    continue;
                }

                allFontAssets.Add(font.Key, e);
            }

            if (CoreConfig.Instance.DebugInfoStartup.Value)
                DebugInfo.Init();

            loadedFiles = true;

            yield break;
        }

        public AssetBundle GetAssetBundle(string _filepath, string _bundle) => AssetBundle.LoadFromFile(Path.Combine(_filepath, _bundle));

        // UNUSED DUE TO FILESIZE
        // - NotoSansSC-VariableFont.ttf
        // - HiMelody-Regular.ttf
        // - NotoSansTC-VariableFont.ttf
        // - NotoSansHK-VariableFont.ttf
        // - NotoSansKR-VariableFont_wght.ttf
        // - HanyiSentyPagoda Regular.ttf
        string ChangeName(string _name1)
        {
            switch (_name1)
            {
                #region Symbol
                case "giedi ancient autobot.otf": return "Ancient Autobot";
                case "transformersmovie-y9ad.ttf": return "Transformers Movie";
                case "webdings.ttf": return "Webdings"; // unused
                case "wingding.ttf": return "Wingdings"; // unused
                case "undertale-wingdings.ttf": return "Determination Wingdings";
                #endregion
                #region English Fonts
                case "about_friend_extended_v2_by_matthewtheprep_ddribq5.otf": return "About Friend";
                case "adamwarrenpro-bold.ttf": return "Adam Warren Pro Bold";
                case "adamwarrenpro-bolditalic.ttf": return "Adam Warren Pro BoldItalic";
                case "adamwarrenpro.ttf": return "Adam Warren Pro";
                case "angsaz.ttf": return "Angsana Z";
                case "arrhythmia-font.ttf": return "Arrhythmia";
                case "arial.ttf": return "Arial"; // unused
                case "arialbd.ttf": return "Arial Bold"; // unused
                case "arialbi.ttf": return "Arial Bold Italic"; // unused
                case "ariali.ttf": return "Arial Italic"; // unused
                case "ariblk.ttf": return "Arial Black"; // unused
                case "badabb__.ttf": return "BadaBoom BB";
                case "calibri.ttf": return "Calibri"; // unused
                case "calibrii.ttf": return "Calibri Italic"; // unused
                case "calibril.ttf": return "Calibri Light"; // unused
                case "calibrili.ttf": return "Calibri Light Italic"; // unused
                case "calibriz.ttf": return "Calibri Bold Italic"; // unused
                case "cambria.ttc": return "Cambria"; // unused
                case "cambriab.ttf": return "Cambria Bold"; // unused
                case "cambriaz.ttf": return "Cambria Bold Italic"; // unused
                case "candara.ttf": return "Candara"; // unused
                case "candarab.ttf": return "Candara Bold"; // unused
                case "candarai.ttf": return "Candara Italic"; // unused
                case "candaral.ttf": return "Candara Light"; // unused
                case "candarali.ttf": return "Candara Light Italic"; // unused
                case "candaraz.ttf": return "Candara Bold Italic"; // unused
                case "construction.ttf": return "Construction"; // unused
                case "comic.ttf": return "Comic Sans";
                case "comicbd.ttf": return "Comic Sans Bold";
                case "comici.ttf": return "Comic Sans Italic";
                case "comicz.ttf": return "Comic Sans Bold Italic";
                case "ldfcomicsans-jj7l.ttf": return "Comic Sans";
                case "ldfcomicsansbold-zgma.ttf": return "Comic Sans Bold";
                case "ldfcomicsanshairline-5pml.ttf": return "Comic Sans Hairline";
                case "ldfcomicsanslight-6dzo.ttf": return "Comic Sans Light";
                case "dtm-mono.otf": return "Determination Mono";
                case "dtm-sans.otf": return "determination sans";
                case "filedeletion-yw6m5.ttf": return "File Deletion";
                case "flowcircular-regular.ttf": return "Flow Circular";
                case "fredokaone-regular.ttf": return "Fredoka One";
                case "hachicro.ttf": return "Hachicro";
                case "inconsolata-variablefont_wdth,wght.ttf": return "Inconsolata Variable";
                case "impact.ttf": return "Impact"; // unused
                case "komikah_.ttf": return "Komika Hand";
                case "komikahb.ttf": return "Komika Hand Bold";
                case "komikask.ttf": return "Komika Slick";
                case "komikasl.ttf": return "Komika Slim";
                case "komikhbi.ttf": return "Komika Hand BoldItalic";
                case "komikhi_.ttf": return "Komika Hand Italic";
                case "komikj__.ttf": return "Komika Jam";
                case "komikji_.ttf": return "Komika Jam Italic";
                case "komikski.ttf": return "Komika Slick Italic";
                case "komiksli.ttf": return "Komika Slim Italic";
                case "bionicle language.ttf": return "Matoran Language 1";
                case "mata nui.ttf": return "Matoran Language 2";
                case "minecraftbold-nmk1.otf": return "Minecraft Text Bold";
                case "minecraftbolditalic-1y1e.otf": return "Minecraft Text BoldItalic";
                case "minecraftitalic-r8mo.otf": return "Minecraft Text Italic";
                case "minecraftregular-bmg3.otf": return "Minecraft Text";
                case "minercraftory.ttf": return "Minecraftory";
                case "micross.ttf": return "Sans Serif"; // unused
                case "monsterfriendback.otf": return "Monster Friend Back";
                case "monsterfriendfore.otf": return "Monster Friend Fore";
                case "necosmic-personalrse.otf": return "Necosmic"; // unused
                case "oxygene1.ttf": return "Oxygene";
                case "piraka theory gf.ttf": return "Piraka Theory";
                case "persons unknown.otf": return "Persons Unknown"; // unused
                case "plastiquekingdom.ttf": return "PlastiqueKingdom"; // unused
                case "piraka.ttf": return "Piraka";
                case "pusab___.otf": return "Pusab";
                case "rahkshi font.ttf": return "Rahkshi";
                case "revuebt-regular 1.otf": return "Revue 1";
                case "revuebt-regular.otf": return "Revue";
                case "times.ttf": return "Times New Roman"; // unused
                case "timesbd.ttf": return "Times New Roman Bold"; // unused
                case "timesbi.ttf": return "Times New Roman Bold Italic"; // unused
                case "timesi.ttf": return "Times New Roman Italic"; // unused
                case "transdings-waoo.ttf": return "Transdings";
                case "nexa bold.otf": return "Nexa Bold";
                case "nexabook.otf": return "Nexa Book";
                case "sans mita aprilia.ttf": return "Sans Sans";
                case "spookyhollow.ttf": return "SpookyHollow"; // unused
                #endregion
                #region Thai Fonts
                case "angsa.ttf": return "Angsana";
                case "angsab.ttf": return "Angsana Bold";
                case "angsai.ttf": return "Angsana Italic";
                case "angsananewbolditalic.ttf": return "Angsana Bold Italic";
                case "krr manga s.otf": return "Manga"; // unused
                case "pixellet.ttf": return "Pixellet";
                case "ploypilinfont.ttf": return "Ploypilin"; // unused
                #endregion
                #region Russian Fonts
                case "18vag rounded m bold.ttf": return "VAG Rounded";
                case "milk(rus by lyajka) regular.ttf": return "LemonMilkRus"; // unused
                case "1 nevrouz m regular.ttf": return "Nevrouz"; // unused
                case "robotomono-bold.ttf": return "Roboto Mono Bold";
                case "robotomono-italic.ttf": return "Roboto Mono Italic";
                case "robotomono-light.ttf": return "Roboto Mono Light";
                case "robotomono-light_0.ttf": return "Roboto Mono Light 1";
                case "robotomono-lightitalic.ttf": return "Roboto Mono Light Italic";
                case "robotomono-lightitalic_0.ttf": return "Roboto Mono Light Italic 1";
                case "robotomono-thin.ttf": return "Roboto Mono Thin";
                case "robotomono-thin_0.ttf": return "Roboto Mono Thin 1";
                case "robotomono-thinitalic.ttf": return "Roboto Mono Thin Italic";
                case "robotomono-thinitalic_0.ttf": return "Roboto Mono Thin Italic 1";
                #endregion
                #region Japanese Fonts
                case "dotgothic16-regular.ttf": return "DotGothic16"; // unused
                case "jkg-l_3.ttf": return "Jkg"; // unused
                case "monomaniacone-regular.ttf": return "Monomaniac One";
                case "rocknrollone-regular.ttf": return "RocknRoll One";
                case "ronde-b_square.otf": return "Ronde-B"; // unused
                case "yokomoji.otf": return "Yokomoji"; // unused
                #endregion
                #region Korean Fonts
                case "himelody-regular.ttf": return "HiMelody"; // unused
                case "jua-regular.ttf": return "Jua"; // unused
                case "kiranghaerang-regular.ttf": return "KirangHaerang"; // unused
                case "notosanskr-variablefont_wght.ttf": return "NotoSansKR"; // unused
                #endregion
                #region Chinese Fonts
                case "azppt regular.ttf": return "AZPPT"; // unused
                case "hanyisentypagoda regular.ttf": return "HanyiSentyPagoda"; // unused
                case "notosanstc-variablefont.ttf": return "NotoSansTC"; // unused
                case "notosanshk-variablefont.ttf": return "NotoSansHK"; // unused
                case "notosanssc-variablefont.ttf": return "NotoSansSC"; // unused
                case "yrdzst medium.ttf": return "YRDZST"; // unused
                #endregion
                #region Tagalog Fonts
                case "notosanstagalog-regular.ttf": return "NotoSansTagalog"; // unused
                case "tagdoc93.ttf": return "TagDoc93"; // unused
                    #endregion
            }
            return _name1;
        }
    }
}
