using DamageBoy.Core.State;
using System;
using System.IO;

namespace DamageBoy.Core;

class WRAM : IState
{
    readonly GameBoyModes gbMode;

#if GBC
    public byte Bank { get; set; }
#endif

    readonly byte[] bytes;

    public const ushort START_ADDRESS = 0xC000;
    public const ushort END_ADDRESS = 0xE000;
    public const ushort DMG_SIZE = END_ADDRESS - START_ADDRESS;
#if GBC
    public const ushort CGB_SIZE = 0x8000;
#endif

    public const ushort ECHO_START_ADDRESS = 0xE000;
    public const ushort ECHO_END_ADDRESS = 0xFE00;

    public WRAM(GameBoyModes gbMode)
    {
        this.gbMode = gbMode;

#if GBC
        Bank = 1;
        bytes = new byte[gbMode == GameBoyModes.CGB ? CGB_SIZE : DMG_SIZE];
#else
        bytes = new byte[DMG_SIZE];
#endif
    }

    public byte this[int index]
    {
        get
        {
            if (gbMode == GameBoyModes.CGB)
            {
                switch (index)
                {
                    case >= 0x0 and < 0x1000: return bytes[index];
                    case >= 0x1000 and < 0x2000: return bytes[(Bank << 12) + index - 0x1000];
                    default: throw new InvalidOperationException($"Tried to read from invalid Internal RAM address: 0x{index:X4}");
                }
            }
            else
            {
                return bytes[index];
            }
        }

        set
        {
            if (gbMode == GameBoyModes.CGB)
            {
                switch (index)
                {
                    case >= 0x0 and < 0x1000: bytes[index] = value; break;
                    case >= 0x1000 and < 0x2000: bytes[(Bank << 12) + index - 0x1000] = value; break;
                    default: throw new InvalidOperationException($"Tried to write to invalid Internal RAM address: 0x{index:X4}");
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
#if GBC
        if (gbMode == GameBoyModes.CGB)
        {
            Bank = SaveState.SaveLoadValue(bw, br, save, Bank);
        }
#endif
    }
}