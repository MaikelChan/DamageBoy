using DamageBoy.Core.State;

namespace DamageBoy.Core.MemoryBankControllers;

interface IMemoryBankController : IState
{
    byte this[int index] { get; set; }
}