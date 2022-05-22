using DamageBoy.Core;
using OpenTK.Mathematics;
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

        public ColorSetting GbColor0 { get; set; }
        public ColorSetting GbColor1 { get; set; }
        public ColorSetting GbColor2 { get; set; }
        public ColorSetting GbColor3 { get; set; }
        public float LcdEffectVisibility { get; set; }

        public float AudioVolume { get; set; }

        public SettingsData()
        {
            LastRomDirectory = Environment.CurrentDirectory;
            RecentRoms = new List<string>(Window.MAX_RECENT_ROMS);

            ResetColors();
            LcdEffectVisibility = 1.0f;

            AudioVolume = 1f;
        }

        public void ResetColors()
        {
            GbColor0 = new ColorSetting(27, 64, 51);
            GbColor1 = new ColorSetting((byte)MathHelper.Lerp(27, 195, 1f / 3f), (byte)MathHelper.Lerp(64, 245, 1f / 3f), (byte)MathHelper.Lerp(51, 162, 1f / 3f));
            GbColor2 = new ColorSetting((byte)MathHelper.Lerp(27, 195, 2f / 3f), (byte)MathHelper.Lerp(64, 245, 2f / 3f), (byte)MathHelper.Lerp(51, 162, 2f / 3f));
            GbColor3 = new ColorSetting(195, 245, 162);
        }
    }

    [Serializable]
    public struct ColorSetting
    {
        public float R { get; set; }
        public float G { get; set; }
        public float B { get; set; }

        public ColorSetting(Color4 color)
        {
            R = color.R;
            G = color.G;
            B = color.B;
        }

        public ColorSetting(System.Numerics.Vector3 color)
        {
            R = color.X;
            G = color.Y;
            B = color.Z;
        }

        public ColorSetting(byte r, byte g, byte b)
        {
            R = r / 255f;
            G = g / 255f;
            B = b / 255f;
        }

        public Color4 ToColor4()
        {
            return new Color4(R, G, B, 1f);
        }
    }
}