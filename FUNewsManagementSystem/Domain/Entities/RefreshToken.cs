using System;
using System.Collections.Generic;

namespace FUNewsManagementSystem.Domain.Entities;

public partial class RefreshToken
{
    public int RefreshTokenID { get; set; }

    public short AccountID { get; set; }

    public string Token { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public virtual SystemAccount Account { get; set; } = null!;
}
