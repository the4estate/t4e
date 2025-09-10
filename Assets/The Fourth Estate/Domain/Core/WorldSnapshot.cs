using System.Collections.Generic;

namespace T4E.Domain
{
    public sealed class WorldSnapshot
    {
        public GameDate GameDate { get; private set; }
        public Dictionary<string, string> Flags { get; private set; }
        public int Credibility { get; set; }
        public int RegimePressure { get; set; }
        // … add Personas, Factions, Leads, Evidence, NewsQueue, Timeline, etc.

        public WorldSnapshot(GameDate date)
        {
            GameDate = date;
            Flags = new Dictionary<string, string>();
        }
    }
}
