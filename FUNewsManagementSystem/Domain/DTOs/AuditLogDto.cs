namespace FUNewsManagementSystem.Domain.DTOs;

public class AuditLogDto
{
    public int AuditLogID { get; set; }
    public string TableName { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string RecordKey { get; set; } = null!;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? ChangedBy { get; set; }
    public string? ChangedByName { get; set; } // User name or email
    public DateTime ChangedAt { get; set; }
}

