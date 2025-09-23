using T4E.Domain;
namespace T4E.App.Abstractions.Ports
{
    public interface IWorldQuery
    {
        WorldSnapshot Snapshot(GameDate now);
    }
}