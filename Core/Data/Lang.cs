using System;
using System.Collections.Generic;

using SimpleJSON;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Language dictionary wrapper that stores strings for multiple languages.
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

        #endregion

        #region Methods

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
                if (jn["text"] != null)
                {
                    this[Language.English] = jn["text"];
                    return;
                }

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

        public static implicit operator string(Lang lang) => lang ? lang.GetText(Configs.CoreConfig.Instance.Language.Value) : null;

        public static implicit operator Lang(string input) => new Lang(Configs.CoreConfig.Instance.Language.Value, input);

        public override string ToString() => this;

        #endregion
    }
}
