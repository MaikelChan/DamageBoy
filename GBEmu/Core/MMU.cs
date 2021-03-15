using System;

namespace GBEmu.Core
{
    class MMU
    {
        readonly IO io;
        readonly RAM ram;
        readonly PPU ppu;
        readonly byte[] bootRom;
        readonly Cartridge cartridge;

        public const ushort BOOT_ROM_START_ADDRESS = 0x0;
        public const ushort BOOT_ROM_END_ADDRESS = 0x100;

        public const ushort INTERNAL_RAM_ECHO_START_ADDRESS = 0xE000;
        public const ushort INTERNAL_RAM_ECHO_END_ADDRESS = 0xFE00;
        public const ushort UNUSABLE_START_ADDRESS = 0xFEA0;
        public const ushort UNUSABLE_END_ADDRESS = 0xFF00;

        public MMU(IO io, RAM ram, PPU ppu, byte[] bootRom, Cartridge cartridge)
        {
            this.io = io;
            this.ram = ram;
            this.ppu = ppu;
            this.bootRom = bootRom;
            this.cartridge = cartridge;
        }

        public byte this[int index]
        {
            get
            {
                if (index >= BOOT_ROM_START_ADDRESS && index < BOOT_ROM_END_ADDRESS) return io.BootROMDisabled ? cartridge[index] : bootRom[index];
                else if (index >= BOOT_ROM_END_ADDRESS && index < Cartridge.SWITCHABLE_ROM_BANK_END_ADDRESS) return cartridge[index];
                else if (index >= VRAM.VRAM_START_ADDRESS && index < VRAM.VRAM_END_ADDRESS) return ppu[index];
                else if (index >= Cartridge.EXTERNAL_RAM_BANK_START_ADDRESS && index < Cartridge.EXTERNAL_RAM_BANK_END_ADDRESS) return cartridge[index];
                else if (index >= RAM.INTERNAL_RAM_START_ADDRESS && index < RAM.INTERNAL_RAM_END_ADDRESS) return ram.InternalRam[index - RAM.INTERNAL_RAM_START_ADDRESS];
                else if (index >= INTERNAL_RAM_ECHO_START_ADDRESS && index < INTERNAL_RAM_ECHO_END_ADDRESS) return ram.InternalRam[index - INTERNAL_RAM_ECHO_START_ADDRESS];
                else if (index >= VRAM.OAM_START_ADDRESS && index < VRAM.OAM_END_ADDRESS) return ppu[index];
                else if (index >= UNUSABLE_START_ADDRESS && index < UNUSABLE_END_ADDRESS) return 0xFF;
                else if (index >= IO.IO_PORTS_START_ADDRESS && index < IO.IO_PORTS_END_ADDRESS) return io[index - IO.IO_PORTS_START_ADDRESS];
                else if (index >= RAM.HIGH_RAM_START_ADDRESS && index < RAM.HIGH_RAM_END_ADDRESS) return ram.HighRam[index - RAM.HIGH_RAM_START_ADDRESS];
                else if (index == IO.INTERRUPT_ENABLE_REGISTER_ADDRESS) return io[0xFF];
                else throw new IndexOutOfRangeException($"Tried to read from out of range memory: 0x{index:X4}");
            }

            set
            {
                if (index >= BOOT_ROM_START_ADDRESS && index < BOOT_ROM_END_ADDRESS)
                {
                    if (io.BootROMDisabled) cartridge[index] = value;
                    else throw new InvalidOperationException("Tried to write into Boot ROM");
                }
                else if (index >= BOOT_ROM_END_ADDRESS && index < Cartridge.SWITCHABLE_ROM_BANK_END_ADDRESS) cartridge[index] = value;
                else if (index >= VRAM.VRAM_START_ADDRESS && index < VRAM.VRAM_END_ADDRESS) ppu[index] = value;
                else if (index >= Cartridge.EXTERNAL_RAM_BANK_START_ADDRESS && index < Cartridge.EXTERNAL_RAM_BANK_END_ADDRESS) cartridge[index] = value;
                else if (index >= RAM.INTERNAL_RAM_START_ADDRESS && index < RAM.INTERNAL_RAM_END_ADDRESS) ram.InternalRam[index - RAM.INTERNAL_RAM_START_ADDRESS] = value;
                else if (index >= INTERNAL_RAM_ECHO_START_ADDRESS && index < INTERNAL_RAM_ECHO_END_ADDRESS) ram.InternalRam[index - INTERNAL_RAM_ECHO_START_ADDRESS] = value;
                else if (index >= VRAM.OAM_START_ADDRESS && index < VRAM.OAM_END_ADDRESS) ppu[index] = value;
                else if (index >= UNUSABLE_START_ADDRESS && index < UNUSABLE_END_ADDRESS) { }
                else if (index >= IO.IO_PORTS_START_ADDRESS && index < IO.IO_PORTS_END_ADDRESS) io[index - IO.IO_PORTS_START_ADDRESS] = value;
                else if (index >= RAM.HIGH_RAM_START_ADDRESS && index < RAM.HIGH_RAM_END_ADDRESS) ram.HighRam[index - RAM.HIGH_RAM_START_ADDRESS] = value;
                else if (index == IO.INTERRUPT_ENABLE_REGISTER_ADDRESS) io[0xFF] = value;
                else throw new IndexOutOfRangeException($"Tried to write to out of range memory: 0x{index:X4}");
            }
        }
    }
}