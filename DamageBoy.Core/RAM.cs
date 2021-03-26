using DamageBoy.Core.State;
using System;

namespace DamageBoy.Core
{
    class RAM : IState
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

        public void GetState(SaveState state)
        {
            Array.Copy(InternalRam, state.InternalRam, INTERNAL_RAM_SIZE);
            Array.Copy(HighRam, state.HighRam, HIGH_RAM_SIZE);
        }

        public void SetState(SaveState state)
        {
            Array.Copy(state.InternalRam, InternalRam, INTERNAL_RAM_SIZE);
            Array.Copy(state.HighRam, HighRam, HIGH_RAM_SIZE);
        }
    }
}