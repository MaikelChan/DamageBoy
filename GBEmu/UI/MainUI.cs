﻿using ImGuiNET;
using GBEmu.Core;

namespace GBEmu.UI
{
    class MainUI : BaseUI
    {
        readonly Window window;
        readonly Settings settings;
        //readonly GameBoy chip8;

        readonly FileBrowserUI fileBrowserUI;
        readonly AboutWindowUI aboutWindowUI;
        //readonly DebugWindowUI debugWindowUI;

        public int MainMenuHeight { get; private set; }

        public MainUI(Window window, Settings settings)
        {
            this.window = window;
            this.settings = settings;
            //this.chip8 = chip8;

            GetSettings();

            fileBrowserUI = new FileBrowserUI("Open ROM", settings.Data.LastRomDirectory, ".gb|.zip", false, window.OpenROM);
            aboutWindowUI = new AboutWindowUI();
            //debugWindowUI = new DebugWindowUI(chip8);
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

                    ImGui.Separator();

                    if (ImGui.MenuItem("Quit", "Ctrl+Q"))
                    {
                        window.RequestClose();
                    }

                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Emulation"))
                {
                    if (ImGui.MenuItem("Run", "F1"/*, chip8.EmulationState == EmulationStates.Running*/))
                    {
                        window.RunEmulation();
                    }

                    if (ImGui.MenuItem("Stop", "F2"/*, chip8.EmulationState == EmulationStates.Stopped*/))
                    {
                        window.StopEmulation();
                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem("Save State", "F5"))
                    {
                        //chip8.SaveState();
                    }

                    if (ImGui.MenuItem("Load State", "F7"))
                    {
                        //chip8.LoadState();
                    }

                    //ImGui.Separator();

                    //if (ImGui.MenuItem("Debug...", null, debugWindowUI.IsVisible))
                    //{
                    //    debugWindowUI.IsVisible = !debugWindowUI.IsVisible;
                    //}

                    ImGui.EndMenu();
                }

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
            //debugWindowUI.Render();
            aboutWindowUI.Render();
        }

        public void OpenFileBrowser()
        {
            fileBrowserUI.IsVisible = true;
        }

        void SetSettings()
        {
            //settings.Data.Alternative8xy6Opcode = alt8xy6Opcode;
            //settings.Data.AlternativeFx55Opcode = altFx55Opcode;

            //chip8.SetSettings(alt8xy6Opcode, altFx55Opcode);
        }

        void GetSettings()
        {
            //alt8xy6Opcode = settings.Data.Alternative8xy6Opcode;
            //altFx55Opcode = settings.Data.AlternativeFx55Opcode;

            //chip8.SetSettings(alt8xy6Opcode, altFx55Opcode);
        }
    }
}