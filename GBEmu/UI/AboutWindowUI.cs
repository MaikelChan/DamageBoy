using ImGuiNET;
using System;
using System.Reflection;

namespace GBEmu.UI
{
    class AboutWindowUI : BaseUI
    {
        Version version;

        protected override void VisibilityChanged(bool isVisible)
        {
            base.VisibilityChanged(isVisible);

            if (isVisible)
            {
                version = Assembly.GetExecutingAssembly().GetName().Version;
            }
        }

        protected override void InternalRender()
        {
            if (!ImGui.Begin("About GBEmu", ref isVisible, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse))
            {
                ImGui.End();
                return;
            }

            ImGui.Text($"GBEmu v{version.Major}.{version.Minor}.{version.Build}");
            ImGui.Separator();
            ImGui.Text("By PacoChan.");
            ImGui.Text("Experimental GameBoy emulator written in C#, and it uses OpenGL for rendering, OpenAL for audio and ImGui for UI.");

            ImGui.End();
        }
    }
}