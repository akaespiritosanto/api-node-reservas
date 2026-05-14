using api_node_reservas.Models;
using Microsoft.EntityFrameworkCore;

namespace api_node_reservas.Data;

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
        modelBuilder.Entity<Node>().HasIndex(node => new { node.SourceTable, node.SourceId }).IsUnique();
        modelBuilder.Entity<Node>().Property(node => node.Id).HasColumnName("id");
        modelBuilder.Entity<Node>().Property(node => node.SourceTable).HasColumnName("source_table").HasMaxLength(100);
        modelBuilder.Entity<Node>().Property(node => node.SourceId).HasColumnName("source_id");
        modelBuilder.Entity<Node>().Property(node => node.Tipo).HasColumnName("tipo").HasMaxLength(100);
        modelBuilder.Entity<Node>().Property(node => node.TipoE).HasColumnName("tipo_e").HasMaxLength(100);
        modelBuilder.Entity<Node>().Property(node => node.Descricao).HasColumnName("descricao").HasMaxLength(2000);
        modelBuilder.Entity<Node>().Property(node => node.IdInformacao).HasColumnName("id_informacao").HasMaxLength(200);
        modelBuilder.Entity<Node>().Property(node => node.Par1).HasColumnName("par1").HasMaxLength(1000);
        modelBuilder.Entity<Node>().Property(node => node.Par2).HasColumnName("par2").HasMaxLength(1000);
        modelBuilder.Entity<Node>().Property(node => node.Par3).HasColumnName("par3").HasMaxLength(1000);
        modelBuilder.Entity<Node>().Property(node => node.Par4).HasColumnName("par4").HasMaxLength(1000);
        modelBuilder.Entity<Node>().Property(node => node.Par5).HasColumnName("par5").HasMaxLength(1000);
        modelBuilder.Entity<Node>().Property(node => node.Par6).HasColumnName("par6").HasMaxLength(1000);
        modelBuilder.Entity<Node>().Property(node => node.Par7).HasColumnName("par7").HasMaxLength(1000);
        modelBuilder.Entity<Node>().Property(node => node.DataCriacao).HasColumnName("data_criacao");
        modelBuilder.Entity<Node>().Property(node => node.DataActualizacao).HasColumnName("data_actualizacao");

        modelBuilder.Entity<Context>().ToTable("Context");
        modelBuilder.Entity<Context>().HasKey(context => context.Id);
        modelBuilder.Entity<Context>().Property(context => context.Id).HasColumnName("id");
        modelBuilder.Entity<Context>().Property(context => context.NodeId).HasColumnName("node_id");
        modelBuilder.Entity<Context>().Property(context => context.Valor).HasColumnName("valor").HasMaxLength(1000);
        modelBuilder.Entity<Context>().Property(context => context.DataCriacao).HasColumnName("data_criacao");

        modelBuilder.Entity<Arc>().ToTable("Arc");
        modelBuilder.Entity<Arc>().HasKey(arc => arc.Id);
        modelBuilder.Entity<Arc>().Property(arc => arc.Id).HasColumnName("id");
        modelBuilder.Entity<Arc>().Property(arc => arc.NodeId).HasColumnName("node_id");
        modelBuilder.Entity<Arc>().Property(arc => arc.Tipo).HasColumnName("tipo").HasMaxLength(100);
        modelBuilder.Entity<Arc>().Property(arc => arc.TargetId).HasColumnName("target_id").HasMaxLength(200);
        modelBuilder.Entity<Arc>().Property(arc => arc.DataCriacao).HasColumnName("data_criacao");
    }
}
