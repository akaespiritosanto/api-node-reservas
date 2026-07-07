using api_node_reservas.Models;
using Microsoft.EntityFrameworkCore;

namespace api_node_reservas.Data;

/*
================================================================================
                             Knowledge database
================================================================================
 This DbContext points to the final database where processed information is saved
 as Nodes, Contexts and Arcs. It only maps the fields that exist in the original
 SQL script for the api_node_reservas database.
================================================================================
*/
public class KnowledgeDbContext : DbContext
{
    // Receives the database configuration created in Program.cs.
    public KnowledgeDbContext(DbContextOptions<KnowledgeDbContext> options) : base(options)
    {
    }

    public DbSet<Node> Nodes => Set<Node>();
    public DbSet<Context> Contexts => Set<Context>();
    public DbSet<Arc> Arcs => Set<Arc>();
    public DbSet<OneNoteSyncState> OneNoteSyncStates => Set<OneNoteSyncState>();
    public DbSet<OneNotePageImport> OneNotePageImports => Set<OneNotePageImport>();

    // Maps the C# models to the original SQL Server table and column names.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Node>().ToTable("Node");
        modelBuilder.Entity<Node>().HasKey(node => node.Id);
        modelBuilder.Entity<Node>().Property(node => node.Id).HasColumnName("id");
        modelBuilder.Entity<Node>().Property(node => node.Reference).HasColumnName("reference").HasColumnType("varchar(1000)");
        modelBuilder.Entity<Node>().Property(node => node.TypeId).HasColumnName("typeId");
        modelBuilder.Entity<Node>().Property(node => node.Type).HasColumnName("type").HasColumnType("varchar(30)");
        modelBuilder.Entity<Node>().Property(node => node.Description).HasColumnName("description").HasColumnType("varchar(8000)");
        modelBuilder.Entity<Node>().Property(node => node.Par1).HasColumnName("par1").HasColumnType("varchar(200)");
        modelBuilder.Entity<Node>().Property(node => node.Par2).HasColumnName("par2").HasColumnType("varchar(200)");
        modelBuilder.Entity<Node>().Property(node => node.Par3).HasColumnName("par3").HasColumnType("varchar(200)");
        modelBuilder.Entity<Node>().Property(node => node.Par4).HasColumnName("par4").HasColumnType("varchar(200)");
        modelBuilder.Entity<Node>().Property(node => node.Par5).HasColumnName("par5").HasColumnType("varchar(200)");
        modelBuilder.Entity<Node>().Property(node => node.Par6).HasColumnName("par6").HasColumnType("varchar(200)");
        modelBuilder.Entity<Node>().Property(node => node.Par7).HasColumnName("par7").HasColumnType("varchar(200)");
        modelBuilder.Entity<Node>().Property(node => node.Link).HasColumnName("link").HasColumnType("varchar(500)");
        modelBuilder.Entity<Node>().Property(node => node.ExternalId).HasColumnName("externalId").HasColumnType("varchar(200)");
        modelBuilder.Entity<Node>().Property(node => node.Security).HasColumnName("security");
        modelBuilder.Entity<Node>().Property(node => node.UpdateDate).HasColumnName("updateDate").HasColumnType("datetime");
        modelBuilder.Entity<Node>().Property(node => node.UpdateUser).HasColumnName("updateUser");
        modelBuilder.Entity<Node>().Property(node => node.DescriptionType).HasColumnName("descriptionType").HasColumnType("varchar(10)");

        modelBuilder.Entity<Context>().ToTable("Context");
        modelBuilder.Entity<Context>().HasKey(context => context.Id);
        modelBuilder.Entity<Context>().Property(context => context.Id).HasColumnName("id");
        modelBuilder.Entity<Context>().Property(context => context.Description).HasColumnName("description").HasColumnType("varchar(8000)");
        // The original 'parent' column was removed and its values copied into
        // the 'location' column. The Context model no longer has a Parent
        // property; Location now contains the parent context id when used
        // as part of the tree structure.
        modelBuilder.Entity<Context>().Property(context => context.Location).HasColumnName("location");
        modelBuilder.Entity<Context>().Property(context => context.NodeId).HasColumnName("nodeId");
        modelBuilder.Entity<Context>().Property(context => context.Par1).HasColumnName("par1").HasColumnType("varchar(200)");
        modelBuilder.Entity<Context>().Property(context => context.UpdateDate).HasColumnName("updateDate").HasColumnType("datetime");
        modelBuilder.Entity<Context>().Property(context => context.DescriptionType).HasColumnName("descriptionType").HasColumnType("varchar(10)");

        modelBuilder.Entity<Arc>().ToTable("Arc");
        modelBuilder.Entity<Arc>().HasKey(arc => arc.Id);
        modelBuilder.Entity<Arc>().Property(arc => arc.Id).HasColumnName("id");
        modelBuilder.Entity<Arc>().Property(arc => arc.Source).HasColumnName("source");
        modelBuilder.Entity<Arc>().Property(arc => arc.Target).HasColumnName("target");
        modelBuilder.Entity<Arc>().Property(arc => arc.TypeId).HasColumnName("typeId");
        modelBuilder.Entity<Arc>().Property(arc => arc.Type).HasColumnName("type").HasColumnType("varchar(50)");
        modelBuilder.Entity<Arc>().Property(arc => arc.UpdateDate).HasColumnName("updateDate").HasColumnType("datetime");

        modelBuilder.Entity<OneNotePageImport>().ToTable("OneNotePageImport");
        modelBuilder.Entity<OneNotePageImport>().HasKey(page => page.Id);
        modelBuilder.Entity<OneNotePageImport>().Property(page => page.Id).HasColumnName("id");
        modelBuilder.Entity<OneNotePageImport>().Property(page => page.GraphPageId).HasColumnName("graphPageId").HasColumnType("nvarchar(200)");
        modelBuilder.Entity<OneNotePageImport>().Property(page => page.UserId).HasColumnName("userId").HasColumnType("nvarchar(200)");
        modelBuilder.Entity<OneNotePageImport>().Property(page => page.NotebookId).HasColumnName("notebookId").HasColumnType("nvarchar(200)");
        modelBuilder.Entity<OneNotePageImport>().Property(page => page.NotebookName).HasColumnName("notebookName").HasColumnType("nvarchar(500)");
        modelBuilder.Entity<OneNotePageImport>().Property(page => page.SectionId).HasColumnName("sectionId").HasColumnType("nvarchar(200)");
        modelBuilder.Entity<OneNotePageImport>().Property(page => page.SectionName).HasColumnName("sectionName").HasColumnType("nvarchar(500)");
        modelBuilder.Entity<OneNotePageImport>().Property(page => page.PageTitle).HasColumnName("pageTitle").HasColumnType("nvarchar(1000)");
        modelBuilder.Entity<OneNotePageImport>().Property(page => page.ContentText).HasColumnName("contentText").HasColumnType("nvarchar(max)");
        modelBuilder.Entity<OneNotePageImport>().Property(page => page.ContentHtml).HasColumnName("contentHtml").HasColumnType("nvarchar(max)");
        modelBuilder.Entity<OneNotePageImport>().Property(page => page.CreatedDateTime).HasColumnName("createdDateTime");
        modelBuilder.Entity<OneNotePageImport>().Property(page => page.LastModifiedDateTime).HasColumnName("lastModifiedDateTime");
        modelBuilder.Entity<OneNotePageImport>().Property(page => page.WebUrl).HasColumnName("webUrl").HasColumnType("nvarchar(1000)");
        modelBuilder.Entity<OneNotePageImport>().Property(page => page.ImportedAt).HasColumnName("importedAt");

        modelBuilder.Entity<OneNoteSyncState>().ToTable("OneNoteSyncState");
        modelBuilder.Entity<OneNoteSyncState>().HasKey(sync => sync.Id);
        modelBuilder.Entity<OneNoteSyncState>().Property(sync => sync.Id).HasColumnName("id");
        modelBuilder.Entity<OneNoteSyncState>().Property(sync => sync.NodeId).HasColumnName("nodeId");
        modelBuilder.Entity<OneNoteSyncState>().Property(sync => sync.OneNotePageId).HasColumnName("oneNotePageId").HasColumnType("nvarchar(200)");
        modelBuilder.Entity<OneNoteSyncState>().Property(sync => sync.LastSyncDate).HasColumnName("lastSyncDate");
        modelBuilder.Entity<OneNoteSyncState>().Property(sync => sync.NodeUpdateDate).HasColumnName("nodeUpdateDate");
        modelBuilder.Entity<OneNoteSyncState>().Property(sync => sync.OneNoteUpdateDate).HasColumnName("oneNoteUpdateDate");
        modelBuilder.Entity<OneNoteSyncState>().Property(sync => sync.Status).HasColumnName("status").HasColumnType("nvarchar(50)");
    }
}
