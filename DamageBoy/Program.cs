using DamageBoy.Core;
using DamageBoy.UI;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;

namespace DamageBoy
{
    class Program
    {
        const int INITIAL_SIZE_MULTIPLIER = 6;
        const int INITIAL_WIDTH = (int)((INITIAL_HEIGHT - MainUI.MAIN_MENU_DEFAULT_HEIGHT) * Constants.ASPECT_RATIO);
        const int INITIAL_HEIGHT = (Constants.RES_Y * INITIAL_SIZE_MULTIPLIER) + MainUI.MAIN_MENU_DEFAULT_HEIGHT;

        static void Main(string[] args)
        {
            GameWindowSettings gameWindowSettings = new GameWindowSettings()
            {
                IsMultiThreaded = false,
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
                IsFullscreen = false,
            };

            Monitors.BuildMonitorCache();
            Utils.Log(LogType.Info, $"Monitors: {Monitors.Count}");

            if (Monitors.TryGetMonitorInfo(nativeWindowSettings.CurrentMonitor, out MonitorInfo monitorInfo) ||
                Monitors.TryGetMonitorInfo(0, out monitorInfo))
            {
                Box2i area = monitorInfo.ClientArea;
                int x = (area.Min.X + area.Max.X - INITIAL_WIDTH) >> 1;
                int y = (area.Min.Y + area.Max.Y - INITIAL_HEIGHT) >> 1;
                nativeWindowSettings.Location = new Vector2i(x, y);

                Utils.Log(LogType.Info, $"Primary monitor: {monitorInfo.HorizontalResolution}x{monitorInfo.VerticalResolution} pixels - {monitorInfo.HorizontalDpi}x{monitorInfo.VerticalDpi} dpi");
            }
            else
            {
                Utils.Log(LogType.Warning, "Couldn't get primary monitor info. Setting window at default location.");
            }

            using (Window window = new Window(gameWindowSettings, nativeWindowSettings))
            {
                window.VSync = VSyncMode.On;
                window.Run();
            }
        }
    }
}
