using System.Collections.Generic;
using T4E.App.Abstractions;
using T4E.Domain;                 // GameDate, WorldSnapshot
using T4E.Domain.Core.CET;

// keep old code using EffectType working

namespace T4E.Infrastructure
{
    public sealed class InMemoryWorld : IWorldQuery, IWorldCommands
    {
        public readonly HashSet<string> News = new HashSet<string>();
        public readonly HashSet<string> Leads = new HashSet<string>();
        public readonly Dictionary<string, int> Flags = new Dictionary<string, int>();

        // Build a snapshot using the GameDate provided by the caller (TimeService/dispatcher)
        public WorldSnapshot Snapshot(GameDate now)
        {
            // If your WorldSnapshot has a ctor(WorldSnapshot(GameDate date)) use this:
            return new WorldSnapshot(now);

            // If your WorldSnapshot currently only has WorldSnapshot(int week):
            // return new WorldSnapshot(now.Week);
        }

        public void Apply(Effect e)
        {
            switch (e.Kind) // or EffectType if you prefer the alias
            {
                case EffectType.AddNews:
                    if (!string.IsNullOrEmpty(e.A)) { News.Add(e.A); UnityEngine.Debug.Log($"[CET] AddNews {e.A}"); }
                    break;

                case EffectType.AddLead:
                    if (!string.IsNullOrEmpty(e.A)) Leads.Add(e.A);
                    break;

                case EffectType.SetFlag:
                    if (!string.IsNullOrEmpty(e.A)) Flags[e.A] = e.I1; // MVP uses int flags
                    break;

                default:
                    break; // add more handlers as you grow the state
            }
        }

        // Remove if unused; you had this in the MVP
        public void Enqueue(TimelineItem _)
        {
            // handled elsewhere in this MVP
        }
    }
}
