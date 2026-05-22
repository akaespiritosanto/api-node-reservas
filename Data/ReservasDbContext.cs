using api_node_reservas.Models;
using Microsoft.EntityFrameworkCore;

namespace api_node_reservas.Data;

/*
================================================================================
|                             ReservasDbContext                                |
================================================================================
| Este DbContext representa a base de dados de origem, ou seja, a base das      |
| reservas.                                                                     |
|                                                                              |
| Aqui dizemos ao Entity Framework que classes C# correspondem as tabelas e     |
| colunas reais da base de dados.                                               |
================================================================================
*/
public class ReservasDbContext : DbContext
{
    public ReservasDbContext(DbContextOptions<ReservasDbContext> options) : base(options)
    {
    }

    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<ProdutoReservado> ProdutosReservados => Set<ProdutoReservado>();

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
    }
}
