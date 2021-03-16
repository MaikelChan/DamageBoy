using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBEmu.Core
{
    class Sound
    {
        readonly Action<SoundState> soundUpdateCallback;

        readonly SoundState soundState;

        public enum SweepTypes : byte { Increase, Decrease }
        public enum EnvelopeDirections : byte { Decrease, Increase }
        public enum WavePatternDuties : byte { Percent12_5, Percent25, Percent50, Percent75 }

        int clocksToWait;

        public Sound(Action<SoundState> soundUpdateCallback)
        {
            this.soundUpdateCallback = soundUpdateCallback;

            soundState = new SoundState();
        }

        public void Update()
        {
            clocksToWait -= 4;
            if (clocksToWait > 0) return;

            clocksToWait = 4;

            soundUpdateCallback?.Invoke(soundState);
        }
    }

    class SoundState
    {
        public bool Channel1IsEnabled;
        public bool Channel1IsContinuous;
        public float Channel1Frequency;
        public float Channel1InitialVolume;
        public Sound.EnvelopeDirections Channel1EnvelopeDirection;
        public float Channel1LengthEnvelopeSteps;
        public Sound.WavePatternDuties Channel1WavePatternDuty;
        public float Channel1Length;
        public float Channel1SweepTime;
        public Sound.SweepTypes Channel1SweepType;
        public float Channel1SweepShift;

        public bool Channel2IsEnabled;
        public bool Channel2IsContinuous;
        public float Channel2Frequency;
        public float Channel2InitialVolume;
        public Sound.EnvelopeDirections Channel2EnvelopeDirection;
        public float Channel2LengthEnvelopeSteps;
        public Sound.WavePatternDuties Channel2WavePatternDuty;
        public float Channel2Length;
    }
}