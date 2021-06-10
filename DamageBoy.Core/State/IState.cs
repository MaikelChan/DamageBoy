using System.IO;

namespace DamageBoy.Core.State
{
    internal interface IState
    {
        void LoadSaveState(Stream stream, BinaryWriter bw, BinaryReader br, bool save);
    }
}