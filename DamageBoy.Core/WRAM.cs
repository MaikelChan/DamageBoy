using DamageBoy.Core.State;
using System;
using System.IO;

namespace DamageBoy.Core;

class WRAM : IState
{
    readonly GameBoy gameBoy;

    public byte Bank { get; set; }
    readonly byte[] bytes;

    public const ushort START_ADDRESS = 0xC000;
    public const ushort END_ADDRESS = 0xE000;
    public const ushort DMG_SIZE = END_ADDRESS - START_ADDRESS;
    public const ushort CGB_SIZE = 0x8000;

    public const ushort ECHO_START_ADDRESS = 0xE000;
    public const ushort ECHO_END_ADDRESS = 0xFE00;

    public WRAM(GameBoy gameBoy)
    {
        this.gameBoy = gameBoy;

        Bank = 1;
        bytes = new byte[gameBoy.IsColorMode ? CGB_SIZE : DMG_SIZE];
    }

    public byte this[int index]
    {
        get
        {
            if (gameBoy.IsColorMode)
            {
                switch (index)
                {
                    case >= 0x0 and < 0x1000: return bytes[index];
                    case >= 0x1000 and < 0x2000: return bytes[(Bank << 12) + index - 0x1000];
                    default: throw new InvalidOperationException($"Tried to read from invalid WRAM address: 0x{index:X4}");
                }
            }
            else
            {
                return bytes[index];
            }
        }

        set
        {
            if (gameBoy.IsColorMode)
            {
                switch (index)
                {
                    case >= 0x0 and < 0x1000: bytes[index] = value; break;
                    case >= 0x1000 and < 0x2000: bytes[(Bank << 12) + index - 0x1000] = value; break;
                    default: throw new InvalidOperationException($"Tried to write to invalid WRAM address: 0x{index:X4}");
                }
            }
            else
            {
                bytes[index] = value;
            }
        }
    }

    public void SaveOrLoadState(Stream stream, BinaryWriter bw, BinaryReader br, bool save)
    {
        SaveState.SaveLoadArray(stream, save, bytes, bytes.Length);

        if (gameBoy.IsColorMode)
        {
            Bank = SaveState.SaveLoadValue(bw, br, save, Bank);
        }
    }
}