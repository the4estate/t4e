using System;
using System.Collections.Generic;
using T4E.App.Abstractions;
using T4E.App.Abstractions.Dtos;
using T4E.App.Abstractions.Ports; // IMemoryLog (your port namespace)
using T4E.Domain.Core.CET;

namespace T4E.App.UseCases.News
{
    public sealed class PublishNews
    {
        private readonly IContentRepository _content;
        private readonly IEffectApplier _effects;
        private readonly IMemoryLog _memory;
        private readonly IAppLogger _log;

        public PublishNews(IContentRepository content,
                           IEffectApplier effects,
                           IMemoryLog memory,
                           IAppLogger log)
        {
            _content = content;
            _effects = effects;
            _memory = memory;
            _log = log;
        }

        public Result Execute(string newsId, Tone tone)
        {
            if (string.IsNullOrWhiteSpace(newsId))
                throw new ArgumentException("newsId required", nameof(newsId));

            var news = _content.Load<NewsDto>(newsId); // IContentRepository.Load<T>(id)
            if (news == null)
                throw new InvalidOperationException($"News not found: {newsId}");

            if (!news.ToneAllowed.Contains(tone))
                throw new InvalidOperationException($"Tone {tone} not allowed for {newsId}");

            var toneKey = tone.ToString(); // "Supportive"/"Neutral"/"Critical"
            if (!news.ToneVariants.TryGetValue(toneKey, out var variant) || variant == null)
                throw new InvalidOperationException($"Tone variant {toneKey} missing for {newsId}");

            // Wrap each Effect in an EffectInvocation with a tiny synthetic Rule as the source.
            // This keeps logs/idempotency consistent with the rest of CET.
            var srcRule = new Rule(
                eventId: news.Id,          // "vic.news.demo_001" (can prefix "news:" if you prefer)
                ruleIndex: 0,              // stable small index
                trigger: default,          // we’re publishing manually; default TriggerType is fine
                priority: 0,
                conditions: Array.Empty<Condition>(),   // none: publish is a player action
                effects: variant.Effects?.ToArray() ?? Array.Empty<Effect>(),
                exclusiveFlag: null,
                slotKind: null,
                background: false
            );

            var invocations = new List<EffectInvocation>(variant.Effects?.Count ?? 0);
            if (variant.Effects != null)
            {
                for (int i = 0; i < variant.Effects.Count; i++)
                    invocations.Add(new EffectInvocation(srcRule, variant.Effects[i]));
            }

            // Apply effects via adapter (will call IWorldCommands.Apply under the hood)
            var applied = _effects.Apply(invocations); // returns count applied

            // Record memory footprint for future conditions/narrative
            _memory.RecordPublishedNews(news.Id, tone);

            // Dev log for traceability
            _log.Info($"Published {news.Id} [{tone}] → applied {applied} effects; headline='{variant.Headline}'");

            // Return what the UI needs to render the article
            return new Result(news.Id, tone, variant.Headline, variant.Short, news.BodyGeneric);
        }

        public readonly struct Result
        {
            public readonly string NewsId;
            public readonly Tone Tone;
            public readonly string Headline;
            public readonly string Short;
            public readonly string Body;

            public Result(string newsId, Tone tone, string headline, string @short, string body)
            {
                NewsId = newsId;
                Tone = tone;
                Headline = headline;
                Short = @short;
                Body = body;
            }
        }
    }
}
