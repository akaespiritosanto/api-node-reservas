using api_node_reservas.Models;
using Microsoft.EntityFrameworkCore;

namespace api_node_reservas.Data;

/*
================================================================================
                              Source database
================================================================================
 This DbContext points to the reservas database. The processing service mainly
 uses its database connection because mappings can choose different columns.
================================================================================
*/
public class ReservasDbContext : DbContext
{
    // Receives the source database configuration created in Program.cs.
    public ReservasDbContext(DbContextOptions<ReservasDbContext> options) : base(options)
    {
    }

    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<ProdutoReservado> ProdutosReservados => Set<ProdutoReservado>();
    public DbSet<OneNotePageImport> OneNotePageImports => Set<OneNotePageImport>();

    // Maps the source database tables and columns that this project knows how to read.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Reserva>().ToTable("Reserva");
        modelBuilder.Entity<Reserva>().HasKey(reserva => reserva.Id);
        modelBuilder.Entity<Reserva>().Property(reserva => reserva.Id).HasColumnName("id");
        modelBuilder.Entity<Reserva>().Property(reserva => reserva.Numero).HasColumnName("numero");
        modelBuilder.Entity<Reserva>().Property(reserva => reserva.Referencia).HasColumnName("referencia");
        modelBuilder.Entity<Reserva>().Property(reserva => reserva.Observacoes).HasColumnName("observacoes");
        modelBuilder.Entity<Reserva>().Property(reserva => reserva.DataPedido).HasColumnName("data_pedido");
        modelBuilder.Entity<Reserva>().Property(reserva => reserva.DataActualizacao).HasColumnName("data_actualizacao");
        modelBuilder.Entity<Reserva>().Property(reserva => reserva.NomeUtilizadorConfirmacao).HasColumnName("nome_utilizador_confirmacao");

        modelBuilder.Entity<ProdutoReservado>().ToTable("ProdutoReservado");
        modelBuilder.Entity<ProdutoReservado>().HasKey(produto => produto.Id);
        modelBuilder.Entity<ProdutoReservado>().Property(produto => produto.Id).HasColumnName("id");
        modelBuilder.Entity<ProdutoReservado>().Property(produto => produto.IdReserva).HasColumnName("id_reserva");
        modelBuilder.Entity<ProdutoReservado>().Property(produto => produto.IdProduto).HasColumnName("id_produto");
        modelBuilder.Entity<ProdutoReservado>().Property(produto => produto.NomeProduto).HasColumnName("nome_produto");
        modelBuilder.Entity<ProdutoReservado>().Property(produto => produto.Referencia).HasColumnName("referencia");
        modelBuilder.Entity<ProdutoReservado>().Property(produto => produto.DataInicio).HasColumnName("DataInicio");
        modelBuilder.Entity<ProdutoReservado>().Property(produto => produto.DataFim).HasColumnName("DataFim");
        modelBuilder.Entity<ProdutoReservado>().Property(produto => produto.DataCriacao).HasColumnName("data_criacao");
        modelBuilder.Entity<ProdutoReservado>().Property(produto => produto.DataActualizacao).HasColumnName("data_actualizacao");

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
