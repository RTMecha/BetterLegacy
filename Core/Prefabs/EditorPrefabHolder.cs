using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using TMPro;

using BetterLegacy.Core.Components;

namespace BetterLegacy.Core.Prefabs
{
    public class EditorPrefabHolder
    {
        public static EditorPrefabHolder Instance { get; set; }
        public EditorPrefabHolder() { }
        public static void Init() => Instance = new EditorPrefabHolder();

        public Transform PrefabParent { get; set; }
        public GameObject StringInputField { get; set; }
        public GameObject NumberInputField { get; set; }
        public GameObject DefaultInputField { get; set; }

        public GameObject SpriteButton { get; set; }
        public GameObject DeleteButton { get; set; }
        public GameObject Function1Button { get; set; }
        public GameObject Function2Button { get; set; }
        public GameObject Dropdown { get; set; }
        public GameObject CurvesDropdown { get; set; }
        public GameObject Toggle { get; set; }
        public GameObject ToggleButton { get; set; }
        public GameObject Labels { get; set; }
        public GameObject ScrollView { get; set; }

        public GameObject CollapseToggle { get; set; }
        public GameObject CloseButton { get; set; }
        public GameObject ColorsLayout { get; set; }

        public GameObject Scrollbar { get; set; }
        public GameObject Slider { get; set; }

        public GameObject Tag { get; set; }

        public GameObject Dialog { get; set; }
    }

    public class EditorDialogStorage : MonoBehaviour
    {
        [SerializeField]
        public Image topPanel;

        [SerializeField]
        public Text title;
    }

    public class DropdownStorage : MonoBehaviour
    {
        [SerializeField]
        public Dropdown dropdown;

        [SerializeField]
        public GridLayoutGroup templateGrid;

        [SerializeField]
        public ContentSizeFitter templateFitter;

        [SerializeField]
        public Image arrow;

        [SerializeField]
        public HideDropdownOptions hideOptions;
    }

    public class ToggleButtonStorage : MonoBehaviour
    {
        [SerializeField]
        public Toggle toggle;

        [SerializeField]
        public Text label;

        public void Assign(GameObject gameObject)
        {
            if (gameObject.transform.TryFind("Text", out Transform transform))
                label = transform.GetComponent<Text>();
            toggle = gameObject.GetComponent<Toggle>();
        }
    }

    public class DeleteButtonStorage : MonoBehaviour
    {
        [SerializeField]
        public Button button;

        [SerializeField]
        public Image baseImage;

        [SerializeField]
        public Image image;

        public void Assign(GameObject gameObject)
        {
            button = gameObject.GetComponent<Button>();
            if (gameObject.transform.TryFind("bg", out Transform bgTransform))
            {
                baseImage = bgTransform.GetComponent<Image>();
                image = button.image;
                return;
            }

            baseImage = button.image;
            image = gameObject.transform.GetChild(0).GetComponent<Image>();
        }
    }

    public class FunctionButtonStorage : MonoBehaviour
    {
        [SerializeField]
        public Button button;

        [SerializeField]
        public Text label;
    }

    public class SpriteFunctionButtonStorage : FunctionButtonStorage
    {
        [SerializeField]
        public Image image;
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
        public Button subButton;
        [SerializeField]
        public Button addButton;
        [SerializeField]
        public InputField inputField;
        [SerializeField]
        public Toggle lockToggle;
        [SerializeField]
        public EventTrigger eventTrigger;

        public void Assign(GameObject gameObject)
        {
            if (gameObject.transform.TryFind("<<", out Transform leftGreater))
                leftGreaterButton = leftGreater.GetComponent<Button>();

            if (gameObject.transform.TryFind("<", out Transform left))
                leftButton = left.GetComponent<Button>();

            if (gameObject.transform.TryFind("|", out Transform middle))
                middleButton = middle.GetComponent<Button>();
            else if (EditorPrefabHolder.Instance.NumberInputField.transform.TryFind("|", out Transform prefabMiddleTransform))
            {
                var index = leftButton?.transform?.GetSiblingIndex() ?? -2;
                middleButton = prefabMiddleTransform.gameObject.Duplicate(gameObject.transform, "|", index + 1).GetComponent<Button>();
                middleButton.transform.AsRT().sizeDelta = new Vector2(8f, 32f);
                middleButton.gameObject.SetActive(false);
            }

            if (gameObject.transform.TryFind(">", out Transform right))
                rightButton = right.GetComponent<Button>();

            if (gameObject.transform.TryFind(">>", out Transform rightGreater))
                rightGreaterButton = rightGreater.GetComponent<Button>();

            if (gameObject.transform.TryFind("input", out Transform input) && input.gameObject.TryGetComponent(out InputField inputField))
                this.inputField = inputField;
            else if (gameObject.transform.TryFind("time", out Transform time) && time.gameObject.TryGetComponent(out InputField timeField))
                this.inputField = timeField;
            else if (gameObject.TryGetComponent(out InputField baseInput))
                this.inputField = baseInput;

            if (gameObject.transform.TryFind("sub", out Transform subTransform))
                subButton = subTransform.GetComponent<Button>();
            else if (EditorPrefabHolder.Instance.NumberInputField.transform.TryFind("sub", out Transform prefabSubTransform))
            {
                subButton = prefabSubTransform.gameObject.Duplicate(gameObject.transform, "sub").GetComponent<Button>();
                subButton.transform.AsRT().sizeDelta = new Vector2(16f, 32f);
                subButton.gameObject.SetActive(false);
            }

            if (gameObject.transform.TryFind("add", out Transform addTransform))
                addButton = addTransform.GetComponent<Button>();
            else if (EditorPrefabHolder.Instance.NumberInputField.transform.TryFind("add", out Transform prefabAddTransform))
            {
                addButton = prefabAddTransform.gameObject.Duplicate(gameObject.transform, "add").GetComponent<Button>();
                addButton.transform.AsRT().sizeDelta = new Vector2(32f, 32f);
                addButton.gameObject.SetActive(false);
            }

            if (gameObject.transform.TryFind("lock", out Transform lockTransform))
                lockToggle = lockTransform.GetComponent<Toggle>();

            eventTrigger = gameObject.GetComponent<EventTrigger>();

            if (!eventTrigger && gameObject.transform.childCount > 0)
                eventTrigger = gameObject.transform.GetChild(0).GetComponent<EventTrigger>();
        }
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
