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

public enum HardwareTypes
{
    DMG,
    CGB
}

public enum GameTypes
{
    GameBoy,
    GameBoyColorBackwardsCompatible,
    GameBoyColor
}

public class GameBoy
{
    readonly WRAM wram;
    readonly HRAM hram;
    readonly VRAM vram;
    readonly OAM oam;
    readonly InterruptHandler interruptHandler;
    readonly Serial serial;
    readonly DMA dma;
    readonly HDMA hdma;
    readonly Timer timer;
    readonly APU apu;
    readonly PPU ppu;
    readonly IO io;
    readonly MMU mmu;
    readonly CPU cpu;

    readonly Cartridge cartridge;

    Action emulationStoppedCallback;

    public string GameTitle => cartridge.Title;

    public HardwareTypes HardwareType { get; }
    public GameTypes GameType => cartridge.Type;
    public bool IsGameGbcCompatible => cartridge.Type != GameTypes.GameBoy;
    public bool IsColorMode => HardwareType == HardwareTypes.CGB && cartridge.Type != GameTypes.GameBoy;

    public EmulationStates EmulationState { get; private set; }

    FrameLimiterStates frameLimiterState;

    public delegate void ScreenUpdateDelegate(ushort[] pixels);
    public delegate void AddToAudioBufferDelegate(byte leftChannel, byte rightChannel);
    public delegate void SaveUpdateDelegate(byte[] saveData);

    public const ushort DMG_BOOT_ROM_START_ADDRESS = 0x0;
    public const ushort DMG_BOOT_ROM_END_ADDRESS = 0x100;
    public const ushort DMG_BOOT_ROM_SIZE = DMG_BOOT_ROM_END_ADDRESS - DMG_BOOT_ROM_START_ADDRESS;

    public const ushort CGB_BOOT_ROM_START_ADDRESS = 0x0;
    public const ushort CGB_BOOT_ROM_FIRST_PART_END_ADDRESS = 0x100;
    public const ushort CGB_BOOT_ROM_SECOND_PART_START_ADDRESS = 0x200;
    public const ushort CGB_BOOT_ROM_END_ADDRESS = 0x900;
    public const ushort CGB_BOOT_ROM_SIZE = CGB_BOOT_ROM_END_ADDRESS - CGB_BOOT_ROM_START_ADDRESS;

    public GameBoy(HardwareTypes hardwareType, byte[] bootRom, byte[] romData, byte[] saveData, ScreenUpdateDelegate screenUpdateCallback, AddToAudioBufferDelegate addToAudioBufferCallback, SaveUpdateDelegate saveUpdateCallback)
    {
        cartridge = new Cartridge(romData, saveData, saveUpdateCallback);

        HardwareType = hardwareType;

        wram = new WRAM(this);
        hram = new HRAM();
        vram = new VRAM(this);
        oam = new OAM();
        interruptHandler = new InterruptHandler();
        serial = new Serial(interruptHandler);
        dma = new DMA(cartridge, wram, vram, oam);
        if (IsColorMode) hdma = new HDMA(cartridge, wram, vram);
        timer = new Timer(interruptHandler);
        apu = new APU(addToAudioBufferCallback);
        ppu = new PPU(this, interruptHandler, vram, oam, dma, screenUpdateCallback, ProcessSaveState);
        io = new IO(this, wram, vram, ppu, dma, hdma, timer, apu, serial, interruptHandler);
        mmu = new MMU(this, io, wram, hram, ppu, dma, bootRom, cartridge);
        cpu = new CPU(this, mmu, bootRom != null);

        InitRegisters(bootRom);

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

    void InitRegisters(byte[] bootRom)
    {
        if (bootRom != null) return;

        // If there's no boot ROM, the emulator will initialize some stuff.
        // If there is, the boot ROM itself will do the initialization.

        // Serial

        mmu[0xFF01] = 0x00;
        mmu[0xFF02] = 0x7E;

        // Timers

        mmu[0xFF04] = 0xCE;
        mmu[0xFF05] = 0x00;
        mmu[0xFF06] = 0x00;
        mmu[0xFF07] = 0xF8;

        // Sound

        mmu[0xFF26] = 0x80; // Need to enable audio before setting some other audio registers 

        mmu[0xFF10] = 0x80;
        mmu[0xFF11] = 0xBF;
        mmu[0xFF12] = 0xF3;
        mmu[0xFF13] = 0xFF;
        //mmu[0xFF14] = 0xBF; // This passes Mooneye's boot_hwio-C test, but generates noise on boot

        mmu[0xFF16] = 0x3F;
        mmu[0xFF17] = 0x00;
        mmu[0xFF18] = 0xFF;
        //mmu[0xFF19] = 0xBF;

        mmu[0xFF1A] = 0x7F;
        mmu[0xFF1B] = 0xFF;
        mmu[0xFF1C] = 0x9F;
        mmu[0xFF1D] = 0xFF;
        //mmu[0xFF1E] = 0xBF;

        mmu[0xFF20] = 0xFF;
        mmu[0xFF21] = 0x00;
        mmu[0xFF22] = 0x00;
        //mmu[0xFF23] = 0xBF;

        mmu[0xFF24] = 0x77;
        mmu[0xFF25] = 0xF3;

        mmu[0xFF30] = 0x00;
        mmu[0xFF31] = 0x00;
        mmu[0xFF32] = 0x00;
        mmu[0xFF33] = 0x00;
        mmu[0xFF34] = 0x00;
        mmu[0xFF35] = 0x00;
        mmu[0xFF36] = 0x00;
        mmu[0xFF37] = 0x00;
        mmu[0xFF38] = 0x00;
        mmu[0xFF39] = 0x00;
        mmu[0xFF3A] = 0x00;
        mmu[0xFF3B] = 0x00;
        mmu[0xFF3C] = 0x00;
        mmu[0xFF3D] = 0x00;
        mmu[0xFF3E] = 0x00;
        mmu[0xFF3F] = 0x00;

        // LCD Registers

        mmu[0xFF40] = 0x91;
        mmu[0xFF41] = 0x81;
        mmu[0xFF42] = 0x00;
        mmu[0xFF43] = 0x00;
        //mmu[0xFF44] = 0x99; // Setting this manually breaks the timings 
        mmu[0xFF45] = 0x00;
        //mmu[0xFF46] = 0x00; // Setting this manually breaks the timings 
        mmu[0xFF47] = 0xFC;
        mmu[0xFF48] = 0x00;
        mmu[0xFF49] = 0x00;
        mmu[0xFF4A] = 0x00;
        mmu[0xFF4B] = 0x00;

        // CGB

        if (HardwareType == HardwareTypes.CGB)
        {
            mmu[0xFF68] = 0x88;
            mmu[0xFF6A] = 0x90;
            mmu[0xFF70] = 0x07;
        }

        // Interrupts

        mmu[0xFF0F] = 0xE1;
        mmu[0xFFFF] = 0x00;

        // Disable the Boot ROM in the corresponding IO register
        mmu[0xFF50] = 0x01;
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
            if (IsColorMode) hdma.Update();

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

    public bool CompressSaveStates { get; set; }

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
            wram,
            hram,
            vram,
            oam,
            interruptHandler,
            serial,
            dma,
            hdma,
            timer,
            apu,
            ppu,
            io,
            cpu
        };

        saveState = new SaveState(this, cartridge, componentsStates);
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