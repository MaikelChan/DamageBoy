using DamageBoy.Core;
using DamageBoy.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DamageBoy
{
    class Settings
    {
        public SettingsData Data { get; private set; }

        const string SETTINGS_FILE = "Settings.json";

        public Settings()
        {
            Load();
        }

        public void Load()
        {
            if (File.Exists(SETTINGS_FILE))
            {
                string json = File.ReadAllText(SETTINGS_FILE);
                Data = JsonSerializer.Deserialize<SettingsData>(json);

                Utils.Log(LogType.Info, $"Settings file successfully loaded: \"{SETTINGS_FILE}\"");
            }
            else
            {
                Utils.Log(LogType.Info, "Settings file not found. Creating a new one...");

                Data = new SettingsData();
                Save();
            }
        }

        public void Save()
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };

            string json = JsonSerializer.Serialize(Data, options);
            File.WriteAllText(SETTINGS_FILE, json);

            Utils.Log(LogType.Info, $"Settings file successfully saved: \"{SETTINGS_FILE}\"");
        }
    }

    class SettingsData
    {
        public string LastRomDirectory { get; set; }
        public List<string> RecentRoms { get; set; }

        public SettingsData()
        {
            LastRomDirectory = Environment.CurrentDirectory;
            RecentRoms = new List<string>(Window.MAX_RECENT_ROMS);
        }
    }
}