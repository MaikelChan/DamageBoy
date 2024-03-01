using DamageBoy.Core.State;
using System;
using System.IO;

namespace DamageBoy.Core.MemoryBankControllers;

internal class HuC3 : IMemoryBankController
{
    readonly Cartridge cartridge;
    readonly byte[] rom;
    readonly CartridgeRam ram;

    byte romBank;
    int RomBank => romBank & ((cartridge.RomSize >> 14) - 1);

    byte ramBank;
    int RamBank => ramBank & ((cartridge.RamSize >> 13) - 1);

    enum Modes : byte { None, RamRead, RamReadWrite, RtcCommandArgumentWrite, RtcCommandResponseRead, RtcSemaphoreReadWrite, IrReadWrite }
    Modes mode;

    byte rtcCommandArgument;
    bool rtcIsReady;
    byte rtcAccessAddress;
    readonly byte[] rtcMemory;
    readonly byte[] rtcCurrentTime;

    const int RTC_MEMORY_SIZE = 256; // TODO: Is it 256?
    const int RTC_TIME_SIZE = 7;

    public HuC3(Cartridge cartridge, byte[] rom, CartridgeRam ram)
    {
        this.cartridge = cartridge;
        this.rom = rom;
        this.ram = ram;

        romBank = 1;
        ramBank = 0;
        mode = Modes.None;

        rtcCommandArgument = 0;
        rtcIsReady = true;
        rtcAccessAddress = 0;
        rtcMemory = new byte[RTC_MEMORY_SIZE];
        rtcCurrentTime = new byte[RTC_TIME_SIZE];
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
                    switch (mode)
                    {
                        case Modes.None:
                            return 0xFF;
                        case Modes.RamRead:
                        case Modes.RamReadWrite:
                            return ram[(RamBank << 13) + index - Cartridge.EXTERNAL_RAM_BANK_START_ADDRESS];
                        case Modes.RtcCommandResponseRead:
                            return rtcCommandArgument;
                        case Modes.RtcSemaphoreReadWrite:
                            return (byte)(rtcIsReady ? 1 : 0);
                        case Modes.IrReadWrite:
                            return 0xC0;
                        default:
                            Utils.Log(LogType.Error, $"HuC3 MBC: Not implemented reading in mode {mode}.");
                            return 0xFF;
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
                    int v = value & 0b0000_1111;

                    switch (v)
                    {
                        case 0x0:
                            //cartridge.IsRamEnabled = false;
                            mode = Modes.RamRead;
                            break;
                        case 0xA:
                            //cartridge.IsRamEnabled = true;
                            mode = Modes.RamReadWrite;
                            break;
                        case 0xB:
                            //cartridge.IsRamEnabled = false;
                            mode = Modes.RtcCommandArgumentWrite;
                            //Utils.Log(LogType.Warning, $"HuC3 MBC is setting the 0x{v:X1} command to the RAM/RTC/IR register.");
                            break;
                        case 0xC:
                            //cartridge.IsRamEnabled = false;
                            mode = Modes.RtcCommandResponseRead;
                            //Utils.Log(LogType.Warning, $"HuC3 MBC is setting the 0x{v:X1} command to the RAM/RTC/IR register.");
                            break;
                        case 0xD:
                            //cartridge.IsRamEnabled = false;
                            mode = Modes.RtcSemaphoreReadWrite;
                            //Utils.Log(LogType.Warning, $"HuC3 MBC is setting the 0x{v:X1} command to the RAM/RTC/IR register.");
                            break;
                        case 0xE:
                            //cartridge.IsRamEnabled = false;
                            mode = Modes.IrReadWrite;
                            //Utils.Log(LogType.Warning, $"HuC3 MBC is setting the 0x{v:X1} command to the RAM/RTC/IR register.");
                            break;
                        default:
                            //cartridge.IsRamEnabled = false;
                            mode = Modes.None;
                            break;
                    }

                    break;
                }

                case >= 0x2000 and < Cartridge.SWITCHABLE_ROM_BANK_START_ADDRESS:
                {
                    romBank = (byte)(value & 0b0111_1111);
                    break;
                }

                case >= Cartridge.SWITCHABLE_ROM_BANK_START_ADDRESS and < 0x6000:
                {
                    // HuC-3 can accept a bank number of at least 2 bits here.
                    // TODO: At least 2? What's the maximum? I've set 4 bits like the MBC5 for now.

                    ramBank = (byte)(value & 0b0000_1111);
                    break;
                }

                case >= 0x6000 and < Cartridge.SWITCHABLE_ROM_BANK_END_ADDRESS:
                {
                    Utils.Log(LogType.Info, $"HuC3 MBC is setting 0x{value:X2} to 0x{index:X4}, which is apparently ignored.");
                    break;
                }

                case >= Cartridge.EXTERNAL_RAM_BANK_START_ADDRESS and < Cartridge.EXTERNAL_RAM_BANK_END_ADDRESS:
                {
                    switch (mode)
                    {
                        //case Modes.None:
                        //    break;
                        //case Modes.RamRead:
                        //    break;
                        case Modes.RamReadWrite:
                            ram[(RamBank << 13) + index - Cartridge.EXTERNAL_RAM_BANK_START_ADDRESS] = value;
                            break;
                        case Modes.RtcCommandArgumentWrite:
                            rtcCommandArgument = value;
                            break;
                        case Modes.RtcSemaphoreReadWrite:
                            if ((value & 0b0000_0001) == 0)
                            {
                                // Execute command

                                rtcIsReady = false;

                                byte command = (byte)((rtcCommandArgument & 0b0111_0000) >> 4);
                                byte argument = (byte)(rtcCommandArgument & 0b0000_1111);

                                switch (command)
                                {
                                    case 0x1:

                                        // Read value and increment access address

                                        rtcCommandArgument = (byte)(rtcCommandArgument & 0b1111_0000);
                                        rtcCommandArgument |= rtcMemory[rtcAccessAddress];
                                        rtcAccessAddress++;

                                        break;

                                    case 0x3:

                                        // Write value and increment access address

                                        rtcMemory[rtcAccessAddress] = argument;
                                        rtcAccessAddress++;

                                        break;

                                    case 0x4:

                                        // Set access address least significant nybble

                                        rtcAccessAddress = (byte)(rtcAccessAddress & 0b1111_0000);
                                        rtcAccessAddress |= argument;

                                        break;

                                    case 0x5:

                                        // Set access address most significant nybble

                                        rtcAccessAddress = (byte)(rtcAccessAddress & 0b0000_1111);
                                        rtcAccessAddress |= (byte)(argument << 4);

                                        break;

                                    case 0x6:

                                        // Execute extended command specified by argument

                                        switch (argument)
                                        {
                                            case 0x0:

                                                // Copy current time to locations $00–06

                                                Array.Copy(rtcCurrentTime, 0, rtcMemory, 0, RTC_TIME_SIZE);

                                                break;

                                            case 0x1:

                                                // Copy locations $00–06 to current time, and update event time

                                                Array.Copy(rtcMemory, 0, rtcCurrentTime, 0, RTC_TIME_SIZE);
                                                // TODO: Event time?

                                                break;

                                            case 0x2:

                                                // Command $2 is used on start and seems to be some kind of status request.
                                                // The games will not start if the result is not $1.

                                                rtcCommandArgument = (byte)(rtcCommandArgument & 0b1111_0000);
                                                rtcCommandArgument |= 0x1;

                                                break;

                                            case 0xE:

                                                // Executing twice triggers tone generator

                                                Utils.Log(LogType.Info, "HuC3 MBC: Trigger tone generator.");
                                                break;

                                            default:
                                                Utils.Log(LogType.Error, $"HuC3 MBC: Not implemented RTC extended command 0x{argument:X1}.");
                                                break;
                                        }

                                        break;

                                    default:
                                        Utils.Log(LogType.Error, $"HuC3 MBC: Not implemented RTC command 0x{command:X1}.");
                                        break;
                                }

                                rtcIsReady = true;
                            }

                            break;

                        case Modes.IrReadWrite:

                            switch (value)
                            {
                                case 0:
                                    Utils.Log(LogType.Info, "HuC3 MBC: Infrared transmitter is OFF.");
                                    break;
                                case 1:
                                    Utils.Log(LogType.Info, "HuC3 MBC: Infrared transmitter is ON.");
                                    break;
                                default:
                                    Utils.Log(LogType.Warning, $"HuC3 MBC: Wrote 0x{value:X2} to Infrared transmitter register.");
                                    break;
                            }

                            break;

                        default:

                            Utils.Log(LogType.Error, $"HuC3 MBC: Not implemented writing in mode {mode}.");
                            break;
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

    public void SaveOrLoadState(Stream stream, BinaryWriter bw, BinaryReader br, bool save)
    {
        romBank = SaveState.SaveLoadValue(bw, br, save, romBank);
        ramBank = SaveState.SaveLoadValue(bw, br, save, ramBank);

        mode = (Modes)SaveState.SaveLoadValue(bw, br, save, (byte)mode);
        rtcCommandArgument = SaveState.SaveLoadValue(bw, br, save, rtcCommandArgument);
        rtcIsReady = SaveState.SaveLoadValue(bw, br, save, rtcIsReady);
        rtcAccessAddress = SaveState.SaveLoadValue(bw, br, save, rtcAccessAddress);

        SaveState.SaveLoadArray(stream, save, rtcMemory, RTC_MEMORY_SIZE);
        SaveState.SaveLoadArray(stream, save, rtcCurrentTime, RTC_TIME_SIZE);
    }
}