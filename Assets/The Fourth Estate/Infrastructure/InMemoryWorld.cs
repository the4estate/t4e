using System.Collections.Generic;
using T4E.App.Abstractions.Ports;
using T4E.Domain;
using T4E.Domain.Core.CET;
using T4E.Domain.Core.Leads;

 namespace T4E.Infrastructure
{
    public sealed class InMemoryWorld : IWorldQuery, IWorldCommands, ILeadProgressStore
    {
        public readonly HashSet<string> News = new();
        public readonly HashSet<string> Leads = new();
        public readonly Dictionary<string, int> Flags = new();

        // NEW: Sources and agency credibility
        public readonly HashSet<string> UnlockedSources = new();
        public int AgencyCredibility = 0;

        private readonly Dictionary<string, LeadProgressState> _leadProgress =
        new Dictionary<string, LeadProgressState>(64, System.StringComparer.OrdinalIgnoreCase);

        public WorldSnapshot Snapshot(GameDate now)
        {
            return new WorldSnapshot(now)
            {
                UnlockedSources = new HashSet<string>(UnlockedSources),
                AgencyCredibility = AgencyCredibility
            };
        }

        public void Apply(Effect e)
        {
            switch (e.Kind)
            {
                case EffectType.AddNews:
                    if (!string.IsNullOrEmpty(e.A))
                        News.Add(e.A);
                    break;

                case EffectType.AddLead:
                    if (!string.IsNullOrEmpty(e.A))
                        Leads.Add(e.A);
                    break;

                case EffectType.SetFlag:
                    if (!string.IsNullOrEmpty(e.A))
                        Flags[e.A] = e.I1;
                    break;

                // NEW: Handle unlocking sources through effects
                case EffectType.UnlockSource:
                    if (!string.IsNullOrEmpty(e.A))
                        UnlockedSources.Add(e.A);
                    break;

                case EffectType.CredibilityDelta:
                    AgencyCredibility += e.I1;
                    break;
            }
        }

        // --- New direct commands ---
        public void AdjustAgencyCredibility(int delta) { AgencyCredibility += delta; }
        public void UnlockSource(string sourceId) { if (!string.IsNullOrEmpty(sourceId)) UnlockedSources.Add(sourceId); }
        public void Enqueue(TimelineItem _) { }

        // --- ILeadProgressStore ---
        public bool TryGet(string leadId, out LeadProgressState state)
            => _leadProgress.TryGetValue(leadId ?? string.Empty, out state);

        public void Upsert(LeadProgressState state)
        {
            if (state == null || string.IsNullOrWhiteSpace(state.LeadId)) return;
            _leadProgress[state.LeadId] = state;
        }
    }
 }
