using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Language dictionary wrapper that stores strings for multiple languages.
    /// </summary>
    public class Lang : Exists
    {
        public Lang() { }

        public Lang(string text) => languages[Language.English] = text;

        public Lang(Dictionary<Language, string> languages) => this.languages = languages;

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

        /// <summary>
        /// Dictionary that contains the languages.
        /// </summary>
        public Dictionary<Language, string> languages = new Dictionary<Language, string>();

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

            if (jn.IsString) // default string. example: { "text": "jn" } with "jn" being the jn parameter.
                lang[Language.English] = jn;
            else if (jn.IsArray) // parses array.
            {
                for (int i = 0; i < jn.Count; i++)
                    lang[i] = jn[i];
            }
            else if (jn.IsObject) // parses an object. example: { "english": "a", "spanish": "b" }
            {
                if (jn["text"] != null)
                {
                    lang[Language.English] = jn["text"];
                    return lang;
                }

                foreach (var pair in jn.Linq)
                    if (Enum.TryParse(pair.Key, true, out Language language))
                        lang[language] = pair.Value;
            }

            return lang;
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

        public static implicit operator string(Lang lang) => lang ? lang.GetText(Configs.CoreConfig.Instance.Language.Value) : null;

        public override string ToString() => this;
    }
}
