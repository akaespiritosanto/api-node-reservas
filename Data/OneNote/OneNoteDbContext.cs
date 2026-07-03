using api_node_reservas.Models;
using Microsoft.EntityFrameworkCore;

namespace api_node_reservas.Data;

/*
================================================================================
                             OneNote staging database
================================================================================
 This DbContext points to the database that stores the OneNotePageImport staging
 table. Keeping it separate avoids saving OneNote import rows in the Reservas
 source database.
================================================================================
*/
public class OneNoteDbContext : DbContext
{
    // Receives the OneNote database configuration created in Program.cs.
    public OneNoteDbContext(DbContextOptions<OneNoteDbContext> options) : base(options)
    {
    }

    public DbSet<OneNotePageImport> OneNotePageImports => Set<OneNotePageImport>();

    // Maps the OneNote staging table and columns.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
    }
}
