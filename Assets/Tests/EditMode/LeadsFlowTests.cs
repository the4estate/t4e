using System.Collections.Generic;
using NUnit.Framework;
using T4E.App.Abstractions.Dtos;
using T4E.App.Abstractions.Ports;
using T4E.App.UseCases.Leads;
using T4E.Domain.Core.CET;
using T4E.Domain.Core.Leads;
using T4E.Infrastructure.Systems;

// ---------------- Fakes ----------------

sealed class FakeLeadsRepo : ILeadRepository
{
    private readonly LeadDto _dto;
    public FakeLeadsRepo(LeadDto dto) { _dto = dto; }
    public bool TryGet(string id, out LeadDto lead) { lead = _dto; return id == _dto.Id; }
    public LeadDto Get(string id) => _dto;
}

sealed class FakeStore : ILeadProgressStore
{
    private readonly Dictionary<string, LeadProgressState> _m = new();
    public bool TryGet(string id, out LeadProgressState s) => _m.TryGetValue(id, out s);
    public void Upsert(LeadProgressState s)
    {
        if (!string.IsNullOrWhiteSpace(s.LeadId)) _m[s.LeadId] = s;
    }
}

sealed class FakeEffects : IEffectApplier
{
    public readonly List<EffectInvocation> Applied = new();

    public int Apply(IReadOnlyList<EffectInvocation> inv)
    {
        if (inv == null) return 0;
        for (int i = 0; i < inv.Count; i++) Applied.Add(inv[i]);
        return inv.Count;
    }
}


sealed class NullLog : IAppLogger
{
    public void Info(string m) { }
    public void Warn(string m) { }
    public void Error(string m) { }
}

// --------------- Tests -----------------

public class LeadsFlowTests
{
    private static LeadDto MakeLead()
    {
        return new LeadDto
        {
            Id = "test.lead.sample_001",
            Era = "test",
            Title = "Test Lead",
            ExposeText = "Exposé body",
            Difficulty = 2,
            Personas = new List<string>(),
            EvidenceRequirement = new EvidenceRequirementDto
            {
                MinTotal = 1,
                Allow = new List<string> { "Document", "Witness" }
            },
            OnExposeEffects = new List<Effect>
            {
                new Effect(EffectType.CredibilityDelta, i1: 2)
            }
        };
    }

    [Test]
    public void Collect_Then_Expose_AppliesEffects_And_Completes()
    {
        var dto = MakeLead();
        var repo = new FakeLeadsRepo(dto);
        var store = new FakeStore();
        var fx = new FakeEffects();
        var log = new NullLog();
        var memory = new MemoryLog();

        var collect = new CollectEvidence(repo, store, fx, memory, log);
        var r1 = collect.Execute(new CollectEvidence.Command(dto.Id, "Document", "doc_001"));
        Assert.IsTrue(r1.Added);
        Assert.That(r1.State, Is.EqualTo(LeadState.ReadyToExpose));

        var expose = new ExposeLead(repo, store, fx, new T4E.Infrastructure.Systems.MemoryLog(), log);
        var result = expose.Execute(dto.Id);

        Assert.AreEqual(dto.Title, result.Title);
        Assert.AreEqual(1, fx.Applied.Count);
        Assert.AreEqual(EffectType.CredibilityDelta, fx.Applied[0].Effect.Kind);

        Assert.IsTrue(store.TryGet(dto.Id, out var saved));
        var progress = LeadProgress.FromState(saved);
        Assert.AreEqual(LeadState.Completed, progress.State);
    }

    [Test]
    public void Expose_Without_Enough_Evidence_Throws()
    {
        var dto = MakeLead();
        dto.EvidenceRequirement.MinTotal = 2; // need two items

        var repo = new FakeLeadsRepo(dto);
        var store = new FakeStore();
        var effect = new FakeEffects();
        var log = new NullLog();
        var memory = new MemoryLog();
        var collect = new CollectEvidence(repo, store, effect, memory, log);

        // collect only one
        collect.Execute(new CollectEvidence.Command(dto.Id, "Document", "doc_001"));

        var expose = new ExposeLead(repo, store, new FakeEffects(), new T4E.Infrastructure.Systems.MemoryLog(), log);
        Assert.Throws<System.InvalidOperationException>(() => expose.Execute(dto.Id));
    }
}
