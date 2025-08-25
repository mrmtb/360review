using System.ComponentModel.DataAnnotations;

public class DataProtectionKey
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string FriendlyName { get; set; } = null!;

    [Required]
    public string Xml { get; set; } = null!;
}