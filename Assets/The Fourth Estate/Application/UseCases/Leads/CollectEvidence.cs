using System;
using System.Runtime.CompilerServices;
using T4E.App.Abstractions.Ports;
using T4E.Domain.Core.Leads;

namespace T4E.App.UseCases.Leads
{
    public sealed class CollectEvidence
    {
        public readonly struct Command
        {
            public readonly string LeadId;
            public readonly string EvidenceType; // "Witness"|"Document"|"Object" (schema casing)
            public readonly string EvidenceId;
            public Command(string leadId, string type, string id)
            { LeadId = leadId; EvidenceType = type; EvidenceId = id; }
        }

        public readonly struct Result
        {
            public readonly bool Added;
            public readonly LeadState State;
            public Result(bool added, LeadState state) { Added = added; State = state; }
        }

        private readonly ILeadRepository _leads;
        private readonly ILeadProgressStore _store;
        private readonly IEffectApplier _effect;
        private readonly IMemoryLog _memory;
        private readonly IAppLogger _log;

        public CollectEvidence(ILeadRepository leads, ILeadProgressStore store, IEffectApplier effect, IMemoryLog memory, IAppLogger log)
        { _leads = leads; _store = store; _effect = effect; _memory = memory; _log = log; }

        public Result Execute(Command cmd)
        {
            if (cmd.LeadId == null) throw new ArgumentNullException(nameof(cmd.LeadId));
            var dto = _leads.Get(cmd.LeadId);

            LeadProgress progress;
            if (_store.TryGet(cmd.LeadId, out var saved))
            {
                progress = LeadProgress.FromState(saved);
            }
            else
            {
                var allow = dto.EvidenceRequirement?.Allow ?? new System.Collections.Generic.List<string>();
                var min = dto.EvidenceRequirement?.MinTotal ?? 0;
                progress  = new LeadProgress(dto.Id, allow, min);
            }

            var added = progress.Collect(cmd.EvidenceType, cmd.EvidenceId);
            if (added)
            {
                _store.Upsert(progress.ToState());
                var note = $"Collected {cmd.EvidenceType}:{cmd.EvidenceId} for {dto.Id}";
                _log.Info($"[Lead] {note} — state={progress.State}");
            }

            return new Result(added, progress.State);
        }
    }
}
