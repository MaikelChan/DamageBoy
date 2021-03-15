using System.Runtime.CompilerServices;

namespace GBEmu.Core
{
    static class Helpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBit(ref byte variable, byte bit, bool value)
        {
            if (value) variable |= (byte)(1 << bit);
            else variable &= (byte)(~(1 << bit));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte SetBit(byte variable, byte bit, bool value)
        {
            if (value) return (byte)(variable | (1 << bit));
            else return (byte)(variable & (~(1 << bit)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetBit(byte variable, byte bit)
        {
            return (variable & (1 << bit)) != 0;
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static bool GetHalfCarryFromAdd(byte value1, byte value2)
        //{
        //    return (((value1 & 0xF) + (value2 & 0xF)) & 0x10) != 0;
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static bool GetHalfCarryFromAddWithCarry(byte value1, byte value2, bool carry)
        //{
        //    return ((value1 & 0xF) + (value2 & 0xF)) >= (carry ? 0xF : 0x10);
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static bool GetCarryFromAdd(byte value1, byte value2)
        //{
        //    return (value1 + value2) > 0xFF;
        //}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Add(byte value1, byte value2, out bool halfCarry, out bool carry)
        {
            int result = value1 + value2;
            halfCarry = (value1 & 0xF) + (value2 & 0xF) > 0xF;
            carry = result > 0xFF;
            return (byte)(result & 0xFF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte AddWithCarry(byte value1, byte value2, bool previousCarry, out bool halfCarry, out bool carry)
        {
            int result = value1 + value2 + (previousCarry ? 1 : 0);
            halfCarry = (value1 & 0xF) + (value2 & 0xF) > (previousCarry ? 0xE : 0xF);
            carry = result > 0xFF;
            return (byte)(result & 0xFF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Subtract(byte value1, byte value2, out bool halfCarry, out bool carry)
        {
            byte result = (byte)(value1 - value2);
            halfCarry = (value1 & 0xF) < (value2 & 0xF);
            carry = value1 < value2;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte SubtractWithCarry(byte value1, byte value2, bool previousCarry, out bool halfCarry, out bool carry)
        {
            int c = previousCarry ? 1 : 0;
            byte result = (byte)(value1 - value2 - c);
            halfCarry = (value1 & 0xF) < ((value2 & 0xF) + c);
            carry = value1 < (value2 + c);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Add(ushort value1, ushort value2, out bool halfCarry, out bool carry)
        {
            int result = value1 + value2;
            halfCarry = (value1 & 0xFFF) + (value2 & 0xFFF) > 0xFFF;
            carry = result > 0xFFFF;
            return (ushort)(result & 0xFFFF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort AddSigned(ushort value1, byte value2, out bool halfCarry, out bool carry)
        {
            int result = value1 + (sbyte)value2;
            halfCarry = (value1 & 0xF) + (value2 & 0xF) > 0xF;
            carry = ((value1 & 0xFF) + value2) > 0xFF;
            return (ushort)(result & 0xFFFF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Subtract(ushort value1, ushort value2, out bool halfCarry, out bool carry)
        {
            int result = value1 - value2;
            halfCarry = (((value1 & 0xFF) - (value2 & 0xFF)) & 0x100) != 0;
            carry = value1 < value2;
            return (ushort)(result & 0xFFFF);
        }
    }
}