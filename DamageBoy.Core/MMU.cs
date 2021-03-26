using System;

namespace DamageBoy.Core
{
    class MMU
    {
        readonly IO io;
        readonly RAM ram;
        readonly PPU ppu;
        readonly DMA dma;
        readonly byte[] bootRom;
        readonly Cartridge cartridge;

        public const ushort BOOT_ROM_START_ADDRESS = 0x0;
        public const ushort BOOT_ROM_END_ADDRESS = 0x100;

        public const ushort INTERNAL_RAM_ECHO_START_ADDRESS = 0xE000;
        public const ushort INTERNAL_RAM_ECHO_END_ADDRESS = 0xFE00;
        public const ushort UNUSABLE_START_ADDRESS = 0xFEA0;
        public const ushort UNUSABLE_END_ADDRESS = 0xFF00;

        public MMU(IO io, RAM ram, PPU ppu, DMA dma, byte[] bootRom, Cartridge cartridge)
        {
            this.io = io;
            this.ram = ram;
            this.ppu = ppu;
            this.dma = dma;
            this.bootRom = bootRom;
            this.cartridge = cartridge;
        }

        public byte this[int index]
        {
            get
            {
                if (dma.IsBusy && index < IO.IO_PORTS_START_ADDRESS)
                {
                    Utils.Log(LogType.Warning, $"Tried to read from 0x{index:X4} during OAM transfer.");
                    return 0xFF;
                }

                switch (index)
                {
                    case >= BOOT_ROM_START_ADDRESS and < BOOT_ROM_END_ADDRESS: return io.BootROMDisabled ? cartridge[index] : bootRom[index];
                    case >= BOOT_ROM_END_ADDRESS and < Cartridge.SWITCHABLE_ROM_BANK_END_ADDRESS: return cartridge[index];
                    case >= VRAM.VRAM_START_ADDRESS and < VRAM.VRAM_END_ADDRESS: return ppu[index];
                    case >= Cartridge.EXTERNAL_RAM_BANK_START_ADDRESS and < Cartridge.EXTERNAL_RAM_BANK_END_ADDRESS: return cartridge[index];
                    case >= RAM.INTERNAL_RAM_START_ADDRESS and < RAM.INTERNAL_RAM_END_ADDRESS: return ram.InternalRam[index - RAM.INTERNAL_RAM_START_ADDRESS];
                    case >= INTERNAL_RAM_ECHO_START_ADDRESS and < INTERNAL_RAM_ECHO_END_ADDRESS: return ram.InternalRam[index - INTERNAL_RAM_ECHO_START_ADDRESS];
                    case >= VRAM.OAM_START_ADDRESS and < VRAM.OAM_END_ADDRESS: return ppu[index];
                    case >= UNUSABLE_START_ADDRESS and < UNUSABLE_END_ADDRESS: return 0xFF;
                    case >= IO.IO_PORTS_START_ADDRESS and < IO.IO_PORTS_END_ADDRESS: return io[index - IO.IO_PORTS_START_ADDRESS];
                    case >= RAM.HIGH_RAM_START_ADDRESS and < RAM.HIGH_RAM_END_ADDRESS: return ram.HighRam[index - RAM.HIGH_RAM_START_ADDRESS];
                    case IO.INTERRUPT_ENABLE_REGISTER_ADDRESS: return io[0xFF];
                    default: throw new IndexOutOfRangeException($"Tried to read from out of range memory: 0x{index:X4}");
                }
            }

            set
            {
                if (dma.IsBusy && index < IO.IO_PORTS_START_ADDRESS)
                {
                    Utils.Log(LogType.Warning, $"Tried to write to 0x{index:X4} during OAM transfer.");
                    return;
                }

                switch (index)
                {
                    case >= BOOT_ROM_START_ADDRESS and < BOOT_ROM_END_ADDRESS: if (io.BootROMDisabled) cartridge[index] = value; break;
                    case >= BOOT_ROM_END_ADDRESS and < Cartridge.SWITCHABLE_ROM_BANK_END_ADDRESS: cartridge[index] = value; break;
                    case >= VRAM.VRAM_START_ADDRESS and < VRAM.VRAM_END_ADDRESS: ppu[index] = value; break;
                    case >= Cartridge.EXTERNAL_RAM_BANK_START_ADDRESS and < Cartridge.EXTERNAL_RAM_BANK_END_ADDRESS: cartridge[index] = value; break;
                    case >= RAM.INTERNAL_RAM_START_ADDRESS and < RAM.INTERNAL_RAM_END_ADDRESS: ram.InternalRam[index - RAM.INTERNAL_RAM_START_ADDRESS] = value; break;
                    case >= INTERNAL_RAM_ECHO_START_ADDRESS and < INTERNAL_RAM_ECHO_END_ADDRESS: ram.InternalRam[index - INTERNAL_RAM_ECHO_START_ADDRESS] = value; break;
                    case >= VRAM.OAM_START_ADDRESS and < VRAM.OAM_END_ADDRESS: ppu[index] = value; break;
                    case >= UNUSABLE_START_ADDRESS and < UNUSABLE_END_ADDRESS: break;
                    case >= IO.IO_PORTS_START_ADDRESS and < IO.IO_PORTS_END_ADDRESS: io[index - IO.IO_PORTS_START_ADDRESS] = value; break;
                    case >= RAM.HIGH_RAM_START_ADDRESS and < RAM.HIGH_RAM_END_ADDRESS: ram.HighRam[index - RAM.HIGH_RAM_START_ADDRESS] = value; break;
                    case IO.INTERRUPT_ENABLE_REGISTER_ADDRESS: io[0xFF] = value; break;
                    default: throw new IndexOutOfRangeException($"Tried to write to out of range memory: 0x{index:X4}");
                }
            }
        }
    }
}