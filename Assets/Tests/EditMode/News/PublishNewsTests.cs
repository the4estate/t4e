using System.Collections.Generic;
using NUnit.Framework;
using T4E.App.Abstractions.Dtos;
using T4E.App.Abstractions.Ports;
using T4E.App.UseCases.News;
using T4E.Domain.Core.CET;

namespace T4E.Tests.EditMode.News
{
    public sealed class PublishNewsTests
    {
        // ---- Test 1: applies effects & records memory ----
        [Test]
        public void Publish_AppliesEffects_And_RecordsMemory()
        {
            // Arrange: a news with ONE effect on Critical tone
            var news = DemoNews("vic.news.demo_001",
                criticalEffects: new List<Effect> { default }); // default Effect is fine for counting

            var repo = new FakeRepo(news);
            var applier = new FakeApplier();
            var memory = new FakeMemory();
            var log = new FakeLogger();

            var usecase = new PublishNews(repo, applier, memory, log);

            // Act
            var result = usecase.Execute(news.Id, Tone.Critical);

            // Assert: correct text returned
            Assert.AreEqual("Unrest in the Capital", result.Headline);
            Assert.AreEqual("Authorities uneasy.", result.Short);

            // Assert: ONE invocation applied (matches our single effect)
            Assert.AreEqual(1, applier.LastInvocationCount);

            // Assert: memory footprint recorded exactly once
            Assert.AreEqual(1, memory.Published.Count);
            Assert.AreEqual(news.Id, memory.Published[0].newsId);
            Assert.AreEqual(Tone.Critical, memory.Published[0].tone);
        }


        // ---- Test 2: determinism (same input -> same result & invocation count) ----
        [Test]
        public void Publish_IsDeterministic_ForSameInput()
        {
            var news = DemoNews("vic.news.demo_001",
                criticalEffects: new List<Effect> { default, default }); // two effects

            var repo = new FakeRepo(news);
            var applier = new FakeApplier();
            var memory = new FakeMemory();
            var log = new FakeLogger();

            var usecase = new PublishNews(repo, applier, memory, log);
            
            var r1 = usecase.Execute(news.Id, Tone.Critical);
            var count1 = applier.LastInvocationCount;

            var r2 = usecase.Execute(news.Id, Tone.Critical);
            var count2 = applier.LastInvocationCount;

            Assert.AreEqual(r1.Headline, r2.Headline);
            Assert.AreEqual(r1.Short, r2.Short);
            Assert.AreEqual(r1.Body, r2.Body);
            Assert.AreEqual(count1, count2);
        }
        // ---- Negative Test 1: invalid tone ----
        [Test]
        public void Publish_InvalidTone_Throws()
        {
            var news = DemoNews("vic.news.invalid_tone");
            // Allow only Supportive
            news.ToneAllowed = new List<Tone> { Tone.Supportive };

            var repo = new FakeRepo(news);
            var applier = new FakeApplier();
            var memory = new FakeMemory();
            var log = new FakeLogger();

            var usecase = new PublishNews(repo, applier, memory, log);

            Assert.Throws<System.InvalidOperationException>(() =>
                usecase.Execute(news.Id, Tone.Critical)); // Critical not allowed
        }

        // ---- Negative Test 2: missing news id ----
        [Test]
        public void Publish_MissingNewsId_Throws()
        {
            // FakeRepo seeded with null so it never finds anything
            var repo = new FakeRepo(null);
            var applier = new FakeApplier();
            var memory = new FakeMemory();
            var log = new FakeLogger();

            var usecase = new PublishNews(repo, applier, memory, log);

            Assert.Throws<System.InvalidOperationException>(() =>
                usecase.Execute("nonexistent.news.id", Tone.Supportive));
        }


        // ---------- helpers ----------

        private static NewsDto DemoNews(string id,
                                        List<Effect>? supportiveEffects = null,
                                        List<Effect>? neutralEffects = null,
                                        List<Effect>? criticalEffects = null)
        {
            return new NewsDto
            {
                Id = id,
                Era = "victorian",
                Subject = "Demonstration in the Square",
                Tags = new List<string> { "politics" },
                ToneAllowed = new List<Tone> { Tone.Supportive, Tone.Neutral, Tone.Critical },
                ToneVariants = new Dictionary<string, NewsToneDetailsDto>
                {
                    ["Supportive"] = new NewsToneDetailsDto
                    {
                        Headline = "Citizens Rally for Reform",
                        Short = "Large peaceful march.",
                        Effects = supportiveEffects ?? new List<Effect>()
                    },
                    ["Neutral"] = new NewsToneDetailsDto
                    {
                        Headline = "Crowds Gather Downtown",
                        Short = "Demonstration reported.",
                        Effects = neutralEffects ?? new List<Effect>()
                    },
                    ["Critical"] = new NewsToneDetailsDto
                    {
                        Headline = "Unrest in the Capital",
                        Short = "Authorities uneasy.",
                        Effects = criticalEffects ?? new List<Effect>()
                    },
                },
                BodyGeneric = "A sizeable crowd gathered in the capital's main square...",
                PersonasInvolved = new List<string>(),
                Source = new SourceDto { Type = SourceType.Anonymous },
                Flags = new List<string>()
            };
        }

        // ---------- fakes (no mocking framework needed) ----------

        private sealed class FakeRepo : IContentRepository
        {
            private readonly Dictionary<string, object> _byId = new();

            public FakeRepo(NewsDto news) => _byId[news.Id] = news;

            public Rule[] GetRulesByTrigger(TriggerType trigger)
            {
                // For these unit tests, rules are irrelevant.
                // Return empty array so nothing breaks.
                return System.Array.Empty<Rule>();
            }

            public T? Load<T>(string id) where T : class
            {
                if (_byId.TryGetValue(id, out var o) && o is T t) return t;
                return null;
            }

            public IEnumerable<T> LoadAll<T>() where T : class
            {
                foreach (var kv in _byId)
                    if (kv.Value is T t)
                        yield return t;
            }
        }


        private sealed class FakeApplier : IEffectApplier
        {
            public int LastInvocationCount { get; private set; }

            public int Apply(IReadOnlyList<EffectInvocation> invocations)
            {
                LastInvocationCount = invocations?.Count ?? 0;
                return LastInvocationCount;
            }
        }

        private sealed class FakeMemory : IMemoryLog
        {
            public readonly List<(string newsId, Tone tone)> Published = new();

            public void RecordPublishedNews(string newsId, Tone tone)
                => Published.Add((newsId, tone));
        }

        private sealed class FakeLogger : IAppLogger
        {
            public void Info(string message) { /* no-op */ }
            public void Warn(string message) { /* no-op */ }
            public void Error(string message) { /* no-op */ }
        }
    }
}
