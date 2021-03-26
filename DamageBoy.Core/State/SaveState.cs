
namespace DamageBoy.Core.State
{
    internal class SaveState
    {
        #region Cartridge

        public byte[] ExternalRam { get; }
        public bool IsExternalRamEnabled { get; set; }
        public MemoryBankControllerState MemoryBankControllerState { get; set; }

        #endregion

        #region RAM

        public byte[] InternalRam { get; }
        public byte[] HighRam { get; }

        #endregion

        #region VRAM

        public byte[] VRam { get; }
        public byte[] Oam { get; }

        #endregion

        #region Interrupt

        public bool RequestVerticalBlanking { get; set; }
        public bool RequestLCDCSTAT { get; set; }
        public bool RequestTimerOverflow { get; set; }
        public bool RequestSerialTransferCompletion { get; set; }
        public bool RequestJoypad { get; set; }

        public bool EnableVerticalBlanking { get; set; }
        public bool EnableLCDCSTAT { get; set; }
        public bool EnableTimerOverflow { get; set; }
        public bool EnableSerialTransferCompletion { get; set; }
        public bool EnableJoypad { get; set; }

        public byte EnableUnusedBits { get; set; }

        #endregion

        #region DMA

        public ushort DmaSourceAddress { get; set; }
        public int DmaCurrentOffset { get; set; }

        #endregion

        #region Timer

        public byte Divider { get; set; }
        public bool TimerEnable { get; set; }
        public Timer.TimerClockSpeeds TimerClockSpeed { get; set; }
        public byte TimerCounter { get; set; }
        public byte TimerModulo { get; set; }

        public int DividerClocksToWait { get; set; }
        public int TimerClocksToWait { get; set; }

        public bool TimerHasOverflown { get; set; }
        public int TimerOverflowWaitCycles { get; set; }

        #endregion

        #region APU

        public SoundChannelState[] SoundChannelsStates { get; }

        public int ApuSampleClocksToWait { get; set; }
        public int ApuLengthControlClocksToWait { get; set; }
        public int ApuVolumeEnvelopeClocksToWait { get; set; }
        public int ApuSweepClocksToWait { get; set; }

        public byte Output1Level { get; set; }
        public bool VinOutput1 { get; set; }
        public byte Output2Level { get; set; }
        public bool VinOutput2 { get; set; }

        public bool AllSoundEnabled { get; set; }

        #endregion

        #region PPU

        public bool LCDDisplayEnable { get; set; }
        public bool WindowTileMapDisplaySelect { get; set; }
        public bool WindowDisplayEnable { get; set; }
        public bool BGAndWindowTileDataSelect { get; set; }
        public bool BGTileMapDisplaySelect { get; set; }
        public bool OBJSize { get; set; }
        public bool OBJDisplayEnable { get; set; }
        public bool BGDisplayEnable { get; set; }

        public PPU.Modes LCDStatusMode { get; set; }
        public PPU.CoincidenceFlagModes LCDStatusCoincidenceFlag { get; set; }
        public bool LCDStatusHorizontalBlankInterrupt { get; set; }
        public bool LCDStatusVerticalBlankInterrupt { get; set; }
        public bool LCDStatusOAMSearchInterrupt { get; set; }
        public bool LCDStatusCoincidenceInterrupt { get; set; }

        public byte ScrollY { get; set; }
        public byte ScrollX { get; set; }
        public byte LY { get; set; }
        public byte LYC { get; set; }
        public byte WindowY { get; set; }
        public byte WindowX { get; set; }

        public byte BackgroundPalette { get; set; }
        public byte ObjectPalette0 { get; set; }
        public byte ObjectPalette1 { get; set; }

        public int PpuClocksToWait { get; set; }

        #endregion

        #region IO

        public bool[] Buttons { get; }

        public bool ButtonSelect { get; set; }
        public bool DirectionSelect { get; set; }

        #endregion

        #region CPU

        public byte A { get; set; }
        public byte B { get; set; }
        public byte C { get; set; }
        public byte D { get; set; }
        public byte E { get; set; }
        public byte F { get; set; }
        public byte H { get; set; }
        public byte L { get; set; }

        public ushort SP { get; set; }
        public ushort PC { get; set; }

        public int ClocksToWait { get; set; }
        public bool IsHalted { get; set; }


        public bool InterruptMasterEnableFlag { get; set; }
        public int InterruptMasterEnablePendingCycles { get; set; }

        #endregion

        IState[] componentsStates;

        public SaveState(IState[] componentsStates, int externalRamSize)
        {
            this.componentsStates = componentsStates;

            if (externalRamSize > 0) ExternalRam = new byte[externalRamSize];

            InternalRam = new byte[RAM.INTERNAL_RAM_SIZE];
            HighRam = new byte[RAM.HIGH_RAM_SIZE];

            VRam = new byte[VRAM.VRAM_SIZE];
            Oam = new byte[VRAM.OAM_SIZE];

            SoundChannelsStates = new SoundChannelState[Constants.SOUND_CHANNEL_COUNT];

            Buttons = new bool[IO.BUTTON_COUNT];
        }

        public void Save()
        {
            for (int cs = 0; cs < componentsStates.Length; cs++)
            {
                componentsStates[cs].GetState(this);
            }
        }

        public void Load()
        {
            for (int cs = 0; cs < componentsStates.Length; cs++)
            {
                componentsStates[cs].SetState(this);
            }
        }
    }
}