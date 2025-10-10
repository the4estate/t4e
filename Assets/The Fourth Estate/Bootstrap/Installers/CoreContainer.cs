using T4E.App.Abstractions;
using T4E.App.Abstractions.Ports;
using T4E.App.UseCases;
using T4E.App.UseCases.News;
using T4E.Infrastructure;
using T4E.Infrastructure.Content;
using T4E.Infrastructure.Systems;

namespace T4E.Bootstrap.Installers
{
    /// <summary>
    /// Minimal, manual DI container: holds singletons and fabricates use cases.
    /// Lives in Bootstrap, so referencing Infrastructure is OK here.
    /// </summary>
    public sealed class CoreContainer
    {
        // Core services (singletons)
        public IRandom Rng { get; }
        public IClock Clock { get; }
        public IAppLogger Log { get; }
        public InMemoryWorld World { get; }
        public IMemoryLog Memory { get; }

        // Adapters
        public IEffectApplier Effects { get; }
        public IContentRepository NewsRepo { get; }
        public IContentRepository SourcesRepo { get; }
        public ILeadRepository LeadsRepo { get; } // remains null when no leads path provided

        // Convenience ports (so callers don’t need to know about InMemoryWorld)
        public IWorldQuery WorldQ => World;
        public IWorldCommands WorldC => World;

        // FULL constructor (centralizes all init)
        public CoreContainer(string newsJsonPath, string sourcesJsonPath, string? leadsJsonPath, int seed)
        {
            // Core
            Rng = new DeterministicRandom();
            Rng.Reseed(seed);
            Clock = new SystemClock();
            Log = new AppLogger();
            World = new InMemoryWorld();
            Memory = new MemoryLog();

            // Content
            NewsRepo    = new JsonNewsRepository(newsJsonPath);
            SourcesRepo = new JsonSourcesRepository(sourcesJsonPath);

            // Leads are optional — wire only if a path is provided
            if (!string.IsNullOrEmpty(leadsJsonPath))
            {
                LeadsRepo = new JsonLeadsRepository(leadsJsonPath);
            }

            // Effects adapter -> world
            Effects = new EffectApplier(World, Log);
        }

        // BACK-COMPAT constructor (no leads)
        public CoreContainer(string newsJsonPath, string sourcesJsonPath, int seed)
            : this(newsJsonPath, sourcesJsonPath, null, seed) { }

        // Use case factories (explicit, no reflection)
        public PublishNews MakePublishNews() =>
            new PublishNews(NewsRepo, SourcesRepo, Effects, Memory, Log, WorldQ, WorldC);
    }
}
