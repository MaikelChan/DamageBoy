using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.IO;

namespace GBEmu.Core
{
    enum EmulationStates
    {
        Stopped,
        Initializing,
        Running
    }

    class GameBoy
    {
        readonly RAM ram;
        readonly VRAM vram;
        readonly InterruptHandler interruptHandler;
        readonly DMA dma;
        readonly Timer timer;
        readonly Sound sound;
        readonly PPU ppu;
        readonly IO io;
        readonly MMU mmu;
        readonly CPU cpu;

        readonly Action<byte[]> screenUpdateCallback;

        Cartridge cartridge;

        public EmulationStates EmulationState { get; private set; }

        bool finishedFrame;

        public GameBoy(byte[] bootRom, byte[] romData, byte[] saveData, Action<byte[]> screenUpdateCallback, Action<SoundState> soundUpdateCallback, Action<byte[]> saveUpdateCallback)
        {
            cartridge = new Cartridge(romData, saveData, saveUpdateCallback);

            ram = new RAM();
            vram = new VRAM();
            interruptHandler = new InterruptHandler();
            dma = new DMA(cartridge, ram, vram);
            timer = new Timer(interruptHandler);
            sound = new Sound(soundUpdateCallback);
            ppu = new PPU(interruptHandler, vram, ScreenUpdate);
            io = new IO(ppu, dma, timer, sound, interruptHandler);
            mmu = new MMU(io, ram, ppu, dma, bootRom, cartridge);
            cpu = new CPU(mmu, bootRom != null);

            this.screenUpdateCallback = screenUpdateCallback;

            EmulationState = EmulationStates.Stopped;
        }

        public void Run()
        {
            if (EmulationState != EmulationStates.Stopped) return;

            EmulationState = EmulationStates.Initializing;
            Utils.Log(LogType.Info, "Emulation is now initializing.");

            //Reset();
            //memory.LoadROM(romStream);

            EmulationState = EmulationStates.Running;
            Utils.Log(LogType.Info, "Emulation is now running.");
        }

        public void Stop()
        {
            if (EmulationState != EmulationStates.Running) return;

            cartridge.Dispose();
            cartridge = null;

            EmulationState = EmulationStates.Stopped;
            Utils.Log(LogType.Info, "Emulation is now stopped.");
        }

        public void Update()
        {
            //ProcessSaveState();

            finishedFrame = false;

            while (!finishedFrame)
            //for (int n = 0; n < 15000; n++)
            {
                if (EmulationState != EmulationStates.Running) break;

#if !DEBUG
                try
                {
#endif
                timer.Update();
                sound.Update();
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

        public void KeyDown(Keys key)
        {
            if (EmulationState != EmulationStates.Running) return;

            switch (key)
            {
                case Keys.Right:
                    io.SetInput(IO.Buttons.Right, true);
                    break;
                case Keys.Left:
                    io.SetInput(IO.Buttons.Left, true);
                    break;
                case Keys.Up:
                    io.SetInput(IO.Buttons.Up, true);
                    break;
                case Keys.Down:
                    io.SetInput(IO.Buttons.Down, true);
                    break;
                case Keys.X:
                    io.SetInput(IO.Buttons.A, true);
                    break;
                case Keys.Z:
                    io.SetInput(IO.Buttons.B, true);
                    break;
                case Keys.RightShift:
                    io.SetInput(IO.Buttons.Select, true);
                    break;
                case Keys.Enter:
                    io.SetInput(IO.Buttons.Start, true);
                    break;
            }
        }

        public void KeyUp(Keys key)
        {
            if (EmulationState != EmulationStates.Running) return;

            switch (key)
            {
                case Keys.Right:
                    io.SetInput(IO.Buttons.Right, false);
                    break;
                case Keys.Left:
                    io.SetInput(IO.Buttons.Left, false);
                    break;
                case Keys.Up:
                    io.SetInput(IO.Buttons.Up, false);
                    break;
                case Keys.Down:
                    io.SetInput(IO.Buttons.Down, false);
                    break;
                case Keys.X:
                    io.SetInput(IO.Buttons.A, false);
                    break;
                case Keys.Z:
                    io.SetInput(IO.Buttons.B, false);
                    break;
                case Keys.RightShift:
                    io.SetInput(IO.Buttons.Select, false);
                    break;
                case Keys.Enter:
                    io.SetInput(IO.Buttons.Start, false);
                    break;
            }
        }

        //public void GetCPUState(CPUState cpuState)
        //{
        //    cpu.GetCPUState(cpuState);
        //}

        void ScreenUpdate(byte[] lcdPixels)
        {
            finishedFrame = true;
            screenUpdateCallback?.Invoke(lcdPixels);
        }

        #region Save States

        //State saveState = null;
        //SaveStateAction saveStateAction = SaveStateAction.None;

        //enum SaveStateAction { None, SavePending, LoadPending }

        public void SaveState()
        {
            //    if (EmulationState != EmulationStates.Running) return;

            //    saveStateAction = SaveStateAction.SavePending;
        }

        public void LoadState()
        {
            //    if (EmulationState != EmulationStates.Running) return;

            //    saveStateAction = SaveStateAction.LoadPending;
        }

        //void DoSaveState()
        //{
        //    if (saveState == null) saveState = new State(this);
        //    saveState.Save();
        //    Utils.Log(LogType.Info, "Finished saving save state.");
        //}

        //void DoLoadState()
        //{
        //    if (saveState == null)
        //    {
        //        Utils.Log(LogType.Info, "There's no save state to load.");
        //        return;
        //    }

        //    saveState.Load();
        //    Utils.Log(LogType.Info, "Finished loading save state.");
        //}

        //void ProcessSaveState()
        //{
        //    switch (saveStateAction)
        //    {
        //        case SaveStateAction.SavePending:
        //            DoSaveState();
        //            break;
        //        case SaveStateAction.LoadPending:
        //            DoLoadState();
        //            break;
        //    }

        //    saveStateAction = SaveStateAction.None;
        //}

        //class State
        //{
        //    public CPUState cpuState;
        //    public byte[] ram;
        //    public byte[] pixels;

        //    readonly GameBoy chip8;

        //    public State(GameBoy chip8)
        //    {
        //        this.chip8 = chip8;

        //        cpuState = new CPUState();
        //        ram = new byte[Memory.RAM_SIZE];
        //        pixels = new byte[Screen.RES_X * Screen.RES_Y];
        //    }

        //    public void Save()
        //    {
        //        chip8.cpu.GetCPUState(cpuState);
        //        Array.Copy(chip8.memory.ram, ram, Memory.RAM_SIZE);
        //        Array.Copy(chip8.screen.pixels, pixels, Screen.RES_X * Screen.RES_Y);
        //    }

        //    public void Load()
        //    {
        //        chip8.cpu.SetCPUState(cpuState);
        //        Array.Copy(ram, chip8.memory.ram, Memory.RAM_SIZE);
        //        Array.Copy(pixels, chip8.screen.pixels, Screen.RES_X * Screen.RES_Y);
        //    }
        //}

        #endregion
    }
}