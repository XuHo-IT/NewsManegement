using System;
using System.Collections.Generic;

namespace FUNewsManagementSystem.Domain.Entities;

public partial class SystemAccount
{
    public short AccountID { get; set; }

    public string? AccountName { get; set; }

    public string? AccountEmail { get; set; }

    public int? AccountRole { get; set; }

    public string? AccountPassword { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }

    public string? AccountPasswordHash { get; set; }

    public string? GoogleId { get; set; }

    public string? AvatarUrl { get; set; }

    public virtual Role? AccountRoleNavigation { get; set; }

    public virtual ICollection<NewsArticle> NewsArticles { get; set; } = new List<NewsArticle>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
