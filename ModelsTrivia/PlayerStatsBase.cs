namespace Models
{
    // Shared stats for player-based score models.
    public abstract class PlayerStatsBase
    {
        // The user this stats row belongs to.
        public int UserID { get; set; }
        // Number of correct answers.
        public int CorrectCount { get; set; }
        // Total number of answers submitted.
        public int AnsweredCount { get; set; }
    }
}
