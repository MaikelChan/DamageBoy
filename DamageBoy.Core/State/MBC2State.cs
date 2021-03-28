
namespace DamageBoy.Core.State
{
    class MBC2State : MemoryBankControllerState
    {
        public byte RomBank { get; set; }
        public bool IsMBC2RamEnabled { get; set; }
    }
}