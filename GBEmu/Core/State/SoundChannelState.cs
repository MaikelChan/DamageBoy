using GBEmu.Core.Audio;

namespace GBEmu.Core.State
{
    internal abstract class SoundChannelState
    {
        public bool Enabled { get; set; }
        public LengthTypes LengthType { get; set; }

        public bool Output2 { get; set; }
        public bool Output1 { get; set; }

        public int CurrentLength { get; set; }
    }
}