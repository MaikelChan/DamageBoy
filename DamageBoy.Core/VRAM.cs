using DamageBoy.Core.State;
using System.IO;

namespace DamageBoy.Core;

class VRAM : IState
{
    readonly GameBoyModes gbMode;

#if IS_CGB
    public byte Bank { get; set; }
#endif

    readonly byte[] bytes;

    public const ushort START_ADDRESS = 0x8000;
    public const ushort END_ADDRESS = 0xA000;
    public const ushort DMG_SIZE = END_ADDRESS - START_ADDRESS;
#if IS_CGB
    public const ushort CGB_SIZE = DMG_SIZE << 1;
#endif

    public const ushort START_TILE_DATA_1_ADDRESS = START_ADDRESS;
    public const ushort START_TILE_DATA_2_ADDRESS = 0x8800;
    public const ushort START_TILE_DATA_3_ADDRESS = 0x9000;
    public const ushort END_TILE_DATA_ADDRESS = 0x9800;

    public const ushort START_TILE_MAP_1_ADDRESS = 0x9800;
    public const ushort START_TILE_MAP_2_ADDRESS = 0x9C00;
    public const ushort END_TILE_MAP_ADDRESS = END_ADDRESS;

    public const ushort UNUSABLE_START_ADDRESS = 0xFEA0;
    public const ushort UNUSABLE_END_ADDRESS = 0xFF00;

    public VRAM(GameBoyModes gbMode)
    {
        this.gbMode = gbMode;

#if IS_CGB
        Bank = 0;
        bytes = new byte[gbMode == GameBoyModes.CGB ? CGB_SIZE : DMG_SIZE];
#else
        bytes = new byte[DMG_SIZE];
#endif
    }

    public byte this[int index]
    {
        get
        {
            if (gbMode == GameBoyModes.CGB) return bytes[index + (DMG_SIZE * Bank) - START_ADDRESS];
            else return bytes[index - START_ADDRESS];
        }
        set
        {
            if (gbMode == GameBoyModes.CGB) bytes[index + (DMG_SIZE * Bank) - START_ADDRESS] = value;
            else bytes[index - START_ADDRESS] = value;
        }
    }

    public byte GetRawBytes(int address)
    {
        return bytes[address - START_ADDRESS];
    }

    public void SaveOrLoadState(Stream stream, BinaryWriter bw, BinaryReader br, bool save)
    {
        SaveState.SaveLoadArray(stream, save, bytes, DMG_SIZE);
#if IS_CGB
        if (gbMode == GameBoyModes.CGB)
        {
            Bank = SaveState.SaveLoadValue(bw, br, save, Bank);
        }
#endif
    }
}