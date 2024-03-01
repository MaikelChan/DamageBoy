using DamageBoy.Core.State;
using System;
using System.IO;

namespace DamageBoy.Core.MemoryBankControllers;

internal class HuC1 : IMemoryBankController
{
    readonly Cartridge cartridge;
    readonly byte[] rom;
    readonly byte[] ram;

    byte romBank;
    int RomBank => romBank & ((cartridge.RomSize >> 14) - 1);

    byte ramBank;
    int RamBank => ramBank & ((cartridge.RamSize >> 13) - 1);

    enum Modes : byte { Ram, Infrared }
    Modes mode;

    public HuC1(Cartridge cartridge, byte[] rom, byte[] ram)
    {
        this.cartridge = cartridge;
        this.rom = rom;
        this.ram = ram;

        romBank = 1;
        ramBank = 0;
        mode = Modes.Ram;
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
                        case Modes.Ram:
                            return ram[(RamBank << 13) + index - Cartridge.EXTERNAL_RAM_BANK_START_ADDRESS];
                        case Modes.Infrared:
                            return 0xC0;
                        default:
                            Utils.Log(LogType.Error, $"HuC1 MBC: Not implemented reading in mode {mode}.");
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
                    switch (value & 0b0000_1111)
                    {
                        case 0xE:
                            mode = Modes.Infrared;
                            break;
                        default:
                            mode = Modes.Ram;
                            break;
                    }

                    break;
                }

                case >= 0x2000 and < Cartridge.SWITCHABLE_ROM_BANK_START_ADDRESS:
                {
                    // HuC1 can accept a bank number of at least 6 bits here.
                    // TODO: At least 6? What's the maximum? I've set 7 bits like the HuC-3 for now.

                    romBank = (byte)(value & 0b0111_1111);
                    break;
                }

                case >= Cartridge.SWITCHABLE_ROM_BANK_START_ADDRESS and < 0x6000:
                {
                    // HuC1 can accept a bank number of at least 2 bits here.
                    // TODO: At least 2? What's the maximum? I've set 4 bits like the MBC5 for now.

                    ramBank = (byte)(value & 0b0000_1111);
                    break;
                }

                case >= 0x6000 and < Cartridge.SWITCHABLE_ROM_BANK_END_ADDRESS:
                {
                    Utils.Log(LogType.Info, $"HuC1 MBC is setting 0x{value:X2} to 0x{index:X4}, which is apparently ignored.");
                    break;
                }

                case >= Cartridge.EXTERNAL_RAM_BANK_START_ADDRESS and < Cartridge.EXTERNAL_RAM_BANK_END_ADDRESS:
                {
                    switch (mode)
                    {
                        case Modes.Ram:
                            //cartridge.IsRamEnabled = true;
                            ram[(RamBank << 13) + index - Cartridge.EXTERNAL_RAM_BANK_START_ADDRESS] = value;
                            cartridge.RamHasBeenModifiedSinceLastSave = true;
                            //cartridge.IsRamEnabled = false;
                            break;
                        case Modes.Infrared:
                            switch (value)
                            {
                                case 0:
                                    Utils.Log(LogType.Info, "HuC1 MBC: Infrared transmitter is OFF.");
                                    break;
                                case 1:
                                    Utils.Log(LogType.Info, "HuC1 MBC: Infrared transmitter is ON.");
                                    break;
                                default:
                                    Utils.Log(LogType.Warning, $"HuC1 MBC: Wrote 0x{value:X2} to Infrared transmitter register.");
                                    break;
                            }
                            break;
                        default:
                            Utils.Log(LogType.Error, $"HuC1 MBC: Not implemented writing in mode {mode}.");
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
    }
}