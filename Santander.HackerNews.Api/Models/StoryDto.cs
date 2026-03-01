namespace Santander.HackerNews.Api.Models
{
    /// <summary>
    /// Represents a Hacker News story exposed by the API.
    /// This DTO defines the external contract returned to API consumers
    /// and is independent of ranking or filtering use cases.
    /// </summary>
    public class StoryDto
    {
        /// <summary>
        /// The title of the story.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// The canonical URL of the story.
        /// </summary>
        public string Uri { get; set; } = string.Empty;

        /// <summary>
        /// The username of the author who submitted the story.
        /// </summary>
        public string PostedBy { get; set; } = string.Empty;

        /// <summary>
        /// The time the story was posted as a string.
        /// </summary>
        public string Time { get; set; } = string.Empty;

        /// <summary>
        /// The score of the story as reported by Hacker News.
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// The total number of comments associated with the story.
        /// </summary>
        public int CommentCount { get; set; }
    }
}