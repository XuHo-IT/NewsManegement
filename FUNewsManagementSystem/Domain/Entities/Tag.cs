using System;
using System.Collections.Generic;

namespace FUNewsManagementSystem.Domain.Entities;

public partial class Tag
{
    public int TagID { get; set; }

    public string? TagName { get; set; }

    public string? Note { get; set; }

    public bool? IsActive { get; set; }

    public int TagStatus { get; set; } = 1; // 1=Draft, 2=Pending, 3=Approved, 4=Published

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }

    public string? ImageUrl { get; set; }

    public short? CreatedByID { get; set; }

    public virtual SystemAccount? CreatedBy { get; set; }

    public virtual ICollection<NewsArticle> NewsArticles { get; set; } = new List<NewsArticle>();
}
