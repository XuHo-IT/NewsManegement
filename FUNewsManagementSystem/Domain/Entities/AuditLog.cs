using System;
using System.Collections.Generic;

namespace FUNewsManagementSystem.Domain.Entities;

public partial class AuditLog
{
    public int AuditLogID { get; set; }

    public string TableName { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string RecordKey { get; set; } = null!;

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; }
}
