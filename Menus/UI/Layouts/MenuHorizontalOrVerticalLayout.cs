namespace BetterLegacy.Menus.UI.Layouts
{
    public abstract class MenuHorizontalOrVerticalLayout : MenuLayoutBase
    {
        public bool childControlHeight;
        public bool childControlWidth;
        public bool childForceExpandHeight;
        public bool childForceExpandWidth;
        public bool childScaleHeight;
        public bool childScaleWidth;

        public float spacing;

        public float minScroll = -100f;
        public float maxScroll = 100f;
    }
}
