namespace POVISAPI.Models.Entity;

/// <summary>A photo attached to a violation as evidence. At most 5 per violation; JPEG or PNG only.</summary>
public class ViolationEvidence : BaseEntity
{
    public Guid ViolationId { get; set; }
    public Violation Violation { get; set; } = null!;

    /// <summary>"image/jpeg" or "image/png".</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>The image as a base64 data URL ("data:image/jpeg;base64,...").</summary>
    public string ImageData { get; set; } = string.Empty;
}
