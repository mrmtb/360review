using System;

namespace reviews.Models
{
    public class vwParticipant
    {
        public Guid ParticipantId { get; set; }
        public required string ParticipantNameFirst { get; set; }
        public required string ParticipantNameLast { get; set; }
        public required string ParticipantEmail { get; set; }
        public required string Email { get; set; }
        public bool ParticipantHasRegistered { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool ParticipantIsActive { get; set; }
        public bool LockoutEnabled { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public int AccessFailedCount { get; set; }
    }
}
