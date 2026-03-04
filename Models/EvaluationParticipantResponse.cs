using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace reviews.Models;

/// <summary>
/// Stores survey response data (draft and final) in PostgreSQL jsonb format.
/// Each EvaluationParticipant assignment can have one response record.
/// </summary>
public class EvaluationParticipantResponse
{
    [Key]
    public Guid EvaluationParticipantResponseId { get; set; }

    /// <summary>
    /// Reference to the ratee being evaluated
    /// </summary>
    public Guid EvaluationRateeId { get; set; }

    /// <summary>
    /// Reference to the participant (rater) providing the evaluation
    /// </summary>
    public Guid RaterParticipantId { get; set; }

    /// <summary>
    /// The unique password/token for this survey assignment.
    /// This is the key used in the survey URL: /survey/{password}
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string EvaluationParticipantSurveyPassword { get; set; } = null!;

    /// <summary>
    /// Identifies the survey template (e.g., "leadership-competency-v1")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string SurveyTemplateKey { get; set; } = null!;

    /// <summary>
    /// Version of the survey template when this response was created
    /// </summary>
    public int SurveyTemplateVersion { get; set; }

    /// <summary>
    /// Draft response data stored as jsonb (autosaved)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? DraftJson { get; set; }

    /// <summary>
    /// Final submitted response data stored as jsonb
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? FinalJson { get; set; }

    /// <summary>
    /// Timestamp of last draft save
    /// </summary>
    public DateTimeOffset? LastSavedAt { get; set; }

    /// <summary>
    /// Timestamp when the survey was submitted
    /// </summary>
    public DateTimeOffset? SubmittedAt { get; set; }

    /// <summary>
    /// Indicates whether the survey has been submitted (read-only after true)
    /// </summary>
    public bool IsSubmitted { get; set; }

    // Navigation properties
    public virtual EvaluationRatee? EvaluationRatee { get; set; }
    public virtual Participant? RaterParticipant { get; set; }
}
