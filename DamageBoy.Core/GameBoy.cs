using DamageBoy.Core.State;
using System;
using System.Diagnostics;
using System.Threading;

namespace DamageBoy.Core
{
    public enum EmulationStates
    {
        Stopped,
        Running,
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

        readonly Action<byte[]> screenUpdateCallback;

        readonly Cartridge cartridge;

        Action emulationStoppedCallback;

        public string GameTitle => cartridge.Title;

        public EmulationStates EmulationState { get; private set; }

        FrameLimiterStates frameLimiterState;

        public GameBoy(byte[] bootRom, byte[] romData, byte[] saveData, Action<byte[]> screenUpdateCallback, Action<byte[]> saveUpdateCallback)
        {
            cartridge = new Cartridge(romData, saveData, saveUpdateCallback);

            ram = new RAM();
            vram = new VRAM();
            interruptHandler = new InterruptHandler();
            serial = new Serial(interruptHandler);
            dma = new DMA(cartridge, ram, vram);
            timer = new Timer(interruptHandler);
            apu = new APU();
            ppu = new PPU(interruptHandler, vram, ScreenUpdate);
            io = new IO(ppu, dma, timer, apu, serial, interruptHandler);
            mmu = new MMU(io, ram, ppu, dma, bootRom, cartridge);
            cpu = new CPU(mmu, bootRom != null);

            this.screenUpdateCallback = screenUpdateCallback;

            frameLimiterState = FrameLimiterStates.Limited;

            Thread thread = new Thread(MainLoop);
            thread.Name = "GameBoy Emulation Loop";
            thread.Start();
        }

        public void Stop(Action emulationStoppedCallback)
        {
            if (EmulationState != EmulationStates.Running) return;
            this.emulationStoppedCallback = emulationStoppedCallback;
            EmulationState = EmulationStates.Stopping;
        }

        void MainLoop()
        {
            EmulationState = EmulationStates.Running;
            Utils.Log(LogType.Info, "Emulation is now running.");

            Stopwatch sw = new Stopwatch();
            sw.Start();

            while (EmulationState == EmulationStates.Running)
            {
                if (apu.IsAudioBufferFull)
                {
                    frameLimiterState = FrameLimiterStates.Paused;
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

                ProcessSaveState();

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

        public bool FillAudioBuffer(byte[] data)
        {
            bool isFilled = apu.FillAudioBuffer(data);
            if (!isFilled) frameLimiterState = FrameLimiterStates.Unlimited;
            else frameLimiterState = FrameLimiterStates.Limited;

            return isFilled;
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

        void ScreenUpdate(byte[] lcdPixels)
        {
            screenUpdateCallback?.Invoke(lcdPixels);
        }

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

        public bool IsTraceLogEnabled => cpu.IsTraceLogEnabled;

        public void ToggleTraceLog()
        {
            if (cpu.IsTraceLogEnabled)
                cpu.DisableTraceLog();
            else
                cpu.EnableTraceLog("LogDamageBoy.txt");
        }

        #endregion
    }
}