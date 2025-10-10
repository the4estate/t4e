using System;
using System.Collections.Generic;
using System.Linq;

namespace T4E.Domain.Core.Leads
{
    [Serializable]
    public sealed class LeadProgressState
    {
        public string LeadId;
        public int State;                       // cast of LeadState
        public int MinTotal;                    // required count (schema)
        public List<string> Allow = new();      // allowed evidence types (Witness/Document/Object)
        public List<EvidenceRequirement> Collected = new();
    }

    public sealed class LeadProgress
    {
        private readonly string _leadId;
        private readonly HashSet<string> _allow;
        private readonly HashSet<EvidenceRequirement> _collected;

        public int MinTotal { get; }
        public LeadState State { get; private set; }

        public LeadProgress(
            string leadId,
            IEnumerable<string> allow,
            int minTotal,
            IEnumerable<EvidenceRequirement> collected = null,
            LeadState state = LeadState.Active)
        {
            _leadId = leadId ?? string.Empty;
            _allow = new HashSet<string>(allow ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            _collected = new HashSet<EvidenceRequirement>(collected ?? Array.Empty<EvidenceRequirement>());
            MinTotal = Math.Max(0, minTotal);
            State = state;
            RecomputeState();
        }

        public IReadOnlyCollection<string> Allow => _allow;
        public IReadOnlyCollection<EvidenceRequirement> Collected => _collected;

        public bool Collect(string type, string id)
        {
            if (!_allow.Contains(type)) return false;
            var ev = new EvidenceRequirement(type, id);
            var added = _collected.Add(ev);
            if (added) RecomputeState();
            return added;
        }

        public bool Remove(string type, string id)
        {
            var removed = _collected.Remove(new EvidenceRequirement(type, id));
            if (removed) RecomputeState();
            return removed;
        }

        public bool HasAllEvidence => MeetsRequirement;

        public bool MeetsRequirement =>
            _collected.Count(e => _allow.Contains(e.Type)) >= MinTotal;

        public int MissingCount =>
            Math.Max(0, MinTotal - _collected.Count(e => _allow.Contains(e.Type)));

        private void RecomputeState()
        {
            if (State == LeadState.Completed) return;
            if (MeetsRequirement)
            {
                State = LeadState.ReadyToExpose;
                return;
            }

            State = _collected.Count > 0 ? LeadState.Active : LeadState.Hidden;
        }

        public void MarkCompleted()
        {
            State = LeadState.Completed;
        }

        public LeadProgressState ToState()
        {
            return new LeadProgressState
            {
                LeadId = _leadId,
                State = (int)State,
                MinTotal = MinTotal,
                Allow = _allow.ToList(),
                Collected = _collected.ToList()
            };
        }

        public static LeadProgress FromState(LeadProgressState s)
        {
            if (s == null)
                return new LeadProgress(string.Empty, Array.Empty<string>(), 0);

            var allow = s.Allow ?? new List<string>();
            var collected = s.Collected ?? new List<EvidenceRequirement>();

            return new LeadProgress(
                s.LeadId ?? string.Empty,
                allow,
                s.MinTotal,
                collected,
                (LeadState)s.State);
        }
    }
}
