using System.Configuration;

namespace IoPS
{
	public class ConfigurationService
	{
		public string GetSetting(string name) => ConfigurationManager.AppSettings.Get(name);

		public string GetSetting(string name, string defaultValue)
		{
			string value = GetSetting(name);

			if (string.IsNullOrWhiteSpace(value))
			{
				return defaultValue;
			}

			return value;
		}
	}
}