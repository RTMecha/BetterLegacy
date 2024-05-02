using BetterLegacy.Components;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BetterLegacy.Editor
{
    public class EditorPrefabHolder
    {
        public static EditorPrefabHolder Instance { get; set; }
        public EditorPrefabHolder()
        {
            Instance = this;
        }

        public Transform PrefabParent { get; set; }
        public GameObject StringInputField { get; set; }
        public GameObject NumberInputField { get; set; }
        public GameObject DeleteButton { get; set; }
        public GameObject Function1Button { get; set; }
        public GameObject Function2Button { get; set; }
    }

    public class DeleteButtonStorage : MonoBehaviour
    {
        [SerializeField]
        public Button button;

        [SerializeField]
        public Image baseImage;

        [SerializeField]
        public Image image;
    }

    public class FunctionButtonStorage : MonoBehaviour
    {
        [SerializeField]
        public Button button;

        [SerializeField]
        public Text text;
    }

    public class InputFieldStorage : MonoBehaviour
    {
        [SerializeField]
        public Button leftGreaterButton;
        [SerializeField]
        public Button leftButton;
        [SerializeField]
        public Button middleButton;
        [SerializeField]
        public Button rightButton;
        [SerializeField]
        public Button rightGreaterButton;
        [SerializeField]
        public InputField inputField;
    }

    public class PrefabPanelStorage : MonoBehaviour
    {
        [SerializeField]
        public Button button;

        [SerializeField]
        public Text nameText;

        [SerializeField]
        public Text typeNameText;

        [SerializeField]
        public Image typeImage;

        [SerializeField]
        public Image typeImageShade;

        [SerializeField]
        public Image typeIconImage;

        [SerializeField]
        public Button deleteButton;
    }

    public class ViewThemePanelStorage : MonoBehaviour
    {
        [SerializeField]
        public Image baseImage;

        [SerializeField]
        public Text text;

        [SerializeField]
        public Text baseColorsText;

        [SerializeField]
        public List<Image> baseColors;

        [SerializeField]
        public Text playerColorsText;

        [SerializeField]
        public List<Image> playerColors;

        [SerializeField]
        public Text objectColorsText;

        [SerializeField]
        public List<Image> objectColors;

        [SerializeField]
        public Text backgroundColorsText;

        [SerializeField]
        public List<Image> backgroundColors;

        [SerializeField]
        public Text effectColorsText;

        [SerializeField]
        public List<Image> effectColors;

        [SerializeField]
        public Button useButton;

        [SerializeField]
        public Button convertButton;
    }

    public class ThemePanelStorage : MonoBehaviour
    {
        [SerializeField]
        public Image baseImage;

        [SerializeField]
        public Button button;

        [SerializeField]
        public Image color1;
        [SerializeField]
        public Image color2;
        [SerializeField]
        public Image color3;
        [SerializeField]
        public Image color4;

        [SerializeField]
        public Text text;

        [SerializeField]
        public Button edit;
        [SerializeField]
        public Button delete;
    }

    public class TimelineObjectStorage : MonoBehaviour
    {
        [SerializeField]
        public HoverUI hoverUI;

        [SerializeField]
        public Image image;

        [SerializeField]
        public TextMeshProUGUI text;

        [SerializeField]
        public EventTrigger eventTrigger;
    }
}
