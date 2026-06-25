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
    public DbSet<CmsContentXml> CmsContentXml => Set<CmsContentXml>();
    public DbSet<CmsMember> CmsMembers => Set<CmsMember>();
    public DbSet<UmbracoNode> UmbracoNodes => Set<UmbracoNode>();
    public DbSet<CmsPropertyType> CmsPropertyTypes => Set<CmsPropertyType>();
    public DbSet<CmsPropertyData> CmsPropertyData => Set<CmsPropertyData>();
    public DbSet<APNode> APNodes => Set<APNode>();
    public DbSet<EntityPage> EntityPages => Set<EntityPage>();
    public DbSet<PaymentTypeNode> PaymentTypeNodes => Set<PaymentTypeNode>();
    public DbSet<ApLog> ApLogs => Set<ApLog>();

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

        modelBuilder.Entity<CmsContentXml>().ToTable("cmsContentXml");
        modelBuilder.Entity<CmsContentXml>().HasKey(x => x.nodeId);
        modelBuilder.Entity<CmsContentXml>().Property(x => x.nodeId).HasColumnName("nodeId");
        modelBuilder.Entity<CmsContentXml>().Property(x => x.xml).HasColumnName("xml");

        modelBuilder.Entity<CmsMember>().ToTable("cmsMember");
        modelBuilder.Entity<CmsMember>().HasKey(m => m.nodeId);
        modelBuilder.Entity<CmsMember>().Property(m => m.nodeId).HasColumnName("nodeId");
        modelBuilder.Entity<CmsMember>().Property(m => m.Email).HasColumnName("Email");
        modelBuilder.Entity<CmsMember>().Property(m => m.LoginName).HasColumnName("LoginName");
        modelBuilder.Entity<CmsMember>().Property(m => m.Password).HasColumnName("Password");

        // UmbracoNode mapping
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

        // CmsPropertyType mapping
        modelBuilder.Entity<CmsPropertyType>().ToTable("cmsPropertyType");
        modelBuilder.Entity<CmsPropertyType>().HasKey(p => p.id);
        modelBuilder.Entity<CmsPropertyType>().Property(p => p.id).HasColumnName("id");
        modelBuilder.Entity<CmsPropertyType>().Property(p => p.dataTypeId).HasColumnName("dataTypeId");
        modelBuilder.Entity<CmsPropertyType>().Property(p => p.contentTypeId).HasColumnName("contentTypeId");
        modelBuilder.Entity<CmsPropertyType>().Property(p => p.propertyTypeGroupId).HasColumnName("propertyTypeGroupId");
        modelBuilder.Entity<CmsPropertyType>().Property(p => p.Alias).HasColumnName("Alias");
        modelBuilder.Entity<CmsPropertyType>().Property(p => p.Name).HasColumnName("Name");
        modelBuilder.Entity<CmsPropertyType>().Property(p => p.helpText).HasColumnName("helpText");
        modelBuilder.Entity<CmsPropertyType>().Property(p => p.sortOrder).HasColumnName("sortOrder");
        modelBuilder.Entity<CmsPropertyType>().Property(p => p.mandatory).HasColumnName("mandatory");
        modelBuilder.Entity<CmsPropertyType>().Property(p => p.validationRegExp).HasColumnName("validationRegExp");
        modelBuilder.Entity<CmsPropertyType>().Property(p => p.Description).HasColumnName("Description");

        // CmsPropertyData mapping
        modelBuilder.Entity<CmsPropertyData>().ToTable("cmsPropertyData");
        modelBuilder.Entity<CmsPropertyData>().HasKey(p => p.id);
        modelBuilder.Entity<CmsPropertyData>().Property(p => p.id).HasColumnName("id");
        modelBuilder.Entity<CmsPropertyData>().Property(p => p.contentNodeId).HasColumnName("contentNodeId");
        modelBuilder.Entity<CmsPropertyData>().Property(p => p.versionId).HasColumnName("versionId");
        modelBuilder.Entity<CmsPropertyData>().Property(p => p.propertytypeid).HasColumnName("propertytypeid");
        modelBuilder.Entity<CmsPropertyData>().Property(p => p.dataInt).HasColumnName("dataInt");
        modelBuilder.Entity<CmsPropertyData>().Property(p => p.dataDate).HasColumnName("dataDate");
        modelBuilder.Entity<CmsPropertyData>().Property(p => p.dataNvarchar).HasColumnName("dataNvarchar");
        modelBuilder.Entity<CmsPropertyData>().Property(p => p.dataNtext).HasColumnName("dataNtext");

        // ApLog mapping (custom table)
        modelBuilder.Entity<ApLog>().ToTable("aplog");
        modelBuilder.Entity<ApLog>().HasKey(a => a.ID);
        modelBuilder.Entity<ApLog>().Property(a => a.ID).HasColumnName("ID");
        modelBuilder.Entity<ApLog>().Property(a => a.RequestDate).HasColumnName("RequestDate");
        modelBuilder.Entity<ApLog>().Property(a => a.RequestAP_ID).HasColumnName("RequestAP_ID");
        modelBuilder.Entity<ApLog>().Property(a => a.RequestAP_AP).HasColumnName("RequestAP_AP");
        modelBuilder.Entity<ApLog>().Property(a => a.RequestAP_T).HasColumnName("RequestAP_T");
        modelBuilder.Entity<ApLog>().Property(a => a.RequestAP_URL).HasColumnName("RequestAP_URL");
        modelBuilder.Entity<ApLog>().Property(a => a.UsedAP_AP).HasColumnName("UsedAP_AP");
        modelBuilder.Entity<ApLog>().Property(a => a.UsedAP_LocationID).HasColumnName("UsedAP_LocationID");
        modelBuilder.Entity<ApLog>().Property(a => a.UsedAP_EntityID).HasColumnName("UsedAP_EntityID");
        modelBuilder.Entity<ApLog>().Property(a => a.UserIP).HasColumnName("UserIP");
        modelBuilder.Entity<ApLog>().Property(a => a.UserBrowserName).HasColumnName("UserBrowserName");
        modelBuilder.Entity<ApLog>().Property(a => a.UserBrowserVersion).HasColumnName("UserBrowserVersion");
        modelBuilder.Entity<ApLog>().Property(a => a.UserBrowserLanguage).HasColumnName("UserBrowserLanguage");
        modelBuilder.Entity<ApLog>().Property(a => a.UserOSPlatform).HasColumnName("UserOSPlatform");
        modelBuilder.Entity<ApLog>().Property(a => a.Notes).HasColumnName("Notes");
        modelBuilder.Entity<ApLog>().Property(a => a.UserAgent).HasColumnName("UserAgent");

        // Document Type models require defined keys or HasNoKey in EF Core
        modelBuilder.Entity<APNode>().HasKey(a => a.NodeId);
        modelBuilder.Entity<EntityPage>().HasKey(e => e.NodeId);
        modelBuilder.Entity<PaymentTypeNode>().HasKey(p => p.NodeId);
    }
}
