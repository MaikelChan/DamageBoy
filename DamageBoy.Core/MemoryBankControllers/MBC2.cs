using DamageBoy.Core.State;
using System;
using System.IO;

namespace DamageBoy.Core.MemoryBankControllers;

class MBC2 : IMemoryBankController
{
    readonly Cartridge cartridge;
    readonly byte[] rom;
    readonly Action<byte[]> saveUpdateCallback;

    readonly byte[] ram;

    byte romBank;
    int RomBank => romBank & ((cartridge.RomSize >> 14) - 1);

    bool isMBC2RamEnabled;
    bool IsMBC2RamEnabled
    {
        get
        {
            return isMBC2RamEnabled;
        }
        set
        {
            if (isMBC2RamEnabled && !value && ramHasBeenModifiedSinceLastSave)
            {
                saveUpdateCallback?.Invoke(ram);
                ramHasBeenModifiedSinceLastSave = false;
            }

            isMBC2RamEnabled = value;
        }
    }

    bool ramHasBeenModifiedSinceLastSave;

    const int RAM_SIZE = 512;

    public MBC2(Cartridge cartridge, byte[] rom, Action<byte[]> saveUpdateCallback)
    {
        this.cartridge = cartridge;
        this.rom = rom;
        this.saveUpdateCallback = saveUpdateCallback;

        ram = new byte[RAM_SIZE];

        romBank = 1;
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
                    if (!IsMBC2RamEnabled) return 0xFF;
                    index -= Cartridge.EXTERNAL_RAM_BANK_START_ADDRESS;
                    index &= 0x1FF;
                    return (byte)(0b1111_0000 | (ram[index] & 0b0000_1111));
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
                case >= Cartridge.ROM_BANK_START_ADDRESS and < Cartridge.SWITCHABLE_ROM_BANK_START_ADDRESS:
                {
                    bool romBankControl = (index & 0b0000_0001_0000_0000) != 0;

                    if (romBankControl)
                    {
                        romBank = (byte)(value & 0xF);
                        if (romBank == 0) romBank = 1;
                    }
                    else
                    {
                        IsMBC2RamEnabled = (value & 0b0000_1111) == 0xA;
                    }

                    break;
                }

                case >= Cartridge.SWITCHABLE_ROM_BANK_START_ADDRESS and < Cartridge.SWITCHABLE_ROM_BANK_END_ADDRESS:
                {
                    break;
                }

                case >= Cartridge.EXTERNAL_RAM_BANK_START_ADDRESS and < Cartridge.EXTERNAL_RAM_BANK_END_ADDRESS:
                {
                    if (!IsMBC2RamEnabled) break;
                    index -= Cartridge.EXTERNAL_RAM_BANK_START_ADDRESS;
                    index &= 0x1FF;
                    ram[index] = (byte)(0b1111_0000 | (value & 0b0000_1111));
                    ramHasBeenModifiedSinceLastSave = true;
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
        isMBC2RamEnabled = SaveState.SaveLoadValue(bw, br, save, isMBC2RamEnabled);
    }
}