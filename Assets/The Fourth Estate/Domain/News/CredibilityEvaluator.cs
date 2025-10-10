using System.Collections.Generic;
using System.Linq;
namespace T4E.Domain.Core.News
{
    public enum CredibilityTier { Weak, Solid, Corroborated, Contested }

    // Define a minimal lightweight record that Domain can understand.
    public readonly struct SourceWeight
    {
        public readonly string Id;
        public readonly int Weight;
        public SourceWeight(string id, int weight)
        {
            Id = id;
            Weight = weight;
        }
    }

    public static class CredibilityEvaluator
    {
        public static CredibilityTier Evaluate(
            List<SourceWeight> supports,
            List<SourceWeight> conflicts,
            HashSet<string> unlockedSourceIds,
            out int netScore)
        {
            int S = supports.Where(s => unlockedSourceIds.Contains(s.Id)).Sum(s => s.Weight);
            int C = conflicts.Where(s => unlockedSourceIds.Contains(s.Id)).Sum(s => s.Weight);
            netScore = S - C;

            bool contested = (S > 0 && C > 0 && C >= S / 2);

            if (netScore <= 3) return contested ? CredibilityTier.Contested : CredibilityTier.Weak;
            if (netScore <= 7) return contested ? CredibilityTier.Contested : CredibilityTier.Solid;
            return contested ? CredibilityTier.Contested : CredibilityTier.Corroborated;
        }
    }
}
