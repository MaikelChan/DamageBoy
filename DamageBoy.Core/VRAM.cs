using DamageBoy.Core.State;
using System.IO;

namespace DamageBoy.Core;

class VRAM : IState
{
    public byte[] VRam { get; }
    public byte[] Oam { get; }

    public const ushort VRAM_START_ADDRESS = 0x8000;
    public const ushort VRAM_END_ADDRESS = 0xA000;
    public const ushort VRAM_SIZE = VRAM_END_ADDRESS - VRAM_START_ADDRESS;

    public const ushort OAM_START_ADDRESS = 0xFE00;
    public const ushort OAM_END_ADDRESS = 0xFEA0;
    public const ushort OAM_SIZE = OAM_END_ADDRESS - OAM_START_ADDRESS;

    public const ushort UNUSABLE_START_ADDRESS = 0xFEA0;
    public const ushort UNUSABLE_END_ADDRESS = 0xFF00;

    public VRAM()
    {
        VRam = new byte[VRAM_SIZE];
        Oam = new byte[OAM_SIZE];
    }

    public void SaveOrLoadState(Stream stream, BinaryWriter bw, BinaryReader br, bool save)
    {
        SaveState.SaveLoadArray(stream, save, VRam, VRAM_SIZE);
        SaveState.SaveLoadArray(stream, save, Oam, OAM_SIZE);
    }
}