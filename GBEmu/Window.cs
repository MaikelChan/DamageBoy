using GBEmu.Audio;
using GBEmu.Core;
using GBEmu.Graphics;
using GBEmu.UI;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.IO;
using System.IO.Compression;
using Sound = GBEmu.Audio.Sound;

namespace GBEmu
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

        string selectedRomFile = string.Empty;

        int lastMainMenuHeight = -1;

        bool isCloseRequested;
        bool isDisposed;

        string SaveFilePath => Path.Combine(SAVES_FOLDER, Path.GetFileNameWithoutExtension(selectedRomFile) + ".sav");
        const string SAVES_FOLDER = "Saves";

        const string BOOT_ROM_FILE_NAME = "gb_boot_rom";

        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
        {
            settings = new Settings();

            renderer = new Renderer();
            sound = new Sound();
            imguiController = new ImGuiController(renderer, ClientSize.X, ClientSize.Y);
            imguiInputData = new ImGuiInputData();

            mainUI = new MainUI(this, settings);
            mainUI.IsVisible = true;
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

        public void OpenROM(string romFile)
        {
            settings.Data.LastRomDirectory = Path.GetDirectoryName(romFile);
            selectedRomFile = romFile;
            StopEmulation();
            RunEmulation();
        }

        #region GameBoy

        public void RunEmulation()
        {
            if (gameBoy != null) return;

            if (string.IsNullOrWhiteSpace(selectedRomFile))
            {
                Utils.Log(LogType.Error, $"There's no ROM file opened.");
            }
            else
            {
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

                string extension = Path.GetExtension(selectedRomFile).ToLower();
                switch (extension)
                {
                    case ".gb":
                        romData = File.ReadAllBytes(selectedRomFile);
                        break;

                    case ".zip":
                        ZipArchive zip = ZipFile.OpenRead(selectedRomFile);
                        for (int z = 0; z < zip.Entries.Count; z++)
                        {
                            if (Path.GetExtension(zip.Entries[z].Name) == ".gb")
                            {
                                using (Stream s = zip.Entries[z].Open())
                                using (MemoryStream romStream = new MemoryStream())
                                {
                                    //romData = new byte[s.Length];
                                    //s.Read(romData, 0, romData.Length);
                                    s.CopyTo(romStream);
                                    romData = romStream.ToArray();
                                }
                            }
                        }
                        break;

                    default:
                        Utils.Log(LogType.Error, $"Extension \"{extension}\" is not supported.");
                        return;
                }

                if (romData == null)
                {
                    Utils.Log(LogType.Error, $"A valid GameBoy ROM has not been found in \"{selectedRomFile}\"");
                    return;
                }

                byte[] saveData = null;

                if (File.Exists(SaveFilePath))
                {
                    Utils.Log(LogType.Info, $"A file save for this ROM has been found at \"{SaveFilePath}\"");
                    saveData = File.ReadAllBytes(SaveFilePath);
                }

                Utils.Log(LogType.Info, $"ROM file successfully loaded: {selectedRomFile}");
                gameBoy = new GameBoy(bootRom, romData, saveData, ScreenUpdate, SoundUpdate, SaveUpdate);
                gameBoy.Run();
            }
        }

        public void StopEmulation()
        {
            if (gameBoy == null) return;

            sound.Stop();
            gameBoy.Stop();
            gameBoy = null;
        }

        public void SaveState()
        {
            if (gameBoy == null) return;
            gameBoy.SaveState();
        }

        public void LoadState()
        {
            if (gameBoy == null) return;
            gameBoy.LoadState();
        }

        #endregion

        #region GameBoy Callbacks

        void ScreenUpdate(byte[] pixels)
        {
            if (IsExiting) return;

            renderer.ScreenUpdate(pixels);
        }

        void SoundUpdate(SoundState soundState)
        {
            sound.Update(soundState);
        }

        void SaveUpdate(byte[] data)
        {
            if (!Directory.Exists(SAVES_FOLDER)) Directory.CreateDirectory(SAVES_FOLDER);
            File.WriteAllBytes(SaveFilePath, data);
            Utils.Log($"Saved data to {SaveFilePath}.");
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

            gameBoy?.Update();
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
                        //gameBoy.SaveState();
                        break;
                    case Keys.F7:
                        //gameBoy.LoadState();
                        break;
                    default:
                        gameBoy?.KeyDown(e.Key);
                        break;
                }
            }
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            base.OnKeyUp(e);

            gameBoy?.KeyUp(e.Key);
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