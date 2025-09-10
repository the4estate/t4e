using System.Collections.Generic;
using T4E.App.Abstractions;

namespace T4E.Infrastructure
{
    /// <summary>
    /// Default: backed by hidden flags (compact string key) in save/world.
    /// Swap for a dedicated array later if you prefer.
    /// </summary>
    public sealed class FiredLedger : IFiredLedger
    {
        private readonly HashSet<string> _cache = new(); // in_memory

        private static string Key(string eventId, int ruleIndex, string trigId)
            => $"{eventId}#{ruleIndex}@{trigId}";

        public bool HasFired(string eventId, int ruleIndex, string triggerInstanceId)
            => _cache.Contains(Key(eventId, ruleIndex, triggerInstanceId));

        public void MarkFired(string eventId, int ruleIndex, string triggerInstanceId)
            => _cache.Add(Key(eventId, ruleIndex, triggerInstanceId));
    }
}
