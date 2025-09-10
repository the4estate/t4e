using System;
using T4E.Domain;
namespace T4E.Domain
{
    // Minimal item; content IDs are strings to keep DTOs flat - IL2CPP
    public sealed class TimelineItem
    {
        public string Id;           // e.g., "vic.event.bread_riots_001"
        public GameDate When;
        public string PayloadType;  // "event" | "meeting" | "trial" | "letter"
        public string[] SpawnNewsIds;
        public string[] SpawnLeadIds;
        public TimelineItem(string id, GameDate when, string payloadType,
                            string[] spawnNewsIds = null, string[] spawnLeadIds = null)
        { Id=id; When=when; PayloadType=payloadType; SpawnNewsIds=spawnNewsIds ?? Array.Empty<string>(); SpawnLeadIds=spawnLeadIds ?? Array.Empty<string>(); }
    }
}
