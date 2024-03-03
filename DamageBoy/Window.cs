using DamageBoy.Core;
using DamageBoy.Graphics;
using DamageBoy.Properties;
using DamageBoy.UI;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using Image = OpenTK.Windowing.Common.Input.Image;
using Sound = DamageBoy.Audio.Sound;

namespace DamageBoy;

class Window : GameWindow
{
    readonly Settings settings;

    readonly BaseRenderer renderer;
    readonly Sound sound;
    readonly ImGuiController imguiController;

    readonly MainUI mainUI;

    public bool IsGameBoyRunning => gameBoy != null;
    public bool IsGameBoyPaused => gameBoy != null && gameBoy.EmulationState == EmulationStates.Paused;

    GameBoy gameBoy;

    int lastMainMenuHeight = -1;

    bool isCloseRequested;
    bool isDisposed;

    EmulationStates stateBeforeMinimized;

    string SaveFilePath => Path.Combine(SAVES_FOLDER, Path.GetFileNameWithoutExtension(selectedRomFileName) + ".sav");

    const string SAVES_FOLDER = "Saves";

    const string DMG_BOOT_ROM_FILE_NAME = "dmg_boot_rom";
    const string CGB_BOOT_ROM_FILE_NAME = "cgb_boot_rom";

    public const string GB_FILE_EXTENSION = ".gb";
    public const string GBC_FILE_EXTENSION = ".gbc";
    public const string ZIP_FILE_EXTENSION = ".zip";

    public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
    {
        settings = new Settings();
        CleanupRecentROMs();

        renderer = new Renderer(settings);
        sound = new Sound(settings, AudioBufferStateChanged);
        imguiController = new ImGuiController(renderer, ClientSize.X, ClientSize.Y);
        imguiInputData = new ImGuiInputData();

        mainUI = new MainUI(this, renderer, settings);
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
        StopEmulation(() =>
        {
            selectedRomFileName = romFileName;
            if (!RunEmulation()) return;

            settings.Data.LastRomDirectory = Path.GetDirectoryName(romFileName);
            AddRecentROM(romFileName);
            settings.Save();

            SetWindowTitle();
        });
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
        byte[] compressedIcon = Resources.WindowIcon;
        byte[] icon;

        using (MemoryStream compressedStream = new MemoryStream(compressedIcon))
        using (BrotliStream decompressionStream = new BrotliStream(compressedStream, CompressionMode.Decompress))
        using (MemoryStream decompressedStream = new MemoryStream())
        {
            decompressionStream.CopyTo(decompressedStream);
            icon = decompressedStream.ToArray();
        }

        Image image = new Image(16, 16, icon);
        Icon = new WindowIcon(image);
    }

    #region GameBoy

#if DEBUG

    public bool IsTraceLogEnabled
    {
        get
        {
            if (gameBoy != null) return gameBoy.IsTraceLogEnabled;
            return false;
        }
    }

#endif

    public bool RunEmulation()
    {
        if (gameBoy != null) return false;

        if (string.IsNullOrWhiteSpace(selectedRomFileName))
        {
            Utils.Log(LogType.Error, $"There's no ROM file opened.");
            return false;
        }

        byte[] bootRom = null;

        string bootRomName = settings.Data.HardwareType == HardwareTypes.CGB ? CGB_BOOT_ROM_FILE_NAME : DMG_BOOT_ROM_FILE_NAME;
        if (File.Exists(bootRomName))
        {
            bootRom = File.ReadAllBytes(bootRomName);

            ushort bootRomSize = settings.Data.HardwareType == HardwareTypes.CGB ? GameBoy.CGB_BOOT_ROM_SIZE : GameBoy.DMG_BOOT_ROM_SIZE;

            if (bootRom.Length != bootRomSize)
            {
                Utils.Log(LogType.Error, $"The boot ROM is {bootRom.Length} bytes, but it should be {bootRomSize}. Ignoring it.");
                bootRom = null;
            }
        }

        byte[] romData = null;

        string extension = Path.GetExtension(selectedRomFileName).ToLower();
        switch (extension)
        {
            case GB_FILE_EXTENSION:
            case GBC_FILE_EXTENSION:
                romData = File.ReadAllBytes(selectedRomFileName);
                break;

            case ZIP_FILE_EXTENSION:
                using (ZipArchive zip = ZipFile.OpenRead(selectedRomFileName))
                {
                    for (int z = 0; z < zip.Entries.Count; z++)
                    {
                        string entryExtension = Path.GetExtension(zip.Entries[z].Name);
                        if (entryExtension == GB_FILE_EXTENSION || entryExtension == GBC_FILE_EXTENSION)
                        {
                            using (Stream s = zip.Entries[z].Open())
                            using (MemoryStream romStream = new MemoryStream())
                            {
                                s.CopyTo(romStream);
                                romData = romStream.ToArray();
                            }
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
            Utils.Log(LogType.Error, $"A valid GameBoy ROM has not been found in \"{selectedRomFileName}\".");
            return false;
        }

        byte[] saveData = null;

        if (File.Exists(SaveFilePath))
        {
            Utils.Log(LogType.Info, $"A file save for this ROM has been found at \"{SaveFilePath}\".");
            saveData = File.ReadAllBytes(SaveFilePath);
        }

        Utils.Log(LogType.Info, $"ROM file successfully loaded: {selectedRomFileName}.");

        try
        {
            gameBoy = new GameBoy(settings.Data.HardwareType, bootRom, romData, saveData, ScreenUpdate, AddToAudioBuffer, SaveUpdate);
            (renderer as Renderer).RenderMode = Renderer.RenderModes.LCD;

            UpdateGameBoySettings();
            sound.Start();

            return true;
        }
        catch (Exception ex)
        {
            Utils.Log(LogType.Error, ex.Message);
            return false;
        }
    }

    public void TogglePauseEmulation()
    {
        if (gameBoy == null) return;

        gameBoy.TogglePause();
        if (gameBoy.EmulationState == EmulationStates.Paused)
            sound.Stop();
        else if (gameBoy.EmulationState == EmulationStates.Running)
            sound.Start();
    }

    public void StopEmulation(Action emulationStoppedCallback = null)
    {
        if (gameBoy != null)
        {
            gameBoy.Stop(() =>
            {
                gameBoy = null;
                sound.Stop();

                (renderer as Renderer).RenderMode = Renderer.RenderModes.Logo;

                emulationStoppedCallback?.Invoke();
            });
        }
        else
        {
            emulationStoppedCallback?.Invoke();
        }
    }

    public void UpdateGameBoySettings()
    {
        if (gameBoy == null) return;

        gameBoy.Channel1Enabled = settings.Data.Channel1Enabled;
        gameBoy.Channel2Enabled = settings.Data.Channel2Enabled;
        gameBoy.Channel3Enabled = settings.Data.Channel3Enabled;
        gameBoy.Channel4Enabled = settings.Data.Channel4Enabled;
    }

#if DEBUG
    public void ToggleTraceLog()
    {
        gameBoy?.ToggleTraceLog();
    }
#endif

    void AudioBufferStateChanged(Sound.BufferStates audioBufferState)
    {
        if (disableFrameLimit)
        {
            gameBoy?.SetFrameLimiterState(FrameLimiterStates.Unlimited);
        }
        else
        {
            switch (audioBufferState)
            {
                case Sound.BufferStates.Uninitialized:
                case Sound.BufferStates.Ok:
                    gameBoy?.SetFrameLimiterState(FrameLimiterStates.Limited);
                    break;
                case Sound.BufferStates.Underrun:
                    gameBoy?.SetFrameLimiterState(FrameLimiterStates.Unlimited);
                    break;
                case Sound.BufferStates.Overrun:
                    gameBoy?.SetFrameLimiterState(FrameLimiterStates.Paused);
                    break;
            }
        }
    }

    #endregion

    #region GameBoy Callbacks

    void ScreenUpdate(byte[] pixels)
    {
        if (IsExiting) return;

        renderer.ScreenUpdate(pixels);
    }

    void AddToAudioBuffer(byte leftValue, byte rightValue)
    {
        if (IsExiting) return;

        sound.AddToAudioBuffer(leftValue, rightValue);
    }

    void SaveUpdate(byte[] data)
    {
        if (!Directory.Exists(SAVES_FOLDER)) Directory.CreateDirectory(SAVES_FOLDER);
        File.WriteAllBytes(SaveFilePath, data);
        Utils.Log($"Saved data to {SaveFilePath}.");
    }

    #endregion

    #region Input

    const byte BUTTON_A = 0;
    const byte BUTTON_X = 2;
    const byte BUTTON_BACK = 6;
    const byte BUTTON_START = 7;
    const byte BUTTON_RIGHT_THUMB = 10;
    const byte BUTTON_UP = 11;
    const byte BUTTON_RIGHT = 12;
    const byte BUTTON_DOWN = 13;
    const byte BUTTON_LEFT = 14;

    const float DEADZONE = 0.65f;

    bool disableFrameLimit = false;

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

        byte pressed = (byte)InputAction.Press;

        InputState inputState = new InputState()
        {
            A = KeyboardState.IsKeyDown(Keys.X) || gamepadState.Buttons[BUTTON_A] == pressed,
            B = KeyboardState.IsKeyDown(Keys.Z) || gamepadState.Buttons[BUTTON_X] == pressed,
            Select = KeyboardState.IsKeyDown(Keys.RightShift) || gamepadState.Buttons[BUTTON_BACK] == pressed,
            Start = KeyboardState.IsKeyDown(Keys.Enter) || gamepadState.Buttons[BUTTON_START] == pressed,
            Up = KeyboardState.IsKeyDown(Keys.Up) || gamepadState.Buttons[BUTTON_UP] == pressed || gamepadState.Axes[1] < -DEADZONE,
            Right = KeyboardState.IsKeyDown(Keys.Right) || gamepadState.Buttons[BUTTON_RIGHT] == pressed || gamepadState.Axes[0] > DEADZONE,
            Down = KeyboardState.IsKeyDown(Keys.Down) || gamepadState.Buttons[BUTTON_DOWN] == pressed || gamepadState.Axes[1] > DEADZONE,
            Left = KeyboardState.IsKeyDown(Keys.Left) || gamepadState.Buttons[BUTTON_LEFT] == pressed || gamepadState.Axes[0] < -DEADZONE
        };

        gameBoy?.SetInput(inputState);

        disableFrameLimit = KeyboardState.IsKeyDown(Keys.Space) || gamepadState.Buttons[BUTTON_RIGHT_THUMB] == pressed;
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

        if (WindowState == WindowState.Minimized)
        {
            Thread.Sleep(32);
            return;
        }

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

        if (WindowState == WindowState.Minimized)
        {
            Thread.Sleep(32);
            return;
        }

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

    protected override void OnMinimized(MinimizedEventArgs e)
    {
        base.OnMinimized(e);

        if (!settings.Data.PauseWhileMinimized) return;

        if (gameBoy == null) return;

        EmulationStates currentState = gameBoy.EmulationState;
        if (currentState == EmulationStates.Stopping || currentState == EmulationStates.Stopped) return;

        if (e.IsMinimized)
        {
            stateBeforeMinimized = currentState;
            if (currentState == EmulationStates.Running) TogglePauseEmulation();
        }
        else
        {
            if (stateBeforeMinimized == EmulationStates.Running) TogglePauseEmulation();
        }
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
                    TogglePauseEmulation();
                    break;
                case Keys.F3:
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