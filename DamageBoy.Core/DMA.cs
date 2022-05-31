using DamageBoy.Core.State;
using System;
using System.IO;

namespace DamageBoy.Core;

class DMA : IState
{
    readonly Cartridge cartridge;
    readonly RAM ram;
    readonly VRAM vram;

    public byte SourceBaseAddress
    {
        get { return (byte)(sourceAddress >> 8); }
        set { sourceAddress = (ushort)(value << 8); Begin(); }
    }

    public bool IsBusy => currentOffset < VRAM.OAM_SIZE;

    ushort sourceAddress;
    int currentOffset;

    public DMA(Cartridge cartridge, RAM ram, VRAM vram)
    {
        this.cartridge = cartridge;
        this.ram = ram;
        this.vram = vram;

        currentOffset = VRAM.OAM_SIZE;
    }

    byte this[int index]
    {
        get
        {
            if (index >= 0xE000) index -= 0x2000;

            switch (index)
            {
                case >= Cartridge.ROM_BANK_START_ADDRESS and < Cartridge.SWITCHABLE_ROM_BANK_END_ADDRESS: return cartridge[index];
                case >= VRAM.VRAM_START_ADDRESS and < VRAM.VRAM_END_ADDRESS: return vram.VRam[index - VRAM.VRAM_START_ADDRESS];
                case >= Cartridge.EXTERNAL_RAM_BANK_START_ADDRESS and < Cartridge.EXTERNAL_RAM_BANK_END_ADDRESS: return cartridge[index];
                case >= RAM.INTERNAL_RAM_START_ADDRESS and < RAM.INTERNAL_RAM_END_ADDRESS: return ram.InternalRam[index - RAM.INTERNAL_RAM_START_ADDRESS];
                default: throw new IndexOutOfRangeException($"DMA tried to read from out of range memory: 0x{index:X4}");
            }
        }

        set
        {
            switch (index)
            {
                case >= VRAM.OAM_START_ADDRESS and < VRAM.OAM_END_ADDRESS: vram.Oam[index - VRAM.OAM_START_ADDRESS] = value; break;
                default: throw new IndexOutOfRangeException($"DMA tried to write to out of range memory: 0x{index:X4}");
            }
        }
    }

    public void Update()
    {
        if (!IsBusy) return;

        if (currentOffset < 0)
        {
            currentOffset++;
            return;
        }

        this[VRAM.OAM_START_ADDRESS + currentOffset] = this[sourceAddress + currentOffset];
        currentOffset++;
    }

    void Begin()
    {
        // DMA transfer doesn't start right away.
        // Setting this to -3 gives enough delay to be able to pass Mooneye's oam_dma_timing test.
        // Probably this is a bit of a hack?
        currentOffset = -3;
    }

    public void SaveOrLoadState(Stream stream, BinaryWriter bw, BinaryReader br, bool save)
    {
        sourceAddress = SaveState.SaveLoadValue(bw, br, save, sourceAddress);
        currentOffset = SaveState.SaveLoadValue(bw, br, save, currentOffset);
    }
}