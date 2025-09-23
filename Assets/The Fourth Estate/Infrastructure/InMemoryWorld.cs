using System.Collections.Generic;
using T4E.App.Abstractions.Ports;
using T4E.Domain;                 
using T4E.Domain.Core.CET;


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
            return new WorldSnapshot(now);
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
                    break; 
            }
        }

        
        public void Enqueue(TimelineItem _)
        {
            // not in sude
        }
    }
}
