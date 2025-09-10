using T4E.App.Abstractions;
using T4E.Domain;
using T4E.Domain.Core.CET;

namespace T4E.App.UseCases
{
    // Listens to segment ticks and applies weekly cadence flags.
    public sealed class CadenceRules
    {
        private readonly IWorldCommands _world;

        public CadenceRules(IWorldCommands world)
        {
            _world = world;
            GameSignals.SegmentAdvanced += OnSegment;
        }

        private void OnSegment(GameDate date)
        {
            if (date.Day == Weekday.Sunday   && date.Segment == DaySegment.Morning)
                _world.Apply(new Effect(EffectType.SetFlag, "editorial_unlocked", 1));

            if (date.Day == Weekday.Monday   && date.Segment == DaySegment.Morning)
                _world.Apply(new Effect(EffectType.SetFlag, "apply_publication_consequences", 1));
        }
    }
}
