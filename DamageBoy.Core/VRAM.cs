using DamageBoy.Core.State;
using System.IO;

namespace DamageBoy.Core;

class VRAM : IState
{
    readonly GameBoyModes gbMode;

#if GBC
    public byte Bank { get; set; }
#endif

    readonly byte[] bytes;

    public const ushort START_ADDRESS = 0x8000;
    public const ushort END_ADDRESS = 0xA000;
    public const ushort DMG_SIZE = END_ADDRESS - START_ADDRESS;
#if GBC
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

#if GBC
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
            if (gbMode == GameBoyModes.CGB)
            {
                //return bytes[index + (DMG_SIZE * Bank) - START_ADDRESS];
                if (index < END_TILE_DATA_ADDRESS) return bytes[index + (DMG_SIZE * Bank) - START_ADDRESS];
                else return bytes[index - START_ADDRESS];
            }
            else
            {
                return bytes[index - START_ADDRESS];
            }
        }
        set
        {
            if (gbMode == GameBoyModes.CGB)
            {
                bytes[index + (DMG_SIZE * Bank) - START_ADDRESS] = value;
                //if (index < END_TILE_DATA_ADDRESS) bytes[index + (DMG_SIZE * Bank) - START_ADDRESS] = value;
                //else bytes[index - START_ADDRESS] = value;
            }
            else
            {
                bytes[index - START_ADDRESS] = value;
            }
        }
    }

    public void SaveOrLoadState(Stream stream, BinaryWriter bw, BinaryReader br, bool save)
    {
        SaveState.SaveLoadArray(stream, save, bytes, DMG_SIZE);
    }
}