using T4E.Domain.Core.CET;
namespace T4E.App.Abstractions
{
    public interface IWorldCommands
    {
        void Apply(Effect effect);
    }
}