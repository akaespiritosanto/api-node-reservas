using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;

namespace api_node_reservas.Services;

public partial class OneNoteImportService
{
    /*
    ============================================================================
                                OneNote import table
    ============================================================================
     This file contains only database work for OneNote imports:
     - Create the OneNotePageImport table if it does not exist.
     - Insert new pages.
     - Update pages that were already imported before.
    ============================================================================
    */

    // Creates the staging table automatically if the database does not have it yet.
    private async Task CreateImportTableIfMissingAsync()
    {
        string sql = @"
IF OBJECT_ID('dbo.OneNotePageImport', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OneNotePageImport
    (
        id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        graphPageId NVARCHAR(200) NOT NULL,
        userId NVARCHAR(200) NOT NULL,
        notebookName NVARCHAR(500) NOT NULL,
        sectionName NVARCHAR(500) NOT NULL,
        pageTitle NVARCHAR(1000) NOT NULL,
        contentText NVARCHAR(MAX) NOT NULL,
        contentHtml NVARCHAR(MAX) NOT NULL,
        createdDateTime DATETIME2 NOT NULL,
        lastModifiedDateTime DATETIME2 NOT NULL,
        webUrl NVARCHAR(1000) NOT NULL,
        importedAt DATETIME2 NOT NULL
    );

    CREATE UNIQUE INDEX IX_OneNotePageImport_GraphPageId ON dbo.OneNotePageImport(graphPageId);
END";

        await reservasDbContext.Database.ExecuteSqlRawAsync(sql);
    }

    // Inserts a new page or updates the existing row for the same Microsoft page id.
    private async Task SavePageAsync(OneNotePageData page)
    {
        DbConnection connection = reservasDbContext.Database.GetDbConnection();
        bool closeConnection = connection.State != ConnectionState.Open;

        if (closeConnection)
        {
            await connection.OpenAsync();
        }

        try
        {
            bool pageAlreadyExists = await PageExistsAsync(connection, page.GraphPageId);

            if (pageAlreadyExists)
            {
                await UpdatePageAsync(connection, page);
            }
            else
            {
                await InsertPageAsync(connection, page);
            }
        }
        finally
        {
            if (closeConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    // Checks if this Microsoft page was already imported before.
    private static async Task<bool> PageExistsAsync(DbConnection connection, string graphPageId)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT id FROM dbo.OneNotePageImport WHERE graphPageId = @graphPageId";
        AddParameter(command, "@graphPageId", graphPageId);

        object? existingId = await command.ExecuteScalarAsync();
        return existingId is not null;
    }

    // Adds one new row to OneNotePageImport.
    private static async Task InsertPageAsync(DbConnection connection, OneNotePageData page)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO dbo.OneNotePageImport
(graphPageId, userId, notebookName, sectionName, pageTitle, contentText, contentHtml, createdDateTime, lastModifiedDateTime, webUrl, importedAt)
VALUES
(@graphPageId, @userId, @notebookName, @sectionName, @pageTitle, @contentText, @contentHtml, @createdDateTime, @lastModifiedDateTime, @webUrl, @importedAt)";

        AddPageParameters(command, page);
        await command.ExecuteNonQueryAsync();
    }

    // Updates the row when this OneNote page was imported before.
    private static async Task UpdatePageAsync(DbConnection connection, OneNotePageData page)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = @"
UPDATE dbo.OneNotePageImport
SET userId = @userId,
    notebookName = @notebookName,
    sectionName = @sectionName,
    pageTitle = @pageTitle,
    contentText = @contentText,
    contentHtml = @contentHtml,
    createdDateTime = @createdDateTime,
    lastModifiedDateTime = @lastModifiedDateTime,
    webUrl = @webUrl,
    importedAt = @importedAt
WHERE graphPageId = @graphPageId";

        AddPageParameters(command, page);
        await command.ExecuteNonQueryAsync();
    }

    // Adds all page values to a SQL command as parameters.
    private static void AddPageParameters(DbCommand command, OneNotePageData page)
    {
        AddParameter(command, "@graphPageId", page.GraphPageId);
        AddParameter(command, "@userId", page.UserId);
        AddParameter(command, "@notebookName", page.NotebookName);
        AddParameter(command, "@sectionName", page.SectionName);
        AddParameter(command, "@pageTitle", page.PageTitle);
        AddParameter(command, "@contentText", page.ContentText);
        AddParameter(command, "@contentHtml", page.ContentHtml);
        AddParameter(command, "@createdDateTime", page.CreatedDateTime);
        AddParameter(command, "@lastModifiedDateTime", page.LastModifiedDateTime);
        AddParameter(command, "@webUrl", page.WebUrl);
        AddParameter(command, "@importedAt", DateTime.UtcNow);
    }

    // Adds one value to a SQL command without writing the value directly into SQL text.
    private static void AddParameter(DbCommand command, string name, object? value)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
