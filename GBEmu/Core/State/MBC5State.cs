
namespace GBEmu.Core.State
{
    class MBC5State : MemoryBankControllerState
    {
        public byte RomBankHi { get; set; }
        public byte RomBankLo { get; set; }
        public byte RamBank { get; set; }
    }
}