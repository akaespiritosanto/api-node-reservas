# API Reservas KB

This project is an ASP.NET Core API that copies information from a reservations database into a knowledge database.

The main idea is simple:

1. The source database has normal business tables, like `Reserva` and `ProdutoReservado`.
2. The file `Data/reservas-mapeamentos.json` explains how each source table should be converted.
3. The API reads new or updated rows from the source database.
4. The API saves the converted information in the knowledge database as `Node`, `Context` and `Arc` records.

The project can also import Microsoft OneNote pages:

1. The user signs in with Microsoft.
2. The API reads the user's OneNote pages through Microsoft Graph.
3. The API saves those pages into `OneNotePageImport` in the source database.
4. The file `Data/onenote-mapeamentos.json` explains how those imported pages become knowledge records.

## What Each Part Does

`Controllers`

These files expose the HTTP endpoints used by Swagger.

- `Mapeamentos_ReservasController.cs` manages Reservas mapping configurations.
- `Mapeamentos_OneNoteController.cs` manages OneNote mapping configurations.
- `Processamento_ReservasController.cs` runs Reservas processing.
- `Processamento_OneNoteController.cs` runs OneNote login, import and processing.

`Services`

These files contain the main project logic.

- `Reservas/MappingRepository.cs` reads and writes `Data/reservas-mapeamentos.json`.
- `Reservas/MappingRepository.Helpers.cs` contains small helper methods used by `MappingRepository.cs`.
- `Reservas/MappingRepository.Defaults.cs` contains the default Reservas mappings created on first run.
- `Reservas/KnowledgeProcessingService.cs` coordinates the full processing flow.
- `Reservas/KnowledgeProcessingService.Reading.cs` reads rows from the source database.
- `Reservas/KnowledgeProcessingService.Mapping.cs` converts one database row to a DTO.
- `Reservas/KnowledgeProcessingService.Saving.cs` coordinates saving one converted record.
- `Reservas/KnowledgeProcessingService.Nodes.cs` saves the main `Node` record.
- `Reservas/KnowledgeProcessingService.Contexts.cs` saves extra `Context` rows.
- `Reservas/KnowledgeProcessingService.Arcs.cs` saves relation `Arc` rows.
- `Reservas/KnowledgeProcessingService.Sql.cs` builds the SQL query used to read source rows.
- `Reservas/KnowledgeProcessingService.Validation.cs` checks the basic mapping safety rules.
- `Reservas/KnowledgeProcessingService.ColumnSelection.cs` chooses which source columns SQL should read.
- `Reservas/KnowledgeProcessingService.TableColumns.cs` checks if mapped columns exist in the source table.
- `OneNote/OneNoteMappingRepository.cs` reads and writes `Data/onenote-mapeamentos.json`.
- `OneNote/OneNoteMappingRepository.Helpers.cs` contains small helper methods used by `OneNoteMappingRepository.cs`.
- `OneNote/OneNoteMappingRepository.Defaults.cs` contains the default OneNote mapping created on first run.
- `OneNote/MicrosoftGraphAuthService.cs` creates the Microsoft login URL and exchanges login codes for tokens.
- `OneNote/OneNoteImportService.cs` coordinates the OneNote import flow.
- `OneNote/OneNoteImportService.Graph.cs` reads the user and pages from Microsoft Graph.
- `OneNote/OneNoteImportService.Database.cs` creates and writes the `OneNotePageImport` table.
- `OneNote/OneNoteImportService.Json.cs` reads Graph JSON and converts OneNote HTML to text.
- `OneNote/OneNoteImportService.PageData.cs` stores one imported page while the import is running.
- `OneNote/OneNoteTokenStore.cs` temporarily stores the Microsoft access token in memory.
- `DotEnvService.cs` loads values from the `.env` file.

`Swagger`

These files only affect the documentation page.

- `SwaggerEndpointOrder.cs` keeps Swagger sections and endpoints in beginner-friendly order.

`Data`

These files configure the two database connections.

- `ReservasDbContext.cs` connects to the source reservations database.
- `KnowledgeDbContext.cs` connects to the knowledge database.
- `reservas-mapeamentos.json` stores the Reservas mapping configuration and the processing checkpoint.
- `onenote-mapeamentos.json` stores the OneNote mapping configuration and its own checkpoint.

`Models`

These classes represent database tables and mapping structures.

`Dtos`

These classes represent data sent to or returned from the API.

## Important Words

`Mapping`

A mapping explains how a table from the source database becomes knowledge database records.

Example: the `Reserva` mapping says that:

- `referencia` becomes `Reference`
- `observacoes` becomes `Descricao`
- `id` becomes `IdInformacao`
- `estado`, `estado_pagamento` and `id_canal` become `Context` values

`Node`

A `Node` is the main knowledge record.

Example: one reservation can become one `Node`.

`Context`

A `Context` is extra information connected to a `Node`.

Example: reservation status, payment status or channel id.

One source row can create more than one `Context` row.

Example: if the `Reserva` mapping has 3 context fields:

```json
"Contexts": [
  "estado",
  "estado_pagamento",
  "id_canal"
]
```

Then each processed `Reserva` row creates 1 `Node` row and up to 3 `Context` rows.

So if you process:

```txt
2 Reserva rows
2 ProdutoReservado rows
```

and each mapping has 3 context fields, the result can be:

```txt
4 Node rows
12 Context rows
```

This is normal. `Context` is not one row per original record. It is one row per extra value connected to a `Node`.

`Arc`

An `Arc` is a relation between two `Node` records.

Example: a reserved product can point to the reservation it belongs to.

`Checkpoint`

A checkpoint tells the API what was already processed.

The checkpoint is stored in `Data/reservas-mapeamentos.json`:

```json
"LastProcessedId": 9,
"LastSuccessfulProcessingDate": "2026-05-27T11:11:14.2919048Z"
```

This prevents the API from processing the same old rows every time.

For OneNote, the checkpoint is stored separately in `Data/onenote-mapeamentos.json`.

## Requirements

- .NET SDK compatible with `net10.0`
- SQL Server
- Two databases:
  - source database, for example `api_aggregations`
  - knowledge database, for example `api_node_reservas`

## Environment Variables

The project reads configuration from `.env`.

Example:

```env
API_KEY=test

RESERVAS_DB_CONNECTION_STRING=Server=YOUR_SERVER;Database=api_aggregations;Trusted_Connection=true;TrustServerCertificate=true;

KB_DB_CONNECTION_STRING=Server=YOUR_SERVER;Database=api_node_reservas;Trusted_Connection=true;TrustServerCertificate=true;

AZURE_TENANT_ID=common
AZURE_CLIENT_ID=YOUR_MICROSOFT_APP_CLIENT_ID
AZURE_CLIENT_SECRET=YOUR_MICROSOFT_APP_CLIENT_SECRET
AZURE_REDIRECT_URI=http://localhost:5253/api/onenote/callback
```

`API_KEY`

The key that must be sent in Swagger using the `x-api-key` header.

`RESERVAS_DB_CONNECTION_STRING`

Connection string for the source database.

`KB_DB_CONNECTION_STRING`

Connection string for the knowledge database.

`AZURE_TENANT_ID`

Microsoft tenant used for login. Use `common` for local testing with work, school or personal Microsoft accounts.

`AZURE_CLIENT_ID`

Application client id from the Microsoft Entra app registration.

`AZURE_CLIENT_SECRET`

Client secret value from the Microsoft Entra app registration. Copy the secret value when Azure shows it, because it is only shown once.

`AZURE_REDIRECT_URI`

The local callback endpoint Microsoft redirects to after login. For the HTTP launch profile, use:

```txt
http://localhost:5253/api/onenote/callback
```

## Microsoft Azure Setup For OneNote

OneNote import uses Microsoft Graph. Microsoft Graph OneNote access needs delegated permissions, which means the user signs in and the API reads OneNote on behalf of that signed-in user.

Microsoft's OneNote API documentation says app-only authentication is not supported for OneNote, so this project uses user login instead.

1. Open Azure Portal:

```txt
https://portal.azure.com
```

2. Search for `Microsoft Entra ID`.

3. Open:

```txt
App registrations -> New registration
```

4. Fill the app registration:

```txt
Name: api-node-reservas-onenote
Supported account types: Accounts in any organizational directory and personal Microsoft accounts
```

5. After creating the app, copy:

```txt
Application (client) ID -> AZURE_CLIENT_ID
Directory (tenant) ID -> AZURE_TENANT_ID
```

For local testing, `AZURE_TENANT_ID=common` is usually easier.

6. Add the redirect URI:

```txt
Authentication -> Add a platform -> Web
Redirect URI: http://localhost:5253/api/onenote/callback
```

The redirect URI in Azure must be exactly the same as `AZURE_REDIRECT_URI` in `.env`.

7. Create the client secret:

```txt
Certificates & secrets -> Client secrets -> New client secret
```

Copy the secret `Value` into:

```env
AZURE_CLIENT_SECRET=YOUR_SECRET_VALUE
```

8. Add Microsoft Graph delegated permissions:

```txt
API permissions -> Add a permission -> Microsoft Graph -> Delegated permissions
```

Add:

```txt
User.Read
Notes.Read
offline_access
```

`Notes.Read` allows the app to read OneNote notebooks on behalf of the signed-in user.

Official Microsoft documentation:

- Register an application: https://learn.microsoft.com/en-us/graph/auth-register-app-v2
- OneNote API overview: https://learn.microsoft.com/en-us/graph/api/resources/onenote-api-overview
- Graph permissions reference: https://learn.microsoft.com/en-us/graph/permissions-reference

## How To Run

From the project folder:

```bash
dotnet run
```

Then open Swagger in the browser.

The local URL depends on your launch settings, but it is usually something like:

```txt
http://localhost:5253/swagger
```

## Swagger Authentication

All API requests need an API key.

In Swagger, click `Authorize` and enter the value from `.env`.

Example:

```txt
test
```

The API expects this header:

```txt
x-api-key: test
```

The Microsoft callback endpoint does not need the API key because Microsoft redirects the browser there after login.

## Main Endpoints

Swagger is organized into four sections:

- `Mapeamentos_Reservas`
- `Mapeamentos_OneNote`
- `Processamento_Reservas`
- `Processamento_OneNote`

### Reservas Endpoints

List all mappings:

```txt
GET /api/mapeamentos
```

Get one mapping by id:

```txt
GET /api/mapeamentos/1
```

Get one mapping by table name:

```txt
GET /api/mapeamentos/tabela/Reserva
```

Process by mapping id:

```txt
POST /api/processamento/1?limit=100
```

Process by table name:

```txt
POST /api/processamento/tabela/Reserva?limit=100
```

### OneNote Endpoints

The OneNote flow in Swagger is ordered like the real import flow.

Create the Microsoft login URL:

```txt
GET /api/onenote/login-url
```

Microsoft redirects here after login:

```txt
GET /api/onenote/callback
```

Check if the API has a temporary Microsoft token:

```txt
GET /api/onenote/token-status
```

Import OneNote pages into the source database:

```txt
POST /api/onenote/import
```

Create, update or delete custom OneNote mappings the same way as Reservas mappings.

List OneNote mappings:

```txt
GET /api/onenote/mapeamentos
```

Create a OneNote mapping:

```txt
POST /api/onenote/mapeamentos
```

Update a OneNote mapping:

```txt
PUT /api/onenote/mapeamentos/1
```

Delete a OneNote mapping:

```txt
DELETE /api/onenote/mapeamentos/1
```

Process imported OneNote pages into the knowledge database:

```txt
POST /api/onenote/processamento/1?limit=100
```

Process imported OneNote pages by table name:

```txt
POST /api/onenote/processamento/tabela/OneNotePageImport?limit=100
```

## Correct Processing Order

Process `Reserva` first:

```txt
POST /api/processamento/tabela/Reserva?limit=100
```

Then process `ProdutoReservado`:

```txt
POST /api/processamento/tabela/ProdutoReservado?limit=100
```

This order matters because `ProdutoReservado` can create an `Arc` relation pointing to a `Reserva` node.

If the reservation node does not exist yet, the relation cannot be created.

## How Processing Works

When you process a mapping, the API:

1. Opens `Data/reservas-mapeamentos.json` or `Data/onenote-mapeamentos.json`.
2. Finds the mapping by id or table name.
3. Reads only rows that are new or updated.
4. Converts each source row to a `KnowledgeRecordDto`.
5. Saves or updates a `Node`.
6. Removes old `Context` and `Arc` rows for that node.
7. Adds the new `Context` rows based on the mapping's `Contexts` list.
8. Adds the new `Arc` rows based on parent/relation mappings.
9. Updates the checkpoint in the same mapping file.

## Why Nothing Appears After Deleting SQL Data Manually

If you manually delete records from the knowledge database and then process again, the API may not recreate them.

That happens because the checkpoint in `Data/reservas-mapeamentos.json` still says those records were already processed.

Example:

```json
"LastProcessedId": 9,
"LastSuccessfulProcessingDate": "2026-05-27T11:11:14.2919048Z"
```

The API then searches only for rows newer than that id/date.

To process everything again, reset the checkpoint:

```json
"LastProcessedId": 0,
"LastSuccessfulProcessingDate": null
```

Then run the processing endpoint again.

## Testing Workflow For Beginners

If you want to test from the beginning:

1. Clear the generated data from the knowledge database if needed.
2. Open `Data/reservas-mapeamentos.json`.
3. Set `LastProcessedId` to `0`.
4. Set `LastSuccessfulProcessingDate` to `null`.
5. Run `POST /api/processamento/tabela/Reserva?limit=100`.
6. Run `POST /api/processamento/tabela/ProdutoReservado?limit=100`.
7. Check the `Node`, `Context` and `Arc` tables in the knowledge database.

## Mapping Example

A simple `Reserva` mapping looks like this:

```json
{
  "Id": 1,
  "TableName": "Reserva",
  "DetectionMethod": "Id",
  "IdFieldName": "id",
  "CreationDateFieldName": "data_pedido",
  "UpdateDateFieldName": "data_actualizacao",
  "LastProcessedId": 0,
  "LastSuccessfulProcessingDate": null,
  "Mapping": {
    "Tabela": "Reserva",
    "Tipo": "Reserva",
    "TipoE": "Reserva",
    "Reference": "referencia",
    "Descricao": "observacoes",
    "IdInformacao": "id",
    "Par1": "numero",
    "Par2": "referencia",
    "Par3": "nome_utilizador_confirmacao",
    "Par4": "",
    "Par5": "",
    "Par6": "",
    "Par7": "",
    "Contexts": [
      "estado",
      "estado_pagamento",
      "id_canal"
    ],
    "Parent": [],
    "Relations": []
  }
}
```

## Common Problems

`401 Unauthorized`

The `x-api-key` header is missing or wrong.

Check `.env` and Swagger authorization.

`Mapeamento nao encontrado`

The mapping id or table name does not exist in `Data/mapeamentos.json`.

`Nome SQL invalido`

A table or column name in the mapping has unsafe characters.

Only simple SQL names are accepted, like:

```txt
Reserva
data_actualizacao
id_reserva
```

`Arc target was not found`

The API tried to create a relation to another node, but that target node does not exist yet.

Most common fix:

```txt
Process Reserva before ProdutoReservado.
```

## Useful Commands

Build the project:

```bash
dotnet build --no-restore
```

Run tests:

```bash
dotnet test --no-build
```

Run the API:

```bash
dotnet run
```
