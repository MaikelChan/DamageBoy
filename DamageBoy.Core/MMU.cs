using System;

namespace DamageBoy.Core;

class MMU
{
    readonly GameBoy gameBoy;
    readonly IO io;
    readonly WRAM wram;
    readonly HRAM hram;
    readonly PPU ppu;
    readonly DMA dma;
    readonly byte[] bootRom;
    readonly Cartridge cartridge;

    public MMU(GameBoy gameBoy, IO io, WRAM wram, HRAM hram, PPU ppu, DMA dma, byte[] bootRom, Cartridge cartridge)
    {
        this.gameBoy = gameBoy;
        this.io = io;
        this.wram = wram;
        this.hram = hram;
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

            if (gameBoy.IsColorMode)
            {
                switch (index)
                {
                    case >= GameBoy.CGB_BOOT_ROM_START_ADDRESS and < GameBoy.CGB_BOOT_ROM_FIRST_PART_END_ADDRESS: return io.BootROMDisabled ? cartridge[index] : bootRom[index];
                    case >= GameBoy.CGB_BOOT_ROM_FIRST_PART_END_ADDRESS and < GameBoy.CGB_BOOT_ROM_SECOND_PART_START_ADDRESS: return cartridge[index];
                    case >= GameBoy.CGB_BOOT_ROM_SECOND_PART_START_ADDRESS and < GameBoy.CGB_BOOT_ROM_END_ADDRESS: return io.BootROMDisabled ? cartridge[index] : bootRom[index];
                    case >= GameBoy.CGB_BOOT_ROM_END_ADDRESS and < Cartridge.SWITCHABLE_ROM_BANK_END_ADDRESS: return cartridge[index];
                    case >= VRAM.START_ADDRESS and < VRAM.END_ADDRESS: return ppu[index];
                    case >= Cartridge.EXTERNAL_RAM_BANK_START_ADDRESS and < Cartridge.EXTERNAL_RAM_BANK_END_ADDRESS: return cartridge[index];
                    case >= WRAM.START_ADDRESS and < WRAM.END_ADDRESS: return wram[index - WRAM.START_ADDRESS];
                    case >= WRAM.ECHO_START_ADDRESS and < WRAM.ECHO_END_ADDRESS: return wram[index - WRAM.ECHO_START_ADDRESS];
                    case >= OAM.START_ADDRESS and < OAM.END_ADDRESS: return ppu[index];
                    case >= VRAM.UNUSABLE_START_ADDRESS and < VRAM.UNUSABLE_END_ADDRESS: return 0xFF;
                    case >= IO.IO_PORTS_START_ADDRESS and < IO.IO_PORTS_END_ADDRESS: return io[index - IO.IO_PORTS_START_ADDRESS];
                    case >= HRAM.START_ADDRESS and < HRAM.END_ADDRESS: return hram[index - HRAM.START_ADDRESS];
                    case IO.INTERRUPT_ENABLE_REGISTER_ADDRESS: return io[0xFF];
                    default: throw new IndexOutOfRangeException($"Tried to read from out of range memory: 0x{index:X4}");
                }
            }
            else
            {
                switch (index)
                {
                    case >= GameBoy.DMG_BOOT_ROM_START_ADDRESS and < GameBoy.DMG_BOOT_ROM_END_ADDRESS: return io.BootROMDisabled ? cartridge[index] : bootRom[index];
                    case >= GameBoy.DMG_BOOT_ROM_END_ADDRESS and < Cartridge.SWITCHABLE_ROM_BANK_END_ADDRESS: return cartridge[index];
                    case >= VRAM.START_ADDRESS and < VRAM.END_ADDRESS: return ppu[index];
                    case >= Cartridge.EXTERNAL_RAM_BANK_START_ADDRESS and < Cartridge.EXTERNAL_RAM_BANK_END_ADDRESS: return cartridge[index];
                    case >= WRAM.START_ADDRESS and < WRAM.END_ADDRESS: return wram[index - WRAM.START_ADDRESS];
                    case >= WRAM.ECHO_START_ADDRESS and < WRAM.ECHO_END_ADDRESS: return wram[index - WRAM.ECHO_START_ADDRESS];
                    case >= OAM.START_ADDRESS and < OAM.END_ADDRESS: return ppu[index];
                    case >= VRAM.UNUSABLE_START_ADDRESS and < VRAM.UNUSABLE_END_ADDRESS: return 0xFF;
                    case >= IO.IO_PORTS_START_ADDRESS and < IO.IO_PORTS_END_ADDRESS: return io[index - IO.IO_PORTS_START_ADDRESS];
                    case >= HRAM.START_ADDRESS and < HRAM.END_ADDRESS: return hram[index - HRAM.START_ADDRESS];
                    case IO.INTERRUPT_ENABLE_REGISTER_ADDRESS: return io[0xFF];
                    default: throw new IndexOutOfRangeException($"Tried to read from out of range memory: 0x{index:X4}");
                }
            }
        }

        set
        {
            if (dma.IsBusy && index < IO.IO_PORTS_START_ADDRESS)
            {
                Utils.Log(LogType.Warning, $"Tried to write to 0x{index:X4} during OAM transfer.");
                return;
            }

            if (gameBoy.IsColorMode)
            {
                switch (index)
                {
                    case >= GameBoy.CGB_BOOT_ROM_START_ADDRESS and < GameBoy.CGB_BOOT_ROM_FIRST_PART_END_ADDRESS: if (io.BootROMDisabled) cartridge[index] = value; break;
                    case >= GameBoy.CGB_BOOT_ROM_FIRST_PART_END_ADDRESS and < GameBoy.CGB_BOOT_ROM_SECOND_PART_START_ADDRESS: cartridge[index] = value; break;
                    case >= GameBoy.CGB_BOOT_ROM_SECOND_PART_START_ADDRESS and < GameBoy.CGB_BOOT_ROM_END_ADDRESS: if (io.BootROMDisabled) cartridge[index] = value; break;
                    case >= GameBoy.CGB_BOOT_ROM_END_ADDRESS and < Cartridge.SWITCHABLE_ROM_BANK_END_ADDRESS: cartridge[index] = value; break;
                    case >= VRAM.START_ADDRESS and < VRAM.END_ADDRESS: ppu[index] = value; break;
                    case >= Cartridge.EXTERNAL_RAM_BANK_START_ADDRESS and < Cartridge.EXTERNAL_RAM_BANK_END_ADDRESS: cartridge[index] = value; break;
                    case >= WRAM.START_ADDRESS and < WRAM.END_ADDRESS: wram[index - WRAM.START_ADDRESS] = value; break;
                    case >= WRAM.ECHO_START_ADDRESS and < WRAM.ECHO_END_ADDRESS: wram[index - WRAM.ECHO_START_ADDRESS] = value; break;
                    case >= OAM.START_ADDRESS and < OAM.END_ADDRESS: ppu[index] = value; break;
                    case >= VRAM.UNUSABLE_START_ADDRESS and < VRAM.UNUSABLE_END_ADDRESS: break;
                    case >= IO.IO_PORTS_START_ADDRESS and < IO.IO_PORTS_END_ADDRESS: io[index - IO.IO_PORTS_START_ADDRESS] = value; break;
                    case >= HRAM.START_ADDRESS and < HRAM.END_ADDRESS: hram[index - HRAM.START_ADDRESS] = value; break;
                    case IO.INTERRUPT_ENABLE_REGISTER_ADDRESS: io[0xFF] = value; break;
                    default: throw new IndexOutOfRangeException($"Tried to write to out of range memory: 0x{index:X4}");
                }
            }
            else
            {
                switch (index)
                {
                    case >= GameBoy.DMG_BOOT_ROM_START_ADDRESS and < GameBoy.DMG_BOOT_ROM_END_ADDRESS: if (io.BootROMDisabled) cartridge[index] = value; break;
                    case >= GameBoy.DMG_BOOT_ROM_END_ADDRESS and < Cartridge.SWITCHABLE_ROM_BANK_END_ADDRESS: cartridge[index] = value; break;
                    case >= VRAM.START_ADDRESS and < VRAM.END_ADDRESS: ppu[index] = value; break;
                    case >= Cartridge.EXTERNAL_RAM_BANK_START_ADDRESS and < Cartridge.EXTERNAL_RAM_BANK_END_ADDRESS: cartridge[index] = value; break;
                    case >= WRAM.START_ADDRESS and < WRAM.END_ADDRESS: wram[index - WRAM.START_ADDRESS] = value; break;
                    case >= WRAM.ECHO_START_ADDRESS and < WRAM.ECHO_END_ADDRESS: wram[index - WRAM.ECHO_START_ADDRESS] = value; break;
                    case >= OAM.START_ADDRESS and < OAM.END_ADDRESS: ppu[index] = value; break;
                    case >= VRAM.UNUSABLE_START_ADDRESS and < VRAM.UNUSABLE_END_ADDRESS: break;
                    case >= IO.IO_PORTS_START_ADDRESS and < IO.IO_PORTS_END_ADDRESS: io[index - IO.IO_PORTS_START_ADDRESS] = value; break;
                    case >= HRAM.START_ADDRESS and < HRAM.END_ADDRESS: hram[index - HRAM.START_ADDRESS] = value; break;
                    case IO.INTERRUPT_ENABLE_REGISTER_ADDRESS: io[0xFF] = value; break;
                    default: throw new IndexOutOfRangeException($"Tried to write to out of range memory: 0x{index:X4}");
                }
            }
        }
    }

    //public void CorruptOAM(ushort modifiedAddress)
    //{
    //    ppu.CorruptOAM(modifiedAddress);
    //}
}