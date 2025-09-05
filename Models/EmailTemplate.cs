using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace reviews.Models;

public partial class EmailTemplate
{
    [Key]
    public Guid EmailTemplateId { get; set; }

    public Guid ClientId { get; set; }

    public string EmailTemplateName { get; set; } = null!;

    public string EmailFromName { get; set; } = null!;

    public string EmailSubject { get; set; } = null!;

    public string EmailTemplateContent { get; set; } = null!;

    public Client? Client { get; set; }
}
