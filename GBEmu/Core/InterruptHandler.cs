namespace GBEmu.Core
{
    class InterruptHandler
    {
        public bool RequestVerticalBlanking { get; set; }
        public bool RequestLCDCSTAT { get; set; }
        public bool RequestTimerOverflow { get; set; }
        public bool RequestSerialTransferCompletion { get; set; }
        public bool RequestJoypad { get; set; }

        public bool EnableVerticalBlanking { get; set; }
        public bool EnableLCDCSTAT { get; set; }
        public bool EnableTimerOverflow { get; set; }
        public bool EnableSerialTransferCompletion { get; set; }
        public bool EnableJoypad { get; set; }
        public byte EnableUnusedBits { get; set; } // In this case, unused bits are actually writable and readable
    }
}