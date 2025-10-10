using System.Collections.Generic;

namespace T4E.Domain
{
    /// <summary>
    /// Immutable snapshot of the current world state.
    /// Captures deterministic simulation data for use cases and conditions.
    /// </summary>
    public sealed class WorldSnapshot
    {
        public GameDate GameDate { get; private set; }

        // Core world state
        public Dictionary<string, string> Flags { get; private set; }

        // Press & regime stats (existing)
        public int Credibility { get; set; }
        public int RegimePressure { get; set; }

        // Which sources (evidence) have been unlocked
        public HashSet<string> UnlockedSources { get; set; }

        // Agency (newspaper) reputation score
        public int AgencyCredibility { get; set; }

        // TODO later: Personas, Factions, Leads, etc.
        // public List<PersonaSnapshot> Personas { get; set; }

        public WorldSnapshot(GameDate date)
        {
            GameDate = date;
            Flags = new Dictionary<string, string>();
            UnlockedSources = new HashSet<string>();
            AgencyCredibility = 0;
        }
    }
}
