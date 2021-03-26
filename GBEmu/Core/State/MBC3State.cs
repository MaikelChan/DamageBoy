
namespace GBEmu.Core.State
{
    class MBC3State : MemoryBankControllerState
    {
        public byte RomBank { get; set; }
        public byte RamOrRtcBank { get; set; }

        public bool IsRtcLatched { get; set; }
        public byte LatchedSeconds { get; set; }
        public byte LatchedMinutes { get; set; }
        public byte LatchedHours { get; set; }
        public byte LatchedDaysLo { get; set; }
        public byte LatchedDaysHi { get; set; }
    }
}