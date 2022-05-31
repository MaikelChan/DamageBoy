using DamageBoy.Core.State;
using System.IO;

namespace DamageBoy.Core;

class Timer : IState
{
    readonly InterruptHandler interruptHandler;

    public enum TimerClockSpeeds : byte { Hz4096, Hz262144, Hz65536, Hz16384 }

    public byte Divider { get; set; }
    public bool TimerEnable { get; set; }
    public TimerClockSpeeds TimerClockSpeed { get; set; }
    public byte TimerCounter { get; set; }
    public byte TimerModulo { get; set; }

    int dividerClocksToWait;
    int timerClocksToWait;

    bool timerHasOverflown;
    int timerOverflowWaitCycles;

    public Timer(InterruptHandler interruptHandler)
    {
        this.interruptHandler = interruptHandler;
    }

    public void Update()
    {
        dividerClocksToWait -= 4;
        if (dividerClocksToWait <= 0)
        {
            dividerClocksToWait = 256;
            Divider++;
            //Utils.Log("Divider: " + io.DividerRegister);
        }

        if (TimerEnable)
        {
            timerClocksToWait -= 4;
            if (timerClocksToWait <= 0)
            {
                switch (TimerClockSpeed)
                {
                    default:
                    case TimerClockSpeeds.Hz4096:
                        timerClocksToWait = CPU.CPU_CLOCKS / 4096;
                        break;
                    case TimerClockSpeeds.Hz262144:
                        timerClocksToWait = CPU.CPU_CLOCKS / 262144;
                        break;
                    case TimerClockSpeeds.Hz65536:
                        timerClocksToWait = CPU.CPU_CLOCKS / 65536;
                        break;
                    case TimerClockSpeeds.Hz16384:
                        timerClocksToWait = CPU.CPU_CLOCKS / 16384;
                        break;
                }

                TimerCounter++;
                if (TimerCounter == 0)
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

                TimerCounter = TimerModulo;
                interruptHandler.RequestTimerOverflow = true;
            }
        }
    }

    public void SaveOrLoadState(Stream stream, BinaryWriter bw, BinaryReader br, bool save)
    {
        Divider = SaveState.SaveLoadValue(bw, br, save, Divider);
        TimerEnable = SaveState.SaveLoadValue(bw, br, save, TimerEnable);
        TimerClockSpeed = (TimerClockSpeeds)SaveState.SaveLoadValue(bw, br, save, (byte)TimerClockSpeed);
        TimerCounter = SaveState.SaveLoadValue(bw, br, save, TimerCounter);
        TimerModulo = SaveState.SaveLoadValue(bw, br, save, TimerModulo);

        dividerClocksToWait = SaveState.SaveLoadValue(bw, br, save, dividerClocksToWait);
        timerClocksToWait = SaveState.SaveLoadValue(bw, br, save, timerClocksToWait);

        timerHasOverflown = SaveState.SaveLoadValue(bw, br, save, timerHasOverflown);
        timerOverflowWaitCycles = SaveState.SaveLoadValue(bw, br, save, timerOverflowWaitCycles);
    }
}