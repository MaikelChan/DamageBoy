using DamageBoy.Core.State;
using System;
using System.IO;

namespace DamageBoy.Core.MemoryBankControllers
{
    class MBC3 : IMemoryBankController
    {
        readonly Cartridge cartridge;
        readonly byte[] rom;
        readonly byte[] ram;

        byte romBank;
        int RomBank => romBank & ((cartridge.RomSize >> 14) - 1);

        byte ramOrRtcBank;
        bool IsRtcRegisterSelected => (ramOrRtcBank & 0b0000_1000) != 0;
        int RamBank => ramOrRtcBank & ((cartridge.RamSize >> 13) - 1);

        bool isRtcLatched;
        byte latchedSeconds;
        byte latchedMinutes;
        byte latchedHours;
        byte latchedDaysLo;
        byte latchedDaysHi;

        public MBC3(Cartridge cartridge, byte[] rom, byte[] ram)
        {
            this.cartridge = cartridge;
            this.rom = rom;
            this.ram = ram;

            romBank = 1;
            ramOrRtcBank = 0;
            isRtcLatched = false;
        }

        public byte this[int index]
        {
            get
            {
                switch (index)
                {
                    case >= Cartridge.ROM_BANK_START_ADDRESS and < Cartridge.ROM_BANK_END_ADDRESS:
                    {
                        return rom[(0 << 14) + index - Cartridge.ROM_BANK_START_ADDRESS];
                    }

                    case >= Cartridge.SWITCHABLE_ROM_BANK_START_ADDRESS and < Cartridge.SWITCHABLE_ROM_BANK_END_ADDRESS:
                    {
                        return rom[(RomBank << 14) + index - Cartridge.SWITCHABLE_ROM_BANK_START_ADDRESS];
                    }

                    case >= Cartridge.EXTERNAL_RAM_BANK_START_ADDRESS and < Cartridge.EXTERNAL_RAM_BANK_END_ADDRESS:
                    {
                        if (!cartridge.IsRamEnabled) return 0xFF;

                        if (IsRtcRegisterSelected)
                        {
                            switch (ramOrRtcBank & 0b0000_1111)
                            {
                                case 0x8: return latchedSeconds;
                                case 0x9: return latchedMinutes;
                                case 0xA: return latchedHours;
                                case 0xB: return latchedDaysLo;
                                case 0xC: return latchedDaysHi;
                                default:
                                    Utils.Log(LogType.Warning, $"Tried to read RTC register number 0x{ramOrRtcBank:X2}, which is not valid.");
                                    return 0xFF;
                            }
                        }
                        else
                        {
                            return ram[(RamBank << 13) + index - Cartridge.EXTERNAL_RAM_BANK_START_ADDRESS];
                        }
                    }

                    default:
                    {
                        throw new InvalidOperationException($"Tried to read from invalid Cartridge address: 0x{index:X4}");
                    }
                }
            }

            set
            {
                switch (index)
                {
                    case >= Cartridge.ROM_BANK_START_ADDRESS and < 0x2000:
                    {
                        cartridge.IsRamEnabled = (value & 0b0000_1111) == 0xA;
                        break;
                    }

                    case >= 0x2000 and < Cartridge.ROM_BANK_END_ADDRESS:
                    {
                        byte bank = (byte)(value & 0b0111_1111);
                        if (bank == 0) bank = 1;
                        romBank = bank;
                        break;
                    }

                    case >= Cartridge.SWITCHABLE_ROM_BANK_START_ADDRESS and < 0x6000:
                    {
                        ramOrRtcBank = (byte)(value & 0b0000_1111);
                        break;
                    }

                    case >= 0x6000 and < Cartridge.SWITCHABLE_ROM_BANK_END_ADDRESS:
                    {
                        bool latched = (value & 0b0000_0001) != 0 ? true : false;
                        if (isRtcLatched == latched) break;
                        isRtcLatched = latched;

                        if (isRtcLatched)
                        {
                            DateTime time = DateTime.Now;
                            latchedSeconds = (byte)time.Second;
                            latchedMinutes = (byte)time.Minute;
                            latchedHours = (byte)time.Hour;
                            latchedDaysLo = (byte)(time.DayOfYear & 0b1111_1111); // TODO: Not the correct way, as the day counter will be reset every year
                            latchedDaysHi = (byte)((time.DayOfYear >> 8) & 0b0000_0001);
                        }

                        break;
                    }

                    case >= Cartridge.EXTERNAL_RAM_BANK_START_ADDRESS and < Cartridge.EXTERNAL_RAM_BANK_END_ADDRESS:
                    {
                        if (!cartridge.IsRamEnabled) break;

                        if (IsRtcRegisterSelected)
                        {
                            // TODO: Since we are getting the time from the PC, it's read only for now.
                        }
                        else
                        {
                            ram[(RamBank << 13) + index - Cartridge.EXTERNAL_RAM_BANK_START_ADDRESS] = value;
                        }

                        break;
                    }

                    default:
                    {
                        throw new InvalidOperationException($"Tried to write to invalid Cartridge address: 0x{index:X4}");
                    }
                }
            }
        }

        public void LoadSaveState(Stream stream, BinaryWriter bw, BinaryReader br, bool save)
        {
            romBank = SaveState.SaveLoadValue(bw, br, save, romBank);
            ramOrRtcBank = SaveState.SaveLoadValue(bw, br, save, ramOrRtcBank);

            isRtcLatched = SaveState.SaveLoadValue(bw, br, save, isRtcLatched);
            latchedSeconds = SaveState.SaveLoadValue(bw, br, save, latchedSeconds);
            latchedMinutes = SaveState.SaveLoadValue(bw, br, save, latchedMinutes);
            latchedHours = SaveState.SaveLoadValue(bw, br, save, latchedHours);
            latchedDaysLo = SaveState.SaveLoadValue(bw, br, save, latchedDaysLo);
            latchedDaysHi = SaveState.SaveLoadValue(bw, br, save, latchedDaysHi);
        }
    }
}