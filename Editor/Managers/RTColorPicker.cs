using System;

using LSFunctions;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Settings;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Popups;

namespace BetterLegacy.Editor.Managers
{
	/// <summary>
	/// Manages color picking.
    /// <br></br>Wraps <see cref="ColorPicker"/>.
	/// </summary>
    public class RTColorPicker : BaseManager<RTColorPicker, EditorManagerSettings>
    {
        #region Values

        public ColorPicker baseColorPicker;

        public Button closeButton;

        public Slider hueSlider;

        public InputField rField;
        public InputField gField;
        public InputField bField;

        public InputField hField;
        public InputField sField;
        public InputField vField;

        public InputField hexField;

        public Image previewImage;

        public Button saveButton;

        public string currentHex;

        public Color currentColor;

        Action<Color, string> colorChanged;

        Action<Color, string> colorSaved;

        Action cancel;

        #endregion

        #region Functions

        public override void OnInit()
        {
			var dialog = EditorManager.inst.GetDialog(EditorPopup.COLOR_PICKER).Dialog.Find("content");
			baseColorPicker = dialog.Find("Color Picker").GetComponent<ColorPicker>();
			var draggable = dialog.gameObject.AddComponent<DraggableUI>();
			draggable.target = dialog;
            draggable.mode = DraggableUI.DragMode.RequiredDrag;

			baseColorPicker.hueSliderTexture = new Texture2D(1, 359, TextureFormat.ARGB32, false);
			Color[] array = new Color[359];
			for (int i = 0; i < array.Length; i++)
				array[i] = LSColors.ColorFromHSV(359f - i, 1.0, 1.0);

			baseColorPicker.hueSliderTexture.SetPixels(array);
			baseColorPicker.hueSliderTexture.wrapMode = TextureWrapMode.Repeat;
			baseColorPicker.hueSliderTexture.filterMode = FilterMode.Point;
			baseColorPicker.hueSliderTexture.Apply();
			baseColorPicker.hueSlider.transform.Find("Background").GetComponent<Image>().sprite = Sprite.Create(baseColorPicker.hueSliderTexture, new Rect(0f, 0f, 1f, 359f), new Vector2(0.5f, 0.5f), 100f);
			hueSlider = baseColorPicker.hueSlider.GetComponent<Slider>();
			hueSlider.onValueChanged.AddListener(_val =>
			{
				RenderPanel(_val);
				LSColors.ColorToHSV(currentColor, out double hue, out double sat, out double val);
				RenderEditor(LSColors.ColorFromHSV(_val * 359f, sat, val));
			});

			TriggerHelper.AddEventTriggers(baseColorPicker.brightnessPanel, TriggerHelper.CreateEntry(EventTriggerType.PointerClick, ClickTrigger), TriggerHelper.CreateEntry(EventTriggerType.Drag, ClickTrigger));

			EditorThemeManager.AddGraphic(dialog.GetComponent<Image>(), ThemeGroup.Background_2, true);

			var topPanel = dialog.Find("title").GetComponent<Image>();
			EditorThemeManager.AddGraphic(topPanel, ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);

			var title = topPanel.transform.Find("Text").GetComponent<Text>();
			EditorThemeManager.AddLightText(title);

			closeButton = topPanel.transform.Find("x").GetComponent<Button>();
			Destroy(closeButton.GetComponent<Animator>());
			closeButton.transition = Selectable.Transition.ColorTint;
			EditorThemeManager.AddSelectable(closeButton, ThemeGroup.Close);
			EditorThemeManager.AddGraphic(closeButton.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);
			closeButton.onClick.NewListener(Cancel);

			rField = baseColorPicker.rgb.transform.Find("R/input").GetComponent<InputField>();
			gField = baseColorPicker.rgb.transform.Find("G/input").GetComponent<InputField>();
			bField = baseColorPicker.rgb.transform.Find("B/input").GetComponent<InputField>();

			hField = baseColorPicker.hsv.transform.Find("H/input").GetComponent<InputField>();
			sField = baseColorPicker.hsv.transform.Find("S/input").GetComponent<InputField>();
			vField = baseColorPicker.hsv.transform.Find("V/input").GetComponent<InputField>();

			hexField = baseColorPicker.hex.GetComponent<InputField>();

			EditorThemeManager.AddInputField(rField);
			EditorThemeManager.AddInputField(gField);
			EditorThemeManager.AddInputField(bField);
			
			EditorThemeManager.AddInputField(hField);
			EditorThemeManager.AddInputField(sField);
			EditorThemeManager.AddInputField(vField);

			EditorThemeManager.AddInputField(hexField);

			rField.characterValidation = InputField.CharacterValidation.None;
			rField.keyboardType = TouchScreenKeyboardType.Default;
			rField.inputType = InputField.InputType.Standard;

			gField.characterValidation = InputField.CharacterValidation.None;
			gField.keyboardType = TouchScreenKeyboardType.Default;
			gField.inputType = InputField.InputType.Standard;

			bField.characterValidation = InputField.CharacterValidation.None;
			bField.keyboardType = TouchScreenKeyboardType.Default;
			bField.inputType = InputField.InputType.Standard;

			hField.characterValidation = InputField.CharacterValidation.None;
			hField.keyboardType = TouchScreenKeyboardType.Default;
			hField.inputType = InputField.InputType.Standard;

			sField.characterValidation = InputField.CharacterValidation.None;
			sField.keyboardType = TouchScreenKeyboardType.Default;
			sField.inputType = InputField.InputType.Standard;

			vField.characterValidation = InputField.CharacterValidation.None;
			vField.keyboardType = TouchScreenKeyboardType.Default;
			vField.inputType = InputField.InputType.Standard;

			previewImage = baseColorPicker.preview.GetComponent<Image>();

			var info = baseColorPicker.transform.Find("info").GetComponent<Image>();
			EditorThemeManager.AddGraphic(info, ThemeGroup.Background_3, true);

			saveButton = info.transform.Find("hex/save").GetComponent<Button>();
			EditorThemeManager.AddSelectable(saveButton, ThemeGroup.Function_2);
			EditorThemeManager.AddGraphic(saveButton.transform.GetChild(0).GetComponent<Text>(), ThemeGroup.Function_2_Text);
			saveButton.onClick.NewListener(SaveColor);

			RenderPanel(hueSlider.value);
			RenderEditor(LSColors.black);
		}

		public void Show(Color currentColor, Action<Color, string> colorChanged, Action<Color, string> colorSaved, Action cancel = null)
		{
			RTEditor.inst.ShowDialog(EditorPopup.COLOR_PICKER);

			this.colorChanged = null;
			SwitchCurrentColor(currentColor);
			this.colorSaved = colorSaved;
			this.colorChanged = colorChanged;
			this.cancel = cancel;
		}

		void SwitchCurrentColor(Color currentColor)
        {
            LSColors.ColorToHSV(currentColor, out double hue, out double sat, out double val);
			this.currentColor = currentColor;
			hueSlider.value = (float)hue / 359f;
			RenderPanel((float)hue / 359f);
			RenderEditor(currentColor);
            UpdateSlider((float)sat, (float)val, LSMath.RectTransformToScreenSpace2(baseColorPicker.brightnessPanel.transform.AsRT()));
        }

		void ClickTrigger(BaseEventData eventData)
		{
			var rect2 = LSMath.RectTransformToScreenSpace2(baseColorPicker.brightnessPanel.transform.AsRT());
			var offset = baseColorPicker.transform.position;
			offset.y -= 100f * CoreHelper.ScreenScale;
			var mousePosition = Input.mousePosition;

			var pos = new Vector2((mousePosition.x - offset.x) * CoreHelper.ScreenScale, (mousePosition.y - offset.y) * CoreHelper.ScreenScale);
			pos.x /= rect2.width * CoreHelper.ScreenScale;
			pos.y /= rect2.height * CoreHelper.ScreenScale;

			pos = RTMath.Clamp(pos, Vector2.zero, Vector2.one);

			var color = LSColors.ColorFromHSV(hueSlider.value * 359f, pos.x, pos.y);
			UpdateSlider(pos.x, pos.y, rect2);
			RenderEditor(color);
		}

		void UpdateSlider(float saturation, float value, Rect panelScreenRect)
		{
			baseColorPicker.brightnessPanelSlider.GetComponent<Image>().color = ((value <= 0.5) ? LSColors.white : LSColors.black);
			baseColorPicker.brightnessPanelSlider.transform.AsRT().anchoredPosition = new Vector3(saturation * panelScreenRect.width - 2.5f, value * panelScreenRect.height - 2.5f) * CoreHelper.ScreenScaleInverse;
		}

		void RenderEditor(Color color)
		{
            try
			{
				LSColors.ColorToHSV(color, out double hue, out double sat, out double val);
				var hex = LSColors.ColorToHex(color);

				rField.SetTextWithoutNotify((color.r * 255f).ToString());
				rField.onValueChanged.NewListener(_val =>
				{
					if (float.TryParse(_val, out float r) && float.TryParse(gField.text, out float g) && float.TryParse(bField.text, out float b))
						SwitchCurrentColor(new Color(r / 255f, g / 255f, b / 255f, 255f));
				});
				rField.onEndEdit.NewListener(_val =>
				{
					if (RTMath.TryParse(_val, color.r * 255f, out float r) && RTMath.TryParse(gField.text, color.g * 255f, out float g) && RTMath.TryParse(bField.text, color.b * 255f, out float b))
						SwitchCurrentColor(new Color(r / 255f, g / 255f, b / 255f, 255f));
				});
				gField.SetTextWithoutNotify((color.g * 255f).ToString());
				gField.onValueChanged.NewListener(_val =>
				{
					if (float.TryParse(rField.text, out float r) && float.TryParse(_val, out float g) && float.TryParse(bField.text, out float b))
						SwitchCurrentColor(new Color(r / 255f, g / 255f, b / 255f, 255f));
				});
				gField.onEndEdit.NewListener(_val =>
				{
					if (RTMath.TryParse(rField.text, color.r * 255f, out float r) && RTMath.TryParse(_val, color.g * 255f, out float g) && RTMath.TryParse(bField.text, color.b * 255f, out float b))
						SwitchCurrentColor(new Color(r / 255f, g / 255f, b / 255f, 255f));
				});
				bField.SetTextWithoutNotify((color.b * 255f).ToString());
				bField.onValueChanged.NewListener(_val =>
				{
					if (float.TryParse(rField.text, out float r) && float.TryParse(gField.text, out float g) && float.TryParse(_val, out float b))
						SwitchCurrentColor(new Color(r / 255f, g / 255f, b / 255f, 255f));
				});
				bField.onEndEdit.NewListener(_val =>
				{
					if (RTMath.TryParse(rField.text, color.r * 255f, out float r) && RTMath.TryParse(gField.text, color.g * 255f, out float g) && RTMath.TryParse(_val, color.b * 255f, out float b))
						SwitchCurrentColor(new Color(r / 255f, g / 255f, b / 255f, 255f));
				});

				hField.SetTextWithoutNotify(Mathf.RoundToInt((float)hue).ToString());
				hField.onValueChanged.NewListener(_val =>
				{
					if (float.TryParse(_val, out float hue) && float.TryParse(sField.text, out float sat) && float.TryParse(vField.text, out float val))
						SwitchCurrentColor(LSColors.ColorFromHSV(hue, sat / 100.0, val / 100.0));
				});
				hField.onEndEdit.NewListener(_val =>
				{
					if (RTMath.TryParse(_val, (float)hue, out float newHue) && RTMath.TryParse(sField.text, (float)sat, out float newSat) && RTMath.TryParse(vField.text, (float)val, out float newVal))
						SwitchCurrentColor(LSColors.ColorFromHSV(newHue, newSat / 100.0, newVal / 100.0));
				});
				sField.SetTextWithoutNotify(Mathf.RoundToInt((float)sat * 100f).ToString());
				sField.onValueChanged.NewListener(_val =>
				{
					if (float.TryParse(hField.text, out float hue) && float.TryParse(_val, out float sat) && float.TryParse(vField.text, out float val))
						SwitchCurrentColor(LSColors.ColorFromHSV(hue, sat / 100.0, val / 100.0));
				});
				sField.onEndEdit.NewListener(_val =>
				{
					if (RTMath.TryParse(hField.text, (float)hue, out float newHue) && RTMath.TryParse(_val, (float)sat, out float newSat) && RTMath.TryParse(vField.text, (float)val, out float newVal))
						SwitchCurrentColor(LSColors.ColorFromHSV(newHue, newSat / 100.0, newVal / 100.0));
				});
				vField.SetTextWithoutNotify(Mathf.RoundToInt((float)val * 100f).ToString());
				vField.onValueChanged.NewListener(_val =>
				{
					if (float.TryParse(hField.text, out float hue) && float.TryParse(sField.text, out float sat) && float.TryParse(_val, out float val))
						SwitchCurrentColor(LSColors.ColorFromHSV(hue, sat / 100.0, val / 100.0));
				});
				vField.onEndEdit.NewListener(_val =>
				{
					if (RTMath.TryParse(hField.text, (float)hue, out float newHue) && RTMath.TryParse(sField.text, (float)sat, out float newSat) && RTMath.TryParse(_val, (float)val, out float newVal))
						SwitchCurrentColor(LSColors.ColorFromHSV(newHue, newSat / 100.0, newVal / 100.0));
				});

				hexField.SetTextWithoutNotify(hex);
				hexField.onValueChanged.NewListener(_val => SwitchCurrentColor(RTColors.HexToColor(_val)));

				//TriggerHelper.IncreaseDecreaseButtons(rField, max: 255f);
				//TriggerHelper.IncreaseDecreaseButtons(gField, max: 255f);
				//TriggerHelper.IncreaseDecreaseButtons(bField, max: 255f);

				TriggerHelper.AddEventTriggers(rField.gameObject, TriggerHelper.ScrollDelta(rField, max: 255f));
				TriggerHelper.AddEventTriggers(gField.gameObject, TriggerHelper.ScrollDelta(gField, max: 255f));
				TriggerHelper.AddEventTriggers(bField.gameObject, TriggerHelper.ScrollDelta(bField, max: 255f));

				TriggerHelper.AddEventTriggers(hField.gameObject, TriggerHelper.ScrollDelta(hField, max: 360f));
				TriggerHelper.AddEventTriggers(sField.gameObject, TriggerHelper.ScrollDelta(sField, max: 100f));
				TriggerHelper.AddEventTriggers(vField.gameObject, TriggerHelper.ScrollDelta(vField, max: 100f));

				currentHex = hex;
				previewImage.color = color;
				currentColor = color;

				colorChanged?.Invoke(color, hex);
			}
            catch (Exception ex)
            {
				CoreHelper.LogException(ex);
            }
        }

        void RenderPanel(float val)
        {
            baseColorPicker.brightnessPanelTexture = new Texture2D(100, 100, TextureFormat.ARGB32, false);
            Color[] array = new Color[10000];
            for (int i = 0; i < 100; i++)
            {
                for (int j = 0; j < 100; j++)
                {
                    double num = i;
                    num /= 100.0;
                    double num2 = j;
                    num2 /= 100.0;
                    array[i * 100 + j] = LSColors.ColorFromHSV(val * 359f, num2, num);
                }
            }

            baseColorPicker.brightnessPanelTexture.SetPixels(array);
            baseColorPicker.brightnessPanelTexture.wrapMode = TextureWrapMode.Clamp;
            baseColorPicker.brightnessPanelTexture.filterMode = FilterMode.Point;
            baseColorPicker.brightnessPanelTexture.Apply();
            baseColorPicker.brightnessPanel.GetComponent<Image>().sprite = Sprite.Create(baseColorPicker.brightnessPanelTexture, new Rect(0f, 0f, 100f, 100f), new Vector2(0.5f, 0.5f), 100f);
        }

		void Cancel()
		{
			RTEditor.inst.HideDialog(EditorPopup.COLOR_PICKER);
			cancel?.Invoke();
		}

		void SaveColor()
		{
            try
			{
				colorSaved?.Invoke(currentColor, currentHex);
				CoreHelper.Log($"Set hex color: {currentHex}");
			}
            catch (Exception ex)
            {
				EditorManager.inst.DisplayNotification($"Color Picker failed to save the color due to an exception.", 4f, EditorManager.NotificationType.Error);
				CoreHelper.LogException(ex);
            }
			RTEditor.inst.HideDialog(EditorPopup.COLOR_PICKER);
		}

		#endregion
	}
}
