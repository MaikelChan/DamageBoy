using DamageBoy.Core.State;
using System.IO;

namespace DamageBoy.Core;

class HRAM : IState
{
    readonly byte[] bytes;

    public const ushort START_ADDRESS = 0xFF80;
    public const ushort END_ADDRESS = 0xFFFF;
    public const ushort SIZE = END_ADDRESS - START_ADDRESS;

    public HRAM()
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