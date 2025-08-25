using System;

namespace reviews.Models
{
    public class vwInvitations
    {
        public required string RateeFirstName { get; set; }
        public required string RateeLastName { get; set; }
        public required string RaterRole { get; set; }
        public required string RaterFirstName { get; set; }
        public required string RaterLastName { get; set; }
        public required string RaterEmail { get; set; }
        public required Guid RaterParticipantId { get; set; }
        public required Guid EvaluationRateeId { get; set; }
        public required bool EvaluationTestEmails { get; set; }
        public required DateTime DueDate { get; set; }
        public required string ClientName { get; set; }
        public required Guid ClientId { get; set; }
        public required string ClientHelpContact { get; set; }
        public required string MessageToSend { get; set; }
    }

    // doesn't appear that these are used anywhere and just create additional work
    public class vwRaterInvitations : vwInvitations
    {
    }

    public class vwRateeInvitations : vwInvitations
    {
    }

    public class vwReminderEmails : vwInvitations
    {
    }
}