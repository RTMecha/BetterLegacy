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
                if (allFonts.ContainsKey(key))
                    return allFonts[key];

                if (allFonts.ContainsKey(defaultFont))
                    return allFonts[defaultFont];

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

            foreach (var beatmapObject in GameData.Current.BeatmapObjects.Where(x =>
                            x.shape == 4 && x.Alive && x.objectType != BeatmapObject.ObjectType.Empty))
            {
                try
                {
                    if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.visualObject is TextObject textObject && textObject.textMeshPro)
                    {
                        var tmp = textObject.textMeshPro;

                        var currentAudioTime = AudioManager.inst.CurrentAudioSource.time;
                        var currentAudioLength = AudioManager.inst.CurrentAudioSource.clip.length;

                        var str = textObject.text;

                        #region Audio

                        if (beatmapObject.text.Contains("<msAudio000>"))
                        {
                            str = str.Replace("<msAudio000>", TextTranslater.PreciseToMilliSeconds(currentAudioTime));
                        }

                        if (beatmapObject.text.Contains("<msAudio00>"))
                        {
                            str = str.Replace("<msAudio00>", TextTranslater.PreciseToMilliSeconds(currentAudioTime, "{0:00}"));
                        }

                        if (beatmapObject.text.Contains("<msAudio0>"))
                        {
                            str = str.Replace("<msAudio0>", TextTranslater.PreciseToMilliSeconds(currentAudioTime, "{0:0}"));
                        }

                        if (beatmapObject.text.Contains("<sAudio00>"))
                        {
                            str = str.Replace("<sAudio00>", TextTranslater.PreciseToSeconds(currentAudioTime));
                        }

                        if (beatmapObject.text.Contains("<sAudio0>"))
                        {
                            str = str.Replace("<sAudio0>", TextTranslater.PreciseToSeconds(currentAudioTime, "{0:0}"));
                        }

                        if (beatmapObject.text.Contains("<mAudio00>"))
                        {
                            str = str.Replace("<mAudio00>", TextTranslater.PreciseToMinutes(currentAudioTime));
                        }

                        if (beatmapObject.text.Contains("<mAudio0>"))
                        {
                            str = str.Replace("<mAudio0>", TextTranslater.PreciseToMinutes(currentAudioTime, "{0:0}"));
                        }

                        if (beatmapObject.text.Contains("<hAudio00>"))
                        {
                            str = str.Replace("<hAudio00>", TextTranslater.PreciseToHours(currentAudioTime));
                        }

                        if (beatmapObject.text.Contains("<hAudio0>"))
                        {
                            str = str.Replace("<hAudio0>", TextTranslater.PreciseToHours(currentAudioTime, "{0:0}"));
                        }

                        #endregion

                        #region Audio Left

                        CoreHelper.RegexMatch(beatmapObject.text, new Regex(@"<msAudioLeftSpan=([0-9.:]+)>"), match =>
                        {
                            str = str.Replace(match.Groups[0].ToString(), TextTranslater.PreciseToMilliSeconds(currentAudioLength - currentAudioTime, "{" + match.Groups[1].ToString() + "}"));
                        });

                        CoreHelper.RegexMatch(beatmapObject.text, new Regex(@"<sAudioLeftSpan=([0-9.:]+)>"), match =>
                        {
                            str = str.Replace(match.Groups[0].ToString(), TextTranslater.PreciseToSeconds(currentAudioLength - currentAudioTime, "{" + match.Groups[1].ToString() + "}"));
                        });

                        CoreHelper.RegexMatch(beatmapObject.text, new Regex(@"<mAudioLeftSpan=([0-9.:]+)>"), match =>
                        {
                            str = str.Replace(match.Groups[0].ToString(), TextTranslater.PreciseToMinutes(currentAudioLength - currentAudioTime, "{" + match.Groups[1].ToString() + "}"));
                        });

                        CoreHelper.RegexMatch(beatmapObject.text, new Regex(@"<hAudioLeftSpan=([0-9.:]+)>"), match =>
                        {
                            str = str.Replace(match.Groups[0].ToString(), TextTranslater.PreciseToHours(currentAudioLength - currentAudioTime, "{" + match.Groups[1].ToString() + "}"));
                        });

                        // No time span
                        CoreHelper.RegexMatch(beatmapObject.text, new Regex(@"<msAudioLeft=([0-9.:]+)>"), match =>
                        {
                            str = str.Replace(match.Groups[0].ToString(), string.Format("{" + match.Groups[1].ToString() + "}", (int)((currentAudioLength - currentAudioTime) * 1000)));
                        });

                        CoreHelper.RegexMatch(beatmapObject.text, new Regex(@"<sAudioLeft=([0-9.:]+)>"), match =>
                        {
                            str = str.Replace(match.Groups[0].ToString(), string.Format("{" + match.Groups[1].ToString() + "}", currentAudioLength - currentAudioTime));
                        });

                        CoreHelper.RegexMatch(beatmapObject.text, new Regex(@"<mAudioLeft=([0-9.:]+)>"), match =>
                        {
                            str = str.Replace(match.Groups[0].ToString(), string.Format("{" + match.Groups[1].ToString() + "}", (int)((currentAudioLength - currentAudioTime) / 60)));
                        });

                        CoreHelper.RegexMatch(beatmapObject.text, new Regex(@"<hAudioLeft=([0-9.:]+)>"), match =>
                        {
                            str = str.Replace(match.Groups[0].ToString(), string.Format("{" + match.Groups[1].ToString() + "}", (currentAudioLength - currentAudioTime) / 600));
                        });

                        #endregion

                        #region Real Time

                        if (beatmapObject.text.Contains("<sRTime00>"))
                        {
                            str = str.Replace("<sRTime00>", DateTime.Now.ToString("ss"));
                        }

                        if (beatmapObject.text.Contains("<sRTime0>"))
                        {
                            str = str.Replace("<sRTime0>", DateTime.Now.ToString("s"));
                        }

                        if (beatmapObject.text.Contains("<mRTime00>"))
                        {
                            str = str.Replace("<mRTime00>", DateTime.Now.ToString("mm"));
                        }

                        if (beatmapObject.text.Contains("<mRTime0>"))
                        {
                            str = str.Replace("<mRTime0>", DateTime.Now.ToString("m"));
                        }

                        if (beatmapObject.text.Contains("<hRTime0012>"))
                        {
                            str = str.Replace("<hRTime0012>", DateTime.Now.ToString("hh"));
                        }

                        if (beatmapObject.text.Contains("<hRTime012>"))
                        {
                            str = str.Replace("<hRTime012>", DateTime.Now.ToString("h"));
                        }

                        if (beatmapObject.text.Contains("<hRTime0024>"))
                        {
                            str = str.Replace("<hRTime0024>", DateTime.Now.ToString("HH"));
                        }

                        if (beatmapObject.text.Contains("<hRTime024>"))
                        {
                            str = str.Replace("<hRTime024>", DateTime.Now.ToString("H"));
                        }

                        if (beatmapObject.text.Contains("<domRTime00>"))
                        {
                            str = str.Replace("<domRTime00>", DateTime.Now.ToString("dd"));
                        }

                        if (beatmapObject.text.Contains("<domRTime0>"))
                        {
                            str = str.Replace("<domRTime0>", DateTime.Now.ToString("d"));
                        }

                        if (beatmapObject.text.Contains("<dowRTime00>"))
                        {
                            str = str.Replace("<dowRTime00>", DateTime.Now.ToString("dddd"));
                        }

                        if (beatmapObject.text.Contains("<dowRTime0>"))
                        {
                            str = str.Replace("<dowRTime0>", DateTime.Now.ToString("ddd"));
                        }

                        if (beatmapObject.text.Contains("<mmRTime00>"))
                        {
                            str = str.Replace("<mnRTime00>", DateTime.Now.ToString("MM"));
                        }

                        if (beatmapObject.text.Contains("<mnRTime0>"))
                        {
                            str = str.Replace("<mnRTime0>", DateTime.Now.ToString("M"));
                        }

                        if (beatmapObject.text.Contains("<mmRTime00>"))
                        {
                            str = str.Replace("<mmRTime00>", DateTime.Now.ToString("MMMM"));
                        }

                        if (beatmapObject.text.Contains("<mmRTime0>"))
                        {
                            str = str.Replace("<mmRTime0>", DateTime.Now.ToString("MMM"));
                        }

                        if (beatmapObject.text.Contains("<yRTime0000>"))
                        {
                            str = str.Replace("<yRTime0000>", DateTime.Now.ToString("yyyy"));
                        }

                        if (beatmapObject.text.Contains("<yRTime00>"))
                        {
                            str = str.Replace("<yRTime00>", DateTime.Now.ToString("yy"));
                        }

                        #endregion

                        #region Players

                        var phRegex = new Regex(@"<playerHealth=(.*?)>");
                        var phMatch = phRegex.Match(beatmapObject.text);

                        if (phMatch.Success && phMatch.Groups.Count > 1 && int.TryParse(phMatch.Groups[1].ToString(), out int num))
                        {
                            if (InputDataManager.inst.players.Count > num)
                            {
                                str = str.Replace("<playerHealth=" + num.ToString() + ">", InputDataManager.inst.players[num].health.ToString());
                            }
                            else
                            {
                                str = str.Replace("<playerHealth=" + num.ToString() + ">", "");
                            }
                        }

                        if (beatmapObject.text.Contains("<playerHealthAll>"))
                        {
                            var ph = 0;

                            for (int i = 0; i < InputDataManager.inst.players.Count; i++)
                            {
                                ph += InputDataManager.inst.players[i].health;
                            }

                            str = str.Replace("<playerHealthAll>", ph.ToString());
                        }

                        var pdRegex = new Regex(@"<playerDeaths=(.*?)>");
                        var pdMatch = pdRegex.Match(beatmapObject.text);

                        if (pdMatch.Success && pdMatch.Groups.Count > 1 && int.TryParse(pdMatch.Groups[1].ToString(), out int numDeath))
                        {
                            if (InputDataManager.inst.players.Count > numDeath)
                            {
                                str = str.Replace("<playerDeaths=" + numDeath.ToString() + ">", InputDataManager.inst.players[numDeath].PlayerDeaths.Count.ToString());
                            }
                            else
                            {
                                str = str.Replace("<playerDeaths=" + numDeath.ToString() + ">", "");
                            }
                        }

                        if (beatmapObject.text.Contains("<playerDeathsAll>"))
                        {
                            var pd = 0;

                            for (int i = 0; i < InputDataManager.inst.players.Count; i++)
                            {
                                pd += InputDataManager.inst.players[i].PlayerDeaths.Count;
                            }

                            str = str.Replace("<playerDeathsAll>", pd.ToString());
                        }

                        var phiRegex = new Regex(@"<playerHits=(.*?)>");
                        var phiMatch = phiRegex.Match(beatmapObject.text);

                        if (phiMatch.Success && phiMatch.Groups.Count > 1 && int.TryParse(phiMatch.Groups[1].ToString(), out int numHit))
                        {
                            if (InputDataManager.inst.players.Count > numHit)
                            {
                                str = str.Replace("<playerHits=" + numHit.ToString() + ">", InputDataManager.inst.players[numHit].PlayerHits.Count.ToString());
                            }
                            else
                            {
                                str = str.Replace("<playerHits=" + numHit.ToString() + ">", "");
                            }
                        }

                        if (beatmapObject.text.Contains("<playerHitsAll>"))
                        {
                            var pd = 0;

                            for (int i = 0; i < InputDataManager.inst.players.Count; i++)
                            {
                                pd += InputDataManager.inst.players[i].PlayerHits.Count;
                            }

                            str = str.Replace("<playerHitsAll>", pd.ToString());
                        }

                        if (beatmapObject.text.Contains("<playerBoostCount>"))
                        {
                            str = str.Replace("<playerBoostCount>", LevelManager.BoostCount.ToString());
                        }

                        #endregion

                        #region QuickElement

                        var qeRegex = new Regex(@"<quickElement=(.*?)>");
                        var qeMatch = qeRegex.Match(beatmapObject.text);

                        if (qeMatch.Success && qeMatch.Groups.Count > 1)
                        {
                            str = str.Replace("<quickElement=" + qeMatch.Groups[1].ToString() + ">", QuickElementManager.ConvertQuickElement(beatmapObject, qeMatch.Groups[1].ToString()));
                        }

                        #endregion

                        #region Random

                        {
                            var ratRegex = new Regex(@"<randomText=(.*?)>");
                            var ratMatch = ratRegex.Match(beatmapObject.text);

                            if (ratMatch.Success && ratMatch.Groups.Count > 1 && int.TryParse(ratMatch.Groups[1].ToString(), out int ratInt))
                            {
                                str = str.Replace("<randomText=" + ratMatch.Groups[1].ToString() + ">", LSText.randomString(ratInt));
                            }

                            var ranRegex = new Regex(@"<randomNumber=(.*?)>");
                            var ranMatch = ranRegex.Match(beatmapObject.text);

                            if (ranMatch.Success && ratMatch.Groups.Count > 1 && int.TryParse(ranMatch.Groups[1].ToString(), out int ranInt))
                            {
                                str = str.Replace("<randomNumber=" + ranMatch.Groups[1].ToString() + ">", LSText.randomNumString(ranInt));
                            }
                        }

                        #endregion

                        #region Theme

                        var beatmapTheme = CoreHelper.CurrentBeatmapTheme;

                        {
                            var matchCollection = Regex.Matches(str, "<themeObject=(.*?)>");

                            foreach (var obj in matchCollection)
                            {
                                var match = (Match)obj;
                                if (match.Groups.Count > 1)
                                    str = str.Replace(match.Groups[0].Value, $"<#{LSColors.ColorToHex(beatmapTheme.GetObjColor(int.Parse(match.Groups[1].ToString())))}>");
                            }
                        }

                        {
                            var matchCollection = Regex.Matches(str, "<themeBGs=(.*?)>");

                            foreach (var obj in matchCollection)
                            {
                                var match = (Match)obj;
                                if (match.Groups.Count > 1)
                                    str = str.Replace(match.Groups[0].Value, $"<#{LSColors.ColorToHex(beatmapTheme.GetBGColor(int.Parse(match.Groups[1].ToString())))}>");
                            }
                        }

                        {
                            var matchCollection = Regex.Matches(str, "<themeFX=(.*?)>");

                            foreach (var obj in matchCollection)
                            {
                                var match = (Match)obj;
                                if (match.Groups.Count > 1)
                                    str = str.Replace(match.Groups[0].Value, $"<#{LSColors.ColorToHex(beatmapTheme.GetFXColor(int.Parse(match.Groups[1].ToString())))}>");
                            }
                        }

                        {
                            var matchCollection = Regex.Matches(str, "<themePlayers=(.*?)>");

                            foreach (var obj in matchCollection)
                            {
                                var match = (Match)obj;
                                if (match.Groups.Count > 1)
                                    str = str.Replace(match.Groups[0].Value, $"<#{LSColors.ColorToHex(beatmapTheme.GetPlayerColor(int.Parse(match.Groups[1].ToString())))}>");
                            }
                        }

                        if (beatmapObject.text.Contains("<themeBG>"))
                        {
                            str = str.Replace("<themeBG>", LSColors.ColorToHex(beatmapTheme.backgroundColor));
                        }

                        if (beatmapObject.text.Contains("<themeGUI>"))
                        {
                            str = str.Replace("<themeGUI>", LSColors.ColorToHex(beatmapTheme.guiColor));
                        }

                        if (beatmapObject.text.Contains("<themeTail>"))
                        {
                            str = str.Replace("<themeTail>", LSColors.ColorToHex(beatmapTheme.guiAccentColor));
                        }

                        #endregion

                        #region LevelRank

                        if (beatmapObject.text.Contains("<levelRank>"))
                        {
                            DataManager.LevelRank levelRank =
                                EditorManager.inst == null && LevelManager.CurrentLevel != null ?
                                    LevelManager.GetLevelRank(LevelManager.CurrentLevel) :
                                    DataManager.inst.levelRanks[0];

                            str = str.Replace("<levelRank>", $"<color=#{LSColors.ColorToHex(levelRank.color)}><b>{levelRank.name}</b></color>");
                        }

                        if (beatmapObject.text.Contains("<levelRankName>"))
                        {
                            DataManager.LevelRank levelRank =
                                EditorManager.inst == null && LevelManager.CurrentLevel != null ?
                                    LevelManager.GetLevelRank(LevelManager.CurrentLevel) :
                                    DataManager.inst.levelRanks[0];

                            str = str.Replace("<levelRankName>", levelRank.name);
                        }

                        if (beatmapObject.text.Contains("<levelRankColor>"))
                        {
                            DataManager.LevelRank levelRank =
                                EditorManager.inst == null && LevelManager.CurrentLevel != null ?
                                    LevelManager.GetLevelRank(LevelManager.CurrentLevel) :
                                    DataManager.inst.levelRanks[0];

                            str = str.Replace("<levelRankColor>", $"<color=#{LSColors.ColorToHex(levelRank.color)}>");
                        }

                        if (beatmapObject.text.Contains("<accuracy>"))
                            str = str.Replace("<accuracy>", $"{LevelManager.CalculateAccuracy(GameManager.inst.hits.Count, AudioManager.inst.CurrentAudioSource.clip.length)}");

                        #endregion

                        #region Mod stuff

                        {
                            var regex = new Regex(@"<modifierVariable=(.*?)>");
                            var match = regex.Match(beatmapObject.text);

                            if (match.Success && match.Groups.Count > 1 && DataManager.inst.gameData.beatmapObjects.TryFind(x => x.name == match.Groups[1].ToString(), out DataManager.GameData.BeatmapObject other))
                            {
                                str = str.Replace("<modifierVariable=" + match.Groups[1].ToString() + ">", ((BeatmapObject)other).integerVariable.ToString());
                            }
                        }

                        {
                            var regex = new Regex(@"<modifierVariableID=(.*?)>");
                            var match = regex.Match(beatmapObject.text);

                            if (match.Success && match.Groups.Count > 1 && DataManager.inst.gameData.beatmapObjects.TryFind(x => x.id == match.Groups[1].ToString(), out DataManager.GameData.BeatmapObject other))
                            {
                                str = str.Replace("<modifierVariableID=" + match.Groups[1].ToString() + ">", ((BeatmapObject)other).integerVariable.ToString());
                            }
                        }

                        CoreHelper.RegexMatch(beatmapObject.text, new Regex(@"<math=(.*?)>"), match =>
                        {
                            var math = RTMath.Evaluate(RTMath.Replace(match.Groups[1].ToString()));

                            str = str.Replace(match.Groups[0].ToString(), math.ToString());
                        });

                        #endregion

                        textObject.SetText(str);
                    }
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
                RTDebugger.Init();

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
                case "webdings.ttf": return "Webdings";
                case "wingding.ttf": return "Wingdings";
                case "undertale-wingdings.ttf": return "Determination Wingdings";
                #endregion
                #region English Fonts
                case "about_friend_extended_v2_by_matthewtheprep_ddribq5.otf": return "About Friend";
                case "adamwarrenpro-bold.ttf": return "Adam Warren Pro Bold";
                case "adamwarrenpro-bolditalic.ttf": return "Adam Warren Pro BoldItalic";
                case "adamwarrenpro.ttf": return "Adam Warren Pro";
                case "angsaz.ttf": return "Angsana Z";
                case "arrhythmia-font.ttf": return "Arrhythmia";
                case "arial.ttf": return "Arial";
                case "arialbd.ttf": return "Arial Bold";
                case "arialbi.ttf": return "Arial Bold Italic";
                case "ariali.ttf": return "Arial Italic";
                case "ariblk.ttf": return "Arial Black";
                case "badabb__.ttf": return "BadaBoom BB";
                case "calibri.ttf": return "Calibri";
                case "calibrii.ttf": return "Calibri Italic";
                case "calibril.ttf": return "Calibri Light";
                case "calibrili.ttf": return "Calibri Light Italic";
                case "calibriz.ttf": return "Calibri Bold Italic";
                case "cambria.ttc": return "Cambria";
                case "cambriab.ttf": return "Cambria Bold";
                case "cambriaz.ttf": return "Cambria Bold Italic";
                case "candara.ttf": return "Candara";
                case "candarab.ttf": return "Candara Bold";
                case "candarai.ttf": return "Candara Italic";
                case "candaral.ttf": return "Candara Light";
                case "candarali.ttf": return "Candara Light Italic";
                case "candaraz.ttf": return "Candara Bold Italic";
                case "construction.ttf": return "Construction";
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
                case "impact.ttf": return "Impact";
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
                case "micross.ttf": return "Sans Serif";
                case "monsterfriendback.otf": return "Monster Friend Back";
                case "monsterfriendfore.otf": return "Monster Friend Fore";
                case "necosmic-personalrse.otf": return "Necosmic";
                case "oxygene1.ttf": return "Oxygene";
                case "piraka theory gf.ttf": return "Piraka Theory";
                case "persons unknown.otf": return "Persons Unknown";
                case "plastiquekingdom.ttf": return "PlastiqueKingdom";
                case "piraka.ttf": return "Piraka";
                case "pusab___.otf": return "Pusab";
                case "rahkshi font.ttf": return "Rahkshi";
                case "revuebt-regular 1.otf": return "Revue 1";
                case "revuebt-regular.otf": return "Revue";
                case "times.ttf": return "Times New Roman";
                case "timesbd.ttf": return "Times New Roman Bold";
                case "timesbi.ttf": return "Times New Roman Bold Italic";
                case "timesi.ttf": return "Times New Roman Italic";
                case "transdings-waoo.ttf": return "Transdings";
                case "nexa bold.otf": return "Nexa Bold";
                case "nexabook.otf": return "Nexa Book";
                case "sans mita aprilia.ttf": return "Sans Sans";
                case "spookyhollow.ttf": return "SpookyHollow";
                #endregion
                #region Thai Fonts
                case "angsa.ttf": return "Angsana";
                case "angsab.ttf": return "Angsana Bold";
                case "angsai.ttf": return "Angsana Italic";
                case "angsananewbolditalic.ttf": return "Angsana Bold Italic";
                case "krr manga s.otf": return "Manga";
                case "pixellet.ttf": return "Pixellet";
                case "ploypilinfont.ttf": return "Ploypilin";
                #endregion
                #region Russian Fonts
                case "18vag rounded m bold.ttf": return "VAG Rounded";
                case "milk(rus by lyajka) regular.ttf": return "LemonMilkRus";
                case "1 nevrouz m regular.ttf": return "Nevrouz";
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
                case "dotgothic16-regular.ttf": return "DotGothic16";
                case "jkg-l_3.ttf": return "Jkg";
                case "monomaniacone-regular.ttf": return "Monomaniac One";
                case "rocknrollone-regular.ttf": return "RocknRoll One";
                case "ronde-b_square.otf": return "Ronde-B";
                case "yokomoji.otf": return "Yokomoji";
                #endregion
                #region Korean Fonts
                case "himelody-regular.ttf": return "HiMelody";
                case "jua-regular.ttf": return "Jua";
                case "kiranghaerang-regular.ttf": return "KirangHaerang";
                case "notosanskr-variablefont_wght.ttf": return "NotoSansKR";
                #endregion
                #region Chinese Fonts
                case "azppt regular.ttf": return "AZPPT";
                case "hanyisentypagoda regular.ttf": return "HanyiSentyPagoda";
                case "notosanstc-variablefont.ttf": return "NotoSansTC";
                case "notosanshk-variablefont.ttf": return "NotoSansHK";
                case "notosanssc-variablefont.ttf": return "NotoSansSC";
                case "yrdzst medium.ttf": return "YRDZST";
                #endregion
                #region Tagalog Fonts
                case "notosanstagalog-regular.ttf": return "NotoSansTagalog";
                case "tagdoc93.ttf": return "TagDoc93";
                #endregion
            }
            return _name1;
        }

        public static class TextTranslater
        {
            public static string ReplaceProperties(string str) => string.IsNullOrEmpty(str) ? str : str
                .Replace("{{GameVersion}}", ProjectArrhythmia.GameVersion.ToString())
                .Replace("{{ModVersion}}", LegacyPlugin.ModVersion.ToString())
                .Replace("{{AppDirectory}}", RTFile.ApplicationDirectory)
                .Replace("{{BepInExAssetsDirectory}}", RTFile.BepInExAssetsPath)
                .Replace("{{LevelPath}}", GameManager.inst ? GameManager.inst.basePath : RTFile.ApplicationDirectory);

            public static string PreciseToMilliSeconds(float seconds, string format = "{0:000}") => string.Format(format, TimeSpan.FromSeconds(seconds).Milliseconds);

            public static string PreciseToSeconds(float seconds, string format = "{0:00}") => string.Format(format, TimeSpan.FromSeconds(seconds).Seconds);

            public static string PreciseToMinutes(float seconds, string format = "{0:00}") => string.Format(format, TimeSpan.FromSeconds(seconds).Minutes);

            public static string PreciseToHours(float seconds, string format = "{0:00}") => string.Format(format, TimeSpan.FromSeconds(seconds).Hours);

            public static string SecondsToTime(float seconds)
            {
                var timeSpan = TimeSpan.FromSeconds(seconds);
                return string.Format("{0:D0}:{1:D1}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
            }

            public static string Percentage(float t, float length) => string.Format("{0:000}", (int)RTMath.Percentage(t, length));

            public static string ConvertHealthToEquals(int _num, int _max = 3)
            {
                string str = "[";
                for (int i = 0; i < _num; i++)
                {
                    str += "=";
                }

                int e = -_num + _max;
                if (e > 0)
                {
                    for (int i = 0; i < e; i++)
                    {
                        str += " ";
                    }
                }

                return str += "]";
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
