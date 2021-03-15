using System;

namespace GBEmu.Core
{
    class DMA
    {
        readonly Cartridge cartridge;
        readonly RAM ram;
        readonly VRAM vram;

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

        public void Begin(byte sourceBaseAddress)
        {
            sourceAddress = (ushort)(sourceBaseAddress << 8);

            if (/*sourceAddress < VRAM.VRAM_START_ADDRESS || */sourceAddress >= RAM.INTERNAL_RAM_END_ADDRESS)
            {
                throw new InvalidOperationException($"Tried to execute a DMA transfer from an invalid address: {sourceAddress}.");
            }

            // DMA transfer doesn't start right away, but after one CPU cycle
            // So set this to -4 to ignore one cycle in Update().
            currentOffset = -4;
        }

        public void Update()
        {
            if (!IsBusy) return;

            if (currentOffset < 0)
            {
                currentOffset += 4;
                return;
            }

            this[VRAM.OAM_START_ADDRESS + currentOffset + 0] = this[sourceAddress + currentOffset + 0];
            this[VRAM.OAM_START_ADDRESS + currentOffset + 1] = this[sourceAddress + currentOffset + 1];
            this[VRAM.OAM_START_ADDRESS + currentOffset + 2] = this[sourceAddress + currentOffset + 2];
            this[VRAM.OAM_START_ADDRESS + currentOffset + 3] = this[sourceAddress + currentOffset + 3];
            currentOffset += 4;
        }
    }
}