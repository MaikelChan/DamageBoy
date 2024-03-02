using DamageBoy.Core.State;
using System;
using System.IO;

namespace DamageBoy.Core;

class HDMA : IState
{
    readonly Cartridge cartridge;
    readonly WRAM wram;
    readonly VRAM vram;

    public byte SourceHighAddress { get; set; }
    public byte SourceLowAddress { get; set; }
    public byte DestinationHighAddress { get; set; }
    public byte DestinationLowAddress { get; set; }
    public TransferModes TransferMode { get; set; }
    public byte InitialLength { get; set; }
    public byte RemainingLength { get; private set; }

    public enum TransferModes : byte { GeneralPurpose, HBlank }

    public ushort SourceAddress => (ushort)(((SourceHighAddress << 8) | SourceLowAddress) & 0b1111_1111_1111_0000);
    public ushort DestinationAddress => (ushort)(((DestinationHighAddress << 8) | (DestinationLowAddress)) & 0b0001_1111_1111_0000);

    public bool IsBusy => RemainingLength > 0;

    const byte MAX_LENGTH = 0x7f;

    public HDMA(Cartridge cartridge, WRAM wram, VRAM vram)
    {
        this.cartridge = cartridge;
        this.wram = wram;
        this.vram = vram;

        SourceHighAddress = 0;
        SourceLowAddress = 0;
        DestinationHighAddress = 0;
        DestinationLowAddress = 0;
        TransferMode = TransferModes.GeneralPurpose;
        InitialLength = 0;

        RemainingLength = 0;
    }

    byte this[int index]
    {
        get
        {
            switch (index)
            {
                case >= Cartridge.ROM_BANK_START_ADDRESS and < Cartridge.SWITCHABLE_ROM_BANK_END_ADDRESS: return cartridge[index];
                case >= VRAM.START_ADDRESS and < VRAM.END_ADDRESS: Utils.Log(LogType.Warning, "HDMA with VRAM address as source is unexpected."); return vram[index];
                case >= Cartridge.EXTERNAL_RAM_BANK_START_ADDRESS and < Cartridge.EXTERNAL_RAM_BANK_END_ADDRESS: return cartridge[index];
                case >= WRAM.START_ADDRESS and < WRAM.END_ADDRESS: return wram[index - WRAM.START_ADDRESS];
                default: throw new IndexOutOfRangeException($"HDMA tried to read from out of range memory: 0x{index:X4}");
            }
        }

        set
        {
            switch (index)
            {
                case >= VRAM.START_ADDRESS and < VRAM.END_ADDRESS: vram[index] = value; break;
                default: throw new IndexOutOfRangeException($"HDMA tried to write to out of range memory: 0x{index:X4}");
            }
        }
    }

    public void Update()
    {

    }

    public void Begin()
    {
        RemainingLength = InitialLength;

        if (TransferMode == TransferModes.GeneralPurpose)
        {
            for (; ; )
            {
                for (int j = 0; j < 0x10; j++)
                {
                    int offset = ((InitialLength - RemainingLength) * 0x10) + j;
                    this[DestinationAddress + VRAM.START_ADDRESS + offset] = this[SourceAddress + offset];
                }

                RemainingLength--;
                if (RemainingLength == 0xFF)
                {
                    RemainingLength = 0;
                    break;
                }
            }
        }
        else
        {
            Utils.Log(LogType.Error, "NOT IMPLEMENTED HBLANK DMA!!!");
        }
    }

    public void SaveOrLoadState(Stream stream, BinaryWriter bw, BinaryReader br, bool save)
    {
        SourceHighAddress = SaveState.SaveLoadValue(bw, br, save, SourceHighAddress);
        SourceLowAddress = SaveState.SaveLoadValue(bw, br, save, SourceLowAddress);
        DestinationHighAddress = SaveState.SaveLoadValue(bw, br, save, DestinationHighAddress);
        DestinationLowAddress = SaveState.SaveLoadValue(bw, br, save, DestinationLowAddress);
        TransferMode = (TransferModes)SaveState.SaveLoadValue(bw, br, save, (byte)TransferMode);
        InitialLength = SaveState.SaveLoadValue(bw, br, save, InitialLength);
        RemainingLength = SaveState.SaveLoadValue(bw, br, save, RemainingLength);
    }
}