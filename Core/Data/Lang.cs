using System;
using System.Collections.Generic;

using SimpleJSON;

using BetterLegacy.Configs;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Language dictionary wrapper that stores strings for multiple languages. Lang is for individual storing of languages, whilst <see cref="LangDictionary"/> is for global language keys.
    /// </summary>
    public class Lang : Exists
    {
        #region Main

        public Lang() { }

        public Lang(string text) : this(Language.English, text) { }

        public Lang(Language language, string text) => languages[language] = text;

        public Lang(string[] array) => Read(array);

        public Lang(Dictionary<Language, string> languages) => this.languages = languages;

        /// <summary>
        /// Dictionary that contains the languages.
        /// </summary>
        public Dictionary<Language, string> languages = new Dictionary<Language, string>();

        /// <summary>
        /// Global dictionary of languages.
        /// </summary>
        public static Dictionary<Language, LangDictionary> global = new Dictionary<Language, LangDictionary>();

        static LangDictionary defaultLang = new LangDictionary();

        /// <summary>
        /// The current language dictionary.
        /// </summary>
        public static LangDictionary Current => global.TryGetValue(CoreConfig.Instance.Language.Value, out LangDictionary langDictionary) ? langDictionary : defaultLang;

        /// <summary>
        /// Loads the global language dictionaries.
        /// </summary>
        public static void LoadGlobal()
        {
            var langValues = Helpers.EnumHelper.GetValues<Language>();
            LoadGlobalAssetPack(AssetPack.BuiltIn, langValues);
            var assetPacks = AssetPack.AssetPacks;
            for (int i = 0; i < assetPacks.Count; i++)
            {
                var assetPack = assetPacks[i];
                LoadGlobalAssetPack(assetPack, langValues);
            }
        }

        static void LoadGlobalAssetPack(AssetPack assetPack, Language[] langValues)
        {
            for (int i = 0; i < langValues.Length; i++)
            {
                var langValue = langValues[i];
                var name = RTString.SplitWords(langValue.ToString()).Replace(" ", "_");
                var path = $"core/lang/{name}.json";
                if (!assetPack.HasFile(path))
                    continue;

                var langDictionary = LangDictionary.Parse(JSON.Parse(RTFile.ReadFromFile(assetPack.GetPath(path))));
                langDictionary.language = langValue;
                if (global.TryGetValue(langValue, out LangDictionary orig))
                    orig.AddDictionary(langDictionary);
                else
                    global[langValue] = langDictionary;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a string from the <see cref="languages"/> dictionary. If it wasn't found, it'll get the English language.
        /// </summary>
        /// <param name="language">Language to find.</param>
        /// <returns>Returns a string from the dictionary if it was found. If no string was found, then returns null.</returns>
        public string GetText() => GetText(CurrentLanguage);

        /// <summary>
        /// Tries to get a string in a specific language.
        /// </summary>
        /// <param name="language">Language to find a string from.</param>
        /// <param name="text">Result string.</param>
        /// <returns>Returns true if a language exists in the <see cref="languages"/> dictionary, otherwise returns false.</returns>
        public bool TryGetText(Language language, out string text) => languages.TryGetValue(language, out text);

        /// <summary>
        /// Gets a string from the <see cref="languages"/> dictionary. If it wasn't found, it'll get the default.
        /// </summary>
        /// <param name="language">Language to find.</param>
        /// <param name="defaultLanguage">The default.</param>
        /// <returns>Returns a string from the dictionary if it was found. If no string was found, then returns null.</returns>
        public string GetText(Language language, Language defaultLanguage)
        {
            if (TryGetText(language, out string text))
                return text;
            if (TryGetText(defaultLanguage, out string defaultText))
                return defaultText;
            return null;
        }

        /// <summary>
        /// Gets a string from the <see cref="languages"/> dictionary. If it wasn't found, it'll get the English language.
        /// </summary>
        /// <param name="language">Language to find.</param>
        /// <returns>Returns a string from the dictionary if it was found. If no string was found, then returns null.</returns>
        public string GetText(Language language) => GetText(language, Language.English);

        /// <summary>
        /// Tries to parse a language dictionary.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <param name="lang">Result language.</param>
        /// <returns>Returns true if the lang parsed successfully, otherwise returns false.</returns>
        public static bool TryParse(JSONNode jn, out Lang lang)
        {
            lang = Parse(jn);
            return !lang.languages.IsEmpty();
        }

        /// <summary>
        /// Parses a language dictionary.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns a parsed <see cref="Lang"/>.</returns>
        public static Lang Parse(JSONNode jn)
        {
            var lang = new Lang();
            lang.Read(jn);
            return lang;
        }

        /// <summary>
        /// Reads from a JSON and applies it to the language dictionary.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        public void Read(JSONNode jn)
        {
            if (jn.IsString) // default string. example: { "text": "jn" } with "jn" being the jn parameter.
                this[Language.English] = jn;
            else if (jn.IsArray) // parses array.
            {
                for (int i = 0; i < jn.Count; i++)
                    this[i] = jn[i];
            }
            else if (jn.IsObject) // parses an object. example: { "english": "a", "spanish": "b" }
            {
                foreach (var pair in jn.Linq)
                    if (Enum.TryParse(pair.Key, true, out Language language))
                        this[language] = pair.Value;
            }
        }

        /// <summary>
        /// Reads from a string array and applies it to the language dictionary.
        /// </summary>
        /// <param name="array">String array to read from.</param>
        public void Read(string[] array)
        {
            for (int i = 0; i < array.Length; i++)
                this[i] = array[i];
        }

        /// <summary>
        /// Writes this instance of <see cref="Lang"/> to a <see cref="JSONNode"/>.
        /// </summary>
        /// <returns>Returns a written <see cref="JSONNode"/>.</returns>
        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");

            if (languages.Count == 1 && languages.TryGetValue(Language.English, out string eng))
                jn = eng;
            else
                foreach (var pair in languages)
                    jn[pair.Key.ToString().ToLower()] = pair.Value;

            return jn;
        }

        #endregion

        #region Operators

        public string this[string language]
        {
            get => languages[Parser.TryParse(language, Language.English)];
            set => languages[Parser.TryParse(language, Language.English)] = value;
        }

        public string this[int language]
        {
            get => languages[(Language)language];
            set => languages[(Language)language] = value;
        }

        public string this[Language language]
        {
            get => languages[language];
            set => languages[language] = value;
        }

        public static implicit operator string(Lang lang) => lang ? lang.GetText() : null;

        public static implicit operator Lang(string input) => new Lang(CurrentLanguage, input);

        public static Language CurrentLanguage => CoreConfig.Instance?.Language?.Value ?? Language.English;

        public string ToLower() => GetText()?.ToLower() ?? string.Empty;

        public override string ToString() => this;

        #endregion

        public class LangDictionary : PAObject<LangDictionary>
        {
            public Language language;

            public Dictionary<string, string> strings = new Dictionary<string, string>();

            public override void CopyData(LangDictionary orig, bool newID = true)
            {
                language = orig.language;
                strings = new Dictionary<string, string>(orig.strings);
            }

            public override void ReadJSON(JSONNode jn)
            {
                if (jn == null)
                    return;

                if (jn.IsArray)
                    for (int i = 0; i < jn["strings"].Count; i++)
                        strings[jn["strings"][i]["key"]] = jn["strings"][i]["text"];
                else if (jn.IsObject)
                    foreach (var keyValuePair in jn.Linq)
                        strings[keyValuePair.Key] = keyValuePair.Value;
            }

            public override JSONNode ToJSON()
            {
                var jn = Parser.NewJSONObject();

                foreach (var keyValuePair in strings)
                    jn[keyValuePair.Key] = keyValuePair.Value;

                return jn;
            }

            public string GetOrDefault(string key, string defaultValue) => !string.IsNullOrEmpty(key) && strings.TryGetValue(key, out string value) ? value : defaultValue;

            public void AddDictionary(LangDictionary langDictionary)
            {
                foreach (var keyValuePair in langDictionary.strings)
                    strings[keyValuePair.Key] = keyValuePair.Value;
            }
        }
    }
}
