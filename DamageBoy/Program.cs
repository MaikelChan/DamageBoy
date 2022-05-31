using DamageBoy.Core;
using DamageBoy.UI;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;
using System.Collections.Generic;

namespace DamageBoy;

class Program
{
    const int INITIAL_SIZE_MULTIPLIER = 6;
    const int INITIAL_WIDTH = (int)((INITIAL_HEIGHT - MainUI.MAIN_MENU_DEFAULT_HEIGHT) * Constants.ASPECT_RATIO);
    const int INITIAL_HEIGHT = (Constants.RES_Y * INITIAL_SIZE_MULTIPLIER) + MainUI.MAIN_MENU_DEFAULT_HEIGHT;

    static void Main(string[] args)
    {
        GameWindowSettings gameWindowSettings = new GameWindowSettings()
        {
            UpdateFrequency = 0.0f,
            RenderFrequency = 0.0f
        };

        NativeWindowSettings nativeWindowSettings = new NativeWindowSettings()
        {
            API = ContextAPI.OpenGL,
            APIVersion = new Version(3, 3),
            Profile = ContextProfile.Core,
#if DEBUG
            Flags = ContextFlags.Debug,
#else
            Flags = ContextFlags.ForwardCompatible,
#endif
            Size = new Vector2i(INITIAL_WIDTH, INITIAL_HEIGHT),
            WindowState = WindowState.Normal
        };

        List<MonitorInfo> monitors = Monitors.GetMonitors();
        Utils.Log(LogType.Info, $"Monitors: {monitors.Count}");

        MonitorInfo primaryMonitor = Monitors.GetPrimaryMonitor();

        Box2i area = primaryMonitor.ClientArea;
        int x = (area.Min.X + area.Max.X - INITIAL_WIDTH) >> 1;
        int y = (area.Min.Y + area.Max.Y - INITIAL_HEIGHT) >> 1;
        nativeWindowSettings.Location = new Vector2i(x, y);

        Utils.Log(LogType.Info, $"Primary monitor: {primaryMonitor.HorizontalResolution}x{primaryMonitor.VerticalResolution} pixels - {primaryMonitor.HorizontalDpi}x{primaryMonitor.VerticalDpi} dpi");

        using (Window window = new Window(gameWindowSettings, nativeWindowSettings))
        {
            window.VSync = VSyncMode.On;
            window.Run();
        }
    }
}
