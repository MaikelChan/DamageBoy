namespace GBEmu.Core
{
    class RAM
    {
        public byte[] InternalRam { get; }
        public byte[] HighRam { get; }

        public const ushort INTERNAL_RAM_START_ADDRESS = 0xC000;
        public const ushort INTERNAL_RAM_END_ADDRESS = 0xE000;
        public const ushort INTERNAL_RAM_SIZE = INTERNAL_RAM_END_ADDRESS - INTERNAL_RAM_START_ADDRESS;

        public const ushort HIGH_RAM_START_ADDRESS = 0xFF80;
        public const ushort HIGH_RAM_END_ADDRESS = 0xFFFF;
        public const ushort HIGH_RAM_SIZE = HIGH_RAM_END_ADDRESS - HIGH_RAM_START_ADDRESS;

        public RAM()
        {
            InternalRam = new byte[INTERNAL_RAM_SIZE];
            HighRam = new byte[HIGH_RAM_SIZE];
        }
    }
}