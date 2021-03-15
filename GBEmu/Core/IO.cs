using System;

namespace GBEmu.Core
{
    class IO
    {
        readonly DMA dma;

        public const ushort IO_PORTS_START_ADDRESS = 0xFF00;
        public const ushort IO_PORTS_END_ADDRESS = 0xFF80;
        public const ushort IO_PORTS_SIZE = IO_PORTS_END_ADDRESS - IO_PORTS_START_ADDRESS;

        public const ushort INTERRUPT_ENABLE_REGISTER_ADDRESS = 0xFFFF;

        public IO(DMA dma)
        {
            this.dma = dma;

            buttons = new bool[8];

            // TODO: According to BGB, ioPorts are initialized to some default values instead of all zeroes.
            // Need to figure out what to do there and what those values are.
        }

        public byte this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0x00: return P1_JOYP;
                    case 0x01: return SB;
                    case 0x02: return SC;
                    case 0x04: return DIV;
                    case 0x05: return TIMA;
                    case 0x06: return TMA;
                    case 0x07: return TAC;
                    case 0x0F: return IF;
                    case 0x10: return NR10;
                    case 0x11: return NR11;
                    case 0x12: return NR12;
                    case 0x13: return NR13;
                    case 0x14: return NR14;
                    case 0x16: return NR21;
                    case 0x17: return NR22;
                    case 0x18: return NR23;
                    case 0x19: return NR24;
                    case 0x1A: return NR30;
                    case 0x1B: return NR31;
                    case 0x1C: return NR32;
                    case 0x1D: return NR33;
                    case 0x1E: return NR34;
                    case 0x20: return NR41;
                    case 0x21: return NR42;
                    case 0x22: return NR43;
                    case 0x23: return NR44;
                    case 0x24: return NR50;
                    case 0x25: return NR51;
                    case 0x26: return NR52;
                    case >= 0x30 and < 0x40: return GetWavePattern(index);
                    case 0x40: return LCDC;
                    case 0x41: return STAT;
                    case 0x42: return SCY;
                    case 0x43: return SCX;
                    case 0x44: return LY;
                    case 0x45: return LYC;
                    case 0x46: return DMA;
                    case 0x47: return BGP;
                    case 0x48: return OBP0;
                    case 0x49: return OBP1;
                    case 0x4A: return WY;
                    case 0x4B: return WX;
                    case 0xFF: return IE;
                    default:
                        Utils.Log(LogType.Warning, $"Read from IO Port 0x{index:X2} not implemented.");
                        return 0xFF;
                }
            }

            set
            {
                switch (index)
                {
                    case 0x00: P1_JOYP = value; break;
                    case 0x01: SB = value; break;
                    case 0x02: SC = value; break;
                    case 0x04: DIV = 0; break;
                    case 0x05: break; // TODO: Effects of writing here? Ignoring for now
                    case 0x06: TMA = value; break;
                    case 0x07: TAC = value; break;
                    case 0x0F: IF = value; break;
                    case 0x10: NR10 = value; break;
                    case 0x11: NR11 = value; break;
                    case 0x12: NR12 = value; break;
                    case 0x13: NR13 = value; break;
                    case 0x14: NR14 = value; break;
                    case 0x16: NR21 = value; break;
                    case 0x17: NR22 = value; break;
                    case 0x18: NR23 = value; break;
                    case 0x19: NR24 = value; break;
                    case 0x1A: NR30 = value; break;
                    case 0x1B: NR31 = value; break;
                    case 0x1C: NR32 = value; break;
                    case 0x1D: NR33 = value; break;
                    case 0x1E: NR34 = value; break;
                    case 0x20: NR41 = value; break;
                    case 0x21: NR42 = value; break;
                    case 0x22: NR43 = value; break;
                    case 0x23: NR44 = value; break;
                    case 0x24: NR50 = value; break;
                    case 0x25: NR51 = value; break;
                    case 0x26: NR52 = value; break;
                    case >= 0x30 and < 0x40: SetWavePattern(index, value); break;
                    case 0x40: LCDC = value; break;
                    case 0x41: STAT = value; break;
                    case 0x42: SCY = value; break;
                    case 0x43: SCX = value; break;
                    case 0x44: break;
                    case 0x45: LYC = value; break;
                    case 0x46: DMA = value; break;
                    case 0x47: BGP = value; break;
                    case 0x48: OBP0 = value; break;
                    case 0x49: OBP1 = value; break;
                    case 0x4A: WY = value; break;
                    case 0x4B: WX = value; break;
                    case 0x50: BootROMDisabled = true; break;
                    case 0xFF: IE = value; break;
                    default:
                        Utils.Log(LogType.Warning, $"Write to IO Port 0x{index:X2} not implemented.");
                        break;
                }
            }
        }

        #region Input

        public enum Buttons { Right, Left, Up, Down, A, B, Select, Start }

        readonly bool[] buttons;

        bool buttonSelect;
        bool directionSelect;

        /// <summary>
        /// FF00 - P1/JOYP - Joypad (R/W). Select either button or direction keys by writing to this register, then read-out bit 0-3.
        /// </summary>
        byte P1_JOYP
        {
            get
            {
                byte register = 0b1100_0000;

                if (directionSelect)
                {
                    Helpers.SetBit(ref register, 0, !buttons[(int)Buttons.Right]);
                    Helpers.SetBit(ref register, 1, !buttons[(int)Buttons.Left]);
                    Helpers.SetBit(ref register, 2, !buttons[(int)Buttons.Up]);
                    Helpers.SetBit(ref register, 3, !buttons[(int)Buttons.Down]);
                }
                else if (buttonSelect)
                {
                    Helpers.SetBit(ref register, 0, !buttons[(int)Buttons.A]);
                    Helpers.SetBit(ref register, 1, !buttons[(int)Buttons.B]);
                    Helpers.SetBit(ref register, 2, !buttons[(int)Buttons.Select]);
                    Helpers.SetBit(ref register, 3, !buttons[(int)Buttons.Start]);
                }
                else
                {
                    Helpers.SetBit(ref register, 0, true);
                    Helpers.SetBit(ref register, 1, true);
                    Helpers.SetBit(ref register, 2, true);
                    Helpers.SetBit(ref register, 3, true);
                }

                Helpers.SetBit(ref register, 4, !directionSelect);
                Helpers.SetBit(ref register, 5, !buttonSelect);

                return register;
            }

            set
            {
                directionSelect = (value & 0b0001_0000) == 0;
                buttonSelect = (value & 0b0010_0000) == 0;
            }
        }

        public void SetInput(Buttons button, bool isPressed)
        {
            buttons[(int)button] = isPressed;
            if (isPressed) InterruptRequestJoypad = true; // TODO: Revise the conditions where this is set
        }

        #endregion

        #region Serial

        byte sb;

        /// <summary>
        /// FF01 - SB - Serial transfer data (R/W)
        /// </summary>
        byte SB
        {
            get => sb;
            set => sb = value;
        }

        enum STCShiftClock : byte { ExternalClock, InternalClock }
        //enum STCClockSpeed : byte { Normal, Fast }
        enum STCTransferStartFlag : byte { NoTransferInProgressOrRequested, TransferInProgressOrRequested }

        STCShiftClock stcShiftClock;
        //STCClockSpeed stcClockSpeed; // Only in CGB
        STCTransferStartFlag stcTransferStartFlag;

        /// <summary>
        /// FF02 - SC - Serial Transfer Control (R/W)
        /// </summary>
        byte SC
        {
            get
            {
                byte register = 0b0111_1110;
                Helpers.SetBit(ref register, 0, stcShiftClock == STCShiftClock.InternalClock);
                //Helpers.SetBit(ref register, 1, stcClockSpeed == STCClockSpeed.Fast);  // Only in CGB
                Helpers.SetBit(ref register, 7, stcTransferStartFlag == STCTransferStartFlag.TransferInProgressOrRequested);
                return register;
            }

            set
            {
                stcShiftClock = Helpers.GetBit(value, 0) ? STCShiftClock.InternalClock : STCShiftClock.ExternalClock;
                // stcClockSpeed = Helpers.GetBit(value, 1) ? STCClockSpeed.Fast : STCClockSpeed.Normal;  // Only in CGB
                stcTransferStartFlag = Helpers.GetBit(value, 7) ? STCTransferStartFlag.TransferInProgressOrRequested : STCTransferStartFlag.NoTransferInProgressOrRequested;
            }
        }

        #endregion

        #region Timers

        byte div;

        /// <summary>
        /// FF04 - DIV - Divider Register (R/W)
        /// </summary>
        public byte DIV
        {
            get => div;
            set => div = value;
        }

        byte tima;

        /// <summary>
        /// FF05 - TIMA - Timer counter (R/W)
        /// </summary>
        public byte TIMA
        {
            get => tima;
            set => tima = value;
        }

        byte tma;

        /// <summary>
        /// FF06 - TMA - Timer Modulo (R/W)
        /// </summary>
        public byte TMA
        {
            get => tma;
            set => tma = value;
        }

        public Timer.TimerControlSpeeds TACInputClockSelect => tacInputClockSelect;
        Timer.TimerControlSpeeds tacInputClockSelect;

        public bool TACTimerEnable => tacTimerEnable;
        bool tacTimerEnable;

        /// <summary>
        /// FF07 - TAC - Timer Control (R/W)
        /// </summary>
        byte TAC
        {
            get
            {
                byte register = 0b1111_1000;
                register |= (byte)tacInputClockSelect;
                Helpers.SetBit(ref register, 2, tacTimerEnable);
                return register;
            }

            set
            {
                tacInputClockSelect = (Timer.TimerControlSpeeds)(value & 0b0000_0011);
                tacTimerEnable = Helpers.GetBit(value, 2);
            }
        }

        #endregion

        #region Interrupts

        public bool InterruptRequestVerticalBlanking { get; set; }
        public bool InterruptRequestLCDCSTAT { get; set; }
        public bool InterruptRequestTimerOverflow { get; set; }
        public bool InterruptRequestSerialTransferCompletion { get; set; }
        public bool InterruptRequestJoypad { get; set; }

        /// <summary>
        /// FF0F - IF - Interrupt Flag (R/W)
        /// </summary>
        byte IF
        {
            get
            {
                byte register = 0b1110_0000;
                Helpers.SetBit(ref register, 0, InterruptRequestVerticalBlanking);
                Helpers.SetBit(ref register, 1, InterruptRequestLCDCSTAT);
                Helpers.SetBit(ref register, 2, InterruptRequestTimerOverflow);
                Helpers.SetBit(ref register, 3, InterruptRequestSerialTransferCompletion);
                Helpers.SetBit(ref register, 4, InterruptRequestJoypad);
                return register;
            }

            set
            {
                InterruptRequestVerticalBlanking = Helpers.GetBit(value, 0);
                InterruptRequestLCDCSTAT = Helpers.GetBit(value, 1);
                InterruptRequestTimerOverflow = Helpers.GetBit(value, 2);
                InterruptRequestSerialTransferCompletion = Helpers.GetBit(value, 3);
                InterruptRequestJoypad = Helpers.GetBit(value, 4);
            }
        }

        bool interruptEnableVerticalBlanking;
        bool interruptEnableLCDCSTAT;
        bool interruptEnableTimerOverflow;
        bool interruptEnableSerialTransferCompletion;
        bool interruptEnableJoypad;
        byte interruptEnableUnusedBits; // In this case, unused bits are actually writable and readable

        /// <summary>
        /// FFFF - IE - Interrupt Enable (R/W)
        /// </summary>
        byte IE
        {
            get
            {
                byte register = 0b0000_0000;
                Helpers.SetBit(ref register, 0, interruptEnableVerticalBlanking);
                Helpers.SetBit(ref register, 1, interruptEnableLCDCSTAT);
                Helpers.SetBit(ref register, 2, interruptEnableTimerOverflow);
                Helpers.SetBit(ref register, 3, interruptEnableSerialTransferCompletion);
                Helpers.SetBit(ref register, 4, interruptEnableJoypad);
                register |= (byte)(interruptEnableUnusedBits << 5);
                return register;
            }

            set
            {
                interruptEnableVerticalBlanking = Helpers.GetBit(value, 0);
                interruptEnableLCDCSTAT = Helpers.GetBit(value, 1);
                interruptEnableTimerOverflow = Helpers.GetBit(value, 2);
                interruptEnableSerialTransferCompletion = Helpers.GetBit(value, 3);
                interruptEnableJoypad = Helpers.GetBit(value, 4);
                interruptEnableUnusedBits = (byte)(value >> 5);
            }
        }

        #endregion

        #region Sound

        #region Sound Channel 1 - Tone & Sweep

        enum SweepTypes : byte { Increase, Decrease }

        byte soundChannel1NumberOfSweepShift;
        SweepTypes soundChannel1SweepType;
        byte soundChannel1SweepTime;

        /// <summary>
        /// FF10 - NR10 - Channel 1 Sweep register (R/W)
        /// </summary>
        byte NR10
        {
            get
            {
                byte register = 0b1000_0000;
                register |= (byte)(soundChannel1NumberOfSweepShift & 0b0000_0111);
                Helpers.SetBit(ref register, 3, soundChannel1SweepType == SweepTypes.Decrease);
                register |= (byte)((soundChannel1SweepTime & 0b0000_0111) << 4);
                return register;
            }

            set
            {
                soundChannel1NumberOfSweepShift = (byte)(value & 0b0000_0111);
                soundChannel1SweepType = (value & 0b0000_1000) == 0 ? SweepTypes.Increase : SweepTypes.Decrease;
                soundChannel1SweepTime = (byte)((value & 0b0111_0000) >> 4);
            }
        }

        byte nr11;

        /// <summary>
        /// FF11 - NR11 - Channel 1 Sound length/Wave pattern duty (R/W)
        /// </summary>
        byte NR11
        {
            get => nr11;
            set => nr11 = value;
        }

        byte nr12;

        /// <summary>
        /// FF12 - NR12 - Channel 1 Volume Envelope (R/W)
        /// </summary>
        byte NR12
        {
            get => nr12;
            set => nr12 = value;
        }

        byte nr13;

        /// <summary>
        /// FF13 - NR13 - Channel 1 Frequency lo (Write Only)
        /// </summary>
        byte NR13
        {
            get => 0xFF; // nr13;
            set => nr13 = value;
        }

        byte nr14;

        /// <summary>
        /// FF14 - NR14 - Channel 1 Frequency hi (R/W)
        /// </summary>
        byte NR14
        {
            get => nr14;
            set => nr14 = value;
        }

        #endregion

        #region Sound Channel 2 - Tone

        byte nr21;

        /// <summary>
        /// FF16 - NR21 - Channel 2 Sound Length/Wave Pattern Duty (R/W)
        /// </summary>
        byte NR21
        {
            get => nr21;
            set => nr21 = value;
        }

        byte nr22;

        /// <summary>
        /// FF17 - NR22 - Channel 2 Volume Envelope (R/W)
        /// </summary>
        byte NR22
        {
            get => nr22;
            set => nr22 = value;
        }

        byte nr23;

        /// <summary>
        /// FF18 - NR23 - Channel 2 Frequency lo data (W)
        /// </summary>
        byte NR23
        {
            get => 0xFF;// nr23;
            set => nr23 = value;
        }

        byte nr24;

        /// <summary>
        /// FF19 - NR24 - Channel 2 Frequency hi data (R/W)
        /// </summary>
        byte NR24
        {
            get => nr24;
            set => nr24 = value;
        }

        #endregion

        #region Sound Channel 3 - Wave Output

        bool soundChannel3On;

        /// <summary>
        /// FF1A - NR30 - Channel 3 Sound on/off (R/W)
        /// </summary>
        byte NR30
        {
            get
            {
                byte register = 0b0111_1111;
                Helpers.SetBit(ref register, 7, soundChannel3On);
                return register;
            }

            set
            {
                soundChannel3On = Helpers.GetBit(value, 7);
            }
        }

        byte nr31;

        /// <summary>
        /// FF1B - NR31 - Channel 3 Sound Length
        /// </summary>
        byte NR31
        {
            get => nr31;
            set => nr31 = value;
        }

        enum SoundChannel3OutputLevels { Mute, Percent100, Percent50, Percent25 }
        SoundChannel3OutputLevels soundChannel3OutputLevel;

        /// <summary>
        /// FF1C - NR32 - Channel 3 Select output level (R/W)
        /// </summary>
        byte NR32
        {
            get
            {
                byte register = 0b1001_1111;
                register |= (byte)((byte)soundChannel3OutputLevel << 5);
                return register;
            }

            set
            {
                soundChannel3OutputLevel = (SoundChannel3OutputLevels)((value & 0b0110_0000) >> 5);
            }
        }

        byte nr33;

        /// <summary>
        /// FF1D - NR33 - Channel 3 Frequency's lower data (W)
        /// </summary>
        byte NR33
        {
            get => 0xFF;// nr33;
            set => nr33 = value;
        }

        byte nr34;

        /// <summary>
        /// FF1E - NR34 - Channel 3 Frequency's higher data (R/W)
        /// </summary>
        byte NR34
        {
            get => nr34;
            set => nr34 = value;
        }

        byte[] wavePattern = new byte[0x10];

        /// <summary>
        /// FF30-FF3F - Wave Pattern RAM
        /// </summary>
        byte GetWavePattern(int index)
        {
            return wavePattern[index - 0x30];
        }

        /// <summary>
        /// FF30-FF3F - Wave Pattern RAM
        /// </summary>
        void SetWavePattern(int index, byte value)
        {
            wavePattern[index - 0x30] = value;
        }

        #endregion

        #region Sound Channel 4 - Noise

        byte soundChannel4LengthData;

        /// <summary>
        /// FF20 - NR41 - Channel 4 Sound Length (R/W)
        /// </summary>
        byte NR41
        {
            get
            {
                byte register = 0b1100_0000;
                register |= (byte)(soundChannel4LengthData & 0b0011_1111);
                return register;
            }

            set
            {
                soundChannel4LengthData = (byte)(value & 0b0011_1111);
            }
        }

        byte nr42;

        /// <summary>
        /// FF21 - NR42 - Channel 4 Volume Envelope (R/W)
        /// </summary>
        byte NR42
        {
            get => nr42;
            set => nr42 = value;
        }

        byte nr43;

        /// <summary>
        /// FF22 - NR43 - Channel 4 Polynomial Counter (R/W)
        /// </summary>
        byte NR43
        {
            get => nr43;
            set => nr43 = value;
        }

        bool soundChannel4CounterConsecutiveSelection;
        bool soundChannel4Initial;

        /// <summary>
        /// FF23 - NR44 - Channel 4 Counter/consecutive; Inital (R/W)
        /// </summary>
        byte NR44
        {
            get
            {
                byte register = 0b0011_1111;
                Helpers.SetBit(ref register, 6, soundChannel4CounterConsecutiveSelection);
                Helpers.SetBit(ref register, 7, soundChannel4Initial);
                return register;
            }

            set
            {
                soundChannel4CounterConsecutiveSelection = Helpers.GetBit(value, 6);
                soundChannel4Initial = Helpers.GetBit(value, 7);
            }
        }

        #endregion

        #region Sound Control Registers

        byte nr50;

        /// <summary>
        /// FF24 - NR50 - Channel control / ON-OFF / Volume (R/W)
        /// </summary>
        byte NR50
        {
            get => nr50;
            set => nr50 = value;
        }

        byte nr51;

        /// <summary>
        /// FF25 - NR51 - Selection of Sound output terminal (R/W)
        /// </summary>
        byte NR51
        {
            get => nr51;
            set => nr51 = value;
        }

        bool SoundChannel1Enabled { get; set; }
        bool SoundChannel2Enabled { get; set; }
        bool SoundChannel3Enabled { get; set; }
        bool SoundChannel4Enabled { get; set; }
        bool AllSoundEnabled { get; set; }

        /// <summary>
        /// FF26 - NR52 - Sound on/off
        /// </summary>
        byte NR52
        {
            get
            {
                byte register = 0b0111_0000;
                Helpers.SetBit(ref register, 0, SoundChannel1Enabled);
                Helpers.SetBit(ref register, 1, SoundChannel2Enabled);
                Helpers.SetBit(ref register, 2, SoundChannel3Enabled);
                Helpers.SetBit(ref register, 3, SoundChannel4Enabled);
                Helpers.SetBit(ref register, 7, AllSoundEnabled);
                return register;
            }

            set
            {
                SoundChannel1Enabled = Helpers.GetBit(value, 0);
                SoundChannel2Enabled = Helpers.GetBit(value, 1);
                SoundChannel3Enabled = Helpers.GetBit(value, 2);
                SoundChannel4Enabled = Helpers.GetBit(value, 3);
                AllSoundEnabled = Helpers.GetBit(value, 7);
            }
        }

        #endregion

        //void SoundOnOff(byte value)
        //{

        //}

        //void StopAllSounds()
        //{

        //}

        #endregion

        #region PPU

        #region LCD Control Register

        /// <summary>
        /// FF40 - LCDC (LCD Control) (R/W)
        /// </summary>
        byte LCDC
        {
            get
            {
                byte register = 0;
                Helpers.SetBit(ref register, 0, BGDisplayEnable);
                Helpers.SetBit(ref register, 1, OBJDisplayEnable);
                Helpers.SetBit(ref register, 2, OBJSize);
                Helpers.SetBit(ref register, 3, BGTileMapDisplaySelect);
                Helpers.SetBit(ref register, 4, BGAndWindowTileDataSelect);
                Helpers.SetBit(ref register, 5, WindowDisplayEnable);
                Helpers.SetBit(ref register, 6, WindowTileMapDisplaySelect);
                Helpers.SetBit(ref register, 7, LCDDisplayEnable);
                return register;
            }

            set
            {
                if (LCDDisplayEnable && !Helpers.GetBit(value, 7) && LCDStatusMode != PPU.Modes.VerticalBlank)
                {
                    throw new InvalidOperationException("Even if original GameBoy allows to disable LCD outside of VBlank period, that can cause damages to the system and it was prohibited by Nintendo. This operation shouldn't be happening.");
                }

                BGDisplayEnable = Helpers.GetBit(value, 0);
                OBJDisplayEnable = Helpers.GetBit(value, 1);
                OBJSize = Helpers.GetBit(value, 2);
                BGTileMapDisplaySelect = Helpers.GetBit(value, 3);
                BGAndWindowTileDataSelect = Helpers.GetBit(value, 4);
                WindowDisplayEnable = Helpers.GetBit(value, 5);
                WindowTileMapDisplaySelect = Helpers.GetBit(value, 6);
                LCDDisplayEnable = Helpers.GetBit(value, 7);
            }
        }

        public bool LCDDisplayEnable { get; set; }
        public bool WindowTileMapDisplaySelect { get; set; }
        public bool WindowDisplayEnable { get; set; }
        public bool BGAndWindowTileDataSelect { get; set; }
        public bool BGTileMapDisplaySelect { get; set; }
        public bool OBJSize { get; set; }
        public bool OBJDisplayEnable { get; set; }
        public bool BGDisplayEnable { get; set; }

        #endregion

        #region LCD Status Register

        /// <summary>
        /// FF41 - STAT (LCD Status) (R/W)
        /// </summary>
        byte STAT
        {
            get
            {
                byte register = 0b1000_0000;
                register |= (byte)LCDStatusMode;
                Helpers.SetBit(ref register, 2, LCDStatusCoincidenceFlag == PPU.CoincidenceFlagModes.Equals);
                Helpers.SetBit(ref register, 3, LCDStatusHorizontalBlankInterrupt);
                Helpers.SetBit(ref register, 4, LCDStatusVerticalBlankInterrupt);
                Helpers.SetBit(ref register, 5, LCDStatusOAMSearchInterrupt);
                Helpers.SetBit(ref register, 6, LCDStatusCoincidenceInterrupt);
                return register;
            }

            set
            {
                LCDStatusMode = (PPU.Modes)(value & 0b0000_0011);
                LCDStatusCoincidenceFlag = Helpers.GetBit(value, 2) ? PPU.CoincidenceFlagModes.Equals : PPU.CoincidenceFlagModes.Different;
                LCDStatusHorizontalBlankInterrupt = Helpers.GetBit(value, 3);
                LCDStatusVerticalBlankInterrupt = Helpers.GetBit(value, 4);
                LCDStatusOAMSearchInterrupt = Helpers.GetBit(value, 5);
                LCDStatusCoincidenceInterrupt = Helpers.GetBit(value, 6);
            }
        }

        public PPU.Modes LCDStatusMode { get; set; }
        public PPU.CoincidenceFlagModes LCDStatusCoincidenceFlag { get; set; }
        public bool LCDStatusHorizontalBlankInterrupt { get; set; }
        public bool LCDStatusVerticalBlankInterrupt { get; set; }
        public bool LCDStatusOAMSearchInterrupt { get; set; }
        public bool LCDStatusCoincidenceInterrupt { get; set; }

        #endregion

        #region LCD Position and Scrolling

        byte scy;

        /// <summary>
        /// FF42 - SCY (Scroll Y) (R/W)
        /// </summary>
        public byte SCY
        {
            get => scy;
            set => scy = value;
        }

        byte scx;

        /// <summary>
        /// FF43 - SCX (Scroll X) (R/W)
        /// </summary>
        public byte SCX
        {
            get => scx;
            set => scx = value;
        }

        byte ly;

        /// <summary>
        /// FF44 - LY (LCDC Y-Coordinate) (R)
        /// </summary>
        public byte LY
        {
            get => ly;
            set => ly = value;
        }

        byte lyc;

        /// <summary>
        /// FF45 - LYC (LY Compare) (R/W)
        /// </summary>
        public byte LYC
        {
            get => lyc;
            set => lyc = value;
        }

        byte wy;

        /// <summary>
        /// FF4A - WY (Window Y Position) (R/W)
        /// </summary>
        public byte WY
        {
            get => wy;
            set => wy = value;
        }

        byte wx;

        /// <summary>
        /// FF4B - WX (Window X Position + 7) (R/W)
        /// </summary>
        public byte WX
        {
            get => wx;
            set => wx = value;
        }

        #endregion

        #region LCD Monochrome Palettes

        byte bgp;

        /// <summary>
        /// FF47 - BGP (BG Palette Data) (R/W) - Non CGB Mode Only
        /// </summary>
        byte BGP
        {
            get => bgp;
            set => bgp = value;
        }

        byte obp0;

        /// <summary>
        /// FF48 - OBP0 (Object Palette 0 Data) (R/W) - Non CGB Mode Only
        /// </summary>
        byte OBP0
        {
            get => obp0;
            set => obp0 = value;
        }

        byte obp1;

        /// <summary>
        /// FF49 - OBP1 (Object Palette 1 Data) (R/W) - Non CGB Mode Only
        /// </summary>
        byte OBP1
        {
            get => obp1;
            set => obp1 = value;
        }

        public byte GetBGPaletteColor(byte colorIndex)
        {
            return (byte)((bgp >> (colorIndex << 1)) & 0x3);
        }

        public byte GetObjPalette0Color(byte colorIndex)
        {
            return (byte)((obp0 >> (colorIndex << 1)) & 0x3);
        }

        public byte GetObjPalette1Color(byte colorIndex)
        {
            return (byte)((obp1 >> (colorIndex << 1)) & 0x3);
        }

        #endregion

        #endregion

        #region DMA

        byte _dma;

        /// <summary>
        /// FF46 - DMA (DMA Transfer and Start Address) (R/W)
        /// </summary>
        byte DMA
        {
            get => _dma;
            set
            {
                _dma = value;
                dma.Begin(value);
            }
        }

        #endregion

        #region Boot ROM

        /// <summary>
        /// // Disables the Boot ROM.
        /// </summary>
        public bool BootROMDisabled { get; private set; }

        #endregion
    }
}