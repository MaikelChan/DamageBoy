
namespace DamageBoy.Core
{
    public static class Constants
    {
        // Video

        public const byte RES_X = 160;
        public const byte RES_Y = 144;
        public const float ASPECT_RATIO = (float)RES_X / RES_Y;

        // Audio

        public const int SAMPLE_RATE = CPU.CPU_CLOCKS >> 7; // 32768Hz
        public const int SOUND_CHANNEL_COUNT = 4;
    }
}