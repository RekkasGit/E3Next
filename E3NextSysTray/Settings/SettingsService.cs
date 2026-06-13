using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3NextSysTray.Settings
{
	public static class SettingsService
	{
		// Best practice: Use AppData for user-specific settings
		private static readonly string ConfigPath = Path.Combine(TrayApplicationContext._currentDirectory,
			"config/E3NextSysTray.json"
		);

		public static void Save(AppSettings settings)
		{
			// Ensure directory exists
			Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));

			// Serialize with Indented formatting for human readability
			string json = JsonConvert.SerializeObject(settings, Formatting.Indented);

			File.WriteAllText(ConfigPath, json);
		}

		public static AppSettings Load()
		{
			if (!File.Exists(ConfigPath)) return new AppSettings();

			string json = File.ReadAllText(ConfigPath);
			return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
		}
	}
}
