using DamageBoy.Core.State;
using System.IO;

namespace DamageBoy.Core
{
    class Serial : IState
    {
        readonly InterruptHandler interruptHandler;

        public enum STCShiftClock : byte { ExternalClock, InternalClock }
        //public enum STCClockSpeed : byte { Normal, Fast }
        public enum STCTransferStartFlag : byte { NoTransferInProgressOrRequested, TransferInProgressOrRequested }

        public STCShiftClock ShiftClock { get; set; }
        //STCClockSpeed stcClockSpeed; // Only in CGB

        STCTransferStartFlag transferStartFlag;
        public STCTransferStartFlag TransferStartFlag
        {
            get => transferStartFlag;
            set => BeginTransfer(value);
        }

        public byte TransferData { get; set; }

        int clocksToWait;
        byte bitsPendingToBeTransfered;

        const int INTERNAL_CLOCK_FREQUENCY = 8192;

        public Serial(InterruptHandler interruptHandler)
        {
            this.interruptHandler = interruptHandler;
        }

        public void Update()
        {
            if (TransferStartFlag == STCTransferStartFlag.NoTransferInProgressOrRequested) return;
            if (ShiftClock == STCShiftClock.ExternalClock) return;

            clocksToWait -= 4;
            if (clocksToWait <= 0)
            {
                clocksToWait = CPU.CPU_CLOCKS / INTERNAL_CLOCK_FREQUENCY;

                SendBit(Helpers.GetBit(TransferData, 7));
                TransferData <<= 1;
                TransferData = Helpers.SetBit(TransferData, 0, ReceiveBit());

                bitsPendingToBeTransfered--;

                if (bitsPendingToBeTransfered <= 0)
                {
                    TransferStartFlag = STCTransferStartFlag.NoTransferInProgressOrRequested;
                    interruptHandler.RequestSerialTransferCompletion = true;
                }
            }
        }

        void BeginTransfer(STCTransferStartFlag transferStartFlag)
        {
            if (this.transferStartFlag == transferStartFlag) return;
            this.transferStartFlag = transferStartFlag;

            if (ShiftClock == STCShiftClock.ExternalClock) return;

            bitsPendingToBeTransfered = 8;
            clocksToWait = 0;
        }

        void SendBit(bool bit)
        {
            // No actual connectivity implemented, so don't send anything.
        }

        bool ReceiveBit()
        {
            // Always receive 1, which will form a 0xFF byte.
            // That's what happens when no GameBoy is connected.
            return true;
        }

        public void SaveOrLoadState(Stream stream, BinaryWriter bw, BinaryReader br, bool save)
        {
            ShiftClock = (STCShiftClock)SaveState.SaveLoadValue(bw, br, save, (byte)ShiftClock);
            TransferStartFlag = (STCTransferStartFlag)SaveState.SaveLoadValue(bw, br, save, (byte)TransferStartFlag);
            TransferData = SaveState.SaveLoadValue(bw, br, save, TransferData);

            clocksToWait = SaveState.SaveLoadValue(bw, br, save, clocksToWait);
            bitsPendingToBeTransfered = SaveState.SaveLoadValue(bw, br, save, bitsPendingToBeTransfered);
        }
    }
}