using T4E.App.Abstractions;
using T4E.Domain;
using T4E.Domain.Core.CET;

namespace T4E.App.UseCases
{
    // Subscribes to ItemDue and forwards to the world as effects.
    // Later, CET interpreter call.
    public sealed class TimelineDispatcher
    {
        private readonly IWorldCommands _world;
        private readonly IAppLogger _log;

        public TimelineDispatcher(IWorldCommands world, IAppLogger log)
        {
            _world = world; _log = log;
            GameSignals.ItemDue += OnItemDue;
        }

        private void OnItemDue(TimelineItem item, GameDate at)
        {
            _log.Info($"[Timeline] {item.Id} due at {at}");

            foreach (var nid in item.SpawnNewsIds)
                _world.Apply(new Effect(EffectType.AddNews, nid));

            foreach (var lid in item.SpawnLeadIds)
                _world.Apply(new Effect(EffectType.AddLead, lid));

        }
    }
}
