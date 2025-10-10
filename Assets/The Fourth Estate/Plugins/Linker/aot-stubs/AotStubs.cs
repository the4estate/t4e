using T4E.App.Abstractions.Dtos;
using T4E.Infrastructure.Content;

namespace T4E.Plugins.Linker
{
    // Ensures IL2CPP generates code for generic repository methods
    internal static class AotStubs
    {
        static AotStubs()
        {
            // Create dummy instances to force IL2CPP generic instantiation

            var newsRepo = new JsonNewsRepository("dummy");
            var _n1 = newsRepo.Load<NewsDto>("");
            foreach (var _ in newsRepo.LoadAll<NewsDto>()) { }

            var sourcesRepo = new JsonSourcesRepository("dummy");
            var _s1 = sourcesRepo.Load<SourceDto>("");
            foreach (var _ in sourcesRepo.LoadAll<SourceDto>()) { }

            // Touch a few generic collections to be safe
            _ = new System.Collections.Generic.List<object>();
            _ = new System.Collections.Generic.Dictionary<string, object>();
        }
    }
}
