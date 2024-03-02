using DamageBoy.Core.State;
using System.IO;

namespace DamageBoy.Core;

class OAM : IState
{
    readonly byte[] bytes;

    public const ushort START_ADDRESS = 0xFE00;
    public const ushort END_ADDRESS = 0xFEA0;
    public const ushort SIZE = END_ADDRESS - START_ADDRESS;

    public const ushort UNUSABLE_START_ADDRESS = 0xFEA0;
    public const ushort UNUSABLE_END_ADDRESS = 0xFF00;

    public OAM()
    {
        bytes = new byte[SIZE];
    }

    public byte this[int index]
    {
        get => bytes[index];
        set => bytes[index] = value;
    }

    public void SaveOrLoadState(Stream stream, BinaryWriter bw, BinaryReader br, bool save)
    {
        SaveState.SaveLoadArray(stream, save, bytes, SIZE);
    }
}