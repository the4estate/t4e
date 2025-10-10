using System;
using System.Linq;
using T4E.App.Abstractions.Ports;
using T4E.Domain.Core.CET;
using T4E.Domain.Core.Leads;

namespace T4E.App.UseCases.Leads
{
    public sealed class ExposeLead
    {
        public readonly struct Result
        {
            public readonly string Title;
            public readonly string ExposeText;
            public Result(string title, string exposeText) { Title = title; ExposeText = exposeText; }
        }

        private readonly ILeadRepository _leads;
        private readonly ILeadProgressStore _store;
        private readonly IEffectApplier _effects;
        private readonly IMemoryLog _memory;
        private readonly IAppLogger _log;

        public ExposeLead(ILeadRepository leads, ILeadProgressStore store, IEffectApplier effects, IMemoryLog memory, IAppLogger log)
        { _leads = leads; _store = store; _effects = effects; _memory = memory; _log = log; }

        public Result Execute(string leadId)
        {
            if (leadId == null) throw new ArgumentNullException(nameof(leadId));
            var dto = _leads.Get(leadId);

            if (!_store.TryGet(leadId, out var saved))
                throw new InvalidOperationException($"Lead '{leadId}' has no progress — collect evidence first.");

            var progress = LeadProgress.FromState(saved);
            if (!progress.MeetsRequirement)
            {
                var missing = progress.MissingCount;
                throw new InvalidOperationException($"Lead '{leadId}' not ready: needs {missing} more allowed evidence item(s).");
            }
            // AFTER
            if (dto.OnExposeEffects != null && dto.OnExposeEffects.Count > 0)
            {
                var inv = dto.OnExposeEffects
                    .Select(e => new EffectInvocation(null, e))
                    .ToList();
                _effects.Apply(inv);
            }
            _log.Info($"[Lead] Exposé published: {dto.Id}");
            progress.MarkCompleted();
            _store.Upsert(progress.ToState());

            _log.Info($"[Lead] Exposé published for {dto.Id}");
            return new Result(dto.Title, dto.ExposeText);
        }
    }
}
