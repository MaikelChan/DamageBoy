using System.IO;

namespace DamageBoy.Core.State;

internal interface IState
{
    void SaveOrLoadState(Stream stream, BinaryWriter bw, BinaryReader br, bool save);
}