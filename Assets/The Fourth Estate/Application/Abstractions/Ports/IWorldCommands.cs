using T4E.Domain.Core.CET;
namespace T4E.App.Abstractions.Ports
{
    public interface IWorldCommands
    {
        void Apply(Effect effect);
        void AdjustAgencyCredibility(int delta);
    }
}