using DamageBoy.Core.MemoryBankControllers;
using DamageBoy.Core.State;
using System;
using System.IO;
using System.Text;

namespace DamageBoy.Core;

class Cartridge : IDisposable, IState
{
    readonly byte[] rom;
    readonly CartridgeRam ram;
    readonly IMemoryBankController mbc;
    readonly Action<byte[]> saveUpdateCallback;

    public const int RAW_TITLE_LENGTH = 16;

    public byte[] RawTitle { get; }
    public string Title { get; }

    public bool IsRamEnabled
    {
        get => ram != null ? isRamEnabled : false;

        set
        {
            if (ram != null)
            {
                if (isRamEnabled && !value && ram.HasBeenModified)
                {
                    saveUpdateCallback?.Invoke(ram.Bytes);
                    ram.HasBeenModified = false;
                }

                isRamEnabled = value;
            }
        }
    }

    public int RomSize
    {
        get
        {
            switch (rom[0x148])
            {
                case >= 0 and < 9: return 32768 << rom[0x148];
                default: throw new NotImplementedException($"ROM of size ID; 0x{rom[0x148]:X2} is not implemented.");
            }
        }
    }

    public int RamSize
    {
        get
        {
            switch (rom[0x149])
            {
                case 0: return 0;
                case 1: throw new InvalidDataException($"Cartridge with RAM of size ID: 0x{rom[0x149]:X2} shouldn't be valid."); // return 1024 * 2;
                case 2: return 1024 * 8;
                case 3: return 1024 * 32;
                case 4: return 1024 * 128;
                case 5: return 1024 * 64;
                default: throw new NotImplementedException($"Cartridge with RAM of size ID: 0x{rom[0x149]:X2} is not implemented.");
            }
        }
    }

    bool isRamEnabled;

    public const ushort ROM_BANK_START_ADDRESS = 0x0000;
    public const ushort ROM_BANK_END_ADDRESS = 0x4000;

    public const ushort SWITCHABLE_ROM_BANK_START_ADDRESS = 0x4000;
    public const ushort SWITCHABLE_ROM_BANK_END_ADDRESS = 0x8000;

    public const ushort EXTERNAL_RAM_BANK_START_ADDRESS = 0xA000;
    public const ushort EXTERNAL_RAM_BANK_END_ADDRESS = 0xC000;

    public Cartridge(byte[] romData, byte[] saveData, Action<byte[]> saveUpdateCallback)
    {
        rom = romData;
        this.saveUpdateCallback = saveUpdateCallback;

        RawTitle = new byte[RAW_TITLE_LENGTH];
        Array.Copy(romData, 0x134, RawTitle, 0, RAW_TITLE_LENGTH);

        Title = Encoding.ASCII.GetString(romData, 0x134, RAW_TITLE_LENGTH).TrimEnd('\0');

        switch (romData[0x147])
        {
            case 0x0:
                mbc = new NullMBC(romData);
                break;

            case 0x1:
            case 0x2:
            case 0x3:
                ram = new CartridgeRam(RamSize, saveData);
                mbc = new MBC1(this, romData, ram);
                break;

            case 0x5:
            case 0x6:
                if (RamSize != 0) throw new InvalidDataException($"Cartridge with MBC2 and RAM of size ID: 0x{rom[0x149]:X2} shouldn't be valid.");
                mbc = new MBC2(this, romData, saveData, saveUpdateCallback);
                break;

            case 0x11:
            case 0x12:
            case 0x13:
                ram = new CartridgeRam(RamSize, saveData);
                mbc = new MBC3(this, romData, ram);
                break;

            case 0x19:
            case 0x1A:
            case 0x1B:
            case 0x1C:
            case 0x1D:
            case 0x1E:
                ram = new CartridgeRam(RamSize, saveData);
                mbc = new MBC5(this, romData, ram);
                break;

            case 0xFE:
                ram = new CartridgeRam(RamSize, saveData);
                mbc = new HuC3(this, romData, ram);
                break;

            case 0xFF:
                ram = new CartridgeRam(RamSize, saveData);
                mbc = new HuC1(this, romData, ram);
                break;

            default:
                throw new NotImplementedException($"MBC with ID: 0x{romData[0x147]:X4} is not implemented");
        }

        int romSize = RomSize;
        if (romData.Length != romSize) throw new InvalidDataException($"The ROM is expected to be {romSize} bytes but is {romData.Length} bytes");
    }

    public byte this[int index]
    {
        get => mbc[index];
        set => mbc[index] = value;
    }

    public void Dispose()
    {
        if (ram != null)
        {
            saveUpdateCallback?.Invoke(ram.Bytes);
        }
    }

    public void SaveOrLoadState(Stream stream, BinaryWriter bw, BinaryReader br, bool save)
    {
        if (ram != null)
        {
            SaveState.SaveLoadArray(stream, save, ram.Bytes, RamSize);
            if (!save) ram.HasBeenModified = true;
        }

        isRamEnabled = SaveState.SaveLoadValue(bw, br, save, isRamEnabled);
        mbc.SaveOrLoadState(stream, bw, br, save);
    }
}