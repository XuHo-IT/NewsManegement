using System;
using System.Collections.Generic;

namespace FUNewsManagementSystem.Domain.Entities;

public partial class NewsArticle
{
    public string NewsArticleID { get; set; } = null!;

    public string? NewsTitle { get; set; }

    public string Headline { get; set; } = null!;

    public DateTime? CreatedDate { get; set; }

    public string? NewsContent { get; set; }

    public string? NewsSource { get; set; }

    public short? CategoryID { get; set; }

    public short? CreatedByID { get; set; }

    public short? UpdatedByID { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public int NewsStatus { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }

    public string? ImageUrl { get; set; }

    public virtual Category? Category { get; set; }

    public virtual SystemAccount? CreatedBy { get; set; }

    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
}
