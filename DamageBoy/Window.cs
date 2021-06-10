using DamageBoy.Core;
using DamageBoy.Graphics;
using DamageBoy.UI;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using Image = OpenTK.Windowing.Common.Input.Image;
using Sound = DamageBoy.Audio.Sound;

namespace DamageBoy
{
    class Window : GameWindow
    {
        readonly Settings settings;

        readonly BaseRenderer renderer;
        readonly Sound sound;
        readonly ImGuiController imguiController;

        readonly MainUI mainUI;

        public bool IsGameBoyRunning => gameBoy != null;

        GameBoy gameBoy;

        int lastMainMenuHeight = -1;

        bool isCloseRequested;
        bool isDisposed;

        string SaveFilePath => Path.Combine(SAVES_FOLDER, Path.GetFileNameWithoutExtension(selectedRomFileName) + ".sav");
        const string SAVES_FOLDER = "Saves";

        const string BOOT_ROM_FILE_NAME = "dmg_boot_rom";

        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
        {
            settings = new Settings();
            CleanupRecentROMs();

            renderer = new Renderer();
            sound = new Sound();
            imguiController = new ImGuiController(renderer, ClientSize.X, ClientSize.Y);
            imguiInputData = new ImGuiInputData();

            mainUI = new MainUI(this, settings);
            mainUI.IsVisible = true;

            SetWindowTitle();
            SetWindowIcon();
        }

        protected override void Dispose(bool disposing)
        {
            if (isDisposed) return;
            isDisposed = true;

            if (disposing)
            {
                StopEmulation();

                settings.Save();

                renderer.Dispose();
                sound.Dispose();
                imguiController.Dispose();
            }

            base.Dispose(disposing);
        }

        public void RequestClose()
        {
            isCloseRequested = true;
        }

        public void OpenROM(string romFileName)
        {
            StopEmulation();
            selectedRomFileName = romFileName;
            if (!RunEmulation()) return;

            settings.Data.LastRomDirectory = Path.GetDirectoryName(romFileName);
            AddRecentROM(romFileName);
            settings.Save();

            SetWindowTitle();
        }

        void SetWindowTitle()
        {
            string gameName = gameBoy != null ? gameBoy.GameTitle : "(No game loaded)";
            string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            Title = $"{assemblyName} v{version.Major}.{version.Minor}.{version.Build}   -   {gameName}";
        }

        void SetWindowIcon()
        {
            const int width = 16;
            const int height = 16;
            const int pixelCount = width * height;
            const int size = width * height * 4;

            BitmapData bData = Resources.WindowIcon.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, Resources.WindowIcon.PixelFormat);
            byte[] pixels = new byte[size];
            Marshal.Copy(bData.Scan0, pixels, 0, size);
            // Resources.WindowIcon.UnlockBits(bData);

            for (int p = 0; p < pixelCount; p++)
            {
                byte b = pixels[p * 4 + 0];
                byte g = pixels[p * 4 + 1];
                byte r = pixels[p * 4 + 2];
                byte a = pixels[p * 4 + 3];

                pixels[p * 4 + 0] = r;
                pixels[p * 4 + 1] = g;
                pixels[p * 4 + 2] = b;
                pixels[p * 4 + 3] = a;
            }

            Image image = new Image(width, height, pixels);
            Icon = new WindowIcon(image);
        }

        #region GameBoy

        public bool IsTraceLogEnabled
        {
            get
            {
                if (gameBoy != null) return gameBoy.IsTraceLogEnabled;
                return false;
            }
        }

        public bool RunEmulation()
        {
            if (gameBoy != null) return false;

            if (string.IsNullOrWhiteSpace(selectedRomFileName))
            {
                Utils.Log(LogType.Error, $"There's no ROM file opened.");
                return false;
            }

            byte[] bootRom = null;

            if (File.Exists(BOOT_ROM_FILE_NAME))
            {
                bootRom = File.ReadAllBytes(BOOT_ROM_FILE_NAME);

                if (bootRom.Length != 256)
                {
                    Utils.Log(LogType.Error, $"The boot ROM is {bootRom.Length} bytes, but it should be 256. Ignoring it.");
                    bootRom = null;
                }
            }

            byte[] romData = null;

            string extension = Path.GetExtension(selectedRomFileName).ToLower();
            switch (extension)
            {
                case ".gb":
                    romData = File.ReadAllBytes(selectedRomFileName);
                    break;

                case ".zip":
                    ZipArchive zip = ZipFile.OpenRead(selectedRomFileName);
                    for (int z = 0; z < zip.Entries.Count; z++)
                    {
                        if (Path.GetExtension(zip.Entries[z].Name) == ".gb")
                        {
                            using (Stream s = zip.Entries[z].Open())
                            using (MemoryStream romStream = new MemoryStream())
                            {
                                s.CopyTo(romStream);
                                romData = romStream.ToArray();
                            }
                        }
                    }
                    break;

                default:
                    Utils.Log(LogType.Error, $"Extension \"{extension}\" is not supported.");
                    return false;
            }

            if (romData == null)
            {
                Utils.Log(LogType.Error, $"A valid GameBoy ROM has not been found in \"{selectedRomFileName}\"");
                return false;
            }

            byte[] saveData = null;

            if (File.Exists(SaveFilePath))
            {
                Utils.Log(LogType.Info, $"A file save for this ROM has been found at \"{SaveFilePath}\"");
                saveData = File.ReadAllBytes(SaveFilePath);
            }

            Utils.Log(LogType.Info, $"ROM file successfully loaded: {selectedRomFileName}");

            try
            {
                gameBoy = new GameBoy(bootRom, romData, saveData, ScreenUpdate, SoundUpdate, SaveUpdate);
            }
            catch (Exception ex)
            {
                Utils.Log(LogType.Error, ex.Message);
            }

            return true;
        }

        public void StopEmulation()
        {
            if (gameBoy == null) return;

            gameBoy.Dispose();
            gameBoy = null;
            sound.Stop();
        }

        public void ToggleTraceLog()
        {
            if (gameBoy == null) return;
            gameBoy.ToggleTraceLog();
        }

        #endregion

        #region GameBoy Callbacks

        void ScreenUpdate(byte[] pixels)
        {
            if (IsExiting) return;

            renderer.ScreenUpdate(pixels);
        }

        void SoundUpdate(ushort[] data)
        {
            if (IsExiting) return;

            Sound.BufferStates bufferState = sound.Update(data);

            switch (bufferState)
            {
                case Sound.BufferStates.Uninitialized:
                case Sound.BufferStates.Ok:
                    gameBoy.SetFrameLimiterState(FrameLimiterStates.Limited);
                    break;
                case Sound.BufferStates.Underrun:
                    gameBoy.SetFrameLimiterState(FrameLimiterStates.Unlimited);
                    break;
                case Sound.BufferStates.Overrun:
                    gameBoy.SetFrameLimiterState(FrameLimiterStates.Paused);
                    break;
            }
        }

        void SaveUpdate(byte[] data)
        {
            if (!Directory.Exists(SAVES_FOLDER)) Directory.CreateDirectory(SAVES_FOLDER);
            File.WriteAllBytes(SaveFilePath, data);
            Utils.Log($"Saved data to {SaveFilePath}.");
        }

        #endregion

        #region Input

        const float DEADZONE = 0.65f;

        unsafe void ProcessInput()
        {
            GamepadState gamepadState = default;

            for (int i = 0; i < 4; i++)
            {
                if (GLFW.JoystickIsGamepad(i) && GLFW.GetGamepadState(i, out gamepadState))
                {
                    break;
                }
            }

            InputState inputState = new InputState()
            {
                A = KeyboardState.IsKeyDown(Keys.X) || gamepadState.Buttons[0] > 0,
                B = KeyboardState.IsKeyDown(Keys.Z) || gamepadState.Buttons[2] > 0,
                Select = KeyboardState.IsKeyDown(Keys.RightShift) || gamepadState.Buttons[6] > 0,
                Start = KeyboardState.IsKeyDown(Keys.Enter) || gamepadState.Buttons[7] > 0,
                Up = KeyboardState.IsKeyDown(Keys.Up) || gamepadState.Buttons[11] > 0 || gamepadState.Axes[1] < -DEADZONE,
                Right = KeyboardState.IsKeyDown(Keys.Right) || gamepadState.Buttons[12] > 0 || gamepadState.Axes[0] > DEADZONE,
                Down = KeyboardState.IsKeyDown(Keys.Down) || gamepadState.Buttons[13] > 0 || gamepadState.Axes[1] > DEADZONE,
                Left = KeyboardState.IsKeyDown(Keys.Left) || gamepadState.Buttons[14] > 0 || gamepadState.Axes[0] < -DEADZONE
            };

            gameBoy?.SetInput(inputState);
        }


        #endregion

        #region Save States

        string SaveStateFilePath => Path.Combine(SAVE_STATES_FOLDER, Path.GetFileNameWithoutExtension(selectedRomFileName) + ".sst");
        const string SAVE_STATES_FOLDER = "SaveStates";

        public void SaveState()
        {
            if (gameBoy == null) return;
            if (!Directory.Exists(SAVE_STATES_FOLDER)) Directory.CreateDirectory(SAVE_STATES_FOLDER);
            gameBoy.SaveState(SaveStateFilePath);
        }

        public void LoadState()
        {
            if (gameBoy == null) return;
            if (!Directory.Exists(SAVE_STATES_FOLDER)) Directory.CreateDirectory(SAVE_STATES_FOLDER);
            gameBoy.LoadState(SaveStateFilePath);
        }

        #endregion

        #region Window events

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            if (IsExiting) return;

            //if (WindowState == WindowState.Minimized || !IsFocused)
            //{
            //    //Thread.Sleep(50);
            //    return;
            //}

            ProcessInput();
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            if (isCloseRequested)
            {
                isCloseRequested = false;
                Close();
                return;
            }

            if (IsExiting) return;

            //if (WindowState == WindowState.Minimized || !IsFocused)
            //{
            //    //Thread.Sleep(50);
            //    return;
            //}

            ProcessUI((float)args.Time);

            if (lastMainMenuHeight != mainUI.MainMenuHeight)
            {
                lastMainMenuHeight = mainUI.MainMenuHeight;
                renderer.Resize(ClientSize.X, ClientSize.Y - mainUI.MainMenuHeight);
            }

            renderer.Render(args.Time);
            imguiController.Render();

            SwapBuffers();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            if (IsExiting) return;

            renderer.Resize(e.Width, e.Height - mainUI.MainMenuHeight);
            imguiController.WindowResized(e.Width, e.Height);
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Control)
            {
                switch (e.Key)
                {
                    case Keys.O:
                        mainUI.OpenFileBrowser();
                        break;
                    case Keys.Q:
                        RequestClose();
                        break;
                }
            }
            else
            {
                switch (e.Key)
                {
                    case Keys.F1:
                        RunEmulation();
                        break;
                    case Keys.F2:
                        StopEmulation();
                        break;
                    case Keys.F5:
                        SaveState();
                        break;
                    case Keys.F7:
                        LoadState();
                        break;
                }
            }
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);

            imguiController.PressChar((char)e.Unicode);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            imguiController.MouseScroll(e.Offset);
        }

        #endregion

        #region Current / Recent ROMs

        string selectedRomFileName = string.Empty;

        public const int MAX_RECENT_ROMS = 10;

        public void CleanupRecentROMs()
        {
            List<string> cleanedRecentROMs = new List<string>(10);

            for (int r = 0; r < settings.Data.RecentRoms.Count; r++)
            {
                if (string.IsNullOrWhiteSpace(settings.Data.RecentRoms[r])) continue;
                if (!File.Exists(settings.Data.RecentRoms[r])) continue;
                if (!cleanedRecentROMs.Contains(settings.Data.RecentRoms[r]))
                {
                    cleanedRecentROMs.Add(settings.Data.RecentRoms[r]);
                    if (cleanedRecentROMs.Count == MAX_RECENT_ROMS) break;
                }
            }

            settings.Data.RecentRoms = cleanedRecentROMs;
        }

        void AddRecentROM(string romFileName)
        {
            settings.Data.RecentRoms.Remove(romFileName);
            settings.Data.RecentRoms.Insert(0, romFileName);
            CleanupRecentROMs();
        }

        #endregion

        #region ImGui

        readonly ImGuiInputData imguiInputData;

        void ProcessUI(float deltaTime)
        {
            ProcessImGuiInput();
            imguiController.Update(imguiInputData, deltaTime);

            //ImGuiNET.ImGui.ShowDemoWindow();
            mainUI.Render();
        }

        void ProcessImGuiInput()
        {
            MouseState mouseState = MouseState;
            KeyboardState keyboardState = KeyboardState;

            imguiInputData.LeftMouseButtonDown = mouseState.IsButtonDown(MouseButton.Left);
            imguiInputData.RightMouseButtonDown = mouseState.IsButtonDown(MouseButton.Right);
            imguiInputData.MiddleMouseButtonDown = mouseState.IsButtonDown(MouseButton.Middle);
            imguiInputData.MousePosition = new Vector2i((int)mouseState.X, (int)mouseState.Y);

            imguiInputData.KeyTab = keyboardState.IsKeyDown(Keys.Tab);
            imguiInputData.KeyLeftArrow = keyboardState.IsKeyDown(Keys.Left);
            imguiInputData.KeyRightArrow = keyboardState.IsKeyDown(Keys.Right);
            imguiInputData.KeyUpArrow = keyboardState.IsKeyDown(Keys.Up);
            imguiInputData.KeyDownArrow = keyboardState.IsKeyDown(Keys.Down);
            imguiInputData.KeyPageUp = keyboardState.IsKeyDown(Keys.PageUp);
            imguiInputData.KeyPageDown = keyboardState.IsKeyDown(Keys.PageDown);
            imguiInputData.KeyHome = keyboardState.IsKeyDown(Keys.Home);
            imguiInputData.KeyEnd = keyboardState.IsKeyDown(Keys.End);
            imguiInputData.KeyInsert = keyboardState.IsKeyDown(Keys.Insert);
            imguiInputData.KeyDelete = keyboardState.IsKeyDown(Keys.Delete);
            imguiInputData.KeyBackspace = keyboardState.IsKeyDown(Keys.Backspace);
            imguiInputData.KeySpace = keyboardState.IsKeyDown(Keys.Space);
            imguiInputData.KeyEnter = keyboardState.IsKeyDown(Keys.Enter);
            imguiInputData.KeyEscape = keyboardState.IsKeyDown(Keys.Escape);
            imguiInputData.KeyKeyPadEnter = keyboardState.IsKeyDown(Keys.KeyPadEnter);
            imguiInputData.KeyA = keyboardState.IsKeyDown(Keys.A);
            imguiInputData.KeyC = keyboardState.IsKeyDown(Keys.C);
            imguiInputData.KeyV = keyboardState.IsKeyDown(Keys.V);
            imguiInputData.KeyX = keyboardState.IsKeyDown(Keys.X);
            imguiInputData.KeyY = keyboardState.IsKeyDown(Keys.Y);
            imguiInputData.KeyZ = keyboardState.IsKeyDown(Keys.Z);

            imguiInputData.KeyCtrl = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
            imguiInputData.KeyAlt = keyboardState.IsKeyDown(Keys.LeftAlt);
            imguiInputData.KeyShift = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
            imguiInputData.KeySuper = keyboardState.IsKeyDown(Keys.LeftSuper) || keyboardState.IsKeyDown(Keys.RightSuper);
        }

        #endregion
    }
}