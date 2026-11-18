namespace POVISAPI.Models.Entity;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Records that are archived (hidden) instead of being permanently deleted.</summary>
public interface IArchivable
{
    bool IsArchived { get; set; }
    DateTime? ArchivedAt { get; set; }
}
