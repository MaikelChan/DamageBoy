using DamageBoy.Graphics;
using ImGuiNET;
using System.Numerics;

namespace DamageBoy.UI;

class MainUI : BaseUI
{
    readonly Window window;
    readonly BaseRenderer renderer;
    readonly Settings settings;

    readonly FileBrowserUI fileBrowserUI;
    readonly AboutWindowUI aboutWindowUI;

    readonly Vector2 separatorMargin = new Vector2(0f, 5f);
    readonly Vector4 menuHeaderColor = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);

    public const int MAIN_MENU_DEFAULT_HEIGHT = 19;

    public int MainMenuHeight { get; private set; }

    public MainUI(Window window, BaseRenderer renderer, Settings settings)
    {
        this.window = window;
        this.renderer = renderer;
        this.settings = settings;

        MainMenuHeight = MAIN_MENU_DEFAULT_HEIGHT;

        fileBrowserUI = new FileBrowserUI("Open ROM", settings.Data.LastRomDirectory, ".gb|.zip", false, window.OpenROM);
        aboutWindowUI = new AboutWindowUI();
    }

    protected override void InternalRender()
    {
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(640, 30));

        if (ImGui.BeginMainMenuBar())
        {
            MainMenuHeight = (int)ImGui.GetWindowSize().Y;

            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("Open ROM...", "Ctrl+O"))
                {
                    OpenFileBrowser();
                }

                if (ImGui.BeginMenu("Open Recent"))
                {
                    for (int r = 0; r < settings.Data.RecentRoms.Count; r++)
                    {
                        if (ImGui.MenuItem(settings.Data.RecentRoms[r]))
                        {
                            window.OpenROM(settings.Data.RecentRoms[r]);
                        }
                    }

                    ImGui.EndMenu();
                }

                ImGui.Separator();

                if (ImGui.MenuItem("Quit", "Ctrl+Q"))
                {
                    window.RequestClose();
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Emulation"))
            {
                if (ImGui.MenuItem("Run", "F1", window.IsGameBoyRunning))
                {
                    window.RunEmulation();
                }

                if (ImGui.MenuItem("Pause", "F2", window.IsGameBoyPaused))
                {
                    window.TogglePauseEmulation();
                }

                if (ImGui.MenuItem("Stop", "F3", !window.IsGameBoyRunning))
                {
                    window.StopEmulation();
                }

                ImGui.Separator();

                if (ImGui.MenuItem("Save State", "F5"))
                {
                    window.SaveState();
                }

                if (ImGui.MenuItem("Load State", "F7"))
                {
                    window.LoadState();
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Settings"))
            {
                ImGui.TextColored(menuHeaderColor, "General");

                bool pauseWhileMinimized = settings.Data.PauseWhileMinimized;
                if (ImGui.Checkbox("Pause While Minimized", ref pauseWhileMinimized))
                {
                    settings.Data.PauseWhileMinimized = pauseWhileMinimized;
                }

                ImGui.Separator();

                ImGui.Dummy(separatorMargin);
                ImGui.TextColored(menuHeaderColor, "Graphics");

                float visibility = settings.Data.LcdEffectVisibility;
                if (ImGui.SliderFloat("LCD Effect Visibility", ref visibility, 0.0f, 1.0f))
                {
                    settings.Data.LcdEffectVisibility = visibility;
                }

                Vector3 color0 = new Vector3(settings.Data.GbColor0.R, settings.Data.GbColor0.G, settings.Data.GbColor0.B);
                if (ImGui.ColorEdit3("LCD Color 0", ref color0))
                {
                    settings.Data.GbColor0 = new ColorSetting(color0);
                }

                Vector3 color1 = new Vector3(settings.Data.GbColor1.R, settings.Data.GbColor1.G, settings.Data.GbColor1.B);
                if (ImGui.ColorEdit3("LCD Color 1", ref color1))
                {
                    settings.Data.GbColor1 = new ColorSetting(color1);
                }

                Vector3 color2 = new Vector3(settings.Data.GbColor2.R, settings.Data.GbColor2.G, settings.Data.GbColor2.B);
                if (ImGui.ColorEdit3("LCD Color 2", ref color2))
                {
                    settings.Data.GbColor2 = new ColorSetting(color2);
                }

                Vector3 color3 = new Vector3(settings.Data.GbColor3.R, settings.Data.GbColor3.G, settings.Data.GbColor3.B);
                if (ImGui.ColorEdit3("LCD Color 3", ref color3))
                {
                    settings.Data.GbColor3 = new ColorSetting(color3);
                }

                if (ImGui.Button("Default Theme"))
                {
                    settings.Data.DefaultColors();
                }

                ImGui.SameLine();

                if (ImGui.Button("Pink Theme"))
                {
                    settings.Data.SetColors(new ColorSetting(82, 15, 96), new ColorSetting(141, 27, 118), new ColorSetting(220, 75, 152), new ColorSetting(245, 162, 182));
                }

                ImGui.SameLine();

                if (ImGui.Button("Blue Theme"))
                {
                    settings.Data.SetColors(new ColorSetting(39, 34, 118), new ColorSetting(67, 92, 160), new ColorSetting(120, 162, 196), new ColorSetting(179, 244, 255));
                }

                ImGui.SameLine();

                if (ImGui.Button("White Theme"))
                {
                    settings.Data.SetColors(new ColorSetting(0, 0, 0), new ColorSetting(85, 85, 85), new ColorSetting(170, 170, 170), new ColorSetting(255, 255, 255));
                }

                ImGui.Separator();

                ImGui.Dummy(separatorMargin);
                ImGui.TextColored(menuHeaderColor, "Audio");

                float volume = settings.Data.AudioVolume;
                if (ImGui.SliderFloat("Audio Volume", ref volume, 0.0f, 1.0f))
                {
                    settings.Data.AudioVolume = volume;
                }

                bool channel1 = settings.Data.Channel1Enabled;
                if (ImGui.Checkbox("Enable Channel 1", ref channel1))
                {
                    settings.Data.Channel1Enabled = channel1;
                    window.UpdateGameBoySettings();
                }

                ImGui.SameLine();

                bool channel2 = settings.Data.Channel2Enabled;
                if (ImGui.Checkbox("Enable Channel 2", ref channel2))
                {
                    settings.Data.Channel2Enabled = channel2;
                    window.UpdateGameBoySettings();
                }

                bool channel3 = settings.Data.Channel3Enabled;
                if (ImGui.Checkbox("Enable Channel 3", ref channel3))
                {
                    settings.Data.Channel3Enabled = channel3;
                    window.UpdateGameBoySettings();
                }

                ImGui.SameLine();

                bool channel4 = settings.Data.Channel4Enabled;
                if (ImGui.Checkbox("Enable Channel 4", ref channel4))
                {
                    settings.Data.Channel4Enabled = channel4;
                    window.UpdateGameBoySettings();
                }

                ImGui.EndMenu();
            }

#if DEBUG
            if (ImGui.BeginMenu("Debug"))
            {
                if (ImGui.MenuItem("Trace Log", null, window.IsTraceLogEnabled))
                {
                    window.ToggleTraceLog();
                }

                ImGui.EndMenu();
            }
#endif

            if (ImGui.BeginMenu("Help"))
            {
                if (ImGui.MenuItem("About...", null, aboutWindowUI.IsVisible))
                {
                    aboutWindowUI.IsVisible = !aboutWindowUI.IsVisible;
                }

                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
        }

        fileBrowserUI.Render();
        aboutWindowUI.Render();
    }

    public void OpenFileBrowser()
    {
        fileBrowserUI.IsVisible = true;
    }
}