namespace GBEmu.Core
{
    class Timer
    {
        readonly IO io;

        public enum TimerControlSpeeds : byte { Hz4096, Hz262144, Hz65536, Hz16384 }

        int dividerClocksToWait;
        int timerClocksToWait;

        bool timerHasOverflown;
        int timerOverflowWaitCycles;

        public Timer(IO io)
        {
            this.io = io;
        }

        public void Update()
        {
            dividerClocksToWait -= 4;
            if (dividerClocksToWait <= 0)
            {
                dividerClocksToWait = 256;
                io.DIV++;
                //Utils.Log("Divider: " + io.DividerRegister);
            }

            if (io.TACTimerEnable)
            {
                timerClocksToWait -= 4;
                if (timerClocksToWait <= 0)
                {
                    switch (io.TACInputClockSelect)
                    {
                        default:
                        case TimerControlSpeeds.Hz4096:
                            timerClocksToWait = 1024;
                            break;
                        case TimerControlSpeeds.Hz262144:
                            timerClocksToWait = 16;
                            break;
                        case TimerControlSpeeds.Hz65536:
                            timerClocksToWait = 64;
                            break;
                        case TimerControlSpeeds.Hz16384:
                            timerClocksToWait = 256;
                            break;
                    }

                    io.TIMA++;
                    if (io.TIMA == 0)
                    {
                        timerOverflowWaitCycles = 8;
                        timerHasOverflown = true;
                    }
                }
            }

            if (timerHasOverflown)
            {
                timerOverflowWaitCycles -= 4;
                if (timerOverflowWaitCycles <= 0)
                {
                    timerHasOverflown = false;

                    io.TIMA = io.TMA;
                    io.InterruptRequestTimerOverflow = true;
                }
            }
        }
    }
}