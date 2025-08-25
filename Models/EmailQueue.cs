namespace reviews.Models;

public partial class EmailQueue
{
    public int EmailQueueId { get; set; }

    public Guid ParticipantId { get; set; }

    public Guid EvaluationRateeId { get; set; }

    public Guid ClientId { get; set; }

    public string EmailActor { get; set; } = null!;

    public DateTime EmailQueuedDateTime { get; set; }

    public bool EmailTest { get; set; } = true;

    public string EmailFrom { get; set; } = null!;

    public string EmailTo { get; set; } = null!;
    
    public string EmailSubject { get; set; } = null!;

    public string EmailBodyHTML { get; set; } = null!;

    public string EmailBodyText { get; set; } = null!;

    public bool EmailSent { get; set; } = false;

    public DateTime EmailSentDateTime { get; set; }

}
