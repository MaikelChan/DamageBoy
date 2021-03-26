﻿using GBEmu.Core.State;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Diagnostics;
using System.Threading;

namespace GBEmu.Core
{
    enum EmulationStates
    {
        Stopped,
        Initializing,
        Running
    }

    enum FrameLimiterStates
    {
        Limited,
        Unlimited,
        Paused
    }

    class GameBoy : IDisposable
    {
        readonly RAM ram;
        readonly VRAM vram;
        readonly InterruptHandler interruptHandler;
        readonly DMA dma;
        readonly Timer timer;
        readonly APU apu;
        readonly PPU ppu;
        readonly IO io;
        readonly MMU mmu;
        readonly CPU cpu;

        readonly Action<byte[]> screenUpdateCallback;

        readonly Cartridge cartridge;
        readonly Action<ushort[]> soundUpdateCallback;

        public string GameTitle => cartridge.Title;

        public EmulationStates EmulationState { get; private set; }

        FrameLimiterStates frameLimiterState;

        public GameBoy(byte[] bootRom, byte[] romData, byte[] saveData, Action<byte[]> screenUpdateCallback, Action<ushort[]> soundUpdateCallback, Action<byte[]> saveUpdateCallback)
        {
            cartridge = new Cartridge(romData, saveData, saveUpdateCallback);
            this.soundUpdateCallback = soundUpdateCallback;

            ram = new RAM();
            vram = new VRAM();
            interruptHandler = new InterruptHandler();
            dma = new DMA(cartridge, ram, vram);
            timer = new Timer(interruptHandler);
            apu = new APU(soundUpdateCallback);
            ppu = new PPU(interruptHandler, vram, ScreenUpdate);
            io = new IO(ppu, dma, timer, apu, interruptHandler);
            mmu = new MMU(io, ram, ppu, dma, bootRom, cartridge);
            cpu = new CPU(mmu, bootRom != null);

            this.screenUpdateCallback = screenUpdateCallback;

            frameLimiterState = FrameLimiterStates.Limited;

            Thread thread = new Thread(MainLoop);
            thread.Start();
        }

        public void Dispose()
        {
            if (EmulationState != EmulationStates.Running) return;

            cartridge.Dispose();

            EmulationState = EmulationStates.Stopped;
            Utils.Log(LogType.Info, "Emulation is now stopped.");
        }

        void MainLoop()
        {
            EmulationState = EmulationStates.Running;
            Utils.Log(LogType.Info, "Emulation is now running.");

            Stopwatch sw = new Stopwatch();
            sw.Start();

            while (EmulationState == EmulationStates.Running)
            {
                if (frameLimiterState == FrameLimiterStates.Paused)
                {
                    if (sw.ElapsedTicks < (4 * Stopwatch.Frequency) / CPU.CPU_CLOCKS) continue;
                    sw.Restart();

                    // We need to keep the sound system alive invoking it, so the GameBoy's main loop
                    // gets notified when an audio buffer has finished playing and the loop continues running. 
                    soundUpdateCallback?.Invoke(null);
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
                dma.Update();
#if !DEBUG
                }
                catch (Exception ex)
                {
                    Utils.Log(LogType.Error, ex.Message);
                }
#endif
            }
        }

        public void SetInput(KeyboardState keyboard, GamepadState gamepad)
        {
            if (EmulationState != EmulationStates.Running) return;

            const float DEADZONE = 0.65f;

            unsafe
            {
                io.SetInput(IO.Buttons.A, keyboard.IsKeyDown(Keys.X) || gamepad.Buttons[0] > 0);
                io.SetInput(IO.Buttons.B, keyboard.IsKeyDown(Keys.Z) || gamepad.Buttons[2] > 0);
                io.SetInput(IO.Buttons.Select, keyboard.IsKeyDown(Keys.RightShift) || gamepad.Buttons[6] > 0);
                io.SetInput(IO.Buttons.Start, keyboard.IsKeyDown(Keys.Enter) || gamepad.Buttons[7] > 0);
                io.SetInput(IO.Buttons.Up, keyboard.IsKeyDown(Keys.Up) || gamepad.Buttons[11] > 0 || gamepad.Axes[1] < -DEADZONE);
                io.SetInput(IO.Buttons.Right, keyboard.IsKeyDown(Keys.Right) || gamepad.Buttons[12] > 0 || gamepad.Axes[0] > DEADZONE);
                io.SetInput(IO.Buttons.Down, keyboard.IsKeyDown(Keys.Down) || gamepad.Buttons[13] > 0 || gamepad.Axes[1] > DEADZONE);
                io.SetInput(IO.Buttons.Left, keyboard.IsKeyDown(Keys.Left) || gamepad.Buttons[14] > 0 || gamepad.Axes[0] < -DEADZONE);
            }
        }

        public void SetFrameLimiterState(FrameLimiterStates state)
        {
            frameLimiterState = state;
        }

        //public void GetCPUState(CPUState cpuState)
        //{
        //    cpu.GetCPUState(cpuState);
        //}

        void ScreenUpdate(byte[] lcdPixels)
        {
            screenUpdateCallback?.Invoke(lcdPixels);
        }

        #region Save States

        SaveState saveState = null;
        SaveStateAction saveStateAction = SaveStateAction.None;

        enum SaveStateAction { None, SavePending, LoadPending }

        public void SaveState()
        {
            if (EmulationState != EmulationStates.Running) return;
            if (!io.BootROMDisabled) return;

            saveStateAction = SaveStateAction.SavePending;
        }

        public void LoadState()
        {
            if (EmulationState != EmulationStates.Running) return;
            if (!io.BootROMDisabled) return;

            saveStateAction = SaveStateAction.LoadPending;
        }

        void DoSaveState()
        {
            if (saveState == null)
            {
                IState[] componentsStates = new IState[]
                {
                    cartridge,
                    ram,
                    vram,
                    interruptHandler,
                    dma,
                    timer,
                    apu,
                    ppu,
                    io,
                    cpu
                };

                saveState = new SaveState(componentsStates, cartridge.RamSize);
            }

            saveState.Save();
            Utils.Log(LogType.Info, "Finished saving save state.");
        }

        void DoLoadState()
        {
            if (saveState == null)
            {
                Utils.Log(LogType.Info, "There's no save state to load.");
                return;
            }

            saveState.Load();
            Utils.Log(LogType.Info, "Finished loading save state.");
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
    }
}