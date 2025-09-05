using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace reviews.Models;

public partial class Client
{
    [Key]
    public Guid ClientId { get; set; }

    public string ClientName { get; set; } = null!;

    public string ClientLogoLocation { get; set; } = null!;

    public string ClientHelpContact { get; set; } = null!;

    public bool ClientIsActive { get; set; }

    public string? ClientEmailDomain { get; set; } = "";

    public ICollection<Evaluation> Evaluations { get; set; } = new List<Evaluation>();

    public ICollection<EmailTemplate> EmailTemplates { get; set; } = new List<EmailTemplate>();

    public ICollection<Participant> Participants { get; set; } = new List<Participant>();
}
