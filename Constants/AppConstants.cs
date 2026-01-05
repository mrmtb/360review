namespace reviews.Constants;

public static class StatusNames
{
    public const string Draft = "Draft";
    public const string Self = "Self";
    public const string ReminderGeneral = "Reminder - General";
    public const string Completed = "Completed";
    public const string Unfamiliar = "Unfamiliar";
}

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Client = "Client";
    public const string Participant = "Participant";
    public const string AdminOrClient = "Admin,Client";
    public const string AdminOrParticipant = "Admin,Participant";
}

public static class EmailTemplateNames
{
    public const string InvitationRater = "Invitation - Rater";
    public const string InvitationRatee = "Invitation - Ratee";
    public const string ReminderGeneral = "Reminder - General";
}

public static class EvaluationParticipantStatusNames
{
    public const string InvitationNotSent = "Invitation Not Sent";
    public const string PendingApproval = "Pending Approval";
    public const string ManagerDeclined = "Manager Declined";
    public const string InvitationSent = "Invitation Sent";
    public const string OptOut = "Opt-out";
    public const string Unfamiliar = "Unfamiliar";
    public const string Completed = "Completed";
    public const string Deleted = "Deleted";
}

public static class EvaluationParticipantStatusIds
{
    public const int InvitationNotSent = 1;
    public const int PendingApproval = 2;
    public const int ManagerDeclined = 3;
    public const int InvitationSent = 4;
    public const int OptOut = 5;
    public const int Unfamiliar = 6;
    public const int Completed = 7;
    public const int Deleted = 8;
}

public static class ParticipantRoleNames
{
    public const string Self = "Self";
    public const string Manager = "Manager";
    public const string Peer = "Peer";
    public const string DirectReport = "Direct Report";
}

public static class ErrorMessages
{
    public const string NoParticipantsSelected = "No participants selected for reminders.";
    public const string InvalidParticipantId = "Invalid participantId.";
    public const string InvalidEvaluationData = "Invalid Evaluation data.";
    public const string InvalidClientData = "Invalid Client data.";
    public const string InvalidEmailTemplateData = "Invalid Email Template data.";
    public const string InvalidEvaluationRateeId = "Invalid evaluation ratee or participant ID.";
    public const string InvalidRequest = "Invalid request. Please provide a valid Participant ID.";
    
    public const string FailedToRetrieveSession = "Failed to retrieve session data";
    public const string FailedToProcessReminders = "Failed to process reminders.";
    public const string FailedToSaveParticipant = "An error occurred while saving the participant.";
    public const string FailedToSaveEvaluation = "Failed to save evaluation.";
    public const string FailedToSaveClient = "Failed to save client.";
    public const string FailedToCreateClient = "Failed to create client.";
    public const string FailedToUpdateClient = "Failed to update client.";
    public const string FailedToSaveEmailTemplate = "Failed to save email template.";
    public const string FailedToProcessInvitations = "Failed to process invitations.";
    public const string FailedToAddNewRater = "Failed to add new rater.";
    public const string FailedToAddRater = "Failed to add rater.";
    
    public const string UnableToLaunchEvaluation = "Unable to launch evaluation.";
    public const string UnableToProcessSurvey = "Unable to process completed survey.";
    
    public const string EmailNotFoundOrSent = "Email not found or already sent.";
    public const string NoEmailTemplate = "No email template found for {0}";
    public const string EmailListEmpty = "Email list cannot be null or empty.";
    public const string EmailTemplatesEmpty = "Email templates cannot be null or empty.";
    
    public const string MigrationDisabled = "Migrations are disabled in configuration.";
    public const string MigrationDataExists = "Data already exists. Migration not required.";
    public const string MigrationLegacyNotFound = "Legacy schema not found. Cannot migrate.";
    public const string MigrationFailed = "Migration failed";
    public const string MigrationRolledBack = "Migration failed and was rolled back";
    public const string ValidationFailed = "Validation failed";
}

public static class SuccessMessages
{
    public const string MigrationCompleted = "Migration completed successfully";
    public const string ClientDeleted = "Client deleted successfully";
}

public static class ActionNames
{
    public const string Error = "Error";
    public const string EmailQueueDashboard = "EmailQueueDashboard";
    public const string ViewRaters = "ViewRaters";
}

public static class ViewDataKeys
{
    public const string ActiveClient = "ActiveClient";
    public const string ReturnUrl = "ReturnUrl";
}

public static class PartialViewNames
{
    public const string ManageClientsPartial = "_ManageClientsPartial";
}
