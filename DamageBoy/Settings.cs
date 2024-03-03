using DamageBoy.Core;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DamageBoy;

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
            Data = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.SettingsData);

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
        string json = JsonSerializer.Serialize(Data, SourceGenerationContext.Default.SettingsData);
        File.WriteAllText(SETTINGS_FILE, json);

        Utils.Log(LogType.Info, $"Settings file successfully saved: \"{SETTINGS_FILE}\"");
    }
}

class SettingsData
{
    public string LastRomDirectory { get; set; }
    public List<string> RecentRoms { get; set; }

    public HardwareTypes HardwareType { get; set; }

    public bool PauseWhileMinimized { get; set; }
    public bool CompressSaveStates { get; set; }

    public ColorSetting GbColor0 { get; set; }
    public ColorSetting GbColor1 { get; set; }
    public ColorSetting GbColor2 { get; set; }
    public ColorSetting GbColor3 { get; set; }
    public float LcdEffectVisibility { get; set; }

    public float AudioVolume { get; set; }
    public bool Channel1Enabled { get; set; }
    public bool Channel2Enabled { get; set; }
    public bool Channel3Enabled { get; set; }
    public bool Channel4Enabled { get; set; }

    public SettingsData()
    {
        LastRomDirectory = Environment.CurrentDirectory;
        RecentRoms = new List<string>(Window.MAX_RECENT_ROMS);

        HardwareType = HardwareTypes.CGB;

        PauseWhileMinimized = true;
        CompressSaveStates = true;

        DefaultColors();
        LcdEffectVisibility = 1.0f;

        AudioVolume = 1f;
        Channel1Enabled = true;
        Channel2Enabled = true;
        Channel3Enabled = true;
        Channel4Enabled = true;
    }

    public void DefaultColors()
    {
        GbColor0 = new ColorSetting(30, 64, 70);
        GbColor1 = new ColorSetting(68, 121, 106);
        GbColor2 = new ColorSetting(123, 183, 127);
        GbColor3 = new ColorSetting(195, 245, 162);
    }

    public void SetColors(ColorSetting color0, ColorSetting color1, ColorSetting color2, ColorSetting color3)
    {
        GbColor0 = color0;
        GbColor1 = color1;
        GbColor2 = color2;
        GbColor3 = color3;
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

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SettingsData))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}