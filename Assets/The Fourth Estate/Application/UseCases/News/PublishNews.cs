using System;
using System.Collections.Generic;
using System.Linq;
using T4E.Domain.Core.News; // for CredibilityEvaluator, SourceWeight, CredibilityTier
using T4E.App.Abstractions.Dtos;
using T4E.App.Abstractions.Ports;
using T4E.Domain.Core.CET;

namespace T4E.App.UseCases.News
{
    public sealed class PublishNews
    {
        private readonly IContentRepository _newsRepo;
        private readonly IContentRepository _sourcesRepo;
        private readonly IEffectApplier _effects;
        private readonly IMemoryLog _memory;
        private readonly IAppLogger _log;
        private readonly IWorldQuery _world;
        private readonly IWorldCommands _cmds;

        public PublishNews(IContentRepository newsRepo,
                           IContentRepository sourcesRepo,
                           IEffectApplier effects,
                           IMemoryLog memory,
                           IAppLogger log,
                           IWorldQuery world,
                           IWorldCommands cmds)
        {
            _newsRepo = newsRepo;
            _sourcesRepo = sourcesRepo;
            _effects = effects;
            _memory = memory;
            _log = log;
            _world = world;
            _cmds = cmds;
        }

        public Result Execute(string newsId, Tone tone)
        {
            if (string.IsNullOrWhiteSpace(newsId))
                throw new ArgumentException("newsId required", nameof(newsId));

            var news = _newsRepo.Load<NewsDto>(newsId)
                       ?? throw new InvalidOperationException($"News not found: {newsId}");

            if (!news.ToneAllowed.Contains(tone))
                throw new InvalidOperationException($"Tone {tone} not allowed for {newsId}");

            var toneKey = tone.ToString();
            if (!news.ToneVariants.TryGetValue(toneKey, out var variant) || variant == null)
                throw new InvalidOperationException($"Tone variant {toneKey} missing for {newsId}");

            // --- credibility logic ---
            var unlocked = _world.Snapshot(default).UnlockedSources;
            var allSources = _sourcesRepo.LoadAll<SourceDto>().ToList();

            // Map DTOs → Domain value type
            var supports = news.Source?.Supports ?? new List<string>();
            var conflicts = news.Source?.Conflicts ?? new List<string>();

            var supportingWeights = allSources
                .Where(s => supports.Contains(s.Id))
                .Select(s => new SourceWeight(s.Id, s.Weight))
                .ToList();

            var conflictingWeights = allSources
                .Where(s => conflicts.Contains(s.Id))
                .Select(s => new SourceWeight(s.Id, s.Weight))
                .ToList();


            var tier = CredibilityEvaluator.Evaluate(supportingWeights, conflictingWeights, unlocked, out int net);
            bool contested = (tier == CredibilityTier.Contested);
            int agencyDelta = tier switch
            {
                CredibilityTier.Weak => -1,
                CredibilityTier.Solid => +1,
                CredibilityTier.Corroborated => +2,
                _ => 0
            };
            if (contested && tier == CredibilityTier.Weak)
                agencyDelta = -1;

            // Apply credibility adjustment to world
            _cmds.AdjustAgencyCredibility(agencyDelta);

            // --- apply tone effects as before ---
            var srcRule = new Rule(
                eventId: news.Id,
                ruleIndex: 0,
                trigger: default,
                priority: 0,
                conditions: Array.Empty<Condition>(),
                effects: variant.Effects?.ToArray() ?? Array.Empty<Effect>(),
                exclusiveFlag: null,
                slotKind: null,
                background: false
            );

            var invocations = new List<EffectInvocation>();
            if (variant.Effects != null)
            {
                foreach (var eff in variant.Effects)
                    invocations.Add(new EffectInvocation(srcRule, eff));
            }

            var applied = _effects.Apply(invocations);

            // Record memory (for determinism/debug)
            _memory.RecordPublishedNews(news.Id, tone);

            // Log trace
            _log.Info($"Published {news.Id} [{tone}] tier={tier}{(contested ? " (Contested)" : "")} " +
                      $"→ applied {applied} effects; Δcred={agencyDelta}");

            return new Result(news.Id, tone, variant.Headline, variant.Short,
                news.BodyGeneric, tier.ToString(), contested);
        }


        public readonly struct Result
        {
            public readonly string NewsId;
            public readonly Tone Tone;
            public readonly string Headline;
            public readonly string Short;
            public readonly string Body;
            public readonly string Tier;
            public readonly bool Contested;

            public Result(string newsId, Tone tone, string headline, string @short,
                          string body, string tier, bool contested)
            {
                NewsId = newsId;
                Tone = tone;
                Headline = headline;
                Short = @short;
                Body = body;
                Tier = tier;
                Contested = contested;
            }
        }
    }
}
