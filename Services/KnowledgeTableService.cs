using api_node_reservas.Data;
using Microsoft.EntityFrameworkCore;

namespace api_node_reservas.Services;

public class KnowledgeTableService
{
    private readonly KnowledgeDbContext dbContext;

    public KnowledgeTableService(KnowledgeDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task CreateTablesIfNeededAsync()
    {
        string sql = """
IF OBJECT_ID('dbo.Node', 'U') IS NULL
BEGIN
    CREATE TABLE [Node](
        [id] [int] IDENTITY(1,1) NOT NULL,
        [source_table] [varchar](100) NOT NULL,
        [source_id] [int] NOT NULL,
        [tipo] [varchar](100) NOT NULL,
        [tipo_e] [varchar](100) NOT NULL,
        [descricao] [varchar](2000) NOT NULL,
        [id_informacao] [varchar](200) NOT NULL,
        [par1] [varchar](1000) NOT NULL,
        [par2] [varchar](1000) NOT NULL,
        [par3] [varchar](1000) NOT NULL,
        [par4] [varchar](1000) NOT NULL,
        [par5] [varchar](1000) NOT NULL,
        [par6] [varchar](1000) NOT NULL,
        [par7] [varchar](1000) NOT NULL,
        [data_criacao] [datetime] NOT NULL,
        [data_actualizacao] [datetime] NOT NULL,
        CONSTRAINT [PK_Node] PRIMARY KEY ([id])
    )

    CREATE UNIQUE INDEX [IX_Node_Source] ON [Node]([source_table], [source_id])
END

IF OBJECT_ID('dbo.Context', 'U') IS NULL
BEGIN
    CREATE TABLE [Context](
        [id] [int] IDENTITY(1,1) NOT NULL,
        [node_id] [int] NOT NULL,
        [valor] [varchar](1000) NOT NULL,
        [data_criacao] [datetime] NOT NULL,
        CONSTRAINT [PK_Context] PRIMARY KEY ([id])
    )
END

IF OBJECT_ID('dbo.Arc', 'U') IS NULL
BEGIN
    CREATE TABLE [Arc](
        [id] [int] IDENTITY(1,1) NOT NULL,
        [node_id] [int] NOT NULL,
        [tipo] [varchar](100) NOT NULL,
        [target_id] [varchar](200) NOT NULL,
        [data_criacao] [datetime] NOT NULL,
        CONSTRAINT [PK_Arc] PRIMARY KEY ([id])
    )
END
""";

        await dbContext.Database.ExecuteSqlRawAsync(sql);
    }
}
