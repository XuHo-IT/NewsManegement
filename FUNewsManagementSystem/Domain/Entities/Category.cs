using System;
using System.Collections.Generic;

namespace FUNewsManagementSystem.Domain.Entities;

public partial class Category
{
    public short CategoryID { get; set; }

    public string CategoryName { get; set; } = null!;

    public string CategoryDesciption { get; set; } = null!;

    public short? ParentCategoryID { get; set; }

    public bool? IsActive { get; set; }

    public int CategoryStatus { get; set; } = 1; // 1=Draft, 2=Pending, 3=Approved, 4=Published

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }

    public string? ImageUrl { get; set; }

    public short? CreatedByID { get; set; }

    public virtual SystemAccount? CreatedBy { get; set; }

    public virtual ICollection<Category> InverseParentCategory { get; set; } = new List<Category>();

    public virtual ICollection<NewsArticle> NewsArticles { get; set; } = new List<NewsArticle>();

    public virtual Category? ParentCategory { get; set; }
}
