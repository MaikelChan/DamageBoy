using GBEmu.Core.State;

namespace GBEmu.Core
{
    class InterruptHandler : IState
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

        public void GetState(SaveState state)
        {
            state.RequestVerticalBlanking = RequestVerticalBlanking;
            state.RequestLCDCSTAT = RequestLCDCSTAT;
            state.RequestTimerOverflow = RequestTimerOverflow;
            state.RequestSerialTransferCompletion = RequestSerialTransferCompletion;
            state.RequestJoypad = RequestJoypad;

            state.EnableVerticalBlanking = EnableVerticalBlanking;
            state.EnableLCDCSTAT = EnableLCDCSTAT;
            state.EnableTimerOverflow = EnableTimerOverflow;
            state.EnableSerialTransferCompletion = EnableSerialTransferCompletion;
            state.EnableJoypad = EnableJoypad;

            state.EnableUnusedBits = EnableUnusedBits;
        }

        public void SetState(SaveState state)
        {
            RequestVerticalBlanking = state.RequestVerticalBlanking;
            RequestLCDCSTAT = state.RequestLCDCSTAT;
            RequestTimerOverflow = state.RequestTimerOverflow;
            RequestSerialTransferCompletion = state.RequestSerialTransferCompletion;
            RequestJoypad = state.RequestJoypad;

            EnableVerticalBlanking = state.EnableVerticalBlanking;
            EnableLCDCSTAT = state.EnableLCDCSTAT;
            EnableTimerOverflow = state.EnableTimerOverflow;
            EnableSerialTransferCompletion = state.EnableSerialTransferCompletion;
            EnableJoypad = state.EnableJoypad;

            EnableUnusedBits = state.EnableUnusedBits;
        }
    }
}