
namespace DamageBoy.Core.State
{
    internal interface IState
    {
        void GetState(SaveState state);
        void SetState(SaveState state);
    }
}