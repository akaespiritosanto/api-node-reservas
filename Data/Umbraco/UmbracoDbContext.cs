using api_node_reservas.Models;
using Microsoft.EntityFrameworkCore;

namespace api_node_reservas.Data;

/*
================================================================================
                             Umbraco source database
================================================================================
 This DbContext maps a small subset of the Umbraco database used by the
 mapping logic. The classes are intentionally minimal so beginners can
 understand and expand them when needed.
================================================================================
*/
public class UmbracoDbContext : DbContext
{
    public UmbracoDbContext(DbContextOptions<UmbracoDbContext> options) : base(options)
    {
    }

    public DbSet<CmsContent> CmsContent => Set<CmsContent>();
    public DbSet<CmsContentType> CmsContentTypes => Set<CmsContentType>();
    public DbSet<CmsDocument> CmsDocuments => Set<CmsDocument>();
    public DbSet<UmbracoNode> UmbracoNodes => Set<UmbracoNode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CmsContent>().ToTable("cmsContent");
        modelBuilder.Entity<CmsContent>().HasKey(c => c.pk);
        modelBuilder.Entity<CmsContent>().Property(c => c.pk).HasColumnName("pk");
        modelBuilder.Entity<CmsContent>().Property(c => c.nodeId).HasColumnName("nodeId");
        modelBuilder.Entity<CmsContent>().Property(c => c.contentType).HasColumnName("contentType");

        modelBuilder.Entity<CmsContentType>().ToTable("cmsContentType");
        modelBuilder.Entity<CmsContentType>().HasKey(c => c.pk);
        modelBuilder.Entity<CmsContentType>().Property(c => c.pk).HasColumnName("pk");
        modelBuilder.Entity<CmsContentType>().Property(c => c.nodeId).HasColumnName("nodeId");
        modelBuilder.Entity<CmsContentType>().Property(c => c.alias).HasColumnName("alias");

        modelBuilder.Entity<CmsDocument>().ToTable("cmsDocument");
        modelBuilder.Entity<CmsDocument>().HasKey(d => d.versionId);
        modelBuilder.Entity<CmsDocument>().Property(d => d.nodeId).HasColumnName("nodeId");
        modelBuilder.Entity<CmsDocument>().Property(d => d.published).HasColumnName("published");
        modelBuilder.Entity<CmsDocument>().Property(d => d.documentUser).HasColumnName("documentUser");
        modelBuilder.Entity<CmsDocument>().Property(d => d.versionId).HasColumnName("versionId");
        modelBuilder.Entity<CmsDocument>().Property(d => d.text).HasColumnName("text");
        modelBuilder.Entity<CmsDocument>().Property(d => d.updateDate).HasColumnName("updateDate");

        modelBuilder.Entity<UmbracoNode>().ToTable("umbracoNode");
        modelBuilder.Entity<UmbracoNode>().HasKey(n => n.id);
        modelBuilder.Entity<UmbracoNode>().Property(n => n.id).HasColumnName("id");
        modelBuilder.Entity<UmbracoNode>().Property(n => n.trashed).HasColumnName("trashed");
        modelBuilder.Entity<UmbracoNode>().Property(n => n.parentID).HasColumnName("parentID");
        modelBuilder.Entity<UmbracoNode>().Property(n => n.nodeUser).HasColumnName("nodeUser");
        modelBuilder.Entity<UmbracoNode>().Property(n => n.level).HasColumnName("level");
        modelBuilder.Entity<UmbracoNode>().Property(n => n.path).HasColumnName("path");
        modelBuilder.Entity<UmbracoNode>().Property(n => n.sortOrder).HasColumnName("sortOrder");
        modelBuilder.Entity<UmbracoNode>().Property(n => n.uniqueID).HasColumnName("uniqueID");
        modelBuilder.Entity<UmbracoNode>().Property(n => n.text).HasColumnName("text");
        modelBuilder.Entity<UmbracoNode>().Property(n => n.nodeObjectType).HasColumnName("nodeObjectType");
        modelBuilder.Entity<UmbracoNode>().Property(n => n.createDate).HasColumnName("createDate");
    }
}
