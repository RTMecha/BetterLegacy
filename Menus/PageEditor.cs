using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using InControl;
using SimpleJSON;
using LSFunctions;
using TMPro;

using Element = InterfaceController.InterfaceElement;
using ButtonSetting = InterfaceController.ButtonSetting;
using Branch = InterfaceController.InterfaceBranch;

using ElementType = InterfaceController.InterfaceElement.Type;
using ButtonType = InterfaceController.ButtonSetting.Type;
using BranchType = InterfaceController.InterfaceBranch.Type;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core;

namespace BetterLegacy.Menus
{
    public class PageEditor : MonoBehaviour
    {
        public static PageEditor inst;

		EditMode editMode;
		public enum EditMode
        {
			Interface,
			QuickElement
        }

		bool loadingEditor = false;

		GameObject menuUI;
		GameObject interfaceContent;

		GameObject unselectedDialog;
		List<GameObject> editors = new List<GameObject>();

		Color offWhite = new Color(0.8679f, 0.86f, 0.9f, 1f);
		Color offBlack = new Color(0.12f, 0.11f, 0.11f, 1f);

		Dictionary<string, bool> expandedBranches = new Dictionary<string, bool>();

		SelectionType selectionType;
		public enum SelectionType
        {
			Interface,
			Branch,
			Element,
			QuickElement
        }

		Element currentElementSelection;
		Branch currentBranchSelection;
		Interface currentInterfaceSelection;
		QuickElement currentQuickElementSelection;

		bool showData = false;

		string searchTerm;
		string SearchTerm
        {
			get
            {
				return searchTerm;
            }
			set
            {
				searchTerm = value;
				StartCoroutine(RefreshInterface());
			}
        }

		List<Interface> interfaces = new List<Interface>();

		List<Branch> defaultBranches = new List<Branch>
		{
			new Branch("empty")
		};

		List<Element> defaultElements = new List<Element>
		{
			new Element(ElementType.Text, "")
		};

		Dictionary<string, Interface> interfaceNames
        {
			get
            {
				var dictionary = new Dictionary<string, Interface>();

				foreach (var inter in interfaces)
					dictionary.Add(Path.GetFileName(inter.filePath).Replace(".lsm", ""), inter);

				return dictionary;
            }
        }

		public static void Init()
		{
			if (inst != null)
			{
				CoreHelper.LogWarning("PageEditor has already been initialized!");
				return;
			}

			CoreHelper.Log("Init() => PageEditor");
			var gameObject = new GameObject("PageEditor");
			gameObject.AddComponent<PageEditor>();
		}

        void Awake()
        {
            if (!inst)
                inst = this;
            else if (inst != this)
                Destroy(gameObject);

			StartCoroutine(CreateUI());
        }

        IEnumerator CreateUI()
        {
            yield return StartCoroutine(DeleteComponents());

			yield return StartCoroutine(GenerateUI());

			yield return StartCoroutine(LoadInterfaces());

			yield return StartCoroutine(RefreshInterface());

			LSHelpers.ShowCursor();

			yield break;
        }

		IEnumerator DeleteComponents()
		{
			Destroy(GameObject.Find("Interface"));
			Destroy(GameObject.Find("EventSystem").GetComponent<InControlInputModule>());
			Destroy(GameObject.Find("EventSystem").GetComponent<BaseInput>());
			GameObject.Find("EventSystem").AddComponent<StandaloneInputModule>();
			Destroy(GameObject.Find("Main Camera").GetComponent<InterfaceLoader>());
			Destroy(GameObject.Find("Main Camera").GetComponent<ArcadeController>());
			Destroy(GameObject.Find("Main Camera").GetComponent<FlareLayer>());
			Destroy(GameObject.Find("Main Camera").GetComponent<GUILayer>());
			yield break;
		}

		IEnumerator GenerateUI()
		{
			var inter = new GameObject("Interface");
			inter.transform.localScale = Vector3.one * CoreHelper.ScreenScale;
			menuUI = inter;
			var interfaceRT = inter.AddComponent<RectTransform>();
			interfaceRT.anchoredPosition = new Vector2(960f, 540f);
			interfaceRT.sizeDelta = new Vector2(1920f, 1080f);
			interfaceRT.pivot = new Vector2(0.5f, 0.5f);
			interfaceRT.anchorMin = Vector2.zero;
			interfaceRT.anchorMax = Vector2.zero;

			var canvas = inter.AddComponent<Canvas>();
			canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;
			canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1;
			canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.Tangent;
			canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.Normal;
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.scaleFactor = CoreHelper.ScreenScale;

			var canvasScaler = inter.AddComponent<CanvasScaler>();
			canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			canvasScaler.referenceResolution = new Vector2(Screen.width, Screen.height);

			inter.AddComponent<GraphicRaycaster>();

			var openFilePopup = UIManager.GenerateUIImage("File List", inter.transform);
			var parent = ((GameObject)openFilePopup["GameObject"]).transform;

			var openFilePopupRT = (RectTransform)openFilePopup["RectTransform"];
			var zeroFive = new Vector2(0.5f, 0.5f);
			UIManager.SetRectTransform(openFilePopupRT, new Vector2(-650f, 0f), zeroFive, zeroFive, zeroFive, new Vector2(500f, 600f));

			((Image)openFilePopup["Image"]).color = new Color(0.1f, 0.1f, 0.1f, 1f);

			//Search
			{
				var search = UIManager.GenerateUIInputField("search", parent);
				UIManager.SetRectTransform((RectTransform)search["RectTransform"], new Vector2(16f, 316f), zeroFive, zeroFive, zeroFive, new Vector2(532.5f, 32f));
				if (search.ContainsKey("Image"))
					((Image)search["Image"]).color = new Color(0.1568f, 0.1568f, 0.1568f);

				var placeholder = (Text)search["Placeholder"];
				placeholder.text = "Search for branch...";
				placeholder.color = new Color(1f, 1f, 1f, 0.1653f);
				placeholder.fontStyle = FontStyle.Italic;

				var searchIF = (InputField)search["InputField"];
				searchIF.onValueChanged.AddListener(delegate (string _val)
				{
					SearchTerm = _val;
				});
			}

			//ScrollView
			{
				var scrollRect = ((GameObject)openFilePopup["GameObject"]).AddComponent<ScrollRect>();

				var mask = UIManager.GenerateUIImage("mask", parent);
				var maskMask = ((GameObject)mask["GameObject"]).AddComponent<Mask>();
				var maskRT = (RectTransform)mask["RectTransform"];
				UIManager.SetRectTransform(maskRT, new Vector2(0f, -16f), Vector2.one, Vector2.zero, zeroFive, new Vector2(0f, 32f));
				maskMask.showMaskGraphic = false;

				var content = new GameObject("content");
				content.transform.SetParent(((GameObject)mask["GameObject"]).transform);
				content.transform.localScale = Vector3.one;
				content.layer = 5;
				var contentRT = content.AddComponent<RectTransform>();
				UIManager.SetRectTransform(contentRT, new Vector2(0f, 32f), Vector2.up, Vector2.up, Vector2.up, new Vector2(1000f, 4276f));

				interfaceContent = content;

				var csf = content.AddComponent<ContentSizeFitter>();
				csf.horizontalFit = ContentSizeFitter.FitMode.MinSize;
				csf.verticalFit = ContentSizeFitter.FitMode.MinSize;

				var glg = content.AddComponent<GridLayoutGroup>();
				glg.cellSize = new Vector2(984f, 32f);
				glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
				glg.constraintCount = 1;
				glg.spacing = new Vector2(0, 8);
				glg.startAxis = GridLayoutGroup.Axis.Vertical;
				glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
				glg.childAlignment = TextAnchor.UpperLeft;

				var scrollbar = UIManager.GenerateUIImage("Scrollbar", parent);
				((Image)scrollbar["Image"]).color = new Color(0.1216f, 0.1216f, 0.1216f, 1f);
				var scrollbarRT = (RectTransform)scrollbar["RectTransform"];
				UIManager.SetRectTransform(scrollbarRT, Vector2.zero, Vector2.one, Vector2.right, new Vector2(0f, 0.5f), new Vector2(32f, 0f));

				var ssbar = ((GameObject)scrollbar["GameObject"]).AddComponent<Scrollbar>();

				var slidingArea = new GameObject("Sliding Area");
				slidingArea.transform.SetParent(((GameObject)scrollbar["GameObject"]).transform);
				slidingArea.transform.localScale = Vector3.one;
				UIManager.SetRectTransform(slidingArea.AddComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, zeroFive, new Vector2(-20f, -20f));

				var handle = UIManager.GenerateUIImage("Handle", slidingArea.transform);
				var handleRT = (RectTransform)handle["RectTransform"];
				UIManager.SetRectTransform(handleRT, Vector2.zero, Vector2.one, Vector2.zero, zeroFive, new Vector2(20f, 20f));
				((Image)handle["Image"]).color = Color.white;

				scrollRect.content = contentRT;
				scrollRect.horizontal = false;
				scrollRect.scrollSensitivity = 20f;
				scrollRect.verticalScrollbar = ssbar;

				ssbar.direction = Scrollbar.Direction.BottomToTop;
				ssbar.numberOfSteps = 0;
				ssbar.handleRect = handleRT;
			}

			//Back
			{
				var exit = UIManager.GenerateUIButton("exit", menuUI.transform);

				var playButton = (GameObject)exit["GameObject"];
				playButton.transform.SetParent(menuUI.transform);
				playButton.transform.localScale = Vector3.one;
				playButton.name = "back";

				var textit = UIManager.GenerateUITextMeshPro("return", playButton.transform);
				((GameObject)textit["GameObject"]).transform.localScale = Vector3.one;

				var play = (TextMeshProUGUI)textit["Text"];
				play.text = "[RETURN]";
				play.fontSize = 20;
				play.alignment = TextAlignmentOptions.Center;
				play.color = offWhite;

				var playRT = (RectTransform)exit["RectTransform"];
				playRT.pivot = new Vector2(0.5f, 0.5f);
				playRT.anchoredPosition = new Vector2(-840f, 420f);
				playRT.sizeDelta = new Vector2(196f, 64f);

				var playButtButt = (Button)exit["Button"];
				playButtButt.onClick.RemoveAllListeners();
				playButtButt.onClick.AddListener(delegate ()
				{
					Exit();
				});

				playButtButt.colors = UIManager.SetColorBlock(playButtButt.colors, new Color(0.1f, 0.05f, 0.12f), new Color(0.3f, 0.2f, 0.35f), new Color(0.1f, 0.05f, 0.12f), new Color(0.1f, 0.05f, 0.12f), Color.red);
			}

			//Save
			{
				var exit = UIManager.GenerateUIButton("save", menuUI.transform);

				var playButton = (GameObject)exit["GameObject"];
				playButton.transform.SetParent(menuUI.transform);
				playButton.transform.localScale = Vector3.one;
				playButton.name = "save";

				var textit = UIManager.GenerateUITextMeshPro("return", playButton.transform);
				((GameObject)textit["GameObject"]).transform.localScale = Vector3.one;

				var play = (TextMeshProUGUI)textit["Text"];
				play.text = "[SAVE]";
				play.fontSize = 20;
				play.alignment = TextAlignmentOptions.Center;
				play.color = offWhite;

				var playRT = (RectTransform)exit["RectTransform"];
				playRT.pivot = new Vector2(0.5f, 0.5f);
				playRT.anchoredPosition = new Vector2(-640f, 420f);
				playRT.sizeDelta = new Vector2(196f, 64f);

				var playButtButt = (Button)exit["Button"];
				playButtButt.onClick.RemoveAllListeners();
				playButtButt.onClick.AddListener(delegate ()
				{
					StartCoroutine(Save());
					QuickElementManager.SaveExternalQuickElements();
				});

				playButtButt.colors = UIManager.SetColorBlock(playButtButt.colors, new Color(0.1f, 0.3f, 0.4f), new Color(0.2f, 0.4f, 0.5f), new Color(0.1f, 0.3f, 0.4f), new Color(0.1f, 0.3f, 0.4f), Color.red);
			}

			//New
			{
				var exit = UIManager.GenerateUIButton("new", menuUI.transform);

				var playButton = (GameObject)exit["GameObject"];
				playButton.transform.SetParent(menuUI.transform);
				playButton.transform.localScale = Vector3.one;
				playButton.name = "new";

				var textit = UIManager.GenerateUITextMeshPro("return", playButton.transform);
				((GameObject)textit["GameObject"]).transform.localScale = Vector3.one;

				var play = (TextMeshProUGUI)textit["Text"];
				play.text = "[NEW]";
				play.fontSize = 20;
				play.alignment = TextAlignmentOptions.Center;
				play.color = offWhite;

				var playRT = (RectTransform)exit["RectTransform"];
				playRT.pivot = new Vector2(0.5f, 0.5f);
				playRT.anchoredPosition = new Vector2(-440f, 420f);
				playRT.sizeDelta = new Vector2(196f, 64f);

				var playButtButt = (Button)exit["Button"];
				playButtButt.onClick.RemoveAllListeners();
				playButtButt.onClick.AddListener(delegate ()
                {
					if (editMode == EditMode.Interface)
					{
						string s = "new_interface";
						int num = 1;
						while (interfaces.Find(x => x.FileName == s) != null)
						{
							s = "new_interface_" + num.ToString();
							num++;
						}

						var inter = new Interface(RTFile.ApplicationDirectory + "beatmaps/menus/" + s + ".lsm", new List<Branch>
						{
							new Branch("alpha")
						});

						interfaces.Add(inter);

						if (!expandedBranches.ContainsKey(inter.filePath + " - alpha"))
							expandedBranches.Add(inter.filePath + " - alpha", false);

						StartCoroutine(Save(inter));
					}
					if (editMode == EditMode.QuickElement)
                    {
						string s = "new_quickelement";
						int num = 1;
						while (QuickElementManager.AllQuickElements.Values.ToList().Find(x => x.name == s) != null)
                        {
							s = "new_quickelement_" + num.ToString();
							num++;
                        }

						QuickElementManager.CreateNewQuickElement(s);
					}

					StartCoroutine(RefreshInterface());
				});

				playButtButt.colors = UIManager.SetColorBlock(playButtButt.colors, new Color(0.3f, 0.1f, 0.4f), new Color(0.4f, 0.2f, 0.5f), new Color(0.3f, 01f, 0.4f), new Color(0.3f, 0.1f, 0.4f), Color.red);
			}

            //Editors
            {
				var backer = UIManager.GenerateUIImage("editor", inter.transform);
				var backerObject = (GameObject)backer["GameObject"];
				backerObject.transform.localScale = Vector3.one;

				UIManager.SetRectTransform((RectTransform)backer["RectTransform"], new Vector2(300f, 0f), zeroFive, zeroFive, zeroFive, new Vector2(1300f, 1000f));
				((Image)backer["Image"]).color = new Color(0.1f, 0.1f, 0.1f, 1f);

				//Unselected
                {
					var unselected = UIManager.GenerateUITextMeshPro("text", backerObject.transform);
					unselectedDialog = (GameObject)unselected["GameObject"];

					var tmp = (TextMeshProUGUI)unselected["Text"];
					tmp.text = "Select an item to edit.";
					tmp.color = offWhite;

					UIManager.SetRectTransform((RectTransform)unselected["RectTransform"], Vector2.zero, zeroFive, zeroFive, zeroFive, new Vector2(200f, 200f));
				}

				//Interface Editor
				{
					var b = new GameObject("Interface Editor");
					b.transform.SetParent(backerObject.transform);
					b.transform.localScale = Vector3.one;
					var brt = b.AddComponent<RectTransform>();
					brt.anchoredPosition = new Vector2(0f, 480f);
					editors.Add(b);

					var interfaceBar = UIManager.GenerateUIImage("bar", b.transform);
					var interfaceBarObject = (GameObject)interfaceBar["GameObject"];

					UIManager.SetRectTransform((RectTransform)interfaceBar["RectTransform"], Vector2.zero, zeroFive, zeroFive, zeroFive, new Vector2(1300f, 32f));

					((Image)interfaceBar["Image"]).color = new Color(0.1132f, 0.5291f, 1f, 1f);

					var interfaceTitle = UIManager.GenerateUITextMeshPro("text", interfaceBarObject.transform);

					var tmp = (TextMeshProUGUI)interfaceTitle["Text"];
					tmp.text = "- Interface Editor -";
					tmp.color = offBlack;
					tmp.alignment = TextAlignmentOptions.Center;

					UIManager.SetRectTransform((RectTransform)interfaceTitle["RectTransform"], Vector2.zero, zeroFive, zeroFive, zeroFive, new Vector2(200f, 200f));

					var vet = new GameObject("vet");
					vet.transform.SetParent(b.transform);
					vet.transform.localScale = Vector3.one;

					var vetRT = vet.AddComponent<RectTransform>();
					vetRT.anchoredPosition = new Vector2(-600f, -150f);

					var vetLayout = vet.AddComponent<GridLayoutGroup>();
					vetLayout.cellSize = new Vector2(350f, 32f);

					//var vetLayout = vet.AddComponent<VerticalLayoutGroup>();
					//UIManager.SetLayoutGroup(vetLayout, true, false, true, false);

					//Times Label
					{
						var times = new GameObject("times label");
						times.transform.SetParent(vet.transform);
						times.transform.localScale = Vector3.one;

						var timesRT = times.AddComponent<RectTransform>();
						timesRT.sizeDelta = new Vector2(350f, 0f);

						var timesLayout = times.AddComponent<HorizontalLayoutGroup>();
						UIManager.SetLayoutGroup(timesLayout, false, true, false, true);
						timesLayout.childAlignment = TextAnchor.MiddleLeft;

						var timesMin = UIManager.GenerateUITextMeshPro("min", times.transform);
						var timesMax = UIManager.GenerateUITextMeshPro("max", times.transform);

						((RectTransform)timesMin["RectTransform"]).sizeDelta = new Vector2(350f, 0f);
						((RectTransform)timesMax["RectTransform"]).sizeDelta = new Vector2(350f, 0f);

						var timesMinText = (TextMeshProUGUI)timesMin["Text"];
						var timesMaxText = (TextMeshProUGUI)timesMax["Text"];

						timesMinText.alignment = TextAlignmentOptions.Left;
						timesMaxText.alignment = TextAlignmentOptions.Left;

						timesMinText.text = "Times Min";
						timesMaxText.text = "Times Max";
						timesMinText.color = offWhite;
						timesMaxText.color = offWhite;
					}

					//Times
					{
						var times = new GameObject("times");
						times.transform.SetParent(vet.transform);
						times.transform.localScale = Vector3.one;

						var timesRT = times.AddComponent<RectTransform>();
						timesRT.pivot = new Vector2(0.5f, 0f);

						var timesLayout = times.AddComponent<HorizontalLayoutGroup>();
						UIManager.SetLayoutGroup(timesLayout, false, true, false, true);
						timesLayout.spacing = 8f;
						timesLayout.childAlignment = TextAnchor.MiddleLeft;

						var timesMin = UIManager.GenerateUIInputField("min", times.transform);
						var timesMax = UIManager.GenerateUIInputField("max", times.transform);

						((RectTransform)timesMin["RectTransform"]).sizeDelta = new Vector2(171f, 38f);
						((RectTransform)timesMax["RectTransform"]).sizeDelta = new Vector2(171f, 38f);

						var timesMinIF = (InputField)timesMin["InputField"];
						var timesMaxIF = (InputField)timesMax["InputField"];

						timesMinIF.textComponent.color = offBlack;
						timesMinIF.textComponent.alignment = TextAnchor.MiddleLeft;
						((Text)timesMinIF.placeholder).text = "Set minimum time...";
						((Text)timesMinIF.placeholder).alignment = TextAnchor.MiddleLeft;
						timesMaxIF.textComponent.color = offBlack;
						timesMaxIF.textComponent.alignment = TextAnchor.MiddleLeft;
						((Text)timesMaxIF.placeholder).text = "Set maximum time...";
						((Text)timesMinIF.placeholder).alignment = TextAnchor.MiddleLeft;
						timesMinIF.placeholder.color = new Color(0.1f, 0.1f, 0.1f, 0.2f);
						timesMaxIF.placeholder.color = new Color(0.1f, 0.1f, 0.1f, 0.2f);

						timesMinIF.onValueChanged.AddListener(delegate (string _val)
						{
							if (!loadingEditor && currentInterfaceSelection != null && float.TryParse(_val, out float num))
                            {
								CoreHelper.Log($"Setting Interface Times Min: {num}");
								currentInterfaceSelection.settings.times.x = num;
                            }
						});

						timesMaxIF.onValueChanged.AddListener(delegate (string _val)
						{
							if (!loadingEditor && currentInterfaceSelection != null && float.TryParse(_val, out float num))
							{
								CoreHelper.Log($"Setting Interface Times Max: {num}");
								currentInterfaceSelection.settings.times.y = num;
                            }
						});
					}

					//Initial Branch Label
					{
						var times = new GameObject("initial branch label");
						times.transform.SetParent(vet.transform);
						times.transform.localScale = Vector3.one;

						var timesRT = times.AddComponent<RectTransform>();
						timesRT.sizeDelta = new Vector2(350f, 0f);

						var timesLayout = times.AddComponent<HorizontalLayoutGroup>();
						UIManager.SetLayoutGroup(timesLayout, false, true, false, true);
						timesLayout.childAlignment = TextAnchor.MiddleLeft;

						var timesMin = UIManager.GenerateUITextMeshPro("label", times.transform);

						((RectTransform)timesMin["RectTransform"]).sizeDelta = new Vector2(350f, 0f);
						var timesMinText = (TextMeshProUGUI)timesMin["Text"];

						timesMinText.alignment = TextAlignmentOptions.Left;
						timesMinText.text = "Initial Branch";
						timesMinText.color = offWhite;
					}

					//Initial Branch
					{
						var times = new GameObject("initial branch");
						times.transform.SetParent(vet.transform);
						times.transform.localScale = Vector3.one;

						var timesRT = times.AddComponent<RectTransform>();

						var timesLayout = times.AddComponent<HorizontalLayoutGroup>();
						UIManager.SetLayoutGroup(timesLayout, false, true, false, true);
						timesLayout.spacing = 8f;
						timesLayout.childAlignment = TextAnchor.MiddleLeft;

						var dd = UIManager.GenerateUIDropdown("dropdown", times.transform);
						((RectTransform)dd["RectTransform"]).sizeDelta = new Vector2(350f, 32f);

						var dropdown = (Dropdown)dd["Dropdown"];
						dropdown.options = new List<Dropdown.OptionData>();

						dropdown.onValueChanged.AddListener(delegate (int _val)
						{
							if (!loadingEditor && currentBranchSelection != null)
							{
								currentInterfaceSelection.settings.initialBranch = currentInterfaceSelection.branches[_val].name;
							}
						});
					}

					b.SetActive(false);
				}

				//Branch Editor
				{
					var b = new GameObject("Branch Editor");
					b.transform.SetParent(backerObject.transform);
					b.transform.localScale = Vector3.one;
					var brt = b.AddComponent<RectTransform>();
					brt.anchoredPosition = new Vector2(0f, 480f);
					editors.Add(b);

					var interfaceBar = UIManager.GenerateUIImage("bar", b.transform);
					var interfaceBarObject = (GameObject)interfaceBar["GameObject"];

					UIManager.SetRectTransform((RectTransform)interfaceBar["RectTransform"], Vector2.zero, zeroFive, zeroFive, zeroFive, new Vector2(1300f, 32f));

					((Image)interfaceBar["Image"]).color = new Color(0.9332f, 0.6091f, 0.1887f, 1f);

					var interfaceTitle = UIManager.GenerateUITextMeshPro("text", interfaceBarObject.transform);

					var tmp = (TextMeshProUGUI)interfaceTitle["Text"];
					tmp.text = "- Branch Editor -";
					tmp.color = offBlack;
					tmp.alignment = TextAlignmentOptions.Center;

					UIManager.SetRectTransform((RectTransform)interfaceTitle["RectTransform"], Vector2.zero, zeroFive, zeroFive, zeroFive, new Vector2(200f, 200f));

					var vet = new GameObject("vet");
					vet.transform.SetParent(b.transform);
					vet.transform.localScale = Vector3.one;

					var vetRT = vet.AddComponent<RectTransform>();
					vetRT.anchoredPosition = new Vector2(-600f, -150f);

					var vetLayout = vet.AddComponent<GridLayoutGroup>();
					vetLayout.cellSize = new Vector2(350f, 32f);
					vetLayout.spacing = new Vector2(8f, 8f);

					//var vetLayout = vet.AddComponent<VerticalLayoutGroup>();
					//UIManager.SetLayoutGroup(vetLayout, true, false, true, false);

					//Name Label
					{
						var times = new GameObject("name label");
						times.transform.SetParent(vet.transform);
						times.transform.localScale = Vector3.one;

						var timesRT = times.AddComponent<RectTransform>();
						timesRT.sizeDelta = new Vector2(350f, 0f);

						var timesLayout = times.AddComponent<HorizontalLayoutGroup>();
						UIManager.SetLayoutGroup(timesLayout, false, true, false, true);
						timesLayout.childAlignment = TextAnchor.MiddleLeft;

						var timesMin = UIManager.GenerateUITextMeshPro("label", times.transform);
						
						((RectTransform)timesMin["RectTransform"]).sizeDelta = new Vector2(350f, 0f);
						var timesMinText = (TextMeshProUGUI)timesMin["Text"];

						timesMinText.alignment = TextAlignmentOptions.Left;
						timesMinText.text = "Name";
						timesMinText.color = offWhite;
					}

					//Name
					{
						var times = new GameObject("name");
						times.transform.SetParent(vet.transform);
						times.transform.localScale = Vector3.one;

						var timesRT = times.AddComponent<RectTransform>();

						var timesLayout = times.AddComponent<HorizontalLayoutGroup>();
						UIManager.SetLayoutGroup(timesLayout, false, true, false, true);
						timesLayout.spacing = 8f;
						timesLayout.childAlignment = TextAnchor.MiddleLeft;

						var timesMin = UIManager.GenerateUIInputField("name", times.transform);

						((RectTransform)timesMin["RectTransform"]).anchoredPosition = new Vector2(175f, 0f);
						((RectTransform)timesMin["RectTransform"]).sizeDelta = new Vector2(171f, 38f);

						var timesMinIF = (InputField)timesMin["InputField"];

						timesMinIF.textComponent.color = offBlack;
						timesMinIF.textComponent.alignment = TextAnchor.MiddleLeft;
						((Text)timesMinIF.placeholder).text = "Set name...";
						((Text)timesMinIF.placeholder).alignment = TextAnchor.MiddleLeft;
						timesMinIF.placeholder.color = new Color(0.1f, 0.1f, 0.1f, 0.2f);

						timesMinIF.onValueChanged.AddListener(delegate (string _val)
						{
							if (!loadingEditor && currentBranchSelection != null && currentInterfaceSelection != null)
							{
								CoreHelper.Log($"Setting Interface Branch Name: {_val}");

								var isExpanded = expandedBranches.ContainsKey(currentInterfaceSelection.filePath + " - " + currentBranchSelection.name) && expandedBranches[currentInterfaceSelection.filePath + " - " + currentBranchSelection.name];

								expandedBranches.Remove(currentInterfaceSelection.filePath + " - " + currentBranchSelection.name);

								currentBranchSelection.name = _val;

								expandedBranches.Add(currentInterfaceSelection.filePath + " - " + _val, isExpanded);

								StartCoroutine(RefreshInterface());
							}
						});
					}

					//Clear Screen Label
					{
						var times = new GameObject("clear screen label");
						times.transform.SetParent(vet.transform);
						times.transform.localScale = Vector3.one;

						var timesRT = times.AddComponent<RectTransform>();
						timesRT.sizeDelta = new Vector2(350f, 0f);

						var timesLayout = times.AddComponent<HorizontalLayoutGroup>();
						UIManager.SetLayoutGroup(timesLayout, false, true, false, true);
						timesLayout.childAlignment = TextAnchor.MiddleLeft;

						var timesMin = UIManager.GenerateUITextMeshPro("label", times.transform);

						((RectTransform)timesMin["RectTransform"]).sizeDelta = new Vector2(350f, 0f);
						var timesMinText = (TextMeshProUGUI)timesMin["Text"];

						timesMinText.alignment = TextAlignmentOptions.Left;
						timesMinText.text = "Clear Screen";
						timesMinText.color = offWhite;
					}

					//Clear Screen
					{
						var times = new GameObject("clear screen");
						times.transform.SetParent(vet.transform);
						times.transform.localScale = Vector3.one;

						var timesRT = times.AddComponent<RectTransform>();

						var timesLayout = times.AddComponent<HorizontalLayoutGroup>();
						UIManager.SetLayoutGroup(timesLayout, false, true, false, true);
						timesLayout.spacing = 8f;
						timesLayout.childAlignment = TextAnchor.MiddleLeft;

						var tmpFrick = UIManager.GenerateUITextMeshPro("text", times.transform);
						var tmpText = (TextMeshProUGUI)tmpFrick["Text"];
						tmpText.text = "Enabled";
						tmpText.color = offWhite;
						tmpText.alignment = TextAlignmentOptions.Left;

						var tog = UIManager.GenerateUIToggle("toggle", times.transform);
						((RectTransform)tog["BackgroundRT"]).sizeDelta = new Vector2(32f, 32f);
						((RectTransform)tog["CheckmarkRT"]).sizeDelta = new Vector2(32f, 32f);

						var toggle = (Toggle)tog["Toggle"];
						toggle.onValueChanged.AddListener(delegate (bool _val)
						{
							if (!loadingEditor && currentBranchSelection != null)
							{
								CoreHelper.Log($"Setting Interface Branch Clear Screen: {_val}");

								currentBranchSelection.clear_screen = _val;

								//StartCoroutine(RefreshInterface());
							}
						});
					}
					
					//Type Label
					{
						var times = new GameObject("type label");
						times.transform.SetParent(vet.transform);
						times.transform.localScale = Vector3.one;

						var timesRT = times.AddComponent<RectTransform>();
						timesRT.sizeDelta = new Vector2(350f, 0f);

						var timesLayout = times.AddComponent<HorizontalLayoutGroup>();
						UIManager.SetLayoutGroup(timesLayout, false, true, false, true);
						timesLayout.childAlignment = TextAnchor.MiddleLeft;

						var timesMin = UIManager.GenerateUITextMeshPro("label", times.transform);

						((RectTransform)timesMin["RectTransform"]).sizeDelta = new Vector2(350f, 0f);
						var timesMinText = (TextMeshProUGUI)timesMin["Text"];

						timesMinText.alignment = TextAlignmentOptions.Left;
						timesMinText.text = "Type";
						timesMinText.color = offWhite;
					}

					//Type
					{
						var times = new GameObject("type");
						times.transform.SetParent(vet.transform);
						times.transform.localScale = Vector3.one;

						var timesRT = times.AddComponent<RectTransform>();

						var timesLayout = times.AddComponent<HorizontalLayoutGroup>();
						UIManager.SetLayoutGroup(timesLayout, false, true, false, true);
						timesLayout.spacing = 8f;
						timesLayout.childAlignment = TextAnchor.MiddleLeft;

						var dd = UIManager.GenerateUIDropdown("dropdown", times.transform);
						((RectTransform)dd["RectTransform"]).sizeDelta = new Vector2(350f, 32f);

						var dropdown = (Dropdown)dd["Dropdown"];
						dropdown.options = new List<Dropdown.OptionData>
						{
							new Dropdown.OptionData("Normal"),
							new Dropdown.OptionData("Menu"),
							new Dropdown.OptionData("Main Menu"),
							new Dropdown.OptionData("Skippable")
						};

						dropdown.onValueChanged.AddListener(delegate (int _val)
						{
							if (!loadingEditor && currentBranchSelection != null)
                            {
								currentBranchSelection.type = (BranchType)_val;
                            }
						});
					}
					
					//Back Branch Label
					{
						var times = new GameObject("back branch label");
						times.transform.SetParent(vet.transform);
						times.transform.localScale = Vector3.one;

						var timesRT = times.AddComponent<RectTransform>();
						timesRT.sizeDelta = new Vector2(350f, 0f);

						var timesLayout = times.AddComponent<HorizontalLayoutGroup>();
						UIManager.SetLayoutGroup(timesLayout, false, true, false, true);
						timesLayout.childAlignment = TextAnchor.MiddleLeft;

						var timesMin = UIManager.GenerateUITextMeshPro("label", times.transform);

						((RectTransform)timesMin["RectTransform"]).sizeDelta = new Vector2(350f, 0f);
						var timesMinText = (TextMeshProUGUI)timesMin["Text"];

						timesMinText.alignment = TextAlignmentOptions.Left;
						timesMinText.text = "Back Branch";
						timesMinText.color = offWhite;
					}

					//Back Branch
					{
						var times = new GameObject("back branch");
						times.transform.SetParent(vet.transform);
						times.transform.localScale = Vector3.one;

						var timesRT = times.AddComponent<RectTransform>();

						var timesLayout = times.AddComponent<HorizontalLayoutGroup>();
						UIManager.SetLayoutGroup(timesLayout, false, true, false, true);
						timesLayout.spacing = 8f;
						timesLayout.childAlignment = TextAnchor.MiddleLeft;

						var dd = UIManager.GenerateUIDropdown("dropdown", times.transform);
						((RectTransform)dd["RectTransform"]).sizeDelta = new Vector2(350f, 32f);

						var dropdown = (Dropdown)dd["Dropdown"];
						dropdown.options = new List<Dropdown.OptionData>();

						dropdown.onValueChanged.AddListener(delegate (int _val)
						{
							if (!loadingEditor && currentBranchSelection != null)
                            {
								if (_val == 0)
									currentBranchSelection.BackBranch = "";
								else if (currentInterfaceSelection != null && currentInterfaceSelection.branches != null && currentInterfaceSelection.branches.Count > _val - 1)
									currentBranchSelection.BackBranch = currentInterfaceSelection.branches[_val - 1].name;
                            }
						});
					}

					b.SetActive(false);
				}

				//Element Editor
				{
					var b = new GameObject("Element Editor");
					b.transform.SetParent(backerObject.transform);
					b.transform.localScale = Vector3.one;
					var brt = b.AddComponent<RectTransform>();
					brt.anchoredPosition = new Vector2(0f, 480f);
					editors.Add(b);

					var interfaceBar = UIManager.GenerateUIImage("bar", b.transform);
					var interfaceBarObject = (GameObject)interfaceBar["GameObject"];

					UIManager.SetRectTransform((RectTransform)interfaceBar["RectTransform"], Vector2.zero, zeroFive, zeroFive, zeroFive, new Vector2(1300f, 32f));

					((Image)interfaceBar["Image"]).color = new Color(0.9332f, 0.2091f, 0.1887f, 1f);

					var interfaceTitle = UIManager.GenerateUITextMeshPro("text", interfaceBarObject.transform);

					var tmp = (TextMeshProUGUI)interfaceTitle["Text"];
					tmp.text = "- Element Editor -";
					tmp.color = offBlack;
					tmp.alignment = TextAlignmentOptions.Center;

					UIManager.SetRectTransform((RectTransform)interfaceTitle["RectTransform"], Vector2.zero, zeroFive, zeroFive, zeroFive, new Vector2(200f, 200f));

					var vet = new GameObject("vet");
					vet.transform.SetParent(b.transform);
					vet.transform.localScale = Vector3.one;

					var vetRT = vet.AddComponent<RectTransform>();
					vetRT.anchoredPosition = new Vector2(-600f, -150f);

					var vetLayout = vet.AddComponent<GridLayoutGroup>();
					vetLayout.cellSize = new Vector2(350f, 32f);
					vetLayout.spacing = new Vector2(8f, 8f);


					var yet = new GameObject("yet");
					yet.transform.SetParent(b.transform);
					yet.transform.localScale = Vector3.one;

					var yetRT = yet.AddComponent<RectTransform>();
					yetRT.anchoredPosition = new Vector2(-200f, -150f);

					var yetLayout = yet.AddComponent<GridLayoutGroup>();
					yetLayout.cellSize = new Vector2(350f, 32f);
					yetLayout.spacing = new Vector2(8f, 8f);


					var zet = new GameObject("zet");
					zet.transform.SetParent(b.transform);
					zet.transform.localScale = Vector3.one;

					var zetRT = zet.AddComponent<RectTransform>();
					zetRT.anchoredPosition = new Vector2(200f, -150f);

					var zetLayout = zet.AddComponent<GridLayoutGroup>();
					zetLayout.cellSize = new Vector2(350f, 32f);
					zetLayout.spacing = new Vector2(8f, 8f);

					//Data Label
					{
						var times = new GameObject("data label");
						times.transform.SetParent(vet.transform);
						times.transform.localScale = Vector3.one;

						var timesRT = times.AddComponent<RectTransform>();
						timesRT.sizeDelta = new Vector2(350f, 0f);

						var timesLayout = times.AddComponent<HorizontalLayoutGroup>();
						UIManager.SetLayoutGroup(timesLayout, false, true, false, true);
						timesLayout.childAlignment = TextAnchor.MiddleLeft;

						var timesMin = UIManager.GenerateUITextMeshPro("label", times.transform);

						((RectTransform)timesMin["RectTransform"]).sizeDelta = new Vector2(350f, 0f);
						var timesMinText = (TextMeshProUGUI)timesMin["Text"];

						timesMinText.alignment = TextAlignmentOptions.Left;
						timesMinText.text = "Data";
						timesMinText.color = offWhite;
					}

					//Add
					{
						var badd = UIManager.GenerateUIButton("add", vet.transform);

						var baddbutton = (Button)badd["Button"];
						baddbutton.onClick.AddListener(delegate ()
						{
							if (!loadingEditor && currentElementSelection != null)
							{
								currentElementSelection.data.Add("text");
								Select(SelectionType.Element);
							}
						});

						((Image)badd["Image"]).color = new Color(0.2f, 0.3f, 0.8f);

						var baddtext = UIManager.GenerateUITextMeshPro("text", ((GameObject)badd["GameObject"]).transform);
						var badTmpText = (TextMeshProUGUI)baddtext["Text"];
						badTmpText.text = "Add";
						badTmpText.color = offBlack;
						badTmpText.alignment = TextAlignmentOptions.Center;
					}

					//Data
					{
						var data = new GameObject("data");
						data.transform.SetParent(vet.transform);
						data.transform.localScale = Vector3.one;

						var dataRT = data.AddComponent<RectTransform>();
						dataRT.anchoredPosition = new Vector2(0f, 0f);

						var dataLayout = data.AddComponent<GridLayoutGroup>();
						dataLayout.cellSize = new Vector2(350f, 32f);
						dataLayout.spacing = new Vector2(8f, 8f);
					}

					//Settings Label
					{
						var times = new GameObject("settings label");
						times.transform.SetParent(yet.transform);
						times.transform.localScale = Vector3.one;

						var timesRT = times.AddComponent<RectTransform>();
						timesRT.sizeDelta = new Vector2(350f, 0f);

						var timesLayout = times.AddComponent<HorizontalLayoutGroup>();
						UIManager.SetLayoutGroup(timesLayout, false, true, false, true);
						timesLayout.childAlignment = TextAnchor.MiddleLeft;

						var timesMin = UIManager.GenerateUITextMeshPro("label", times.transform);

						((RectTransform)timesMin["RectTransform"]).sizeDelta = new Vector2(350f, 0f);
						var timesMinText = (TextMeshProUGUI)timesMin["Text"];

						timesMinText.alignment = TextAlignmentOptions.Left;
						timesMinText.text = "Settings";
						timesMinText.color = offWhite;
					}

					//Settings Add
					{
						var badd = UIManager.GenerateUIButton("settings add", yet.transform);

						var baddbutton = (Button)badd["Button"];
						baddbutton.onClick.AddListener(delegate ()
						{
							if (!loadingEditor && currentElementSelection != null)
							{
								currentElementSelection.data.Add("text");

								if (!currentElementSelection.settings.ContainsKey("Setting " + currentElementSelection.settings.Count.ToString()))
									currentElementSelection.settings.Add("Setting " + currentElementSelection.settings.Count.ToString(), "");

								Select(SelectionType.Element);
							}
						});

						((Image)badd["Image"]).color = new Color(0.2f, 0.3f, 0.8f);

						var baddtext = UIManager.GenerateUITextMeshPro("text", ((GameObject)badd["GameObject"]).transform);
						var badTmpText = (TextMeshProUGUI)baddtext["Text"];
						badTmpText.text = "Add";
						badTmpText.color = offBlack;
						badTmpText.alignment = TextAlignmentOptions.Center;
					}

					//Settings
					{
						var data = new GameObject("settings");
						data.transform.SetParent(yet.transform);
						data.transform.localScale = Vector3.one;

						var dataRT = data.AddComponent<RectTransform>();
						dataRT.anchoredPosition = new Vector2(0f, 0f);

						var dataLayout = data.AddComponent<GridLayoutGroup>();
						dataLayout.cellSize = new Vector2(350f, 32f);
						dataLayout.spacing = new Vector2(8f, 8f);
					}
					
					//Type Label
					{
						var times = new GameObject("type label");
						times.transform.SetParent(zet.transform);
						times.transform.localScale = Vector3.one;

						var timesRT = times.AddComponent<RectTransform>();
						timesRT.sizeDelta = new Vector2(350f, 0f);

						var timesLayout = times.AddComponent<HorizontalLayoutGroup>();
						UIManager.SetLayoutGroup(timesLayout, false, true, false, true);
						timesLayout.childAlignment = TextAnchor.MiddleLeft;

						var timesMin = UIManager.GenerateUITextMeshPro("label", times.transform);

						((RectTransform)timesMin["RectTransform"]).sizeDelta = new Vector2(350f, 0f);
						var timesMinText = (TextMeshProUGUI)timesMin["Text"];

						timesMinText.alignment = TextAlignmentOptions.Left;
						timesMinText.text = "Type";
						timesMinText.color = offWhite;
					}

					//Type
					{
						var times = new GameObject("type");
						times.transform.SetParent(zet.transform);
						times.transform.localScale = Vector3.one;

						var timesRT = times.AddComponent<RectTransform>();

						var timesLayout = times.AddComponent<HorizontalLayoutGroup>();
						UIManager.SetLayoutGroup(timesLayout, false, true, false, true);
						timesLayout.spacing = 8f;
						timesLayout.childAlignment = TextAnchor.MiddleLeft;

						var dd = UIManager.GenerateUIDropdown("dropdown", times.transform);
						((RectTransform)dd["RectTransform"]).sizeDelta = new Vector2(350f, 32f);

						var dropdown = (Dropdown)dd["Dropdown"];
						dropdown.options = new List<Dropdown.OptionData>
						{
							new Dropdown.OptionData("Text"),
							new Dropdown.OptionData("Divider"),
							new Dropdown.OptionData("Buttons"),
							new Dropdown.OptionData("Media"),
							new Dropdown.OptionData("Event")
						};

						dropdown.onValueChanged.AddListener(delegate (int _val)
						{
							if (!loadingEditor && currentBranchSelection != null)
							{
								currentElementSelection.type = (ElementType)_val;
							}
						});
					}

					b.SetActive(false);
				}

				//QuickElement Editor
				{
					var b = new GameObject("QuickElement Editor");
					b.transform.SetParent(backerObject.transform);
					b.transform.localScale = Vector3.one;
					var brt = b.AddComponent<RectTransform>();
					brt.anchoredPosition = new Vector2(0f, 480f);
					editors.Add(b);

					var interfaceBar = UIManager.GenerateUIImage("bar", b.transform);
					var interfaceBarObject = (GameObject)interfaceBar["GameObject"];

					UIManager.SetRectTransform((RectTransform)interfaceBar["RectTransform"], Vector2.zero, zeroFive, zeroFive, zeroFive, new Vector2(1300f, 32f));

					((Image)interfaceBar["Image"]).color = new Color(0.9332f, 0.2091f, 0.1887f, 1f);

					var interfaceTitle = UIManager.GenerateUITextMeshPro("text", interfaceBarObject.transform);

					var tmp = (TextMeshProUGUI)interfaceTitle["Text"];
					tmp.text = "- QuickElement Editor -";
					tmp.color = offBlack;
					tmp.alignment = TextAlignmentOptions.Center;

					UIManager.SetRectTransform((RectTransform)interfaceTitle["RectTransform"], Vector2.zero, zeroFive, zeroFive, zeroFive, new Vector2(200f, 200f));

					var vet = new GameObject("vet");
					vet.transform.SetParent(b.transform);
					vet.transform.localScale = Vector3.one;

					var vetRT = vet.AddComponent<RectTransform>();
					vetRT.anchoredPosition = new Vector2(-600f, -150f);

					var vetLayout = vet.AddComponent<GridLayoutGroup>();
					vetLayout.cellSize = new Vector2(350f, 32f);
					vetLayout.spacing = new Vector2(8f, 8f);
					
					var yet = new GameObject("yet");
					yet.transform.SetParent(b.transform);
					yet.transform.localScale = Vector3.one;

					var yetRT = yet.AddComponent<RectTransform>();
					yetRT.anchoredPosition = new Vector2(-200f, -150f);

					var yetLayout = yet.AddComponent<GridLayoutGroup>();
					yetLayout.cellSize = new Vector2(350f, 32f);
					yetLayout.spacing = new Vector2(8f, 8f);

					//Settings Label
					{
						var times = new GameObject("settings label");
						times.transform.SetParent(vet.transform);
						times.transform.localScale = Vector3.one;

						var timesRT = times.AddComponent<RectTransform>();
						timesRT.sizeDelta = new Vector2(350f, 0f);

						var timesLayout = times.AddComponent<HorizontalLayoutGroup>();
						UIManager.SetLayoutGroup(timesLayout, false, true, false, true);
						timesLayout.childAlignment = TextAnchor.MiddleLeft;

						var timesMin = UIManager.GenerateUITextMeshPro("label", times.transform);

						((RectTransform)timesMin["RectTransform"]).sizeDelta = new Vector2(350f, 0f);
						var timesMinText = (TextMeshProUGUI)timesMin["Text"];

						timesMinText.alignment = TextAlignmentOptions.Left;
						timesMinText.text = "Keyframes";
						timesMinText.color = offWhite;
					}

					//Settings Add
					{
						var badd = UIManager.GenerateUIButton("settings add", vet.transform);

						var baddbutton = (Button)badd["Button"];
						baddbutton.onClick.AddListener(delegate ()
						{
							if (!loadingEditor && currentQuickElementSelection != null)
							{
								if (currentQuickElementSelection.keyframes == null)
									currentQuickElementSelection.keyframes = new List<QuickElement.Keyframe>();

								currentQuickElementSelection.keyframes.Add(new QuickElement.Keyframe
								{
									text = "text",
									time = 1f
								});

								Select(SelectionType.QuickElement);
							}
						});

						((Image)badd["Image"]).color = new Color(0.2f, 0.3f, 0.8f);

						var baddtext = UIManager.GenerateUITextMeshPro("text", ((GameObject)badd["GameObject"]).transform);
						var badTmpText = (TextMeshProUGUI)baddtext["Text"];
						badTmpText.text = "Add";
						badTmpText.color = offBlack;
						badTmpText.alignment = TextAlignmentOptions.Center;
					}

					//Settings
					{
						var data = new GameObject("settings");
						data.transform.SetParent(vet.transform);
						data.transform.localScale = Vector3.one;

						var dataRT = data.AddComponent<RectTransform>();
						dataRT.anchoredPosition = new Vector2(0f, 0f);

						var dataLayout = data.AddComponent<GridLayoutGroup>();
						dataLayout.cellSize = new Vector2(350f, 32f);
						dataLayout.spacing = new Vector2(8f, 8f);
					}
					
					//Settings Label
					{
						var times = new GameObject("settings label");
						times.transform.SetParent(yet.transform);
						times.transform.localScale = Vector3.one;

						var timesRT = times.AddComponent<RectTransform>();
						timesRT.sizeDelta = new Vector2(350f, 0f);

						var timesLayout = times.AddComponent<HorizontalLayoutGroup>();
						UIManager.SetLayoutGroup(timesLayout, false, true, false, true);
						timesLayout.childAlignment = TextAnchor.MiddleLeft;

						var timesMin = UIManager.GenerateUITextMeshPro("label", times.transform);

						((RectTransform)timesMin["RectTransform"]).sizeDelta = new Vector2(350f, 0f);
						var timesMinText = (TextMeshProUGUI)timesMin["Text"];

						timesMinText.alignment = TextAlignmentOptions.Left;
						timesMinText.text = "Effects";
						timesMinText.color = offWhite;
					}

					//Settings Add
					{
						var badd = UIManager.GenerateUIButton("settings add", yet.transform);

						var baddbutton = (Button)badd["Button"];
						baddbutton.onClick.AddListener(delegate ()
						{
							if (!loadingEditor && currentQuickElementSelection != null)
							{
								if (currentQuickElementSelection.effects == null)
									currentQuickElementSelection.effects = new List<QuickElement.Effect>();

								currentQuickElementSelection.effects.Add(new QuickElement.Effect
								{
									data = new List<string>
                                    {
										"loop"
                                    },
									name = "loop"
								});

								Select(SelectionType.QuickElement);
							}
						});

						((Image)badd["Image"]).color = new Color(0.2f, 0.3f, 0.8f);

						var baddtext = UIManager.GenerateUITextMeshPro("text", ((GameObject)badd["GameObject"]).transform);
						var badTmpText = (TextMeshProUGUI)baddtext["Text"];
						badTmpText.text = "Add";
						badTmpText.color = offBlack;
						badTmpText.alignment = TextAlignmentOptions.Center;
					}

					//Settings
					{
						var data = new GameObject("settings");
						data.transform.SetParent(yet.transform);
						data.transform.localScale = Vector3.one;

						var dataRT = data.AddComponent<RectTransform>();
						dataRT.anchoredPosition = new Vector2(0f, 0f);

						var dataLayout = data.AddComponent<GridLayoutGroup>();
						dataLayout.cellSize = new Vector2(350f, 32f);
						dataLayout.spacing = new Vector2(8f, 8f);
					}

					b.SetActive(false);
				}
			}

			//Interface
			{
				var exit = UIManager.GenerateUIButton("edit mode interface", menuUI.transform);

				var playButton = (GameObject)exit["GameObject"];
				playButton.transform.SetParent(menuUI.transform);
				playButton.transform.localScale = Vector3.one;

				var textit = UIManager.GenerateUITextMeshPro("return", playButton.transform);
				((GameObject)textit["GameObject"]).transform.localScale = Vector3.one;

				var play = (TextMeshProUGUI)textit["Text"];
				play.text = "[INTERFACE]";
				play.fontSize = 20;
				play.alignment = TextAlignmentOptions.Center;
				play.color = offBlack;

				var playRT = (RectTransform)exit["RectTransform"];
				playRT.pivot = new Vector2(0.5f, 0.5f);
				playRT.anchoredPosition = new Vector2(-760f, 350f);
				playRT.sizeDelta = new Vector2(196f, 32f);

				var playButtButt = (Button)exit["Button"];
				playButtButt.onClick.RemoveAllListeners();
				playButtButt.onClick.AddListener(delegate ()
				{
					editMode = EditMode.Interface;
					StartCoroutine(RefreshInterface());
				});

				playButtButt.colors = UIManager.SetColorBlock(playButtButt.colors, LSColors.HexToColor("F6AC1A"), LSColors.HexToColor("F4D281"), LSColors.HexToColor("F4D281"), LSColors.HexToColor("F4D281"), Color.red);
			}

			//QuickElement
			{
				var exit = UIManager.GenerateUIButton("edit mode quick element", menuUI.transform);

				var playButton = (GameObject)exit["GameObject"];
				playButton.transform.SetParent(menuUI.transform);
				playButton.transform.localScale = Vector3.one;

				var textit = UIManager.GenerateUITextMeshPro("text", playButton.transform);
				((GameObject)textit["GameObject"]).transform.localScale = Vector3.one;

				var play = (TextMeshProUGUI)textit["Text"];
				play.text = "[QUICKELEMENT]";
				play.fontSize = 20;
				play.alignment = TextAlignmentOptions.Center;
				play.color = offBlack;

				var playRT = (RectTransform)exit["RectTransform"];
				playRT.pivot = new Vector2(0.5f, 0.5f);
				playRT.anchoredPosition = new Vector2(-500f, 350f);
				playRT.sizeDelta = new Vector2(196f, 32f);

				var playButtButt = (Button)exit["Button"];
				playButtButt.onClick.RemoveAllListeners();
				playButtButt.onClick.AddListener(delegate ()
				{
					editMode = EditMode.QuickElement;
					StartCoroutine(RefreshInterface());
				});

				playButtButt.colors = UIManager.SetColorBlock(playButtButt.colors, LSColors.HexToColor("2FCBD6"), LSColors.HexToColor("7AD3CA"), LSColors.HexToColor("7AD3CA"), LSColors.HexToColor("7AD3CA"), Color.red);
			}

			yield break;
		}

        IEnumerator LoadInterfaces()
        {
            if (RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/menus"))
            {
                var files = Directory.GetFiles(RTFile.ApplicationDirectory + "beatmaps/menus", "*.lsm", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    var json = FileManager.inst.LoadJSONFileRaw(file);
                    var inter = new Interface(file.Replace("\\", "/"), new List<Branch>());

					ParseLilScript(inter, json);

                    interfaces.Add(inter);

					foreach (var branch in inter.branches)
                    {
						if (!expandedBranches.ContainsKey(inter.filePath + " - " + branch.name))
							expandedBranches.Add(inter.filePath + " - " + branch.name, false);
					}
                }
            }

            yield break;
        }

		IEnumerator RefreshInterface()
		{
			LSHelpers.DeleteChildren(interfaceContent.transform);

			if (editMode == EditMode.Interface)
			{
				foreach (var inter in interfaces)
				{
					var tmpInterface = inter;

					var baseButton = UIManager.GenerateUIButton(Path.GetFileName(inter.filePath), interfaceContent.transform);
					var baseButtonObject = (GameObject)baseButton["GameObject"];
					baseButtonObject.transform.localScale = Vector3.one;

					var baseText = UIManager.GenerateUITextMeshPro("text", baseButtonObject.transform);
					var tmp = (TextMeshProUGUI)baseText["Text"];
					tmp.transform.localScale = Vector3.one;

					string expandedInterface = " >";
					if (inter.expanded)
						expandedInterface = " <rotate=-90>>";

					tmp.text = Path.GetFileName(inter.filePath) + expandedInterface;
					tmp.color = offWhite;

					UIManager.SetRectTransform((RectTransform)baseText["RectTransform"], Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 0.5f), Vector2.zero);

					var baseButtonFunc = (Button)baseButton["Button"];
					baseButtonFunc.colors = UIManager.SetColorBlock(baseButtonFunc.colors, new Color(0.2f, 0.2f, 0.2f), new Color(0.4008f, 0.4008f, 0.4008f), new Color(0.4608f, 0.4608f, 0.4608f), new Color(0.2f, 0.2f, 0.2f), new Color(0.4f, 0.2f, 0.2f));
					baseButtonFunc.onClick.AddListener(delegate ()
					{
						CoreHelper.Log($"Clicked {Path.GetFileName(inter.filePath)}");
						inter.expanded = !inter.expanded;
						StartCoroutine(RefreshInterface());
					});

					// Add Branch
					{
						var editButton = UIManager.GenerateUIButton("add", baseButtonObject.transform);
						var editButtonObject = (GameObject)editButton["GameObject"];
						editButtonObject.transform.localScale = Vector3.one;

						((Image)editButton["Image"]).color = new Color(0.5321f, 0.4196f, 0.934f, 1f);

						UIManager.SetRectTransform((RectTransform)editButton["RectTransform"], new Vector2(-80f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f));

						var editButtonFunc = (Button)editButton["Button"];
						editButtonFunc.onClick.AddListener(delegate ()
						{
							Add(tmpInterface);
						});
						//editButtonFunc.colors = UIManager.SetColorBlock(editButtonFunc.colors, new Color(0.2f, 0.2f, 0.2f), new Color(0.4008f, 0.4008f, 0.4008f), new Color(0.4608f, 0.4608f, 0.4608f), new Color(0.2f, 0.2f, 0.2f), new Color(0.4f, 0.2f, 0.2f));

						var img = UIManager.GenerateUIImage("image", editButtonObject.transform);
						var imgObject = (GameObject)img["GameObject"];
						imgObject.transform.localScale = Vector3.one;

						UIManager.SetRectTransform((RectTransform)img["RectTransform"], Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f));

						var image = (Image)img["Image"];
						if (RTFile.FileExists(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/add.png"))
							image.sprite = SpriteManager.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/add.png");
					}

					// Edit
					{
						var editButton = UIManager.GenerateUIButton("edit", baseButtonObject.transform);
						var editButtonObject = (GameObject)editButton["GameObject"];
						editButtonObject.transform.localScale = Vector3.one;

						((Image)editButton["Image"]).color = new Color(0.12f, 0.12f, 0.12f, 1f);

						UIManager.SetRectTransform((RectTransform)editButton["RectTransform"], new Vector2(-45f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f));

						var editButtonFunc = (Button)editButton["Button"];
						editButtonFunc.onClick.AddListener(delegate ()
						{
							currentInterfaceSelection = inter;
							Select(SelectionType.Interface);
						});
						//editButtonFunc.colors = UIManager.SetColorBlock(editButtonFunc.colors, new Color(0.2f, 0.2f, 0.2f), new Color(0.4008f, 0.4008f, 0.4008f), new Color(0.4608f, 0.4608f, 0.4608f), new Color(0.2f, 0.2f, 0.2f), new Color(0.4f, 0.2f, 0.2f));

						var img = UIManager.GenerateUIImage("image", editButtonObject.transform);
						var imgObject = (GameObject)img["GameObject"];
						imgObject.transform.localScale = Vector3.one;

						UIManager.SetRectTransform((RectTransform)img["RectTransform"], Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f));

						var image = (Image)img["Image"];
						if (RTFile.FileExists(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_edit.png"))
							image.sprite = SpriteManager.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_edit.png");
					}

					// Delete
					{
						var editButton = UIManager.GenerateUIButton("delete", baseButtonObject.transform);
						var editButtonObject = (GameObject)editButton["GameObject"];
						editButtonObject.transform.localScale = Vector3.one;

						((Image)editButton["Image"]).color = new Color(0.934f, 0.4196f, 0.5321f, 1f);

						UIManager.SetRectTransform((RectTransform)editButton["RectTransform"], new Vector2(-10f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f));

						var editButtonFunc = (Button)editButton["Button"];
						editButtonFunc.onClick.AddListener(delegate ()
						{
							//currentInterfaceSelection = inter;
							//Select(SelectionType.Interface);

							Delete(tmpInterface);
						});
						//editButtonFunc.colors = UIManager.SetColorBlock(editButtonFunc.colors, new Color(0.2f, 0.2f, 0.2f), new Color(0.4008f, 0.4008f, 0.4008f), new Color(0.4608f, 0.4608f, 0.4608f), new Color(0.2f, 0.2f, 0.2f), new Color(0.4f, 0.2f, 0.2f));

						var img = UIManager.GenerateUIImage("image", editButtonObject.transform);
						var imgObject = (GameObject)img["GameObject"];
						imgObject.transform.localScale = Vector3.one;

						((Image)img["Image"]).color = offWhite;

						UIManager.SetRectTransform((RectTransform)img["RectTransform"], Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f));

						var image = (Image)img["Image"];
						if (RTFile.FileExists(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_delete.png"))
							image.sprite = SpriteManager.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_delete.png");
					}

					if (inter.expanded)
					{
						int br = 0;
						foreach (var branch in inter.branches)
						{
							var tmpBranch = branch;

							if (string.IsNullOrEmpty(searchTerm) || branch.name.ToLower().Contains(searchTerm.ToLower()))
							{
								var baseBranchButton = UIManager.GenerateUIButton(branch.name, interfaceContent.transform);
								var baseBranchButtonObject = (GameObject)baseBranchButton["GameObject"];
								baseBranchButtonObject.transform.localScale = Vector3.one;

								var baseBranchText = UIManager.GenerateUITextMeshPro("text", baseBranchButtonObject.transform);
								var tmpBranchT = (TextMeshProUGUI)baseBranchText["Text"];
								tmpBranchT.transform.localScale = Vector3.one;

								string expandedBranch = " >";
								if (expandedBranches.ContainsKey(inter.filePath + " - " + branch.name) && expandedBranches[inter.filePath + " - " + branch.name])
									expandedBranch = " <rotate=-90>>";

								tmpBranchT.text = "- " + branch.name + expandedBranch;
								tmpBranchT.color = offWhite;

								UIManager.SetRectTransform((RectTransform)baseBranchText["RectTransform"], Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 0.5f), Vector2.zero);

								var baseBranchButtonFunc = (Button)baseBranchButton["Button"];
								baseBranchButtonFunc.colors = UIManager.SetColorBlock(baseBranchButtonFunc.colors, new Color(0.2f, 0.2f, 0.2f), new Color(0.4008f, 0.4008f, 0.4008f), new Color(0.4608f, 0.4608f, 0.4608f), new Color(0.2f, 0.2f, 0.2f), new Color(0.4f, 0.2f, 0.2f));
								baseBranchButtonFunc.onClick.AddListener(delegate ()
								{
									CoreHelper.Log($"Clicked {branch.name}");

									if (expandedBranches.ContainsKey(inter.filePath + " - " + branch.name))
									{
										expandedBranches[inter.filePath + " - " + branch.name] = !expandedBranches[inter.filePath + " - " + branch.name];
										StartCoroutine(RefreshInterface());
									}
								});

								// Add Element
								{
									var editButton = UIManager.GenerateUIButton("add", baseBranchButtonObject.transform);
									var editButtonObject = (GameObject)editButton["GameObject"];
									editButtonObject.transform.localScale = Vector3.one;

									((Image)editButton["Image"]).color = new Color(0.5321f, 0.4196f, 0.934f, 1f);

									UIManager.SetRectTransform((RectTransform)editButton["RectTransform"], new Vector2(-80f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f));

									var editButtonFunc = (Button)editButton["Button"];
									editButtonFunc.onClick.AddListener(delegate ()
									{
										Add(tmpBranch);
									});
									//editButtonFunc.colors = UIManager.SetColorBlock(editButtonFunc.colors, new Color(0.2f, 0.2f, 0.2f), new Color(0.4008f, 0.4008f, 0.4008f), new Color(0.4608f, 0.4608f, 0.4608f), new Color(0.2f, 0.2f, 0.2f), new Color(0.4f, 0.2f, 0.2f));

									var img = UIManager.GenerateUIImage("image", editButtonObject.transform);
									var imgObject = (GameObject)img["GameObject"];
									imgObject.transform.localScale = Vector3.one;

									UIManager.SetRectTransform((RectTransform)img["RectTransform"], Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f));

									var image = (Image)img["Image"];
									if (RTFile.FileExists(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/add.png"))
										image.sprite = SpriteManager.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/add.png");
								}

								// Edit
								{
									var editButton = UIManager.GenerateUIButton("edit", baseBranchButtonObject.transform);
									var editButtonObject = (GameObject)editButton["GameObject"];
									editButtonObject.transform.localScale = Vector3.one;

									((Image)editButton["Image"]).color = new Color(0.12f, 0.12f, 0.12f, 1f);

									UIManager.SetRectTransform((RectTransform)editButton["RectTransform"], new Vector2(-45f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f));

									var editButtonFunc = (Button)editButton["Button"];
									editButtonFunc.onClick.AddListener(delegate ()
									{
										currentBranchSelection = branch;
										currentInterfaceSelection = tmpInterface;
										Select(SelectionType.Branch);
									});
									//editButtonFunc.colors = UIManager.SetColorBlock(editButtonFunc.colors, new Color(0.2f, 0.2f, 0.2f), new Color(0.4008f, 0.4008f, 0.4008f), new Color(0.4608f, 0.4608f, 0.4608f), new Color(0.2f, 0.2f, 0.2f), new Color(0.4f, 0.2f, 0.2f));

									var img = UIManager.GenerateUIImage("image", editButtonObject.transform);
									var imgObject = (GameObject)img["GameObject"];
									imgObject.transform.localScale = Vector3.one;

									UIManager.SetRectTransform((RectTransform)img["RectTransform"], Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f));

									var image = (Image)img["Image"];
									if (RTFile.FileExists(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_edit.png"))
										image.sprite = SpriteManager.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_edit.png");
								}

								// Delete
								{
									var editButton = UIManager.GenerateUIButton("delete", baseBranchButtonObject.transform);
									var editButtonObject = (GameObject)editButton["GameObject"];
									editButtonObject.transform.localScale = Vector3.one;

									((Image)editButton["Image"]).color = new Color(0.934f, 0.4196f, 0.5321f, 1f);

									UIManager.SetRectTransform((RectTransform)editButton["RectTransform"], new Vector2(-10f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f));

									var editButtonFunc = (Button)editButton["Button"];
									editButtonFunc.onClick.AddListener(delegate ()
									{
										//currentInterfaceSelection = inter;
										//Select(SelectionType.Interface);

										Delete(tmpInterface, tmpBranch);
									});
									//editButtonFunc.colors = UIManager.SetColorBlock(editButtonFunc.colors, new Color(0.2f, 0.2f, 0.2f), new Color(0.4008f, 0.4008f, 0.4008f), new Color(0.4608f, 0.4608f, 0.4608f), new Color(0.2f, 0.2f, 0.2f), new Color(0.4f, 0.2f, 0.2f));

									var img = UIManager.GenerateUIImage("image", editButtonObject.transform);
									var imgObject = (GameObject)img["GameObject"];
									imgObject.transform.localScale = Vector3.one;

									((Image)img["Image"]).color = offWhite;

									UIManager.SetRectTransform((RectTransform)img["RectTransform"], Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f));

									var image = (Image)img["Image"];
									if (RTFile.FileExists(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_delete.png"))
										image.sprite = SpriteManager.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_delete.png");
								}

								int num = 0;
								foreach (var element in branch.elements)
								{
									if (expandedBranches.ContainsKey(inter.filePath + " - " + branch.name) && expandedBranches[inter.filePath + " - " + branch.name])
									{
										var tmpElement = element;

										string data = "Element " + num.ToString();
										if (element.data != null && element.data.Count > 0 && showData)
											data = element.data[0];

										var baseElementButton = UIManager.GenerateUIButton(data, interfaceContent.transform);
										var baseElementButtonObject = (GameObject)baseElementButton["GameObject"];
										baseElementButtonObject.transform.localScale = Vector3.one;

										var baseElementText = UIManager.GenerateUITextMeshPro("text", baseElementButtonObject.transform);
										var tmpElementT = (TextMeshProUGUI)baseElementText["Text"];
										tmpElementT.transform.localScale = Vector3.one;

										tmpElementT.text = "-- " + data;
										tmpElementT.color = offWhite;

										UIManager.SetRectTransform((RectTransform)baseElementText["RectTransform"], Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 0.5f), Vector2.zero);

										var baseElementButtonFunc = (Button)baseElementButton["Button"];
										baseElementButtonFunc.colors = UIManager.SetColorBlock(baseElementButtonFunc.colors, new Color(0.2f, 0.2f, 0.2f), new Color(0.4008f, 0.4008f, 0.4008f), new Color(0.4608f, 0.4608f, 0.4608f), new Color(0.2f, 0.2f, 0.2f), new Color(0.4f, 0.2f, 0.2f));
										baseElementButtonFunc.onClick.AddListener(delegate ()
										{
											CoreHelper.Log($"Clicked {data}");

											currentBranchSelection = tmpBranch;
											currentInterfaceSelection = tmpInterface;
											currentElementSelection = element;
											Select(SelectionType.Element);
										});

										// Delete
										{
											var editButton = UIManager.GenerateUIButton("delete", baseElementButtonObject.transform);
											var editButtonObject = (GameObject)editButton["GameObject"];
											editButtonObject.transform.localScale = Vector3.one;

											((Image)editButton["Image"]).color = new Color(0.934f, 0.4196f, 0.5321f, 1f);

											UIManager.SetRectTransform((RectTransform)editButton["RectTransform"], new Vector2(-10f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f));

											var editButtonFunc = (Button)editButton["Button"];
											editButtonFunc.onClick.AddListener(delegate ()
											{
												//currentInterfaceSelection = inter;
												//Select(SelectionType.Interface);

												Delete(tmpBranch, tmpElement);
											});
											//editButtonFunc.colors = UIManager.SetColorBlock(editButtonFunc.colors, new Color(0.2f, 0.2f, 0.2f), new Color(0.4008f, 0.4008f, 0.4008f), new Color(0.4608f, 0.4608f, 0.4608f), new Color(0.2f, 0.2f, 0.2f), new Color(0.4f, 0.2f, 0.2f));

											var img = UIManager.GenerateUIImage("image", editButtonObject.transform);
											var imgObject = (GameObject)img["GameObject"];
											imgObject.transform.localScale = Vector3.one;

											((Image)img["Image"]).color = offWhite;

											UIManager.SetRectTransform((RectTransform)img["RectTransform"], Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f));

											var image = (Image)img["Image"];
											if (RTFile.FileExists(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_delete.png"))
												image.sprite = SpriteManager.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_delete.png");
										}
									}
									num++;
								}
							}
							br++;
						}
					}
				}
			}
			if (editMode == EditMode.QuickElement)
            {
				foreach (var quickElement in QuickElementManager.AllQuickElements)
				{
					string name = quickElement.Key;
					var qe = quickElement.Value;

					var baseButton = UIManager.GenerateUIButton(name, interfaceContent.transform);
					var baseButtonObject = (GameObject)baseButton["GameObject"];
					baseButtonObject.transform.localScale = Vector3.one;

					var baseText = UIManager.GenerateUITextMeshPro("text", baseButtonObject.transform);
					var tmp = (TextMeshProUGUI)baseText["Text"];
					tmp.transform.localScale = Vector3.one;

					tmp.text = name;
					tmp.color = offWhite;

					UIManager.SetRectTransform((RectTransform)baseText["RectTransform"], Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 0.5f), Vector2.zero);

					var baseButtonFunc = (Button)baseButton["Button"];
					baseButtonFunc.colors = UIManager.SetColorBlock(baseButtonFunc.colors, new Color(0.2f, 0.2f, 0.2f), new Color(0.4008f, 0.4008f, 0.4008f), new Color(0.4608f, 0.4608f, 0.4608f), new Color(0.2f, 0.2f, 0.2f), new Color(0.4f, 0.2f, 0.2f));

					if (!QuickElementManager.quickElements.ContainsKey(name))
					{
						baseButtonFunc.onClick.AddListener(delegate ()
						{
							CoreHelper.Log($"Clicked {name}");
							currentQuickElementSelection = qe;
							Select(SelectionType.QuickElement);
						});


						// Delete
						{
							var editButton = UIManager.GenerateUIButton("delete", baseButtonObject.transform);
							var editButtonObject = (GameObject)editButton["GameObject"];
							editButtonObject.transform.localScale = Vector3.one;

							((Image)editButton["Image"]).color = new Color(0.934f, 0.4196f, 0.5321f, 1f);

							UIManager.SetRectTransform((RectTransform)editButton["RectTransform"], new Vector2(-10f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f));

							var editButtonFunc = (Button)editButton["Button"];
							editButtonFunc.onClick.AddListener(delegate ()
							{
								string current = currentQuickElementSelection.name;

								QuickElementManager.customQuickElements.Remove(name);
								StartCoroutine(RefreshInterface());

								if (current == name)
									Select(SelectionType.QuickElement);
							});
							//editButtonFunc.colors = UIManager.SetColorBlock(editButtonFunc.colors, new Color(0.2f, 0.2f, 0.2f), new Color(0.4008f, 0.4008f, 0.4008f), new Color(0.4608f, 0.4608f, 0.4608f), new Color(0.2f, 0.2f, 0.2f), new Color(0.4f, 0.2f, 0.2f));

							var img = UIManager.GenerateUIImage("image", editButtonObject.transform);
							var imgObject = (GameObject)img["GameObject"];
							imgObject.transform.localScale = Vector3.one;

							((Image)img["Image"]).color = offWhite;

							UIManager.SetRectTransform((RectTransform)img["RectTransform"], Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f));

							var image = (Image)img["Image"];
							if (RTFile.FileExists(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_delete.png"))
								image.sprite = SpriteManager.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_delete.png");
						}
					}
					else
						baseButtonFunc.interactable = false;
				}
			}

			yield break;
		}

		void ParseLilScript(Interface inter, string _json, bool loops = false)
		{
			var jn = JSON.Parse(_json);

			if (jn["settings"] != null)
			{
				inter.settings.times = new Vector2(jn["settings"]["times"]["min"].AsFloat, jn["settings"]["times"]["max"].AsFloat);
				inter.settings.language = DataManager.inst.GetSettingInt("Language_i", 0);
				inter.settings.initialBranch = jn["settings"]["initial_branch"];
				if (jn["settings"]["text_color"] != null)
				{
					inter.settings.textColor = LSColors.HexToColor(jn["settings"]["text_color"]);
				}
				if (jn["settings"]["bg_color"] != null)
				{
					inter.settings.bgColor = LSColors.HexToColor(jn["settings"]["bg_color"]);
				}
				inter.settings.music = jn["settings"]["music"];
				inter.settings.returnBranch = jn["settings"]["return_branch"];
			}
			//StartCoroutine(handleEvent(null, "apply_ui_theme", true));
			for (int i = 0; i < jn["branches"].Count; i++)
			{
				JSONNode jnbranch = jn["branches"];
				inter.branches.Add(new Branch(jnbranch[i]["name"]));
				inter.branches[i].clear_screen = jnbranch[i]["settings"]["clear_screen"].AsBool;
				if (jnbranch[i]["settings"]["back_branch"] != null)
				{
					inter.branches[i].BackBranch = jnbranch[i]["settings"]["back_branch"];
				}
				else
				{
					inter.branches[i].BackBranch = "";
				}
				inter.branches[i].type = convertBranchToEnum(jnbranch[i]["settings"]["type"]);
				for (int j = 0; j < jnbranch[i]["elements"].Count; j++)
				{
					var type = ElementType.Text;
					Dictionary<string, string> dictionary = new Dictionary<string, string>();
					List<string> list = new List<string>();
					if (jnbranch[i]["elements"][j]["type"] != null)
					{
						type = convertElementToEnum(jnbranch[i]["elements"][j]["type"]);
					}
					if (jnbranch[i]["elements"][j]["settings"] != null)
					{
						foreach (JSONNode child in jnbranch[i]["elements"][j]["settings"].Children)
						{
							string[] array = ((string)child).Split(new char[1] { ':' }, 2);
							dictionary.Add(array[0], array[1]);
						}
					}

					int num = 1;
					if (dictionary.ContainsKey("loop") && loops)
					{
						num = int.Parse(dictionary["loop"]);
					}

					if (jnbranch[i]["elements"][j]["data"] != null)
						list.AddRange(jnbranch[i]["elements"][j]["data"].Children.Select(x => (string)x));
					else
						CoreHelper.Log($"Couldn't load data for branch [{i}] element [{j}]");

					if (dictionary.Count > 0)
					{
						for (int k = 0; k < num; k++)
						{
							inter.branches[i].elements.Add(new Element(jnbranch[i]["name"], type, dictionary, list));
						}
						continue;
					}

					for (int l = 0; l < num; l++)
					{
						inter.branches[i].elements.Add(new Element(jnbranch[i]["name"], type, list));
					}
				}
			}

			CoreHelper.Log($"Parsed interface with [{jn["branches"].Count}] branches");
		}

		void Select(SelectionType selectionType)
        {
			loadingEditor = true;

			this.selectionType = selectionType;

			int s = (int)selectionType;
			for (int i = 0; i < editors.Count; i++)
			{
				if (i == s) editors[i].SetActive(true);
				else editors[i].SetActive(false);
            }

            switch (selectionType)
            {
                case SelectionType.Interface:
                    {
                        if (currentInterfaceSelection != null)
                        {
                            unselectedDialog.SetActive(false);

                            var times = editors[0].transform.Find("vet/times");
                            var timesMin = times.Find("min").GetComponent<InputField>();
                            var timesMax = times.Find("max").GetComponent<InputField>();

							timesMin.text = currentInterfaceSelection.settings.times.x.ToString();
							timesMax.text = currentInterfaceSelection.settings.times.y.ToString();

							var initailBranch = editors[0].transform.Find("vet/initial branch/dropdown").GetComponent<Dropdown>();
							initailBranch.options = convertBranchesToOptions(currentInterfaceSelection, false);

							int index = currentInterfaceSelection.branches.FindIndex(x => x.name == currentInterfaceSelection.settings.initialBranch);
							if (index < 0)
								index = 0;

							initailBranch.value = index;

                            CoreHelper.Log($"Selected Interface: {Path.GetFileName(currentInterfaceSelection.filePath)}");
                        }
                        else
                        {
                            unselectedDialog.SetActive(true);
                        }
                        break;
                    }
                case SelectionType.Branch:
                    {
                        if (currentBranchSelection != null)
                        {
                            unselectedDialog.SetActive(false);

							var name = editors[1].transform.Find("vet/name/name").GetComponent<InputField>();
							name.text = currentBranchSelection.name;

							var toggle = editors[1].transform.Find("vet/clear screen/toggle").GetComponent<Toggle>();
							toggle.isOn = currentBranchSelection.clear_screen;

							var type = editors[1].transform.Find("vet/type/dropdown").GetComponent<Dropdown>();
							type.value = (int)currentBranchSelection.type;

							var back = editors[1].transform.Find("vet/back branch/dropdown").GetComponent<Dropdown>();
							if (currentInterfaceSelection != null)
							{
								back.options = convertBranchesToOptions(currentInterfaceSelection);
								back.value = GetBackBranchValue(currentBranchSelection);
							}

							CoreHelper.Log($"Selected Branch: {Path.GetFileName(currentBranchSelection.name)}");
						}
                        else
                        {
                            unselectedDialog.SetActive(true);
                        }
                        break;
                    }
                case SelectionType.Element:
                    {
                        if (currentElementSelection != null && currentElementSelection.data != null && currentElementSelection.data.Count > 0)
                        {
                            unselectedDialog.SetActive(false);

							LSHelpers.DeleteChildren(editors[2].transform.Find("vet/data"));
							LSHelpers.DeleteChildren(editors[2].transform.Find("yet/settings"));

							for (int i = 0; i < currentElementSelection.data.Count; i++)
                            {
								var data = currentElementSelection.data[i];

								var iTmp = i;

								//Name
								{
									var times = new GameObject("element " + iTmp.ToString());
									times.transform.SetParent(editors[2].transform.Find("vet/data"));
									times.transform.localScale = Vector3.one;

									var timesRT = times.AddComponent<RectTransform>();

									var timesLayout = times.AddComponent<HorizontalLayoutGroup>();
									UIManager.SetLayoutGroup(timesLayout, false, true, false, true);
									timesLayout.spacing = 8f;
									timesLayout.childAlignment = TextAnchor.MiddleLeft;

									var timesMin = UIManager.GenerateUIInputField("data", times.transform);

									((RectTransform)timesMin["RectTransform"]).anchoredPosition = new Vector2(175f, 0f);
									((RectTransform)timesMin["RectTransform"]).sizeDelta = new Vector2(171f, 38f);

									var timesMinIF = (InputField)timesMin["InputField"];

									timesMinIF.textComponent.color = offBlack;
									timesMinIF.textComponent.alignment = TextAnchor.MiddleLeft;
									((Text)timesMinIF.placeholder).text = "Set data...";
									((Text)timesMinIF.placeholder).alignment = TextAnchor.MiddleLeft;
									timesMinIF.placeholder.color = new Color(0.1f, 0.1f, 0.1f, 0.2f);

									timesMinIF.text = data;
									timesMinIF.onValueChanged.AddListener(delegate (string _val)
									{
										if (!loadingEditor && currentElementSelection != null)
										{
											CoreHelper.Log($"Setting Interface Branch Name: {_val}");

											currentElementSelection.data[iTmp] = _val;
										}
									});

									// Delete
									{
										var editButton = UIManager.GenerateUIButton("delete", times.transform);
										var editButtonObject = (GameObject)editButton["GameObject"];
										editButtonObject.transform.localScale = Vector3.one;

										var lay = editButtonObject.AddComponent<LayoutElement>();
										lay.preferredWidth = 32f;
										lay.ignoreLayout = true;

										((Image)editButton["Image"]).color = new Color(0.934f, 0.4196f, 0.5321f, 1f);

										UIManager.SetRectTransform((RectTransform)editButton["RectTransform"], new Vector2(190f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f));

										var editButtonFunc = (Button)editButton["Button"];
										editButtonFunc.onClick.AddListener(delegate ()
										{
											if (!loadingEditor && currentElementSelection != null)
											{
												CoreHelper.Log($"Deleting Interface Branch Element {currentElementSelection.data[iTmp]}");

												currentElementSelection.data.RemoveAt(iTmp);

												Select(selectionType);
											}
										});
										//editButtonFunc.colors = UIManager.SetColorBlock(editButtonFunc.colors, new Color(0.2f, 0.2f, 0.2f), new Color(0.4008f, 0.4008f, 0.4008f), new Color(0.4608f, 0.4608f, 0.4608f), new Color(0.2f, 0.2f, 0.2f), new Color(0.4f, 0.2f, 0.2f));

										var img = UIManager.GenerateUIImage("image", editButtonObject.transform);
										var imgObject = (GameObject)img["GameObject"];
										imgObject.transform.localScale = Vector3.one;

										((Image)img["Image"]).color = offWhite;

										UIManager.SetRectTransform((RectTransform)img["RectTransform"], Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f));

										var image = (Image)img["Image"];
										if (RTFile.FileExists(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_delete.png"))
											UIManager.GetImage(image, RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_delete.png");
									}
								}
							}

							for (int i = 0; i < currentElementSelection.settings.Count; i++)
							{
								var iTmp = i;

								//Name
								{
									var times = new GameObject("element " + iTmp.ToString());
									times.transform.SetParent(editors[2].transform.Find("yet/settings"));
									times.transform.localScale = Vector3.one;

									var timesRT = times.AddComponent<RectTransform>();
									timesRT.sizeDelta = new Vector2(350f, 32f);

									var timesLayout = times.AddComponent<HorizontalLayoutGroup>();
									UIManager.SetLayoutGroup(timesLayout, true, true, false, true);
									timesLayout.spacing = 8f;
									timesLayout.childAlignment = TextAnchor.MiddleLeft;

									var timesMin = UIManager.GenerateUIInputField("key", times.transform);

									var timesMinObject = (GameObject)timesMin["GameObject"];
									var timesMinLE = timesMinObject.AddComponent<LayoutElement>();
									timesMinLE.preferredHeight = 64f;

									//((RectTransform)timesMin["RectTransform"]).anchoredPosition = new Vector2(175f, 0f);
									((RectTransform)timesMin["RectTransform"]).sizeDelta = new Vector2(184f, 38f);

									var timesMinIF = (InputField)timesMin["InputField"];

									timesMinIF.textComponent.color = offBlack;
									timesMinIF.textComponent.alignment = TextAnchor.MiddleLeft;
									((Text)timesMinIF.placeholder).text = "Set setting...";
									((Text)timesMinIF.placeholder).alignment = TextAnchor.MiddleLeft;
									timesMinIF.placeholder.color = new Color(0.1f, 0.1f, 0.1f, 0.2f);

									timesMinIF.text = currentElementSelection.settings.ElementAt(i).Key;
									timesMinIF.onValueChanged.AddListener(delegate (string _val)
									{
										if (!loadingEditor && currentElementSelection != null)
										{
											CoreHelper.Log($"Setting Interface Branch {currentElementSelection.settings.ElementAt(iTmp).Key}: {_val}");

											var key = currentElementSelection.settings.ElementAt(iTmp).Key;
											var value = currentElementSelection.settings.ElementAt(iTmp).Value;

											currentElementSelection.settings.Remove(key);
											if (!currentElementSelection.settings.ContainsKey(_val))
												currentElementSelection.settings.Add(_val, value);

											Select(selectionType);

											//currentElementSelection.data[iTmp] = _val;
										}
									});

									var timesMax = UIManager.GenerateUIInputField("value", times.transform);

									var timesMaxObject = (GameObject)timesMax["GameObject"];
									var timesMaxLE = timesMaxObject.AddComponent<LayoutElement>();
									timesMaxLE.preferredHeight = 64f;

									//((RectTransform)timesMin["RectTransform"]).anchoredPosition = new Vector2(175f, 0f);
									((RectTransform)timesMin["RectTransform"]).sizeDelta = new Vector2(158f, 38f);

									var timesMaxIF = (InputField)timesMax["InputField"];

									timesMaxIF.textComponent.color = offBlack;
									timesMaxIF.textComponent.alignment = TextAnchor.MiddleLeft;
									((Text)timesMaxIF.placeholder).text = "Set setting...";
									((Text)timesMaxIF.placeholder).alignment = TextAnchor.MiddleLeft;
									timesMaxIF.placeholder.color = new Color(0.1f, 0.1f, 0.1f, 0.2f);

									timesMaxIF.text = currentElementSelection.settings.ElementAt(i).Value;
									timesMaxIF.onValueChanged.AddListener(delegate (string _val)
									{
										if (!loadingEditor && currentElementSelection != null)
										{
											CoreHelper.Log($"Setting Interface Branch {currentElementSelection.settings.ElementAt(iTmp).Key}: {_val}");

											var keyAt = currentElementSelection.settings.ElementAt(iTmp).Key;

											currentElementSelection.settings[keyAt] = _val;

											//currentElementSelection.data[iTmp] = _val;
										}
									});

									// Delete
									{
										var editButton = UIManager.GenerateUIButton("delete", times.transform);
										var editButtonObject = (GameObject)editButton["GameObject"];
										editButtonObject.transform.localScale = Vector3.one;

										var lay = editButtonObject.AddComponent<LayoutElement>();
										lay.preferredWidth = 32f;
										lay.ignoreLayout = true;

										((Image)editButton["Image"]).color = new Color(0.934f, 0.4196f, 0.5321f, 1f);

										UIManager.SetRectTransform((RectTransform)editButton["RectTransform"], new Vector2(190f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f));

										var editButtonFunc = (Button)editButton["Button"];
										editButtonFunc.onClick.AddListener(delegate ()
										{
											if (!loadingEditor && currentElementSelection != null)
											{
												CoreHelper.Log($"Deleting Interface Branch Element {currentElementSelection.settings.ElementAt(iTmp).Key}");

												var key = currentElementSelection.settings.ElementAt(iTmp).Key;

												currentElementSelection.settings.Remove(key);

												Select(selectionType);
											}
										});
										//editButtonFunc.colors = UIManager.SetColorBlock(editButtonFunc.colors, new Color(0.2f, 0.2f, 0.2f), new Color(0.4008f, 0.4008f, 0.4008f), new Color(0.4608f, 0.4608f, 0.4608f), new Color(0.2f, 0.2f, 0.2f), new Color(0.4f, 0.2f, 0.2f));

										var img = UIManager.GenerateUIImage("image", editButtonObject.transform);
										var imgObject = (GameObject)img["GameObject"];
										imgObject.transform.localScale = Vector3.one;

										((Image)img["Image"]).color = offWhite;

										UIManager.SetRectTransform((RectTransform)img["RectTransform"], Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f));

										var image = (Image)img["Image"];
										if (RTFile.FileExists(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_delete.png"))
											UIManager.GetImage(image, RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_delete.png");
									}
								}
							}

							var type = editors[2].transform.Find("zet/type/dropdown").GetComponent<Dropdown>();
							type.value = (int)currentElementSelection.type;

							CoreHelper.Log($"Selected Element: {currentElementSelection.data[0]}");
                        }
                        else
                        {
                            unselectedDialog.SetActive(true);
                        }
                        break;
                    }
				case SelectionType.QuickElement:
                    {
						if (currentQuickElementSelection != null)
						{
							unselectedDialog.SetActive(false);

							LSHelpers.DeleteChildren(editors[3].transform.Find("vet/settings"));
							LSHelpers.DeleteChildren(editors[3].transform.Find("yet/settings"));

							for (int i = 0; i < currentQuickElementSelection.keyframes.Count; i++)
							{
								var iTmp = i;

								//Name
								{
									var times = new GameObject("element " + iTmp.ToString());
									times.transform.SetParent(editors[3].transform.Find("vet/settings"));
									times.transform.localScale = Vector3.one;

									var timesRT = times.AddComponent<RectTransform>();
									timesRT.sizeDelta = new Vector2(350f, 32f);

									var timesLayout = times.AddComponent<HorizontalLayoutGroup>();
									UIManager.SetLayoutGroup(timesLayout, true, true, false, true);
									timesLayout.spacing = 8f;
									timesLayout.childAlignment = TextAnchor.MiddleLeft;

									var timesMin = UIManager.GenerateUIInputField("key", times.transform);

									var timesMinObject = (GameObject)timesMin["GameObject"];
									var timesMinLE = timesMinObject.AddComponent<LayoutElement>();
									timesMinLE.preferredHeight = 64f;

									//((RectTransform)timesMin["RectTransform"]).anchoredPosition = new Vector2(175f, 0f);
									((RectTransform)timesMin["RectTransform"]).sizeDelta = new Vector2(184f, 38f);

									var timesMinIF = (InputField)timesMin["InputField"];

									timesMinIF.textComponent.color = offBlack;
									timesMinIF.textComponent.alignment = TextAnchor.MiddleLeft;
									((Text)timesMinIF.placeholder).text = "Set text...";
									((Text)timesMinIF.placeholder).alignment = TextAnchor.MiddleLeft;
									timesMinIF.placeholder.color = new Color(0.1f, 0.1f, 0.1f, 0.2f);

									timesMinIF.text = currentQuickElementSelection.keyframes[i].text;
									timesMinIF.onValueChanged.AddListener(delegate (string _val)
									{
										if (!loadingEditor && currentQuickElementSelection != null)
										{
											CoreHelper.Log($"Setting Interface Branch Name: {_val}");

											currentQuickElementSelection.keyframes[iTmp].text = _val;

											//Select(selectionType);

											//currentElementSelection.data[iTmp] = _val;
										}
									});

									var timesMax = UIManager.GenerateUIInputField("value", times.transform);

									var timesMaxObject = (GameObject)timesMax["GameObject"];
									var timesMaxLE = timesMaxObject.AddComponent<LayoutElement>();
									timesMaxLE.preferredHeight = 64f;

									//((RectTransform)timesMin["RectTransform"]).anchoredPosition = new Vector2(175f, 0f);
									((RectTransform)timesMin["RectTransform"]).sizeDelta = new Vector2(158f, 38f);

									var timesMaxIF = (InputField)timesMax["InputField"];

									timesMaxIF.textComponent.color = offBlack;
									timesMaxIF.textComponent.alignment = TextAnchor.MiddleLeft;
									((Text)timesMaxIF.placeholder).text = "Set time...";
									((Text)timesMaxIF.placeholder).alignment = TextAnchor.MiddleLeft;
									timesMaxIF.placeholder.color = new Color(0.1f, 0.1f, 0.1f, 0.2f);

									timesMaxIF.text = currentQuickElementSelection.keyframes[i].time.ToString();
									timesMaxIF.onValueChanged.AddListener(delegate (string _val)
									{
										if (!loadingEditor && currentElementSelection != null && float.TryParse(_val, out float num))
										{
											CoreHelper.Log($"Setting Interface Branch Time: {_val}");

											currentQuickElementSelection.keyframes[iTmp].time = num;

											//currentElementSelection.data[iTmp] = _val;
										}
									});

									// Delete
									{
										var editButton = UIManager.GenerateUIButton("delete", times.transform);
										var editButtonObject = (GameObject)editButton["GameObject"];
										editButtonObject.transform.localScale = Vector3.one;

										var lay = editButtonObject.AddComponent<LayoutElement>();
										lay.preferredWidth = 32f;
										lay.ignoreLayout = true;

										((Image)editButton["Image"]).color = new Color(0.934f, 0.4196f, 0.5321f, 1f);

										UIManager.SetRectTransform((RectTransform)editButton["RectTransform"], new Vector2(190f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f));

										var editButtonFunc = (Button)editButton["Button"];
										editButtonFunc.onClick.AddListener(delegate ()
										{
											if (!loadingEditor && currentQuickElementSelection != null)
											{
												CoreHelper.Log($"Deleting QuickElement Keyframe: {iTmp}");

												currentQuickElementSelection.keyframes.RemoveAt(iTmp);

												Select(selectionType);
											}
										});
										//editButtonFunc.colors = UIManager.SetColorBlock(editButtonFunc.colors, new Color(0.2f, 0.2f, 0.2f), new Color(0.4008f, 0.4008f, 0.4008f), new Color(0.4608f, 0.4608f, 0.4608f), new Color(0.2f, 0.2f, 0.2f), new Color(0.4f, 0.2f, 0.2f));

										var img = UIManager.GenerateUIImage("image", editButtonObject.transform);
										var imgObject = (GameObject)img["GameObject"];
										imgObject.transform.localScale = Vector3.one;

										((Image)img["Image"]).color = offWhite;

										UIManager.SetRectTransform((RectTransform)img["RectTransform"], Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f));

										var image = (Image)img["Image"];
										if (RTFile.FileExists(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_delete.png"))
											UIManager.GetImage(image, RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_delete.png");
									}
								}
							}

							for (int i = 0; i < currentQuickElementSelection.effects.Count; i++)
							{
								var iTmp = i;

								//Name
								{
									var times = new GameObject("element " + iTmp.ToString());
									times.transform.SetParent(editors[3].transform.Find("yet/settings"));
									times.transform.localScale = Vector3.one;

									var timesRT = times.AddComponent<RectTransform>();
									timesRT.sizeDelta = new Vector2(350f, 32f);

									var timesLayout = times.AddComponent<HorizontalLayoutGroup>();
									UIManager.SetLayoutGroup(timesLayout, true, true, false, true);
									timesLayout.spacing = 8f;
									timesLayout.childAlignment = TextAnchor.MiddleLeft;

									var timesMin = UIManager.GenerateUIInputField("key", times.transform);

									var timesMinObject = (GameObject)timesMin["GameObject"];
									var timesMinLE = timesMinObject.AddComponent<LayoutElement>();
									timesMinLE.preferredHeight = 64f;

									//((RectTransform)timesMin["RectTransform"]).anchoredPosition = new Vector2(175f, 0f);
									((RectTransform)timesMin["RectTransform"]).sizeDelta = new Vector2(184f, 38f);

									var timesMinIF = (InputField)timesMin["InputField"];

									timesMinIF.textComponent.color = offBlack;
									timesMinIF.textComponent.alignment = TextAnchor.MiddleLeft;
									((Text)timesMinIF.placeholder).text = "Set text...";
									((Text)timesMinIF.placeholder).alignment = TextAnchor.MiddleLeft;
									timesMinIF.placeholder.color = new Color(0.1f, 0.1f, 0.1f, 0.2f);

									timesMinIF.text = currentQuickElementSelection.effects[i].name;
									timesMinIF.onValueChanged.AddListener(delegate (string _val)
									{
										if (!loadingEditor && currentQuickElementSelection != null)
										{
											CoreHelper.Log($"Setting Interface Branch Name: {_val}");

											currentQuickElementSelection.effects[iTmp].name = _val;
										}
									});

									// Delete
									{
										var editButton = UIManager.GenerateUIButton("delete", times.transform);
										var editButtonObject = (GameObject)editButton["GameObject"];
										editButtonObject.transform.localScale = Vector3.one;

										var lay = editButtonObject.AddComponent<LayoutElement>();
										lay.preferredWidth = 32f;
										lay.ignoreLayout = true;

										((Image)editButton["Image"]).color = new Color(0.934f, 0.4196f, 0.5321f, 1f);

										UIManager.SetRectTransform((RectTransform)editButton["RectTransform"], new Vector2(190f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f));

										var editButtonFunc = (Button)editButton["Button"];
										editButtonFunc.onClick.AddListener(delegate ()
										{
											if (!loadingEditor && currentQuickElementSelection != null)
											{
												CoreHelper.Log($"Deleting QuickElement Effect {iTmp}");

												currentQuickElementSelection.effects.RemoveAt(iTmp);

												Select(selectionType);
											}
										});
										//editButtonFunc.colors = UIManager.SetColorBlock(editButtonFunc.colors, new Color(0.2f, 0.2f, 0.2f), new Color(0.4008f, 0.4008f, 0.4008f), new Color(0.4608f, 0.4608f, 0.4608f), new Color(0.2f, 0.2f, 0.2f), new Color(0.4f, 0.2f, 0.2f));

										var img = UIManager.GenerateUIImage("image", editButtonObject.transform);
										var imgObject = (GameObject)img["GameObject"];
										imgObject.transform.localScale = Vector3.one;

										((Image)img["Image"]).color = offWhite;

										UIManager.SetRectTransform((RectTransform)img["RectTransform"], Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f));

										var image = (Image)img["Image"];
										if (RTFile.FileExists(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_delete.png"))
											UIManager.GetImage(image, RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_delete.png");
									}
								}
							}

							CoreHelper.Log($"Selected QuickElement: {currentQuickElementSelection.name}");
						}
						else
						{
							unselectedDialog.SetActive(true);
						}
						break;
                    }
            }

			loadingEditor = false;
        }

        void Exit()
        {
			CoreHelper.Log("Exit Page Editor");
            interfaces.Clear();
            SceneManager.inst.LoadScene("Main Menu");
        }

		IEnumerator Save(Interface @interface)
		{
			var jn = JSON.Parse("{}");

			var inter = @interface;

			var fileName = Path.GetFileName(inter.filePath);

			if (inter.settings != null)
			{
				//Debug.LogFormat("{0}Saving {1} - Times: {2}", PagePlugin.className, fileName, inter.settings.times);
				jn["settings"]["times"]["min"] = inter.settings.times.x;
				jn["settings"]["times"]["max"] = inter.settings.times.y;

				//Debug.LogFormat("{0}Saving {1} - Initial Branch: {2}", PagePlugin.className, fileName, inter.settings.initialBranch);
				jn["settings"]["initial_branch"] = inter.settings.initialBranch;

				if (inter.settings.textColor != LSColors.gray900)
				{
					//Debug.LogFormat("{0}Saving {1} - Text Color: {2}", PagePlugin.className, fileName, inter.settings.textColor);
					jn["settings"]["text_color"] = LSColors.ColorToHex(inter.settings.textColor);
				}

				if (inter.settings.bgColor != LSColors.gray100)
				{
					//Debug.LogFormat("{0}Saving {1} - BG Color: {2}", PagePlugin.className, fileName, inter.settings.bgColor);
					jn["settings"]["bg_color"] = LSColors.ColorToHex(inter.settings.bgColor);
				}

				//Debug.LogFormat("{0}Saving {1} - Music: {2}", PagePlugin.className, fileName, inter.settings.music);
				if (!string.IsNullOrEmpty(inter.settings.music))
					jn["settings"]["music"] = inter.settings.music;
				else
					jn["settings"]["music"] = "menu";

				if (!string.IsNullOrEmpty(inter.settings.returnBranch))
				{
					//Debug.LogFormat("{0}Saving {1} - Return Branch: {2}", PagePlugin.className, fileName, inter.settings.returnBranch);
					jn["settings"]["return_branch"] = inter.settings.returnBranch;
				}
			}

			if (inter.branches != null)
				for (int j = 0; j < inter.branches.Count; j++)
				{
					var branch = inter.branches[j];
					//Debug.LogFormat("{0}Saving {1} - Branch: {2} - Name: {3}", PagePlugin.className, fileName, branch.name, branch.name);
					jn["branches"][j]["name"] = branch.name;
					//Debug.LogFormat("{0}Saving {1} - Branch: {2} - Back Branch: {3}", PagePlugin.className, fileName, branch.name, branch.BackBranch);
					jn["branches"][j]["settings"]["back_branch"] = branch.BackBranch;
					//Debug.LogFormat("{0}Saving {1} - Branch: {2} - Clear Screen: {3}", PagePlugin.className, fileName, branch.name, branch.clear_screen);
					jn["branches"][j]["settings"]["clear_screen"] = branch.clear_screen;
					//Debug.LogFormat("{0}Saving {1} - Branch: {2} - Type: {3} > {4}", PagePlugin.className, fileName, branch.name, branch.type, convertEnumToBranch(branch.type));
					jn["branches"][j]["settings"]["type"] = convertEnumToBranch(branch.type);

					for (int k = 0; k < branch.elements.Count; k++)
					{
						var element = branch.elements[k];

						//Debug.LogFormat("{0}Saving {1} - Branch: {2} - Element: {3} - Type: {4} > {5}", PagePlugin.className, fileName, branch.name, k, element.type, convertEnumToElement(element.type));
						jn["branches"][j]["elements"][k]["type"] = convertEnumToElement(element.type);

						int num = 0;
						if (element.settings != null && element.settings.Count > 0)
							foreach (var setting in element.settings)
							{
								if (setting.Key != null && setting.Value != null)
								{
									string str = setting.Key + ":" + setting.Value;

									//Debug.LogFormat("{0}Saving {1} - Branch: {2} - Element: {3} - Settings: {4} = {5}", PagePlugin.className, fileName, branch.name, k, num, str);
									jn["branches"][j]["elements"][k]["settings"][num] = str;
									num++;
								}
							}

						num = 0;
						foreach (var data in element.data)
						{
							if (data != null)
							{
								//Debug.LogFormat("{0}Saving {1} - Branch: {2} - Element: {3} - Data: {4} = {5}", PagePlugin.className, fileName, branch.name, k, num, data);
								jn["branches"][j]["elements"][k]["data"][num] = data;
								num++;
							}
							else
								jn["branches"][j]["elements"][k]["data"][num] = "";
						}
					}
				}

			CoreHelper.Log($"Saving {inter.filePath.Replace(RTFile.ApplicationDirectory, "")}");

			RTFile.WriteToFile(inter.filePath.Replace(RTFile.ApplicationDirectory, ""), jn.ToString(3));

			yield break;
		}

		IEnumerator Save()
        {
			CoreHelper.Log("Saving...");

			for (int i = 0; i < interfaces.Count; i++)
            {
				var jn = JSON.Parse("{}");

				var inter = interfaces[i];

				var fileName = Path.GetFileName(inter.filePath);

				if (inter.settings != null)
				{
					//Debug.LogFormat("{0}Saving {1} - Times: {2}", PagePlugin.className, fileName, inter.settings.times);
					jn["settings"]["times"]["min"] = inter.settings.times.x;
					jn["settings"]["times"]["max"] = inter.settings.times.y;

					//Debug.LogFormat("{0}Saving {1} - Initial Branch: {2}", PagePlugin.className, fileName, inter.settings.initialBranch);
					jn["settings"]["initial_branch"] = inter.settings.initialBranch;

					if (inter.settings.textColor != LSColors.gray900)
                    {
						//Debug.LogFormat("{0}Saving {1} - Text Color: {2}", PagePlugin.className, fileName, inter.settings.textColor);
						jn["settings"]["text_color"] = LSColors.ColorToHex(inter.settings.textColor);
					}

					if (inter.settings.bgColor != LSColors.gray100)
					{
						//Debug.LogFormat("{0}Saving {1} - BG Color: {2}", PagePlugin.className, fileName, inter.settings.bgColor);
						jn["settings"]["bg_color"] = LSColors.ColorToHex(inter.settings.bgColor);
					}

					//Debug.LogFormat("{0}Saving {1} - Music: {2}", PagePlugin.className, fileName, inter.settings.music);
					if (!string.IsNullOrEmpty(inter.settings.music))
						jn["settings"]["music"] = inter.settings.music;
					else
						jn["settings"]["music"] = "menu";

					if (!string.IsNullOrEmpty(inter.settings.returnBranch))
					{
						//Debug.LogFormat("{0}Saving {1} - Return Branch: {2}", PagePlugin.className, fileName, inter.settings.returnBranch);
						jn["settings"]["return_branch"] = inter.settings.returnBranch;
					}
				}

				if (inter.branches != null)
					for (int j = 0; j < inter.branches.Count; j++)
					{
						var branch = inter.branches[j];
						//Debug.LogFormat("{0}Saving {1} - Branch: {2} - Name: {3}", PagePlugin.className, fileName, branch.name, branch.name);
						jn["branches"][j]["name"] = branch.name;
						//Debug.LogFormat("{0}Saving {1} - Branch: {2} - Back Branch: {3}", PagePlugin.className, fileName, branch.name, branch.BackBranch);
						jn["branches"][j]["settings"]["back_branch"] = branch.BackBranch;
						//Debug.LogFormat("{0}Saving {1} - Branch: {2} - Clear Screen: {3}", PagePlugin.className, fileName, branch.name, branch.clear_screen);
						jn["branches"][j]["settings"]["clear_screen"] = branch.clear_screen;
						//Debug.LogFormat("{0}Saving {1} - Branch: {2} - Type: {3} > {4}", PagePlugin.className, fileName, branch.name, branch.type, convertEnumToBranch(branch.type));
						jn["branches"][j]["settings"]["type"] = convertEnumToBranch(branch.type);

						for (int k = 0; k < branch.elements.Count; k++)
						{
							var element = branch.elements[k];

							//Debug.LogFormat("{0}Saving {1} - Branch: {2} - Element: {3} - Type: {4} > {5}", PagePlugin.className, fileName, branch.name, k, element.type, convertEnumToElement(element.type));
							jn["branches"][j]["elements"][k]["type"] = convertEnumToElement(element.type);

							int num = 0;
							if (element.settings != null && element.settings.Count > 0)
								foreach (var setting in element.settings)
								{
									if (setting.Key != null && setting.Value != null)
									{
										string str = setting.Key + ":" + setting.Value;

										//Debug.LogFormat("{0}Saving {1} - Branch: {2} - Element: {3} - Settings: {4} = {5}", PagePlugin.className, fileName, branch.name, k, num, str);
										jn["branches"][j]["elements"][k]["settings"][num] = str;
										num++;
									}
								}

							num = 0;
							foreach (var data in element.data)
							{
								if (data != null)
								{
									//Debug.LogFormat("{0}Saving {1} - Branch: {2} - Element: {3} - Data: {4} = {5}", PagePlugin.className, fileName, branch.name, k, num, data);
									jn["branches"][j]["elements"][k]["data"][num] = data;
									num++;
								}
								else
									jn["branches"][j]["elements"][k]["data"][num] = "";
							}
						}
					}

				CoreHelper.Log($"Saving {inter.filePath.Replace(RTFile.ApplicationDirectory, "")}");

				RTFile.WriteToFile(inter.filePath.Replace(RTFile.ApplicationDirectory, ""), jn.ToString(3));
			}

			CoreHelper.Log("Saved!");
			yield break;
		}

		void Add(Interface @interface)
        {
			int branchCount = @interface.branches.Count;
			string n = "empty_branch_" + branchCount;
			if (branchCount <= 0)
				n = "empty_branch";

			@interface.branches.Add(new Branch(n));

			if (!expandedBranches.ContainsKey(@interface.filePath + " - " + n))
				expandedBranches.Add(@interface.filePath + " - " + n, false);
			StartCoroutine(RefreshInterface());
        }

		void Add(Branch branch)
        {
			var element = new Element(ElementType.Text, "text");
			branch.elements.Add(element);
			StartCoroutine(RefreshInterface());
        }

		void Delete(Interface @interface)
        {
			if (RTFile.FileExists(@interface.filePath))
            {
				if (@interface == currentInterfaceSelection)
                {
					currentInterfaceSelection = null;
					editors[0].SetActive(false);
                }

				File.Delete(@interface.filePath);
				StartCoroutine(ReloadInterface());
            }
        }

		void Delete(Interface @interface, Branch branch)
        {
			if (currentBranchSelection == branch)
            {
				currentBranchSelection = null;
				editors[1].SetActive(false);
            }

			if (expandedBranches.ContainsKey(@interface.filePath + " - " + branch.name))
				expandedBranches.Remove(@interface.filePath + " - " + branch.name);
            @interface.branches.Remove(branch);
			StartCoroutine(RefreshInterface());
        }

        void Delete(Branch branch, Element element)
        {
			if (currentElementSelection == element)
			{
				currentElementSelection = null;
				editors[2].SetActive(false);
			}

			branch.elements.Remove(element);
			StartCoroutine(RefreshInterface());
		}

		IEnumerator ReloadInterface()
        {
			interfaces.Clear();
			expandedBranches.Clear();

			yield return StartCoroutine(LoadInterfaces());

			for (int i = 0; i < editors.Count; i++)
            {
				editors[i].SetActive(false);
            }

			unselectedDialog.SetActive(true);

			currentInterfaceSelection = null;
			currentBranchSelection = null;
			currentElementSelection = null;

			yield return StartCoroutine(RefreshInterface());

			yield break;
        }

		ElementType convertElementToEnum(string _type)
		{
			_type = _type.ToLower();
			switch (_type)
			{
				case "text": return ElementType.Text;
				case "divider": return ElementType.Divider;
				case "buttons": return ElementType.Buttons;
				case "media": return ElementType.Media;
				case "event": return ElementType.Event;
				default:
					CoreHelper.LogWarning($"Couldn't convert type [{_type}]");
					return ElementType.Text;
			}
		}

		BranchType convertBranchToEnum(string _type)
		{
			if (string.IsNullOrEmpty(_type))
			{
				return BranchType.Normal;
			}
			_type = _type.ToLower();
			switch (_type)
			{
				case "normal":
					return BranchType.Normal;
				case "menu":
					return BranchType.Menu;
				case "main_menu":
					return BranchType.MainMenu;
				case "skipable":
					return BranchType.Skipable;
				default:
					CoreHelper.LogWarning($"Couldn't convert type [{_type}]");
					return BranchType.Normal;
			}
		}

		string convertEnumToElement(ElementType type)
		{
			return type.ToString().ToLower();
		}

		string convertEnumToBranch(BranchType type)
		{
			switch (type)
			{
				case BranchType.Normal: return "normal";
				case BranchType.Menu: return "menu";
				case BranchType.MainMenu: return "main_menu";
				case BranchType.Skipable: return "skipable";
			}
			return "normal";
		}

		int GetBackBranchValue(Branch branch)
		{
			var current = currentInterfaceSelection.branches.FindIndex(x => x.name == branch.BackBranch);

			if (string.IsNullOrEmpty(branch.BackBranch) || current + 1 < 1)
				return 0;

			return current + 1;
        }

		List<Dropdown.OptionData> convertBranchesToOptions(Interface @interface, bool addNone = true)
        {
			var list = new List<Dropdown.OptionData>();

			if (addNone)
				list.Add(new Dropdown.OptionData("None"));

			foreach (var branch in @interface.branches)
				list.Add(new Dropdown.OptionData(branch.name));

			return list;
        }

        public class Interface
        {
            public Interface(string filePath, List<Branch> branches)
            {
                this.filePath = filePath;
                this.branches = branches;
				settings = new Settings();
            }

			public string FileName
            {
				get
                {
					return Path.GetFileName(filePath).Replace(".lsm", "");
				}
            }

            public string filePath;
			public Settings settings;
            public List<Branch> branches = new List<Branch>();
			public bool expanded = false;

			public class Settings
			{
				public int language;

				public Vector2 times = new Vector2(0.01f, 0.05f);

				public string initialBranch = "alpha";

				public Color textColor = LSColors.gray900;

				public Color textHighlightColor = Color.black;

				public Color borderColor = Color.black;

				public Color borderHighlightColor = new Color32(66, 66, 66, byte.MaxValue);

				public Color bgColor = LSColors.gray100;

				public string music = "menu";

				public string returnBranch = "";
			}
        }

		public class InterfaceTheme
        {
			public string name;
			public Color bg;
			public Color text;
			public Color highlight;
			public Color texthighlight;
			public Color buttonbg;
        }
    }
}
