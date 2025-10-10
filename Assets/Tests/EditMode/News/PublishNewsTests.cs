using NUnit.Framework;
using System.Collections.Generic;
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
                criticalEffects: new List<Effect> { default }); // one fake effect

            var sourcesRepo = new FakeSourcesRepo(
                new SourceDto { Id = "vic.source.fake", Type = SourceType.Primary, Weight = 5 });

            var world = new FakeWorld(); // no unlocked sources needed for this simple test
            var newsRepo = new FakeNewsRepo(news);
            var applier = new FakeApplier();
            var memory = new FakeMemory();
            var log = new FakeLogger();
            world.UnlockSource("vic.source.fake");
            var usecase = new PublishNews(newsRepo, sourcesRepo, applier, memory, log, world, world);

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

            var sourcesRepo = new FakeSourcesRepo(
                new SourceDto { Id = "vic.source.fake", Type = SourceType.Primary, Weight = 5 });

            var world = new FakeWorld();
            var newsRepo = new FakeNewsRepo(news);
            var applier = new FakeApplier();
            var memory = new FakeMemory();
            var log = new FakeLogger();
            world.UnlockSource("vic.source.fake");
            var usecase = new PublishNews(newsRepo, sourcesRepo, applier, memory, log, world, world);

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

            var sourcesRepo = new FakeSourcesRepo();
            var world = new FakeWorld();
            var repo = new FakeNewsRepo(news);
            var applier = new FakeApplier();
            var memory = new FakeMemory();
            var log = new FakeLogger();

            var usecase = new PublishNews(repo, sourcesRepo, applier, memory, log, world, world);

            Assert.Throws<System.InvalidOperationException>(() =>
                usecase.Execute(news.Id, Tone.Critical)); // Critical not allowed
        }

        // ---- Negative Test 2: missing news id ----
        [Test]
        public void Publish_MissingNewsId_Throws()
        {
            // FakeRepo seeded with null so it never finds anything
            var sourcesRepo = new FakeSourcesRepo();
            var world = new FakeWorld();
            var repo = new FakeNewsRepo(null);
            var applier = new FakeApplier();
            var memory = new FakeMemory();
            var log = new FakeLogger();

            var usecase = new PublishNews(repo, sourcesRepo, applier, memory, log, world, world);

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
                Source = new NewsSourcesDto{
                    Supports = new List<string> { "vic.source.fake" },
                    MinToPublish = 1
                },
                Flags = new List<string>()
            };
        }

        // ---------- fakes (no mocking framework needed) ----------

        private sealed class FakeNewsRepo : IContentRepository
        {
            private readonly Dictionary<string, object> _byId = new();

            public FakeNewsRepo(NewsDto? news = null)
            {
                if (news != null)
                    _byId[news.Id] = news;
            }

            public Rule[] GetRulesByTrigger(TriggerType trigger) => System.Array.Empty<Rule>();

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

        private sealed class FakeSourcesRepo : IContentRepository
        {
            private readonly Dictionary<string, object> _byId = new();

            public FakeSourcesRepo(params SourceDto[] sources)
            {
                foreach (var s in sources)
                    _byId[s.Id] = s;
            }

            public Rule[] GetRulesByTrigger(TriggerType trigger) => System.Array.Empty<Rule>();
            public T? Load<T>(string id) where T : class => _byId.TryGetValue(id, out var o) && o is T t ? t : null;
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

        private sealed class FakeWorld : IWorldQuery, IWorldCommands
        {
            public readonly HashSet<string> Unlocked = new();
            public int AgencyCredibility { get; private set; }

            public void UnlockSource(string id) => Unlocked.Add(id);
            public void AdjustAgencyCredibility(int delta) => AgencyCredibility += delta;

            // For CET compatibility
            public void Apply(Effect e) { /* no-op */ }

            public T4E.Domain.WorldSnapshot Snapshot(T4E.Domain.GameDate now)
            {
                var snap = new T4E.Domain.WorldSnapshot(now);
                snap.UnlockedSources = new HashSet<string>(Unlocked);
                snap.AgencyCredibility = AgencyCredibility;
                return snap;
            }
        }

        private sealed class FakeMemory : IMemoryLog
        {
            public readonly List<(string newsId, Tone tone)> Published = new();
            public void RecordPublishedNews(string newsId, Tone tone) => Published.Add((newsId, tone));
        }

        private sealed class FakeLogger : IAppLogger
        {
            public void Info(string message) { }
            public void Warn(string message) { }
            public void Error(string message) { }
        }
    }
}
