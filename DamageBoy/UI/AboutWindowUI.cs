using ImGuiNET;
using System;
using System.Reflection;

namespace DamageBoy.UI
{
    class AboutWindowUI : BaseUI
    {
        Version version;
        string name;

        protected override void VisibilityChanged(bool isVisible)
        {
            base.VisibilityChanged(isVisible);

            if (isVisible)
            {
                version = Assembly.GetExecutingAssembly().GetName().Version;
                name = Assembly.GetExecutingAssembly().GetName().Name;
            }
        }

        protected override void InternalRender()
        {
            if (!ImGui.Begin($"About {name}", ref isVisible, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse))
            {
                ImGui.End();
                return;
            }

            ImGui.Text($"{name} v{version.Major}.{version.Minor}.{version.Build}");
            ImGui.Separator();
            ImGui.Text("By PacoChan.");
            ImGui.Text("Experimental GameBoy emulator written in C#, and it uses OpenGL for rendering, OpenAL for audio and ImGui for UI.");

            ImGui.End();
        }
    }
}