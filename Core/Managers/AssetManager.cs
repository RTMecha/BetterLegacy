using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Core.Managers
{
    /// <summary>
    /// This class is used to store and generate specific assets to use for other mods.
    /// </summary>
    public class AssetManager : MonoBehaviour
    {
        public static AssetManager inst;

        void Awake()
        {
            inst = this;
        }

        public static Dictionary<string, Sprite> SpriteAssets { get; set; } = new Dictionary<string, Sprite>();
        public static Dictionary<string, AudioClip> AudioAssets { get; set; } = new Dictionary<string, AudioClip>();

        GameObject stringInput;
        public GameObject StringInput
        {
            get
            {
                if (!stringInput)
                {
                    stringInput = new GameObject("String Input");

                    var stringInputRT = stringInput.AddComponent<RectTransform>();

                    var stringInputImage = stringInput.AddComponent<Image>();
                    stringInputImage.color = new Color(0.9373f, 0.9216f, 0.9373f);

                    var stringInputField = stringInput.AddComponent<InputField>();

                    var stringInputLayoutElement = stringInput.AddComponent<LayoutElement>();
                    stringInputLayoutElement.minWidth = 104f;

                    stringInputRT.sizeDelta = new Vector2(104f, 34f);

                    var placeholder = TextObject.Duplicate(stringInputRT, "Placeholder").GetComponent<Text>();

                    stringInputField.placeholder = placeholder;

                    var text = TextObject.Duplicate(stringInputRT, "Text").GetComponent<Text>();

                    ((RectTransform)stringInputField.transform).sizeDelta = new Vector2(104f, 34f);
                    stringInputField.textComponent = text;
                    stringInputField.characterValidation = InputField.CharacterValidation.None;
                    stringInputField.characterLimit = 0;

                    DontDestroyOnLoad(stringInput);
                }

                return stringInput;
            }
        }

        GameObject textObject;
        public GameObject TextObject
        {
            get
            {
                if (!textObject)
                {
                    textObject = new GameObject("Text");
                    var textObjectRT = textObject.AddComponent<RectTransform>();
                    var textObjectText = textObject.AddComponent<Text>();
                    if (FontManager.inst && FontManager.inst.allFonts.ContainsKey("Inconsolata Variable"))
                        textObjectText.font = FontManager.inst.allFonts["Inconsolata Variable"];
                    else
                        textObjectText.font = Font.GetDefault();

                    textObjectText.fontSize = 19;
                    textObjectText.alignment = TextAnchor.MiddleLeft;
                    textObjectText.color = new Color(0.1294f, 0.1294f, 0.1294f);

                    DontDestroyOnLoad(textObject);
                }

                return textObject;
            }
        }
    }
}
