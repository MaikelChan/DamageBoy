using GBEmu.Core.MemoryBankControllers;
using System;
using System.IO;
using System.Text;

namespace GBEmu.Core
{
    class Cartridge : IDisposable
    {
        readonly byte[] rom;
        readonly byte[] ram;
        readonly IMemoryBankController mbc;
        readonly Action<byte[]> saveUpdateCallback;

        public string Title { get; }

        public bool IsRamEnabled
        {
            get
            {
                if (ram != null) return isRamEnabled;
                return false;
            }

            set
            {
                if (ram != null)
                {
                    if (isRamEnabled && !value) saveUpdateCallback?.Invoke(ram);
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
                    default: throw new NotImplementedException($"ROM of size ID; 0x{rom[0x148]:X2} is not implemented");
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
                    case 1: throw new InvalidDataException($"Cartridge with MBC1 and RAM with ID: 0x{rom[0x149]:X2} shouldn't be valid"); // return 1024 * 2;
                    case 2: return 1024 * 8;
                    case 3: return 1024 * 32;
                    case 4: return 1024 * 128;
                    case 5: return 1024 * 64;
                    default: throw new NotImplementedException($"Cartridge with MBC1 and RAM with ID: 0x{rom[0x149]:X2} is not implemented");
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

            Title = Encoding.ASCII.GetString(romData, 0x134, 0xF).TrimEnd('\0');

            switch (romData[0x147])
            {
                case 0:
                    mbc = new NullMBC(romData);
                    break;

                case 1:
                case 2:
                case 3:
                    int ramSize = RamSize;
                    if (ramSize == 0)
                    {
                        ram = null;
                    }
                    else
                    {
                        if (saveData == null)
                        {
                            ram = new byte[ramSize];
                        }
                        else
                        {
                            if (saveData.Length != ramSize)
                            {
                                Utils.Log(LogType.Warning, $"Save data is {saveData.Length} bytes of size but the game expects {ramSize} bytes.");
                                ram = new byte[ramSize];
                            }
                            else
                            {
                                ram = saveData;
                            }
                        }
                    }
                    mbc = new MBC1(this, romData, ram);
                    break;

                default:
                    throw new NotImplementedException($"MBC with ID: {romData[0x147]} is not implemented");
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
                saveUpdateCallback?.Invoke(ram);
            }
        }
    }
}