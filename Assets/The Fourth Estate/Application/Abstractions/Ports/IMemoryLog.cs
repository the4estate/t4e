namespace T4E.App.Abstractions.Ports
{
    using T4E.App.Abstractions.Dtos;

    /// <summary>
    /// Records player-visible footprints (what the player actually did)
    /// so content and conditions can react later.
    /// </summary>
    public interface IMemoryLog
    {
        /// <summary>
        /// The player published a news item with the given tone.
        /// Example UI/log line: "You published vic.news.demo_001 [Critical]".
        /// </summary>
        void RecordPublishedNews(string newsId, Tone tone);
    }
}
