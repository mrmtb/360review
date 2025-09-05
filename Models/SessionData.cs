namespace reviews.Models
{
    public class SessionData
    {
        public Guid UserId { get; set; }
        public Guid ParticipantId { get; set; }
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public Guid ClientId { get; set; }
        public string ClientName { get; set; } = null!;
        public string ClientLogoLocation { get; set; } = "";
        public Guid DefaultEvaluationId { get; set; }
        public int OpenRatingCount { get; set; } = -1;
    }

}