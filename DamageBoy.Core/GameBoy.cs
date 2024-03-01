using DamageBoy.Core.State;
using System;
using System.Diagnostics;
using System.Threading;

namespace DamageBoy.Core;

public enum EmulationStates
{
    Stopped,
    Running,
    Paused,
    Stopping
}

public enum FrameLimiterStates
{
    Limited,
    Unlimited,
    Paused
}

public class GameBoy
{
    readonly RAM ram;
    readonly VRAM vram;
    readonly InterruptHandler interruptHandler;
    readonly Serial serial;
    readonly DMA dma;
    readonly Timer timer;
    readonly APU apu;
    readonly PPU ppu;
    readonly IO io;
    readonly MMU mmu;
    readonly CPU cpu;

    readonly Cartridge cartridge;

    Action emulationStoppedCallback;

    public string GameTitle => cartridge.Title;

    public EmulationStates EmulationState { get; private set; }

    FrameLimiterStates frameLimiterState;

    public delegate void ScreenUpdateDelegate(byte[] pixels);
    public delegate void AddToAudioBufferDelegate(byte leftChannel, byte rightChannel);
    public delegate void SaveUpdateDelegate(byte[] saveData);

    public GameBoy(byte[] bootRom, byte[] romData, byte[] saveData, ScreenUpdateDelegate screenUpdateCallback, AddToAudioBufferDelegate addToAudioBufferCallback, SaveUpdateDelegate saveUpdateCallback)
    {
        cartridge = new Cartridge(romData, saveData, saveUpdateCallback);

        ram = new RAM();
        vram = new VRAM();
        interruptHandler = new InterruptHandler();
        serial = new Serial(interruptHandler);
        dma = new DMA(cartridge, ram, vram);
        timer = new Timer(interruptHandler);
        apu = new APU(addToAudioBufferCallback);
        ppu = new PPU(interruptHandler, vram, dma, screenUpdateCallback, ProcessSaveState);
        io = new IO(ppu, dma, timer, apu, serial, interruptHandler);
        mmu = new MMU(io, ram, ppu, dma, bootRom, cartridge);
        cpu = new CPU(mmu, bootRom != null);

        frameLimiterState = FrameLimiterStates.Limited;

        Thread thread = new Thread(MainLoop);
        thread.Name = "GameBoy Emulation Loop";
        thread.Start();
    }

    public void TogglePause()
    {
        if (EmulationState == EmulationStates.Running)
            EmulationState = EmulationStates.Paused;
        else if (EmulationState == EmulationStates.Paused)
            EmulationState = EmulationStates.Running;
    }

    public void Stop(Action emulationStoppedCallback)
    {
        if (EmulationState != EmulationStates.Running && EmulationState != EmulationStates.Paused) return;
        this.emulationStoppedCallback = emulationStoppedCallback;
        EmulationState = EmulationStates.Stopping;
    }

    void MainLoop()
    {
        EmulationState = EmulationStates.Running;
        Utils.Log(LogType.Info, "Emulation is now running.");

        Stopwatch sw = new Stopwatch();
        sw.Start();

        while (EmulationState == EmulationStates.Running || EmulationState == EmulationStates.Paused)
        {
            if (EmulationState == EmulationStates.Paused)
            {
                Thread.Sleep(32);
                continue;
            }

            if (frameLimiterState == FrameLimiterStates.Paused)
            {
                if (sw.ElapsedTicks < (4 * Stopwatch.Frequency) / CPU.CPU_CLOCKS) continue;
                sw.Restart();

                continue;
            }

            if (frameLimiterState == FrameLimiterStates.Limited)
            {
                if (sw.ElapsedTicks < (4 * Stopwatch.Frequency) / CPU.CPU_CLOCKS) continue;
                sw.Restart();
            }

#if !DEBUG
            try
            {
#endif
            timer.Update();
            apu.Update();
            ppu.Update();
            cpu.Update();
            serial.Update();
            dma.Update();
#if !DEBUG
            }
            catch (Exception ex)
            {
                Utils.Log(LogType.Error, ex.Message);
            }
#endif
        }

        cpu.Dispose();
        ppu.Dispose();
        cartridge.Dispose();

        EmulationState = EmulationStates.Stopped;
        Utils.Log(LogType.Info, "Emulation is now stopped.");

        emulationStoppedCallback?.Invoke();
    }

    public void SetInput(InputState inputState)
    {
        if (EmulationState != EmulationStates.Running) return;

        io.SetInput(Buttons.A, inputState.A);
        io.SetInput(Buttons.B, inputState.B);
        io.SetInput(Buttons.Select, inputState.Select);
        io.SetInput(Buttons.Start, inputState.Start);
        io.SetInput(Buttons.Up, inputState.Up);
        io.SetInput(Buttons.Right, inputState.Right);
        io.SetInput(Buttons.Down, inputState.Down);
        io.SetInput(Buttons.Left, inputState.Left);
    }

    public void SetFrameLimiterState(FrameLimiterStates state)
    {
        frameLimiterState = state;
    }

    #region Settings

    public bool Channel1Enabled { get => apu.Channel1Enabled; set => apu.Channel1Enabled = value; }
    public bool Channel2Enabled { get => apu.Channel2Enabled; set => apu.Channel2Enabled = value; }
    public bool Channel3Enabled { get => apu.Channel3Enabled; set => apu.Channel3Enabled = value; }
    public bool Channel4Enabled { get => apu.Channel4Enabled; set => apu.Channel4Enabled = value; }

    #endregion

    #region Save States

    SaveState saveState = null;
    SaveStateAction saveStateAction = SaveStateAction.None;
    string saveStateFileName = string.Empty;

    enum SaveStateAction { None, SavePending, LoadPending }

    public void SaveState(string fileName)
    {
        if (EmulationState != EmulationStates.Running) return;
        if (!io.BootROMDisabled) return;
        if (string.IsNullOrWhiteSpace(fileName)) return;

        saveStateFileName = fileName;
        saveStateAction = SaveStateAction.SavePending;
    }

    public void LoadState(string fileName)
    {
        if (EmulationState != EmulationStates.Running) return;
        if (!io.BootROMDisabled) return;
        if (string.IsNullOrWhiteSpace(fileName)) return;

        saveStateFileName = fileName;
        saveStateAction = SaveStateAction.LoadPending;
    }

    void InitializeSaveState()
    {
        if (saveState != null) return;

        // After any change in this array,
        // remember to increase SAVE_SATATE_FORMAT_VERSION in SaveState 

        IState[] componentsStates = new IState[]
        {
            cartridge,
            ram,
            vram,
            interruptHandler,
            serial,
            dma,
            timer,
            apu,
            ppu,
            io,
            cpu
        };

        saveState = new SaveState(componentsStates, cartridge);
    }

    void DoSaveState()
    {
        InitializeSaveState();
        bool success = saveState.Save(saveStateFileName);
        if (success) Utils.Log(LogType.Info, "Finished saving save state.");
    }

    void DoLoadState()
    {
        InitializeSaveState();
        bool success = saveState.Load(saveStateFileName);
        if (success) Utils.Log(LogType.Info, "Finished loading save state.");
    }

    void ProcessSaveState()
    {
        switch (saveStateAction)
        {
            case SaveStateAction.SavePending:
                DoSaveState();
                break;
            case SaveStateAction.LoadPending:
                DoLoadState();
                break;
        }

        saveStateAction = SaveStateAction.None;
    }

    #endregion

    #region TraceLog

#if DEBUG

    public bool IsTraceLogEnabled => cpu.IsTraceLogEnabled;

    public void ToggleTraceLog()
    {
        if (cpu.IsTraceLogEnabled)
            cpu.DisableTraceLog();
        else
            cpu.EnableTraceLog("LogDamageBoy.txt");
    }

#endif

    #endregion
}