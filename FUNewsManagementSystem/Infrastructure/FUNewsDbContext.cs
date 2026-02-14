using System;
using System.Collections.Generic;
using FUNewsManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FUNewsManagementSystem.Infrastructure;

public partial class FUNewsDbContext : DbContext
{
    public FUNewsDbContext()
    {
    }

    public FUNewsDbContext(DbContextOptions<FUNewsDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<NewsArticle> NewsArticles { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SystemAccount> SystemAccounts { get; set; }

    public virtual DbSet<Tag> Tags { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Name=ConnectionStrings:DefaultConnection");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditLogID).HasName("PK__AuditLog__EB5F6CDD196F5A65");

            entity.ToTable("AuditLog");

            entity.Property(e => e.Action).HasMaxLength(20);
            entity.Property(e => e.ChangedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ChangedBy).HasMaxLength(100);
            entity.Property(e => e.RecordKey).HasMaxLength(128);
            entity.Property(e => e.TableName).HasMaxLength(128);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Category", tb => tb.HasTrigger("TR_Audit_Category"));

            entity.Property(e => e.CategoryDesciption).HasMaxLength(250);
            entity.Property(e => e.CategoryName).HasMaxLength(100);
            entity.Property(e => e.CategoryStatus).HasDefaultValue(1);
            entity.Property(e => e.DeletedAt).HasColumnType("datetime");
            entity.Property(e => e.DeletedBy).HasMaxLength(100);

            entity.HasOne(d => d.ParentCategory).WithMany(p => p.InverseParentCategory)
                .HasForeignKey(d => d.ParentCategoryID)
                .HasConstraintName("FK_Category_Category");

            entity.HasOne(d => d.CreatedBy).WithMany()
                .HasForeignKey(d => d.CreatedByID)
                .HasConstraintName("FK_Category_SystemAccount");
        });

        modelBuilder.Entity<NewsArticle>(entity =>
        {
            entity.ToTable("NewsArticle", tb => tb.HasTrigger("TR_Audit_NewsArticle"));

            entity.Property(e => e.NewsArticleID).HasMaxLength(20);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.DeletedAt).HasColumnType("datetime");
            entity.Property(e => e.DeletedBy).HasMaxLength(100);
            entity.Property(e => e.Headline).HasMaxLength(150);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.NewsContent).HasMaxLength(4000);
            entity.Property(e => e.NewsSource).HasMaxLength(400);
            entity.Property(e => e.NewsStatus).HasDefaultValue(1);
            entity.Property(e => e.NewsTitle).HasMaxLength(400);

            entity.HasOne(d => d.Category).WithMany(p => p.NewsArticles)
                .HasForeignKey(d => d.CategoryID)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_NewsArticle_Category");

            entity.HasOne(d => d.CreatedBy).WithMany(p => p.NewsArticles)
                .HasForeignKey(d => d.CreatedByID)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_NewsArticle_SystemAccount");

            entity.HasMany(d => d.Tags).WithMany(p => p.NewsArticles)
                .UsingEntity<Dictionary<string, object>>(
                    "NewsTag",
                    r => r.HasOne<Tag>().WithMany()
                        .HasForeignKey("TagID")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_NewsTag_Tag"),
                    l => l.HasOne<NewsArticle>().WithMany()
                        .HasForeignKey("NewsArticleID")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_NewsTag_NewsArticle"),
                    j =>
                    {
                        j.HasKey("NewsArticleID", "TagID");
                        j.ToTable("NewsTag");
                        j.IndexerProperty<string>("NewsArticleID").HasMaxLength(20);
                    });
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.RefreshTokenID).HasName("PK__RefreshT__F5845E59A7D510B6");

            entity.ToTable("RefreshToken");

            entity.HasIndex(e => e.Token, "IX_RefreshToken_Token");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ExpiresAt).HasColumnType("datetime");
            entity.Property(e => e.RevokedAt).HasColumnType("datetime");
            entity.Property(e => e.Token).HasMaxLength(200);

            entity.HasOne(d => d.Account).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.AccountID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RefreshToken_SystemAccount");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleID).HasName("PK__Role__8AFACE3A0C2E32F6");

            entity.ToTable("Role");

            entity.Property(e => e.RoleID).ValueGeneratedNever();
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<SystemAccount>(entity =>
        {
            entity.HasKey(e => e.AccountID);

            entity.ToTable("SystemAccount", tb => tb.HasTrigger("TR_Audit_SystemAccount"));

            entity.Property(e => e.AccountID).ValueGeneratedNever();
            entity.Property(e => e.AccountEmail).HasMaxLength(70);
            entity.Property(e => e.AccountName).HasMaxLength(100);
            entity.Property(e => e.AccountPassword).HasMaxLength(70);
            entity.Property(e => e.AccountPasswordHash).HasMaxLength(200);
            entity.Property(e => e.DeletedAt).HasColumnType("datetime");
            entity.Property(e => e.DeletedBy).HasMaxLength(100);

            entity.HasOne(d => d.AccountRoleNavigation).WithMany(p => p.SystemAccounts)
                .HasForeignKey(d => d.AccountRole)
                .HasConstraintName("FK_SystemAccount_Role");
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.TagID).HasName("PK_HashTag");

            entity.ToTable("Tag", tb => tb.HasTrigger("TR_Audit_Tag"));

            entity.Property(e => e.TagID).ValueGeneratedNever();
            entity.Property(e => e.DeletedAt).HasColumnType("datetime");
            entity.Property(e => e.DeletedBy).HasMaxLength(100);
            entity.Property(e => e.Note).HasMaxLength(400);
            entity.Property(e => e.TagName).HasMaxLength(50);
            entity.Property(e => e.TagStatus).HasDefaultValue(1);

            entity.HasOne(d => d.CreatedBy).WithMany()
                .HasForeignKey(d => d.CreatedByID)
                .HasConstraintName("FK_Tag_SystemAccount");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
