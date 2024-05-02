namespace BetterLegacy.Core
{
    public static class Parser
	{
		public static int TryParse(string input, int defaultValue)
        {
			if (int.TryParse(input, out int num))
				return num;
			return defaultValue;
        }
		
		public static float TryParse(string input, float defaultValue)
        {
			if (float.TryParse(input, out float num))
				return num;
			return defaultValue;
        }

		public static bool TryParse(string input, bool defaultValue)
        {
			if (bool.TryParse(input, out bool result))
				return result;
			return defaultValue;
        }
	}
}
