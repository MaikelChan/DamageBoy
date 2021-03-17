using System;

namespace GBEmu.Core.MemoryBankControllers
{
    class MBC1 : IMemoryBankController
    {
        readonly Cartridge cartridge;
        readonly byte[] rom;
        readonly byte[] ram;

        enum BankingModes : byte { RomMode, RamMode_AdvancedRomMode }

        byte romBank;
        byte upperRomOrRamBank;
        BankingModes bankingMode = BankingModes.RomMode;

        int RamBank
        {
            get
            {
                if (bankingMode == BankingModes.RomMode) return 0;
                else return upperRomOrRamBank & ((cartridge.RamSize >> 13) - 1);
            }
        }

        const int CART_1_MEGABYTE_SIZE = 1 * 1024 * 1024;

        public MBC1(Cartridge cartridge, byte[] rom, byte[] ram)
        {
            this.cartridge = cartridge;
            this.rom = rom;
            this.ram = ram;

            romBank = 1;
            upperRomOrRamBank = 0;
        }

        public byte this[int index]
        {
            get
            {
                switch (index)
                {
                    case >= Cartridge.ROM_BANK_START_ADDRESS and < Cartridge.ROM_BANK_END_ADDRESS:
                    {
                        int bank = 0;

                        if (bankingMode == BankingModes.RamMode_AdvancedRomMode && cartridge.RomSize >= CART_1_MEGABYTE_SIZE)
                        {
                            // Carts 1Mbyte ROM size or larger can actually remap this area if bankingMode == 1.
                            // If bankingMode == 0, it behaves normally.

                            bank = upperRomOrRamBank;
                            bank &= (cartridge.RomSize >> 19) - 1;
                        }

                        return rom[(bank << 19) + index - Cartridge.ROM_BANK_START_ADDRESS];
                    }

                    case >= Cartridge.SWITCHABLE_ROM_BANK_START_ADDRESS and < Cartridge.SWITCHABLE_ROM_BANK_END_ADDRESS:
                    {
                        int bank;

                        if (bankingMode == BankingModes.RomMode)
                        {
                            bank = upperRomOrRamBank << 5 | romBank;
                        }
                        else
                        {
                            if (cartridge.RomSize >= CART_1_MEGABYTE_SIZE)
                            {
                                // Carts 1Mbyte ROM size or larger can actually access banks >= 0x20 even in bankingMode == 1.

                                bank = upperRomOrRamBank << 5 | romBank;
                            }
                            else
                            {
                                bank = romBank;
                            }
                        }

                        bank &= (cartridge.RomSize >> 14) - 1;

                        return rom[(bank << 14) + index - Cartridge.SWITCHABLE_ROM_BANK_START_ADDRESS];
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

                    case >= 0x2000 and < Cartridge.ROM_BANK_END_ADDRESS:
                    {
                        byte bank = (byte)(value & 0b0001_1111);
                        if (bank == 0) bank = 1;
                        romBank = bank;
                        break;
                    }

                    case >= Cartridge.SWITCHABLE_ROM_BANK_START_ADDRESS and < 0x6000:
                    {
                        upperRomOrRamBank = (byte)(value & 0b0000_0011);
                        break;
                    }

                    case >= 0x6000 and < Cartridge.SWITCHABLE_ROM_BANK_END_ADDRESS:
                    {
                        bankingMode = (BankingModes)(value & 0b0000_0001);
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
    }
}