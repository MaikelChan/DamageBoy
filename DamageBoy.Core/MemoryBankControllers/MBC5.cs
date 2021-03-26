using DamageBoy.Core.State;
using System;

namespace DamageBoy.Core.MemoryBankControllers
{
    class MBC5 : IMemoryBankController
    {
        readonly Cartridge cartridge;
        readonly byte[] rom;
        readonly byte[] ram;

        byte romBankHi, romBankLo;
        int RomBank => ((romBankHi << 8) | romBankLo) & ((cartridge.RomSize >> 14) - 1);

        byte ramBank;
        int RamBank => ramBank & ((cartridge.RamSize >> 13) - 1);

        public MBC5(Cartridge cartridge, byte[] rom, byte[] ram)
        {
            this.cartridge = cartridge;
            this.rom = rom;
            this.ram = ram;

            romBankHi = 0;
            romBankLo = 1;
            ramBank = 0;
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
                        return ram[(RamBank << 13) + index - Cartridge.EXTERNAL_RAM_BANK_START_ADDRESS];
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

                    case >= 0x2000 and < 0x3000:
                    {
                        romBankLo = value;
                        break;
                    }

                    case >= 0x3000 and < Cartridge.SWITCHABLE_ROM_BANK_START_ADDRESS:
                    {
                        romBankHi = (byte)(value & 0b0000_0001);
                        break;
                    }

                    case >= Cartridge.SWITCHABLE_ROM_BANK_START_ADDRESS and < 0x6000:
                    {
                        ramBank = (byte)(value & 0b0000_1111);
                        break;
                    }

                    case >= 0x6000 and < Cartridge.SWITCHABLE_ROM_BANK_END_ADDRESS:
                    {
                        break;
                    }

                    case >= Cartridge.EXTERNAL_RAM_BANK_START_ADDRESS and < Cartridge.EXTERNAL_RAM_BANK_END_ADDRESS:
                    {
                        if (!cartridge.IsRamEnabled) break;
                        ram[(RamBank << 13) + index - Cartridge.EXTERNAL_RAM_BANK_START_ADDRESS] = value;
                        break;
                    }

                    default:
                    {
                        throw new InvalidOperationException($"Tried to write to invalid Cartridge address: 0x{index:X4}");
                    }
                }
            }
        }

        public MemoryBankControllerState GetState()
        {
            MBC5State mbc5State = new MBC5State();

            mbc5State.RomBankHi = romBankHi;
            mbc5State.RomBankLo = romBankLo;
            mbc5State.RamBank = ramBank;

            return mbc5State;
        }

        public void SetState(MemoryBankControllerState state)
        {
            MBC5State mbc5State = (MBC5State)state;

            romBankHi = mbc5State.RomBankHi;
            romBankLo = mbc5State.RomBankLo;
            ramBank = mbc5State.RamBank;
        }
    }
}