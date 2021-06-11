using System;
using System.IO;

namespace DamageBoy.Core.MemoryBankControllers
{
    class NullMBC : IMemoryBankController
    {
        readonly byte[] rom;

        public NullMBC(byte[] rom)
        {
            this.rom = rom;
        }

        public byte this[int index]
        {
            get
            {
                switch (index)
                {
                    case >= Cartridge.ROM_BANK_START_ADDRESS and < Cartridge.SWITCHABLE_ROM_BANK_END_ADDRESS: return rom[index];
                    case >= Cartridge.EXTERNAL_RAM_BANK_START_ADDRESS and < Cartridge.EXTERNAL_RAM_BANK_END_ADDRESS: return 0xFF;
                    default: throw new InvalidOperationException($"Tried to read from invalid Cartridge address: 0x{index:X4}");
                }
            }

            set
            {
                switch (index)
                {
                    case >= Cartridge.ROM_BANK_START_ADDRESS and < Cartridge.SWITCHABLE_ROM_BANK_END_ADDRESS: break;
                    case >= Cartridge.EXTERNAL_RAM_BANK_START_ADDRESS and < Cartridge.EXTERNAL_RAM_BANK_END_ADDRESS: break;
                    default: throw new InvalidOperationException($"Tried to write to invalid Cartridge address: 0x{index:X4}");
                }
            }
        }

        public void SaveOrLoadState(Stream stream, BinaryWriter bw, BinaryReader br, bool save)
        {

        }
    }
}