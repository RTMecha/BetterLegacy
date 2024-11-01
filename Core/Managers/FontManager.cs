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

            var beatmapObjects = GameData.Current.beatmapObjects.FindAll(x => x.shape == 4 && x.Alive && x.objectType != BeatmapObject.ObjectType.Empty);

            var currentAudioTime = AudioManager.inst.CurrentAudioSource.time;
            var currentAudioLength = AudioManager.inst.CurrentAudioSource.clip.length;

            for (int i = 0; i < beatmapObjects.Count; i++)
            {
                var beatmapObject = beatmapObjects[i];

                try
                {
                    if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.visualObject is TextObject textObject)
                        textObject.SetText(TextTranslater.FormatText(beatmapObject, textObject.text));
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
                string str = asset.Replace("assets/font/", "");
                var font = assetBundle.LoadAsset<Font>(str);

                if (font == null)
                {
                    Debug.LogError($"{className}The font ({str}) does not exist in the asset bundle for some reason.");
                    continue;
                }

                var fontCopy = Instantiate(font);
                fontCopy.name = ChangeName(str);

                if (allFonts.ContainsKey(fontCopy.name))
                {
                    Debug.LogError($"{className}The font ({str}) was already in the font dictionary.");
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

        public static class TextTranslater
        {
            public static string FormatText(BeatmapObject beatmapObject, string str)
            {
                var currentAudioTime = AudioManager.inst.CurrentAudioSource.time;
                var currentAudioLength = AudioManager.inst.CurrentAudioSource.clip.length;

                if (str.Contains("math"))
                {
                    CoreHelper.RegexMatch(str, new Regex(@"<math=""(.*?)>"""), match =>
                    {
                        try
                        {
                            var replace = RTMath.Replace(match.Groups[1].ToString());

                            var math = RTMath.Evaluate(replace);

                            str = str.Replace(match.Groups[0].ToString(), math.ToString());
                        }
                        catch
                        {
                        }
                    });

                    CoreHelper.RegexMatch(str, new Regex(@"<math=(.*?)>"), match =>
                    {
                        try
                        {
                            var replace = RTMath.Replace(match.Groups[1].ToString());

                            var math = RTMath.Evaluate(replace);

                            str = str.Replace(match.Groups[0].ToString(), math.ToString());
                        }
                        catch
                        {
                        }
                    });
                }

                #region Audio

                #region Time Span

                if (str.Contains("msAudioSpan"))
                    CoreHelper.RegexMatches(str, new Regex(@"<msAudioSpan=([0-9.:]+)>"), match =>
                    {
                        str = str.Replace(match.Groups[0].ToString(), PreciseToMilliSeconds(currentAudioTime, "{" + match.Groups[1].ToString() + "}"));
                    });

                if (str.Contains("sAudioSpan"))
                    CoreHelper.RegexMatches(str, new Regex(@"<sAudioSpan=([0-9.:]+)>"), match =>
                    {
                        str = str.Replace(match.Groups[0].ToString(), PreciseToSeconds(currentAudioTime, "{" + match.Groups[1].ToString() + "}"));
                    });

                if (str.Contains("mAudioSpan"))
                    CoreHelper.RegexMatches(str, new Regex(@"<mAudioSpan=([0-9.:]+)>"), match =>
                    {
                        str = str.Replace(match.Groups[0].ToString(), PreciseToMinutes(currentAudioTime, "{" + match.Groups[1].ToString() + "}"));
                    });

                if (str.Contains("hAudioSpan"))
                    CoreHelper.RegexMatches(str, new Regex(@"<hAudioSpan=([0-9.:]+)>"), match =>
                    {
                        str = str.Replace(match.Groups[0].ToString(), PreciseToHours(currentAudioTime, "{" + match.Groups[1].ToString() + "}"));
                    });

                #endregion

                #region No Time Span

                if (str.Contains("msAudio"))
                    CoreHelper.RegexMatches(str, new Regex(@"<msAudio=([0-9.:]+)>"), match =>
                    {
                        str = str.Replace(match.Groups[0].ToString(), string.Format("{" + match.Groups[1].ToString() + "}", (int)((currentAudioTime) * 1000)));
                    });

                if (str.Contains("sAudio"))
                    CoreHelper.RegexMatches(str, new Regex(@"<sAudio=([0-9.:]+)>"), match =>
                    {
                        str = str.Replace(match.Groups[0].ToString(), string.Format("{" + match.Groups[1].ToString() + "}", currentAudioTime));
                    });

                if (str.Contains("mAudio"))
                    CoreHelper.RegexMatches(str, new Regex(@"<mAudio=([0-9.:]+)>"), match =>
                    {
                        str = str.Replace(match.Groups[0].ToString(), string.Format("{" + match.Groups[1].ToString() + "}", (int)((currentAudioTime) / 60)));
                    });

                if (str.Contains("hAudio"))
                    CoreHelper.RegexMatches(str, new Regex(@"<hAudio=([0-9.:]+)>"), match =>
                    {
                        str = str.Replace(match.Groups[0].ToString(), string.Format("{" + match.Groups[1].ToString() + "}", (currentAudioTime) / 600));
                    });

                #endregion

                #endregion

                #region Audio Left

                #region Time Span

                if (str.Contains("msAudioLeftSpan"))
                    CoreHelper.RegexMatches(str, new Regex(@"<msAudioLeftSpan=([0-9.:]+)>"), match =>
                    {
                        str = str.Replace(match.Groups[0].ToString(), PreciseToMilliSeconds(currentAudioLength - currentAudioTime, "{" + match.Groups[1].ToString() + "}"));
                    });

                if (str.Contains("sAudioLeftSpan"))
                    CoreHelper.RegexMatches(str, new Regex(@"<sAudioLeftSpan=([0-9.:]+)>"), match =>
                    {
                        str = str.Replace(match.Groups[0].ToString(), PreciseToSeconds(currentAudioLength - currentAudioTime, "{" + match.Groups[1].ToString() + "}"));
                    });

                if (str.Contains("mAudioLeftSpan"))
                    CoreHelper.RegexMatches(str, new Regex(@"<mAudioLeftSpan=([0-9.:]+)>"), match =>
                    {
                        str = str.Replace(match.Groups[0].ToString(), PreciseToMinutes(currentAudioLength - currentAudioTime, "{" + match.Groups[1].ToString() + "}"));
                    });

                if (str.Contains("hAudioLeftSpan"))
                    CoreHelper.RegexMatches(str, new Regex(@"<hAudioLeftSpan=([0-9.:]+)>"), match =>
                    {
                        str = str.Replace(match.Groups[0].ToString(), PreciseToHours(currentAudioLength - currentAudioTime, "{" + match.Groups[1].ToString() + "}"));
                    });

                #endregion

                #region No Time Span

                if (str.Contains("msAudioLeft"))
                    CoreHelper.RegexMatches(str, new Regex(@"<msAudioLeft=([0-9.:]+)>"), match =>
                    {
                        str = str.Replace(match.Groups[0].ToString(), string.Format("{" + match.Groups[1].ToString() + "}", (int)((currentAudioLength - currentAudioTime) * 1000)));
                    });

                if (str.Contains("sAudioLeft"))
                    CoreHelper.RegexMatches(str, new Regex(@"<sAudioLeft=([0-9.:]+)>"), match =>
                    {
                        str = str.Replace(match.Groups[0].ToString(), string.Format("{" + match.Groups[1].ToString() + "}", currentAudioLength - currentAudioTime));
                    });

                if (str.Contains("mAudioLeft"))
                    CoreHelper.RegexMatches(str, new Regex(@"<mAudioLeft=([0-9.:]+)>"), match =>
                    {
                        str = str.Replace(match.Groups[0].ToString(), string.Format("{" + match.Groups[1].ToString() + "}", (int)((currentAudioLength - currentAudioTime) / 60)));
                    });

                if (str.Contains("hAudioLeft"))
                    CoreHelper.RegexMatches(str, new Regex(@"<hAudioLeft=([0-9.:]+)>"), match =>
                    {
                        str = str.Replace(match.Groups[0].ToString(), string.Format("{" + match.Groups[1].ToString() + "}", (currentAudioLength - currentAudioTime) / 600));
                    });

                #endregion

                #endregion

                #region Real Time

                if (str.Contains("realTime"))
                    CoreHelper.RegexMatches(str, new Regex(@"<realTime=([a-z]+)>"), match =>
                    {
                        try
                        {
                            str = str.Replace(match.Groups[0].ToString(), DateTime.Now.ToString(match.Groups[1].ToString()));
                        }
                        catch
                        {
                        }
                    });

                #endregion

                #region Players

                if (str.Contains("playerHealth"))
                    CoreHelper.RegexMatches(str, new Regex(@"<playerHealth=([0-9]+)>"), match =>
                    {
                        if (int.TryParse(match.Groups[1].ToString(), out int index) && index < InputDataManager.inst.players.Count)
                            str = str.Replace(match.Groups[0].ToString(), InputDataManager.inst.players[index].health.ToString());
                        else
                            str = str.Replace(match.Groups[0].ToString(), "");
                    });
                
                if (str.Contains("playerHealthBar"))
                    CoreHelper.RegexMatches(str, new Regex(@"<playerHealthBar=([0-9]+)>"), match =>
                    {
                        if (int.TryParse(match.Groups[1].ToString(), out int index) && index < InputDataManager.inst.players.Count)
                        {
                            var player = PlayerManager.Players[index];
                            str = str.Replace(match.Groups[0].ToString(), ConvertHealthToEquals(player.Health, player.PlayerModel.basePart.health));
                        }
                        else
                            str = str.Replace(match.Groups[0].ToString(), "");
                    });

                if (str.Contains("<playerHealthTotal>"))
                {
                    var ph = 0;

                    for (int j = 0; j < InputDataManager.inst.players.Count; j++)
                        ph += InputDataManager.inst.players[j].health;

                    str = str.Replace("<playerHealthTotal>", ph.ToString());
                }

                if (str.Contains("<deathCount>"))
                    str = str.Replace("<deathCount>", GameManager.inst.deaths.Count.ToString());

                if (str.Contains("<hitCount>"))
                {
                    var pd = GameManager.inst.hits.Count;

                    str = str.Replace("<hitCount>", pd.ToString());
                }

                if (str.Contains("<boostCount>"))
                    str = str.Replace("<boostCount>", LevelManager.BoostCount.ToString());

                #endregion

                #region QuickElement

                if (str.Contains("quickElement"))
                    CoreHelper.RegexMatches(str, new Regex(@"<quickElement=(.*?)>"), match =>
                    {
                        str = str.Replace(match.Groups[0].ToString(), QuickElementManager.ConvertQuickElement(beatmapObject, match.Groups[1].ToString()));
                    });

                #endregion

                #region Random

                if (str.Contains("randomText"))
                    CoreHelper.RegexMatches(str, new Regex(@"<randomText=([0-9]+)>"), match =>
                    {
                        if (int.TryParse(match.Groups[1].ToString(), out int length))
                            str = str.Replace(match.Groups[0].ToString(), LSText.randomString(length));
                    });

                if (str.Contains("randomNumber"))
                    CoreHelper.RegexMatches(str, new Regex(@"<randomNumber=([0-9]+)>"), match =>
                    {
                        if (int.TryParse(match.Groups[1].ToString(), out int length))
                            str = str.Replace(match.Groups[0].ToString(), LSText.randomNumString(length));
                    });

                #endregion

                #region Theme

                var beatmapTheme = CoreHelper.CurrentBeatmapTheme;

                if (str.Contains("themeObject"))
                    CoreHelper.RegexMatches(str, new Regex(@"<themeObject=([0-9]+)>"), match =>
                    {
                        if (match.Groups.Count > 1)
                            str = str.Replace(match.Groups[0].Value, $"<#{LSColors.ColorToHex(beatmapTheme.GetObjColor(int.Parse(match.Groups[1].ToString())))}>");
                    });

                if (str.Contains("themeBGs"))
                    CoreHelper.RegexMatches(str, new Regex(@"<themeBGs=([0-9]+)>"), match =>
                    {
                        if (match.Groups.Count > 1)
                            str = str.Replace(match.Groups[0].Value, $"<#{LSColors.ColorToHex(beatmapTheme.GetBGColor(int.Parse(match.Groups[1].ToString())))}>");
                    });

                if (str.Contains("themeFX"))
                    CoreHelper.RegexMatches(str, new Regex(@"<themeFX=([0-9]+)>"), match =>
                    {
                        if (match.Groups.Count > 1)
                            str = str.Replace(match.Groups[0].Value, $"<#{LSColors.ColorToHex(beatmapTheme.GetFXColor(int.Parse(match.Groups[1].ToString())))}>");
                    });

                if (str.Contains("themePlayers"))
                    CoreHelper.RegexMatches(str, new Regex(@"<themePlayers=([0-9]+)>"), match =>
                    {
                        if (match.Groups.Count > 1)
                            str = str.Replace(match.Groups[0].Value, $"<#{LSColors.ColorToHex(beatmapTheme.GetPlayerColor(int.Parse(match.Groups[1].ToString())))}>");
                    });

                if (str.Contains("<themeBG>"))
                    str = str.Replace("<themeBG>", LSColors.ColorToHex(beatmapTheme.backgroundColor));

                if (str.Contains("<themeGUI>"))
                    str = str.Replace("<themeGUI>", LSColors.ColorToHex(beatmapTheme.guiColor));

                if (str.Contains("<themeTail>"))
                    str = str.Replace("<themeTail>", LSColors.ColorToHex(beatmapTheme.guiAccentColor));

                #endregion

                #region LevelRank

                if (str.Contains("<levelRank>"))
                {
                    var levelRank =
                        !CoreHelper.InEditor && LevelManager.CurrentLevel != null ?
                            LevelManager.GetLevelRank(LevelManager.CurrentLevel) : CoreHelper.InEditor ? LevelManager.EditorRank :
                            DataManager.inst.levelRanks[0];

                    str = str.Replace("<levelRank>", $"<color=#{LSColors.ColorToHex(levelRank.color)}><b>{levelRank.name}</b></color>");
                }

                if (str.Contains("<levelRankName>"))
                {
                    var levelRank =
                        !CoreHelper.InEditor && LevelManager.CurrentLevel != null ?
                            LevelManager.GetLevelRank(LevelManager.CurrentLevel) :
                            DataManager.inst.levelRanks[0];

                    str = str.Replace("<levelRankName>", levelRank.name);
                }

                if (str.Contains("<levelRankColor>"))
                {
                    var levelRank =
                        !CoreHelper.InEditor && LevelManager.CurrentLevel != null ?
                            LevelManager.GetLevelRank(LevelManager.CurrentLevel) : CoreHelper.InEditor ? LevelManager.EditorRank :
                            DataManager.inst.levelRanks[0];

                    str = str.Replace("<levelRankColor>", $"<color=#{LSColors.ColorToHex(levelRank.color)}>");
                }

                if (str.Contains("<levelRankCurrent>"))
                {
                    var levelRank = !CoreHelper.InEditor ? LevelManager.GetLevelRank(GameManager.inst.hits) : LevelManager.EditorRank;

                    str = str.Replace("<levelRankCurrent>", $"<color=#{LSColors.ColorToHex(levelRank.color)}><b>{levelRank.name}</b></color>");
                }

                if (str.Contains("<levelRankCurrentName>"))
                {
                    var levelRank = !CoreHelper.InEditor ? LevelManager.GetLevelRank(GameManager.inst.hits) : LevelManager.EditorRank;

                    str = str.Replace("<levelRankCurrentName>", levelRank.name);
                }

                if (str.Contains("<levelRankCurrentColor>"))
                {
                    var levelRank = !CoreHelper.InEditor ? LevelManager.GetLevelRank(GameManager.inst.hits) : LevelManager.EditorRank;

                    str = str.Replace("<levelRankCurrentColor>", $"<color=#{LSColors.ColorToHex(levelRank.color)}>");
                }

                // Level Rank Other
                {
                    if (str.Contains("levelRankOther"))
                        CoreHelper.RegexMatches(str, new Regex(@"<levelRankOther=([0-9]+)>"), match =>
                        {
                            DataManager.LevelRank levelRank;
                            if (LevelManager.Levels.TryFind(x => x.id == match.Groups[1].ToString(), out Level level))
                            {
                                levelRank = LevelManager.GetLevelRank(level);
                            }
                            else
                            {
                                levelRank = CoreHelper.InEditor ? LevelManager.EditorRank : DataManager.inst.levelRanks[0];
                            }

                            str = str.Replace(match.Groups[0].ToString(), $"<color=#{LSColors.ColorToHex(levelRank.color)}><b>{levelRank.name}</b></color>");
                        });

                    if (str.Contains("levelRankOtherName"))
                        CoreHelper.RegexMatches(str, new Regex(@"<levelRankOtherName=([0-9]+)>"), match =>
                        {
                            DataManager.LevelRank levelRank;
                            if (LevelManager.Levels.TryFind(x => x.id == match.Groups[1].ToString(), out Level level))
                            {
                                levelRank = LevelManager.GetLevelRank(level);
                            }
                            else
                            {
                                levelRank = CoreHelper.InEditor ? LevelManager.EditorRank : DataManager.inst.levelRanks[0];
                            }

                            str = str.Replace(match.Groups[0].ToString(), levelRank.name);
                        });

                    if (str.Contains("levelRankOtherColor"))
                        CoreHelper.RegexMatches(str, new Regex(@"<levelRankOtherColor=([0-9]+)>"), match =>
                        {
                            DataManager.LevelRank levelRank;
                            if (LevelManager.Levels.TryFind(x => x.id == match.Groups[1].ToString(), out Level level))
                            {
                                levelRank = LevelManager.GetLevelRank(level);
                            }
                            else
                            {
                                levelRank = CoreHelper.InEditor ? LevelManager.EditorRank : DataManager.inst.levelRanks[0];
                            }

                            str = str.Replace(match.Groups[0].ToString(), $"<color=#{LSColors.ColorToHex(levelRank.color)}>");
                        });
                }

                if (str.Contains("<accuracy>"))
                    str = str.Replace("<accuracy>", $"{LevelManager.CalculateAccuracy(GameManager.inst.hits.Count, AudioManager.inst.CurrentAudioSource.clip.length)}");

                #endregion

                #region Mod stuff

                if (str.Contains("modifierVariable"))
                {
                    CoreHelper.RegexMatches(str, new Regex(@"<modifierVariable=(.*?)>"), match =>
                    {
                        var beatmapObject = CoreHelper.FindObjectWithTag(match.Groups[1].ToString());
                        if (beatmapObject)
                            str = str.Replace(match.Groups[0].ToString(), beatmapObject.integerVariable.ToString());
                    });

                    CoreHelper.RegexMatches(str, new Regex(@"<modifierVariableID=(.*?)>"), match =>
                    {
                        var beatmapObject = GameData.Current.beatmapObjects.Find(x => x.id == match.Groups[1].ToString());
                        if (beatmapObject)
                            str = str.Replace(match.Groups[0].ToString(), beatmapObject.integerVariable.ToString());
                    });
                }

                if (str.Contains("<username>"))
                    str = str.Replace("<username>", CoreConfig.Instance.DisplayName.Value);
                
                if (str.Contains("<modVersion>"))
                    str = str.Replace("<modVersion>", LegacyPlugin.ModVersion.ToString());

                #endregion

                return str;
            }

            public static string ReplaceProperties(string str) => string.IsNullOrEmpty(str) ? str : str
                .Replace("{{GameVersion}}", ProjectArrhythmia.GameVersion.ToString())
                .Replace("{{ModVersion}}", LegacyPlugin.ModVersion.ToString())
                .Replace("{{AppDirectory}}", RTFile.ApplicationDirectory)
                .Replace("{{BepInExAssetsDirectory}}", RTFile.BepInExAssetsPath)
                .Replace("{{LevelPath}}", GameManager.inst ? GameManager.inst.basePath : RTFile.ApplicationDirectory)
                .Replace("{{SplashText}}", LegacyPlugin.SplashText);

            public static string PreciseToMilliSeconds(float seconds, string format = "{0:000}") => string.Format(format, TimeSpan.FromSeconds(seconds).Milliseconds);

            public static string PreciseToSeconds(float seconds, string format = "{0:00}") => string.Format(format, TimeSpan.FromSeconds(seconds).Seconds);

            public static string PreciseToMinutes(float seconds, string format = "{0:00}") => string.Format(format, TimeSpan.FromSeconds(seconds).Minutes);

            public static string PreciseToHours(float seconds, string format = "{0:00}") => string.Format(format, TimeSpan.FromSeconds(seconds).Hours);

            public static string SecondsToTime(float seconds)
            {
                var timeSpan = TimeSpan.FromSeconds(seconds);
                return seconds >= 86400f ? string.Format("{0:D0}:{1:D1}:{2:D2}:{3:D3}", timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds) : string.Format("{0:D0}:{1:D1}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
            }

            public static string Percentage(float t, float length) => string.Format("{0:000}", (int)RTMath.Percentage(t, length));

            public static string ConvertHealthToEquals(int health, int max = 3)
            {
                string result = "[";
                for (int i = 0; i < health; i++)
                    result += "=";

                int spaces = -health + max;
                if (spaces > 0)
                    for (int i = 0; i < spaces; i++)
                        result += " ";

                return result += "]";
            }

            public static string ConvertBar(string s, float progress, int count = 10)
            {
                var result = "";
                for (int i = 0; i < (int)(progress / count); i++)
                    result += s;
                while (result.Length < count)
                    result = " " + result;

                return result;
            }

            public static string ArrayToString(params object[] vs)
            {
                string s = "";
                if (vs.Length > 0)
                    for (int i = 0; i < vs.Length; i++)
                    {
                        s += vs[i].ToString();
                        if (i != vs.Length - 1)
                            s += ", ";
                    }
                return s;
            }

            public static string AlphabetBinaryEncrypt(string c)
            {
                var t = c;
                var str = "";

                foreach (var ch in t)
                {
                    var pl = ch.ToString();
                    pl = AlphabetBinaryEncryptChar(ch.ToString());
                    str += pl + " ";
                }
                return str;
            }

            public static string AlphabetBinaryEncryptChar(string c) => alphabetLowercase.Contains(c.ToLower()) ? binary[alphabetLowercase.IndexOf(c.ToLower())] : c;

            public static string AlphabetByteEncrypt(string c)
            {
                var t = c;
                var str = "";

                foreach (var ch in c.ToLower())
                {
                    var pl = ch.ToString();
                    pl = AlphabetByteEncryptChar(ch);
                    str += pl + " ";
                }
                return str;
            }

            public static string AlphabetByteEncryptChar(char c) => ((byte)c).ToString();

            public static string AlphabetKevinEncrypt(string c)
            {
                var t = c;
                var str = "";

                foreach (var ch in t)
                {
                    var pl = ch.ToString();
                    pl = AlphabetKevinEncryptChar(ch.ToString());
                    str += pl;
                }
                return str;
            }

            public static string AlphabetKevinEncryptChar(string c) => alphabetLowercase.Contains(c.ToLower()) ? kevin[alphabetLowercase.IndexOf(c.ToLower())] : c;

            public static string AlphabetA1Z26Encrypt(string c)
            {
                var t = c;
                var str = "";
                foreach (var ch in t)
                {
                    var pl = ch.ToString();
                    pl = AlphabetA1Z26EncryptChar(ch.ToString());
                    str += pl + " ";
                }
                return str;
            }

            public static string AlphabetA1Z26EncryptChar(string c) => alphabetLowercase.Contains(c.ToLower()) ? (alphabetLowercase.IndexOf(c.ToLower()) + 1).ToString() : c;

            public static string AlphabetCaesarEncrypt(string c, int offset = 3)
            {
                var t = c;
                var str = "";
                foreach (var ch in t)
                {
                    var pl = ch.ToString();
                    pl = AlphabetCaesarEncryptChar(ch.ToString(), offset);
                    str += pl;
                }
                return str;
            }

            public static string AlphabetCaesarEncryptChar(string c, int offset = 3)
            {
                var t = c;

                if (alphabetLowercase.Contains(t))
                {
                    var index = alphabetLowercase.IndexOf(t) - offset;
                    if (index < 0)
                        index += 26;

                    if (index < alphabetLowercase.Count && index >= 0)
                    {
                        return alphabetLowercase[index];
                    }
                }

                if (alphabetUppercase.Contains(t))
                {
                    var index = alphabetUppercase.IndexOf(t) - offset;
                    if (index < 0)
                        index += 26;

                    if (index < alphabetUppercase.Count && index >= 0)
                    {
                        return alphabetUppercase[index];
                    }
                }

                return t;
            }

            public static string AlphabetAtbashEncrypt(string c)
            {
                var t = c;
                var str = "";
                foreach (var ch in t)
                {
                    var pl = ch.ToString();
                    pl = AlphabetAtbashEncryptChar(ch.ToString());
                    str += pl;
                }
                return str;
            }

            public static string AlphabetAtbashEncryptChar(string c)
            {
                var t = c;

                if (alphabetLowercase.Contains(t))
                {
                    var index = -(alphabetLowercase.IndexOf(t) - alphabetLowercase.Count + 1);
                    if (index < alphabetLowercase.Count && index >= 0)
                    {
                        return alphabetLowercase[index];
                    }
                }

                if (alphabetUppercase.Contains(t))
                {
                    var index = -(alphabetUppercase.IndexOf(t) - alphabetUppercase.Count + 1);
                    if (index < alphabetUppercase.Count && index >= 0)
                    {
                        return alphabetUppercase[index];
                    }
                }

                return t;
            }

            public static List<string> alphabetLowercase = new List<string>
            {
                "a",
                "b",
                "c",
                "d",
                "e",
                "f",
                "g",
                "h",
                "i",
                "j",
                "k",
                "l",
                "m",
                "n",
                "o",
                "p",
                "q",
                "r",
                "s",
                "t",
                "u",
                "v",
                "w",
                "x",
                "y",
                "z"
            };

            public static List<string> alphabetUppercase = new List<string>
            {
                "A",
                "B",
                "C",
                "D",
                "E",
                "F",
                "G",
                "H",
                "I",
                "J",
                "K",
                "L",
                "M",
                "N",
                "O",
                "P",
                "Q",
                "R",
                "S",
                "T",
                "U",
                "V",
                "W",
                "X",
                "Y",
                "Z"
            };

            public static List<string> kevin = new List<string>
            {
                "@",
                "|}",
                "(",
                "|)",
                "[-",
                "T-",
                "&",
                "|-|",
                "!",
                "_/",
                "|<",
                "|",
                "^^",
                "^",
                "0",
                "/>",
                "\\<",
                "|-",
                "5",
                "-|-",
                "(_)",
                "\\/",
                "\\/\\/",
                "*",
                "-/",
                "-/_"
            };

            public static List<string> binary = new List<string>
            {
                "01100001", // a
			    "01100010", // b
			    "01100011", // c
			    "01100100", // d
			    "01100101", // e
			    "01100110", // f
			    "01100111", // g
			    "01101000", // h
			    "01001001", // i
			    "01001001", // i
			    "01001010", // j
			    "01001011", // k
			    "01001100", // l
			    "01001101", // m
			    "01001110", // n
			    "01001111", // o
			    "01010000", // p
			    "01010001", // q
			    "01010010", // r
			    "01010011", // s
			    "01010100", // t
			    "01010101", // u
			    "01010110", // v
			    "01010111", // w
			    "01011000", // x
			    "01011001", // y
			    "01011010", // z
		    };
        }
    }
}
