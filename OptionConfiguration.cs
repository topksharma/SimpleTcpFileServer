using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

namespace PRepo.Tcp.Common
{
	public static class OptionConfiguration
	{
		public const string CONFIG_FILE_NAME = @"option_config.xml";
		private static readonly Dictionary<string, string> _configValues = new Dictionary<string, string>();
		//static OptionConfiguration()
		//{
		//	ReadConfigurations();
		//}

		public static void ReadConfigurations(string filePath)
		{
			if (File.Exists(CONFIG_FILE_NAME))
			{
				try
				{
					XDocument xDocument = XDocument.Load(CONFIG_FILE_NAME);

					foreach (XElement ele in xDocument.Root.Elements("setting"))
					{
						string settingKey = ele.GetAttributeValue("key").ToLower();
						if (!string.IsNullOrEmpty(settingKey))
						{
							_configValues[settingKey] = ele.GetAttributeValue("value");
						}
					}
				}
				catch { }
			}
		}

		//public static bool GetBool(string key, bool defaulValue)
		//{
		//	if (key.IsNotNullOrEmpty())
		//	{
		//		key = key.ToLower();
		//		string strVal;
		//		if (_configValues.TryGetValue(key, out strVal))
		//		{
		//			return strVal.TryParseToBool();
		//		}
		//	}
		//	return defaulValue;
		//}

		public static string GetValue(string key, string defaulValue)
		{
			if (key.IsNotNullOrEmpty())
			{
				key = key.ToLower();
				string strVal;
				if (_configValues.TryGetValue(key, out strVal))
				{
					return strVal;
				}
			}
			return defaulValue;
		}
		public static string GetRootFolder()
		{
			return GetValue("RootFolder", Assembly.GetExecutingAssembly().CodeBase);
		}
		public static string GetDownloadFolder()
		{
			return GetValue("DownloadFolder", Assembly.GetExecutingAssembly().CodeBase);
		}
	}
}
