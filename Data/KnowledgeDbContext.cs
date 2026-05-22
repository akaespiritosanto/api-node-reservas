using api_node_reservas.Models;
using Microsoft.EntityFrameworkCore;

namespace api_node_reservas.Data;

/*
================================================================================
|                            KnowledgeDbContext                                |
================================================================================
| Este DbContext representa a base de conhecimento.                             |
|                                                                              |
| Aqui ligamos as classes C# as tabelas reais da base de dados e indicamos o    |
| nome de cada coluna. Assim o Entity Framework sabe como ler e gravar dados.   |
================================================================================
*/
public class KnowledgeDbContext : DbContext
{
    public KnowledgeDbContext(DbContextOptions<KnowledgeDbContext> options) : base(options)
    {
    }

    public DbSet<Node> Nodes => Set<Node>();
    public DbSet<Context> Contexts => Set<Context>();
    public DbSet<Arc> Arcs => Set<Arc>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Node>().ToTable("Node");
        modelBuilder.Entity<Node>().HasKey(node => node.Id);
        modelBuilder.Entity<Node>().HasIndex(node => new { node.TypeId, node.Type }).IsUnique();
        modelBuilder.Entity<Node>().Property(node => node.Id).HasColumnName("id");
        modelBuilder.Entity<Node>().Property(node => node.Reference).HasColumnName("reference").HasMaxLength(1000);
        modelBuilder.Entity<Node>().Property(node => node.TypeId).HasColumnName("typeId");
        modelBuilder.Entity<Node>().Property(node => node.Type).HasColumnName("type").HasMaxLength(30);
        modelBuilder.Entity<Node>().Property(node => node.Description).HasColumnName("description").HasMaxLength(8000);
        modelBuilder.Entity<Node>().Property(node => node.Par1).HasColumnName("par1").HasMaxLength(200);
        modelBuilder.Entity<Node>().Property(node => node.Par2).HasColumnName("par2").HasMaxLength(200);
        modelBuilder.Entity<Node>().Property(node => node.Par3).HasColumnName("par3").HasMaxLength(200);
        modelBuilder.Entity<Node>().Property(node => node.Par4).HasColumnName("par4").HasMaxLength(200);
        modelBuilder.Entity<Node>().Property(node => node.Par5).HasColumnName("par5").HasMaxLength(200);
        modelBuilder.Entity<Node>().Property(node => node.Par6).HasColumnName("par6").HasMaxLength(200);
        modelBuilder.Entity<Node>().Property(node => node.Par7).HasColumnName("par7").HasMaxLength(200);
        modelBuilder.Entity<Node>().Property(node => node.Link).HasColumnName("link").HasMaxLength(500);
        modelBuilder.Entity<Node>().Property(node => node.Security).HasColumnName("security");
        modelBuilder.Entity<Node>().Property(node => node.UpdateDate).HasColumnName("updateDate");
        modelBuilder.Entity<Node>().Property(node => node.UpdateUser).HasColumnName("updateUser");
        modelBuilder.Entity<Node>().Property(node => node.DescriptionType).HasColumnName("descriptionType").HasMaxLength(10);

        modelBuilder.Entity<Context>().ToTable("Context");
        modelBuilder.Entity<Context>().HasKey(context => context.Id);
        modelBuilder.Entity<Context>().Property(context => context.Id).HasColumnName("id");
        modelBuilder.Entity<Context>().Property(context => context.Description).HasColumnName("description").HasMaxLength(8000);
        modelBuilder.Entity<Context>().Property(context => context.Location).HasColumnName("location");
        modelBuilder.Entity<Context>().Property(context => context.NodeId).HasColumnName("nodeId");
        modelBuilder.Entity<Context>().Property(context => context.Par1).HasColumnName("par1").HasMaxLength(200);
        modelBuilder.Entity<Context>().Property(context => context.UpdateDate).HasColumnName("updateDate");
        modelBuilder.Entity<Context>().Property(context => context.DescriptionType).HasColumnName("descriptionType").HasMaxLength(10);

        modelBuilder.Entity<Arc>().ToTable("Arc");
        modelBuilder.Entity<Arc>().HasKey(arc => arc.Id);
        modelBuilder.Entity<Arc>().Property(arc => arc.Id).HasColumnName("id");
        modelBuilder.Entity<Arc>().Property(arc => arc.Source).HasColumnName("source");
        modelBuilder.Entity<Arc>().Property(arc => arc.Target).HasColumnName("target");
        modelBuilder.Entity<Arc>().Property(arc => arc.TypeId).HasColumnName("typeId");
        modelBuilder.Entity<Arc>().Property(arc => arc.Type).HasColumnName("type").HasMaxLength(50);
        modelBuilder.Entity<Arc>().Property(arc => arc.UpdateDate).HasColumnName("updateDate");
    }
}
