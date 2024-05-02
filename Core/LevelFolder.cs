using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Core
{
	public class LevelFolder<T>
	{
		public LevelFolder(T level, GameObject gameObject, RectTransform rectTransform, Image icon)
		{
			this.level = level; this.gameObject = gameObject; this.rectTransform = rectTransform; this.icon = icon;
		}

		public T level;
		public GameObject gameObject;
		public RectTransform rectTransform;
		public Image icon;
	}
}
